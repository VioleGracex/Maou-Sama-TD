using UnityEngine;
using UnityEditor;
using MaouSamaTD.Units;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MaouSamaTD.Editor
{
    public class MaouUnitBrowser : EditorWindow
    {
        public enum ViewMode { List, Grid }

        private List<UnitData> _allUnits = new List<UnitData>();
        private List<UnitData> _filteredUnits = new List<UnitData>();
        private string _searchText = "";
        private int _rarityFilter = -1; // -1 for All
        
        // Layout & Paging
        private ViewMode _currentViewMode = ViewMode.List;
        private int _itemsPerPage = 24;
        private int _currentPage = 0;
        
        // Selection State
        private UnitData _selectedUnit;
        private bool _showDetails = true;
        private Vector2 _scrollPos;
        private Vector2 _detailScrollPos;
        
        public enum ThumbnailType { Avatar, Chibi, Portrait, FullBody }
        public enum SortMode { Name, Rarity, Class }
        
        private ThumbnailType _thumbnailType = ThumbnailType.Avatar;
        private SortMode _sortMode = SortMode.Name;
        
        private float _browserWidth = 450f;
        private bool _isResizingDetails = false;
        
        // Zoom & Settings
        private float _zoomFactor = 1.0f;
        private bool _showTitles = true;
        private bool _sortAscending = true;
        
        // Caching
        private Dictionary<string, string> _characterLore = new Dictionary<string, string>();
        private Texture2D _tempSplash;
        private GUIStyle _cardStyle;
        private GUIStyle _selectionStyle;

        [MenuItem("Maou-TD/Unit Browser")]
        public static void Open()
        {
            GetWindow<MaouUnitBrowser>("Unit Browser");
        }

        private void OnEnable()
        {
            RefreshData();
        }

        private void RefreshData()
        {
            _allUnits.Clear();
            _characterLore.Clear();
            string[] guids = AssetDatabase.FindAssets("t:UnitData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnitData unit = AssetDatabase.LoadAssetAtPath<UnitData>(path);
                if (unit != null) _allUnits.Add(unit);
            }
            _allUnits = _allUnits.OrderBy(u => u.UnitName).ToList();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            _filteredUnits = _allUnits.Where(u => 
            {
                // Search filter
                bool matchesSearch = string.IsNullOrEmpty(_searchText) || 
                                     u.UnitName.ToLower().Contains(_searchText.ToLower()) || 
                                     u.UnitTitle.ToLower().Contains(_searchText.ToLower());
                
                // Rarity filter
                bool matchesRarity = _rarityFilter == -1 || (int)u.Rarity == _rarityFilter;
                
                return matchesSearch && matchesRarity;
            }).ToList();

            ApplySort();
            _currentPage = 0;
        }

        private void ApplySort()
        {
            switch (_sortMode)
            {
                case SortMode.Name:
                    _filteredUnits = _sortAscending ? _filteredUnits.OrderBy(u => u.UnitName).ToList() : _filteredUnits.OrderByDescending(u => u.UnitName).ToList();
                    break;
                case SortMode.Rarity:
                    _filteredUnits = _sortAscending ? _filteredUnits.OrderBy(u => u.Rarity).ThenBy(u => u.UnitName).ToList() : _filteredUnits.OrderByDescending(u => u.Rarity).ThenBy(u => u.UnitName).ToList();
                    break;
                case SortMode.Class:
                    _filteredUnits = _sortAscending ? _filteredUnits.OrderBy(u => u.Class).ThenBy(u => u.UnitName).ToList() : _filteredUnits.OrderByDescending(u => u.Class).ThenBy(u => u.UnitName).ToList();
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

            // --- Toolbar ---
            DrawToolbar();
            
            // --- Input Handling ---
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
            
            // Search field
            string newSearch = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            if (newSearch != _searchText)
            {
                _searchText = newSearch;
                ApplyFilter();
            }

            GUILayout.Space(10);

            // Rarity Filter
            GUILayout.Label("Rarity:", GUILayout.Width(45));
            string[] rarityOptions = new string[] { "All", "C", "UC", "R", "SR", "SSR", "UR" };
            int newRarity = EditorGUILayout.Popup(_rarityFilter + 1, rarityOptions, GUILayout.Width(60)) - 1;
            if (newRarity != _rarityFilter)
            {
                _rarityFilter = newRarity;
                ApplyFilter();
            }

            GUILayout.FlexibleSpace();

            // Sort Mode Option
            GUILayout.Label("Sort:", GUILayout.Width(32));
            SortMode newSort = (SortMode)EditorGUILayout.EnumPopup(_sortMode, GUILayout.Width(65));
            if (newSort != _sortMode)
            {
                _sortMode = newSort;
                ApplySort();
            }
            
            // Asc/Desc Toggle
            string sortIcon = _sortAscending ? "d_ToolHandleLocal" : "d_ToolHandleGlobal"; // Just using some icons as placeholders for arrows
            if (GUILayout.Button(EditorGUIUtility.IconContent(_sortAscending ? "d_ViewToolMove" : "d_ViewToolMove"), EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                _sortAscending = !_sortAscending;
                ApplySort();
            }
            
            GUILayout.Space(10);
            
            // Items Per Page
            GUILayout.Label("Per Page:", GUILayout.Width(60));
            int[] pageCounts = new int[] { 12, 24, 48, 96, 120 };
            string[] pageLabels = new string[] { "12", "24", "48", "96", "All" };
            int newItemsPerPage = EditorGUILayout.IntPopup(_itemsPerPage, pageLabels, pageCounts, GUILayout.Width(45));
            if (newItemsPerPage != _itemsPerPage)
            {
                _itemsPerPage = newItemsPerPage;
                _currentPage = 0;
            }
            GUILayout.Space(10);

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
            
            // Details Toggle
            _showDetails = GUILayout.Toggle(_showDetails, _showDetails ? "Hide Details" : "Show Details", EditorStyles.toolbarButton, GUILayout.Width(100));

            GUILayout.Space(5);
            
            // Show Titles Toggle
            _showTitles = GUILayout.Toggle(_showTitles, "Titles", EditorStyles.toolbarButton, GUILayout.Width(50));
            
            GUILayout.Space(5);
            _thumbnailType = (ThumbnailType)EditorGUILayout.EnumPopup(_thumbnailType, GUILayout.Width(80));

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

            // Pagination at bottom of browser
            DrawPagination();

            EditorGUILayout.EndVertical();
        }

        private void DrawListView()
        {
            int startIdx = _currentPage * _itemsPerPage;
            int endIdx = Mathf.Min(startIdx + _itemsPerPage, _filteredUnits.Count);

            for (int i = startIdx; i < endIdx; i++)
            {
                UnitData unit = _filteredUnits[i];
                bool isSelected = _selectedUnit == unit;
                
                Rect rect = EditorGUILayout.BeginHorizontal(isSelected ? _selectionStyle : GUIStyle.none, GUILayout.Height(45 * _zoomFactor));
                GUILayout.Space(10 * _zoomFactor); // Side padding
                
                // Avatar thumbnail
                float thumbSize = 35 * _zoomFactor;
                Rect iconRect = GUILayoutUtility.GetRect(thumbSize, thumbSize);
                iconRect.y += ( (40 * _zoomFactor) - thumbSize) / 2f;
                DrawUnitThumbnail(iconRect, unit);
                
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                
                GUIStyle nameStyle = new GUIStyle(isSelected ? EditorStyles.whiteBoldLabel : EditorStyles.boldLabel);
                nameStyle.fontSize = (int)(12 * _zoomFactor);
                EditorGUILayout.LabelField(unit.UnitName, nameStyle);
                
                if (_showTitles && !string.IsNullOrEmpty(unit.UnitTitle))
                {
                    GUILayout.Space(4 * _zoomFactor);
                    GUIStyle titleStyle = new GUIStyle(EditorStyles.miniLabel);
                    titleStyle.fontSize = (int)(9 * _zoomFactor);
                    EditorGUILayout.LabelField(unit.UnitTitle, titleStyle);
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    if (_selectedUnit == unit) _showDetails = true;
                    SelectUnit(unit);
                    Event.current.Use();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawGridView(float containerWidth)
        {
            int startIdx = _currentPage * _itemsPerPage;
            int endIdx = Mathf.Min(startIdx + _itemsPerPage, _filteredUnits.Count);
            
                int cellWidth = (int)(120 * _zoomFactor);
                int cellHeight = (int)(155 * _zoomFactor);
                
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
                            UnitData unit = _filteredUnits[index];
                            bool isSelected = _selectedUnit == unit;
                            
                            Rect cardRect = EditorGUILayout.BeginVertical(isSelected ? _selectionStyle : _cardStyle, GUILayout.Width(cellWidth), GUILayout.Height(cellHeight));
                            
                            float thumbSize = cellWidth - 10;
                            Rect thumbnailRect = GUILayoutUtility.GetRect(thumbSize, thumbSize);
                            DrawUnitThumbnail(thumbnailRect, unit);
                            
                            GUIStyle nameStyle = new GUIStyle(EditorStyles.miniLabel);
                            nameStyle.fontSize = (int)(10 * _zoomFactor);
                            nameStyle.alignment = TextAnchor.MiddleCenter;
                            
                            EditorGUILayout.LabelField(unit.UnitName, nameStyle, GUILayout.Width(cellWidth - 10));
                            
                            if (_showTitles && !string.IsNullOrEmpty(unit.UnitTitle))
                            {
                                GUILayout.Space(4 * _zoomFactor);
                                GUIStyle titleStyle = new GUIStyle(EditorStyles.miniLabel);
                                titleStyle.fontSize = (int)(9 * _zoomFactor);
                                titleStyle.alignment = TextAnchor.MiddleCenter;
                                titleStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                                EditorGUILayout.LabelField(unit.UnitTitle, titleStyle, GUILayout.Width(cellWidth - 10));
                            }
                            
                            GUILayout.Space(10 * _zoomFactor); 

                        // Overlay button for selection
                        if (GUI.Button(cardRect, "", GUIStyle.none))
                        {
                            if (_selectedUnit == unit) _showDetails = true;
                            SelectUnit(unit);
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

        private void DrawUnitThumbnail(Rect r, UnitData unit)
        {
            Sprite s = null;
            switch (_thumbnailType)
            {
                case ThumbnailType.Avatar: s = unit.BaseSkin.Avatar; break;
                case ThumbnailType.Chibi: s = unit.BaseSkin.Chibi; break;
                case ThumbnailType.Portrait: s = unit.BaseSkin.WaistUp; break;
                case ThumbnailType.FullBody: s = unit.BaseSkin.FullBodyCutout; break;
            }
            if (s == null) s = unit.BaseSkin.Avatar ?? unit.BaseSkin.Chibi ?? unit.BaseSkin.WaistUp ?? unit.BaseSkin.FullBodyCutout;

            if (s != null)
                GUI.DrawTexture(r, s.texture, ScaleMode.ScaleToFit);
            else
                GUI.Box(r, "?");
        }

        private void DrawPagination()
        {
            int totalPages = Mathf.CeilToInt((float)_filteredUnits.Count / _itemsPerPage);
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("<", EditorStyles.toolbarButton) && _currentPage > 0) _currentPage--;
            GUILayout.FlexibleSpace();
            
            string countText = $"{_currentPage + 1} / {Mathf.Max(1, totalPages)}  ({_filteredUnits.Count} units)";
            EditorGUILayout.LabelField(countText, EditorStyles.miniLabel, GUILayout.Width(100));
            
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(">", EditorStyles.toolbarButton) && _currentPage < totalPages - 1) _currentPage++;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDetailsArea()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            
            if (_selectedUnit == null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Select a unit to view details", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 14 });
                GUILayout.FlexibleSpace();
            }
            else
            {
                DrawUnitHeader();
                _detailScrollPos = EditorGUILayout.BeginScrollView(_detailScrollPos);
                
                DrawVisuals();
                DrawLore();
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawUnitHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(_selectedUnit.UnitName, new GUIStyle(EditorStyles.boldLabel) { fontSize = 28, fixedHeight = 34 });
            GUILayout.Space(8);
            EditorGUILayout.LabelField(_selectedUnit.UnitTitle, new GUIStyle(EditorStyles.label) { fontSize = 15, fontStyle = FontStyle.Italic, normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } });
            EditorGUILayout.EndVertical();
            
            if (GUILayout.Button("Ping Asset", GUILayout.Width(100), GUILayout.Height(30)))
            {
                EditorGUIUtility.PingObject(_selectedUnit);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        private void DrawVisuals()
        {
            EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
            
            // 1. Default Base Visuals (NOW ABOVE)
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Default Base Visuals", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            DrawVariantIcon("Avatar", _selectedUnit.BaseSkin.Avatar);
            DrawVariantIcon("Chibi", _selectedUnit.BaseSkin.Chibi);
            DrawVariantIcon("Portrait", _selectedUnit.BaseSkin.WaistUp);
            DrawVariantIcon("Full Body", _selectedUnit.BaseSkin.FullBodyCutout);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 2. Main Preview (Centered)
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            float splashWidth = position.width * 0.45f;
            Rect splashRect = GUILayoutUtility.GetRect(splashWidth, splashWidth * 1.4f, GUILayout.Width(splashWidth));
            if (_tempSplash != null)
                GUI.DrawTexture(splashRect, _tempSplash, ScaleMode.ScaleToFit);
            else
                GUI.Box(splashRect, "No Splash Loaded");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 3. Skin / Art Variants (NOW BELOW)
            if (_selectedUnit.Skins != null && _selectedUnit.Skins.Count > 0)
            {
                EditorGUILayout.LabelField("Skin / Art Variants", EditorStyles.miniBoldLabel);
                foreach (var skin in _selectedUnit.Skins)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(skin.SkinThemeName, EditorStyles.miniBoldLabel, GUILayout.Width(150));
                    GUILayout.FlexibleSpace();
                    DrawVariantIcon("Avatar", skin.Avatar);
                    DrawVariantIcon("Chibi", skin.Chibi);
                    DrawVariantIcon("Portrait", skin.WaistUp);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("View Full", EditorStyles.miniButton, GUILayout.Width(80))) 
                        _tempSplash = GetSpriteTexture(skin.FullSplashArt) ?? GetSpriteTexture(skin.FullBodyCutout);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
            }
            
            EditorGUILayout.Space(10);
        }

        private void DrawVariantIcon(string name, Sprite sprite)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(50));
            Rect r = GUILayoutUtility.GetRect(50, 50);
            if (sprite != null) 
            {
                Texture2D tex = GetSpriteTexture(sprite);
                if (tex != null)
                {
                    GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
                    if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                    {
                        _tempSplash = tex;
                        Event.current.Use();
                    }
                }
                else GUI.Box(r, "Err");
            }
            else GUI.Box(r, "N/A");
            EditorGUILayout.LabelField(name, EditorStyles.miniLabel, GUILayout.Width(50), GUILayout.Height(15));
            EditorGUILayout.EndVertical();
        }

        private void DrawLore()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Character Lore & Development", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();

            // 1. Brief Description
            EditorGUILayout.LabelField("Brief Summary", EditorStyles.miniBoldLabel);
            _selectedUnit.BriefDescription = EditorGUILayout.TextArea(_selectedUnit.BriefDescription, GUILayout.Height(60));
            
            EditorGUILayout.Space(10);
            
            // 2. Lore Entries
            EditorGUILayout.LabelField("Story Fragments / Chambers", EditorStyles.miniBoldLabel);
            
            if (_selectedUnit.LoreEntries == null) _selectedUnit.LoreEntries = new System.Collections.Generic.List<UnitData.UnitLoreEntry>();
            
            for (int i = 0; i < _selectedUnit.LoreEntries.Count; i++)
            {
                var entry = _selectedUnit.LoreEntries[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                entry.Title = EditorGUILayout.TextField("Title", entry.Title);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    _selectedUnit.LoreEntries.RemoveAt(i);
                    i--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndHorizontal();
                
                entry.Content = EditorGUILayout.TextArea(entry.Content, GUILayout.Height(100));
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            if (GUILayout.Button("+ Add Lore Story", EditorStyles.miniButton))
            {
                _selectedUnit.LoreEntries.Add(new UnitData.UnitLoreEntry() { Title = "New Fragment", Content = "Write story here..." });
            }

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditor.EditorUtility.SetDirty(_selectedUnit);
            }

            EditorGUILayout.EndVertical();
        }

        private Texture2D GetSpriteTexture(Sprite sprite)
        {
            if (sprite == null) return null;
            try { return sprite.texture; } catch { return null; }
        }

        private void SelectUnit(UnitData unit)
        {
            _selectedUnit = unit;
            _tempSplash = null;
            
            // Lazy load first art variant appropriately
            _tempSplash = GetSpriteTexture(unit.BaseSkin.FullSplashArt)
                ?? GetSpriteTexture(unit.BaseSkin.FullBodyCutout)
                ?? GetSpriteTexture(unit.BaseSkin.WaistUp)
                ?? GetSpriteTexture(unit.BaseSkin.Chibi);

            if (_tempSplash == null && unit.Skins != null && unit.Skins.Count > 0)
                _tempSplash = GetSpriteTexture(unit.Skins[0].FullSplashArt);
        }

        private string GetLoreForUnit(UnitData unit)
        {
            if (_characterLore.TryGetValue(unit.UnitName, out string cachedLore)) return cachedLore;

            string docsPath = Path.Combine(Application.dataPath, "_Game/docs~/characters");
            if (Directory.Exists(docsPath))
            {
                string searchPattern = $"*{unit.UnitName.ToLower()}*.md";
                string[] files = Directory.GetFiles(docsPath, searchPattern);
                if (files.Length > 0)
                {
                    string content = File.ReadAllText(files[0]);
                    _characterLore[unit.UnitName] = content;
                    return content;
                }
            }
            return "Background documentation not found in docs~.";
        }
    }
}
