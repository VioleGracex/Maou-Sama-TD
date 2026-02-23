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

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            
            // Draw Sprite
            GUILayout.BeginVertical();
            EditorGUILayout.LabelField("Unit Sprite", GUILayout.Width(100));
            SerializedProperty spriteProp = serializedObject.FindProperty("UnitSprite");
            spriteProp.objectReferenceValue = EditorGUILayout.ObjectField(spriteProp.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(80), GUILayout.Height(80));
            GUILayout.EndVertical();

            // Draw Icon
            GUILayout.BeginVertical();
            EditorGUILayout.LabelField("Unit Icon", GUILayout.Width(100));
            SerializedProperty iconProp = serializedObject.FindProperty("UnitIcon");
            iconProp.objectReferenceValue = EditorGUILayout.ObjectField(iconProp.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(80), GUILayout.Height(80));
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
            DrawProperty("ViableTiles");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
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
