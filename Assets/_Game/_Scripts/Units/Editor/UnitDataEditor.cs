using UnityEngine;
using UnityEditor;
using MaouSamaTD.Units;
using System.Collections.Generic;

namespace MaouSamaTD.Units.Editor
{
    [CustomEditor(typeof(UnitData))]
    public class UnitDataEditor : UnityEditor.Editor
    {
        private UnitData _target;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "General", "Combat", "Skills & SP", "Skins Collection" };

        private int _skinSubTab = 0;
        private GUIStyle _tabStyle;
        private GUIStyle _selectedTabStyle;

        private void OnEnable()
        {
            _target = (UnitData)target;
        }

        private void InitStyles()
        {
            if (_tabStyle != null) return;
            _tabStyle = new GUIStyle(EditorStyles.miniButtonMid);
            _selectedTabStyle = new GUIStyle(EditorStyles.miniButtonMid);
            _selectedTabStyle.normal.background = _tabStyle.active.background;
            _selectedTabStyle.normal.textColor = Color.white;
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            // Toggle between default and custom editor
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 11 };
            if (GUILayout.Button(_target.useDefaultInspector ? "Switch to Custom Editor" : "Switch to Default Editor", buttonStyle))
            {
                _target.useDefaultInspector = !_target.useDefaultInspector;
                EditorUtility.SetDirty(_target);
            }

            if (_target.useDefaultInspector)
            {
                DrawDefaultInspector();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Space(5);
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, GUILayout.Height(25));
            EditorGUILayout.Space(10);

            switch (_selectedTab)
            {
                case 0: DrawGeneralTab(); break;
                case 1: DrawCombatTab(); break;
                case 2: DrawSkillsTab(); break;
                case 3: DrawSkinsTab(); break;
            }

            EditorGUILayout.Space(20);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGeneralTab()
        {
            bool identityState = BeginSection("Identity", "general_identity");
            if (identityState)
            {
                using (new EditorGUI.DisabledScope(true)) DrawProperty("UniqueID", "Unique ID");
                DrawProperty("UnitName", "Unit Name");
                DrawProperty("UnitTitle", "Unit Title");
            }
            EndSection(identityState);

            bool progressionState = BeginSection("Progression", "general_progression");
            if (progressionState)
            {
                DrawProperty("Level");
                DrawProperty("StarRating", "Star Rating (1-6)");
                
                SerializedProperty rarityProp = serializedObject.FindProperty("Rarity");
                EditorGUILayout.PropertyField(rarityProp, new GUIContent($"Rarity ({GetStarLabel((UnitRarity)rarityProp.enumValueIndex)})"));
                
                DrawProperty("Class", "Tactical Class");
                DrawProperty("AcquisitionDate", "Acquired (Ticks)");
            }
            EndSection(progressionState);

            bool placementState = BeginSection("Placement", "general_placement");
            if (placementState)
            {
                DrawProperty("_viableTiles", "Viable Tiles");
            }
            EndSection(placementState);
        }

        private void DrawCombatTab()
        {
            bool baseAttState = BeginSection("Attributes (Base)", "combat_attributes");
            if (baseAttState)
            {
                DrawProperty("MaxHp", "Max HP");
                DrawProperty("AttackPower", "Attack Power");
                DrawProperty("Defense", "Defense");
                DrawProperty("Resistance", "Resistance");
                DrawProperty("RespawnTime", "Redeploy Timer (Sec)");
            }
            EndSection(baseAttState);

            bool calcState = BeginSection("Calculated Stats (One Source of Truth)", "combat_calculated");
            if (calcState)
            {
                var scaling = MaouSamaTD.Core.AppEntryPoint.LoadedScalingData;
                
                // Try to find scaling data if not loaded (Editor convenience)
                if (scaling == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:ClassScalingData");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        scaling = AssetDatabase.LoadAssetAtPath<ClassScalingData>(path);
                    }
                }

                if (scaling == null)
                {
                    EditorGUILayout.HelpBox("ClassScalingData not found in project or not loaded. Final stats may be incomplete.", MessageType.Warning);
                }
                
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.FloatField("Final Max HP", _target.CalculatedStats.MaxHp);
                    EditorGUILayout.FloatField("Final Attack", _target.CalculatedStats.Attack);
                    EditorGUILayout.FloatField("Final Defense", _target.CalculatedStats.Defense);
                    EditorGUILayout.TextField("Effective Class", _target.CalculatedStats.ClassName);
                    EditorGUILayout.ObjectField("Class Icon", _target.CalculatedStats.ClassIcon, typeof(Sprite), false);
                }

                if (GUILayout.Button("Force Recalculate"))
                {
                    _target.RefreshStats(scaling);
                    EditorUtility.SetDirty(_target);
                }
            }
            EndSection(calcState);

            bool rulesState = BeginSection("Attack Rules", "combat_rules");
            if (rulesState)
            {
                DrawProperty("AttackInterval", "Attack Interval (Sec)");
                DrawProperty("Range", "Attack Range (Tiles)");
                DrawProperty("AttackType", "Attack Method");
                DrawProperty("DamageType", "Damage Flavor");
                DrawProperty("BlockCount", "Block Count");
                DrawProperty("DeploymentCost", "Deployment Cost");
            }
            EndSection(rulesState);

            bool patternState = BeginSection("Attack Pattern", "combat_pattern");
            if (patternState)
            {
                DrawProperty("AttackPattern");
                DrawAttackPatternPreview();
            }
            EndSection(patternState);
        }

        private void DrawSkillsTab()
        {
            bool skillDataState = BeginSection("Skill Data", "skill_data");
            if (skillDataState)
            {
                DrawProperty("PassiveSkill");
                DrawProperty("ActiveSkill");
                DrawProperty("UltimateSkill");
            }
            EndSection(skillDataState);

            bool spChargeState = BeginSection("SP / Charge", "skill_charge");
            if (spChargeState)
            {
                DrawProperty("MaxCharge", "Max SP");
                DrawProperty("ChargePerSecond", "SP/Sec");
                DrawProperty("ChargePerAttack", "SP/Attack");
                
                EditorGUILayout.Space(5);
                SerializedProperty resistProp = serializedObject.FindProperty("UltimateDamageResistance");
                if (resistProp != null)
                {
                    float percent = resistProp.floatValue * 100f;
                    EditorGUI.BeginChangeCheck();
                    percent = EditorGUILayout.Slider("Ult Damage Resist %", percent, 0f, 100f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        resistProp.floatValue = percent / 100f;
                    }
                }
            }
            EndSection(spChargeState);

            bool resonanceState = BeginSection("Resonance & Advancement", "skill_ resonance");
            if (resonanceState)
            {
                DrawProperty("AscensionNodes", "Ascension Tree");
                DrawProperty("BaseStatMultiplier", "Stat Multiplier");
            }
            EndSection(resonanceState);
        }

        private void DrawSkinsTab()
        {
            bool baseVisState = BeginSection("Base Visuals", "skin_base_visuals");
            if (baseVisState)
            {
                SerializedProperty baseSkinProp = serializedObject.FindProperty("BaseSkin");
                
                // Modern List View (One per row, fixed right-side preview)
                DrawResponsiveSpriteField(baseSkinProp.FindPropertyRelative("Avatar"), "Avatar (Headshot)");
                DrawResponsiveSpriteField(baseSkinProp.FindPropertyRelative("Chibi"), "Chibi (In-Game)");
                DrawResponsiveSpriteField(baseSkinProp.FindPropertyRelative("WaistUp"), "Waist-Up Portrait");
                DrawResponsiveSpriteField(baseSkinProp.FindPropertyRelative("FullSplashArt"), "Full Splash Art");
                DrawResponsiveSpriteField(baseSkinProp.FindPropertyRelative("FullBodyCutout"), "Full Body Cutout");
                
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Base Animation (Required for Chibi Preview)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(baseSkinProp.FindPropertyRelative("AnimatorController"), new GUIContent("Chibi Animator"));
                
                EditorGUILayout.Space(10);
                bool starAdvState = BeginSubSection("Star Advancement Visual Overrides", "skin_star_advancement");
                if (starAdvState)
                {
                    SerializedProperty starAdvProps = serializedObject.FindProperty("StarAdvancementVisuals");
                    EditorGUILayout.PropertyField(starAdvProps, new GUIContent("Visuals by Star Rank"), true);
                }
                EndSubSection(starAdvState);
            }
            EndSection(baseVisState);

            bool uiSetState = BeginSection("UI Settings", "skin_ui_settings");
            if (uiSetState)
            {
                DrawProperty("CardSlotImageType", "Card Slot Image Type");
                DrawProperty("ButtonImageType", "Unit Button Image Type");
            }
            EndSection(uiSetState);

            bool altSkinsState = BeginSection("Alternate Skins Collection", "skin_alternate_collection");
            if (altSkinsState)
            {
                SerializedProperty skinsProp = serializedObject.FindProperty("Skins");
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Skins ({skinsProp.arraySize})", EditorStyles.boldLabel);
                if (GUILayout.Button("+ Add Skin", GUILayout.Width(100)))
                {
                    skinsProp.arraySize++;
                    _skinSubTab = skinsProp.arraySize - 1;
                }
                EditorGUILayout.EndHorizontal();

                if (skinsProp.arraySize > 0)
                {
                    EditorGUILayout.Space(5);
                    string[] skinNames = new string[skinsProp.arraySize];
                    for (int i = 0; i < skinsProp.arraySize; i++)
                    {
                        var s = skinsProp.GetArrayElementAtIndex(i);
                        var id = s.FindPropertyRelative("SkinID").stringValue;
                        var theme = s.FindPropertyRelative("SkinThemeName").stringValue;
                        skinNames[i] = string.IsNullOrEmpty(theme) ? (string.IsNullOrEmpty(id) ? $"Skin {i}" : id) : theme;
                    }

                    _skinSubTab = Mathf.Clamp(_skinSubTab, 0, skinsProp.arraySize - 1);
                    _skinSubTab = GUILayout.SelectionGrid(_skinSubTab, skinNames, 4, EditorStyles.miniButton);

                    EditorGUILayout.Space(5);
                    SerializedProperty selectedSkin = skinsProp.GetArrayElementAtIndex(_skinSubTab);
                    
                    EditorGUILayout.BeginVertical("helpbox");
                    bool deleteThisSkin = false;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Editing: {skinNames[_skinSubTab]}", EditorStyles.whiteMiniLabel);
                    GUI.color = Color.red;
                    if (GUILayout.Button("Delete Skin", GUILayout.Width(100))) deleteThisSkin = true;
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();

                    SerializedProperty skinIDProp = selectedSkin.FindPropertyRelative("SkinID");
                    SerializedProperty skinNameProp = selectedSkin.FindPropertyRelative("SkinThemeName");

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(skinNameProp, new GUIContent("Skin Name (Theme)", "The display name for this skin (e.g. 'Abyssal Hunter')"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (string.IsNullOrEmpty(skinIDProp.stringValue) || skinIDProp.stringValue == Slugify(skinNameProp.displayName))
                        {
                            skinIDProp.stringValue = Slugify(skinNameProp.stringValue);
                        }
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(skinIDProp, new GUIContent("Skin ID", "Unique identifier used for skill overrides and save data."));
                    if (GUILayout.Button("auto", GUILayout.Width(40))) skinIDProp.stringValue = Slugify(skinNameProp.stringValue);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(selectedSkin.FindPropertyRelative("SeriesName"), new GUIContent("Series / Collection", "The theme or collection name (e.g. 'Pool Party', 'Halloween')"));

                    EditorGUILayout.Space(10);
                    
                    bool skinArtState = BeginSubSection("Skin Visual Resources", "skin_edit_art");
                    if (skinArtState)
                    {
                        DrawResponsiveSpriteField(selectedSkin.FindPropertyRelative("Avatar"), "Avatar (Headshot)");
                        DrawResponsiveSpriteField(selectedSkin.FindPropertyRelative("Chibi"), "Chibi (In-Game)");
                        DrawResponsiveSpriteField(selectedSkin.FindPropertyRelative("WaistUp"), "Waist-Up Portrait");
                        DrawResponsiveSpriteField(selectedSkin.FindPropertyRelative("FullSplashArt"), "Splash Art");
                        DrawResponsiveSpriteField(selectedSkin.FindPropertyRelative("FullBodyCutout"), "Full Body Cutout");
                    }
                    EndSubSection(skinArtState);
                    
                    bool skinAnimState = BeginSubSection("Animation", "skin_edit_anim");
                    if (skinAnimState)
                    {
                        EditorGUILayout.PropertyField(selectedSkin.FindPropertyRelative("AnimatorController"), new GUIContent("Skin Animator"));
                    }
                    EndSubSection(skinAnimState);

                    bool skinSettState = BeginSubSection("Skin Settings", "skin_edit_settings");
                    if (skinSettState)
                    {
                        EditorGUILayout.PropertyField(selectedSkin.FindPropertyRelative("IsDefault"), new GUIContent("Unlocked by Default"));
                        EditorGUILayout.PropertyField(selectedSkin.FindPropertyRelative("UnlockCost"), new GUIContent("Unlock Cost"));
                        EditorGUILayout.PropertyField(selectedSkin.FindPropertyRelative("IsPremium"), new GUIContent("Premium Skin"));
                    }
                    EndSubSection(skinSettState);
                    
                    EditorGUILayout.EndVertical();
                    
                    if (deleteThisSkin)
                    {
                        skinsProp.DeleteArrayElementAtIndex(_skinSubTab);
                        _skinSubTab = Mathf.Max(0, _skinSubTab - 1);
                        serializedObject.ApplyModifiedProperties();
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No alternate skins defined for this unit.", MessageType.Info);
                }
            }
            EndSection(altSkinsState);
            
            bool runtimeState = BeginSection("Runtime State", "skin_runtime");
            if (runtimeState)
            {
                DrawProperty("_equippedSkinID", "Equipped Skin ID");
                DrawProperty("_unlockedSkinIDs", "Unlocked Skin IDs");
            }
            EndSection(runtimeState);
        }

        private void DrawResponsiveSpriteField(SerializedProperty prop, string label)
        {
            if (prop == null) return;
            
            EditorGUILayout.BeginHorizontal();
            
            // Left Column: Label + Property Field (Flexible)
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(prop, GUIContent.none);
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            // Right Column: Fixed Square Preview (64x64)
            Rect rect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            
            if (prop.objectReferenceValue != null)
            {
                Texture2D tex = AssetPreview.GetAssetPreview(prop.objectReferenceValue);
                if (tex == null && prop.objectReferenceValue is Sprite s) tex = s.texture;
                if (tex != null)
                    GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.Label(rect, "None", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter });
            }
            
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);
        }

        private bool BeginSection(string title, string key, bool defaultState = true)
        {
            string prefsKey = $"UnitDataEditor_Foldout_{key}";
            bool state = EditorPrefs.GetBool(prefsKey, defaultState);
            
            // Render header and close immediately to keep the Unity GUI stack for groups clean
            EditorGUI.BeginChangeCheck();
            state = EditorGUILayout.BeginFoldoutHeaderGroup(state, title);
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(prefsKey, state);
            }
            
            GUILayout.BeginVertical("helpbox");
            if (state)
            {
                EditorGUI.indentLevel++;
                GUILayout.Space(2);
            }
            return state;
        }

        private void EndSection(bool state)
        {
            if (state) EditorGUI.indentLevel--;
            GUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private bool BeginSubSection(string title, string key, bool defaultState = true)
        {
            string prefsKey = $"UnitDataEditor_Foldout_{key}";
            bool state = EditorPrefs.GetBool(prefsKey, defaultState);
            
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUI.BeginChangeCheck();
            state = EditorGUILayout.Foldout(state, title, true, EditorStyles.foldout);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(prefsKey, state);
            }
            
            if (state)
            {
                EditorGUI.indentLevel++;
            }
            return state;
        }

        private void EndSubSection(bool state)
        {
            if (state) EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawProperty(string propName, string label = null)
        {
            SerializedProperty prop = serializedObject.FindProperty(propName);
            if (prop != null)
            {
                if (string.IsNullOrEmpty(label)) EditorGUILayout.PropertyField(prop, true);
                else EditorGUILayout.PropertyField(prop, new GUIContent(label), true);
            }
        }

        private void DrawAttackPatternPreview()
        {
            SerializedProperty patternProp = serializedObject.FindProperty("AttackPattern");
            SerializedProperty rangeProp = serializedObject.FindProperty("Range");
            SerializedProperty offsetsProp = serializedObject.FindProperty("CustomPatternOffsets");

            AttackPattern pattern = (AttackPattern)patternProp.enumValueIndex;
            float range = rangeProp.floatValue;
            int iRange = Mathf.CeilToInt(range);

            int gridSize = 7;
            int mid = gridSize / 2;
            float cellSize = 22f;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect rect = GUILayoutUtility.GetRect(gridSize * cellSize, gridSize * cellSize);
            
            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    Rect cellRect = new Rect(rect.x + x * cellSize, rect.y + y * cellSize, cellSize - 2, cellSize - 2);
                    Vector2Int offset = new Vector2Int(x - mid, mid - y);

                    bool isCenter = (x == mid && y == mid);
                    bool isInPattern = isCenter || (pattern == AttackPattern.Custom ? IsCustomOffset(offsetsProp, offset) : IsOffsetInPattern(offset, pattern, iRange));

                    Color color = isCenter ? new Color(0.2f, 0.4f, 1f, 0.9f) : 
                                 isInPattern ? new Color(1f, 0.3f, 0.3f, 0.8f) : 
                                 new Color(0.3f, 0.3f, 0.3f, 0.2f);

                    EditorGUI.DrawRect(cellRect, color);
                    
                    if (pattern == AttackPattern.Custom && !isCenter)
                    {
                        if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                        {
                            ToggleOffset(offsetsProp, offset);
                            serializedObject.ApplyModifiedProperties();
                            Event.current.Use();
                        }
                    }
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private bool IsCustomOffset(SerializedProperty offsets, Vector2Int offset)
        {
            for (int i = 0; i < offsets.arraySize; i++)
                if (offsets.GetArrayElementAtIndex(i).vector2IntValue == offset) return true;
            return false;
        }

        private bool IsOffsetInPattern(Vector2Int offset, AttackPattern pattern, int range)
        {
            int dx = Mathf.Abs(offset.x);
            int dy = Mathf.Abs(offset.y);
            if (dx > range || dy > range) return false;
            return pattern switch {
                AttackPattern.Vertical => dx == 0,
                AttackPattern.Horizontal => dy == 0,
                AttackPattern.Cross => dx == 0 || dy == 0,
                AttackPattern.Diagonal => dx == dy,
                AttackPattern.All => true,
                _ => false
            };
        }

        private void ToggleOffset(SerializedProperty offsetsProp, Vector2Int offset)
        {
            for (int i = 0; i < offsetsProp.arraySize; i++)
            {
                if (offsetsProp.GetArrayElementAtIndex(i).vector2IntValue == offset)
                {
                    offsetsProp.DeleteArrayElementAtIndex(i);
                    return;
                }
            }
            offsetsProp.InsertArrayElementAtIndex(offsetsProp.arraySize);
            offsetsProp.GetArrayElementAtIndex(offsetsProp.arraySize - 1).vector2IntValue = offset;
        }

        private string GetStarLabel(UnitRarity rarity)
        {
            return rarity switch {
                UnitRarity.Common => "★ (C)",
                UnitRarity.Uncommon => "★★ (UC)",
                UnitRarity.Rare => "★★★ (R)",
                UnitRarity.Elite => "★★★★ (SR)",
                UnitRarity.Master => "★★★★★ (SSR)",
                UnitRarity.Legendary => "★★★★★★ (UR)",
                _ => ""
            };
        }

        private string Slugify(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string slug = text.ToLower().Trim();
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[\s-]+", "_");
            return slug;
        }
    }
}
