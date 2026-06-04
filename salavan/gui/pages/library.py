import tkinter as tk
from tkinter import ttk, messagebox
from PIL import Image, ImageTk, ImageDraw
import os

class AutoScrollbar(ttk.Scrollbar):
    def set(self, lo, hi):
        if float(lo) <= 0.0 and float(hi) >= 1.0:
            self.grid_remove()
        else:
            self.grid()
        super().set(lo, hi)

class LibraryPage(tk.Frame):
    def __init__(self, parent, app):
        super().__init__(parent, bg=app.bg_dark)
        self.app = app
        self.cards = []
        self.games_frame = self
        self.create_widgets()

    def create_widgets(self):
        self.build_games_tab()
        self.populate_library()

    # ── TAB 1: GAMES GRID ──
    def build_games_tab(self):
        # Header
        header_frame = tk.Frame(self.games_frame, bg=self.app.bg_dark, padx=20, pady=20)
        header_frame.pack(fill="x")

        title_container = tk.Frame(header_frame, bg=self.app.bg_dark)
        title_container.pack(side="left")

        title_lbl = tk.Label(
            title_container, text="🎮 GAMES LIBRARY CATALOG", 
            fg="#ffffff", bg=self.app.bg_dark, 
            font=("Segoe UI", 16, "bold")
        )
        title_lbl.pack(anchor="w")

        desc_lbl = tk.Label(
            title_container, text="Select a game profile from the list to enter the testing console.", 
            fg="#9ca3af", bg=self.app.bg_dark, 
            font=("Segoe UI", 9)
        )
        desc_lbl.pack(anchor="w", pady=(2, 0))

        # Register New Game Button (Outlined / Flat style matching references)
        btn_border = tk.Frame(header_frame, bg=self.app.accent_glow, bd=1)
        btn_border.pack(side="right", padx=10, pady=5)

        self.btn_register_game = tk.Button(
            btn_border, text="+ REGISTER NEW GAME", command=self.app.manage_game_profiles,
            bg=self.app.accent_dim, fg="#ffffff", activebackground=self.app.accent_glow,
            activeforeground="#ffffff", bd=0, padx=15, pady=8, font=("Segoe UI", 9, "bold"), cursor="hand2"
        )
        self.btn_register_game.pack()

        # Hover effects for register button
        def on_enter_reg(e):
            self.btn_register_game.config(bg=self.app.accent_glow)
        def on_leave_reg(e):
            self.btn_register_game.config(bg=self.app.accent_dim)
        self.btn_register_game.bind("<Enter>", on_enter_reg)
        self.btn_register_game.bind("<Leave>", on_leave_reg)

        # Search Bar
        search_frame = tk.Frame(header_frame, bg=self.app.bg_dark)
        search_frame.pack(side="right", padx=20)

        tk.Label(
            search_frame, text="SEARCH:", 
            fg="#9ca3af", bg=self.app.bg_dark, 
            font=("Segoe UI", 9, "bold")
        ).pack(side="left", padx=5)

        self.search_var = tk.StringVar()
        self.search_var.trace_add("write", lambda *args: self.filter_cards())
        self.search_entry = tk.Entry(
            search_frame, textvariable=self.search_var, 
            bg="#131317", fg=self.app.fg_light, 
            insertbackground=self.app.fg_light, bd=1, 
            highlightbackground=self.app.accent_dim, highlightcolor=self.app.accent_glow,
            width=18, font=("Segoe UI", 9, "bold")
        )
        self.search_entry.pack(side="left", padx=5, ipady=4)

        # Scrollable Cards Grid Container
        grid_outer = tk.Frame(self.games_frame, bg=self.app.bg_dark, padx=20, pady=10)
        grid_outer.pack(fill="both", expand=True)

        grid_outer.rowconfigure(0, weight=1)
        grid_outer.columnconfigure(0, weight=1)

        self.canvas = tk.Canvas(grid_outer, bg=self.app.bg_dark, bd=0, highlightthickness=0)
        self.canvas.grid(row=0, column=0, sticky="nsew")

        scroll = AutoScrollbar(grid_outer, orient="vertical", command=self.canvas.yview)
        scroll.grid(row=0, column=1, sticky="ns")
        self.canvas.config(yscrollcommand=scroll.set)

        self.cards_frame = tk.Frame(self.canvas, bg=self.app.bg_dark)
        self.canvas_window = self.canvas.create_window((0, 0), window=self.cards_frame, anchor="nw")

        self.cards_frame.bind("<Configure>", lambda e: self.canvas.configure(scrollregion=self.canvas.bbox("all")))
        self.canvas.bind("<Configure>", self.on_canvas_resize)

        # Mousewheel scroll binding
        self.canvas.bind("<MouseWheel>", self.on_mousewheel)
        self.cards_frame.bind("<MouseWheel>", self.on_mousewheel)

    def on_canvas_resize(self, event):
        self.canvas.itemconfig(self.canvas_window, width=event.width)
        self.populate_library(force_rebuild=False)

    def on_mousewheel(self, event):
        bbox = self.canvas.bbox("all")
        if bbox and (bbox[3] - bbox[1]) > self.canvas.winfo_height():
            self.canvas.yview_scroll(int(-1 * (event.delta / 120)), "units")

    def populate_library(self, force_rebuild=True):
        # Responsive columns count
        width = self.canvas.winfo_width()
        cols = max(1, width // 260) # Adapt to card width 230 + padx

        if self.cards and not force_rebuild:
            # Re-grid existing cards without destroying/recreating them
            visible_index = 0
            query = self.search_var.get().strip().lower()
            for card in self.cards:
                if query in card.title.lower():
                    r = visible_index // cols
                    c = visible_index % cols
                    card.grid(row=r, column=c, padx=12, pady=12, sticky="nsew")
                    visible_index += 1
                else:
                    card.grid_remove()
            # Configure weights
            for i in range(cols):
                self.cards_frame.columnconfigure(i, weight=1)
            return

        # Clear previous cards
        for c in self.cards:
            c.destroy()
        self.cards.clear()

        games = self.app.config.games
        
        for index, g in enumerate(games):
            g_id = g.get("id")
            title = g.get("title")
            proc_name = g.get("process_name", "Unknown.exe")
            
            card = self.create_game_card(self.cards_frame, g_id, title, proc_name)
            r = index // cols
            c = index % cols
            card.grid(row=r, column=c, padx=12, pady=12, sticky="nsew")
            self.cards.append(card)

        # Configure weights
        for i in range(cols):
            self.cards_frame.columnconfigure(i, weight=1)

    def create_game_card(self, parent, game_id, title, proc_name):
        # Landscape Card Frame: width=230, height=230
        card = tk.Frame(parent, bg="#131317", bd=1, highlightbackground=self.app.accent_dim, highlightthickness=1, width=230, height=230)
        card.pack_propagate(False)
        card.grid_propagate(False)
        card.game_id = game_id
        card.title = title

        # Thumbnail canvas (landscape 230x130 area)
        thumb_canvas = tk.Canvas(card, bg="#0d0d11", height=130, bd=0, highlightthickness=0)
        thumb_canvas.pack(fill="x", side="top")

        # Load cover image or use emoji fallback
        img_loaded = False
        cover_path = os.path.join(self.app.base_dir, f"{game_id}_cover.png")
        if not os.path.exists(cover_path) and game_id == "maou_sama_td":
            cover_path = os.path.join(self.app.base_dir, "maou_sama_td_cover.png")

        if os.path.exists(cover_path):
            try:
                img = Image.open(cover_path)
                img = img.resize((230, 130), Image.Resampling.LANCEZOS)
                photo = ImageTk.PhotoImage(img)
                thumb_canvas.create_image(0, 0, image=photo, anchor="nw")
                thumb_canvas.image = photo
                img_loaded = True
            except Exception:
                pass

        if not img_loaded:
            # Set background and draw emoji in center
            emoji = "🎮"
            if "light" in title.lower():
                emoji = "🕯️"
            elif "magic" in title.lower() or "academy" in title.lower():
                emoji = "🧙"
            
            thumb_canvas.create_text(115, 65, text=emoji, font=("Segoe UI", 36), fill=self.app.accent_glow)

        # Meta Details Frame
        meta_frame = tk.Frame(card, bg="#131317", padx=12, pady=8)
        meta_frame.pack(fill="both", expand=True)

        title_lbl = tk.Label(
            meta_frame, text=title, 
            fg="#ffffff", bg="#131317", 
            font=("Segoe UI", 10, "bold")
        )
        title_lbl.pack(anchor="w")

        proc_lbl = tk.Label(
            meta_frame, text=f"Process Name: {proc_name}", 
            fg="#9ca3af", bg="#131317", 
            font=("Segoe UI", 8)
        )
        proc_lbl.pack(anchor="w", pady=(2, 0))

        # Count scenarios
        scenarios_dir = os.path.join(self.app.base_dir, "scenarios", game_id)
        scenarios_count = 0
        if os.path.exists(scenarios_dir):
            scenarios_count = len([f for f in os.listdir(scenarios_dir) if f.endswith(".lua")])
            
        scenarios_lbl = tk.Label(
            meta_frame, text=f"Scenarios Registered: {scenarios_count}", 
            fg=self.app.accent_glow, bg="#131317", 
            font=("Segoe UI", 8, "bold")
        )
        scenarios_lbl.pack(anchor="w", pady=(2, 0))

        # Button Frame packed at bottom right
        btn_frame = tk.Frame(meta_frame, bg="#131317")
        btn_frame.pack(fill="x", side="bottom", pady=(0, 2))

        # 1px border outline wrap
        btn_border = tk.Frame(btn_frame, bg=self.app.accent_glow, bd=1)
        btn_border.pack(side="right")

        btn_test = tk.Button(
            btn_border, text="TEST WORKSPACE", command=lambda: self.app.show_details_page(game_id),
            bg="#131317", fg=self.app.accent_glow, activebackground=self.app.accent_glow,
            activeforeground="#ffffff", bd=0, font=("Segoe UI", 8, "bold"), padx=10, pady=3, cursor="hand2"
        )
        btn_test.pack()

        # Hover actions for button
        def on_enter_btn(event):
            btn_test.config(bg=self.app.accent_glow, fg="#ffffff")
        def on_leave_btn(event):
            btn_test.config(bg="#131317", fg=self.app.accent_glow)
        btn_test.bind("<Enter>", on_enter_btn)
        btn_test.bind("<Leave>", on_leave_btn)

        # Hover actions for card border
        def on_enter_card(event):
            card.config(highlightbackground=self.app.accent_glow)
        def on_leave_card(event):
            card.config(highlightbackground=self.app.accent_dim)

        def on_click(event):
            self.app.show_details_page(game_id)

        # Bind hover and scroll to components
        for w in (card, thumb_canvas, meta_frame, title_lbl, proc_lbl, scenarios_lbl, btn_frame):
            w.bind("<Enter>", on_enter_card)
            w.bind("<Leave>", on_leave_card)
            w.bind("<Button-1>", on_click)
            w.bind("<MouseWheel>", self.on_mousewheel)

        return card

    def filter_cards(self):
        query = self.search_var.get().strip().lower()
        visible_index = 0
        width = self.canvas.winfo_width()
        cols = max(1, width // 260)
        for card in self.cards:
            if hasattr(card, "title"):
                if query in card.title.lower():
                    card.grid()
                    r = visible_index // cols
                    c = visible_index % cols
                    card.grid(row=r, column=c, padx=12, pady=12, sticky="nsew")
                    visible_index += 1
                else:
                    card.grid_remove()
