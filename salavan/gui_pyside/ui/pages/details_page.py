import os
from PySide6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QPushButton, 
    QSplitter, QStackedWidget
)
from PySide6.QtCore import Qt

# Import modular widgets
from ui.widgets.scenarios_sidebar import ScenariosSidebar
from ui.widgets.center_dashboard import CenterDashboard
from ui.widgets.logs_panel import LogsPanel
from ui.widgets.builds_sidebar import BuildsSidebar

class DetailsPage(QWidget):
    def __init__(self, app_controller, main_window, parent=None):
        super().__init__(parent)
        self.main_window = main_window
        self.app_controller = app_controller
        self._init_ui()

    def _init_ui(self):
        main_layout = QVBoxLayout(self)
        main_layout.setContentsMargins(10, 10, 10, 10)
        main_layout.setSpacing(10)

        # Header Navigation (Back to Library)
        hdr_frame = QWidget()
        hdr_layout = QHBoxLayout(hdr_frame)
        hdr_layout.setContentsMargins(0, 0, 0, 0)
        
        self.btn_back = QPushButton("◀ BACK TO LIBRARY")
        self.btn_back.setStyleSheet("""
            QPushButton { background-color: #27272a; color: white; border: none; padding: 10px 20px; font-weight: bold; border-radius: 4px; }
            QPushButton:hover { background-color: #3f3f46; }
        """)
        self.btn_back.clicked.connect(lambda: self.main_window.switch_tab(0))  # Switch to Library
        hdr_layout.addWidget(self.btn_back)
        hdr_layout.addStretch()
        
        main_layout.addWidget(hdr_frame)

        # Splitter Layout (Left Sidebar | Center Stack | Right Sidebar)
        main_splitter = QSplitter(Qt.Horizontal)
        
        # 1. Left Sidebar (Scenarios)
        self.scenarios_sidebar = ScenariosSidebar(self.app_controller)
        main_splitter.addWidget(self.scenarios_sidebar)
        
        # 2. Center Column (Tabs Dashboard over Logs Panel)
        center_column = QWidget()
        center_layout = QVBoxLayout(center_column)
        center_layout.setContentsMargins(0, 0, 0, 0)
        
        self.center_splitter = QSplitter(Qt.Vertical)
        
        self.center_dashboard = CenterDashboard(self.app_controller)
        self.center_splitter.addWidget(self.center_dashboard)
        
        self.logs_panel = LogsPanel(self.app_controller)
        self.center_splitter.addWidget(self.logs_panel)
        
        self.center_splitter.setSizes([700, 300]) # 70% top, 30% bottom
        
        center_layout.addWidget(self.center_splitter)
        main_splitter.addWidget(center_column)
        
        # 3. Right Sidebar (Builds)
        self.builds_sidebar = BuildsSidebar(self.app_controller)
        main_splitter.addWidget(self.builds_sidebar)
        
        # Set Splitter Sizes (25% | 55% | 20%)
        main_splitter.setSizes([350, 800, 300])
        main_layout.addWidget(main_splitter, 1)
        
        # Connect Scenarios Control Buttons
        self.scenarios_sidebar.btn_run.clicked.connect(self.run_selected_test)
        self.scenarios_sidebar.btn_pause.clicked.connect(self.app_controller.toggle_pause)
        self.scenarios_sidebar.btn_stop.clicked.connect(self.app_controller.abort_test)
        self.scenarios_sidebar.btn_prev.clicked.connect(self.app_controller.prev_step)
        self.scenarios_sidebar.btn_repeat.clicked.connect(self.app_controller.repeat_step)
        self.scenarios_sidebar.btn_next.clicked.connect(self.app_controller.next_step)
        
        # Connect AppController execution signals
        self.app_controller.test_started.connect(lambda: self.update_control_states(True))
        self.app_controller.test_finished.connect(lambda s: self.update_control_states(False))

        # Connect dock/popout logs
        self.center_dashboard.btn_dock.clicked.connect(self.dock_logs)
        self.logs_panel.btn_pop.clicked.connect(self.undock_logs)
        
        # Initialize control states
        self.update_control_states(False)

    def update_control_states(self, running):
        self.scenarios_sidebar.btn_run.setEnabled(not running)
        self.scenarios_sidebar.btn_pause.setEnabled(running)
        self.scenarios_sidebar.btn_stop.setEnabled(running)
        self.scenarios_sidebar.btn_prev.setEnabled(running)
        self.scenarios_sidebar.btn_repeat.setEnabled(running)
        self.scenarios_sidebar.btn_next.setEnabled(running)

    def run_selected_test(self):
        sidebar = self.scenarios_sidebar
        files = sorted([f for f in os.listdir(sidebar.scenarios_dir) if f.endswith('.lua')])
        if sidebar.selected_scenario_index < len(files):
            filename = files[sidebar.selected_scenario_index]
            scenario_name = os.path.splitext(filename)[0]
            scenario_path = os.path.join(sidebar.scenarios_dir, filename)
            
            from core.paths import get_base_dir
            logs_dir = os.path.join(get_base_dir(), "reports", self.app_controller.config.get_active_game()["id"])
            capture_dir = os.path.join(get_base_dir(), "recordings")
            os.makedirs(logs_dir, exist_ok=True)
            os.makedirs(capture_dir, exist_ok=True)
            
            # Switch to Live View tab on dashboard
            self.center_dashboard.switch_tab(1)
            
            self.app_controller.start_test(scenario_path, scenario_name, logs_dir, capture_dir)

    # Required API methods that MainWindow calls on DetailsPage
    def populate_scenarios(self):
        self.scenarios_sidebar.populate_scenarios()
        
    def populate_reports(self):
        # Stub for scanning reports
        pass
        
    def load_game(self, game_data):
        self.app_controller.config.set_active_game(game_data['id'])
        self.scenarios_sidebar.populate_scenarios()
        self.center_dashboard.refresh()
        self.builds_sidebar.refresh_builds()
        self.builds_sidebar.refresh_reports()

    def undock_logs(self):
        if not hasattr(self, 'logs_dialog'):
            from PySide6.QtWidgets import QDialog, QVBoxLayout
            self.logs_dialog = QDialog(self)
            self.logs_dialog.setWindowTitle("HUD Diagnostic Logs")
            self.logs_dialog.resize(800, 350)
            self.logs_dialog.setStyleSheet("background-color: #18181c; color: white;")
            layout = QVBoxLayout(self.logs_dialog)
            layout.setContentsMargins(5, 5, 5, 5)
            self.logs_dialog.setLayout(layout)
            
            # Dock back when closed
            self.logs_dialog.finished.connect(lambda r: self.dock_logs())
            
        self.logs_panel.setParent(self.logs_dialog)
        self.logs_dialog.layout().addWidget(self.logs_panel)
        self.logs_panel.show()
        self.logs_dialog.show()
        
        self.center_dashboard.btn_dock.setVisible(True)
        self.logs_panel.btn_pop.setVisible(False)

    def dock_logs(self):
        if hasattr(self, 'logs_dialog') and self.logs_dialog.isVisible():
            self.logs_dialog.blockSignals(True)
            self.logs_dialog.close()
            self.logs_dialog.blockSignals(False)
            
        self.logs_panel.setParent(self.center_splitter)
        self.center_splitter.insertWidget(1, self.logs_panel)
        self.logs_panel.show()
        
        self.center_dashboard.btn_dock.setVisible(False)
        self.logs_panel.btn_pop.setVisible(True)
