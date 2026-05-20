using UnityEngine;
using UnityEditor;
using MaouSamaTD.Units;
using MaouSamaTD.Core;
using System.Linq;
using System.Collections.Generic;

namespace MaouSamaTD.Units.Editor
{
    [CustomEditor(typeof(ClassScalingData))]
    public class ClassScalingDataEditor : UnityEditor.Editor
    {
        private ClassScalingData _target;
        private int _selectedClassIndex = 0;
        private int _selectedStarIndex = 0;
        private string[] _vassalClassNames;
        private UnitClass[] _vassalClasses;

        private void OnEnable()
        {
            _target = (ClassScalingData)target;
            RefreshClasses();
        }

        private void RefreshClasses()
        {
            var allClasses = (UnitClass[])System.Enum.GetValues(typeof(UnitClass));
            // Filter out Enemy types
            _vassalClasses = allClasses.Where(c => 
                c != UnitClass.EnemyMelee && 
                c != UnitClass.EnemyRanged && 
                c != UnitClass.EnemyBoss).ToArray();
            
            _vassalClassNames = _vassalClasses.Select(c => c.ToString()).ToArray();
        }

        public override void OnInspectorGUI()
        {
            if (_target == null) _target = (ClassScalingData)target;
            
            serializedObject.Update();

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14, alignment = TextAnchor.MiddleCenter };
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 12 };
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(_target.useDefaultInspector ? "Switch to Custom Editor" : "Switch to Default Editor", buttonStyle, GUILayout.Height(30)))
            {
                _target.useDefaultInspector = !_target.useDefaultInspector;
                EditorUtility.SetDirty(_target);
            }
            if (GUILayout.Button(new GUIContent(" Force Refresh", EditorGUIUtility.IconContent("d_Refresh").image), buttonStyle, GUILayout.Height(30)))
            {
                AssetDatabase.Refresh();
                RefreshClasses();
                EditorUtility.SetDirty(_target);
            }
            EditorGUILayout.EndHorizontal();

            if (_target.useDefaultInspector)
            {
                DrawDefaultInspector();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AssetLabel"));
            EditorGUILayout.Space(10);

            if (_vassalClassNames == null || _vassalClassNames.Length == 0) RefreshClasses();

            // Tabs for each Class
            _selectedClassIndex = GUILayout.SelectionGrid(_selectedClassIndex, _vassalClassNames, 4);
            
            EditorGUILayout.Space(10);

            if (_selectedClassIndex < _vassalClasses.Length)
            {
                UnitClass selectedClass = _vassalClasses[_selectedClassIndex];
                DrawClassScalingArea(selectedClass);
            }

            EditorGUILayout.Space(20);
            
            if (GUILayout.Button("Show Raw Data", EditorStyles.miniButton))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ClassScalings"), true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawClassScalingArea(UnitClass classType)
        {
            SerializedProperty scalingsProp = serializedObject.FindProperty("ClassScalings");
            int existingIndex = -1;

            if (scalingsProp == null) return;

            for (int i = 0; i < scalingsProp.arraySize; i++)
            {
                SerializedProperty element = scalingsProp.GetArrayElementAtIndex(i);
                if (element.FindPropertyRelative("ClassType").enumValueIndex == (int)classType)
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                SerializedProperty scaling = scalingsProp.GetArrayElementAtIndex(existingIndex);
                
                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.LabelField($"Scaling Settings for {classType}", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                EditorGUILayout.PropertyField(scaling.FindPropertyRelative("OverrideClassName"));
                
                // Icon Preview Area
                DrawIconWithPreview(scaling.FindPropertyRelative("ClassIcon"));
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Base Multipliers", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(scaling.FindPropertyRelative("BaseHpMultiplier"), new GUIContent("HP Multiplier"));
                EditorGUILayout.PropertyField(scaling.FindPropertyRelative("BaseAtkMultiplier"), new GUIContent("ATK Multiplier"));
                EditorGUILayout.PropertyField(scaling.FindPropertyRelative("BaseDefMultiplier"), new GUIContent("DEF Multiplier"));

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Promotion Requirements", EditorStyles.miniBoldLabel);
                DrawRequiredMaterialsList(scaling.FindPropertyRelative("RequiredMaterials"));

                EditorGUILayout.Space(10);
                DrawRarityGrowthTabs(scaling.FindPropertyRelative("RarityGrowths"));
                
                EditorGUILayout.Space(15);
                if (GUILayout.Button("Remove This Class Entry", GUILayout.Width(180)))
                {
                    scalingsProp.DeleteArrayElementAtIndex(existingIndex);
                }
                
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox($"No scaling data found for {classType}.", MessageType.Info);
                if (GUILayout.Button($"Initialize {classType} Scaling"))
                {
                    scalingsProp.InsertArrayElementAtIndex(scalingsProp.arraySize);
                    SerializedProperty newItem = scalingsProp.GetArrayElementAtIndex(scalingsProp.arraySize - 1);
                    newItem.FindPropertyRelative("ClassType").enumValueIndex = (int)classType;
                    
                    // Set some defaults
                    newItem.FindPropertyRelative("BaseHpMultiplier").floatValue = 1.0f;
                    newItem.FindPropertyRelative("BaseAtkMultiplier").floatValue = 1.0f;
                    newItem.FindPropertyRelative("BaseDefMultiplier").floatValue = 1.0f;
                }
            }
        }

        private void DrawIconWithPreview(SerializedProperty iconProp)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(iconProp);
            EditorGUILayout.EndVertical();

            if (iconProp.objectReferenceValue != null)
            {
                Texture2D texture = AssetPreview.GetAssetPreview(iconProp.objectReferenceValue);
                if (texture != null)
                {
                    GUILayout.Label("", GUILayout.Width(64), GUILayout.Height(64));
                    GUI.DrawTexture(GUILayoutUtility.GetLastRect(), texture);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No Icon Set", MessageType.None);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRarityGrowthTabs(SerializedProperty rarityGrowthsProp)
        {
            EditorGUILayout.LabelField("Rarity (Star) Growth", EditorStyles.boldLabel);
            
            string[] starTabs = { "1⭐", "2⭐", "3⭐", "4⭐", "5⭐", "6⭐" };
            _selectedStarIndex = GUILayout.Toolbar(_selectedStarIndex, starTabs);

            // Ensure we have 6 elements
            while (rarityGrowthsProp.arraySize < 6)
            {
                rarityGrowthsProp.InsertArrayElementAtIndex(rarityGrowthsProp.arraySize);
                SerializedProperty newGrowth = rarityGrowthsProp.GetArrayElementAtIndex(rarityGrowthsProp.arraySize - 1);
                newGrowth.FindPropertyRelative("Rarity").enumValueIndex = rarityGrowthsProp.arraySize - 1;
            }

            SerializedProperty selectedGrowth = rarityGrowthsProp.GetArrayElementAtIndex(_selectedStarIndex);
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{_selectedStarIndex + 1}-Star Growth Stats", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(selectedGrowth.FindPropertyRelative("HpGrowthPerLevel"), new GUIContent("HP Growth"));
            EditorGUILayout.PropertyField(selectedGrowth.FindPropertyRelative("AtkGrowthPerLevel"), new GUIContent("ATK Growth"));
            EditorGUILayout.PropertyField(selectedGrowth.FindPropertyRelative("DefGrowthPerLevel"), new GUIContent("DEF Growth"));
            EditorGUILayout.EndVertical();
        }

        private void DrawRequiredMaterialsList(SerializedProperty materialsProp)
        {
            if (materialsProp == null || !materialsProp.isArray) return;

            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Item ID", EditorStyles.boldLabel, GUILayout.Width(180));
            EditorGUILayout.LabelField("Base Amount", EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Add", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                int newIndex = materialsProp.arraySize;
                materialsProp.InsertArrayElementAtIndex(newIndex);
                if (materialsProp.arraySize > 0)
                {
                    SerializedProperty newElem = materialsProp.GetArrayElementAtIndex(newIndex);
                    newElem.FindPropertyRelative("ItemID").stringValue = "";
                    newElem.FindPropertyRelative("BaseAmount").intValue = 1;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(2);

            int indexToRemove = -1;
            for (int i = 0; i < materialsProp.arraySize; i++)
            {
                SerializedProperty element = materialsProp.GetArrayElementAtIndex(i);
                SerializedProperty itemIdProp = element.FindPropertyRelative("ItemID");
                SerializedProperty baseAmountProp = element.FindPropertyRelative("BaseAmount");

                EditorGUILayout.BeginHorizontal();
                
                itemIdProp.stringValue = EditorGUILayout.TextField(itemIdProp.stringValue, GUILayout.Width(180));
                baseAmountProp.intValue = EditorGUILayout.IntField(baseAmountProp.intValue, GUILayout.Width(120));
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(30)))
                {
                    indexToRemove = i;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (indexToRemove >= 0)
            {
                materialsProp.DeleteArrayElementAtIndex(indexToRemove);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
