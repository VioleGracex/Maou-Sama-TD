from pynput import keyboard
from PySide6.QtCore import QObject, Signal
import threading

class HotkeyService(QObject):
    pause_signal = Signal()
    resume_signal = Signal()
    kill_signal = Signal()
    toggle_logs_signal = Signal()

    def __init__(self):
        super().__init__()
        self.mouse_listener = None
        self.listener = None
        
    def start(self):
        def on_pause():
            print("[Hotkey] F8 Pressed: Pausing automation. Releasing mouse.")
            self.pause_signal.emit()
            
        def on_resume():
            print("[Hotkey] F9 Pressed: Resuming automation.")
            self.resume_signal.emit()
            
        def on_kill():
            print("[Hotkey] CTRL+SHIFT+F12 Pressed: Emergency Stop. Releasing mouse.")
            self.kill_signal.emit()

        def on_toggle_logs():
            print("[Hotkey] F10 Pressed: Toggling Logs Overlay.")
            self.toggle_logs_signal.emit()
            
        # Define hotkeys
        # <f8> and <f9> for pause/resume. 
        # <f10> for toggling logs.
        # <ctrl>+<shift>+<f12> for kill.
        hotkeys = {
            '<f8>': on_pause,
            '<f9>': on_resume,
            '<f10>': on_toggle_logs,
            '<ctrl>+<shift>+<f12>': on_kill
        }
        
        self.listener = keyboard.GlobalHotKeys(hotkeys)
        self.listener.start()
        
    def stop(self):
        if self.listener:
            self.listener.stop()
            self.listener = None
