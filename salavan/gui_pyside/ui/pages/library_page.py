import os
from PySide6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QLabel, QPushButton, 
    QScrollArea, QGridLayout, QFrame
)
from PySide6.QtGui import QPixmap
from PySide6.QtCore import Qt, QSize
from ui.dialogs.manage_games_dialog import ManageGamesDialog

class LibraryPage(QWidget):
    def __init__(self, app_controller, main_window=None):
        super().__init__(main_window)
        self.app_controller = app_controller
        self.main_window = main_window
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
        
        title = QLabel("Game Library")
        title.setStyleSheet("color: white; font-size: 18px; font-weight: bold; font-family: 'Segoe UI'; border: none;")
        header_layout.addWidget(title)
        header_layout.addStretch()
        
        btn_manage = QPushButton("Manage Games")
        btn_manage.setStyleSheet("QPushButton { background: #27272a; color: white; border-radius: 4px; padding: 6px 15px; } QPushButton:hover { background: #3f3f46; }")
        btn_manage.clicked.connect(self._open_manage)
        header_layout.addWidget(btn_manage)
        
        # Scroll Area
        self.scroll_area = QScrollArea()
        self.scroll_area.setWidgetResizable(True)
        self.scroll_area.setMinimumSize(300, 200)
        self.scroll_area.setStyleSheet("""
            QScrollArea { border: none; background-color: #0d0d11; }
            QScrollBar:vertical {
                background: #101012;
                width: 10px;
                margin: 0px 0px 0px 0px;
            }
            QScrollBar::handle:vertical {
                background: #3f3f46;
                min-height: 20px;
                border-radius: 5px;
            }
            QScrollBar::handle:vertical:hover {
                background: #52525b;
            }
            QScrollBar::add-line:vertical, QScrollBar::sub-line:vertical { height: 0px; }
        """)
        
        self.grid_container = QWidget()
        self.grid_container.setStyleSheet("background-color: #0d0d11;")
        self.grid_layout = QGridLayout(self.grid_container)
        self.grid_layout.setContentsMargins(30, 30, 30, 30)
        self.grid_layout.setSpacing(20)
        
        self.scroll_area.setWidget(self.grid_container)
        
        self.main_layout.addWidget(self.header)
        self.main_layout.addWidget(self.scroll_area)
        
        self._populate_games()

    def _populate_games(self):
        # Clear existing
        while self.grid_layout.count():
            child = self.grid_layout.takeAt(0)
            if child.widget():
                child.widget().deleteLater()
                
        games = self.app_controller.config.games
        
        col = 0
        row = 0
        max_cols = 4
        
        for game in games:
            card = self._create_game_card(game)
            self.grid_layout.addWidget(card, row, col)
            col += 1
            if col >= max_cols:
                col = 0
                row += 1
                
        # Push items to the top-left if the grid isn't full
        self.grid_layout.setRowStretch(row + 1, 1)
        self.grid_layout.setColumnStretch(max_cols, 1)

    def _create_game_card(self, game_data):
        card = QFrame()
        card.setFixedSize(260, 240)
        card.setStyleSheet("""
            QFrame {
                background-color: #18181c;
                border-radius: 8px;
                border: 1px solid #27272a;
            }
            QFrame:hover {
                border: 1px solid #a855f7;
            }
        """)
        
        layout = QVBoxLayout(card)
        layout.setContentsMargins(0, 0, 0, 0)
        layout.setSpacing(0)
        
        # Cover Image
        cover_label = QLabel()
        cover_label.setFixedHeight(130)
        cover_label.setStyleSheet("border-bottom: 1px solid #27272a; border-radius: 8px; border-bottom-left-radius: 0px; border-bottom-right-radius: 0px; background-color: #0d0d11;")
        
        from core.paths import get_base_dir
        base_dir = get_base_dir()
        img_path = os.path.join(base_dir, f"{game_data['id']}_cover.png")
        if not os.path.exists(img_path):
            img_path = os.path.join(base_dir, "assets", f"{game_data['id']}_cover.png")
            
        if os.path.exists(img_path):
            pixmap = QPixmap(img_path).scaled(260, 130, Qt.KeepAspectRatioByExpanding, Qt.SmoothTransformation)
            cover_label.setPixmap(pixmap)
        else:
            # Placeholder text if no image
            cover_label.setText("🎮")
            cover_label.setAlignment(Qt.AlignCenter)
            cover_label.setStyleSheet("font-size: 48px; border-bottom: 1px solid #27272a; background-color: #0d0d11;")
            
        layout.addWidget(cover_label)
        
        # Info Area
        info_widget = QWidget()
        info_layout = QVBoxLayout(info_widget)
        info_layout.setContentsMargins(15, 10, 15, 15)
        info_layout.setSpacing(5)
        
        title_label = QLabel(game_data.get("title", "Unknown Game"))
        title_label.setStyleSheet("color: white; font-size: 14px; font-weight: bold; border: none;")
        
        proc_label = QLabel(f"Target: {game_data.get('process_name', 'None')}")
        proc_label.setStyleSheet("color: #71717a; font-size: 11px; border: none;")
        
        play_btn = QPushButton("Play / Test")
        play_btn.setCursor(Qt.PointingHandCursor)
        play_btn.setStyleSheet("""
            QPushButton {
                background-color: #9333ea;
                color: white;
                border-radius: 4px;
                padding: 6px 0px;
                font-weight: bold;
                margin-top: 5px;
            }
            QPushButton:hover {
                background-color: #a855f7;
            }
        """)
        
        # Fix lambda to accept signal's 'checked' parameter and capture game_data
        play_btn.clicked.connect(lambda checked=False, g=game_data: self.main_window.show_details_page(g))
        
        info_layout.addWidget(title_label)
        info_layout.addWidget(proc_label)
        info_layout.addWidget(play_btn)
        
        layout.addWidget(info_widget)
        return card

    def _open_manage(self):
        dlg = ManageGamesDialog(self.app_controller, self)
        if dlg.exec():
            self._populate_games()
