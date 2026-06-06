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
        private UnitData _hoveredUnit;
        
        public enum ThumbnailType { Avatar, Chibi, WaistUp, FullBody, SplashArt }
        public enum SortMode { Name, Rarity, Class }
        
        private ThumbnailType _thumbnailType = ThumbnailType.Avatar;
        private SortMode _sortMode = SortMode.Name;
        
        private float _browserWidth = 450f;
        private bool _isResizingDetails = false;
        
        // Zoom & Settings
        private float _previewZoom = 1.0f;
        private float _listZoom = 1.0f;
        private bool _showTitles = true;
        private bool _sortAscending = true;
        
        // Caching
        private Dictionary<string, string> _characterLore = new Dictionary<string, string>();
        private Texture2D _tempSplash;
        private GUIStyle _cardStyle;
        private GUIStyle _selectionStyle;

        public enum BrowserTab { Vassals, Classes }
        private BrowserTab _currentTab = BrowserTab.Vassals;

        private class ClassInfo
        {
            public UnitClass ClassType;
            public string Name;
            public string Icon;
            public string Role;
            public string ColorTheme;
            public string Summary;
            public string Description;
        }

        private List<ClassInfo> _classes = new List<ClassInfo>();
        private UnitClass _selectedClass = UnitClass.Bastion;
        private Vector2 _classListScroll;
        private Vector2 _classDetailScroll;

        [MenuItem("Maou-TD/Unit Browser")]
        public static void Open()
        {
            GetWindow<MaouUnitBrowser>("Unit Browser");
        }

        public static void OpenAndSelect(UnitData unit)
        {
            var window = GetWindow<MaouUnitBrowser>("Unit Browser");
            window.SelectUnit(unit);
            window.ShowDetailsWithUnit(unit);
        }

        private void ShowDetailsWithUnit(UnitData unit)
        {
            _showDetails = true;
            Repaint();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            LoadSettings();
            RefreshData();
            InitializeClassInfo();
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        private void LoadSettings()
        {
            _searchText = EditorPrefs.GetString("MaouUnitBrowser_SearchText", "");
            _rarityFilter = EditorPrefs.GetInt("MaouUnitBrowser_RarityFilter", -1);
            _currentViewMode = (ViewMode)EditorPrefs.GetInt("MaouUnitBrowser_ViewMode", (int)ViewMode.List);
            _itemsPerPage = EditorPrefs.GetInt("MaouUnitBrowser_ItemsPerPage", 24);
            _currentPage = EditorPrefs.GetInt("MaouUnitBrowser_CurrentPage", 0);
            _showDetails = EditorPrefs.GetBool("MaouUnitBrowser_ShowDetails", true);
            _thumbnailType = (ThumbnailType)EditorPrefs.GetInt("MaouUnitBrowser_ThumbnailType", (int)ThumbnailType.Avatar);
            _sortMode = (SortMode)EditorPrefs.GetInt("MaouUnitBrowser_SortMode", (int)SortMode.Name);
            _browserWidth = EditorPrefs.GetFloat("MaouUnitBrowser_BrowserWidth", 450f);
            _previewZoom = EditorPrefs.GetFloat("MaouUnitBrowser_PreviewZoom", 1.0f);
            _listZoom = EditorPrefs.GetFloat("MaouUnitBrowser_ListZoom", 1.0f);
            _showTitles = EditorPrefs.GetBool("MaouUnitBrowser_ShowTitles", true);
            _sortAscending = EditorPrefs.GetBool("MaouUnitBrowser_SortAscending", true);
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString("MaouUnitBrowser_SearchText", _searchText);
            EditorPrefs.SetInt("MaouUnitBrowser_RarityFilter", _rarityFilter);
            EditorPrefs.SetInt("MaouUnitBrowser_ViewMode", (int)_currentViewMode);
            EditorPrefs.SetInt("MaouUnitBrowser_ItemsPerPage", _itemsPerPage);
            EditorPrefs.SetInt("MaouUnitBrowser_CurrentPage", _currentPage);
            EditorPrefs.SetBool("MaouUnitBrowser_ShowDetails", _showDetails);
            EditorPrefs.SetInt("MaouUnitBrowser_ThumbnailType", (int)_thumbnailType);
            EditorPrefs.SetInt("MaouUnitBrowser_SortMode", (int)_sortMode);
            EditorPrefs.SetFloat("MaouUnitBrowser_BrowserWidth", _browserWidth);
            EditorPrefs.SetFloat("MaouUnitBrowser_PreviewZoom", _previewZoom);
            EditorPrefs.SetFloat("MaouUnitBrowser_ListZoom", _listZoom);
            EditorPrefs.SetBool("MaouUnitBrowser_ShowTitles", _showTitles);
            EditorPrefs.SetBool("MaouUnitBrowser_SortAscending", _sortAscending);
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
            _hoveredUnit = null;
            InitializeStyles();

            // --- Tab Selection Header ---
            DrawTabsHeader();

            if (_currentTab == BrowserTab.Classes)
            {
                DrawClassBestiaryArea();
            }
            else
            {
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

            if (Event.current.type == EventType.MouseMove)
            {
                Repaint();
            }

            if (_currentTab == BrowserTab.Vassals)
            {
                DrawHoverTooltip();
            }
        }

        private void HandleGlobalInput()
        {
            Event e = Event.current;
            if (e.type == EventType.ScrollWheel && (e.control || e.command))
            {
                float delta = -e.delta.y * 0.05f;
                
                // Strictly contextual zoom based on mouse position
                if (e.mousePosition.x < _browserWidth)
                {
                    _listZoom = Mathf.Clamp(_listZoom + delta, 0.5f, 2.5f);
                    // Ensure list zoom doesn't leak to preview
                }
                else
                {
                    _previewZoom = Mathf.Clamp(_previewZoom + delta, 0.5f, 4.0f);
                    // Ensure preview zoom doesn't leak to list
                }
                    
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
            
            if (GUILayout.Button(new GUIContent(" Force Refresh", EditorGUIUtility.IconContent("d_Refresh").image), EditorStyles.toolbarButton))
            {
                AssetDatabase.Refresh();
                RefreshData();
                Repaint();
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

            GUILayout.Space(10);
            
            // Draggable List Zoom
            DrawDraggableZoom("List", ref _listZoom, 0.5f, 1.5f, 120);
            
            GUILayout.Space(10);
            
            // Draggable Preview Zoom
            DrawDraggableZoom("Preview", ref _previewZoom, 0.5f, 4.0f, 120);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDraggableZoom(string label, ref float value, float min, float max, float width)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.Width(width));
            
            // Draw label that acts as a draggable handle
            Rect labelRect = new Rect(rect.x, rect.y, width * 0.45f, rect.height);
            EditorGUI.LabelField(labelRect, label, EditorStyles.miniLabel);
            
            // Draggable behavior for the label
            EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.SlideArrow);
            if (Event.current.type == EventType.MouseDrag && labelRect.Contains(Event.current.mousePosition))
            {
                value = Mathf.Clamp(value + Event.current.delta.x * 0.01f, min, max);
                Event.current.Use();
                Repaint();
            }

            // The numeric field/slider part
            Rect sliderRect = new Rect(rect.x + width * 0.5f, rect.y, width * 0.5f, rect.height);
            value = EditorGUI.Slider(sliderRect, value, min, max);
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
                
                Rect rect = EditorGUILayout.BeginHorizontal(isSelected ? _selectionStyle : GUIStyle.none, GUILayout.Height(45 * _listZoom));
                GUILayout.Space(10 * _listZoom); // Side padding
                
                // Avatar thumbnail
                float thumbSize = 35 * _listZoom;
                Rect iconRect = GUILayoutUtility.GetRect(thumbSize, thumbSize);
                iconRect.y += ( (40 * _listZoom) - thumbSize) / 2f;
                DrawUnitThumbnail(iconRect, unit);
                
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                
                GUIStyle nameStyle = new GUIStyle(isSelected ? EditorStyles.whiteBoldLabel : EditorStyles.boldLabel);
                nameStyle.fontSize = (int)(12 * _listZoom);
                EditorGUILayout.LabelField($"{unit.UnitName} [{unit.Class}]", nameStyle);
                
                if (_showTitles && !string.IsNullOrEmpty(unit.UnitTitle))
                {
                    GUILayout.Space(4 * _listZoom);
                    GUIStyle titleStyle = new GUIStyle(EditorStyles.miniLabel);
                    titleStyle.fontSize = (int)(9 * _listZoom);
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
            
                int cellWidth = (int)(120 * _listZoom);
                int cellHeight = (int)(155 * _listZoom);
                
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
                            
                            if (cardRect.Contains(Event.current.mousePosition))
                            {
                                _hoveredUnit = unit;
                            }
                            
                            float thumbSize = cellWidth - 10;
                            Rect thumbnailRect = GUILayoutUtility.GetRect(thumbSize, thumbSize);
                            DrawUnitThumbnail(thumbnailRect, unit);
                            
                            GUIStyle nameStyle = new GUIStyle(EditorStyles.miniLabel);
                            nameStyle.fontSize = (int)(10 * _listZoom);
                            nameStyle.alignment = TextAnchor.MiddleCenter;
                            nameStyle.wordWrap = true;
                            
                            EditorGUILayout.LabelField(unit.UnitName, nameStyle, GUILayout.Width(cellWidth - 10));
                            
                            // Draw the Class Name in a subtle blue badge
                            GUIStyle classStyle = new GUIStyle(EditorStyles.miniLabel);
                            classStyle.fontSize = (int)(9 * _listZoom);
                            classStyle.alignment = TextAnchor.MiddleCenter;
                            classStyle.fontStyle = FontStyle.Bold;
                            classStyle.normal.textColor = new Color(0.3f, 0.7f, 1f);
                            EditorGUILayout.LabelField($"[{unit.Class}]", classStyle, GUILayout.Width(cellWidth - 10));
                            
                            if (_showTitles && !string.IsNullOrEmpty(unit.UnitTitle))
                            {
                                GUILayout.Space(4 * _listZoom);
                                GUIStyle titleStyle = new GUIStyle(EditorStyles.miniLabel);
                                titleStyle.fontSize = (int)(9 * _listZoom);
                                titleStyle.alignment = TextAnchor.MiddleCenter;
                                titleStyle.wordWrap = true;
                                titleStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                                EditorGUILayout.LabelField(unit.UnitTitle, titleStyle, GUILayout.Width(cellWidth - 10));
                            }
                            
                            // Push everything to the top
                            GUILayout.FlexibleSpace();
                            
                            EditorGUILayout.EndVertical();
                            
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
                case ThumbnailType.WaistUp: s = unit.BaseSkin.WaistUp; break;
                case ThumbnailType.FullBody: s = unit.BaseSkin.FullBodyCutout; break;
                case ThumbnailType.SplashArt: s = unit.BaseSkin.FullSplashArt; break;
            }
            if (s == null) s = unit.BaseSkin.Avatar ?? unit.BaseSkin.Chibi ?? unit.BaseSkin.WaistUp ?? unit.BaseSkin.FullBodyCutout ?? unit.BaseSkin.FullSplashArt;

            if (s != null)
            {
                GUI.DrawTexture(r, s.texture, ScaleMode.ScaleToFit);
                GUI.Label(r, new GUIContent("", unit.UnitName));
            }
            else
            {
                GUI.Box(r, new GUIContent("?", unit.UnitName));
            }
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
                
                DrawStats();
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
            
            EditorGUI.BeginChangeCheck();
            
            // Unit Name with uniqueness check
            string newName = EditorGUILayout.TextField("Unit Name", _selectedUnit.UnitName, new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, fixedHeight = 22 });
            
            bool nameExists = _allUnits.Exists(u => u != _selectedUnit && u.UnitName.Trim().Equals(newName.Trim(), System.StringComparison.OrdinalIgnoreCase));
            if (nameExists && !string.IsNullOrEmpty(newName))
            {
                EditorGUILayout.HelpBox("Warning: A unit with this name already exists!", MessageType.Warning);
            }
            
            _selectedUnit.UnitName = newName;

            // Unit Title
            _selectedUnit.UnitTitle = EditorGUILayout.TextField("Unit Title", _selectedUnit.UnitTitle, new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Italic });

            // Show Class Classification clearly in header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Class Class:", EditorStyles.miniBoldLabel, GUILayout.Width(75));
            GUIStyle headerClassStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            headerClassStyle.normal.textColor = new Color(0.2f, 0.7f, 1f);
            EditorGUILayout.LabelField(_selectedUnit.Class.ToString().ToUpper(), headerClassStyle);
            EditorGUILayout.EndHorizontal();
            
            if (EditorGUI.EndChangeCheck())
            {
                UnityEditor.EditorUtility.SetDirty(_selectedUnit);
            }
            
            EditorGUILayout.EndVertical();
            
            if (GUILayout.Button("Ping Asset", GUILayout.Width(100), GUILayout.Height(30)))
            {
                EditorGUIUtility.PingObject(_selectedUnit);
            }
            if (GUILayout.Button("Ping Image", GUILayout.Width(100), GUILayout.Height(30)))
            {
                if (_tempSplash != null)
                    EditorGUIUtility.PingObject(_tempSplash);
                else
                    Debug.LogWarning("No image currently loaded to ping.");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        private void DrawStats()
        {
            EditorGUILayout.LabelField("Unit Stats & Attributes", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Save original label width
            float originalLabelWidth = EditorGUIUtility.labelWidth;

            // Row 1: Tactical Details
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 55f;
            EditorGUILayout.LabelField("Rarity:", EditorStyles.miniBoldLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField(_selectedUnit.Rarity.ToString(), GUILayout.Width(70));
            
            EditorGUILayout.LabelField("Class:", EditorStyles.miniBoldLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField(_selectedUnit.Class.ToString(), GUILayout.Width(90));
            
            EditorGUILayout.LabelField("Deploy Cost:", EditorStyles.miniBoldLabel, GUILayout.Width(80));
            EditorGUI.BeginChangeCheck();
            int newCost = EditorGUILayout.IntField(_selectedUnit.DeploymentCost, GUILayout.Width(45));
            if (EditorGUI.EndChangeCheck())
            {
                _selectedUnit.DeploymentCost = newCost;
                EditorUtility.SetDirty(_selectedUnit);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Row 2: Combat properties
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Range:", EditorStyles.miniBoldLabel, GUILayout.Width(50));
            EditorGUI.BeginChangeCheck();
            float newRange = EditorGUILayout.FloatField(_selectedUnit.Range, GUILayout.Width(45));
            if (EditorGUI.EndChangeCheck())
            {
                _selectedUnit.Range = newRange;
                EditorUtility.SetDirty(_selectedUnit);
            }
            GUILayout.Space(25);
            EditorGUILayout.LabelField("Block Count:", EditorStyles.miniBoldLabel, GUILayout.Width(80));
            EditorGUI.BeginChangeCheck();
            int newBlock = EditorGUILayout.IntField(_selectedUnit.BlockCount, GUILayout.Width(45));
            if (EditorGUI.EndChangeCheck())
            {
                _selectedUnit.BlockCount = newBlock;
                EditorUtility.SetDirty(_selectedUnit);
            }
            GUILayout.Space(25);
            EditorGUILayout.LabelField("Atk Interval:", EditorStyles.miniBoldLabel, GUILayout.Width(80));
            EditorGUI.BeginChangeCheck();
            float newInt = EditorGUILayout.FloatField(_selectedUnit.AttackInterval, GUILayout.Width(45));
            if (EditorGUI.EndChangeCheck())
            {
                _selectedUnit.AttackInterval = newInt;
                EditorUtility.SetDirty(_selectedUnit);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Row 3: Flying & Redeploy
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Hit Flying:", EditorStyles.miniBoldLabel, GUILayout.Width(70));
            EditorGUI.BeginChangeCheck();
            bool newFly = EditorGUILayout.Toggle(_selectedUnit.CanAttackFlying, GUILayout.Width(30));
            if (EditorGUI.EndChangeCheck())
            {
                _selectedUnit.CanAttackFlying = newFly;
                EditorUtility.SetDirty(_selectedUnit);
            }
            GUILayout.Space(40);
            EditorGUILayout.LabelField("Redeploy Time:", EditorStyles.miniBoldLabel, GUILayout.Width(95));
            EditorGUI.BeginChangeCheck();
            float newResp = EditorGUILayout.FloatField(_selectedUnit.RespawnTime, GUILayout.Width(45));
            if (EditorGUI.EndChangeCheck())
            {
                _selectedUnit.RespawnTime = newResp;
                EditorUtility.SetDirty(_selectedUnit);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            
            // Draw a separator line
            Rect rect = GUILayoutUtility.GetRect(10, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            EditorGUILayout.Space(8);

            // Calculate exact column widths based on right pane size
            float availableWidth = position.width - _browserWidth - 50;
            if (availableWidth < 300) availableWidth = 350; // fallback safety
            float colWidth = availableWidth * 0.46f;

            // Double Column layout: Base Stats vs Calculated/Final Stats
            EditorGUILayout.BeginHorizontal();
            
            // Left column: Base Stats (Editable)
            EditorGUILayout.BeginVertical(GUILayout.Width(colWidth));
            EditorGUILayout.LabelField("Base Attributes", EditorStyles.miniBoldLabel);
            
            EditorGUIUtility.labelWidth = 95f;
            EditorGUI.BeginChangeCheck();
            float baseHp = EditorGUILayout.FloatField("Base Max HP", _selectedUnit.MaxHp);
            float baseAtk = EditorGUILayout.FloatField("Base Attack", _selectedUnit.AttackPower);
            float baseDef = EditorGUILayout.FloatField("Base Defense", _selectedUnit.Defense);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedUnit.MaxHp = baseHp;
                _selectedUnit.AttackPower = baseAtk;
                _selectedUnit.Defense = baseDef;
                EditorUtility.SetDirty(_selectedUnit);
                
                // Recalculate if possible
                var scaling = MaouSamaTD.Core.AppEntryPoint.LoadedScalingData;
                if (scaling == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:ClassScalingData");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        scaling = AssetDatabase.LoadAssetAtPath<ClassScalingData>(path);
                    }
                }
                if (scaling != null)
                {
                    _selectedUnit.RefreshStats(scaling);
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            // Right column: Calculated Stats (Read-Only)
            EditorGUILayout.BeginVertical(GUILayout.Width(colWidth));
            EditorGUILayout.LabelField("Final Calculated Stats", EditorStyles.miniBoldLabel);
            
            EditorGUIUtility.labelWidth = 95f;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("Final Max HP", _selectedUnit.CalculatedStats.MaxHp);
                EditorGUILayout.FloatField("Final Attack", _selectedUnit.CalculatedStats.Attack);
                EditorGUILayout.FloatField("Final Defense", _selectedUnit.CalculatedStats.Defense);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();

            // Restore label width
            EditorGUIUtility.labelWidth = originalLabelWidth;

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Recalculate Stats", EditorStyles.miniButton))
            {
                var scaling = MaouSamaTD.Core.AppEntryPoint.LoadedScalingData;
                if (scaling == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:ClassScalingData");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        scaling = AssetDatabase.LoadAssetAtPath<ClassScalingData>(path);
                    }
                }
                if (scaling != null)
                {
                    _selectedUnit.RefreshStats(scaling);
                    EditorUtility.SetDirty(_selectedUnit);
                }
            }

            EditorGUILayout.EndVertical();
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
            DrawVariantIcon("WaistUp", _selectedUnit.BaseSkin.WaistUp);
            DrawVariantIcon("FullBody", _selectedUnit.BaseSkin.FullBodyCutout);
            DrawVariantIcon("Splash", _selectedUnit.BaseSkin.FullSplashArt);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 2. Main Preview (Centered)
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            float splashWidth = position.width * 0.45f * _previewZoom;
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
                    DrawVariantIcon("WaistUp", skin.WaistUp);
                    DrawVariantIcon("FullBody", skin.FullBodyCutout);
                    DrawVariantIcon("Splash", skin.FullSplashArt);
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
                        if (Event.current.button == 0) // Left click
                        {
                            _tempSplash = tex;
                        }
                        else if (Event.current.button == 1) // Right click
                        {
                            EditorGUIUtility.PingObject(sprite);
                        }
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

        private void DrawHoverTooltip()
        {
            if (_hoveredUnit == null) return;

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
            GUI.Label(new Rect(paddingRect.x, paddingRect.y, paddingRect.width, 20), _hoveredUnit.UnitName, nameStyle);

            string subText = !string.IsNullOrEmpty(_hoveredUnit.UnitTitle) ? _hoveredUnit.UnitTitle : "Vassal Unit";
            GUIStyle subStyle = new GUIStyle(EditorStyles.miniLabel);
            subStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(paddingRect.x, paddingRect.y + 20, paddingRect.width, 16), subText, subStyle);

            GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel);
            badgeStyle.normal.textColor = GetRarityColor(_hoveredUnit.Rarity.ToString());
            badgeStyle.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(paddingRect.x, paddingRect.y + 38, paddingRect.width, 16), $"{_hoveredUnit.Rarity} | {_hoveredUnit.Class}", badgeStyle);

            Handles.EndGUI();
        }

        private Color GetRarityColor(string rarityStr)
        {
            switch (rarityStr)
            {
                case "C": return new Color(0.7f, 0.7f, 0.7f);
                case "UC": return new Color(0.4f, 0.8f, 0.4f);
                case "R": return new Color(0.4f, 0.7f, 1.0f);
                case "SR": return new Color(0.8f, 0.4f, 1.0f);
                case "SSR": return new Color(1.0f, 0.6f, 0.1f);
                case "UR": return new Color(1.0f, 0.2f, 0.3f);
                default: return Color.white;
            }
        }

        private void DrawTabsHeader()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Premium tab buttons
            GUIStyle tabStyleLeft = new GUIStyle(EditorStyles.miniButtonLeft);
            GUIStyle tabStyleRight = new GUIStyle(EditorStyles.miniButtonRight);
            
            tabStyleLeft.fontSize = 12;
            tabStyleLeft.fontStyle = FontStyle.Bold;
            tabStyleLeft.fixedHeight = 26;
            
            tabStyleRight.fontSize = 12;
            tabStyleRight.fontStyle = FontStyle.Bold;
            tabStyleRight.fixedHeight = 26;
            
            // Set text color/background for active tab
            if (_currentTab == BrowserTab.Vassals)
            {
                tabStyleLeft.normal.textColor = new Color(0.2f, 0.6f, 1.0f);
            }
            else
            {
                tabStyleRight.normal.textColor = new Color(0.2f, 0.6f, 1.0f);
            }

            if (GUILayout.Button("🛡️  COHORT REPOSITORY", tabStyleLeft, GUILayout.ExpandWidth(true)))
            {
                _currentTab = BrowserTab.Vassals;
            }
            
            if (GUILayout.Button("📖  CLASS BESTIARY", tabStyleRight, GUILayout.ExpandWidth(true)))
            {
                _currentTab = BrowserTab.Classes;
            }
            
            if (GUILayout.Button(new GUIContent("🔄", "Force Refresh Assets"), EditorStyles.miniButtonRight, GUILayout.Width(35)))
            {
                AssetDatabase.Refresh();
                RefreshData();
                InitializeClassInfo();
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }

        private void InitializeClassInfo()
        {
            if (_classes.Count > 0) return;

            _classes.Add(new ClassInfo
            {
                ClassType = UnitClass.Bastion,
                Name = "Bastion",
                Icon = "🛡️",
                Role = "Heavy Tank",
                ColorTheme = "Blue (Defense)",
                Summary = "High HP/DEF. Block Count: 3-4. Essential for holding chokepoints.",
                Description = "The absolute bulwark of Maou's legion. Bastions are designed to sit in the path of the enemy horde, soaking up immense amounts of damage and physically holding back up to 4 enemies at once. Pair them with a Sage to ensure they never fall."
            });
            _classes.Add(new ClassInfo
            {
                ClassType = UnitClass.Vanguard,
                Name = "Vanguard",
                Icon = "⚔️",
                Role = "Melee DPS",
                ColorTheme = "Red (Combat)",
                Summary = "Balanced stats. Block Count: 2. High SP regeneration while engaged.",
                Description = "Vanguards are front-line combatants who excel at both dealing and receiving steady physical damage. They have a block count of 2 and gain increased skill points (SP) or energy while actively engaged in combat, allowing them to activate their skills frequently."
            });
            _classes.Add(new ClassInfo
            {
                ClassType = UnitClass.Executioner,
                Name = "Executioner",
                Icon = "🗡️",
                Role = "Burst Assassin",
                ColorTheme = "Red (Combat)",
                Summary = "High ATK/ASPD, low HP. Best for single-target elite elimination.",
                Description = "Glass cannons of the melee battlefield. Executioners possess astronomical Attack and Attack Speed stats, but their low health and lack of defensive options make them highly vulnerable. Position them behind a Bastion or to the side of paths to shred elite hostiles."
            });
            _classes.Add(new ClassInfo
            {
                ClassType = UnitClass.Ranger,
                Name = "Ranger",
                Icon = "🏹",
                Role = "Physical Ranged",
                ColorTheme = "Red (Combat)",
                Summary = "High Ground deployment. Prioritizes Flying enemies. High range.",
                Description = "Deploys on high-ground platforms to rain down a volley of arrows or physical projectiles. Rangers have an extensive targeting range and automatically prioritize flying enemies (like Sky-Zealots) to protect the ground ranks from aerial bypass."
            });
            _classes.Add(new ClassInfo
            {
                ClassType = UnitClass.Warlock,
                Name = "Warlock",
                Icon = "🔮",
                Role = "Magical Ranged",
                ColorTheme = "Purple (Arcane)",
                Summary = "High Ground. AoE magic damage. Often applies CC (Slow/Stun).",
                Description = "Arcane spellcasters deployed on elevated tiles. Warlocks deal magical damage, ignoring standard physical armor. They often deal area-of-effect (AoE) damage and apply powerful crowd control effects, such as slows, freezes, or stuns, to entire groups of advancing enemies."
            });
            _classes.Add(new ClassInfo
            {
                ClassType = UnitClass.Sage,
                Name = "Sage",
                Icon = "⚕️",
                Role = "Healer",
                ColorTheme = "Green (Life)",
                Summary = "Restores HP to allies within range. Essential for Bastion longevity.",
                Description = "Support healers that keep your frontline alive. Sages do not attack enemies directly; instead, they target damaged allies within their range to restore their health. Placing a Sage within range of your core Bastions and Vanguards is vital for survival."
            });
            _classes.Add(new ClassInfo
            {
                ClassType = UnitClass.Architect,
                Name = "Architect",
                Icon = "🏗️",
                Role = "Fortifier",
                ColorTheme = "Blue (Defense)",
                Summary = "Deploys stationary Towers or non-blocking Traps. Immune to standard melee.",
                Description = "Battlefield engineers who build defensive structures. Architects can construct stationary defensive turrets or lay non-blocking explosive and debuffing traps. They are often immune to standard melee attacks and can rebuild structures on the fly."
            });
            _classes.Add(new ClassInfo
            {
                ClassType = UnitClass.Necromancer,
                Name = "Necromancer",
                Icon = "💀",
                Role = "Summoner",
                ColorTheme = "Purple (Arcane)",
                Summary = "Spawns temporary 'Fodder' units to increase effective Block Count.",
                Description = "Masters of the dark arts who manipulate the dead. Necromancers summon hordes of skeleton minions, ghouls, or spirits onto nearby walkable tiles. These temporary summons can block enemies, absorb high-damage attacks, and distract elites."
            });
            _classes.Add(new ClassInfo
            {
                ClassType = UnitClass.Support,
                Name = "Support",
                Icon = "📣",
                Role = "Buffer",
                ColorTheme = "Gold (Special)",
                Summary = "Provides passive auras (ATK/DEF/SP speed) to nearby allies.",
                Description = "Force multipliers that elevate your entire roster. Supports provide powerful passive auras, buffing the Attack, Defense, Attack Speed, or SP regeneration rates of all allies within their sphere of influence. Their presence turns a standard line into an unbreakable army."
            });
            _classes.Add(new ClassInfo
            {
                ClassType = UnitClass.Gunner,
                Name = "Gunner",
                Icon = "🔫",
                Role = "Rapid Fire",
                ColorTheme = "Gold (Special)",
                Summary = "Extreme ASPD. Deals True Damage but often consumes Authority per shot.",
                Description = "High-technology ranged combatants. Gunners feature unparalleled fire rates, shredding single targets with True Damage that completely bypasses both physical armor and magic resistance. However, their high-octane fire often consumes valuable tactical resources like Authority."
            });
            _classes.Add(new ClassInfo
            {
                ClassType = UnitClass.Assassin,
                Name = "Assassin",
                Icon = "👤",
                Role = "Infiltrator",
                ColorTheme = "Red (Combat)",
                Summary = "Can be placed near enemy spawns. Ignores 1 enemy to strike the backline.",
                Description = "High-mobility agents designed for deep infiltration. Assassins can be deployed directly in or near enemy spawn zones, ignoring the standard high/low ground restrictions. They can allow one standard enemy to bypass them in order to target and assassinate backline casters or high-threat targets."
            });
            _classes.Add(new ClassInfo
            {
                ClassType = UnitClass.Overlord,
                Name = "Overlord",
                Icon = "👑",
                Role = "The Ruler",
                ColorTheme = "Gold (Special)",
                Summary = "Specialized skills and high authority. The absolute commander.",
                Description = "The absolute peak of command on the battlefield. Overlords dictate the flow of the entire war, unlocking unique battlefield commands, granting widespread inspiration buffs, and managing global resources. They command respect from all ranks of Maou's legion."
            });
        }

        private void DrawClassBestiaryArea()
        {
            EditorGUILayout.BeginHorizontal();

            // --- Left Sidebar (Class List) ---
            float sidebarWidth = 200f;
            EditorGUILayout.BeginVertical(GUILayout.Width(sidebarWidth));
            
            // Sidebar header/toolbar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Classes", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent(" Force Refresh", EditorGUIUtility.IconContent("d_Refresh").image), EditorStyles.toolbarButton))
            {
                AssetDatabase.Refresh();
                RefreshData();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            _classListScroll = EditorGUILayout.BeginScrollView(_classListScroll, GUILayout.ExpandHeight(true));
            foreach (var classData in _classes)
            {
                bool isSelected = _selectedClass == classData.ClassType;
                
                // Style for items
                GUIStyle itemStyle = new GUIStyle(GUI.skin.button);
                itemStyle.alignment = TextAnchor.MiddleLeft;
                itemStyle.fontSize = 11;
                itemStyle.fixedHeight = 32;
                itemStyle.margin = new RectOffset(4, 4, 2, 2);
                itemStyle.padding = new RectOffset(8, 8, 4, 4);

                if (isSelected)
                {
                    itemStyle.normal.textColor = new Color(0.2f, 0.7f, 1.0f);
                    itemStyle.fontStyle = FontStyle.Bold;
                }

                if (GUILayout.Button($" {classData.Icon}  {classData.Name}", itemStyle, GUILayout.Width(sidebarWidth - 16)))
                {
                    _selectedClass = classData.ClassType;
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Divider vertical bar
            Rect dividerRect = GUILayoutUtility.GetRect(2, position.height, GUILayout.Width(2));
            EditorGUI.DrawRect(new Rect(dividerRect.x, 0, 2, position.height), new Color(0.15f, 0.15f, 0.15f, 0.5f));

            // --- Right Area (Class Details) ---
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            
            var selClassData = _classes.Find(c => c.ClassType == _selectedClass);
            if (selClassData != null)
            {
                _classDetailScroll = EditorGUILayout.BeginScrollView(_classDetailScroll, GUILayout.ExpandHeight(true));
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(15);
                EditorGUILayout.BeginVertical();
                GUILayout.Space(15);

                // Title header
                EditorGUILayout.BeginHorizontal();
                GUIStyle iconStyle = new GUIStyle(EditorStyles.largeLabel);
                iconStyle.fontSize = 28;
                GUILayout.Label(selClassData.Icon, iconStyle, GUILayout.Width(45));

                EditorGUILayout.BeginVertical();
                GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
                titleStyle.fontSize = 20;
                titleStyle.normal.textColor = Color.white;
                EditorGUILayout.SelectableLabel(selClassData.Name.ToUpper(), titleStyle, GUILayout.Height(30));

                GUIStyle subtitleStyle = new GUIStyle(EditorStyles.miniLabel);
                subtitleStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                EditorGUILayout.SelectableLabel($"{selClassData.Role}  |  {selClassData.ColorTheme}", subtitleStyle, GUILayout.Height(20));
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(15);

                // Tactical summary card
                EditorGUILayout.BeginVertical(GUI.skin.box);
                GUIStyle summaryTitleStyle = new GUIStyle(EditorStyles.miniBoldLabel);
                summaryTitleStyle.normal.textColor = new Color(0.9f, 0.6f, 0.2f);
                GUILayout.Label("TACTICAL SUMMARY", summaryTitleStyle);
                GUILayout.Space(4);
                
                GUIStyle summaryTextStyle = new GUIStyle(EditorStyles.label);
                summaryTextStyle.wordWrap = true;
                summaryTextStyle.fontStyle = FontStyle.Italic;
                summaryTextStyle.fontSize = 12;
                summaryTextStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
                EditorGUILayout.SelectableLabel(selClassData.Summary, summaryTextStyle, GUILayout.Height(40));
                EditorGUILayout.EndVertical();

                GUILayout.Space(15);

                // Body description
                GUIStyle descTitleStyle = new GUIStyle(EditorStyles.boldLabel);
                descTitleStyle.fontSize = 12;
                descTitleStyle.normal.textColor = Color.white;
                GUILayout.Label("Tactical Operations & Behavior", descTitleStyle);
                GUILayout.Space(4);

                GUIStyle descBodyStyle = new GUIStyle(EditorStyles.label);
                descBodyStyle.wordWrap = true;
                descBodyStyle.fontSize = 11;
                descBodyStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
                EditorGUILayout.SelectableLabel(selClassData.Description, descBodyStyle, GUILayout.Height(60));

                // --- Promotion & Rank-Up Configuration ---
                GUILayout.Space(15);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Promotion & Rank-Up Configuration", descTitleStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(" Force Refresh", EditorGUIUtility.IconContent("d_Refresh").image), GUILayout.Width(130), GUILayout.Height(25)))
                {
                    AssetDatabase.Refresh();
                    RefreshData();
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(6);

                ClassScalingData scalingData = null;
                string[] scalingGuids = AssetDatabase.FindAssets("t:ClassScalingData");
                if (scalingGuids.Length > 0)
                {
                    string scalingPath = AssetDatabase.GUIDToAssetPath(scalingGuids[0]);
                    scalingData = AssetDatabase.LoadAssetAtPath<ClassScalingData>(scalingPath);
                }

                if (scalingData != null)
                {
                    // Find the multiplier entry for this class
                    int elementIndex = -1;
                    if (scalingData.ClassScalings != null)
                    {
                        for (int i = 0; i < scalingData.ClassScalings.Length; i++)
                        {
                            if (scalingData.ClassScalings[i].ClassType == _selectedClass)
                            {
                                elementIndex = i;
                                break;
                            }
                        }
                    }

                    if (elementIndex >= 0)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        
                        SerializedObject so = new SerializedObject(scalingData);
                        SerializedProperty classScalingsProp = so.FindProperty("ClassScalings");
                        SerializedProperty scalingElementProp = classScalingsProp.GetArrayElementAtIndex(elementIndex);
                        SerializedProperty requiredMatsProp = scalingElementProp.FindPropertyRelative("RequiredMaterials");

                        EditorGUILayout.PropertyField(requiredMatsProp, new GUIContent("Promotion Required Materials"), true);

                        if (so.ApplyModifiedProperties())
                        {
                            AssetDatabase.SaveAssets();
                        }
                        
                        var reqMats = scalingData.GetRequiredMaterials(_selectedClass);
                        EditorGUILayout.Space(4);
                        
                        string info = "Cost for 2⭐ Rank-Up:\n";
                        foreach(var m in reqMats) { info += $"- {m.ItemID}: {m.BaseAmount * 2}x\n"; }
                        info += "\nCost for 6⭐ Rank-Up:\n";
                        foreach(var m in reqMats) { info += $"- {m.ItemID}: {m.BaseAmount * 6}x\n"; }
                        
                        EditorGUILayout.HelpBox(info, MessageType.Info);

                        EditorGUILayout.EndVertical();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox($"No scaling entry found for {_selectedClass} in ClassScalingData. Please initialize it in the scaling asset first.", MessageType.Warning);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("ClassScalingData asset not found in project.", MessageType.Error);
                }

                GUILayout.Space(25);

                // Vassals lists
                GUILayout.Label($"Legion Cohorts ({selClassData.Name})", descTitleStyle);
                GUILayout.Space(6);

                var vassalsInClass = _allUnits.Where(u => u.Class == _selectedClass).ToList();
                if (vassalsInClass.Count == 0)
                {
                    GUILayout.Label("No vassals currently deployed under this classification.", EditorStyles.miniLabel);
                }
                else
                {
                    // Render interactive roster grid
                    float panelWidth = position.width - sidebarWidth - 40;
                    int columns = Mathf.Max(1, Mathf.FloorToInt(panelWidth / 120f));
                    int rows = Mathf.CeilToInt((float)vassalsInClass.Count / columns);
                    
                    EditorGUILayout.BeginVertical();
                    for (int r = 0; r < rows; r++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        for (int c = 0; c < columns; c++)
                        {
                            int index = r * columns + c;
                            if (index < vassalsInClass.Count)
                            {
                                var unit = vassalsInClass[index];
                                
                                // Draw single unit card
                                Rect cardRect = EditorGUILayout.BeginVertical(GUI.skin.button, GUILayout.Width(110), GUILayout.Height(100));
                                
                                // Thumbnail
                                Rect avatarRect = new Rect(cardRect.x + 35, cardRect.y + 10, 40, 40);
                                var avatarTex = GetSpriteTexture(unit.BaseSkin.Avatar);
                                if (avatarTex != null)
                                {
                                    GUI.DrawTexture(avatarRect, avatarTex);
                                }
                                else
                                {
                                    GUI.Box(avatarRect, "?");
                                }

                                GUILayout.Space(55);
                                
                                // Label name
                                GUIStyle uNameStyle = new GUIStyle(EditorStyles.miniBoldLabel);
                                uNameStyle.alignment = TextAnchor.MiddleCenter;
                                uNameStyle.wordWrap = true;
                                GUILayout.Label(unit.UnitName, uNameStyle);
                                
                                // Rarity badge
                                GUIStyle uRarityStyle = new GUIStyle(EditorStyles.miniLabel);
                                uRarityStyle.alignment = TextAnchor.MiddleCenter;
                                uRarityStyle.fontStyle = FontStyle.Bold;
                                uRarityStyle.normal.textColor = GetRarityColor(unit.Rarity.ToString());
                                GUILayout.Label(unit.Rarity.ToString(), uRarityStyle);

                                EditorGUILayout.EndVertical();
                                
                                // Detect click to open
                                if (Event.current.type == EventType.MouseDown && cardRect.Contains(Event.current.mousePosition))
                                {
                                    _currentTab = BrowserTab.Vassals;
                                    SelectUnit(unit);
                                    ShowDetailsWithUnit(unit);
                                    Event.current.Use();
                                }
                            }
                            else
                            {
                                GUILayout.Space(110);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(15);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
    }
}
