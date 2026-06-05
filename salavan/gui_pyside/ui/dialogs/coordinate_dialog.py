import os
from PySide6.QtWidgets import (
    QDialog, QVBoxLayout, QHBoxLayout, QLabel, QLineEdit, 
    QPushButton, QMessageBox
)
from PySide6.QtCore import Qt

class CoordinateDialog(QDialog):
    def __init__(self, target_path, initial_coords, on_save_callback, parent=None):
        super().__init__(parent)
        self.target_path = target_path
        self.initial_coords = initial_coords
        self.on_save_callback = on_save_callback
        
        self.setWindowTitle("Edit Coordinate Mapping")
        self.setFixedSize(400, 260)
        self.setStyleSheet("background-color: #18181c; color: white;")
        self._init_ui()

    def _init_ui(self):
        layout = QVBoxLayout(self)
        layout.setContentsMargins(20, 20, 20, 20)
        layout.setSpacing(10)

        # Object Path Info
        lbl_path = QLabel(f"<b>Object Path:</b> {self.target_path}")
        lbl_path.setWordWrap(True)
        lbl_path.setStyleSheet("color: #a855f7;")
        layout.addWidget(lbl_path)

        # X Coord Row
        row_x = QHBoxLayout()
        lbl_x = QLabel("X Coordinate:")
        lbl_x.setFixedWidth(120)
        self.ent_x = QLineEdit(str(self.initial_coords.get("x", 0.0)))
        self.ent_x.setStyleSheet("QLineEdit { background: #0d0d11; border: 1px solid #3f3f46; padding: 5px; border-radius: 4px; color: white; }")
        row_x.addWidget(lbl_x)
        row_x.addWidget(self.ent_x)
        layout.addLayout(row_x)

        # Y Coord Row
        row_y = QHBoxLayout()
        lbl_y = QLabel("Y Coordinate:")
        lbl_y.setFixedWidth(120)
        self.ent_y = QLineEdit(str(self.initial_coords.get("y", 0.0)))
        self.ent_y.setStyleSheet("QLineEdit { background: #0d0d11; border: 1px solid #3f3f46; padding: 5px; border-radius: 4px; color: white; }")
        row_y.addWidget(lbl_y)
        row_y.addWidget(self.ent_y)
        layout.addLayout(row_y)

        # Resolution Info
        lbl_res = QLabel(f"<b>Reference Resolution:</b> {self.initial_coords.get('resolution', '1280x720')}")
        lbl_res.setStyleSheet("color: #6b7280; font-size: 11px;")
        layout.addWidget(lbl_res)

        layout.addStretch()

        # Buttons Row
        btn_layout = QHBoxLayout()
        btn_layout.addStretch()

        btn_cancel = QPushButton("Cancel")
        btn_cancel.setStyleSheet("QPushButton { background: #27272a; border-radius: 4px; padding: 6px 15px; } QPushButton:hover { background: #3f3f46; }")
        btn_cancel.clicked.connect(self.reject)

        btn_save = QPushButton("Save")
        btn_save.setStyleSheet("QPushButton { background: #10b981; color: white; border-radius: 4px; padding: 6px 15px; font-weight: bold; } QPushButton:hover { background: #34d399; }")
        btn_save.clicked.connect(self._save)

        btn_layout.addWidget(btn_cancel)
        btn_layout.addWidget(btn_save)
        layout.addLayout(btn_layout)

    def _save(self):
        try:
            new_x = float(self.ent_x.text().strip())
            new_y = float(self.ent_y.text().strip())
        except ValueError:
            QMessageBox.warning(self, "Validation Error", "Coordinates must be valid numbers!")
            return

        resolution = self.initial_coords.get("resolution", "1280x720")
        self.on_save_callback(new_x, new_y, 100, 50, resolution)
        self.accept()
