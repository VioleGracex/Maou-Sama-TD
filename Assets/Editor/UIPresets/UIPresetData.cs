using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UIPresetManager
{
    // ─────────────────────────────────────────────────────────────────────────
    //  DATA STRUCTURES
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class UIObjectState
    {
        /// <summary>GlobalObjectId string — survives object renames.</summary>
        public string guid;

        /// <summary>Full hierarchy path as fallback if the GUID can't resolve.</summary>
        public string hierarchyPath;

        public bool isActive;
    }

    [Serializable]
    public class UIPreset
    {
        public string name;

        /// <summary>Asset path to the scene, e.g. "Assets/_Game/Scenes/Home_New.unity"</summary>
        public string scenePath;

        /// <summary>Display name derived from scenePath.</summary>
        public string sceneName;

        public string createdAt;

        public string group = "";

        public List<UIObjectState> entries = new List<UIObjectState>();
    }

    [Serializable]
    public class UIPresetLibrary
    {
        public List<UIPreset> presets = new List<UIPreset>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PERSISTENCE HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    public static class UIPresetStorage
    {
        /// <summary>
        /// Stored under ProjectSettings/ so Unity never triggers an asset reimport.
        /// This folder is committed to git like other project settings.
        /// </summary>
        private static readonly string StorageRoot =
            Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName,
                         "ProjectSettings", "UIPresets");

        /// <summary>
        /// Returns the JSON file path for a given scene asset path.
        /// e.g. "Assets/_Game/Scenes/Home_New.unity" → ".../ProjectSettings/UIPresets/Home_New_presets.json"
        /// </summary>
        public static string GetPresetFilePath(string scenePath)
        {
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            return Path.Combine(StorageRoot, $"{sceneName}_presets.json");
        }

        /// <summary>Loads (or creates empty) the preset library for the given scene.</summary>
        public static UIPresetLibrary Load(string scenePath)
        {
            string filePath = GetPresetFilePath(scenePath);

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var lib = JsonUtility.FromJson<UIPresetLibrary>(json);
                    if (lib != null && lib.presets != null)
                        return lib;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[UIPresetManager] Failed to parse preset file '{filePath}': {e.Message}");
                }
            }

            return new UIPresetLibrary();
        }

        /// <summary>Saves the preset library for the given scene to disk (no asset reimport).</summary>
        public static void Save(UIPresetLibrary library, string scenePath)
        {
            EnsureStorageDir();
            string filePath = GetPresetFilePath(scenePath);

            try
            {
                string json = JsonUtility.ToJson(library, prettyPrint: true);
                File.WriteAllText(filePath, json);
                // No AssetDatabase.Refresh() call — ProjectSettings is outside Assets/
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIPresetManager] Failed to save presets to '{filePath}': {e.Message}");
            }
        }

        /// <summary>
        /// Loads all preset libraries across all scenes and returns a flat list of presets.
        /// Used to display presets from other scenes in the window.
        /// </summary>
        public static List<(string scenePath, UIPreset preset)> LoadAllPresets()
        {
            var results = new List<(string, UIPreset)>();

            if (!Directory.Exists(StorageRoot))
                return results;

            foreach (string file in Directory.GetFiles(StorageRoot, "*_presets.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var lib = JsonUtility.FromJson<UIPresetLibrary>(json);
                    if (lib?.presets == null) continue;

                    foreach (var preset in lib.presets)
                        results.Add((preset.scenePath, preset));
                }
                catch { /* skip malformed files */ }
            }

            return results;
        }

        private static void EnsureStorageDir()
        {
            if (!Directory.Exists(StorageRoot))
                Directory.CreateDirectory(StorageRoot);
        }
    }
}
