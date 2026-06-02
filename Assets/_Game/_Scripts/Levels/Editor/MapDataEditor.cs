using UnityEngine;
using UnityEditor;
using MaouSamaTD.Levels;
using MaouSamaTD.Grid;
using MaouSamaTD.Units;
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
        private string[] _tabNames = { "Layout", "Visuals", "Environment" };
        private Vector2 _scrollPosition;
        private EnemyData _pathingSimulationEnemy;

        // Section Foldouts
        private static bool _showDimensions = true;
        private static bool _showWalls = true;
        private static bool _showInteractiveEditor = true;
        private static bool _showTools = true;
        private static bool _showPathing = true;
        private static bool _showGeneration = true;
        private static bool _showGlobalVisuals = true;
        private static bool _showEnvironmentLighting = true;
        private static bool _showSideOverridesHeader = true;
        private static bool _showBulkActions = true;
        private static bool _showCameraSettings = true;
        
        private static Texture2D s_TextureClipboard;
        private static DecorationData s_DecorationClipboard;
        private static bool s_HasDecorationClipboard = false;

        private static GameObject s_BatchDecoPrefab;
        private static Vector3 s_BatchDecoOffset = new Vector3(0f, 0.5f, 0f);
        private static Vector3 s_BatchDecoRotation = Vector3.zero;
        private static Vector3 s_BatchDecoScale = Vector3.one;
        
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

            // Toggle button for Default/Custom inspector
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.normal.textColor = data.useDefaultInspector ? Color.gray : new Color(0.1f, 0.7f, 0.2f);
            buttonStyle.fontSize = 12;

            if (GUILayout.Button(data.useDefaultInspector ? "Switch to Custom Editor" : "Switch to Default Editor", buttonStyle, GUILayout.Height(30)))
            {
                data.useDefaultInspector = !data.useDefaultInspector;
                EditorUtility.SetDirty(data);
            }

            EditorGUILayout.Space();

            if (data.useDefaultInspector)
            {
                DrawDefaultInspectorWithReadOnlyID();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            EditorGUILayout.Space();

            if (_selectedTab == 0)
            {
                DrawLayoutTab(data);
            }
            else if (_selectedTab == 1)
            {
                DrawVisualsTab(data);
            }
            else
            {
                DrawEnvironmentTab(data);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDefaultInspectorWithReadOnlyID()
        {
            SerializedProperty iter = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iter.NextVisible(enterChildren))
            {
                using (new EditorGUI.DisabledScope(iter.name == "UniqueID" || iter.name == "m_Script"))
                {
                    EditorGUILayout.PropertyField(iter, true);
                }
                enterChildren = false;
            }
        }

        private void DrawLayoutTab(MapData data)
        {
            if (DrawSectionHeader("Map Dimensions & Logic", ref _showDimensions))
            {
                if (!data.UseManualLayout)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("MapSeed"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("HighGroundChance"));
                }
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Width"), new GUIContent("Width", "The horizontal size of the map (X axis)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Height"), new GUIContent("Height", "The vertical size of the map (Y axis)"));
                
                if (data.UseManualLayout)
                {
                    EditorGUILayout.HelpBox("Changing dimensions while using Manual Layout will resize the grid. Tiles outside the new bounds will still be saved but won't be visible or editable in the preview.", MessageType.Info);
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("UseManualLayout"));
            }
            EndSection(_showDimensions);

            if (DrawSectionHeader("Camera Zoom Settings", ref _showCameraSettings))
            {
                SerializedProperty autoZoomProp = serializedObject.FindProperty("AutoCalculateDefaultZoom");
                EditorGUILayout.PropertyField(autoZoomProp);
                
                if (!autoZoomProp.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("CustomDefaultZoom"));
                }
            }
            EndSection(_showCameraSettings);
            
            if (DrawSectionHeader("Wall Configuration", ref _showWalls))
            {
                SerializedProperty wallsProp = serializedObject.FindProperty("Walls");
                
                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 35;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("North"), new GUIContent("N"), GUILayout.Width(55));
                EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("South"), new GUIContent("S"), GUILayout.Width(55));
                EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("East"), new GUIContent("E"), GUILayout.Width(55));
                EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("West"), new GUIContent("W"), GUILayout.Width(55));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("NW"), new GUIContent("NW"), GUILayout.Width(55));
                EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("NE"), new GUIContent("NE"), GUILayout.Width(55));
                EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("SW"), new GUIContent("SW"), GUILayout.Width(55));
                EditorGUILayout.PropertyField(wallsProp.FindPropertyRelative("SE"), new GUIContent("SE"), GUILayout.Width(55));
                EditorGUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = oldLabelWidth;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("WallCascadeOnHoles"), new GUIContent("Wall Cascade On Holes"));
            }
            EndSection(_showWalls);

            if (DrawSectionHeader("Interactive Editor", ref _showInteractiveEditor))
            {
                EditorGUILayout.HelpBox("Click tiles to cycle types. In 'Visuals' tab, click walls to customize them individually.", MessageType.Info);

                if (data.Width > 0 && data.Height > 0)
                {
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                    DrawMapPreview(data, true);
                    DrawPalette(data);
                    DrawSpawnPointConfig(data);
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.HelpBox("Width and Height must be greater than 0 for preview.", MessageType.Warning);
                }
            }
            EndSection(_showInteractiveEditor);
            
            if (DrawSectionHeader("Layout Tools", ref _showTools))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Flip H")) Flip(data, true); 
                if (GUILayout.Button("Flip V")) Flip(data, false);
                if (GUILayout.Button("Rotate 90")) Rotate(data);
                if (GUILayout.Button("Refresh")) { EditorUtility.SetDirty(data); GUI.changed = true; }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Shift N", GUILayout.Width(60))) Shift(data, 0, 1);
                if (GUILayout.Button("Shift S", GUILayout.Width(60))) Shift(data, 0, -1);
                if (GUILayout.Button("Shift E", GUILayout.Width(60))) Shift(data, 1, 0);
                if (GUILayout.Button("Shift W", GUILayout.Width(60))) Shift(data, -1, 0);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear Manual"))
                {
                    if (EditorUtility.DisplayDialog("Clear Layout", "Are you sure?", "Yes", "No"))
                    {
                        Undo.RecordObject(data, "Clear Manual Layout");
                        data.ManualLayoutData.Clear();
                        data.UseManualLayout = false;
                        EditorUtility.SetDirty(data);
                    }
                }
                if (GUILayout.Button("Capture Random")) CaptureRandomToManual(data);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Sync Data Points"))
                {
                    Undo.RecordObject(data, "Sync Data Points");
                    SyncPointsFromLayout(data);
                    EditorUtility.SetDirty(data);
                }
                if (GUILayout.Button("Auto-Assign Exits"))
                {
                    Undo.RecordObject(data, "Auto-Assign All Nearest Exits");
                    for (int i = 0; i < data.SpawnPoints.Count; i++) {
                        var s = data.SpawnPoints[i];
                        s.TargetExitIndex = -1;
                        data.SpawnPoints[i] = s;
                    }
                    SyncPointsFromLayout(data);
                    EditorUtility.SetDirty(data);
                }
                EditorGUILayout.EndHorizontal();
            }
            EndSection(_showTools);

            if (DrawSectionHeader("Visualization & Pathing", ref _showPathing))
            {
                data.ShowPathing = EditorGUILayout.Toggle("Show Pathing Paths", data.ShowPathing);
                if (data.ShowPathing)
                {
                    EditorGUILayout.HelpBox("Showing Ground (Orange) and Flying (Cyan) paths.", MessageType.None);
                    _pathingSimulationEnemy = (EnemyData)EditorGUILayout.ObjectField("Simulation Enemy", _pathingSimulationEnemy, typeof(EnemyData), false);
                }
            }
            EndSection(_showPathing);

            if (DrawSectionHeader("Prefab Generation", ref _showGeneration))
            {
                if (GUILayout.Button("Generate Map Prefab", GUILayout.Height(30)))
                {
                    GenerateMapPrefab(data);
                }
            }
            EndSection(_showGeneration);
        }

        private void DrawVisualsTab(MapData data)
        {
            if (DrawSectionHeader("Global Wall Visuals", ref _showGlobalVisuals))
            {
                SerializedProperty wallVisualsProp = serializedObject.FindProperty("WallVisuals");
                EditorGUILayout.PropertyField(wallVisualsProp.FindPropertyRelative("WallPrefab"));
                EditorGUILayout.PropertyField(wallVisualsProp.FindPropertyRelative("WallMaterial"));
                EditorGUILayout.PropertyField(wallVisualsProp.FindPropertyRelative("WallScale"), new GUIContent("Wall Scale (X=Thick, Y=Height, Z=Length)"));
                EditorGUILayout.PropertyField(wallVisualsProp.FindPropertyRelative("WallOffset"), new GUIContent("Wall Global Offset"));
                EditorGUILayout.PropertyField(wallVisualsProp.FindPropertyRelative("SeamlessCorners"), new GUIContent("Seamless Wall Corners (Fix Gaps)"));
            }
            EndSection(_showGlobalVisuals);

            if (DrawSectionHeader("Side & Edge Overrides", ref _showSideOverridesHeader))
            {
                DrawSideOverrides(data);
            }
            EndSection(_showSideOverridesHeader);

            if (DrawSectionHeader("Bulk Actions & Cleanup", ref _showBulkActions))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear All Walls"))
                {
                    if (EditorUtility.DisplayDialog("Clear Wall Textures", "Are you sure?", "Yes", "No"))
                    {
                        Undo.RecordObject(data, "Clear All Wall Textures");
                        for (int i = 0; i < data.SideVisualOverrides.Count; i++) {
                            var so = data.SideVisualOverrides[i];
                            so.TextureOverride = null;
                            data.SideVisualOverrides[i] = so;
                        }
                        for (int i = data.WallOverrides.Count - 1; i >= 0; i--) {
                            var wo = data.WallOverrides[i];
                            wo.TextureOverride = null;
                            if (!wo.OverrideScale && !wo.OverrideOffset && (wo.Decorations == null || wo.Decorations.Count == 0)) data.WallOverrides.RemoveAt(i);
                            else data.WallOverrides[i] = wo;
                        }
                    }
                }
                if (GUILayout.Button("Clear All Floors"))
                {
                    if (EditorUtility.DisplayDialog("Clear Tile Textures", "Are you sure?", "Yes", "No"))
                    {
                        Undo.RecordObject(data, "Clear All Tile Textures");
                        for (int i = data.VisualOverrides.Count - 1; i >= 0; i--) {
                            var to = data.VisualOverrides[i];
                            to.Texture = null;
                            if (to.Decorations == null || to.Decorations.Count == 0) data.VisualOverrides.RemoveAt(i);
                            else data.VisualOverrides[i] = to;
                        }
                    }
                }
                if (GUILayout.Button("Refresh View")) { EditorUtility.SetDirty(data); GUI.changed = true; }
                EditorGUILayout.EndHorizontal();
            }
            EndSection(_showBulkActions);

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

        private void DrawEnvironmentTab(MapData data)
        {
            if (DrawSectionHeader("Environment & Void Settings", ref _showEnvironmentLighting))
            {
                SerializedProperty envProp = serializedObject.FindProperty("Environment");
                if (envProp != null)
                {
                    SerializedProperty useGlobalBg = envProp.FindPropertyRelative("UseGlobalBackground");
                    SerializedProperty globalBgPrefab = envProp.FindPropertyRelative("GlobalBackgroundPrefab");
                    SerializedProperty globalBgHeight = envProp.FindPropertyRelative("GlobalBackgroundHeightOffset");
                    SerializedProperty fillVoidFog = envProp.FindPropertyRelative("FillVoidWithFog");
                    SerializedProperty tileFogPrefab = envProp.FindPropertyRelative("TileFogPrefab");
                    SerializedProperty tileFogHeight = envProp.FindPropertyRelative("TileFogHeightOffset");
                    SerializedProperty cameraBg = envProp.FindPropertyRelative("CameraBackground");
                    SerializedProperty cameraBgColor = envProp.FindPropertyRelative("CameraBackgroundColor");
                    SerializedProperty skyboxMaterial = envProp.FindPropertyRelative("SkyboxMaterial");

                    EditorGUILayout.LabelField("Environment Elements", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    // Global Background
                    EditorGUILayout.PropertyField(useGlobalBg);
                    if (useGlobalBg.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(globalBgPrefab);
                        EditorGUILayout.PropertyField(globalBgHeight);
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Space();

                    // Void Fog
                    EditorGUILayout.PropertyField(fillVoidFog);
                    if (fillVoidFog.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(tileFogPrefab);
                        EditorGUILayout.PropertyField(tileFogHeight);
                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();

                    // Camera Background
                    EditorGUILayout.LabelField("Camera Background Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(cameraBg);

                    if (cameraBg.enumValueIndex == (int)CameraBackgroundMode.SolidColor)
                    {
                        EditorGUILayout.PropertyField(cameraBgColor);
                    }
                    else if (cameraBg.enumValueIndex == (int)CameraBackgroundMode.Skybox)
                    {
                        EditorGUILayout.PropertyField(skyboxMaterial);
                    }
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Environment"));
                }

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Lighting"));
            }
            EndSection(_showEnvironmentLighting);
        }

        private bool _showSidesOverrides = false;
        private bool _showEdgesOverrides = false;

        private void DrawSideOverrides(MapData data)
        {
            SerializedProperty sideOverridesProp = serializedObject.FindProperty("SideVisualOverrides");
            
            // Define groups
            WallSide[] sides = new WallSide[] { WallSide.North, WallSide.South, WallSide.East, WallSide.West };
            WallSide[] edges = new WallSide[] { WallSide.NorthWest, WallSide.NorthEast, WallSide.SouthWest, WallSide.SouthEast };

            EditorGUI.indentLevel++;
            _showSidesOverrides = EditorGUILayout.Foldout(_showSidesOverrides, "Sides (N, S, E, W)", true, EditorStyles.foldoutHeader);
            if (_showSidesOverrides)
            {
                EditorGUI.indentLevel--;
                DrawWallSideGroup(data, sideOverridesProp, sides);
                EditorGUI.indentLevel++;
            }
            
            _showEdgesOverrides = EditorGUILayout.Foldout(_showEdgesOverrides, "Edges (NW, NE, SW, SE)", true, EditorStyles.foldoutHeader);
            if (_showEdgesOverrides)
            {
                EditorGUI.indentLevel--;
                DrawWallSideGroup(data, sideOverridesProp, edges);
                EditorGUI.indentLevel++;
            }
            EditorGUI.indentLevel--;
        }

        private void DrawWallSideGroup(MapData data, SerializedProperty sideOverridesProp, WallSide[] group)
        {
            foreach (WallSide side in group)
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
            
            EditorGUILayout.BeginHorizontal();
            Texture2D newBatchTexture = (Texture2D)EditorGUILayout.ObjectField("Apply Texture to All", commonTexture, typeof(Texture2D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (s_TextureClipboard != null)
            {
                if (GUILayout.Button("Paste", GUILayout.Width(50)))
                {
                    newBatchTexture = s_TextureClipboard;
                    GUI.changed = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            
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
            EditorGUILayout.BeginHorizontal();
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

            if (s_HasDecorationClipboard)
            {
                string clipboardName = s_DecorationClipboard.Prefab != null ? s_DecorationClipboard.Prefab.name : "None";
                if (GUILayout.Button($"+ Paste Decoration ({clipboardName}) to All", GUILayout.Height(30)))
                {
                    Undo.RecordObject(data, "Batch Paste Decoration");
                    foreach (var sel in _selection)
                    {
                        if (sel.Type == SelectionType.Tile) PasteTileDecoration(data, sel.TileCoord, s_DecorationClipboard);
                        else PasteWallDecoration(data, sel.WallSide, sel.WallIndex, s_DecorationClipboard);
                    }
                    EditorUtility.SetDirty(data);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Batch Apply Specific Decoration", EditorStyles.boldLabel);
            s_BatchDecoPrefab = (GameObject)EditorGUILayout.ObjectField("Decoration Prefab", s_BatchDecoPrefab, typeof(GameObject), false);
            s_BatchDecoOffset = EditorGUILayout.Vector3Field("Offset", s_BatchDecoOffset);
            s_BatchDecoRotation = EditorGUILayout.Vector3Field("Rotation", s_BatchDecoRotation);
            s_BatchDecoScale = EditorGUILayout.Vector3Field("Scale", s_BatchDecoScale);

            if (GUILayout.Button("Apply Decoration to All Selected", GUILayout.Height(30)))
            {
                if (s_BatchDecoPrefab == null)
                {
                    EditorUtility.DisplayDialog("Apply Decoration", "Please assign a Decoration Prefab first.", "OK");
                }
                else
                {
                    Undo.RecordObject(data, "Batch Apply Decoration");
                    foreach (var sel in _selection)
                    {
                        if (sel.Type == SelectionType.Tile)
                        {
                            ApplyTileDecorationBatch(data, sel.TileCoord, s_BatchDecoPrefab, s_BatchDecoOffset, s_BatchDecoRotation, s_BatchDecoScale);
                        }
                        else
                        {
                            ApplyWallDecorationBatch(data, sel.WallSide, sel.WallIndex, s_BatchDecoPrefab, s_BatchDecoOffset, s_BatchDecoRotation, s_BatchDecoScale);
                        }
                    }
                    EditorUtility.SetDirty(data);
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);

            if (GUILayout.Button("Clear Decorations for All Selected", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear Decorations", $"Are you sure you want to clear decorations for all {_selection.Count} selected items? (Textures will be kept)", "Yes", "No"))
                {
                    Undo.RecordObject(data, "Batch Clear Decorations");
                    foreach (var sel in _selection)
                    {
                        if (sel.Type == SelectionType.Tile) 
                        {
                            var idx = data.VisualOverrides.FindIndex(o => o.Coordinate == sel.TileCoord);
                            if (idx != -1) data.VisualOverrides[idx].Decorations?.Clear();
                        }
                        else 
                        {
                            var idx = data.WallOverrides.FindIndex(o => o.Side == sel.WallSide && o.Index == sel.WallIndex);
                            if (idx != -1) data.WallOverrides[idx].Decorations?.Clear();
                        }
                    }
                    EditorUtility.SetDirty(data);
                }
            }

            if (GUILayout.Button("Clear All Overrides for All Selected", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear All Selected", $"Are you sure you want to clear EVERYTHING for all {_selection.Count} selected items?", "Yes", "No"))
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

        private void PasteTileDecoration(MapData data, Vector2Int coord, DecorationData clipboardDeco)
        {
            int idx = data.VisualOverrides.FindIndex(o => o.Coordinate == coord);
            if (idx != -1)
            {
                if (data.VisualOverrides[idx].Decorations == null) {
                    var o = data.VisualOverrides[idx];
                    o.Decorations = new List<DecorationData>();
                    data.VisualOverrides[idx] = o;
                }
                data.VisualOverrides[idx].Decorations.Add(clipboardDeco);
            }
            else data.VisualOverrides.Add(new TileVisualOverride { Coordinate = coord, Decorations = new List<DecorationData> { clipboardDeco } });
        }

        private void PasteWallDecoration(MapData data, WallSide side, int index, DecorationData clipboardDeco)
        {
            int idx = data.WallOverrides.FindIndex(o => o.Side == side && o.Index == index);
            if (idx != -1)
            {
                if (data.WallOverrides[idx].Decorations == null) {
                    var o = data.WallOverrides[idx];
                    o.Decorations = new List<DecorationData>();
                    data.WallOverrides[idx] = o;
                }
                data.WallOverrides[idx].Decorations.Add(clipboardDeco);
            }
            else data.WallOverrides.Add(new WallVisualOverride { Side = side, Index = index, Decorations = new List<DecorationData> { clipboardDeco } });
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
                DrawDecorationsList(data, overrideProp.FindPropertyRelative("Decorations"), $"tile_{coord.x}_{coord.y}", coord);
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

        private void DrawDecorationsList(MapData data, SerializedProperty decosProp, string idPrefix, Vector2Int? sourceCoord = null)
        {
            EditorGUILayout.LabelField("Decorations", EditorStyles.miniBoldLabel);
            bool moved = false;
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
                
                if (sourceCoord.HasValue)
                {
                    EditorGUILayout.LabelField("Move:", GUILayout.Width(40));
                    if (GUILayout.Button("W", GUILayout.Width(25))) { MoveDecoration(data, sourceCoord.Value, i, Vector2Int.left); moved = true; }
                    if (GUILayout.Button("N", GUILayout.Width(25))) { MoveDecoration(data, sourceCoord.Value, i, Vector2Int.up); moved = true; }
                    if (GUILayout.Button("S", GUILayout.Width(25))) { MoveDecoration(data, sourceCoord.Value, i, Vector2Int.down); moved = true; }
                    if (GUILayout.Button("E", GUILayout.Width(25))) { MoveDecoration(data, sourceCoord.Value, i, Vector2Int.right); moved = true; }
                }

                if (GUILayout.Button("Copy", GUILayout.Width(50)))
                {
                    s_DecorationClipboard = new DecorationData
                    {
                        Prefab = prefabProp.objectReferenceValue as GameObject,
                        Offset = decoProp.FindPropertyRelative("Offset").vector3Value,
                        Rotation = decoProp.FindPropertyRelative("Rotation").vector3Value,
                        Scale = decoProp.FindPropertyRelative("Scale").vector3Value
                    };
                    s_HasDecorationClipboard = true;
                    Debug.Log($"[MapDataEditor] Copied decoration: {(s_DecorationClipboard.Prefab != null ? s_DecorationClipboard.Prefab.name : "None")}");
                }

                if (GUILayout.Button("Remove", GUILayout.Width(60))) { decosProp.DeleteArrayElementAtIndex(i); break; }
                EditorGUILayout.EndHorizontal();

                if (moved) break;

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

            if (moved)
            {
                serializedObject.Update();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add New Decoration")) { 
                decosProp.arraySize++; 
                SerializedProperty newDeco = decosProp.GetArrayElementAtIndex(decosProp.arraySize - 1);
                newDeco.FindPropertyRelative("Scale").vector3Value = Vector3.one;
                newDeco.FindPropertyRelative("Offset").vector3Value = Vector3.zero;
                newDeco.FindPropertyRelative("Rotation").vector3Value = Vector3.zero;
                newDeco.FindPropertyRelative("Prefab").objectReferenceValue = null;
            }

            if (s_HasDecorationClipboard)
            {
                string clipboardName = s_DecorationClipboard.Prefab != null ? s_DecorationClipboard.Prefab.name : "None";
                if (GUILayout.Button($"+ Paste Decoration ({clipboardName})"))
                {
                    decosProp.arraySize++;
                    SerializedProperty newDeco = decosProp.GetArrayElementAtIndex(decosProp.arraySize - 1);
                    newDeco.FindPropertyRelative("Prefab").objectReferenceValue = s_DecorationClipboard.Prefab;
                    newDeco.FindPropertyRelative("Offset").vector3Value = s_DecorationClipboard.Offset;
                    newDeco.FindPropertyRelative("Rotation").vector3Value = s_DecorationClipboard.Rotation;
                    newDeco.FindPropertyRelative("Scale").vector3Value = s_DecorationClipboard.Scale;
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void MoveDecoration(MapData data, Vector2Int fromCoord, int decoIndex, Vector2Int dir)
        {
            Vector2Int toCoord = fromCoord + dir;
            if (toCoord.x < 0 || toCoord.x >= data.Width || toCoord.y < 0 || toCoord.y >= data.Height) return; // OOB

            Undo.RecordObject(data, "Move Decoration");

            int fromIdx = data.VisualOverrides.FindIndex(o => o.Coordinate == fromCoord);
            if (fromIdx == -1) return;
            var fromOv = data.VisualOverrides[fromIdx];
            if (fromOv.Decorations == null || decoIndex >= fromOv.Decorations.Count) return;
            
            var deco = fromOv.Decorations[decoIndex];
            fromOv.Decorations.RemoveAt(decoIndex);
            if (fromOv.Texture == null && fromOv.Decorations.Count == 0) data.VisualOverrides.RemoveAt(fromIdx);
            else data.VisualOverrides[fromIdx] = fromOv;
            
            int toIdx = data.VisualOverrides.FindIndex(o => o.Coordinate == toCoord);
            if (toIdx == -1) {
                data.VisualOverrides.Add(new TileVisualOverride { Coordinate = toCoord, Decorations = new List<DecorationData> { deco } });
            } else {
                var toOv = data.VisualOverrides[toIdx];
                if (toOv.Decorations == null) toOv.Decorations = new List<DecorationData>();
                toOv.Decorations.Add(deco);
                data.VisualOverrides[toIdx] = toOv;
            }

            _selection.Clear();
            _selection.Add(new SelectionItem { Type = SelectionType.Tile, TileCoord = toCoord });
            EditorUtility.SetDirty(data);
            Repaint();
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
                    GUI.FocusControl(null);
                    Repaint();
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
                    string mark = "";
                    if (ovIdx != -1) {
                        var o = data.VisualOverrides[ovIdx];
                        bool hasTex = o.Texture != null;
                        bool hasDeco = o.Decorations != null && o.Decorations.Count > 0;
                        
                        if (hasTex && hasDeco) mark = "*+D";
                        else if (hasTex) mark = "*";
                        else if (hasDeco) mark = "D";
                    }
                    DrawItem(x, y, GetTileColor(data, coord), mark, SelectionType.Tile);
                }
            }

            if (data.ShowPathing)
            {
                DrawPathingVisualization(data, tileGridRect, cellW, cellH);
            }

            Random.state = oldState;
        }

        private void DrawPathingVisualization(MapData data, Rect tileGridRect, float cellW, float cellH)
        {
            // Gather all spawns and exits
            List<Vector2Int> actualSpawns = new List<Vector2Int>();
            List<Vector2Int> actualExits = new List<Vector2Int>();

            if (data.UseManualLayout)
            {
                foreach (var tile in data.ManualLayoutData)
                {
                    if (tile.Type == TileType.SpawnPoint || tile.Type == TileType.SpawnPointHigh)
                        actualSpawns.Add(tile.Coordinate);
                    if (tile.Type == TileType.ExitPoint || tile.Type == TileType.ExitPointHigh)
                        actualExits.Add(tile.Coordinate);
                }
            }
            else
            {
                // Fallback to legacy lists if not manual
                foreach (var s in data.SpawnPoints) actualSpawns.Add(s.Coordinate);
                foreach (var e in data.ExitPoints) actualExits.Add(e);
            }

            if (actualSpawns.Count == 0 || actualExits.Count == 0) return;

            Handles.BeginGUI();
            foreach (var start in actualSpawns)
            {
                // Get type at start
                TileType startType = TileType.None;
                if (data.UseManualLayout)
                {
                    var tile = data.ManualLayoutData.Find(d => d.Coordinate == start);
                    startType = tile.Type;
                }

                // Try to find target index from legacy SpawnPoints list if coordinate matches
                int targetIndex = -1;
                var legacySpawn = data.SpawnPoints.Find(s => s.Coordinate == start);
                if (legacySpawn.Coordinate == start) targetIndex = legacySpawn.TargetExitIndex;

                List<Vector2Int> targets = new List<Vector2Int>();
                
                // Smart pairing: Only match Ground to Ground, High to High
                bool isHighStart = startType == TileType.SpawnPointHigh;
                
                if (targetIndex >= 0 && targetIndex < actualExits.Count)
                {
                    // User specified a specific exit. Check if it matches.
                    Vector2Int exitCoord = actualExits[targetIndex];
                    TileType exitType = TileType.None;
                    if (data.UseManualLayout)
                    {
                        var tile = data.ManualLayoutData.Find(d => d.Coordinate == exitCoord);
                        exitType = tile.Type;
                    }
                    
                    bool isHighExit = exitType == TileType.ExitPointHigh;
                    if (isHighStart == isHighExit)
                    {
                        targets.Add(exitCoord);
                    }
                }
                
                // If no valid target yet, find the NEAREST matching one
                if (targets.Count == 0)
                {
                    Vector2Int nearestExit = Vector2Int.zero;
                    float minSqrDist = float.MaxValue;
                    bool found = false;

                    foreach (var exitCoord in actualExits)
                    {
                        TileType exitType = TileType.None;
                        if (data.UseManualLayout)
                        {
                            var tile = data.ManualLayoutData.Find(d => d.Coordinate == exitCoord);
                            if (tile.Coordinate == exitCoord) exitType = tile.Type;
                        }

                        bool isHighExit = exitType == TileType.ExitPointHigh;
                        if (isHighStart == isHighExit)
                        {
                            float sqrDist = (start - exitCoord).sqrMagnitude;
                            if (sqrDist < minSqrDist)
                            {
                                minSqrDist = sqrDist;
                                nearestExit = exitCoord;
                                found = true;
                            }
                        }
                    }

                    if (found)
                    {
                        targets.Add(nearestExit);
                    }
                }

                foreach (var target in targets)
                {
                    // Only show the path relevant to the spawn type to reduce clutter
                    if (isHighStart)
                    {
                        // High Ground Spawn -> High Ground Exit: Usually for Flying or High-path units
                        var flyingPath = GetPathInEditor(data, start, target, MaouSamaTD.Units.EnemyMovementType.Flying);
                        if (flyingPath != null) 
                        {
                            DrawPathLine(flyingPath, Color.cyan, 2f, data, tileGridRect, cellW, cellH, 0);
                            DrawPathSimulationLabel(flyingPath, Color.cyan, data, tileGridRect, cellW, cellH);
                        }
                    }
                    else
                    {
                        // Regular Spawn -> Regular Exit: Ground path
                        var groundPath = GetPathInEditor(data, start, target, MaouSamaTD.Units.EnemyMovementType.Ground);
                        if (groundPath != null) 
                        {
                            DrawPathLine(groundPath, Color.orange, 2f, data, tileGridRect, cellW, cellH, 0);
                            DrawPathSimulationLabel(groundPath, Color.orange, data, tileGridRect, cellW, cellH);
                        }
                        
                        // If user specifically wants to see flying paths from ground spawns, 
                        // we could add a toggle, but for now we follow the "don't show 2 arrows" request.
                    }
                }
            }
            Handles.EndGUI();

            // Add Legend Hint
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Pathing Legend:", EditorStyles.boldLabel);
            var rectOrange = EditorGUILayout.GetControlRect(false, 16);
            EditorGUI.DrawRect(new Rect(rectOrange.x, rectOrange.y + 4, 12, 8), Color.orange);
            EditorGUI.LabelField(new Rect(rectOrange.x + 20, rectOrange.y, rectOrange.width - 20, 16), "Ground Path (Spawn -> Exit)");
            
            var rectCyan = EditorGUILayout.GetControlRect(false, 16);
            EditorGUI.DrawRect(new Rect(rectCyan.x, rectCyan.y + 4, 12, 8), Color.cyan);
            EditorGUI.LabelField(new Rect(rectCyan.x + 20, rectCyan.y, rectCyan.width - 20, 16), "Flying/High Path (SpawnHigh -> ExitHigh)");
            EditorGUILayout.EndVertical();
        }

        private void DrawPathLine(List<Vector2Int> path, Color color, float width, MapData data, Rect tileGridRect, float cellW, float cellH, float offset)
        {
            if (path == null || path.Count < 2) return;

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector2 p1 = GetCellCenter(path[i], data, tileGridRect, cellW, cellH) + new Vector2(offset, offset);
                Vector2 p2 = GetCellCenter(path[i + 1], data, tileGridRect, cellW, cellH) + new Vector2(offset, offset);
                Handles.color = color;
                Handles.DrawAAConvexPolygon(
                    p1 + new Vector2(-width, -width),
                    p1 + new Vector2(width, width),
                    p2 + new Vector2(width, width),
                    p2 + new Vector2(-width, -width)
                );
            }
            
            // Draw arrow at end
            Vector2 end = GetCellCenter(path[path.Count - 1], data, tileGridRect, cellW, cellH) + new Vector2(offset, offset);
            Vector2 prev = GetCellCenter(path[path.Count - 2], data, tileGridRect, cellW, cellH) + new Vector2(offset, offset);
            Vector2 dir = (end - prev).normalized;
            Vector2 side = new Vector2(-dir.y, dir.x);
            Handles.DrawAAConvexPolygon(
                end,
                end - dir * 10 + side * 5,
                end - dir * 10 - side * 5
            );
        }

        private void DrawPathSimulationLabel(List<Vector2Int> path, Color color, MapData data, Rect tileGridRect, float cellW, float cellH)
        {
            if (_pathingSimulationEnemy == null || path == null || path.Count < 2) return;

            float speed = Mathf.Max(0.1f, _pathingSimulationEnemy.MoveSpeed);

            // 1. Draw intermediate time tick labels every 3 blocks to represent flow
            GUIStyle tickStyle = new GUIStyle(EditorStyles.boldLabel);
            tickStyle.normal.textColor = new Color(color.r * 0.9f, color.g * 0.9f, color.b * 0.9f, 0.85f);
            tickStyle.fontSize = 9;

            for (int i = 1; i < path.Count - 1; i++)
            {
                if (i % 3 == 0)
                {
                    float cumulativeTime = i / speed;
                    Vector2 nodePos = GetCellCenter(path[i], data, tileGridRect, cellW, cellH);
                    string nodeText = $"+{cumulativeTime:F1}s";
                    Vector2 nodeSize = tickStyle.CalcSize(new GUIContent(nodeText));

                    // Small background for readability
                    EditorGUI.DrawRect(new Rect(nodePos.x - nodeSize.x * 0.5f - 1, nodePos.y - nodeSize.y * 0.5f - 1, nodeSize.x + 2, nodeSize.y + 2), new Color(0, 0, 0, 0.45f));
                    GUI.Label(new Rect(nodePos.x - nodeSize.x * 0.5f, nodePos.y - nodeSize.y * 0.5f, nodeSize.x, nodeSize.y), nodeText, tickStyle);
                }
            }

            // 2. Draw final total arrival time at the destination (exit node)
            float distance = path.Count - 1;
            float totalTime = distance / speed;

            Vector2 exitPos = GetCellCenter(path[path.Count - 1], data, tileGridRect, cellW, cellH);
            
            GUIStyle finalStyle = new GUIStyle(EditorStyles.boldLabel);
            finalStyle.normal.textColor = color;
            finalStyle.fontSize = 11;
            
            string text = $"{totalTime:F1}s ({distance} blocks)";
            Vector2 size = finalStyle.CalcSize(new GUIContent(text));
            
            // Draw a beautiful background highlight for the final result
            EditorGUI.DrawRect(new Rect(exitPos.x - size.x * 0.5f - 3, exitPos.y - size.y * 0.5f - 3, size.x + 6, size.y + 6), new Color(0, 0, 0, 0.75f));
            GUI.Label(new Rect(exitPos.x - size.x * 0.5f, exitPos.y - size.y * 0.5f, size.x, size.y), text, finalStyle);
        }

        private Vector2 GetCellCenter(Vector2Int coord, MapData data, Rect tileGridRect, float cellW, float cellH)
        {
            return new Vector2(
                tileGridRect.x + (coord.x * cellW) + cellW * 0.5f,
                tileGridRect.y + ((data.Height - 1 - coord.y) * cellH) + cellH * 0.5f
            );
        }

        private List<Vector2Int> GetPathInEditor(MapData data, Vector2Int start, Vector2Int end, MaouSamaTD.Units.EnemyMovementType moveType)
        {
            // Simple BFS for editor pathing
            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            frontier.Enqueue(start);

            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            cameFrom[start] = start;

            bool found = false;
            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                if (current == end) { found = true; break; }

                foreach (Vector2Int next in GetNeighborsInEditor(data, current, moveType))
                {
                    if (!cameFrom.ContainsKey(next))
                    {
                        frontier.Enqueue(next);
                        cameFrom[next] = current;
                    }
                }
            }

            if (!found) return null;

            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int curr = end;
            while (curr != start)
            {
                path.Add(curr);
                curr = cameFrom[curr];
            }
            path.Add(start);
            path.Reverse();
            return path;
        }

        private IEnumerable<Vector2Int> GetNeighborsInEditor(MapData data, Vector2Int current, MaouSamaTD.Units.EnemyMovementType moveType)
        {
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            foreach (var dir in dirs)
            {
                Vector2Int next = current + dir;
                if (next.x < 0 || next.x >= data.Width || next.y < 0 || next.y >= data.Height) continue;

                TileType type = TileType.Walkable;
                if (data.UseManualLayout)
                {
                    int idx = data.ManualLayoutData.FindIndex(d => d.Coordinate == next);
                    if (idx != -1) type = data.ManualLayoutData[idx].Type;
                    else type = TileType.None;
                }
                else
                {
                    Random.State tempState = Random.state;
                    Random.InitState(data.MapSeed + next.x * 1000 + next.y);
                    bool isHighGround = Random.value < data.HighGroundChance;
                    if (next.y == 0 || next.y == data.Height - 1) isHighGround = true;
                    Random.state = tempState;
                    type = isHighGround ? TileType.HighGround : TileType.Walkable;
                }

                bool isWalkable = true;
                if (moveType == MaouSamaTD.Units.EnemyMovementType.Ground)
                {
                    // Ground can only walk on low ground (Walkable, LowTile, SpawnPoint, ExitPoint)
                    // High ground, Walls, and Void are blocked.
                    if (type == TileType.HighGround || type == TileType.DecoHighGround || 
                        type == TileType.SpawnPointHigh || type == TileType.ExitPointHigh ||
                        type == TileType.None || type == TileType.Wall || type == TileType.NonWalkableDecor)
                    {
                        isWalkable = false;
                    }
                }
                else if (moveType == MaouSamaTD.Units.EnemyMovementType.Mixed)
                {
                    // Mixed can walk on both low and high ground, but blocked by walls and void
                    if (type == TileType.None || type == TileType.Wall || type == TileType.NonWalkableDecor)
                        isWalkable = false;
                }
                else if (moveType == MaouSamaTD.Units.EnemyMovementType.Flying)
                {
                    // Flying can go anywhere except out of bounds (None)
                    if (type == TileType.None) isWalkable = false;
                }

                if (isWalkable) yield return next;
            }
        }

        private void GenerateMapPrefab(MapData data)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Map Prefab", $"Map_{data.name}", "prefab", "Select where to save the generated map prefab", "Assets/_Game/Prefabs/Maps");
            if (string.IsNullOrEmpty(path)) return;

            // Create temporary generation root
            GameObject root = new GameObject($"Map_{data.name}_Root");
            
            // Create Containers
            GameObject gridContainer = new GameObject("Grid");
            gridContainer.transform.SetParent(root.transform);
            GameObject wallContainer = new GameObject("Walls");
            wallContainer.transform.SetParent(root.transform);

            // Add GridManager
            MaouSamaTD.Grid.GridManager gridManager = root.AddComponent<MaouSamaTD.Grid.GridManager>();
            
            // Find default tile prefab
            Tile tilePrefab = AssetDatabase.LoadAssetAtPath<Tile>("Assets/_Game/Visuals/Map/Prefabs/TilePrefab.prefab");
            if (tilePrefab == null)
            {
                // Try searching as fallback
                string[] guids = AssetDatabase.FindAssets("t:GameObject TilePrefab");
                if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:GameObject Tile");
                foreach (var guid in guids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    if (p.EndsWith("TilePrefab.prefab") || p.EndsWith("Tile.prefab"))
                    {
                        tilePrefab = AssetDatabase.LoadAssetAtPath<Tile>(p);
                        break;
                    }
                }
            }

            // Private field access via reflection since they are serialized but private
            var managerType = typeof(MaouSamaTD.Grid.GridManager);
            managerType.GetField("_tilePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(gridManager, tilePrefab);
            managerType.GetField("_gridContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(gridManager, gridContainer.transform);
            managerType.GetField("_wallContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(gridManager, wallContainer.transform);

            // Add GridGenerator
            MaouSamaTD.Grid.GridGenerator gridGenerator = root.AddComponent<MaouSamaTD.Grid.GridGenerator>();
            var genType = typeof(MaouSamaTD.Grid.GridGenerator);
            genType.GetField("_gridManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(gridGenerator, gridManager);
            genType.GetField("_mapData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(gridGenerator, data);

            // Generate
            gridGenerator.GenerateMap();

            // Cleanup components we don't want in the final prefab
            DestroyImmediate(gridGenerator);
            // gridManager.RecalculateBounds(); 

            // Save Prefab
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, path);
            
            // Cleanup scene
            DestroyImmediate(root);

            AssetDatabase.Refresh();
            
            if (prefabAsset != null)
            {
                EditorGUIUtility.PingObject(prefabAsset);
                EditorUtility.DisplayDialog("Map Prefab Created", $"Successfully generated and saved map prefab to:\n{path}", "OK");
            }
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
                return GetTileColor(TileType.None); // Match pathfinder fallback
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
            string selectionText = $"Selection: {_selection.Count} Tiles";
            if (_selection.Count == 1)
            {
                selectionText += $" ({_selection[0].TileCoord.x}, {_selection[0].TileCoord.y})";
            }
            else if (_selection.Count > 1)
            {
                List<string> coords = new List<string>();
                for (int i = 0; i < Mathf.Min(_selection.Count, 5); i++)
                {
                    coords.Add($"({_selection[i].TileCoord.x}, {_selection[i].TileCoord.y})");
                }
                selectionText += $" {string.Join(", ", coords)}";
                if (_selection.Count > 5)
                {
                    selectionText += ", ...";
                }
            }
            EditorGUILayout.LabelField(selectionText, EditorStyles.miniBoldLabel);
            
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
            SyncPointsFromLayout(data); // Sync lists and auto-pair nearest exits
            EditorUtility.SetDirty(data);
        }

        private void SyncPointsFromLayout(MapData data)
        {
            if (!data.UseManualLayout) return;

            // 1. Discovery from Layout
            List<Vector2Int> foundExits = new List<Vector2Int>();
            List<Vector2Int> foundExitsHigh = new List<Vector2Int>();
            List<Vector2Int> foundSpawns = new List<Vector2Int>();
            List<Vector2Int> foundSpawnsHigh = new List<Vector2Int>();

            foreach (var tile in data.ManualLayoutData)
            {
                if (tile.Type == TileType.SpawnPoint) foundSpawns.Add(tile.Coordinate);
                else if (tile.Type == TileType.SpawnPointHigh) foundSpawnsHigh.Add(tile.Coordinate);
                else if (tile.Type == TileType.ExitPoint) foundExits.Add(tile.Coordinate);
                else if (tile.Type == TileType.ExitPointHigh) foundExitsHigh.Add(tile.Coordinate);
            }

            // 2. Update ExitPoints list
            List<Vector2Int> allFoundExits = new List<Vector2Int>();
            allFoundExits.AddRange(foundExits);
            allFoundExits.AddRange(foundExitsHigh);
            data.ExitPoints = allFoundExits;

            // 3. Update SpawnPoints list (keeping existing data for TargetExitIndex)
            List<SpawnPointData> newSpawnList = new List<SpawnPointData>();
            List<Vector2Int> allFoundSpawns = new List<Vector2Int>();
            allFoundSpawns.AddRange(foundSpawns);
            allFoundSpawns.AddRange(foundSpawnsHigh);

            foreach (var coord in allFoundSpawns)
            {
                int existingIdx = data.SpawnPoints.FindIndex(s => s.Coordinate == coord);
                if (existingIdx != -1)
                {
                    newSpawnList.Add(data.SpawnPoints[existingIdx]);
                }
                else
                {
                    newSpawnList.Add(new SpawnPointData { Coordinate = coord, TargetExitIndex = -1 });
                }
            }
            data.SpawnPoints = newSpawnList;

            // 4. Auto-assign nearest for those with -1
            for (int i = 0; i < data.SpawnPoints.Count; i++)
            {
                var s = data.SpawnPoints[i];
                if (s.TargetExitIndex == -1)
                {
                    // Find type of this spawn
                    var tile = data.ManualLayoutData.Find(t => t.Coordinate == s.Coordinate);
                    bool isHigh = tile.Type == TileType.SpawnPointHigh;

                    // Search for nearest exit of same type
                    Vector2Int nearestExit = Vector2Int.zero;
                    float minSqrDist = float.MaxValue;
                    int nearestIdx = -1;

                    for (int j = 0; j < data.ExitPoints.Count; j++)
                    {
                        var exitCoord = data.ExitPoints[j];
                        var exitTile = data.ManualLayoutData.Find(t => t.Coordinate == exitCoord);
                        bool isExitHigh = exitTile.Type == TileType.ExitPointHigh;

                        if (isHigh == isExitHigh)
                        {
                            float sqrDist = (s.Coordinate - exitCoord).sqrMagnitude;
                            if (sqrDist < minSqrDist)
                            {
                                minSqrDist = sqrDist;
                                nearestExit = exitCoord;
                                nearestIdx = j;
                            }
                        }
                    }

                    if (nearestIdx != -1)
                    {
                        s.TargetExitIndex = nearestIdx;
                        data.SpawnPoints[i] = s;
                    }
                }
            }
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
        private bool DrawSectionHeader(string label, ref bool foldout)
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.foldout);
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.fontSize = 12;
            
            GUILayout.BeginVertical("helpbox");
            foldout = EditorGUILayout.Foldout(foldout, label, true, headerStyle);
            if (foldout) EditorGUILayout.Space(2);
            return foldout;
        }

        private void ApplyTileDecorationBatch(MapData data, Vector2Int coord, GameObject prefab, Vector3 offset, Vector3 rotation, Vector3 scale)
        {
            int idx = data.VisualOverrides.FindIndex(o => o.Coordinate == coord);
            TileVisualOverride to;
            if (idx != -1)
            {
                to = data.VisualOverrides[idx];
            }
            else
            {
                to = new TileVisualOverride
                {
                    Coordinate = coord,
                    Texture = null,
                    Decorations = new List<DecorationData>()
                };
                data.VisualOverrides.Add(to);
                idx = data.VisualOverrides.Count - 1;
            }

            if (to.Decorations == null)
            {
                to.Decorations = new List<DecorationData>();
            }

            DecorationData newDeco = new DecorationData
            {
                Prefab = prefab,
                Offset = offset,
                Rotation = rotation,
                Scale = scale
            };
            to.Decorations.Add(newDeco);
            data.VisualOverrides[idx] = to;
        }

        private void ApplyWallDecorationBatch(MapData data, WallSide side, int index, GameObject prefab, Vector3 offset, Vector3 rotation, Vector3 scale)
        {
            int idx = data.WallOverrides.FindIndex(o => o.Side == side && o.Index == index);
            WallVisualOverride wo;
            if (idx != -1)
            {
                wo = data.WallOverrides[idx];
            }
            else
            {
                wo = new WallVisualOverride
                {
                    Side = side,
                    Index = index,
                    TextureOverride = null,
                    Decorations = new List<DecorationData>()
                };
                data.WallOverrides.Add(wo);
                idx = data.WallOverrides.Count - 1;
            }

            if (wo.Decorations == null)
            {
                wo.Decorations = new List<DecorationData>();
            }

            DecorationData newDeco = new DecorationData
            {
                Prefab = prefab,
                Offset = offset,
                Rotation = rotation,
                Scale = scale
            };
            wo.Decorations.Add(newDeco);
            data.WallOverrides[idx] = wo;
        }

        private void EndSection(bool foldout)
        {
            if (foldout) EditorGUILayout.Space(5);
            GUILayout.EndVertical();
            GUILayout.Space(2);
        }
    }
}
