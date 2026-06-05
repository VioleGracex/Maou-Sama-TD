import pygetwindow as gw
from PySide6.QtCore import QObject, QTimer, Signal

class OverlayService(QObject):
    # Emitted when the game window is found and its rect is determined
    # signature: (x, y, width, height, hwnd)
    game_rect_updated = Signal(int, int, int, int, int)
    
    # Emitted when the game is found vs lost
    game_status_changed = Signal(bool)

    # Emitted when we want the MainWindow to snap to a specific coordinate
    # signature: (target_x, target_y)
    request_snap = Signal(int, int)

    def __init__(self, parent=None):
        super().__init__(parent)
        self.polling_timer = QTimer(self)
        self.polling_timer.setInterval(200) # 200ms
        self.polling_timer.timeout.connect(self._poll_game_window)
        
        self.target_process_name = ""
        self.target_window_title = ""
        self.auto_sync_ui = False
        
        self._last_game_rect = None
        self._is_game_running = False

    def update_settings(self, process_name, window_title, auto_sync):
        self.target_process_name = process_name
        self.target_window_title = window_title
        self.auto_sync_ui = auto_sync

    def start_polling(self):
        self.polling_timer.start()

    def stop_polling(self):
        self.polling_timer.stop()

    def _poll_game_window(self):
        if not self.target_window_title:
            return

        try:
            windows = gw.getWindowsWithTitle(self.target_window_title)
            game_win = None
            for w in windows:
                if w.title.strip() == self.target_window_title.strip():
                    game_win = w
                    break

            if game_win and game_win.width > 0 and game_win.height > 0:
                if not self._is_game_running:
                    self._is_game_running = True
                    self.game_status_changed.emit(True)

                current_rect = (game_win.left, game_win.top, game_win.width, game_win.height, game_win._hWnd)
                
                # Emit updated rect if it changed
                if current_rect != self._last_game_rect:
                    self._last_game_rect = current_rect
                    self.game_rect_updated.emit(*current_rect)
                    
                    # If auto-sync is enabled, request the main window to snap alongside the game
                    if self.auto_sync_ui:
                        target_x = game_win.left + game_win.width
                        target_y = game_win.top
                        self.request_snap.emit(target_x, target_y)

            else:
                if self._is_game_running:
                    self._is_game_running = False
                    self.game_status_changed.emit(False)
                    self._last_game_rect = None
                    self.game_rect_updated.emit(0, 0, 0, 0, 0)

        except Exception:
            pass
