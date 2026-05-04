using UnityEngine;
using UnityEditor;
using MaouSamaTD.Skills;
using UnityEditorInternal;

namespace MaouSamaTD.Editors
{
    [CustomEditor(typeof(SovereignRiteData))]
    public class SovereignRiteDataEditor : UnityEditor.Editor
    {
        private ReorderableList _modifiersList;
        private SerializedProperty _modifiersProp;
        private SerializedProperty _effectTypeProp;
        private SerializedProperty _persistenceProp;
        private SerializedProperty _valueProp;
        private SerializedProperty _durationProp;
        private SerializedProperty _sealCostProp;
        private SerializedProperty _cooldownProp;
        private SerializedProperty _targetTypeProp;
        private SerializedProperty _rangeProp;
        private SerializedProperty _radiusProp;
        private SerializedProperty _visualsProp;
        private SerializedProperty _skillNameProp;
        private SerializedProperty _descriptionProp;
        private SerializedProperty _iconProp;
        private SerializedProperty _aoeShapeProp;
        private SerializedProperty _customOffsetsProp;
        private bool _showDefaultInspector;
        private int _tabIndex = 0;
        private string[] _tabNames = { "Identity", "Targeting", "Effects", "Visuals" };

        private void OnEnable()
        {
            _modifiersProp = serializedObject.FindProperty("Modifiers");
            _effectTypeProp = serializedObject.FindProperty("EffectType");
            _persistenceProp = serializedObject.FindProperty("Persistence");
            _valueProp = serializedObject.FindProperty("Value");
            _durationProp = serializedObject.FindProperty("Duration");
            _sealCostProp = serializedObject.FindProperty("SealCost");
            _cooldownProp = serializedObject.FindProperty("Cooldown");
            _targetTypeProp = serializedObject.FindProperty("TargetType");
            _rangeProp = serializedObject.FindProperty("Range");
            _radiusProp = serializedObject.FindProperty("Radius");
            _visualsProp = serializedObject.FindProperty("BaseVisuals");
            _skillNameProp = serializedObject.FindProperty("SkillName");
            _descriptionProp = serializedObject.FindProperty("Description");
            _iconProp = serializedObject.FindProperty("Icon");
            _aoeShapeProp = serializedObject.FindProperty("AoeShape");
            _customOffsetsProp = serializedObject.FindProperty("CustomShapeOffsets");

            _modifiersList = new ReorderableList(serializedObject, _modifiersProp, true, true, true, true);
            
            _modifiersList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width * 0.6f, rect.height), "Stat Type");
                EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.6f + 5, rect.y, rect.width * 0.4f - 5, rect.height), "Value (%)");
            };

            _modifiersList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                var element = _modifiersProp.GetArrayElementAtIndex(index);
                rect.y += 2;
                
                float statWidth = rect.width * 0.6f;
                float valWidth = rect.width * 0.4f - 5;

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, statWidth, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Stat"), GUIContent.none);
                
                EditorGUI.PropertyField(
                    new Rect(rect.x + statWidth + 5, rect.y, valWidth, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Value"), GUIContent.none);
            };

            // Fix default black color if needed
            SerializedProperty colorProp = _visualsProp.FindPropertyRelative("RangeIndicatorColor");
            if (colorProp.colorValue == Color.black || colorProp.colorValue == new Color(0,0,0,0))
            {
                // Default to a nice semi-transparent purple for Sovereign powers
                colorProp.colorValue = new Color(0.6f, 0.2f, 0.8f, 0.4f);
                serializedObject.ApplyModifiedProperties();
            }
        }

        public override void OnInspectorGUI()
        {
            /* if (GUILayout.Button("Open Rite Browser", GUILayout.Height(30)))
            {
                SovereignRiteBrowser.Open();
            } */


            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Archetype"));
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(_showDefaultInspector ? "Switch to Custom Editor" : "Switch to Default Inspector", GUILayout.Height(25)))
            {
                _showDefaultInspector = !_showDefaultInspector;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();

            if (_showDefaultInspector)
            {
                DrawDefaultInspector();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            _tabIndex = GUILayout.Toolbar(_tabIndex, _tabNames);
            EditorGUILayout.Space();

            switch (_tabIndex)
            {
                case 0: DrawIdentityTab(); break;
                case 1: DrawTargetingTab(); break;
                case 2: DrawEffectsTab(); break;
                case 3: DrawVisualsTab(); break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawIdentityTab()
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_skillNameProp);
                EditorGUILayout.PropertyField(_descriptionProp);
                
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.PropertyField(_iconProp);
                EditorGUILayout.EndVertical();

                if (_iconProp.objectReferenceValue != null)
                {
                    Texture2D preview = AssetPreview.GetAssetPreview(_iconProp.objectReferenceValue);
                    if (preview != null)
                    {
                        Rect rect = GUILayoutUtility.GetLastRect();
                        GUILayout.Label(GUIContent.none, GUILayout.Width(64), GUILayout.Height(64));
                        GUI.DrawTexture(GUILayoutUtility.GetLastRect(), preview, ScaleMode.ScaleToFit);
                    }
                    else
                    {
                        // Fallback placeholder while loading
                        GUILayout.Box("Loading...", GUILayout.Width(64), GUILayout.Height(64));
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Global Costs", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_sealCostProp);
                EditorGUILayout.PropertyField(_cooldownProp);
            }
        }

        private void DrawTargetingTab()
        {
            EditorGUILayout.LabelField("Costs & Targeting", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Targeting", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_targetTypeProp);
                
                SkillTargetType targetType = (SkillTargetType)_targetTypeProp.enumValueIndex;
                if (targetType == SkillTargetType.Tile)
                {
                    EditorGUILayout.PropertyField(_radiusProp);
                    EditorGUILayout.PropertyField(_aoeShapeProp);
                }
            }

            // ── AOE Radius Preview (Only for Ground Targeting) ───────────────────
            SkillTargetType currentTargetType = (SkillTargetType)_targetTypeProp.enumValueIndex;
            if (currentTargetType == SkillTargetType.Tile)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("AOE Radius Preview", EditorStyles.boldLabel);
                
                float radius = _radiusProp.floatValue;
                // Ensure at least a 1x1 grid (radius 0) up to 5x5
                int gridRadius = Mathf.Min(Mathf.CeilToInt(radius), 5);
                int gridSize = (gridRadius * 2) + 1;
                float cellSize = Mathf.Clamp(220f / gridSize, 14f, 32f);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    // Center the grid
                    Rect r = GUILayoutUtility.GetRect(gridSize * cellSize, gridSize * cellSize, GUILayout.ExpandWidth(false));
                    r.x += (EditorGUIUtility.currentViewWidth - r.width) * 0.4f;

                    AoeShape shape = (AoeShape)_aoeShapeProp.enumValueIndex;
                    Color centerColor = new Color(0.3f, 0.5f, 1.0f, 0.9f);   // blue
                    Color hitColor    = new Color(1.0f, 0.3f, 0.3f, 0.8f);   // red
                    Color missColor   = new Color(0.3f, 0.3f, 0.3f, 0.5f);   // grey

                    for (int y = gridRadius; y >= -gridRadius; y--)
                    {
                        for (int x = -gridRadius; x <= gridRadius; x++)
                        {
                            Rect cellRect = new Rect(r.x + (x + gridRadius) * cellSize, r.y + (gridRadius - y) * cellSize, cellSize - 2, cellSize - 2);
                            Vector2Int coord = new Vector2Int(x, y);
                            bool isCenter = (x == 0 && y == 0);
                            bool inShape = false;

                            if (shape == AoeShape.Custom)
                            {
                                for (int i = 0; i < _customOffsetsProp.arraySize; i++)
                                {
                                    if (_customOffsetsProp.GetArrayElementAtIndex(i).vector2IntValue == coord)
                                    {
                                        inShape = true;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                float dist = Mathf.Sqrt(x * x + y * y);
                                if (shape == AoeShape.Circle) inShape = dist <= radius + 0.1f;
                                else if (shape == AoeShape.Square) inShape = Mathf.Abs(x) <= radius && Mathf.Abs(y) <= radius;
                                else if (shape == AoeShape.Cross) inShape = (x == 0 && Mathf.Abs(y) <= radius) || (y == 0 && Mathf.Abs(x) <= radius);
                                else if (shape == AoeShape.DiagonalX) inShape = Mathf.Abs(x) == Mathf.Abs(y) && dist <= radius + 0.1f;
                                else if (shape == AoeShape.Star) inShape = (x == 0 || y == 0 || Mathf.Abs(x) == Mathf.Abs(y)) && dist <= radius + 0.1f;
                            }

                            // Interaction for Custom Shape
                            if (shape == AoeShape.Custom && !isCenter)
                            {
                                EditorGUIUtility.AddCursorRect(cellRect, MouseCursor.Link);
                                if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                                {
                                    ToggleCustomOffset(coord);
                                    Event.current.Use();
                                }
                            }

                            Color fill = isCenter ? centerColor : (inShape ? hitColor : missColor);
                            EditorGUI.DrawRect(cellRect, fill);
                        }
                    }

                    EditorGUILayout.Space(4);
                    if (radius <= 0)
                    {
                        EditorGUILayout.HelpBox("Radius 0 hits exactly 1 tile.", MessageType.Info);
                    }
                    else if (shape == AoeShape.Custom)
                    {
                        EditorGUILayout.HelpBox("Click tiles to paint custom shape.", MessageType.None);
                    }
                }
            }
        }

        private void DrawEffectsTab()
        {
            EditorGUILayout.LabelField("Rite Effects", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_effectTypeProp);
                EditorGUILayout.PropertyField(_persistenceProp);
                
                SkillEffectType effectType = (SkillEffectType)_effectTypeProp.enumValueIndex;
                SkillPersistenceType persistence = (SkillPersistenceType)_persistenceProp.enumValueIndex;

                // Duration is relevant if the zone is persistent, or if it's a buff/debuff applied to units.
                bool needsDuration = persistence == SkillPersistenceType.Persistent || 
                                     effectType == SkillEffectType.Buff || 
                                     effectType == SkillEffectType.Debuff ||
                                     effectType == SkillEffectType.Zone;

                if (needsDuration)
                {
                    EditorGUILayout.PropertyField(_durationProp);
                }
                
                if (effectType == SkillEffectType.Damage)
                {
                    EditorGUILayout.PropertyField(_valueProp, new GUIContent("Base Damage"));
                }
                else if (effectType == SkillEffectType.Buff || effectType == SkillEffectType.Debuff || effectType == SkillEffectType.Zone)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Stat Table", EditorStyles.miniBoldLabel);
                    _modifiersList.DoLayoutList();
                    
                    if (_modifiersProp.arraySize == 0)
                    {
                        EditorGUILayout.HelpBox("No modifiers added. This rite will have no stat effect.", MessageType.Warning);
                    }
                }
            }
        }

        private void DrawVisualsTab()
        {
            EditorGUILayout.LabelField("Visuals & SFX", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_visualsProp, true);
            }
        }

        private void ToggleCustomOffset(Vector2Int coord)
        {
            int index = -1;
            for (int i = 0; i < _customOffsetsProp.arraySize; i++)
            {
                if (_customOffsetsProp.GetArrayElementAtIndex(i).vector2IntValue == coord)
                {
                    index = i;
                    break;
                }
            }

            if (index >= 0)
            {
                _customOffsetsProp.DeleteArrayElementAtIndex(index);
            }
            else
            {
                int newIndex = _customOffsetsProp.arraySize;
                _customOffsetsProp.InsertArrayElementAtIndex(newIndex);
                _customOffsetsProp.GetArrayElementAtIndex(newIndex).vector2IntValue = coord;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
