import sys
from PySide6.QtWidgets import QWidget, QLabel, QVBoxLayout, QHBoxLayout, QFrame
from PySide6.QtCore import Qt, QTimer
from PySide6.QtGui import QFont, QColor, QPainter, QLinearGradient

class LockOverlay(QWidget):
    def __init__(self):
        super().__init__()

        self.setWindowFlags(
            Qt.WindowStaysOnTopHint |
            Qt.FramelessWindowHint |
            Qt.WindowTransparentForInput |
            Qt.Tool
        )
        self.setAttribute(Qt.WA_TranslucentBackground)
        self.setAttribute(Qt.WA_TransparentForMouseEvents)

        self._steps = []
        self._active_idx = -1

        # Main layout: header bar at top, step panel on right side
        self._root = QWidget(self)
        self._root.setGeometry(0, 0, 340, 900)
        self._root.setStyleSheet("background: transparent;")

        layout = QVBoxLayout(self._root)
        layout.setContentsMargins(8, 8, 8, 8)
        layout.setSpacing(6)

        # ── Header Banner ──────────────────────────────────────────────
        self._header = QLabel("⚙ AUTOMATION RUNNING\nF8 Pause  ·  F9 Resume\nCtrl+Shift+F12 Kill")
        self._header.setAlignment(Qt.AlignCenter)
        self._header.setFont(QFont("Arial", 11, QFont.Bold))
        self._header.setStyleSheet("""
            QLabel {
                background-color: rgba(220, 30, 30, 200);
                color: white;
                border-radius: 10px;
                padding: 8px 14px;
            }
        """)
        layout.addWidget(self._header)

        # ── Step List Panel ────────────────────────────────────────────
        self._step_container = QFrame()
        self._step_container.setStyleSheet("""
            QFrame {
                background-color: rgba(0, 0, 0, 130);
                border-radius: 10px;
            }
        """)
        self._step_layout = QVBoxLayout(self._step_container)
        self._step_layout.setContentsMargins(10, 8, 10, 8)
        self._step_layout.setSpacing(3)

        title = QLabel("SCENARIO STEPS")
        title.setFont(QFont("Arial", 8, QFont.Bold))
        title.setStyleSheet("color: rgba(255,255,255,120); background: transparent;")
        self._step_layout.addWidget(title)

        self._step_labels = []
        layout.addWidget(self._step_container)
        layout.addStretch()

        self.setGeometry(0, 0, 340, 900)

    def update_steps(self, steps, active_idx):
        self._steps = steps
        self._active_idx = active_idx

        # Clear old labels
        for lbl in self._step_labels:
            lbl.setParent(None)
            lbl.deleteLater()
        self._step_labels.clear()

        for i, step in enumerate(steps):
            # Strip leading "N. " numbering for display
            import re
            clean = re.sub(r'^\d+\.\s*', '', step).strip()
            if len(clean) > 32:
                clean = clean[:31] + "…"

            if i < active_idx:
                # Completed
                text = f"✓  {clean}"
                style = "color: rgba(120,220,120,160); font-size: 11px; background: transparent; padding: 1px 4px;"
            elif i == active_idx:
                # Current — bright + bold
                text = f"▶  {clean}"
                style = """
                    color: #FFE066;
                    font-size: 12px;
                    font-weight: bold;
                    background-color: rgba(255,220,50,40);
                    border-left: 3px solid #FFE066;
                    border-radius: 4px;
                    padding: 2px 6px;
                """
            else:
                # Pending
                text = f"○  {clean}"
                style = "color: rgba(200,200,200,100); font-size: 11px; background: transparent; padding: 1px 4px;"

            lbl = QLabel(text)
            lbl.setWordWrap(False)
            lbl.setStyleSheet(style)
            self._step_layout.addWidget(lbl)
            self._step_labels.append(lbl)

        # Resize height dynamically
        step_count = max(len(steps), 1)
        panel_h = 110 + step_count * 24 + 20
        self._root.setFixedHeight(panel_h)
        self.setFixedHeight(panel_h)

    def show_overlay(self, screen_width=1920):
        # Position on top-right corner
        x = screen_width - 360
        self.setGeometry(x, 20, 340, self.height() or 900)
        self._root.setGeometry(0, 0, 340, self.height())
        self.show()
        self.raise_()

    def hide_overlay(self):
        self.hide()
