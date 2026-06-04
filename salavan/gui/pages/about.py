import tkinter as tk

class AboutPage(tk.Frame):
    def __init__(self, parent, app):
        super().__init__(parent, bg=app.bg_dark, padx=30, pady=30)
        self.app = app
        self.create_widgets()

    def create_widgets(self):
        lbl_hdr = tk.Label(self, text="// PLATFORM SCHEMATICS & ABOUT", fg=self.app.accent_glow, bg=self.app.bg_dark, font=("Segoe UI", 14, "bold"))
        lbl_hdr.pack(anchor="w", pady=(0, 20))
        
        border = tk.Frame(self, bg=self.app.accent_dim, bd=1)
        border.pack(fill="both", expand=True)
        
        frame = tk.Frame(border, bg=self.app.bg_panel, padx=25, pady=25)
        frame.pack(fill="both", expand=True)
        
        spec_text = (
            "SYSTEM DIAGNOSTIC SIGNATURE:\n"
            "----------------------------\n"
            "Application   : Salavan-HUD Game Salavan Panel\n"
            "Version       : v3.0.0 (Production Release Candidate)\n"
            "Design Style  : Ultra-Dark Charcoal Gaming Dashboard\n\n"
            "PLATFORM CAPABILITIES:\n"
            "---------------------\n"
            "• Decoupled Threading scenario test sequence parser.\n"
            "• Native Lupa Lua engine integration for test flows automation.\n"
            "• Computer Vision OpenCV template classification.\n"
            "• Direct Live OBS-style capturing thread recorder.\n"
            "• Windows hardware specifications inspector.\n"
            "• JUnit standard compliant XML historical reports hub."
        )
        
        lbl_body = tk.Label(frame, text=spec_text, justify="left", anchor="nw", fg=self.app.fg_light, bg=self.app.bg_panel, font=("Segoe UI", 10, "bold"), wraplength=480)
        lbl_body.pack(fill="both", expand=True)
