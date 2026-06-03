import tkinter as tk
from tkinter import ttk, messagebox

def show_manage_games_dialog(app):
    dialog = tk.Toplevel(app.root)
    dialog.title("Manage Game Profiles")
    dialog.geometry("620x450+1000+180")
    dialog.configure(bg=app.bg_dark)
    dialog.attributes("-topmost", True)
    dialog.transient(app.root)
    dialog.grab_set()

    # Left: List of Game Profiles
    left_frame = tk.Frame(dialog, bg=app.bg_panel, padx=10, pady=10)
    left_frame.pack(side="left", fill="both", expand=True, padx=10, pady=10)

    lbl_list = tk.Label(left_frame, text="// REGISTERED GAMES", bg=app.bg_panel, fg=app.accent_glow, font=("Consolas", 10, "bold"))
    lbl_list.pack(anchor="w", pady=(0, 5))

    list_container = tk.Frame(left_frame, bg=app.bg_panel)
    list_container.pack(fill="both", expand=True)

    scroll = ttk.Scrollbar(list_container)
    scroll.pack(side="right", fill="y")

    games_tree = ttk.Treeview(list_container, columns=("ID", "Title"), show="headings", yscrollcommand=scroll.set, height=12)
    games_tree.heading("ID", text="ID")
    games_tree.heading("Title", text="GAME TITLE")
    games_tree.column("ID", width=90, anchor="w")
    games_tree.column("Title", width=150, anchor="w")
    games_tree.pack(fill="both", expand=True)
    scroll.config(command=games_tree.yview)

    # Right: Edit / Add Form
    right_frame = tk.Frame(dialog, bg=app.bg_panel, padx=10, pady=10)
    right_frame.pack(side="right", fill="both", padx=10, pady=10)

    lbl_form = tk.Label(right_frame, text="// GAME DETAILS", bg=app.bg_panel, fg=app.accent_glow, font=("Consolas", 10, "bold"))
    lbl_form.grid(row=0, column=0, columnspan=2, sticky="w", pady=(0, 10))

    # Form Fields
    tk.Label(right_frame, text="ID (alphanumeric):", bg=app.bg_panel, fg=app.fg_light, font=("Consolas", 8, "bold")).grid(row=1, column=0, sticky="w", pady=4)
    ent_id = tk.Entry(right_frame, bg="#1b1b26", fg=app.fg_light, bd=0, insertbackground=app.fg_light, width=28)
    ent_id.grid(row=1, column=1, sticky="w", pady=4)

    tk.Label(right_frame, text="Display Title:", bg=app.bg_panel, fg=app.fg_light, font=("Consolas", 8, "bold")).grid(row=2, column=0, sticky="w", pady=4)
    ent_title = tk.Entry(right_frame, bg="#1b1b26", fg=app.fg_light, bd=0, insertbackground=app.fg_light, width=28)
    ent_title.grid(row=2, column=1, sticky="w", pady=4)

    tk.Label(right_frame, text="Window Title:", bg=app.bg_panel, fg=app.fg_light, font=("Consolas", 8, "bold")).grid(row=3, column=0, sticky="w", pady=4)
    ent_window = tk.Entry(right_frame, bg="#1b1b26", fg=app.fg_light, bd=0, insertbackground=app.fg_light, width=28)
    ent_window.grid(row=3, column=1, sticky="w", pady=4)

    tk.Label(right_frame, text="Process Name:", bg=app.bg_panel, fg=app.fg_light, font=("Consolas", 8, "bold")).grid(row=4, column=0, sticky="w", pady=4)
    ent_process = tk.Entry(right_frame, bg="#1b1b26", fg=app.fg_light, bd=0, insertbackground=app.fg_light, width=28)
    ent_process.grid(row=4, column=1, sticky="w", pady=4)

    tk.Label(right_frame, text="Player.log Path:", bg=app.bg_panel, fg=app.fg_light, font=("Consolas", 8, "bold")).grid(row=5, column=0, sticky="w", pady=4)
    ent_log = tk.Entry(right_frame, bg="#1b1b26", fg=app.fg_light, bd=0, insertbackground=app.fg_light, width=28)
    ent_log.grid(row=5, column=1, sticky="w", pady=4)

    tk.Label(right_frame, text="Save Paths (csv):", bg=app.bg_panel, fg=app.fg_light, font=("Consolas", 8, "bold")).grid(row=6, column=0, sticky="w", pady=4)
    ent_saves = tk.Text(right_frame, bg="#1b1b26", fg=app.fg_light, bd=0, insertbackground=app.fg_light, width=28, height=3, font=("Consolas", 8))
    ent_saves.grid(row=6, column=1, sticky="w", pady=4)

    # Actions panel
    btn_panel = tk.Frame(right_frame, bg=app.bg_panel)
    btn_panel.grid(row=7, column=0, columnspan=2, pady=15, sticky="ew")

    def populate_tree():
        games_tree.delete(*games_tree.get_children())
        for g in app.config.games:
            games_tree.insert("", "end", values=(g.get("id"), g.get("title")))

    def on_select_game(event):
        selected = games_tree.selection()
        if not selected:
            return
        g_id, g_title = games_tree.item(selected[0], "values")
        game = app.config.get_game_by_id(g_id)
        if game:
            ent_id.delete(0, tk.END)
            ent_id.insert(0, game.get("id", ""))
            ent_id.config(state="disabled") # Prevent ID editing
            
            ent_title.delete(0, tk.END)
            ent_title.insert(0, game.get("title", ""))
            
            ent_window.delete(0, tk.END)
            ent_window.insert(0, game.get("window_title", ""))
            
            ent_process.delete(0, tk.END)
            ent_process.insert(0, game.get("process_name", ""))
            
            ent_log.delete(0, tk.END)
            ent_log.insert(0, game.get("log_path", ""))
            
            ent_saves.delete("1.0", tk.END)
            ent_saves.insert("1.0", ", ".join(game.get("save_paths", [])))

    games_tree.bind("<<TreeviewSelect>>", on_select_game)

    def clear_form():
        ent_id.config(state="normal")
        ent_id.delete(0, tk.END)
        ent_title.delete(0, tk.END)
        ent_window.delete(0, tk.END)
        ent_process.delete(0, tk.END)
        ent_log.delete(0, tk.END)
        ent_saves.delete("1.0", tk.END)

    def save_game():
        g_id = ent_id.get().strip().lower()
        title = ent_title.get().strip()
        window_title = ent_window.get().strip()
        process_name = ent_process.get().strip()
        log_path = ent_log.get().strip()
        saves_text = ent_saves.get("1.0", tk.END).strip()
        
        save_paths = [p.strip() for p in saves_text.split(",") if p.strip()]
        
        if not g_id or not title or not window_title or not process_name:
            messagebox.showerror("Validation Error", "ID, Title, Window Title, and Process Name are required!")
            return
            
        if not g_id.isalnum():
            messagebox.showerror("Validation Error", "Game ID must be alphanumeric only (no spaces/symbols)!")
            return

        existing = app.config.get_game_by_id(g_id)
        if existing:
            # Edit existing
            existing["title"] = title
            existing["window_title"] = window_title
            existing["process_name"] = process_name
            existing["log_path"] = log_path
            existing["save_paths"] = save_paths
            app.log_message("SYSTEM", "INFO", f"Updated profile: {title} ({g_id})")
        else:
            # Create new
            app.config.games.append({
                "id": g_id,
                "title": title,
                "window_title": window_title,
                "process_name": process_name,
                "log_path": log_path,
                "save_paths": save_paths,
                "active_exe_path": ""
            })
            app.log_message("SYSTEM", "INFO", f"Registered new profile: {title} ({g_id})")

        app.config.save()
        app.update_game_profiles_list()
        populate_tree()
        clear_form()

    def delete_game():
        selected = games_tree.selection()
        if not selected:
            messagebox.showwarning("Selection Required", "Please select a game profile to delete.")
            return
        g_id, g_title = games_tree.item(selected[0], "values")
        
        if g_id == "maou_sama_td":
            messagebox.showerror("Error", "The default profile 'maou_sama_td' cannot be deleted!")
            return
            
        confirm = messagebox.askyesno("Delete Profile", f"Are you sure you want to delete profile '{g_title}'?\nThis will remove its build references.")
        if confirm:
            app.config.games = [g for g in app.config.games if g.get("id") != g_id]
            # Delete associated builds
            app.config.builds = [b for b in app.config.builds if b.get("game_id", "maou_sama_td") != g_id]
            
            if app.config.active_game_id == g_id:
                app.config.active_game_id = "maou_sama_td"
                
            app.config.save()
            app.log_message("SYSTEM", "INFO", f"Removed profile: {g_title}")
            
            app.update_game_profiles_list()
            populate_tree()
            clear_form()

    # Form buttons
    btn_save = ttk.Button(btn_panel, text="SAVE", command=save_game)
    btn_save.grid(row=0, column=0, padx=2, sticky="ew")
    
    btn_delete = ttk.Button(btn_panel, text="DELETE", command=delete_game)
    btn_delete.grid(row=0, column=1, padx=2, sticky="ew")
    
    btn_clear = ttk.Button(btn_panel, text="NEW/CLEAR", command=clear_form)
    btn_clear.grid(row=0, column=2, padx=2, sticky="ew")
    
    btn_panel.columnconfigure(0, weight=1)
    btn_panel.columnconfigure(1, weight=1)
    btn_panel.columnconfigure(2, weight=1)

    populate_tree()
