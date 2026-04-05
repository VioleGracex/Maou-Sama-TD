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
        private readonly string[] _tabNames = { "General", "Combat", "Visuals" };

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
                case 2: DrawVisualsTab(); break;
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
            DrawProperty("AttackPower", "Attack Power");
            DrawProperty("AttackInterval", "Attack Interval (Sec)");
            DrawProperty("AttackRange", "Attack Range (Tiles)");
            DrawProperty("DamageToPlayerBase", "Damage to Player Base");
            EndSection();

            BeginSection("Combat Pattern");
            DrawProperty("AttackPattern", "Targeting Pattern");
            EndSection();
            
            BeginSection("Immunities");
            DrawProperty("Immunities", "Damage Immunities");
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
                else EditorGUILayout.PropertyField(prop, new GUIContent(label), true);
            }
        }
    }
}
