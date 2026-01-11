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

            if (GUILayout.Button("Set as Spawn Point"))
            {
                foreach (var obj in targets)
                {
                    ((Tile)obj).SetType(TileType.Spawn);
                }
            }

            if (GUILayout.Button("Set as Exit Point"))
            {
                foreach (var obj in targets)
                {
                    ((Tile)obj).SetType(TileType.Exit);
                }
            }

            if (GUILayout.Button("Set as Walkable (Low)"))
            {
                foreach (var obj in targets)
                {
                    ((Tile)obj).SetType(TileType.Walkable);
                }
            }

            if (GUILayout.Button("Set as High Ground"))
            {
                foreach (var obj in targets)
                {
                    ((Tile)obj).SetType(TileType.HighGround);
                }
            }
        }
    }
}
