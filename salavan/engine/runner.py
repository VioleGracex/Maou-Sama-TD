import os
import time
import threading
import shutil
from lupa import LuaRuntime
from engine.monitor import UnityLogMonitorThread

LUA_ASSERTIONS_HEADER = """
function assert_true(cond, step, msg)
    if not cond then
        log_test(step, "FAIL", msg or "Assertion failed: expected true, got false")
        error("Assertion failed: " .. (msg or "expected true, got false"))
    else
        log_test(step, "PASS", msg or "Assertion passed")
    end
    return true
end

function assert_false(cond, step, msg)
    if cond then
        log_test(step, "FAIL", msg or "Assertion failed: expected false, got true")
        error("Assertion failed: " .. (msg or "expected false, got true"))
    else
        log_test(step, "PASS", msg or "Assertion passed")
    end
    return true
end

function assert_equal(expected, actual, step, msg)
    if expected ~= actual then
        local err_msg = (msg or "Assertion failed") .. " (Expected: " .. tostring(expected) .. ", Got: " .. tostring(actual) .. ")"
        log_test(step, "FAIL", err_msg)
        error(err_msg)
    else
        log_test(step, "PASS", (msg or "Assertion passed") .. " (" .. tostring(actual) .. ")")
    end
    return true
end

function assert_template(name, timeout, threshold, step)
    log_test(step, "STARTING", "Searching for visual target: " .. name)
    local pos = wait_template(name, timeout or 10, threshold or 0.8)
    if not pos then
        local err_msg = "Visual target not found: " .. name
        log_test(step, "FAIL", err_msg)
        error(err_msg)
    else
        log_test(step, "PASS", "Visual target located: " .. name)
        return pos
    end
end

function assert_not_template(name, timeout, threshold, step)
    log_test(step, "STARTING", "Verifying absence of visual target: " .. name)
    local pos = wait_template(name, timeout or 5, threshold or 0.8)
    if pos then
        local err_msg = "Visual target unexpectedly present: " .. name
        log_test(step, "FAIL", err_msg)
        error(err_msg)
    else
        log_test(step, "PASS", "Visual target is absent as expected: " .. name)
        return true
    end
end
"""

class TestSequenceRunner(threading.Thread):
    def __init__(self, app, scenario_name, script_path):
        super().__init__(daemon=True)
        self.app = app
        self.scenario_name = scenario_name
        self.script_path = script_path
        
    def run(self):
        active_game = self.app.config.get_active_game()
        game_title = active_game.get("title", "Maou-Sama-TD")
        game_id = active_game.get("id", "maou_sama_td")
        
        teardown_fn = None
        
        try:
            # Setup report directories in user's Documents folder dynamically
            timestamp = time.strftime("%Y%m%d_%H%M%S")
            report_dir_name = f"Report_{timestamp}"
            self.app.current_report_dir = os.path.join(
                os.path.expanduser('~'), 'Documents', game_title, 'salavan', 'Reports', report_dir_name
            )
            self.app.current_screenshots_dir = os.path.join(self.app.current_report_dir, "screenshots")
            
            os.makedirs(self.app.current_report_dir, exist_ok=True)
            os.makedirs(self.app.current_screenshots_dir, exist_ok=True)
            
            log_path = os.path.join(self.app.current_report_dir, "test_log.txt")
            self.app.report_logger.initialize(log_path, self.scenario_name)
            
            # Start continuous background video writer directly into report dir
            if self.app.config.record_test:
                rec_path = os.path.join(self.app.current_report_dir, "video.avi")
                self.app.log_message("Recorder", "INFO", "Recording redirected to report directory.")
                self.app.capture_thread.start_recording(rec_path)
                
            # Initialize Unity Log Monitor Thread if Dev Mode is checked
            self.app.log_monitor = None
            if self.app.config.dev_build_mode:
                log_path_raw = active_game.get("log_path", "")
                log_file_path = os.path.normpath(os.path.expandvars(log_path_raw))
                start_pos = 0
                if os.path.exists(log_file_path):
                    try:
                        start_pos = os.path.getsize(log_file_path)
                    except Exception:
                        pass
                self.app.log_monitor = UnityLogMonitorThread(self.app, log_file_path, start_pos)
                self.app.log_monitor.start()
                self.app.log_message("SYSTEM", "INFO", f"Unity Player.log scanning activated for: {log_file_path}")
                
            lua = LuaRuntime(unpack_returned_tuples=True)
            
            lua.globals().set_stage = self.app.set_stage_lbl
            lua.globals().log_test = self.app.log_message
            lua.globals().clear_save_data = self.app.clear_save_data
            lua.globals().launch_game = self.app.launch_game
            lua.globals().click = self.app.click_game_relative
            lua.globals().drag = self.app.drag_game_relative
            lua.globals().wait = self.app.sleep_wait
            
            def wait_template_lua(name, timeout=10, threshold=0.8):
                res = self.app.wait_for_template_coord(name, timeout, threshold)
                if res:
                    return lua.table(x=res[0], y=res[1])
                return None
            lua.globals().wait_template = wait_template_lua
            
            # Inject standard assertions
            lua.execute(LUA_ASSERTIONS_HEADER)
            
            self.app.log_message("HUD", "INFO", f"Reading scenario: {self.scenario_name}...")
            
            with open(self.script_path, "r") as f:
                lua_code = f.read()
                
            # Execute Lua Code (defines functions and runs top level scripts)
            try:
                lua.execute(lua_code)
                try:
                    teardown_fn = lua.globals().teardown
                except Exception:
                    pass
            except Exception as le:
                try:
                    teardown_fn = lua.globals().teardown
                except Exception:
                    pass
                raise le
                
            self.app.log_message("SYSTEM", "INFO", "Sequence execution successfully concluded.")
            
            # Update build status in DB
            found_build = False
            for b in self.app.config.builds:
                # Associate with current active game profile
                b_game_id = b.get("game_id", "maou_sama_td")
                if b_game_id == game_id and os.path.normpath(b.get("path")) == os.path.normpath(self.app.config.game_exe_path):
                    b["status"] = "Tested"
                    b["last_tested"] = time.strftime("%Y-%m-%d %H:%M:%S")
                    b["report_ref"] = os.path.basename(self.app.current_report_dir)
                    found_build = True
                    break
                    
            if found_build:
                self.app.config.save()
                self.app.root.after(0, self.app.refresh_builds_tree)
            
        except InterruptedError:
            self.app.log_message("SYSTEM", "INFO", "Sequence aborted.")
        except Exception as e:
            self.app.log_message("SYSTEM", "FAIL", f"Sequence crashed: {str(e)}")
            
        finally:
            # Run Teardown Hook if defined in Lua script
            if teardown_fn is not None:
                try:
                    self.app.log_message("SYSTEM", "INFO", "Executing teardown hook...")
                    teardown_fn()
                except Exception as te:
                    self.app.log_message("SYSTEM", "FAIL", f"Teardown hook crashed: {str(te)}")
            
            # Write JUnit XML Report
            try:
                xml_path = os.path.join(self.app.current_report_dir, "junit_report.xml")
                self.app.report_logger.write_xml_report(xml_path)
                self.app.log_message("SYSTEM", "INFO", "JUnit XML report generated.")
            except Exception as xe:
                self.app.log_message("SYSTEM", "FAIL", f"XML Report generation failed: {str(xe)}")
            
            # Terminate and cleanup log monitor
            if self.app.log_monitor:
                self.app.log_monitor.running = False
                self.app.log_monitor = None
                
            # Copy Unity logs to the report folder for debugging
            if self.app.config.dev_build_mode:
                log_path_raw = active_game.get("log_path", "")
                log_file_path = os.path.normpath(os.path.expandvars(log_path_raw))
                if os.path.exists(log_file_path):
                    try:
                        dest_log = os.path.join(self.app.current_report_dir, "Player_unity.log")
                        shutil.copy(log_file_path, dest_log)
                        self.app.log_message("SYSTEM", "INFO", "Unity engine logs archived to Report.")
                    except Exception:
                        pass
            
            if hasattr(self.app, 'capture_thread'):
                self.app.capture_thread.stop_recording()
            
            # Clean up screenshots folder if empty
            if self.app.current_screenshots_dir and os.path.exists(self.app.current_screenshots_dir):
                if not os.listdir(self.app.current_screenshots_dir):
                    try:
                        os.rmdir(self.app.current_screenshots_dir)
                    except Exception:
                        pass
                        
            self.app.root.after(0, self.app._reset_controls_post_run)

