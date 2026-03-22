using UnityEngine;
using UnityEditor;
using MaouSamaTD.Units;

namespace MaouSamaTD.Units.Editor
{
    [CustomEditor(typeof(UnitData))]
    public class UnitDataEditor : UnityEditor.Editor
    {
        private UnitData _target;

        private void OnEnable()
        {
            _target = (UnitData)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            buttonStyle.normal.textColor = _target.useDefaultInspector ? Color.gray : new Color(0.1f, 0.7f, 0.2f);

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
            EditorGUIUtility.labelWidth = 180f;

            DrawCustomInspector();

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

        private void DrawCustomInspector()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Identity", headerStyle);
            EditorGUI.indentLevel++;
            
            using (new EditorGUI.DisabledScope(true))
            {
                DrawProperty("UniqueID", "Unique ID");
            }
            DrawProperty("UnitName", "Unit Name");
            DrawProperty("UnitTitle", "Unit Title");

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            
            // Draw Sprite (Icon)
            GUILayout.BeginVertical();
            EditorGUILayout.LabelField("Unit Icon (UI)", GUILayout.Width(100));
            SerializedProperty iconProp = serializedObject.FindProperty("UnitIcon");
            iconProp.objectReferenceValue = EditorGUILayout.ObjectField(iconProp.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(80), GUILayout.Height(80));
            GUILayout.EndVertical();

            // Draw Chibi
            GUILayout.BeginVertical();
            EditorGUILayout.LabelField("Unit Chibi (Battle)", GUILayout.Width(100));
            SerializedProperty chibiProp = serializedObject.FindProperty("UnitChibi");
            chibiProp.objectReferenceValue = EditorGUILayout.ObjectField(chibiProp.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(80), GUILayout.Height(80));
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
            GUILayout.Space(10);

            // Class & Rules
            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Class & Rarity", headerStyle);
            EditorGUI.indentLevel++;

            DrawProperty("Level", "Current Level");
            
            // Rarity Display with Stars
            SerializedProperty rarityProp = serializedObject.FindProperty("Rarity");
            string starLabel = GetStarLabel((UnitRarity)rarityProp.enumValueIndex);
            EditorGUILayout.PropertyField(rarityProp, new GUIContent($"Rarity ({starLabel})"));

            DrawProperty("Class", "Tactical Class");
            DrawProperty("AttackPattern");
            DrawProperty("AttackType");
            DrawProperty("DamageType");
            
            DrawAttackPatternPreview();
            
            DrawProperty("DeploymentCost", "Deployment Cost");
            DrawProperty("BlockCount", "Block Count");
            
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
            GUILayout.Space(10);

            // Stats
            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Base Stats (Level 1)", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("MaxHp", "Max HP");
            DrawProperty("AttackPower", "Attack Power");
            DrawProperty("Defense", "Defense");
            DrawProperty("AttackInterval", "Attack Interval (Sec)");
            DrawProperty("Range", "Attack Range (Tiles)");
            DrawProperty("RespawnTime", "Redeploy Timer (Sec)");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
            GUILayout.Space(10);

            // Skill & Charge
            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Skill & SP", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("Skill", "Equipped Skill");
            DrawProperty("MaxCharge", "Max SP");
            DrawProperty("ChargePerSecond", "SP Per Second");
            DrawProperty("ChargePerAttack", "SP Per Attack");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
            GUILayout.Space(10);

            // Placement
            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Placement Rules", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("_viableTiles", "Viable Tiles");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();

            GUILayout.Space(10);
            
            // Advanced Art Crops (New)
            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Advanced Art Crops", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("UnitWaistUp", "Waist-Up Portrait");
            DrawProperty("UnitSplashArt", "Splash Art");
            DrawProperty("UnitFullSprite", "Full Body Sprite");

            GUILayout.Space(10);
            DrawProperty("AlternateSkins", "Alternate Skins");
            DrawProperty("EquippedSkin", "Equipped Skin");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
        }

        private void DrawAttackPatternPreview()
        {
            EditorGUILayout.Space(5);
            EditorUtility.SetDirty(_target); // Ensure changes are saved

            SerializedProperty patternProp = serializedObject.FindProperty("AttackPattern");
            SerializedProperty rangeProp = serializedObject.FindProperty("Range");
            SerializedProperty offsetsProp = serializedObject.FindProperty("CustomPatternOffsets");

            AttackPattern pattern = (AttackPattern)patternProp.enumValueIndex;
            float range = rangeProp.floatValue;
            int iRange = Mathf.CeilToInt(range);

            EditorGUILayout.LabelField("Attack Pattern Preview", EditorStyles.boldLabel);
            
            // Grid settings
            int gridSize = 7; // Fixed 7x7 for UI consistency
            int mid = gridSize / 2;
            float cellSize = 25f;

            Rect rect = GUILayoutUtility.GetRect(gridSize * cellSize, gridSize * cellSize);
            rect.x += (EditorGUIUtility.currentViewWidth - gridSize * cellSize) / 2f - 20f;

            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    Rect cellRect = new Rect(rect.x + x * cellSize, rect.y + y * cellSize, cellSize - 2, cellSize - 2);
                    Vector2Int offset = new Vector2Int(x - mid, mid - y); // Correct coordinate mapping

                    bool isCenter = (x == mid && y == mid);
                    bool isInPattern = false;

                    if (isCenter)
                    {
                        EditorGUI.DrawRect(cellRect, new Color(0.2f, 0.2f, 0.8f, 0.8f)); // Unit position
                    }
                    else
                    {
                        // Check if this tile is in the pattern
                        if (pattern == AttackPattern.Custom)
                        {
                            for (int i = 0; i < offsetsProp.arraySize; i++)
                            {
                                if (offsetsProp.GetArrayElementAtIndex(i).vector2IntValue == offset)
                                {
                                    isInPattern = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            isInPattern = IsOffsetInPattern(offset, pattern, iRange);
                        }

                        Color color = isInPattern ? new Color(0.8f, 0.2f, 0.2f, 0.8f) : new Color(0.3f, 0.3f, 0.3f, 0.2f);
                        EditorGUI.DrawRect(cellRect, color);
                        
                        // Interaction for Custom pattern
                        if (pattern == AttackPattern.Custom)
                        {
                            if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                            {
                                ToggleOffset(offsetsProp, offset);
                                serializedObject.ApplyModifiedProperties();
                                GUI.changed = true;
                                Event.current.Use();
                            }
                        }
                    }

                    // Border
                    Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, new Color(1, 1, 1, 0.1f));
                }
            }

            if (pattern == AttackPattern.Custom)
            {
                EditorGUILayout.HelpBox("Click tiles to toggle attack offsets.", MessageType.Info);
                if (GUILayout.Button("Clear Pattern"))
                {
                    offsetsProp.ClearArray();
                }
            }
            
            EditorGUILayout.Space(5);
        }

        private bool IsOffsetInPattern(Vector2Int offset, AttackPattern pattern, int range)
        {
            int dx = Mathf.Abs(offset.x);
            int dy = Mathf.Abs(offset.y);

            if (dx > range || dy > range) return false;

            return pattern switch
            {
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
            
            int index = offsetsProp.arraySize;
            offsetsProp.InsertArrayElementAtIndex(index);
            offsetsProp.GetArrayElementAtIndex(index).vector2IntValue = offset;
        }

        private string GetStarLabel(UnitRarity rarity)
        {
            return rarity switch
            {
                UnitRarity.Common => "1 Star \u2605",
                UnitRarity.Uncommon => "2 Star \u2605\u2605",
                UnitRarity.Rare => "3 Star \u2605\u2605\u2605",
                UnitRarity.Elite => "4 Star \u2605\u2605\u2605\u2605",
                UnitRarity.Master => "5 Star \u2605\u2605\u2605\u2605\u2605",
                UnitRarity.Legendary => "6 Star \u2605\u2605\u2605\u2605\u2605\u2605",
                _ => ""
            };
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
    }
}
