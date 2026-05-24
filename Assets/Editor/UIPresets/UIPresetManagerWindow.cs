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
        private const double SaveCooldown       = 0.8;
        private const double ApplyCooldown      = 0.4;
        private const double ScreenshotCooldown = 2.0;

        // ─── Data ─────────────────────────────────────────────────────────────
        private List<(string scenePath, UIPreset preset)> _allPresets = new();
        
        // ─── Grouping & Selection ─────────────────────────────────────────────
        private HashSet<int> _selectedIndices = new HashSet<int>();
        private Dictionary<string, bool> _groupExpanded = new Dictionary<string, bool>();
        private HashSet<string> _emptyGroups = new HashSet<string>(); // Tracks user-created empty groups in session
        private int _lastSelectedIndex = -1; // For shift-click ranges

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
        private static readonly Color ColRowSelected  = new Color(0.15f, 0.40f, 0.65f, 0.70f);
        private static readonly Color ColApplyBtn     = new Color(0.26f, 0.60f, 0.35f);
        private static readonly Color ColDeleteBtn    = new Color(0.70f, 0.23f, 0.23f);
        private static readonly Color ColBadgeSelf    = new Color(0.20f, 0.65f, 0.35f);
        private static readonly Color ColBadgeOther   = new Color(0.80f, 0.55f, 0.10f);
        private static readonly Color ColScreenshot   = new Color(0.30f, 0.45f, 0.70f);
        private static readonly Color ColBusy         = new Color(0.35f, 0.35f, 0.35f);
        private static readonly Color ColPlayMode     = new Color(0.60f, 0.15f, 0.15f, 0.80f);

        // ─────────────────────────────────────────────────────────────────────
        //  MENU & LIFECYCLE
        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Tools/UI Preset Manager %#u", priority = 200)]
        public static void ShowWindow()
        {
            var win = GetWindow<UIPresetManagerWindow>("UI Preset Manager");
            win.minSize = new Vector2(420, 520);
            win.Show();
        }

        private void OnEnable()
        {
            RefreshAllPresets();
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaved  += OnSceneSaved;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            wantsMouseMove = true; // Need mouse move for hover effects
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
            _isBusy = false;
            Repaint();
        }

        private void RefreshAllPresets()
        {
            _allPresets = UIPresetStorage.LoadAllPresets();
            
            // Clean up selections that no longer exist
            var validHashes = new HashSet<int>(_allPresets.Select(p => GetStableIndex(p.preset, p.scenePath)));
            _selectedIndices.RemoveWhere(hash => !validHashes.Contains(hash));
            
            Repaint();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  GUI — MAIN
        // ─────────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader();

            if (Application.isPlaying)
            {
                DrawPlayModeWarning();
                return;
            }

            DrawSearchBar();
            GUILayout.Space(4);
            DrawPresetList();
            GUILayout.FlexibleSpace();
            DrawBottomPanel();
            DrawFeedback();
        }

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

            bool screenshotReady = !Application.isPlaying && !_isBusy &&
                                   (EditorApplication.timeSinceStartup - _lastScreenshotTime) > ScreenshotCooldown;

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = screenshotReady ? ColScreenshot : ColBusy;
            GUI.enabled = screenshotReady;

            string ssLabel = _isBusy ? "…" : "📷";
            if (GUI.Button(new Rect(headerRect.xMax - 100, headerRect.y + 9, 30, 24), ssLabel, EditorStyles.miniButton))
                OnTakeScreenshot();

            GUI.enabled = true;
            GUI.backgroundColor = prev;

            if (GUI.Button(new Rect(headerRect.xMax - 66, headerRect.y + 9, 58, 24), "⟳ Refresh", EditorStyles.miniButton))
                RefreshAllPresets();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Filter:", GUILayout.Width(40));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (!string.IsNullOrEmpty(_searchFilter) &&
                GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
                _searchFilter = "";

            GUILayout.Space(10);
            if (GUILayout.Button("+ New Group", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                TextInputWindow.Show("New Group", "Group Name:", "", (newName) =>
                {
                    if (!string.IsNullOrWhiteSpace(newName))
                    {
                        _emptyGroups.Add(newName.Trim());
                        Repaint();
                    }
                });
            }
            EditorGUILayout.EndHorizontal();
        }

        // ─── PRESET LIST & GROUPS ────────────────────────────────────────────

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
                GUILayout.Label("No presets match.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // Global Headers
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Presets  ({filtered.Count})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Scene",  EditorStyles.miniLabel, GUILayout.Width(90));
            GUILayout.Label("Apply",  EditorStyles.miniLabel, GUILayout.Width(45));
            GUILayout.Label("Save",   EditorStyles.miniLabel, GUILayout.Width(26));
            GUILayout.Label("Del",    EditorStyles.miniLabel, GUILayout.Width(28));
            EditorGUILayout.EndHorizontal();

            Rect sep = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            EditorGUI.DrawRect(sep, new Color(0.4f, 0.4f, 0.4f, 0.5f));

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // Clear drop target handling outside of scroll rect
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                _selectedIndices.Clear();
                _lastSelectedIndex = -1;
                GUI.FocusControl(null);
                Repaint();
            }

            var grouped = filtered.GroupBy(p => string.IsNullOrEmpty(p.preset.group) ? "Ungrouped" : p.preset.group)
                                  .ToDictionary(g => g.Key, g => g.ToList());

            // Inject any empty groups created by the user this session
            foreach (var eg in _emptyGroups)
            {
                if (!grouped.ContainsKey(eg))
                    grouped[eg] = new List<(string scenePath, UIPreset preset)>();
            }

            var sortedGroups = grouped.OrderBy(g => g.Key == "Ungrouped" ? 1 : 0) // Ungrouped at bottom
                                      .ThenBy(g => g.Key)
                                      .ToList();

            int displayIndex = 0;
            List<int> visibleFlatIndices = new List<int>(); // For shift-click ranges

            foreach (var groupKV in sortedGroups)
            {
                string groupName = groupKV.Key;
                var groupItems = groupKV.Value;

                if (!_groupExpanded.ContainsKey(groupName)) _groupExpanded[groupName] = true;

                DrawGroupHeader(groupName);

                if (_groupExpanded[groupName])
                {
                    if (groupItems.Count == 0 && groupName != "Ungrouped")
                    {
                        GUI.Label(EditorGUILayout.GetControlRect(GUILayout.Height(20)), "   (Empty - drag presets here)", EditorStyles.centeredGreyMiniLabel);
                    }

                    foreach (var item in groupItems)
                    {
                        visibleFlatIndices.Add(GetStableIndex(item.preset, item.scenePath));
                        DrawPresetRow(displayIndex++, item.preset, item.scenePath, item.scenePath == activeScenePath, visibleFlatIndices);
                    }
                }
                GUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGroupHeader(string groupName)
        {
            Rect headerRect = EditorGUILayout.GetControlRect(GUILayout.Height(24));
            bool isHovered = headerRect.Contains(Event.current.mousePosition);
            
            EditorGUI.DrawRect(headerRect, isHovered ? new Color(0.25f, 0.25f, 0.25f, 0.8f) : new Color(0.18f, 0.18f, 0.18f, 0.8f));
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 1, headerRect.width, 1), new Color(0.1f, 0.1f, 0.1f, 0.5f));

            bool expanded = _groupExpanded[groupName];
            string foldoutStr = expanded ? "▼" : "▶";
            
            GUI.Label(new Rect(headerRect.x + 4, headerRect.y + 4, 16, 16), foldoutStr, EditorStyles.miniBoldLabel);
            GUI.Label(new Rect(headerRect.x + 20, headerRect.y + 4, headerRect.width - 100, 16), groupName, EditorStyles.boldLabel);

            // Quick actions on the right side
            if (groupName != "Ungrouped")
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                if (GUI.Button(new Rect(headerRect.xMax - 54, headerRect.y + 4, 24, 16), "✏", EditorStyles.miniButton))
                {
                    BeginRenameGroup(groupName);
                }
                GUI.backgroundColor = new Color(0.7f, 0.3f, 0.3f, 0.8f);
                if (GUI.Button(new Rect(headerRect.xMax - 28, headerRect.y + 4, 24, 16), "✕", EditorStyles.miniButton))
                {
                    DisbandGroup(groupName);
                }
                GUI.backgroundColor = prev;
            }

            // ── Drag & Drop Target for Groups ──
            if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
            {
                if (headerRect.Contains(Event.current.mousePosition) && DragAndDrop.GetGenericData("UIPresets") != null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    
                    if (Event.current.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        OnDropPresetsToGroup(groupName);
                        Event.current.Use();
                    }
                }
            }

            // ── Click to expand/collapse ──
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && headerRect.Contains(Event.current.mousePosition))
            {
                _groupExpanded[groupName] = !expanded;
                Event.current.Use();
            }

            // ── Context Menu ──
            if (Event.current.type == EventType.ContextClick && headerRect.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                if (groupName != "Ungrouped")
                {
                    menu.AddItem(new GUIContent("Rename Group"), false, () => BeginRenameGroup(groupName));
                    menu.AddItem(new GUIContent("Disband Group (Keep Presets)"), false, () => DisbandGroup(groupName));
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Rename Group"));
                }
                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        private void DrawPresetRow(int displayIndex, UIPreset preset, string scenePath, bool isCurrentScene, List<int> visibleFlatIndices)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(32));
            int stableIndex = GetStableIndex(preset, scenePath);
            bool isSelected = _selectedIndices.Contains(stableIndex);
            bool isHovered  = rowRect.Contains(Event.current.mousePosition);

            Color rowColor = isSelected ? ColRowSelected : (isHovered ? ColRowHover : (displayIndex % 2 == 0 ? ColRowEven : ColRowOdd));
            EditorGUI.DrawRect(rowRect, rowColor);

            if (isCurrentScene)
                EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, 3, rowRect.height), ColBadgeSelf);

            float x = rowRect.x + 8;
            float y = rowRect.y;

            // ── Inline rename ────────────────────────────────────────────────
            if (_renamingIndex == stableIndex)
            {
                GUI.SetNextControlName("RenameField");
                _renameBuffer = GUI.TextField(new Rect(x, y + 7, rowRect.width - 200, 18), _renameBuffer);

                if (GUI.GetNameOfFocusedControl() != "RenameField")
                    GUI.FocusControl("RenameField");

                bool confirm = (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return) ||
                               GUI.Button(new Rect(rowRect.xMax - 116, y + 6, 45, 20), "✓ OK", EditorStyles.miniButton);
                bool cancel  = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;

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
            bool applyReady = !_isBusy && (EditorApplication.timeSinceStartup - _lastApplyTime) > ApplyCooldown;
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = applyReady ? (isCurrentScene ? ColApplyBtn : ColBadgeOther) : ColBusy;
            GUI.enabled = applyReady;

            if (GUI.Button(new Rect(rowRect.xMax - 116, y + 6, 45, 20), "Apply", EditorStyles.miniButton))
            {
                OnApplyClicked(preset, scenePath, isCurrentScene);
            }

            GUI.enabled = true;

            // ── Overwrite Button ──────────────────────────────────────────────
            GUI.backgroundColor = applyReady && isCurrentScene ? new Color(0.2f, 0.45f, 0.75f) : ColBusy;
            GUI.enabled = applyReady && isCurrentScene;
            if (GUI.Button(new Rect(rowRect.xMax - 68, y + 6, 26, 20), "💾", EditorStyles.miniButton))
            {
                OnOverwriteClicked(preset, scenePath);
            }

            GUI.enabled = true;

            // ── Delete Button ─────────────────────────────────────────────────
            GUI.backgroundColor = ColDeleteBtn;
            if (GUI.Button(new Rect(rowRect.xMax - 38, y + 6, 28, 20), "✕", EditorStyles.miniButton))
            {
                OnDeleteClicked(preset, scenePath);
            }
            GUI.backgroundColor = prev;

            // ── Selection & Drag Handling ─────────────────────────────────────
            HandleItemInteractions(rowRect, stableIndex, preset, scenePath, isCurrentScene, visibleFlatIndices);
        }

        private void HandleItemInteractions(Rect rowRect, int stableIndex, UIPreset preset, string scenePath, bool isCurrentScene, List<int> visibleFlatIndices)
        {
            Event e = Event.current;

            // Context Menu
            if (e.type == EventType.ContextClick && rowRect.Contains(e.mousePosition))
            {
                if (!_selectedIndices.Contains(stableIndex))
                {
                    _selectedIndices.Clear();
                    _selectedIndices.Add(stableIndex);
                    _lastSelectedIndex = stableIndex;
                }
                ShowContextMenu(preset, scenePath, isCurrentScene);
                e.Use();
            }

            // Mouse Down (Selection)
            if (e.type == EventType.MouseDown && e.button == 0 && rowRect.Contains(e.mousePosition))
            {
                // Don't swallow the event if dragging starts, but handle selection immediately
                if (e.control || e.command)
                {
                    if (_selectedIndices.Contains(stableIndex)) _selectedIndices.Remove(stableIndex);
                    else _selectedIndices.Add(stableIndex);
                    _lastSelectedIndex = stableIndex;
                }
                else if (e.shift && _lastSelectedIndex != -1)
                {
                    int startIdx = visibleFlatIndices.IndexOf(_lastSelectedIndex);
                    int endIdx = visibleFlatIndices.IndexOf(stableIndex);
                    if (startIdx != -1 && endIdx != -1)
                    {
                        int min = Mathf.Min(startIdx, endIdx);
                        int max = Mathf.Max(startIdx, endIdx);
                        _selectedIndices.Clear();
                        for (int i = min; i <= max; i++) _selectedIndices.Add(visibleFlatIndices[i]);
                    }
                }
                else
                {
                    // If clicking a selected item, wait for mouse up to deselect others (allows dragging multiple)
                    if (!_selectedIndices.Contains(stableIndex))
                    {
                        _selectedIndices.Clear();
                        _selectedIndices.Add(stableIndex);
                        _lastSelectedIndex = stableIndex;
                    }
                }
                GUI.FocusControl(null);
                Repaint();
            }

            // Mouse Up (resolve single click on selected item without modifier)
            if (e.type == EventType.MouseUp && e.button == 0 && rowRect.Contains(e.mousePosition))
            {
                if (!e.control && !e.command && !e.shift && _selectedIndices.Contains(stableIndex))
                {
                    _selectedIndices.Clear();
                    _selectedIndices.Add(stableIndex);
                    _lastSelectedIndex = stableIndex;
                    Repaint();
                }
            }

            // Drag Start
            if (e.type == EventType.MouseDrag && e.button == 0 && rowRect.Contains(e.mousePosition) && _selectedIndices.Contains(stableIndex))
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData("UIPresets", _selectedIndices.ToList());
                DragAndDrop.paths = null;
                DragAndDrop.objectReferences = new UnityEngine.Object[0];
                DragAndDrop.StartDrag($"Move {_selectedIndices.Count} Presets");
                e.Use();
            }
        }

        private void MovePresetsToGroup(List<int> presetIds, string targetGroupName)
        {
            if (presetIds == null || presetIds.Count == 0) return;

            string finalGroup = targetGroupName == "Ungrouped" ? "" : targetGroupName;
            int movedCount = 0;

            // Need to map hashes back to actual objects and save per-scene
            var scenesToSave = new HashSet<string>();
            var scenesData = new Dictionary<string, UIPresetLibrary>();

            foreach (var (scenePath, preset) in _allPresets)
            {
                int hash = GetStableIndex(preset, scenePath);
                if (presetIds.Contains(hash))
                {
                    if (preset.group == finalGroup) continue; // no change

                    if (!scenesData.ContainsKey(scenePath))
                        scenesData[scenePath] = UIPresetStorage.Load(scenePath);

                    var targetPreset = scenesData[scenePath].presets.FirstOrDefault(p => p.name == preset.name);
                    if (targetPreset != null)
                    {
                        targetPreset.group = finalGroup;
                        scenesToSave.Add(scenePath);
                        movedCount++;
                    }
                }
            }

            foreach (string path in scenesToSave)
            {
                UIPresetStorage.Save(scenesData[path], path);
            }

            if (movedCount > 0)
            {
                RefreshAllPresets();
                string targetDisplay = string.IsNullOrEmpty(finalGroup) ? "Ungrouped" : $"'{finalGroup}'";
                ShowFeedback($"Moved {movedCount} preset(s) to {targetDisplay}.", MessageType.Info);
            }
        }

        private void OnDropPresetsToGroup(string targetGroupName)
        {
            var draggedIds = DragAndDrop.GetGenericData("UIPresets") as List<int>;
            MovePresetsToGroup(draggedIds, targetGroupName);
        }

        // ─── BOTTOM PANEL (Save + Screenshot) ────────────────────────────────

        private void DrawBottomPanel()
        {
            Rect panelRect = EditorGUILayout.GetControlRect(GUILayout.Height(54));
            EditorGUI.DrawRect(panelRect, new Color(0.12f, 0.15f, 0.20f, 0.85f));
            EditorGUI.DrawRect(new Rect(panelRect.x, panelRect.y, panelRect.width, 1), new Color(0.4f, 0.4f, 0.4f, 0.5f));

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

            bool savePressed = GUI.Button(new Rect(panelRect.xMax - 76, y + 4, 68, 22), "💾 Save") ||
                               (canSave && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "NewPresetName");

            if (savePressed) { OnSaveNewPreset(); Event.current.Use(); }

            GUI.enabled = true;
            GUI.backgroundColor = prev;

            string hint = _isBusy ? "Working…" : string.IsNullOrEmpty(SceneManager.GetActiveScene().path) ? "Save your scene first" : string.IsNullOrWhiteSpace(_newPresetName) ? "Enter a preset name" : $"Captures Canvas UI in '{SceneManager.GetActiveScene().name}'";
            GUI.Label(new Rect(x + 88, y + 26, panelRect.width - 100, 14), hint, EditorStyles.centeredGreyMiniLabel);

            // Screenshot strip
            Rect ssRect = EditorGUILayout.GetControlRect(GUILayout.Height(28));
            EditorGUI.DrawRect(ssRect, new Color(0.10f, 0.12f, 0.18f, 0.90f));
            EditorGUI.DrawRect(new Rect(ssRect.x, ssRect.y, ssRect.width, 1), new Color(0.3f, 0.3f, 0.3f, 0.5f));

            bool ssReady = !_isBusy && (EditorApplication.timeSinceStartup - _lastScreenshotTime) > ScreenshotCooldown;
            GUI.backgroundColor = ssReady ? ColScreenshot : ColBusy;
            GUI.enabled = ssReady;

            string ssLabel = _isBusy ? "Working…" : (EditorApplication.timeSinceStartup - _lastScreenshotTime) < ScreenshotCooldown ? $"📷 Screenshot (wait {ScreenshotCooldown - (EditorApplication.timeSinceStartup - _lastScreenshotTime):F0}s)" : "📷 Take Screenshot";

            if (GUI.Button(new Rect(ssRect.x + 8, ssRect.y + 4, 180, 20), ssLabel, EditorStyles.miniButton))
                OnTakeScreenshot();

            GUI.enabled = true;
            GUI.backgroundColor = prev;

            string screenshotFolder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");
            GUI.Label(new Rect(ssRect.x + 196, ssRect.y + 6, ssRect.width - 204, 16), "→ Screenshots/", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 0.6f, 0.8f) } });
        }

        // ─── FEEDBACK BAR ────────────────────────────────────────────────────

        private void DrawFeedback()
        {
            if (string.IsNullOrEmpty(_lastFeedback)) return;
            if (EditorApplication.timeSinceStartup - _feedbackTime > FeedbackDuration) { _lastFeedback = ""; return; }
            EditorGUILayout.HelpBox(_lastFeedback, _lastFeedbackType);
            Repaint();
        }

        private void ShowFeedback(string msg, MessageType type = MessageType.Info)
        {
            _lastFeedback = msg;
            _lastFeedbackType = type;
            _feedbackTime = EditorApplication.timeSinceStartup;
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
                bool duplicate = lib.presets.Any(p => string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase));

                if (duplicate)
                {
                    bool overwrite = EditorUtility.DisplayDialog("Preset Already Exists", $"A preset named '{name}' already exists for this scene.\nOverwrite it?", "Overwrite", "Cancel");
                    if (!overwrite) { _isBusy = false; return; }
                    lib.presets.RemoveAll(p => string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase));
                }

                UIPreset captured = UIPresetCapture.CaptureCurrentScene(name);
                
                // If a specific group is heavily selected, default new preset to that group
                string targetGroup = "";
                if (_selectedIndices.Count > 0)
                {
                    var selectedPresets = _allPresets.Where(p => _selectedIndices.Contains(GetStableIndex(p.preset, p.scenePath))).ToList();
                    targetGroup = selectedPresets.FirstOrDefault().preset.group; 
                }
                captured.group = targetGroup;

                lib.presets.Add(captured);
                UIPresetStorage.Save(lib, activeScene.path);

                _newPresetName = "";
                GUI.FocusControl(null);
                RefreshAllPresets();
                ShowFeedback($"✔ Preset '{name}' saved.", MessageType.Info);
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
                bool switchScene = EditorUtility.DisplayDialog("Different Scene", $"Preset '{preset.name}' belongs to scene '{preset.sceneName}'.\nSwitch to it?", "Switch & Apply", "Cancel");
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
                if (result.Skipped > 0) msg += $", {result.Skipped} skipped";
                ShowFeedback(msg, result.Skipped > 0 ? MessageType.Warning : MessageType.Info);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void OnOverwriteClicked(UIPreset preset, string scenePath)
        {
            if (_isBusy) return;
            if (!UIPresetCapture.IsEditorSafe()) return;

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != scenePath)
            {
                ShowFeedback($"Cannot overwrite preset for a different scene.", MessageType.Warning);
                return;
            }

            bool overwrite = EditorUtility.DisplayDialog("Overwrite Preset", $"Overwrite preset '{preset.name}' with the current scene layout?", "Overwrite", "Cancel");
            if (!overwrite) return;

            _isBusy = true;
            try
            {
                var lib = UIPresetStorage.Load(scenePath);
                
                UIPreset captured = UIPresetCapture.CaptureCurrentScene(preset.name);
                captured.group = preset.group;
                
                int index = lib.presets.FindIndex(p => string.Equals(p.name, preset.name, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    lib.presets[index] = captured;
                }
                else
                {
                    lib.presets.Add(captured);
                }
                
                UIPresetStorage.Save(lib, scenePath);
                RefreshAllPresets();
                ShowFeedback($"✔ Overwrote '{preset.name}'.", MessageType.Info);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void OnDeleteClicked(UIPreset preset, string scenePath)
        {
            if (_isBusy) return;
            bool confirm = EditorUtility.DisplayDialog("Delete Preset", $"Delete preset '{preset.name}'?\nThis cannot be undone.", "Delete", "Cancel");
            if (!confirm) return;

            var lib = UIPresetStorage.Load(scenePath);
            lib.presets.RemoveAll(p => p.name == preset.name && p.scenePath == preset.scenePath);
            UIPresetStorage.Save(lib, scenePath);
            RefreshAllPresets();
            ShowFeedback($"Preset '{preset.name}' deleted.", MessageType.Info);
        }

        private void OnDeleteSelected()
        {
            if (_selectedIndices.Count == 0) return;

            bool confirm = EditorUtility.DisplayDialog("Delete Presets", $"Delete {_selectedIndices.Count} selected presets?\nThis cannot be undone.", "Delete All", "Cancel");
            if (!confirm) return;

            var scenesToSave = new HashSet<string>();
            var scenesData = new Dictionary<string, UIPresetLibrary>();

            foreach (var (scenePath, preset) in _allPresets)
            {
                if (_selectedIndices.Contains(GetStableIndex(preset, scenePath)))
                {
                    if (!scenesData.ContainsKey(scenePath)) scenesData[scenePath] = UIPresetStorage.Load(scenePath);
                    scenesData[scenePath].presets.RemoveAll(p => p.name == preset.name);
                    scenesToSave.Add(scenePath);
                }
            }

            foreach (string path in scenesToSave) UIPresetStorage.Save(scenesData[path], path);
            _selectedIndices.Clear();
            RefreshAllPresets();
            ShowFeedback($"Deleted selected presets.", MessageType.Info);
        }

        private void OnTakeScreenshot()
        {
            if (_isBusy || !UIPresetCapture.IsEditorSafe()) return;
            _isBusy = true;
            _lastScreenshotTime = EditorApplication.timeSinceStartup;

            try
            {
                string screenshotFolder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");
                if (!Directory.Exists(screenshotFolder)) Directory.CreateDirectory(screenshotFolder);

                Scene scene = SceneManager.GetActiveScene();
                string fileName = $"{(string.IsNullOrEmpty(scene.name) ? "screenshot" : scene.name)}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.png";
                string fullPath = Path.Combine(screenshotFolder, fileName);

                ScreenCapture.CaptureScreenshot(fullPath, superSize: 1);
                EditorApplication.delayCall += () =>
                {
                    _isBusy = false;
                    ShowFeedback($"📷 Screenshot saved: Screenshots/{fileName}", MessageType.Info);
                    Repaint();
                };
            }
            catch (Exception e)
            {
                _isBusy = false;
                ShowFeedback($"Screenshot failed: {e.Message}", MessageType.Error);
            }
        }

        // ─── CONTEXT MENUS & GROUP ACTIONS ────────────────────────────────────

        private void ShowContextMenu(UIPreset preset, string scenePath, bool isCurrentScene)
        {
            var menu = new GenericMenu();

            if (_selectedIndices.Count > 1)
            {
                menu.AddItem(new GUIContent("Move to New Group..."), false, () => BeginMoveToNewGroup());
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Delete Selected"), false, OnDeleteSelected);
            }
            else
            {
                if (isCurrentScene)
                {
                    menu.AddItem(new GUIContent("Apply"), false, () => ApplyAndReport(preset));
                    menu.AddItem(new GUIContent("Rename"), false, () => { _renamingIndex = GetStableIndex(preset, scenePath); _renameBuffer = preset.name; });
                    menu.AddItem(new GUIContent("Duplicate"), false, () => DuplicatePreset(preset, scenePath));
                    menu.AddSeparator("");
                }
                else
                {
                    menu.AddItem(new GUIContent("Apply (Switch Scene)"), false, () => OnApplyClicked(preset, scenePath, false));
                    menu.AddSeparator("");
                }
                menu.AddItem(new GUIContent("Move to New Group..."), false, () => BeginMoveToNewGroup());
                menu.AddItem(new GUIContent("Delete"), false, () => OnDeleteClicked(preset, scenePath));
            }

            menu.ShowAsContext();
        }

        private void BeginMoveToNewGroup()
        {
            TextInputWindow.Show("New Group Name", "Group Name:", "", (newName) =>
            {
                if (!string.IsNullOrWhiteSpace(newName)) MovePresetsToGroup(_selectedIndices.ToList(), newName.Trim());
            });
        }

        private void BeginRenameGroup(string oldName)
        {
            TextInputWindow.Show("Rename Group", "New Group Name:", oldName, (newName) =>
            {
                if (!string.IsNullOrWhiteSpace(newName) && newName != oldName)
                {
                    string trimmed = newName.Trim();
                    
                    // Rename in session memory if it was an empty group
                    if (_emptyGroups.Contains(oldName))
                    {
                        _emptyGroups.Remove(oldName);
                        _emptyGroups.Add(trimmed);
                    }

                    // Move everything from oldName to newName in JSON
                    var scenesToSave = new HashSet<string>();
                    var scenesData = new Dictionary<string, UIPresetLibrary>();

                    foreach (var (scenePath, preset) in _allPresets)
                    {
                        if (preset.group == oldName)
                        {
                            if (!scenesData.ContainsKey(scenePath)) scenesData[scenePath] = UIPresetStorage.Load(scenePath);
                            var p = scenesData[scenePath].presets.FirstOrDefault(x => x.name == preset.name);
                            if (p != null) { p.group = trimmed; scenesToSave.Add(scenePath); }
                        }
                    }
                    foreach (string path in scenesToSave) UIPresetStorage.Save(scenesData[path], path);
                    RefreshAllPresets();
                }
            });
        }

        private void DisbandGroup(string groupName)
        {
            if (EditorUtility.DisplayDialog("Delete Group", $"Are you sure you want to delete the group '{groupName}'?\n\n(Any presets inside will NOT be deleted, they will just move to Ungrouped).", "Delete Group", "Cancel"))
            {
                _emptyGroups.Remove(groupName);

                var scenesToSave = new HashSet<string>();
                var scenesData = new Dictionary<string, UIPresetLibrary>();

                foreach (var (scenePath, preset) in _allPresets)
                {
                    if (preset.group == groupName)
                    {
                        if (!scenesData.ContainsKey(scenePath)) scenesData[scenePath] = UIPresetStorage.Load(scenePath);
                        var p = scenesData[scenePath].presets.FirstOrDefault(x => x.name == preset.name);
                        if (p != null) { p.group = ""; scenesToSave.Add(scenePath); }
                    }
                }
                foreach (string path in scenesToSave) UIPresetStorage.Save(scenesData[path], path);
                RefreshAllPresets();
            }
        }

        private void CommitRename(UIPreset preset, string scenePath, string newName)
        {
            newName = newName.Trim();
            if (string.IsNullOrEmpty(newName) || newName == preset.name) return;

            var lib = UIPresetStorage.Load(scenePath);
            var target = lib.presets.FirstOrDefault(p => p.name == preset.name);
            if (target == null) return;

            target.name = newName;
            UIPresetStorage.Save(lib, scenePath);
            RefreshAllPresets();
            ShowFeedback($"Preset renamed to '{newName}'.", MessageType.Info);
        }

        private void DuplicatePreset(UIPreset preset, string scenePath)
        {
            var lib = UIPresetStorage.Load(scenePath);
            string copyName = preset.name + "_copy";
            int n = 1;
            while (lib.presets.Any(p => p.name == copyName)) copyName = preset.name + "_copy" + n++;

            lib.presets.Add(new UIPreset
            {
                name = copyName, scenePath = preset.scenePath, sceneName = preset.sceneName,
                createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), group = preset.group,
                entries = preset.entries.Select(e => new UIObjectState { guid = e.guid, hierarchyPath = e.hierarchyPath, isActive = e.isActive }).ToList()
            });

            UIPresetStorage.Save(lib, scenePath);
            RefreshAllPresets();
            ShowFeedback($"Duplicated as '{copyName}'.", MessageType.Info);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UTILITIES & STYLES
        // ─────────────────────────────────────────────────────────────────────

        private static int GetStableIndex(UIPreset preset, string scenePath) => (scenePath + "::" + preset.name).GetHashCode();

        private void EnsureStyles()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, normal = { textColor = ColAccent } };
            _presetNameStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, fontSize = 12, normal = { textColor = Color.white } };
            _badgeCurrentStyle = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = ColBadgeSelf } };
            _badgeOtherStyle = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = ColBadgeOther } };
            _playModeStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, normal = { textColor = new Color(1f, 0.7f, 0.7f) } };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  POPUP WINDOW FOR TEXT INPUT
    // ─────────────────────────────────────────────────────────────────────────
    public class TextInputWindow : EditorWindow
    {
        private string _prompt;
        private string _text;
        private Action<string> _onConfirm;

        public static void Show(string title, string prompt, string defaultText, Action<string> onConfirm)
        {
            var win = CreateInstance<TextInputWindow>();
            win.titleContent = new GUIContent(title);
            win._prompt = prompt;
            win._text = defaultText;
            win._onConfirm = onConfirm;
            win.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 100);
            win.ShowUtility();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label(_prompt, EditorStyles.boldLabel);
            GUI.SetNextControlName("InputField");
            _text = EditorGUILayout.TextField(_text);
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel")) Close();
            if (GUILayout.Button("OK") || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
            {
                _onConfirm?.Invoke(_text);
                Close();
            }
            GUILayout.EndHorizontal();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) Close();

            if (Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl("InputField");
            }
        }
    }
}
