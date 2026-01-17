using UnityEngine;
using UnityEditor;
using MaouSamaTD.Managers;

namespace MaouSamaTD.Editor
{
    [CustomEditor(typeof(DebugManager))]
    public class DebugManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DebugManager script = (DebugManager)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Global Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Damage All Units"))
            {
                script.DamageAllUnits();
            }

            if (GUILayout.Button("Heal All Units"))
            {
                script.HealAllUnits();
            }

            if (GUILayout.Button("Retreat All Units"))
            {
                script.RetreatAllUnits();
            }
        }
    }
}
