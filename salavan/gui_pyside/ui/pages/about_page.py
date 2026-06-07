from PySide6.QtWidgets import QWidget, QVBoxLayout, QHBoxLayout, QLabel, QFrame
from PySide6.QtCore import Qt

class AboutPage(QWidget):
    def __init__(self, app_controller, parent=None):
        super().__init__(parent)
        self.app_controller = app_controller
        self._init_ui()

    def _init_ui(self):
        self.main_layout = QVBoxLayout(self)
        self.main_layout.setContentsMargins(0, 0, 0, 0)
        self.main_layout.setSpacing(0)
        
        # Header
        self.header = QWidget()
        self.header.setStyleSheet("background-color: #101012; border-bottom: 1px solid #1a1a21;")
        self.header.setFixedHeight(60)
        header_layout = QHBoxLayout(self.header)
        header_layout.setContentsMargins(20, 0, 20, 0)
        
        title = QLabel("About")
        title.setStyleSheet("color: white; font-size: 18px; font-weight: bold; font-family: 'Segoe UI'; border: none;")
        header_layout.addWidget(title)
        header_layout.addStretch()
        
        # Content
        self.content_container = QWidget()
        self.content_container.setStyleSheet("background-color: #0d0d11;")
        self.content_layout = QVBoxLayout(self.content_container)
        self.content_layout.setContentsMargins(30, 30, 30, 30)
        self.content_layout.setSpacing(20)
        self.content_layout.setAlignment(Qt.AlignTop | Qt.AlignHCenter)
        
        # Card
        card = QFrame()
        card.setStyleSheet("QFrame { background-color: #18181c; border-radius: 8px; border: 1px solid #27272a; }")
        card.setFixedSize(400, 250)
        
        card_layout = QVBoxLayout(card)
        card_layout.setAlignment(Qt.AlignCenter)
        card_layout.setSpacing(10)
        
        icon = QLabel("⚔️")
        icon.setStyleSheet("font-size: 64px; border: none;")
        icon.setAlignment(Qt.AlignCenter)
        
        app_title = QLabel("Sylvan-HUD Game Salavan Panel")
        app_title.setStyleSheet("color: white; font-size: 18px; font-weight: bold; border: none;")
        app_title.setAlignment(Qt.AlignCenter)
        
        version = QLabel("Version 3.2.5 (PySide6 Edition)")
        version.setStyleSheet("color: #a855f7; font-size: 14px; border: none;")
        version.setAlignment(Qt.AlignCenter)
        
        credits = QLabel("Automated testing framework for Unity games.\nPowered by Qt for Python and Lupa.")
        credits.setStyleSheet("color: #a1a1aa; font-size: 12px; border: none;")
        credits.setAlignment(Qt.AlignCenter)
        
        card_layout.addWidget(icon)
        card_layout.addWidget(app_title)
        card_layout.addWidget(version)
        card_layout.addSpacing(10)
        card_layout.addWidget(credits)
        
        self.content_layout.addWidget(card)
        
        self.main_layout.addWidget(self.header)
        self.main_layout.addWidget(self.content_container)
