import os
import time
import pyautogui
pyautogui.FAILSAFE = False
import cv2
import numpy as np
from PIL import ImageGrab
import ctypes
try:
    ctypes.windll.user32.SetProcessDPIAware()
except Exception:
    pass

class GameHooks:
    """Encapsulates simulated input and visual scanning (template matching)."""
    
    def __init__(self, templates_dir):
        self.templates_dir = templates_dir
        self.game_rect = None # (x, y, w, h)
        self._cached_templates = {}

    def set_game_rect(self, rect):
        self.game_rect = rect

    def _get_absolute_coords(self, rel_x, rel_y):
        if not self.game_rect:
            return None
            
        if len(self.game_rect) == 5:
            gx, gy, gw, gh, hwnd = self.game_rect
        else:
            gx, gy, gw, gh = self.game_rect
            hwnd = 0

        try:
            import win32gui
            client_w = gw - 16
            client_h = gh - 39
            offset_x = 8
            offset_y = 32
        except Exception:
            client_w = gw
            client_h = gh
            offset_x = 0
            offset_y = 0
        
        abs_x = gx + offset_x + (client_w * (rel_x / 1280.0))
        abs_y = gy + offset_y + (client_h * (rel_y / 720.0))
        return abs_x, abs_y

    def click_relative(self, rel_x, rel_y):
        coords = self._get_absolute_coords(rel_x, rel_y)
        if coords:
            print(f"[GameHooks] Translating ({rel_x:.1f}, {rel_y:.1f}) -> Abs: ({coords[0]:.1f}, {coords[1]:.1f}) using Rect: {self.game_rect}", flush=True)
            try:
                import win32gui, win32con
                hwnd = win32gui.FindWindow(None, "Maou-Sama-TD")
                if hwnd:
                    win32gui.ShowWindow(hwnd, win32con.SW_RESTORE)
                    win32gui.SetForegroundWindow(hwnd)
            except Exception:
                pass
            pyautogui.moveTo(coords[0], coords[1], 0.1)
            pyautogui.click()
            time.sleep(0.05)
            pyautogui.click()
            return True
        return False

    def drag_relative(self, start_x, start_y, end_x, end_y, duration=0.5):
        start = self._get_absolute_coords(start_x, start_y)
        end = self._get_absolute_coords(end_x, end_y)
        if start and end:
            pyautogui.moveTo(start[0], start[1], 0.35)
            pyautogui.dragTo(end[0], end[1], duration, button='left')
            return True
        return False

    def get_template(self, template_name):
        if template_name in self._cached_templates:
            return self._cached_templates[template_name]
        
        path = os.path.join(self.templates_dir, f"{template_name}.png")
        if not os.path.exists(path):
            return None
            
        template = cv2.imread(path, cv2.IMREAD_COLOR)
        if template is not None:
            self._cached_templates[template_name] = template
        return template

    def wait_for_template(self, template_name, timeout=10.0, threshold=0.8):
        """Synchronous blocking wait for a visual target. Used by the Lua thread."""
        template = self.get_template(template_name)
        if template is None:
            return None
            
        start_time = time.time()
        while (time.time() - start_time) < timeout:
            if self.game_rect:
                gx, gy, gw, gh = self.game_rect
                try:
                    # Grab screenshot of game window
                    screenshot = ImageGrab.grab(bbox=(gx, gy, gx + gw, gy + gh))
                    frame = cv2.cvtColor(np.array(screenshot), cv2.COLOR_RGB2BGR)
                    
                    res = cv2.matchTemplate(frame, template, cv2.TM_CCOEFF_NORMED)
                    min_val, max_val, min_loc, max_loc = cv2.minMaxLoc(res)
                    
                    if max_val >= threshold:
                        tw, th = template.shape[1], template.shape[0]
                        center_x = max_loc[0] + (tw / 2)
                        center_y = max_loc[1] + (th / 2)
                        
                        rel_x = (center_x / gw) * 1280.0
                        rel_y = (center_y / gh) * 720.0
                        return (rel_x, rel_y)
                except Exception:
                    pass
            time.sleep(0.5)
            
        return None
