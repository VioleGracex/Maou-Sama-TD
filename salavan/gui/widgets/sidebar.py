import tkinter as tk
from tkinter import ttk
import os
import re

def create_sidebar(app, parent):
    # Left Border Frame (Sidebar: Scenarios)
    app.left_border = tk.Frame(parent, bg=app.accent_dim, bd=1)
    app.left_border.pack(fill="both", expand=True, padx=(15, 5), pady=15)
    
    left_frame = tk.Frame(app.left_border, bg=app.bg_panel, padx=10, pady=10)
    left_frame.pack(fill="both", expand=True)
    
    # Sidebar Scenarios Section Header
    sidebar_header = tk.Frame(left_frame, bg=app.bg_panel)
    sidebar_header.pack(fill="x", pady=(0, 10))
    
    sidebar_title = tk.Label(sidebar_header, text="📂 SCENARIOS DATABASE", fg=app.alert_yellow, bg=app.bg_panel, font=("Segoe UI", 10, "bold"))
    sidebar_title.pack(side="left")
    
    btn_refresh = tk.Button(
        sidebar_header, text="🔄", command=app.load_scenarios,
        bg="#131317", fg="#38bdf8", activebackground=app.accent_glow,
        activeforeground="#131317", bd=0, padx=6, font=("Segoe UI", 9, "bold"), cursor="hand2"
    )
    btn_refresh.pack(side="right")
    app.add_tooltip(btn_refresh, "Refresh Scenario Files")

    btn_import = tk.Button(
        sidebar_header, text="➕", command=app.import_scenarios_dialog,
        bg="#131317", fg="#10b981", activebackground=app.accent_glow,
        activeforeground="#131317", bd=0, padx=6, font=("Segoe UI", 9, "bold"), cursor="hand2"
    )
    btn_import.pack(side="right", padx=(0, 4))
    app.add_tooltip(btn_import, "Import Scenario Files (.lua)")

    # Instantiate the hidden listbox so we don't break core app.py curselection/get logic
    app.scenario_listbox = tk.Listbox(left_frame)
    # We do NOT pack it, keeping it completely hidden from view!

    list_container = tk.Frame(left_frame, bg=app.bg_panel)
    list_container.pack(fill="both", expand=True) 
    list_container.pack_propagate(False)
    
    list_scroll = ttk.Scrollbar(list_container)
    list_scroll.pack(side="right", fill="y")
    
    canvas = tk.Canvas(list_container, bg=app.bg_panel, bd=0, highlightthickness=0, yscrollcommand=list_scroll.set)
    canvas.pack(side="left", fill="both", expand=True)
    list_scroll.config(command=canvas.yview)
    
    app.custom_scenarios_container = tk.Frame(canvas, bg=app.bg_panel)
    canvas_window = canvas.create_window((0, 0), window=app.custom_scenarios_container, anchor="nw")
    
    app.custom_scenarios_container.bind("<Configure>", lambda e: canvas.configure(scrollregion=canvas.bbox("all")))
    canvas.bind("<Configure>", lambda e: canvas.itemconfig(canvas_window, width=e.width))
    
    # Mousewheel scroll binding for scenarios list
    def on_mousewheel(event):
        canvas.yview_scroll(int(-1 * (event.delta / 120)), "units")
    canvas.bind_all("<MouseWheel>", on_mousewheel)

    # ----------------------------------------------------------------
    # Drag-and-drop state
    # ----------------------------------------------------------------
    _drag = {
        "active":       False,
        "source_idx":   None,
        "ghost":        None,   # Toplevel ghost label
        "insert_line":  None,   # Canvas line showing drop target
    }

    def _reorder_scenarios(from_idx, to_idx):
        """Rename .lua files on disk so their numeric prefixes reflect the new order."""
        if not hasattr(app, 'scenarios_dir') or not os.path.exists(app.scenarios_dir):
            return
        files = sorted(f for f in os.listdir(app.scenarios_dir) if f.endswith(".lua"))
        if from_idx == to_idx or from_idx >= len(files) or to_idx > len(files):
            return

        # Build new order
        item = files.pop(from_idx)
        insert_pos = to_idx if to_idx <= from_idx else to_idx - 1
        files.insert(insert_pos, item)

        # Rename files with a temp prefix first (avoids collision)
        import tempfile, shutil
        tmp_names = []
        for i, fname in enumerate(files):
            tmp = f"__tmp_{i:03d}__{fname}"
            src = os.path.join(app.scenarios_dir, fname)
            dst = os.path.join(app.scenarios_dir, tmp)
            os.rename(src, dst)
            tmp_names.append((tmp, fname))

        # Strip old numeric prefix and apply new one
        for new_idx, (tmp_fname, orig_fname) in enumerate(tmp_names):
            # Remove existing leading number prefix (e.g. "1_", "02_", etc.)
            bare = re.sub(r'^\d+_', '', orig_fname)
            final = f"{new_idx + 1}_{bare}"
            src = os.path.join(app.scenarios_dir, tmp_fname)
            dst = os.path.join(app.scenarios_dir, final)
            os.rename(src, dst)

        # Clear step cache so new filenames are resolved correctly
        if hasattr(app, '_scenario_steps_cache'):
            app._scenario_steps_cache.clear()
        if hasattr(app, '_last_sidebar_state'):
            del app._last_sidebar_state

        # Reset selection to moved item's new position and refresh
        if hasattr(app, 'scenario_listbox'):
            app.scenario_listbox.selection_clear(0, tk.END)
            app.scenario_listbox.selection_set(insert_pos)
        app.load_scenarios()
        app.update_custom_sidebar()

    def _destroy_ghost():
        if _drag["ghost"] and _drag["ghost"].winfo_exists():
            _drag["ghost"].destroy()
        _drag["ghost"] = None

    def _destroy_insert_line():
        if _drag["insert_line"] is not None:
            try:
                canvas.delete(_drag["insert_line"])
            except Exception:
                pass
            _drag["insert_line"] = None

    def _card_index_at_y(root_y):
        """Return the card index (0-based) closest to root_y inside the canvas."""
        container = app.custom_scenarios_container
        cards = container.winfo_children()
        if not cards:
            return 0, len(cards)
        # Canvas scroll offset
        canvas_top = canvas.winfo_rooty() + canvas.canvasy(0)
        rel_y = root_y - canvas.winfo_rooty() + canvas.canvasy(0)
        for i, card_border in enumerate(cards):
            card_top    = card_border.winfo_y()
            card_bottom = card_top + card_border.winfo_height()
            mid = (card_top + card_bottom) / 2
            if rel_y < mid:
                return i, len(cards)          # insert before card i
        return len(cards), len(cards)         # insert at end

    def _make_drag_handlers(card_widgets, card_idx, name):
        """Bind drag-and-drop handlers to a list of widgets in one card."""

        def on_drag_start(event):
            _drag["active"]     = True
            _drag["source_idx"] = card_idx
            _destroy_ghost()

            ghost = tk.Toplevel(app.root)
            ghost.overrideredirect(True)
            ghost.attributes("-topmost", True)
            ghost.attributes("-alpha", 0.75)
            ghost.configure(bg=app.accent_glow)
            tk.Label(
                ghost,
                text=f"  ⠿  {name}.lua  ",
                bg=app.accent_glow,
                fg="#101012",
                font=("Segoe UI", 9, "bold"),
                padx=6, pady=4
            ).pack()
            ghost.geometry(f"+{event.x_root + 12}+{event.y_root - 10}")
            _drag["ghost"] = ghost

        def on_drag_motion(event):
            if not _drag["active"]:
                return
            # Move ghost
            if _drag["ghost"] and _drag["ghost"].winfo_exists():
                _drag["ghost"].geometry(f"+{event.x_root + 12}+{event.y_root - 10}")

            # Draw insertion indicator line
            _destroy_insert_line()
            insert_before, total = _card_index_at_y(event.y_root)
            container = app.custom_scenarios_container
            cards = container.winfo_children()
            cw = canvas.winfo_width()
            scroll_y = canvas.canvasy(0)
            if insert_before < len(cards):
                ref = cards[insert_before]
                line_y = ref.winfo_y() - scroll_y - 2
            else:
                if cards:
                    ref = cards[-1]
                    line_y = ref.winfo_y() + ref.winfo_height() - scroll_y + 2
                else:
                    line_y = 4
            _drag["insert_line"] = canvas.create_line(
                6, line_y, cw - 6, line_y,
                fill=app.accent_glow, width=2, dash=(4, 2)
            )

        def on_drag_release(event):
            if not _drag["active"]:
                return
            _drag["active"] = False
            _destroy_ghost()
            _destroy_insert_line()

            from_idx = _drag["source_idx"]
            to_idx, _ = _card_index_at_y(event.y_root)
            _drag["source_idx"] = None

            if to_idx != from_idx and to_idx != from_idx + 1:
                _reorder_scenarios(from_idx, to_idx)

        for w in card_widgets:
            w.bind("<ButtonPress-1>",   on_drag_start,   add="+")
            w.bind("<B1-Motion>",        on_drag_motion,  add="+")
            w.bind("<ButtonRelease-1>",  on_drag_release, add="+")

    # Test Controls Frame
    test_ctrl_frame = tk.Frame(left_frame, bg=app.bg_panel, pady=5)
    test_ctrl_frame.pack(fill="x", pady=(8, 0))
    
    app.btn_run = ttk.Button(test_ctrl_frame, text="RUN TEST", command=app.start_test_flow, style="Sidebar.TButton")
    app.btn_run.grid(row=0, column=0, columnspan=2, pady=(0, 4), sticky="ew")
    app.add_tooltip(app.btn_run, "Execute active Lua scenario sequence")
    
    app.btn_pause = ttk.Button(test_ctrl_frame, text="PAUSE", command=app.toggle_pause, state="disabled", style="Sidebar.TButton")
    app.btn_pause.grid(row=1, column=0, padx=(0, 2), sticky="ew")
    app.add_tooltip(app.btn_pause, "Pause / Resume sequence execution")
    
    app.btn_stop = ttk.Button(test_ctrl_frame, text="ABORT", command=app.stop_test_flow, state="disabled", style="Sidebar.TButton")
    app.btn_stop.grid(row=1, column=1, padx=(2, 0), sticky="ew")
    app.add_tooltip(app.btn_stop, "Force abort active scenario and close game process")
    
    debug_ctrl_frame = tk.Frame(test_ctrl_frame, bg=app.bg_panel)
    debug_ctrl_frame.grid(row=2, column=0, columnspan=2, pady=(4, 0), sticky="ew")
    
    app.btn_prev = ttk.Button(debug_ctrl_frame, text="⏮ PREV", command=app.prev_step, state="disabled", style="Sidebar.TButton")
    app.btn_prev.pack(side="left", fill="x", expand=True, padx=(0, 2))
    app.add_tooltip(app.btn_prev, "Rewind and run the previous step")
    
    app.btn_repeat = ttk.Button(debug_ctrl_frame, text="🔁 REPEAT", command=app.repeat_step, state="disabled", style="Sidebar.TButton")
    app.btn_repeat.pack(side="left", fill="x", expand=True, padx=2)
    app.add_tooltip(app.btn_repeat, "Restart and repeat the current step")
    
    app.btn_next = ttk.Button(debug_ctrl_frame, text="⏭ NEXT", command=app.next_step, state="disabled", style="Sidebar.TButton")
    app.btn_next.pack(side="left", fill="x", expand=True, padx=(2, 0))
    app.add_tooltip(app.btn_next, "Skip the current step / advance execution")
    
    test_ctrl_frame.columnconfigure(0, weight=1)
    test_ctrl_frame.columnconfigure(1, weight=1)

    # Sync selection callback
    def select_scenario_by_index(index):
        if hasattr(app, 'scenario_listbox') and app.scenario_listbox:
            app.scenario_listbox.selection_clear(0, tk.END)
            app.scenario_listbox.selection_set(index)
            update_custom_sidebar()
            
    # Accordion style custom sidebar update function
    def update_custom_sidebar():
        selected_scenario = None
        def toggle_step_skip(s_name, st_name):
            app.skipped_steps[(s_name, st_name)] = not app.skipped_steps.get((s_name, st_name), False)
            update_custom_sidebar()
            
        files = []
        if hasattr(app, 'scenarios_dir') and os.path.exists(app.scenarios_dir):
            files = [f for f in os.listdir(app.scenarios_dir) if f.endswith(".lua")]
            files.sort()
            
        selected_idx = 0
        if hasattr(app, 'scenario_listbox'):
            sel = app.scenario_listbox.curselection()
            if sel:
                selected_idx = sel[0]
                
        active_stage = app.stage_lbl["text"] if hasattr(app, 'stage_lbl') else "Idle"
        active_stage_lower = active_stage.lower()
        if " [scene:" in active_stage_lower:
            active_stage_lower = active_stage_lower.split(" [scene:")[0]
        if "status:" in active_stage_lower:
            active_stage_lower = active_stage_lower.split("status:")[1].strip()
        if "system status:" in active_stage_lower:
            active_stage_lower = active_stage_lower.split("system status:")[1].strip()
            
        current_state = (files, selected_idx, active_stage_lower)
        if hasattr(app, '_last_sidebar_state') and app._last_sidebar_state == current_state:
            return
            
        app._last_sidebar_state = current_state

        for w in app.custom_scenarios_container.winfo_children():
            w.destroy()
            
        for idx, f in enumerate(files):
            name = os.path.splitext(f)[0]
            is_selected = (idx == selected_idx)
            
            border_color = app.accent_glow if is_selected else app.accent_dim
            card_border = tk.Frame(app.custom_scenarios_container, bg=border_color, bd=1)
            card_border.pack(fill="x", pady=6)
            card_border.config(cursor="fleur")
            
            card = tk.Frame(card_border, bg="#131317", padx=10, pady=10)
            card.pack(fill="both", expand=True)
            
            title_frame = tk.Frame(card, bg="#131317")
            title_frame.pack(fill="x")
            
            bullet_color = app.accent_glow if is_selected else "#6b7280"
            bullet_lbl = tk.Label(title_frame, text="●", fg=bullet_color, bg="#131317", font=("Segoe UI", 9))
            bullet_lbl.pack(side="left")
            
            title_lbl = tk.Label(title_frame, text=name + ".lua", fg="#ffffff", bg="#131317", font=("Segoe UI", 10, "bold"))
            title_lbl.pack(side="left", padx=5)
            
            desc_text = "Lua automated test sequence."
            if "1_Fresh" in name:
                desc_text = "Wipes player profiles, loads game screen, roll dices for Ascension, deploys Ignis, and clears level 1 tutorial."
            elif "2_Level" in name:
                desc_text = "Hooks to running client, navigates layout map, triggers Level 2, and runs vanguard deployment rules."
            elif "3_Level" in name:
                desc_text = "Navigates maps, connects active build, clears level 3 nodes, and deploys custom ultimate gacha rites."
                
            desc_lbl = tk.Label(card, text=desc_text, fg="#9ca3af", bg="#131317", font=("Segoe UI", 8), wraplength=210, justify="left", anchor="w")
            desc_lbl.pack(fill="x", pady=(3, 0))
            
            if is_selected:
                selected_scenario = name
                sep = tk.Frame(card, bg=app.accent_dim, height=1)
                sep.pack(fill="x", pady=8)
                
                script_path = os.path.join(app.scenarios_dir, f)
                steps = []
                if not hasattr(app, '_scenario_steps_cache'):
                    app._scenario_steps_cache = {}
                
                if script_path in app._scenario_steps_cache:
                    steps = app._scenario_steps_cache[script_path]
                else:
                    if os.path.exists(script_path):
                        try:
                            with open(script_path, "r", encoding="utf-8") as f_in:
                                content = f_in.read()
                            matches = re.findall(r'set_stage\([\'"]([^\'"]+)[\'"]\)', content)
                            for m in matches:
                                steps.append(m)
                            app._scenario_steps_cache[script_path] = steps
                        except Exception:
                            pass
                
                active_stage = app.stage_lbl["text"] if hasattr(app, 'stage_lbl') else "Idle"
                active_stage_lower = active_stage.lower()
                if " [scene:" in active_stage_lower:
                    active_stage_lower = active_stage_lower.split(" [scene:")[0]
                if "status:" in active_stage_lower:
                    active_stage_lower = active_stage_lower.split("status:")[1].strip()
                if "system status:" in active_stage_lower:
                    active_stage_lower = active_stage_lower.split("system status:")[1].strip()
                
                active_idx = -1
                for s_idx, step_name in enumerate(steps):
                    s_clean = re.sub(r'^\d+\.\s*', '', step_name).lower().strip()
                    if s_clean in active_stage_lower or active_stage_lower in s_clean:
                        active_idx = s_idx
                        break
                
                if "completed" in active_stage_lower or "aborted" in active_stage_lower:
                    active_idx = len(steps)
                    
                for s_idx, step_name in enumerate(steps):
                    row = tk.Frame(card, bg="#131317")
                    row.pack(fill="x", pady=2)
                    
                    is_step_skipped = app.skipped_steps.get((selected_scenario, step_name), False)
                    cb_text = "☐" if is_step_skipped else "☑"
                    cb_fg = "#6b7280" if is_step_skipped else app.success_glow
                    
                    cb_lbl = tk.Label(row, text=cb_text, fg=cb_fg, bg="#131317", font=("Segoe UI", 10, "bold"), cursor="hand2")
                    cb_lbl.pack(side="left", padx=(0, 5))
                    
                    def make_toggle_cb(s_name=selected_scenario, st_name=step_name):
                        return lambda e: toggle_step_skip(s_name, st_name)
                    cb_lbl.bind("<Button-1>", make_toggle_cb())
                    
                    is_done = (s_idx <= active_idx)
                    
                    if is_step_skipped:
                        status_text = "SKIP"
                        status_color = "#4b5563"
                        step_fg = "#4b5563"
                        step_font = ("Segoe UI", 8, "italic")
                    else:
                        status_text = "DONE" if is_done else "PENDING"
                        status_color = app.success_glow if is_done else "#6b7280"
                        step_fg = status_color if is_done else "#f3f4f6"
                        step_font = ("Segoe UI", 8, "bold" if is_done else "normal")
                    
                    step_lbl = tk.Label(row, text=step_name, fg=step_fg, bg="#131317", font=step_font)
                    step_lbl.pack(side="left")
                    
                    status_lbl = tk.Label(row, text=status_text, fg=status_color, bg="#131317", font=("Segoe UI", 8, "bold"))
                    status_lbl.pack(side="right")
            
            # Collect all non-interactive widgets in this card for drag binding
            drag_widgets = [card_border, card, title_frame, bullet_lbl, title_lbl, desc_lbl]

            def make_select(index):
                return lambda e, idx=index: select_scenario_by_index(idx)

            for w in drag_widgets:
                w.bind("<Button-1>", make_select(idx), add="+")
                w.config(cursor="fleur")

            # Attach drag-and-drop handlers (add="+" keeps the select binding)
            _make_drag_handlers(drag_widgets, idx, name)

    app.update_custom_sidebar = update_custom_sidebar
    app.select_scenario_by_index = select_scenario_by_index
