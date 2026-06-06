import sys
import os
from PySide6.QtWidgets import QApplication

sys.path.insert(0, 'd:/OuikiDev/Maou-Sama-TD/salavan/gui_pyside')
from core.config import ConfigManager
from core.logger import ReportLogger
from core.app_controller import AppController

app = QApplication(sys.argv)

config = ConfigManager('d:/OuikiDev/Maou-Sama-TD/salavan/config.json')
logger = ReportLogger()
app_controller = AppController(config, logger)

def on_finished(status):
    print(f"Test finished with status: {status}")
    app_controller.shutdown()
    app.quit()

app_controller.test_finished.connect(on_finished)
app_controller.start_test(
    'd:/OuikiDev/Maou-Sama-TD/salavan/scenarios/maou_sama_td/1_Fresh_Start.lua',
    '1_Fresh_Start',
    'd:/OuikiDev/Maou-Sama-TD/salavan/reports/maou_sama_td',
    'd:/OuikiDev/Maou-Sama-TD/salavan/reports/maou_sama_td'
)

sys.exit(app.exec())
