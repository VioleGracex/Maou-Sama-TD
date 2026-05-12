using UnityEngine;
using UnityEditor;
using MaouSamaTD.Units;

namespace MaouSamaTD.Units.Editor
{
    [CustomEditor(typeof(EnemyData))]
    public class EnemyDataEditor : UnityEditor.Editor
    {
        private EnemyData _target;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "General", "Combat", "Abilities", "Visuals" };


        private void OnEnable()
        {
            _target = (EnemyData)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Toggle between default and custom editor
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11
            };
            buttonStyle.normal.textColor = _target.useDefaultInspector ? Color.gray : new Color(0.1f, 0.7f, 0.2f);

            if (GUILayout.Button(_target.useDefaultInspector ? "Switch to Custom Editor" : "Switch to Default Editor", buttonStyle, GUILayout.Height(25)))
            {
                _target.useDefaultInspector = !_target.useDefaultInspector;
                EditorUtility.SetDirty(_target);
            }

            if (_target.useDefaultInspector)
            {
                DrawDefaultInspectorWithReadOnlyID();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Space(5);
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, GUILayout.Height(25));
            EditorGUILayout.Space(10);

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 160f;

            switch (_selectedTab)
            {
                case 0: DrawGeneralTab(); break;
                case 1: DrawCombatTab(); break;
                case 2: DrawAbilitiesTab(); break;
                case 3: DrawVisualsTab(); break;

            }

            EditorGUIUtility.labelWidth = originalLabelWidth;
            EditorGUILayout.Space(10);
            
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGeneralTab()
        {
            BeginSection("Identity");
            using (new EditorGUI.DisabledScope(true))
            {
                DrawProperty("UniqueID", "Unique ID");
            }
            DrawProperty("EnemyName", "Enemy Name");
            
            GUILayout.Space(5);
            DrawSpritePreviewField(serializedObject.FindProperty("EnemySprite"), "Enemy Sprite");
            EndSection();

            BeginSection("Behavior & Rules");
            DrawProperty("MovementType", "Movement Type");
            DrawProperty("CollisionType", "Collision Rules");
            DrawProperty("EvasionType", "Evasion Style");
            DrawProperty("PhasingCharges", "Phasing Charges");
            EndSection();

            BeginSection("Rewards");
            DrawProperty("CurrencyReward", "Gold/Drops Reward");
            EndSection();
        }

        private void DrawCombatTab()
        {
            BeginSection("Stats");
            DrawProperty("MaxHp", "Max Health Points");
            DrawProperty("MoveSpeed", "Movement Speed");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField("Blocks Per Second", _target.BlocksPerSecond.ToString("F2"));
            }
            DrawProperty("AttackPower", "Attack Power");
            DrawProperty("AttackInterval", "Attack Interval (Sec)");
            DrawProperty("AttackRange", "Attack Range (Tiles)");
            DrawProperty("ExitDamage", "Exit Damage (To Player)");
            DrawProperty("DamageType", "Damage Type");
            EndSection();

            BeginSection("Combat Pattern");
            DrawProperty("AttackPattern", "Targeting Pattern");
            DrawAttackPatternPreview();
            DrawPropertyWithTooltip("TargetingPriority", "Movement Priority", "How this unit decides where to go or when to stop.");
            DrawPropertyWithTooltip("OnlyAttackIfBlocked", "Only Attack If Blocked", "Checked: Unit keeps walking unless physically blocked by a vassal.\nUnchecked: Unit stops to attack any vassal within range (including diagonals).");
            DrawProperty("GroundAttackTargets", "Targetable Ground Types");
            EndSection();
            
            BeginSection("Immunities");
            DrawProperty("Immunities", "Damage Immunities");
            EndSection();
        }

        private void DrawAbilitiesTab()
        {
            BeginSection("Special Abilities & Passives");
            EditorGUILayout.HelpBox("Add ScriptableObject-based abilities here. These are initialized per-instance when the enemy spawns.", MessageType.Info);
            DrawProperty("Abilities", "Unit Abilities");
            EndSection();
        }


        private void DrawVisualsTab()
        {
            BeginSection("Sprite Settings");
            DrawProperty("Tint", "Sprite Color Tint");
            DrawProperty("VisualYOffset", "Sprite Y Offset");
            DrawProperty("BaseVisualHeight", "Base Visual Height");
            DrawProperty("AnimatorController", "Animator Controller");
            EndSection();

            BeginSection("UI Settings");
            DrawProperty("HpBarYOffset", "HP Bar Vertical Offset");
            EndSection();
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
        }

        private void DrawSpritePreviewField(SerializedProperty prop, string label)
        {
            if (prop == null) return;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(prop, GUIContent.none);
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(10);
            
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
                else EditorGUILayout.PropertyField(prop, new GUIContent(label, prop.tooltip), true);
            }
        }

        private void DrawPropertyWithTooltip(string propName, string label, string tooltip)
        {
            SerializedProperty prop = serializedObject.FindProperty(propName);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip), true);
            }
        }

        private void DrawAttackPatternPreview()
        {
            SerializedProperty patternProp = serializedObject.FindProperty("AttackPattern");
            SerializedProperty rangeProp = serializedObject.FindProperty("AttackRange");
            SerializedProperty offsetsProp = serializedObject.FindProperty("CustomPatternOffsets");

            if (patternProp == null || rangeProp == null) return;

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
                    
                    if (pattern == AttackPattern.Custom && !isCenter && offsetsProp != null)
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
            EditorGUILayout.Space(5);
        }

        private bool IsCustomOffset(SerializedProperty offsets, Vector2Int offset)
        {
            if (offsets == null) return false;
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
    }
}
