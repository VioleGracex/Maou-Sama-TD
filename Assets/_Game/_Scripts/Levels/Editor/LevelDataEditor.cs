using UnityEngine;
using UnityEditor;
using MaouSamaTD.Levels;

namespace MaouSamaTD.Levels.Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        private LevelData _target;

        // Static allows the foldout states to persist while clicking between different levels in the project
        private static bool _showIdentity = true;
        private static bool _showSetup = true;
        private static bool _showRoster = true;
        private static bool _showMapWaves = true;

        private void OnEnable()
        {
            _target = (LevelData)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Toggle button for Default/Custom inspector
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.normal.textColor = _target.useDefaultInspector ? Color.gray : new Color(0.1f, 0.7f, 0.2f);
            buttonStyle.fontSize = 12;

            if (GUILayout.Button(_target.useDefaultInspector ? "Switch to Custom Editor" : "Switch to Default Editor", buttonStyle, GUILayout.Height(30)))
            {
                _target.useDefaultInspector = !_target.useDefaultInspector;
                EditorUtility.SetDirty(_target);
            }

            EditorGUILayout.Space();

            if (_target.useDefaultInspector)
            {
                // Draw all properties like default inspector, but explicitly make UniqueID read-only
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
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // Increase label width so long property names (e.g. "Delay Before Next Wave" or "Spawn Interval") 
            // aren't truncated, making the object input fields smaller as requested
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 210f; // Adjusted width to prevent label hiding

            // Draw custom editor
            DrawCustomInspector();

            // Restore original width just in case
            EditorGUIUtility.labelWidth = originalLabelWidth;

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCustomInspector()
        {
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontStyle = FontStyle.Bold;
            foldoutStyle.fontSize = 13;

            // Level Identification
            GUILayout.BeginVertical("helpbox");
            EditorGUI.indentLevel++;
            _showIdentity = EditorGUILayout.Foldout(_showIdentity, "Level Identification", true, foldoutStyle);
            if (_showIdentity)
            {
                EditorGUILayout.Space(2);
                
                using (new EditorGUI.DisabledScope(true))
                {
                    DrawProperty("UniqueID", "Unique ID", "Permanent generic GUID for this scriptable object.");
                }

                DrawProperty("LevelIndex", "Integer Index", "Numeric index used for Addressables or logic (e.g., 1, 2, 3)");
                DrawProperty("LevelID", "String ID", "String identifier. Naming convention: [Chapter]-[Level] (e.g., 1-1, 1-2, 2-1)");
                DrawProperty("LevelName", "Level Name");
                
                EditorGUILayout.LabelField("Description");
                SerializedProperty descProp = serializedObject.FindProperty("Description");
                descProp.stringValue = EditorGUILayout.TextArea(descProp.stringValue, EditorStyles.textArea, GUILayout.Height(50));
            }
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            // Setup
            GUILayout.BeginVertical("helpbox");
            EditorGUI.indentLevel++;
            _showSetup = EditorGUILayout.Foldout(_showSetup, "Rules & Setup", true, foldoutStyle);
            if (_showSetup)
            {
                EditorGUILayout.Space(2);
                DrawProperty("GracePeriod", "Grace Period (Sec)", "Time before the first wave begins.");
                DrawProperty("StartingAuthoritySeals", "Starting Authority (Economy)");
                DrawProperty("AuthoritySealsPerSecond", "Authority Per Second", "Economy generation speed.");
                
                EditorGUILayout.Space(5);
                DrawProperty("WinRewards", "Level Win Rewards", "List of rewards granted upon clearing the level.");
            }
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            // Units
            GUILayout.BeginVertical("helpbox");
            EditorGUI.indentLevel++;
            _showRoster = EditorGUILayout.Foldout(_showRoster, "Unit Roster Settings", true, foldoutStyle);
            if (_showRoster)
            {
                EditorGUILayout.Space(2);
                DrawProperty("PremadeCohort", "Premade Cohort (Base Roster)", "Forced units for this level.");
                DrawProperty("IsCohortLocked", "Lock Premade Cohort", "Prevent player from changing them.");
                EditorGUILayout.Space(5);
                DrawProperty("SupportAssistant", "Support Assistant (11th Unit)", "Additional helper unit.");
                DrawProperty("IsAssistantLocked", "Lock Assistant Unit");
                EditorGUILayout.Space(5);
                DrawProperty("MaleSovereignRites", "Male Rites", "The rites available for Male Maou.");
                DrawProperty("FemaleSovereignRites", "Female Rites", "The rites available for Female Maou.");
                DrawProperty("IsRitesLocked", "Lock Sovereign Rites");
            }
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            // Map & Waves
            GUILayout.BeginVertical("helpbox");
            EditorGUI.indentLevel++;
            _showMapWaves = EditorGUILayout.Foldout(_showMapWaves, "Map & Encounter", true, foldoutStyle);
            if (_showMapWaves)
            {
                EditorGUILayout.Space(2);
                DrawProperty("MapData", "Linked Map Data");
                EditorGUILayout.Space(5);
                DrawProperty("Waves", "Enemy Waves");
            }
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
        }

        private void DrawProperty(string propName, string customLabel = null, string tooltip = null)
        {
            SerializedProperty prop = serializedObject.FindProperty(propName);
            if (prop != null)
            {
                if (string.IsNullOrEmpty(customLabel))
                    EditorGUILayout.PropertyField(prop, true);
                else
                    EditorGUILayout.PropertyField(prop, new GUIContent(customLabel, tooltip), true);
            }
            else
            {
                EditorGUILayout.HelpBox($"Property '{propName}' not found.", MessageType.Error);
            }
        }
    }
}
