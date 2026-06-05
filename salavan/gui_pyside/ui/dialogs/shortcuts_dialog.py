from PySide6.QtWidgets import (
    QDialog, QVBoxLayout, QLabel, QPushButton, QGridLayout
)
from PySide6.QtCore import Qt

class ShortcutsDialog(QDialog):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("Keyboard Shortcuts")
        self.setFixedSize(300, 250)
        self.setStyleSheet("background-color: #18181c; color: white;")
        
        self._init_ui()

    def _init_ui(self):
        layout = QVBoxLayout(self)
        layout.setContentsMargins(20, 20, 20, 20)
        layout.setSpacing(15)
        
        lbl = QLabel("Global Hotkeys")
        lbl.setStyleSheet("font-size: 16px; font-weight: bold; border: none;")
        lbl.setAlignment(Qt.AlignCenter)
        layout.addWidget(lbl)
        
        grid = QGridLayout()
        grid.setSpacing(10)
        
        shortcuts = [
            ("Pause Execution", "Ctrl + P"),
            ("Abort Execution", "Ctrl + Q"),
            ("Toggle HUD Mode", "Ctrl + O"),
            ("Next Scenario", "Ctrl + Right"),
            ("Previous Scenario", "Ctrl + Left"),
            ("Repeat Scenario", "Ctrl + Down")
        ]
        
        for row, (desc, key) in enumerate(shortcuts):
            d_lbl = QLabel(desc)
            d_lbl.setStyleSheet("color: #a1a1aa; border: none;")
            k_lbl = QLabel(key)
            k_lbl.setStyleSheet("color: #a855f7; font-weight: bold; border: none;")
            k_lbl.setAlignment(Qt.AlignRight)
            grid.addWidget(d_lbl, row, 0)
            grid.addWidget(k_lbl, row, 1)
            
        layout.addLayout(grid)
        layout.addStretch()
        
        btn_close = QPushButton("Close")
        btn_close.setStyleSheet("QPushButton { background: #27272a; border-radius: 4px; padding: 6px 15px; } QPushButton:hover { background: #3f3f46; }")
        btn_close.clicked.connect(self.accept)
        layout.addWidget(btn_close)
