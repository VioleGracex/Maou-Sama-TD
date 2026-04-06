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
            _currentPage = 0;
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
                DrawDetailsArea();

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

            // View Mode Toggles
            bool isList = _currentViewMode == ViewMode.List;
            if (GUILayout.Toggle(isList, EditorGUIUtility.IconContent("d_RectTransform Icon"), EditorStyles.toolbarButton, GUILayout.Width(35))) 
                _currentViewMode = ViewMode.List;
            
            bool isGrid = _currentViewMode == ViewMode.Grid;
            if (GUILayout.Toggle(isGrid, EditorGUIUtility.IconContent("d_LayoutElement Icon"), EditorStyles.toolbarButton, GUILayout.Width(35))) 
                _currentViewMode = ViewMode.Grid;

            GUILayout.Space(10);
            
            // Details Toggle
            _showDetails = GUILayout.Toggle(_showDetails, _showDetails ? "Hide Details" : "Show Details", EditorStyles.toolbarButton, GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBrowserArea()
        {
            float width = _currentViewMode == ViewMode.List ? 280 : 450;
            EditorGUILayout.BeginVertical(GUILayout.Width(width));
            
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
            
            int columns = Mathf.FloorToInt((containerWidth - 20) / 90);
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
                        
                        Rect cardRect = EditorGUILayout.BeginVertical(isSelected ? _selectionStyle : _cardStyle, GUILayout.Width(80), GUILayout.Height(100));
                        
                        Rect thumbnailRect = GUILayoutUtility.GetRect(70, 70);
                        DrawUnitThumbnail(thumbnailRect, unit);
                        
                        EditorGUILayout.LabelField(unit.UnitName, EditorStyles.miniLabel, GUILayout.Width(75));

                        // Overlay button for selection
                        if (GUI.Button(cardRect, "", GUIStyle.none))
                        {
                            SelectUnit(unit);
                        }
                        
                        EditorGUILayout.EndVertical();
                    }
                    else
                    {
                        GUILayout.Space(80);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawUnitThumbnail(Rect r, UnitData unit)
        {
            var avatar = unit.BaseSkin.Avatar;
            if (avatar != null)
                GUI.DrawTexture(r, avatar.texture, ScaleMode.ScaleToFit);
            else
                GUI.Box(r, "?");
        }

        private void DrawPagination()
        {
            int totalPages = Mathf.CeilToInt((float)_filteredUnits.Count / _itemsPerPage);
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("<", EditorStyles.toolbarButton) && _currentPage > 0) _currentPage--;
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"{_currentPage + 1} / {Mathf.Max(1, totalPages)}", EditorStyles.miniLabel);
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
            if (sprite != null) GUI.DrawTexture(r, sprite.texture, ScaleMode.ScaleToFit);
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
            
            // Lazy load first art variant
            if (unit.BaseSkin.FullSplashArt != null)
                _tempSplash = unit.BaseSkin.FullSplashArt.texture;
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
