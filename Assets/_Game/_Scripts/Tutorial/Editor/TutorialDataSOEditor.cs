using UnityEditor;
using UnityEngine;

namespace MaouSamaTD.Tutorial
{
    [CustomEditor(typeof(TutorialDataSO))]
    public class TutorialDataSOEditor : UnityEditor.Editor
    {
        private static int _selectedStepIndex = 0;

        public override void OnInspectorGUI()
        {
            TutorialDataSO data = (TutorialDataSO)target;
            serializedObject.Update();

            // Header for switching (Button style)
            GUIStyle toggleButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            toggleButtonStyle.normal.textColor = data.ShowCustomEditor ? new Color(0.1f, 0.7f, 0.2f) : Color.gray;

            if (GUILayout.Button(data.ShowCustomEditor ? "Switch to Default Editor" : "Switch to Custom Editor", toggleButtonStyle, GUILayout.Height(30)))
            {
                data.ShowCustomEditor = !data.ShowCustomEditor;
                EditorUtility.SetDirty(data);
            }

            EditorGUILayout.Space();

            if (data.ShowCustomEditor)
            {
                DrawCustomTutorialEditor(data);
            }
            else
            {
                DrawDefaultInspector();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCustomTutorialEditor(TutorialDataSO data)
        {
            SerializedProperty stepsProp = serializedObject.FindProperty("Steps");
            
            if (stepsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No tutorial steps defined.", MessageType.Info);
                if (GUILayout.Button("Add First Step"))
                {
                    stepsProp.InsertArrayElementAtIndex(0);
                    _selectedStepIndex = 0;
                }
                return;
            }

            // Tabs / Navigation
            EditorGUILayout.LabelField("Tutorial Steps Navigation", EditorStyles.boldLabel);
            
            string[] stepNames = new string[stepsProp.arraySize];
            for (int i = 0; i < stepsProp.arraySize; i++)
            {
                SerializedProperty step = stepsProp.GetArrayElementAtIndex(i);
                string stepName = step.FindPropertyRelative("StepName").stringValue;
                if (string.IsNullOrEmpty(stepName)) stepName = $"Step {i}";
                stepNames[i] = $"{i}: {stepName}";
            }

            // Using a scroll view for the toolbar if there are many steps
            int columns = Mathf.Min(stepsProp.arraySize, 4);
            _selectedStepIndex = GUILayout.SelectionGrid(_selectedStepIndex, stepNames, columns);

            EditorGUILayout.Space(10);

            // Bounds check
            if (_selectedStepIndex >= stepsProp.arraySize) _selectedStepIndex = stepsProp.arraySize - 1;
            if (_selectedStepIndex < 0) _selectedStepIndex = 0;

            // Draw Selected Step
            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField($"Editing Step {_selectedStepIndex}: {stepNames[_selectedStepIndex]}", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            SerializedProperty selectedStep = stepsProp.GetArrayElementAtIndex(_selectedStepIndex);
            EditorGUILayout.PropertyField(selectedStep, true);

            EditorGUILayout.Space(10);
            
            // Management Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Delete This Step", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Delete Step", $"Are you sure you want to delete step {_selectedStepIndex}?", "Delete", "Cancel"))
                {
                    stepsProp.DeleteArrayElementAtIndex(_selectedStepIndex);
                    if (_selectedStepIndex >= stepsProp.arraySize && stepsProp.arraySize > 0)
                        _selectedStepIndex = stepsProp.arraySize - 1;
                    return;
                }
            }

            if (GUILayout.Button("Add New Step (End)", GUILayout.Height(25)))
            {
                int newIndex = stepsProp.arraySize;
                stepsProp.InsertArrayElementAtIndex(newIndex);
                _selectedStepIndex = newIndex;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Use the navigation grid above to switch between steps. This view prevents accidental edits to the wrong step.", MessageType.None);
        }
    }
}
