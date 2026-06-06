import sys
sys.path.append(r'd:\OuikiDev\Maou-Sama-TD\salavan\gui_pyside')
sys.path.append(r'd:\OuikiDev\Maou-Sama-TD\salavan\gui_pyside\core')

from PySide6.QtWidgets import QApplication
app = QApplication(sys.argv)

import app_controller
class DummyConfig:
    automation_key = 'salavantester'
class DummyApp:
    def __init__(self):
        self.config = DummyConfig()
c = app_controller.AppController(DummyConfig(), None)
print(c.lua_find_element('UnitButton_Ignis'))
