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
        private readonly string[] _tabNames = { "General", "Economy", "Units", "Encounter", "Story" };

        // Section Foldouts
        private static bool _showIdentity = true;
        private static bool _showTutorial = true;
        private static bool _showTiming = true;
        private static bool _showEconomy = true;
        private static bool _showRewards = true;
        private static bool _showUnits = true;
        private static bool _showRites = true;
        private static bool _showMapSettings = true;
        private static bool _showWaves = true;
        private static bool _showStarConditions = true;
        private static bool _showWinLose = true;
        private static bool _showCinematics = true;
        private static bool _showStorySettings = true;

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
                case 4: DrawStoryTab(); break;
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
            if (DrawSectionHeader("Identity & Info", ref _showIdentity))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    DrawProperty("UniqueID", "Unique ID", "Permanent generic GUID for this scriptable object.");
                }

                DrawProperty("LevelIndex", "Integer Index");
                DrawProperty("LevelID", "String ID (e.g. 1-1)");
                DrawProperty("LevelName", "Level Name");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Objective (Sovereign / Base)", EditorStyles.miniBoldLabel);
                DrawProperty("SovereignHpNameKey", "Objective Name Key", "Localization key for the objective HP bar (e.g. SovereignHP_Level2)");
                DrawProperty("SovereignMaxHp", "Objective Max HP", "Total health of the sovereign/base for this level.");
                
                EditorGUILayout.LabelField("Description");
                SerializedProperty descProp = serializedObject.FindProperty("Description");
                descProp.stringValue = EditorGUILayout.TextArea(descProp.stringValue, EditorStyles.textArea, GUILayout.Height(60));
            }
            EndSection(_showIdentity);

            if (DrawSectionHeader("Tutorial Settings", ref _showTutorial))
            {
                SerializedProperty hasTutorialProp = serializedObject.FindProperty("HasTutorial");
                EditorGUILayout.PropertyField(hasTutorialProp, new GUIContent("Enable Tutorial"));
                
                if (hasTutorialProp.boolValue)
                {
                    DrawProperty("TutorialData", "Tutorial Sequence");
                }
            }
            EndSection(_showTutorial);
        }

        private void DrawEconomyTab()
        {
            if (DrawSectionHeader("Timing & Rules", ref _showTiming))
            {
                DrawProperty("GracePeriod", "Grace Period (Sec)");
            }
            EndSection(_showTiming);

            if (DrawSectionHeader("Economy (Authority Seals)", ref _showEconomy))
            {
                DrawProperty("StartingAuthoritySeals", "Starting Amount");
                DrawProperty("MaxAuthoritySeals", "Max Capacity");
                DrawProperty("AuthoritySealsPerSecond", "Passive Generation");
            }
            EndSection(_showEconomy);

            if (DrawSectionHeader("Rewards", ref _showRewards))
            {
                DrawProperty("WinRewards", "Level Win Rewards");
                DrawProperty("StageLootConfig", "Stage Completion Loot");
                DrawProperty("MissionXP", "Mission Base XP");
            }
            EndSection(_showRewards);
        }

        private void DrawUnitsTab()
        {
            if (DrawSectionHeader("Unit Roster Settings", ref _showUnits))
            {
                DrawProperty("PremadeCohort", "Premade Cohort");
                DrawProperty("IsCohortLocked", "Lock Cohort Slots");
                EditorGUILayout.Space(5);
                DrawProperty("SupportAssistant", "Support Assistant");
                DrawProperty("IsAssistantLocked", "Lock Assistant Slot");
            }
            EndSection(_showUnits);

            if (DrawSectionHeader("Sovereign Rites", ref _showRites))
            {
                DrawProperty("MaleSovereignRites", "Male Rites");
                DrawProperty("FemaleSovereignRites", "Female Rites");
                DrawProperty("IsRitesLocked", "Lock Rites Selection");
            }
            EndSection(_showRites);
        }

        private void DrawEncounterTab()
        {
            if (DrawSectionHeader("Map Settings", ref _showMapSettings))
            {
                DrawProperty("MapData", "Linked Map Data");
            }
            EndSection(_showMapSettings);

            if (DrawSectionHeader("Enemy Waves", ref _showWaves))
            {
                DrawWavesProperty();
            }
            EndSection(_showWaves);

            if (DrawSectionHeader("Star Conditions", ref _showStarConditions))
            {
                DrawProperty("StarConditions", "Conditions List");
            }
            EndSection(_showStarConditions);

            if (DrawSectionHeader("Win / Lose Logic", ref _showWinLose))
            {
                DrawProperty("WinConditions", "Custom Win Conditions");
                DrawProperty("LoseConditions", "Custom Lose Conditions");
            }
            EndSection(_showWinLose);

            if (DrawSectionHeader("Cinematics", ref _showCinematics))
            {
                DrawProperty("EnableCinematicCombatEnd", "Enable Slow-Mo Victory");
                DrawProperty("CinematicDuration", "Slow-Mo Duration");
            }
            EndSection(_showCinematics);
        }

        private void DrawStoryTab()
        {
            if (DrawSectionHeader("Story Settings", ref _showStorySettings))
            {
                SerializedProperty hasStoryProp = serializedObject.FindProperty("HasStory");
                EditorGUILayout.PropertyField(hasStoryProp, new GUIContent("Enable Story Cutscenes"));

                if (hasStoryProp.boolValue)
                {
                    EditorGUILayout.Space(5);
                    DrawProperty("IntroStory", "Intro Sequence (Start)");
                    DrawProperty("OutroStory", "Outro Sequence (End)");
                }
            }
            EndSection(_showStorySettings);
        }

        private void DrawWavesProperty()
        {
            SerializedProperty wavesProp = serializedObject.FindProperty("Waves");
            if (wavesProp == null) return;

            EditorGUILayout.LabelField("Wave List", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            // Header for array size
            int newSize = EditorGUILayout.IntField("Wave Count", wavesProp.arraySize);
            if (newSize != wavesProp.arraySize) wavesProp.arraySize = newSize;

            for (int i = 0; i < wavesProp.arraySize; i++)
            {
                SerializedProperty waveProp = wavesProp.GetArrayElementAtIndex(i);
                
                // Construct a nice label: "Wave 1: [Message]"
                string message = waveProp.FindPropertyRelative("WaveMessage").stringValue;
                var preStory = waveProp.FindPropertyRelative("PreWaveStory").objectReferenceValue;
                var postStory = waveProp.FindPropertyRelative("PostWaveStory").objectReferenceValue;
                
                string label = $"Wave {i + 1}";
                if (!string.IsNullOrEmpty(message)) label += $" ({message})";
                if (preStory != null || postStory != null) label += " [STORY]";
                
                EditorGUILayout.PropertyField(waveProp, new GUIContent(label), true);
            }
            
            EditorGUI.indentLevel--;
        }

        private bool DrawSectionHeader(string label, ref bool foldout)
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.foldout);
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.fontSize = 12;
            
            GUILayout.BeginVertical("helpbox");
            foldout = EditorGUILayout.Foldout(foldout, label, true, headerStyle);
            if (foldout) EditorGUILayout.Space(2);
            return foldout;
        }

        private void EndSection(bool foldout)
        {
            if (foldout) EditorGUILayout.Space(5);
            GUILayout.EndVertical();
            GUILayout.Space(2);
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
