import sys
from PySide6.QtWidgets import QWidget, QVBoxLayout, QLabel, QFrame
from PySide6.QtCore import Qt
from PySide6.QtGui import QFont

class TimerOverlay(QWidget):
    def __init__(self):
        super().__init__()

        self.setWindowFlags(
            Qt.WindowStaysOnTopHint |
            Qt.FramelessWindowHint |
            Qt.Tool | 
            Qt.WindowTransparentForInput
        )
        self.setAttribute(Qt.WA_TranslucentBackground)
        self.setAttribute(Qt.WA_TransparentForMouseEvents)

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

    def show_wait(self, title, total_seconds, screen_width=1920):
        self.title_label.setText(title)
        self.time_label.setText(f"{total_seconds:.1f}s")
        
        # Position top-center
        x = (screen_width // 2) - 150
        self.setGeometry(x, 20, 300, 60)
        self.show()
        self.raise_()

    def update_progress(self, remaining_seconds):
        self.time_label.setText(f"{remaining_seconds:.1f}s")

    def hide_wait(self):
        self.hide()
