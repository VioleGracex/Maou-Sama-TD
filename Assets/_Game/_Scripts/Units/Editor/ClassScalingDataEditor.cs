using UnityEngine;
using UnityEditor;
using MaouSamaTD.Units;
using MaouSamaTD.Core;
using System.Linq;

namespace MaouSamaTD.Units.Editor
{
    [CustomEditor(typeof(ClassScalingData))]
    public class ClassScalingDataEditor : UnityEditor.Editor
    {
        private ClassScalingData _target;
        private int _selectedTabIndex = 0;
        private string[] _allClassNames;
        private UnitClass[] _allClasses;

        private void OnEnable()
        {
            _target = (ClassScalingData)target;
            _allClasses = (UnitClass[])System.Enum.GetValues(typeof(UnitClass));
            _allClassNames = _allClasses.Select(c => c.ToString()).ToArray();
        }

        public override void OnInspectorGUI()
        {
            if (_target == null) _target = (ClassScalingData)target;
            
            serializedObject.Update();

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 12 };
            
            // Accessing useDefaultInspector from GameDataSO
            if (GUILayout.Button(_target.useDefaultInspector ? "Switch to Custom Editor" : "Switch to Default Editor", buttonStyle, GUILayout.Height(30)))
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AssetLabel"));
            EditorGUILayout.Space(10);

            if (_allClassNames == null || _allClassNames.Length == 0)
            {
                _allClasses = (UnitClass[])System.Enum.GetValues(typeof(UnitClass));
                _allClassNames = _allClasses.Select(c => c.ToString()).ToArray();
            }

            // Tabs for each Class
            _selectedTabIndex = GUILayout.SelectionGrid(_selectedTabIndex, _allClassNames, 4);
            
            EditorGUILayout.Space(10);

            if (_selectedTabIndex < _allClasses.Length)
            {
                UnitClass selectedClass = _allClasses[_selectedTabIndex];
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
                EditorGUILayout.PropertyField(scaling.FindPropertyRelative("ClassIcon"));
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Base Multipliers", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(scaling.FindPropertyRelative("BaseHpMultiplier"), new GUIContent("HP Multiplier"));
                EditorGUILayout.PropertyField(scaling.FindPropertyRelative("BaseAtkMultiplier"), new GUIContent("ATK Multiplier"));
                EditorGUILayout.PropertyField(scaling.FindPropertyRelative("BaseDefMultiplier"), new GUIContent("DEF Multiplier"));

                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(scaling.FindPropertyRelative("RarityGrowths"), true);
                
                EditorGUILayout.Space(10);
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
    }
}
