import os
import sys
import platform
import subprocess
import re
import json
from PySide6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QLabel, QPushButton, 
    QScrollArea, QFrame, QStackedWidget, QComboBox, QLineEdit, QCheckBox,
    QTreeWidget, QTreeWidgetItem, QTextEdit
)
from PySide6.QtCore import Qt, QTimer
from PySide6.QtGui import QPixmap

class CenterDashboard(QWidget):
    def __init__(self, app_controller, parent=None):
        super().__init__(parent)
        self.app_controller = app_controller
        self._init_ui()

    def _init_ui(self):
        main_layout = QVBoxLayout(self)
        main_layout.setContentsMargins(10, 10, 10, 10)
        main_layout.setSpacing(10)
        self.setStyleSheet("background-color: #18181c;")
        
        # Title and Status
        hdr = QHBoxLayout()
        title = QLabel("// SALAVAN TEST RUNNER HUD")
        title.setStyleSheet("color: #eab308; font-weight: bold; font-size: 16px;")
        hdr.addWidget(title)
        hdr.addStretch()
        main_layout.addLayout(hdr)
        
        status_row = QHBoxLayout()
        dot = QLabel("●")
        dot.setStyleSheet("color: #eab308;")
        status_lbl = QLabel("SYSTEM STATUS: IDLE")
        status_lbl.setStyleSheet("color: white; font-weight: bold;")
        status_row.addWidget(dot)
        status_row.addWidget(status_lbl)
        status_row.addStretch()
        
        self.btn_mode = QPushButton("[ 🖥 OVERLAY MODE ]")
        self.btn_mode.setStyleSheet("background: #2c2c35; color: white; padding: 5px; border: none;")
        self.btn_mode.setCursor(Qt.PointingHandCursor)
        self.btn_mode.clicked.connect(self._toggle_overlay)
        
        self.btn_dock = QPushButton("[ 📥 DOCK LOGS ]")
        self.btn_dock.setStyleSheet("background: #2c2c35; color: white; padding: 5px; border: none;")
        self.btn_dock.setCursor(Qt.PointingHandCursor)
        self.btn_dock.setVisible(False)
        
        status_row.addWidget(self.btn_mode)
        status_row.addWidget(self.btn_dock)
        main_layout.addLayout(status_row)
        
        # Tabs navigator
        tabs_nav = QHBoxLayout()
        tabs_nav.setSpacing(2)
        
        tab_names = [
            "⚙️ SETUP", "📺 LIVE VIEW", "📋 SCENARIO", "📍 UI COORDS", 
            "📄 LOGS", "📟 SPECS", "🎥 MEDIA", "🗺️ MAPPINGS"
        ]
        self.tab_btns = []
        for i, name in enumerate(tab_names):
            btn = QPushButton(name)
            btn.setStyleSheet("""
                QPushButton { background: #101012; color: #9ca3af; border: none; padding: 8px; font-weight: bold; }
                QPushButton:hover { background: #202024; color: #eab308; }
            """)
            btn.setCursor(Qt.PointingHandCursor)
            btn.clicked.connect(lambda checked=False, idx=i: self.switch_tab(idx))
            tabs_nav.addWidget(btn)
            self.tab_btns.append(btn)
            
        main_layout.addLayout(tabs_nav)
        
        # Stacked Widget
        self.stack = QStackedWidget()
        
        # Setup Tab
        self.tab_setup = self._create_setup_tab()
        self.stack.addWidget(self.tab_setup)
        
        # Live View
        self.tab_live = self._create_live_tab()
        self.stack.addWidget(self.tab_live)
        
        # Scenario Tab
        self.tab_scenario = self._create_scenario_tab()
        self.stack.addWidget(self.tab_scenario)
        
        # UI Coords Tab
        self.tab_ui = self._create_ui_tab()
        self.stack.addWidget(self.tab_ui)
        
        # Logs Tab
        self.tab_logs = self._create_logs_tab()
        self.stack.addWidget(self.tab_logs)
        
        # Specs Tab
        self.tab_specs = self._create_specs_tab()
        self.stack.addWidget(self.tab_specs)
        
        # Media Tab
        self.tab_media = self._create_media_tab()
        self.stack.addWidget(self.tab_media)
        
        # Mappings Tab
        self.tab_mappings = self._create_mappings_tab()
        self.stack.addWidget(self.tab_mappings)
        
        main_layout.addWidget(self.stack)
        
        # Wire up preview updates and console additions
        self.app_controller.test_finished.connect(self._on_test_finished)
        self.app_controller.preview_frame.connect(self.update_preview)
        self.app_controller.prompt_missing_template_sig.connect(self._on_missing_template_prompt)
        self.app_controller.log_added.connect(self.append_console)

        # Refresh mappings now that mappings_tree widget exists
        self.refresh_mappings_tree()

        # Timer for live scenario/UI coords updates
        self.update_timer = QTimer(self)
        self.update_timer.timeout.connect(self._on_timer_tick)
        self.update_timer.start(1000)

        # Default tab
        self.switch_tab(0) # SETUP

    def _on_missing_template_prompt(self, name):
        try:
            from ui.dialogs.capture_wizard_dialog import CaptureWizardDialog
            wizard = CaptureWizardDialog(self.app_controller, parent=self.window())
            wizard.expected_name = name
            if wizard.exec():
                if hasattr(wizard, 'last_saved_coords'):
                    self.app_controller.missing_template_coords = wizard.last_saved_coords
        except Exception as e:
            self.app_controller.log_message("SYSTEM", "FAIL", f"Capture Wizard auto-mapper failed to launch: {e}")
        finally:
            self.app_controller.missing_template_resolved_event.set()

    def update_preview(self, qimage):
        if not getattr(self, 'chk_live_preview', None) or not self.chk_live_preview.isChecked():
            return
        pixmap = QPixmap.fromImage(qimage)
        if not self.preview_lbl.size().isEmpty():
            scaled_pix = pixmap.scaled(self.preview_lbl.size(), Qt.KeepAspectRatio, Qt.SmoothTransformation)
            self.preview_lbl.setPixmap(scaled_pix)

    def append_console(self, step, status, msg):
        self.console.append(f"[{step}] {status}: {msg}")

    def switch_tab(self, index):
        self.stack.setCurrentIndex(index)
        for i, btn in enumerate(self.tab_btns):
            if i == index:
                btn.setStyleSheet("QPushButton { background: #242428; color: #eab308; border: none; padding: 8px; font-weight: bold; }")
            else:
                btn.setStyleSheet("""
                    QPushButton { background: #101012; color: #9ca3af; border: none; padding: 8px; font-weight: bold; }
                    QPushButton:hover { background: #202024; color: #eab308; }
                """)

    def _create_setup_tab(self):
        w = QWidget()
        l = QVBoxLayout(w)
        l.setContentsMargins(15, 15, 15, 15)
        
        lbl = QLabel("ACTIVE GAME PROFILE:")
        lbl.setStyleSheet("color: #eab308; font-weight: bold;")
        l.addWidget(lbl)
        
        row1 = QHBoxLayout()
        self.cb_profile = QComboBox()
        self.cb_profile.setStyleSheet("QComboBox { background: #151518; color: white; border: 1px solid #3f3f46; padding: 5px; }")
        row1.addWidget(self.cb_profile, 1)
        self.btn_manage = QPushButton("MANAGE...")
        self.btn_manage.setStyleSheet("background: #27272a; color: white; padding: 5px;")
        row1.addWidget(self.btn_manage)
        l.addLayout(row1)
        
        lbl2 = QLabel("TARGET FILE PATH:")
        lbl2.setStyleSheet("color: #eab308; font-weight: bold;")
        l.addWidget(lbl2)
        
        row2 = QHBoxLayout()
        self.ent_path = QLineEdit()
        self.ent_path.setStyleSheet("background: #151518; color: white; border: 1px solid #3f3f46; padding: 5px;")
        self.ent_path.setReadOnly(True)
        row2.addWidget(self.ent_path, 1)
        self.btn_browse = QPushButton("BROWSE...")
        self.btn_browse.setStyleSheet("background: #27272a; color: white; padding: 5px;")
        row2.addWidget(self.btn_browse)
        l.addLayout(row2)
        
        self.chk_capture = QCheckBox("CAPTURE SYSTEM OUTPUT (LIVE VIDEO)")
        self.chk_capture.setStyleSheet("color: white; font-weight: bold;")
        l.addWidget(self.chk_capture)
        
        self.chk_dev = QCheckBox("DEVELOPMENT BUILD (SCAN ENGINE LOGS)")
        self.chk_dev.setStyleSheet("color: white; font-weight: bold;")
        l.addWidget(self.chk_dev)
        
        self.chk_hook = QCheckBox("HOOK TO UNITY EDITOR (FALLBACK MATCH)")
        self.chk_hook.setStyleSheet("color: white; font-weight: bold;")
        l.addWidget(self.chk_hook)
        
        self.chk_sync = QCheckBox("AUTO-SYNC GAME WINDOW UI POSITIONS")
        self.chk_sync.setStyleSheet("color: white; font-weight: bold;")
        l.addWidget(self.chk_sync)
        
        row3 = QHBoxLayout()
        lbl3 = QLabel("TEST WINDOW RESOLUTION:")
        lbl3.setStyleSheet("color: white; font-weight: bold;")
        row3.addWidget(lbl3)
        self.cb_res = QComboBox()
        self.cb_res.addItems(["960x540", "1024x576", "1280x720", "1366x768", "1600x900", "Fullscreen"])
        self.cb_res.setStyleSheet("QComboBox { background: #151518; color: white; border: 1px solid #3f3f46; padding: 5px; }")
        row3.addWidget(self.cb_res)
        row3.addStretch()
        l.addLayout(row3)
        
        l.addStretch()
        
        # Populate initially
        self.refresh()
        
        # Wire up changes to save configuration dynamically
        self.chk_capture.stateChanged.connect(self._save_setup_settings)
        self.chk_dev.stateChanged.connect(self._save_setup_settings)
        self.chk_hook.stateChanged.connect(self._save_setup_settings)
        self.chk_sync.stateChanged.connect(self._save_setup_settings)
        self.cb_res.currentTextChanged.connect(self._save_setup_settings)
        self.cb_profile.currentIndexChanged.connect(self._on_profile_changed)
        self.btn_manage.clicked.connect(self._manage_games)
        self.btn_browse.clicked.connect(self._browse_game_exe)
        
        return w

    def _manage_games(self):
        from ui.dialogs.manage_games_dialog import ManageGamesDialog
        parent_window = self.window()
        dlg = ManageGamesDialog(self.app_controller, parent_window)
        if dlg.exec():
            self.refresh()
            main_win = self.window()
            if hasattr(main_win, 'page_library'):
                main_win.page_library._populate_games()

    def _browse_game_exe(self):
        from PySide6.QtWidgets import QFileDialog
        filepath, _ = QFileDialog.getOpenFileName(self, "Select Game Executable", "", "Executable Files (*.exe)")
        if filepath:
            self.app_controller.config.game_exe_path = filepath
            self.app_controller.config.save()
            self.refresh()
            self.app_controller.log_message("SYSTEM", "INFO", f"Target verified: {os.path.basename(filepath)}")

    def _on_profile_changed(self, index):
        if index < 0 or index >= len(self.app_controller.config.games):
            return
        game = self.app_controller.config.games[index]
        main_win = self.window()
        if hasattr(main_win, 'page_details'):
            main_win.page_details.load_game(game)
        else:
            self.app_controller.config.set_active_game(game['id'])
            self.refresh()

    def refresh(self):
        # Populate game profiles
        self.cb_profile.blockSignals(True)
        self.cb_profile.clear()
        for g in self.app_controller.config.games:
            self.cb_profile.addItem(g.get("title", g.get("id")))
        
        active = self.app_controller.config.get_active_game()
        if active:
            index = self.cb_profile.findText(active.get("title", ""))
            if index >= 0:
                self.cb_profile.setCurrentIndex(index)
        self.cb_profile.blockSignals(False)
                
        # Set target file path
        self.ent_path.setText(self.app_controller.config.game_exe_path)
        
        # Set check states without triggering signals if possible (or just set them)
        self.chk_capture.blockSignals(True)
        self.chk_dev.blockSignals(True)
        self.chk_hook.blockSignals(True)
        self.chk_sync.blockSignals(True)
        self.cb_res.blockSignals(True)
        
        self.chk_capture.setChecked(self.app_controller.config.record_test)
        self.chk_dev.setChecked(self.app_controller.config.dev_build_mode)
        self.chk_hook.setChecked(self.app_controller.config.hook_unity_editor)
        self.chk_sync.setChecked(self.app_controller.config.auto_sync_ui)
        
        res_str = f"{self.app_controller.config.game_width}x{self.app_controller.config.game_height}"
        res_idx = self.cb_res.findText(res_str)
        if res_idx >= 0:
            self.cb_res.setCurrentIndex(res_idx)
            
        self.chk_capture.blockSignals(False)
        self.chk_dev.blockSignals(False)
        self.chk_hook.blockSignals(False)
        self.chk_sync.blockSignals(False)
        self.cb_res.blockSignals(False)
        
        self.ui_mappings = None
        if hasattr(self, "mappings_tree"):
            self.refresh_mappings_tree()

    def _save_setup_settings(self):
        self.app_controller.config.record_test = self.chk_capture.isChecked()
        self.app_controller.config.dev_build_mode = self.chk_dev.isChecked()
        self.app_controller.config.hook_unity_editor = self.chk_hook.isChecked()
        self.app_controller.config.auto_sync_ui = self.chk_sync.isChecked()
        
        res_text = self.cb_res.currentText()
        if "x" in res_text:
            try:
                w, h = map(int, res_text.split("x"))
                self.app_controller.config.game_width = w
                self.app_controller.config.game_height = h
            except Exception:
                pass
        self.app_controller.config.save()

    def _create_live_tab(self):
        w = QWidget()
        l = QVBoxLayout(w)
        
        ctrl = QHBoxLayout()
        self.btn_clear_save = QPushButton("PURGE SAVE")
        self.btn_launch = QPushButton("BOOT GAME")
        self.btn_capture = QPushButton("COORDS TOOL")
        
        self.btn_clear_save.clicked.connect(self._purge_save)
        self.btn_launch.clicked.connect(self._boot_game)
        self.btn_capture.clicked.connect(self._launch_coords_tool)
        
        for b in (self.btn_clear_save, self.btn_launch, self.btn_capture):
            b.setStyleSheet("background: #27272a; color: white; padding: 8px; border-radius: 4px;")
            b.setCursor(Qt.PointingHandCursor)
            ctrl.addWidget(b)
            
        self.chk_live_preview = QCheckBox("ENABLE LIVE PREVIEW")
        self.chk_live_preview.setChecked(True)
        self.chk_live_preview.setStyleSheet("color: white; font-weight: bold;")
        ctrl.addWidget(self.chk_live_preview)
        
        l.addLayout(ctrl)
        
        self.preview_lbl = QLabel()
        self.preview_lbl.setStyleSheet("background: #050508; border: 1px solid #3f3f46;")
        l.addWidget(self.preview_lbl, 1)
        
        self.console = QTextEdit()
        self.console.setStyleSheet("background: #050508; color: white; font-family: Consolas; border: 1px solid #3f3f46;")
        self.console.setReadOnly(True)
        self.console.setFixedHeight(80)
        l.addWidget(self.console)
        
        return w

    def _purge_save(self):
        self.app_controller.clear_save_data()
        self.app_controller.log_message("SYSTEM", "INFO", "Purge Save command dispatched.")

    def _boot_game(self):
        self.app_controller.launch_game()
        
    def _launch_coords_tool(self):
        try:
            from ui.dialogs.capture_wizard_dialog import CaptureWizardDialog
            wizard = CaptureWizardDialog(self.app_controller, parent=self.window())
            if wizard.exec():
                self.refresh_mappings_tree()
                self.switch_tab(7) # Switch to MAPPINGS tab
        except Exception as e:
            from PySide6.QtWidgets import QMessageBox
            QMessageBox.critical(self, "Error", f"Failed to launch Capture Wizard: {e}")

    def _create_scenario_tab(self):
        w = QWidget()
        l = QVBoxLayout(w)
        lbl = QLabel("📋 ACTIVE SCENARIO STEPS & ACTIONS")
        lbl.setStyleSheet("color: #eab308; font-weight: bold;")
        l.addWidget(lbl)
        
        self.scenario_steps_tree = QTreeWidget()
        self.scenario_steps_tree.setHeaderLabels(["Step / Action Type", "Lua Action", "Target / Argument", "Resolved Coords", "UI Info (Text/Size)"])
        self.scenario_steps_tree.setStyleSheet("QTreeWidget { background: #131317; border: none; }")
        self.scenario_steps_tree.itemClicked.connect(self._on_scenario_tree_item_clicked)
        l.addWidget(self.scenario_steps_tree)
        return w

    def _on_scenario_tree_item_clicked(self, item, column):
        if "⚠️ MISSING MAPPING!" in item.text(3):
            target_name = item.text(2)
            from PySide6.QtWidgets import QMessageBox
            msg = QMessageBox.question(self, "Missing Mapping", f"Target '{target_name}' is missing both coordinate mapping and visual template.\n\nWould you like to map it now using the Capture Wizard?", QMessageBox.Yes | QMessageBox.No)
            if msg == QMessageBox.Yes:
                self._on_missing_template_prompt(target_name)

    def _create_ui_tab(self):
        w = QWidget()
        l = QVBoxLayout(w)
        
        self.chk_absolute_coords = QCheckBox("SHOW ABSOLUTE MONITOR SCREEN COORDINATES")
        self.chk_absolute_coords.setStyleSheet("color: white; font-weight: bold;")
        l.addWidget(self.chk_absolute_coords)
        
        lbl = QLabel("📍 LIVE UNITY UI ELEMENTS INSPECTOR")
        lbl.setStyleSheet("color: #eab308; font-weight: bold;")
        l.addWidget(lbl)
        
        self.live_buttons_tree = QTreeWidget()
        self.live_buttons_tree.setHeaderLabels(["UI Element Path/Name", "Type", "Text/Value", "X", "Y", "Width", "Height"])
        self.live_buttons_tree.setStyleSheet("QTreeWidget { background: #131317; border: none; }")
        l.addWidget(self.live_buttons_tree)
        return w

    def _create_logs_tab(self):
        w = QWidget()
        l = QVBoxLayout(w)
        lbl = QLabel("📄 DIAGNOSTIC LOGS READER")
        lbl.setStyleSheet("color: #eab308; font-weight: bold;")
        l.addWidget(lbl)
        
        self.full_logs_text = QTextEdit()
        self.full_logs_text.setStyleSheet("background: #050508; color: white; font-family: Consolas; border: none;")
        self.full_logs_text.setReadOnly(True)
        l.addWidget(self.full_logs_text)
        return w

    def get_system_specs(self):
        specs = {
            "os": f"{platform.system()} {platform.release()}",
            "cpu": "Unknown Processor",
            "ram": "Unknown Memory",
            "gpu": "Unknown GPU"
        }
        try:
            if platform.system() == "Windows":
                import winreg
                key = winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, r"HARDWARE\DESCRIPTION\System\CentralProcessor\0")
                cpu_name, _ = winreg.QueryValueEx(key, "ProcessorNameString")
                if cpu_name:
                    specs["cpu"] = cpu_name.strip()
            else:
                specs["cpu"] = platform.processor() or "Unknown CPU"
        except Exception:
            specs["cpu"] = platform.processor() or "Unknown CPU"

        try:
            if platform.system() == "Windows":
                out = subprocess.check_output("wmic computersystem get totalphysicalmemory", shell=True, stderr=subprocess.DEVNULL).decode()
                lines = [line.strip() for line in out.splitlines() if line.strip()]
                if len(lines) > 1 and lines[1].isdigit():
                    ram_bytes = int(lines[1])
                    specs["ram"] = f"{ram_bytes / (1024**3):.1f} GB"
        except Exception:
            pass

        try:
            if platform.system() == "Windows":
                out = subprocess.check_output("wmic path win32_VideoController get name", shell=True, stderr=subprocess.DEVNULL).decode()
                lines = [line.strip() for line in out.splitlines() if line.strip()]
                if len(lines) > 1:
                    specs["gpu"] = lines[1]
        except Exception:
            pass
        return specs

    def _create_specs_tab(self):
        w = QWidget()
        l = QVBoxLayout(w)
        l.setContentsMargins(15, 15, 15, 15)
        
        lbl = QLabel("// SYSTEM HARDWARE SPECS")
        lbl.setStyleSheet("color: #eab308; font-weight: bold;")
        l.addWidget(lbl)
        
        specs = self.get_system_specs()
        
        for k, v in [
            ("Operating System:", specs["os"]),
            ("Processor (CPU):", specs["cpu"]),
            ("System Memory (RAM):", specs["ram"]),
            ("Graphics Controller (GPU):", specs["gpu"])
        ]:
            row = QHBoxLayout()
            lbl_k = QLabel(k)
            lbl_k.setStyleSheet("color: white; font-weight: bold;")
            row.addWidget(lbl_k)
            row.addStretch()
            lbl_v = QLabel(v)
            lbl_v.setStyleSheet("color: #10b981; font-weight: bold;")
            row.addWidget(lbl_v)
            l.addLayout(row)
            
        l.addStretch()
        return w

    def _create_media_tab(self):
        w = QWidget()
        l = QVBoxLayout(w)
        l.setContentsMargins(15, 15, 15, 15)
        
        lbl = QLabel("// CAPTURE & RECORDINGS HUB")
        lbl.setStyleSheet("color: #eab308; font-weight: bold;")
        l.addWidget(lbl)
        
        desc = QLabel("Configure video screen captures, compression, and manual recording output.")
        desc.setStyleSheet("color: #9ca3af;")
        l.addWidget(desc)
        
        row1 = QHBoxLayout()
        lbl_codec = QLabel("Recording Codec Format:")
        lbl_codec.setStyleSheet("color: white; font-weight: bold;")
        row1.addWidget(lbl_codec)
        self.cb_codec = QComboBox()
        self.cb_codec.addItems(["Google VP8 (.IVF) - Recommended", "Raw uncompressed (.AVI)", "MPEG-4 (.MP4)"])
        self.cb_codec.setStyleSheet("QComboBox { background: #151518; color: white; border: 1px solid #3f3f46; padding: 5px; }")
        row1.addWidget(self.cb_codec)
        l.addLayout(row1)
        
        row2 = QHBoxLayout()
        lbl_fps = QLabel("Framerate Capture Preset:")
        lbl_fps.setStyleSheet("color: white; font-weight: bold;")
        row2.addWidget(lbl_fps)
        self.cb_fps = QComboBox()
        self.cb_fps.addItems(["30 Frames Per Second (Standard)", "60 Frames Per Second (High Refresh)", "15 Frames Per Second (Low CPU Overhead)"])
        self.cb_fps.setStyleSheet("QComboBox { background: #151518; color: white; border: 1px solid #3f3f46; padding: 5px; }")
        row2.addWidget(self.cb_fps)
        l.addLayout(row2)
        
        sep = QFrame()
        sep.setFrameShape(QFrame.HLine)
        sep.setStyleSheet("color: #3f3f46;")
        l.addWidget(sep)
        
        self.btn_trigger_record = QPushButton("🔴 TRIGGER MANUAL RECORDING")
        self.btn_trigger_record.setStyleSheet("background: #2c2c35; color: #10b981; padding: 10px; font-weight: bold; border: none;")
        self.btn_trigger_record.setCursor(Qt.PointingHandCursor)
        self.btn_trigger_record.clicked.connect(self._toggle_manual_recording)
        l.addWidget(self.btn_trigger_record)
        
        l.addStretch()
        return w

    def _create_mappings_tab(self):
        w = QWidget()
        l = QVBoxLayout(w)
        
        hdr = QHBoxLayout()
        lbl = QLabel("🗺️ JSON UI COORDINATES MAPPING")
        lbl.setStyleSheet("color: #eab308; font-weight: bold;")
        hdr.addWidget(lbl)
        
        lbl_search = QLabel("  🔍 Search:")
        lbl_search.setStyleSheet("color: #9ca3af;")
        hdr.addWidget(lbl_search)
        self.mappings_search_ent = QLineEdit()
        self.mappings_search_ent.setStyleSheet("background: #2c2c35; color: white; border: none; padding: 3px;")
        self.mappings_search_ent.textChanged.connect(self.refresh_mappings_tree)
        hdr.addWidget(self.mappings_search_ent)
        
        hdr.addStretch()
        self.btn_imp_mappings = QPushButton("IMPORT JSON")
        self.btn_imp_mappings.setStyleSheet("background: #27272a; color: white; padding: 5px; border-radius: 3px;")
        self.btn_imp_mappings.clicked.connect(self._load_mappings)
        
        self.btn_exp_mappings = QPushButton("EXPORT JSON")
        self.btn_exp_mappings.setStyleSheet("background: #27272a; color: white; padding: 5px; border-radius: 3px;")
        self.btn_exp_mappings.clicked.connect(self._save_mappings)
        
        hdr.addWidget(self.btn_imp_mappings)
        hdr.addWidget(self.btn_exp_mappings)
        l.addLayout(hdr)
        
        self.mappings_tree = QTreeWidget()
        self.mappings_tree.setHeaderLabels(["Object Path", "Component Type", "X Coord", "Y Coord"])
        self.mappings_tree.setStyleSheet("QTreeWidget { background: #131317; border: none; }")
        self.mappings_tree.itemDoubleClicked.connect(self._on_mapping_double_clicked)
        l.addWidget(self.mappings_tree)
        
        return w

    def _load_mappings(self):
        from PySide6.QtWidgets import QFileDialog
        active_game = self.app_controller.config.get_active_game()
        if not active_game:
            return
            
        filepath, _ = QFileDialog.getOpenFileName(self, "Import Coordinate JSON", "", "JSON Files (*.json)")
        if filepath:
            import json
            try:
                with open(filepath, "r", encoding="utf-8") as f:
                    data = json.load(f)
                    if isinstance(data, dict) and "entries" in data:
                        self.ui_mappings = data["entries"]
                        self.mappings_has_entries_wrapper = True
                    else:
                        self.ui_mappings = data
                        self.mappings_has_entries_wrapper = False
                
                active_game["ui_mapping_path"] = filepath
                self.app_controller.config.save()
                
                self.refresh_mappings_tree()
            except Exception as e:
                from PySide6.QtWidgets import QMessageBox
                QMessageBox.warning(self, "Import Mapping", f"Error loading mappings: {str(e)}")

    def _save_mappings(self):
        from PySide6.QtWidgets import QFileDialog
        filepath, _ = QFileDialog.getSaveFileName(self, "Export Coordinate JSON", "", "JSON Files (*.json)")
        if filepath:
            import json
            try:
                with open(filepath, "w", encoding="utf-8") as f:
                    if getattr(self, "mappings_has_entries_wrapper", True):
                        json.dump({"entries": self.ui_mappings}, f, indent=4)
                    else:
                        json.dump(self.ui_mappings, f, indent=4)
            except Exception as e:
                from PySide6.QtWidgets import QMessageBox
                QMessageBox.warning(self, "Export Mapping", f"Error saving mappings: {str(e)}")

    def refresh_mappings_tree(self):
        self.mappings_tree.clear()
        
        if not hasattr(self, 'ui_mappings') or self.ui_mappings is None:
            self.ui_mappings = []
            self.mappings_has_entries_wrapper = True
            
            active_game = self.app_controller.config.get_active_game()
            if active_game:
                path = active_game.get("ui_mapping_path", "")
                if path and os.path.exists(path):
                    import json
                    try:
                        with open(path, "r", encoding="utf-8") as f:
                            data = json.load(f)
                            if isinstance(data, dict) and "entries" in data:
                                self.ui_mappings = data["entries"]
                                self.mappings_has_entries_wrapper = True
                            else:
                                self.ui_mappings = data
                                self.mappings_has_entries_wrapper = False
                    except Exception:
                        pass
                        
        query = self.mappings_search_ent.text().lower().strip()
        
        for entry in self.ui_mappings:
            path = entry.get("Path", "")
            comp_type = entry.get("Type", "")
            x = str(entry.get("X", ""))
            y = str(entry.get("Y", ""))
            
            if query:
                if query not in path.lower() and query not in comp_type.lower():
                    continue
                    
            item = QTreeWidgetItem([path, comp_type, x, y])
            self.mappings_tree.addTopLevelItem(item)

    def read_game_state(self):
        return self.app_controller.read_game_state()

    def find_button_in_state(self, btn_name, buttons_dict):
        clean_name = btn_name.lower().replace("_", "").replace("-", "").replace(" ", "").replace(".png", "").strip()
        for k, v in buttons_dict.items():
            clean_k = k.lower().replace("_", "").replace("-", "").replace(" ", "").strip()
            if clean_k == clean_name or clean_name in clean_k or clean_k in clean_name:
                return v
        for k, v in buttons_dict.items():
            text_val = v.get("text", "")
            if text_val:
                clean_text = text_val.lower().replace("_", "").replace("-", "").replace(" ", "").strip()
                if clean_text == clean_name or clean_name in clean_text or clean_text in clean_name:
                    return v
        return None

    def try_resolve_variable_button(self, vx, vy, actions, buttons_dict):
        var_x = vx.split('.')[0] if '.' in vx else vx
        var_y = vy.split('.')[0] if '.' in vy else vy
        if var_x == var_y:
            for act in actions:
                if act["type"] == "wait_template":
                    line = act["line"]
                    if var_x in line:
                        return self.find_button_in_state(act["target"], buttons_dict)
        return None

    def parse_lua_actions(self, file_path):
        steps = []
        if not os.path.exists(file_path):
            return steps
            
        current_step = {"name": "Lobby / Pre-run", "actions": []}
        try:
            with open(file_path, "r", encoding="utf-8") as f:
                lines = f.readlines()
        except Exception:
            return steps
            
        for line in lines:
            line_strip = line.strip()
            if not line_strip or line_strip.startswith("--"):
                continue
                
            m_stage = re.search(r'set_stage\([\'"]([^\'"]+)[\'"]\)', line_strip)
            if m_stage:
                if current_step["actions"] or current_step["name"] != "Lobby / Pre-run":
                    steps.append(current_step)
                current_step = {"name": m_stage.group(1), "actions": []}
                continue
                
            m_wait_temp = re.search(r'wait_template\([\'"]([^\'"]+)[\'"]', line_strip)
            if m_wait_temp:
                current_step["actions"].append({
                    "type": "wait_template",
                    "target": m_wait_temp.group(1),
                    "line": line_strip
                })
                continue
                
            m_click = re.search(r'click\(([^,]+),\s*([^)]+)\)', line_strip)
            if m_click:
                current_step["actions"].append({
                    "type": "click",
                    "x": m_click.group(1).strip(),
                    "y": m_click.group(2).strip(),
                    "line": line_strip
                })
                continue
                
            m_drag = re.search(r'drag\(([^,]+),\s*([^,]+),\s*([^,]+),\s*([^,)]+)', line_strip)
            if m_drag:
                current_step["actions"].append({
                    "type": "drag",
                    "x1": m_drag.group(1).strip(),
                    "y1": m_drag.group(2).strip(),
                    "x2": m_drag.group(3).strip(),
                    "y2": m_drag.group(4).strip(),
                    "line": line_strip
                })
                continue
                
            m_wait = re.search(r'\bwait\((\d+(\.\d+)?)\)', line_strip)
            if m_wait:
                current_step["actions"].append({
                    "type": "wait",
                    "seconds": m_wait.group(1),
                    "line": line_strip
                })
                continue
                
            if "launch_game" in line_strip:
                current_step["actions"].append({
                    "type": "launch_game",
                    "line": line_strip
                })
                continue
                
            if "clear_save_data" in line_strip:
                current_step["actions"].append({
                    "type": "clear_save_data",
                    "line": line_strip
                })
                continue
                
        if current_step["actions"] or current_step["name"] != "Lobby / Pre-run":
            steps.append(current_step)
        return steps

    def refresh_locations_view(self):
        game_state = self.read_game_state()
        elements_dict = {}
        if game_state:
            elements_dict = game_state.get("elements", game_state.get("buttons", {}))
            
        rect = self.app_controller.game_hooks.game_rect if (hasattr(self, 'chk_absolute_coords') and self.chk_absolute_coords.isChecked()) else None
        
        # 1. Update Live Buttons tree
        if hasattr(self, 'live_buttons_tree'):
            self.live_buttons_tree.clear()
            for name, coords in sorted(elements_dict.items()):
                text_val = coords.get("text", "")
                if not text_val:
                    text_val = coords.get("value", "")
                elem_type = coords.get("type", "Button")
                if rect:
                    cx, cy, gw_w, gw_h = rect
                    x_val = f"{cx + int(coords.get('x', 0.0) * gw_w / 1280):.1f}"
                    y_val = f"{cy + int(coords.get('y', 0.0) * gw_h / 720):.1f}"
                    w_val = f"{int(coords.get('w', 0.0) * gw_w / 1280):.1f}"
                    h_val = f"{int(coords.get('h', 0.0) * gw_h / 720):.1f}"
                else:
                    x_val = f"{coords.get('x', 0.0):.1f}"
                    y_val = f"{coords.get('y', 0.0):.1f}"
                    w_val = f"{coords.get('w', 0.0):.1f}"
                    h_val = f"{coords.get('h', 0.0):.1f}"
                item = QTreeWidgetItem([name, elem_type, text_val, x_val, y_val, w_val, h_val])
                self.live_buttons_tree.addTopLevelItem(item)
                
        # 2. Update Scenario Steps tree
        main_win = self.window()
        selected_scenario = None
        scenarios_dir = ""
        if hasattr(main_win, 'page_details') and hasattr(main_win.page_details, 'scenarios_sidebar'):
            sidebar = main_win.page_details.scenarios_sidebar
            scenarios_dir = sidebar.scenarios_dir
            if os.path.exists(scenarios_dir):
                files = sorted([f for f in os.listdir(scenarios_dir) if f.endswith('.lua')])
                if sidebar.selected_scenario_index < len(files):
                    selected_scenario = os.path.splitext(files[sidebar.selected_scenario_index])[0]
                    
        if not selected_scenario or not hasattr(self, 'scenario_steps_tree'):
            if hasattr(self, 'scenario_steps_tree'):
                self.scenario_steps_tree.clear()
            return
            
        script_path = os.path.join(scenarios_dir, f"{selected_scenario}.lua")
        parsed_steps = self.parse_lua_actions(script_path)
        
        self.scenario_steps_tree.clear()
        
        known_targets = set()
        active_game = self.app_controller.config.get_active_game()
        ui_path = active_game.get("ui_mapping_path", "") if active_game else ""
        from core.paths import get_base_dir
        for p in [ui_path, os.path.join(get_base_dir(), "assets", "UIConfig_Custom.json")]:
            if p and os.path.exists(p):
                try:
                    with open(p, "r", encoding="utf-8") as f:
                        data = json.load(f)
                        entries = data.get("entries", data) if isinstance(data, dict) else data
                        for entry in entries:
                            pstr = entry.get("Path", "")
                            known_targets.add(pstr)
                            if "/" in pstr: known_targets.add(pstr.split("/")[-1])
                except Exception: pass
        t_dir = os.path.join(get_base_dir(), "assets", "templates", active_game["id"] if active_game else "")
        if os.path.exists(t_dir):
            for f in os.listdir(t_dir):
                if f.endswith('.png'): known_targets.add(f[:-4])
        
        for step in parsed_steps:
            step_name = step["name"]
            
            step_display = step_name
            is_skipped = self.app_controller.skipped_steps.get((selected_scenario, step_name), False)
            if is_skipped:
                step_display += " (SKIPPED)"
                
            step_item = QTreeWidgetItem([step_display, "set_stage", step_name, "", ""])
            self.scenario_steps_tree.addTopLevelItem(step_item)
            
            for action in step["actions"]:
                action_type = action["type"]
                action_target = ""
                action_coords = ""
                action_text_size = ""
                
                if action_type == "wait_template":
                    action_target = action["target"]
                    btn_pos = self.find_button_in_state(action_target, elements_dict)
                    if btn_pos:
                        if rect:
                            cx, cy, gw_w, gw_h = rect
                            abs_x = cx + int(btn_pos['x'] * gw_w / 1280)
                            abs_y = cy + int(btn_pos['y'] * gw_h / 720)
                            action_coords = f"({abs_x:.1f}, {abs_y:.1f}) [Screen Live]"
                        else:
                            action_coords = f"({btn_pos['x']:.1f}, {btn_pos['y']:.1f}) [Unity Live]"
                        action_text_size = f"'{btn_pos.get('text', '')}' | {btn_pos.get('w', 0.0):.1f}x{btn_pos.get('h', 0.0):.1f}"
                    else:
                        if action_target in known_targets:
                            action_coords = "Pending / CV Match"
                        else:
                            action_coords = "⚠️ MISSING MAPPING! (Click to fix)"
                elif action_type == "click":
                    cx_val = action["x"]
                    cy_val = action["y"]
                    action_target = f"({cx_val}, {cy_val})"
                    btn_pos = self.try_resolve_variable_button(cx_val, cy_val, step["actions"], elements_dict)
                    if btn_pos:
                        if rect:
                            cx, cy, gw_w, gw_h = rect
                            abs_x = cx + int(btn_pos['x'] * gw_w / 1280)
                            abs_y = cy + int(btn_pos['y'] * gw_h / 720)
                            action_coords = f"({abs_x:.1f}, {abs_y:.1f}) [Resolved Screen]"
                        else:
                            action_coords = f"({btn_pos['x']:.1f}, {btn_pos['y']:.1f}) [Resolved]"
                        action_text_size = f"'{btn_pos.get('text', '')}' | {btn_pos.get('w', 0.0):.1f}x{btn_pos.get('h', 0.0):.1f}"
                    else:
                        try:
                            rx = float(cx_val)
                            ry = float(cy_val)
                            if rect:
                                cx, cy, gw_w, gw_h = rect
                                abs_x = cx + int(rx * gw_w / 1280)
                                abs_y = cy + int(ry * gw_h / 720)
                                action_coords = f"({abs_x:.1f}, {abs_y:.1f}) [Screen]"
                            else:
                                action_coords = f"({rx:.1f}, {ry:.1f})"
                        except ValueError:
                            action_coords = "Unknown Variable"
                elif action_type == "drag":
                    cx1, cy1, cx2, cy2 = action["x1"], action["y1"], action["x2"], action["y2"]
                    action_target = f"({cx1},{cy1}) to ({cx2},{cy2})"
                    btn1 = self.try_resolve_variable_button(cx1, cy1, step["actions"], elements_dict)
                    btn2 = self.try_resolve_variable_button(cx2, cy2, step["actions"], elements_dict)
                    r1 = (btn1["x"], btn1["y"]) if btn1 else None
                    r2 = (btn2["x"], btn2["y"]) if btn2 else None
                    if r1 or r2:
                        if rect:
                            cx, cy, gw_w, gw_h = rect
                            c1_str = f"({cx + int(r1[0] * gw_w / 1280):.1f}, {cy + int(r1[1] * gw_h / 720):.1f})" if r1 else f"({cx1}, {cy1})"
                            c2_str = f"({cx + int(r2[0] * gw_w / 1280):.1f}, {cy + int(r2[1] * gw_h / 720):.1f})" if r2 else f"({cx2}, {cy2})"
                            action_coords = f"{c1_str} -> {c2_str} [Resolved Screen]"
                        else:
                            c1_str = f"({r1[0]:.1f}, {r1[1]:.1f})" if r1 else f"({cx1}, {cy1})"
                            c2_str = f"({r2[0]:.1f}, {r2[1]:.1f})" if r2 else f"({cx2}, {cy2})"
                            action_coords = f"{c1_str} -> {c2_str} [Resolved]"
                    else:
                        try:
                            rx1, ry1 = float(cx1), float(cy1)
                            rx2, ry2 = float(cx2), float(cy2)
                            if rect:
                                cx, cy, gw_w, gw_h = rect
                                c1_str = f"({cx + int(rx1 * gw_w / 1280):.1f}, {cy + int(ry1 * gw_h / 720):.1f})"
                                c2_str = f"({cx + int(rx2 * gw_w / 1280):.1f}, {cy + int(ry2 * gw_h / 720):.1f})"
                                action_coords = f"{c1_str} -> {c2_str} [Screen]"
                            else:
                                action_coords = f"({rx1:.1f},{ry1:.1f}) -> ({rx2:.1f},{ry2:.1f})"
                        except ValueError:
                            action_coords = f"({cx1},{cy1}) -> ({cx2},{cy2})"
                    parts = []
                    if btn1: parts.append(f"Start: '{btn1.get('text', '')}'")
                    if btn2: parts.append(f"End: '{btn2.get('text', '')}'")
                    action_text_size = " | ".join(parts)
                elif action_type == "wait":
                    action_target = f"{action['seconds']} seconds"
                    action_coords = "Wait"
                elif action_type == "launch_game":
                    action_target = "Start Game Process"
                    action_coords = "System Action"
                elif action_type == "clear_save_data":
                    action_target = "Purge Cache"
                    action_coords = "System Action"
                    
                action_item = QTreeWidgetItem(["• " + action_type.upper(), action_type, action_target, action_coords, action_text_size])
                if "⚠️" in action_coords:
                    from PySide6.QtGui import QColor
                    action_item.setForeground(3, QColor("#ef4444"))
                    action_item.setForeground(2, QColor("#ef4444"))
                step_item.addChild(action_item)
            
            step_item.setExpanded(True)

    def _on_timer_tick(self):
        active_idx = self.stack.currentIndex()
        if active_idx in (2, 3):
            self.refresh_locations_view()

    def _purge_save(self):
        from PySide6.QtWidgets import QMessageBox
        reply = QMessageBox.question(self, "Purge Save", "Are you sure you want to delete local save files?",
                                     QMessageBox.Yes | QMessageBox.No, QMessageBox.No)
        if reply == QMessageBox.Yes:
            self.app_controller.clear_save_data()
            QMessageBox.information(self, "Purge Save", "Local game save data purged successfully!")

    def _boot_game(self):
        if not self.app_controller.config.game_exe_path or not os.path.exists(self.app_controller.config.game_exe_path):
            from PySide6.QtWidgets import QMessageBox
            QMessageBox.warning(self, "Error", "Please select a valid game executable first!")
            return
        import threading
        threading.Thread(target=self.app_controller.launch_game, daemon=True).start()

    def _launch_coords_tool(self):
        from PySide6.QtWidgets import QInputDialog, QMessageBox
        name, ok = QInputDialog.getText(self, "Name Pattern Target", "Enter Target Pattern Identifier:")
        if ok and name.strip():
            # Check if game window is running/visible
            rect = self.app_controller.game_hooks.game_rect
            if not rect or rect[2] <= 0 or rect[3] <= 0:
                QMessageBox.warning(self, "Capture", "Game window must be running and visible to capture templates!")
                return
            
            # Start the capture wizard dialog
            from ui.dialogs.capture_wizard import CaptureWizard
            wizard = CaptureWizard(name.strip(), self.app_controller, self)
            wizard.exec()

    def _on_mapping_double_clicked(self, item, column):
        path = item.text(0)
        comp_type = item.text(1)
        x_str = item.text(2)
        y_str = item.text(3)
        
        try:
            x_val = float(x_str) if x_str else 0.0
            y_val = float(y_str) if y_str else 0.0
        except ValueError:
            x_val = 0.0
            y_val = 0.0
            
        initial_coords = {
            "x": x_val,
            "y": y_val,
            "width": 100,
            "height": 50,
            "resolution": f"{self.app_controller.config.game_width}x{self.app_controller.config.game_height}" if self.app_controller.config.game_width > 0 and self.app_controller.config.game_height > 0 else "1280x720"
        }
        
        # Find matching entry in self.ui_mappings to update it
        target_entry = None
        for entry in self.ui_mappings:
            if entry.get("Path") == path:
                target_entry = entry
                break
                
        if not target_entry:
            # Create a new entry if not found
            target_entry = {"Path": path, "Type": comp_type, "X": x_val, "Y": y_val}
            self.ui_mappings.append(target_entry)
            
        def on_save(new_x, new_y, new_w, new_h, resolution):
            target_entry["X"] = float(new_x)
            target_entry["Y"] = float(new_y)
            self.refresh_mappings_tree()
            
            # Save mappings file
            active_game = self.app_controller.config.get_active_game()
            if active_game and active_game.get("ui_mapping_path"):
                mapping_path = active_game["ui_mapping_path"]
                try:
                    os.makedirs(os.path.dirname(mapping_path), exist_ok=True)
                    with open(mapping_path, "w", encoding="utf-8") as f:
                        if getattr(self, "mappings_has_entries_wrapper", True):
                            json.dump({"entries": self.ui_mappings}, f, indent=4)
                        else:
                            json.dump(self.ui_mappings, f, indent=4)
                    self.app_controller.log_message("MAPPINGS", "INFO", f"Saved coordinates for '{path}' to mappings file.")
                except Exception as e:
                    print(f"Failed to auto-save mappings: {e}")
                    
        from ui.dialogs.coordinate_dialog import CoordinateDialog
        dlg = CoordinateDialog(path, initial_coords, on_save, self)
        dlg.exec()

    def _toggle_manual_recording(self):
        codec = self.cb_codec.currentText()
        fps = self.cb_fps.currentText()
        recording = self.app_controller.toggle_manual_recording(codec, fps)
        if recording:
            self.btn_trigger_record.setText("⏹️ STOP MANUAL RECORDING")
            self.btn_trigger_record.setStyleSheet("background: #ef4444; color: white; padding: 10px; font-weight: bold; border: none;")
        else:
            self.btn_trigger_record.setText("🔴 TRIGGER MANUAL RECORDING")
            self.btn_trigger_record.setStyleSheet("background: #2c2c35; color: #10b981; padding: 10px; font-weight: bold; border: none;")

    def _toggle_overlay(self):
        main_win = self.window()
        if hasattr(main_win, 'toggle_overlay_mode'):
            main_win.toggle_overlay_mode()

    def _on_test_finished(self, status):
        # Reset manual recording button if test aborts or stops
        self.btn_trigger_record.setText("🔴 TRIGGER MANUAL RECORDING")
        self.btn_trigger_record.setStyleSheet("background: #2c2c35; color: #10b981; padding: 10px; font-weight: bold; border: none;")

