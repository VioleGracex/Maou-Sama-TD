using UnityEngine;
using UnityEditor;
using MaouSamaTD.Units;

namespace MaouSamaTD.Units.Editor
{
    [CustomEditor(typeof(EnemyData))]
    public class EnemyDataEditor : UnityEditor.Editor
    {
        private EnemyData _target;

        private void OnEnable()
        {
            _target = (EnemyData)target;
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
            EditorGUIUtility.labelWidth = 160f;

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
            DrawProperty("EnemyName", "Enemy Name");

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            
            GUILayout.BeginVertical();
            EditorGUILayout.LabelField("Enemy Sprite", GUILayout.Width(100));
            SerializedProperty spriteProp = serializedObject.FindProperty("EnemySprite");
            spriteProp.objectReferenceValue = EditorGUILayout.ObjectField(spriteProp.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(80), GUILayout.Height(80));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Stats & Combat", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("MaxHp", "Max Health Points");
            DrawProperty("MoveSpeed", "Movement Speed");
            DrawProperty("AttackPower", "Attack Power");
            DrawProperty("AttackInterval", "Attack Interval (Sec)");
            DrawProperty("AttackRange", "Attack Range (Tiles)");
            DrawProperty("DamageToPlayerBase", "Damage to Player Base");
            DrawProperty("AttackPattern", "Targeting Pattern");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Behavior & Rules", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("MovementType", "Movement Type");
            DrawProperty("CollisionType", "Collision Rules");
            DrawProperty("CurrencyReward", "Gold/Drops Reward");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Visuals", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("Tint", "Sprite Color Tint");
            DrawProperty("VisualYOffset", "Sprite Y Offset");
            DrawProperty("BaseVisualHeight", "Base Visual Height");
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
    }
}
