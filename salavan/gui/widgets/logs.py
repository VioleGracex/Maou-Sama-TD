import tkinter as tk
from tkinter import ttk

def create_logs_panel(app, parent):
    # Right Frame (Full Log list)
    app.right_border = tk.Frame(parent, bg=app.accent_dim, bd=1)
    app.right_border.pack(fill="both", expand=True, padx=(5, 15), pady=15)
    
    right_frame = tk.Frame(app.right_border, bg=app.bg_panel, padx=5, pady=5)
    right_frame.pack(fill="both", expand=True)
    
    hdr_frame = tk.Frame(right_frame, bg=app.bg_panel)
    hdr_frame.pack(fill="x", pady=(5, 5))
    
    right_title = tk.Label(hdr_frame, text="// HUD DIAGNOSTIC LOGS", fg=app.accent_glow, bg=app.bg_panel, font=("Segoe UI", 10, "bold"))
    right_title.pack(side="left")
    
    app.btn_pop_logs = tk.Button(
        hdr_frame, text="[ ⧉ POP OUT ]", command=app.toggle_logs_docking,
        bg="#2c2c35", fg=app.fg_light, activebackground=app.accent_glow,
        activeforeground="#101012", bd=0, padx=6, font=("Segoe UI", 8, "bold")
    )
    app.btn_pop_logs.pack(side="right")
    
    app.btn_toggle_right = tk.Button(
        hdr_frame, text="[ ⬇ COLLAPSE ]", command=app.toggle_right_panel,
        bg="#2c2c35", fg=app.fg_light, activebackground=app.accent_glow,
        activeforeground="#101012", bd=0, padx=6, font=("Segoe UI", 8, "bold")
    )
    app.btn_toggle_right.pack(side="right", padx=5)

    app.btn_clear_logs = tk.Button(
        hdr_frame, text="[ 🗑 CLEAR LOGS ]", command=app.clear_logs,
        bg="#2c2c35", fg=app.fg_light, activebackground=app.accent_glow,
        activeforeground="#101012", bd=0, padx=6, font=("Segoe UI", 8, "bold")
    )
    app.btn_clear_logs.pack(side="right", padx=5)
    
    # Hover effects for pop out, toggle and clear buttons
    def on_enter_clear(e):
        app.btn_clear_logs.config(bg=app.accent_glow, fg="#101012")
    def on_leave_clear(e):
        app.btn_clear_logs.config(bg="#2c2c35", fg=app.fg_light)
    app.btn_clear_logs.bind("<Enter>", on_enter_clear)
    app.btn_clear_logs.bind("<Leave>", on_leave_clear)
    app.add_tooltip(app.btn_clear_logs, "Clear all logs and console stream text")

    def on_enter_pop(e):
        app.btn_pop_logs.config(bg=app.accent_glow, fg="#101012")
    def on_leave_pop(e):
        app.btn_pop_logs.config(bg="#2c2c35", fg=app.fg_light)
    app.btn_pop_logs.bind("<Enter>", on_enter_pop)
    app.btn_pop_logs.bind("<Leave>", on_leave_pop)
    
    def on_enter_tr(e):
        app.btn_toggle_right.config(bg=app.accent_glow, fg="#101012")
    def on_leave_tr(e):
        app.btn_toggle_right.config(bg="#2c2c35", fg=app.fg_light)
    app.btn_toggle_right.bind("<Enter>", on_enter_tr)
    app.btn_toggle_right.bind("<Leave>", on_leave_tr)

    # Tooltips
    app.add_tooltip(app.btn_pop_logs, "Pop out diagnostic logs to a separate window / dock back")
    app.add_tooltip(app.btn_toggle_right, "Collapse/Expand Diagnostic Logs Panel")
    
    scroll = ttk.Scrollbar(right_frame)
    scroll.pack(side="right", fill="y")
    
    app.tree = ttk.Treeview(right_frame, columns=("Step", "Result", "Message"), show="headings", yscrollcommand=scroll.set)
    app.tree.heading("Step", text="HUD STEP")
    app.tree.heading("Result", text="RESULT")
    app.tree.heading("Message", text="HUD DIAGNOSTIC MESSAGE")
    
    app.tree.column("Step", width=110, anchor="w")
    app.tree.column("Result", width=65, anchor="center")
    app.tree.column("Message", width=180, anchor="w")
    
    app.tree.tag_configure("PASS", foreground=app.success_glow, font=("Consolas", 10, "bold"))
    app.tree.tag_configure("FAIL", foreground=app.fail_glow, font=("Consolas", 10, "bold"))
    app.tree.tag_configure("STARTING", foreground="#00b4d8", font=("Consolas", 10, "bold", "italic"))
    app.tree.tag_configure("INFO", foreground=app.fg_light, font=("Consolas", 10, "bold"))
    
    app.tree.pack(fill="both", expand=True)
    scroll.config(command=app.tree.yview)
