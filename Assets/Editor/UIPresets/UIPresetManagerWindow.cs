using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UIPresetManager
{
    /// <summary>
    /// Scene UI Preset Manager
    /// Open via:  Tools › UI Preset Manager   or   Ctrl+Shift+U
    ///
    /// Lets you snapshot and restore the active/inactive state of all Canvas-rooted
    /// UI GameObjects in a scene. Presets are named, scene-aware, support undo,
    /// and gracefully handle deleted/missing objects.
    ///
    /// Editor-only: all operations are blocked during Play Mode.
    /// </summary>
    public class UIPresetManagerWindow : EditorWindow
    {
        // ─── Window State ─────────────────────────────────────────────────────
        private Vector2     _scroll;
        private string      _newPresetName    = "";
        private string      _searchFilter     = "";
        private int         _renamingIndex    = -1;
        private string      _renameBuffer     = "";
        private string      _lastFeedback     = "";
        private MessageType _lastFeedbackType = MessageType.Info;
        private double      _feedbackTime;
        private const double FeedbackDuration = 4.0;

        // ─── Spam / Busy Protection ───────────────────────────────────────────
        private bool   _isBusy;
        private double _lastSaveTime;
        private double _lastApplyTime;
        private double _lastScreenshotTime;
        private const double SaveCooldown       = 0.8;  // seconds
        private const double ApplyCooldown      = 0.4;
        private const double ScreenshotCooldown = 2.0;  // screenshot involves end-of-frame write

        // ─── Data ─────────────────────────────────────────────────────────────
        private List<(string scenePath, UIPreset preset)> _allPresets = new();

        // ─── Styles (built once) ──────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _badgeCurrentStyle;
        private GUIStyle _badgeOtherStyle;
        private GUIStyle _presetNameStyle;
        private GUIStyle _playModeStyle;
        private bool     _stylesBuilt;

        // ─── Colors ───────────────────────────────────────────────────────────
        private static readonly Color ColAccent       = new Color(0.35f, 0.75f, 1.00f);
        private static readonly Color ColRowEven      = new Color(0.20f, 0.20f, 0.20f, 0.40f);
        private static readonly Color ColRowOdd       = new Color(0.23f, 0.23f, 0.23f, 0.55f);
        private static readonly Color ColRowHover     = new Color(0.30f, 0.55f, 0.80f, 0.30f);
        private static readonly Color ColApplyBtn     = new Color(0.26f, 0.60f, 0.35f);
        private static readonly Color ColDeleteBtn    = new Color(0.70f, 0.23f, 0.23f);
        private static readonly Color ColBadgeSelf    = new Color(0.20f, 0.65f, 0.35f);
        private static readonly Color ColBadgeOther   = new Color(0.80f, 0.55f, 0.10f);
        private static readonly Color ColScreenshot   = new Color(0.30f, 0.45f, 0.70f);
        private static readonly Color ColBusy         = new Color(0.35f, 0.35f, 0.35f);
        private static readonly Color ColPlayMode     = new Color(0.60f, 0.15f, 0.15f, 0.80f);

        // ─────────────────────────────────────────────────────────────────────
        //  MENU
        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Tools/UI Preset Manager %#u", priority = 200)]
        public static void ShowWindow()
        {
            var win = GetWindow<UIPresetManagerWindow>("UI Preset Manager");
            win.minSize = new Vector2(420, 520);
            win.Show();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  LIFECYCLE
        // ─────────────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            RefreshAllPresets();
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaved  += OnSceneSaved;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneSaved  -= OnSceneSaved;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode) => RefreshAllPresets();
        private void OnSceneSaved(Scene scene)                       => RefreshAllPresets();

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            // Force repaint so the play-mode banner shows/hides promptly
            _isBusy = false;
            Repaint();
        }

        private void RefreshAllPresets()
        {
            _allPresets = UIPresetStorage.LoadAllPresets();
            Repaint();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  GUI — MAIN
        // ─────────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EnsureStyles();

            DrawHeader();

            // ── Play-mode guard ───────────────────────────────────────────────
            if (Application.isPlaying)
            {
                DrawPlayModeWarning();
                return; // block all interaction during play mode
            }

            DrawSearchBar();
            GUILayout.Space(4);
            DrawPresetList();
            GUILayout.FlexibleSpace();
            DrawBottomPanel();
            DrawFeedback();
        }

        // ─── PLAY MODE WARNING ────────────────────────────────────────────────

        private void DrawPlayModeWarning()
        {
            GUILayout.Space(20);
            Rect bannerRect = EditorGUILayout.GetControlRect(GUILayout.Height(60));
            EditorGUI.DrawRect(bannerRect, ColPlayMode);
            EditorGUI.DrawRect(new Rect(bannerRect.x, bannerRect.y, 4, bannerRect.height), new Color(1f, 0.3f, 0.3f));

            GUI.Label(new Rect(bannerRect.x + 12, bannerRect.y + 10, bannerRect.width - 20, 20),
                "⛔  Play Mode Active",
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, normal = { textColor = new Color(1f, 0.7f, 0.7f) } });
            GUI.Label(new Rect(bannerRect.x + 12, bannerRect.y + 32, bannerRect.width - 20, 18),
                "UI Preset operations are disabled during Play Mode.",
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.9f, 0.6f, 0.6f) } });
        }

        // ─── HEADER ──────────────────────────────────────────────────────────

        private void DrawHeader()
        {
            Rect headerRect = EditorGUILayout.GetControlRect(GUILayout.Height(42));
            EditorGUI.DrawRect(headerRect, new Color(0.12f, 0.15f, 0.20f, 1f));
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, 4, headerRect.height), ColAccent);

            GUI.Label(new Rect(headerRect.x + 12, headerRect.y + 4, headerRect.width - 200, 18),
                      "UI Preset Manager", _headerStyle);

            Scene active = SceneManager.GetActiveScene();
            string sceneName = string.IsNullOrEmpty(active.path) ? "<Unsaved Scene>" : active.name;
            GUI.Label(new Rect(headerRect.x + 12, headerRect.y + 24, headerRect.width - 200, 14),
                      $"Scene: {sceneName}",
                      new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.6f, 0.8f, 1f) } });

            // Screenshot button (header-level, always accessible)
            bool screenshotReady = !Application.isPlaying &&
                                   !_isBusy &&
                                   (EditorApplication.timeSinceStartup - _lastScreenshotTime) > ScreenshotCooldown;

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = screenshotReady ? ColScreenshot : ColBusy;
            GUI.enabled = screenshotReady;

            string ssLabel = _isBusy ? "…" : "📷";
            if (GUI.Button(new Rect(headerRect.xMax - 100, headerRect.y + 9, 30, 24), ssLabel, EditorStyles.miniButton))
                OnTakeScreenshot();

            GUI.enabled = true;
            GUI.backgroundColor = prev;

            // Refresh button
            GUI.backgroundColor = prev;
            if (GUI.Button(new Rect(headerRect.xMax - 66, headerRect.y + 9, 58, 24), "⟳ Refresh", EditorStyles.miniButton))
                RefreshAllPresets();
        }

        // ─── SEARCH ──────────────────────────────────────────────────────────

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Filter:", GUILayout.Width(40));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (!string.IsNullOrEmpty(_searchFilter) &&
                GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
                _searchFilter = "";
            EditorGUILayout.EndHorizontal();
        }

        // ─── PRESET LIST ─────────────────────────────────────────────────────

        private void DrawPresetList()
        {
            string activeScenePath = SceneManager.GetActiveScene().path;

            var filtered = _allPresets
                .Where(p => string.IsNullOrEmpty(_searchFilter) ||
                            p.preset.name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            p.preset.sceneName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(p => p.scenePath != activeScenePath)
                .ThenBy(p => p.preset.name)
                .ToList();

            if (filtered.Count == 0)
            {
                GUILayout.Space(20);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(_allPresets.Count == 0
                    ? "No presets yet — save one below!"
                    : "No presets match the filter.",
                    EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Presets  ({filtered.Count})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Scene",  EditorStyles.miniLabel, GUILayout.Width(90));
            GUILayout.Label("Apply",  EditorStyles.miniLabel, GUILayout.Width(48));
            GUILayout.Label("Del",    EditorStyles.miniLabel, GUILayout.Width(28));
            EditorGUILayout.EndHorizontal();

            Rect sep = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            EditorGUI.DrawRect(sep, new Color(0.4f, 0.4f, 0.4f, 0.5f));

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < filtered.Count; i++)
            {
                var (scenePath, preset) = filtered[i];
                DrawPresetRow(i, preset, scenePath, scenePath == activeScenePath);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawPresetRow(int index, UIPreset preset, string scenePath, bool isCurrentScene)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(32));

            bool hovered  = rowRect.Contains(Event.current.mousePosition);
            Color rowColor = hovered ? ColRowHover : (index % 2 == 0 ? ColRowEven : ColRowOdd);
            EditorGUI.DrawRect(rowRect, rowColor);

            if (isCurrentScene)
                EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, 3, rowRect.height), ColBadgeSelf);

            float x = rowRect.x + 8;
            float y = rowRect.y;

            // ── Inline rename ────────────────────────────────────────────────
            if (_renamingIndex == GetStableIndex(preset, scenePath))
            {
                GUI.SetNextControlName("RenameField");
                _renameBuffer = GUI.TextField(
                    new Rect(x, y + 7, rowRect.width - 200, 18), _renameBuffer);

                if (GUI.GetNameOfFocusedControl() != "RenameField")
                    GUI.FocusControl("RenameField");

                bool confirm = (Event.current.type == EventType.KeyDown &&
                                Event.current.keyCode == KeyCode.Return) ||
                               GUI.Button(new Rect(rowRect.xMax - 90, y + 6, 40, 20), "✓ OK", EditorStyles.miniButton);
                bool cancel  = Event.current.type == EventType.KeyDown &&
                               Event.current.keyCode == KeyCode.Escape;

                if (confirm) { CommitRename(preset, scenePath, _renameBuffer); _renamingIndex = -1; Event.current.Use(); }
                if (cancel)  { _renamingIndex = -1; Event.current.Use(); }
                return;
            }

            // ── Preset Name & Date ───────────────────────────────────────────
            GUI.Label(new Rect(x, y + 4,  rowRect.width - 195, 16), preset.name, _presetNameStyle);
            GUI.Label(new Rect(x, y + 18, rowRect.width - 195, 12), preset.createdAt,
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } });

            // ── Scene Badge ──────────────────────────────────────────────────
            string   badgeLabel = isCurrentScene ? preset.sceneName : $"⚠ {preset.sceneName}";
            GUIStyle badgeStyle = isCurrentScene ? _badgeCurrentStyle : _badgeOtherStyle;
            GUI.Label(new Rect(rowRect.xMax - 195, y + 9, 100, 16), badgeLabel, badgeStyle);

            // ── Apply Button ──────────────────────────────────────────────────
            bool applyReady = !_isBusy &&
                              (EditorApplication.timeSinceStartup - _lastApplyTime) > ApplyCooldown;

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = applyReady
                ? (isCurrentScene ? ColApplyBtn : ColBadgeOther)
                : ColBusy;
            GUI.enabled = applyReady;

            if (GUI.Button(new Rect(rowRect.xMax - 90, y + 6, 50, 20), "Apply", EditorStyles.miniButton))
                OnApplyClicked(preset, scenePath, isCurrentScene);

            GUI.enabled = true;

            // ── Delete Button ─────────────────────────────────────────────────
            GUI.backgroundColor = ColDeleteBtn;
            if (GUI.Button(new Rect(rowRect.xMax - 36, y + 6, 28, 20), "✕", EditorStyles.miniButton))
                OnDeleteClicked(preset, scenePath);

            GUI.backgroundColor = prev;

            // ── Right-click context menu ──────────────────────────────────────
            if (Event.current.type == EventType.ContextClick && rowRect.Contains(Event.current.mousePosition))
            {
                ShowContextMenu(preset, scenePath, isCurrentScene);
                Event.current.Use();
            }

            // ── Tooltip ───────────────────────────────────────────────────────
            if (hovered && Event.current.type == EventType.Repaint)
                GUI.Label(rowRect, new GUIContent("", $"{preset.entries.Count} UI objects\nSaved: {preset.createdAt}"));
        }

        // ─── BOTTOM PANEL (Save + Screenshot) ────────────────────────────────

        private void DrawBottomPanel()
        {
            // ── Save New Preset row ──────────────────────────────────────────
            Rect panelRect = EditorGUILayout.GetControlRect(GUILayout.Height(54));
            EditorGUI.DrawRect(panelRect, new Color(0.12f, 0.15f, 0.20f, 0.85f));
            EditorGUI.DrawRect(new Rect(panelRect.x, panelRect.y, panelRect.width, 1),
                               new Color(0.4f, 0.4f, 0.4f, 0.5f));

            float x = panelRect.x + 8;
            float y = panelRect.y;

            GUI.Label(new Rect(x, y + 5, 90, 16), "New Preset:", EditorStyles.boldLabel);

            GUI.SetNextControlName("NewPresetName");
            _newPresetName = GUI.TextField(new Rect(x + 88, y + 5, panelRect.width - 180, 18), _newPresetName);

            bool canSave = !string.IsNullOrWhiteSpace(_newPresetName) &&
                           !string.IsNullOrEmpty(SceneManager.GetActiveScene().path) &&
                           !_isBusy &&
                           (EditorApplication.timeSinceStartup - _lastSaveTime) > SaveCooldown;

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = canSave ? ColApplyBtn : ColBusy;
            GUI.enabled = canSave;

            bool savePressed =
                GUI.Button(new Rect(panelRect.xMax - 76, y + 4, 68, 22), "💾 Save") ||
                (canSave && Event.current.type == EventType.KeyDown &&
                 Event.current.keyCode == KeyCode.Return &&
                 GUI.GetNameOfFocusedControl() == "NewPresetName");

            if (savePressed) { OnSaveNewPreset(); Event.current.Use(); }

            GUI.enabled = true;
            GUI.backgroundColor = prev;

            // Hint text
            string hint = _isBusy
                ? "Working…"
                : string.IsNullOrEmpty(SceneManager.GetActiveScene().path)
                    ? "Save your scene first"
                    : string.IsNullOrWhiteSpace(_newPresetName)
                        ? "Enter a preset name"
                        : $"Captures all Canvas UI in '{SceneManager.GetActiveScene().name}'";

            GUI.Label(new Rect(x + 88, y + 26, panelRect.width - 100, 14),
                      hint, EditorStyles.centeredGreyMiniLabel);

            // ── Screenshot strip ─────────────────────────────────────────────
            Rect ssRect = EditorGUILayout.GetControlRect(GUILayout.Height(28));
            EditorGUI.DrawRect(ssRect, new Color(0.10f, 0.12f, 0.18f, 0.90f));
            EditorGUI.DrawRect(new Rect(ssRect.x, ssRect.y, ssRect.width, 1),
                               new Color(0.3f, 0.3f, 0.3f, 0.5f));

            bool ssReady = !_isBusy &&
                           (EditorApplication.timeSinceStartup - _lastScreenshotTime) > ScreenshotCooldown;

            GUI.backgroundColor = ssReady ? ColScreenshot : ColBusy;
            GUI.enabled = ssReady;

            string ssLabel = _isBusy
                ? "Working…"
                : (EditorApplication.timeSinceStartup - _lastScreenshotTime) < ScreenshotCooldown
                    ? $"📷 Screenshot (wait {ScreenshotCooldown - (EditorApplication.timeSinceStartup - _lastScreenshotTime):F0}s)"
                    : "📷 Take Screenshot";

            if (GUI.Button(new Rect(ssRect.x + 8, ssRect.y + 4, 180, 20), ssLabel, EditorStyles.miniButton))
                OnTakeScreenshot();

            GUI.enabled = true;
            GUI.backgroundColor = prev;

            // Screenshot path hint
            string screenshotFolder = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName, "Screenshots");
            GUI.Label(new Rect(ssRect.x + 196, ssRect.y + 6, ssRect.width - 204, 16),
                      "→ Screenshots/",
                      new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 0.6f, 0.8f) } });
        }

        // ─── FEEDBACK BAR ────────────────────────────────────────────────────

        private void DrawFeedback()
        {
            if (string.IsNullOrEmpty(_lastFeedback)) return;
            if (EditorApplication.timeSinceStartup - _feedbackTime > FeedbackDuration)
            {
                _lastFeedback = "";
                return;
            }
            EditorGUILayout.HelpBox(_lastFeedback, _lastFeedbackType);
            Repaint();
        }

        private void ShowFeedback(string msg, MessageType type = MessageType.Info)
        {
            _lastFeedback     = msg;
            _lastFeedbackType = type;
            _feedbackTime     = EditorApplication.timeSinceStartup;
            Repaint();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  ACTIONS
        // ─────────────────────────────────────────────────────────────────────

        private void OnSaveNewPreset()
        {
            if (_isBusy) return;
            if (!UIPresetCapture.IsEditorSafe()) return;

            string name = _newPresetName.Trim();
            if (string.IsNullOrEmpty(name)) return;

            Scene activeScene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
            {
                ShowFeedback("Please save the scene before creating a preset.", MessageType.Warning);
                return;
            }

            _isBusy = true;
            _lastSaveTime = EditorApplication.timeSinceStartup;

            try
            {
                var lib = UIPresetStorage.Load(activeScene.path);

                bool duplicate = lib.presets.Any(p =>
                    string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase));

                if (duplicate)
                {
                    bool overwrite = EditorUtility.DisplayDialog(
                        "Preset Already Exists",
                        $"A preset named '{name}' already exists for this scene.\nOverwrite it?",
                        "Overwrite", "Cancel");
                    if (!overwrite) { _isBusy = false; return; }
                    lib.presets.RemoveAll(p => string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase));
                }

                UIPreset captured = UIPresetCapture.CaptureCurrentScene(name);
                lib.presets.Add(captured);
                UIPresetStorage.Save(lib, activeScene.path);

                _newPresetName = "";
                GUI.FocusControl(null);
                RefreshAllPresets();
                ShowFeedback($"✔ Preset '{name}' saved — {captured.entries.Count} UI objects captured.",
                             MessageType.Info);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void OnApplyClicked(UIPreset preset, string scenePath, bool isCurrentScene)
        {
            if (_isBusy) return;
            if (!UIPresetCapture.IsEditorSafe()) return;

            if (!isCurrentScene)
            {
                bool switchScene = EditorUtility.DisplayDialog(
                    "Different Scene",
                    $"The preset '{preset.name}' belongs to scene:\n'{preset.sceneName}'\n\n" +
                    $"You are currently in:\n'{SceneManager.GetActiveScene().name}'\n\n" +
                    "Switch to the preset's scene and apply?",
                    "Switch Scene & Apply", "Cancel");

                if (!switchScene) return;
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

                EditorSceneManager.OpenScene(scenePath);
                EditorApplication.delayCall += () => ApplyAndReport(preset);
                return;
            }

            ApplyAndReport(preset);
        }

        private void ApplyAndReport(UIPreset preset)
        {
            if (_isBusy) return;
            if (!UIPresetCapture.IsEditorSafe()) return;

            _isBusy = true;
            _lastApplyTime = EditorApplication.timeSinceStartup;

            try
            {
                var result = UIPresetCapture.ApplyPreset(preset);

                string msg = $"✔ Applied '{preset.name}': {result.Applied} changed, {result.Unchanged} unchanged";
                if (result.Skipped > 0) msg += $", {result.Skipped} missing (skipped)";

                ShowFeedback(msg, result.Skipped > 0 ? MessageType.Warning : MessageType.Info);

                if (result.Skipped > 0)
                    Debug.LogWarning($"[UIPresetManager] Preset '{preset.name}': " +
                                     $"{result.Skipped} object(s) not found (likely deleted). Skipped safely.");
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void OnDeleteClicked(UIPreset preset, string scenePath)
        {
            if (_isBusy) return;

            bool confirm = EditorUtility.DisplayDialog(
                "Delete Preset",
                $"Delete preset '{preset.name}' from scene '{preset.sceneName}'?\nThis cannot be undone.",
                "Delete", "Cancel");

            if (!confirm) return;

            var lib = UIPresetStorage.Load(scenePath);
            lib.presets.RemoveAll(p => p.name == preset.name && p.scenePath == preset.scenePath);
            UIPresetStorage.Save(lib, scenePath);
            RefreshAllPresets();
            ShowFeedback($"Preset '{preset.name}' deleted.", MessageType.Info);
        }

        // ─── SCREENSHOT ───────────────────────────────────────────────────────

        private void OnTakeScreenshot()
        {
            if (_isBusy) return;
            if (!UIPresetCapture.IsEditorSafe()) return;

            _isBusy = true;
            _lastScreenshotTime = EditorApplication.timeSinceStartup;

            try
            {
                // Resolve Screenshots folder at project root (alongside Assets/)
                string projectRoot      = Directory.GetParent(Application.dataPath).FullName;
                string screenshotFolder = Path.Combine(projectRoot, "Screenshots");

                if (!Directory.Exists(screenshotFolder))
                    Directory.CreateDirectory(screenshotFolder);

                Scene scene    = SceneManager.GetActiveScene();
                string prefix  = string.IsNullOrEmpty(scene.name) ? "screenshot" : scene.name;
                string stamp   = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string fileName = $"{prefix}_{stamp}.png";
                string fullPath = Path.Combine(screenshotFolder, fileName);

                // ScreenCapture.CaptureScreenshot works in edit mode (captures the Game view).
                // The file is written at the end of the current frame by Unity's rendering backend.
                ScreenCapture.CaptureScreenshot(fullPath, superSize: 1);

                // Deferred feedback — give Unity one frame to finish the write
                EditorApplication.delayCall += () =>
                {
                    _isBusy = false;
                    // Relative path for display
                    string relativePath = $"Screenshots/{fileName}";
                    ShowFeedback($"📷 Screenshot saved: {relativePath}", MessageType.Info);
                    Debug.Log($"[UIPresetManager] Screenshot saved → {fullPath}");
                    Repaint();
                };
            }
            catch (Exception e)
            {
                _isBusy = false;
                ShowFeedback($"Screenshot failed: {e.Message}", MessageType.Error);
                Debug.LogError($"[UIPresetManager] Screenshot error: {e}");
            }
        }

        // ─── CONTEXT MENU ─────────────────────────────────────────────────────

        private void ShowContextMenu(UIPreset preset, string scenePath, bool isCurrentScene)
        {
            var menu = new GenericMenu();

            if (isCurrentScene)
            {
                menu.AddItem(new GUIContent("Apply"),     false, () => ApplyAndReport(preset));
                menu.AddItem(new GUIContent("Rename"),    false, () => BeginRename(preset, scenePath));
                menu.AddItem(new GUIContent("Duplicate"), false, () => DuplicatePreset(preset, scenePath));
                menu.AddSeparator("");
            }
            else
            {
                menu.AddItem(new GUIContent("Apply (Switch Scene)"), false,
                    () => OnApplyClicked(preset, scenePath, false));
                menu.AddSeparator("");
            }

            menu.AddItem(new GUIContent("Delete"), false, () => OnDeleteClicked(preset, scenePath));
            menu.ShowAsContext();
        }

        private void BeginRename(UIPreset preset, string scenePath)
        {
            _renamingIndex = GetStableIndex(preset, scenePath);
            _renameBuffer  = preset.name;
        }

        private void CommitRename(UIPreset preset, string scenePath, string newName)
        {
            newName = newName.Trim();
            if (string.IsNullOrEmpty(newName) || newName == preset.name) return;

            var lib    = UIPresetStorage.Load(scenePath);
            var target = lib.presets.FirstOrDefault(p =>
                p.name == preset.name && p.scenePath == preset.scenePath);
            if (target == null) return;

            target.name = newName;
            UIPresetStorage.Save(lib, scenePath);
            RefreshAllPresets();
            ShowFeedback($"Preset renamed to '{newName}'.", MessageType.Info);
        }

        private void DuplicatePreset(UIPreset preset, string scenePath)
        {
            var lib      = UIPresetStorage.Load(scenePath);
            string copyName = preset.name + "_copy";
            int n = 1;
            while (lib.presets.Any(p => p.name == copyName))
                copyName = preset.name + "_copy" + n++;

            lib.presets.Add(new UIPreset
            {
                name      = copyName,
                scenePath = preset.scenePath,
                sceneName = preset.sceneName,
                createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                entries   = preset.entries.Select(e => new UIObjectState
                {
                    guid          = e.guid,
                    hierarchyPath = e.hierarchyPath,
                    isActive      = e.isActive
                }).ToList()
            });

            UIPresetStorage.Save(lib, scenePath);
            RefreshAllPresets();
            ShowFeedback($"Preset duplicated as '{copyName}'.", MessageType.Info);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  STYLE SETUP
        // ─────────────────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal   = { textColor = ColAccent }
            };

            _presetNameStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 12,
                normal    = { textColor = Color.white }
            };

            _badgeCurrentStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = ColBadgeSelf }
            };

            _badgeOtherStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = ColBadgeOther }
            };

            _playModeStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal   = { textColor = new Color(1f, 0.7f, 0.7f) }
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UTILITIES
        // ─────────────────────────────────────────────────────────────────────

        private static int GetStableIndex(UIPreset preset, string scenePath) =>
            (scenePath + "::" + preset.name).GetHashCode();
    }
}
