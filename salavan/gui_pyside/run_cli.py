import sys
import os
from PySide6.QtWidgets import QApplication

current_dir = os.path.dirname(os.path.abspath(__file__))
parent_dir = os.path.dirname(current_dir)
if current_dir not in sys.path:
    sys.path.insert(0, current_dir)
if parent_dir not in sys.path:
    sys.path.insert(0, parent_dir)

from core.config import ConfigManager
from core.logger import ReportLogger
from core.app_controller import AppController
from core.paths import get_config_path, get_base_dir

def run_test():
    app = QApplication(sys.argv)
    config = ConfigManager(get_config_path())
    logger = ReportLogger()
    app_controller = AppController(config, logger)
    
    scenario_path = os.path.join(get_base_dir(), "scenarios", "maou_sama_td", "1_Fresh_Start.lua")
    logs_dir = os.path.join(get_base_dir(), "reports", config.get_active_game()["id"])
    capture_dir = os.path.join(get_base_dir(), "recordings")
    
    def on_test_finished(success):
        print(f"Test finished with success: {success}")
        app.quit()
        
    app_controller.test_finished.connect(on_test_finished)
    app_controller.start_test(scenario_path, '1_Fresh_Start', logs_dir, capture_dir)
    
    sys.exit(app.exec())

if __name__ == '__main__':
    run_test()
