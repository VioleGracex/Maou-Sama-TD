using UnityEngine;
using UnityEditor;
using MaouSamaTD.Grid;

namespace MaouSamaTD.Editor
{
    [CustomEditor(typeof(GridGenerator))]
    public class GridGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GridGenerator generator = (GridGenerator)target;

            GUILayout.Space(10);
            GUILayout.Label("Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate Map", GUILayout.Height(30)))
            {
                generator.GenerateMap();
            }

            if (GUILayout.Button("Clear Map", GUILayout.Height(30)))
            {
                generator.ClearMap();
            }
        }
    }
}
