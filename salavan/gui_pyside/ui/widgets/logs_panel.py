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
        
        self.btn_clear = QPushButton("[ 🗑 CLEAR LOGS ]")
        self.btn_clear.setStyleSheet("background: #2c2c35; color: white; padding: 5px; border: none;")
        self.btn_clear.clicked.connect(lambda: self.tree.clear())
        btn_collapse = QPushButton("[ ⬇ COLLAPSE ]")
        btn_collapse.setStyleSheet("background: #2c2c35; color: white; padding: 5px; border: none;")
        btn_collapse.clicked.connect(self.toggle_collapse)
        self.btn_pop = QPushButton("[ ⧉ POP OUT ]")
        self.btn_pop.setStyleSheet("background: #2c2c35; color: white; padding: 5px; border: none;")
        
        hdr.addWidget(self.btn_clear)
        hdr.addWidget(btn_collapse)
        hdr.addWidget(self.btn_pop)
        main_layout.addLayout(hdr)
        
        self.tree = QTreeWidget()
        self.tree.setHeaderLabels(["HUD STEP", "RESULT", "HUD DIAGNOSTIC MESSAGE"])
        self.tree.setStyleSheet("QTreeWidget { background: #131317; border: none; }")
        self.tree.setColumnWidth(0, 150)
        self.tree.setColumnWidth(1, 100)
        main_layout.addWidget(self.tree)
        
        self.app_controller.log_added.connect(self.add_log)

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
