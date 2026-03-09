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
        private Vector2Int _selectedTile = new Vector2Int(-1, -1);
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MapSeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Width"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Height"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("HighGroundChance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("UseManualLayout"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Map Preview & Interactive Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Click tiles to cycle types: \nWalkable -> High Ground -> Deco Walkable -> Deco High Ground.", MessageType.Info);

            if (data.Width <= 0 || data.Height <= 0)
            {
                EditorGUILayout.HelpBox("Width and Height must be greater than 0 for preview.", MessageType.Warning);
                return;
            }

            DrawMapPreview(data, false);

            EditorGUILayout.Space();
            
            // Layout Tools
            EditorGUILayout.LabelField("Layout Tools", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Flip Horizontal")) Flip(data, true); 
            if (GUILayout.Button("Flip Vertical")) Flip(data, false);
            if (GUILayout.Button("Rotate 90 CW")) Rotate(data);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Manual Layout"))
            {
                Undo.RecordObject(data, "Clear Manual Layout");
                data.ManualHighGround.Clear();
                data.DecoratedWalkable.Clear();
                data.DecoratedHighGround.Clear();
                data.UseManualLayout = false;
                EditorUtility.SetDirty(data);
            }
            if (GUILayout.Button("Capture Random to Manual"))
            {
                CaptureRandomToManual(data);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Legend:", EditorStyles.miniBoldLabel);
            DrawLegendItem("Spawn Point", Color.red);
            DrawLegendItem("Exit Point", Color.green);
            DrawLegendItem("Walkable", Color.white);
            DrawLegendItem("High Ground", Color.gray);
            DrawLegendItem("Deco Walkable (Unusable)", new Color(0.7f, 0.7f, 1f));
            DrawLegendItem("Deco High Ground (Unusable)", new Color(0.3f, 0.3f, 0.3f));
            EditorGUILayout.EndVertical();
        }

        private void DrawVisualsTab(MapData data)
        {
            EditorGUILayout.LabelField("Tile Visual Customization", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select a tile to override its texture or add a decoration prefab.", MessageType.Info);

            if (data.Width <= 0 || data.Height <= 0)
            {
                EditorGUILayout.HelpBox("Width and Height must be greater than 0 for preview.", MessageType.Warning);
                return;
            }

            DrawMapPreview(data, true);

            EditorGUILayout.Space();

            if (_selectedTile.x >= 0 && _selectedTile.y >= 0)
            {
                DrawTileCustomizer(data, _selectedTile);
            }
            else
            {
                EditorGUILayout.HelpBox("Click a tile to customize visuals.", MessageType.None);
            }
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

            // Get SerializedProperty for the override
            SerializedProperty visualOverridesProp = serializedObject.FindProperty("VisualOverrides");
            SerializedProperty overrideProp = null;
            
            if (overrideIndex != -1)
            {
                overrideProp = visualOverridesProp.GetArrayElementAtIndex(overrideIndex);
            }

            EditorGUI.BeginChangeCheck();
            
            if (overrideProp != null)
            {
                SerializedProperty texProp = overrideProp.FindPropertyRelative("Texture");
                EditorGUILayout.PropertyField(texProp, new GUIContent("Base Texture"));
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
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Decorations List", EditorStyles.boldLabel);

            if (overrideProp != null)
            {
                SerializedProperty decosProp = overrideProp.FindPropertyRelative("Decorations");
                
                for (int i = 0; i < decosProp.arraySize; i++)
                {
                    SerializedProperty decoProp = decosProp.GetArrayElementAtIndex(i);
                    SerializedProperty prefabProp = decoProp.FindPropertyRelative("Prefab");
                    
                    string foldoutKey = $"{coord.x}_{coord.y}_{i}";
                    if (!_decoFoldouts.ContainsKey(foldoutKey)) _decoFoldouts[foldoutKey] = true;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.BeginHorizontal();
                    string label = prefabProp.objectReferenceValue != null ? prefabProp.objectReferenceValue.name : $"Decoration {i}";
                    _decoFoldouts[foldoutKey] = EditorGUILayout.Foldout(_decoFoldouts[foldoutKey], label, true, EditorStyles.foldoutHeader);
                    
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        decosProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    if (_decoFoldouts[foldoutKey])
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(prefabProp, new GUIContent("Prefab"));
                        EditorGUILayout.PropertyField(decoProp.FindPropertyRelative("Offset"));
                        EditorGUILayout.PropertyField(decoProp.FindPropertyRelative("Rotation"));
                        EditorGUILayout.PropertyField(decoProp.FindPropertyRelative("Scale"));
                        EditorGUI.indentLevel--;
                        EditorGUILayout.Space(2);
                    }
                    
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(2);
                if (GUILayout.Button("+ Add New Decoration", GUILayout.Height(25)))
                {
                    decosProp.arraySize++;
                    SerializedProperty newDeco = decosProp.GetArrayElementAtIndex(decosProp.arraySize - 1);
                    newDeco.FindPropertyRelative("Scale").vector3Value = Vector3.one;
                    newDeco.FindPropertyRelative("Offset").vector3Value = Vector3.zero;
                    newDeco.FindPropertyRelative("Rotation").vector3Value = Vector3.zero;
                    newDeco.FindPropertyRelative("Prefab").objectReferenceValue = null;
                }
            }
            else
            {
                if (GUILayout.Button("+ Add Initial Decoration", GUILayout.Height(25)))
                {
                    Undo.RecordObject(data, "Add Tile Override");
                    data.VisualOverrides.Add(new TileVisualOverride { 
                        Coordinate = coord, 
                        Decorations = new List<DecorationData> { DecorationData.Default } 
                    });
                    EditorUtility.SetDirty(data);
                    serializedObject.Update();
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space(10);
            GUI.backgroundColor = new Color(1, 0.5f, 0.5f);
            if (GUILayout.Button("Clear All Overrides for Tile", GUILayout.Height(20)))
            {
                if (overrideIndex != -1 && EditorUtility.DisplayDialog("Clear Overrides", "Are you sure you want to remove all visual overrides for this tile?", "Yes", "No"))
                {
                    Undo.RecordObject(data, "Clear Tile Visuals");
                    data.VisualOverrides.RemoveAt(overrideIndex);
                    EditorUtility.SetDirty(data);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void DrawMapPreview(MapData data, bool isVisualMode)
        {
            float availableWidth = EditorGUIUtility.currentViewWidth - 40 - LabelSpace;
            
            float cellW = Mathf.Min(availableWidth / data.Height, MaxCellSize);
            float cellH = cellW;

            float gridWidth = cellW * data.Height;
            float gridHeight = cellH * data.Width;

            Rect outerRect = GUILayoutUtility.GetRect(gridWidth + LabelSpace, gridHeight + LabelSpace);
            outerRect.x += (availableWidth + LabelSpace - (gridWidth + LabelSpace)) / 2f;

            Rect gridRect = new Rect(outerRect.x + LabelSpace, outerRect.y, gridWidth, gridHeight);
            EditorGUI.DrawRect(gridRect, new Color(0.1f, 0.1f, 0.1f, 1f));

            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };

            // Labels
            for (int y = 0; y < data.Height; y++)
            {
                Rect labelRect = new Rect(gridRect.x + (data.Height - 1 - y) * cellW, gridRect.y + gridRect.height, cellW, LabelSpace);
                EditorGUI.LabelField(labelRect, y.ToString(), labelStyle);
            }
            for (int x = 0; x < data.Width; x++)
            {
                Rect labelRect = new Rect(outerRect.x, gridRect.y + x * cellH, LabelSpace, cellH);
                EditorGUI.LabelField(labelRect, x.ToString(), labelStyle);
            }

            Event e = Event.current;
            bool clicked = (e.type == EventType.MouseDown) && gridRect.Contains(e.mousePosition);

            Random.State oldState = Random.state;
            Random.InitState(data.MapSeed);

            for (int y = 0; y < data.Height; y++)
            {
                for (int x = 0; x < data.Width; x++)
                {
                    Vector2Int coord = new Vector2Int(x, y);
                    Color color = GetTileColor(data, coord);

                    Rect cellRect = new Rect(
                        gridRect.x + ((data.Height - 1 - y) * cellW) + CellPadding,
                        gridRect.y + (x * cellH) + CellPadding,
                        cellW - CellPadding * 2,
                        cellH - CellPadding * 2
                    );

                    // Highlight selection in visual mode
                    if (isVisualMode && _selectedTile == coord)
                    {
                        EditorGUI.DrawRect(cellRect, Color.yellow);
                        Rect innerRect = new Rect(cellRect.x + 2, cellRect.y + 2, cellRect.width - 4, cellRect.height - 4);
                        EditorGUI.DrawRect(innerRect, color);
                    }
                    else
                    {
                        EditorGUI.DrawRect(cellRect, color);
                    }

                    // Visual override indicators
                    bool hasOverride = false;
                    foreach(var o in data.VisualOverrides) 
                        if(o.Coordinate == coord) { hasOverride = true; break; }

                    if (hasOverride)
                    {
                        EditorGUI.LabelField(cellRect, "*", new GUIStyle(EditorStyles.boldLabel) { 
                            alignment = TextAnchor.UpperRight, 
                            normal = { textColor = Color.yellow } 
                        });
                    }

                    if (clicked && cellRect.Contains(e.mousePosition))
                    {
                        if (isVisualMode)
                        {
                            _selectedTile = coord;
                        }
                        else
                        {
                            CycleTile(data, coord);
                        }
                        e.Use();
                    }
                }
            }
            Random.state = oldState;
        }

        private Color GetTileColor(MapData data, Vector2Int coord)
        {
            if (data.SpawnPoints.Contains(coord)) return Color.red;
            if (data.ExitPoints.Contains(coord)) return Color.green;

            if (data.UseManualLayout)
            {
                if (data.ManualHighGround.Contains(coord)) return Color.gray;
                if (data.DecoratedWalkable.Contains(coord)) return new Color(0.7f, 0.7f, 1f);
                if (data.DecoratedHighGround.Contains(coord)) return new Color(0.3f, 0.3f, 0.3f);
                return Color.white;
            }

            Random.State tempState = Random.state;
            Random.InitState(data.MapSeed + coord.x * 1000 + coord.y);
            bool isHighGround = Random.value < data.HighGroundChance;
            if (coord.y == 0 || coord.y == data.Height - 1) isHighGround = true;
            Random.state = tempState;
            return isHighGround ? Color.gray : Color.white;
        }

        private void CycleTile(MapData data, Vector2Int coord)
        {
            Undo.RecordObject(data, "Cycle Map Tile");
            if (!data.UseManualLayout) CaptureRandomToManual(data);

            if (data.ManualHighGround.Contains(coord))
            {
                data.ManualHighGround.Remove(coord);
                data.DecoratedWalkable.Add(coord);
            }
            else if (data.DecoratedWalkable.Contains(coord))
            {
                data.DecoratedWalkable.Remove(coord);
                data.DecoratedHighGround.Add(coord);
            }
            else if (data.DecoratedHighGround.Contains(coord))
            {
                data.DecoratedHighGround.Remove(coord);
            }
            else
            {
                data.ManualHighGround.Add(coord);
            }

            data.UseManualLayout = true;
            EditorUtility.SetDirty(data);
        }

        private void CaptureRandomToManual(MapData data)
        {
            Undo.RecordObject(data, "Capture Random to Manual");
            data.ManualHighGround.Clear();
            data.DecoratedWalkable.Clear();
            data.DecoratedHighGround.Clear();
            
            Random.State oldState = Random.state;
            Random.InitState(data.MapSeed);
            for (int x = 0; x < data.Width; x++)
            {
                for (int y = 0; y < data.Height; y++)
                {
                    bool isHighGround = Random.value < data.HighGroundChance;
                    if (y == 0 || y == data.Height - 1) isHighGround = true;
                    if (isHighGround) data.ManualHighGround.Add(new Vector2Int(x, y));
                }
            }
            data.UseManualLayout = true;
            Random.state = oldState;
            EditorUtility.SetDirty(data);
        }

        private void Flip(MapData data, bool horizontal)
        {
            Undo.RecordObject(data, horizontal ? "Flip Horizontal" : "Flip Vertical");
            TransformCoordSet(data.ManualHighGround, horizontal, data.Width, data.Height);
            TransformCoordSet(data.DecoratedWalkable, horizontal, data.Width, data.Height);
            TransformCoordSet(data.DecoratedHighGround, horizontal, data.Width, data.Height);
            TransformCoordSet(data.SpawnPoints, horizontal, data.Width, data.Height);
            TransformCoordSet(data.ExitPoints, horizontal, data.Width, data.Height);
            EditorUtility.SetDirty(data);
        }

        private void TransformCoordSet(List<Vector2Int> coords, bool horizontal, int w, int h)
        {
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
            RotateCoordSet(data.ManualHighGround, oldW, oldH);
            RotateCoordSet(data.DecoratedWalkable, oldW, oldH);
            RotateCoordSet(data.DecoratedHighGround, oldW, oldH);
            RotateCoordSet(data.SpawnPoints, oldW, oldH);
            RotateCoordSet(data.ExitPoints, oldW, oldH);
            data.Width = oldH;
            data.Height = oldW;
            EditorUtility.SetDirty(data);
        }

        private void RotateCoordSet(List<Vector2Int> coords, int w, int h)
        {
            for (int i = 0; i < coords.Count; i++)
            {
                Vector2Int c = coords[i];
                int newX = c.y;
                int newY = (w - 1) - c.x;
                coords[i] = new Vector2Int(newX, newY);
            }
        }

        private void DrawLegendItem(string label, Color color)
        {
            Rect r = EditorGUILayout.GetControlRect(false, 16);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 16, 16), color);
            EditorGUI.LabelField(new Rect(r.x + 20, r.y, r.width - 20, r.height), label);
        }
    }
}
