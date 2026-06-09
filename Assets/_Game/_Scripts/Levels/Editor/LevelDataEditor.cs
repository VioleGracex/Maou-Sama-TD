using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
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
        private static Dictionary<int, bool> _waveFoldouts = new Dictionary<int, bool>();

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
            
            // Header for array size
            int newSize = EditorGUILayout.IntField("Wave Count", wavesProp.arraySize);
            if (newSize != wavesProp.arraySize) wavesProp.arraySize = newSize;

            EditorGUILayout.Space(5);

            // Initialize simulation lists
            List<SpawnPointData> activeSpawns = new List<SpawnPointData>();
            List<Vector2Int> activeExits = new List<Vector2Int>();
            
            MapData mapData = _target.MapData;
            if (mapData != null)
            {
                activeSpawns.AddRange(mapData.SpawnPoints);
                activeExits.AddRange(mapData.ExitPoints);
            }

            for (int i = 0; i < wavesProp.arraySize; i++)
            {
                SerializedProperty waveProp = wavesProp.GetArrayElementAtIndex(i);
                
                // Calculate wave total enemies
                int totalEnemies = 0;
                SerializedProperty groupsProp = waveProp.FindPropertyRelative("Groups");
                for (int g = 0; g < groupsProp.arraySize; g++)
                {
                    SerializedProperty groupProp = groupsProp.GetArrayElementAtIndex(g);
                    totalEnemies += groupProp.FindPropertyRelative("Count").intValue;
                }
                
                string message = waveProp.FindPropertyRelative("WaveMessage").stringValue;
                string label = $"Wave {i + 1}";
                if (!string.IsNullOrEmpty(message)) label += $" ({message})";
                label += $" - {totalEnemies} Enemies";
                
                bool foldout = GetWaveFoldoutState(i);
                foldout = EditorGUILayout.Foldout(foldout, label, true, EditorStyles.foldoutHeader ?? EditorStyles.boldLabel);
                SetWaveFoldoutState(i, foldout);
                
                if (foldout)
                {
                    EditorGUI.indentLevel++;
                    
                    // Draw message, delay, and story fields
                    EditorGUILayout.PropertyField(waveProp.FindPropertyRelative("WaveMessage"));
                    EditorGUILayout.PropertyField(waveProp.FindPropertyRelative("DelayBeforeNextWave"));
                    EditorGUILayout.PropertyField(waveProp.FindPropertyRelative("PreWaveStory"));
                    EditorGUILayout.PropertyField(waveProp.FindPropertyRelative("PostWaveStory"));
                    
                    // Draw Groups
                    EditorGUILayout.PropertyField(groupsProp, new GUIContent("Spawn Groups"), true);
                    
                    // Draw Tile Alterations
                    SerializedProperty alterationsProp = waveProp.FindPropertyRelative("TileAlterations");
                    EditorGUILayout.PropertyField(alterationsProp, new GUIContent("Tile Alterations (Post-Wave)"), true);
                    
                    // Display stats BEFORE applying this wave's alterations (i.e. active for this wave)
                    EditorGUILayout.Space(5);
                    DrawWaveStatsAndValidation(i, activeSpawns, activeExits, groupsProp, alterationsProp, mapData);
                    
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(10);
                }
                
                // Now, APPLY this wave's alterations to activeSpawns and activeExits for the next wave
                ApplyAlterationsToSim(waveProp.FindPropertyRelative("TileAlterations"), activeSpawns, activeExits);
            }
        }

        private bool GetWaveFoldoutState(int index)
        {
            if (!_waveFoldouts.TryGetValue(index, out bool state))
            {
                _waveFoldouts[index] = false; // Default closed
            }
            return state;
        }

        private void SetWaveFoldoutState(int index, bool state)
        {
            _waveFoldouts[index] = state;
        }

        private TileType GetDefaultTileType(MapData mapData, Vector2Int coordinate)
        {
            if (mapData == null) return TileType.None;
            if (mapData.UseManualLayout)
            {
                var tile = mapData.ManualLayoutData.Find(t => t.Coordinate == coordinate);
                if (tile.Coordinate == coordinate)
                {
                    return tile.Type;
                }
                return TileType.None;
            }
            else
            {
                // Simple default procedural guess
                bool isHigh = coordinate.y == 0 || coordinate.y == mapData.Height - 1;
                return isHigh ? TileType.HighGround : TileType.Walkable;
            }
        }

        private void ApplyAlterationsToSim(SerializedProperty alterationsProp, List<SpawnPointData> activeSpawns, List<Vector2Int> activeExits)
        {
            if (alterationsProp == null) return;
            
            for (int i = 0; i < alterationsProp.arraySize; i++)
            {
                SerializedProperty altProp = alterationsProp.GetArrayElementAtIndex(i);
                TileAlterationAction action = (TileAlterationAction)altProp.FindPropertyRelative("Action").enumValueIndex;
                TilePointType pointType = (TilePointType)altProp.FindPropertyRelative("PointType").enumValueIndex;
                Vector2Int coord = altProp.FindPropertyRelative("Coordinate").vector2IntValue;
                int targetExitIndex = altProp.FindPropertyRelative("TargetExitIndex").intValue;
                
                bool isSpawn = pointType == TilePointType.SpawnGround || pointType == TilePointType.SpawnHigh;
                bool isExit = pointType == TilePointType.ExitGround || pointType == TilePointType.ExitHigh;
                
                if (action == TileAlterationAction.Override)
                {
                    if (isSpawn)
                    {
                        activeSpawns.Clear();
                        activeSpawns.Add(new SpawnPointData { Coordinate = coord, TargetExitIndex = targetExitIndex });
                    }
                    else if (isExit)
                    {
                        activeExits.Clear();
                        activeExits.Add(coord);
                    }
                    else
                    {
                        activeSpawns.RemoveAll(s => s.Coordinate == coord);
                        activeExits.Remove(coord);
                    }
                }
                else if (action == TileAlterationAction.Add)
                {
                    if (isSpawn)
                    {
                        activeSpawns.RemoveAll(s => s.Coordinate == coord);
                        activeSpawns.Add(new SpawnPointData { Coordinate = coord, TargetExitIndex = targetExitIndex });
                    }
                    else if (isExit)
                    {
                        if (!activeExits.Contains(coord))
                        {
                            activeExits.Add(coord);
                        }
                    }
                    else
                    {
                        activeSpawns.RemoveAll(s => s.Coordinate == coord);
                        activeExits.Remove(coord);
                    }
                }
                else if (action == TileAlterationAction.Subtract)
                {
                    if (isSpawn)
                    {
                        activeSpawns.RemoveAll(s => s.Coordinate == coord);
                    }
                    else if (isExit)
                    {
                        activeExits.Remove(coord);
                    }
                    else
                    {
                        activeSpawns.RemoveAll(s => s.Coordinate == coord);
                        activeExits.Remove(coord);
                    }
                }
            }
        }

        private void DrawWaveStatsAndValidation(int waveIndex, List<SpawnPointData> activeSpawns, List<Vector2Int> activeExits, SerializedProperty groupsProp, SerializedProperty alterationsProp, MapData mapData)
        {
            // Cumulative Stats Box
            GUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField($"--- WAVE {waveIndex + 1} STARTING ENVIRONMENT STATE ---", EditorStyles.boldLabel);
            
            // Total enemy count in wave
            int totalEnemies = 0;
            for (int g = 0; g < groupsProp.arraySize; g++)
            {
                SerializedProperty groupProp = groupsProp.GetArrayElementAtIndex(g);
                totalEnemies += groupProp.FindPropertyRelative("Count").intValue;
            }
            EditorGUILayout.LabelField($"Enemy Spawns in Wave: {totalEnemies}", EditorStyles.miniBoldLabel);
            
            EditorGUILayout.Space(2);
            
            // Active Spawns
            EditorGUILayout.LabelField($"Active Spawns ({activeSpawns.Count}):", EditorStyles.miniBoldLabel);
            if (activeSpawns.Count == 0)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("  None! Enemies will fail to spawn.", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                for (int s = 0; s < activeSpawns.Count; s++)
                {
                    var spawn = activeSpawns[s];
                    string exitStr = spawn.TargetExitIndex >= 0 && spawn.TargetExitIndex < activeExits.Count ? $"({activeExits[spawn.TargetExitIndex].x}, {activeExits[spawn.TargetExitIndex].y}) [Index {spawn.TargetExitIndex}]" : "Any/First";
                    EditorGUILayout.LabelField($"  [{s}] Coordinate: ({spawn.Coordinate.x}, {spawn.Coordinate.y}) -> Target Exit: {exitStr}");
                }
            }

            // Active Exits
            EditorGUILayout.LabelField($"Active Exits ({activeExits.Count}):", EditorStyles.miniBoldLabel);
            if (activeExits.Count == 0)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("  None! Pathfinding will fail.", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                for (int e = 0; e < activeExits.Count; e++)
                {
                    var exit = activeExits[e];
                    EditorGUILayout.LabelField($"  [{e}] Coordinate: ({exit.x}, {exit.y})");
                }
            }

            GUILayout.EndVertical();

            // Warnings/Errors Box
            bool hasIssues = false;

            // 1. Check for no active spawns/exits
            if (activeSpawns.Count == 0)
            {
                EditorGUILayout.HelpBox($"Wave {waveIndex + 1} has NO active spawn points! Ground/High enemies cannot spawn.", MessageType.Error);
                hasIssues = true;
            }
            if (activeExits.Count == 0)
            {
                EditorGUILayout.HelpBox($"Wave {waveIndex + 1} has NO active exit points! Ground/High enemies cannot navigate.", MessageType.Error);
                hasIssues = true;
            }

            // 2. Check group spawn index bounds
            for (int g = 0; g < groupsProp.arraySize; g++)
            {
                SerializedProperty groupProp = groupsProp.GetArrayElementAtIndex(g);
                int spawnIdx = groupProp.FindPropertyRelative("SpawnPointIndex").intValue;
                var enemyTypeProp = groupProp.FindPropertyRelative("EnemyType");
                string enemyName = enemyTypeProp.objectReferenceValue != null ? enemyTypeProp.objectReferenceValue.name : $"Group {g}";

                if (spawnIdx < 0 || spawnIdx >= activeSpawns.Count)
                {
                    EditorGUILayout.HelpBox($"Group '{enemyName}' uses invalid Spawn Index {spawnIdx}! There are only {activeSpawns.Count} active spawns.", MessageType.Error);
                    hasIssues = true;
                }
            }

            // 3. Map bounds and tile type match checks for alterations of this wave
            if (mapData == null)
            {
                EditorGUILayout.HelpBox("No MapData assigned to LevelData. Cannot validate coordinate bounds or tile types.", MessageType.Warning);
            }
            else
            {
                for (int a = 0; a < alterationsProp.arraySize; a++)
                {
                    SerializedProperty altProp = alterationsProp.GetArrayElementAtIndex(a);
                    TileAlterationAction action = (TileAlterationAction)altProp.FindPropertyRelative("Action").enumValueIndex;
                    TilePointType pointType = (TilePointType)altProp.FindPropertyRelative("PointType").enumValueIndex;
                    Vector2Int coord = altProp.FindPropertyRelative("Coordinate").vector2IntValue;

                    if (coord.x < 0 || coord.x >= mapData.Width || coord.y < 0 || coord.y >= mapData.Height)
                    {
                        EditorGUILayout.HelpBox($"Alteration {a} coordinate ({coord.x}, {coord.y}) is OUT OF MAP BOUNDS (Map: {mapData.Width}x{mapData.Height})!", MessageType.Error);
                        hasIssues = true;
                    }
                    else if (pointType == TilePointType.Decoration)
                    {
                        // Decorations can be subtracted anywhere
                    }
                    else if (action != TileAlterationAction.Subtract) // Tile type mismatch is only relevant for Add/Override
                    {
                        TileType defaultType = GetDefaultTileType(mapData, coord);
                        bool isHigh = pointType == TilePointType.SpawnHigh || pointType == TilePointType.ExitHigh || pointType == TilePointType.HighGround;
                        
                        if (isHigh)
                        {
                            // Expected High Tile, warn if walkable ground / low / none
                            bool isDefaultHigh = defaultType == TileType.HighGround || defaultType == TileType.DecoHighGround || defaultType == TileType.SpawnPointHigh || defaultType == TileType.ExitPointHigh || defaultType == TileType.Wall;
                            if (!isDefaultHigh)
                            {
                                EditorGUILayout.HelpBox($"Alteration {a} at ({coord.x}, {coord.y}) targets high ground point but the map default is ground (Type: {defaultType})!", MessageType.Warning);
                                hasIssues = true;
                            }
                        }
                        else
                        {
                            // Expected Ground Tile, warn if high ground / none / wall / decor
                            bool isDefaultGround = defaultType == TileType.Walkable || defaultType == TileType.LowTile || defaultType == TileType.SpawnPoint || defaultType == TileType.ExitPoint;
                            if (!isDefaultGround)
                            {
                                EditorGUILayout.HelpBox($"Alteration {a} at ({coord.x}, {coord.y}) targets ground point but the map default is high ground/void/wall (Type: {defaultType})!", MessageType.Warning);
                                hasIssues = true;
                            }
                        }
                    }
                }
            }

            if (!hasIssues)
            {
                GUI.color = new Color(0.7f, 1f, 0.7f);
                EditorGUILayout.HelpBox("All validations passed. Environment data matches map layout.", MessageType.None);
                GUI.color = Color.white;
            }
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

    [CustomPropertyDrawer(typeof(WaveTileAlteration))]
    public class WaveTileAlterationDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw foldout header
            property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label, true);
            
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                float y = position.y + EditorGUIUtility.singleLineHeight + 2;
                float height = EditorGUIUtility.singleLineHeight;

                SerializedProperty actionProp = property.FindPropertyRelative("Action");
                SerializedProperty pointTypeProp = property.FindPropertyRelative("PointType");
                SerializedProperty coordProp = property.FindPropertyRelative("Coordinate");
                SerializedProperty exitIdxProp = property.FindPropertyRelative("TargetExitIndex");

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), actionProp);
                y += height + 2;

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), pointTypeProp);
                y += height + 2;

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), coordProp);
                y += height + 2;

                TilePointType pType = (TilePointType)pointTypeProp.enumValueIndex;
                if (pType == TilePointType.SpawnGround || pType == TilePointType.SpawnHigh)
                {
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), exitIdxProp);
                    y += height + 2;
                }

                // Show default tile type at this coordinate
                var levelData = property.serializedObject.targetObject as LevelData;
                MapData mapData = levelData != null ? levelData.MapData : null;
                
                if (mapData != null)
                {
                    Vector2Int coord = coordProp.vector2IntValue;
                    string typeString = "OUT OF MAP BOUNDS";
                    if (coord.x >= 0 && coord.x < mapData.Width && coord.y >= 0 && coord.y < mapData.Height)
                    {
                        TileType tType = GetDefaultTileType(mapData, coord);
                        typeString = tType.ToString();
                    }
                    
                    GUI.enabled = false;
                    EditorGUI.TextField(new Rect(position.x, y, position.width, height), "Current Map Tile Type", typeString);
                    GUI.enabled = true;
                    y += height + 2;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float totalHeight = EditorGUIUtility.singleLineHeight + 2; // for foldout

            totalHeight += (EditorGUIUtility.singleLineHeight + 2) * 3; // Action, PointType, Coordinate

            SerializedProperty pointTypeProp = property.FindPropertyRelative("PointType");
            TilePointType pType = (TilePointType)pointTypeProp.enumValueIndex;
            if (pType == TilePointType.SpawnGround || pType == TilePointType.SpawnHigh)
            {
                totalHeight += EditorGUIUtility.singleLineHeight + 2; // TargetExitIndex
            }

            var levelData = property.serializedObject.targetObject as LevelData;
            MapData mapData = levelData != null ? levelData.MapData : null;
            if (mapData != null)
            {
                totalHeight += EditorGUIUtility.singleLineHeight + 2;
            }

            return totalHeight;
        }

        private TileType GetDefaultTileType(MapData mapData, Vector2Int coordinate)
        {
            if (mapData == null) return TileType.None;
            if (mapData.UseManualLayout)
            {
                var tile = mapData.ManualLayoutData.Find(t => t.Coordinate == coordinate);
                if (tile.Coordinate == coordinate)
                {
                    return tile.Type;
                }
                return TileType.None;
            }
            else
            {
                bool isHigh = coordinate.y == 0 || coordinate.y == mapData.Height - 1;
                return isHigh ? TileType.HighGround : TileType.Walkable;
            }
        }
    }
}
