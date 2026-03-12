using UnityEngine;
using UnityEditor;
using MaouSamaTD.Levels;
using System.Collections.Generic;

namespace MaouSamaTD.Editor
{
    [CustomEditor(typeof(MapData))]
    public class MapDataEditor : UnityEditor.Editor
    {
        private const float MaxCellSize = 25f;
        private const float CellPadding = 1f;
        private const float LabelSpace = 20f;

        private int _selectedTab = 0;
        private string[] _tabNames = { "Layout", "Visuals" };
        private Vector2 _scrollPosition;
        
        private static Texture2D s_TextureClipboard;
        
        [System.Serializable]
        private struct SelectionItem
        {
            public SelectionType Type;
            public Vector2Int TileCoord;
            public WallSide WallSide;
            public int WallIndex;

            public bool Equals(SelectionItem other)
            {
                if (Type != other.Type) return false;
                if (Type == SelectionType.Tile) return TileCoord == other.TileCoord;
                return WallSide == other.WallSide && WallIndex == other.WallIndex;
            }
        }

        private List<SelectionItem> _selection = new List<SelectionItem>();
        private SelectionItem _lastSelectedItem = new SelectionItem { Type = (SelectionType)(-1) };
        private enum SelectionType { Tile, Wall }

        private Dictionary<string, bool> _decoFoldouts = new Dictionary<string, bool>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            MapData data = (MapData)target;

            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            EditorGUILayout.Space();

            if (_selectedTab == 0)
            {
                DrawLayoutTab(data);
            }
            else
            {
                DrawVisualsTab(data);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLayoutTab(MapData data)
        {
            if (!data.UseManualLayout)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("MapSeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("HighGroundChance"));
            }
            
            EditorGUILayout.LabelField("Map Dimensions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Width"), new GUIContent("Width", "The horizontal size of the map (X axis)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Height"), new GUIContent("Height", "The vertical size of the map (Y axis)"));
            
            if (data.UseManualLayout)
            {
                EditorGUILayout.HelpBox("Changing dimensions while using Manual Layout will resize the grid. Tiles outside the new bounds will still be saved but won't be visible or editable in the preview.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("UseManualLayout"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Wall Toggles", EditorStyles.boldLabel);
            SerializedProperty wallsProp = serializedObject.FindProperty("Walls");
            
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 20;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("North"), new GUIContent("N"), GUILayout.Width(40));
            EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("South"), new GUIContent("S"), GUILayout.Width(40));
            EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("East"), new GUIContent("E"), GUILayout.Width(40));
            EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("West"), new GUIContent("W"), GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("NW"), new GUIContent("NW"), GUILayout.Width(40));
            EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("NE"), new GUIContent("NE"), GUILayout.Width(40));
            EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("SW"), new GUIContent("SW"), GUILayout.Width(40));
            EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("SE"), new GUIContent("SE"), GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = oldLabelWidth;
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("WallCascadeOnHoles"), new GUIContent("Wall Cascade On Holes"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Map Preview & Interactive Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Click tiles to cycle types. In 'Visuals' tab, click walls to customize them individually.", MessageType.Info);

            if (data.Width <= 0 || data.Height <= 0)
            {
                EditorGUILayout.HelpBox("Width and Height must be greater than 0 for preview.", MessageType.Warning);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition); // Start ScrollView
            DrawMapPreview(data, true);
            DrawPalette(data);
            DrawSpawnPointConfig(data);
            EditorGUILayout.EndScrollView(); // End ScrollView
            
            // Layout Tools
            EditorGUILayout.LabelField("Layout Tools", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Flip Horizontal")) Flip(data, true); 
            if (GUILayout.Button("Flip Vertical")) Flip(data, false);
            if (GUILayout.Button("Rotate 90 CW")) Rotate(data);
            if (GUILayout.Button("Refresh View")) { EditorUtility.SetDirty(data); GUI.changed = true; }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Shift N")) Shift(data, 1, 0);
            if (GUILayout.Button("Shift S")) Shift(data, -1, 0);
            if (GUILayout.Button("Shift E")) Shift(data, 0, -1);
            if (GUILayout.Button("Shift W")) Shift(data, 0, 1);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Manual Layout"))
            {
                if (EditorUtility.DisplayDialog("Clear Layout", "Are you sure you want to clear the manual layout?", "Yes", "No"))
                {
                    Undo.RecordObject(data, "Clear Manual Layout");
                    data.ManualLayoutData.Clear();
                    data.UseManualLayout = false;
                    EditorUtility.SetDirty(data);
                }
            }
            if (GUILayout.Button("Capture Random to Manual"))
            {
                CaptureRandomToManual(data);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void DrawVisualsTab(MapData data)
        {
            EditorGUILayout.LabelField("Tile & Wall Visual Customization", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select a tile or a boundary wall segment to override its texture or add decorations.", MessageType.Info);

            if (data.Width <= 0 || data.Height <= 0)
            {
                EditorGUILayout.HelpBox("Width and Height must be greater than 0 for preview.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Global Wall Visuals", EditorStyles.boldLabel);
            SerializedProperty wallVisualsProp = serializedObject.FindProperty("WallVisuals");
            EditorGUILayout.PropertyField(wallVisualsProp.FindPropertyRelative("WallPrefab"));
            EditorGUILayout.PropertyField(wallVisualsProp.FindPropertyRelative("WallMaterial"));
            EditorGUILayout.PropertyField(wallVisualsProp.FindPropertyRelative("WallScale"), new GUIContent("Wall Scale (X=Thick, Y=Height, Z=Length)"));
            EditorGUILayout.PropertyField(wallVisualsProp.FindPropertyRelative("WallOffset"), new GUIContent("Wall Global Offset"));
            EditorGUILayout.PropertyField(wallVisualsProp.FindPropertyRelative("SeamlessCorners"), new GUIContent("Seamless Wall Corners (Fix Gaps)"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Side Overrides", EditorStyles.boldLabel);
            DrawSideOverrides(data);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Bulk Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All Wall Textures"))
            {
                if (EditorUtility.DisplayDialog("Clear Wall Textures", "Are you sure you want to clear ALL wall texture overrides?", "Yes", "No"))
                {
                    Undo.RecordObject(data, "Clear All Wall Textures");
                    // Clear side-wide textures
                    for (int i = 0; i < data.SideVisualOverrides.Count; i++) {
                        var so = data.SideVisualOverrides[i];
                        so.TextureOverride = null;
                        data.SideVisualOverrides[i] = so;
                    }
                    // Clear individual wall textures and remove if empty
                    for (int i = data.WallOverrides.Count - 1; i >= 0; i--) {
                        var wo = data.WallOverrides[i];
                        wo.TextureOverride = null;
                        
                        bool hasDecorations = wo.Decorations != null && wo.Decorations.Count > 0;
                        if (!wo.OverrideScale && !wo.OverrideOffset && !hasDecorations) {
                            data.WallOverrides.RemoveAt(i);
                        } else {
                            data.WallOverrides[i] = wo;
                        }
                    }
                    EditorUtility.SetDirty(data);
                }
            }
            if (GUILayout.Button("Clear All Floor Textures"))
            {
                if (EditorUtility.DisplayDialog("Clear Tile Textures", "Are you sure you want to clear ALL tile texture overrides?", "Yes", "No"))
                {
                    Undo.RecordObject(data, "Clear All Tile Textures");
                    for (int i = data.VisualOverrides.Count - 1; i >= 0; i--) {
                        var to = data.VisualOverrides[i];
                        to.Texture = null;

                        bool hasDecorations = to.Decorations != null && to.Decorations.Count > 0;
                        if (!hasDecorations) {
                            data.VisualOverrides.RemoveAt(i);
                        } else {
                            data.VisualOverrides[i] = to;
                        }
                    }
                    EditorUtility.SetDirty(data);
                }
            }
            if (GUILayout.Button("Refresh View")) { EditorUtility.SetDirty(data); GUI.changed = true; }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            DrawMapPreview(data, true);

            EditorGUILayout.Space();

            if (_selection.Count == 0)
            {
                EditorGUILayout.HelpBox("Click a tile or wall segment above to customize.", MessageType.None);
            }
            else if (_selection.Count == 1)
            {
                var sel = _selection[0];
                if (sel.Type == SelectionType.Tile) DrawTileCustomizer(data, sel.TileCoord);
                else DrawWallCustomizer(data, sel.WallSide, sel.WallIndex);
            }
            else
            {
                DrawBatchCustomizer(data);
            }
        }

        private void DrawSideOverrides(MapData data)
        {
            SerializedProperty sideOverridesProp = serializedObject.FindProperty("SideVisualOverrides");
            System.Array sides = System.Enum.GetValues(typeof(WallSide));

            foreach (WallSide side in sides)
            {
                int idx = data.SideVisualOverrides.FindIndex(o => o.Side == side);
                if (idx == -1)
                {
                    data.SideVisualOverrides.Add(new SideVisualOverride { Side = side });
                    serializedObject.Update();
                    idx = data.SideVisualOverrides.Count - 1;
                }
                
                SerializedProperty p = sideOverridesProp.GetArrayElementAtIndex(idx);
                SerializedProperty overScaleProp = p.FindPropertyRelative("OverrideScale");
                SerializedProperty scaleProp = p.FindPropertyRelative("Scale");
                SerializedProperty overOffsetProp = p.FindPropertyRelative("OverrideOffset");
                SerializedProperty offsetProp = p.FindPropertyRelative("Offset");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(side.ToString(), EditorStyles.boldLabel, GUILayout.Width(60));
                
                EditorGUILayout.PropertyField(overScaleProp, new GUIContent("Scale"), GUILayout.Width(60));
                EditorGUI.BeginDisabledGroup(!overScaleProp.boolValue);
                EditorGUILayout.PropertyField(scaleProp, GUIContent.none);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("", GUILayout.Width(60));
                EditorGUILayout.PropertyField(overOffsetProp, new GUIContent("Offset"), GUILayout.Width(60));
                EditorGUI.BeginDisabledGroup(!overOffsetProp.boolValue);
                EditorGUILayout.PropertyField(offsetProp, GUIContent.none);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("", GUILayout.Width(60));
                EditorGUILayout.PropertyField(p.FindPropertyRelative("TextureOverride"), new GUIContent("Texture"), true);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawBatchCustomizer(MapData data)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorUtility.SetDirty(data);
            EditorGUILayout.LabelField($"Batch Editing ({_selection.Count} items)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Applying a texture or decoration here will be added to ALL selected items.", MessageType.Info);

            // Determine if all selected items have the same texture
            Texture2D commonTexture = null;
            bool first = true;
            bool multipleValues = false;

            foreach (var sel in _selection)
            {
                Texture2D currentTex = null;
                if (sel.Type == SelectionType.Tile)
                {
                    int idx = data.VisualOverrides.FindIndex(o => o.Coordinate == sel.TileCoord);
                    if (idx != -1) currentTex = data.VisualOverrides[idx].Texture;
                }
                else
                {
                    int idx = data.WallOverrides.FindIndex(o => o.Side == sel.WallSide && o.Index == sel.WallIndex);
                    if (idx != -1) currentTex = data.WallOverrides[idx].TextureOverride;
                }

                if (first)
                {
                    commonTexture = currentTex;
                    first = false;
                }
                else if (currentTex != commonTexture)
                {
                    multipleValues = true;
                    break;
                }
            }

            EditorGUI.showMixedValue = multipleValues;
            EditorGUI.BeginChangeCheck();
            Texture2D newBatchTexture = (Texture2D)EditorGUILayout.ObjectField("Apply Texture to All", commonTexture, typeof(Texture2D), false);
            
            EditorGUILayout.BeginHorizontal();
            bool overS = EditorGUILayout.ToggleLeft("Scale", false, GUILayout.Width(60));
            Vector3 newScale = Vector3.one;
            if (overS) newScale = EditorGUILayout.Vector3Field("", Vector3.one);
            else EditorGUILayout.LabelField("(Default)");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool overO = EditorGUILayout.ToggleLeft("Offset", false, GUILayout.Width(60));
            Vector3 newOffset = Vector3.zero;
            if (overO) newOffset = EditorGUILayout.Vector3Field("", Vector3.zero);
            else EditorGUILayout.LabelField("(Default)");
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(data, "Batch Apply Texture/Size");
                foreach (var sel in _selection)
                {
                    if (sel.Type == SelectionType.Tile) ApplyTileTexture(data, sel.TileCoord, newBatchTexture);
                    else ApplyWallOverride(data, sel.WallSide, sel.WallIndex, newBatchTexture, overS, newScale, overO, newOffset);
                }
                EditorUtility.SetDirty(data);
            }
            EditorGUI.showMixedValue = false;

            EditorGUILayout.Space(5);
            if (GUILayout.Button("+ Add Decoration to All Selected", GUILayout.Height(30)))
            {
                Undo.RecordObject(data, "Batch Add Decoration");
                foreach (var sel in _selection)
                {
                    if (sel.Type == SelectionType.Tile) AddTileDecoration(data, sel.TileCoord);
                    else AddWallDecoration(data, sel.WallSide, sel.WallIndex);
                }
                EditorUtility.SetDirty(data);
            }

            if (GUILayout.Button("Clear Overrides for All Selected", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear All Selected", $"Are you sure you want to clear overrides for all {_selection.Count} selected items?", "Yes", "No"))
                {
                    Undo.RecordObject(data, "Batch Clear Overrides");
                    foreach (var sel in _selection)
                    {
                        if (sel.Type == SelectionType.Tile) data.VisualOverrides.RemoveAll(o => o.Coordinate == sel.TileCoord);
                        else data.WallOverrides.RemoveAll(o => o.Side == sel.WallSide && o.Index == sel.WallIndex);
                    }
                    EditorUtility.SetDirty(data);
                }
            }

            if (GUILayout.Button("Deselect All", GUILayout.Height(20))) _selection.Clear();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All Tiles"))
            {
                _selection.Clear();
                for (int x = 0; x < data.Width; x++)
                    for (int y = 0; y < data.Height; y++)
                        _selection.Add(new SelectionItem { Type = SelectionType.Tile, TileCoord = new Vector2Int(x, y) });
            }
            if (GUILayout.Button("Select All Walls"))
            {
                _selection.Clear();
                // North/South
                for (int x = -1; x <= data.Width; x++) {
                    _selection.Add(new SelectionItem { Type = SelectionType.Wall, WallSide = WallSide.North, WallIndex = x });
                    _selection.Add(new SelectionItem { Type = SelectionType.Wall, WallSide = WallSide.South, WallIndex = x });
                }
                // East/West
                for (int y = 0; y < data.Height; y++) {
                    _selection.Add(new SelectionItem { Type = SelectionType.Wall, WallSide = WallSide.East, WallIndex = y });
                    _selection.Add(new SelectionItem { Type = SelectionType.Wall, WallSide = WallSide.West, WallIndex = y });
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ApplyTileTexture(MapData data, Vector2Int coord, Texture2D tex)
        {
            int idx = data.VisualOverrides.FindIndex(o => o.Coordinate == coord);
            if (idx != -1)
            {
                var o = data.VisualOverrides[idx];
                o.Texture = tex;
                data.VisualOverrides[idx] = o;
            }
            else data.VisualOverrides.Add(new TileVisualOverride { Coordinate = coord, Texture = tex, Decorations = new List<DecorationData>() });
        }

        private void ApplyWallOverride(MapData data, WallSide side, int index, Texture2D tex, bool overS, Vector3 s, bool overO, Vector3 oPos)
        {
            int idx = data.WallOverrides.FindIndex(o => o.Side == side && o.Index == index);
            if (idx != -1)
            {
                var o = data.WallOverrides[idx];
                if (tex != null) o.TextureOverride = tex;
                if (overS) { o.OverrideScale = true; o.Scale = s; }
                if (overO) { o.OverrideOffset = true; o.Offset = oPos; }
                data.WallOverrides[idx] = o;
            }
            else
            {
                data.WallOverrides.Add(new WallVisualOverride { 
                    Side = side, Index = index, TextureOverride = tex, 
                    OverrideScale = overS, Scale = s, 
                    OverrideOffset = overO, Offset = oPos, 
                    Decorations = new List<DecorationData>() 
                });
            }
        }

        private void AddTileDecoration(MapData data, Vector2Int coord)
        {
            int idx = data.VisualOverrides.FindIndex(o => o.Coordinate == coord);
            if (idx != -1)
            {
                if (data.VisualOverrides[idx].Decorations == null) {
                    var o = data.VisualOverrides[idx];
                    o.Decorations = new List<DecorationData>();
                    data.VisualOverrides[idx] = o;
                }
                data.VisualOverrides[idx].Decorations.Add(DecorationData.Default);
            }
            else data.VisualOverrides.Add(new TileVisualOverride { Coordinate = coord, Decorations = new List<DecorationData> { DecorationData.Default } });
        }

        private void AddWallDecoration(MapData data, WallSide side, int index)
        {
            int idx = data.WallOverrides.FindIndex(o => o.Side == side && o.Index == index);
            if (idx != -1)
            {
                if (data.WallOverrides[idx].Decorations == null) {
                    var o = data.WallOverrides[idx];
                    o.Decorations = new List<DecorationData>();
                    data.WallOverrides[idx] = o;
                }
                data.WallOverrides[idx].Decorations.Add(DecorationData.Default);
            }
            else data.WallOverrides.Add(new WallVisualOverride { Side = side, Index = index, Decorations = new List<DecorationData> { DecorationData.Default } });
        }

        private void DrawWallCustomizer(MapData data, WallSide side, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Customizing Wall: {side} Segment {index}", EditorStyles.boldLabel);

            int overrideIndex = -1;
            for (int i = 0; i < data.WallOverrides.Count; i++)
            {
                if (data.WallOverrides[i].Side == side && data.WallOverrides[i].Index == index)
                {
                    overrideIndex = i;
                    break;
                }
            }

            SerializedProperty wallOverridesProp = serializedObject.FindProperty("WallOverrides");
            SerializedProperty overrideProp = null;
            if (overrideIndex != -1) overrideProp = wallOverridesProp.GetArrayElementAtIndex(overrideIndex);

            EditorGUI.BeginChangeCheck();

            if (overrideProp != null)
            {
                EditorGUILayout.PropertyField(overrideProp.FindPropertyRelative("TextureOverride"), new GUIContent("Wall Texture Override"));
                
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                SerializedProperty overScaleProp = overrideProp.FindPropertyRelative("OverrideScale");
                SerializedProperty overOffsetProp = overrideProp.FindPropertyRelative("OverrideOffset");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(overScaleProp, new GUIContent("Scale"), GUILayout.Width(100));
                EditorGUI.BeginDisabledGroup(!overScaleProp.boolValue);
                EditorGUILayout.PropertyField(overrideProp.FindPropertyRelative("Scale"), GUIContent.none);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(overOffsetProp, new GUIContent("Offset"), GUILayout.Width(100));
                EditorGUI.BeginDisabledGroup(!overOffsetProp.boolValue);
                EditorGUILayout.PropertyField(overrideProp.FindPropertyRelative("Offset"), GUIContent.none);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                DrawDecorationsList(data, overrideProp.FindPropertyRelative("Decorations"), $"wall_{side}_{index}");
            }
            else
            {
                Texture2D tex = (Texture2D)EditorGUILayout.ObjectField("Wall Texture Override", null, typeof(Texture2D), false);
                
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                bool overS = EditorGUILayout.ToggleLeft("Scale", false, GUILayout.Width(100));
                Vector3 s = Vector3.one;
                if (overS) s = EditorGUILayout.Vector3Field("", Vector3.one);
                else EditorGUILayout.LabelField("(Default)", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                bool overO = EditorGUILayout.ToggleLeft("Offset", false, GUILayout.Width(100));
                Vector3 oPos = Vector3.zero;
                if (overO) oPos = EditorGUILayout.Vector3Field("", Vector3.zero);
                else EditorGUILayout.LabelField("(Default)", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                if (tex != null || overS || overO)
                {
                    Undo.RecordObject(data, "Add Wall Override");
                    data.WallOverrides.Add(new WallVisualOverride { 
                        Side = side, Index = index, TextureOverride = tex, 
                        OverrideScale = overS, Scale = s,
                        OverrideOffset = overO, Offset = oPos,
                        Decorations = new List<DecorationData>() 
                    });
                    EditorUtility.SetDirty(data);
                    serializedObject.Update();
                    return;
                }
                
                if (GUILayout.Button("+ Add Decoration to Wall Segment"))
                {
                    Undo.RecordObject(data, "Add Wall Decoration");
                    data.WallOverrides.Add(new WallVisualOverride { 
                        Side = side, Index = index, 
                        Decorations = new List<DecorationData> { DecorationData.Default } 
                    });
                    EditorUtility.SetDirty(data);
                    serializedObject.Update();
                }
            }

            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();

            if (overrideIndex != -1)
            {
                EditorGUILayout.Space(10);
                GUI.backgroundColor = new Color(1, 0.5f, 0.5f);
                if (GUILayout.Button("Remove All Overrides for this Wall Segment"))
                {
                    Undo.RecordObject(data, "Remove Wall Override");
                    data.WallOverrides.RemoveAt(overrideIndex);
                    EditorUtility.SetDirty(data);
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTileCustomizer(MapData data, Vector2Int coord)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Customizing Tile ({coord.x}, {coord.y})", EditorStyles.boldLabel);

            int overrideIndex = -1;
            for (int i = 0; i < data.VisualOverrides.Count; i++)
            {
                if (data.VisualOverrides[i].Coordinate == coord)
                {
                    overrideIndex = i;
                    break;
                }
            }

            SerializedProperty visualOverridesProp = serializedObject.FindProperty("VisualOverrides");
            SerializedProperty overrideProp = null;
            if (overrideIndex != -1) overrideProp = visualOverridesProp.GetArrayElementAtIndex(overrideIndex);

            EditorGUI.BeginChangeCheck();
            if (overrideProp != null)
            {
                EditorGUILayout.PropertyField(overrideProp.FindPropertyRelative("Texture"), new GUIContent("Base Texture"));
                DrawDecorationsList(data, overrideProp.FindPropertyRelative("Decorations"), $"tile_{coord.x}_{coord.y}");
            }
            else
            {
                Texture2D tex = (Texture2D)EditorGUILayout.ObjectField("Base Texture", null, typeof(Texture2D), false);
                if (tex != null)
                {
                    Undo.RecordObject(data, "Add Tile Override");
                    data.VisualOverrides.Add(new TileVisualOverride { Coordinate = coord, Texture = tex, Decorations = new List<DecorationData>() });
                    EditorUtility.SetDirty(data);
                    serializedObject.Update();
                    return;
                }
                if (GUILayout.Button("+ Add Decoration to Tile"))
                {
                    Undo.RecordObject(data, "Add Tile Decoration");
                    data.VisualOverrides.Add(new TileVisualOverride { Coordinate = coord, Decorations = new List<DecorationData> { DecorationData.Default } });
                    EditorUtility.SetDirty(data);
                    serializedObject.Update();
                }
            }

            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();

            if (overrideIndex != -1)
            {
                EditorGUILayout.Space(10);
                GUI.backgroundColor = new Color(1, 0.5f, 0.5f);
                if (GUILayout.Button("Clear All Overrides for Tile"))
                {
                    Undo.RecordObject(data, "Clear Tile Visuals");
                    data.VisualOverrides.RemoveAt(overrideIndex);
                    EditorUtility.SetDirty(data);
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawDecorationsList(MapData data, SerializedProperty decosProp, string idPrefix)
        {
            EditorGUILayout.LabelField("Decorations", EditorStyles.miniBoldLabel);
            for (int i = 0; i < decosProp.arraySize; i++)
            {
                SerializedProperty decoProp = decosProp.GetArrayElementAtIndex(i);
                SerializedProperty prefabProp = decoProp.FindPropertyRelative("Prefab");
                
                string foldoutKey = $"{idPrefix}_deco_{i}";
                if (!_decoFoldouts.ContainsKey(foldoutKey)) _decoFoldouts[foldoutKey] = true;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                string label = prefabProp.objectReferenceValue != null ? prefabProp.objectReferenceValue.name : $"Decoration {i}";
                _decoFoldouts[foldoutKey] = EditorGUILayout.Foldout(_decoFoldouts[foldoutKey], label, true, EditorStyles.foldoutHeader);
                if (GUILayout.Button("Remove", GUILayout.Width(60))) { decosProp.DeleteArrayElementAtIndex(i); break; }
                EditorGUILayout.EndHorizontal();

                if (_decoFoldouts[foldoutKey])
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(prefabProp, new GUIContent("Prefab"));
                    EditorGUILayout.PropertyField(decoProp.FindPropertyRelative("Offset"));
                    EditorGUILayout.PropertyField(decoProp.FindPropertyRelative("Rotation"));
                    EditorGUILayout.PropertyField(decoProp.FindPropertyRelative("Scale"));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("+ Add New Decoration")) { 
                decosProp.arraySize++; 
                SerializedProperty newDeco = decosProp.GetArrayElementAtIndex(decosProp.arraySize - 1);
                newDeco.FindPropertyRelative("Scale").vector3Value = Vector3.one;
                newDeco.FindPropertyRelative("Offset").vector3Value = Vector3.zero;
                newDeco.FindPropertyRelative("Rotation").vector3Value = Vector3.zero;
                newDeco.FindPropertyRelative("Prefab").objectReferenceValue = null;
            }
        }

        private void DrawMapPreview(MapData data, bool isVisualMode)
        {
            float availableWidth = EditorGUIUtility.currentViewWidth - 60 - LabelSpace;
            // Map boundaries are Width x Height. We draw walls at -1 and data.Width/Height.
            // So total grid drawn is (Width + 2) high and (Height + 2) wide in terms of cells.
            // Standardized: Width is Horizontal (X), Height is Vertical (Y)
            float cellW = Mathf.Min(availableWidth / (data.Width + 2), MaxCellSize);
            float cellH = cellW;

            float gridWidth = cellW * (data.Width + 2);
            float gridHeight = cellH * (data.Height + 2);

            // Increase outerRect to provide room for labels around the dark box
            float totalWidth = gridWidth + LabelSpace * 4;
            float totalHeight = gridHeight + LabelSpace * 4;
            Rect outerRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);
            outerRect.x += (availableWidth + LabelSpace * 4 - totalWidth) / 2f;

            // cellAreaRect is the dark box containing walls and tiles
            Rect cellAreaRect = new Rect(outerRect.x + LabelSpace * 2, outerRect.y + LabelSpace * 2, gridWidth, gridHeight);
            EditorGUI.DrawRect(cellAreaRect, new Color(0.12f, 0.12f, 0.12f, 1f));

            // tileGridRect is for the actual tiles (0 to width)
            Rect tileGridRect = new Rect(cellAreaRect.x + cellW, cellAreaRect.y + cellH, cellW * data.Width, cellH * data.Height);

            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            GUIStyle dirStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.9f, 0.8f, 0.2f) } };

            // Tile labels (X horizontal, Y vertical)
            for (int x = 0; x < data.Width; x++)
            {
                // Draw numbers below the South wall (cellAreaRect.y + cellAreaRect.height)
                Rect labelRect = new Rect(tileGridRect.x + x * cellW, cellAreaRect.y + cellAreaRect.height + 2, cellW, LabelSpace);
                EditorGUI.LabelField(labelRect, x.ToString(), labelStyle);
            }
            for (int y = 0; y < data.Height; y++)
            {
                // Draw numbers to the left of the West wall (cellAreaRect.x)
                Rect labelRect = new Rect(cellAreaRect.x - LabelSpace - 2, tileGridRect.y + (data.Height - 1 - y) * cellH, LabelSpace, cellH);
                EditorGUI.LabelField(labelRect, y.ToString(), labelStyle);
            }

            // Cardinal direction labels (Moved significantly outside the dark box)
            float midX = cellAreaRect.x + cellAreaRect.width / 2f;
            float midY = cellAreaRect.y + cellAreaRect.height / 2f;
            // North label above
            EditorGUI.LabelField(new Rect(midX - 20, cellAreaRect.y - LabelSpace - 5, 40, LabelSpace), "N", dirStyle);
            // South label below
            EditorGUI.LabelField(new Rect(midX - 20, cellAreaRect.y + cellAreaRect.height + LabelSpace, 40, LabelSpace), "S", dirStyle);
            // West label left
            EditorGUI.LabelField(new Rect(cellAreaRect.x - LabelSpace * 2, midY - 8, LabelSpace, 16), "W", dirStyle);
            // East label right
            EditorGUI.LabelField(new Rect(cellAreaRect.x + cellAreaRect.width + LabelSpace, midY - 8, LabelSpace, 16), "E", dirStyle);

            Event e = Event.current;
            Random.State oldState = Random.state;
            Random.InitState(data.MapSeed);

            // Closure for drawing selectable items (tiles or walls)
            void DrawItem(int gridX, int gridY, Color baseColor, string label, SelectionType type, WallSide side = WallSide.North, int index = 0)
            {
                // Standard: X is horizontal, Y is vertical (0,0 at bottom-left)
                Rect rect = new Rect(
                    tileGridRect.x + (gridX * cellW) + CellPadding,
                    tileGridRect.y + ((data.Height - 1 - gridY) * cellH) + CellPadding,
                    cellW - CellPadding * 2,
                    cellH - CellPadding * 2
                );

                // NO OFFSET NEEDED HERE. The coordinates x,y are already transformed to rect pos.
                // gridX outside 0..Width-1 range automatically places walls in the margin rows/cols.

                SelectionItem thisItem = new SelectionItem { Type = type, TileCoord = new Vector2Int(gridX, gridY), WallSide = side, WallIndex = index };
                bool isSelected = _selection.Exists(s => s.Equals(thisItem));

                EditorGUI.DrawRect(rect, isSelected && isVisualMode ? Color.yellow : baseColor);
                if (isSelected && isVisualMode) {
                    Rect inner = new Rect(rect.x+2, rect.y+2, rect.width-4, rect.height-4);
                    EditorGUI.DrawRect(inner, baseColor);
                }

                if (!string.IsNullOrEmpty(label))
                {
                    EditorGUI.LabelField(rect, label, new GUIStyle(EditorStyles.boldLabel) { 
                        alignment = TextAnchor.UpperRight, normal = { textColor = Color.yellow } 
                    });
                }

                if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
                {
                    if (isVisualMode)
                    {
                        if (e.button == 0) // Left Click
                        {
                            if (e.control)
                            {
                                if (isSelected) _selection.RemoveAll(s => s.Equals(thisItem));
                                else _selection.Add(thisItem);
                                _lastSelectedItem = thisItem;
                            }
                            else if (e.alt)
                            {
                                _selection.RemoveAll(s => s.Equals(thisItem));
                            }
                            else if (e.shift && (int)_lastSelectedItem.Type != -1 && _lastSelectedItem.Type == type)
                            {
                                if (type == SelectionType.Tile) SelectTileRange(data, _lastSelectedItem.TileCoord, thisItem.TileCoord);
                                else if (type == SelectionType.Wall) SelectWallRange(data, _lastSelectedItem, thisItem);
                            }
                            else
                            {
                                _selection.Clear();
                                _selection.Add(thisItem);
                                _lastSelectedItem = thisItem;
                            }
                        }
                        else if (e.button == 1) // Right Click
                        {
                            if (!isSelected)
                            {
                                _selection.Clear();
                                _selection.Add(thisItem);
                                _lastSelectedItem = thisItem;
                            }
                            ShowContextMenu(data, thisItem);
                        }
                    }
                    if (type == SelectionType.Tile && e.button == 0)
                    {
                        if (e.control)
                        {
                            if (isSelected) _selection.RemoveAll(s => s.Equals(thisItem));
                            else _selection.Add(thisItem);
                            _lastSelectedItem = thisItem;
                        }
                        else if (e.shift && (int)_lastSelectedItem.Type != -1 && _lastSelectedItem.Type == type)
                        {
                            SelectTileRange(data, _lastSelectedItem.TileCoord, thisItem.TileCoord);
                        }
                        else
                        {
                            _selection.Clear();
                            _selection.Add(thisItem);
                            _lastSelectedItem = thisItem;
                        }
                    }
                    e.Use();
                }
            }

            // Draw Walls
            bool toggleCascade = data.WallCascadeOnHoles;

            // East = Right side (x = Width), runs along Y, index = y
            for (int y = 0; y < data.Height; y++) {
                int ovIdx = data.WallOverrides.FindIndex(o => o.Side == WallSide.East && o.Index == y);
                bool hasOverride = false;
                if (ovIdx != -1) {
                    var o = data.WallOverrides[ovIdx];
                    hasOverride = o.TextureOverride != null || o.OverrideScale || o.OverrideOffset || (o.Decorations != null && o.Decorations.Count > 0);
                }
                
                bool isEnabled = data.Walls.East;
                bool isCascaded = !toggleCascade && IsTileTypeHole(data, data.Width - 1, y);
                Color wallColor = (!isEnabled || isCascaded) ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.2f, 0.2f, 0.3f);
                DrawItem(data.Width, y, wallColor, hasOverride ? "*" : "", SelectionType.Wall, WallSide.East, y);
            }
            // West = Left side (x = -1), runs along Y, index = y
            for (int y = 0; y < data.Height; y++) {
                int ovIdx = data.WallOverrides.FindIndex(o => o.Side == WallSide.West && o.Index == y);
                bool hasOverride = false;
                if (ovIdx != -1) {
                    var o = data.WallOverrides[ovIdx];
                    hasOverride = o.TextureOverride != null || o.OverrideScale || o.OverrideOffset || (o.Decorations != null && o.Decorations.Count > 0);
                }

                bool isEnabled = data.Walls.West;
                bool isCascaded = !toggleCascade && IsTileTypeHole(data, 0, y);
                Color wallColor = (!isEnabled || isCascaded) ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.2f, 0.2f, 0.3f);
                DrawItem(-1, y, wallColor, hasOverride ? "*" : "", SelectionType.Wall, WallSide.West, y);
            }
            // North/South segments
            for (int x = 0; x < data.Width; x++) {
                // North
                int ovIdxN = data.WallOverrides.FindIndex(o => o.Side == WallSide.North && o.Index == x);
                bool hasOverrideN = ovIdxN != -1 && (data.WallOverrides[ovIdxN].TextureOverride != null || data.WallOverrides[ovIdxN].Decorations.Count > 0);
                bool isCascadedN = !toggleCascade && IsTileTypeHole(data, x, data.Height - 1);
                DrawItem(x, data.Height, (!data.Walls.North || isCascadedN) ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.25f, 0.2f, 0.35f), hasOverrideN ? "*" : "", SelectionType.Wall, WallSide.North, x);

                // South
                int ovIdxS = data.WallOverrides.FindIndex(o => o.Side == WallSide.South && o.Index == x);
                bool hasOverrideS = ovIdxS != -1 && (data.WallOverrides[ovIdxS].TextureOverride != null || data.WallOverrides[ovIdxS].Decorations.Count > 0);
                bool isCascadedS = !toggleCascade && IsTileTypeHole(data, x, 0);
                DrawItem(x, -1, (!data.Walls.South || isCascadedS) ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.25f, 0.2f, 0.35f), hasOverrideS ? "*" : "", SelectionType.Wall, WallSide.South, x);
            }

            // Corners
            void DrawCorner(int x, int y, WallSide side, bool enabled) {
                int ovIdx = data.WallOverrides.FindIndex(o => o.Side == side && o.Index == 0);
                bool hasOverride = ovIdx != -1 && (data.WallOverrides[ovIdx].TextureOverride != null || data.WallOverrides[ovIdx].Decorations.Count > 0);
                DrawItem(x, y, enabled ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.1f, 0.1f, 0.1f), hasOverride ? "*" : "", SelectionType.Wall, side, 0);
            }
            DrawCorner(-1, data.Height, WallSide.NorthWest, data.Walls.NW);
            DrawCorner(data.Width, data.Height, WallSide.NorthEast, data.Walls.NE);
            DrawCorner(-1, -1, WallSide.SouthWest, data.Walls.SW);
            DrawCorner(data.Width, -1, WallSide.SouthEast, data.Walls.SE);

            // Draw Tiles
            for (int y = 0; y < data.Height; y++)
            {
                for (int x = 0; x < data.Width; x++)
                {
                    Vector2Int coord = new Vector2Int(x, y);
                    int ovIdx = data.VisualOverrides.FindIndex(o => o.Coordinate == coord);
                    bool hasOverride = false;
                    if (ovIdx != -1) {
                        var o = data.VisualOverrides[ovIdx];
                        hasOverride = o.Texture != null || (o.Decorations != null && o.Decorations.Count > 0);
                    }
                    DrawItem(x, y, GetTileColor(data, coord), hasOverride ? "*" : "", SelectionType.Tile);
                }
            }

            Random.state = oldState;
        }

        private void ShowContextMenu(MapData data, SelectionItem targetItem)
        {
            GenericMenu menu = new GenericMenu();

            Texture2D targetTex = null;
            if (targetItem.Type == SelectionType.Tile)
            {
                int idx = data.VisualOverrides.FindIndex(o => o.Coordinate == targetItem.TileCoord);
                if (idx != -1) targetTex = data.VisualOverrides[idx].Texture;
            }
            else
            {
                int idx = data.WallOverrides.FindIndex(o => o.Side == targetItem.WallSide && o.Index == targetItem.WallIndex);
                if (idx != -1) targetTex = data.WallOverrides[idx].TextureOverride;
            }

            if (targetTex != null)
            {
                menu.AddItem(new GUIContent("Copy Texture"), false, () => s_TextureClipboard = targetTex);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Copy Texture"));
            }

            if (s_TextureClipboard != null)
            {
                menu.AddItem(new GUIContent("Paste Texture"), false, () => {
                    Undo.RecordObject(data, "Paste Texture Override");
                    foreach (var sel in _selection)
                    {
                        if (sel.Type == SelectionType.Tile) ApplyTileTexture(data, sel.TileCoord, s_TextureClipboard);
                        else ApplyWallOverride(data, sel.WallSide, sel.WallIndex, s_TextureClipboard, false, Vector3.one, false, Vector3.zero);
                    }
                    EditorUtility.SetDirty(data);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste Texture"));
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Clear Overrides"), false, () => {
                Undo.RecordObject(data, "Clear Overrides");
                foreach (var sel in _selection)
                {
                    if (sel.Type == SelectionType.Tile) data.VisualOverrides.RemoveAll(o => o.Coordinate == sel.TileCoord);
                    else data.WallOverrides.RemoveAll(o => o.Side == sel.WallSide && o.Index == sel.WallIndex);
                }
                EditorUtility.SetDirty(data);
            });

            menu.ShowAsContext();
        }

        private void SelectTileRange(MapData data, Vector2Int start, Vector2Int end)
        {
            int xMin = Mathf.Min(start.x, end.x);
            int xMax = Mathf.Max(start.x, end.x);
            int yMin = Mathf.Min(start.y, end.y);
            int yMax = Mathf.Max(start.y, end.y);

            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    SelectionItem item = new SelectionItem { Type = SelectionType.Tile, TileCoord = new Vector2Int(x, y) };
                    if (!_selection.Exists(s => s.Equals(item))) _selection.Add(item);
                }
            }
        }

        private void SelectWallRange(MapData data, SelectionItem start, SelectionItem end)
        {
            if (start.WallSide != end.WallSide) return;

            int iMin = Mathf.Min(start.WallIndex, end.WallIndex);
            int iMax = Mathf.Max(start.WallIndex, end.WallIndex);

            for (int i = iMin; i <= iMax; i++)
            {
                SelectionItem item = new SelectionItem { Type = SelectionType.Wall, WallSide = start.WallSide, WallIndex = i };
                if (!_selection.Exists(s => s.Equals(item))) _selection.Add(item);
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

        private Color GetTileColor(MapData data, Vector2Int coord)
        {
            if (data.UseManualLayout)
            {
                int idx = data.ManualLayoutData.FindIndex(d => d.Coordinate == coord);
                if (idx != -1)
                {
                    return GetTileColor(data.ManualLayoutData[idx].Type);
                }
                return Color.white;
            }

            Random.State tempState = Random.state;
            Random.InitState(data.MapSeed + coord.x * 1000 + coord.y);
            bool isHighGround = Random.value < data.HighGroundChance;
            if (coord.y == 0 || coord.y == data.Height - 1) isHighGround = true;
            Random.state = tempState;
            return isHighGround ? Color.gray : Color.white;
        }

        private void DrawPalette(MapData data)
        {
            EditorGUILayout.LabelField("Tile Palette", EditorStyles.boldLabel);
            if (_selection.Count == 0 || _selection.Exists(s => s.Type != SelectionType.Tile))
            {
                EditorGUILayout.HelpBox("Select tiles in the grid to change their type.", MessageType.None);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Selection: {_selection.Count} Tiles", EditorStyles.miniBoldLabel);
            
            TileType[] paletteOrder = new TileType[] {
                TileType.None, TileType.Walkable, TileType.HighGround,
                TileType.SpawnPoint, TileType.SpawnPointHigh, TileType.ExitPoint,
                TileType.ExitPointHigh, TileType.LowTile, TileType.NonWalkableDecor,
                TileType.DecoHighGround, TileType.Wall
            };

            int typesPerRow = 3;
            for (int i = 0; i < paletteOrder.Length; i += typesPerRow)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = 0; j < typesPerRow && (i + j) < paletteOrder.Length; j++)
                {
                    TileType type = paletteOrder[i + j];
                    Color typeColor = GetTileColor(type);
                    
                    GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                    buttonStyle.alignment = TextAnchor.MiddleLeft;
                    buttonStyle.padding.left = 20;
                    
                    float buttonWidth = (EditorGUIUtility.currentViewWidth - 60) / typesPerRow;
                    Rect rect = GUILayoutUtility.GetRect(new GUIContent(type.ToString()), buttonStyle, GUILayout.Width(buttonWidth));
                    
                    if (GUI.Button(rect, type.ToString(), buttonStyle))
                    {
                        SetTileType(data, type);
                    }
                    
                    Rect colorRect = new Rect(rect.x + 4, rect.y + 4, 12, 12);
                    EditorGUI.DrawRect(colorRect, typeColor);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }


        private void SetTileType(MapData data, TileType type)
        {
            Undo.RecordObject(data, $"Set Tile Type to {type}");
            if (!data.UseManualLayout) CaptureRandomToManual(data);

            foreach (var sel in _selection)
            {
                if (sel.Type != SelectionType.Tile) continue;
                
                int idx = data.ManualLayoutData.FindIndex(d => d.Coordinate == sel.TileCoord);
                if (idx != -1)
                {
                    var d = data.ManualLayoutData[idx];
                    d.Type = type;
                    data.ManualLayoutData[idx] = d;
                }
                else
                {
                    data.ManualLayoutData.Add(new TileLayoutData { Coordinate = sel.TileCoord, Type = type });
                }
            }

            data.UseManualLayout = true;
            EditorUtility.SetDirty(data);
        }

        private void CaptureRandomToManual(MapData data)
        {
            Undo.RecordObject(data, "Capture Random to Manual");
            data.ManualLayoutData.Clear();
            
            Random.State oldState = Random.state;
            Random.InitState(data.MapSeed);
            for (int x = 0; x < data.Width; x++)
            {
                for (int y = 0; y < data.Height; y++)
                {
                    bool isHighGround = Random.value < data.HighGroundChance;
                    if (y == 0 || y == data.Height - 1) isHighGround = true;
                    
                    TileType type = isHighGround ? TileType.HighGround : TileType.Walkable;
                    data.ManualLayoutData.Add(new TileLayoutData { Coordinate = new Vector2Int(x, y), Type = type });
                }
            }
            data.UseManualLayout = true;
            Random.state = oldState;
            EditorUtility.SetDirty(data);
        }

        private void Flip(MapData data, bool horizontal)
        {
            Undo.RecordObject(data, horizontal ? "Flip Horizontal" : "Flip Vertical");
            
            // Flip Layout Data
            for (int i = 0; i < data.ManualLayoutData.Count; i++)
            {
                var d = data.ManualLayoutData[i];
                if (horizontal) d.Coordinate.x = (data.Width - 1) - d.Coordinate.x;
                else d.Coordinate.y = (data.Height - 1) - d.Coordinate.y;
                data.ManualLayoutData[i] = d;
            }
            
            TransformCoordSet(data.SpawnPoints, horizontal, data.Width, data.Height);
            TransformVectorSet(data.ExitPoints, horizontal, data.Width, data.Height);

            // Flip Visual Overrides
            for (int i = 0; i < data.VisualOverrides.Count; i++)
            {
                var v = data.VisualOverrides[i];
                if (horizontal) v.Coordinate.x = (data.Width - 1) - v.Coordinate.x;
                else v.Coordinate.y = (data.Height - 1) - v.Coordinate.y;
                data.VisualOverrides[i] = v;
            }

            // Flip Wall Overrides & Toggles
            if (horizontal)
            {
                // Toggles
                bool tempW = data.Walls.West;
                data.Walls.West = data.Walls.East;
                data.Walls.East = tempW;
                bool tempNW = data.Walls.NW;
                data.Walls.NW = data.Walls.NE;
                data.Walls.NE = tempNW;
                bool tempSW = data.Walls.SW;
                data.Walls.SW = data.Walls.SE;
                data.Walls.SE = tempSW;

                for (int i = 0; i < data.WallOverrides.Count; i++)
                {
                    var w = data.WallOverrides[i];
                    if (w.Side == WallSide.North || w.Side == WallSide.South) w.Index = (data.Width - 1) - w.Index;
                    else if (w.Side == WallSide.West) w.Side = WallSide.East;
                    else if (w.Side == WallSide.East) w.Side = WallSide.West;
                    else if (w.Side == WallSide.NorthWest) w.Side = WallSide.NorthEast;
                    else if (w.Side == WallSide.NorthEast) w.Side = WallSide.NorthWest;
                    else if (w.Side == WallSide.SouthWest) w.Side = WallSide.SouthEast;
                    else if (w.Side == WallSide.SouthEast) w.Side = WallSide.SouthWest;
                    data.WallOverrides[i] = w;
                }

                // Side Visual Overrides
                for (int i = 0; i < data.SideVisualOverrides.Count; i++)
                {
                    var sideOv = data.SideVisualOverrides[i];
                    if (sideOv.Side == WallSide.West) sideOv.Side = WallSide.East;
                    else if (sideOv.Side == WallSide.East) sideOv.Side = WallSide.West;
                    else if (sideOv.Side == WallSide.NorthWest) sideOv.Side = WallSide.NorthEast;
                    else if (sideOv.Side == WallSide.NorthEast) sideOv.Side = WallSide.NorthWest;
                    else if (sideOv.Side == WallSide.SouthWest) sideOv.Side = WallSide.SouthEast;
                    else if (sideOv.Side == WallSide.SouthEast) sideOv.Side = WallSide.SouthWest;
                    data.SideVisualOverrides[i] = sideOv;
                }
            }
            else
            {
                // Toggles
                bool tempN = data.Walls.North;
                data.Walls.North = data.Walls.South;
                data.Walls.South = tempN;
                bool tempNW = data.Walls.NW;
                data.Walls.NW = data.Walls.SW;
                data.Walls.SW = tempNW;
                bool tempNE = data.Walls.NE;
                data.Walls.NE = data.Walls.SE;
                data.Walls.SE = tempNE;

                for (int i = 0; i < data.WallOverrides.Count; i++)
                {
                    var w = data.WallOverrides[i];
                    if (w.Side == WallSide.West || w.Side == WallSide.East) w.Index = (data.Height - 1) - w.Index;
                    else if (w.Side == WallSide.North) w.Side = WallSide.South;
                    else if (w.Side == WallSide.South) w.Side = WallSide.North;
                    else if (w.Side == WallSide.NorthWest) w.Side = WallSide.SouthWest;
                    else if (w.Side == WallSide.SouthWest) w.Side = WallSide.NorthWest;
                    else if (w.Side == WallSide.NorthEast) w.Side = WallSide.SouthEast;
                    else if (w.Side == WallSide.SouthEast) w.Side = WallSide.NorthEast;
                    data.WallOverrides[i] = w;
                }

                // Side Visual Overrides
                for (int i = 0; i < data.SideVisualOverrides.Count; i++)
                {
                    var sideOv = data.SideVisualOverrides[i];
                    if (sideOv.Side == WallSide.North) sideOv.Side = WallSide.South;
                    else if (sideOv.Side == WallSide.South) sideOv.Side = WallSide.North;
                    else if (sideOv.Side == WallSide.NorthWest) sideOv.Side = WallSide.SouthWest;
                    else if (sideOv.Side == WallSide.SouthWest) sideOv.Side = WallSide.NorthWest;
                    else if (sideOv.Side == WallSide.NorthEast) sideOv.Side = WallSide.SouthEast;
                    else if (sideOv.Side == WallSide.SouthEast) sideOv.Side = WallSide.NorthEast;
                    data.SideVisualOverrides[i] = sideOv;
                }
            }

            _selection.Clear();
            EditorUtility.SetDirty(data);
        }

        private void TransformCoordSet(List<SpawnPointData> coords, bool horizontal, int w, int h)
        {
            if (coords == null) return;
            for (int i = 0; i < coords.Count; i++)
            {
                var s = coords[i];
                if (horizontal) s.Coordinate.x = (w - 1) - s.Coordinate.x;
                else s.Coordinate.y = (h - 1) - s.Coordinate.y;
                coords[i] = s;
            }
        }

        private void TransformVectorSet(List<Vector2Int> coords, bool horizontal, int w, int h)
        {
            if (coords == null) return;
            for (int i = 0; i < coords.Count; i++)
            {
                Vector2Int c = coords[i];
                if (horizontal) c.x = (w - 1) - c.x;
                else c.y = (h - 1) - c.y;
                coords[i] = c;
            }
        }

        private void Rotate(MapData data)
        {
            Undo.RecordObject(data, "Rotate 90 CW");
            int oldW = data.Width;
            int oldH = data.Height;
            
            // Rotate Layout Data
            for (int i = 0; i < data.ManualLayoutData.Count; i++)
            {
                var d = data.ManualLayoutData[i];
                int newX = d.Coordinate.y;
                int newY = (oldW - 1) - d.Coordinate.x;
                d.Coordinate = new Vector2Int(newX, newY);
                data.ManualLayoutData[i] = d;
            }
            
            RotateCoordSet(data.SpawnPoints, oldW, oldH);
            RotateVectorSet(data.ExitPoints, oldW, oldH);

            // Rotate Visual Overrides
            for (int i = 0; i < data.VisualOverrides.Count; i++)
            {
                var v = data.VisualOverrides[i];
                int newX = v.Coordinate.y;
                int newY = (oldW - 1) - v.Coordinate.x;
                v.Coordinate = new Vector2Int(newX, newY);
                data.VisualOverrides[i] = v;
            }

            // Toggles
            bool oldN = data.Walls.North;
            bool oldS = data.Walls.South;
            bool oldE = data.Walls.East;
            bool oldW_wall = data.Walls.West;
            data.Walls.North = oldW_wall;
            data.Walls.East = oldN;
            data.Walls.South = oldE;
            data.Walls.West = oldS;

            bool oldNW = data.Walls.NW;
            bool oldNE = data.Walls.NE;
            bool oldSW = data.Walls.SW;
            bool oldSE = data.Walls.SE;
            data.Walls.NW = oldSW;
            data.Walls.NE = oldNW;
            data.Walls.SE = oldNE;
            data.Walls.SW = oldSE;

            // Side Visual Overrides
            for (int i = 0; i < data.SideVisualOverrides.Count; i++)
            {
                var sideOv = data.SideVisualOverrides[i];
                switch (sideOv.Side)
                {
                    case WallSide.North: sideOv.Side = WallSide.East; break;
                    case WallSide.East: sideOv.Side = WallSide.South; break;
                    case WallSide.South: sideOv.Side = WallSide.West; break;
                    case WallSide.West: sideOv.Side = WallSide.North; break;
                    case WallSide.NorthWest: sideOv.Side = WallSide.NorthEast; break;
                    case WallSide.NorthEast: sideOv.Side = WallSide.SouthEast; break;
                    case WallSide.SouthEast: sideOv.Side = WallSide.SouthWest; break;
                    case WallSide.SouthWest: sideOv.Side = WallSide.NorthWest; break;
                }
                data.SideVisualOverrides[i] = sideOv;
            }

            // Wall Overrides
            for (int i = 0; i < data.WallOverrides.Count; i++)
            {
                var w = data.WallOverrides[i];
                WallSide newSide = w.Side;
                int newIndex = w.Index;

                switch (w.Side)
                {
                    case WallSide.North: newSide = WallSide.East; newIndex = (oldW - 1) - w.Index; break;
                    case WallSide.South: newSide = WallSide.West; newIndex = (oldW - 1) - w.Index; break;
                    case WallSide.East: newSide = WallSide.South; newIndex = w.Index; break;
                    case WallSide.West: newSide = WallSide.North; newIndex = w.Index; break;
                    case WallSide.NorthWest: newSide = WallSide.NorthEast; newIndex = 0; break;
                    case WallSide.NorthEast: newSide = WallSide.SouthEast; newIndex = 0; break;
                    case WallSide.SouthEast: newSide = WallSide.SouthWest; newIndex = 0; break;
                    case WallSide.SouthWest: newSide = WallSide.NorthWest; newIndex = 0; break;
                }
                w.Side = newSide;
                w.Index = newIndex;
                data.WallOverrides[i] = w;
            }

            data.Width = oldH;
            data.Height = oldW;
            _selection.Clear();
            EditorUtility.SetDirty(data);
        }

        private void RotateCoordSet(List<SpawnPointData> coords, int w, int h)
        {
            if (coords == null) return;
            for (int i = 0; i < coords.Count; i++)
            {
                var s = coords[i];
                int newX = s.Coordinate.y;
                int newY = (w - 1) - s.Coordinate.x;
                s.Coordinate = new Vector2Int(newX, newY);
                coords[i] = s;
            }
        }

        private void RotateVectorSet(List<Vector2Int> coords, int w, int h)
        {
            if (coords == null) return;
            for (int i = 0; i < coords.Count; i++)
            {
                Vector2Int c = coords[i];
                int newX = c.y;
                int newY = (w - 1) - c.x;
                coords[i] = new Vector2Int(newX, newY);
            }
        }


        private void DrawSpawnPointConfig(MapData data)
        {
            // Only show if a single spawn point is selected
            if (_selection.Count != 1 || _selection[0].Type != SelectionType.Tile) return;
            
            Vector2Int coord = _selection[0].TileCoord;
            int spawnIdx = data.SpawnPoints.FindIndex(s => s.Coordinate == coord);
            if (spawnIdx == -1) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Spawn Point Configuration", EditorStyles.boldLabel);
            
            var spawnData = data.SpawnPoints[spawnIdx];
            
            string[] exitOptions = new string[data.ExitPoints.Count + 1];
            exitOptions[0] = "Any/First Exit (-1)";
            for (int i = 0; i < data.ExitPoints.Count; i++)
            {
                exitOptions[i + 1] = $"Exit {i} at {data.ExitPoints[i]}";
            }

            EditorGUI.BeginChangeCheck();
            int selectedExit = EditorGUILayout.Popup("Target Exit", spawnData.TargetExitIndex + 1, exitOptions) - 1;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(data, "Change Spawn Point Target Exit");
                spawnData.TargetExitIndex = selectedExit;
                data.SpawnPoints[spawnIdx] = spawnData;
                EditorUtility.SetDirty(data);
            }
            
            if (spawnData.TargetExitIndex >= 0 && spawnData.TargetExitIndex < data.ExitPoints.Count)
            {
                EditorGUILayout.HelpBox($"Mapped to Exit {spawnData.TargetExitIndex} at {data.ExitPoints[spawnData.TargetExitIndex]}", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private bool IsTileTypeHole(MapData data, int x, int y)
        {
            if (x < 0 || x >= data.Width || y < 0 || y >= data.Height) return true;
            if (data.UseManualLayout)
            {
                int idx = data.ManualLayoutData.FindIndex(d => d.Coordinate.x == x && d.Coordinate.y == y);
                if (idx != -1) return data.ManualLayoutData[idx].Type == TileType.None;
                // If it's missing in manual layout but within bounds, don't treat it as a hole for the preview's cascade logic,
                // otherwise increasing map height/width makes all walls look broken (black).
                return false; 
            }
            return false;
        }

        private void Shift(MapData data, int dx, int dy)
        {
            Undo.RecordObject(data, "Shift Map Layout");

            // Shift Tiles
            for (int i = 0; i < data.ManualLayoutData.Count; i++)
            {
                var d = data.ManualLayoutData[i];
                d.Coordinate += new Vector2Int(dx, dy);
                data.ManualLayoutData[i] = d;
            }

            // Shift Spawn Points
            for (int i = 0; i < data.SpawnPoints.Count; i++)
            {
                var s = data.SpawnPoints[i];
                s.Coordinate += new Vector2Int(dx, dy);
                data.SpawnPoints[i] = s;
            }

            // Shift Exit Points
            for (int i = 0; i < data.ExitPoints.Count; i++)
            {
                data.ExitPoints[i] += new Vector2Int(dx, dy);
            }

            // Shift Visual Overrides
            for (int i = 0; i < data.VisualOverrides.Count; i++)
            {
                var v = data.VisualOverrides[i];
                v.Coordinate += new Vector2Int(dx, dy);
                data.VisualOverrides[i] = v;
            }

            // Shift Wall Overrides
            for (int i = 0; i < data.WallOverrides.Count; i++)
            {
                var w = data.WallOverrides[i];
                if (w.Side == WallSide.North || w.Side == WallSide.South) w.Index += dy;
                else w.Index += dx;
                data.WallOverrides[i] = w;
            }

            EditorUtility.SetDirty(data);
        }
    }
}
