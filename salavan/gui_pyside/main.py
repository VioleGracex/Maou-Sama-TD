import sys
import os
import ctypes
from PySide6.QtWidgets import QApplication, QMessageBox
from PySide6.QtCore import QSharedMemory

current_dir = os.path.dirname(os.path.abspath(__file__))
if current_dir not in sys.path:
    sys.path.insert(0, current_dir)

from core.config import ConfigManager
from core.logger import ReportLogger
from core.app_controller import AppController
from ui.main_window import MainWindow

def main():
    try:
        ctypes.windll.shcore.SetProcessDpiAwareness(2)
    except Exception:
        try:
            ctypes.windll.user32.SetProcessDPIAware()
        except Exception:
            pass

    app = QApplication(sys.argv)
    
    shared_mem = QSharedMemory("SylvanHUDSalavanPanelSingleInstanceMutex")
    if not shared_mem.create(1):
        QMessageBox.critical(None, "Sylvan-HUD Game Salavan Panel", "Another instance of Sylvan-HUD Salavan Panel is already running.")
        sys.exit(0)

    from core.paths import get_config_path
    config = ConfigManager(get_config_path())
    logger = ReportLogger()
    
    app_controller = AppController(config, logger)
    window = MainWindow(app_controller)
    window.show()

    exit_code = app.exec()
    app_controller.shutdown()
    sys.exit(exit_code)

if __name__ == "__main__":
    if getattr(sys, 'frozen', False):
        class NullWriter:
            def write(self, text): pass
            def flush(self): pass
        sys.stdout = NullWriter()
        sys.stderr = NullWriter()
        
    main()
