import tkinter as tk
from tkinter import ttk
import time

def create_center_panel(app, parent):
    center_panel = parent
    
    # 1. Header Info Section (Permanently pinned at top)
    header_frame = tk.Frame(center_panel, bg=app.bg_panel, pady=0)
    header_frame.pack(fill="x", pady=(0, 5))
    
    title_lbl = tk.Label(header_frame, text="// SALAVAN TEST RUNNER HUD", fg=app.accent_glow, bg=app.bg_panel, font=("Segoe UI", 12, "bold"))
    title_lbl.pack(anchor="w")
    
    status_box = tk.Frame(header_frame, bg=app.bg_panel)
    status_box.pack(anchor="w", pady=(5, 0))
    
    app.status_dot = tk.Label(status_box, text="●", fg=app.alert_yellow, bg=app.bg_panel, font=("Segoe UI", 12, "bold"))
    app.status_dot.pack(side="left")
    
    app.stage_lbl = tk.Label(status_box, text="SYSTEM STATUS: IDLE", fg=app.fg_light, bg=app.bg_panel, font=("Segoe UI", 9, "bold"))
    app.stage_lbl.pack(side="left", padx=5)
    
    # 2. Inner Middle Tabs Navigator
    tabs_header = tk.Frame(center_panel, bg=app.bg_dark)
    tabs_header.pack(fill="x", pady=(5, 5))
    
    # Configure equal-width columns for tabs
    tabs_header.grid_columnconfigure(0, weight=1)
    tabs_header.grid_columnconfigure(1, weight=1)
    tabs_header.grid_columnconfigure(2, weight=1)
    tabs_header.grid_columnconfigure(3, weight=1)
    tabs_header.grid_columnconfigure(4, weight=1)
    
    app.tab_buttons = {}
    tab_ids = [
        ("setup", "⚙️ SETUP"),
        ("live", "📺 LIVE VIEW"),
        ("scenario", "📋 SCENARIO"),
        ("ui", "📍 UI COORDS"),
        ("logs", "📄 LOGS"),
        ("specs", "📟 SPECS"),
        ("media", "🎥 MEDIA"),
        ("mappings", "🗺️ MAPPINGS")
    ]
    
    # Tab content container
    app.tab_content_container = tk.Frame(center_panel, bg=app.bg_panel)
    app.tab_content_container.pack(fill="both", expand=True)
    
    app.tab_live = tk.Frame(app.tab_content_container, bg=app.bg_panel)
    app.tab_setup = tk.Frame(app.tab_content_container, bg=app.bg_panel)
    app.tab_scenario = tk.Frame(app.tab_content_container, bg=app.bg_panel)
    app.tab_ui = tk.Frame(app.tab_content_container, bg=app.bg_panel)
    app.tab_logs = tk.Frame(app.tab_content_container, bg=app.bg_panel)
    app.tab_specs = tk.Frame(app.tab_content_container, bg=app.bg_panel)
    app.tab_media = tk.Frame(app.tab_content_container, bg=app.bg_panel)
    app.tab_mappings = tk.Frame(app.tab_content_container, bg=app.bg_panel)
    
    app.active_mid_tab = "setup"
    
    def switch_tab(tab_id):
        app.active_mid_tab = tab_id
        
        # Hide all tab frames
        app.tab_live.pack_forget()
        app.tab_setup.pack_forget()
        app.tab_scenario.pack_forget()
        app.tab_ui.pack_forget()
        app.tab_logs.pack_forget()
        app.tab_specs.pack_forget()
        app.tab_media.pack_forget()
        app.tab_mappings.pack_forget()
        
        # Show active tab frame
        if tab_id == "live":
            app.tab_live.pack(fill="both", expand=True)
        elif tab_id == "setup":
            app.tab_setup.pack(fill="both", expand=True)
        elif tab_id == "scenario":
            app.tab_scenario.pack(fill="both", expand=True)
            if hasattr(app, 'refresh_locations_view'):
                app.refresh_locations_view()
        elif tab_id == "ui":
            app.tab_ui.pack(fill="both", expand=True)
            if hasattr(app, 'refresh_locations_view'):
                app.refresh_locations_view()
        elif tab_id == "logs":
            app.tab_logs.pack(fill="both", expand=True)
        elif tab_id == "specs":
            app.tab_specs.pack(fill="both", expand=True)
        elif tab_id == "media":
            app.tab_media.pack(fill="both", expand=True)
        elif tab_id == "mappings":
            app.tab_mappings.pack(fill="both", expand=True)
            
        # Update tab buttons selection styling
        for tid, btn in app.tab_buttons.items():
            if tid == tab_id:
                btn.config(bg="#242428", fg=app.accent_glow)
            else:
                btn.config(bg="#101012", fg="#9ca3af")
                
    app.switch_mid_tab = switch_tab

    for idx, (tab_id, text) in enumerate(tab_ids):
        btn = tk.Button(
            tabs_header, text=text, command=lambda t=tab_id: switch_tab(t),
            bg="#101012", fg="#9ca3af", activebackground="#242428",
            activeforeground=app.accent_glow, bd=0, relief="flat",
            font=("Segoe UI", 9, "bold"), pady=8
        )
        row = idx // 4
        col = idx % 4
        btn.grid(row=row, column=col, sticky="ew", padx=1, pady=1)
        app.tab_buttons[tab_id] = btn
        
        # Hover effect
        def make_tab_hover(b, tid):
            b.bind("<Enter>", lambda e, button=b, t_id=tid: button.config(fg=app.accent_glow, bg="#202024") if app.active_mid_tab != t_id else None)
            b.bind("<Leave>", lambda e, button=b, t_id=tid: button.config(fg="#9ca3af", bg="#101012") if app.active_mid_tab != t_id else None)
        make_tab_hover(btn, tab_id)
        app.add_tooltip(btn, f"Switch view to {text}")

    # ==========================================
    # TAB 1: LIVE VIEWPORT
    # ==========================================
    # 1a. Action buttons control strip
    ctrl_border = tk.Frame(app.tab_live, bg=app.accent_dim, bd=1)
    ctrl_border.pack(fill="x", pady=(0, 5))
    ctrl_frame = tk.Frame(ctrl_border, bg=app.bg_panel, padx=8, pady=8)
    ctrl_frame.pack(fill="both")
    
    # Initialize with default tab
    switch_tab("setup")

    # Action Buttons
    app.btn_clear_save = ttk.Button(ctrl_frame, text="PURGE SAVE", command=app.manual_clear_save)
    app.btn_clear_save.grid(row=0, column=0, padx=2, pady=3, sticky="ew")
    app.add_tooltip(app.btn_clear_save, "Purge local cache and game save directory database")
    
    app.btn_launch = ttk.Button(ctrl_frame, text="BOOT GAME", command=app.manual_launch_game)
    app.btn_launch.grid(row=0, column=1, padx=2, pady=3, sticky="ew")
    app.add_tooltip(app.btn_launch, "Boot configured game build executable")
    
    app.btn_capture = ttk.Button(ctrl_frame, text="COORDS TOOL", command=app.manual_capture_template)
    app.btn_capture.grid(row=0, column=2, padx=2, pady=3, sticky="ew")
    app.add_tooltip(app.btn_capture, "Configure coordinates and size parameters for a UI element mapping")
    
    ctrl_frame.columnconfigure(0, weight=1)
    ctrl_frame.columnconfigure(1, weight=1)
    ctrl_frame.columnconfigure(2, weight=1)
    
    # 1b. OBS-style video preview bounds
    app.preview_border = tk.Frame(app.tab_live, bg=app.accent_dim, bd=1)
    app.preview_border.pack(pady=5, fill="both", expand=True)
    preview_frame = tk.Frame(app.preview_border, bg=app.bg_panel, padx=5, pady=5)
    preview_frame.pack(fill="both", expand=True)
    
    app.preview_lbl = tk.Label(preview_frame, bg="#050508")
    app.preview_lbl.pack(fill="both", expand=True)
    
    # 1c. Diagnostic log messages console
    console_border = tk.Frame(app.tab_live, bg=app.accent_dim, bd=1)
    console_border.pack(fill="x", pady=5)
    console_frame = tk.Frame(console_border, bg="#050508", padx=5, pady=5)
    console_frame.pack(fill="both")
    
    app.console_text = tk.Text(console_frame, bg="#050508", bd=0, height=3, wrap="word", font=("Consolas", 10, "bold"))
    app.console_text.pack(fill="both", expand=True)
    app.console_text.tag_config("pass", foreground=app.success_glow)
    app.console_text.tag_config("fail", foreground=app.fail_glow)
    app.console_text.tag_config("info", foreground="#00b4d8")
    app.console_text.config(state="disabled")

    # ==========================================
    # TAB 2: OPERATION SETUP
    # ==========================================
    app.setup_border = tk.Frame(app.tab_setup, bg=app.accent_dim, bd=1)
    app.setup_border.pack(fill="both", expand=True, pady=5)
    setup_frame = tk.Frame(app.setup_border, bg=app.bg_panel, padx=10, pady=10)
    setup_frame.pack(fill="both", expand=True)
    
    game_lbl = tk.Label(setup_frame, text="ACTIVE GAME PROFILE:", fg=app.accent_glow, bg=app.bg_panel, font=("Segoe UI", 9, "bold"))
    game_lbl.pack(anchor="w", pady=(0, 2))
    
    game_select_frame = tk.Frame(setup_frame, bg=app.bg_panel)
    game_select_frame.pack(fill="x", pady=(0, 10))
    
    app.game_profile_var = tk.StringVar()
    app.game_profile_combo = ttk.Combobox(game_select_frame, textvariable=app.game_profile_var, state="readonly", font=("Segoe UI", 9, "bold"))
    app.game_profile_combo.pack(side="left", fill="x", expand=True, padx=(0, 5))
    app.add_tooltip(app.game_profile_combo, "Select active game profile catalog")
    
    app.btn_manage_games = ttk.Button(game_select_frame, text="MANAGE...", command=app.manage_game_profiles, width=10)
    app.btn_manage_games.pack(side="right")
    app.add_tooltip(app.btn_manage_games, "Add, edit, or delete game profiles configuration")
    
    path_lbl = tk.Label(setup_frame, text="TARGET FILE PATH:", fg=app.accent_glow, bg=app.bg_panel, font=("Segoe UI", 9, "bold"))
    path_lbl.pack(anchor="w")
    
    path_select_frame = tk.Frame(setup_frame, bg=app.bg_panel)
    path_select_frame.pack(fill="x", pady=5)
    
    app.path_entry_var = tk.StringVar(value=app.config.game_exe_path)
    app.path_entry = tk.Entry(
        path_select_frame, textvariable=app.path_entry_var, state="readonly", 
        font=("Segoe UI", 10, "bold"), bg="#151518", fg=app.fg_light,
        readonlybackground="#151518", bd=1, relief="flat", highlightbackground=app.accent_dim, highlightthickness=1
    )
    app.path_entry.pack(side="left", fill="x", expand=True, ipady=3, padx=(0, 5))
    app.add_tooltip(app.path_entry, "Currently selected build path")
    
    browse_btn = ttk.Button(path_select_frame, text="BROWSE...", command=app.browse_game_exe, width=10)
    browse_btn.pack(side="right")
    app.add_tooltip(browse_btn, "Browse files to select game build executable")
    
    app.record_var = tk.BooleanVar(value=app.config.record_test)
    def on_record_toggle():
        app.config.record_test = app.record_var.get()
        app.config.save()
        if hasattr(app, 'settings_page') and hasattr(app.settings_page, 'pref_record'):
            app.settings_page.pref_record.set(app.config.record_test)
        
    record_chk = tk.Checkbutton(setup_frame, text="CAPTURE SYSTEM OUTPUT (LIVE VIDEO)", variable=app.record_var, command=on_record_toggle, bg=app.bg_panel, fg=app.fg_light, selectcolor=app.bg_dark, activebackground=app.bg_panel, activeforeground=app.fg_light, font=("Segoe UI", 9, "bold"))
    record_chk.pack(anchor="w", pady=(2, 0))
    app.add_tooltip(record_chk, "Toggle screen recording for error proof and diagnostics")

    app.dev_build_var = tk.BooleanVar(value=app.config.dev_build_mode)
    def on_dev_build_toggle():
        app.config.dev_build_mode = app.dev_build_var.get()
        app.config.save()
        if hasattr(app, 'settings_page') and hasattr(app.settings_page, 'pref_dev'):
            app.settings_page.pref_dev.set(app.config.dev_build_mode)
        
    dev_build_chk = tk.Checkbutton(setup_frame, text="DEVELOPMENT BUILD (SCAN ENGINE LOGS)", variable=app.dev_build_var, command=on_dev_build_toggle, bg=app.bg_panel, fg=app.fg_light, selectcolor=app.bg_dark, activebackground=app.bg_panel, activeforeground=app.fg_light, font=("Segoe UI", 9, "bold"))
    dev_build_chk.pack(anchor="w", pady=(2, 0))

    app.hook_unity_var = tk.BooleanVar(value=app.config.hook_unity_editor)
    def on_hook_unity_toggle():
        app.config.hook_unity_editor = app.hook_unity_var.get()
        app.config.save()
        if hasattr(app, 'settings_page') and hasattr(app.settings_page, 'pref_hook_unity'):
            app.settings_page.pref_hook_unity.set(app.config.hook_unity_editor)
        
    hook_unity_chk = tk.Checkbutton(setup_frame, text="HOOK TO UNITY EDITOR (FALLBACK MATCH)", variable=app.hook_unity_var, command=on_hook_unity_toggle, bg=app.bg_panel, fg=app.fg_light, selectcolor=app.bg_dark, activebackground=app.bg_panel, activeforeground=app.fg_light, font=("Segoe UI", 9, "bold"))
    hook_unity_chk.pack(anchor="w", pady=(2, 0))
    app.add_tooltip(hook_unity_chk, "Allows falling back to active Unity Editor windows if the game executable is not running")

    app.autosync_var = tk.BooleanVar(value=app.config.auto_sync_ui)
    def on_autosync_toggle():
        app.config.auto_sync_ui = app.autosync_var.get()
        app.config.save()
        if hasattr(app, 'settings_page') and hasattr(app.settings_page, 'pref_autosync'):
            app.settings_page.pref_autosync.set(app.config.auto_sync_ui)
            
    autosync_chk = tk.Checkbutton(setup_frame, text="AUTO-SYNC GAME WINDOW UI POSITIONS", variable=app.autosync_var, command=on_autosync_toggle, bg=app.bg_panel, fg=app.fg_light, selectcolor=app.bg_dark, activebackground=app.bg_panel, activeforeground=app.fg_light, font=("Segoe UI", 9, "bold"))
    autosync_chk.pack(anchor="w", pady=(2, 0))

    # Game Resolution Combobox
    f_res = tk.Frame(setup_frame, bg=app.bg_panel)
    f_res.pack(fill="x", pady=10)
    tk.Label(f_res, text="TEST WINDOW RESOLUTION:", bg=app.bg_panel, fg=app.fg_light, font=("Segoe UI", 9, "bold")).pack(side="left")
    
    app.resolution_var = tk.StringVar(value=f"{app.config.game_width}x{app.config.game_height}" if app.config.game_width > 0 else "Fullscreen")
    def on_resolution_change(e):
        res_str = app.resolution_var.get()
        if "x" in res_str:
            w_s, h_s = res_str.split("x")
            app.config.game_width = int(w_s)
            app.config.game_height = int(h_s)
        elif res_str == "Fullscreen":
            app.config.game_width = -1
            app.config.game_height = -1
        app.config.save()
        if hasattr(app, 'settings_page') and hasattr(app.settings_page, 'pref_resolution'):
            app.settings_page.pref_resolution.set(res_str)
            
    resolution_combo = ttk.Combobox(
        f_res, textvariable=app.resolution_var, values=["960x540", "1024x576", "1280x720", "1366x768", "1600x900", "Fullscreen"],
        state="readonly", width=12, font=("Segoe UI", 9, "bold")
    )
    resolution_combo.pack(side="right")
    resolution_combo.bind("<<ComboboxSelected>>", on_resolution_change)

    # ==========================================
    # TAB 3: SYSTEM SPECS
    # ==========================================
    app.specs_border = tk.Frame(app.tab_specs, bg=app.accent_dim, bd=1)
    app.specs_border.pack(fill="both", expand=True, pady=5)
    specs_frame = tk.Frame(app.specs_border, bg=app.bg_panel, padx=15, pady=15)
    specs_frame.pack(fill="both", expand=True)
    
    lbl_specs_title = tk.Label(specs_frame, text="// SYSTEM HARDWARE SPECS", fg=app.accent_glow, bg=app.bg_panel, font=("Segoe UI", 10, "bold"))
    lbl_specs_title.pack(anchor="w", pady=(0, 10))
    
    specs = app.get_system_specs()
    def add_spec_row(label, value):
        row = tk.Frame(specs_frame, bg=app.bg_panel)
        row.pack(fill="x", pady=4)
        tk.Label(row, text=label, fg=app.fg_light, bg=app.bg_panel, font=("Segoe UI", 9, "bold")).pack(side="left")
        tk.Label(row, text=value, fg=app.success_glow, bg=app.bg_panel, font=("Segoe UI", 9, "bold")).pack(side="right")
        
    add_spec_row("Operating System:", specs["os"])
    add_spec_row("Processor (CPU):", specs["cpu"])
    add_spec_row("System Memory (RAM):", specs["ram"])
    add_spec_row("Graphics Controller (GPU):", specs["gpu"])

    # ==========================================
    # TAB 4: RECORDINGS & MEDIA
    # ==========================================
    media_border = tk.Frame(app.tab_media, bg=app.accent_dim, bd=1)
    media_border.pack(fill="both", expand=True, pady=5)
    media_frame = tk.Frame(media_border, bg=app.bg_panel, padx=15, pady=15)
    media_frame.pack(fill="both", expand=True)
    
    lbl_media_title = tk.Label(media_frame, text="// CAPTURE & RECORDINGS HUB", fg=app.accent_glow, bg=app.bg_panel, font=("Segoe UI", 10, "bold"))
    lbl_media_title.pack(anchor="w", pady=(0, 2))
    
    lbl_media_desc = tk.Label(media_frame, text="Configure video screen captures, compression, and manual recording output.", fg="#9ca3af", bg=app.bg_panel, font=("Segoe UI", 8))
    lbl_media_desc.pack(anchor="w", pady=(0, 15))
    
    # Codec Select row
    f_codec = tk.Frame(media_frame, bg=app.bg_panel)
    f_codec.pack(fill="x", pady=6)
    tk.Label(f_codec, text="Recording Codec Format:", bg=app.bg_panel, fg=app.fg_light, font=("Segoe UI", 9, "bold")).pack(side="left")
    
    app.recording_codec_var = tk.StringVar(value="Google VP8 (.IVF) - Recommended")
    codec_combo = ttk.Combobox(
        f_codec, textvariable=app.recording_codec_var, 
        values=["Google VP8 (.IVF) - Recommended", "Raw uncompressed (.AVI)", "MPEG-4 (.MP4)"],
        state="readonly", width=30, font=("Segoe UI", 9, "bold")
    )
    codec_combo.pack(side="right")
    
    # FPS Select row
    f_fps = tk.Frame(media_frame, bg=app.bg_panel)
    f_fps.pack(fill="x", pady=6)
    tk.Label(f_fps, text="Framerate Capture Preset:", bg=app.bg_panel, fg=app.fg_light, font=("Segoe UI", 9, "bold")).pack(side="left")
    
    app.recording_fps_var = tk.StringVar(value="30 Frames Per Second (Standard)")
    fps_combo = ttk.Combobox(
        f_fps, textvariable=app.recording_fps_var, 
        values=["30 Frames Per Second (Standard)", "60 Frames Per Second (High Refresh)", "15 Frames Per Second (Low CPU Overhead)"],
        state="readonly", width=30, font=("Segoe UI", 9, "bold")
    )
    fps_combo.pack(side="right")
    
    # Separator
    sep = tk.Frame(media_frame, bg=app.accent_dim, height=1)
    sep.pack(fill="x", pady=15)
    
    # Trigger button
    app.btn_trigger_record = tk.Button(
        media_frame, text="🔴 TRIGGER MANUAL RECORDING", command=app.toggle_manual_recording,
        bg="#2c2c35", fg=app.success_glow, activebackground=app.accent_glow,
        activeforeground="#101012", bd=0, relief="flat",
        font=("Segoe UI", 9, "bold"), pady=8
    )
    app.btn_trigger_record.pack(fill="x", pady=5)
    
    def make_btn_hover(b):
        b.bind("<Enter>", lambda e: b.config(bg=app.accent_glow, fg="#101012") if "STOP" not in b["text"] else None)
        b.bind("<Leave>", lambda e: b.config(bg="#2c2c35", fg=app.success_glow) if "STOP" not in b["text"] else None)
    make_btn_hover(app.btn_trigger_record)

    # ==========================================
    # TAB 5: SCENARIO STEPS & ACTIONS
    # ==========================================
    left_loc_frame = tk.Frame(app.tab_scenario, bg=app.bg_panel, padx=10, pady=10)
    left_loc_frame.pack(fill="both", expand=True)
    
    tk.Label(left_loc_frame, text="📋 ACTIVE SCENARIO STEPS & ACTIONS", fg=app.accent_glow, bg=app.bg_panel, font=("Segoe UI", 9, "bold")).pack(anchor="w", pady=(0, 5))
    
    loc_scroll_y = ttk.Scrollbar(left_loc_frame, orient="vertical")
    loc_scroll_y.pack(side="right", fill="y")
    loc_scroll_x = ttk.Scrollbar(left_loc_frame, orient="horizontal")
    loc_scroll_x.pack(side="bottom", fill="x")
    
    app.scenario_steps_tree = ttk.Treeview(
        left_loc_frame, columns=("Action", "Target / Info", "Coords (1280x720)", "Text / Size"), 
        show="tree headings", yscrollcommand=loc_scroll_y.set, xscrollcommand=loc_scroll_x.set
    )
    app.scenario_steps_tree.heading("#0", text="Step / Action Type", anchor="w")
    app.scenario_steps_tree.heading("Action", text="Lua Action", anchor="w")
    app.scenario_steps_tree.heading("Target / Info", text="Target / Argument", anchor="w")
    app.scenario_steps_tree.heading("Coords (1280x720)", text="Resolved Coords", anchor="w")
    app.scenario_steps_tree.heading("Text / Size", text="UI Info (Text/Size)", anchor="w")
    
    app.scenario_steps_tree.column("#0", width=160, stretch=True)
    app.scenario_steps_tree.column("Action", width=90, stretch=True)
    app.scenario_steps_tree.column("Target / Info", width=120, stretch=True)
    app.scenario_steps_tree.column("Coords (1280x720)", width=110, stretch=True)
    app.scenario_steps_tree.column("Text / Size", width=140, stretch=True)
    
    app.scenario_steps_tree.pack(fill="both", expand=True)
    loc_scroll_y.config(command=app.scenario_steps_tree.yview)
    loc_scroll_x.config(command=app.scenario_steps_tree.xview)
    
    # ==========================================
    # TAB 6: UI BUTTON COORDINATES
    # ==========================================
    loc_toolbar = tk.Frame(app.tab_ui, bg=app.bg_panel)
    loc_toolbar.pack(fill="x", padx=10, pady=(5, 0))
    
    app.show_screen_coords_var = tk.BooleanVar(value=False)
    def on_coords_toggle():
        app.refresh_locations_view()
        
    coords_chk = tk.Checkbutton(
        loc_toolbar, text="SHOW ABSOLUTE MONITOR SCREEN COORDINATES", 
        variable=app.show_screen_coords_var, command=on_coords_toggle, 
        bg=app.bg_panel, fg=app.fg_light, selectcolor=app.bg_dark, 
        activebackground=app.bg_panel, activeforeground=app.fg_light, 
        font=("Segoe UI", 8, "bold")
    )
    coords_chk.pack(side="left")
    app.add_tooltip(coords_chk, "Toggle displaying coordinates as absolute screen pixels rather than game-relative coordinates (1280x720 reference)")

    right_loc_frame = tk.Frame(app.tab_ui, bg=app.bg_panel, padx=10, pady=10)
    right_loc_frame.pack(fill="both", expand=True)
    
    tk.Label(right_loc_frame, text="📍 LIVE UNITY UI BUTTON COORDINATES", fg=app.accent_glow, bg=app.bg_panel, font=("Segoe UI", 9, "bold")).pack(anchor="w", pady=(0, 5))
    
    live_scroll_y = ttk.Scrollbar(right_loc_frame, orient="vertical")
    live_scroll_y.pack(side="right", fill="y")
    
    app.live_buttons_tree = ttk.Treeview(
        right_loc_frame, columns=("Name", "Text", "X Coord", "Y Coord", "Width", "Height"), 
        show="headings", yscrollcommand=live_scroll_y.set
    )
    app.live_buttons_tree.heading("Name", text="UI Button Name", anchor="w")
    app.live_buttons_tree.heading("Text", text="Button Text", anchor="w")
    app.live_buttons_tree.heading("X Coord", text="X", anchor="w")
    app.live_buttons_tree.heading("Y Coord", text="Y", anchor="w")
    app.live_buttons_tree.heading("Width", text="Width", anchor="w")
    app.live_buttons_tree.heading("Height", text="Height", anchor="w")
    
    app.live_buttons_tree.column("Name", width=140, stretch=True)
    app.live_buttons_tree.column("Text", width=120, stretch=True)
    app.live_buttons_tree.column("X Coord", width=50, stretch=True)
    app.live_buttons_tree.column("Y Coord", width=50, stretch=True)
    app.live_buttons_tree.column("Width", width=60, stretch=True)
    app.live_buttons_tree.column("Height", width=60, stretch=True)
    
    app.live_buttons_tree.pack(fill="both", expand=True)
    live_scroll_y.config(command=app.live_buttons_tree.yview)

    # ==========================================
    # TAB 7: FULL LOG READER
    # ==========================================
    logs_frame = tk.Frame(app.tab_logs, bg=app.bg_panel, padx=10, pady=10)
    logs_frame.pack(fill="both", expand=True)
    
    tk.Label(logs_frame, text="📄 DIAGNOSTIC LOGS READER", fg=app.accent_glow, bg=app.bg_panel, font=("Segoe UI", 9, "bold")).pack(anchor="w", pady=(0, 5))
    
    log_scroll_y = ttk.Scrollbar(logs_frame, orient="vertical")
    log_scroll_y.pack(side="right", fill="y")
    
    app.full_logs_text = tk.Text(logs_frame, bg="#050508", bd=0, wrap="word", font=("Consolas", 10, "bold"), yscrollcommand=log_scroll_y.set)
    app.full_logs_text.pack(fill="both", expand=True)
    app.full_logs_text.tag_config("pass", foreground=app.success_glow)
    app.full_logs_text.tag_config("fail", foreground=app.fail_glow)
    app.full_logs_text.tag_config("info", foreground="#00b4d8")
    app.full_logs_text.config(state="disabled")
    
    log_scroll_y.config(command=app.full_logs_text.yview)

    # ==========================================
    # TAB 8: MAPPINGS EDITOR
    # ==========================================
    mappings_frame = tk.Frame(app.tab_mappings, bg=app.bg_panel, padx=10, pady=10)
    mappings_frame.pack(fill="both", expand=True)

    mappings_header = tk.Frame(mappings_frame, bg=app.bg_panel)
    mappings_header.pack(fill="x", pady=(0, 5))
    
    tk.Label(mappings_header, text="🗺️ JSON UI COORDINATES MAPPING", fg=app.accent_glow, bg=app.bg_panel, font=("Segoe UI", 9, "bold")).pack(side="left")

    import json
    from tkinter import filedialog
    
    app.ui_mappings = []
    app.mappings_has_entries_wrapper = True
    app.mappings_search_var = tk.StringVar()

    def load_mappings(path=None):
        if not path:
            path = filedialog.askopenfilename(filetypes=[("JSON Files", "*.json")])
            
        if path:
            try:
                with open(path, "r", encoding="utf-8") as f:
                    data = json.load(f)
                    if "entries" in data:
                        app.ui_mappings = data["entries"]
                        app.mappings_has_entries_wrapper = True
                    else:
                        app.ui_mappings = data
                        app.mappings_has_entries_wrapper = False
                        
                # Save path to config
                active_game = app.config.get_active_game()
                if active_game:
                    active_game["ui_mapping_path"] = path
                    app.config.save()
                    
                refresh_mappings_tree()
            except Exception as e:
                print(f"Error loading mappings: {e}")

    def save_mappings():
        path = filedialog.asksaveasfilename(defaultextension=".json", filetypes=[("JSON Files", "*.json")])
        if path:
            try:
                with open(path, "w", encoding="utf-8") as f:
                    if getattr(app, "mappings_has_entries_wrapper", True):
                        json.dump({"entries": app.ui_mappings}, f, indent=4)
                    else:
                        json.dump(app.ui_mappings, f, indent=4)
            except Exception as e:
                print(f"Error saving mappings: {e}")

    # Search Widget
    tk.Label(mappings_header, text="  🔍 Search:", fg=app.fg_light, bg=app.bg_panel, font=("Segoe UI", 9)).pack(side="left")
    entry_search = tk.Entry(mappings_header, textvariable=app.mappings_search_var, bg="#2c2c35", fg=app.fg_light, insertbackground=app.fg_light, bd=1, relief="flat", font=("Segoe UI", 9), width=30)
    entry_search.pack(side="left", padx=5, ipady=1)

    btn_import = ttk.Button(mappings_header, text="IMPORT JSON", command=load_mappings)
    btn_import.pack(side="right", padx=2)
    btn_export = ttk.Button(mappings_header, text="EXPORT JSON", command=save_mappings)
    btn_export.pack(side="right", padx=2)

    map_scroll_y = ttk.Scrollbar(mappings_frame, orient="vertical")
    map_scroll_y.pack(side="right", fill="y")

    app.mappings_tree = ttk.Treeview(
        mappings_frame, columns=("Path", "Type", "X", "Y"),
        show="headings", yscrollcommand=map_scroll_y.set
    )
    app.mappings_tree.heading("Path", text="Object Path", anchor="w")
    app.mappings_tree.heading("Type", text="Component Type", anchor="w")
    app.mappings_tree.heading("X", text="X Coord", anchor="w")
    app.mappings_tree.heading("Y", text="Y Coord", anchor="w")

    app.mappings_tree.column("Path", width=250, stretch=True)
    app.mappings_tree.column("Type", width=100, stretch=True)
    app.mappings_tree.column("X", width=60, stretch=True)
    app.mappings_tree.column("Y", width=60, stretch=True)

    app.mappings_tree.pack(fill="both", expand=True)
    map_scroll_y.config(command=app.mappings_tree.yview)

    def refresh_mappings_tree(*args):
        query = app.mappings_search_var.get().lower().strip() if hasattr(app, "mappings_search_var") else ""
        for row in app.mappings_tree.get_children():
            app.mappings_tree.delete(row)
        for entry in app.ui_mappings:
            path = entry.get("Path", "")
            comp_type = entry.get("Type", "")
            coords = entry.get("Coordinates", {})
            x = coords.get("x", 0)
            y = coords.get("y", 0)
            if not query or query in path.lower() or query in comp_type.lower():
                app.mappings_tree.insert("", "end", values=(path, comp_type, x, y))

    app.mappings_search_var.trace_add("write", refresh_mappings_tree)

    def on_mapping_double_click(event):
        selected_item = app.mappings_tree.selection()
        if not selected_item:
            return
        item_values = app.mappings_tree.item(selected_item[0], "values")
        if not item_values:
            return
        path, comp_type, x_val, y_val = item_values
        
        target_entry = None
        for entry in app.ui_mappings:
            if entry.get("Path") == path:
                target_entry = entry
                break
                
        if not target_entry:
            return
            
        coords = target_entry.get("Coordinates", {})
        x = coords.get("x", 0)
        y = coords.get("y", 0)
        
        initial_coords = {
            "x": int(float(x)),
            "y": int(float(y)),
            "width": 100,
            "height": 50,
            "resolution": f"{app.config.game_width}x{app.config.game_height}" if app.config.game_width > 0 and app.config.game_height > 0 else "1280x720"
        }
        
        def on_save(new_x, new_y, new_w, new_h, resolution):
            target_entry["Coordinates"] = {
                "x": float(new_x),
                "y": float(new_y)
            }
            refresh_mappings_tree()
            
            active_game = app.config.get_active_game()
            if active_game and active_game.get("ui_mapping_path"):
                mapping_path = active_game["ui_mapping_path"]
                try:
                    import os
                    os.makedirs(os.path.dirname(mapping_path), exist_ok=True)
                    with open(mapping_path, "w", encoding="utf-8") as f:
                        if getattr(app, "mappings_has_entries_wrapper", True):
                            json.dump({"entries": app.ui_mappings}, f, indent=4)
                        else:
                            json.dump(app.ui_mappings, f, indent=4)
                    app.log_message("MAPPINGS", "INFO", f"Auto-saved coordinates for '{path}' to mappings file.")
                except Exception as e:
                    print(f"Failed to auto-save mappings: {e}")
                    
        import threading
        app.show_coordinate_dialog(path, threading.Event(), on_save_callback=on_save, initial_coords=initial_coords)

    app.mappings_tree.bind("<Double-1>", on_mapping_double_click)

    # Auto-load project mappings on startup
    active_game = app.config.get_active_game()
    if active_game and active_game.get("ui_mapping_path"):
        import os
        if os.path.exists(active_game["ui_mapping_path"]):
            load_mappings(active_game["ui_mapping_path"])

    # Initialize tab showing
    switch_tab("live")
