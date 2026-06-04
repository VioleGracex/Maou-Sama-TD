import os
import json

class ConfigManager:
    def __init__(self, config_path):
        self.config_path = config_path
        self.game_exe_path = ""
        self.record_test = True
        self.dev_build_mode = False
        self.hook_unity_editor = False
        self.auto_sync_ui = True
        self.game_width = 960
        self.game_height = 540
        self.builds = []
        self.hotkeys = {
            "pause": "<Control-p>",
            "abort": "<Control-q>",
            "toggle_mode": "<Control-o>",
            "next": "<Control-Right>",
            "prev": "<Control-Left>",
            "repeat": "<Control-Down>"
        }
        self.games = []
        self.active_game_id = "maou_sama_td"
        self.load()

    def get_default_games(self):
        base_dir = os.path.dirname(self.config_path)
        default_mapping = os.path.normpath(os.path.join(base_dir, "mappings", "maou_sama_td_mappings.json"))
        return [
            {
                "id": "maou_sama_td",
                "title": "Maou-Sama-TD",
                "window_title": "Maou-Sama-TD",
                "process_name": "Maou-Sama-TD.exe",
                "save_paths": [
                    "%USERPROFILE%/Documents/Maou-Sama-TD/player_save.json",
                    "%USERPROFILE%/AppData/Local/Low/Ouiki.Dev/Maou-Sama-TD/player_save.json",
                    "%USERPROFILE%/AppData/LocalLow/Ouiki.Dev/Maou-Sama-TD/player_save.json"
                ],
                "log_path": "%USERPROFILE%/AppData/LocalLow/Ouiki.Dev/Maou-Sama-TD/Player.log",
                "active_exe_path": "",
                "ui_mapping_path": default_mapping
            }
        ]

    def get_game_by_id(self, game_id):
        for g in self.games:
            if g.get("id") == game_id:
                return g
        return None

    def get_active_game(self):
        g = self.get_game_by_id(self.active_game_id)
        if not g:
            if self.games:
                g = self.games[0]
                self.active_game_id = g.get("id")
            else:
                self.games = self.get_default_games()
                g = self.games[0]
                self.active_game_id = g.get("id")
        return g

    def load(self):
        if os.path.exists(self.config_path):
            try:
                with open(self.config_path, "r", encoding="utf-8") as f:
                    data = json.load(f)
                    self.game_exe_path = data.get("game_exe_path", "")
                    self.record_test = data.get("record_test", True)
                    self.dev_build_mode = data.get("dev_build_mode", False)
                    self.hook_unity_editor = data.get("hook_unity_editor", False)
                    self.auto_sync_ui = data.get("auto_sync_ui", True)
                    self.game_width = data.get("game_width", 960)
                    self.game_height = data.get("game_height", 540)
                    self.builds = data.get("builds", [])
                    self.active_game_id = data.get("active_game_id", "maou_sama_td")
                    self.games = data.get("games", [])
                    
                    loaded_hotkeys = data.get("hotkeys", {})
                    for k, v in loaded_hotkeys.items():
                        if k in self.hotkeys:
                            self.hotkeys[k] = v
            except Exception:
                pass
        
        # Ensure default games are loaded if empty
        if not self.games:
            self.games = self.get_default_games()
        else:
            # Sync missing keys from defaults
            defaults = self.get_default_games()
            for g in self.games:
                for dg in defaults:
                    if g.get("id") == dg.get("id"):
                        for k, v in dg.items():
                            if k not in g:
                                g[k] = v
            
        # Synced compatibility for game_exe_path
        active_game = self.get_active_game()
        if active_game:
            if not active_game.get("active_exe_path") and self.game_exe_path:
                active_game["active_exe_path"] = self.game_exe_path
            self.game_exe_path = active_game.get("active_exe_path", "")

    def save(self):
        try:
            # Sync active_exe_path with game_exe_path on save
            active_game = self.get_active_game()
            if active_game:
                active_game["active_exe_path"] = self.game_exe_path
                
            with open(self.config_path, "w", encoding="utf-8") as f:
                json.dump({
                    "game_exe_path": self.game_exe_path,
                    "record_test": self.record_test,
                    "dev_build_mode": self.dev_build_mode,
                    "hook_unity_editor": self.hook_unity_editor,
                    "auto_sync_ui": self.auto_sync_ui,
                    "game_width": self.game_width,
                    "game_height": self.game_height,
                    "builds": self.builds,
                    "hotkeys": self.hotkeys,
                    "active_game_id": self.active_game_id,
                    "games": self.games
                }, f, indent=4)
        except Exception:
            pass

