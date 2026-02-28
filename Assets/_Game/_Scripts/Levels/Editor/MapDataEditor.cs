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

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MapData data = (MapData)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Map Preview & Interactive Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Click tiles to cycle types: \nWalkable -> High Ground -> Deco Walkable -> Deco High Ground. \nEditing automatically enables 'Use Manual Layout'.", MessageType.Info);

            if (data.Width <= 0 || data.Height <= 0)
            {
                EditorGUILayout.HelpBox("Width and Height must be greater than 0 for preview.", MessageType.Warning);
                return;
            }

            DrawMapPreview(data);

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

        private void DrawMapPreview(MapData data)
        {
            float availableWidth = EditorGUIUtility.currentViewWidth - 40 - LabelSpace;
            float cellW = Mathf.Min(availableWidth / data.Width, MaxCellSize);
            float cellH = cellW;

            float gridWidth = cellW * data.Width;
            float gridHeight = cellH * data.Height;

            Rect outerRect = GUILayoutUtility.GetRect(gridWidth + LabelSpace, gridHeight + LabelSpace);
            
            // Center if small
            outerRect.x += (availableWidth + LabelSpace - (gridWidth + LabelSpace)) / 2f;

            Rect gridRect = new Rect(outerRect.x + LabelSpace, outerRect.y, gridWidth, gridHeight);
            
            EditorGUI.DrawRect(gridRect, new Color(0.1f, 0.1f, 0.1f, 1f));

            // Draw Labels
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.alignment = TextAnchor.MiddleCenter;

            // Y Labels
            for (int y = 0; y < data.Height; y++)
            {
                Rect labelRect = new Rect(outerRect.x, gridRect.y + (gridRect.height - (y + 1) * cellH), LabelSpace, cellH);
                EditorGUI.LabelField(labelRect, y.ToString(), labelStyle);
            }

            // X Labels
            for (int x = 0; x < data.Width; x++)
            {
                Rect labelRect = new Rect(gridRect.x + x * cellW, gridRect.y + gridRect.height, cellW, LabelSpace);
                EditorGUI.LabelField(labelRect, x.ToString(), labelStyle);
            }

            // Handle Clicks
            Event e = Event.current;
            bool clicked = (e.type == EventType.MouseDown) && gridRect.Contains(e.mousePosition);

            // Seeded random state for background (if not manual)
            Random.State oldState = Random.state;
            Random.InitState(data.MapSeed);

            for (int x = 0; x < data.Width; x++)
            {
                for (int y = 0; y < data.Height; y++)
                {
                    Vector2Int coord = new Vector2Int(x, y);
                    Color color = Color.white;
                    
                    if (data.UseManualLayout)
                    {
                        if (data.ManualHighGround.Contains(coord)) color = Color.gray;
                        else if (data.DecoratedWalkable.Contains(coord)) color = new Color(0.7f, 0.7f, 1f);
                        else if (data.DecoratedHighGround.Contains(coord)) color = new Color(0.3f, 0.3f, 0.3f);
                        else color = Color.white;
                    }
                    else
                    {
                        bool isHighGround = Random.value < data.HighGroundChance;
                        if (y == 0 || y == data.Height - 1) isHighGround = true;
                        color = isHighGround ? Color.gray : Color.white;
                    }

                    if (data.SpawnPoints.Contains(coord)) color = Color.red;
                    if (data.ExitPoints.Contains(coord)) color = Color.green;

                    Rect cellRect = new Rect(
                        gridRect.x + (x * cellW) + CellPadding,
                        gridRect.y + (gridRect.height - (y + 1) * cellH) + CellPadding,
                        cellW - CellPadding * 2,
                        cellH - CellPadding * 2
                    );

                    EditorGUI.DrawRect(cellRect, color);

                    // Check for click on this cell
                    if (clicked && cellRect.Contains(e.mousePosition))
                    {
                        CycleTile(data, coord);
                        e.Use(); // Consume event
                    }
                }
            }

            Random.state = oldState;
        }

        private void CycleTile(MapData data, Vector2Int coord)
        {
            Undo.RecordObject(data, "Cycle Map Tile");
            
            if (!data.UseManualLayout)
            {
                CaptureRandomToManual(data);
            }

            // Cycle: Walkable (Normal) -> HighGround -> DecoratedWalkable -> DecoratedHighGround -> Walkable
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
                // Back to Walkable (which is just empty lists)
            }
            else
            {
                // Is Walkable, go to HighGround
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
                // (x, y) -> (y, (W-1)-x)
                int newX = c.y;
                int newY = (w - 1) - c.x;
                coords[i] = new Vector2Int(newX, newY);
            }
        }

        private void DrawLegendItem(string label, Color color)
        {
            Rect r = EditorGUILayout.GetControlRect(false, 16);
            Rect colorRect = new Rect(r.x, r.y, 16, 16);
            EditorGUI.DrawRect(colorRect, color);
            EditorGUI.LabelField(new Rect(r.x + 20, r.y, r.width - 20, r.height), label);
        }
    }
}
