import sys
import os
import json

sys.path.insert(0, os.path.abspath('salavan/gui_pyside'))

from core.app_controller import AppController
from core.config import ConfigManager

class DummyLogger:
    def log_message(self, *args, **kwargs): pass

app = AppController(ConfigManager('salavan/config.json'), DummyLogger())
state = app.read_game_state()

if state:
    elements = state.get("elements", {})
    buttons = []
    for path, elem in elements.items():
        if elem.get("type") == "Button":
            buttons.append((path, elem.get("text")))
    
    print("CURRENT BUTTONS IN STATE:")
    for b, txt in buttons:
        print(f"  - {b} (Text: {txt})")
    print("CURRENT SCENE:", state.get("current_scene"))
else:
    print("No state found.")
