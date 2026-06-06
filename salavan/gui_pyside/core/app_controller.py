import os
import subprocess
import threading
import time
import re
from PySide6.QtCore import QObject, Signal
from PySide6.QtGui import QImage
from services.capture_service import CaptureService
from services.overlay_service import OverlayService
from engine.lua_runner import LuaRunner
from engine.monitor_runner import UnityLogMonitor
from engine.hooks import GameHooks
from ui.windows.lock_overlay import LockOverlay
from services.hotkey_service import HotkeyService

class AppController(QObject):
    """
    Central orchestrator connecting UI signals to background services.
    """
    # Signals emitted back to the UI
    log_added = Signal(str, str, str) # step, status, msg
    test_started = Signal()
    test_finished = Signal(str) # status
    stage_changed = Signal(str)
    steps_updated = Signal(list, int)  # all steps, current index
    prompt_missing_template_sig = Signal(str) # Prompts Auto-Mapper
    preview_frame = Signal(QImage)
    game_rect_updated = Signal(int, int, int, int)
    pause_toggled = Signal(bool)
    wait_started = Signal(str, float)
    wait_progress = Signal(float)
    wait_finished = Signal()
    
    def __init__(self, config, logger, parent=None):
        super().__init__(parent)
        self.config = config
        self.logger = logger
        
        self.capture_service = CaptureService()
        self.capture_service.frame_ready.connect(self.preview_frame)
        self.capture_service.recorder_error.connect(lambda e: self.log_message("RECORDER", "FAIL", e))
        self.capture_service.start()
        
        self.overlay_service = OverlayService()
        self.overlay_service.game_rect_updated.connect(self._on_game_rect_updated)
        
        from core.paths import get_base_dir
        self.game_hooks = GameHooks(os.path.join(get_base_dir(), "templates"))
        
        self.lua_runner = None
        self.log_monitor = None
        self.game_process = None
        self.game_logs = []
        
        # Test Execution State Toggles
        self.pause_event = threading.Event()
        self.pause_event.set()
        self.stop_flag = False
        self.current_step_idx = None
        self.current_step_is_skipped = False
        self.skip_current_step = False
        self.run_to_next_step = False
        self.auto_skip_to_step_idx = None
        self.pause_at_target = False
        self.current_scenario_name = None
        self.skipped_steps = {}
        
        self.missing_template_resolved_event = threading.Event()
        self.missing_template_coords = None
        self.step_status_overrides = {}
        
        self.lock_overlay = LockOverlay()
        self.steps_updated.connect(self.lock_overlay.update_steps)
        
        from ui.windows.log_overlay import LogOverlay
        self.log_overlay = LogOverlay()
        
        from ui.windows.timer_overlay import TimerOverlay
        self.timer_overlay = TimerOverlay()
        self.wait_started.connect(lambda title, sec: self.timer_overlay.show_wait(title, sec, self.config.game_width))
        self.wait_progress.connect(self.timer_overlay.update_progress)
        self.wait_finished.connect(self.timer_overlay.hide_wait)
        
        self.hotkey_service = HotkeyService()
        self.hotkey_service.pause_signal.connect(self._hotkey_pause)
        self.hotkey_service.resume_signal.connect(self._hotkey_resume)
        self.hotkey_service.kill_signal.connect(self.abort_test)
        self.hotkey_service.toggle_logs_signal.connect(self.log_overlay.toggle_visibility)
        
        self.config.config_changed.connect(self._sync_settings)
        self._sync_settings()

    def _sync_settings(self):
        active_game = self.config.get_active_game()
        title = active_game.get("window_title", "") if active_game else ""
        proc = active_game.get("process_name", "") if active_game else ""
        
        if self.config.hook_unity_editor:
            title = "Unity Editor"
            proc = "Unity.exe"
            
        self.overlay_service.update_settings(proc, title, self.config.auto_sync_ui)
        if self.config.auto_sync_ui:
            self.overlay_service.start_polling()
        else:
            self.overlay_service.stop_polling()

    def _on_game_rect_updated(self, x, y, w, h, hwnd=0):
        self.game_hooks.set_game_rect((x, y, w, h, hwnd))
        self.capture_service.set_game_window(x, y, w, h, hwnd)
        self.game_rect_updated.emit(x, y, w, h)

    def log_message(self, step, status, msg):
        self.logger.log(step, status, msg)
        self.log_added.emit(step, status, msg)
        if hasattr(self, 'log_overlay'):
            self.log_overlay.append_log(step, status, msg)

    def is_game_running(self):
        """Returns True if a game process is alive and tracked."""
        if self.game_process is None:
            return False
        try:
            return self.game_process.poll() is None
        except Exception:
            return False

    def kill_game(self):
        """Forcefully terminate the tracked game process and any stray instances."""
        import subprocess as sp
        exe_name = ""
        active_game = self.config.get_active_game()
        if active_game:
            exe_path = active_game.get("active_exe_path", "")
            exe_name = os.path.basename(exe_path)

        if self.game_process is not None:
            try:
                self.game_process.kill()
                self.game_process.wait(timeout=5)
            except Exception:
                pass
            self.game_process = None

        # Also kill any stray instances by exe name
        if exe_name:
            try:
                sp.run(["taskkill", "/F", "/IM", exe_name], capture_output=True)
            except Exception:
                pass
        self.log_message("SYSTEM", "INFO", "Game process terminated.")

    def launch_game(self, force_restart=False):
        if self.config.hook_unity_editor:
            self.log_message("SYSTEM", "INFO", "Hook Unity Editor is True. Not launching standalone exe.")
            return True

            try:
                import pygetwindow as gw
                windows = gw.getWindowsWithTitle(self.config.get_active_game().get("window_title", "Maou-Sama-TD"))
                if windows:
                    win = windows[0]
                    if not win.isActive:
                        win.activate()
            except Exception: pass
            return True

        # Kill old process if still alive before spawning a new one
        if self.is_game_running():
            self.log_message("SYSTEM", "INFO", "Killing existing game process before relaunch...")
            self.kill_game()
            import time
            time.sleep(2)  # Give OS time to release window handles

        active_game = self.config.get_active_game()
        exe = active_game.get("active_exe_path", "")
        if not exe or not os.path.exists(exe):
            self.log_message("SYSTEM", "FAIL", "Game EXE path is invalid.")
            return False
            
        self.log_message("SYSTEM", "INFO", f"Launching Game: {exe}")
        try:
            args = [
                exe,
                "-automation-key", self.config.automation_key
            ]
            env_args = os.environ.get("LUA_TEST_ARGS", "")
            if env_args:
                args.extend(env_args.split())
            else:
                args.extend([
                    "-screen-fullscreen", "1",
                    "-screen-width", str(self.config.game_width),
                    "-screen-height", str(self.config.game_height)
                ])
            
            # Force log output
            log_path = os.path.expandvars(active_game.get("log_path", ""))
            if log_path:
                args.extend(["-logfile", log_path])
                
            self.game_process = subprocess.Popen(args)
            
            # Wait for window and verify process stays alive
            import time
            import pygetwindow as gw
            window_found = False
            for _ in range(40):  # Wait up to 20 seconds
                if self.game_process.poll() is not None:
                    self.log_message("SYSTEM", "FAIL", f"Game process exited immediately with code {self.game_process.returncode}")
                    return False
                    
                try:
                    target_title = active_game.get("window_title", "Maou-Sama-TD")
                    all_windows = gw.getWindowsWithTitle(target_title)
                    windows = [w for w in all_windows if w.title == target_title]
                    self.log_message("SYSTEM", "DEBUG", f"Found matching windows: {[w.title for w in all_windows]} exact: {len(windows)}")
                    if windows:
                        win = windows[-1]
                        window_found = True
                        try:
                            if not win.isActive:
                                win.activate()
                        except Exception as e:
                            self.log_message("SYSTEM", "DEBUG", f"Could not activate window: {e}")
                        break
                except Exception as ex:
                    self.log_message("SYSTEM", "DEBUG", f"Window matching error: {ex}")
                    pass
                time.sleep(0.5)
                
            if not window_found:
                self.log_message("SYSTEM", "FAIL", "Game process started but window did not appear within timeout.")
                return False
                
            return True
        except Exception as e:
            self.log_message("SYSTEM", "FAIL", f"Failed to launch game: {str(e)}")
            return False


    def clear_save_data(self):
        active_game = self.config.get_active_game()
        if not active_game: return
        
        paths = active_game.get("save_paths", [])
        for path in paths:
            exp_path = os.path.expandvars(path)
            if os.path.exists(exp_path):
                try:
                    os.remove(exp_path)
                    self.log_message("SYSTEM", "INFO", f"Deleted save: {exp_path}")
                except Exception as e:
                    self.log_message("SYSTEM", "FAIL", f"Could not delete save: {str(e)}")

    def start_test(self, scenario_path, scenario_name, logs_dir, capture_dir):
        if self.lua_runner and self.lua_runner.isRunning():
            return
            
        self.stop_flag = False
        self.pause_event.set()
        self.current_scenario_name = scenario_name
        self.test_started.emit()
        
        log_file = os.path.join(logs_dir, f"{scenario_name}_report.txt")
        self.logger.initialize(log_file, scenario_name)
        
        self.lock_overlay.show_overlay(self.config.game_width)
        self.hotkey_service.start()
        
        if self.config.record_test:
            out_vid = os.path.join(capture_dir, f"{scenario_name}_capture.avi")
            self.capture_service.start_recording(out_vid, fps=10)
            
        self.capture_service.start()
        
        if self.config.dev_build_mode:
            active_game = self.config.get_active_game()
            log_path = os.path.expandvars(active_game.get("log_path", ""))
            if os.path.exists(log_path):
                start_pos = os.path.getsize(log_path)
                self.log_monitor = UnityLogMonitor(log_path, start_pos)
                self.log_monitor.error_detected.connect(self.log_message)
                self.log_monitor.log_line_read.connect(self._on_game_log_line)
                self.log_monitor.start()

        self.lua_runner = LuaRunner(scenario_name, scenario_path, self.game_hooks, self)
        self.lua_runner.log_emitted.connect(self.log_message)
        self.lua_runner.stage_changed.connect(self.stage_changed)
        self.lua_runner.test_finished.connect(self._on_test_finished)
        self.lua_runner.start()

    def abort_test(self):
        self.stop_flag = True
        self.pause_event.set()
        if self.lua_runner and self.lua_runner.isRunning():
            self.lua_runner.terminate()
            self.lua_runner.wait()
            self._on_test_finished("Aborted")

    def _on_test_finished(self, status):
        self.capture_service.stop_recording()
        self.lock_overlay.hide_overlay()
        self.hotkey_service.stop()
        
        if self.log_monitor:
            self.log_monitor.stop()
            self.log_monitor = None
            
        if self.game_process:
            try:
                self.game_process.terminate()
            except Exception:
                pass
            self.game_process = None
            
        # Reset debugger stepping / skip variables
        self.current_step_idx = None
        self.current_step_is_skipped = False
        self.skip_current_step = False
        self.run_to_next_step = False
        self.auto_skip_to_step_idx = None
        self.pause_at_target = False
        self.current_scenario_name = None
        self.game_logs = []
        
        active_game = self.config.get_active_game()
        if active_game:
            from core.paths import get_base_dir
            xml_path = os.path.join(
                get_base_dir(),
                "reports",
                active_game["id"],
                "junit",
                f"{self.logger.scenario_name}_junit.xml"
            )
            os.makedirs(os.path.dirname(xml_path), exist_ok=True)
            self.logger.write_xml_report(xml_path)
            
        self.test_finished.emit(status)

    def _on_game_log_line(self, line):
        self.game_logs.append(line)

    # ── APIs for Lua Engine ──
    def set_stage_lbl(self, text):
        self.skip_current_step = False
        self.stage_changed.emit(text)
        
        selected_scenario = self.current_scenario_name
        if selected_scenario:
            from core.paths import get_base_dir
            scenarios_dir = os.path.join(get_base_dir(), "scenarios", self.config.get_active_game()["id"])
            script_path = os.path.join(scenarios_dir, f"{selected_scenario}.lua")
            
            steps = []
            if os.path.exists(script_path):
                with open(script_path, "r", encoding="utf-8") as f:
                    content = f.read()
                steps = re.findall(r'set_stage\([\'"]([^\'"]+)[\'"]\)', content)
                
            active_idx = -1
            text_clean = re.sub(r'^\d+\.\s*', '', text).lower().strip()
            for s_idx, step_name in enumerate(steps):
                s_clean = re.sub(r'^\d+\.\s*', '', step_name).lower().strip()
                if s_clean in text_clean or text_clean in s_clean:
                    active_idx = s_idx
                    break
                    
            self.current_step_idx = active_idx if active_idx != -1 else None
            
            if self.auto_skip_to_step_idx is not None and active_idx != -1:
                if active_idx < self.auto_skip_to_step_idx:
                    self.current_step_is_skipped = True
                    self.log_message("DEBUG", "INFO", f"Auto-skipping step '{text}' (rewinding/jumping)...")
                else:
                    self.auto_skip_to_step_idx = None
                    if getattr(self, 'pause_at_target', False):
                        self.pause_at_target = False
                        self.pause_event.clear()
                        self.log_message("DEBUG", "INFO", f"Reached target step: '{text}'. Pausing execution.")
                        
            if active_idx != -1 and active_idx < len(steps):
                step_name = steps[active_idx]
                if self.skipped_steps.get((selected_scenario, step_name), False):
                    self.current_step_is_skipped = True
                    self.log_message("DEBUG", "INFO", f"Skipping step '{text}' (marked as skip)...")

            self.steps_updated.emit(steps, active_idx)

    def _hotkey_pause(self):
        self.pause_event.clear()
        self.lock_overlay.hide_overlay()
        self.log_message("SYSTEM", "INFO", "Automation Paused via F8.")
        self.pause_toggled.emit(True)

    def _hotkey_resume(self):
        self.pause_event.set()
        self.lock_overlay.show_overlay(self.config.game_width)
        self.log_message("SYSTEM", "INFO", "Automation Resumed via F9.")
        self.pause_toggled.emit(False)

                    
        if getattr(self, 'run_to_next_step', False):
            self.run_to_next_step = False
            self.pause_event.clear()
            self.log_message("DEBUG", "INFO", f"Stepped to: '{text}'. Pausing execution.")

    def check_paused(self):
        while not self.pause_event.is_set():
            if self.stop_flag:
                raise InterruptedError("Aborted by user.")
            time.sleep(0.1)

    def sleep_wait(self, seconds):
        if getattr(self, 'current_step_is_skipped', False) or getattr(self, 'skip_current_step', False):
            return
        self.wait_started.emit("Waiting...", float(seconds))
        start_time = time.time()
        while time.time() - start_time < seconds:
            self.check_paused()
            if self.stop_flag:
                self.wait_finished.emit()
                raise InterruptedError()
            if getattr(self, 'skip_current_step', False):
                break
            self.wait_progress.emit(float(seconds - (time.time() - start_time)))
            time.sleep(0.1)
        self.wait_finished.emit()

    def click_game_relative(self, rx, ry):
        if getattr(self, 'current_step_is_skipped', False) or getattr(self, 'skip_current_step', False):
            return True
        self.check_paused()
        return self.game_hooks.click_relative(rx, ry)

    def drag_game_relative(self, rx1, ry1, rx2, ry2, duration=0.5):
        if getattr(self, 'current_step_is_skipped', False) or getattr(self, 'skip_current_step', False):
            return True
        self.check_paused()
        return self.game_hooks.drag_relative(rx1, ry1, rx2, ry2, duration)

    def toggle_pause(self):
        if self.pause_event.is_set():
            self.pause_event.clear()
            self.log_message("HUD", "INFO", "Sequence execution SUSPENDED.")
            self.pause_toggled.emit(True)
        else:
            self.pause_event.set()
            self.log_message("HUD", "INFO", "Sequence execution RESUMED.")
            self.pause_toggled.emit(False)

    def toggle_manual_recording(self, codec_val, fps_val_str):
        if self.capture_service.recording_path:
            self.capture_service.stop_recording()
            self.log_message("Recorder", "INFO", "Manual screen recording stopped.")
            return False
        else:
            from core.paths import get_base_dir
            recordings_dir = os.path.join(get_base_dir(), "recordings")
            os.makedirs(recordings_dir, exist_ok=True)
            
            ext = ".avi"
            codec_str = "MJPG"
            if "IVF" in codec_val or "VP8" in codec_val:
                ext = ".ivf"
                codec_str = "VP80"
            elif "MP4" in codec_val:
                ext = ".mp4"
                codec_str = "mp4v"
            
            fps = 30
            if "60" in fps_val_str:
                fps = 60
            elif "15" in fps_val_str:
                fps = 15
                
            out_vid = os.path.join(recordings_dir, f"manual_recording_{int(time.time())}{ext}")
            self.capture_service.start_recording(out_vid, codec_str=codec_str, fps=fps)
            self.log_message("Recorder", "INFO", f"Manual screen recording started: {os.path.basename(out_vid)}")
            return True

    def next_step(self):
        if not (self.lua_runner and self.lua_runner.isRunning()):
            return
        self.log_message("DEBUG", "INFO", "Skipping current step...")
        self.skip_current_step = True
        if not self.pause_event.is_set():
            self.run_to_next_step = True
            self.pause_event.set()

    def prev_step(self):
        if not (self.lua_runner and self.lua_runner.isRunning()):
            return
        if self.current_step_idx is None or self.current_step_idx <= 0:
            self.log_message("DEBUG", "WARNING", "No previous step to jump to.")
            return
        target_idx = self.current_step_idx - 1
        self.log_message("DEBUG", "INFO", f"Rewinding to step index {target_idx}...")
        
        scenario_path = self.lua_runner.script_path
        scenario_name = self.lua_runner.scenario_name
        
        self.abort_test()
        
        def restart():
            self.lua_runner.wait()
            self.start_test_to_step(scenario_path, scenario_name, target_idx)
            
        threading.Thread(target=restart, daemon=True).start()

    def repeat_step(self):
        if not (self.lua_runner and self.lua_runner.isRunning()):
            return
        if self.current_step_idx is None:
            self.log_message("DEBUG", "WARNING", "No current step to repeat.")
            return
        target_idx = self.current_step_idx
        self.log_message("DEBUG", "INFO", f"Repeating step index {target_idx}...")
        
        scenario_path = self.lua_runner.script_path
        scenario_name = self.lua_runner.scenario_name
        
        self.abort_test()
        
        def restart():
            self.lua_runner.wait()
            self.start_test_to_step(scenario_path, scenario_name, target_idx)
            
        threading.Thread(target=restart, daemon=True).start()

    def start_test_to_step(self, scenario_path, scenario_name, target_idx):
        self.stop_flag = False
        self.pause_event.set()
        self.auto_skip_to_step_idx = target_idx
        self.pause_at_target = True
        
        from core.paths import get_base_dir
        logs_dir = os.path.join(get_base_dir(), "reports", self.config.get_active_game()["id"])
        capture_dir = os.path.join(get_base_dir(), "recordings")
        os.makedirs(logs_dir, exist_ok=True)
        os.makedirs(capture_dir, exist_ok=True)
        
        self.start_test(scenario_path, scenario_name, logs_dir, capture_dir)

    def shutdown(self):
        self.capture_service.stop()
        self.overlay_service.stop_polling()
        if self.log_monitor:
            self.log_monitor.stop()
        self.abort_test()

    def read_game_state(self):
        try:
            from core.paths import get_base_dir
            path = os.path.join(get_base_dir(), "game_state.json")
            
            for _ in range(10): # increased retries
                if os.path.exists(path):
                    import json
                    try:
                        with open(path, "r", encoding="utf-8-sig") as f:
                            content = f.read().strip()
                            if content.startswith("{"):
                                state = json.loads(content)
                                from crypto_utils import merge_map_tiles_into_state
                                merge_map_tiles_into_state(state, os.path.dirname(path), self.config.automation_key)
                                self._last_valid_state = state
                                return state
                            else:
                                from crypto_utils import decrypt_state, merge_map_tiles_into_state
                                decrypted = decrypt_state(content, self.config.automation_key)
                                if decrypted:
                                    state = json.loads(decrypted)
                                    merge_map_tiles_into_state(state, os.path.dirname(path), self.config.automation_key)
                                    self._last_valid_state = state
                                    return state
                    except Exception as e:
                        import traceback
                        self.log_message("SYSTEM", "DEBUG", f"Error reading game state: {e} \n{traceback.format_exc()}")
                        pass
                import time
                time.sleep(0.05)
        except Exception as e:
            pass
            
        return getattr(self, "_last_valid_state", None)

    def click_element_by_id(self, element_id):
        if getattr(self, 'current_step_is_skipped', False) or getattr(self, 'skip_current_step', False):
            return True
        self.check_paused()
        state = self.read_game_state()
        from crypto_utils import find_element_in_state
        elem = find_element_in_state(element_id, state)
        if elem:
            return self.click_game_relative(elem["x"], elem["y"])
        else:
            self.log_message("CLICK", "FAIL", f"UI Element '{element_id}' not found.")
            return False

    def drag_elements(self, source_id, target_id, duration=1.0):
        if getattr(self, 'current_step_is_skipped', False) or getattr(self, 'skip_current_step', False):
            return True
        self.check_paused()
        state = self.read_game_state()
        from crypto_utils import find_element_in_state
        source = find_element_in_state(source_id, state)
        target = find_element_in_state(target_id, state)
        if not source:
            self.log_message("DRAG", "FAIL", f"Source UI Element '{source_id}' not found.")
            return False
        if not target:
            self.log_message("DRAG", "FAIL", f"Target UI Element '{target_id}' not found.")
            return False
        return self.drag_game_relative(source["x"], source["y"], target["x"], target["y"], duration)

    def lua_find_element(self, element_id):
        state = self.read_game_state()
        from crypto_utils import find_element_in_state
        elem = find_element_in_state(element_id, state)
        return elem
        
    def lua_wait_for_element(self, element_id, timeout=10.0):
        self.wait_started.emit(f"Wait Element: {element_id}", float(timeout))
        start_time = time.time()
        from crypto_utils import find_element_in_state
        while time.time() - start_time < timeout:
            self.check_paused()
            if self.stop_flag:
                self.wait_finished.emit()
                raise InterruptedError()
            state = self.read_game_state()
            elem = find_element_in_state(element_id, state)
            if state:
                self.log_message("SYSTEM", "DEBUG", f"Waiting for {element_id}. Keys in state: {len(state.get('elements', {}))} Elem found: {elem is not None}")
            if elem and elem.get("visible", False) and elem.get("interactable", True):
                self.wait_finished.emit()
                return elem
            self.wait_progress.emit(float(timeout - (time.time() - start_time)))
            time.sleep(0.2)
        self.wait_finished.emit()
        return None

    def lua_assert_visible(self, element_id, step_name="Assertion"):
        state = self.read_game_state()
        from crypto_utils import find_element_in_state
        elem = find_element_in_state(element_id, state)
        if elem and elem.get("visible", False):
            self.log_message(step_name, "PASS", f"UI Element '{element_id}' is visible as expected.")
            return True
        else:
            self.log_message(step_name, "FAIL", f"UI Element '{element_id}' is NOT visible.")
            raise AssertionError(f"UI Element '{element_id}' is NOT visible.")

    def lua_assert_log_contains(self, substring, timeout=10.0, step_name="Log Assertion"):
        self.wait_started.emit(f"Wait Log: {substring}", float(timeout))
        start_time = time.time()
        start_idx = len(self.game_logs)
        # Check existing first
        for i in range(max(0, start_idx - 1000), start_idx):
            if substring.lower() in self.game_logs[i].lower():
                self.log_message(step_name, "PASS", f"Log contains '{substring}'.")
                self.wait_finished.emit()
                return True
                
        while time.time() - start_time < timeout:
            self.check_paused()
            if self.stop_flag:
                self.wait_finished.emit()
                raise InterruptedError()
            
            # Check newly added logs
            current_len = len(self.game_logs)
            for i in range(start_idx, current_len):
                if substring.lower() in self.game_logs[i].lower():
                    self.log_message(step_name, "PASS", f"Log contains '{substring}'.")
                    self.wait_finished.emit()
                    return True
            start_idx = current_len
            self.wait_progress.emit(float(timeout - (time.time() - start_time)))
            time.sleep(0.1)
            
        self.wait_finished.emit()
        self.log_message(step_name, "FAIL", f"Timeout waiting for log '{substring}'.")
        raise AssertionError(f"Log did not contain '{substring}' within {timeout}s.")
