using UnityEngine;
using UnityEditor;
using MaouSamaTD.Units;

namespace MaouSamaTD.Units.Editor
{
    [CustomEditor(typeof(UnitData))]
    public class UnitDataEditor : UnityEditor.Editor
    {
        private UnitData _target;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "General", "Combat", "Skills & SP", "Visuals & UI" };

        private void OnEnable()
        {
            _target = (UnitData)target;
        }

        public override void OnInspectorGUI()
        {
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
                case 3: DrawVisualsTab(); break;
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
            EndSection();

            BeginSection("Resonance & Advancement");
            DrawProperty("AscensionNodes", "Ascension Tree");
            DrawProperty("BaseStatMultiplier", "Stat Multiplier");
            EndSection();
        }

        private void DrawVisualsTab()
        {
            BeginSection("Core Art Assets");
            DrawSpriteWithPreview("UnitAvatar", "Unit Avatar (Headshot)");
            DrawSpriteWithPreview("UnitChibi", "Unit Chibi (In-Game)");
            DrawSpriteWithPreview("UnitWaistUp", "Waist-Up Portrait");
            DrawSpriteWithPreview("UnitSplashArt", "Full Splash Art");
            DrawSpriteWithPreview("UnitFullSprite", "Full Body Cutout");
            EndSection();

            BeginSection("UI Customization");
            DrawProperty("CardSlotImageType", "Card Slot image Type");
            DrawProperty("ButtonImageType", "Unit Button image Type");
            EndSection();

            BeginSection("Animation & Skins");
            DrawProperty("AnimatorController", "Animator");
            DrawProperty("Rank2Art", "Elite / Rank 2 Art");
            DrawProperty("AlternateSkins", "Skins Collection");
            DrawProperty("EquippedSkin", "Current Skin");
            EndSection();
        }

        private void DrawSpriteWithPreview(string propName, string label)
        {
            SerializedProperty prop = serializedObject.FindProperty(propName);
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUILayout.LabelField(label, EditorStyles.wordWrappedLabel);
            EditorGUILayout.PropertyField(prop, GUIContent.none);
            EditorGUILayout.EndVertical();

            if (prop.objectReferenceValue != null)
            {
                Sprite sprite = (Sprite)prop.objectReferenceValue;
                Rect rect = GUILayoutUtility.GetRect(80, 80, GUILayout.ExpandWidth(false));
                DrawSpritePreview(rect, sprite);
            }
            else
            {
                GUILayout.Box("No Sprite", GUILayout.Width(80), GUILayout.Height(80));
            }
            
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void DrawSpritePreview(Rect rect, Sprite sprite)
        {
            if (sprite == null) return;
            
            Texture2D tex = AssetPreview.GetAssetPreview(sprite);
            if (tex != null)
            {
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
            }
            else
            {
                // Fallback for when preview isn't ready
                GUI.DrawTexture(rect, sprite.texture, ScaleMode.ScaleToFit);
            }
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

            if (pattern == AttackPattern.Custom)
                EditorGUILayout.HelpBox("Click tiles to toggle Custom offsets.", MessageType.None);
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
    }
}
