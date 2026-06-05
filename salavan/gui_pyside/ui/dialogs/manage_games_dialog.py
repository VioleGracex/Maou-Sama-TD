from PySide6.QtWidgets import (
    QDialog, QVBoxLayout, QHBoxLayout, QLabel, QLineEdit, 
    QPushButton, QListWidget, QMessageBox
)
from PySide6.QtCore import Qt

class ManageGamesDialog(QDialog):
    def __init__(self, app_controller, parent=None):
        super().__init__(parent)
        self.app_controller = app_controller
        self.setWindowTitle("Manage Games")
        self.setFixedSize(500, 400)
        self.setStyleSheet("background-color: #18181c; color: white;")
        
        self._init_ui()

    def _init_ui(self):
        layout = QVBoxLayout(self)
        layout.setContentsMargins(20, 20, 20, 20)
        layout.setSpacing(15)
        
        lbl = QLabel("Configured Games:")
        lbl.setStyleSheet("font-size: 14px; font-weight: bold; border: none;")
        layout.addWidget(lbl)
        
        self.games_list = QListWidget()
        self.games_list.setStyleSheet("""
            QListWidget { background: #0d0d11; border: 1px solid #3f3f46; border-radius: 4px; padding: 5px; }
            QListWidget::item:selected { background: #9333ea; }
        """)
        self.games_list.itemSelectionChanged.connect(self._on_select)
        layout.addWidget(self.games_list)
        
        # Details Form
        form = QVBoxLayout()
        form.setSpacing(10)
        
        self.id_entry = self._make_row(form, "ID (Folder Name):")
        self.title_entry = self._make_row(form, "Display Title:")
        self.proc_entry = self._make_row(form, "Process / Window Name:")
        
        layout.addLayout(form)
        
        # Buttons
        btn_layout = QHBoxLayout()
        
        self.btn_add = QPushButton("Add New")
        self.btn_add.setStyleSheet("QPushButton { background: #27272a; padding: 6px 15px; border-radius: 4px; } QPushButton:hover { background: #3f3f46; }")
        self.btn_add.clicked.connect(self._add_game)
        
        self.btn_del = QPushButton("Delete")
        self.btn_del.setStyleSheet("QPushButton { background: #ef4444; color: white; padding: 6px 15px; border-radius: 4px; } QPushButton:hover { background: #f87171; }")
        self.btn_del.clicked.connect(self._delete_game)
        
        btn_layout.addWidget(self.btn_add)
        btn_layout.addWidget(self.btn_del)
        btn_layout.addStretch()
        
        self.btn_save = QPushButton("Save & Close")
        self.btn_save.setStyleSheet("QPushButton { background: #10b981; color: white; padding: 6px 15px; border-radius: 4px; font-weight: bold; } QPushButton:hover { background: #34d399; }")
        self.btn_save.clicked.connect(self._save_changes)
        
        btn_layout.addWidget(self.btn_save)
        layout.addLayout(btn_layout)
        
        self._refresh_list()

    def _make_row(self, layout, text):
        row = QHBoxLayout()
        lbl = QLabel(text)
        lbl.setFixedWidth(130)
        entry = QLineEdit()
        entry.setStyleSheet("QLineEdit { background: #0d0d11; border: 1px solid #3f3f46; padding: 5px; border-radius: 4px; }")
        row.addWidget(lbl)
        row.addWidget(entry)
        layout.addLayout(row)
        return entry

    def _refresh_list(self):
        self.games_list.clear()
        for g in self.app_controller.config.games:
            self.games_list.addItem(g.get("title", g.get("id", "Unknown")))

    def _on_select(self):
        idx = self.games_list.currentRow()
        if idx >= 0 and idx < len(self.app_controller.config.games):
            g = self.app_controller.config.games[idx]
            self.id_entry.setText(g.get("id", ""))
            self.title_entry.setText(g.get("title", ""))
            self.proc_entry.setText(g.get("process_name", ""))

    def _add_game(self):
        self.app_controller.config.games.append({
            "id": "new_game",
            "title": "New Game",
            "process_name": "Game.exe"
        })
        self._refresh_list()
        self.games_list.setCurrentRow(len(self.app_controller.config.games) - 1)

    def _delete_game(self):
        idx = self.games_list.currentRow()
        if idx >= 0:
            del self.app_controller.config.games[idx]
            self.id_entry.clear()
            self.title_entry.clear()
            self.proc_entry.clear()
            self._refresh_list()

    def _save_changes(self):
        idx = self.games_list.currentRow()
        if idx >= 0:
            self.app_controller.config.games[idx]["id"] = self.id_entry.text().strip()
            self.app_controller.config.games[idx]["title"] = self.title_entry.text().strip()
            self.app_controller.config.games[idx]["process_name"] = self.proc_entry.text().strip()
            self.app_controller.config.games[idx]["window_title"] = self.proc_entry.text().strip()
            
        self.app_controller.config.save()
        self.accept()
