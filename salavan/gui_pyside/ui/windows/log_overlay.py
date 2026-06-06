import sys
from PySide6.QtWidgets import QWidget, QVBoxLayout, QTextEdit, QFrame
from PySide6.QtCore import Qt
from PySide6.QtGui import QFont, QColor

class LogOverlay(QWidget):
    def __init__(self):
        super().__init__()

        # Frameless, stays on top, tool window so it doesn't show in taskbar
        self.setWindowFlags(
            Qt.WindowStaysOnTopHint |
            Qt.FramelessWindowHint |
            Qt.Tool | 
            Qt.WindowTransparentForInput  # Make it transparent to mouse clicks
        )
        self.setAttribute(Qt.WA_TranslucentBackground)
        self.setAttribute(Qt.WA_TransparentForMouseEvents)

        self._root = QFrame(self)
        self._root.setGeometry(0, 0, 500, 400)
        self._root.setStyleSheet("""
            QFrame {
                background-color: rgba(20, 20, 20, 200);
                border-radius: 10px;
                border: 1px solid rgba(100, 100, 100, 100);
            }
        """)

        layout = QVBoxLayout(self._root)
        layout.setContentsMargins(10, 10, 10, 10)
        
        self.log_text = QTextEdit()
        self.log_text.setReadOnly(True)
        self.log_text.setFont(QFont("Consolas", 10))
        self.log_text.setStyleSheet("""
            QTextEdit {
                background: transparent;
                border: none;
                color: #CCCCCC;
            }
        """)
        # Disable scrollbar interaction since we are transparent to input
        self.log_text.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.log_text.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)

        layout.addWidget(self.log_text)
        
        self.setGeometry(20, 20, 500, 400)

    def append_log(self, step, status, msg):
        color = "#CCCCCC"
        if status == "PASS":
            color = "#4CAF50"
        elif status == "FAIL" or status == "ERROR":
            color = "#F44336"
        elif status == "INFO":
            color = "#2196F3"
        elif status == "WARNING":
            color = "#FFC107"

        # Format HTML log line
        line = f'<span style="color: {color};"><b>[{status}]</b></span> <span style="color: #AAAAAA;">{step}:</span> {msg}'
        self.log_text.append(line)
        # Scroll to bottom automatically
        self.log_text.ensureCursorVisible()

    def toggle_visibility(self):
        if self.isVisible():
            self.hide()
        else:
            screen_height = self.screen().geometry().height()
            self.setGeometry(20, screen_height - 420, 500, 400)
            self.show()
            self.raise_()

    def show_overlay(self, screen_height=1080):
        # Position bottom-left
        self.setGeometry(20, screen_height - 420, 500, 400)
        self.show()

    def hide_overlay(self):
        self.hide()
