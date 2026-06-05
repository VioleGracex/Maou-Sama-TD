from PySide6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QLabel, QPushButton, 
    QScrollArea, QFrame, QLineEdit, QCheckBox, QComboBox, QFileDialog
)
from PySide6.QtCore import Qt
from ui.dialogs.shortcuts_dialog import ShortcutsDialog

class SettingsPage(QWidget):
    def __init__(self, app_controller, parent=None):
        super().__init__(parent)
        self.app_controller = app_controller
        self._init_ui()

    def _init_ui(self):
        self.main_layout = QVBoxLayout(self)
        self.main_layout.setContentsMargins(0, 0, 0, 0)
        self.main_layout.setSpacing(0)
        
        # Header
        self.header = QWidget()
        self.header.setStyleSheet("background-color: #101012; border-bottom: 1px solid #1a1a21;")
        self.header.setFixedHeight(60)
        header_layout = QHBoxLayout(self.header)
        header_layout.setContentsMargins(20, 0, 20, 0)
        
        title = QLabel("Settings")
        title.setStyleSheet("color: white; font-size: 18px; font-weight: bold; font-family: 'Segoe UI'; border: none;")
        header_layout.addWidget(title)
        
        header_layout.addStretch()
        
        btn_shortcuts = QPushButton("View Shortcuts")
        btn_shortcuts.setCursor(Qt.PointingHandCursor)
        btn_shortcuts.setStyleSheet("QPushButton { background: #27272a; color: white; border-radius: 4px; padding: 6px 15px; font-weight: bold; } QPushButton:hover { background: #3f3f46; }")
        btn_shortcuts.clicked.connect(self._open_shortcuts)
        header_layout.addWidget(btn_shortcuts)
        
        self.btn_save = QPushButton("Save Settings")
        self.btn_save.setCursor(Qt.PointingHandCursor)
        self.btn_save.setStyleSheet("""
            QPushButton {
                background-color: #9333ea;
                color: white;
                border-radius: 4px;
                padding: 6px 15px;
                font-weight: bold;
            }
            QPushButton:hover {
                background-color: #a855f7;
            }
        """)
        self.btn_save.clicked.connect(self._save_settings)
        header_layout.addWidget(self.btn_save)
        
        # Scroll Area
        self.scroll_area = QScrollArea()
        self.scroll_area.setWidgetResizable(True)
        self.scroll_area.setStyleSheet("""
            QScrollArea { border: none; background-color: #0d0d11; }
            QScrollBar:vertical {
                background: #101012;
                width: 10px;
                margin: 0px 0px 0px 0px;
            }
            QScrollBar::handle:vertical {
                background: #3f3f46;
                min-height: 20px;
                border-radius: 5px;
            }
            QScrollBar::handle:vertical:hover {
                background: #52525b;
            }
            QScrollBar::add-line:vertical, QScrollBar::sub-line:vertical { height: 0px; }
        """)
        
        self.content_container = QWidget()
        self.content_container.setStyleSheet("background-color: #0d0d11;")
        self.content_layout = QVBoxLayout(self.content_container)
        self.content_layout.setContentsMargins(30, 30, 30, 30)
        self.content_layout.setSpacing(20)
        self.content_layout.setAlignment(Qt.AlignTop)
        
        self._build_paths_section()
        self._build_options_section()
        self._build_resolution_section()
        
        self.scroll_area.setWidget(self.content_container)
        
        self.main_layout.addWidget(self.header)
        self.main_layout.addWidget(self.scroll_area)
        
        self._load_settings()

    def _build_paths_section(self):
        group = QFrame()
        group.setStyleSheet("QFrame { background-color: #18181c; border-radius: 8px; border: 1px solid #27272a; }")
        layout = QVBoxLayout(group)
        layout.setContentsMargins(20, 20, 20, 20)
        layout.setSpacing(15)
        
        lbl = QLabel("Paths & Executables")
        lbl.setStyleSheet("color: white; font-size: 14px; font-weight: bold; border: none;")
        layout.addWidget(lbl)
        
        # Game EXE Path
        row = QHBoxLayout()
        row.setSpacing(10)
        exe_lbl = QLabel("Game EXE Path:")
        exe_lbl.setStyleSheet("color: #a1a1aa; border: none;")
        exe_lbl.setFixedWidth(120)
        
        self.entry_exe = QLineEdit()
        self.entry_exe.setStyleSheet("""
            QLineEdit {
                background-color: #0d0d11;
                color: white;
                border: 1px solid #3f3f46;
                border-radius: 4px;
                padding: 5px;
            }
            QLineEdit:focus {
                border: 1px solid #9333ea;
            }
        """)
        
        btn_browse = QPushButton("Browse")
        btn_browse.setCursor(Qt.PointingHandCursor)
        btn_browse.setStyleSheet("""
            QPushButton {
                background-color: #27272a;
                color: white;
                border-radius: 4px;
                padding: 5px 15px;
            }
            QPushButton:hover {
                background-color: #3f3f46;
            }
        """)
        btn_browse.clicked.connect(self._browse_exe)
        
        row.addWidget(exe_lbl)
        row.addWidget(self.entry_exe)
        row.addWidget(btn_browse)
        
        layout.addLayout(row)
        self.content_layout.addWidget(group)

    def _build_options_section(self):
        group = QFrame()
        group.setStyleSheet("QFrame { background-color: #18181c; border-radius: 8px; border: 1px solid #27272a; }")
        layout = QVBoxLayout(group)
        layout.setContentsMargins(20, 20, 20, 20)
        layout.setSpacing(15)
        
        lbl = QLabel("Test Engine Options")
        lbl.setStyleSheet("color: white; font-size: 14px; font-weight: bold; border: none;")
        layout.addWidget(lbl)
        
        cb_style = """
            QCheckBox { color: #a1a1aa; font-size: 12px; border: none; }
            QCheckBox::indicator { width: 16px; height: 16px; border-radius: 3px; border: 1px solid #3f3f46; background: #0d0d11; }
            QCheckBox::indicator:checked { background: #9333ea; border: 1px solid #9333ea; image: url(None); }
        """
        
        self.cb_record = QCheckBox("Record Test Execution Video")
        self.cb_record.setStyleSheet(cb_style)
        layout.addWidget(self.cb_record)
        
        self.cb_dev_mode = QCheckBox("Enable Unity Log Scanning (Dev Build)")
        self.cb_dev_mode.setStyleSheet(cb_style)
        layout.addWidget(self.cb_dev_mode)
        
        self.cb_auto_sync = QCheckBox("Auto-Sync UI Position with Game Window")
        self.cb_auto_sync.setStyleSheet(cb_style)
        layout.addWidget(self.cb_auto_sync)
        
        self.cb_editor_hook = QCheckBox("Hook 'Unity Editor' (Overrides EXE launch)")
        self.cb_editor_hook.setStyleSheet(cb_style)
        layout.addWidget(self.cb_editor_hook)
        
        self.content_layout.addWidget(group)

    def _build_resolution_section(self):
        group = QFrame()
        group.setStyleSheet("QFrame { background-color: #18181c; border-radius: 8px; border: 1px solid #27272a; }")
        layout = QVBoxLayout(group)
        layout.setContentsMargins(20, 20, 20, 20)
        layout.setSpacing(15)
        
        lbl = QLabel("Launch Resolution")
        lbl.setStyleSheet("color: white; font-size: 14px; font-weight: bold; border: none;")
        layout.addWidget(lbl)
        
        row = QHBoxLayout()
        row.setSpacing(20)
        
        combo_style = """
            QComboBox {
                background-color: #0d0d11;
                color: white;
                border: 1px solid #3f3f46;
                border-radius: 4px;
                padding: 5px;
            }
            QComboBox::drop-down { border: none; }
        """
        
        # Width
        w_layout = QHBoxLayout()
        w_lbl = QLabel("Width:")
        w_lbl.setStyleSheet("color: #a1a1aa; border: none;")
        self.combo_w = QComboBox()
        self.combo_w.setStyleSheet(combo_style)
        self.combo_w.addItems(["960", "1280", "1920", "2560"])
        w_layout.addWidget(w_lbl)
        w_layout.addWidget(self.combo_w)
        
        # Height
        h_layout = QHBoxLayout()
        h_lbl = QLabel("Height:")
        h_lbl.setStyleSheet("color: #a1a1aa; border: none;")
        self.combo_h = QComboBox()
        self.combo_h.setStyleSheet(combo_style)
        self.combo_h.addItems(["540", "720", "1080", "1440"])
        h_layout.addWidget(h_lbl)
        h_layout.addWidget(self.combo_h)
        
        row.addLayout(w_layout)
        row.addLayout(h_layout)
        row.addStretch()
        
        layout.addLayout(row)
        self.content_layout.addWidget(group)

    def _browse_exe(self):
        file_path, _ = QFileDialog.getOpenFileName(self, "Select Game EXE", "", "Executable Files (*.exe);;All Files (*.*)")
        if file_path:
            self.entry_exe.setText(file_path)

    def _load_settings(self):
        config = self.app_controller.config
        self.entry_exe.setText(config.game_exe_path)
        self.cb_record.setChecked(config.record_test)
        self.cb_dev_mode.setChecked(config.dev_build_mode)
        self.cb_editor_hook.setChecked(config.hook_unity_editor)
        self.cb_auto_sync.setChecked(config.auto_sync_ui)
        self.combo_w.setCurrentText(str(config.game_width))
        self.combo_h.setCurrentText(str(config.game_height))

    def _save_settings(self):
        config = self.app_controller.config
        config.game_exe_path = self.entry_exe.text().strip()
        config.record_test = self.cb_record.isChecked()
        config.dev_build_mode = self.cb_dev_mode.isChecked()
        config.hook_unity_editor = self.cb_editor_hook.isChecked()
        config.auto_sync_ui = self.cb_auto_sync.isChecked()
        
        try:
            config.game_width = int(self.combo_w.currentText())
            config.game_height = int(self.combo_h.currentText())
        except ValueError:
            pass
            
        config.save()
        
        # Show brief save confirmation by changing button text
        orig_text = self.btn_save.text()
        self.btn_save.setText("Saved!")
        self.btn_save.setStyleSheet("QPushButton { background-color: #10b981; color: white; border-radius: 4px; padding: 6px 15px; font-weight: bold; }")
        
        from PySide6.QtCore import QTimer
        def restore():
            self.btn_save.setText(orig_text)
            self.btn_save.setStyleSheet("""
                QPushButton { background-color: #9333ea; color: white; border-radius: 4px; padding: 6px 15px; font-weight: bold; }
                QPushButton:hover { background-color: #a855f7; }
            """)
        QTimer.singleShot(1500, restore)

    def _open_shortcuts(self):
        dlg = ShortcutsDialog(self)
        dlg.exec()
