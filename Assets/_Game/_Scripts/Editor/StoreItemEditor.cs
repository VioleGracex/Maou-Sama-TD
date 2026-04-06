using UnityEngine;
using UnityEditor;
using MaouSamaTD.Data;
using MaouSamaTD.Units;
using System.Collections.Generic;
using System.Linq;

namespace MaouSamaTD.Editor
{
    [CustomEditor(typeof(StoreItemSO))]
    public class StoreItemEditor : UnityEditor.Editor
    {
        private StoreItemSO _target;
        private List<string> _availableSkins = new List<string>();
        private bool _isScanning = false;

        private void OnEnable()
        {
            _target = (StoreItemSO)target;
            RefreshSkinList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Default drawing for common fields
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ItemName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Description"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Type"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("USDPrice"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("GemPrice"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Icon"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("IsOfficialOffering"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Specific Content", EditorStyles.boldLabel);

            var type = (StoreItemType)serializedObject.FindProperty("Type").enumValueIndex;

            if (type == StoreItemType.Currency)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("CurrencyAmount"));
            }
            else if (type == StoreItemType.Skin)
            {
                DrawSkinSelector();
            }
            else if (type == StoreItemType.Gift)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("GiftID"));
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSkinSelector()
        {
            var skinIdProp = serializedObject.FindProperty("SkinID");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Skin Browser", EditorStyles.miniBoldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(skinIdProp);
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                ShowSkinDropdown(skinIdProp);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Refresh Units", EditorStyles.miniButton))
            {
                RefreshSkinList();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void ShowSkinDropdown(SerializedProperty prop)
        {
            GenericMenu menu = new GenericMenu();
            string currentIds = prop.stringValue;
            string[] selected = currentIds.Split(',', System.StringSplitOptions.RemoveEmptyEntries);

            foreach (var skin in _availableSkins)
            {
                bool isSelected = selected.Contains(skin);
                menu.AddItem(new GUIContent(skin.Replace("_", "/")), isSelected, () => 
                {
                    ToggleSkin(prop, skin);
                });
            }

            menu.ShowAsContext();
        }

        private void ToggleSkin(SerializedProperty prop, string skinId)
        {
            HashSet<string> selected = new HashSet<string>(prop.stringValue.Split(',', System.StringSplitOptions.RemoveEmptyEntries));
            
            if (selected.Contains(skinId))
                selected.Remove(skinId);
            else
                selected.Add(skinId);

            prop.stringValue = string.Join(",", selected);
            prop.serializedObject.ApplyModifiedProperties();
        }

        private void RefreshSkinList()
        {
            _availableSkins.Clear();
            string[] guids = AssetDatabase.FindAssets("t:UnitData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnitData unit = AssetDatabase.LoadAssetAtPath<UnitData>(path);
                if (unit != null)
                {
                    // Add Base Skin Option (represented as empty/null ID or a specific convention)
                    // But usually base is already unlocked, so we prioritize premium skins.
                    _availableSkins.Add($"{unit.UnitName} (Base)");

                    // Add Premium Skins
                    if (unit.Skins != null)
                    {
                        foreach (var skin in unit.Skins)
                        {
                            if (!string.IsNullOrEmpty(skin.SkinID))
                                _availableSkins.Add(skin.SkinID);
                        }
                    }
                }
            }
            _availableSkins = _availableSkins.Distinct().OrderBy(s => s).ToList();
        }
    }
}
