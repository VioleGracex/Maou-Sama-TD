import time
from lupa import LuaRuntime
from PySide6.QtCore import QThread, Signal

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

ui = ui or {}

function ui.exists(name)
    return get_ui_element(name) ~= nil
end

function ui.wait_for(name, timeout)
    timeout = timeout or 10
    local start = os.time()
    while os.difftime(os.time(), start) < timeout do
        local e = get_ui_element(name)
        if e then
            return e
        end
        wait(0.2)
    end
    return nil
end

function ui.click(name_or_element)
    local e
    if type(name_or_element) == "string" then
        e = get_ui_element(name_or_element)
        if not e then
            error("ui.click: Element '" .. name_or_element .. "' not found")
        end
    else
        e = name_or_element
    end
    
    local click_x = e.x or e.fx or (e.ScreenPos and e.ScreenPos[1])
    local click_y = e.y or e.fy or (e.ScreenPos and e.ScreenPos[2])
    if click_x and click_y then
        click(click_x, click_y)
        return true
    else
        error("ui.click: Invalid element coordinates")
    end
end

function ui.drag(src_name_or_element, dest_name_or_element, duration)
    duration = duration or 1.0
    local src
    if type(src_name_or_element) == "string" then
        src = get_ui_element(src_name_or_element)
        if not src then
            error("ui.drag: Source element '" .. src_name_or_element .. "' not found")
        end
    else
        src = src_name_or_element
    end
    
    local dest
    if type(dest_name_or_element) == "string" then
        dest = get_ui_element(dest_name_or_element)
        if not dest then
            error("ui.drag: Destination element '" .. dest_name_or_element .. "' not found")
        end
    else
        dest = dest_name_or_element
    end
    
    local sx = src.x or src.fx or (src.ScreenPos and src.ScreenPos[1])
    local sy = src.y or src.fy or (src.ScreenPos and src.ScreenPos[2])
    local dx = dest.x or dest.fx or (dest.ScreenPos and dest.ScreenPos[1])
    local dy = dest.y or dest.fy or (dest.ScreenPos and dest.ScreenPos[2])
    
    if sx and sy and dx and dy then
        drag(sx, sy, dx, dy, duration)
        return true
    else
        error("ui.drag: Missing coordinates for source or destination")
    end
end

function ui.click_text(text_val)
    local state = get_state()
    if state and state.elements then
        for path, e in pairs(state.elements) do
            if e.type == "Button" and e.text == text_val then
                local click_x = e.x or e.fx
                local click_y = e.y or e.fy
                if click_x and click_y then
                    click(click_x, click_y)
                    return true
                end
            end
        end
    end
    error("ui.click_text: Button with text '" .. tostring(text_val) .. "' not found")
end
"""

class LuaRunner(QThread):
    log_emitted = Signal(str, str, str) # step, result, message
    stage_changed = Signal(str)
    test_finished = Signal(str) # Status: "Tested", "Aborted", "Failed"

    def __init__(self, scenario_name, script_path, game_hooks, app_controller, parent=None):
        super().__init__(parent)
        self.scenario_name = scenario_name
        self.script_path = script_path
        self.game_hooks = game_hooks
        self.app_controller = app_controller

    def run(self):
        test_status = "Tested"
        teardown_fn = None
        
        try:
            lua = LuaRuntime(unpack_returned_tuples=True)
            
            # Setup package.path to ensure require('lua_api.xxx') works regardless of cwd
            from core.paths import get_base_dir
            import os
            base_dir = get_base_dir().replace('\\', '/')
            lua.execute(f"package.path = package.path .. ';{base_dir}/?.lua;{base_dir}/?/init.lua'")
            
            lua.globals().set_stage = self.app_controller.set_stage_lbl
            lua.globals().log_test = lambda step, res, msg: self.log_emitted.emit(step, res, msg)
            lua.globals().wait = self.app_controller.sleep_wait
            
            # Dispatcher wrappers for click and drag (supports coords and element identities)
            def click_lua(arg1, arg2=None):
                try:
                    rx = float(arg1)
                    ry = float(arg2)
                    return self.app_controller.click_game_relative(rx, ry)
                except (ValueError, TypeError, IndexError):
                    return self.app_controller.click_element_by_id(arg1)
            lua.globals().click = click_lua

            def double_click_lua(arg1, arg2=None):
                try:
                    rx = float(arg1)
                    ry = float(arg2)
                    return self.app_controller.game_hooks.double_click_relative(rx, ry)
                except (ValueError, TypeError, IndexError, AttributeError):
                    return False
            lua.globals().double_click = double_click_lua
            def drag_lua(arg1, arg2, arg3=None, arg4=None, arg5=None):
                # Check if arg1 is an element ID (string)
                if isinstance(arg1, str) and not arg1.replace('.','',1).isdigit():
                    # Format 1: drag(source_id, target_id, duration)
                    if isinstance(arg2, str) and not arg2.replace('.','',1).isdigit():
                        duration = float(arg3) if arg3 is not None else 1.0
                        return self.app_controller.drag_elements(arg1, arg2, duration)
                    # Format 2: drag(source_id, target_x, target_y, duration)
                    else:
                        target_x = float(arg2)
                        target_y = float(arg3)
                        duration = float(arg4) if arg4 is not None else 1.0
                        
                        source_elem = self.app_controller.lua_find_element(arg1)
                        if not source_elem:
                            self.app_controller.log_message("DRAG", "FAIL", f"Source UI Element '{arg1}' not found.")
                            return False
                        return self.app_controller.drag_game_relative(source_elem["x"], source_elem["y"], target_x, target_y, duration)
                # Format 3: drag(x1, y1, x2, y2, duration)
                else:
                    rx1 = float(arg1)
                    ry1 = float(arg2)
                    rx2 = float(arg3)
                    ry2 = float(arg4)
                    duration = float(arg5) if arg5 is not None else 1.0
                    return self.app_controller.drag_game_relative(rx1, ry1, rx2, ry2, duration)
            lua.globals().drag = drag_lua

            def find_lua(element_id):
                elem = self.app_controller.lua_find_element(element_id)
                if elem:
                    return lua.table(
                        path=elem.get("path", ""),
                        type=elem.get("type", ""),
                        x=elem.get("x", 0.0),
                        y=elem.get("y", 0.0),
                        w=elem.get("w", 0.0),
                        h=elem.get("h", 0.0),
                        text=elem.get("text", ""),
                        value=elem.get("value", ""),
                        visible=elem.get("visible", False),
                        interactable=elem.get("interactable", False)
                    )
                return None
            lua.globals().find = find_lua
            
            def assert_log_contains_lua(substring, timeout=10.0, step_name="Log Assertion"):
                return self.app_controller.lua_assert_log_contains(substring, timeout, step_name)
            lua.globals().assert_log_contains = assert_log_contains_lua

            def wait_event_lua(event_name, timeout=15.0):
                return self.app_controller.wait_for_salavan_event(event_name, timeout)
            lua.globals().wait_event = wait_event_lua

            def wait_for_lua(element_id, timeout=10.0):
                elem = self.app_controller.lua_wait_for_element(element_id, timeout)
                if elem:
                    return lua.table(
                        path=elem.get("path", ""),
                        type=elem.get("type", ""),
                        x=elem.get("x", 0.0),
                        y=elem.get("y", 0.0),
                        text=elem.get("text", ""),
                        value=elem.get("value", ""),
                        visible=elem.get("visible", False)
                    )
                return None
            lua.globals().wait_for = wait_for_lua

            def assert_visible_lua(element_id, step_name="Assertion"):
                return self.app_controller.lua_assert_visible(element_id, step_name)
            lua.globals().assert_visible = assert_visible_lua
            
            lua.globals().clear_save_data = self.app_controller.clear_save_data
            lua.globals().kill_game = self.app_controller.kill_game
            lua.globals().is_game_running = self.app_controller.is_game_running

            def launch_game_lua(force_restart=False):
                return self.app_controller.launch_game(force_restart=bool(force_restart))
            lua.globals().launch_game = launch_game_lua


            def get_state_lua():
                state = self.app_controller.read_game_state()
                if state:
                    # Convert python dict to lua table
                    import json
                    # A quick way to get a lua table is evaluate a json string
                    json_str = json.dumps(state)
                    return lua.execute(f"return (require('dkjson') and require('dkjson').decode('{json_str}') or nil)") # Requires a json library, or we can just build a basic table.
                return None
            
            # Since dkjson might not be available, let's just return a simpler wrapper or manually convert.
            def get_state_lua_simple():
                state = self.app_controller.read_game_state()
                if state:
                    lua_state = lua.table()
                    lua_state.current_scene = state.get("current_scene", "")
                    lua_state.is_dialogue_active = state.get("is_dialogue_active", False)
                    
                    lua_state.debug_events = lua.table()
                    for idx, ev in enumerate(state.get("debug_events", [])):
                        lua_state.debug_events[idx+1] = ev

                    lua_state.unit_button_names = lua.table()
                    for idx, name in enumerate(state.get("unit_button_names", [])):
                        lua_state.unit_button_names[idx+1] = name

                    lua_state.occupied_tiles = lua.table()
                    for idx, t in enumerate(state.get("occupied_tiles", [])):
                        lua_state.occupied_tiles[idx+1] = lua.table(id=t.get("id"), occupant=t.get("occupant"))
                    
                    lua_state.elements = lua.table()
                    elements = state.get("elements", {})
                    for path, elem in elements.items():
                        lua_state.elements[path] = lua.table(
                            id=elem.get("id", ""),
                            type=elem.get("type", ""),
                            x=elem.get("x", 0.0),
                            y=elem.get("y", 0.0),
                            fx=elem.get("fx", 0.0),
                            fy=elem.get("fy", 0.0),
                            width=elem.get("w", 0.0),
                            height=elem.get("h", 0.0),
                            fw=elem.get("fw", 0.0),
                            fh=elem.get("fh", 0.0),
                            w=elem.get("w", 0.0),
                            h=elem.get("h", 0.0),
                            text=elem.get("text", ""),
                            visible=elem.get("visible", False),
                            interactable=elem.get("interactable", False)
                        )
                    return lua_state
                return None
            lua.globals().get_state = get_state_lua_simple
            
            def get_ui_element_lua(name):
                state = self.app_controller.read_game_state()
                if state:
                    from crypto_utils import find_element_in_state
                    elem = find_element_in_state(name, state)
                    if elem:
                        return lua.table(
                            path=elem.get("path", ""),
                            id=elem.get("id", ""),
                            type=elem.get("type", ""),
                            x=elem.get("x", 0.0),
                            y=elem.get("y", 0.0),
                            fx=elem.get("fx", 0.0),
                            fy=elem.get("fy", 0.0),
                            width=elem.get("w", 0.0),
                            height=elem.get("h", 0.0),
                            fw=elem.get("fw", 0.0),
                            fh=elem.get("fh", 0.0),
                            w=elem.get("w", 0.0),
                            h=elem.get("h", 0.0),
                            text=elem.get("text", ""),
                            visible=elem.get("visible", False),
                            interactable=elem.get("interactable", False)
                        )
                return None
            lua.globals().get_ui_element = get_ui_element_lua
            
            def wait_template_lua(name, timeout=10, threshold=0.8):
                import os, json, time

                # 1. First, try to fetch dynamically from live Game State JSON (100% accurate runtime coords)
                try:
                    elem = get_ui_element_lua(name)
                    if elem:
                        self.app_controller.log_message("HUD", "INFO", f"Found dynamic live coordinate mapping for '{name}'.")
                        time.sleep(1.0) # simulate loading screen delay
                        
                        # Return the exact fullscreen scaled coordinates provided by GameStateExporter.cs
                        return lua.table(x=elem.x, y=elem.y)
                except Exception as e:
                    self.app_controller.log_message("SYSTEM", "WARNING", f"Live state check failed: {str(e)}")

                # 2. Fallback: Try to fetch from static UIConfig JSON
                active_game = self.app_controller.config.get_active_game()
                ui_config_path = active_game.get("ui_mapping_path", "") if active_game else ""
                if ui_config_path and not os.path.isabs(ui_config_path):
                    from core.paths import get_base_dir
                    ui_config_path = os.path.join(get_base_dir(), ui_config_path)
                    
                paths_to_check = [ui_config_path]
                from core.paths import get_base_dir
                paths_to_check.append(os.path.join(get_base_dir(), "assets", "UIConfig_Custom.json"))
                
                for p in paths_to_check:
                    if p and os.path.exists(p):
                        try:
                            with open(p, "r", encoding="utf-8") as f:
                                data = json.load(f)
                                entries = data.get("entries", data) if isinstance(data, dict) else data
                                for entry in entries:
                                    if entry.get("Path", "") == name or entry.get("Path", "").endswith("/" + name):
                                        coords = entry.get("Coordinates", {})
                                        if "x" in coords and "y" in coords:
                                            self.app_controller.log_message("HUD", "INFO", f"Found static coordinate mapping for '{name}'.")
                                            time.sleep(1.0)
                                            gw = self.app_controller.config.game_width or 1280
                                            gh = self.app_controller.config.game_height or 720
                                            x = coords["x"] * (gw / 1920.0)
                                            y = (1080.0 - coords["y"]) * (gh / 1080.0)
                                            return lua.table(x=x, y=y)
                        except Exception:
                            pass

                # 2. Try visual template search
                res = None
                self.app_controller.wait_started.emit(f"Wait Template: {name}", float(timeout))
                start_time = time.time()
                while (time.time() - start_time) < timeout:
                    self.app_controller.check_paused()
                    if self.app_controller.stop_flag:
                        self.app_controller.wait_finished.emit()
                        raise InterruptedError()
                    if getattr(self.app_controller, 'current_step_is_skipped', False) or getattr(self.app_controller, 'skip_current_step', False):
                        self.app_controller.wait_finished.emit()
                        return lua.table(x=0, y=0)
                    
                    res = self.game_hooks.wait_for_template(name, timeout=0.5, threshold=threshold)
                    if res:
                        break
                    self.app_controller.wait_progress.emit(float(timeout - (time.time() - start_time)))
                
                self.app_controller.wait_finished.emit()
                
                if res:
                    return lua.table(x=res[0], y=res[1])
                
                # 3. Target not found visually or in JSON - TRIGGER AUTO MAPPER!
                self.app_controller.log_message("HUD", "WARNING", f"Target '{name}' not found! Prompting manual coordinate mapper...")
                self.app_controller.prompt_missing_template_sig.emit(name)
                
                # Wait for user to map it or cancel (blocking the Lua thread)
                self.app_controller.missing_template_resolved_event.wait()
                self.app_controller.missing_template_resolved_event.clear()
                
                if self.app_controller.missing_template_coords:
                    self.app_controller.log_message("HUD", "PASS", f"Manual mapping accepted for '{name}'. Resuming sequence...")
                    x, y = self.app_controller.missing_template_coords
                    return lua.table(x=x, y=y)
                
                self.app_controller.log_message("HUD", "FAIL", f"Manual mapping cancelled or failed for '{name}'.")
                return None
            lua.globals().wait_template = wait_template_lua
            
            lua.execute(LUA_ASSERTIONS_HEADER)
            
            self.log_emitted.emit("HUD", "INFO", f"Reading scenario: {self.scenario_name}...")
            
            with open(self.script_path, "r", encoding="utf-8") as f:
                lua_code = f.read()
                
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
                
            self.log_emitted.emit("SYSTEM", "INFO", "Sequence execution successfully concluded.")
            
        except InterruptedError:
            self.log_emitted.emit("SYSTEM", "INFO", "Sequence aborted.")
            test_status = "Aborted"
        except Exception as e:
            self.log_emitted.emit("SYSTEM", "FAIL", f"Sequence crashed: {str(e)}")
            test_status = "Failed"
            
        finally:
            if teardown_fn is not None:
                try:
                    self.log_emitted.emit("SYSTEM", "INFO", "Executing teardown hook...")
                    teardown_fn()
                except Exception as te:
                    self.log_emitted.emit("SYSTEM", "FAIL", f"Teardown hook crashed: {str(te)}")
                    
            self.test_finished.emit(test_status)
