using UnityEngine;
using UnityEditor;
using MaouSamaTD.Levels;

namespace MaouSamaTD.Levels.Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        private LevelData _target;

        private static int _selectedTab = 0;
        private readonly string[] _tabNames = { "General", "Economy", "Units", "Encounter" };

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
                DrawDefaultInspectorWithReadOnlyID();
                return;
            }

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 210f;

            // Tab Selection
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, GUILayout.Height(25));
            EditorGUILayout.Space(5);

            switch (_selectedTab)
            {
                case 0: DrawGeneralTab(); break;
                case 1: DrawEconomyTab(); break;
                case 2: DrawUnitsTab(); break;
                case 3: DrawEncounterTab(); break;
            }

            EditorGUIUtility.labelWidth = originalLabelWidth;
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
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGeneralTab()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Identity & Info", headerStyle);
            EditorGUI.indentLevel++;
            
            using (new EditorGUI.DisabledScope(true))
            {
                DrawProperty("UniqueID", "Unique ID", "Permanent generic GUID for this scriptable object.");
            }

            DrawProperty("LevelIndex", "Integer Index");
            DrawProperty("LevelID", "String ID (e.g. 1-1)");
            DrawProperty("LevelName", "Level Name");
            
            EditorGUILayout.LabelField("Description");
            SerializedProperty descProp = serializedObject.FindProperty("Description");
            descProp.stringValue = EditorGUILayout.TextArea(descProp.stringValue, EditorStyles.textArea, GUILayout.Height(60));
            
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Tutorial Settings", headerStyle);
            EditorGUI.indentLevel++;
            SerializedProperty hasTutorialProp = serializedObject.FindProperty("HasTutorial");
            EditorGUILayout.PropertyField(hasTutorialProp, new GUIContent("Enable Tutorial"));
            
            if (hasTutorialProp.boolValue)
            {
                DrawProperty("TutorialData", "Tutorial Sequence");
            }
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
        }

        private void DrawEconomyTab()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Timing & Rules", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("GracePeriod", "Grace Period (Sec)");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Economy (Authority Seals)", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("StartingAuthoritySeals", "Starting Amount");
            DrawProperty("MaxAuthoritySeals", "Max Capacity");
            DrawProperty("AuthoritySealsPerSecond", "Passive Generation");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Rewards", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("WinRewards", "Level Win Rewards");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
        }

        private void DrawUnitsTab()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Unit Roster Settings", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("PremadeCohort", "Premade Cohort");
            DrawProperty("IsCohortLocked", "Lock Cohort Slots");
            EditorGUILayout.Space(5);
            DrawProperty("SupportAssistant", "Support Assistant");
            DrawProperty("IsAssistantLocked", "Lock Assistant Slot");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Sovereign Rites", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("MaleSovereignRites", "Male Rites");
            DrawProperty("FemaleSovereignRites", "Female Rites");
            DrawProperty("IsRitesLocked", "Lock Rites Selection");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
        }

        private void DrawEncounterTab()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Map Settings", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("MapData", "Linked Map Data");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Enemy Waves", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("Waves", "Wave List");
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
