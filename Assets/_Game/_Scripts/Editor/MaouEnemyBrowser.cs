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
        private int _categoryFilter = -1;  // -1 for All
        
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
        private EnemyData _hoveredEnemy;
        
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

        private enum EnemyBrowserTab
        {
            Enemies,
            LootDrops
        }
        private EnemyBrowserTab _currentTab = EnemyBrowserTab.Enemies;
        private Vector2 _lootScrollPos;
        private Dictionary<EnemyCategory, bool> _categoryExpanded = new Dictionary<EnemyCategory, bool>();

        [MenuItem("Maou-TD/Enemy Browser")]
        public static void Open()
        {
            GetWindow<MaouEnemyBrowser>("Enemy Browser");
        }

        public static void OpenAndSelect(EnemyData enemy)
        {
            var window = GetWindow<MaouEnemyBrowser>("Enemy Browser");
            window.SelectEnemy(enemy);
            window._showDetails = true;
            window.Repaint();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
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
            _categoryFilter = EditorPrefs.GetInt("MaouEnemyBrowser_CategoryFilter", -1);
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
            EditorPrefs.SetInt("MaouEnemyBrowser_CategoryFilter", _categoryFilter);
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
                // Category filter (-1 = Any, 0 = None/Fallback, 1-6 = specific)
                bool matchesCategory = _categoryFilter == -1 || (int)e.GetEffectiveCategory() == _categoryFilter;
                
                return matchesSearch && matchesMovement && matchesCategory;
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
            _hoveredEnemy = null;
            InitializeStyles();

            DrawToolbar();
            HandleGlobalInput();

            DrawTabsHeader();

            if (_currentTab == EnemyBrowserTab.LootDrops)
            {
                DrawLootDropsArea();
            }
            else
            {
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

            if (Event.current.type == EventType.MouseMove)
            {
                Repaint();
            }

            DrawHoverTooltip();
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

            GUILayout.Space(6);
            GUILayout.Label("Cat:", GUILayout.Width(30));
            string[] catOptions = new string[] { "Any", "None", "Shadow", "Bandit", "Animal", "Golem", "Undead", "Demon" };
            int[] catValues     = new int[]    {  -1,    0,      1,        2,        3,        4,       5,        6 };
            int curCatIdx = System.Array.IndexOf(catValues, _categoryFilter);
            if (curCatIdx < 0) curCatIdx = 0;
            int newCatIdx = EditorGUILayout.Popup(curCatIdx, catOptions, GUILayout.Width(70));
            if (catValues[newCatIdx] != _categoryFilter)
            {
                _categoryFilter = catValues[newCatIdx];
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
                        
                        if (cardRect.Contains(Event.current.mousePosition))
                        {
                            _hoveredEnemy = enemy;
                        }
                        
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
            {
                GUI.DrawTexture(r, s.texture, ScaleMode.ScaleToFit);
                GUI.Label(r, new GUIContent("", enemy.EnemyName));
            }
            else
            {
                GUI.Box(r, new GUIContent("?", enemy.EnemyName));
            }
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
                DrawCategoryAndDrops();
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
        private void DrawCategoryAndDrops()
        {
            EditorGUILayout.LabelField("Category & Loot Drops", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // ── Category Picker ────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            var newCat = (EnemyCategory)EditorGUILayout.EnumPopup(
                new GUIContent("Category", "Which loot pool this enemy drops from. 'None' uses the auto-fallback."),
                _selectedEnemy.Category);
            var newRank = (EnemyRank)EditorGUILayout.EnumPopup(
                new GUIContent("Rank", "Enemy rank: Normal, Elite (2x drops), or Boss (guaranteed loot)."),
                _selectedEnemy.Rank);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_selectedEnemy, "Edit Enemy Category/Rank");
                _selectedEnemy.Category = newCat;
                _selectedEnemy.Rank     = newRank;
                EditorUtility.SetDirty(_selectedEnemy);
            }

            // ── Effective values with fallback indicator ───────────────────
            var effectiveCat  = _selectedEnemy.GetEffectiveCategory();
            var effectiveRank = _selectedEnemy.GetEffectiveRank();
            bool catFallback  = _selectedEnemy.Category == EnemyCategory.None;
            bool rankFallback = !_selectedEnemy.IsBoss && _selectedEnemy.Rank == effectiveRank;

            EditorGUILayout.Space(4);
            GUIStyle effectiveStyle = new GUIStyle(EditorStyles.miniLabel);
            effectiveStyle.normal.textColor = catFallback ? new Color(1f, 0.6f, 0.2f) : new Color(0.4f, 0.9f, 0.5f);
            string catLabel = catFallback ? $"⚠ Fallback → {effectiveCat}" : $"✓ {effectiveCat}";
            EditorGUILayout.LabelField("Effective Category:", catLabel, effectiveStyle);

            GUIStyle rankStyle = new GUIStyle(EditorStyles.miniLabel);
            rankStyle.normal.textColor = _selectedEnemy.IsBoss ? new Color(1f, 0.3f, 0.3f) : new Color(0.4f, 0.9f, 0.5f);
            string rankLabel = _selectedEnemy.IsBoss ? $"⚡ IsBoss → {effectiveRank}" : $"✓ {effectiveRank}";
            EditorGUILayout.LabelField("Effective Rank:", rankLabel, rankStyle);

            EditorGUILayout.Space(6);

            // ── Drop Preview ───────────────────────────────────────────────
            string matID = CategoryToMaterialPreview(effectiveCat);
            EditorGUILayout.LabelField("Drop Preview:", EditorStyles.miniBoldLabel);

            if (effectiveRank == EnemyRank.Boss)
            {
                EditorGUILayout.LabelField($"  ✦ 100%  → 3x {matID}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"  ✦ 100%  → 1x xp_core_legendary", EditorStyles.miniLabel);
            }
            else
            {
                int qty = effectiveRank == EnemyRank.Elite ? 2 : 1;
                EditorGUILayout.LabelField($"  ◆ 40%   → {qty}x {matID}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"  ◆ 15%   → 1x xp_core_common", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"  ◆  4%   → 1x xp_core_rare", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"  ◆  1%   → 1x xp_core_epic", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private static string CategoryToMaterialPreview(EnemyCategory cat)
        {
            return cat switch
            {
                EnemyCategory.Shadow => "mat_shadow_essence",
                EnemyCategory.Bandit => "mat_bandit_insignia",
                EnemyCategory.Animal => "mat_animal_fang",
                EnemyCategory.Golem  => "mat_golem_core",
                EnemyCategory.Undead => "mat_shadow_essence",
                EnemyCategory.Demon  => "mat_shadow_essence",
                _                    => "mat_bandit_insignia",
            };
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

        private void DrawHoverTooltip()
        {
            if (_hoveredEnemy == null) return;

            Vector2 mousePos = Event.current.mousePosition;
            float width = 220;
            float height = 75;
            float x = mousePos.x + 15;
            float y = mousePos.y + 15;

            if (x + width > position.width) x = mousePos.x - width - 15;
            if (y + height > position.height) y = mousePos.y - height - 15;

            Rect tooltipRect = new Rect(x, y, width, height);

            Handles.BeginGUI();
            GUI.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);
            GUI.Box(tooltipRect, "", (GUIStyle)"helpbox");
            GUI.color = Color.white;

            Rect paddingRect = new Rect(tooltipRect.x + 10, tooltipRect.y + 8, tooltipRect.width - 20, tooltipRect.height - 16);
            
            GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel);
            nameStyle.fontSize = 12;
            nameStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(paddingRect.x, paddingRect.y, paddingRect.width, 20), _hoveredEnemy.EnemyName, nameStyle);

            GUIStyle subStyle = new GUIStyle(EditorStyles.miniLabel);
            subStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            string shortId = !string.IsNullOrEmpty(_hoveredEnemy.UniqueID) && _hoveredEnemy.UniqueID.Length >= 8 
                ? _hoveredEnemy.UniqueID.Substring(0, 8) 
                : "N/A";
            GUI.Label(new Rect(paddingRect.x, paddingRect.y + 20, paddingRect.width, 16), $"Enemy Unit | ID: {shortId}", subStyle);

            GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel);
            badgeStyle.normal.textColor = new Color(1.0f, 0.3f, 0.3f);
            badgeStyle.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(paddingRect.x, paddingRect.y + 38, paddingRect.width, 16), $"HP: {_hoveredEnemy.MaxHp} | Speed: {_hoveredEnemy.MoveSpeed}", badgeStyle);

            Handles.EndGUI();
        }

        private void DrawTabsHeader()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUIStyle tabStyleLeft = new GUIStyle(EditorStyles.miniButtonLeft);
            GUIStyle tabStyleRight = new GUIStyle(EditorStyles.miniButtonRight);
            
            tabStyleLeft.fontSize = 12;
            tabStyleLeft.fontStyle = FontStyle.Bold;
            tabStyleLeft.fixedHeight = 26;
            
            tabStyleRight.fontSize = 12;
            tabStyleRight.fontStyle = FontStyle.Bold;
            tabStyleRight.fixedHeight = 26;
            
            if (_currentTab == EnemyBrowserTab.Enemies)
            {
                tabStyleLeft.normal.textColor = new Color(0.2f, 0.6f, 1.0f);
            }
            else
            {
                tabStyleRight.normal.textColor = new Color(0.2f, 0.6f, 1.0f);
            }

            if (GUILayout.Button("💀  ENEMY BESTIARY", tabStyleLeft, GUILayout.ExpandWidth(true)))
            {
                _currentTab = EnemyBrowserTab.Enemies;
            }
            
            if (GUILayout.Button("⚖️  LOOT CONFIG", tabStyleRight, GUILayout.ExpandWidth(true)))
            {
                _currentTab = EnemyBrowserTab.LootDrops;
            }
            
            if (GUILayout.Button(new GUIContent("🔄", "Force Refresh Assets"), EditorStyles.miniButtonRight, GUILayout.Width(35)))
            {
                AssetDatabase.Refresh();
                RefreshData();
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void DrawLootDropsArea()
        {
            // Find and load LootConfig
            MaouLootConfig lootConfig = null;
            string[] guids = AssetDatabase.FindAssets("t:MaouLootConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                lootConfig = AssetDatabase.LoadAssetAtPath<MaouLootConfig>(path);
            }

            if (lootConfig == null)
            {
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                EditorGUILayout.HelpBox("Loot Configuration ScriptableObject not found. Create one to begin customizing drops.", MessageType.Warning);
                if (GUILayout.Button("Create New Loot Configuration Asset", GUILayout.Height(30)))
                {
                    var config = ScriptableObject.CreateInstance<MaouLootConfig>();
                    
                    // Create direct under _Game/Resources if exists, or Assets/
                    string dir = "Assets/_Game/Resources";
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    AssetDatabase.CreateAsset(config, "Assets/_Game/Resources/MaouLootConfig.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                EditorGUILayout.EndVertical();
                return;
            }

            _lootScrollPos = EditorGUILayout.BeginScrollView(_lootScrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(15);

            // --- Header & Title ---
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("⚖️  GLOBAL LOOT & DROPS SYSTEM CONFIG", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, normal = { textColor = Color.white } });
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent(" Force Refresh", EditorGUIUtility.IconContent("d_Refresh").image), GUILayout.Width(130), GUILayout.Height(25)))
            {
                AssetDatabase.Refresh();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Configure rates, quantities, categories, fallbacks, and overrides for the drop engine.", EditorStyles.miniLabel);
            GUILayout.Space(15);

            EditorGUI.BeginChangeCheck();

            // --- Section 1: Default Fallback & General Settings ---
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("FALLBACKS & GLOBAL SETTINGS", EditorStyles.boldLabel);
            lootConfig.FallbackCategory = (EnemyCategory)EditorGUILayout.EnumPopup("Default Category Fallback", lootConfig.FallbackCategory);
            EditorGUILayout.EndVertical();
            GUILayout.Space(15);

            // --- Section 2: XP Core Weights ---
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("XP CORE WEIGHT DISTRIBUTIONS", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Weights will be normalized automatically when rolled.", MessageType.Info);
            
            lootConfig.CommonWeight = EditorGUILayout.Slider("Common Core Weight", lootConfig.CommonWeight, 0f, 1f);
            lootConfig.RareWeight = EditorGUILayout.Slider("Rare Core Weight", lootConfig.RareWeight, 0f, 1f);
            lootConfig.EpicWeight = EditorGUILayout.Slider("Epic Core Weight", lootConfig.EpicWeight, 0f, 1f);
            
            float sum = lootConfig.CommonWeight + lootConfig.RareWeight + lootConfig.EpicWeight;
            if (sum > 0f)
            {
                EditorGUILayout.HelpBox(
                    $"Effective Rates:\n" +
                    $"- Common: {lootConfig.CommonWeight / sum:P1}\n" +
                    $"- Rare: {lootConfig.RareWeight / sum:P1}\n" +
                    $"- Epic: {lootConfig.EpicWeight / sum:P1}", 
                    MessageType.None);
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(15);

            // --- Section 3: Category Settings ---
            EditorGUILayout.LabelField("CATEGORY DROP RATES & QUANTITIES", EditorStyles.boldLabel);
            
            var categories = new EnemyCategory[] { 
                EnemyCategory.Shadow, 
                EnemyCategory.Bandit, 
                EnemyCategory.Animal, 
                EnemyCategory.Golem, 
                EnemyCategory.Undead, 
                EnemyCategory.Demon 
            };

            foreach (var cat in categories)
            {
                var settings = lootConfig.GetSettingsForCategory(cat);
                
                if (!_categoryExpanded.ContainsKey(cat))
                {
                    _categoryExpanded[cat] = false;
                }

                _categoryExpanded[cat] = EditorGUILayout.Foldout(_categoryExpanded[cat], $"💀 {cat.ToString().ToUpper()} Drops Configuration", true, EditorStyles.foldoutHeader);

                if (_categoryExpanded[cat])
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(5);
                    
                    settings.PrimaryMaterialID = EditorGUILayout.TextField("Primary Material ID", settings.PrimaryMaterialID);
                    
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Normal Enemies:", EditorStyles.miniBoldLabel);
                    settings.NormalMaterialChance = EditorGUILayout.Slider("  Material Drop Chance", settings.NormalMaterialChance, 0f, 1f);
                    settings.NormalXpCoreChance = EditorGUILayout.Slider("  XP Core Drop Chance", settings.NormalXpCoreChance, 0f, 1f);

                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Elite Enemies:", EditorStyles.miniBoldLabel);
                    settings.EliteMaterialChance = EditorGUILayout.Slider("  Material Drop Chance", settings.EliteMaterialChance, 0f, 1f);
                    settings.EliteXpCoreChance = EditorGUILayout.Slider("  XP Core Drop Chance", settings.EliteXpCoreChance, 0f, 1f);
                    settings.EliteMaterialQuantity = EditorGUILayout.IntField("  Material Quantity", settings.EliteMaterialQuantity);

                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Boss Enemies:", EditorStyles.miniBoldLabel);
                    settings.BossMaterialQuantity = EditorGUILayout.IntField("  Guaranteed Mat Qty", settings.BossMaterialQuantity);
                    settings.BossGuaranteedXpCoreID = EditorGUILayout.TextField("  Guaranteed XP Core ID", settings.BossGuaranteedXpCoreID);
                    
                    GUILayout.Space(5);
                    EditorGUILayout.EndVertical();
                }
                GUILayout.Space(6);
            }

            GUILayout.Space(15);

            // --- Section 4: Special Overrides ---
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🎯 SPECIAL ENEMY OVERRIDES", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Add Override", GUILayout.Width(110)))
            {
                lootConfig.SpecialOverrides.Add(new SpecialEnemyOverride { EnableOverride = true });
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);

            int overrideToDelete = -1;
            for (int i = 0; i < lootConfig.SpecialOverrides.Count; i++)
            {
                var over = lootConfig.SpecialOverrides[i];
                
                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                EditorGUILayout.BeginHorizontal();
                over.EnableOverride = EditorGUILayout.ToggleLeft("Enable Override", over.EnableOverride, GUILayout.Width(130));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("🗑️ Remove", GUILayout.Width(75)))
                {
                    overrideToDelete = i;
                }
                EditorGUILayout.EndHorizontal();
                
                // Select Enemy Dropdown
                if (_allEnemies != null && _allEnemies.Count > 0)
                {
                    string[] enemyNames = _allEnemies.Select(e => $"{e.EnemyName} ({e.UniqueID})").ToArray();
                    string[] enemyIds = _allEnemies.Select(e => e.UniqueID).ToArray();
                    
                    int currentIdx = System.Array.IndexOf(enemyIds, over.EnemyUniqueID);
                    if (currentIdx < 0) currentIdx = 0;
                    
                    int newIdx = EditorGUILayout.Popup("Target Enemy", currentIdx, enemyNames);
                    if (newIdx >= 0 && newIdx < enemyIds.Length)
                    {
                        over.EnemyUniqueID = enemyIds[newIdx];
                        over.EnemyName = _allEnemies[newIdx].EnemyName;
                    }
                }
                else
                {
                    over.EnemyUniqueID = EditorGUILayout.TextField("Target Unique ID", over.EnemyUniqueID);
                }

                over.CustomMaterialID = EditorGUILayout.TextField("Custom Material ID", over.CustomMaterialID);
                over.CustomMaterialQuantity = EditorGUILayout.IntField("Custom Material Qty", over.CustomMaterialQuantity);
                over.CustomMaterialChance = EditorGUILayout.Slider("Custom Material Chance", over.CustomMaterialChance, 0f, 1f);

                over.CustomXpCoreID = EditorGUILayout.TextField("Custom XP Core ID", over.CustomXpCoreID);
                over.CustomXpCoreChance = EditorGUILayout.Slider("Custom XP Core Chance", over.CustomXpCoreChance, 0f, 1f);

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }

            if (overrideToDelete >= 0)
            {
                lootConfig.SpecialOverrides.RemoveAt(overrideToDelete);
            }

            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(lootConfig, "Modify Global Loot Drops Settings");
                EditorUtility.SetDirty(lootConfig);
                AssetDatabase.SaveAssets();
            }

            GUILayout.Space(40);
            EditorGUILayout.EndVertical();
            GUILayout.Space(15);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();
        }
    }
}
