import tkinter as tk
from tkinter import ttk, messagebox

class SettingsPage(tk.Frame):
    def __init__(self, parent, app):
        super().__init__(parent, bg=app.bg_dark, padx=30, pady=30)
        self.app = app
        self.create_widgets()

    def create_widgets(self):
        # Settings Scrollable Pane
        scroll_y = ttk.Scrollbar(self)
        scroll_y.pack(side="right", fill="y")
        
        self.settings_canvas = tk.Canvas(self, bg=self.app.bg_dark, bd=0, highlightthickness=0, yscrollcommand=scroll_y.set)
        self.settings_canvas.pack(side="left", fill="both", expand=True)
        scroll_y.config(command=self.settings_canvas.yview)
        
        self.settings_inner = tk.Frame(self.settings_canvas, bg=self.app.bg_dark)
        self.settings_window = self.settings_canvas.create_window((0, 0), window=self.settings_inner, anchor="nw")
        
        self.settings_inner.bind("<Configure>", lambda e: self.settings_canvas.configure(scrollregion=self.settings_canvas.bbox("all")))
        self.settings_canvas.bind("<Configure>", lambda e: self.settings_canvas.itemconfig(self.settings_window, width=e.width))

        # Title
        tk.Label(
            self.settings_inner, text="// GENERAL SETTINGS", 
            fg=self.app.accent_glow, bg=self.app.bg_dark, 
            font=("Segoe UI", 14, "bold")
        ).pack(anchor="w", pady=(0, 20))

        # Group 1: Access shortcuts
        self.build_settings_group(self.settings_inner, "SHORTCUT KEYBOARD BINDINGS", self.build_shortcuts_fields)
        
        # Group 2: Salavan Options
        self.build_settings_group(self.settings_inner, "SALAVAN HUD PREFERENCES", self.build_preferences_fields)

        # Group 3: Profiles Scan
        self.build_settings_group(self.settings_inner, "DATABASE PROFILES CONFIG", self.build_profiles_fields)


    def build_settings_group(self, parent, title, build_func):
        group_border = tk.Frame(parent, bg=self.app.accent_dim, bd=1)
        group_border.pack(fill="x", pady=12)
        
        group_frame = tk.Frame(group_border, bg=self.app.bg_panel, padx=20, pady=15)
        group_frame.pack(fill="both")
        
        lbl_title = tk.Label(group_frame, text=f"// {title}", fg=self.app.accent_glow, bg=self.app.bg_panel, font=("Segoe UI", 10, "bold"))
        lbl_title.pack(anchor="w", pady=(0, 10))
        
        build_func(group_frame)

    def build_shortcuts_fields(self, frame):
        # Pause
        f1 = tk.Frame(frame, bg=self.app.bg_panel)
        f1.pack(fill="x", pady=6)
        tk.Label(f1, text="Pause/Resume Execution:", bg=self.app.bg_panel, fg=self.app.fg_light, font=("Segoe UI", 9, "bold")).pack(side="left")
        self.ent_pause = tk.Entry(f1, bg="#151518", fg=self.app.fg_light, insertbackground=self.app.fg_light, bd=1, highlightbackground="#2c2c35", width=16, font=("Segoe UI", 9, "bold"))
        self.ent_pause.pack(side="right")

        # Abort
        f2 = tk.Frame(frame, bg=self.app.bg_panel)
        f2.pack(fill="x", pady=6)
        tk.Label(f2, text="Abort Test Script:", bg=self.app.bg_panel, fg=self.app.fg_light, font=("Segoe UI", 9, "bold")).pack(side="left")
        self.ent_abort = tk.Entry(f2, bg="#151518", fg=self.app.fg_light, insertbackground=self.app.fg_light, bd=1, highlightbackground="#2c2c35", width=16, font=("Segoe UI", 9, "bold"))
        self.ent_abort.pack(side="right")

        # Mode
        f3 = tk.Frame(frame, bg=self.app.bg_panel)
        f3.pack(fill="x", pady=6)
        tk.Label(f3, text="Toggle Overlay HUD:", bg=self.app.bg_panel, fg=self.app.fg_light, font=("Segoe UI", 9, "bold")).pack(side="left")
        self.ent_mode = tk.Entry(f3, bg="#151518", fg=self.app.fg_light, insertbackground=self.app.fg_light, bd=1, highlightbackground="#2c2c35", width=16, font=("Segoe UI", 9, "bold"))
        self.ent_mode.pack(side="right")

        btn_save = ttk.Button(frame, text="SAVE HOTKEYS BINDINGS", command=self.save_hotkeys)
        btn_save.pack(anchor="e", pady=(10, 0))

    def build_preferences_fields(self, frame):
        # Capture Var
        self.pref_record = tk.BooleanVar()
        chk_rec = tk.Checkbutton(
            frame, text="ENABLE LIVE VIDEO CAPTURE (SAVED TO DOCUMENTS)", 
            variable=self.pref_record, command=self.save_preferences,
            bg=self.app.bg_panel, fg=self.app.fg_light, selectcolor=self.app.bg_dark,
            activebackground=self.app.bg_panel, activeforeground=self.app.fg_light,
            font=("Segoe UI", 9, "bold")
        )
        chk_rec.pack(anchor="w", pady=4)

        # Dev Build Var
        self.pref_dev = tk.BooleanVar()
        chk_dev = tk.Checkbutton(
            frame, text="SCAN GAME ENGINE PLAYER.LOG TRACES FOR FATAL EXCEPTIONS", 
            variable=self.pref_dev, command=self.save_preferences,
            bg=self.app.bg_panel, fg=self.app.fg_light, selectcolor=self.app.bg_dark,
            activebackground=self.app.bg_panel, activeforeground=self.app.fg_light,
            font=("Segoe UI", 9, "bold")
        )
        chk_dev.pack(anchor="w", pady=4)
        # Auto Sync UI Var
        self.pref_autosync = tk.BooleanVar()
        chk_sync = tk.Checkbutton(
            frame, text="ENABLE AUTO-SYNC GAME WINDOW UI POSITIONS", 
            variable=self.pref_autosync, command=self.save_preferences,
            bg=self.app.bg_panel, fg=self.app.fg_light, selectcolor=self.app.bg_dark,
            activebackground=self.app.bg_panel, activeforeground=self.app.fg_light,
            font=("Segoe UI", 9, "bold")
        )
        chk_sync.pack(anchor="w", pady=4)

        # Resolution Selection
        f_res = tk.Frame(frame, bg=self.app.bg_panel)
        f_res.pack(fill="x", pady=6)
        tk.Label(f_res, text="TEST WINDOW RESOLUTION:", bg=self.app.bg_panel, fg=self.app.fg_light, font=("Segoe UI", 9, "bold")).pack(side="left")
        self.pref_resolution = ttk.Combobox(
            f_res, values=["960x540", "1024x576", "1280x720", "1366x768", "1600x900"],
            state="readonly", width=12, font=("Segoe UI", 9, "bold")
        )
        self.pref_resolution.pack(side="right")
        self.pref_resolution.bind("<<ComboboxSelected>>", lambda e: self.save_preferences())

    def build_profiles_fields(self, frame):
        tk.Label(
            frame, text="Edit or add game directories, log output targets, and save paths database.",
            fg="#9ca3af", bg=self.app.bg_panel, font=("Segoe UI", 9, "bold")
        ).pack(anchor="w", pady=(0, 10))

        btn = ttk.Button(frame, text="LAUNCH PROFILES MANAGER...", command=self.app.manage_game_profiles)
        btn.pack(anchor="w")

    def load_settings_values(self):
        # Shortcuts entries
        self.ent_pause.delete(0, tk.END)
        self.ent_pause.insert(0, self.app.config.hotkeys.get("pause", "<Control-p>"))
        self.ent_abort.delete(0, tk.END)
        self.ent_abort.insert(0, self.app.config.hotkeys.get("abort", "<Control-q>"))
        self.ent_mode.delete(0, tk.END)
        self.ent_mode.insert(0, self.app.config.hotkeys.get("toggle_mode", "<Control-o>"))

        self.pref_record.set(self.app.config.record_test)
        self.pref_dev.set(self.app.config.dev_build_mode)
        self.pref_autosync.set(self.app.config.auto_sync_ui)
        self.pref_resolution.set(f"{self.app.config.game_width}x{self.app.config.game_height}")

    def save_hotkeys(self):
        p = self.ent_pause.get().strip()
        a = self.ent_abort.get().strip()
        m = self.ent_mode.get().strip()
        
        if not p or not a or not m:
            messagebox.showerror("Error", "Keyboard shortcuts cannot be empty!")
            return
            
        self.app.config.hotkeys["pause"] = p
        self.app.config.hotkeys["abort"] = a
        self.app.config.hotkeys["toggle_mode"] = m
        self.app.config.save()
        self.app.bind_hotkeys()
        
        messagebox.showinfo("Success", "Custom keyboard remapping saved successfully!")
        self.app.log_message("SYSTEM", "INFO", "Remapped shortcuts.")

    def save_preferences(self):
        self.app.config.record_test = self.pref_record.get()
        self.app.config.dev_build_mode = self.pref_dev.get()
        self.app.config.auto_sync_ui = self.pref_autosync.get()
        
        res_str = self.pref_resolution.get()
        if "x" in res_str:
            w_s, h_s = res_str.split("x")
            self.app.config.game_width = int(w_s)
            self.app.config.game_height = int(h_s)
            
        self.app.config.save()
        
        # Synchronize details page variables if they exist
        if hasattr(self.app, 'record_var'):
            self.app.record_var.set(self.app.config.record_test)
        if hasattr(self.app, 'dev_build_var'):
            self.app.dev_build_var.set(self.app.config.dev_build_mode)
        if hasattr(self.app, 'autosync_var'):
            self.app.autosync_var.set(self.app.config.auto_sync_ui)
        if hasattr(self.app, 'resolution_var'):
            self.app.resolution_var.set(res_str)

