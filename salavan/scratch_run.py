import sys
import os
import ctypes

current_dir = os.path.dirname(os.path.abspath(__file__))
if current_dir not in sys.path:
    sys.path.insert(0, current_dir)
pyside_dir = os.path.join(current_dir, "gui_pyside")
if pyside_dir not in sys.path:
    sys.path.insert(0, pyside_dir)

from PySide6.QtWidgets import QApplication
from core.config import ConfigManager
from core.logger import ReportLogger
from core.app_controller import AppController

def main():
    try:
        ctypes.windll.shcore.SetProcessDpiAwareness(2)
    except Exception:
        try:
            ctypes.windll.user32.SetProcessDPIAware()
        except Exception:
            pass

    app = QApplication(sys.argv)
    
    from core.paths import get_config_path
    config_path = get_config_path()
    config = ConfigManager(config_path)
    
    # Run against the built executable
    config.hook_unity_editor = False
    config.save()
    
    logger = ReportLogger()
    app_controller = AppController(config, logger)
    
    def log_handler(step, status, msg):
        print(f"[{status}] {step}: {msg}", flush=True)
    
    app_controller.log_added.connect(log_handler)
    
    os.environ["LUA_TEST_ARGS"] = "-screen-width 1920 -screen-height 1080 -screen-fullscreen 1"
    scenario_name = "1_Fresh_Start"
    from core.paths import get_base_dir
    scenario_path = os.path.join(get_base_dir(), "scenarios", config.get_active_game()["id"], f"{scenario_name}.lua")
    
    logs_dir = os.path.join(get_base_dir(), "reports", config.get_active_game()["id"])
    capture_dir = os.path.join(get_base_dir(), "recordings")
    
    print(f"Running scenario: {scenario_path}")
    
    def finish_handler(status):
        print(f"Scenario Finished with status: {status}")
        app_controller.shutdown()
        QApplication.quit()
        
    app_controller.test_finished.connect(finish_handler)
    
    import time
    if app_controller.launch_game():
        time.sleep(3.0)  # Wait for game to initialize
        app_controller.start_test(scenario_path, scenario_name, logs_dir, capture_dir)
    else:
        print("Failed to launch game executable.")
        QApplication.quit()

    sys.exit(app.exec())

if __name__ == "__main__":
    current_dir = os.path.dirname(os.path.abspath(__file__))
    if current_dir not in sys.path:
        sys.path.insert(0, current_dir)
        
    pyside_dir = os.path.join(current_dir, "gui_pyside")
    if pyside_dir not in sys.path:
        sys.path.insert(0, pyside_dir)
        
    main()
