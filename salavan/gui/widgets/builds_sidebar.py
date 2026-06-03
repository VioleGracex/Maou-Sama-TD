import tkinter as tk
from tkinter import ttk
from gui.dialogs.add_build import show_add_build_dialog

def create_builds_sidebar(app, parent):
    # Right Sidebar Border Frame (Builds & Reports)
    app.right_sidebar_border = tk.Frame(parent, bg=app.accent_dim, bd=1)
    app.right_sidebar_border.pack(fill="both", expand=True, padx=(5, 15), pady=15)
    
    right_frame = tk.Frame(app.right_sidebar_border, bg=app.bg_panel, padx=10, pady=10)
    right_frame.pack(fill="both", expand=True)
    
    # Split sidebar vertically using a PanedWindow
    sidebar_pw = tk.PanedWindow(right_frame, orient=tk.VERTICAL, bg=app.bg_panel, bd=0, sashwidth=4, sashpad=2)
    sidebar_pw.pack(fill="both", expand=True)
    
    # --- PANE 1: GAME BUILDS DATABASE ---
    builds_pane = tk.Frame(sidebar_pw, bg=app.bg_panel)
    sidebar_pw.add(builds_pane, minsize=380, stretch="always")
    
    builds_header = tk.Frame(builds_pane, bg=app.bg_panel)
    builds_header.pack(fill="x", pady=(0, 5))
    
    builds_title = tk.Label(builds_header, text="// GAME BUILDS DATABASE", fg=app.accent_glow, bg=app.bg_panel, font=("Segoe UI", 10, "bold"))
    builds_title.pack(side="left")
    
    btn_close_builds = tk.Button(
        builds_header, text="✕", command=app.toggle_builds_panel,
        bg="#2c2c35", fg=app.fg_light, activebackground="#ff3b30",
        activeforeground="#ffffff", bd=0, padx=6, font=("Segoe UI", 8, "bold")
    )
    btn_close_builds.pack(side="right")
    
    # Hover binding
    def on_enter_close(e):
        btn_close_builds.config(bg="#ff3b30", fg="#ffffff")
    def on_leave_close(e):
        btn_close_builds.config(bg="#2c2c35", fg=app.fg_light)
    btn_close_builds.bind("<Enter>", on_enter_close)
    btn_close_builds.bind("<Leave>", on_leave_close)
    
    app.add_tooltip(btn_close_builds, "Collapse Builds Sidebar")
    
    builds_container = tk.Frame(builds_pane, bg=app.bg_panel)
    builds_container.pack(fill="both", expand=True)
    builds_container.pack_propagate(False)
    
    builds_scroll = ttk.Scrollbar(builds_container)
    builds_scroll.pack(side="right", fill="y")
    
    app.builds_tree = ttk.Treeview(builds_container, columns=("Title", "Ver", "Status", "Last Run"), show="headings", yscrollcommand=builds_scroll.set)
    app.builds_tree.heading("Title", text="GAME TITLE")
    app.builds_tree.heading("Ver", text="VERSION")
    app.builds_tree.heading("Status", text="STATUS")
    app.builds_tree.heading("Last Run", text="LAST RUN")
    
    app.builds_tree.column("Title", width=70, anchor="w")
    app.builds_tree.column("Ver", width=45, anchor="center")
    app.builds_tree.column("Status", width=55, anchor="center")
    app.builds_tree.column("Last Run", width=70, anchor="w")
    
    app.builds_tree.pack(fill="both", expand=True)
    builds_scroll.config(command=app.builds_tree.yview)
    app.builds_tree.bind("<Button-3>", app.show_builds_context_menu)
    
    # DB Buttons Panel
    db_btn_frame = tk.Frame(builds_pane, bg=app.bg_panel)
    db_btn_frame.pack(fill="x", pady=5)
    
    btn_add_db = ttk.Button(db_btn_frame, text="ADD BUILD", command=lambda: show_add_build_dialog(app), style="Sidebar.TButton")
    btn_add_db.grid(row=0, column=0, padx=2, pady=2, sticky="ew")
    app.add_tooltip(btn_add_db, "Add a new game build executable to the profile database")
    
    btn_del_db = ttk.Button(db_btn_frame, text="DELETE", command=app.delete_selected_build, style="Sidebar.TButton")
    btn_del_db.grid(row=0, column=1, padx=2, pady=2, sticky="ew")
    app.add_tooltip(btn_del_db, "Delete selected game build from database")
    
    btn_set_active = ttk.Button(db_btn_frame, text="SET ACTIVE", command=app.select_active_build, style="Sidebar.TButton")
    btn_set_active.grid(row=1, column=0, columnspan=2, padx=2, pady=2, sticky="ew")
    app.add_tooltip(btn_set_active, "Set selected build as the active runner path")
    
    db_btn_frame.columnconfigure(0, weight=1)
    db_btn_frame.columnconfigure(1, weight=1)

    # SELECTED BUILD STATUS Panel
    status_panel_border = tk.Frame(builds_pane, bg=app.accent_dim, bd=1)
    status_panel_border.pack(fill="x", pady=(5, 0))
    
    status_panel = tk.Frame(status_panel_border, bg=app.bg_panel, padx=8, pady=8)
    status_panel.pack(fill="both")
    
    lbl_selected_build = tk.Label(status_panel, text="NO BUILD SELECTED", fg=app.accent_glow, bg=app.bg_panel, font=("Segoe UI", 9, "bold"), anchor="w")
    lbl_selected_build.grid(row=0, column=0, columnspan=3, sticky="w", pady=(0, 6))
    app.lbl_selected_build = lbl_selected_build
    
    # Status select state variable on app
    app.status_selected_build_status = "Pending"
    
    btn_status_success = tk.Button(
        status_panel, text="SUCCESS", command=lambda: select_status("Success"),
        bg="#064e3b", fg=app.success_glow, activebackground=app.success_glow, activeforeground="#101012",
        bd=0, font=("Segoe UI", 8, "bold"), pady=4
    )
    btn_status_success.grid(row=1, column=0, padx=1, sticky="ew")
    app.add_tooltip(btn_status_success, "Mark selected build status as Success")
    
    btn_status_failed = tk.Button(
        status_panel, text="FAILED", command=lambda: select_status("Failed"),
        bg="#7f1d1d", fg=app.fail_glow, activebackground=app.fail_glow, activeforeground="#ffffff",
        bd=0, font=("Segoe UI", 8, "bold"), pady=4
    )
    btn_status_failed.grid(row=1, column=1, padx=1, sticky="ew")
    app.add_tooltip(btn_status_failed, "Mark selected build status as Failed")
    
    btn_status_pending = tk.Button(
        status_panel, text="PENDING", command=lambda: select_status("Pending"),
        bg="#222228", fg="#e2e2e5", activebackground="#3e3e4a", activeforeground="#ffffff",
        bd=0, font=("Segoe UI", 8, "bold"), pady=4
    )
    btn_status_pending.grid(row=1, column=2, padx=1, sticky="ew")
    app.add_tooltip(btn_status_pending, "Mark selected build status as Pending")
    
    status_panel.columnconfigure(0, weight=1)
    status_panel.columnconfigure(1, weight=1)
    status_panel.columnconfigure(2, weight=1)
    
    # Summary input
    summary_label = tk.Label(status_panel, text="SUMMARY:", fg=app.fg_light, bg=app.bg_panel, font=("Segoe UI", 8, "bold"))
    summary_label.grid(row=2, column=0, sticky="w", pady=(8, 4))
    
    status_summary_var = tk.StringVar()
    app.status_summary_var = status_summary_var
    
    status_summary_entry = tk.Entry(
        status_panel, textvariable=status_summary_var,
        font=("Segoe UI", 9, "bold"), bg="#151518", fg=app.fg_light,
        insertbackground=app.fg_light, bd=1, relief="flat",
        highlightbackground=app.accent_dim, highlightthickness=1
    )
    status_summary_entry.grid(row=2, column=1, columnspan=2, sticky="ew", pady=(8, 4))
    app.status_summary_entry = status_summary_entry
    app.add_tooltip(status_summary_entry, "Enter custom test summary (e.g. 3/5 Passed)")
    
    # Action buttons
    btn_save_status = ttk.Button(status_panel, text="SAVE STATUS", command=app.save_selected_build_status, style="Sidebar.TButton")
    btn_save_status.grid(row=3, column=0, columnspan=3, pady=(6, 0), sticky="ew")
    app.add_tooltip(btn_save_status, "Save selected status and notes to build database")
    
    btn_rerun_selected = ttk.Button(status_panel, text="RERUN TEST", command=app.rerun_selected_build, style="Sidebar.TButton")
    btn_rerun_selected.grid(row=4, column=0, columnspan=3, pady=(4, 0), sticky="ew")
    app.add_tooltip(btn_rerun_selected, "Set selected build active and rerun active scenario")
    
    # Functions for status buttons highlight selection
    def update_status_buttons_ui():
        status = app.status_selected_build_status
        if status == "Success":
            btn_status_success.config(bg=app.success_glow, fg="#101012")
        else:
            btn_status_success.config(bg="#064e3b", fg=app.success_glow)
            
        if status == "Failed":
            btn_status_failed.config(bg=app.fail_glow, fg="#ffffff")
        else:
            btn_status_failed.config(bg="#7f1d1d", fg=app.fail_glow)
            
        if status == "Pending":
            btn_status_pending.config(bg=app.accent_glow, fg="#101012")
        else:
            btn_status_pending.config(bg="#222228", fg="#e2e2e5")

    def select_status(status):
        app.status_selected_build_status = status
        update_status_buttons_ui()
        
    app.update_status_buttons_ui = update_status_buttons_ui
    
    # Hover for Success
    def on_enter_success(e):
        if app.status_selected_build_status != "Success":
            btn_status_success.config(bg="#24582a")
    def on_leave_success(e):
        if app.status_selected_build_status != "Success":
            btn_status_success.config(bg="#183a1c")
    btn_status_success.bind("<Enter>", on_enter_success)
    btn_status_success.bind("<Leave>", on_leave_success)
    
    # Hover for Failed
    def on_enter_failed(e):
        if app.status_selected_build_status != "Failed":
            btn_status_failed.config(bg="#6e1f28")
    def on_leave_failed(e):
        if app.status_selected_build_status != "Failed":
            btn_status_failed.config(bg="#4a151b")
    btn_status_failed.bind("<Enter>", on_enter_failed)
    btn_status_failed.bind("<Leave>", on_leave_failed)
    
    # Hover for Pending
    def on_enter_pending(e):
        if app.status_selected_build_status != "Pending":
            btn_status_pending.config(bg="#3e3e4a")
    def on_leave_pending(e):
        if app.status_selected_build_status != "Pending":
            btn_status_pending.config(bg="#2c2c35")
    btn_status_pending.bind("<Enter>", on_enter_pending)
    btn_status_pending.bind("<Leave>", on_leave_pending)
    
    # Initialize UI highlights
    update_status_buttons_ui()
    
    # Bind Treeview selection event to app.on_build_select
    app.builds_tree.bind("<<TreeviewSelect>>", app.on_build_select)


    # --- PANE 2: HISTORICAL REPORTS HUB ---
    reports_pane = tk.Frame(sidebar_pw, bg=app.bg_panel)
    sidebar_pw.add(reports_pane, minsize=140, stretch="always")
    
    reports_header = tk.Frame(reports_pane, bg=app.bg_panel)
    reports_header.pack(fill="x", pady=(5, 5))
    
    reports_title = tk.Label(reports_header, text="// HISTORICAL REPORTS", fg=app.accent_glow, bg=app.bg_panel, font=("Segoe UI", 10, "bold"))
    reports_title.pack(side="left")
    
    btn_purge = ttk.Button(reports_header, text="PURGE", command=app.delete_all_reports, style="Sidebar.TButton", width=7)
    btn_purge.pack(side="right", padx=(2, 0))
    app.add_tooltip(btn_purge, "Purge all historical JUnit XML reports")
 
    btn_scan = ttk.Button(reports_header, text="SCAN", command=lambda: app.details_page.populate_reports(), style="Sidebar.TButton", width=6)
    btn_scan.pack(side="right")
    app.add_tooltip(btn_scan, "Scan directory and refresh past JUnit runs")
    
    rep_scroll = ttk.Scrollbar(reports_pane)
    rep_scroll.pack(side="right", fill="y")
    
    app.reports_tree = ttk.Treeview(
        reports_pane, columns=("Date", "Time", "Status", "Duration"), 
        show="headings", yscrollcommand=rep_scroll.set
    )
    app.reports_tree.heading("Date", text="RUN DATE")
    app.reports_tree.heading("Time", text="RUN TIME")
    app.reports_tree.heading("Status", text="VERDICT")
    app.reports_tree.heading("Duration", text="ELAPSED")
    
    app.reports_tree.column("Date", width=65, anchor="center")
    app.reports_tree.column("Time", width=55, anchor="center")
    app.reports_tree.column("Status", width=55, anchor="center")
    app.reports_tree.column("Duration", width=55, anchor="center")
    
    app.reports_tree.tag_configure("PASS", foreground=app.success_glow, font=("Consolas", 9, "bold"))
    app.reports_tree.tag_configure("FAIL", foreground=app.fail_glow, font=("Consolas", 9, "bold"))
    app.reports_tree.tag_configure("Unknown", foreground=app.fg_light)
    
    app.reports_tree.pack(fill="both", expand=True)
    rep_scroll.config(command=app.reports_tree.yview)
