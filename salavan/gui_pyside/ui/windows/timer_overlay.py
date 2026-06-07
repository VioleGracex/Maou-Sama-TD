import sys
from PySide6.QtWidgets import QWidget, QVBoxLayout, QLabel, QFrame
from PySide6.QtCore import Qt, QPoint
from PySide6.QtGui import QFont, QCursor

class TimerOverlay(QWidget):
    def __init__(self):
        super().__init__()

        self._drag_mode = False
        self._drag_offset = QPoint()

        self._set_window_flags(interactive=False)
        self.setAttribute(Qt.WA_TranslucentBackground)

        self._root = QFrame(self)
        self._root.setGeometry(0, 0, 300, 60)
        self._root.setStyleSheet("""
            QFrame {
                background-color: rgba(20, 20, 20, 220);
                border-radius: 8px;
                border: 1px solid rgba(100, 100, 100, 150);
            }
        """)

        layout = QVBoxLayout(self._root)
        layout.setContentsMargins(10, 5, 10, 5)

        self.title_label = QLabel("Waiting...")
        self.title_label.setStyleSheet("color: #AAAAAA; font-weight: bold; font-family: 'Segoe UI'; font-size: 12px;")
        self.title_label.setAlignment(Qt.AlignCenter)

        self.time_label = QLabel("0.0s")
        self.time_label.setStyleSheet("color: #4CAF50; font-weight: bold; font-family: 'Consolas'; font-size: 16px;")
        self.time_label.setAlignment(Qt.AlignCenter)

        layout.addWidget(self.title_label)
        layout.addWidget(self.time_label)

        self.setGeometry(0, 0, 300, 60)

    # ── Window flag helpers ────────────────────────────────────────────

    def _set_window_flags(self, interactive: bool):
        flags = Qt.WindowStaysOnTopHint | Qt.FramelessWindowHint | Qt.Tool
        if not interactive:
            flags |= Qt.WindowTransparentForInput
            self.setAttribute(Qt.WA_TransparentForMouseEvents, True)
        else:
            self.setAttribute(Qt.WA_TransparentForMouseEvents, False)
        self.setWindowFlags(flags)

    # ── Drag mode API ─────────────────────────────────────────────────

    def enable_drag_mode(self):
        if self._drag_mode:
            return
        self._drag_mode = True
        self._set_window_flags(interactive=True)
        self.setCursor(QCursor(Qt.OpenHandCursor))
        self._root.setStyleSheet("""
            QFrame {
                background-color: rgba(20, 80, 20, 240);
                border-radius: 8px;
                border: 1px solid rgba(80, 220, 80, 200);
            }
        """)
        self.show()
        self.raise_()

    def disable_drag_mode(self):
        self._drag_mode = False
        self._set_window_flags(interactive=False)
        self.setCursor(QCursor(Qt.ArrowCursor))
        self._root.setStyleSheet("""
            QFrame {
                background-color: rgba(20, 20, 20, 220);
                border-radius: 8px;
                border: 1px solid rgba(100, 100, 100, 150);
            }
        """)
        self.show()
        self.raise_()

    # ── Mouse drag events ─────────────────────────────────────────────

    def mousePressEvent(self, event):
        if self._drag_mode and event.button() == Qt.LeftButton:
            self._drag_offset = event.globalPosition().toPoint() - self.frameGeometry().topLeft()
            self.setCursor(QCursor(Qt.ClosedHandCursor))
        super().mousePressEvent(event)

    def mouseMoveEvent(self, event):
        if self._drag_mode and event.buttons() & Qt.LeftButton:
            self.move(event.globalPosition().toPoint() - self._drag_offset)
        super().mouseMoveEvent(event)

    def mouseReleaseEvent(self, event):
        if self._drag_mode:
            self.setCursor(QCursor(Qt.OpenHandCursor))
        super().mouseReleaseEvent(event)

    # ── Content methods ───────────────────────────────────────────────

    def show_wait(self, title, total_seconds, screen_width=1920):
        self.title_label.setText(title)
        self.time_label.setText(f"{total_seconds:.1f}s")

        # Position top-center using real monitor width
        try:
            from PySide6.QtWidgets import QApplication
            monitor_w = QApplication.primaryScreen().geometry().width()
        except Exception:
            monitor_w = screen_width
        x = (monitor_w // 2) - 150
        self.setGeometry(x, 20, 300, 60)
        self.show()
        self.raise_()

    def update_progress(self, remaining_seconds):
        self.time_label.setText(f"{remaining_seconds:.1f}s")

    def hide_wait(self):
        self.hide()
