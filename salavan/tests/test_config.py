import os
import tempfile
import unittest
import sys

# Ensure salavan package is in path for imports
current_dir = os.path.dirname(os.path.abspath(__file__))
parent_dir = os.path.dirname(current_dir)
if parent_dir not in sys.path:
    sys.path.insert(0, parent_dir)

from config import ConfigManager

class TestConfigManager(unittest.TestCase):
    def setUp(self):
        self.temp_file = tempfile.NamedTemporaryFile(delete=False, suffix=".json")
        self.temp_file.close()
        self.config_path = self.temp_file.name

    def tearDown(self):
        if os.path.exists(self.config_path):
            os.remove(self.config_path)

    def test_default_initialization(self):
        manager = ConfigManager(self.config_path)
        self.assertEqual(manager.active_game_id, "maou_sama_td")
        active = manager.get_active_game()
        self.assertIsNotNone(active)
        self.assertEqual(active.get("title"), "Maou-Sama-TD")
        self.assertTrue(len(manager.games) >= 1)

    def test_add_and_switch_game(self):
        manager = ConfigManager(self.config_path)
        new_game = {
            "id": "space_shooter",
            "title": "Space Shooter",
            "window_title": "SpaceShooter",
            "process_name": "SpaceShooter.exe",
            "save_paths": ["%USERPROFILE%/save.json"],
            "log_path": "%USERPROFILE%/Player.log",
            "active_exe_path": ""
        }
        manager.games.append(new_game)
        manager.active_game_id = "space_shooter"
        manager.save()

        # Load fresh configuration and assert
        new_manager = ConfigManager(self.config_path)
        self.assertEqual(new_manager.active_game_id, "space_shooter")
        active = new_manager.get_active_game()
        self.assertEqual(active.get("title"), "Space Shooter")
        self.assertEqual(active.get("process_name"), "SpaceShooter.exe")

if __name__ == "__main__":
    unittest.main()
