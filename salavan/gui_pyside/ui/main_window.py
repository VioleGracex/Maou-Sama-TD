from PySide6.QtWidgets import (
    QMainWindow, QWidget, QVBoxLayout, QHBoxLayout, QPushButton, QStackedWidget, QLabel
)
from PySide6.QtCore import Qt
from ui.pages.library_page import LibraryPage
from ui.pages.settings_page import SettingsPage
from ui.pages.about_page import AboutPage
from ui.pages.details_page import DetailsPage

class MainWindow(QMainWindow):
    def __init__(self, app_controller, parent=None):
        super().__init__(parent)
        self.app_controller = app_controller
        self.setWindowTitle("SALAVAN-HUD GAME SALAVAN PANEL v3.2.3")
        self.setMinimumSize(800, 500)
        self.resize(1200, 800)
        self.setStyleSheet("background-color: #0d0d11; color: white;")
        
        self._init_ui()

    def _init_ui(self):
        self.central_widget = QWidget()
        self.setCentralWidget(self.central_widget)
        
        # Use a vertical root layout to hold the top bar and the content area
        self.root_layout = QVBoxLayout(self.central_widget)
        self.root_layout.setContentsMargins(0, 0, 0, 0)
        self.root_layout.setSpacing(0)
        
        # Top Header Bar
        self.header_bar = QWidget()
        self.header_bar.setFixedHeight(40)
        self.header_bar.setStyleSheet("background-color: #18181c; border-bottom: 1px solid #1a1a21;")
        header_layout = QHBoxLayout(self.header_bar)
        header_layout.setContentsMargins(10, 0, 10, 0)
        header_layout.setSpacing(10)
        
        self.btn_nav_toggle = QPushButton("☰")
        self.btn_nav_toggle.setFixedSize(30, 30)
        self.btn_nav_toggle.setStyleSheet("""
            QPushButton { background-color: transparent; color: #f3f4f6; font-size: 16px; border: none; font-weight: bold; }
            QPushButton:hover { background-color: #2c2c35; color: #a855f7; }
        """)
        self.btn_nav_toggle.clicked.connect(self.toggle_sidebar)
        header_layout.addWidget(self.btn_nav_toggle)
        
        self.title_lbl = QLabel(" 🛡️  SALAVAN-HUD GAME SALAVAN PANEL v3.2.3")
        self.title_lbl.setStyleSheet("color: #a855f7; font-weight: bold; font-family: 'Segoe UI'; font-size: 13px; border: none;")
        header_layout.addWidget(self.title_lbl)
        header_layout.addStretch()
        
        self.root_layout.addWidget(self.header_bar)

        # Content Area
        self.content_widget = QWidget()
        self.main_layout = QHBoxLayout(self.content_widget)
        self.main_layout.setContentsMargins(0, 0, 0, 0)
        self.main_layout.setSpacing(0)
        
        self.root_layout.addWidget(self.content_widget)

        # Navigation Sidebar
        self.nav_sidebar = QWidget()
        self.nav_sidebar.setFixedWidth(65)
        self.nav_sidebar.setStyleSheet("background-color: #101012; border-right: 1px solid #1a1a21;")
        self.nav_layout = QVBoxLayout(self.nav_sidebar)
        self.nav_layout.setContentsMargins(0, 20, 0, 20)
        
        self.btn_games = QPushButton("GAMES")
        self.btn_settings = QPushButton("SETTINGS")
        self.btn_about = QPushButton("ABOUT")
        
        nav_btn_style = """
            QPushButton {
                background-color: transparent;
                color: #9ca3af;
                border: none;
                padding: 15px 0;
                font-weight: bold;
                font-family: 'Segoe UI';
            }
            QPushButton:hover {
                background-color: #202024;
                color: #ffffff;
            }
            QPushButton:checked {
                color: #a855f7;
            }
        """
        for btn in [self.btn_games, self.btn_settings, self.btn_about]:
            btn.setStyleSheet(nav_btn_style)
            btn.setCheckable(True)
            self.nav_layout.addWidget(btn)
        
        self.nav_layout.addStretch()

        # Pages Stack
        self.pages_stack = QStackedWidget()
        
        # Instantiate pages
        self.page_library = LibraryPage(self.app_controller, self)
        self.page_settings = SettingsPage(self.app_controller, self)
        self.page_about = AboutPage(self.app_controller, self)
        self.page_details = DetailsPage(self.app_controller, self)
        
        self.pages_stack.addWidget(self.page_library) # 0: Library
        self.pages_stack.addWidget(self.page_settings) # 1: Settings
        self.pages_stack.addWidget(self.page_about) # 2: About
        self.pages_stack.addWidget(self.page_details) # 3: Details
        
        self.main_layout.addWidget(self.nav_sidebar)
        self.main_layout.addWidget(self.pages_stack)

        self.btn_games.clicked.connect(lambda: self.switch_tab(0))
        self.btn_settings.clicked.connect(lambda: self.switch_tab(1))
        self.btn_about.clicked.connect(lambda: self.switch_tab(2))
        
        self.btn_games.setChecked(True)

    def switch_tab(self, index):
        self.pages_stack.setCurrentIndex(index)
        self.btn_games.setChecked(index == 0)
        self.btn_settings.setChecked(index == 1)
        self.btn_about.setChecked(index == 2)

    def show_details_page(self, game_data):
        self.page_details.load_game(game_data)
        self.switch_tab(3)

    def toggle_sidebar(self):
        is_visible = self.nav_sidebar.isVisible()
        self.nav_sidebar.setVisible(not is_visible)

    def toggle_overlay_mode(self):
        if not hasattr(self, 'overlay_mode'):
            self.overlay_mode = False
            
        if not self.overlay_mode:
            self.overlay_mode = True
            self.normal_geometry = self.geometry()
            
            # Hide title header and navigation sidebar
            self.header_bar.hide()
            self.nav_sidebar.hide()
            
            # Switch page stack to details
            self.switch_tab(3)
            
            # Hide surrounding elements on details page
            self.page_details.scenarios_sidebar.hide()
            self.page_details.builds_sidebar.hide()
            self.page_details.logs_panel.hide()
            
            # Switch dashboard to Live View
            self.page_details.center_dashboard.switch_tab(1)
            
            # Change window flags to frameless, translucent, and topmost
            self.setWindowFlags(self.windowFlags() | Qt.WindowStaysOnTopHint | Qt.FramelessWindowHint)
            self.setWindowOpacity(0.95)
            self.show() # Apply window flags changes
            
            # Set target geometry (380x600 size at top-left screen position)
            self.setGeometry(0, 0, 380, 600)
        else:
            self.overlay_mode = False
            
            # Restore window flags
            self.setWindowFlags(self.windowFlags() & ~Qt.WindowStaysOnTopHint & ~Qt.FramelessWindowHint)
            self.setWindowOpacity(1.0)
            self.show() # Apply changes
            
            # Restore elements
            self.header_bar.show()
            self.nav_sidebar.show()
            self.page_details.scenarios_sidebar.show()
            self.page_details.builds_sidebar.show()
            self.page_details.logs_panel.show()
            
            self.setGeometry(self.normal_geometry)

    def mousePressEvent(self, event):
        if hasattr(self, 'overlay_mode') and self.overlay_mode:
            if event.button() == Qt.LeftButton:
                self.drag_position = event.globalPosition().toPoint() - self.frameGeometry().topLeft()
                event.accept()

    def mouseMoveEvent(self, event):
        if hasattr(self, 'overlay_mode') and self.overlay_mode:
            if event.buttons() == Qt.LeftButton:
                self.move(event.globalPosition().toPoint() - self.drag_position)
                event.accept()

