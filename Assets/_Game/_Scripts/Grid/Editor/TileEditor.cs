using UnityEngine;
using UnityEditor;
using MaouSamaTD.Grid;
using System.Reflection;

namespace MaouSamaTD.Editor
{
    [CustomEditor(typeof(Tile))]
    [CanEditMultipleObjects]
    public class TileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Tile tile = (Tile)target;

            GUILayout.Space(10);
            GUILayout.Label("Map Editing", EditorStyles.boldLabel);

            if (GUILayout.Button("Set as Spawn Point"))
            {
                tile.SetType(TileType.Spawn);
            }

            if (GUILayout.Button("Set as Exit Point"))
            {
                tile.SetType(TileType.Exit);
            }

            if (GUILayout.Button("Set as Walkable (Low)"))
            {
                tile.SetType(TileType.Walkable);
            }

            if (GUILayout.Button("Set as High Ground"))
            {
                tile.SetType(TileType.HighGround);
            }
        }
    }
}
