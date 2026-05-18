using UnityEngine;
using UnityEditor;
using MaouSamaTD.Levels;
using MaouSamaTD.UI.MainMenu;

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
                
                SerializedProperty posProp = serializedObject.FindProperty("CampaignMapPosition");
                if (posProp != null)
                {
                    EditorGUILayout.PropertyField(posProp, new GUIContent("Campaign Map Position", "Pixel coordinates on the 2048x1143 Gehenna map. X: 0..2048, Y: 0..1143"));
                    DrawMapCoordinatePicker(posProp);
                }
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

        private void DrawMapCoordinatePicker(SerializedProperty posProp)
        {
            Texture2D mapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Gehenna.png");
            if (mapTexture == null)
            {
                EditorGUILayout.HelpBox("Map texture not found at Assets/_Game/Art/Gehenna.png", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Interactive Map Position Picker (Click/Drag to Set)", EditorStyles.miniBoldLabel);

            // Calculate sizing keeping aspect ratio (2048 / 1143)
            float aspect = 2048f / 1143f;
            float padding = 30f;
            float width = EditorGUIUtility.currentViewWidth - padding;
            // Clamp width to a reasonable maximum to avoid huge inspector blocks
            width = Mathf.Min(width, 400f);
            float height = width / aspect;

            // Rect for the map drawing
            Rect mapRect = GUILayoutUtility.GetRect(width, height);
            
            // Draw background frame / box
            GUI.Box(new Rect(mapRect.x - 2, mapRect.y - 2, mapRect.width + 4, mapRect.height + 4), GUIContent.none, EditorStyles.helpBox);
            
            // Draw the map texture
            GUI.DrawTexture(mapRect, mapTexture, ScaleMode.ScaleToFit);

            // Get current coordinate
            Vector2 currentPos = posProp.vector2Value;
            
            // Calculate marker GUI position relative to the mapRect
            // Since coordinates start from bottom-left (0,0) to top-right (2048,1143)
            float markerPctX = currentPos.x / 2048f;
            float markerPctY = 1f - (currentPos.y / 1143f); // Invert Y for GUI

            float markerGuiX = mapRect.x + (markerPctX * mapRect.width);
            float markerGuiY = mapRect.y + (markerPctY * mapRect.height);

            // Draw a beautiful crosshair/marker at this position
            Color oldColor = GUI.color;
            
            // Draw the marker circle
            GUI.color = new Color(1.0f, 0.2f, 0.1f, 0.9f); // Hot demonic crimson
            Rect markerRect = new Rect(markerGuiX - 8, markerGuiY - 8, 16, 16);
            
            // Load selection circle icon if it exists, otherwise fall back to standard radio button
            Texture2D dotTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/UI/Icons/Circle.png");
            if (dotTex != null)
            {
                GUI.DrawTexture(markerRect, dotTex);
            }
            else
            {
                GUI.Box(markerRect, GUIContent.none, EditorStyles.radioButton);
            }
            
            // Draw visual coordinate text over the marker or below it
            GUI.color = Color.white;
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.normal.textColor = Color.black;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = 9;

            // Subdued background shadow label
            GUI.Label(new Rect(markerGuiX - 40, markerGuiY - 26, 80, 20), $"({(int)currentPos.x}, {(int)currentPos.y})", labelStyle);
            labelStyle.normal.textColor = new Color(1.0f, 0.6f, 0.1f, 1f); // Flame gold
            GUI.Label(new Rect(markerGuiX - 40, markerGuiY - 25, 80, 20), $"({(int)currentPos.x}, {(int)currentPos.y})", labelStyle);

            GUI.color = oldColor;

            // Handle Mouse click/drag events on the mapRect
            Event evt = Event.current;
            if (mapRect.Contains(evt.mousePosition))
            {
                if (evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag)
                {
                    // Compute relative click coordinates (0 to 1)
                    float relX = (evt.mousePosition.x - mapRect.x) / mapRect.width;
                    float relY = 1f - ((evt.mousePosition.y - mapRect.y) / mapRect.height); // Invert Y back

                    // Map to 2048 x 1143 space
                    float posX = Mathf.Clamp(relX * 2048f, 0f, 2048f);
                    float posY = Mathf.Clamp(relY * 1143f, 0f, 1143f);

                    // Snap/Round for cleaner coordinates
                    posX = Mathf.Round(posX);
                    posY = Mathf.Round(posY);

                    posProp.vector2Value = new Vector2(posX, posY);
                    serializedObject.ApplyModifiedProperties();
                    
                    // Mark target as dirty
                    EditorUtility.SetDirty(target);
                    
                    // Repaint the inspector
                    evt.Use();
                    
                    // Real-time Scene refresh: Find CampaignPage in the editor and force a visual redraw!
                    CampaignPage campaignPage = null;
#if UNITY_2023_1_OR_NEWER
                    campaignPage = Object.FindAnyObjectByType<CampaignPage>();
#else
                    campaignPage = (CampaignPage)Object.FindObjectOfType(typeof(CampaignPage));
#endif
                    if (campaignPage != null)
                    {
                        campaignPage.Refresh();
                    }
                }
            }
        }
    }
}
