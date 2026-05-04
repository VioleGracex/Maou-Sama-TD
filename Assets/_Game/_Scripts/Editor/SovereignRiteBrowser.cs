using UnityEngine;
using UnityEditor;
using MaouSamaTD.Skills;
using System.Collections.Generic;
using System.Linq;

namespace MaouSamaTD.Editors
{
    public class SovereignRiteBrowser : EditorWindow
    {
        private List<SovereignRiteData> _allRites = new List<SovereignRiteData>();
        private List<SovereignRiteData> _filteredRites = new List<SovereignRiteData>();
        
        private string _searchText = "";
        private int _genderFilter = 0; // 0: All, 1: Male, 2: Female
        
        private SovereignRiteData _selectedRite;
        private Vector2 _listScrollPos;
        private Vector2 _detailScrollPos;
        
        private float _sidebarWidth = 250f;
        private GUIStyle _selectedStyle;
        private GUIStyle _headerStyle;

        [MenuItem("Maou-TD/Sovereign Rite Browser")]
        public static void Open()
        {
            GetWindow<SovereignRiteBrowser>("Rite Browser");
        }

        private void OnEnable()
        {
            RefreshData();
        }

        private void RefreshData()
        {
            _allRites.Clear();
            string[] guids = AssetDatabase.FindAssets("t:SovereignRiteData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SovereignRiteData rite = AssetDatabase.LoadAssetAtPath<SovereignRiteData>(path);
                if (rite != null) _allRites.Add(rite);
            }
            _allRites = _allRites.OrderBy(r => r.SkillName).ToList();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            _filteredRites = _allRites.Where(r => 
            {
                bool matchesSearch = string.IsNullOrEmpty(_searchText) || 
                                     (r.SkillName != null && r.SkillName.ToLower().Contains(_searchText.ToLower())) || 
                                     r.name.ToLower().Contains(_searchText.ToLower());
                
                bool matchesGender = _genderFilter == 0 || 
                                     (_genderFilter == 1 && r.Archetype == MaouSamaTD.Data.MaouGender.Male) || 
                                     (_genderFilter == 2 && r.Archetype == MaouSamaTD.Data.MaouGender.Female);
                
                return matchesSearch && matchesGender;
            }).ToList();
        }

        private void OnGUI()
        {
            InitializeStyles();
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            
            // Left Sidebar
            DrawSidebar();
            
            // Right Details
            DrawDetails();
            
            EditorGUILayout.EndHorizontal();
        }

        private void InitializeStyles()
        {
            if (_selectedStyle == null)
            {
                _selectedStyle = new GUIStyle("selectionRect");
            }
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel);
                _headerStyle.fontSize = 18;
                _headerStyle.fixedHeight = 25;
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button(new GUIContent(" Refresh", EditorGUIUtility.IconContent("d_Refresh").image), EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshData();
            }

            GUILayout.Space(10);
            
            string newSearch = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            if (newSearch != _searchText)
            {
                _searchText = newSearch;
                ApplyFilter();
            }

            GUILayout.Space(10);
            
            string[] genderOptions = { "All Genders", "Male Rites", "Female Rites" };
            int newGender = EditorGUILayout.Popup(_genderFilter, genderOptions, EditorStyles.toolbarPopup, GUILayout.Width(120));
            if (newGender != _genderFilter)
            {
                _genderFilter = newGender;
                ApplyFilter();
            }

            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_sidebarWidth));
            _listScrollPos = EditorGUILayout.BeginScrollView(_listScrollPos, "box");

            bool needsCleanup = false;
            foreach (var rite in _filteredRites)
            {
                if (rite == null)
                {
                    needsCleanup = true;
                    continue;
                }

                bool isSelected = _selectedRite == rite;
                Rect rect = EditorGUILayout.BeginHorizontal(isSelected ? _selectedStyle : GUIStyle.none, GUILayout.Height(40));
                
                // Icon
                Rect iconRect = GUILayoutUtility.GetRect(32, 32);
                iconRect.y += 4;
                iconRect.x += 4;
                if (rite.Icon != null)
                    GUI.DrawTexture(iconRect, rite.Icon.texture, ScaleMode.ScaleToFit);
                else
                    GUI.Box(iconRect, "");

                GUILayout.Space(5);
                
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(string.IsNullOrEmpty(rite.SkillName) ? rite.name : rite.SkillName, isSelected ? EditorStyles.whiteLabel : EditorStyles.label);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(rite.SealCost + " Seals", EditorStyles.miniLabel, GUILayout.Width(60));
                GUIStyle genderTagStyle = new GUIStyle(EditorStyles.miniLabel);
                genderTagStyle.normal.textColor = rite.Archetype == MaouSamaTD.Data.MaouGender.Male ? Color.cyan : Color.magenta;
                EditorGUILayout.LabelField($"[{rite.Archetype}]", genderTagStyle);
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    _selectedRite = rite;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }

                EditorGUILayout.EndHorizontal();
            }

            if (needsCleanup)
            {
                _allRites.RemoveAll(r => r == null);
                _filteredRites.RemoveAll(r => r == null);
                if (_selectedRite == null) _selectedRite = null; // Ensure destroyed reference is cleared
                Repaint();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawDetails()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (_selectedRite == null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Select a Sovereign Rite to view details", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 14 });
                GUILayout.FlexibleSpace();
            }
            else
            {
                _detailScrollPos = EditorGUILayout.BeginScrollView(_detailScrollPos);
                
                // Header
                EditorGUILayout.BeginHorizontal();
                if (_selectedRite.Icon != null)
                    GUILayout.Label(_selectedRite.Icon.texture, GUILayout.Width(64), GUILayout.Height(64));
                
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(string.IsNullOrEmpty(_selectedRite.SkillName) ? _selectedRite.name : _selectedRite.SkillName, _headerStyle);
                EditorGUILayout.LabelField(_selectedRite.name, EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Ping Asset", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    EditorGUIUtility.PingObject(_selectedRite);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(10);
                
                // Description
                EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(_selectedRite.Description, MessageType.None);
                
                EditorGUILayout.Space(10);
                
                // Stats Row
                EditorGUILayout.BeginHorizontal();
                DrawStatBox("Archetype", _selectedRite.Archetype.ToString());
                DrawStatBox("Seal Cost", _selectedRite.SealCost.ToString());
                DrawStatBox("Cooldown", _selectedRite.Cooldown + "s");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                DrawStatBox("Radius", _selectedRite.Radius.ToString());
                DrawStatBox("Target", _selectedRite.TargetType.ToString());
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(10);
                
                // Effect Details
                EditorGUILayout.LabelField("Effect Info", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Effect Type: " + _selectedRite.EffectType);
                EditorGUILayout.LabelField("Persistence: " + _selectedRite.Persistence);
                EditorGUILayout.LabelField("Base Value: " + _selectedRite.Value);
                EditorGUILayout.LabelField("Duration: " + _selectedRite.Duration + "s");
                EditorGUILayout.EndVertical();

                if (_selectedRite.Modifiers != null && _selectedRite.Modifiers.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Stat Modifiers", EditorStyles.miniBoldLabel);
                    foreach (var mod in _selectedRite.Modifiers)
                    {
                        EditorGUILayout.LabelField($"• {mod.Stat}: {mod.Value}%", EditorStyles.miniLabel);
                    }
                }

                EditorGUILayout.Space(10);
                
                // Visuals
                EditorGUILayout.LabelField("Visual References", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                DrawObjectField("Cast VFX", _selectedRite.BaseVisuals.CastVFX);
                DrawObjectField("Hit VFX", _selectedRite.BaseVisuals.HitVFX);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                DrawObjectField("Buff VFX", _selectedRite.BuffVFXPrefab);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawStatBox(string label, string value)
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(100));
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(90));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel, GUILayout.Width(90));
            EditorGUILayout.EndVertical();
        }

        private void DrawObjectField(string label, Object obj)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            EditorGUILayout.ObjectField(obj, typeof(Object), false);
            EditorGUILayout.EndVertical();
        }
    }
}
