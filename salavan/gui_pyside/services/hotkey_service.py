from pynput import keyboard
from PySide6.QtCore import QObject, Signal
import threading

class HotkeyService(QObject):
    pause_signal = Signal()
    resume_signal = Signal()
    kill_signal = Signal()

    def __init__(self):
        super().__init__()
        self.mouse_listener = None
        
    def start(self):
        def on_pause():
            print("[Hotkey] F8 Pressed: Pausing automation. Releasing mouse.")
            self._set_mouse_lock(False)
            self.pause_signal.emit()
            
        def on_resume():
            print("[Hotkey] F9 Pressed: Resuming automation. Locking mouse.")
            self._set_mouse_lock(True)
            self.resume_signal.emit()
            
        def on_kill():
            print("[Hotkey] CTRL+SHIFT+F12 Pressed: Emergency Stop. Releasing mouse.")
            self._set_mouse_lock(False)
            self.kill_signal.emit()
            
        # Define hotkeys
        # <f8> and <f9> for pause/resume. 
        # <ctrl>+<shift>+<f12> for kill.
        hotkeys = {
            '<f8>': on_pause,
            '<f9>': on_resume,
            '<ctrl>+<shift>+<f12>': on_kill
        }
        
        self.listener = keyboard.GlobalHotKeys(hotkeys)
        self.listener.start()
        self._set_mouse_lock(True)
        
    def _set_mouse_lock(self, enable):
        try:
            from pynput import mouse
            if enable and self.mouse_listener is None:
                self.mouse_listener = mouse.Listener(suppress=True)
                self.mouse_listener.start()
            elif not enable and self.mouse_listener is not None:
                self.mouse_listener.stop()
                self.mouse_listener = None
        except Exception as e:
            print(f"[Hotkey] Failed to toggle mouse lock: {e}")
        
    def stop(self):
        self._set_mouse_lock(False)
        if self.listener:
            self.listener.stop()
            self.listener = None
