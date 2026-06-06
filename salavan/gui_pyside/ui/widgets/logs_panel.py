from PySide6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QLabel, QPushButton, 
    QTreeWidget, QTreeWidgetItem
)
from PySide6.QtCore import Qt

class LogsPanel(QWidget):
    def __init__(self, app_controller, parent=None):
        super().__init__(parent)
        self.app_controller = app_controller
        self._init_ui()

    def _init_ui(self):
        main_layout = QVBoxLayout(self)
        main_layout.setContentsMargins(10, 10, 10, 10)
        main_layout.setSpacing(10)
        self.setStyleSheet("background-color: #18181c;")
        
        hdr = QHBoxLayout()
        lbl = QLabel("// HUD DIAGNOSTIC LOGS")
        lbl.setStyleSheet("color: #eab308; font-weight: bold; font-size: 13px;")
        hdr.addWidget(lbl)
        hdr.addStretch()
        
        btn_style = "QPushButton { background: #2c2c35; color: white; padding: 5px 8px; border: none; } QPushButton:hover { background: #3e3e4a; }"
        
        self.btn_clear = QPushButton("[ 🗑 CLEAR LOGS ]")
        self.btn_clear.setStyleSheet(btn_style)
        self.btn_clear.clicked.connect(lambda: self.tree.clear())
        
        self.btn_copy_all = QPushButton("[ 📋 COPY ALL ]")
        self.btn_copy_all.setStyleSheet(btn_style)
        self.btn_copy_all.clicked.connect(self.copy_all_logs)

        self.btn_open_file = QPushButton("[ 📂 OPEN LOG FILE ]")
        self.btn_open_file.setStyleSheet(btn_style)
        self.btn_open_file.clicked.connect(self.open_log_file)
        
        btn_collapse = QPushButton("[ ⬇ COLLAPSE ]")
        btn_collapse.setStyleSheet(btn_style)
        btn_collapse.clicked.connect(self.toggle_collapse)
        
        self.btn_pop = QPushButton("[ ⧉ POP OUT ]")
        self.btn_pop.setStyleSheet(btn_style)
        
        hdr.addWidget(self.btn_clear)
        hdr.addWidget(self.btn_copy_all)
        hdr.addWidget(self.btn_open_file)
        hdr.addWidget(btn_collapse)
        hdr.addWidget(self.btn_pop)
        main_layout.addLayout(hdr)
        
        self.tree = QTreeWidget()
        self.tree.setHeaderLabels(["HUD STEP", "RESULT", "HUD DIAGNOSTIC MESSAGE"])
        self.tree.setStyleSheet("QTreeWidget { background: #131317; border: none; }")
        self.tree.setColumnWidth(0, 150)
        self.tree.setColumnWidth(1, 100)
        
        # Enable multiple selection and context menu
        self.tree.setSelectionMode(QTreeWidget.ExtendedSelection)
        self.tree.setContextMenuPolicy(Qt.CustomContextMenu)
        self.tree.customContextMenuRequested.connect(self.show_context_menu)
        
        main_layout.addWidget(self.tree)
        
        self.app_controller.log_added.connect(self.add_log)

    def keyPressEvent(self, event):
        from PySide6.QtGui import QKeySequence
        if event.matches(QKeySequence.Copy):
            self.copy_selected_logs()
            event.accept()
        else:
            super().keyPressEvent(event)

    def copy_selected_logs(self):
        from PySide6.QtGui import QGuiApplication
        selected = self.tree.selectedItems()
        if not selected:
            return
        lines = []
        for item in selected:
            lines.append(f"[{item.text(0)}] [{item.text(1)}] {item.text(2)}")
        clipboard = QGuiApplication.clipboard()
        clipboard.setText("\n".join(lines))

    def copy_all_logs(self):
        from PySide6.QtGui import QGuiApplication
        lines = []
        for i in range(self.tree.topLevelItemCount()):
            item = self.tree.topLevelItem(i)
            lines.append(f"[{item.text(0)}] [{item.text(1)}] {item.text(2)}")
        clipboard = QGuiApplication.clipboard()
        clipboard.setText("\n".join(lines))

    def open_log_file(self):
        import os, platform, subprocess
        from core.paths import get_base_dir
        
        log_file = getattr(self.app_controller.logger, "report_log_file", None)
        target = None
        if log_file and os.path.exists(log_file):
            target = log_file
        else:
            active_game = self.app_controller.config.get_active_game()
            if active_game:
                logs_dir = os.path.join(get_base_dir(), "reports", active_game["id"])
                if os.path.exists(logs_dir):
                    target = logs_dir
                    
        if target:
            try:
                if platform.system() == "Windows":
                    os.startfile(target)
                else:
                    subprocess.Popen(['xdg-open', target])
            except Exception:
                pass

    def show_context_menu(self, pos):
        item = self.tree.itemAt(pos)
        if not item:
            return
        
        # Highlight/select the right-clicked item if not already part of a selection
        if not item.isSelected():
            self.tree.clearSelection()
            item.setSelected(True)
            
        from PySide6.QtWidgets import QMenu
        from PySide6.QtGui import QAction
        
        menu = QMenu(self)
        copy_action = QAction("Copy Selected Log(s)", self)
        copy_action.triggered.connect(self.copy_selected_logs)
        menu.addAction(copy_action)
        menu.exec(self.tree.mapToGlobal(pos))

    def add_log(self, step, status, msg):
        item = QTreeWidgetItem([step, status, msg])
        # Color coding status
        if status in ("PASS", "SUCCESS"):
            item.setForeground(1, Qt.green)
        elif status in ("FAIL", "FAILED"):
            item.setForeground(1, Qt.red)
        elif status in ("STARTING", "INFO"):
            item.setForeground(1, Qt.cyan)
        self.tree.addTopLevelItem(item)
        self.tree.scrollToItem(item)

    def toggle_collapse(self):
        self.tree.setVisible(not self.tree.isVisible())
