using UnityEngine;
using UnityEditor;
using MaouSamaTD.Levels;
using System.Collections.Generic;
using System.Linq;

namespace MaouSamaTD.Editor
{
    public class MapDataBrowser : EditorWindow
    {
        public enum TabType { MainStory, ResourceDungeon, SpecialDungeons, All }
        public enum BrowserMode { Gallery, Editor }
        
        private BrowserMode _mode = BrowserMode.Gallery;
        private TabType _currentTab = TabType.All;
        private List<MapData> _allMapData = new List<MapData>();
        private List<MapData> _filteredMapData = new List<MapData>();
        private string _searchText = "";
        
        private Vector2 _galleryScroll;
        private Vector2 _detailScroll;
        
        private MapData _selectedMapData;
        private UnityEditor.Editor _currentMapDataEditor;
        
        private GUIStyle _headerStyle;
        private GUIStyle _cardStyle;

        private float _cardWidth = 220f;
        private float _cardHeight = 240f;

        [MenuItem("Maou-TD/Map Data Browser", false, 10)]
        public static void Open()
        {
            var window = GetWindow<MapDataBrowser>("Map Data Browser");
            window.minSize = new Vector2(800f, 600f);
            window.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            RefreshData();
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            if (_currentMapDataEditor != null)
            {
                DestroyImmediate(_currentMapDataEditor);
            }
        }

        private void OnUndoRedoPerformed()
        {
            Repaint();
        }

        public void RefreshData()
        {
            _allMapData.Clear();
            string[] guids = AssetDatabase.FindAssets("t:MapData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MapData mapData = AssetDatabase.LoadAssetAtPath<MapData>(path);
                if (mapData != null) _allMapData.Add(mapData);
            }
            
            _allMapData.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            _filteredMapData.Clear();
            foreach (var mapData in _allMapData)
            {
                if (mapData == null) continue;
                
                if (_currentTab == TabType.MainStory && mapData.Category != LevelCategory.MainStory) continue;
                if (_currentTab == TabType.ResourceDungeon && mapData.Category != LevelCategory.ResourceDungeon) continue;
                if (_currentTab == TabType.SpecialDungeons && mapData.Category != LevelCategory.RiteDungeon && mapData.Category != LevelCategory.VassalDungeon) continue;

                if (!string.IsNullOrEmpty(_searchText))
                {
                    string search = _searchText.ToLower();
                    bool match = mapData.name.ToLower().Contains(search);
                    if (!match) continue;
                }

                _filteredMapData.Add(mapData);
            }
        }

        private void InitializeStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                    margin = new RectOffset(4, 4, 4, 4)
                };
            }

            if (_cardStyle == null)
            {
                _cardStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(10, 10, 10, 10)
                };
            }
        }

        private void OnGUI()
        {
            InitializeStyles();

            if (_mode == BrowserMode.Gallery)
            {
                DrawGalleryTopToolbar();
                DrawGalleryView();
            }
            else
            {
                DrawEditorView();
            }
        }

        private void DrawGalleryTopToolbar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label("Map Gallery", EditorStyles.boldLabel, GUILayout.Width(100));
            
            // Tab Buttons
            Color originalColor = GUI.color;
            for (int i = 0; i < 4; i++)
            {
                TabType tab = (TabType)i;
                string label = tab.ToString();
                if (tab == TabType.MainStory) label = "Story";
                else if (tab == TabType.ResourceDungeon) label = "Resource";
                else if (tab == TabType.SpecialDungeons) label = "Special";
                else if (tab == TabType.All) label = "All";

                bool isActive = _currentTab == tab;
                GUI.color = isActive ? new Color(0.35f, 0.7f, 1f) : Color.white;
                if (GUILayout.Button(label, EditorStyles.miniButtonMid, GUILayout.Height(24), GUILayout.Width(80)))
                {
                    _currentTab = tab;
                    ApplyFilter();
                }
            }
            GUI.color = originalColor;

            GUILayout.Space(20);
            
            // Search box
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

            GUILayout.FlexibleSpace();

            Color originalBtnColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (GUILayout.Button("+ Create New MapData", GUILayout.Height(24), GUILayout.Width(150)))
            {
                CreateNewMapDataAsset();
            }
            GUI.backgroundColor = originalBtnColor;

            if (GUILayout.Button("Force Refresh", GUILayout.Height(24), GUILayout.Width(100)))
            {
                RefreshData();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawGalleryView()
        {
            _galleryScroll = EditorGUILayout.BeginScrollView(_galleryScroll);
            
            float windowWidth = EditorGUIUtility.currentViewWidth - 20f;
            int columns = Mathf.Max(1, Mathf.FloorToInt(windowWidth / (_cardWidth + 20f)));
            
            int currentIndex = 0;
            
            EditorGUILayout.BeginVertical();
            
            while (currentIndex < _filteredMapData.Count)
            {
                EditorGUILayout.BeginHorizontal();
                
                for (int col = 0; col < columns; col++)
                {
                    if (currentIndex >= _filteredMapData.Count)
                    {
                        GUILayout.FlexibleSpace();
                        continue;
                    }

                    MapData map = _filteredMapData[currentIndex];
                    DrawMapCard(map);
                    currentIndex++;
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawMapCard(MapData map)
        {
            Rect cardRect = EditorGUILayout.BeginVertical(_cardStyle, GUILayout.Width(_cardWidth), GUILayout.Height(_cardHeight));
            
            // Hover effect and Click
            EditorGUIUtility.AddCursorRect(cardRect, MouseCursor.Link);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && cardRect.Contains(Event.current.mousePosition))
            {
                SelectMapData(map);
                Event.current.Use();
            }
            
            GUILayout.Label(map.name, EditorStyles.boldLabel);
            GUILayout.Label($"{map.Width} x {map.Height} | {map.Category}", EditorStyles.miniLabel);
            
            GUILayout.Space(5);
            
            // Draw Mini-Grid
            Rect gridRect = GUILayoutUtility.GetRect(_cardWidth - 20, _cardHeight - 60);
            DrawMiniMapGrid(gridRect, map);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawMiniMapGrid(Rect rect, MapData map)
        {
            GUI.Box(rect, "", GUI.skin.box);
            
            if (map.Width <= 0 || map.Height <= 0)
            {
                GUI.Label(rect, "Invalid Dimensions", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (!map.UseManualLayout)
            {
                GUI.Label(rect, "Procedural", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // Build a dictionary for fast lookup
            Dictionary<Vector2Int, TileType> layout = new Dictionary<Vector2Int, TileType>();
            if (map.ManualLayoutData != null)
            {
                foreach (var tile in map.ManualLayoutData)
                {
                    layout[tile.Coordinate] = tile.Type;
                }
            }

            // Calculate cell size
            float padding = 4f;
            float availableWidth = rect.width - (padding * 2);
            float availableHeight = rect.height - (padding * 2);
            
            float cellWidth = availableWidth / map.Width;
            float cellHeight = availableHeight / map.Height;
            float cellSize = Mathf.Min(cellWidth, cellHeight);
            
            // Center the grid in the rect
            float gridTotalWidth = cellSize * map.Width;
            float gridTotalHeight = cellSize * map.Height;
            
            float startX = rect.x + (rect.width - gridTotalWidth) / 2f;
            float startY = rect.y + (rect.height - gridTotalHeight) / 2f;

            // Draw tiles (Y goes from Height-1 down to 0 to match standard visual grids)
            for (int y = map.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    Vector2Int coord = new Vector2Int(x, y);
                    TileType type = layout.ContainsKey(coord) ? layout[coord] : TileType.None;
                    
                    Rect cellRect = new Rect(
                        startX + (x * cellSize),
                        startY + ((map.Height - 1 - y) * cellSize),
                        cellSize,
                        cellSize
                    );

                    // Draw cell with 1px margin
                    Rect innerCellRect = new Rect(cellRect.x + 0.5f, cellRect.y + 0.5f, cellRect.width - 1f, cellRect.height - 1f);
                    Color cellColor = GetTileColor(type);
                    
                    EditorGUI.DrawRect(innerCellRect, cellColor);
                }
            }
        }

        private Color GetTileColor(TileType type)
        {
            switch (type)
            {
                case TileType.SpawnPoint: return Color.red;
                case TileType.ExitPoint: return Color.green;
                case TileType.Walkable: return Color.white;
                case TileType.HighGround: return Color.gray;
                case TileType.DecoHighGround: return new Color(0.3f, 0.3f, 0.3f);
                case TileType.None: return new Color(0.1f, 0.1f, 0.1f);
                case TileType.LowTile: return new Color(0.8f, 0.6f, 0.4f);
                case TileType.NonWalkableDecor: return new Color(0.5f, 0.2f, 0.5f);
                case TileType.Wall: return new Color(0.2f, 0.2f, 0.6f);
                case TileType.SpawnPointHigh: return new Color(1f, 0.4f, 0.4f);
                case TileType.ExitPointHigh: return new Color(0.4f, 1f, 1f);
                default: return Color.black;
            }
        }

        private void SelectMapData(MapData mapData)
        {
            _selectedMapData = mapData;
            _mode = BrowserMode.Editor;
            GUI.FocusControl(null);
            
            if (_currentMapDataEditor != null)
            {
                DestroyImmediate(_currentMapDataEditor);
            }
            
            if (_selectedMapData != null)
            {
                _currentMapDataEditor = UnityEditor.Editor.CreateEditor(_selectedMapData);
            }
        }

        private void DrawEditorView()
        {
            if (_selectedMapData == null)
            {
                _mode = BrowserMode.Gallery;
                return;
            }

            // Top Navigation Bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("🔙 Back to Gallery", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                _mode = BrowserMode.Gallery;
                if (_currentMapDataEditor != null)
                {
                    DestroyImmediate(_currentMapDataEditor);
                    _currentMapDataEditor = null;
                }
                return;
            }

            GUILayout.Space(20);

            if (GUILayout.Button("◀ Prev", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                int currentIdx = _filteredMapData.IndexOf(_selectedMapData);
                int nextIdx = (currentIdx - 1 + _filteredMapData.Count) % _filteredMapData.Count;
                SelectMapData(_filteredMapData[nextIdx]);
            }

            GUILayout.FlexibleSpace();
            
            GUILayout.Label($"Editing: {_selectedMapData.name} ({_selectedMapData.Category})", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Ping in Project", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                EditorGUIUtility.PingObject(_selectedMapData);
            }

            if (GUILayout.Button("Next ▶", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                int currentIdx = _filteredMapData.IndexOf(_selectedMapData);
                int nextIdx = (currentIdx + 1) % _filteredMapData.Count;
                SelectMapData(_filteredMapData[nextIdx]);
            }

            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);

            if (_currentMapDataEditor != null)
            {
                EditorGUI.BeginChangeCheck();
                _currentMapDataEditor.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_selectedMapData);
                }
            }
        }

        private void CreateNewMapDataAsset()
        {
            string defaultPath = "Assets/_Game/Data/Maps";
            if (!AssetDatabase.IsValidFolder(defaultPath))
            {
                defaultPath = "Assets";
            }

            string path = EditorUtility.SaveFilePanelInProject("Create New MapData", "NewMapData.asset", "asset", "Choose where to save the new MapData", defaultPath);
            if (string.IsNullOrEmpty(path)) return;

            MapData newAsset = ScriptableObject.CreateInstance<MapData>();
            AssetDatabase.CreateAsset(newAsset, path);
            AssetDatabase.SaveAssets();

            RefreshData();
            SelectMapData(newAsset);
            EditorGUIUtility.PingObject(newAsset);
        }
    }
}
