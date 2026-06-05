import os
import re
from PySide6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QLabel, QPushButton, 
    QScrollArea, QFrame, QMenu
)
from PySide6.QtCore import Qt, Signal, QTimer
from PySide6.QtGui import QCursor, QColor

class HoverFrame(QFrame):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.default_style = "background: transparent; border-radius: 3px;"
        self.hover_style = "background: #202024; border-radius: 3px;"
        self.setStyleSheet(self.default_style)
        
    def enterEvent(self, event):
        self.setStyleSheet(self.hover_style)
        super().enterEvent(event)
        
    def leaveEvent(self, event):
        self.setStyleSheet(self.default_style)
        super().leaveEvent(event)

class ScenariosSidebar(QWidget):
    scenario_selected = Signal(str)

    def __init__(self, app_controller, parent=None):
        super().__init__(parent)
        self.app_controller = app_controller
        self.active_game = self.app_controller.config.get_active_game()
        from core.paths import get_base_dir
        self.scenarios_dir = os.path.join(get_base_dir(), "scenarios", self.active_game['id']) if self.active_game else ""
        
        self.selected_scenario_index = 0
        self.skipped_steps = self.app_controller.skipped_steps
        self.step_status_overrides = self.app_controller.step_status_overrides
        
        self._init_ui()
        self.populate_scenarios()
        
        self.app_controller.stage_changed.connect(lambda stage: self.populate_scenarios())

    def _init_ui(self):
        main_layout = QVBoxLayout(self)
        main_layout.setContentsMargins(10, 10, 10, 10)
        main_layout.setSpacing(10)
        self.setStyleSheet("background-color: #18181c;")
        
        # Header
        header = QHBoxLayout()
        self.title_lbl = QLabel("📂 SCENARIOS DATABASE")
        self.title_lbl.setStyleSheet("color: #eab308; font-weight: bold; font-size: 13px;")
        header.addWidget(self.title_lbl)
        header.addStretch()
        
        btn_refresh = QPushButton("🔄")
        btn_refresh.setStyleSheet("background: transparent; color: #38bdf8; border: none; font-size: 14px;")
        btn_refresh.setCursor(Qt.PointingHandCursor)
        btn_refresh.clicked.connect(self.populate_scenarios)
        
        btn_import = QPushButton("➕")
        btn_import.setStyleSheet("background: transparent; color: #10b981; border: none; font-size: 14px;")
        btn_import.setCursor(Qt.PointingHandCursor)
        
        header.addWidget(btn_import)
        header.addWidget(btn_refresh)
        main_layout.addLayout(header)

        # Scroll Area
        self.scroll_area = QScrollArea()
        self.scroll_area.setWidgetResizable(True)
        self.scroll_area.setStyleSheet("""
            QScrollArea { border: none; background: transparent; } 
            QScrollBar:vertical { background: #18181c; width: 10px; margin: 0px; }
            QScrollBar::handle:vertical { background: #3f3f46; min-height: 20px; border-radius: 5px; }
            QScrollBar::handle:vertical:hover { background: #52525b; }
            QScrollBar::add-line:vertical, QScrollBar::sub-line:vertical { height: 0px; }
        """)
        
        self.container = QWidget()
        self.container_layout = QVBoxLayout(self.container)
        self.container_layout.setContentsMargins(0, 0, 0, 0)
        self.container_layout.setSpacing(10)
        self.container_layout.addStretch()
        
        self.scroll_area.setWidget(self.container)
        main_layout.addWidget(self.scroll_area)

        # Test Controls
        ctrl_frame = QWidget()
        ctrl_layout = QVBoxLayout(ctrl_frame)
        ctrl_layout.setContentsMargins(0, 0, 0, 0)
        ctrl_layout.setSpacing(5)
        
        self.btn_run = QPushButton("RUN TEST")
        self.btn_run.setStyleSheet("QPushButton { background: #27272a; color: white; padding: 6px; border-radius: 3px; font-weight: bold; } QPushButton:hover { background: #3f3f46; }")
        ctrl_layout.addWidget(self.btn_run)
        
        row1 = QHBoxLayout()
        self.btn_pause = QPushButton("PAUSE")
        self.btn_pause.setStyleSheet(self.btn_run.styleSheet())
        self.btn_stop = QPushButton("ABORT")
        self.btn_stop.setStyleSheet(self.btn_run.styleSheet())
        row1.addWidget(self.btn_pause)
        row1.addWidget(self.btn_stop)
        ctrl_layout.addLayout(row1)
        
        row2 = QHBoxLayout()
        self.btn_prev = QPushButton("⏮ PREV")
        self.btn_prev.setStyleSheet(self.btn_run.styleSheet())
        self.btn_repeat = QPushButton("🔁 REPEAT")
        self.btn_repeat.setStyleSheet(self.btn_run.styleSheet())
        self.btn_next = QPushButton("⏭ NEXT")
        self.btn_next.setStyleSheet(self.btn_run.styleSheet())
        row2.addWidget(self.btn_prev)
        row2.addWidget(self.btn_repeat)
        row2.addWidget(self.btn_next)
        ctrl_layout.addLayout(row2)
        
        main_layout.addWidget(ctrl_frame)

    def populate_scenarios(self):
        # Update active game and scenarios directory target dynamically
        self.active_game = self.app_controller.config.get_active_game()
        from core.paths import get_base_dir
        self.scenarios_dir = os.path.join(get_base_dir(), "scenarios", self.active_game['id']) if self.active_game else ""

        # Clear layout
        while self.container_layout.count() > 1:
            item = self.container_layout.takeAt(0)
            if item.widget():
                item.widget().deleteLater()
                
        files = []
        if os.path.exists(self.scenarios_dir):
            files = sorted([f for f in os.listdir(self.scenarios_dir) if f.endswith('.lua')])
            
        self.title_lbl.setText(f"📂 SCENARIOS DATABASE ({len(files)})")

        for idx, f in enumerate(files):
            name = os.path.splitext(f)[0]
            is_selected = (idx == self.selected_scenario_index)
            
            card = QFrame()
            card.setObjectName(f"card_{idx}")
            card.setProperty("selected", is_selected)
            card.setStyleSheet(f"""
                QFrame#card_{idx} {{
                    background-color: #131317;
                    border: 1px solid {'#9333ea' if is_selected else '#3f3f46'};
                    border-radius: 4px;
                }}
            """)
            
            card_layout = QVBoxLayout(card)
            card_layout.setContentsMargins(10, 10, 10, 10)
            card_layout.setSpacing(5)
            
            # Title Row
            title_row = QHBoxLayout()
            bullet = QLabel("●")
            bullet.setStyleSheet(f"color: {'#9333ea' if is_selected else '#6b7280'};")
            title_row.addWidget(bullet)
            
            title_lbl = QLabel(name + ".lua")
            title_lbl.setStyleSheet("color: white; font-weight: bold;")
            title_row.addWidget(title_lbl)
            title_row.addStretch()
            card_layout.addLayout(title_row)
            
            desc_lbl = QLabel("Lua automated test sequence.")
            desc_lbl.setStyleSheet("color: #9ca3af; font-size: 11px;")
            desc_lbl.setWordWrap(True)
            card_layout.addWidget(desc_lbl)
            
            if is_selected:
                sep = QFrame()
                sep.setFrameShape(QFrame.HLine)
                sep.setStyleSheet("color: #3f3f46;")
                card_layout.addWidget(sep)
                
                # Fetch steps
                steps = self._extract_steps(os.path.join(self.scenarios_dir, f))
                
                hdr = QHBoxLayout()
                lbl1 = QLabel("[ STEP NAME ]")
                lbl1.setStyleSheet("color: #6b7280; font-size: 10px; font-weight: bold;")
                lbl2 = QLabel("[ STATUS ]")
                lbl2.setStyleSheet("color: #6b7280; font-size: 10px; font-weight: bold;")
                hdr.addWidget(lbl1)
                hdr.addStretch()
                hdr.addWidget(lbl2)
                card_layout.addLayout(hdr)
                
                for s_idx, step in enumerate(steps):
                    step_frame = HoverFrame()
                    step_frame.setContextMenuPolicy(Qt.CustomContextMenu)
                    # We have to explicitly capture variables inside the lambda loop
                    step_frame.customContextMenuRequested.connect(
                        lambda pos, s=name, st=step, i=s_idx: self.show_step_context_menu(pos, s, st, i)
                    )
                    
                    step_row = QHBoxLayout(step_frame)
                    step_row.setContentsMargins(5, 5, 5, 5)
                    
                    is_skipped = self.skipped_steps.get((name, step), False)
                    cb_text = "☐" if is_skipped else "☑"
                    cb_color = "#4b5563" if is_skipped else "#9333ea"
                    
                    cb = QLabel(cb_text)
                    cb.setStyleSheet(f"color: {cb_color}; font-size: 14px;")
                    cb.setCursor(Qt.PointingHandCursor)
                    cb.mousePressEvent = lambda e, s=name, st=step: self.toggle_step_skip(s, st)
                    step_row.addWidget(cb)
                    
                    is_active = (self.app_controller.current_step_idx == s_idx)
                    step_text = step
                    if is_active:
                        step_text += "  ◀"
                        
                    step_name_lbl = QLabel(step_text)
                    if is_active:
                        step_name_lbl.setStyleSheet("color: #eab308; font-weight: bold;")
                    else:
                        step_name_lbl.setStyleSheet(f"color: {'#4b5563' if is_skipped else '#f3f4f6'};")
                    step_row.addWidget(step_name_lbl)
                    step_row.addStretch()
                    
                    override = self.step_status_overrides.get((name, step))
                    status_text = "-"
                    status_color = "#6b7280"
                    
                    if is_skipped:
                        status_text = "SKIP"
                    elif override == "SUCCESS":
                        status_text = "SUCCESS"
                        status_color = "#10b981"
                    elif override == "FAILED":
                        status_text = "FAILED"
                        status_color = "#ef4444"
                        
                    status_lbl = QLabel(status_text)
                    status_lbl.setStyleSheet(f"color: {status_color}; font-weight: bold;")
                    step_row.addWidget(status_lbl)
                    
                    card_layout.addWidget(step_frame)
            
            # Click event: only toggle accordion if clicking the top title area with left click
            card.mousePressEvent = lambda e, i=idx: self.select_scenario(i) if e.position().y() < 35 and e.button() == Qt.LeftButton else None
            
            self.container_layout.insertWidget(self.container_layout.count() - 1, card)

    def toggle_step_skip(self, s_name, st_name):
        key = (s_name, st_name)
        self.skipped_steps[key] = not self.skipped_steps.get(key, False)
        self.populate_scenarios()

    def _extract_steps(self, path):
        steps = []
        if os.path.exists(path):
            with open(path, "r", encoding="utf-8") as f:
                content = f.read()
            matches = re.findall(r'set_stage\([\'"]([^\'"]+)[\'"]\)', content)
            steps.extend(matches)
        return steps
        
    def select_scenario(self, index):
        if self.selected_scenario_index == index:
            self.selected_scenario_index = -1 # Collapse!
        else:
            self.selected_scenario_index = index
            
        scrollbar = self.scroll_area.verticalScrollBar()
        vval = scrollbar.value()
        
        self.populate_scenarios()
        
        QTimer.singleShot(0, lambda v=vval: self.scroll_area.verticalScrollBar().setValue(v))
        
        files = sorted([f for f in os.listdir(self.scenarios_dir) if f.endswith('.lua')])
        if 0 <= index < len(files):
            self.scenario_selected.emit(os.path.join(self.scenarios_dir, files[index]))

    def show_step_context_menu(self, pos, scenario_name, step_name, step_idx):
        menu = QMenu(self)
        menu.setStyleSheet("QMenu { background-color: #18181c; color: white; border: 1px solid #3f3f46; } QMenu::item:selected { background-color: #3f3f46; }")
        
        action_success = menu.addAction("✅ Mark Success")
        action_failed = menu.addAction("❌ Mark Failed")
        action_clear = menu.addAction("🔄 Clear Override")
        menu.addSeparator()
        action_start = menu.addAction("▶️ Start Testing From Here")
        action_details = menu.addAction("ℹ️ View Details")
        
        action = menu.exec_(QCursor.pos())
        key = (scenario_name, step_name)
        
        if action == action_success:
            self.step_status_overrides[key] = "SUCCESS"
            self.populate_scenarios()
        elif action == action_failed:
            self.step_status_overrides[key] = "FAILED"
            self.populate_scenarios()
        elif action == action_clear:
            if key in self.step_status_overrides:
                del self.step_status_overrides[key]
            self.populate_scenarios()
        elif action == action_start:
            self.app_controller.current_step_idx = step_idx
            self.populate_scenarios()
        elif action == action_details:
            from PySide6.QtWidgets import QMessageBox
            QMessageBox.information(self, "Step Details", f"Scenario: {scenario_name}\nStep: {step_name}\nIndex: {step_idx}")
