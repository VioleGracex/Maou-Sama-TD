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
            if (string.IsNullOrEmpty(_searchText))
            {
                _filteredUnits = new List<UnitData>(_allUnits);
            }
            else
            {
                _filteredUnits = _allUnits.Where(u => 
                    u.UnitName.ToLower().Contains(_searchText.ToLower()) || 
                    u.UnitTitle.ToLower().Contains(_searchText.ToLower())).ToList();
            }
            ApplySort();
            _currentPage = 0;
        }

        private void ApplySort()
        {
            switch (_sortMode)
            {
                case SortMode.Name:
                    _filteredUnits = _filteredUnits.OrderBy(u => u.UnitName).ToList();
                    break;
                case SortMode.Rarity:
                    _filteredUnits = _filteredUnits.OrderByDescending(u => u.Rarity).ThenBy(u => u.UnitName).ToList();
                    break;
                case SortMode.Class:
                    _filteredUnits = _filteredUnits.OrderBy(u => u.Class).ThenBy(u => u.UnitName).ToList();
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

            GUILayout.FlexibleSpace();

            // Sort Mode Option
            GUILayout.Label("Sort:", GUILayout.Width(32));
            SortMode newSort = (SortMode)EditorGUILayout.EnumPopup(_sortMode, GUILayout.Width(65));
            if (newSort != _sortMode)
            {
                _sortMode = newSort;
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
                
                Rect rect = EditorGUILayout.BeginHorizontal(isSelected ? _selectionStyle : GUIStyle.none, GUILayout.Height(40));
                
                // Avatar thumbnail
                Rect iconRect = GUILayoutUtility.GetRect(35, 35);
                iconRect.y += 2.5f;
                DrawUnitThumbnail(iconRect, unit);
                
                EditorGUILayout.BeginVertical();
                GUILayout.Space(5);
                EditorGUILayout.LabelField(unit.UnitName, isSelected ? EditorStyles.whiteBoldLabel : EditorStyles.boldLabel);
                EditorGUILayout.LabelField(unit.UnitTitle, EditorStyles.miniLabel);
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
            
            int cellWidth = 120;
            int cellHeight = 155;
            
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
                        
                        Rect thumbnailRect = GUILayoutUtility.GetRect(cellWidth - 10, cellWidth - 10);
                        DrawUnitThumbnail(thumbnailRect, unit);
                        
                        EditorGUILayout.LabelField(unit.UnitName, EditorStyles.miniLabel, GUILayout.Width(cellWidth - 5));

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
            EditorGUILayout.BeginHorizontal();
            
            // Full Splash Art (Lazy Loaded)
            float splashWidth = position.width * 0.35f;
            Rect splashRect = GUILayoutUtility.GetRect(splashWidth, splashWidth * 1.5f, GUILayout.Width(splashWidth));
            if (_tempSplash != null)
                GUI.DrawTexture(splashRect, _tempSplash, ScaleMode.ScaleToFit);
            else
                GUI.Box(splashRect, "No Splash Loaded");

            EditorGUILayout.BeginVertical();
            
            // Base Skin explicitly drawn
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Default Base Visuals", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            DrawVariantIcon("Avatar", _selectedUnit.BaseSkin.Avatar);
            DrawVariantIcon("Chibi", _selectedUnit.BaseSkin.Chibi);
            DrawVariantIcon("Portrait", _selectedUnit.BaseSkin.WaistUp);
            DrawVariantIcon("Full Body", _selectedUnit.BaseSkin.FullBodyCutout);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("View Details", EditorStyles.miniButton))
            {
                _tempSplash = _selectedUnit.BaseSkin.FullSplashArt?.texture 
                    ?? _selectedUnit.BaseSkin.FullBodyCutout?.texture
                    ?? _selectedUnit.BaseSkin.WaistUp?.texture
                    ?? _selectedUnit.BaseSkin.Chibi?.texture;
            }
            EditorGUILayout.EndVertical();

            // Variants List
            if (_selectedUnit.Skins != null)
            {
                EditorGUILayout.LabelField("Skin / Art Variants", EditorStyles.miniBoldLabel);
                foreach (var skin in _selectedUnit.Skins)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(skin.SkinThemeName, EditorStyles.miniBoldLabel);
                    
                    EditorGUILayout.BeginHorizontal();
                    DrawVariantIcon("Avatar", skin.Avatar);
                    DrawVariantIcon("Chibi", skin.Chibi);
                    DrawVariantIcon("Portrait", skin.WaistUp);
                    EditorGUILayout.EndHorizontal();
                    
                    if (GUILayout.Button("View Full Splash", EditorStyles.miniButton)) 
                        _tempSplash = skin.FullSplashArt != null ? skin.FullSplashArt.texture : null;
                    
                    EditorGUILayout.EndVertical();
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        private void DrawVariantIcon(string name, Sprite sprite)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(50));
            Rect r = GUILayoutUtility.GetRect(50, 50);
            if (sprite != null) 
            {
                GUI.DrawTexture(r, sprite.texture, ScaleMode.ScaleToFit);
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    _tempSplash = sprite.texture;
                    Event.current.Use();
                }
            }
            else GUI.Box(r, "N/A");
            EditorGUILayout.LabelField(name, EditorStyles.miniLabel, GUILayout.Width(50), GUILayout.Height(15));
            EditorGUILayout.EndVertical();
        }

        private void DrawLore()
        {
            EditorGUILayout.LabelField("Lore & Story", EditorStyles.boldLabel);
            string lore = GetLoreForUnit(_selectedUnit);
            
            GUIStyle loreStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            loreStyle.fontSize = 12;
            loreStyle.padding = new RectOffset(10, 10, 10, 10);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(lore, loreStyle);
            EditorGUILayout.EndVertical();
        }

        private void SelectUnit(UnitData unit)
        {
            _selectedUnit = unit;
            _tempSplash = null;
            
            // Lazy load first art variant appropriately
            if (unit.BaseSkin.FullSplashArt != null)
                _tempSplash = unit.BaseSkin.FullSplashArt.texture;
            else if (unit.BaseSkin.FullBodyCutout != null)
                _tempSplash = unit.BaseSkin.FullBodyCutout.texture;
            else if (unit.BaseSkin.WaistUp != null)
                _tempSplash = unit.BaseSkin.WaistUp.texture;
            else if (unit.BaseSkin.Chibi != null)
                _tempSplash = unit.BaseSkin.Chibi.texture;
            else if (unit.Skins != null && unit.Skins.Count > 0)
                _tempSplash = unit.Skins[0].FullSplashArt?.texture;
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
