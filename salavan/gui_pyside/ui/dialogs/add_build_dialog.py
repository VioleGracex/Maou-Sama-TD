import os
import re
import ctypes
from PySide6.QtWidgets import (
    QDialog, QVBoxLayout, QHBoxLayout, QLabel, QLineEdit, 
    QPushButton, QMessageBox, QFileDialog
)
from PySide6.QtCore import Qt

def get_exe_version(path):
    try:
        version_dll = ctypes.WinDLL('version.dll')
        version_dll.GetFileVersionInfoSizeW.argtypes = [ctypes.c_wchar_p, ctypes.POINTER(ctypes.c_uint32)]
        version_dll.GetFileVersionInfoSizeW.restype = ctypes.c_uint32
        
        version_dll.GetFileVersionInfoW.argtypes = [ctypes.c_wchar_p, ctypes.c_uint32, ctypes.c_uint32, ctypes.c_void_p]
        version_dll.GetFileVersionInfoW.restype = ctypes.c_int
        
        version_dll.VerQueryValueW.argtypes = [ctypes.c_void_p, ctypes.c_wchar_p, ctypes.POINTER(ctypes.c_void_p), ctypes.POINTER(ctypes.c_uint)]
        version_dll.VerQueryValueW.restype = ctypes.c_int

        dw_handle = ctypes.c_uint32(0)
        size = version_dll.GetFileVersionInfoSizeW(path, ctypes.byref(dw_handle))
        if not size:
            return None
            
        res = ctypes.create_string_buffer(size)
        if not version_dll.GetFileVersionInfoW(path, 0, size, res):
            return None
            
        lplpBuffer = ctypes.c_void_p()
        puLen = ctypes.c_uint()
        if version_dll.VerQueryValueW(res, "\\", ctypes.byref(lplpBuffer), ctypes.byref(puLen)):
            if puLen.value >= 52:
                ffi = ctypes.cast(lplpBuffer, ctypes.POINTER(ctypes.c_uint32))
                file_ver_ms = ffi[2]
                file_ver_ls = ffi[3]
                major = (file_ver_ms >> 16) & 0xFFFF
                minor = file_ver_ms & 0xFFFF
                build = (file_ver_ls >> 16) & 0xFFFF
                revision = file_ver_ls & 0xFFFF
                return f"{major}.{minor}.{build}.{revision}"
    except Exception:
        pass
    return None

def extract_version_from_path(path):
    normalized = path.replace('\\', '/')
    filename = os.path.basename(normalized)
    match = re.search(r'[vV]?(\d+\.\d+\.\d+(\.\d+)?)', filename)
    if match:
        return match.group(0)
    parts = normalized.split('/')
    for part in reversed(parts[:-1]):
        match = re.search(r'[vV]?(\d+\.\d+\.\d+(\.\d+)?)', part)
        if match:
            return match.group(0)
    return None

def detect_version(path):
    pe_ver = get_exe_version(path)
    if pe_ver and pe_ver not in ("0.0.0.0", "1.0.0.0"):
        if pe_ver.endswith(".0.0"):
            pe_ver = pe_ver[:-4]
        elif pe_ver.endswith(".0"):
            pe_ver = pe_ver[:-2]
        return f"v{pe_ver}"
        
    path_ver = extract_version_from_path(path)
    if path_ver:
        if not path_ver.lower().startswith('v'):
            path_ver = f"v{path_ver}"
        return path_ver
    return "v1.0.0"

class AddBuildDialog(QDialog):
    def __init__(self, app_controller, parent=None):
        super().__init__(parent)
        self.app_controller = app_controller
        self.setWindowTitle("Add Game Build")
        self.setFixedSize(450, 240)
        self.setStyleSheet("background-color: #18181c; color: white;")
        self._init_ui()

    def _init_ui(self):
        layout = QVBoxLayout(self)
        layout.setContentsMargins(20, 20, 20, 20)
        layout.setSpacing(10)
        
        active_game = self.app_controller.config.get_active_game()
        active_title = active_game.get("title", "") if active_game else ""

        # Title
        row_title = QHBoxLayout()
        lbl_title = QLabel("Game Title:")
        lbl_title.setFixedWidth(100)
        self.title_entry = QLineEdit(active_title)
        self.title_entry.setStyleSheet("QLineEdit { background: #0d0d11; border: 1px solid #3f3f46; padding: 5px; border-radius: 4px; color: white; }")
        row_title.addWidget(lbl_title)
        row_title.addWidget(self.title_entry)
        layout.addLayout(row_title)

        # Path
        row_path = QHBoxLayout()
        lbl_path = QLabel("Executable:")
        lbl_path.setFixedWidth(100)
        self.path_entry = QLineEdit()
        self.path_entry.setReadOnly(True)
        self.path_entry.setStyleSheet("QLineEdit { background: #0d0d11; border: 1px solid #3f3f46; padding: 5px; border-radius: 4px; color: white; }")
        btn_browse = QPushButton("BROWSE")
        btn_browse.setStyleSheet("QPushButton { background: #27272a; padding: 5px 12px; border-radius: 4px; } QPushButton:hover { background: #3f3f46; }")
        btn_browse.clicked.connect(self._browse)
        row_path.addWidget(lbl_path)
        row_path.addWidget(self.path_entry)
        row_path.addWidget(btn_browse)
        layout.addLayout(row_path)

        # Version
        row_ver = QHBoxLayout()
        lbl_ver = QLabel("Version:")
        lbl_ver.setFixedWidth(100)
        self.version_entry = QLineEdit()
        self.version_entry.setStyleSheet("QLineEdit { background: #0d0d11; border: 1px solid #3f3f46; padding: 5px; border-radius: 4px; color: white; }")
        row_ver.addWidget(lbl_ver)
        row_ver.addWidget(self.version_entry)
        layout.addLayout(row_ver)

        layout.addStretch()

        # Buttons
        btn_layout = QHBoxLayout()
        btn_layout.addStretch()
        
        self.btn_cancel = QPushButton("Cancel")
        self.btn_cancel.setStyleSheet("QPushButton { background: #27272a; border-radius: 4px; padding: 6px 15px; } QPushButton:hover { background: #3f3f46; }")
        self.btn_cancel.clicked.connect(self.reject)
        
        self.btn_save = QPushButton("Save Build")
        self.btn_save.setStyleSheet("QPushButton { background: #10b981; color: white; border-radius: 4px; padding: 6px 15px; font-weight: bold; } QPushButton:hover { background: #34d399; }")
        self.btn_save.clicked.connect(self._save)
        
        btn_layout.addWidget(self.btn_cancel)
        btn_layout.addWidget(self.btn_save)
        layout.addLayout(btn_layout)

    def _browse(self):
        f, _ = QFileDialog.getOpenFileName(self, "Select Game Executable", "", "Executable Files (*.exe)")
        if f:
            self.path_entry.setText(f)
            ver = detect_version(f)
            self.version_entry.setText(ver)

    def _save(self):
        t = self.title_entry.text().strip()
        v = self.version_entry.text().strip()
        p = self.path_entry.text().strip()
        
        if not t or not v or not p:
            QMessageBox.warning(self, "Validation Error", "All fields must be filled!")
            return
            
        config = self.app_controller.config
        config.builds.append({
            "game_id": config.active_game_id,
            "title": t,
            "version": v,
            "path": p,
            "status": "Pending",
            "last_tested": "-",
            "report_ref": "-"
        })
        
        # Auto-activate build path
        config.game_exe_path = p
        config.save()
        
        self.accept()
