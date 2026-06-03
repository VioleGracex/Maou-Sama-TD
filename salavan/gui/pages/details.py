import os
import sys
import time
import subprocess
import platform
import tkinter as tk
from tkinter import ttk, messagebox
import threading

class DetailsPage(tk.Frame):
    def __init__(self, parent, app):
        super().__init__(parent, bg=app.bg_dark)
        self.app = app
        
        # Override parent's column frames to bind to DetailsPage
        self.app.left_border = None
        self.app.center_border = None
        self.app.right_border = None
        self.app.right_sidebar_border = None
        
        self.create_layouts()

    def create_layouts(self):
        # Create Vertical PanedWindow split: Top (Sidebar & Dashboard) and Bottom (Logs)
        self.paned_window = tk.PanedWindow(self, orient=tk.VERTICAL, bg=self.app.bg_dark, bd=0, sashwidth=4, sashpad=2)
        self.paned_window.pack(fill="both", expand=True)

        # Create Top Content Container (holds Center dashboard, Right Handle, and Right Sidebar)
        self.top_content_container = tk.Frame(self.paned_window, bg=self.app.bg_dark)
        self.paned_window.add(self.top_content_container, minsize=350, stretch="always")

        # 2. Right Sidebar (Game Builds Database)
        self.right_sidebar_pane = tk.Frame(self.top_content_container, bg=self.app.bg_dark, width=260)
        self.right_sidebar_pane.pack_propagate(False)
        self.right_sidebar_pane.pack(side="right", fill="y")
        
        # Right Sidebar Divider Line
        self.right_sidebar_div = tk.Frame(self.top_content_container, bg=self.app.accent_dim, width=1)
        self.right_sidebar_div.pack(side="right", fill="y")
        
        # Right Handle Button
        self.btn_right_handle = tk.Button(
            self.top_content_container, text="▶", command=self.app.toggle_builds_panel,
            bg="#131317", fg=self.app.accent_glow, activebackground=self.app.accent_glow,
            activeforeground="#131317", bd=0, relief="flat",
            font=("Segoe UI", 7, "bold"), width=1, padx=2, cursor="hand2"
        )
        self.btn_right_handle.pack(side="right", fill="y")
        self.app.btn_right_handle = self.btn_right_handle
        
        # Hover binding for right handle
        def make_r_hover(b):
            b.bind("<Enter>", lambda e: b.config(bg=self.app.accent_glow, fg="#131317"))
            b.bind("<Leave>", lambda e: b.config(bg="#131317", fg=self.app.accent_glow))
        make_r_hover(self.btn_right_handle)
        self.app.add_tooltip(self.btn_right_handle, "Toggle Builds & Reports Sidebar")
        
        from gui.widgets.builds_sidebar import create_builds_sidebar
        create_builds_sidebar(self.app, self.right_sidebar_pane)

        # 1. Center Column Frame (Operations Dashboard)
        self.center_pane = tk.Frame(self.top_content_container, bg=self.app.bg_dark)
        self.center_pane.pack(side="left", fill="both", expand=True)
        
        self.app.center_border = tk.Frame(self.center_pane, bg=self.app.accent_dim, bd=1)
        self.app.center_border.pack(fill="both", expand=True, padx=5, pady=15)
        
        center_panel = tk.Frame(self.app.center_border, bg=self.app.bg_panel, padx=10, pady=10)
        center_panel.pack(fill="both", expand=True)
        
        # Top Header control strip
        center_bar = tk.Frame(center_panel, bg=self.app.bg_dark)
        center_bar.pack(fill="x", pady=(0, 10))
        self.app.center_bar = center_bar
        
        self.btn_back_lib = tk.Button(
            center_bar, text="[ 🏠 GAMES LIBRARY ]", command=self.app.show_library_page, 
            bg="#2c2c35", fg=self.app.fg_light, activebackground=self.app.accent_glow, 
            activeforeground="#101012", bd=0, padx=10, font=("Segoe UI", 9, "bold")
        )
        self.btn_back_lib.pack(side="left", padx=5)
 
        self.app.btn_toggle_mode = tk.Button(
            center_bar, text="[ 🖥 OVERLAY MODE ]", command=self.app.toggle_hud_mode, 
            bg="#2c2c35", fg=self.app.fg_light, activebackground=self.app.accent_glow, 
            activeforeground="#101012", bd=0, padx=12, font=("Segoe UI", 9, "bold")
        )
        self.app.btn_toggle_mode.pack(side="left", expand=True)
        
        self.app.btn_dock_main = tk.Button(
            center_bar, text="[ 📥 DOCK LOGS ]", command=self.app.toggle_logs_docking, 
            bg="#2c2c35", fg=self.app.fg_light, activebackground=self.app.accent_glow, 
            activeforeground="#101012", bd=0, padx=10, font=("Segoe UI", 9, "bold")
        )
        # Not packed initially (logs are docked by default)
        
        # Tkinter buttons hover bindings
        def bind_hover(btn):
            btn.bind("<Enter>", lambda e: btn.config(bg=self.app.accent_glow, fg="#101012"))
            btn.bind("<Leave>", lambda e: btn.config(bg="#2c2c35", fg=self.app.fg_light))
        bind_hover(self.btn_back_lib)
        bind_hover(self.app.btn_toggle_mode)
        bind_hover(self.app.btn_dock_main)

        # Attach tooltips to details header buttons
        self.app.add_tooltip(self.btn_back_lib, "Return to Games Library Catalog")
        self.app.add_tooltip(self.app.btn_toggle_mode, "Toggle Fullscreen Low-Opacity Overlay HUD")
        self.app.add_tooltip(self.app.btn_dock_main, "Dock Diagnostic Logs back into operations dashboard")
        
        # Setup inputs panel
        from gui.widgets.center import create_center_panel
        create_center_panel(self.app, center_panel)
        
        # Reports Hub suggestions context menu
        self.context_menu = tk.Menu(self, tearoff=0, bg="#111114", fg=self.app.fg_light, activebackground=self.app.accent_glow, activeforeground=self.app.fg_light, font=("Consolas", 9))
        
        if hasattr(self.app, 'reports_tree') and self.app.reports_tree:
            self.app.reports_tree.bind("<Button-3>", self.show_reports_context_menu)
            self.app.reports_tree.bind("<Double-1>", self.on_double_click_report)
        
        # 3. Bottom Column (Diagnostic Logs list)
        self.right_pane = tk.Frame(self.paned_window, bg=self.app.bg_dark)
        self.paned_window.add(self.right_pane, minsize=180, height=220, stretch="never")
        
        from gui.widgets.logs import create_logs_panel
        create_logs_panel(self.app, self.right_pane)
        
        # Populate lists
        self.populate_reports()

    def get_system_specs(self):
        specs = {
            "os": f"{platform.system()} {platform.release()}",
            "cpu": "Unknown Processor",
            "ram": "Unknown Memory",
            "gpu": "Unknown GPU"
        }
        
        # Query Registry for precise CPU Name
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

        # Query total physical RAM (wmic)
        try:
            if platform.system() == "Windows":
                out = subprocess.check_output("wmic computersystem get totalphysicalmemory", shell=True).decode()
                lines = [line.strip() for line in out.splitlines() if line.strip()]
                if len(lines) > 1 and lines[1].isdigit():
                    ram_bytes = int(lines[1])
                    specs["ram"] = f"{ram_bytes / (1024**3):.1f} GB"
        except Exception:
            pass

        # Query Video Card name (wmic)
        try:
            if platform.system() == "Windows":
                out = subprocess.check_output("wmic path win32_VideoController get name", shell=True).decode()
                lines = [line.strip() for line in out.splitlines() if line.strip()]
                if len(lines) > 1:
                    specs["gpu"] = lines[1]
        except Exception:
            pass

        return specs

    def populate_reports(self):
        if not hasattr(self.app, 'reports_tree') or not self.app.reports_tree:
            return
        self.app.reports_tree.delete(*self.app.reports_tree.get_children())
        self.reports_data = []
        
        active_game = self.app.config.get_active_game()
        game_title = active_game.get("title", "Maou-Sama-TD")
        reports_dir = os.path.join(
            os.path.expanduser('~'), 'Documents', game_title, 'salavan', 'Reports'
        )
        
        if not os.path.exists(reports_dir):
            return
            
        try:
            for item in os.listdir(reports_dir):
                item_path = os.path.join(reports_dir, item)
                if os.path.isdir(item_path) and item.startswith("Report_"):
                    parts = item.split("_")
                    date_str, time_str = "-", "-"
                    if len(parts) >= 3:
                        d, t = parts[1], parts[2]
                        if len(d) == 8 and len(t) == 6:
                            date_str = f"{d[0:4]}-{d[4:6]}-{d[6:8]}"
                            time_str = f"{t[0:2]}:{t[2:4]}:{t[4:6]}"
                            
                    status = "Unknown"
                    duration = "-"
                    xml_path = os.path.join(item_path, "junit_report.xml")
                    if os.path.exists(xml_path):
                        try:
                            import xml.etree.ElementTree as ET
                            tree = ET.parse(xml_path)
                            root = tree.getroot()
                            failures = int(root.find("testsuite").get("failures", "0"))
                            duration = f"{float(root.get('time', '0')):.1f}s"
                            status = "FAIL" if failures > 0 else "PASS"
                        except Exception:
                            pass
                            
                    self.reports_data.append({
                        "name": item,
                        "path": item_path,
                        "date": date_str,
                        "time": time_str,
                        "status": status,
                        "duration": duration
                    })
            
            # Sort newest first
            self.reports_data.sort(key=lambda r: r["name"], reverse=True)
            for r in self.reports_data:
                self.app.reports_tree.insert(
                    "", "end",
                    values=(r["date"], r["time"], r["status"], r["duration"]),
                    tags=(r["status"],)
                )
        except Exception:
            pass

    def show_reports_context_menu(self, event):
        item = self.app.reports_tree.identify_row(event.y)
        if not item:
            return
            
        self.app.reports_tree.selection_set(item)
        idx = self.app.reports_tree.index(item)
        if idx >= len(self.reports_data):
            return
            
        report = self.reports_data[idx]
        self.context_menu.delete(0, tk.END)
        self.context_menu.add_command(label="📁 Open Report Directory", command=lambda: self.open_report_dir(report["path"]))
        
        video_path = os.path.join(report["path"], "video.avi")
        if os.path.exists(video_path):
            self.context_menu.add_command(label="🎥 Play Screen Recording", command=lambda: self.play_recording(video_path))
            
        log_path = os.path.join(report["path"], "test_log.txt")
        if os.path.exists(log_path):
            self.context_menu.add_command(label="📄 View Plaintext Log File", command=lambda: self.view_text_file(log_path))
            
        xml_path = os.path.join(report["path"], "junit_report.xml")
        if os.path.exists(xml_path):
            self.context_menu.add_command(label="📊 View JUnit XML Report", command=lambda: self.view_text_file(xml_path))
            
        self.context_menu.post(event.x_root, event.y_root)

    def on_double_click_report(self, event):
        selected = self.app.reports_tree.selection()
        if not selected:
            return
        idx = self.app.reports_tree.index(selected[0])
        if idx < len(self.reports_data):
            report = self.reports_data[idx]
            self.open_report_dir(report["path"])

    def open_report_dir(self, path):
        try:
            os.startfile(path)
        except Exception as e:
            messagebox.showerror("Error", f"Could not open directory: {str(e)}")

    def play_recording(self, path):
        try:
            os.startfile(path)
        except Exception as e:
            messagebox.showerror("Error", f"Could not play video: {str(e)}")

    def view_text_file(self, path):
        try:
            os.startfile(path)
        except Exception as e:
            messagebox.showerror("Error", f"Could not view file: {str(e)}")
