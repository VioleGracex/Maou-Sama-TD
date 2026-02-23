using UnityEngine;
using UnityEditor;
using MaouSamaTD.Skills;

namespace MaouSamaTD.Skills.Editor
{
    [CustomEditor(typeof(UnitSkillData))]
    public class UnitSkillDataEditor : UnityEditor.Editor
    {
        private UnitSkillData _target;

        private void OnEnable()
        {
            _target = (UnitSkillData)target;
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
            EditorGUILayout.LabelField("Visuals & Identity", headerStyle);
            EditorGUI.indentLevel++;
            
            using (new EditorGUI.DisabledScope(true))
            {
                DrawProperty("UniqueID", "Unique ID");
            }
            DrawProperty("SkillName", "Skill Name");

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            
            GUILayout.BeginVertical();
            EditorGUILayout.LabelField("Skill Icon", GUILayout.Width(100));
            SerializedProperty iconProp = serializedObject.FindProperty("Icon");
            iconProp.objectReferenceValue = EditorGUILayout.ObjectField(iconProp.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(80), GUILayout.Height(80));
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            EditorGUILayout.LabelField("Description");
            SerializedProperty descProp = serializedObject.FindProperty("Description");
            descProp.stringValue = EditorGUILayout.TextArea(descProp.stringValue, EditorStyles.textArea, GUILayout.Height(60));
            GUILayout.EndVertical();
            
            GUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
            GUILayout.Space(10);
            
            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Targeting & Effect", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("TargetType", "Targeting Type");
            DrawProperty("Range", "Cast Range");
            DrawProperty("Radius", "AoE Radius");
            GUILayout.Space(5);
            DrawProperty("EffectType", "Effect Type");
            DrawProperty("Value", "Effect Value/Damage");
            DrawProperty("Duration", "Duration (Sec)");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Costs & VFX", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("ChargeCost", "SP Cost (Unit Skills)");
            GUILayout.Space(5);
            DrawProperty("CastVFX", "Cast Effects Prefab");
            DrawProperty("HitVFX", "Impact Effects Prefab");
            DrawProperty("RangeIndicatorColor", "AoE Color Tint");
            DrawProperty("AnimationTriggerName", "Animator Trigger");
            GUILayout.Space(5);
            DrawProperty("CastSFX", "Cast Sound");
            DrawProperty("HitSFX", "Impact Sound");
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Stat Upgrades (Per Level)", headerStyle);
            EditorGUI.indentLevel++;
            DrawProperty("BonusHpPerLevel", "Bonus HP / Level");
            DrawProperty("BonusAtkPerLevel", "Bonus ATK / Level");
            DrawProperty("BonusDefPerLevel", "Bonus DEF / Level");
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
