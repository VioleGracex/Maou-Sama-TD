using UnityEngine;
using UnityEditor;
using MaouSamaTD.Units;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MaouSamaTD.Editor
{
    public class MaouEnemyBrowser : EditorWindow
    {
        public enum ViewMode { List, Grid }

        private List<EnemyData> _allEnemies = new List<EnemyData>();
        private List<EnemyData> _filteredEnemies = new List<EnemyData>();
        private string _searchText = "";
        private int _movementFilter = -1; // -1 for All
        
        // Layout & Paging
        private ViewMode _currentViewMode = ViewMode.Grid;
        private int _itemsPerPage = 24;
        private int _currentPage = 0;
        
        // Selection State
        private EnemyData _selectedEnemy;
        private bool _showDetails = true;
        private Vector2 _scrollPos;
        private Vector2 _detailScrollPos;
        private Vector2 _infoScrollPos;
        
        public enum ThumbnailType { Chibi, Portrait, Splash }
        public enum SortMode { Name, HP, Speed }
        
        private ThumbnailType _thumbnailType = ThumbnailType.Chibi;
        private SortMode _sortMode = SortMode.Name;
        
        private float _browserWidth = 450f;
        private bool _isResizingDetails = false;
        
        // Zoom & Settings
        private float _zoomFactor = 1.0f;
        private float _previewScale = 0.4f;
        private bool _sortAscending = true;
        
        private Texture2D _tempPreview;
        private GUIStyle _cardStyle;
        private GUIStyle _selectionStyle;

        [MenuItem("Maou-TD/Enemy Browser")]
        public static void Open()
        {
            GetWindow<MaouEnemyBrowser>("Enemy Browser");
        }

        private void OnEnable()
        {
            LoadSettings();
            RefreshData();
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        private void LoadSettings()
        {
            _searchText = EditorPrefs.GetString("MaouEnemyBrowser_SearchText", "");
            _movementFilter = EditorPrefs.GetInt("MaouEnemyBrowser_MovementFilter", -1);
            _currentViewMode = (ViewMode)EditorPrefs.GetInt("MaouEnemyBrowser_ViewMode", (int)ViewMode.Grid);
            _itemsPerPage = EditorPrefs.GetInt("MaouEnemyBrowser_ItemsPerPage", 24);
            _currentPage = EditorPrefs.GetInt("MaouEnemyBrowser_CurrentPage", 0);
            _showDetails = EditorPrefs.GetBool("MaouEnemyBrowser_ShowDetails", true);
            _thumbnailType = (ThumbnailType)EditorPrefs.GetInt("MaouEnemyBrowser_ThumbnailType", (int)ThumbnailType.Chibi);
            _sortMode = (SortMode)EditorPrefs.GetInt("MaouEnemyBrowser_SortMode", (int)SortMode.Name);
            _browserWidth = EditorPrefs.GetFloat("MaouEnemyBrowser_BrowserWidth", 450f);
            _zoomFactor = EditorPrefs.GetFloat("MaouEnemyBrowser_ZoomFactor", 1.0f);
            _previewScale = EditorPrefs.GetFloat("MaouEnemyBrowser_PreviewScale", 0.4f);
            _sortAscending = EditorPrefs.GetBool("MaouEnemyBrowser_SortAscending", true);
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString("MaouEnemyBrowser_SearchText", _searchText);
            EditorPrefs.SetInt("MaouEnemyBrowser_MovementFilter", _movementFilter);
            EditorPrefs.SetInt("MaouEnemyBrowser_ViewMode", (int)_currentViewMode);
            EditorPrefs.SetInt("MaouEnemyBrowser_ItemsPerPage", _itemsPerPage);
            EditorPrefs.SetInt("MaouEnemyBrowser_CurrentPage", _currentPage);
            EditorPrefs.SetBool("MaouEnemyBrowser_ShowDetails", _showDetails);
            EditorPrefs.SetInt("MaouEnemyBrowser_ThumbnailType", (int)_thumbnailType);
            EditorPrefs.SetInt("MaouEnemyBrowser_SortMode", (int)_sortMode);
            EditorPrefs.SetFloat("MaouEnemyBrowser_BrowserWidth", _browserWidth);
            EditorPrefs.SetFloat("MaouEnemyBrowser_ZoomFactor", _zoomFactor);
            EditorPrefs.SetFloat("MaouEnemyBrowser_PreviewScale", _previewScale);
            EditorPrefs.SetBool("MaouEnemyBrowser_SortAscending", _sortAscending);
        }

        private void RefreshData()
        {
            _allEnemies.Clear();
            string[] guids = AssetDatabase.FindAssets("t:EnemyData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if (enemy != null) _allEnemies.Add(enemy);
            }
            _allEnemies = _allEnemies.OrderBy(e => e.EnemyName).ToList();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            _filteredEnemies = _allEnemies.Where(e => 
            {
                // Search filter
                bool matchesSearch = string.IsNullOrEmpty(_searchText) || 
                                     e.EnemyName.ToLower().Contains(_searchText.ToLower());
                
                // Movement filter
                bool matchesMovement = _movementFilter == -1 || (int)e.MovementType == _movementFilter;
                
                return matchesSearch && matchesMovement;
            }).ToList();

            ApplySort();
            _currentPage = 0;
        }

        private void ApplySort()
        {
            switch (_sortMode)
            {
                case SortMode.Name:
                    _filteredEnemies = _sortAscending ? _filteredEnemies.OrderBy(e => e.EnemyName).ToList() : _filteredEnemies.OrderByDescending(e => e.EnemyName).ToList();
                    break;
                case SortMode.HP:
                    _filteredEnemies = _sortAscending ? _filteredEnemies.OrderBy(e => e.MaxHp).ThenBy(e => e.EnemyName).ToList() : _filteredEnemies.OrderByDescending(e => e.MaxHp).ThenBy(e => e.EnemyName).ToList();
                    break;
                case SortMode.Speed:
                    _filteredEnemies = _sortAscending ? _filteredEnemies.OrderBy(e => e.MoveSpeed).ThenBy(e => e.EnemyName).ToList() : _filteredEnemies.OrderByDescending(e => e.MoveSpeed).ThenBy(e => e.EnemyName).ToList();
                    break;
            }
        }

        private void DrawSplitter()
        {
            Rect splitterRect = new Rect(_browserWidth, 20, 10, position.height - 20);
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            
            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                _isResizingDetails = true;
                Event.current.Use();
            }
            if (_isResizingDetails)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    _browserWidth = Event.current.mousePosition.x;
                    _browserWidth = Mathf.Clamp(_browserWidth, 200, position.width - 250);
                    Repaint();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    _isResizingDetails = false;
                    Event.current.Use();
                }
            }
        }

        private void OnGUI()
        {
            InitializeStyles();

            DrawToolbar();
            HandleGlobalInput();

            EditorGUILayout.BeginHorizontal();

            // --- Sidebar / Browser (Left) ---
            DrawBrowserArea();

            // --- Details (Right) ---
            if (_showDetails)
            {
                DrawSplitter();
                DrawDetailsArea();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void HandleGlobalInput()
        {
            Event e = Event.current;
            if (e.type == EventType.ScrollWheel && (e.control || e.command))
            {
                float delta = -e.delta.y * 0.05f;
                _zoomFactor = Mathf.Clamp(_zoomFactor + delta, 0.5f, 2.5f);
                e.Use();
                Repaint();
            }
        }

        private void InitializeStyles()
        {
            if (_cardStyle == null)
            {
                _cardStyle = new GUIStyle(GUI.skin.box);
                _cardStyle.padding = new RectOffset(5, 5, 5, 5);
                _cardStyle.alignment = TextAnchor.LowerCenter;
            }

            if (_selectionStyle == null)
            {
                _selectionStyle = new GUIStyle("selectionRect");
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button(new GUIContent(" Refresh", EditorGUIUtility.IconContent("d_Refresh").image), EditorStyles.toolbarButton))
            {
                RefreshData();
            }

            GUILayout.Space(10);
            
            string newSearch = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.Width(180));
            if (newSearch != _searchText)
            {
                _searchText = newSearch;
                ApplyFilter();
            }

            GUILayout.Space(10);

            GUILayout.Label("Move:", GUILayout.Width(40));
            string[] moveOptions = new string[] { "All", "Ground", "Flying", "Mixed" };
            int newMove = EditorGUILayout.Popup(_movementFilter + 1, moveOptions, GUILayout.Width(70)) - 1;
            if (newMove != _movementFilter)
            {
                _movementFilter = newMove;
                ApplyFilter();
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label("Sort:", GUILayout.Width(32));
            SortMode newSort = (SortMode)EditorGUILayout.EnumPopup(_sortMode, GUILayout.Width(65));
            if (newSort != _sortMode)
            {
                _sortMode = newSort;
                ApplySort();
            }
            
            if (GUILayout.Button(EditorGUIUtility.IconContent(_sortAscending ? "d_ViewToolMove" : "d_ViewToolMove"), EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                _sortAscending = !_sortAscending;
                ApplySort();
            }
            
            GUILayout.Space(10);
            
            ThumbnailType newThumbType = (ThumbnailType)EditorGUILayout.EnumPopup(_thumbnailType, GUILayout.Width(80));
            if (newThumbType != _thumbnailType)
            {
                _thumbnailType = newThumbType;
                if (_selectedEnemy != null) SelectEnemy(_selectedEnemy);
            }

            // View Mode Toggles
            bool isList = _currentViewMode == ViewMode.List;
            if (GUILayout.Toggle(isList, EditorGUIUtility.IconContent("d_RectTransform Icon"), EditorStyles.toolbarButton, GUILayout.Width(35))) 
            {
                if (_currentViewMode != ViewMode.List)
                {
                    _currentViewMode = ViewMode.List;
                    _browserWidth = 280f;
                }
            }
            
            bool isGrid = _currentViewMode == ViewMode.Grid;
            if (GUILayout.Toggle(isGrid, EditorGUIUtility.IconContent("d_LayoutElement Icon"), EditorStyles.toolbarButton, GUILayout.Width(35))) 
            {
                if (_currentViewMode != ViewMode.Grid)
                {
                    _currentViewMode = ViewMode.Grid;
                    _browserWidth = 450f;
                }
            }

            GUILayout.Space(10);
            _showDetails = GUILayout.Toggle(_showDetails, _showDetails ? "Hide Details" : "Show Details", EditorStyles.toolbarButton, GUILayout.Width(100));

            GUILayout.Space(10);
            GUILayout.Label("Preview:", EditorStyles.miniLabel);
            _previewScale = GUILayout.HorizontalSlider(_previewScale, 0.1f, 0.8f, GUILayout.Width(60));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBrowserArea()
        {
            float width = _showDetails ? _browserWidth : position.width;
            
            if (_showDetails)
                EditorGUILayout.BeginVertical(GUILayout.Width(width));
            else
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
            
            if (_currentViewMode == ViewMode.List)
                DrawListView();
            else
                DrawGridView(width);
            
            EditorGUILayout.EndScrollView();
            DrawPagination();
            EditorGUILayout.EndVertical();
        }

        private void DrawListView()
        {
            int startIdx = _currentPage * _itemsPerPage;
            int endIdx = Mathf.Min(startIdx + _itemsPerPage, _filteredEnemies.Count);

            for (int i = startIdx; i < endIdx; i++)
            {
                EnemyData enemy = _filteredEnemies[i];
                bool isSelected = _selectedEnemy == enemy;
                
                Rect rect = EditorGUILayout.BeginHorizontal(isSelected ? _selectionStyle : GUIStyle.none, GUILayout.Height(45 * _zoomFactor));
                GUILayout.Space(10 * _zoomFactor);
                
                float thumbSize = 35 * _zoomFactor;
                Rect iconRect = GUILayoutUtility.GetRect(thumbSize, thumbSize);
                iconRect.y += ((40 * _zoomFactor) - thumbSize) / 2f;
                DrawEnemyThumbnail(iconRect, enemy);
                
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(enemy.EnemyName, isSelected ? EditorStyles.whiteBoldLabel : EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"HP: {enemy.MaxHp} | Spd: {enemy.MoveSpeed}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    SelectEnemy(enemy);
                    Event.current.Use();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawGridView(float containerWidth)
        {
            int startIdx = _currentPage * _itemsPerPage;
            int endIdx = Mathf.Min(startIdx + _itemsPerPage, _filteredEnemies.Count);
            
            int cellWidth = (int)(120 * _zoomFactor);
            int cellHeight = (int)(150 * _zoomFactor);
            
            int columns = Mathf.FloorToInt((containerWidth - 20) / (cellWidth + 10));
            if (columns < 1) columns = 1;

            int count = endIdx - startIdx;
            for (int i = 0; i < count; i += columns)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < columns; c++)
                {
                    int index = startIdx + i + c;
                    if (index < endIdx)
                    {
                        EnemyData enemy = _filteredEnemies[index];
                        bool isSelected = _selectedEnemy == enemy;
                        
                        Rect cardRect = EditorGUILayout.BeginVertical(isSelected ? _selectionStyle : _cardStyle, GUILayout.Width(cellWidth), GUILayout.Height(cellHeight));
                        
                        float thumbSize = cellWidth - 10;
                        Rect thumbnailRect = GUILayoutUtility.GetRect(thumbSize, thumbSize);
                        DrawEnemyThumbnail(thumbnailRect, enemy);
                        
                        EditorGUILayout.LabelField(enemy.EnemyName, EditorStyles.miniLabel, GUILayout.Width(cellWidth - 10));
                        EditorGUILayout.LabelField($"HP: {enemy.MaxHp}", EditorStyles.miniLabel, GUILayout.Width(cellWidth - 10));

                        if (GUI.Button(cardRect, "", GUIStyle.none))
                        {
                            SelectEnemy(enemy);
                        }
                        
                        EditorGUILayout.EndVertical();
                    }
                    else
                    {
                        GUILayout.Space(cellWidth);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawEnemyThumbnail(Rect r, EnemyData enemy)
        {
            Sprite s = null;
            switch (_thumbnailType)
            {
                case ThumbnailType.Chibi: s = enemy.EnemySprite; break;
                case ThumbnailType.Portrait: s = enemy.FullBodyArt; break;
                case ThumbnailType.Splash: s = enemy.FullSplashArt; break;
            }
            if (s == null) s = enemy.EnemySprite ?? enemy.FullBodyArt ?? enemy.FullSplashArt;

            if (s != null)
                GUI.DrawTexture(r, s.texture, ScaleMode.ScaleToFit);
            else
                GUI.Box(r, "?");
        }

        private void DrawPagination()
        {
            int totalPages = Mathf.CeilToInt((float)_filteredEnemies.Count / _itemsPerPage);
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("<", EditorStyles.toolbarButton) && _currentPage > 0) _currentPage--;
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"{_currentPage + 1} / {Mathf.Max(1, totalPages)} ({_filteredEnemies.Count} enemies)", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(">", EditorStyles.toolbarButton) && _currentPage < totalPages - 1) _currentPage++;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDetailsArea()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            
            if (_selectedEnemy == null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Select an enemy to view details", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 14 });
                GUILayout.FlexibleSpace();
            }
            else
            {
                DrawEnemyHeader();
                
                EditorGUILayout.BeginHorizontal();
                
                float detailAreaWidth = position.width - _browserWidth;
                float leftColWidth = Mathf.Clamp(detailAreaWidth * _previewScale, 100f, detailAreaWidth - 150f);

                // Left Column: Visuals
                EditorGUILayout.BeginVertical(GUILayout.Width(leftColWidth));

                _detailScrollPos = EditorGUILayout.BeginScrollView(_detailScrollPos);
                DrawVisuals();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);

                // Right Column: Info
                EditorGUILayout.BeginVertical();
                _infoScrollPos = EditorGUILayout.BeginScrollView(_infoScrollPos);
                DrawStats();
                DrawAbilities();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawEnemyHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_selectedEnemy.EnemyName, new GUIStyle(EditorStyles.boldLabel) { fontSize = 24, fixedHeight = 30 });
            if (GUILayout.Button("Ping Asset", GUILayout.Width(80)))
            {
                EditorGUIUtility.PingObject(_selectedEnemy);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        private void DrawVisuals()
        {
            EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            DrawThumbnailPreview("Chibi", _selectedEnemy.EnemySprite);
            DrawThumbnailPreview("Portrait", _selectedEnemy.FullBodyArt);
            DrawThumbnailPreview("Splash", _selectedEnemy.FullSplashArt);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (_tempPreview != null)
            {
                float detailAreaWidth = position.width - (_showDetails ? _browserWidth : 0);
                float previewWidth = (detailAreaWidth * _previewScale) - 30;
                Rect r = GUILayoutUtility.GetRect(previewWidth, previewWidth * 1.4f);
                GUI.DrawTexture(r, _tempPreview, ScaleMode.ScaleToFit);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }


        private void DrawThumbnailPreview(string label, Sprite s)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(60));
            Rect r = GUILayoutUtility.GetRect(60, 60);
            if (s != null)
            {
                GUI.DrawTexture(r, s.texture, ScaleMode.ScaleToFit);
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    _tempPreview = s.texture;
                    Event.current.Use();
                }
            }
            else GUI.Box(r, "N/A");
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.EndVertical();
        }

        private void DrawStats()
        {
            EditorGUILayout.LabelField("Combat Stats", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"HP: {_selectedEnemy.MaxHp}");
            EditorGUILayout.LabelField($"Speed: {_selectedEnemy.MoveSpeed}");
            EditorGUILayout.LabelField($"Attack: {_selectedEnemy.AttackPower}");
            EditorGUILayout.LabelField($"Range: {_selectedEnemy.AttackRange}");
            EditorGUILayout.LabelField($"Movement: {_selectedEnemy.MovementType}");
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawAbilities()
        {
            EditorGUILayout.LabelField("Abilities", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (_selectedEnemy.Abilities == null || _selectedEnemy.Abilities.Count == 0)
            {
                EditorGUILayout.LabelField("No abilities assigned.", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var ability in _selectedEnemy.Abilities)
                {
                    if (ability == null) continue;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"- {ability.name}", EditorStyles.miniLabel);
                    if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        Selection.activeObject = ability;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void SelectEnemy(EnemyData enemy)
        {
            _selectedEnemy = enemy;
            Sprite s = null;
            switch (_thumbnailType)
            {
                case ThumbnailType.Chibi: s = enemy.EnemySprite; break;
                case ThumbnailType.Portrait: s = enemy.FullBodyArt; break;
                case ThumbnailType.Splash: s = enemy.FullSplashArt; break;
            }
            if (s == null) s = enemy.EnemySprite ?? enemy.FullBodyArt ?? enemy.FullSplashArt;
            _tempPreview = s != null ? s.texture : null;
        }
    }
}
