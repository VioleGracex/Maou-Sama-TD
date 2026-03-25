using UnityEngine;
using UnityEditor;
using MaouSamaTD.Units;
using MaouSamaTD.Core;
using System.Linq;

namespace MaouSamaTD.Units.Editor
{
    [CustomEditor(typeof(EnemyScalingData))]
    public class EnemyScalingDataEditor : UnityEditor.Editor
    {
        private EnemyScalingData _target;
        private int _selectedEnemyIndex = 0;
        private int _selectedStarIndex = 0;
        private string[] _enemyClassNames;
        private UnitClass[] _enemyClasses;

        private void OnEnable()
        {
            _target = (EnemyScalingData)target;
            RefreshClasses();
        }

        private void RefreshClasses()
        {
            var allClasses = (UnitClass[])System.Enum.GetValues(typeof(UnitClass));
            // Filter only Enemy types
            _enemyClasses = allClasses.Where(c => 
                c == UnitClass.EnemyMelee || 
                c == UnitClass.EnemyRanged || 
                c == UnitClass.EnemyBoss).ToArray();
            
            _enemyClassNames = _enemyClasses.Select(c => c.ToString()).ToArray();
        }

        public override void OnInspectorGUI()
        {
            if (_target == null) _target = (EnemyScalingData)target;
            
            serializedObject.Update();

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 12 };
            
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

            if (_enemyClassNames == null || _enemyClassNames.Length == 0) RefreshClasses();

            // Tabs for each Enemy Type
            _selectedEnemyIndex = GUILayout.SelectionGrid(_selectedEnemyIndex, _enemyClassNames, 3);
            
            EditorGUILayout.Space(10);

            if (_selectedEnemyIndex < _enemyClasses.Length)
            {
                UnitClass selectedClass = _enemyClasses[_selectedEnemyIndex];
                DrawEnemyScalingArea(selectedClass);
            }

            EditorGUILayout.Space(20);
            
            if (GUILayout.Button("Show Raw Data", EditorStyles.miniButton))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("EnemyScalings"), true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEnemyScalingArea(UnitClass classType)
        {
            SerializedProperty scalingsProp = serializedObject.FindProperty("EnemyScalings");
            int existingIndex = -1;

            if (scalingsProp == null) return;

            for (int i = 0; i < scalingsProp.arraySize; i++)
            {
                SerializedProperty element = scalingsProp.GetArrayElementAtIndex(i);
                if (element.FindPropertyRelative("EnemyType").enumValueIndex == (int)classType)
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                SerializedProperty scaling = scalingsProp.GetArrayElementAtIndex(existingIndex);
                
                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.LabelField($"Enemy Scaling Settings for {classType}", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                EditorGUILayout.PropertyField(scaling.FindPropertyRelative("OverrideName"));
                
                // Icon Preview Area
                DrawIconWithPreview(scaling.FindPropertyRelative("Icon"));
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Base Multipliers", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(scaling.FindPropertyRelative("BaseHpMultiplier"), new GUIContent("HP Multiplier"));
                EditorGUILayout.PropertyField(scaling.FindPropertyRelative("BaseAtkMultiplier"), new GUIContent("ATK Multiplier"));
                EditorGUILayout.PropertyField(scaling.FindPropertyRelative("BaseDefMultiplier"), new GUIContent("DEF Multiplier"));

                EditorGUILayout.Space(10);
                DrawDifficultyGrowthTabs(scaling.FindPropertyRelative("DifficultyGrowths"));
                
                EditorGUILayout.Space(15);
                if (GUILayout.Button("Remove This Enemy Entry", GUILayout.Width(180)))
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
                    newItem.FindPropertyRelative("EnemyType").enumValueIndex = (int)classType;
                    
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

        private void DrawDifficultyGrowthTabs(SerializedProperty growthsProp)
        {
            EditorGUILayout.LabelField("Difficulty (Star) Growth", EditorStyles.boldLabel);
            
            string[] starTabs = { "1⭐", "2⭐", "3⭐", "4⭐", "5⭐", "6⭐" };
            _selectedStarIndex = GUILayout.Toolbar(_selectedStarIndex, starTabs);

            // Ensure we have 6 elements
            while (growthsProp.arraySize < 6)
            {
                growthsProp.InsertArrayElementAtIndex(growthsProp.arraySize);
                SerializedProperty newGrowth = growthsProp.GetArrayElementAtIndex(growthsProp.arraySize - 1);
                newGrowth.FindPropertyRelative("Rarity").enumValueIndex = growthsProp.arraySize - 1;
            }

            SerializedProperty selectedGrowth = growthsProp.GetArrayElementAtIndex(_selectedStarIndex);
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Tier {(_selectedStarIndex + 1)} Growth Stats", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(selectedGrowth.FindPropertyRelative("HpGrowthPerLevel"), new GUIContent("HP Growth"));
            EditorGUILayout.PropertyField(selectedGrowth.FindPropertyRelative("AtkGrowthPerLevel"), new GUIContent("ATK Growth"));
            EditorGUILayout.PropertyField(selectedGrowth.FindPropertyRelative("DefGrowthPerLevel"), new GUIContent("DEF Growth"));
            EditorGUILayout.EndVertical();
        }
    }
}
