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
            BeginSection("Identity");
            using (new EditorGUI.DisabledScope(true)) DrawProperty("UniqueID", "Unique ID");
            DrawProperty("UnitName", "Unit Name");
            DrawProperty("UnitTitle", "Unit Title");
            EndSection();

            BeginSection("Progression");
            DrawProperty("Level");
            DrawProperty("StarRating", "Star Rating (1-6)");
            
            SerializedProperty rarityProp = serializedObject.FindProperty("Rarity");
            EditorGUILayout.PropertyField(rarityProp, new GUIContent($"Rarity ({GetStarLabel((UnitRarity)rarityProp.enumValueIndex)})"));
            
            DrawProperty("Class", "Tactical Class");
            DrawProperty("AcquisitionDate", "Acquired (Ticks)");
            EndSection();

            BeginSection("Placement");
            DrawProperty("_viableTiles", "Viable Tiles");
            EndSection();
        }

        private void DrawCombatTab()
        {
            BeginSection("Attributes (Base)");
            DrawProperty("MaxHp", "Max HP");
            DrawProperty("AttackPower", "Attack Power");
            DrawProperty("Defense", "Defense");
            DrawProperty("Resistance", "Resistance");
            DrawProperty("RespawnTime", "Redeploy Timer (Sec)");
            EndSection();

            BeginSection("Calculated Stats (One Source of Truth)");
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
            EndSection();

            BeginSection("Attack Rules");
            DrawProperty("AttackInterval", "Attack Interval (Sec)");
            DrawProperty("Range", "Attack Range (Tiles)");
            DrawProperty("AttackType", "Attack Method");
            DrawProperty("DamageType", "Damage Flavor");
            DrawProperty("BlockCount", "Block Count");
            DrawProperty("DeploymentCost", "Deployment Cost");
            EndSection();

            BeginSection("Attack Pattern");
            DrawProperty("AttackPattern");
            DrawAttackPatternPreview();
            EndSection();
        }

        private void DrawSkillsTab()
        {
            BeginSection("Skill Data");
            DrawProperty("PassiveSkill");
            DrawProperty("ActiveSkill");
            DrawProperty("UltimateSkill");
            EndSection();

            BeginSection("SP / Charge");
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
            EndSection();

            BeginSection("Resonance & Advancement");
            DrawProperty("AscensionNodes", "Ascension Tree");
            DrawProperty("BaseStatMultiplier", "Stat Multiplier");
            EndSection();
        }

        private void DrawSkinsTab()
        {
            BeginSection("Base Visuals");
            SerializedProperty baseSkinProp = serializedObject.FindProperty("BaseSkin");
            
            // Modern List View (One per row, fixed right-side preview)
            DrawResponsiveSpriteField(baseSkinProp.FindPropertyRelative("Avatar"), "Avatar (Headshot)");
            DrawResponsiveSpriteField(baseSkinProp.FindPropertyRelative("Chibi"), "Chibi (In-Game)");
            DrawResponsiveSpriteField(baseSkinProp.FindPropertyRelative("WaistUp"), "Waist-Up Portrait");
            DrawResponsiveSpriteField(baseSkinProp.FindPropertyRelative("FullSplashArt"), "Full Splash Art");
            DrawResponsiveSpriteField(baseSkinProp.FindPropertyRelative("FullBodyCutout"), "Full Body Cutout");
            
            GUILayout.Space(5);
            DrawProperty(baseSkinProp.FindPropertyRelative("AnimatorController").name, "Base Animator");
            DrawResponsiveSpriteField(serializedObject.FindProperty("Rank2Art"), "Elite / Rank 2 Art");
            EndSection();

            BeginSection("UI Settings");
            DrawProperty("CardSlotImageType", "Card Slot Image Type");
            DrawProperty("ButtonImageType", "Unit Button Image Type");
            EndSection();

            BeginSection("Alternate Skins Collection");
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
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Editing: {skinNames[_skinSubTab]}", EditorStyles.whiteMiniLabel);
                GUI.color = Color.red;
                if (GUILayout.Button("Delete Skin", GUILayout.Width(100)))
                {
                    skinsProp.DeleteArrayElementAtIndex(_skinSubTab);
                    _skinSubTab = Mathf.Max(0, _skinSubTab - 1);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                    return;
                }
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
                EditorGUILayout.LabelField("Art Resources", EditorStyles.boldLabel);
                DrawResponsiveSpriteField(selectedSkin.FindPropertyRelative("Avatar"), "Avatar");
                DrawResponsiveSpriteField(selectedSkin.FindPropertyRelative("Chibi"), "Chibi");
                DrawResponsiveSpriteField(selectedSkin.FindPropertyRelative("WaistUp"), "Waist-Up");
                DrawResponsiveSpriteField(selectedSkin.FindPropertyRelative("FullSplashArt"), "Splash Art");
                DrawResponsiveSpriteField(selectedSkin.FindPropertyRelative("FullBodyCutout"), "Full Body");
                DrawProperty(selectedSkin.FindPropertyRelative("AnimatorController").name, "Animator");

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Skin Settings", EditorStyles.boldLabel);
                DrawProperty(selectedSkin.FindPropertyRelative("IsDefault").name, "Unlocked by Default");
                DrawProperty(selectedSkin.FindPropertyRelative("UnlockCost").name, "Unlock Cost");
                DrawProperty(selectedSkin.FindPropertyRelative("IsPremium").name, "Premium Skin");
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("No alternate skins defined for this unit.", MessageType.Info);
            }
            EndSection();
            
            BeginSection("Runtime State");
            DrawProperty("_equippedSkinID", "Equipped Skin ID");
            DrawProperty("_unlockedSkinIDs", "Unlocked Skin IDs");
            EndSection();
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

        private void BeginSection(string title)
        {
            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            GUILayout.Space(2);
        }

        private void EndSection()
        {
            GUILayout.Space(2);
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
            GUILayout.Space(10);
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
                UnitRarity.Common => "\u2605",
                UnitRarity.Uncommon => "\u2605\u2605",
                UnitRarity.Rare => "\u2605\u2605\u2605",
                UnitRarity.Elite => "\u2605\u2605\u2605\u2605",
                UnitRarity.Master => "\u2605\u2605\u2605\u2605\u2605",
                UnitRarity.Legendary => "\u2605\u2605\u2605\u2605\u2605\u2605",
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
