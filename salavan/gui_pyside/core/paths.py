import os
import sys

def get_base_dir():
    if getattr(sys, 'frozen', False):
        return os.path.dirname(sys.executable)
    # If gui_pyside/core/paths.py, then base_dir is salavan/
    return os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

def get_config_path():
    return os.path.normpath(os.path.join(get_base_dir(), "config.json"))
