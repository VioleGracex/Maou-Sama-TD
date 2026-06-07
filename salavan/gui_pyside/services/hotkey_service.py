from pynput import keyboard
from PySide6.QtCore import QObject, Signal
import threading

class HotkeyService(QObject):
    pause_signal = Signal()
    resume_signal = Signal()
    kill_signal = Signal()
    toggle_logs_signal = Signal()
    # Overlay drag signals — emitted when Alt is pressed/released globally
    overlay_drag_start = Signal()
    overlay_drag_stop  = Signal()

    def __init__(self):
        super().__init__()
        self.mouse_listener = None
        self.listener = None
        # Raw keyboard listener for Alt key tracking
        self._raw_listener = None
        self._alt_pressed = False

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
        hotkeys = {
            '<f8>': on_pause,
            '<f9>': on_resume,
            '<f10>': on_toggle_logs,
            '<ctrl>+<shift>+<f12>': on_kill
        }

        self.listener = keyboard.GlobalHotKeys(hotkeys)
        self.listener.start()

        # Raw listener for Alt key drag toggle
        def _on_key_press(key):
            if key in (keyboard.Key.alt, keyboard.Key.alt_l, keyboard.Key.alt_r, keyboard.Key.alt_gr):
                if not self._alt_pressed:
                    self._alt_pressed = True
                    self.overlay_drag_start.emit()

        def _on_key_release(key):
            if key in (keyboard.Key.alt, keyboard.Key.alt_l, keyboard.Key.alt_r, keyboard.Key.alt_gr):
                if self._alt_pressed:
                    self._alt_pressed = False
                    self.overlay_drag_stop.emit()

        self._raw_listener = keyboard.Listener(
            on_press=_on_key_press,
            on_release=_on_key_release
        )
        self._raw_listener.daemon = True
        self._raw_listener.start()

    def stop(self):
        if self.listener:
            self.listener.stop()
            self.listener = None
        if self._raw_listener:
            self._raw_listener.stop()
            self._raw_listener = None
