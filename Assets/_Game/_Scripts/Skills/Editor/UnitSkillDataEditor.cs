using UnityEngine;
using UnityEditor;
using MaouSamaTD.Skills;
using MaouSamaTD.Units;
using System.Collections.Generic;

namespace MaouSamaTD.Skills.Editor
{
    [CustomEditor(typeof(UnitSkillData))]
    public class UnitSkillDataEditor : UnityEditor.Editor
    {
        private UnitSkillData _target;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "General", "Visuals & Skins", "Stats" };
        private int _selectedOverrideTab = 0;

        private UnitData _cachedUnit;
        private string[] _skinOptions;
        private string[] _skinDisplayNames;

        private void OnEnable()
        {
            _target = (UnitSkillData)target;
            UpdateSkinOptionsFromOwner();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

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
                case 1: DrawVisualsTab(); break;
                case 2: DrawStatsTab(); break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGeneralTab()
        {
            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Skill Identity", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Identity", EditorStyles.miniBoldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("UniqueID"), new GUIContent("System ID (GUID)"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SkillName"), new GUIContent("Display Name"));
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
            
            // Responsive Icon Field
            DrawResponsiveSpriteField(serializedObject.FindProperty("Icon"), "Skill Icon");

            GUILayout.Space(10);
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Costs", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ChargeCost"), new GUIContent("SP Cost"));
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
            
            // Description Row
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Lore / Skill Description", EditorStyles.miniBoldLabel);
            SerializedProperty descProp = serializedObject.FindProperty("Description");
            descProp.stringValue = EditorGUILayout.TextArea(descProp.stringValue, EditorStyles.textArea, GUILayout.MinHeight(60), GUILayout.ExpandWidth(true));
            EditorGUILayout.EndVertical();
            
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
        }

        private void DrawVisualsTab()
        {
            // Owner Unit Reference (Required for skin overrides)
            SerializedProperty ownerProp = serializedObject.FindProperty("OwnerUnit");
            
            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            
            bool hasOwner = ownerProp.objectReferenceValue != null;
            
            if (!hasOwner) GUI.color = Color.yellow;
            EditorGUILayout.BeginHorizontal();
            string ownerLabel = hasOwner ? "Linked Unit" : "Linked Unit (Required *)";
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(ownerProp, new GUIContent(ownerLabel));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                UpdateSkinOptionsFromOwner();
            }
            if (GUILayout.Button("Refresh", GUILayout.Width(70))) UpdateSkinOptionsFromOwner();
            EditorGUILayout.EndHorizontal();
            GUI.color = Color.white;
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical("helpbox");
            GUI.color = new Color(0.8f, 0.9f, 1f);
            EditorGUILayout.LabelField("✦ BASE VISUALS (Global Default) ✦", EditorStyles.boldLabel);
            GUI.color = Color.white;
            EditorGUI.indentLevel++;
            
            SerializedProperty baseVisualsProp = serializedObject.FindProperty("BaseVisuals");
            DrawVisualFields(baseVisualsProp);
            
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical("helpbox");
            
            SerializedProperty overridesProp = serializedObject.FindProperty("SkinOverrides");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Skin Specific Overrides ({overridesProp.arraySize})", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Add Override", GUILayout.Width(110)))
            {
                overridesProp.arraySize++;
                _selectedOverrideTab = overridesProp.arraySize - 1;
            }
            EditorGUILayout.EndHorizontal();

            if (overridesProp.arraySize > 0)
            {
                string[] overrideNames = new string[overridesProp.arraySize];
                for (int i = 0; i < overridesProp.arraySize; i++)
                {
                    var skinID = overridesProp.GetArrayElementAtIndex(i).FindPropertyRelative("SkinID").stringValue;
                    overrideNames[i] = string.IsNullOrEmpty(skinID) ? $"Override {i}" : skinID;
                }

                _selectedOverrideTab = Mathf.Clamp(_selectedOverrideTab, 0, overridesProp.arraySize - 1);
                _selectedOverrideTab = GUILayout.SelectionGrid(_selectedOverrideTab, overrideNames, 4, EditorStyles.miniButton);

                EditorGUILayout.Space(5);
                
                SerializedProperty selectedOverride = overridesProp.GetArrayElementAtIndex(_selectedOverrideTab);
                
                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.BeginHorizontal();
                
                SerializedProperty skinIDProp = selectedOverride.FindPropertyRelative("SkinID");
                
                // Always use Popup, since _skinOptions is guaranteed to have at least "None"
                int currentIndex = _skinOptions != null ? System.Array.IndexOf(_skinOptions, skinIDProp.stringValue) : -1;
                
                int newIndex = EditorGUILayout.Popup("Target Skin", Mathf.Max(0, currentIndex), _skinDisplayNames);
                if (newIndex >= 0 && newIndex < (_skinOptions?.Length ?? 0))
                {
                    skinIDProp.stringValue = _skinOptions[newIndex];
                }

                GUI.color = Color.red;
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    overridesProp.DeleteArrayElementAtIndex(_selectedOverrideTab);
                    _selectedOverrideTab = Mathf.Max(0, _selectedOverrideTab - 1);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                    return;
                }
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
                DrawVisualFields(selectedOverride.FindPropertyRelative("Visuals"));
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("No skin-specific overrides defined.", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
        }

        private void DrawVisualFields(SerializedProperty visualsProp)
        {
            if (visualsProp == null) return;

            EditorGUILayout.LabelField("Assets", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(visualsProp.FindPropertyRelative("UltimatePrefab"), new GUIContent("Ultimate Projectile"));
            EditorGUILayout.PropertyField(visualsProp.FindPropertyRelative("CastVFX"), new GUIContent("Cast VFX"));
            EditorGUILayout.PropertyField(visualsProp.FindPropertyRelative("HitVFX"), new GUIContent("Impact VFX"));
            
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Audio", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(visualsProp.FindPropertyRelative("CastSFX"), new GUIContent("Cast Sound"));
            EditorGUILayout.PropertyField(visualsProp.FindPropertyRelative("HitSFX"), new GUIContent("Impact Sound"));

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Colors & Animation", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(visualsProp.FindPropertyRelative("UltimateColor"), new GUIContent("Banner Color"));
            EditorGUILayout.PropertyField(visualsProp.FindPropertyRelative("TitleBgColor"), new GUIContent("Title BG Color"));
            EditorGUILayout.PropertyField(visualsProp.FindPropertyRelative("SkillNameBgColor"), new GUIContent("Name BG Color"));
            EditorGUILayout.PropertyField(visualsProp.FindPropertyRelative("RangeIndicatorColor"), new GUIContent("AoE Color"));
            EditorGUILayout.PropertyField(visualsProp.FindPropertyRelative("AnimationTriggerName"), new GUIContent("Anim Trigger"));
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

        private void DrawStatsTab()
        {
            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Stat Growth (Per Level)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            DrawProperty("BonusHpPerLevel", "Bonus HP / Lvl");
            DrawProperty("BonusAtkPerLevel", "Bonus ATK / Lvl");
            DrawProperty("BonusDefPerLevel", "Bonus DEF / Lvl");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
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

        private void UpdateSkinOptionsFromOwner()
        {
            SerializedProperty ownerProp = serializedObject.FindProperty("OwnerUnit");
            UnitData unit = ownerProp?.objectReferenceValue as UnitData;
            
            List<string> ids = new List<string> { "" };
            List<string> displays = new List<string> { "None" };

            if (unit != null)
            {
                foreach (var skin in unit.Skins)
                {
                    if (skin != null && !string.IsNullOrEmpty(skin.SkinID))
                    {
                        ids.Add(skin.SkinID);
                        string theme = string.IsNullOrEmpty(skin.SkinThemeName) ? "Unnamed" : skin.SkinThemeName;
                        displays.Add($"{theme} ({skin.SkinID})");
                    }
                }
            }
            
            _skinOptions = ids.ToArray();
            _skinDisplayNames = displays.ToArray();
        }
    }
}
