import os
import sys
import time
import shutil
import threading
import subprocess
import tkinter as tk
from tkinter import ttk, messagebox, filedialog
from PIL import Image, ImageTk, ImageGrab, ImageDraw
import cv2
import numpy as np
import pyautogui
import pygetwindow as gw
import re

# Decoupled Local Imports
from config import ConfigManager
from logger import ReportLogger
from capture import LiveCaptureThread
from engine.runner import TestSequenceRunner
from gui.widgets.sidebar import create_sidebar
from gui.widgets.center import create_center_panel
from gui.widgets.logs import create_logs_panel
from gui.dialogs.add_build import show_add_build_dialog
from gui.dialogs.shortcuts import show_shortcuts_dialog
try:
    import windnd
except ImportError:
    windnd = None
from gui.dialogs.manage_games import show_manage_games_dialog
from gui.pages.library import LibraryPage
from gui.pages.details import DetailsPage

class GameSalavanApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Sylvan-HUD Game Salavan Panel v2.9.0")
        self.root.geometry("1160x820+50+50")
        self.root.minsize(1024, 720)
        self.web_log_buffer = []
        self.latest_screenshot = None
        
        # Force registration of the app in Windows Taskbar
        try:
            import ctypes
            myappid = 'Ouiki.Dev.Salavan.Tester.2.9' # arbitrary string
            ctypes.windll.shell32.SetCurrentProcessExplicitAppUserModelID(myappid)
        except Exception:
            pass
            
        # Frameless window configuration
        # Instead of overrideredirect(True) which breaks Alt-Tab and Windows Taskbar integration,
        # we will use native Win32 window styles via SetWindowLongW in show_in_taskbar.
        # This keeps the window visible in Alt-Tab and Taskbar with its proper icon.
        self.root.overrideredirect(False)
        self.is_maximized = False
        self.normal_geometry = "1160x820+50+50"
        
        # Handle cleanup on window exit
        self.root.protocol("WM_DELETE_WINDOW", self.on_close)
        
        # Resolve Base Directory (Handles standalone pyinstaller exe location)
        if getattr(sys, 'frozen', False):
            self.base_dir = os.path.dirname(sys.executable)
        else:
            self.base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
            
        self.recordings_dir = os.path.join(self.base_dir, "recordings")
        self.config_path = os.path.join(self.base_dir, "config.json")
        self.icon_path = os.path.join(self.base_dir, "icon.ico")
        
        # Initialize Core Utilities
        self.config = ConfigManager(self.config_path)
        self.report_logger = ReportLogger()
        
        # Dynamically set active game directories
        self.update_game_directories()
        
        # Threading and monitoring states
        self.test_thread = None
        self.log_monitor = None
        self.stop_flag = False
        self.pause_event = threading.Event()
        self.pause_event.set()
        self.game_process = None
        
        # Debugging step control variables
        self.skipped_steps = {}
        self.current_step_idx = None
        self.current_step_is_skipped = False
        self.auto_skip_to_step_idx = None
        self.pause_at_target = False
        self.skip_current_step = False
        self.run_to_next_step = False
        
        # Collapsible Panel Flags and Modes
        self.left_collapsed = False
        self.right_collapsed = False
        self.builds_collapsed = False
        self.hud_mode = "ANALYZE"
        self.last_geometry = "1160x820"
        self.current_page = "library"
        
        # UI Styling Definitions - Modern Gaming Purple & Ultra-Dark Theme
        self.bg_dark = "#09090b"     # Ultra-Deep Charcoal/Black
        self.bg_panel = "#131317"    # Matte Dark Card
        self.accent_glow = "#a855f7" # Vibrant Violet/Purple
        self.accent_dim = "#2e1065"  # Deep Violet border/accent
        self.fg_light = "#f3f4f6"    # Comfortable light off-white
        self.success_glow = "#10b981" # Softer emerald green
        self.fail_glow = "#ef4444"    # Softer Apple Red
        self.alert_yellow = "#f59e0b" # Softer gold/orange

        if os.path.exists(self.icon_path):
            try:
                self.root.iconbitmap(self.icon_path)
            except Exception:
                pass

        self.root.configure(bg=self.bg_dark)
        
        # ttk styles configurations
        self.style = ttk.Style()
        self.style.theme_use("clam")
        self.style.configure(".", background=self.bg_dark, foreground=self.fg_light)
        self.style.configure("TLabel", background=self.bg_dark, foreground=self.fg_light, font=("Segoe UI", 10, "bold"))
        self.style.configure("TButton", background="#2c2c35", foreground=self.fg_light, borderwidth=0, font=("Segoe UI", 9, "bold"))
        self.style.map("TButton", background=[("active", self.accent_glow), ("hover", "#3e3e4a")], foreground=[("active", "#101012")])
        self.style.configure("Sidebar.TButton", background="#2c2c35", foreground=self.fg_light, borderwidth=0, font=("Segoe UI", 8, "bold"))
        self.style.map("Sidebar.TButton", background=[("active", self.accent_glow), ("hover", "#3e3e4a")], foreground=[("active", "#101012")])
        self.style.configure("TCheckbutton", background=self.bg_dark, foreground=self.fg_light, font=("Segoe UI", 9, "bold"))
        self.style.configure("TEntry", fieldbackground="#151518", foreground=self.fg_light, font=("Segoe UI", 9, "bold"))
        
        # Combobox styling to prevent white/unreadable boxes
        self.style.configure("TCombobox", fieldbackground="#151518", background="#2c2c35", foreground=self.fg_light, arrowcolor=self.accent_glow, bordercolor="#2c2c35", font=("Segoe UI", 9, "bold"))
        self.style.map("TCombobox", fieldbackground=[("readonly", "#151518")], foreground=[("readonly", self.fg_light)])
        
        # Modern Flat Dark Scrollbar Styling
        self.style.configure("Vertical.TScrollbar", troughcolor=self.bg_dark, background="#2c2c35", arrowcolor=self.fg_light, bordercolor=self.bg_dark, gripcount=0, darkcolor="#2c2c35", lightcolor="#2c2c35")
        self.style.configure("Horizontal.TScrollbar", troughcolor=self.bg_dark, background="#2c2c35", arrowcolor=self.fg_light, bordercolor=self.bg_dark, gripcount=0, darkcolor="#2c2c35", lightcolor="#2c2c35")

        self.style.configure("Treeview", background="#151518", fieldbackground="#151518", foreground=self.fg_light, font=("Segoe UI", 9, "bold"))
        self.style.configure("Treeview.Heading", background="#222228", foreground=self.fg_light, font=("Segoe UI", 9, "bold"))
        
        # Build layout from widgets pack
        self.create_layouts()
        
        # Populate Game Profiles combobox
        self.update_game_profiles_list()
        self.game_profile_combo.bind("<<ComboboxSelected>>", self.on_game_profile_changed)

        # Migrate and populate widgets lists
        self.migrate_old_assets()
        self.update_game_directories()
        self.ensure_default_scenarios()
        self.load_scenarios()
        self.refresh_builds_tree()
        self.bind_hotkeys()
        self.root.after(200, self.setup_file_drag_drop)
        
        # Initialize and start background capture preview thread
        self.capture_thread = LiveCaptureThread(self, fps=10)
        self.capture_thread.start()
        
        self.log_message("SYSTEM", "INFO", "Sylvan-HUD Game Salavan Panel v2.9.0 Activated.")
        
        self.status_blink_on = True
        self.blink_status_dot()
        
        # Start background window auto-hook daemon
        self.background_auto_hook()
        
        self.root.after(100, self.show_in_taskbar)

    def create_layouts(self):
        # Custom Title Bar
        self.title_bar = tk.Frame(self.root, bg="#18181c", height=32)
        self.title_bar.pack(fill="x", side="top")
        self.title_bar.pack_propagate(False)
        
        # Navigation toggle button (hamburger icon)
        self.btn_nav_toggle = tk.Button(
            self.title_bar, text="☰", command=self.toggle_navigation_sidebar,
            bg="#18181c", fg=self.fg_light, activebackground=self.accent_glow,
            activeforeground="#101012", bd=0, width=4, font=("Segoe UI", 10, "bold")
        )
        self.btn_nav_toggle.pack(side="left", fill="y")
        self.add_tooltip(self.btn_nav_toggle, "Toggle Navigation Sidebar")
        
        def on_enter_nav(e):
            self.btn_nav_toggle.config(bg="#2c2c35", fg=self.accent_glow)
        def on_leave_nav(e):
            self.btn_nav_toggle.config(bg="#18181c", fg=self.fg_light)
        self.btn_nav_toggle.bind("<Enter>", on_enter_nav)
        self.btn_nav_toggle.bind("<Leave>", on_leave_nav)
        
        # Icon / Title Label
        self.title_lbl = tk.Label(self.title_bar, text=" 🛡️  SYLVAN-HUD GAME SALAVAN PANEL v2.9.0", fg=self.accent_glow, bg="#18181c", font=("Segoe UI", 10, "bold"))
        self.title_lbl.pack(side="left", padx=10)
        
        # Window buttons
        self.close_btn = tk.Button(self.title_bar, text="✕", command=self.on_close, bg="#18181c", fg=self.fg_light, activebackground="#ff3b30", activeforeground="#ffffff", bd=0, width=4, font=("Segoe UI", 10, "bold"))
        self.close_btn.pack(side="right", fill="y")
        
        self.max_btn = tk.Button(self.title_bar, text="⬜", command=self.toggle_maximize, bg="#18181c", fg=self.fg_light, activebackground=self.accent_dim, activeforeground=self.fg_light, bd=0, width=4, font=("Segoe UI", 10, "bold"))
        self.max_btn.pack(side="right", fill="y")
        
        self.min_btn = tk.Button(self.title_bar, text="—", command=self.minimize_window, bg="#18181c", fg=self.fg_light, activebackground=self.accent_dim, activeforeground=self.fg_light, bd=0, width=4, font=("Segoe UI", 10, "bold"))
        self.min_btn.pack(side="right", fill="y")
        
        self.help_btn = tk.Button(
            self.title_bar, text="❓", command=self.show_shortcuts_help,
            bg="#18181c", fg=self.fg_light, activebackground=self.accent_dim,
            activeforeground=self.fg_light, bd=0, width=4, font=("Segoe UI", 10, "bold")
        )
        self.help_btn.pack(side="right", fill="y")
        
        # Title bar hover bindings
        def on_enter_close(e):
            self.close_btn.config(bg="#ff3b30", fg="#ffffff")
        def on_leave_close(e):
            self.close_btn.config(bg="#18181c", fg=self.fg_light)
        self.close_btn.bind("<Enter>", on_enter_close)
        self.close_btn.bind("<Leave>", on_leave_close)
        
        def on_enter_max(e):
            self.max_btn.config(bg="#2c2c35")
        def on_leave_max(e):
            self.max_btn.config(bg="#18181c")
        self.max_btn.bind("<Enter>", on_enter_max)
        self.max_btn.bind("<Leave>", on_leave_max)
        
        def on_enter_min(e):
            self.min_btn.config(bg="#2c2c35")
        def on_leave_min(e):
            self.min_btn.config(bg="#18181c")
        self.min_btn.bind("<Enter>", on_enter_min)
        self.min_btn.bind("<Leave>", on_leave_min)

        def on_enter_help(e):
            self.help_btn.config(bg="#2c2c35")
        def on_leave_help(e):
            self.help_btn.config(bg="#18181c")
        self.help_btn.bind("<Enter>", on_enter_help)
        self.help_btn.bind("<Leave>", on_leave_help)

        # Attach tooltips to title bar buttons
        self.add_tooltip(self.close_btn, "Close Application")
        self.add_tooltip(self.max_btn, "Maximize / Restore Window")
        self.add_tooltip(self.min_btn, "Minimize Window")
        self.add_tooltip(self.help_btn, "Keyboard Shortcuts Help")
        
        # Dragging logic bindings
        self.title_bar.bind("<ButtonPress-1>", self.start_drag)
        self.title_bar.bind("<B1-Motion>", self.drag_window)
        self.title_lbl.bind("<ButtonPress-1>", self.start_drag)
        self.title_lbl.bind("<B1-Motion>", self.drag_window)
        
        # Global Main Layout Container (below custom title bar)
        self.main_layout = tk.Frame(self.root, bg=self.bg_dark)
        self.main_layout.pack(fill="both", expand=True)

        # Global Left Navigation Sidebar (Vertical Side Tabs)
        self.navigation_sidebar = tk.Frame(self.main_layout, bg="#101012", width=65)
        self.navigation_sidebar.pack(side="left", fill="y")
        self.navigation_sidebar.pack_propagate(False)
        
        self.navigation_sidebar_div = tk.Frame(self.main_layout, bg=self.accent_dim, width=1)
        self.navigation_sidebar_div.pack(side="left", fill="y")

        # Global Left Sidebar (for page specific content like Scenarios list)
        self.global_sidebar = tk.Frame(self.main_layout, bg="#141416", width=260)
        self.global_sidebar.pack(side="left", fill="y")
        self.global_sidebar.pack_propagate(False)

        # Divider Line
        self.global_sidebar_div = tk.Frame(self.main_layout, bg=self.accent_dim, width=1)
        self.global_sidebar_div.pack(side="left", fill="y")

        # Left handle button removed per user request

        # Right docking tab handle (will be created and packed by DetailsPage dynamically)
        self.btn_right_handle = None


        # Global Content Container (on the right of sidebar)
        self.container = tk.Frame(self.main_layout, bg=self.bg_dark)
        self.container.pack(side="left", fill="both", expand=True)

        # Top Content Frame inside Sidebar (dynamic depending on screen)
        self.sidebar_top_frame = tk.Frame(self.global_sidebar, bg="#141416")
        self.sidebar_top_frame.pack(fill="both", expand=True)

        # Initialize Far-Left Navigation tabs and toggles
        self.nav_tabs_frame = tk.Frame(self.navigation_sidebar, bg="#101012")
        self.nav_tabs_frame.pack(side="top", fill="x", pady=10)
        
        self.nav_toggles_frame = tk.Frame(self.navigation_sidebar, bg="#101012")
        self.nav_toggles_frame.pack(side="bottom", fill="x", pady=15)

        self.active_tab = "games"
        self.tab_buttons = {}
        tabs = [
            ("games", "🎮\nGAMES"),
            ("settings", "⚙️\nSETTINGS"),
            ("about", "ℹ️\nABOUT")
        ]
        
        for tab_id, text in tabs:
            btn = tk.Button(
                self.nav_tabs_frame, text=text, command=lambda t=tab_id: self.switch_global_tab(t),
                bg="#101012", fg="#9ca3af", activebackground="#202024",
                activeforeground=self.accent_glow, bd=0, relief="flat",
                font=("Segoe UI", 8, "bold"), pady=12
            )
            btn.pack(fill="x", pady=4)
            btn.tab_id = tab_id
            
            # Hover effects
            def make_hover(b):
                b.bind("<Enter>", lambda e: b.config(fg=self.fg_light, bg="#202024") if self.active_tab != b.tab_id else None)
                b.bind("<Leave>", lambda e: b.config(fg="#9ca3af", bg="#101012") if self.active_tab != b.tab_id else None)
            make_hover(btn)
            self.tab_buttons[tab_id] = btn

        # Toggle buttons at the bottom of the navigation sidebar
        # (self.btn_toggle_left_nav has been removed as per user request to simplify UI)

        def make_toggle_hover(b):
            b.bind("<Enter>", lambda e: b.config(fg=self.accent_glow, bg="#202024"))
            b.bind("<Leave>", lambda e: b.config(fg="#9ca3af", bg="#101012"))

        self.btn_collapse_nav = tk.Button(
            self.nav_toggles_frame, text="◀", command=self.toggle_navigation_sidebar,
            bg="#101012", fg="#9ca3af", activebackground="#202024",
            activeforeground=self.accent_glow, bd=0, relief="flat",
            font=("Segoe UI", 12), pady=8
        )
        self.btn_collapse_nav.pack(fill="x", pady=2)
        self.add_tooltip(self.btn_collapse_nav, "Collapse Navigation Sidebar")
        make_toggle_hover(self.btn_collapse_nav)

        # Import Page Classes
        from gui.pages.settings import SettingsPage
        from gui.pages.about import AboutPage

        # Initialize library, details, settings, and about pages
        self.library_page = LibraryPage(self.container, self)
        self.details_page = DetailsPage(self.container, self)
        self.settings_page = SettingsPage(self.container, self)
        self.about_page = AboutPage(self.container, self)
        
        # Bind logs interaction suggestions
        self.bind_log_actions()
        
        # Default view is Games Library (under Games tab)
        self.switch_global_tab("games")


    def start_drag(self, event):
        # Retrieve the specific top-level window being dragged (supports both self.root and self.overlay_hud)
        win = event.widget.winfo_toplevel()
        try:
            import ctypes
            hwnd = win.winfo_id()
            # If wrapped by wrapper frames, grab parent window first
            parent_hwnd = ctypes.windll.user32.GetParent(hwnd)
            target_hwnd = parent_hwnd if parent_hwnd else hwnd
            # Release mouse capture and trigger native Windows caption drag to avoid lag and redraw artifacts
            ctypes.windll.user32.ReleaseCapture()
            # WM_NCLBUTTONDOWN = 0xA1, HTCAPTION = 2
            ctypes.windll.user32.SendMessageW(target_hwnd, 0xA1, 2, 0)
        except Exception:
            # Fallback to manual Tkinter dragging
            self.drag_win = win
            self.drag_x = event.x_root
            self.drag_y = event.y_root
            self.win_x = win.winfo_x()
            self.win_y = win.winfo_y()

    def drag_window(self, event):
        # Fallback manual Tkinter dragging if native drag failed
        if hasattr(self, 'drag_win') and hasattr(self, 'drag_x'):
            dx = event.x_root - self.drag_x
            dy = event.y_root - self.drag_y
            self.drag_win.geometry(f"+{self.win_x + dx}+{self.win_y + dy}")

    def toggle_maximize(self):
        if self.is_maximized:
            self.root.state("normal")
            self.root.geometry(self.normal_geometry)
            self.max_btn.config(text="⬜")
            self.is_maximized = False
        else:
            self.normal_geometry = self.root.geometry()
            self.root.state("zoomed")
            self.max_btn.config(text="❐")
            self.is_maximized = True

    def minimize_window(self):
        self.root.state("iconic")

    def switch_global_tab(self, tab_id):
        self.active_tab = tab_id
        
        # Reset button styling
        for t_id, btn in self.tab_buttons.items():
            if t_id == tab_id:
                btn.config(bg="#202024", fg=self.accent_glow)
            else:
                btn.config(bg="#101012", fg="#9ca3af")
                
        # Hide all pages
        self.library_page.pack_forget()
        self.details_page.pack_forget()
        self.settings_page.pack_forget()
        self.about_page.pack_forget()
            
        # Show target page
        if tab_id == "games":
            self.show_library_page()
        elif tab_id == "settings":
            self.show_settings_page()
        elif tab_id == "about":
            self.show_about_page()

    def show_library_page(self):
        if self.hud_mode == "OVERLAY":
            self.toggle_hud_mode()
        self.details_page.pack_forget()
        self.settings_page.pack_forget()
        self.about_page.pack_forget()
        
        self.current_page = "library"
        self.repack_main_layout()
        
        self.library_page.pack(fill="both", expand=True)
        self.library_page.populate_library()
        
        self.log_message("SYSTEM", "INFO", "Entered Games Library catalog.")

    def show_settings_page(self):
        if self.hud_mode == "OVERLAY":
            self.toggle_hud_mode()
        self.library_page.pack_forget()
        self.details_page.pack_forget()
        self.about_page.pack_forget()
        
        self.current_page = "settings"
        self.repack_main_layout()
        
        self.settings_page.pack(fill="both", expand=True)
        self.settings_page.load_settings_values()
        
        self.log_message("SYSTEM", "INFO", "Opened global settings configuration dashboard.")

    def show_about_page(self):
        if self.hud_mode == "OVERLAY":
            self.toggle_hud_mode()
        self.library_page.pack_forget()
        self.details_page.pack_forget()
        self.settings_page.pack_forget()
        
        self.current_page = "about"
        self.repack_main_layout()
        
        self.about_page.pack(fill="both", expand=True)
        
        self.log_message("SYSTEM", "INFO", "Opened platform schematics and documentation view.")

    def show_details_page(self, game_id):
        # Set active game in config
        self.config.active_game_id = game_id
        self.config.save()
        self.update_game_directories()
        self.ensure_default_scenarios()
        self.load_scenarios()
        
        # Change title bar
        self.update_window_title_with_version()
        
        # Refresh treeview
        self.refresh_builds_tree()
        if hasattr(self, 'details_page'):
            self.details_page.populate_reports()
            
        # Hide all pages
        self.library_page.pack_forget()
        self.settings_page.pack_forget()
        self.about_page.pack_forget()
        
        self.current_page = "details"
        self.repack_main_layout()
        
        # Show details page
        self.details_page.pack(fill="both", expand=True)
        
        # Populate scenarios sidebar
        for w in self.sidebar_top_frame.winfo_children():
            w.destroy()
        create_sidebar(self, self.sidebar_top_frame)
        self.load_scenarios()
        
        # Highlight Games tab as active
        self.active_tab = "games"
        for t_id, btn in self.tab_buttons.items():
            if t_id == "games":
                btn.config(bg="#202024", fg=self.accent_glow)
            else:
                btn.config(bg="#101012", fg="#9ca3af")
                
        self.log_message("SYSTEM", "INFO", f"Entered Details Page dashboard for game: {game_id}")
        active_game = self.config.get_active_game()
        self.config.game_exe_path = active_game.get("active_exe_path", "")
        self.config.save()
        
        # Update directory targets
        self.update_game_directories()
        self.ensure_default_scenarios()
        self.load_scenarios()
        self.refresh_builds_tree()
        
        # Update UI bindings on Details page
        self.path_entry_var.set(self.config.game_exe_path)
        self.game_profile_var.set(active_game.get("title"))
        
        # Refresh historical reports tree
        self.details_page.populate_reports()
        
        # Switch visible packs
        self.library_page.pack_forget()
        self.details_page.pack(fill="both", expand=True)
        self.log_message("SYSTEM", "INFO", f"Hooked game profile details: {active_game.get('title')}")
        
        # Ensure header buttons are updated
        self.repack_left_buttons()
        self.repack_right_buttons()


    def refresh_all_data(self):
        self.update_game_directories()
        self.ensure_default_scenarios()
        self.load_scenarios()
        self.refresh_builds_tree()
        if hasattr(self, 'library_page'):
            self.library_page.populate_library()
        if hasattr(self, 'details_page'):
            self.details_page.populate_reports()
        self.log_message("SYSTEM", "INFO", "Refreshed catalog database and local configurations.")

    def show_documentation_dialog(self):
        doc_win = tk.Toplevel(self.root)
        doc_win.title("Automation & Test Flows Guide")
        doc_win.geometry("720x540+200+100")
        doc_win.configure(bg=self.bg_dark)
        doc_win.transient(self.root)
        doc_win.grab_set()
        doc_win.attributes("-topmost", True)
        
        lbl = tk.Label(doc_win, text="// PLATFORM AUTOMATION & SCENARIO GUIDE", bg=self.bg_dark, fg=self.accent_glow, font=("Consolas", 11, "bold"))
        lbl.pack(anchor="w", padx=15, pady=10)
        
        frame = tk.Frame(doc_win, bg=self.bg_panel, bd=1, highlightbackground=self.accent_dim, highlightthickness=1)
        frame.pack(fill="both", expand=True, padx=15, pady=(0, 15))
        
        scroll = ttk.Scrollbar(frame)
        scroll.pack(side="right", fill="y")
        
        text = tk.Text(frame, bg="#111114", fg=self.fg_light, bd=0, wrap="word", yscrollcommand=scroll.set, font=("Consolas", 9, "bold"))
        text.pack(side="left", fill="both", expand=True)
        scroll.config(command=text.yview)
        
        doc_path = os.path.join(self.base_dir, "test_flows.md")
        doc_content = ""
        if os.path.exists(doc_path):
            with open(doc_path, "r", encoding="utf-8") as f:
                doc_content = f.read()
        else:
            doc_content = "Documentation file 'test_flows.md' not found."
            
        text.insert("1.0", doc_content)
        text.config(state="disabled")

    def show_about_dialog(self):
        messagebox.showinfo("About Sylvan Salavan", "Sylvan-HUD Game Salavan Panel v2.9.0\n\nA professional automated game-testing platform styled after modern hardware controls, supporting Lua scenarios, OpenCV image classification, dynamic JUnit reporting, and OBS-style viewport capture.")

    def show_shortcuts_help(self):
        doc_win = tk.Toplevel(self.root)
        doc_win.title("Keyboard Shortcuts Help")
        doc_win.geometry("380x280")
        doc_win.configure(bg=self.bg_dark)
        doc_win.transient(self.root)
        doc_win.grab_set()
        doc_win.attributes("-topmost", True)
        
        # Center the window relative to root
        rx = self.root.winfo_x()
        ry = self.root.winfo_y()
        rw = self.root.winfo_width()
        rh = self.root.winfo_height()
        doc_win.geometry(f"+{rx + (rw - 380)//2}+{ry + (rh - 280)//2}")
        
        lbl = tk.Label(doc_win, text="// KEYBOARD SHORTCUTS REFERENCE", bg=self.bg_dark, fg=self.accent_glow, font=("Consolas", 11, "bold"))
        lbl.pack(anchor="w", padx=20, pady=(15, 10))
        
        frame = tk.Frame(doc_win, bg=self.bg_panel, bd=1, highlightbackground=self.accent_dim, highlightthickness=1)
        frame.pack(fill="both", expand=True, padx=20, pady=(0, 20))
        
        shortcuts = [
            ("Ctrl + P", "Pause / Resume Test Flow"),
            ("Ctrl + Q", "Abort Running Test"),
            ("Ctrl + O", "Toggle Float HUD Overlay"),
            ("Ctrl + Left Arrow", "Previous Step (⏮ PREV)"),
            ("Ctrl + Down Arrow", "Repeat Step (🔁 REPEAT)"),
            ("Ctrl + Right Arrow", "Next Step (⏭ NEXT)")
        ]
        
        for keys, desc in shortcuts:
            row = tk.Frame(frame, bg=self.bg_panel, pady=4)
            row.pack(fill="x", padx=15)
            tk.Label(row, text=keys, fg=self.accent_glow, bg=self.bg_panel, font=("Consolas", 9, "bold")).pack(side="left")
            tk.Label(row, text=f" - {desc}", fg=self.fg_light, bg=self.bg_panel, font=("Segoe UI", 9)).pack(side="left")
            
        btn = ttk.Button(doc_win, text="CLOSE", command=doc_win.destroy)
        btn.pack(pady=(0, 15))

    def bind_log_actions(self):
        self.tree.bind("<Button-3>", self.show_log_context_menu)
        self.tree.bind("<Double-1>", self.on_double_click_log)
        
        self.log_context_menu = tk.Menu(self.root, tearoff=0, bg="#111114", fg=self.fg_light, activebackground=self.accent_glow, activeforeground="#101012", font=("Consolas", 9, "bold"))

    def show_log_context_menu(self, event):
        selected_items = self.tree.selection()
        item = self.tree.identify_row(event.y)
        if not item:
            return
            
        if item not in selected_items:
            self.tree.selection_set(item)
            
        values = self.tree.item(item, "values")
        if not values or len(values) < 3:
            return
            
        step, result, message = values
        
        self.log_context_menu.delete(0, tk.END)
        
        selected_count = len(self.tree.selection())
        if selected_count > 1:
            self.log_context_menu.add_command(label=f"📋 Copy Selected Logs ({selected_count})", command=self.copy_to_clipboard)
        else:
            self.log_context_menu.add_command(label="📋 Copy Diagnostic Message", command=lambda: self.copy_to_clipboard(message))
            
        self.log_context_menu.add_separator()
        
        if result == "FAIL":
            self.log_context_menu.add_command(label="📸 View Error Screenshots", command=self.open_screenshots_dir)
            self.log_context_menu.add_command(label="📁 Open Active Reports Folder", command=self.open_active_reports_dir)
        else:
            self.log_context_menu.add_command(label="📁 Open Active Reports Folder", command=self.open_active_reports_dir)
            
        self.log_context_menu.add_command(label="📄 Open Game Player.log", command=self.open_game_player_log)
        self.log_context_menu.post(event.x_root, event.y_root)

    def on_double_click_log(self, event):
        selected = self.tree.selection()
        if not selected:
            return
        values = self.tree.item(selected[0], "values")
        if not values or len(values) < 3:
            return
        step, result, message = values
        if result == "FAIL":
            self.open_screenshots_dir()
        else:
            self.open_active_reports_dir()

    def copy_to_clipboard(self, text=None):
        selected = self.tree.selection()
        if not text and selected:
            if len(selected) > 1:
                lines = []
                for item in selected:
                    val = self.tree.item(item, "values")
                    if val and len(val) >= 3:
                        lines.append(f"[{val[1]}] {val[0]}: {val[2]}")
                text = "\n".join(lines)
            else:
                val = self.tree.item(selected[0], "values")
                if val and len(val) >= 3:
                    text = val[2]
        
        if text:
            self.root.clipboard_clear()
            self.root.clipboard_append(text)
            self.log_message("SYSTEM", "INFO", f"Copied {len(selected)} log(s) to clipboard.")

    def open_screenshots_dir(self):
        try:
            active_game = self.config.get_active_game()
            game_title = active_game.get("title", "Maou-Sama-TD")
            screenshots_dir = os.path.join(os.path.expanduser('~'), 'Documents', game_title, 'salavan', 'Screenshots')
            if not os.path.exists(screenshots_dir):
                os.makedirs(screenshots_dir, exist_ok=True)
            os.startfile(screenshots_dir)
        except Exception as e:
            messagebox.showerror("Error", f"Could not open screenshots folder: {str(e)}")

    def open_active_reports_dir(self):
        try:
            active_game = self.config.get_active_game()
            game_title = active_game.get("title", "Maou-Sama-TD")
            reports_dir = os.path.join(os.path.expanduser('~'), 'Documents', game_title, 'salavan', 'Reports')
            if not os.path.exists(reports_dir):
                os.makedirs(reports_dir, exist_ok=True)
            os.startfile(reports_dir)
        except Exception as e:
            messagebox.showerror("Error", f"Could not open reports folder: {str(e)}")

    def open_game_player_log(self):
        try:
            active_game = self.config.get_active_game()
            log_path_raw = active_game.get("log_path", "")
            if log_path_raw:
                p = os.path.normpath(os.path.expandvars(log_path_raw))
                if os.path.exists(p):
                    os.startfile(p)
                    return
            messagebox.showwarning("Log File Not Found", "Player.log is not present or has not been generated yet.")
        except Exception as e:
            messagebox.showerror("Error", f"Could not open log file: {str(e)}")

    def toggle_logs_docking(self):
        if not hasattr(self, 'logs_popped_out'):
            self.logs_popped_out = False
            
        if not self.logs_popped_out:
            self.logs_popped_out = True
            if hasattr(self, 'btn_pop_logs') and self.btn_pop_logs:
                self.btn_pop_logs.config(text="[ ⤓ DOCK ]")
            
            # Update header buttons to show dock button
            self.repack_right_buttons()
            
            self.logs_window = tk.Toplevel(self.root)
            self.logs_window.title("Sylvan HUD Diagnostic Logs")
            
            # Position pop-out window close to main window and on top
            mx = self.root.winfo_x()
            my = self.root.winfo_y()
            mw = self.root.winfo_width()
            mh = self.root.winfo_height()
            pop_x = mx + (mw - 600) // 2
            pop_y = my + (mh - 500) // 2
            
            self.logs_window.geometry(f"600x500+{pop_x}+{pop_y}")
            self.logs_window.configure(bg=self.bg_dark)
            self.logs_window.transient(self.root)
            self.logs_window.attributes("-topmost", True)
            self.logs_window.protocol("WM_DELETE_WINDOW", self.toggle_logs_docking)
            
            # Remove from parent Details Page PanedWindow
            if hasattr(self, 'details_page') and self.details_page.winfo_exists():
                self.details_page.paned_window.forget(self.details_page.right_pane)
                
            # Repack right_border inside the new popup window
            self.right_border.pack(in_=self.logs_window, fill="both", expand=True, padx=10, pady=10)
            self.log_message("SYSTEM", "INFO", "Diagnostic Logs popped out to separate window.")
        else:
            self.logs_popped_out = False
            
            # Unpack from popup window
            self.right_border.pack_forget()
            
            # Add back to Details page PanedWindow (at the bottom)
            if hasattr(self, 'details_page') and self.details_page.winfo_exists():
                self.details_page.paned_window.add(self.details_page.right_pane, minsize=35, height=220, stretch="never")
                self.right_border.pack(in_=self.details_page.right_pane, fill="both", expand=True, padx=15, pady=(5, 15))
                
            # Destroy window
            if hasattr(self, 'logs_window') and self.logs_window:
                self.logs_window.destroy()
                self.logs_window = None
                
            if hasattr(self, 'btn_pop_logs') and self.btn_pop_logs:
                self.btn_pop_logs.config(text="[ ⧉ POP OUT ]")
                
            self.repack_right_buttons()
            
            if self.right_collapsed:
                self.toggle_right_panel()
                
            self.log_message("SYSTEM", "INFO", "Diagnostic Logs docked back to operations dashboard.")

    def setup_file_drag_drop(self):
        if windnd:
            try:
                # Use hook_dropfiles from windnd package
                windnd.hook_dropfiles(self.root, func=self.on_files_dropped)
                self.log_message("SYSTEM", "INFO", "File Drag-and-Drop listener registered successfully.")
            except Exception as e:
                self.log_message("SYSTEM", "WARN", f"Failed to register Drag-and-Drop: {str(e)}")

    def on_files_dropped(self, files):
        if not hasattr(self, 'scenarios_dir') or not self.scenarios_dir:
            return
            
        imported_count = 0
        try:
            existing_files = sorted(fl for fl in os.listdir(self.scenarios_dir) if fl.endswith(".lua"))
            next_prefix = len(existing_files) + 1
        except Exception:
            next_prefix = 1
            
        for f in files:
            try:
                if isinstance(f, bytes):
                    filepath = f.decode('utf-8', errors='ignore')
                else:
                    filepath = str(f)
                
                # Strip wrapping braces from paths containing spaces
                filepath = filepath.strip("{}").strip()
                
                if not filepath.lower().endswith(".lua"):
                    continue
                
                if not os.path.exists(filepath):
                    continue
                
                filename = os.path.basename(filepath)
                
                import re
                clean_name = re.sub(r'^\d+_', '', filename)
                target_filename = f"{next_prefix}_{clean_name}"
                dest_path = os.path.join(self.scenarios_dir, target_filename)
                
                import shutil
                shutil.copy2(filepath, dest_path)
                self.log_message("SYSTEM", "INFO", f"Imported scenario: {target_filename}")
                imported_count += 1
                next_prefix += 1
            except Exception as e:
                self.log_message("SYSTEM", "ERROR", f"Failed to import scenario file: {str(e)}")
                
        if imported_count > 0:
            if hasattr(self, '_scenario_steps_cache'):
                self._scenario_steps_cache.clear()
            if hasattr(self, '_last_sidebar_state'):
                del self._last_sidebar_state
            
            self.load_scenarios()
            if hasattr(self, 'update_custom_sidebar'):
                self.update_custom_sidebar()

    def import_scenarios_dialog(self):
        from tkinter import filedialog
        files = filedialog.askopenfilenames(
            title="Import Scenario Files",
            filetypes=[("Lua Scenarios", "*.lua"), ("All Files", "*.*")]
        )
        if files:
            self.on_files_dropped(files)

    def update_window_title_with_version(self):
        active_game = self.config.get_active_game()
        game_title = active_game.get("title", "Maou-Sama-TD")
        
        active_ver = "No Active Build"
        for b in self.config.builds:
            if b.get("game_id", "maou_sama_td") == self.config.active_game_id:
                if b.get("path") == self.config.game_exe_path:
                    active_ver = f"v{b.get('version', '')}"
                    break
        
        title_str = f" 🛡️  SYLVAN-HUD GAME SALAVAN PANEL v2.9.0 | {game_title.upper()} [{active_ver}]"
        if hasattr(self, 'title_lbl') and self.title_lbl:
            self.title_lbl.config(text=title_str)

    def find_game_windows(self):
        import time
        now = time.time()
        if hasattr(self, '_last_hook_time') and (now - self._last_hook_time < 1.0):
            return getattr(self, '_cached_game_windows', [])
        self._last_hook_time = now

        import os
        import psutil
        try:
            import ctypes
            from ctypes import wintypes
            has_ctypes = True
        except ImportError:
            has_ctypes = False

        active_game = self.config.get_active_game()
        window_title = active_game.get("window_title", "Maou-Sama-TD")
        process_name = active_game.get("process_name", "Maou-Sama-TD.exe").lower()
        
        # Override process name using active executable config path if set
        config_exe = self.config.game_exe_path
        if config_exe:
            process_name = os.path.basename(config_exe).lower()
            
        my_pid = os.getpid()

        # Helper to safely retrieve process filename using Win32 API
        def get_process_name_by_pid(pid):
            if not has_ctypes:
                return ""
            try:
                # PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
                h_process = ctypes.windll.kernel32.OpenProcess(0x1000, False, pid)
                if h_process:
                    try:
                        buf = ctypes.create_unicode_buffer(1024)
                        size = wintypes.DWORD(1024)
                        if ctypes.windll.kernel32.QueryFullProcessImageNameW(h_process, 0, buf, ctypes.byref(size)):
                            return os.path.basename(buf.value).lower()
                    finally:
                        ctypes.windll.kernel32.CloseHandle(h_process)
            except Exception:
                pass
            return ""

        # Helper to retrieve window class name to exclude system windows and editors
        def get_window_class_by_hwnd(hwnd):
            if not has_ctypes:
                return ""
            try:
                buf = ctypes.create_unicode_buffer(256)
                ctypes.windll.user32.GetClassNameW(hwnd, buf, 256)
                return buf.value
            except Exception:
                return ""

        # Let's find all windows containing the title
        all_candidate_windows = gw.getWindowsWithTitle(window_title)
        
        # We will categorize the candidate windows
        exact_process_matches = []
        editor_matches = []
        fallback_matches = []
        
        # Keep lists of blacklisted process names and window classes to avoid hooking IDE/file browser windows
        EXCLUDED_PROCESSES = {
            "explorer.exe", "cmd.exe", "powershell.exe", "chrome.exe", "msedge.exe", 
            "firefox.exe", "code.exe", "cursor.exe", "antigravity ide.exe", 
            "antigravity_tools.exe", "python.exe", "pythonw.exe", "node.exe", 
            "electron.exe", "brave.exe", "discord.exe", "sharex.exe"
        }
        
        EXCLUDED_CLASSES = {
            "Chrome_WidgetWin_1", # Electron/VS Code/Antigravity IDE/Browsers/Unity Hub
            "CabinetWClass",      # File Explorer
            "TkTopLevel",         # Tkinter windows (including our own)
            "ConsoleWindowClass", # Command Prompt / PowerShell
            "Progman",            # Desktop
            "Shell_TrayWnd"       # Taskbar
        }
        
        for w in all_candidate_windows:
            if not w.title:
                continue
            
            # Exclude our own window (Sylvan-HUD Game Tester Panel)
            title_upper = w.title.upper()
            if "SYLVAN-HUD" in title_upper or "GAME TESTER PANEL" in title_upper or "SALAVAN" in title_upper:
                continue
                
            # Exclude IDE windows (Antigravity IDE, VS Code, Visual Studio, Cursor, etc.)
            if any(x in title_upper for x in ["ANTIGRAVITY", "IDE", "VISUAL STUDIO", "CURSOR", "VSCODE", "SUBLIME", "CLION", "RIDER", "GEMINI", "WORKSPACE"]):
                continue

            # Strict Win32 class checks to filter out common UI shells/browsers/IDEs
            if has_ctypes:
                w_class = get_window_class_by_hwnd(w._hWnd)
                if w_class in EXCLUDED_CLASSES:
                    continue
                
            pid_val = None
            p_name = ""
            if has_ctypes:
                try:
                    hwnd = w._hWnd
                    pid = wintypes.DWORD()
                    ctypes.windll.user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
                    pid_val = pid.value
                    if pid_val == my_pid:
                        continue
                        
                    # Safely query process name via ctypes first (handles elevated permissions)
                    p_name = get_process_name_by_pid(pid_val)
                    if not p_name:
                        # Fallback to psutil
                        try:
                            p = psutil.Process(pid_val)
                            p_name = p.name().lower()
                        except Exception:
                            pass
                except Exception:
                    pass

            if p_name:
                p_name_lower = p_name.lower()
                # Exclude IDE/python/editor/system processes explicitly!
                if p_name_lower in EXCLUDED_PROCESSES or any(x in p_name_lower for x in ["python", "node", "electron", "code", "cursor", "antigravity", "gemini", "editor", "deity"]):
                    continue
                    
                if p_name_lower == process_name:
                    exact_process_matches.append(w)
                elif self.config.hook_unity_editor and p_name_lower == "unity.exe" and window_title.lower() in w.title.lower():
                    editor_matches.append(w)
                elif p_name_lower == "unity.exe":
                    continue
                continue
                
            # Fallback title match (only if process name could not be resolved)
            if w.title == window_title:
                if not any(x in title_upper for x in [" - ", "VSCODE", "VISUAL STUDIO", "DEITY", "ANTIGRAVITY", "GEMINI", "WORKSPACE"]):
                    fallback_matches.append(w)
            elif w.title.startswith(window_title):
                if not any(x in title_upper for x in [" - ", "VSCODE", "VISUAL STUDIO", "DEITY", "ANTIGRAVITY", "GEMINI", "WORKSPACE"]):
                    fallback_matches.append(w)
                
        if exact_process_matches:
            self._cached_game_windows = exact_process_matches
            return exact_process_matches
        if editor_matches:
            self._cached_game_windows = editor_matches
            return editor_matches
        if fallback_matches:
            self._cached_game_windows = fallback_matches
            return fallback_matches
            
        self._cached_game_windows = []
        return []


    def background_auto_hook(self):
        if not self.stop_flag:
            try:
                win = self.find_game_windows()
                
                if win:
                    scene_info = ""
                    if self.config.auto_sync_ui:
                        state = self.read_game_state()
                        if state:
                            scene = state.get("current_scene", "Unknown")
                            is_dial = state.get("is_dialogue_active", False)
                            scene_info = f" [Scene: {scene}"
                            if is_dial:
                                scene_info += " | Dialogue Active"
                            scene_info += "]"
                    
                    current_status = self.stage_lbl["text"]
                    is_running = self.test_thread and self.test_thread.is_alive()
                    
                    if is_running:
                        base_stage = current_status.split(" [Scene:")[0]
                        self.stage_lbl.config(text=f"{base_stage}{scene_info}")
                    else:
                        self.set_stage_lbl(f"Hooked (Game Running){scene_info}")
                    self.status_dot.config(fg=self.success_glow)
                else:
                    current_status = self.stage_lbl["text"].upper()
                    if "HOOKED" in current_status:
                        self.set_stage_lbl("Idle")
                        self.status_dot.config(fg=self.alert_yellow)
            except Exception:
                pass
            self.root.after(2000, self.background_auto_hook)

    def add_tooltip(self, widget, text):
        ToolTip(widget, text)

    def on_close(self):
        self.stop_flag = True
        self.release_all_inputs()   # <-- always clean up on close
        if hasattr(self, 'capture_thread'):
            self.capture_thread.running = False
            self.capture_thread.stop_recording()
        if self.log_monitor:
            self.log_monitor.running = False
        self.root.destroy()

    def refresh_builds_tree(self):
        self.builds_tree.delete(*self.builds_tree.get_children())
        active_game_id = self.config.active_game_id
        for b in self.config.builds:
            if b.get("game_id", "maou_sama_td") == active_game_id:
                self.builds_tree.insert(
                    "", "end",
                    values=(b.get("title", ""), b.get("version", ""), b.get("status", "Pending"), b.get("last_tested", "-"))
                )

    def select_active_build(self):
        selected = self.builds_tree.selection()
        if not selected:
            messagebox.showwarning("Select Build", "Please select a build from the database first!")
            return
            
        values = self.builds_tree.item(selected[0], "values")
        title = values[0]
        version = values[1]
        
        for b in self.config.builds:
            if b.get("game_id", "maou_sama_td") == self.config.active_game_id and b.get("title") == title and b.get("version") == version:
                self.config.game_exe_path = b.get("path")
                self.path_entry_var.set(self.config.game_exe_path)
                self.config.save()
                self.log_message("SYSTEM", "INFO", f"Active build set to: {title} ({version})")
                return

    def delete_selected_build(self):
        selected = self.builds_tree.selection()
        if not selected:
            messagebox.showwarning("Select Build", "Please select a build to delete!")
            return
            
        values = self.builds_tree.item(selected[0], "values")
        title = values[0]
        version = values[1]
        
        self.config.builds = [
            b for b in self.config.builds 
            if not (b.get("game_id", "maou_sama_td") == self.config.active_game_id and b.get("title") == title and b.get("version") == version)
        ]
        self.config.save()
        self.refresh_builds_tree()
        self.log_message("SYSTEM", "INFO", f"Removed build: {title} ({version})")

    def show_builds_context_menu(self, event):
        item = self.builds_tree.identify_row(event.y)
        if not item:
            return
            
        self.builds_tree.selection_set(item)
        values = self.builds_tree.item(item, "values")
        if not values or len(values) < 2:
            return
            
        title, version = values[0], values[1]
        
        # Create menu
        menu = tk.Menu(self.root, tearoff=0, bg="#111114", fg=self.fg_light, activebackground=self.accent_glow, activeforeground="#101012", font=("Segoe UI", 9, "bold"))
        
        # Edit Status option
        menu.add_command(label="✏️ Edit Build Status / Summary...", command=lambda: self.show_edit_build_status_dialog(title, version))
        menu.add_separator()
        
        # Rerun Test option
        menu.add_command(label="🔄 Rerun Active Scenario", command=self.start_test_flow)
        
        # Select active build option
        menu.add_command(label="🎯 Set as Active Run Target", command=self.select_active_build)
        
        menu.post(event.x_root, event.y_root)

    def show_edit_build_status_dialog(self, title, version):
        # Find the build object
        target_build = None
        for b in self.config.builds:
            if b.get("game_id", "maou_sama_td") == self.config.active_game_id and b.get("title") == title and b.get("version") == version:
                target_build = b
                break
                
        if not target_build:
            messagebox.showerror("Error", "Selected build not found in config database.")
            return
            
        dialog = tk.Toplevel(self.root)
        dialog.title("Edit Build Status")
        dialog.geometry("380x260+200+200")
        dialog.configure(bg=self.bg_dark)
        dialog.transient(self.root)
        dialog.grab_set()
        dialog.attributes("-topmost", True)
        
        lbl = tk.Label(dialog, text=f"// EDIT BUILD: {title.upper()}", bg=self.bg_dark, fg=self.accent_glow, font=("Segoe UI", 10, "bold"))
        lbl.pack(anchor="w", padx=15, pady=10)
        
        # Status Selection combobox
        f1 = tk.Frame(dialog, bg=self.bg_dark)
        f1.pack(fill="x", padx=15, pady=5)
        tk.Label(f1, text="Status/Verdict:", bg=self.bg_dark, fg=self.fg_light, font=("Segoe UI", 9, "bold")).pack(side="left")
        status_var = tk.StringVar(value=target_build.get("status", "Pending"))
        status_combo = ttk.Combobox(f1, textvariable=status_var, values=["Pending", "Tested", "Success", "Failed", "3/5 Passed", "4/5 Passed"], state="normal", font=("Segoe UI", 9, "bold"), width=16)
        status_combo.pack(side="right")
        
        # Summary Note entry
        f2 = tk.Frame(dialog, bg=self.bg_dark)
        f2.pack(fill="x", padx=15, pady=5)
        tk.Label(f2, text="Tests Summary (e.g. 3/5):", bg=self.bg_dark, fg=self.fg_light, font=("Segoe UI", 9, "bold")).pack(side="left")
        summary_var = tk.StringVar(value=target_build.get("summary", ""))
        summary_ent = tk.Entry(f2, textvariable=summary_var, bg="#151518", fg=self.fg_light, insertbackground=self.fg_light, bd=1, highlightbackground=self.accent_dim, font=("Segoe UI", 9, "bold"), width=18)
        summary_ent.pack(side="right")
        
        # Last Tested datetime entry
        f3 = tk.Frame(dialog, bg=self.bg_dark)
        f3.pack(fill="x", padx=15, pady=5)
        tk.Label(f3, text="Last Run Timestamp:", bg=self.bg_dark, fg=self.fg_light, font=("Segoe UI", 9, "bold")).pack(side="left")
        time_var = tk.StringVar(value=target_build.get("last_tested", "-"))
        time_ent = tk.Entry(f3, textvariable=time_var, bg="#151518", fg=self.fg_light, insertbackground=self.fg_light, bd=1, highlightbackground=self.accent_dim, font=("Segoe UI", 9, "bold"), width=18)
        time_ent.pack(side="right")
        
        # Save command
        def on_save():
            new_status = status_var.get().strip()
            new_summary = summary_var.get().strip()
            
            target_build["status"] = new_status
            target_build["summary"] = new_summary
            if new_summary:
                target_build["status"] = f"{new_status} ({new_summary})" if new_status in ["Tested", "Success", "Failed"] else new_summary
            target_build["last_tested"] = time_var.get().strip()
            
            self.config.save()
            self.refresh_builds_tree()
            dialog.destroy()
            self.log_message("DATABASE", "INFO", f"Updated status of build: {title} to {target_build['status']}")
            
        btn_save = ttk.Button(dialog, text="SAVE BUILD CHANGES", command=on_save)
        btn_save.pack(anchor="e", padx=15, pady=15)

    def on_build_select(self, event=None):
        if not hasattr(self, 'lbl_selected_build') or not self.lbl_selected_build:
            return
            
        selected = self.builds_tree.selection()
        if not selected:
            self.lbl_selected_build.config(text="NO BUILD SELECTED")
            self.status_selected_build_status = "Pending"
            self.status_summary_var.set("")
            if hasattr(self, 'update_status_buttons_ui'):
                self.update_status_buttons_ui()
            return
            
        values = self.builds_tree.item(selected[0], "values")
        title = values[0]
        version = values[1]
        
        target_build = None
        for b in self.config.builds:
            if b.get("game_id", "maou_sama_td") == self.config.active_game_id and b.get("title") == title and b.get("version") == version:
                target_build = b
                break
                
        if target_build:
            self.lbl_selected_build.config(text=f"BUILD: {title} ({version})")
            status_str = target_build.get("status", "Pending")
            raw_status = "Pending"
            summary_str = target_build.get("summary", "")
            
            if "(" in status_str:
                parts = status_str.split("(")
                raw_status = parts[0].strip()
                if not summary_str:
                    summary_str = parts[1].replace(")", "").strip()
            else:
                if status_str in ["Success", "Failed", "Pending", "Tested"]:
                    if status_str == "Tested":
                        raw_status = "Success"
                    else:
                        raw_status = status_str
                else:
                    raw_status = "Pending"
                    if not summary_str:
                        summary_str = status_str
                        
            self.status_selected_build_status = raw_status
            self.status_summary_var.set(summary_str)
            if hasattr(self, 'update_status_buttons_ui'):
                self.update_status_buttons_ui()

    def save_selected_build_status(self):
        selected = self.builds_tree.selection()
        if not selected:
            messagebox.showwarning("Select Build", "Please select a build from the database first!")
            return
            
        values = self.builds_tree.item(selected[0], "values")
        title = values[0]
        version = values[1]
        
        target_build = None
        for b in self.config.builds:
            if b.get("game_id", "maou_sama_td") == self.config.active_game_id and b.get("title") == title and b.get("version") == version:
                target_build = b
                break
                
        if not target_build:
            messagebox.showerror("Error", "Selected build not found in config database.")
            return
            
        new_status = self.status_selected_build_status
        new_summary = self.status_summary_var.get().strip()
        
        target_build["status"] = new_status
        target_build["summary"] = new_summary
        if new_summary:
            target_build["status"] = f"{new_status} ({new_summary})" if new_status in ["Success", "Failed", "Pending"] else new_summary
            
        target_build["last_tested"] = time.strftime("%Y-%m-%d %H:%M")
        
        self.config.save()
        self.refresh_builds_tree()
        self.log_message("DATABASE", "INFO", f"Manually updated status of build {title} ({version}) to: {target_build['status']}")

    def rerun_selected_build(self):
        selected = self.builds_tree.selection()
        if not selected:
            messagebox.showwarning("Select Build", "Please select a build to rerun!")
            return
            
        self.select_active_build()
        self.start_test_flow()

    def clear_logs(self):
        self.web_log_buffer = []
        if hasattr(self, 'tree') and self.tree:
            self.tree.delete(*self.tree.get_children())
        if hasattr(self, 'console_text') and self.console_text:
            self.console_text.config(state="normal")
            self.console_text.delete("1.0", tk.END)
            self.console_text.config(state="disabled")
        if hasattr(self, 'overlay_console_text') and self.overlay_console_text:
            self.overlay_console_text.config(state="normal")
            self.overlay_console_text.delete("1.0", tk.END)
            self.overlay_console_text.config(state="disabled")
        self.log_message("SYSTEM", "INFO", "Diagnostic logs cleared.")

    def delete_all_reports(self):
        active_game = self.config.get_active_game()
        game_title = active_game.get("title", "Maou-Sama-TD")
        reports_dir = os.path.join(
            os.path.expanduser('~'), 'Documents', game_title, 'salavan', 'Reports'
        )
        if not os.path.exists(reports_dir):
            return
            
        if messagebox.askyesno("Purge Reports", "Are you sure you want to delete all historical JUnit reports? This cannot be undone."):
            try:
                import shutil
                shutil.rmtree(reports_dir)
                os.makedirs(reports_dir, exist_ok=True)
                self.log_message("SYSTEM", "INFO", "Purged all historical reports.")
                if hasattr(self, 'details_page'):
                    self.details_page.populate_reports()
            except Exception as e:
                messagebox.showerror("Error", f"Failed to delete reports: {str(e)}")

    def update_game_directories(self):
        active_game_id = self.config.active_game_id
        self.template_dir = os.path.join(self.base_dir, "templates", active_game_id)
        self.scenarios_dir = os.path.join(self.base_dir, "scenarios", active_game_id)
        os.makedirs(self.template_dir, exist_ok=True)
        os.makedirs(self.scenarios_dir, exist_ok=True)
        os.makedirs(self.recordings_dir, exist_ok=True)

    def migrate_old_assets(self):
        old_scenarios_dir = os.path.join(self.base_dir, "scenarios")
        target_scenarios_dir = os.path.join(self.base_dir, "scenarios", "maou_sama_td")
        if os.path.exists(old_scenarios_dir) and os.path.isdir(old_scenarios_dir):
            for item in os.listdir(old_scenarios_dir):
                item_path = os.path.join(old_scenarios_dir, item)
                if os.path.isfile(item_path) and item.endswith(".lua"):
                    os.makedirs(target_scenarios_dir, exist_ok=True)
                    try:
                        shutil.move(item_path, os.path.join(target_scenarios_dir, item))
                        self.log_message("SYSTEM", "INFO", f"Migrated scenario: {item}")
                    except Exception:
                        pass

        old_templates_dir = os.path.join(self.base_dir, "templates")
        target_templates_dir = os.path.join(self.base_dir, "templates", "maou_sama_td")
        if os.path.exists(old_templates_dir) and os.path.isdir(old_templates_dir):
            for item in os.listdir(old_templates_dir):
                item_path = os.path.join(old_templates_dir, item)
                if os.path.isfile(item_path) and item.endswith(".png"):
                    os.makedirs(target_templates_dir, exist_ok=True)
                    try:
                        shutil.move(item_path, os.path.join(target_templates_dir, item))
                        self.log_message("SYSTEM", "INFO", f"Migrated template: {item}")
                    except Exception:
                        pass

    def update_game_profiles_list(self):
        titles = [g.get("title") for g in self.config.games]
        self.game_profile_combo["values"] = titles
        active_game = self.config.get_active_game()
        self.game_profile_var.set(active_game.get("title"))

    def on_game_profile_changed(self, event=None):
        selected_title = self.game_profile_var.get()
        new_active = None
        for g in self.config.games:
            if g.get("title") == selected_title:
                new_active = g
                break
        if new_active:
            self.config.active_game_id = new_active.get("id")
            self.config.game_exe_path = new_active.get("active_exe_path", "")
            self.path_entry_var.set(self.config.game_exe_path)
            
            self.config.save()
            self.update_game_directories()
            self.ensure_default_scenarios()
            self.load_scenarios()
            self.refresh_builds_tree()
            self.log_message("SYSTEM", "INFO", f"Switched active profile to: {selected_title}")

    def manage_game_profiles(self):
        show_manage_games_dialog(self)

    def load_scenarios(self):
        if hasattr(self, '_scenario_steps_cache'):
            self._scenario_steps_cache.clear()
        if not hasattr(self, 'scenario_listbox') or not self.scenario_listbox:
            return
        self.scenario_listbox.delete(0, tk.END)
        files = [f for f in os.listdir(self.scenarios_dir) if f.endswith(".lua")]
        files.sort()
        for f in files:
            name = os.path.splitext(f)[0]
            self.scenario_listbox.insert(tk.END, name)
        if files:
            self.scenario_listbox.selection_set(0)
        if hasattr(self, 'update_custom_sidebar'):
            self.update_custom_sidebar()


    def bind_hotkeys(self):
        for key in ["pause", "abort", "toggle_mode", "next", "prev", "repeat"]:
            try:
                self.root.unbind_all(self.config.hotkeys.get(key))
            except Exception:
                pass
        try:
            self.root.bind_all(self.config.hotkeys.get("pause"), lambda event: self.toggle_pause())
            self.root.bind_all(self.config.hotkeys.get("abort"), lambda event: self.stop_test_flow())
            self.root.bind_all(self.config.hotkeys.get("toggle_mode"), lambda event: self.toggle_hud_mode())
            self.root.bind_all(self.config.hotkeys.get("next"), lambda event: self.next_step())
            self.root.bind_all(self.config.hotkeys.get("prev"), lambda event: self.prev_step())
            self.root.bind_all(self.config.hotkeys.get("repeat"), lambda event: self.repeat_step())
        except Exception as e:
            self.log_message("SYSTEM", "FAIL", f"Shortcut bind failed: {str(e)}")

    def log_message(self, step, result, message):
        self.root.after(0, self._add_log_to_tree, step, result, message)
        if result == "FAIL":
            self.pause_event.clear()
            self.root.after(0, self._set_ui_paused_on_fail)

    def _set_ui_paused_on_fail(self):
        if hasattr(self, 'btn_pause') and self.btn_pause:
            self.btn_pause.config(text="RESUME")
            self.set_control_states("normal", "normal", "normal", "RESUME")
        if hasattr(self, 'btn_overlay_pause') and self.btn_overlay_pause:
            self.btn_overlay_pause.config(text="RESUME")
        
    def _add_log_to_tree(self, step, result, message):
        timestamp = time.strftime("%H:%M:%S")
        if not hasattr(self, 'web_log_buffer'):
            self.web_log_buffer = []
        self.web_log_buffer.append({
            "timestamp": timestamp,
            "source": step,
            "result": result,
            "detail": message
        })
        if len(self.web_log_buffer) > 100:
            self.web_log_buffer.pop(0)
            
        item = self.tree.insert("", "end", values=(step, result, message), tags=(result,))
        self.tree.see(item)
        self.report_logger.log(step, result, message)
        self.write_to_compact_console(step, result, message)
        if result == "FAIL":
            threading.Thread(target=self.capture_error_screenshot, args=(step,), daemon=True).start()

    def write_to_compact_console(self, step, result, message):
        symbol = "+" if result == "PASS" else "-" if result == "FAIL" else ">"
        color_tag = "pass" if result == "PASS" else "fail" if result == "FAIL" else "info"
        log_str = f"[{symbol}] {step}: {message}\n"
        
        if hasattr(self, 'console_text'):
            try:
                self.console_text.config(state="normal")
                self.console_text.insert(tk.END, log_str, color_tag)
                self.console_text.see(tk.END)
                self.console_text.config(state="disabled")
            except Exception:
                pass
                
        if hasattr(self, 'full_logs_text'):
            try:
                self.full_logs_text.config(state="normal")
                self.full_logs_text.insert(tk.END, log_str, color_tag)
                self.full_logs_text.see(tk.END)
                self.full_logs_text.config(state="disabled")
            except Exception:
                pass

    def capture_error_screenshot(self, step_name):
        if not hasattr(self, 'current_screenshots_dir') or not self.current_screenshots_dir:
            return
        rect = self.get_game_rect()
        if rect:
            gx, gy, gw_w, gw_h = rect
            try:
                screenshot = ImageGrab.grab(bbox=(gx, gy, gx + gw_w, gy + gw_h))
                os.makedirs(self.current_screenshots_dir, exist_ok=True)
                safe_step = "".join(c for c in str(step_name) if c.isalnum() or c in (' ', '_', '-')).strip().replace(' ', '_')
                filename = f"error_{safe_step}_{time.strftime('%H%M%S')}.png"
                screenshot.save(os.path.join(self.current_screenshots_dir, filename))
                self.log_message("SYSTEM", "INFO", f"Saved error screenshot: {filename}")
            except Exception:
                pass

    def browse_game_exe(self):
        filepath = filedialog.askopenfilename(title="Select Game Executable", filetypes=[("Executable Files", "*.exe")])
        if filepath:
            self.config.game_exe_path = filepath
            self.path_entry_var.set(filepath)
            self.config.save()
            self.log_message("SYSTEM", "INFO", f"Target verified: {os.path.basename(filepath)}")

    def set_stage_lbl(self, text):
        # Reset current step skip flag
        self.skip_current_step = False
        
        tech_status = f"HUD STATUS: {text.upper()}"
        if hasattr(self, 'stage_lbl'):
            if self.stage_lbl["text"] != tech_status:
                self.root.after(0, lambda: self.stage_lbl.config(text=tech_status))
                
        # Find index of this step and determine if it should be skipped
        selected_indices = self.scenario_listbox.curselection()
        selected_scenario = self.scenario_listbox.get(selected_indices[0]) if selected_indices else None
        
        self.current_step_is_skipped = False
        if selected_scenario:
            script_path = os.path.join(self.scenarios_dir, f"{selected_scenario}.lua")
            steps = self._scenario_steps_cache.get(script_path, [])
            
            # Find the active step index
            active_idx = -1
            text_clean = re.sub(r'^\d+\.\s*', '', text).lower().strip()
            for s_idx, step_name in enumerate(steps):
                s_clean = re.sub(r'^\d+\.\s*', '', step_name).lower().strip()
                if s_clean in text_clean or text_clean in s_clean:
                    active_idx = s_idx
                    break
                    
            self.current_step_idx = active_idx if active_idx != -1 else None
            
            # Check auto skip to step index
            if self.auto_skip_to_step_idx is not None and active_idx != -1:
                if active_idx < self.auto_skip_to_step_idx:
                    self.current_step_is_skipped = True
                    self.log_message("DEBUG", "INFO", f"Auto-skipping step '{text}' (rewinding/jumping)...")
                else:
                    self.auto_skip_to_step_idx = None
                    if getattr(self, 'pause_at_target', False):
                        self.pause_at_target = False
                        self.pause_event.clear()
                        self.root.after(0, lambda: self.btn_pause.config(text="RESUME"))
                        self.log_message("DEBUG", "INFO", f"Reached target step: '{text}'. Pausing execution.")
                        
            # Check user marked skip
            if active_idx != -1 and active_idx < len(steps):
                step_name = steps[active_idx]
                if self.skipped_steps.get((selected_scenario, step_name), False):
                    self.current_step_is_skipped = True
                    self.log_message("DEBUG", "INFO", f"Skipping step '{text}' (marked as skip)...")
                    
        # Check run_to_next_step step debugger pause
        if getattr(self, 'run_to_next_step', False):
            self.run_to_next_step = False
            self.pause_event.clear()
            self.root.after(0, lambda: self.btn_pause.config(text="RESUME"))
            self.log_message("DEBUG", "INFO", f"Stepped to: '{text}'. Pausing execution.")

        if hasattr(self, 'update_custom_sidebar'):
            self.root.after(0, self.update_custom_sidebar)

    def toggle_pause(self):
        if self.pause_event.is_set():
            self.pause_event.clear()
            self.btn_pause.config(text="RESUME")
            self.log_message("HUD", "INFO", "Sequence execution SUSPENDED.")
        else:
            self.pause_event.set()
            self.btn_pause.config(text="PAUSE")
            self.log_message("HUD", "INFO", "Sequence execution RESUMED.")

    def check_paused(self):
        while not self.pause_event.is_set():
            if self.stop_flag:
                raise InterruptedError("Aborted by user.")
            time.sleep(0.1)

    def blink_status_dot(self):
        if not self.stop_flag and self.test_thread and self.test_thread.is_alive():
            self.status_blink_on = not self.status_blink_on
            color = self.success_glow if self.status_blink_on else "#005500"
            self.status_dot.config(fg=color)
        elif self.get_game_rect() is not None:
            self.status_blink_on = not self.status_blink_on
            color = self.alert_yellow if self.status_blink_on else "#553b00"
            self.status_dot.config(fg=color)
        else:
            self.status_dot.config(fg=self.alert_yellow)
        self.root.after(500, self.blink_status_dot)

    def start_test_flow(self):
        if not self.config.game_exe_path or not os.path.exists(self.config.game_exe_path):
            messagebox.showerror("Error", "Please select a valid game executable first!")
            return
            
        if hasattr(self, 'switch_mid_tab'):
            self.switch_mid_tab("live")
            
        selected_indices = self.scenario_listbox.curselection()
        if not selected_indices:
            messagebox.showerror("Error", "Please select a scenario from the sidebar first!")
            return
            
        if self.test_thread and self.test_thread.is_alive():
            return
            
        self.stop_flag = False
        self.pause_event.set()
        self.set_control_states("disabled", "normal", "normal", "PAUSE")
        self.btn_clear_save.config(state="disabled")
        self.btn_launch.config(state="disabled")
        
        self.tree.delete(*self.tree.get_children())
        
        selected_scenario = self.scenario_listbox.get(selected_indices[0])
        script_path = os.path.join(self.scenarios_dir, f"{selected_scenario}.lua")
        
        self.test_thread = TestSequenceRunner(self, selected_scenario, script_path)
        self.test_thread.start()

    def release_all_inputs(self):
        """Force-release all modifier keys and mouse buttons via Win32 SendInput.
        Called on every abort, teardown, and close so pyautogui can never leave
        keys physically held down in the OS."""
        import ctypes

        # --- Release mouse buttons (left, right, middle) via Win32 mouse_event ---
        MOUSEEVENTF_LEFTUP   = 0x0004
        MOUSEEVENTF_RIGHTUP  = 0x0010
        MOUSEEVENTF_MIDDLEUP = 0x0040
        ctypes.windll.user32.mouse_event(MOUSEEVENTF_LEFTUP,   0, 0, 0, 0)
        ctypes.windll.user32.mouse_event(MOUSEEVENTF_RIGHTUP,  0, 0, 0, 0)
        ctypes.windll.user32.mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0)

        # --- Release modifier keys via Win32 keybd_event ---
        KEYEVENTF_KEYUP = 0x0002
        modifier_vks = [
            0x10,  # VK_SHIFT       (generic Shift)
            0xA0,  # VK_LSHIFT      (Left Shift)
            0xA1,  # VK_RSHIFT      (Right Shift)
            0x11,  # VK_CONTROL     (generic Ctrl)
            0xA2,  # VK_LCONTROL    (Left Ctrl)
            0xA3,  # VK_RCONTROL    (Right Ctrl)
            0x12,  # VK_MENU        (generic Alt)
            0xA4,  # VK_LMENU       (Left Alt)
            0xA5,  # VK_RMENU       (Right Alt)
            0x5B,  # VK_LWIN        (Left Windows)
            0x5C,  # VK_RWIN        (Right Windows)
        ]
        for vk in modifier_vks:
            ctypes.windll.user32.keybd_event(vk, 0, KEYEVENTF_KEYUP, 0)

        # --- Also reset pyautogui's internal key-state tracking ---
        try:
            import pyautogui
            for key in ("shift", "ctrl", "alt", "win",
                        "shiftleft", "shiftright",
                        "ctrlleft", "ctrlright",
                        "altleft", "altright"):
                try:
                    pyautogui.keyUp(key)
                except Exception:
                    pass
        except Exception:
            pass

        self.log_message("SYSTEM", "INFO", "All modifier keys and mouse buttons force-released.")

    def stop_test_flow(self):
        self.stop_flag = True
        self.pause_event.set()
        self.release_all_inputs()   # <-- release before anything else
        
        self.log_message("SYSTEM", "INFO", "Sequence ABORT requested. Releasing locks...")
        if self.log_monitor:
            self.log_monitor.running = False
        if hasattr(self, 'capture_thread'):
            self.capture_thread.stop_recording()
        self.kill_game_process()
        
        self.set_control_states("normal", "disabled", "disabled", "PAUSE")
        self.btn_clear_save.config(state="normal")
        self.btn_launch.config(state="normal")
        self.set_stage_lbl("Aborted")

    def _reset_controls_post_run(self):
        self.release_all_inputs()   # <-- safety release after every run
        self.set_control_states("normal", "disabled", "disabled", "PAUSE")
        self.btn_clear_save.config(state="normal")
        self.btn_launch.config(state="normal")

    def set_control_states(self, run_state, pause_state, stop_state, pause_text="PAUSE"):
        if hasattr(self, 'btn_run') and self.btn_run:
            try: self.btn_run.config(state=run_state)
            except Exception: pass
        if hasattr(self, 'btn_pause') and self.btn_pause:
            try: self.btn_pause.config(state=pause_state, text=pause_text)
            except Exception: pass
        if hasattr(self, 'btn_stop') and self.btn_stop:
            try: self.btn_stop.config(state=stop_state)
            except Exception: pass
            
        debug_state = "normal" if stop_state == "normal" else "disabled"
        if hasattr(self, 'btn_prev') and self.btn_prev:
            try: self.btn_prev.config(state=debug_state)
            except Exception: pass
        if hasattr(self, 'btn_repeat') and self.btn_repeat:
            try: self.btn_repeat.config(state=debug_state)
            except Exception: pass
        if hasattr(self, 'btn_next') and self.btn_next:
            try: self.btn_next.config(state=debug_state)
            except Exception: pass
            
        if hasattr(self, 'btn_overlay_run') and self.btn_overlay_run:
            try: self.btn_overlay_run.config(state=run_state)
            except Exception: pass
        if hasattr(self, 'btn_overlay_pause') and self.btn_overlay_pause:
            try: self.btn_overlay_pause.config(state=pause_state, text=pause_text)
            except Exception: pass
        if hasattr(self, 'btn_overlay_stop') and self.btn_overlay_stop:
            try: self.btn_overlay_stop.config(state=stop_state)
            except Exception: pass
            
        if hasattr(self, 'btn_overlay_prev') and self.btn_overlay_prev:
            try: self.btn_overlay_prev.config(state=debug_state)
            except Exception: pass
        if hasattr(self, 'btn_overlay_repeat') and self.btn_overlay_repeat:
            try: self.btn_overlay_repeat.config(state=debug_state)
            except Exception: pass
        if hasattr(self, 'btn_overlay_next') and self.btn_overlay_next:
            try: self.btn_overlay_next.config(state=debug_state)
            except Exception: pass

    def toggle_navigation_sidebar(self):
        if not hasattr(self, 'nav_collapsed'):
            self.nav_collapsed = False
        self.nav_collapsed = not self.nav_collapsed
        self.repack_main_layout()

    def toggle_left_panel(self):
        if self.hud_mode == "OVERLAY":
            return
        if self.active_tab != "games" or getattr(self, 'current_page', 'library') != 'details':
            active_game = self.config.get_active_game()
            self.show_details_page(active_game.get("id"))
            self.left_collapsed = True
            
        if self.left_collapsed:
            self.left_collapsed = False
            self.global_sidebar.config(width=260)
            self.repack_main_layout()
        else:
            self.left_collapsed = True
            self.global_sidebar.pack_forget()
            self.global_sidebar_div.pack_forget()
            self.repack_left_buttons()

    def repack_main_layout(self):
        self.navigation_sidebar.pack_forget()
        self.navigation_sidebar_div.pack_forget()
        self.global_sidebar.pack_forget()
        self.global_sidebar_div.pack_forget()
        self.container.pack_forget()
        
        if not getattr(self, 'nav_collapsed', False):
            self.navigation_sidebar.pack(side="left", fill="y")
            self.navigation_sidebar_div.pack(side="left", fill="y")
            
        if self.active_tab == "games" and getattr(self, 'current_page', 'library') == 'details':
            if not getattr(self, 'left_collapsed', False):
                self.global_sidebar.pack(side="left", fill="y")
                self.global_sidebar_div.pack(side="left", fill="y")
            
        self.container.pack(side="left", fill="both", expand=True)

    def show_in_taskbar(self):
        import platform
        if platform.system() == "Windows":
            try:
                import ctypes
                GWL_STYLE = -16
                GWL_EXSTYLE = -20
                WS_POPUP = 0x80000000
                WS_SYSMENU = 0x00080000
                WS_CAPTION = 0x00C00000
                WS_THICKFRAME = 0x00040000
                WS_EX_APPWINDOW = 0x00040000
                WS_EX_TOOLWINDOW = 0x00000080
                
                hwnd = self.root.winfo_id()
                hwnds = [hwnd]
                
                parent_hwnd = ctypes.windll.user32.GetParent(hwnd)
                if parent_hwnd:
                    hwnds.append(parent_hwnd)
                    
                for h in hwnds:
                    # Apply native borderless/frameless style to the window
                    style = ctypes.windll.user32.GetWindowLongW(h, GWL_STYLE)
                    style = style & ~WS_CAPTION
                    # Keep WS_THICKFRAME to allow resizing window by edges
                    style = style | WS_POPUP | WS_SYSMENU | WS_THICKFRAME
                    ctypes.windll.user32.SetWindowLongW(h, GWL_STYLE, style)
                    
                    # Apply styles to make it registered in the Taskbar & Alt-Tab
                    ex_style = ctypes.windll.user32.GetWindowLongW(h, GWL_EXSTYLE)
                    ex_style = ex_style & ~WS_EX_TOOLWINDOW
                    ex_style = ex_style | WS_EX_APPWINDOW
                    ctypes.windll.user32.SetWindowLongW(h, GWL_EXSTYLE, ex_style)
                    
                    # Enable Immersive Dark Mode and set custom border color to blend top line
                    try:
                        # DWMWA_USE_IMMERSIVE_DARK_MODE = 20 (Windows 10 20H1+) or 19 (Windows 10 pre-20H1)
                        DWMWA_USE_IMMERSIVE_DARK_MODE = 20
                        ctypes.windll.dwmapi.DwmSetWindowAttribute(
                            h,
                            DWMWA_USE_IMMERSIVE_DARK_MODE,
                            ctypes.byref(ctypes.c_int(1)),
                            ctypes.sizeof(ctypes.c_int)
                        )
                    except Exception:
                        try:
                            DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19
                            ctypes.windll.dwmapi.DwmSetWindowAttribute(
                                h,
                                DWMWA_USE_IMMERSIVE_DARK_MODE_OLD,
                                ctypes.byref(ctypes.c_int(1)),
                                ctypes.sizeof(ctypes.c_int)
                            )
                        except Exception:
                            pass

                    try:
                        # DWMWA_BORDER_COLOR = 34 (Windows 11 build 22000+)
                        DWMWA_BORDER_COLOR = 34
                        # Color #18181c (BGR representation: 0x001c1818)
                        border_color = 0x001c1818
                        ctypes.windll.dwmapi.DwmSetWindowAttribute(
                            h,
                            DWMWA_BORDER_COLOR,
                            ctypes.byref(ctypes.c_int(border_color)),
                            ctypes.sizeof(ctypes.c_int)
                        )
                    except Exception:
                        pass

                    # Force Windows shell to refresh the window properties and icon
                    SWP_NOMOVE = 0x0002
                    SWP_NOSIZE = 0x0001
                    SWP_NOZORDER = 0x0004
                    SWP_NOACTIVATE = 0x0010
                    SWP_FRAMECHANGED = 0x0020
                    ctypes.windll.user32.SetWindowPos(
                        h, 0, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED
                    )
                    
                    ctypes.windll.user32.ShowWindow(h, 5)
            except Exception:
                pass

    def show_shortcuts_help(self):
        from tkinter import messagebox
        help_msg = (
            "Sylvan-HUD Keyboard Shortcuts:\n\n"
            "• [Ctrl + P] : Pause / Resume scenario execution\n"
            "• [Ctrl + Q] : Abort active test runner sequence\n"
            "• [Ctrl + O] : Toggle between full ANALYZE mode and float OVERLAY HUD\n"
            "• [Ctrl + Left Arrow] : ⏮ PREV step (Rewind one step)\n"
            "• [Ctrl + Down Arrow] : 🔁 REPEAT step (Restart active step)\n"
            "• [Ctrl + Right Arrow] : ⏭ NEXT step (Skip current step)\n\n"
            "Note: Global hotkeys are active even when the window is focused in the background."
        )
        messagebox.showinfo("Salavan Shortcuts Help", help_msg)

    def animate_left_sidebar(self, target_width):
        # Instant show/hide — no animation to avoid redraw artifacts
        if target_width == 0:
            self.repack_left_buttons()
            self.global_sidebar.pack_forget()
            self.global_sidebar_div.pack_forget()
        else:
            self.global_sidebar.config(width=target_width)
            self.repack_left_buttons()

    def toggle_builds_panel(self):
        if self.hud_mode == "OVERLAY":
            return
        if not hasattr(self, 'btn_right_handle') or not self.btn_right_handle:
            return
        if self.active_tab != "games" or not hasattr(self, 'details_page') or not self.details_page.winfo_viewable():
            active_game = self.config.get_active_game()
            self.show_details_page(active_game.get("id"))
            self.builds_collapsed = True
            
        if self.builds_collapsed:
            self.builds_collapsed = False
            self.btn_right_handle.config(text="▶")
            pane = self.details_page.right_sidebar_pane
            div = self.details_page.right_sidebar_div
            handle = self.btn_right_handle
            pane.config(width=260)
            if not pane.winfo_viewable():
                pane.pack(side="right", fill="y", before=handle)
                div.pack(side="right", fill="y", before=handle)
            self.repack_right_buttons()
        else:
            self.builds_collapsed = True
            self.btn_right_handle.config(text="◀")
            self.details_page.right_sidebar_pane.pack_forget()
            self.details_page.right_sidebar_div.pack_forget()
            self.repack_right_buttons()

    def animate_builds_sidebar(self, target_width):
        # Instant show/hide — no animation to avoid redraw artifacts
        if not hasattr(self, 'details_page') or not self.details_page.winfo_exists():
            return
        pane = self.details_page.right_sidebar_pane
        div = self.details_page.right_sidebar_div
        handle = self.btn_right_handle
        if target_width == 0:
            pane.pack_forget()
            div.pack_forget()
        else:
            pane.config(width=target_width)
            if not pane.winfo_viewable():
                pane.pack(side="right", fill="y", before=handle)
                div.pack(side="right", fill="y", before=handle)
        self.repack_right_buttons()

    def toggle_right_panel(self):
        if self.hud_mode == "OVERLAY":
            return
        if hasattr(self, 'logs_popped_out') and self.logs_popped_out:
            self.toggle_logs_docking()
            return
        if self.right_collapsed:
            self.animate_right_logs(220)
            self.right_collapsed = False
        else:
            self.animate_right_logs(35)
            self.right_collapsed = True

    def animate_right_logs(self, target_height):
        # Instant resize — no animation to avoid redraw artifacts
        if not hasattr(self, 'details_page') or not self.details_page.winfo_exists():
            return
        pw = self.details_page.paned_window
        pane = self.details_page.right_pane
        is_managed = pane in pw.panes() or str(pane) in [str(p) for p in pw.panes()]
        if target_height == 0:
            if is_managed:
                pw.forget(pane)
        else:
            if not is_managed:
                pw.add(pane, minsize=0, height=target_height, stretch="never")
            pw.paneconfigure(pane, minsize=target_height, height=target_height)
        if hasattr(self, 'btn_toggle_right') and self.btn_toggle_right:
            t = "[ ⬆ EXPAND LOGS ]" if self.right_collapsed else "[ ⬇ COLLAPSE ]"
            self.btn_toggle_right.config(text=t)
    def repack_left_buttons(self):
        if not hasattr(self, 'details_page') or not self.details_page.winfo_exists():
            return
        self.details_page.btn_back_lib.pack_forget()
        self.details_page.btn_back_lib.pack(side="left", padx=5)

    def repack_right_buttons(self):
        if not hasattr(self, 'details_page') or not self.details_page.winfo_exists():
            return
        if hasattr(self, 'btn_dock_main') and self.btn_dock_main:
            self.btn_dock_main.pack_forget()
            
        if hasattr(self, 'logs_popped_out') and self.logs_popped_out:
            if hasattr(self, 'btn_dock_main') and self.btn_dock_main:
                self.btn_dock_main.pack(side="right", padx=5)



    def get_system_specs(self):
        import platform
        import subprocess
        specs = {
            "os": f"{platform.system()} {platform.release()}",
            "cpu": "Unknown Processor",
            "ram": "Unknown Memory",
            "gpu": "Unknown GPU"
        }
        try:
            if platform.system() == "Windows":
                import winreg
                key = winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, r"HARDWARE\DESCRIPTION\System\CentralProcessor\0")
                cpu_name, _ = winreg.QueryValueEx(key, "ProcessorNameString")
                if cpu_name:
                    specs["cpu"] = cpu_name.strip()
            else:
                specs["cpu"] = platform.processor()
        except Exception:
            specs["cpu"] = platform.processor()
        try:
            if platform.system() == "Windows":
                out = subprocess.check_output("wmic computersystem get totalphysicalmemory", shell=True).decode()
                lines = [line.strip() for line in out.splitlines() if line.strip()]
                if len(lines) > 1 and lines[1].isdigit():
                    ram_bytes = int(lines[1])
                    specs["ram"] = f"{ram_bytes / (1024**3):.1f} GB"
        except Exception:
            pass
        try:
            if platform.system() == "Windows":
                out = subprocess.check_output("wmic path win32_VideoController get name", shell=True).decode()
                lines = [line.strip() for line in out.splitlines() if line.strip()]
                if len(lines) > 1:
                    specs["gpu"] = lines[1]
        except Exception:
            pass
        return specs

    def toggle_hud_mode(self):
        if self.hud_mode == "ANALYZE":
            self.hud_mode = "OVERLAY"
            self.last_geometry = self.root.geometry()
            
            # Hide top custom title bar, navigation bar, and main layout completely
            self.title_bar.pack_forget()
            self.main_layout.pack_forget()
            
            # Set float overlay geometry (380x600 size, positioned at top-left +0+0)
            self.root.geometry("380x600+0+0")
            
            # Keep topmost focus but keep normal opacity (or slight transparent look like 0.95)
            self.root.attributes("-alpha", 0.95)
            self.root.attributes("-topmost", True)
            self.root.config(bg=self.bg_dark)
            
            # Spawn the floating, draggable control HUD panel
            self.create_overlay_hud()
            
            self.log_message("SYSTEM", "INFO", "Switched to Float Overlay Mode.")
            self.refresh_overlay_steps()
        else:
            self.hud_mode = "ANALYZE"
            
            if hasattr(self, 'overlay_hud') and self.overlay_hud:
                self.overlay_hud.pack_forget()
                self.overlay_hud.destroy()
                self.overlay_hud = None
                self.overlay_steps_tree = None
                
            # Restore normal window properties
            self.root.attributes("-alpha", 1.0)
            self.root.attributes("-topmost", False)
            self.root.config(bg=self.bg_dark)
            
            # Re-pack top custom title bar and main layout
            self.title_bar.pack(fill="x", side="top")
            self.main_layout.pack(fill="both", expand=True)
            
            self.repack_main_layout()
            
            self.root.geometry(self.last_geometry)
            self.log_message("SYSTEM", "INFO", "Restored to full ANALYZE Mode.")

    def create_overlay_hud(self):
        self.overlay_hud = tk.Frame(self.root, bg=self.bg_panel, bd=2, highlightbackground=self.accent_glow, highlightthickness=1)
        self.overlay_hud.pack(fill="both", expand=True)
        
        # Header (Drag handle)
        hdr = tk.Frame(self.overlay_hud, bg="#111114", height=32)
        hdr.pack(fill="x", side="top")
        hdr.pack_propagate(False)
        
        hdr_lbl = tk.Label(hdr, text=" 🖥️  SYLVAN CONTROL HUD", fg=self.accent_glow, bg="#111114", font=("Segoe UI", 9, "bold"))
        hdr_lbl.pack(side="left", padx=10)
        
        # Minimize button in overlay header
        min_btn = tk.Button(
            hdr, text="—", command=self.minimize_window,
            bg="#111114", fg=self.fg_light, activebackground=self.accent_dim,
            activeforeground=self.fg_light, bd=0, width=3, font=("Segoe UI", 9, "bold")
        )
        min_btn.pack(side="right", fill="y")
        self.add_tooltip(min_btn, "Minimize HUD Panel")
        
        # Bind Drag events to move the actual window natively
        hdr.bind("<ButtonPress-1>", self.start_drag)
        hdr.bind("<B1-Motion>", self.drag_window)
        hdr_lbl.bind("<ButtonPress-1>", self.start_drag)
        hdr_lbl.bind("<B1-Motion>", self.drag_window)
        
        # Body frame
        body = tk.Frame(self.overlay_hud, bg=self.bg_panel, padx=15, pady=10)
        body.pack(fill="both", expand=True)
        
        # Active profile info
        active_game = self.config.get_active_game()
        game_title = active_game.get("title", "Maou-Sama-TD")
        
        lbl_game = tk.Label(body, text=f"GAME: {game_title.upper()}", fg=self.fg_light, bg=self.bg_panel, font=("Segoe UI", 9, "bold"))
        lbl_game.pack(anchor="w", pady=(0, 2))
        
        # Prominent Active Step banner
        self.overlay_active_step_lbl = tk.Label(body, text="ACTIVE STEP: IDLE", fg=self.alert_yellow, bg=self.bg_panel, font=("Segoe UI", 10, "bold"), anchor="w")
        self.overlay_active_step_lbl.pack(fill="x", pady=(2, 5))
        
        # Steps checklist frame & Treeview
        steps_frame = tk.Frame(body, bg=self.bg_panel)
        steps_frame.pack(fill="both", expand=True, pady=5)
        
        steps_scroll = ttk.Scrollbar(steps_frame)
        steps_scroll.pack(side="right", fill="y")
        
        self.overlay_steps_tree = ttk.Treeview(
            steps_frame, columns=("Status",), show="tree", 
            yscrollcommand=steps_scroll.set, height=6
        )
        self.overlay_steps_tree.column("#0", width=250)
        self.overlay_steps_tree.column("Status", width=60, anchor="center")
        self.overlay_steps_tree.pack(side="left", fill="both", expand=True)
        steps_scroll.config(command=self.overlay_steps_tree.yview)
        
        # Control Buttons Grid
        btn_frame = tk.Frame(body, bg=self.bg_panel)
        btn_frame.pack(fill="x", pady=5)
        
        # Verify active states of run process
        run_state = "disabled" if self.test_thread and self.test_thread.is_alive() else "normal"
        pause_state = "normal" if self.test_thread and self.test_thread.is_alive() else "disabled"
        stop_state = "normal" if self.test_thread and self.test_thread.is_alive() else "disabled"
        pause_txt = "RESUME" if not self.pause_event.is_set() else "PAUSE"
        
        self.btn_overlay_run = ttk.Button(btn_frame, text="RUN TEST", command=self.start_test_flow, state=run_state)
        self.btn_overlay_run.grid(row=0, column=0, padx=2, pady=2, sticky="ew")
        
        self.btn_overlay_pause = ttk.Button(btn_frame, text=pause_txt, command=self.toggle_pause, state=pause_state)
        self.btn_overlay_pause.grid(row=0, column=1, padx=2, pady=2, sticky="ew")
        
        self.btn_overlay_stop = ttk.Button(btn_frame, text="ABORT", command=self.stop_test_flow, state=stop_state)
        self.btn_overlay_stop.grid(row=0, column=2, padx=2, pady=2, sticky="ew")
        
        self.btn_overlay_prev = ttk.Button(btn_frame, text="⏮ PREV", command=self.prev_step, state=pause_state)
        self.btn_overlay_prev.grid(row=1, column=0, padx=2, pady=2, sticky="ew")
        
        self.btn_overlay_repeat = ttk.Button(btn_frame, text="🔁 REPEAT", command=self.repeat_step, state=pause_state)
        self.btn_overlay_repeat.grid(row=1, column=1, padx=2, pady=2, sticky="ew")
        
        self.btn_overlay_next = ttk.Button(btn_frame, text="⏭ NEXT", command=self.next_step, state=pause_state)
        self.btn_overlay_next.grid(row=1, column=2, padx=2, pady=2, sticky="ew")
        
        btn_frame.columnconfigure(0, weight=1)
        btn_frame.columnconfigure(1, weight=1)
        btn_frame.columnconfigure(2, weight=1)
        
        # Console Live Log Text
        console_lbl = tk.Label(body, text="// LIVE LOG STREAM", fg=self.accent_glow, bg=self.bg_panel, font=("Segoe UI", 8, "bold"))
        console_lbl.pack(anchor="w", pady=(5, 2))
        
        console_border = tk.Frame(body, bg=self.accent_dim, bd=1)
        console_border.pack(fill="both", expand=False)
        
        self.overlay_console_text = tk.Text(console_border, bg="#050508", bd=0, height=4, wrap="word", font=("Consolas", 9, "bold"))
        self.overlay_console_text.pack(fill="both", expand=True)
        self.overlay_console_text.tag_config("pass", foreground=self.success_glow)
        self.overlay_console_text.tag_config("fail", foreground=self.fail_glow)
        self.overlay_console_text.tag_config("info", foreground="#00b4d8")
        self.overlay_console_text.config(state="disabled")
        
        # Populate overlay console with existing text logs
        try:
            current_logs = self.console_text.get("1.0", tk.END)
            self.overlay_console_text.config(state="normal")
            self.overlay_console_text.insert("1.0", current_logs)
            self.overlay_console_text.see(tk.END)
            self.overlay_console_text.config(state="disabled")
        except Exception:
            pass
            
        # Exit Overlay mode button
        btn_exit = tk.Button(body, text="[ RETURN TO ANALYZE MODE ]", command=self.toggle_hud_mode, bg="#2c2c35", fg=self.fg_light, activebackground=self.accent_glow, activeforeground="#101012", bd=0, pady=5, font=("Segoe UI", 9, "bold"))
        btn_exit.pack(fill="x", pady=(10, 0))

    def refresh_overlay_steps(self):
        if not hasattr(self, 'overlay_steps_tree') or not self.overlay_steps_tree:
            return
        
        self.overlay_steps_tree.delete(*self.overlay_steps_tree.get_children())
        
        selected_indices = self.scenario_listbox.curselection()
        if not selected_indices:
            return
        selected_scenario = self.scenario_listbox.get(selected_indices[0])
        script_path = os.path.join(self.scenarios_dir, f"{selected_scenario}.lua")
        
        if not hasattr(self, '_scenario_steps_cache'):
            self._scenario_steps_cache = {}
        parsed_steps = []
        steps_names = self._scenario_steps_cache.get(script_path)
        if steps_names:
            for s in steps_names:
                parsed_steps.append({"name": s})
        else:
            if os.path.exists(script_path):
                try:
                    with open(script_path, "r", encoding="utf-8") as f_in:
                        content = f_in.read()
                    matches = re.findall(r'set_stage\([\'"]([^\'"]+)[\'"]\)', content)
                    for m in matches:
                        parsed_steps.append({"name": m})
                except Exception:
                    pass
        
        active_stage = self.stage_lbl["text"] if hasattr(self, 'stage_lbl') else "Idle"
        active_stage_lower = active_stage.lower()
        if " [scene:" in active_stage_lower:
            active_stage_lower = active_stage_lower.split(" [scene:")[0]
        if "status:" in active_stage_lower:
            active_stage_lower = active_stage_lower.split("status:")[1].strip()
        if "system status:" in active_stage_lower:
            active_stage_lower = active_stage_lower.split("system status:")[1].strip()
            
        active_idx = -1
        for s_idx, step in enumerate(parsed_steps):
            step_name = step["name"]
            s_clean = re.sub(r'^\d+\.\s*', '', step_name).lower().strip()
            if s_clean in active_stage_lower or active_stage_lower in s_clean:
                active_idx = s_idx
                break
                
        if "completed" in active_stage_lower:
            active_idx = len(parsed_steps)
            
        if 0 <= active_idx < len(parsed_steps):
            self.overlay_active_step_lbl.config(text=f"ACTIVE STEP: {parsed_steps[active_idx]['name']}", fg=self.alert_yellow)
        elif active_idx >= len(parsed_steps):
            self.overlay_active_step_lbl.config(text="ACTIVE STEP: COMPLETED", fg=self.success_glow)
        else:
            self.overlay_active_step_lbl.config(text="ACTIVE STEP: IDLE / WAIT", fg=self.fg_light)
            
        for s_idx, step in enumerate(parsed_steps):
            step_name = step["name"]
            is_skipped = self.skipped_steps.get((selected_scenario, step_name), False)
            is_done = (s_idx <= active_idx)
            
            if is_skipped:
                status_text = "SKIP"
            else:
                status_text = "✓" if is_done else "·"
                
            display_name = step_name
            if is_skipped:
                display_name += " (SKIP)"
            elif s_idx == active_idx:
                display_name += " ◀"
                
            self.overlay_steps_tree.insert("", "end", text=display_name, values=(status_text,))

    # ── APIs for Lua Engine ──
    def sleep_wait(self, seconds):
        if getattr(self, 'current_step_is_skipped', False) or getattr(self, 'skip_current_step', False):
            return
        start_time = time.time()
        while time.time() - start_time < seconds:
            self.check_paused()
            if self.stop_flag:
                raise InterruptedError()
            if getattr(self, 'skip_current_step', False):
                break
            time.sleep(0.1)

    def clear_save_data(self):
        self.check_paused()
        active_game = self.config.get_active_game()
        save_paths_raw = active_game.get("save_paths", [])
        cleared_any = False
        for p_raw in save_paths_raw:
            p = os.path.normpath(os.path.expandvars(p_raw))
            if os.path.exists(p):
                try:
                    os.remove(p)
                    self.log_message("PURGE SAVE", "INFO", f"Erased cache: {p}")
                    cleared_any = True
                except Exception as e:
                    self.log_message("PURGE SAVE", "INFO", f"Access Denied {p}: {str(e)}")
        return True

    def manual_clear_save(self):
        if self.clear_save_data():
            messagebox.showinfo("Purge Save", "Local game save data purged successfully!")
        else:
            messagebox.showerror("Purge Save", "Failed to clear save data or files not present.")

    def kill_game_process(self):
        active_game = self.config.get_active_game()
        process_name = active_game.get("process_name", "Maou-Sama-TD.exe")
        if self.game_process:
            try:
                self.game_process.terminate()
                self.game_process.wait(timeout=2)
                self.game_process = None
            except Exception:
                pass
        subprocess.run(f'taskkill /f /im "{process_name}"', shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    def launch_game(self, force_restart=False):
        self.check_paused()
        if force_restart is None:
            force_restart = False
            
        active_game = self.config.get_active_game()
        window_title = active_game.get("window_title", "Maou-Sama-TD")
            
        if not force_restart:
            win = self.find_game_windows()
            if win:
                self.log_message("BOOT GAME", "INFO", f"Existing {window_title} window detected. Hooking...")
                w = win[0]
                try:
                    w.restore()
                    w.activate()
                except Exception:
                    pass
                width = self.config.game_width
                height = self.config.game_height
                if width > 0 and height > 0:
                    w.moveTo(0, 0)
                    w.resizeTo(width, height)
                    try:
                        screen_width = self.root.winfo_screenwidth()
                        target_x = width + 5
                        if target_x + 1160 > screen_width:
                            target_x = max(0, screen_width - 1160)
                        self.root.geometry(f"1160x820+{target_x}+0")
                        self.normal_geometry = f"1160x820+{target_x}+0"
                    except Exception:
                        pass
                else:
                    try: w.maximize()
                    except Exception: pass
                return True
                
        self.kill_game_process()
        if not self.config.game_exe_path or not os.path.exists(self.config.game_exe_path):
            self.log_message("BOOT GAME", "FAIL", "Game executable not selected or missing.")
            return False
            
        try:
            width = self.config.game_width
            height = self.config.game_height
            if width > 0 and height > 0:
                cmd = f'"{self.config.game_exe_path}" -screen-width {width} -screen-height {height} -screen-fullscreen 0'
            else:
                cmd = f'"{self.config.game_exe_path}" -screen-fullscreen 1'
                
            self.game_process = subprocess.Popen(cmd, shell=True)
            self.log_message("BOOT GAME", "INFO", "Process spawned. Aligning interface...")
            
            positioned = False
            for _ in range(30):
                self.check_paused()
                if self.stop_flag:
                    raise InterruptedError()
                win = self.find_game_windows()
                if win:
                    w = win[0]
                    w.restore()
                    w.activate()
                    if width > 0 and height > 0:
                        w.moveTo(0, 0)
                        w.resizeTo(width, height)
                        try:
                            screen_width = self.root.winfo_screenwidth()
                            target_x = width + 5
                            if target_x + 1160 > screen_width:
                                target_x = max(0, screen_width - 1160)
                            self.root.geometry(f"1160x820+{target_x}+0")
                            self.normal_geometry = f"1160x820+{target_x}+0"
                        except Exception:
                            pass
                    else:
                        try: w.maximize()
                        except Exception: pass
                    positioned = True
                    break
                time.sleep(0.5)
                
            if positioned:
                self.log_message("BOOT GAME", "INFO", "Display window locked at (0, 0).")
                return True
            else:
                self.log_message("BOOT GAME", "FAIL", "Display window could not be locked.")
                return False
        except Exception as e:
            self.log_message("BOOT GAME", "FAIL", f"Display error: {str(e)}")
            return False

    def manual_launch_game(self):
        if not self.config.game_exe_path or not os.path.exists(self.config.game_exe_path):
            messagebox.showerror("Error", "Please select a valid game executable first!")
            return
        threading.Thread(target=self.launch_game, args=(False,), daemon=True).start()

    def get_game_rect(self):
        win = self.find_game_windows()
        if not win:
            return None
        w = win[0]
        if w.isMinimized:
            # Only restore if a test is active
            is_testing = hasattr(self, 'test_thread') and self.test_thread and self.test_thread.is_alive()
            if is_testing:
                try:
                    w.restore()
                except Exception:
                    pass
            else:
                return None
                
        # Natively get Client Area coordinates using Win32 API via ctypes to exclude window borders/title bar
        try:
            import ctypes
            from ctypes import wintypes
            
            hwnd = w._hWnd
            rect = wintypes.RECT()
            ctypes.windll.user32.GetClientRect(hwnd, ctypes.byref(rect))
            width = rect.right - rect.left
            height = rect.bottom - rect.top
            
            point = wintypes.POINT(0, 0)
            ctypes.windll.user32.ClientToScreen(hwnd, ctypes.byref(point))
            client_x = point.x
            client_y = point.y
            
            if width > 0 and height > 0:
                return (client_x, client_y, width, height)
        except Exception:
            pass
            
        # Fallback to pygetwindow borders
        return (w.left, w.top, w.width, w.height)

    def click_game_relative(self, rx, ry):
        if getattr(self, 'current_step_is_skipped', False) or getattr(self, 'skip_current_step', False):
            return True
        self.check_paused()
        rect = self.get_game_rect()
        if not rect:
            self.log_message("CLICK", "FAIL", "Window bounds out of range.")
            return False
        cx, cy, gw_w, gw_h = rect
        screen_x = cx + int(rx * gw_w / 1280)
        screen_y = cy + int(ry * gw_h / 720)
        
        win = self.find_game_windows()
        if win:
            try: win[0].activate()
            except Exception: pass
        pyautogui.click(screen_x, screen_y)
        return True

    def drag_game_relative(self, rx1, ry1, rx2, ry2, duration=1.0):
        if getattr(self, 'current_step_is_skipped', False) or getattr(self, 'skip_current_step', False):
            return True
        self.check_paused()
        rect = self.get_game_rect()
        if not rect:
            self.log_message("DRAG", "FAIL", "Window bounds out of range.")
            return False
        cx, cy, gw_w, gw_h = rect
        sx1 = cx + int(rx1 * gw_w / 1280)
        sy1 = cy + int(ry1 * gw_h / 720)
        sx2 = cx + int(rx2 * gw_w / 1280)
        sy2 = cy + int(ry2 * gw_h / 720)
        
        win = self.find_game_windows()
        if win:
            try: win[0].activate()
            except Exception: pass
        try:
            pyautogui.moveTo(sx1, sy1, duration=0.2)
            pyautogui.dragTo(sx2, sy2, duration=duration, button="left")
        finally:
            # Always release the left mouse button — even if aborted or exception raised
            import ctypes
            ctypes.windll.user32.mouse_event(0x0004, 0, 0, 0, 0)  # MOUSEEVENTF_LEFTUP
        return True


    def update_preview_image(self, pil_image):
        self.latest_screenshot = pil_image
        self.root.after(0, self._set_preview_image, pil_image)
        
    def _set_preview_image(self, pil_image):
        try:
            photo = ImageTk.PhotoImage(pil_image)
            self.preview_lbl.config(image=photo)
            self.preview_lbl.image = photo
        except Exception:
            pass

    def reset_preview_placeholder(self):
        self.root.after(0, self._reset_preview_placeholder_ui)

    def _reset_preview_placeholder_ui(self):
        try:
            img = Image.new("RGB", (320, 180), (8, 8, 12))
            draw = ImageDraw.Draw(img)
            for i in range(0, 320, 20):
                draw.line([i, 0, i, 180], fill=(20, 20, 32), width=1)
            for j in range(0, 180, 20):
                draw.line([0, j, 320, j], fill=(20, 20, 32), width=1)
            draw.ellipse([110, 40, 210, 140], outline=(40, 40, 60), width=1)
            draw.line([160, 0, 160, 180], fill=(40, 40, 60), width=1)
            draw.line([0, 90, 320, 90], fill=(40, 40, 60), width=1)
            draw.text((115, 82), "HUD FEED: STANDBY", fill=(157, 78, 221))
            photo = ImageTk.PhotoImage(img)
            self.preview_lbl.config(image=photo)
            self.preview_lbl.image = photo
            self.latest_screenshot = img
        except Exception:
            pass

    def get_template_path(self, name):
        if not name.endswith(".png"):
            name = name + ".png"
        return os.path.join(self.template_dir, name)

    def read_game_state(self):
        try:
            path = os.path.join(self.base_dir, "game_state.json")
            if os.path.exists(path):
                import json
                with open(path, "r", encoding="utf-8") as f:
                    return json.load(f)
        except Exception:
            pass
        return None

    def find_button_in_state(self, btn_name, buttons_dict):
        clean_name = btn_name.lower().replace("_", "").replace("-", "").replace(" ", "").replace(".png", "").strip()
        # First pass: try to match key (GameObject name)
        for k, v in buttons_dict.items():
            clean_k = k.lower().replace("_", "").replace("-", "").replace(" ", "").strip()
            if clean_k == clean_name or clean_name in clean_k or clean_k in clean_name:
                return v
        # Second pass: try to match button label text
        for k, v in buttons_dict.items():
            text_val = v.get("text", "")
            if text_val:
                clean_text = text_val.lower().replace("_", "").replace("-", "").replace(" ", "").strip()
                if clean_text == clean_name or clean_name in clean_text or clean_text in clean_name:
                    return v
        return None

    def check_and_auto_progress_dialogue(self):
        if not self.config.auto_sync_ui:
            return False
        state = self.read_game_state()
        if state and state.get("is_dialogue_active"):
            buttons = state.get("buttons", {})
            
            # Check for skip buttons
            skip_btn = self.find_button_in_state("_fullSkipButton", buttons) or \
                       self.find_button_in_state("_miniSkipButton", buttons) or \
                       self.find_button_in_state("skip", buttons)
            if skip_btn:
                self.click_game_relative(skip_btn["x"], skip_btn["y"])
                self.log_message("DIALOGUE", "INFO", f"Dialogue skip button clicked at ({skip_btn['x']}, {skip_btn['y']})")
                time.sleep(1.0)
                return True
                
            # Check for next buttons
            next_btn = self.find_button_in_state("_fullNextButton", buttons) or \
                       self.find_button_in_state("_miniNextButton", buttons) or \
                       self.find_button_in_state("next", buttons)
            if next_btn:
                self.click_game_relative(next_btn["x"], next_btn["y"])
                self.log_message("DIALOGUE", "INFO", f"Dialogue next button clicked at ({next_btn['x']}, {next_btn['y']})")
                time.sleep(1.0)
                return True
                
            # Fallback coordinate
            self.click_game_relative(1100, 650)
            self.log_message("DIALOGUE", "INFO", "Dialogue advanced via screen fallback (1100, 650)")
            time.sleep(1.0)
            return True
        return False

    def wait_for_template_coord(self, name, timeout=10, threshold=0.8):
        if getattr(self, 'current_step_is_skipped', False) or getattr(self, 'skip_current_step', False):
            return (0, 0)
            
        self.check_paused()
        
        # 1. ALWAYS check the game state JSON from Unity exporter first!
        # This completely avoids cropping UI templates if the exporter provides it!
        start_time = time.time()
        while time.time() - start_time < timeout:
            self.check_paused()
            if self.stop_flag:
                raise InterruptedError()
            if getattr(self, 'skip_current_step', False):
                return (0, 0)
                
            # If auto dialogue progress is enabled, run it
            if self.config.auto_sync_ui:
                if self.check_and_auto_progress_dialogue():
                    start_time = time.time()
                    continue
                    
            state = self.read_game_state()
            if state:
                btn_pos = self.find_button_in_state(name, state.get("buttons", {}))
                if btn_pos:
                    self.log_message("SYNC UI", "INFO", f"Successfully synced button '{name}' to coordinate ({btn_pos['x']}, {btn_pos['y']})")
                    return (btn_pos["x"], btn_pos["y"])
                    
            # 2. Fall back to template match if JSON state does not contain the button,
            # but only check template match if the file exists (no wizard prompt by default unless we must)
            template_path = self.get_template_path(name)
            if os.path.exists(template_path):
                pos = self.match_template_on_screen(template_path, threshold)
                if pos:
                    self.log_message("CROP UI", "INFO", f"CV match template '{name}' at {pos}")
                    return pos
            
            time.sleep(0.5)
            
        # 3. If still not found and template doesn't exist, we fall back to capture wizard
        template_path = self.get_template_path(name)
        if not os.path.exists(template_path):
            self.log_message("CROP UI", "INFO", f"Pattern '{name}' is unmapped. Starting capture sequence...")
            captured = self.request_template_capture(name)
            if captured:
                # Try to match it once
                pos = self.match_template_on_screen(template_path, threshold)
                if pos:
                    return pos
                    
        self.log_message("SYNC UI", "FAIL", f"Button '{name}' not found via game state sync or CV within timeout.")
        return None

    def match_template_on_screen(self, template_path, threshold=0.8):
        rect = self.get_game_rect()
        if not rect:
            return None
        gx, gy, gw_w, gw_h = rect
        screenshot = ImageGrab.grab(bbox=(gx, gy, gx + gw_w, gy + gw_h))
        screen_np = cv2.cvtColor(np.array(screenshot), cv2.COLOR_RGB2BGR)
        screen_np = cv2.resize(screen_np, (1280, 720))
        screen_gray = cv2.cvtColor(screen_np, cv2.COLOR_BGR2GRAY)
        
        template = cv2.imread(template_path, cv2.IMREAD_GRAYSCALE)
        if template is None:
            return None
        res = cv2.matchTemplate(screen_gray, template, cv2.TM_CCOEFF_NORMED)
        min_val, max_val, min_loc, max_loc = cv2.minMaxLoc(res)
        if max_val >= threshold:
            h, w = template.shape
            center_x = max_loc[0] + w // 2
            center_y = max_loc[1] + h // 2
            return (center_x, center_y)
        return None

    def request_template_capture(self, name):
        capture_done_event = threading.Event()
        self.crop_result = False
        def run_gui_capture():
            self.show_capture_wizard(name, capture_done_event)
        self.root.after(0, run_gui_capture)
        capture_done_event.wait()
        return self.crop_result

    def show_capture_wizard(self, name, done_event):
        rect = self.get_game_rect()
        active_game = self.config.get_active_game()
        window_title = active_game.get("window_title", "Maou-Sama-TD")
        if not rect:
            messagebox.showerror("Capture", f"Game window '{window_title}' must be running and visible to capture templates!")
            self.crop_result = False
            done_event.set()
            return
        gx, gy, gw_w, gw_h = rect
        
        was_recording = False
        if hasattr(self, 'capture_thread') and self.capture_thread.writer is not None:
            was_recording = True
            rec_path = self.capture_thread.recording_path
            self.capture_thread.stop_recording()
            
        messagebox.showinfo("HUD Capture Mode", f"Visual Frame target '{name}' required.\n\nClick OK, then click and drag a box closely over the button or element on the game screen.")
        screenshot = ImageGrab.grab(bbox=(gx, gy, gx + gw_w, gy + gw_h))
        screenshot = screenshot.resize((1280, 720), Image.Resampling.LANCZOS)
        gw_w = 1280
        gw_h = 720
        
        crop_root = tk.Toplevel(self.root)
        crop_root.title("Select Area")
        crop_root.geometry(f"{gw_w}x{gw_h}+{gx}+{gy}")
        crop_root.overrideredirect(True)
        crop_root.attributes("-topmost", True)
        
        photo = ImageTk.PhotoImage(screenshot)
        canvas = tk.Canvas(crop_root, cursor="cross", width=gw_w, height=gw_h)
        canvas.pack(fill="both", expand=True)
        canvas.create_image(0, 0, image=photo, anchor="nw")
        canvas.photo = photo

        
        state = {"rect": None, "sx": 0, "sy": 0, "cx": 0, "cy": 0}
        
        def on_press(event):
            state["sx"] = event.x
            state["sy"] = event.y
            state["rect"] = canvas.create_rectangle(event.x, event.y, event.x, event.y, outline="red", width=2)
            
        def on_drag(event):
            state["cx"] = event.x
            state["cy"] = event.y
            canvas.coords(state["rect"], state["sx"], state["sy"], event.x, event.y)
            
        def on_release(event):
            state["cx"] = event.x
            state["cy"] = event.y
            crop_root.destroy()
            x1 = min(state["sx"], state["cx"])
            y1 = min(state["sy"], state["cy"])
            x2 = max(state["sx"], state["cx"])
            y2 = max(state["sy"], state["cy"])
            if x2 - x1 > 5 and y2 - y1 > 5:
                cropped = screenshot.crop((x1, y1, x2, y2))
                dest_path = self.get_template_path(name)
                cropped.save(dest_path)
                self.log_message("CROP UI", "INFO", f"Pattern '{name}' mapped successfully.")
                self.crop_result = True
            else:
                self.crop_result = False
            if was_recording and not self.stop_flag:
                self.capture_thread.start_recording(rec_path)
            done_event.set()
            
        canvas.bind("<ButtonPress-1>", on_press)
        canvas.bind("<B1-Motion>", on_drag)
        canvas.bind("<ButtonRelease-1>", on_release)
        crop_root.focus_force()

    def manual_capture_template(self):
        name_win = tk.Toplevel(self.root)
        name_win.title("Name Pattern Target")
        name_win.geometry("300x120+1320+100")
        name_win.configure(bg=self.bg_dark)
        name_win.attributes("-topmost", True)
        lbl = tk.Label(name_win, text="Enter Target Pattern Identifier:", bg=self.bg_dark, fg=self.fg_light, font=("Consolas", 8, "bold"))
        lbl.pack(pady=10)
        entry = tk.Entry(name_win, width=30, bg="#1b1b26", fg=self.fg_light, bd=0)
        entry.pack(pady=5, ipady=3)
        entry.focus()
        def confirm():
            name = entry.get().strip()
            if not name:
                messagebox.showerror("Error", "Name identifier cannot be empty!")
                return
            name_win.destroy()
            done_evt = threading.Event()
            self.root.after(100, lambda: self.show_capture_wizard(name, done_evt))
        btn = ttk.Button(name_win, text="CROP REGION", command=confirm)
        btn.pack(pady=10)

    def toggle_manual_recording(self):
        if not hasattr(self, 'capture_thread') or not self.capture_thread:
            messagebox.showerror("Error", "Live capture thread is not running.")
            return
            
        is_recording = False
        with self.capture_thread.writer_lock:
            if self.capture_thread.writer is not None:
                is_recording = True
                
        if is_recording:
            self.capture_thread.stop_recording()
            if hasattr(self, 'btn_trigger_record') and self.btn_trigger_record:
                self.btn_trigger_record.config(text="🔴 TRIGGER MANUAL RECORDING", fg=self.success_glow)
            self.log_message("Recorder", "INFO", "Manual screen recording stopped.")
        else:
            os.makedirs(self.recordings_dir, exist_ok=True)
            
            codec = getattr(self, 'recording_codec_var', None)
            codec_val = codec.get() if codec else "IVF"
            
            fps_var = getattr(self, 'recording_fps_var', None)
            fps_val_str = fps_var.get() if fps_var else "30"
            
            ext = ".avi"
            if "IVF" in codec_val or "VP8" in codec_val:
                ext = ".ivf"
                codec_str = "VP80"
            elif "MP4" in codec_val:
                ext = ".mp4"
                codec_str = "mp4v"
            else:
                ext = ".avi"
                codec_str = "MJPG"
                
            try:
                fps = int(fps_val_str.split()[0])
            except Exception:
                fps = 30
                
            rec_path = os.path.join(self.recordings_dir, f"manual_record_{int(time.time())}{ext}")
            self.capture_thread.start_recording(rec_path, codec_str=codec_str, fps=fps)
            
            if hasattr(self, 'btn_trigger_record') and self.btn_trigger_record:
                self.btn_trigger_record.config(text="⏹️ STOP RECORDING", fg=self.fail_glow)
            self.log_message("Recorder", "INFO", f"Manual recording started: {os.path.basename(rec_path)} (Codec={codec_str}, FPS={fps})")

    def ensure_default_scenarios(self):
        # Only ensure the scenarios directory exists — no default files are generated.
        # All scenarios are maintained as hand-crafted .lua files in the scenarios folder.
        os.makedirs(self.scenarios_dir, exist_ok=True)

    def next_step(self):
        if not (self.test_thread and self.test_thread.is_alive()):
            return
        self.log_message("DEBUG", "INFO", "Skipping current step...")
        self.skip_current_step = True
        # If paused, temporarily resume to process the skip
        if not self.pause_event.is_set():
            self.run_to_next_step = True
            self.pause_event.set()
            self.btn_pause.config(text="PAUSE")

    def prev_step(self):
        if not (self.test_thread and self.test_thread.is_alive()):
            return
        if self.current_step_idx is None or self.current_step_idx <= 0:
            self.log_message("DEBUG", "WARNING", "No previous step to jump to.")
            return
            
        target_idx = self.current_step_idx - 1
        self.log_message("DEBUG", "INFO", f"Rewinding to step index {target_idx}...")
        
        selected_indices = self.scenario_listbox.curselection()
        if not selected_indices:
            return
        selected_scenario = self.scenario_listbox.get(selected_indices[0])
        
        # Abort active run
        self.stop_flag = True
        self.pause_event.set()
        if self.log_monitor:
            self.log_monitor.running = False
        if hasattr(self, 'capture_thread'):
            self.capture_thread.stop_recording()
            
        def restart_flow():
            self.test_thread.join(timeout=2.0)
            self.root.after(0, lambda: self.start_test_flow_to_step(selected_scenario, target_idx))
            
        threading.Thread(target=restart_flow, daemon=True).start()

    def repeat_step(self):
        if not (self.test_thread and self.test_thread.is_alive()):
            return
        if self.current_step_idx is None:
            self.log_message("DEBUG", "WARNING", "No current step to repeat.")
            return
            
        target_idx = self.current_step_idx
        self.log_message("DEBUG", "INFO", f"Repeating step index {target_idx}...")
        
        selected_indices = self.scenario_listbox.curselection()
        if not selected_indices:
            return
        selected_scenario = self.scenario_listbox.get(selected_indices[0])
        
        # Abort active run
        self.stop_flag = True
        self.pause_event.set()
        if self.log_monitor:
            self.log_monitor.running = False
        if hasattr(self, 'capture_thread'):
            self.capture_thread.stop_recording()
            
        def restart_flow():
            self.test_thread.join(timeout=2.0)
            self.root.after(0, lambda: self.start_test_flow_to_step(selected_scenario, target_idx))
            
        threading.Thread(target=restart_flow, daemon=True).start()

    def start_test_flow_to_step(self, scenario_name, target_idx):
        self.stop_flag = False
        self.pause_event.set()
        self.auto_skip_to_step_idx = target_idx
        self.pause_at_target = True
        
        self.set_control_states("disabled", "normal", "normal", "PAUSE")
        self.btn_clear_save.config(state="disabled")
        self.btn_launch.config(state="disabled")
        self.tree.delete(*self.tree.get_children())
        
        script_path = os.path.join(self.scenarios_dir, f"{scenario_name}.lua")
        self.test_thread = TestSequenceRunner(self, scenario_name, script_path)
        self.test_thread.start()

    def parse_lua_actions(self, file_path):
        steps = []
        if not os.path.exists(file_path):
            return steps
            
        current_step = {"name": "Lobby / Pre-run", "actions": []}
        try:
            with open(file_path, "r", encoding="utf-8") as f:
                lines = f.readlines()
        except Exception:
            return steps
            
        for line in lines:
            line_strip = line.strip()
            if not line_strip or line_strip.startswith("--"):
                continue
                
            m_stage = re.search(r'set_stage\([\'"]([^\'"]+)[\'"]\)', line_strip)
            if m_stage:
                if current_step["actions"] or current_step["name"] != "Lobby / Pre-run":
                    steps.append(current_step)
                current_step = {"name": m_stage.group(1), "actions": []}
                continue
                
            m_wait_temp = re.search(r'wait_template\([\'"]([^\'"]+)[\'"]', line_strip)
            if m_wait_temp:
                current_step["actions"].append({
                    "type": "wait_template",
                    "target": m_wait_temp.group(1),
                    "line": line_strip
                })
                continue
                
            m_click = re.search(r'click\(([^,]+),\s*([^)]+)\)', line_strip)
            if m_click:
                current_step["actions"].append({
                    "type": "click",
                    "x": m_click.group(1).strip(),
                    "y": m_click.group(2).strip(),
                    "line": line_strip
                })
                continue
                
            m_drag = re.search(r'drag\(([^,]+),\s*([^,]+),\s*([^,]+),\s*([^,)]+)', line_strip)
            if m_drag:
                current_step["actions"].append({
                    "type": "drag",
                    "x1": m_drag.group(1).strip(),
                    "y1": m_drag.group(2).strip(),
                    "x2": m_drag.group(3).strip(),
                    "y2": m_drag.group(4).strip(),
                    "line": line_strip
                })
                continue
                
            m_wait = re.search(r'\bwait\((\d+(\.\d+)?)\)', line_strip)
            if m_wait:
                current_step["actions"].append({
                    "type": "wait",
                    "seconds": m_wait.group(1),
                    "line": line_strip
                })
                continue
                
            if "launch_game" in line_strip:
                current_step["actions"].append({
                    "type": "launch_game",
                    "line": line_strip
                })
                continue
                
            if "clear_save_data" in line_strip:
                current_step["actions"].append({
                    "type": "clear_save_data",
                    "line": line_strip
                })
                continue
                
        if current_step["actions"] or current_step["name"] != "Lobby / Pre-run":
            steps.append(current_step)
        return steps

    def try_resolve_variable_button(self, vx, vy, actions, buttons_dict):
        var_x = vx.split('.')[0] if '.' in vx else vx
        var_y = vy.split('.')[0] if '.' in vy else vy
        if var_x == var_y:
            for act in actions:
                if act["type"] == "wait_template":
                    line = act["line"]
                    if var_x in line:
                        return self.find_button_in_state(act["target"], buttons_dict)
        return None

    def try_resolve_variable_coords(self, vx, vy, actions, buttons_dict):
        btn = self.try_resolve_variable_button(vx, vy, actions, buttons_dict)
        if btn:
            return (btn["x"], btn["y"])
        return None

    def refresh_locations_view(self):
        game_state = self.read_game_state()
        buttons_dict = {}
        if game_state:
            buttons_dict = game_state.get("buttons", {})
            
        rect = self.get_game_rect() if self.show_screen_coords_var.get() else None
        
        if hasattr(self, 'live_buttons_tree'):
            selected_items = self.live_buttons_tree.selection()
            selected_name = self.live_buttons_tree.item(selected_items[0])["values"][0] if selected_items else None
            self.live_buttons_tree.delete(*self.live_buttons_tree.get_children())
            for name, coords in sorted(buttons_dict.items()):
                text_val = coords.get("text", "")
                if rect:
                    cx, cy, gw_w, gw_h = rect
                    x_val = f"{cx + int(coords.get('x', 0.0) * gw_w / 1280):.1f}"
                    y_val = f"{cy + int(coords.get('y', 0.0) * gw_h / 720):.1f}"
                    w_val = f"{int(coords.get('w', 0.0) * gw_w / 1280):.1f}"
                    h_val = f"{int(coords.get('h', 0.0) * gw_h / 720):.1f}"
                else:
                    x_val = f"{coords.get('x', 0.0):.1f}"
                    y_val = f"{coords.get('y', 0.0):.1f}"
                    w_val = f"{coords.get('w', 0.0):.1f}"
                    h_val = f"{coords.get('h', 0.0):.1f}"
                item_id = self.live_buttons_tree.insert(
                    "", "end", 
                    values=(name, text_val, x_val, y_val, w_val, h_val)
                )
                if selected_name and name == selected_name:
                    self.live_buttons_tree.selection_set(item_id)
                    
        selected_indices = self.scenario_listbox.curselection()
        if not selected_indices:
            if hasattr(self, 'scenario_steps_tree'):
                self.scenario_steps_tree.delete(*self.scenario_steps_tree.get_children())
            return
            
        selected_scenario = self.scenario_listbox.get(selected_indices[0])
        script_path = os.path.join(self.scenarios_dir, f"{selected_scenario}.lua")
        parsed_steps = self.parse_lua_actions(script_path)
        
        if hasattr(self, 'scenario_steps_tree'):
            expanded_nodes = {}
            for child in self.scenario_steps_tree.get_children():
                node_text = self.scenario_steps_tree.item(child)["text"]
                expanded_nodes[node_text] = self.scenario_steps_tree.item(child, "open")
                
            self.scenario_steps_tree.delete(*self.scenario_steps_tree.get_children())
            
            for step in parsed_steps:
                step_name = step["name"]
                
                active_stage = self.stage_lbl["text"] if hasattr(self, 'stage_lbl') else "Idle"
                active_stage_lower = active_stage.lower()
                if " [scene:" in active_stage_lower:
                    active_stage_lower = active_stage_lower.split(" [scene:")[0]
                if "status:" in active_stage_lower:
                    active_stage_lower = active_stage_lower.split("status:")[1].strip()
                if "system status:" in active_stage_lower:
                    active_stage_lower = active_stage_lower.split("system status:")[1].strip()
                    
                s_clean = re.sub(r'^\d+\.\s*', '', step_name).lower().strip()
                is_active = (s_clean in active_stage_lower or active_stage_lower in s_clean)
                
                is_skipped = self.skipped_steps.get((selected_scenario, step_name), False)
                step_display = step_name
                if is_skipped:
                    step_display += " (SKIPPED)"
                elif is_active:
                    step_display += " ◀ CURRENT"
                    
                step_node_id = self.scenario_steps_tree.insert("", "end", text=step_display, values=("set_stage", step_name, "", ""))
                
                # Expand by default or keep previous state
                if expanded_nodes.get(step_display, True) or is_active:
                    self.scenario_steps_tree.item(step_node_id, open=True)
                    
                for action in step["actions"]:
                    action_type = action["type"]
                    action_target = ""
                    action_coords = ""
                    action_text_size = ""
                    
                    if action_type == "wait_template":
                        action_target = action["target"]
                        btn_pos = self.find_button_in_state(action_target, buttons_dict)
                        if btn_pos:
                            if rect:
                                cx, cy, gw_w, gw_h = rect
                                abs_x = cx + int(btn_pos['x'] * gw_w / 1280)
                                abs_y = cy + int(btn_pos['y'] * gw_h / 720)
                                action_coords = f"({abs_x:.1f}, {abs_y:.1f}) [Screen Live]"
                            else:
                                action_coords = f"({btn_pos['x']:.1f}, {btn_pos['y']:.1f}) [Unity Live]"
                            action_text_size = f"'{btn_pos.get('text', '')}' | {btn_pos.get('w', 0.0):.1f}x{btn_pos.get('h', 0.0):.1f}"
                        else:
                            action_coords = "Pending / CV Match"
                            action_text_size = ""
                    elif action_type == "click":
                        cx_val = action["x"]
                        cy_val = action["y"]
                        action_target = f"({cx_val}, {cy_val})"
                        btn_pos = self.try_resolve_variable_button(cx_val, cy_val, step["actions"], buttons_dict)
                        if btn_pos:
                            if rect:
                                cx, cy, gw_w, gw_h = rect
                                abs_x = cx + int(btn_pos['x'] * gw_w / 1280)
                                abs_y = cy + int(btn_pos['y'] * gw_h / 720)
                                action_coords = f"({abs_x:.1f}, {abs_y:.1f}) [Resolved Screen]"
                            else:
                                action_coords = f"({btn_pos['x']:.1f}, {btn_pos['y']:.1f}) [Resolved]"
                            action_text_size = f"'{btn_pos.get('text', '')}' | {btn_pos.get('w', 0.0):.1f}x{btn_pos.get('h', 0.0):.1f}"
                        else:
                            try:
                                rx = float(cx_val)
                                ry = float(cy_val)
                                if rect:
                                    cx, cy, gw_w, gw_h = rect
                                    abs_x = cx + int(rx * gw_w / 1280)
                                    abs_y = cy + int(ry * gw_h / 720)
                                    action_coords = f"({abs_x:.1f}, {abs_y:.1f}) [Screen]"
                                else:
                                    action_coords = f"({rx:.1f}, {ry:.1f})"
                            except ValueError:
                                action_coords = "Unknown Variable"
                            action_text_size = ""
                    elif action_type == "drag":
                        cx1, cy1, cx2, cy2 = action["x1"], action["y1"], action["x2"], action["y2"]
                        action_target = f"({cx1},{cy1}) to ({cx2},{cy2})"
                        btn1 = self.try_resolve_variable_button(cx1, cy1, step["actions"], buttons_dict)
                        btn2 = self.try_resolve_variable_button(cx2, cy2, step["actions"], buttons_dict)
                        
                        r1 = (btn1["x"], btn1["y"]) if btn1 else None
                        r2 = (btn2["x"], btn2["y"]) if btn2 else None
                        
                        if r1 or r2:
                            if rect:
                                cx, cy, gw_w, gw_h = rect
                                c1_str = f"({cx + int(r1[0] * gw_w / 1280):.1f}, {cy + int(r1[1] * gw_h / 720):.1f})" if r1 else f"({cx1}, {cy1})"
                                c2_str = f"({cx + int(r2[0] * gw_w / 1280):.1f}, {cy + int(r2[1] * gw_h / 720):.1f})" if r2 else f"({cx2}, {cy2})"
                                action_coords = f"{c1_str} -> {c2_str} [Resolved Screen]"
                            else:
                                c1_str = f"({r1[0]:.1f}, {r1[1]:.1f})" if r1 else f"({cx1}, {cy1})"
                                c2_str = f"({r2[0]:.1f}, {r2[1]:.1f})" if r2 else f"({cx2}, {cy2})"
                                action_coords = f"{c1_str} -> {c2_str} [Resolved]"
                        else:
                            try:
                                rx1 = float(cx1)
                                ry1 = float(cy1)
                                rx2 = float(cx2)
                                ry2 = float(cy2)
                                if rect:
                                    cx, cy, gw_w, gw_h = rect
                                    c1_str = f"({cx + int(rx1 * gw_w / 1280):.1f}, {cy + int(ry1 * gw_h / 720):.1f})"
                                    c2_str = f"({cx + int(rx2 * gw_w / 1280):.1f}, {cy + int(ry2 * gw_h / 720):.1f})"
                                    action_coords = f"{c1_str} -> {c2_str} [Screen]"
                                else:
                                    action_coords = f"({rx1:.1f},{ry1:.1f}) -> ({rx2:.1f},{ry2:.1f})"
                            except ValueError:
                                action_coords = f"({cx1},{cy1}) -> ({cx2},{cy2})"
                            
                        parts = []
                        if btn1:
                            parts.append(f"Start: '{btn1.get('text', '')}'")
                        if btn2:
                            parts.append(f"End: '{btn2.get('text', '')}'")
                        action_text_size = " | ".join(parts) if parts else ""
                    elif action_type == "wait":
                        action_target = f"{action['seconds']} seconds"
                        action_coords = "Wait"
                        action_text_size = ""
                    elif action_type == "launch_game":
                        action_target = "Start Game Process"
                        action_coords = "System Action"
                        action_text_size = ""
                    elif action_type == "clear_save_data":
                        action_target = "Purge Cache"
                        action_coords = "System Action"
                        action_text_size = ""
                        
                    self.scenario_steps_tree.insert(
                        step_node_id, "end", 
                        text=f"• {action_type.upper()}", 
                        values=(action_type, action_target, action_coords, action_text_size)
                    )

    def loop_refresh_locations(self):
        if getattr(self, 'active_mid_tab', '') == 'locations':
            self.refresh_locations_view()
            self.root.after(1000, self.loop_refresh_locations)


class ToolTip:
    def __init__(self, widget, text):
        self.widget = widget
        self.text = text
        self.tip_window = None
        widget.bind("<Enter>", self.show_tip)
        widget.bind("<Leave>", self.hide_tip)

    def show_tip(self, event=None):
        if self.tip_window or not self.text:
            return
        x = self.widget.winfo_rootx() + 20
        y = self.widget.winfo_rooty() + self.widget.winfo_height() + 5
        self.tip_window = tw = tk.Toplevel(self.widget)
        tw.withdraw()
        tw.wm_overrideredirect(True)
        tw.wm_geometry(f"+{x}+{y}")
        tw.attributes("-topmost", True)
        tw.configure(bg="#1a1a20")
        
        # Tooltip frame & label styled in dark theme
        label = tk.Label(
            tw, text=self.text, justify="left",
            background="#1a1a20", foreground="#a855f7",
            relief="solid", borderwidth=1,
            font=("Segoe UI", 8, "bold"), padx=5, pady=3
        )
        label.pack(fill="both", expand=True)
        tw.update_idletasks()
        tw.deiconify()

    def hide_tip(self, event=None):
        tw = self.tip_window
        self.tip_window = None
        if tw:
            tw.destroy()
