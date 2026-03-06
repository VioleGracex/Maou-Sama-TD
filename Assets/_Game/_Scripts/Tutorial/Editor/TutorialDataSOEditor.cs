using UnityEditor;
using UnityEngine;

namespace MaouSamaTD.Tutorial
{
    [CustomEditor(typeof(TutorialDataSO))]
    public class TutorialDataSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            TutorialDataSO data = (TutorialDataSO)target;

            // Header for switching
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            data.ShowCustomEditor = EditorGUILayout.ToggleLeft("Use Custom Tutorial Editor", data.ShowCustomEditor, EditorStyles.boldLabel);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(data);
            }
            EditorGUILayout.Space();

            if (data.ShowCustomEditor)
            {
                // Custom Editor logic (simplified for now, mimicking NaughtyAttributes but with a toggle)
                DrawCustomTutorialEditor(data);
            }
            else
            {
                // Default Inspector
                DrawDefaultInspector();
            }
        }

        private void DrawCustomTutorialEditor(TutorialDataSO data)
        {
            EditorGUILayout.HelpBox("Custom Tutorial Editor is active. This view provides a streamlined way to manage tutorial steps. Disable 'Use Custom Tutorial Editor' to see the standard list.", MessageType.Info);
            
            // For now, we still show the default inspector but we can add more specific custom UI here later.
            // The requirement was to make it togglable.
            DrawDefaultInspector();
        }
    }
}
