import tkinter as tk
from tkinter import ttk, messagebox

def show_shortcuts_dialog(app):
    dialog = tk.Toplevel(app.root)
    dialog.title("Edit Hotkeys")
    dialog.geometry("320x240+1200+200")
    dialog.configure(bg=app.bg_dark)
    dialog.attributes("-topmost", True)
    
    lbl_info = tk.Label(dialog, text="Tkinter hotkey format (e.g. <Control-p>, <F5>):", bg=app.bg_dark, fg=app.accent_glow, font=("Consolas", 8, "italic"))
    lbl_info.pack(pady=5)
    
    f_pause = tk.Frame(dialog, bg=app.bg_dark)
    f_pause.pack(fill="x", padx=15, pady=8)
    lbl_pause = tk.Label(f_pause, text="Pause/Resume:", bg=app.bg_dark, fg=app.fg_light, font=("Consolas", 9, "bold"))
    lbl_pause.pack(side="left")
    ent_pause = tk.Entry(f_pause, bg="#1b1b26", fg=app.fg_light, bd=0, width=12, insertbackground=app.fg_light)
    ent_pause.pack(side="right")
    ent_pause.insert(0, app.config.hotkeys.get("pause", "<Control-p>"))
    
    f_abort = tk.Frame(dialog, bg=app.bg_dark)
    f_abort.pack(fill="x", padx=15, pady=8)
    lbl_abort = tk.Label(f_abort, text="Abort Test:", bg=app.bg_dark, fg=app.fg_light, font=("Consolas", 9, "bold"))
    lbl_abort.pack(side="left")
    ent_abort = tk.Entry(f_abort, bg="#1b1b26", fg=app.fg_light, bd=0, width=12, insertbackground=app.fg_light)
    ent_abort.pack(side="right")
    ent_abort.insert(0, app.config.hotkeys.get("abort", "<Control-q>"))
    
    f_mode = tk.Frame(dialog, bg=app.bg_dark)
    f_mode.pack(fill="x", padx=15, pady=8)
    lbl_mode = tk.Label(f_mode, text="Toggle Overlay:", bg=app.bg_dark, fg=app.fg_light, font=("Consolas", 9, "bold"))
    lbl_mode.pack(side="left")
    ent_mode = tk.Entry(f_mode, bg="#1b1b26", fg=app.fg_light, bd=0, width=12, insertbackground=app.fg_light)
    ent_mode.pack(side="right")
    ent_mode.insert(0, app.config.hotkeys.get("toggle_mode", "<Control-o>"))
    
    def save():
        p = ent_pause.get().strip()
        a = ent_abort.get().strip()
        m = ent_mode.get().strip()
        
        if not p or not a or not m:
            messagebox.showerror("Error", "Keys cannot be empty!")
            return
            
        app.config.hotkeys["pause"] = p
        app.config.hotkeys["abort"] = a
        app.config.hotkeys["toggle_mode"] = m
        
        app.config.save()
        app.bind_hotkeys()
        dialog.destroy()
        app.log_message("SYSTEM", "INFO", "Keyboard shortcuts remapped successfully.")
        
    btn_save = ttk.Button(dialog, text="SAVE SHORTCUTS", command=save)
    btn_save.pack(pady=15)
