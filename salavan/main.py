import sys
import ctypes

# Set DPI awareness for correct coordinate mapping on High DPI displays
try:
    ctypes.windll.shcore.SetProcessDpiAwareness(2) # Per-monitor DPI aware
except Exception:
    try:
        ctypes.windll.user32.SetProcessDPIAware()
    except Exception:
        pass

# Safe redirection of stdout/stderr for PyInstaller windowed console=False mode
if getattr(sys, 'frozen', False):
    class NullWriter:
        def write(self, text): pass
        def flush(self): pass
    sys.stdout = NullWriter()
    sys.stderr = NullWriter()

import os
import tkinter as tk

# Ensure local imports inside salavan package resolve correctly
current_dir = os.path.dirname(os.path.abspath(__file__))
if current_dir not in sys.path:
    sys.path.insert(0, current_dir)

from gui.app import GameSalavanApp

if __name__ == "__main__":
    # Create a named mutex to ensure single instance
    ERROR_ALREADY_EXISTS = 183
    mutex_name = "Local\\SylvanHUDSalavanPanelSingleInstanceMutex"
    global_mutex = ctypes.windll.kernel32.CreateMutexW(None, False, mutex_name)
    last_error = ctypes.windll.kernel32.GetLastError()
    if last_error == ERROR_ALREADY_EXISTS:
        ctypes.windll.user32.MessageBoxW(0, "Another instance of Sylvan-HUD Salavan Panel is already running.", "Sylvan-HUD Game Salavan Panel", 0x30)
        sys.exit(0)

    root = tk.Tk()
    
    def show_error(exc, val, tb):
        import traceback
        from tkinter import messagebox
        err_msg = "".join(traceback.format_exception(exc, val, tb))
        messagebox.showerror("Runtime Error", err_msg)
    root.report_callback_exception = show_error

    app = GameSalavanApp(root)
    root.mainloop()
