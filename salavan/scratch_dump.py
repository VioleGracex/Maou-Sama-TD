import sys
import os
import json

sys.path.insert(0, os.path.abspath('salavan/gui_pyside'))

from core.app_controller import AppController
from core.config import ConfigManager
from core.logger import ReportLogger

app = AppController(ConfigManager('salavan/config.json'), ReportLogger())
state = app.read_game_state()

if state:
    elements = state.get("elements", {})
    buttons = []
    for path, elem in elements.items():
        if elem.get("type") == "Button":
            buttons.append(path)
    
    print("CURRENT BUTTONS IN STATE:")
    for b in buttons:
        print("  -", b)
else:
    print("No state found.")
