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

        lbl = QLabel("Game Resolution")
        lbl.setStyleSheet("color: white; font-size: 14px; font-weight: bold; border: none;")
        layout.addWidget(lbl)

        hint = QLabel("Sets the resolution Salavan requests when launching the game executable.")
        hint.setStyleSheet("color: #52525b; font-size: 11px; border: none;")
        hint.setWordWrap(True)
        layout.addWidget(hint)

        # ── Resolution combo ──────────────────────────────────────────
        res_row = QHBoxLayout()
        res_lbl = QLabel("Resolution:")
        res_lbl.setStyleSheet("color: #a1a1aa; border: none;")
        res_lbl.setFixedWidth(100)

        self.combo_resolution = QComboBox()
        self.combo_resolution.setMinimumWidth(160)
        self.combo_resolution.setStyleSheet("""
            QComboBox {
                background-color: #0d0d11;
                color: white;
                border: 1px solid #3f3f46;
                border-radius: 6px;
                padding: 6px 12px;
                font-size: 13px;
            }
            QComboBox:focus { border: 1px solid #9333ea; }
            QComboBox::drop-down { border: none; width: 24px; }
            QComboBox::down-arrow {
                image: none;
                border-left: 4px solid transparent;
                border-right: 4px solid transparent;
                border-top: 6px solid #a1a1aa;
            }
            QComboBox QAbstractItemView {
                background-color: #18181c;
                color: white;
                border: 1px solid #3f3f46;
                selection-background-color: #7e22ce;
            }
        """)
        self._populate_resolutions()

        res_row.addWidget(res_lbl)
        res_row.addWidget(self.combo_resolution)
        res_row.addStretch()
        layout.addLayout(res_row)

        # ── Fullscreen toggle ─────────────────────────────────────────
        fs_row = QHBoxLayout()
        fs_lbl = QLabel("Fullscreen:")
        fs_lbl.setStyleSheet("color: #a1a1aa; border: none;")
        fs_lbl.setFixedWidth(100)

        self.cb_fullscreen = QCheckBox("Launch in fullscreen mode")
        self.cb_fullscreen.setStyleSheet("""
            QCheckBox {
                color: #a1a1aa;
                font-size: 12px;
                border: none;
                spacing: 8px;
            }
            QCheckBox::indicator {
                width: 40px;
                height: 22px;
                border-radius: 11px;
                border: 2px solid #3f3f46;
                background: #27272a;
            }
            QCheckBox::indicator:checked {
                background: #9333ea;
                border: 2px solid #7e22ce;
                image: none;
            }
            QCheckBox::indicator:unchecked:hover { border: 2px solid #52525b; }
            QCheckBox::indicator:checked:hover { background: #a855f7; }
        """)
        self.cb_fullscreen.setCursor(Qt.PointingHandCursor)

        fs_row.addWidget(fs_lbl)
        fs_row.addWidget(self.cb_fullscreen)
        fs_row.addStretch()
        layout.addLayout(fs_row)

        self.content_layout.addWidget(group)

    def _populate_resolutions(self):
        """Build resolution list from monitor capabilities + safe presets."""
        # Preset list (all common aspect-correct resolutions)
        presets = [
            (640, 360), (960, 540), (1280, 720),
            (1366, 768), (1600, 900), (1920, 1080),
            (2560, 1440), (3840, 2160),
        ]

        # Query the primary monitor for supported sizes
        try:
            from PySide6.QtWidgets import QApplication
            screen = QApplication.primaryScreen()
            for mode in screen.availableSizes() if hasattr(screen, "availableSizes") else []:
                presets.append((mode.width(), mode.height()))
        except Exception:
            pass

        # Deduplicate and sort by pixel count
        seen = set()
        unique = []
        for w, h in sorted(presets, key=lambda r: r[0] * r[1]):
            key = (w, h)
            if key not in seen:
                seen.add(key)
                unique.append((w, h))

        self.combo_resolution.clear()
        for w, h in unique:
            self.combo_resolution.addItem(f"{w} × {h}", userData=(w, h))

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

        # Select matching resolution in the single combo
        target_w = config.game_width
        target_h = config.game_height
        matched = False
        for i in range(self.combo_resolution.count()):
            data = self.combo_resolution.itemData(i)
            if data and data[0] == target_w and data[1] == target_h:
                self.combo_resolution.setCurrentIndex(i)
                matched = True
                break
        if not matched:
            # Insert and select the custom resolution at the top
            label = f"{target_w} × {target_h}"
            self.combo_resolution.insertItem(0, label, userData=(target_w, target_h))
            self.combo_resolution.setCurrentIndex(0)

        self.cb_fullscreen.setChecked(getattr(config, 'fullscreen', False))

    def _save_settings(self):
        config = self.app_controller.config
        config.game_exe_path = self.entry_exe.text().strip()
        config.record_test = self.cb_record.isChecked()
        config.dev_build_mode = self.cb_dev_mode.isChecked()
        config.hook_unity_editor = self.cb_editor_hook.isChecked()
        config.auto_sync_ui = self.cb_auto_sync.isChecked()

        # Save resolution from the single combo
        res_data = self.combo_resolution.currentData()
        if res_data:
            config.game_width  = res_data[0]
            config.game_height = res_data[1]

        # Save fullscreen toggle
        config.fullscreen = self.cb_fullscreen.isChecked()

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
