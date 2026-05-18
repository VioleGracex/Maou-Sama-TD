using UnityEngine;
using UnityEditor;
using MaouSamaTD.Levels;
using MaouSamaTD.UI.MainMenu;
using System.Collections.Generic;
using System.Linq;

namespace MaouSamaTD.Editor
{
    public class CampaignMapBrowser : EditorWindow
    {
        public enum TabType { MainStory, ResourceDungeon, SpecialDungeons, All }

        private TabType _currentTab = TabType.MainStory;
        private List<LevelData> _allLevels = new List<LevelData>();
        private List<LevelData> _filteredLevels = new List<LevelData>();
        private string _searchText = "";
        private Vector2 _sidebarScroll;
        private Vector2 _detailScroll;

        private float _leftSidebarWidth = 320f;
        private float _rightSidebarWidth = 350f;
        private bool _isResizingLeft = false;
        private bool _isResizingRight = false;
        
        private LevelData _selectedLevel;
        private Texture2D _mapTexture;
        private Sprite _mapSprite;
        
        // Interactive map options
        private bool _lockPositions = false;
        private bool _showNames = true;
        private bool _showPaths = true;
        private float _nodeScale = 1.0f;
        
        // Zoom and Pan settings
        private float _zoomLevel = 1.0f;
        private Vector2 _panOffset = Vector2.zero;
        private bool _isPanning = false;
        private Vector2 _panStartMouse;
        private Vector2 _panStartOffset;
        private Rect _lastRectArea;
        
        // Drag and Drop Placement state
        private bool _isDraggingNode = false;
        private LevelData _draggingLevel = null;
        private Vector2 _dragOffset;
        
        // Link Mode State
        private bool _linkMode = false;
        private LevelData _linkSourceNode = null;
        
        // Style variables
        private GUIStyle _headerStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _selectedCardStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _badgeStyle;

        [MenuItem("Maou-TD/Campaign Map Browser")]
        public static void Open()
        {
            var window = GetWindow<CampaignMapBrowser>("Campaign Map Browser");
            window.minSize = new Vector2(1000f, 650f);
            window.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            RefreshData();
            LoadMapTexture();
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            RefreshData();
            Repaint();
        }

        private void LoadMapTexture()
        {
            _mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Gehenna.png");
            if (_mapSprite != null)
            {
                _mapTexture = _mapSprite.texture;
            }
            else
            {
                _mapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Gehenna.png");
            }
        }

        public void RefreshData()
        {
            _allLevels.Clear();
            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level != null) _allLevels.Add(level);
            }
            
            _allLevels = _allLevels.OrderBy(l => l.Category).ThenBy(l => l.LevelIndex).ThenBy(l => l.LevelID).ToList();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            _filteredLevels.Clear();
            foreach (var level in _allLevels)
            {
                if (level == null) continue;
                
                if (_currentTab == TabType.MainStory && level.Category != LevelCategory.MainStory) continue;
                if (_currentTab == TabType.ResourceDungeon && level.Category != LevelCategory.ResourceDungeon) continue;
                if (_currentTab == TabType.SpecialDungeons && level.Category != LevelCategory.RiteDungeon && level.Category != LevelCategory.VassalDungeon) continue;

                if (!string.IsNullOrEmpty(_searchText))
                {
                    string search = _searchText.ToLower();
                    bool match = (level.LevelName != null && level.LevelName.ToLower().Contains(search)) ||
                                 (level.LevelID != null && level.LevelID.ToLower().Contains(search)) ||
                                 level.LevelIndex.ToString().Contains(search);
                    if (!match) continue;
                }

                _filteredLevels.Add(level);
            }
        }

        private void InitializeStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                margin = new RectOffset(4, 4, 4, 4)
            };

            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(4, 4, 4, 4)
            };

            _selectedCardStyle = new GUIStyle(_cardStyle);
            _selectedCardStyle.normal.background = CreateSolidTexture(2, 2, new Color(0.18f, 0.35f, 0.5f, 0.8f));

            _titleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white, background = CreateSolidTexture(2, 2, new Color(0.2f, 0.2f, 0.2f)) },
                padding = new RectOffset(4, 4, 2, 2)
            };
        }

        private Texture2D CreateSolidTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        private void OnGUI()
        {
            InitializeStyles();

            DrawTopToolbar();

            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8); // Left padding from window edge

            // Left Sidebar: separate level/dungeon categories
            DrawLeftSidebar();

            DrawResizeHandleLeft();

            // Middle: visual workspace map canvas
            DrawMiddleMapWorkspace();

            DrawResizeHandleRight();

            // Right Sidebar: selected level inspector Details panel
            DrawRightDetailsPanel();

            GUILayout.Space(8); // Right padding from window edge
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8); // Bottom padding
        }

        private void DrawResizeHandleLeft()
        {
            Rect handleRect = GUILayoutUtility.GetRect(6f, position.height, GUILayout.ExpandHeight(true), GUILayout.Width(6f));
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);

            Color handleColor = _isResizingLeft ? new Color(0.1f, 0.8f, 1.0f, 0.8f) : new Color(0.2f, 0.2f, 0.2f, 0.4f);
            Color originalGUIColor = GUI.color;
            GUI.color = handleColor;
            GUI.DrawTexture(new Rect(handleRect.x + 2, handleRect.y, 2, handleRect.height), EditorGUIUtility.whiteTexture);
            GUI.color = originalGUIColor;

            if (Event.current.type == EventType.MouseDown && handleRect.Contains(Event.current.mousePosition))
            {
                _isResizingLeft = true;
                Event.current.Use();
            }

            if (_isResizingLeft)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    _leftSidebarWidth += Event.current.delta.x;
                    _leftSidebarWidth = Mathf.Clamp(_leftSidebarWidth, 200f, 500f);
                    Repaint();
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    _isResizingLeft = false;
                    Event.current.Use();
                }
            }
        }

        private void DrawResizeHandleRight()
        {
            Rect handleRect = GUILayoutUtility.GetRect(6f, position.height, GUILayout.ExpandHeight(true), GUILayout.Width(6f));
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);

            Color handleColor = _isResizingRight ? new Color(0.1f, 0.8f, 1.0f, 0.8f) : new Color(0.2f, 0.2f, 0.2f, 0.4f);
            Color originalGUIColor = GUI.color;
            GUI.color = handleColor;
            GUI.DrawTexture(new Rect(handleRect.x + 2, handleRect.y, 2, handleRect.height), EditorGUIUtility.whiteTexture);
            GUI.color = originalGUIColor;

            if (Event.current.type == EventType.MouseDown && handleRect.Contains(Event.current.mousePosition))
            {
                _isResizingRight = true;
                Event.current.Use();
            }

            if (_isResizingRight)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    _rightSidebarWidth -= Event.current.delta.x;
                    _rightSidebarWidth = Mathf.Clamp(_rightSidebarWidth, 250f, 600f);
                    Repaint();
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    _isResizingRight = false;
                    Event.current.Use();
                }
            }
        }

        private void DrawTopToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Label("Gehenna Map Editor Browser", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Force Refresh Database", EditorStyles.toolbarButton, GUILayout.Width(150)))
            {
                RefreshData();
                LoadMapTexture();
            }

            if (GUILayout.Button("Sync & Update Game Scene", EditorStyles.toolbarButton, GUILayout.Width(170)))
            {
                var scenePage = FindObjectOfType<CampaignPage>();
                if (scenePage != null)
                {
                    scenePage.Refresh();
                    EditorUtility.SetDirty(scenePage);
                    var activeScene = scenePage.gameObject.scene;
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
                    ShowNotification(new GUIContent("Map visual nodes synced & saved to Scene!"));
                }
                else
                {
                    ShowNotification(new GUIContent("No CampaignPage active in current scene. Open 'Home_New' scene."));
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftSidebar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_leftSidebarWidth), GUILayout.ExpandHeight(true));
            
            // Tab Buttons
            EditorGUILayout.BeginHorizontal();
            Color originalColor = GUI.color;
            
            for (int i = 0; i < 4; i++)
            {
                TabType tab = (TabType)i;
                string label = tab.ToString();
                if (tab == TabType.MainStory) label = "Main Story";
                else if (tab == TabType.ResourceDungeon) label = "Resource";
                else if (tab == TabType.SpecialDungeons) label = "Special/Rites";
                else if (tab == TabType.All) label = "All";

                bool isActive = _currentTab == tab;
                GUI.color = isActive ? new Color(0.35f, 0.7f, 1f) : Color.white;
                if (GUILayout.Button(label, EditorStyles.miniButtonMid, GUILayout.Height(28)))
                {
                    _currentTab = tab;
                    ApplyFilter();
                }
            }
            GUI.color = originalColor;
            EditorGUILayout.EndHorizontal();

            // Search box
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("🔍", GUILayout.Width(20));
            string newSearch = EditorGUILayout.TextField(_searchText, GUILayout.ExpandWidth(true));
            if (newSearch != _searchText)
            {
                _searchText = newSearch;
                ApplyFilter();
            }
            if (!string.IsNullOrEmpty(_searchText))
            {
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    _searchText = "";
                    ApplyFilter();
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Box("DRAG LEVEL TO MAP WORKSPACE", EditorStyles.centeredGreyMiniLabel);

            // Scrollable Category list
            _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll, EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6); // Inner left margin
            EditorGUILayout.BeginVertical();
            
            if (_filteredLevels.Count == 0)
            {
                EditorGUILayout.HelpBox("No levels found for the selected category filter.", MessageType.Info);
            }
            else
            {
                foreach (var level in _filteredLevels)
                {
                    if (level == null) continue;
                    
                    bool isSelected = _selectedLevel == level;
                    GUIStyle style = isSelected ? _selectedCardStyle : _cardStyle;
                    
                    EditorGUILayout.BeginVertical(style);
                    EditorGUILayout.BeginHorizontal();
                    
                    Color indicatorColor = GetCategoryColor(level.Category);
                    Rect r = GUILayoutUtility.GetRect(12, 12);
                    r.y += 2;
                    Handles.BeginGUI();
                    Handles.color = indicatorColor;
                    Handles.DrawSolidDisc(new Vector3(r.x + 6, r.y + 6, 0), Vector3.forward, 5f);
                    Handles.EndGUI();
                    
                    GUILayout.Space(4);
                    
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"[{level.LevelID}] {level.LevelName}", _titleStyle, GUILayout.ExpandWidth(true));
                    
                    // Placed/Unplaced Icon Mark
                    bool isPlaced = IsLevelPlaced(level);
                    GUI.color = isPlaced ? new Color(0.4f, 1.0f, 0.4f) : new Color(0.9f, 0.5f, 0.5f);
                    GUILayout.Label(isPlaced ? "📍 Placed" : "⚠️ Unplaced", EditorStyles.miniLabel, GUILayout.Width(70));
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                    
                    GUILayout.Label($"Index: {level.LevelIndex} | Pos: ({Mathf.RoundToInt(level.CampaignMapPosition.x)}, {Mathf.RoundToInt(level.CampaignMapPosition.y)})", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.EndHorizontal();
                    
                    var lastRect = GUILayoutUtility.GetLastRect();
                    
                    // Support drag-and-drop from the sidebar list onto the map workspace
                    HandleSidebarDragStart(level, lastRect);
                    
                    // Mouse Down Selection
                    if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
                    {
                        _selectedLevel = level;
                        GUI.FocusControl(null);
                        
                        // Detect Double Click (button 0 is left click)
                        if (Event.current.clickCount == 2 && Event.current.button == 0)
                        {
                            if (IsLevelPlaced(level))
                            {
                                FocusOnLevel(level);
                            }
                            else
                            {
                                ShowNotification(new GUIContent($"Level [{level.LevelID}] is not placed. Drag it to the map first."));
                            }
                        }
                        
                        Event.current.Use();
                    }
                    
                    EditorGUILayout.EndVertical();
                }
            }
            
            EditorGUILayout.EndVertical();
            GUILayout.Space(6); // Inner right margin
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (GUILayout.Button("+ Create New Level SO", GUILayout.Height(32)))
            {
                CreateNewLevelAsset();
            }
            GUI.backgroundColor = originalColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void HandleSidebarDragStart(LevelData level, Rect rect)
        {
            var currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDrag && rect.Contains(currentEvent.mousePosition))
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new UnityEngine.Object[] { level };
                DragAndDrop.StartDrag($"Place Level: {level.LevelName}");
                currentEvent.Use();
            }
        }

        private void DrawMiddleMapWorkspace()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            // Header controls
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Interactive Map Workspace", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();

            // Link Mode Connect toggle
            GUI.backgroundColor = _linkMode ? new Color(1.0f, 0.35f, 0.2f) : Color.white;
            if (GUILayout.Button(_linkMode ? "🔗 Link Mode (ACTIVE - Click Node A then B)" : "🔗 Interactive Link Mode", EditorStyles.toolbarButton))
            {
                _linkMode = !_linkMode;
                _linkSourceNode = null;
                if (_linkMode)
                {
                    _lockPositions = true; // Lock position movements while connecting
                }
            }
            GUI.backgroundColor = Color.white;
            
            _lockPositions = GUILayout.Toggle(_lockPositions, "🔒 Lock Coordinates", EditorStyles.toolbarButton);
            _showNames = GUILayout.Toggle(_showNames, "🏷️ Show Names", EditorStyles.toolbarButton);
            _showPaths = GUILayout.Toggle(_showPaths, "🔗 Show Path Connections", EditorStyles.toolbarButton);
            
            if (GUILayout.Button("Reset View", EditorStyles.toolbarButton))
            {
                _zoomLevel = 1.0f;
                _panOffset = Vector2.zero;
            }
            EditorGUILayout.EndHorizontal();

            // Canvas Workspace Area
            Rect rectArea = GUILayoutUtility.GetRect(300, 300, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _lastRectArea = rectArea;
            GUI.Box(rectArea, "");

            // Handle middle-click or right-click panning and scroll zoom in the workspace rect
            HandleZoomAndPan(rectArea);

            if (_mapTexture != null)
            {
                // Calculate basic map viewport aspect rect
                float mapAspect = 2048f / 1143f;
                float areaAspect = rectArea.width / rectArea.height;
                Rect mapBaseRect;
                
                if (areaAspect > mapAspect)
                {
                    float width = rectArea.height * mapAspect;
                    mapBaseRect = new Rect(rectArea.x + (rectArea.width - width) / 2f, rectArea.y, width, rectArea.height);
                }
                else
                {
                    float height = rectArea.width / mapAspect;
                    mapBaseRect = new Rect(rectArea.x, rectArea.y + (rectArea.height - height) / 2f, rectArea.width, height);
                }

                // Apply zoom and panning offsets to calculate actual rendering rect
                Rect mapRenderRect = new Rect(
                    (mapBaseRect.x - rectArea.center.x) * _zoomLevel + rectArea.center.x + _panOffset.x,
                    (mapBaseRect.y - rectArea.center.y) * _zoomLevel + rectArea.center.y + _panOffset.y,
                    mapBaseRect.width * _zoomLevel,
                    mapBaseRect.height * _zoomLevel
                );

                // Draw map texture clipped/scrolled inside rectArea
                GUI.BeginGroup(rectArea);
                
                // Draw texture relative to the Group container (offset local)
                Rect localMapRenderRect = new Rect(
                    mapRenderRect.x - rectArea.x,
                    mapRenderRect.y - rectArea.y,
                    mapRenderRect.width,
                    mapRenderRect.height
                );
                
                GUI.DrawTexture(localMapRenderRect, _mapTexture, ScaleMode.StretchToFill);

                // Handle visual connection dragging and rendering
                if (_showPaths)
                {
                    DrawConnectionPaths(localMapRenderRect);
                }

                // Draw interactive, draggable level nodes
                DrawLevelNodes(localMapRenderRect, rectArea);

                // Draw Link Mode help text overlay
                if (_linkMode)
                {
                    Rect helpRect = new Rect(10, 10, 450, 40);
                    GUI.backgroundColor = new Color(0, 0, 0, 0.75f);
                    GUI.Box(helpRect, "");
                    string helpText = _linkSourceNode == null 
                        ? "🔗 Link Mode: Click on the starting level node." 
                        : $"🔗 Link Mode: Click on target node to connect/disconnect [{_linkSourceNode.LevelID}] or Right-Click to cancel.";
                    GUI.Label(new Rect(15, 12, 440, 36), helpText, EditorStyles.boldLabel);
                    GUI.backgroundColor = Color.white;
                }

                // Handle Double Clicks on local map
                HandleDoubleClicksOnMap(localMapRenderRect);

                // Handle incoming Drag & Drop operations from the lists
                HandleIncomingListDrag(rectArea, localMapRenderRect);

                GUI.EndGroup();
            }
            else
            {
                var cent = rectArea.center;
                GUI.color = Color.red;
                GUI.Label(new Rect(cent.x - 200, cent.y - 20, 400, 40), "Gehenna Map asset not found at 'Assets/_Game/Art/Gehenna.png'!", _headerStyle);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndVertical();
        }

        private void HandleZoomAndPan(Rect rectArea)
        {
            var currentEvent = Event.current;
            Vector2 mousePos = currentEvent.mousePosition;

            if (!rectArea.Contains(mousePos)) return;

            // Scroll zoom
            if (currentEvent.type == EventType.ScrollWheel)
            {
                float oldZoom = _zoomLevel;
                _zoomLevel = Mathf.Clamp(_zoomLevel - currentEvent.delta.y * 0.05f, 0.25f, 4.0f);
                
                // Adjust pan offset to zoom centered on mouse
                Vector2 localMouse = mousePos - rectArea.center;
                _panOffset -= localMouse * (_zoomLevel / oldZoom - 1f);
                
                currentEvent.Use();
                Repaint();
            }

            // Right-Click or Middle-Click Panning
            if (currentEvent.type == EventType.MouseDown && (currentEvent.button == 1 || currentEvent.button == 2))
            {
                // In Link Mode, Right-Click cancels the active connection link source node
                if (_linkMode && currentEvent.button == 1)
                {
                    if (_linkSourceNode != null)
                    {
                        _linkSourceNode = null;
                        ShowNotification(new GUIContent("Link selection cleared"));
                        currentEvent.Use();
                        Repaint();
                        return;
                    }
                }

                _isPanning = true;
                _panStartMouse = mousePos;
                _panStartOffset = _panOffset;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && _isPanning)
            {
                _panOffset = _panStartOffset + (mousePos - _panStartMouse);
                currentEvent.Use();
                Repaint();
            }
            else if (currentEvent.type == EventType.MouseUp && _isPanning)
            {
                _isPanning = false;
                currentEvent.Use();
            }
        }

        private void HandleIncomingListDrag(Rect globalRectArea, Rect localMapRenderRect)
        {
            var currentEvent = Event.current;
            // Since we are inside GUI.Group, mouse positions are local. Adjust accordingly.
            Vector2 localMouse = currentEvent.mousePosition;

            if (currentEvent.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is LevelData droppedLevel)
                    {
                        Undo.RecordObject(droppedLevel, "Place Level on Map Workspace");
                        
                        Vector2 campCoords = GuiToMapPosition(localMouse, localMapRenderRect);
                        campCoords.x = Mathf.Clamp(Mathf.Round(campCoords.x), 0f, 2048f);
                        campCoords.y = Mathf.Clamp(Mathf.Round(campCoords.y), 0f, 1143f);
                        
                        droppedLevel.CampaignMapPosition = campCoords;
                        EditorUtility.SetDirty(droppedLevel);
                        
                        _selectedLevel = droppedLevel;
                        ShowNotification(new GUIContent($"Placed [{droppedLevel.LevelID}] on Map!"));
                    }
                }
                currentEvent.Use();
                Repaint();
            }
        }

        private void DrawRightDetailsPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_rightSidebarWidth), GUILayout.ExpandHeight(true));
            
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 110f; // Premium layout spacing
            
            GUILayout.Label("Level Details & Setup", _headerStyle);
            
            if (_selectedLevel == null)
            {
                EditorGUILayout.HelpBox("Select a level from the sidebar list, drag one onto the map, or double-click the map workspace background to configure.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6); // Inner left margin
            EditorGUILayout.BeginVertical();

            Undo.RecordObject(_selectedLevel, "Edit Level Details");

            EditorGUI.BeginChangeCheck();

            // Core Identity
            EditorGUILayout.LabelField("IDENTIFICATION", EditorStyles.boldLabel);
            _selectedLevel.LevelID = EditorGUILayout.TextField("Level ID", _selectedLevel.LevelID);
            _selectedLevel.LevelName = EditorGUILayout.TextField("Level Name", _selectedLevel.LevelName);
            _selectedLevel.LevelIndex = EditorGUILayout.IntField("Level Index", _selectedLevel.LevelIndex);
            _selectedLevel.Category = (LevelCategory)EditorGUILayout.EnumPopup("Category", _selectedLevel.Category);
            _selectedLevel.LevelIcon = (Sprite)EditorGUILayout.ObjectField("Level Icon", _selectedLevel.LevelIcon, typeof(Sprite), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("COORDINATES ON MAP (2048 x 1143)", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            float originalSubLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 50f;
            float posX = EditorGUILayout.FloatField("Pos X", _selectedLevel.CampaignMapPosition.x);
            float posY = EditorGUILayout.FloatField("Pos Y", _selectedLevel.CampaignMapPosition.y);
            EditorGUIUtility.labelWidth = originalSubLabelWidth;
            EditorGUILayout.EndHorizontal();

            posX = Mathf.Clamp(Mathf.Round(posX), 0f, 2048f);
            posY = Mathf.Clamp(Mathf.Round(posY), 0f, 1143f);
            _selectedLevel.CampaignMapPosition = new Vector2(posX, posY);

            if (GUILayout.Button("Center (1024, 571)"))
            {
                Undo.RecordObject(_selectedLevel, "Center Level Position");
                _selectedLevel.CampaignMapPosition = new Vector2(1024f, 571f);
                EditorUtility.SetDirty(_selectedLevel);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("MAP CONNECTIONS / FLOW PATHS", EditorStyles.boldLabel);

            if (_selectedLevel.ConnectedLevels == null)
            {
                _selectedLevel.ConnectedLevels = new List<LevelData>();
            }

            for (int i = _selectedLevel.ConnectedLevels.Count - 1; i >= 0; i--)
            {
                var conn = _selectedLevel.ConnectedLevels[i];
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                string label = conn != null ? $"[{conn.LevelID}] {conn.LevelName}" : "None (Missing Reference)";
                GUILayout.Label(label, EditorStyles.label);
                
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Disconnect", GUILayout.Width(90)))
                {
                    Undo.RecordObject(_selectedLevel, "Disconnect Level Connection");
                    _selectedLevel.ConnectedLevels.RemoveAt(i);
                    EditorUtility.SetDirty(_selectedLevel);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            List<LevelData> connectable = _allLevels.Where(l => l != _selectedLevel && !_selectedLevel.ConnectedLevels.Contains(l)).ToList();
            if (connectable.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                
                string[] options = connectable.Select(l => $"[{l.Category}] {l.LevelID} - {l.LevelName}").ToArray();
                int selectedIndex = EditorGUILayout.Popup(-1, options, GUILayout.ExpandWidth(true));
                if (selectedIndex >= 0)
                {
                    var target = connectable[selectedIndex];
                    Undo.RecordObject(_selectedLevel, "Add Level Connection");
                    _selectedLevel.ConnectedLevels.Add(target);
                    EditorUtility.SetDirty(_selectedLevel);
                }
                
                EditorGUILayout.EndHorizontal();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_selectedLevel);
            }

            EditorGUILayout.Space(20);
            
            GUI.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
            if (GUILayout.Button("🗑️ Delete Level Data SO Asset", GUILayout.Height(32)))
            {
                if (EditorUtility.DisplayDialog("Delete Level Data SO?", $"Are you sure you want to permanently delete {_selectedLevel.name}.asset?", "Delete", "Cancel"))
                {
                    string path = AssetDatabase.GetAssetPath(_selectedLevel);
                    
                    // Remove from LevelDatabase
                    var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/_Game/Data/Levels/LevelDatabase.asset");
                    if (db != null && db.AllLevels != null)
                    {
                        Undo.RecordObject(db, "Remove Level From Database");
                        db.AllLevels.Remove(_selectedLevel);
                        EditorUtility.SetDirty(db);
                    }
                    
                    // Remove from other levels' connections
                    foreach (var other in _allLevels)
                    {
                        if (other != null && other.ConnectedLevels != null && other.ConnectedLevels.Contains(_selectedLevel))
                        {
                            Undo.RecordObject(other, "Remove Connection to Deleted Level");
                            other.ConnectedLevels.Remove(_selectedLevel);
                            EditorUtility.SetDirty(other);
                        }
                    }
                    
                    AssetDatabase.DeleteAsset(path);
                    _selectedLevel = null;
                    RefreshData();
                    AssetDatabase.SaveAssets();
                    GUIUtility.ExitGUI();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
            GUILayout.Space(6); // Inner right margin
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            
            EditorGUIUtility.labelWidth = originalLabelWidth; // Restore global label width!
            EditorGUILayout.EndVertical();
        }

        private void DrawConnectionPaths(Rect localMapRenderRect)
        {
            Handles.BeginGUI();
            var drawnConnections = new HashSet<(string, string)>();

            foreach (var level in _allLevels)
            {
                if (level == null || level.ConnectedLevels == null || level.ConnectedLevels.Count == 0) continue;
                
                Vector2 fromGui = MapToGuiPosition(level.CampaignMapPosition, localMapRenderRect);
                Color pathColor = GetCategoryColor(level.Category);
                pathColor.a = 0.85f; // High-contrast, premium glowing look!
                
                foreach (var target in level.ConnectedLevels)
                {
                    if (target == null) continue;
                    
                    Vector2 toGui = MapToGuiPosition(target.CampaignMapPosition, localMapRenderRect);
                    
                    string idA = level.LevelID;
                    string idB = target.LevelID;
                    var key = string.Compare(idA, idB, System.StringComparison.Ordinal) < 0 ? (idA, idB) : (idB, idA);

                    if (drawnConnections.Contains(key)) continue;
                    drawnConnections.Add(key);

                    // Perpendicular vector to create a beautiful curved arc (consistent with CampaignPage)
                    Vector2 dir = toGui - fromGui;
                    Vector2 perp = new Vector2(-dir.y, dir.x).normalized;
                    
                    float dist = dir.magnitude;
                    float arcFactor = dist * 0.12f; 
                    Vector2 control = (fromGui + toGui) * 0.5f + perp * arcFactor;

                    // Generate curved segments
                    int segments = Mathf.Max(8, Mathf.RoundToInt(dist / 15f));
                    Vector3[] points = new Vector3[segments + 1];
                    for (int i = 0; i <= segments; i++)
                    {
                        float t = i / (float)segments;
                        Vector2 pos = (1f - t) * (1f - t) * fromGui + 2f * (1f - t) * t * control + t * t * toGui;
                        points[i] = new Vector3(pos.x, pos.y, 0f);
                    }

                    // Render gorgeous curved path line shadow/glow
                    Handles.color = new Color(pathColor.r, pathColor.g, pathColor.b, 0.3f);
                    Handles.DrawAAPolyLine(6f * _nodeScale * _zoomLevel, points);
                    
                    Handles.color = pathColor;
                    Handles.DrawAAPolyLine(3f * _nodeScale * _zoomLevel, points);

                    // Flow direction indicator
                    Vector2 mid = (0.25f) * fromGui + 0.5f * control + 0.25f * toGui; // Curve midpoint
                    Vector2 tangent = 2f * (1f - 0.5f) * (control - fromGui) + 2f * 0.5f * (toGui - control);
                    Vector2 flowDir = tangent.normalized;
                    Vector2 flowPerp = new Vector2(-flowDir.y, flowDir.x);
                    
                    Vector2 arrowA = mid - flowDir * 6f * _zoomLevel + flowPerp * 5f * _zoomLevel;
                    Vector2 arrowB = mid - flowDir * 6f * _zoomLevel - flowPerp * 5f * _zoomLevel;
                    
                    Handles.color = Color.white;
                    Handles.DrawAAPolyLine(2f * _nodeScale * _zoomLevel, arrowA, mid, arrowB);
                }
            }
            Handles.EndGUI();
        }

        private void DrawLevelNodes(Rect localMapRenderRect, Rect globalRectArea)
        {
            var currentEvent = Event.current;
            Vector2 localMouse = currentEvent.mousePosition;

            Handles.BeginGUI();
            
            // Mouse Clicks / Drag states detection
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                bool hitNode = false;
                for (int i = _allLevels.Count - 1; i >= 0; i--)
                {
                    var level = _allLevels[i];
                    if (level == null) continue;
                    
                    Vector2 guiPos = MapToGuiPosition(level.CampaignMapPosition, localMapRenderRect);
                    float clickRadius = 14f * _nodeScale * _zoomLevel;
                    
                    if (Vector2.Distance(localMouse, guiPos) <= clickRadius)
                    {
                        hitNode = true;
                        
                        if (_linkMode)
                        {
                            // Connections editor: interactive Link Mode clicking
                            if (_linkSourceNode == null)
                            {
                                _linkSourceNode = level;
                                ShowNotification(new GUIContent($"Link starting from [{level.LevelID}]"));
                            }
                            else if (_linkSourceNode == level)
                            {
                                _linkSourceNode = null;
                                ShowNotification(new GUIContent("Link mode selection cleared"));
                            }
                            else
                            {
                                // Toggle connection
                                Undo.RecordObject(_linkSourceNode, "Toggle Map Link Path");
                                if (_linkSourceNode.ConnectedLevels.Contains(level))
                                {
                                    _linkSourceNode.ConnectedLevels.Remove(level);
                                    ShowNotification(new GUIContent($"Disconnected [{_linkSourceNode.LevelID}] -> [{level.LevelID}]"));
                                }
                                else
                                {
                                    _linkSourceNode.ConnectedLevels.Add(level);
                                    ShowNotification(new GUIContent($"Connected [{_linkSourceNode.LevelID}] -> [{level.LevelID}]"));
                                }
                                EditorUtility.SetDirty(_linkSourceNode);
                                
                                // Daisy-chain connection target as next source!
                                _linkSourceNode = level;
                            }
                        }
                        else
                        {
                            // Standard node selection & dragging
                            _selectedLevel = level;
                            if (!_lockPositions)
                            {
                                _isDraggingNode = true;
                                _draggingLevel = level;
                                _dragOffset = level.CampaignMapPosition - GuiToMapPosition(localMouse, localMapRenderRect);
                            }
                        }
                        
                        currentEvent.Use();
                        break;
                    }
                }

                // Clicked background: cancel active link source node
                if (!hitNode && _linkMode && _linkSourceNode != null)
                {
                    _linkSourceNode = null;
                    ShowNotification(new GUIContent("Link selection cleared"));
                    currentEvent.Use();
                }
            }

            // Continuous Drag Translation movement on map
            if (_isDraggingNode && _draggingLevel != null)
            {
                if (currentEvent.type == EventType.MouseDrag)
                {
                    Undo.RecordObject(_draggingLevel, "Drag Level Node");
                    Vector2 newCampPos = GuiToMapPosition(localMouse, localMapRenderRect) + _dragOffset;
                    
                    newCampPos.x = Mathf.Clamp(Mathf.Round(newCampPos.x), 0f, 2048f);
                    newCampPos.y = Mathf.Clamp(Mathf.Round(newCampPos.y), 0f, 1143f);
                    
                    _draggingLevel.CampaignMapPosition = newCampPos;
                    EditorUtility.SetDirty(_draggingLevel);
                    currentEvent.Use();
                    Repaint();
                }
                else if (currentEvent.type == EventType.MouseUp)
                {
                    _isDraggingNode = false;
                    _draggingLevel = null;
                    currentEvent.Use();
                    Repaint();
                }
            }

            // Draw level node bubbles
            foreach (var level in _allLevels)
            {
                if (level == null) continue;

                Vector2 guiPos = MapToGuiPosition(level.CampaignMapPosition, localMapRenderRect);
                bool isSelected = _selectedLevel == level;
                bool isLinkSource = _linkSourceNode == level;
                Color nodeColor = GetCategoryColor(level.Category);
                
                // Ring Selection Glow
                if (isSelected)
                {
                    Handles.color = new Color(0.2f, 0.8f, 1.0f, 0.45f);
                    Handles.DrawSolidDisc(guiPos, Vector3.forward, 15f * _nodeScale * _zoomLevel);
                    Handles.color = new Color(0.2f, 0.8f, 1.0f, 0.9f);
                    Handles.DrawWireDisc(guiPos, Vector3.forward, 15f * _nodeScale * _zoomLevel);
                }
                else if (isLinkSource)
                {
                    // Green link mode source indicator
                    Handles.color = new Color(0.2f, 1.0f, 0.4f, 0.45f);
                    Handles.DrawSolidDisc(guiPos, Vector3.forward, 16f * _nodeScale * _zoomLevel);
                    Handles.color = new Color(0.2f, 1.0f, 0.4f, 0.9f);
                    Handles.DrawWireDisc(guiPos, Vector3.forward, 16f * _nodeScale * _zoomLevel);
                }

                // Core Node Disc
                Handles.color = Color.white;
                Handles.DrawSolidDisc(guiPos, Vector3.forward, 10f * _nodeScale * _zoomLevel);
                Handles.color = nodeColor;
                Handles.DrawSolidDisc(guiPos, Vector3.forward, 8f * _nodeScale * _zoomLevel);

                // Label text ID inside the bubble
                GUIStyle nodeLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
                
                Vector2 idRectSize = new Vector2(28f, 16f) * _nodeScale * _zoomLevel;
                Rect idRect = new Rect(guiPos.x - idRectSize.x/2f, guiPos.y - idRectSize.y/2f, idRectSize.x, idRectSize.y);
                
                if (_showNames)
                {
                    GUIStyle nameStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = isSelected ? new Color(0.2f, 0.8f, 1.0f) : Color.white }
                    };
                    
                    Rect nameRect = new Rect(guiPos.x - 75f, guiPos.y + 11f * _nodeScale * _zoomLevel, 150f, 16f);
                    GUI.Label(nameRect, level.LevelName, nameStyle);
                }

                GUI.Label(idRect, level.LevelID, nodeLabelStyle);
            }
            
            Handles.EndGUI();
        }

        private void HandleDoubleClicksOnMap(Rect localMapRenderRect)
        {
            var currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && currentEvent.clickCount == 2 && currentEvent.button == 0)
            {
                Vector2 localMouse = currentEvent.mousePosition;
                if (localMapRenderRect.Contains(localMouse))
                {
                    Vector2 newCampPos = GuiToMapPosition(localMouse, localMapRenderRect);
                    newCampPos.x = Mathf.Clamp(Mathf.Round(newCampPos.x), 0f, 2048f);
                    newCampPos.y = Mathf.Clamp(Mathf.Round(newCampPos.y), 0f, 1143f);

                    CreateNewLevelAsset(newCampPos);
                    currentEvent.Use();
                }
            }
        }

        private void CreateNewLevelAsset(Vector2? position = null)
        {
            Vector2 initialPos = position ?? new Vector2(1024f, 571f);

            LevelCategory selectedCategory = LevelCategory.MainStory;
            string idPrefix = "1-";
            string folderName = "MainStory";

            if (_currentTab == TabType.ResourceDungeon)
            {
                selectedCategory = LevelCategory.ResourceDungeon;
                idPrefix = "R-";
                folderName = "ResourceDungeons";
            }
            else if (_currentTab == TabType.SpecialDungeons)
            {
                selectedCategory = LevelCategory.RiteDungeon;
                idPrefix = "S-";
                folderName = "SpecialDungeons";
            }

            int count = _allLevels.Count(l => l.Category == selectedCategory) + 1;
            string targetID = $"{idPrefix}{count}";
            string targetName = $"New Level {count}";

            string directory = $"Assets/_Game/Data/Levels/{folderName}";
            if (!AssetDatabase.IsValidFolder(directory))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Game/Data"))
                    AssetDatabase.CreateFolder("Assets/_Game", "Data");
                if (!AssetDatabase.IsValidFolder("Assets/_Game/Data/Levels"))
                    AssetDatabase.CreateFolder("Assets/_Game/Data", "Levels");
                
                AssetDatabase.CreateFolder("Assets/_Game/Data/Levels", folderName);
            }

            string assetPath = $"{directory}/LevelData_{idPrefix}{count}.asset";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            LevelData newLevel = ScriptableObject.CreateInstance<LevelData>();
            newLevel.Category = selectedCategory;
            newLevel.LevelID = targetID;
            newLevel.LevelName = targetName;
            newLevel.LevelIndex = _allLevels.Count + 1;
            newLevel.CampaignMapPosition = initialPos;

            AssetDatabase.CreateAsset(newLevel, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshData();
            
            _selectedLevel = _allLevels.FirstOrDefault(l => l.LevelID == targetID && l.Category == selectedCategory) ?? newLevel;
            ShowNotification(new GUIContent($"Created Level {targetID} Scriptable Object!"));
        }

        private bool IsLevelPlaced(LevelData level)
        {
            if (level == null) return false;
            // Let's assume a level is not placed if its coordinates are Vector2.zero
            return level.CampaignMapPosition != Vector2.zero;
        }

        private void FocusOnLevel(LevelData level)
        {
            if (level == null) return;
            
            // Set a nice zoom level so it is centered and zoomed in
            _zoomLevel = Mathf.Clamp(_zoomLevel, 1.5f, 2.5f);
            
            if (_lastRectArea.width <= 0 || _lastRectArea.height <= 0) return;
            
            float mapAspect = 2048f / 1143f;
            float areaAspect = _lastRectArea.width / _lastRectArea.height;
            Rect mapBaseRect;
            
            if (areaAspect > mapAspect)
            {
                float width = _lastRectArea.height * mapAspect;
                mapBaseRect = new Rect(_lastRectArea.x + (_lastRectArea.width - width) / 2f, _lastRectArea.y, width, _lastRectArea.height);
            }
            else
            {
                float height = _lastRectArea.width / mapAspect;
                mapBaseRect = new Rect(_lastRectArea.x, _lastRectArea.y + (_lastRectArea.height - height) / 2f, _lastRectArea.width, height);
            }
            
            float pctX = level.CampaignMapPosition.x / 2048f;
            float pctY = level.CampaignMapPosition.y / 1143f;
            
            _panOffset.x = -(mapBaseRect.x + pctX * mapBaseRect.width - _lastRectArea.center.x) * _zoomLevel;
            _panOffset.y = -(mapBaseRect.y + (1f - pctY) * mapBaseRect.height - _lastRectArea.center.y) * _zoomLevel;
            
            _selectedLevel = level;
            Repaint();
            ShowNotification(new GUIContent($"Focused on level [{level.LevelID}]"));
        }

        private Vector2 MapToGuiPosition(Vector2 campPos, Rect localMapRenderRect)
        {
            float pctX = campPos.x / 2048f;
            float pctY = campPos.y / 1143f;
            
            float x = localMapRenderRect.x + pctX * localMapRenderRect.width;
            float y = localMapRenderRect.y + (1f - pctY) * localMapRenderRect.height;
            return new Vector2(x, y);
        }

        private Vector2 GuiToMapPosition(Vector2 guiPos, Rect localMapRenderRect)
        {
            float pctX = (guiPos.x - localMapRenderRect.x) / localMapRenderRect.width;
            float pctY = 1f - (guiPos.y - localMapRenderRect.y) / localMapRenderRect.height;
            
            float campX = pctX * 2048f;
            float campY = pctY * 1143f;
            return new Vector2(campX, campY);
        }

        private Color GetCategoryColor(LevelCategory category)
        {
            switch (category)
            {
                case LevelCategory.MainStory:
                    return new Color(0.2f, 0.8f, 1.0f); // Cyan
                case LevelCategory.ResourceDungeon:
                    return new Color(1.0f, 0.73f, 0.2f); // Amber / Gold
                case LevelCategory.RiteDungeon:
                    return new Color(0.85f, 0.35f, 1.0f); // Purple
                case LevelCategory.VassalDungeon:
                    return new Color(1.0f, 0.35f, 0.35f); // Red / Coral
                default:
                    return Color.white;
            }
        }
    }
}