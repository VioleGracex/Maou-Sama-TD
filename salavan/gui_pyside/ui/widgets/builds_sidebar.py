import os
import time
import glob
import xml.etree.ElementTree as ET
from PySide6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QLabel, QPushButton, 
    QScrollArea, QFrame, QTreeWidget, QTreeWidgetItem, QSplitter, QMessageBox
)
from PySide6.QtCore import Qt

class BuildsSidebar(QWidget):
    def __init__(self, app_controller, parent=None):
        super().__init__(parent)
        self.app_controller = app_controller
        self._init_ui()
        self.refresh_builds()
        self.refresh_reports()

    def _init_ui(self):
        main_layout = QVBoxLayout(self)
        main_layout.setContentsMargins(10, 10, 10, 10)
        main_layout.setSpacing(10)
        self.setStyleSheet("background-color: #18181c;")
        
        splitter = QSplitter(Qt.Vertical)
        
        # Pane 1: Game Builds Database
        builds_pane = QWidget()
        builds_layout = QVBoxLayout(builds_pane)
        builds_layout.setContentsMargins(0, 0, 0, 0)
        
        b_hdr = QHBoxLayout()
        lbl1 = QLabel("// GAME BUILDS DATABASE")
        lbl1.setStyleSheet("color: #eab308; font-weight: bold; font-size: 13px;")
        self.btn_close = QPushButton("✕")
        self.btn_close.setStyleSheet("background: #2c2c35; color: white; border: none; padding: 5px;")
        self.btn_close.clicked.connect(lambda: self.setVisible(False))
        
        b_hdr.addWidget(lbl1)
        b_hdr.addStretch()
        b_hdr.addWidget(self.btn_close)
        builds_layout.addLayout(b_hdr)
        
        self.builds_tree = QTreeWidget()
        self.builds_tree.setHeaderLabels(["GAME TITLE", "VER", "STATUS", "LAST RUN"])
        self.builds_tree.setStyleSheet("QTreeWidget { background: #131317; border: none; }")
        builds_layout.addWidget(self.builds_tree)
        
        db_btns = QHBoxLayout()
        self.btn_add = QPushButton("ADD BUILD")
        self.btn_add.setStyleSheet("background: #27272a; color: white; padding: 5px;")
        self.btn_add.clicked.connect(self._add_build)
        
        self.btn_del = QPushButton("DELETE")
        self.btn_del.setStyleSheet("background: #27272a; color: white; padding: 5px;")
        self.btn_del.clicked.connect(self._delete_build)
        
        db_btns.addWidget(self.btn_add)
        db_btns.addWidget(self.btn_del)
        builds_layout.addLayout(db_btns)
        
        self.btn_set = QPushButton("SET ACTIVE")
        self.btn_set.setStyleSheet("background: #27272a; color: white; padding: 5px;")
        self.btn_set.clicked.connect(self._set_active_build)
        builds_layout.addWidget(self.btn_set)
        
        splitter.addWidget(builds_pane)
        
        # Pane 2: Historical Reports
        reps_pane = QWidget()
        reps_layout = QVBoxLayout(reps_pane)
        reps_layout.setContentsMargins(0, 0, 0, 0)
        
        r_hdr = QHBoxLayout()
        lbl2 = QLabel("// HISTORICAL REPORTS")
        lbl2.setStyleSheet("color: #eab308; font-weight: bold; font-size: 13px;")
        self.btn_scan = QPushButton("SCAN")
        self.btn_scan.setStyleSheet("background: #27272a; color: white; padding: 5px;")
        self.btn_scan.clicked.connect(self.refresh_reports)
        
        r_hdr.addWidget(lbl2)
        r_hdr.addStretch()
        r_hdr.addWidget(self.btn_scan)
        reps_layout.addLayout(r_hdr)
        
        self.reports_tree = QTreeWidget()
        self.reports_tree.setHeaderLabels(["RUN DATE", "RUN TIME", "VERDICT", "ELAPSED"])
        self.reports_tree.setStyleSheet("QTreeWidget { background: #131317; border: none; }")
        reps_layout.addWidget(self.reports_tree)
        
        splitter.addWidget(reps_pane)
        main_layout.addWidget(splitter)

    def refresh_builds(self):
        self.builds_tree.clear()
        active_game = self.app_controller.config.get_active_game()
        if not active_game:
            return
            
        active_game_id = active_game.get("id")
        for b in self.app_controller.config.builds:
            if b.get("game_id", "maou_sama_td") == active_game_id:
                item = QTreeWidgetItem([
                    b.get("title", ""),
                    b.get("version", ""),
                    b.get("status", "Pending"),
                    b.get("last_tested", "-")
                ])
                status = b.get("status", "Pending")
                if status in ("PASS", "SUCCESS"):
                    item.setForeground(2, Qt.green)
                elif status in ("FAIL", "FAILED"):
                    item.setForeground(2, Qt.red)
                self.builds_tree.addTopLevelItem(item)

    def refresh_reports(self):
        self.reports_tree.clear()
        
        active_game = self.app_controller.config.get_active_game()
        if not active_game:
            return
            
        from core.paths import get_base_dir
        junit_dir = os.path.join(get_base_dir(), "reports", active_game["id"], "junit")
        if os.path.exists(junit_dir):
            xml_files = glob.glob(os.path.join(junit_dir, "*.xml"))
            for f in xml_files:
                try:
                    tree = ET.parse(f)
                    root = tree.getroot()
                    failures = int(root.get("failures", 0))
                    errors = int(root.get("errors", 0))
                    elapsed = root.get("time", "0")
                    
                    mtime = os.path.getmtime(f)
                    date_str = time.strftime("%Y-%m-%d", time.localtime(mtime))
                    time_str = time.strftime("%H:%M:%S", time.localtime(mtime))
                    
                    verdict = "PASS"
                    if failures > 0 or errors > 0:
                        verdict = "FAIL"
                        
                    item = QTreeWidgetItem([date_str, time_str, verdict, f"{elapsed}s"])
                    if verdict == "PASS":
                        item.setForeground(2, Qt.green)
                    else:
                        item.setForeground(2, Qt.red)
                    self.reports_tree.addTopLevelItem(item)
                except Exception:
                    pass

    def _add_build(self):
        from ui.dialogs.add_build_dialog import AddBuildDialog
        parent_window = self.window()
        dlg = AddBuildDialog(self.app_controller, parent_window)
        if dlg.exec():
            self.refresh_builds()
            main_win = self.window()
            if hasattr(main_win, 'page_details') and hasattr(main_win.page_details, 'center_dashboard'):
                main_win.page_details.center_dashboard.refresh()

    def _delete_build(self):
        selected = self.builds_tree.selectedItems()
        if not selected:
            QMessageBox.warning(self, "Delete Build", "Please select a build to delete!")
            return
            
        item = selected[0]
        title = item.text(0)
        version = item.text(1)
        
        active_game = self.app_controller.config.get_active_game()
        if not active_game:
            return
            
        self.app_controller.config.builds = [
            b for b in self.app_controller.config.builds 
            if not (b.get("game_id", "maou_sama_td") == active_game["id"] and b.get("title") == title and b.get("version") == version)
        ]
        self.app_controller.config.save()
        self.refresh_builds()
        self.app_controller.log_message("SYSTEM", "INFO", f"Removed build: {title} ({version})")

    def _set_active_build(self):
        selected = self.builds_tree.selectedItems()
        if not selected:
            QMessageBox.warning(self, "Select Build", "Please select a build first!")
            return
            
        item = selected[0]
        title = item.text(0)
        version = item.text(1)
        
        active_game = self.app_controller.config.get_active_game()
        if not active_game:
            return
            
        for b in self.app_controller.config.builds:
            if b.get("game_id", "maou_sama_td") == active_game["id"] and b.get("title") == title and b.get("version") == version:
                self.app_controller.config.game_exe_path = b.get("path")
                self.app_controller.config.save()
                self.app_controller.log_message("SYSTEM", "INFO", f"Active build set to: {title} ({version})")
                
                main_win = self.window()
                if hasattr(main_win, 'page_details') and hasattr(main_win.page_details, 'center_dashboard'):
                    main_win.page_details.center_dashboard.refresh()
                return
