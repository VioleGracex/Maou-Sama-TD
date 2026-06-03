import os
import re
import ctypes
from ctypes import wintypes
import tkinter as tk
from tkinter import ttk, messagebox, filedialog

def get_exe_version(path):
    try:
        # Load version.dll
        version_dll = ctypes.WinDLL('version.dll')
        
        # Explicitly declare ctypes function prototypes for 64-bit stability
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
        # Query fixed file info struct
        if version_dll.VerQueryValueW(res, "\\", ctypes.byref(lplpBuffer), ctypes.byref(puLen)):
            if puLen.value >= 52:
                ffi = ctypes.cast(lplpBuffer, ctypes.POINTER(ctypes.c_uint32))
                # dwFileVersionMS at ffi[2], dwFileVersionLS at ffi[3]
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
    # Normalize slashes
    normalized = path.replace('\\', '/')
    filename = os.path.basename(normalized)
    
    # 1. Search filename
    match = re.search(r'[vV]?(\d+\.\d+\.\d+(\.\d+)?)', filename)
    if match:
        return match.group(0)
        
    # 2. Search parent directories from right to left
    parts = normalized.split('/')
    for part in reversed(parts[:-1]):
        match = re.search(r'[vV]?(\d+\.\d+\.\d+(\.\d+)?)', part)
        if match:
            return match.group(0)
            
    return None

def detect_version(path):
    # 1. Try reading executable properties
    pe_ver = get_exe_version(path)
    if pe_ver and pe_ver not in ("0.0.0.0", "1.0.0.0"):
        # Strip trailing .0 elements if present
        if pe_ver.endswith(".0.0"):
            pe_ver = pe_ver[:-4]
        elif pe_ver.endswith(".0"):
            pe_ver = pe_ver[:-2]
        return f"v{pe_ver}"
        
    # 2. Fall back to folder/filename regex parsing
    path_ver = extract_version_from_path(path)
    if path_ver:
        if not path_ver.lower().startswith('v'):
            path_ver = f"v{path_ver}"
        return path_ver
        
    return "v1.0.0"

def show_add_build_dialog(app):
    # Prevent duplicate dialog instances
    if hasattr(app, 'add_build_dialog') and app.add_build_dialog and app.add_build_dialog.winfo_exists():
        app.add_build_dialog.lift()
        app.add_build_dialog.focus_force()
        return

    dialog = tk.Toplevel(app.root)
    app.add_build_dialog = dialog
    dialog.title("Add Game Build")
    dialog.geometry("380x220")
    
    # Position the modal dialog in the center of the main window
    app.root.update_idletasks()
    x = app.root.winfo_rootx() + (app.root.winfo_width() - 380) // 2
    y = app.root.winfo_rooty() + (app.root.winfo_height() - 220) // 2
    dialog.geometry(f"380x220+{max(0, x)}+{max(0, y)}")
    dialog.configure(bg=app.bg_dark)
    dialog.attributes("-topmost", True)
    
    # Make modal to prevent opening multiple build dialog windows
    dialog.transient(app.root)
    dialog.grab_set()
    
    lbl_title = tk.Label(dialog, text="Game Title:", bg=app.bg_dark, fg=app.fg_light, font=("Consolas", 9, "bold"))
    lbl_title.grid(row=0, column=0, padx=10, pady=10, sticky="w")
    ent_title = tk.Entry(dialog, bg="#1b1b26", fg=app.fg_light, bd=0, insertbackground=app.fg_light)
    ent_title.grid(row=0, column=1, padx=10, pady=10, sticky="ew")
    
    active_game = app.config.get_active_game()
    ent_title.insert(0, active_game.get("title", ""))
    
    lbl_ver = tk.Label(dialog, text="Version:", bg=app.bg_dark, fg=app.fg_light, font=("Consolas", 9, "bold"))
    lbl_ver.grid(row=1, column=0, padx=10, pady=10, sticky="w")
    ent_ver = tk.Entry(dialog, bg="#1b1b26", fg=app.fg_light, bd=0, insertbackground=app.fg_light)
    ent_ver.grid(row=1, column=1, padx=10, pady=10, sticky="ew")
    
    lbl_path = tk.Label(dialog, text="Executable:", bg=app.bg_dark, fg=app.fg_light, font=("Consolas", 9, "bold"))
    lbl_path.grid(row=2, column=0, padx=10, pady=10, sticky="w")
    
    path_var = tk.StringVar()
    ent_path = tk.Entry(dialog, textvariable=path_var, bg="#1b1b26", fg=app.fg_light, bd=0, state="readonly")
    ent_path.grid(row=2, column=1, padx=10, pady=10, sticky="ew")
    
    def browse():
        f = filedialog.askopenfilename(title="Select Game Executable", filetypes=[("Executable Files", "*.exe")])
        if f:
            path_var.set(f)
            ver = detect_version(f)
            ent_ver.delete(0, tk.END)
            ent_ver.insert(0, ver)
            
    btn_browse = ttk.Button(dialog, text="BROWSE", command=browse, width=8)
    btn_browse.grid(row=2, column=2, padx=10, pady=10)
    
    def save():
        t = ent_title.get().strip()
        v = ent_ver.get().strip()
        p = path_var.get().strip()
        
        if not t or not v or not p:
            messagebox.showerror("Error", "All fields must be filled!")
            return
            
        app.config.builds.append({
            "game_id": app.config.active_game_id,
            "title": t,
            "version": v,
            "path": p,
            "status": "Pending",
            "last_tested": "-",
            "report_ref": "-"
        })
        
        # Always set the newly added build as the active runner target automatically
        app.config.game_exe_path = p
        if hasattr(app, 'path_entry_var') and app.path_entry_var:
            app.path_entry_var.set(p)
                
        app.config.save()
        app.refresh_builds_tree()
        if hasattr(app, 'update_window_title_with_version'):
            app.update_window_title_with_version()
        dialog.destroy()
        app.log_message("SYSTEM", "INFO", f"Registered and activated build for {active_game.get('title')}: {t} ({v})")
        
    btn_save = ttk.Button(dialog, text="SAVE BUILD", command=save)
    btn_save.grid(row=3, column=1, pady=15, sticky="ew")
    
    dialog.columnconfigure(1, weight=1)
