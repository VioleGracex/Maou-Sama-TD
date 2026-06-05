import os
import time
from PySide6.QtCore import QThread, Signal

class UnityLogMonitorThread(QThread):
    error_detected = Signal(str, str, str) # step, status, msg
    
    def __init__(self, log_path, start_pos, parent=None):
        super().__init__(parent)
        self.log_path = log_path
        self.position = start_pos
        self.running = True
        
    def run(self):
        while self.running:
            if os.path.exists(self.log_path):
                try:
                    with open(self.log_path, "r", encoding="utf-8", errors="ignore") as f:
                        f.seek(self.position)
                        new_lines = f.readlines()
                        self.position = f.tell()
                        
                        for line in new_lines:
                            line_stripped = line.strip()
                            if not line_stripped:
                                continue
                            
                            lower_line = line_stripped.lower()
                            is_error = False
                            if "exception:" in lower_line or "nullreferenceexception" in lower_line:
                                is_error = True
                            elif "assertion failed" in lower_line or "assertionfailed" in lower_line:
                                is_error = True
                            elif "error:" in lower_line and not "error: 0" in lower_line:
                                is_error = True
                            elif "crash" in lower_line or "fallback handler" in lower_line:
                                is_error = True
                                
                            if is_error:
                                self.error_detected.emit("UNITY ENGINE", "FAIL", line_stripped)
                except Exception:
                    pass
            time.sleep(1.0)

    def stop(self):
        self.running = False
        self.wait()
