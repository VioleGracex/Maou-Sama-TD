import sys
import os
import unittest
from unittest.mock import patch, MagicMock
from PySide6.QtWidgets import QApplication, QMessageBox, QInputDialog
from PySide6.QtCore import Qt, QTimer
from PySide6.QtTest import QTest

# Ensure gui_pyside is in the path
current_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
pyside_dir = os.path.join(current_dir, "gui_pyside")
if pyside_dir not in sys.path:
    sys.path.insert(0, pyside_dir)
if current_dir not in sys.path:
    sys.path.insert(0, current_dir)

from core.config import ConfigManager
from core.logger import ReportLogger
from core.app_controller import AppController
from ui.main_window import MainWindow

class TestGUIDashboard(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        # Create QApplication instance if not already running
        cls.app = QApplication.instance()
        if cls.app is None:
            cls.app = QApplication(sys.argv)
            
        cls.config_path = os.path.join(current_dir, "config_test_gui.json")
        cls.config = ConfigManager(cls.config_path)
        cls.logger = ReportLogger()
        cls.app_controller = AppController(cls.config, cls.logger)
        
    @classmethod
    def tearDownClass(cls):
        cls.app_controller.shutdown()
        if os.path.exists(cls.config_path):
            try:
                os.remove(cls.config_path)
            except Exception:
                pass

    def setUp(self):
        self.window = MainWindow(self.app_controller)
        self.window.show()

    def tearDown(self):
        self.window.close()

    def test_sidebar_navigation_buttons(self):
        """Validate main sidebar navigation buttons exist, are clickable, and switch pages."""
        self.assertTrue(self.window.btn_games.isEnabled())
        self.assertTrue(self.window.btn_settings.isEnabled())
        self.assertTrue(self.window.btn_about.isEnabled())

        # Click settings tab and check index
        QTest.mouseClick(self.window.btn_settings, Qt.LeftButton)
        self.assertEqual(self.window.pages_stack.currentIndex(), 1)

        # Click about tab and check index
        QTest.mouseClick(self.window.btn_about, Qt.LeftButton)
        self.assertEqual(self.window.pages_stack.currentIndex(), 2)

        # Click games tab and check index
        QTest.mouseClick(self.window.btn_games, Qt.LeftButton)
        self.assertEqual(self.window.pages_stack.currentIndex(), 0)

    def test_details_page_routing_and_back(self):
        """Validate navigating to Details page and clicking BACK TO LIBRARY works."""
        active_game = self.config.get_active_game()
        self.window.show_details_page(active_game)
        self.assertEqual(self.window.pages_stack.currentIndex(), 3)

        # Click back button and verify library page is displayed
        details = self.window.page_details
        self.assertTrue(details.btn_back.isEnabled())
        QTest.mouseClick(details.btn_back, Qt.LeftButton)
        self.assertEqual(self.window.pages_stack.currentIndex(), 0)

    @patch('core.app_controller.AppController.start_test')
    def test_scenarios_and_builds_control_stubs(self, mock_start_test):
        """Validate scenarios execution buttons and builds database buttons are clickable and functional."""
        active_game = self.config.get_active_game()
        self.window.show_details_page(active_game)
        details = self.window.page_details

        # Sidebars existence
        self.assertIsNotNone(details.scenarios_sidebar)
        self.assertIsNotNone(details.builds_sidebar)

        # Scenarios sidebar execution buttons clickable states when not running
        self.assertTrue(details.scenarios_sidebar.btn_run.isEnabled())
        self.assertFalse(details.scenarios_sidebar.btn_pause.isEnabled())
        self.assertFalse(details.scenarios_sidebar.btn_stop.isEnabled())
        
        # Test clicking RUN TEST triggers start_test
        QTest.mouseClick(details.scenarios_sidebar.btn_run, Qt.LeftButton)
        mock_start_test.assert_called_once()

        # Builds database buttons
        self.assertTrue(details.builds_sidebar.btn_add.isEnabled())
        self.assertTrue(details.builds_sidebar.btn_del.isEnabled())
        self.assertTrue(details.builds_sidebar.btn_set.isEnabled())
        self.assertTrue(details.builds_sidebar.btn_scan.isEnabled())

    def test_dashboard_tabs_switching(self):
        """Validate tabs inside CenterDashboard switch correctly and buttons exist."""
        active_game = self.config.get_active_game()
        self.window.show_details_page(active_game)
        dashboard = self.window.page_details.center_dashboard

        # Switch to each of the 6 tabs and check stack index
        for i in range(6):
            QTest.mouseClick(dashboard.tab_btns[i], Qt.LeftButton)
            self.assertEqual(dashboard.stack.currentIndex(), i)

    @patch('core.app_controller.AppController.clear_save_data')
    @patch('core.app_controller.AppController.launch_game')
    @patch('ui.dialogs.capture_wizard.CaptureWizard.exec')
    @patch('PySide6.QtWidgets.QInputDialog.getText', return_value=("TestPattern", True))
    @patch('PySide6.QtWidgets.QMessageBox.question', return_value=QMessageBox.Yes)
    @patch('PySide6.QtWidgets.QMessageBox.information')
    def test_live_view_tab_buttons(self, mock_info, mock_question, mock_get_text, mock_capture_exec, mock_launch, mock_clear_save):
        """Validate PURGE SAVE, BOOT GAME, and COORDS TOOL buttons are functional and call corresponding actions."""
        active_game = self.config.get_active_game()
        self.window.show_details_page(active_game)
        dashboard = self.window.page_details.center_dashboard

        # Switch to Live View tab (index 1)
        dashboard.switch_tab(1)

        self.assertTrue(dashboard.btn_clear_save.isEnabled())
        self.assertTrue(dashboard.btn_launch.isEnabled())
        self.assertTrue(dashboard.btn_capture.isEnabled())
        
        # Mocking config game_exe_path for boot game test
        self.app_controller.config.game_exe_path = sys.executable
        
        # Test PURGE SAVE
        QTest.mouseClick(dashboard.btn_clear_save, Qt.LeftButton)
        mock_clear_save.assert_called_once()
        
        # Test BOOT GAME
        QTest.mouseClick(dashboard.btn_launch, Qt.LeftButton)
        mock_launch.assert_called_once()
        
        # Test COORDS TOOL
        # Mock game window running state so it doesn't fail the visible check
        self.app_controller.game_hooks.game_rect = (0, 0, 1280, 720)
        QTest.mouseClick(dashboard.btn_capture, Qt.LeftButton)
        mock_capture_exec.assert_called_once()

    def test_logs_dock_undock_reparenting(self):
        """Validate pop out and dock logs logic moves the widget correctly."""
        active_game = self.config.get_active_game()
        self.window.show_details_page(active_game)
        details = self.window.page_details

        # Initially, logs_panel is in splitter
        self.assertEqual(details.logs_panel.parent(), details.center_splitter)
        self.assertFalse(details.center_dashboard.btn_dock.isVisible())
        self.assertTrue(details.logs_panel.btn_pop.isVisible())

        # Click POP OUT
        QTest.mouseClick(details.logs_panel.btn_pop, Qt.LeftButton)
        self.assertNotEqual(details.logs_panel.parent(), details.center_splitter)
        self.assertEqual(details.logs_panel.parent(), details.logs_dialog)
        self.assertTrue(details.center_dashboard.btn_dock.isVisible())
        self.assertFalse(details.logs_panel.btn_pop.isVisible())

        # Click DOCK LOGS
        QTest.mouseClick(details.center_dashboard.btn_dock, Qt.LeftButton)
        self.assertEqual(details.logs_panel.parent(), details.center_splitter)
        self.assertFalse(details.center_dashboard.btn_dock.isVisible())
        self.assertTrue(details.logs_panel.btn_pop.isVisible())

    def test_overlay_mode_geometry_changes(self):
        """Validate overlay mode toggles window flags and updates visibility."""
        self.assertFalse(self.window.isMinimized())
        self.assertEqual(self.window.windowOpacity(), 1.0)

        # Toggle overlay mode
        self.window.toggle_overlay_mode()
        self.assertTrue(self.window.overlay_mode)
        self.assertAlmostEqual(self.window.windowOpacity(), 0.95, places=2)
        self.assertTrue(self.window.windowFlags() & Qt.FramelessWindowHint)
        self.assertTrue(self.window.windowFlags() & Qt.WindowStaysOnTopHint)

        # Toggle back
        self.window.toggle_overlay_mode()
        self.assertFalse(self.window.overlay_mode)
        self.assertEqual(self.window.windowOpacity(), 1.0)
        self.assertFalse(self.window.windowFlags() & Qt.FramelessWindowHint)
        self.assertFalse(self.window.windowFlags() & Qt.WindowStaysOnTopHint)

if __name__ == "__main__":
    unittest.main()
