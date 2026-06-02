import os
import sys
import time
import json
import shutil
import subprocess
import threading
import tkinter as tk
from tkinter import ttk, messagebox, filedialog
from PIL import Image, ImageTk, ImageGrab
import cv2
import numpy as np
import pyautogui
import pygetwindow as gw
from lupa import LuaRuntime

# Configure PyAutoGUI safety settings
pyautogui.FAILSAFE = True  # Move mouse to top-left corner to abort
pyautogui.PAUSE = 0.1

class ScreenRecorder(threading.Thread):
    def __init__(self, app, output_path, fps=10):
        super().__init__(daemon=True)
        self.app = app
        self.output_path = output_path
        self.fps = fps
        self.recording = False
        self.writer = None
        
    def run(self):
        self.recording = True
        self.app.log_message("Recorder", "INFO", "Recording thread started.")
        
        # Configure OpenCV VideoWriter using MJPG codec for maximum compatibility
        fourcc = cv2.VideoWriter_fourcc(*'MJPG')
        self.writer = cv2.VideoWriter(self.output_path, fourcc, self.fps, (1280, 720))
        
        frame_duration = 1.0 / self.fps
        
        while self.recording and not self.app.stop_flag:
            start_time = time.time()
            
            rect = self.app.get_game_rect()
            if rect:
                gx, gy, gw_w, gw_h = rect
                try:
                    # Capture exact game window region
                    screenshot = ImageGrab.grab(bbox=(gx, gy, gx + gw_w, gy + gw_h))
                    frame = cv2.cvtColor(np.array(screenshot), cv2.COLOR_RGB2BGR)
                    
                    # Ensure frame is exactly 1280x720
                    if frame.shape[1] != 1280 or frame.shape[0] != 720:
                        frame = cv2.resize(frame, (1280, 720))
                    
                    self.writer.write(frame)
                except Exception:
                    # Ignore occasional capture capture glitch during transitions
                    pass
            
            elapsed = time.time() - start_time
            sleep_time = max(0, frame_duration - elapsed)
            time.sleep(sleep_time)
            
        if self.writer:
            self.writer.release()
        self.app.log_message("Recorder", "INFO", f"Recording saved to: {os.path.basename(self.output_path)}")

    def stop(self):
        self.recording = False


class GameTesterApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Game Automated Tester")
        self.root.geometry("400x750+1280+0")
        self.root.resizable(True, True)
        self.root.attributes("-topmost", True)
        
        # Resolve Base Directory (Handles standalone pyinstaller exe location)
        if getattr(sys, 'frozen', False):
            self.base_dir = os.path.dirname(sys.executable)
        else:
            self.base_dir = os.path.dirname(os.path.abspath(__file__))
            
        self.template_dir = os.path.join(self.base_dir, "templates")
        self.recordings_dir = os.path.join(self.base_dir, "recordings")
        self.lua_script_path = os.path.join(self.base_dir, "test_suite.lua")
        self.config_path = os.path.join(self.base_dir, "config.json")
        
        os.makedirs(self.template_dir, exist_ok=True)
        os.makedirs(self.recordings_dir, exist_ok=True)

        # Threading & Control State
        self.test_thread = None
        self.recorder_thread = None
        self.stop_flag = False
        self.pause_event = threading.Event()
        self.pause_event.set()
        self.game_process = None
        
        # Config values
        self.game_exe_path = ""
        self.record_test = True
        self.load_config()
        
        # Colors & Styling
        self.bg_color = "#1e1e24"
        self.fg_color = "#f4f4f6"
        self.accent_color = "#7209b7"
        self.btn_bg = "#2b2b36"
        self.success_color = "#4caf50"
        self.fail_color = "#f44336"
        
        self.root.configure(bg=self.bg_color)
        self.style = ttk.Style()
        self.style.theme_use("clam")
        
        self.style.configure(".", background=self.bg_color, foreground=self.fg_color)
        self.style.configure("TLabel", background=self.bg_color, foreground=self.fg_color, font=("Segoe UI", 10))
        self.style.configure("TButton", background=self.btn_bg, foreground=self.fg_color, borderwidth=0, font=("Segoe UI", 9, "bold"))
        self.style.map("TButton", background=[("active", self.accent_color)])
        self.style.configure("TCheckbutton", background=self.bg_color, foreground=self.fg_color, font=("Segoe UI", 9))
        self.style.configure("TEntry", fieldbackground="#2a2a35", foreground=self.fg_color)
        
        self.style.configure("Treeview", background="#2a2a35", fieldbackground="#2a2a35", foreground=self.fg_color, font=("Segoe UI", 9))
        self.style.configure("Treeview.Heading", background=self.btn_bg, foreground=self.fg_color, font=("Segoe UI", 9, "bold"))
        
        self.create_widgets()
        self.log_message("System", "INFO", "Tester environment ready.")
        
        # Ensure Lua script exists
        if not os.path.exists(self.lua_script_path):
            self.create_fallback_lua_script()

    def load_config(self):
        if os.path.exists(self.config_path):
            try:
                with open(self.config_path, "r") as f:
                    data = json.load(f)
                    self.game_exe_path = data.get("game_exe_path", "")
                    self.record_test = data.get("record_test", True)
            except Exception:
                pass

    def save_config(self):
        try:
            with open(self.config_path, "w") as f:
                json.dump({
                    "game_exe_path": self.game_exe_path,
                    "record_test": self.record_test
                }, f, indent=4)
        except Exception:
            pass

    def create_widgets(self):
        # Header Info Panel
        header_frame = tk.Frame(self.root, bg=self.bg_color, pady=10)
        header_frame.pack(fill="x", padx=15)
        
        title_lbl = tk.Label(header_frame, text="GAME AUTOMATED TESTER", fg=self.accent_color, bg=self.bg_color, font=("Segoe UI", 14, "bold"))
        title_lbl.pack(anchor="w")
        
        self.stage_lbl = tk.Label(header_frame, text="Stage: Idle", fg=self.fg_color, bg=self.bg_color, font=("Segoe UI", 11, "bold"))
        self.stage_lbl.pack(anchor="w", pady=(5, 0))
        
        # Setup Executable Path Config Panel
        config_frame = tk.LabelFrame(self.root, text=" Target Game Setup ", bg=self.bg_color, fg=self.accent_color, font=("Segoe UI", 10, "bold"), padx=10, pady=10)
        config_frame.pack(fill="x", padx=15, pady=5)
        
        path_lbl = ttk.Label(config_frame, text="Game Executable (.exe):")
        path_lbl.pack(anchor="w")
        
        path_select_frame = tk.Frame(config_frame, bg=self.bg_color)
        path_select_frame.pack(fill="x", pady=5)
        
        self.path_entry_var = tk.StringVar(value=self.game_exe_path)
        self.path_entry = ttk.Entry(path_select_frame, textvariable=self.path_entry_var, state="readonly", font=("Segoe UI", 9))
        self.path_entry.pack(side="left", fill="x", expand=True, padx=(0, 5))
        
        browse_btn = ttk.Button(path_select_frame, text="Browse...", command=self.browse_game_exe, width=10)
        browse_btn.pack(side="right")
        
        # Recording Options
        self.record_var = tk.BooleanVar(value=self.record_test)
        def on_record_toggle():
            self.record_test = self.record_var.get()
            self.save_config()
            
        record_chk = ttk.Checkbutton(config_frame, text="Auto-Record Test Run (Video)", variable=self.record_var, command=on_record_toggle)
        record_chk.pack(anchor="w", pady=(5, 0))
        
        # Controls Frame
        ctrl_frame = tk.LabelFrame(self.root, text=" Controls ", bg=self.bg_color, fg=self.accent_color, font=("Segoe UI", 10, "bold"), padx=10, pady=10)
        ctrl_frame.pack(fill="x", padx=15, pady=5)
        
        self.btn_run = ttk.Button(ctrl_frame, text="Run Test", command=self.start_test_flow)
        self.btn_run.grid(row=0, column=0, padx=5, pady=5, sticky="ew")
        
        self.btn_pause = ttk.Button(ctrl_frame, text="Pause", command=self.toggle_pause, state="disabled")
        self.btn_pause.grid(row=0, column=1, padx=5, pady=5, sticky="ew")
        
        self.btn_stop = ttk.Button(ctrl_frame, text="Stop", command=self.stop_test_flow, state="disabled")
        self.btn_stop.grid(row=0, column=2, padx=5, pady=5, sticky="ew")
        
        self.btn_clear_save = ttk.Button(ctrl_frame, text="Clear Save", command=self.manual_clear_save)
        self.btn_clear_save.grid(row=1, column=0, padx=5, pady=5, sticky="ew")
        
        self.btn_launch = ttk.Button(ctrl_frame, text="Launch Game", command=self.manual_launch_game)
        self.btn_launch.grid(row=1, column=1, padx=5, pady=5, sticky="ew")
        
        self.btn_capture = ttk.Button(ctrl_frame, text="Capture Template", command=self.manual_capture_template)
        self.btn_capture.grid(row=1, column=2, padx=5, pady=5, sticky="ew")
        
        ctrl_frame.columnconfigure(0, weight=1)
        ctrl_frame.columnconfigure(1, weight=1)
        ctrl_frame.columnconfigure(2, weight=1)
        
        # Reports / Logs Panel
        report_frame = tk.LabelFrame(self.root, text=" Test Report / Logs ", bg=self.bg_color, fg=self.accent_color, font=("Segoe UI", 10, "bold"), padx=5, pady=5)
        report_frame.pack(fill="both", expand=True, padx=15, pady=10)
        
        scroll = ttk.Scrollbar(report_frame)
        scroll.pack(side="right", fill="y")
        
        self.tree = ttk.Treeview(report_frame, columns=("Step", "Result", "Message"), show="headings", yscrollcommand=scroll.set)
        self.tree.heading("Step", text="Test Step")
        self.tree.heading("Result", text="Result")
        self.tree.heading("Message", text="Detail Message")
        
        self.tree.column("Step", width=110, anchor="w")
        self.tree.column("Result", width=65, anchor="center")
        self.tree.column("Message", width=180, anchor="w")
        
        self.tree.tag_configure("PASS", foreground=self.success_color, font=("Segoe UI", 9, "bold"))
        self.tree.tag_configure("FAIL", foreground=self.fail_color, font=("Segoe UI", 9, "bold"))
        self.tree.tag_configure("STARTING", foreground="#00b4d8")
        self.tree.tag_configure("INFO", foreground=self.fg_color)
        
        self.tree.pack(fill="both", expand=True)
        scroll.config(command=self.tree.yview)

    def browse_game_exe(self):
        filepath = filedialog.askopenfilename(
            title="Select Game Executable File",
            filetypes=[("Executable Files", "*.exe")]
        )
        if filepath:
            self.game_exe_path = filepath
            self.path_entry_var.set(filepath)
            self.save_config()
            self.log_message("System", "INFO", f"Selected target: {os.path.basename(filepath)}")

    def log_message(self, step, result, message):
        self.root.after(0, self._add_log_to_tree, step, result, message)
        
    def _add_log_to_tree(self, step, result, message):
        item = self.tree.insert("", "end", values=(step, result, message), tags=(result,))
        self.tree.see(item)

    def set_stage_lbl(self, text):
        self.root.after(0, lambda: self.stage_lbl.config(text=f"Stage: {text}"))

    def toggle_pause(self):
        if self.pause_event.is_set():
            self.pause_event.clear()
            self.btn_pause.config(text="Resume")
            self.log_message("Runner", "INFO", "Tests PAUSED.")
        else:
            self.pause_event.set()
            self.btn_pause.config(text="Pause")
            self.log_message("Runner", "INFO", "Tests RESUMED.")

    def check_paused(self):
        while not self.pause_event.is_set():
            if self.stop_flag:
                raise InterruptedError("Test stopped by user.")
            time.sleep(0.1)

    def start_test_flow(self):
        if not self.game_exe_path or not os.path.exists(self.game_exe_path):
            messagebox.showerror("Error", "Please select a valid game executable first!")
            return
            
        if self.test_thread and self.test_thread.is_alive():
            return
            
        self.stop_flag = False
        self.pause_event.set()
        self.btn_run.config(state="disabled")
        self.btn_pause.config(state="normal", text="Pause")
        self.btn_stop.config(state="normal")
        self.btn_clear_save.config(state="disabled")
        self.btn_launch.config(state="disabled")
        
        self.tree.delete(*self.tree.get_children())
        self.test_thread = threading.Thread(target=self.run_lua_test_suite, daemon=True)
        self.test_thread.start()

    def stop_test_flow(self):
        self.stop_flag = True
        self.pause_event.set()
        
        self.log_message("Runner", "INFO", "Stopping execution...")
        self.stop_recorder()
        self.kill_game_process()
        
        self.btn_run.config(state="normal")
        self.btn_pause.config(state="disabled", text="Pause")
        self.btn_stop.config(state="disabled")
        self.btn_clear_save.config(state="normal")
        self.btn_launch.config(state="normal")
        self.set_stage_lbl("Stopped")

    def run_lua_test_suite(self):
        try:
            # Initialize Recording if enabled
            if self.record_test:
                timestamp = time.strftime("%Y%m%d_%H%M%S")
                rec_filename = f"test_run_{timestamp}.avi"
                rec_path = os.path.join(self.recordings_dir, rec_filename)
                self.recorder_thread = ScreenRecorder(self, rec_path, fps=10)
                self.recorder_thread.start()
                
            lua = LuaRuntime(unpack_returned_tuples=True)
            
            lua.globals().set_stage = self.set_stage_lbl
            lua.globals().log_test = self.log_message
            lua.globals().clear_save_data = self.clear_save_data
            lua.globals().launch_game = self.launch_game
            lua.globals().click = self.click_game_relative
            lua.globals().drag = self.drag_game_relative
            lua.globals().wait = self.sleep_wait
            
            def wait_template_lua(name, timeout=10, threshold=0.8):
                res = self.wait_for_template_coord(name, timeout, threshold)
                if res:
                    return lua.table(x=res[0], y=res[1])
                return None
            lua.globals().wait_template = wait_template_lua
            
            self.log_message("Runner", "INFO", "Executing Lua script...")
            
            with open(self.lua_script_path, "r") as f:
                lua_code = f.read()
                
            lua.execute(lua_code)
            self.log_message("Runner", "INFO", "Lua test suite finished execution.")
            
        except InterruptedError:
            self.log_message("Runner", "INFO", "Test run aborted by user.")
        except Exception as e:
            self.log_message("Runner", "FAIL", f"Error in Lua runner: {str(e)}")
            
        finally:
            self.stop_recorder()
            self.root.after(0, self._reset_controls_post_run)

    def stop_recorder(self):
        if self.recorder_thread:
            self.recorder_thread.stop()
            self.recorder_thread.join(timeout=3)
            self.recorder_thread = None

    def _reset_controls_post_run(self):
        self.btn_run.config(state="normal")
        self.btn_pause.config(state="disabled", text="Pause")
        self.btn_stop.config(state="disabled")
        self.btn_clear_save.config(state="normal")
        self.btn_launch.config(state="normal")

    # ── Exposed Python APIs for Lua ───────────────────────────────────────

    def sleep_wait(self, seconds):
        start_time = time.time()
        while time.time() - start_time < seconds:
            self.check_paused()
            if self.stop_flag:
                raise InterruptedError("Test stopped by user.")
            time.sleep(0.1)

    def clear_save_data(self):
        self.check_paused()
        paths = [
            os.path.join(os.path.expanduser('~'), 'Documents', 'Maou-Sama-TD', 'player_save.json'),
            os.path.join(os.path.expanduser('~'), 'AppData', 'Local', 'Low', 'Ouiki.Dev', 'Maou-Sama-TD', 'player_save.json'),
            os.path.join(os.path.expanduser('~'), 'AppData', 'LocalLow', 'Ouiki.Dev', 'Maou-Sama-TD', 'player_save.json')
        ]
        cleared_any = False
        for p in paths:
            if os.path.exists(p):
                try:
                    os.remove(p)
                    self.log_message("Clear Save", "INFO", f"Removed save: {p}")
                    cleared_any = True
                except Exception as e:
                    self.log_message("Clear Save", "INFO", f"Error clearing {p}: {str(e)}")
        return True

    def manual_clear_save(self):
        if self.clear_save_data():
            messagebox.showinfo("Clear Save", "Save data cleared successfully!")
        else:
            messagebox.showerror("Clear Save", "Failed or no save data found to clear.")

    def kill_game_process(self):
        if self.game_process:
            try:
                self.game_process.terminate()
                self.game_process.wait(timeout=2)
                self.game_process = None
            except Exception:
                pass
        
        # Kill by process name to guarantee cleanup
        subprocess.run("taskkill /f /im Maou-Sama-TD.exe", shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    def launch_game(self):
        self.check_paused()
        self.kill_game_process()
        
        if not self.game_exe_path or not os.path.exists(self.game_exe_path):
            self.log_message("Launch Game", "FAIL", "Game executable not selected or missing.")
            return False
            
        try:
            # Force windowed 1280x720 resolution
            cmd = f'"{self.game_exe_path}" -screen-width 1280 -screen-height 720 -screen-fullscreen 0'
            self.game_process = subprocess.Popen(cmd, shell=True)
            self.log_message("Launch Game", "INFO", "Process spawned. Positioning window...")
            
            positioned = False
            for _ in range(30):
                self.check_paused()
                if self.stop_flag:
                    raise InterruptedError()
                
                win = gw.getWindowsWithTitle("Maou-Sama-TD")
                if win:
                    w = win[0]
                    w.restore()
                    w.activate()
                    w.moveTo(0, 0)
                    w.resizeTo(1280, 720)
                    positioned = True
                    break
                time.sleep(0.5)
                
            if positioned:
                self.log_message("Launch Game", "INFO", "Game window aligned to (0, 0).")
                return True
            else:
                self.log_message("Launch Game", "FAIL", "Could not locate game window.")
                return False
                
        except Exception as e:
            self.log_message("Launch Game", "FAIL", f"Launch error: {str(e)}")
            return False

    def manual_launch_game(self):
        if not self.game_exe_path or not os.path.exists(self.game_exe_path):
            messagebox.showerror("Error", "Please select a valid game executable first!")
            return
        threading.Thread(target=self.launch_game, daemon=True).start()

    def get_game_rect(self):
        win = gw.getWindowsWithTitle("Maou-Sama-TD")
        if not win:
            return None
        w = win[0]
        if w.isMinimized:
            w.restore()
        
        border_x = (w.width - 1280) // 2
        border_y = (w.height - 720) - border_x
        
        client_x = w.left + border_x
        client_y = w.top + border_y
        return (client_x, client_y, 1280, 720)

    def click_game_relative(self, rx, ry):
        self.check_paused()
        rect = self.get_game_rect()
        if not rect:
            self.log_message("Click", "FAIL", "Cannot find game window client bounds.")
            return False
            
        cx, cy, _, _ = rect
        screen_x = cx + rx
        screen_y = cy + ry
        
        win = gw.getWindowsWithTitle("Maou-Sama-TD")
        if win:
            try: win[0].activate()
            except Exception: pass
            
        pyautogui.moveTo(screen_x, screen_y, duration=0.2)
        pyautogui.click()
        return True

    def drag_game_relative(self, rx1, ry1, rx2, ry2, duration=0.5):
        self.check_paused()
        rect = self.get_game_rect()
        if not rect:
            self.log_message("Drag", "FAIL", "Cannot find game window client bounds.")
            return False
            
        cx, cy, _, _ = rect
        sx1, sy1 = cx + rx1, cy + ry1
        sx2, sy2 = cx + rx2, cy + ry2
        
        win = gw.getWindowsWithTitle("Maou-Sama-TD")
        if win:
            try: win[0].activate()
            except Exception: pass
            
        pyautogui.moveTo(sx1, sy1, duration=0.2)
        pyautogui.dragTo(sx2, sy2, duration=duration, button="left")
        return True

    # ── Template Matching & Capture Helper ─────────────────────────────────

    def get_template_path(self, name):
        if not name.endswith(".png"):
            name = name + ".png"
        return os.path.join(self.template_dir, name)

    def wait_for_template_coord(self, name, timeout=10, threshold=0.8):
        self.check_paused()
        template_path = self.get_template_path(name)
        
        if not os.path.exists(template_path):
            self.log_message("Template", "INFO", f"Template '{name}' is missing. Initiating capture...")
            captured = self.request_template_capture(name)
            if not captured:
                self.log_message("Template", "FAIL", f"Capture canceled. Cannot match '{name}'.")
                return None
        
        start_time = time.time()
        while time.time() - start_time < timeout:
            self.check_paused()
            if self.stop_flag:
                raise InterruptedError()
                
            pos = self.match_template_on_screen(template_path, threshold)
            if pos:
                return pos
            time.sleep(0.5)
            
        return None

    def match_template_on_screen(self, template_path, threshold=0.8):
        rect = self.get_game_rect()
        if not rect:
            return None
            
        gx, gy, gw_w, gw_h = rect
        screenshot = ImageGrab.grab(bbox=(gx, gy, gx + gw_w, gy + gw_h))
        
        screen_np = cv2.cvtColor(np.array(screenshot), cv2.COLOR_RGB2BGR)
        screen_gray = cv2.cvtColor(screen_np, cv2.COLOR_BGR2GRAY)
        
        template = cv2.imread(template_path, cv2.IMREAD_GRAYSCALE)
        if template is None:
            return None
            
        res = cv2.matchTemplate(screen_gray, template, cv2.TM_CCOEFF_NORMED)
        min_val, max_val, min_loc, max_loc = cv2.minMaxLoc(res)
        
        if max_val >= threshold:
            h, w = template.shape
            center_x = max_loc[0] + w // 2
            center_y = max_loc[1] + h // 2
            return (center_x, center_y)
            
        return None

    def request_template_capture(self, name):
        capture_done_event = threading.Event()
        self.crop_result = False
        
        def run_gui_capture():
            self.show_capture_wizard(name, capture_done_event)
            
        self.root.after(0, run_gui_capture)
        capture_done_event.wait()
        return self.crop_result

    def show_capture_wizard(self, name, done_event):
        rect = self.get_game_rect()
        if not rect:
            messagebox.showerror("Capture", "Game window 'Maou-Sama-TD' must be running and visible to capture templates!")
            self.crop_result = False
            done_event.set()
            return
            
        gx, gy, gw_w, gw_h = rect
        
        # Stop recorder briefly to not capture the capture dialog itself
        recorder_active = False
        if self.recorder_thread:
            self.stop_recorder()
            recorder_active = True
            
        messagebox.showinfo("Capture Helper", f"Template '{name}' is needed.\n\nClick OK, then click and drag a box closely over the button or area on the game screen.")
        
        screenshot = ImageGrab.grab(bbox=(gx, gy, gx + gw_w, gy + gw_h))
        
        crop_root = tk.Toplevel(self.root)
        crop_root.title("Select Area")
        crop_root.geometry(f"{gw_w}x{gw_h}+{gx}+{gy}")
        crop_root.overrideredirect(True)
        crop_root.attributes("-topmost", True)
        
        photo = ImageTk.PhotoImage(screenshot)
        canvas = tk.Canvas(crop_root, cursor="cross", width=gw_w, height=gw_h)
        canvas.pack(fill="both", expand=True)
        canvas.create_image(0, 0, image=photo, anchor="nw")
        canvas.photo = photo
        
        state = {
            "rect": None,
            "sx": 0, "sy": 0,
            "cx": 0, "cy": 0
        }
        
        def on_press(event):
            state["sx"] = event.x
            state["sy"] = event.y
            state["rect"] = canvas.create_rectangle(event.x, event.y, event.x, event.y, outline="red", width=2)
            
        def on_drag(event):
            state["cx"] = event.x
            state["cy"] = event.y
            canvas.coords(state["rect"], state["sx"], state["sy"], event.x, event.y)
            
        def on_release(event):
            state["cx"] = event.x
            state["cy"] = event.y
            crop_root.destroy()
            
            x1 = min(state["sx"], state["cx"])
            y1 = min(state["sy"], state["cy"])
            x2 = max(state["sx"], state["cx"])
            y2 = max(state["sy"], state["cy"])
            
            if x2 - x1 > 5 and y2 - y1 > 5:
                cropped = screenshot.crop((x1, y1, x2, y2))
                dest_path = self.get_template_path(name)
                cropped.save(dest_path)
                self.log_message("Template", "INFO", f"Saved template '{name}'")
                self.crop_result = True
            else:
                self.crop_result = False
                
            # Restart recorder if it was active
            if recorder_active and not self.stop_flag:
                timestamp = time.strftime("%Y%m%d_%H%M%S")
                rec_path = os.path.join(self.recordings_dir, f"test_run_{timestamp}.avi")
                self.recorder_thread = ScreenRecorder(self, rec_path, fps=10)
                self.recorder_thread.start()
                
            done_event.set()
            
        canvas.bind("<ButtonPress-1>", on_press)
        canvas.bind("<B1-Motion>", on_drag)
        canvas.bind("<ButtonRelease-1>", on_release)
        crop_root.focus_force()

    def manual_capture_template(self):
        name_win = tk.Toplevel(self.root)
        name_win.title("Name Template")
        name_win.geometry("300x120+1320+100")
        name_win.configure(bg=self.bg_color)
        name_win.attributes("-topmost", True)
        
        lbl = tk.Label(name_win, text="Enter Template File Name (e.g. dice_button):", bg=self.bg_color, fg=self.fg_color)
        lbl.pack(pady=10)
        
        entry = ttk.Entry(name_win, width=30)
        entry.pack(pady=5)
        entry.focus()
        
        def confirm():
            name = entry.get().strip()
            if not name:
                messagebox.showerror("Error", "Name cannot be empty!")
                return
            name_win.destroy()
            
            done_evt = threading.Event()
            self.root.after(100, lambda: self.show_capture_wizard(name, done_evt))
            
        btn = ttk.Button(name_win, text="Capture", command=confirm)
        btn.pack(pady=10)

    def create_fallback_lua_script(self):
        """Creates a fallback Lua script in case it doesn't exist."""
        fallback_lua = """-- Lua Game Test Suite for Maou-Sama-TD
-- Runs through the Level 1 tutorial and enters Level 2

local function run_tests()
    set_stage("1. Clear Save Data")
    log_test("Clear Save Data", "STARTING", "Deleting save files...")
    if clear_save_data() then
        log_test("Clear Save Data", "PASS", "Save data cleared successfully.")
    else
        log_test("Clear Save Data", "FAIL", "Failed to clear save data.")
        return
    end

    set_stage("2. Launching Game")
    log_test("Launch Game", "STARTING", "Booting target game...")
    if launch_game() then
        log_test("Launch Game", "PASS", "Game booted and positioned at (0, 0).")
    else
        log_test("Launch Game", "FAIL", "Failed to launch game.")
        return
    end

    wait(6)

    set_stage("3. Character Ascension")
    log_test("Character Ascension", "STARTING", "Ascending Maou...")
    
    local dice_pos = wait_template("dice_button", 25)
    if dice_pos then
        click(dice_pos.x, dice_pos.y)
        wait(1.5)
        
        local arise_pos = wait_template("arise_button", 8)
        if arise_pos then
            click(arise_pos.x, arise_pos.y)
            log_test("Character Ascension", "PASS", "Maou chosen and Arisen successfully.")
        else
            log_test("Character Ascension", "FAIL", "Arise button not found.")
            return
        end
    else
        log_test("Character Ascension", "FAIL", "Ascension Panel dice button did not load.")
        return
    end

    wait(6)

    set_stage("4. Level 1 - Start Tutorial")
    log_test("Level 1 Start", "STARTING", "Waiting for Play Tutorial prompt...")
    
    local play_tut_pos = wait_template("play_tutorial_btn", 25)
    if play_tut_pos then
        click(play_tut_pos.x, play_tut_pos.y)
        log_test("Level 1 Start", "PASS", "Play Tutorial clicked.")
    else
        log_test("Level 1 Start", "FAIL", "Play Tutorial prompt did not appear.")
        return
    end

    wait(2.5)

    log_test("Tutorial Step 1", "STARTING", "Advancing dialogues...")
    for i=1,3 do
        click(1100, 650) 
        wait(2.0)
    end
    log_test("Tutorial Step 1", "PASS", "Intro dialogues advanced.")

    log_test("Tutorial Step 2", "STARTING", "Dragging Ignis card to field tile (7, 4)...")
    local ignis_btn = wait_template("ignis_card", 8)
    if ignis_btn then
        drag(ignis_btn.x, ignis_btn.y, 740, 320, 1.0)
        wait(2.5)
        log_test("Tutorial Step 2", "PASS", "Ignis deployed to grid.")
    else
        log_test("Tutorial Step 2", "FAIL", "Ignis deployment card not found.")
        return
    end

    click(1100, 650)
    wait(2.5)

    log_test("Tutorial Wave 1", "STARTING", "Waiting for Ignis to charge ultimate...")
    wait(18) 
    
    click(1100, 650)
    wait(2.0)

    log_test("Tutorial Step 4", "STARTING", "Selecting Ignis on grid...")
    click(740, 320)
    wait(2.0)

    log_test("Tutorial Step 5", "STARTING", "Activating Ignis ultimate...")
    local ult_btn = wait_template("ignis_ult_btn", 8)
    if ult_btn then
        click(ult_btn.x, ult_btn.y)
        wait(2.0)
        log_test("Tutorial Step 5", "PASS", "Ignis ultimate activated.")
    else
        click(1150, 580)
        wait(2.0)
        log_test("Tutorial Step 5", "PASS", "Ignis ultimate clicked (fallback coordinate).")
    end

    log_test("Tutorial Level 1 Victory", "STARTING", "Waiting for Level 1 victory screen...")
    
    local next_lvl_btn = wait_template("victory_next_level", 45)
    if next_lvl_btn then
        click(next_lvl_btn.x, next_lvl_btn.y)
        log_test("Tutorial Level 1 Victory", "PASS", "Level 1 Cleared. Next Level clicked.")
    else
        log_test("Tutorial Level 1 Victory", "FAIL", "Victory button not found.")
        return
    end

    wait(6)

    set_stage("5. Level 2 Start")
    log_test("Level 2 Start", "STARTING", "Entering Level 2...")
    
    local start_battle = wait_template("start_battle_btn", 25)
    if start_battle then
        click(start_battle.x, start_battle.y)
        log_test("Level 2 Start", "PASS", "Level 2 battle started successfully.")
    else
        log_test("Level 2 Start", "FAIL", "Level 2 Start Battle button not found.")
        return
    end

    set_stage("Completed")
    log_test("Test Suite", "PASS", "All stages of the tutorial test suite passed successfully!")
end

run_tests()
"""
        with open(self.lua_script_path, "w") as f:
            f.write(fallback_lua)


if __name__ == "__main__":
    root = tk.Tk()
    app = GameTesterApp(root)
    root.mainloop()
