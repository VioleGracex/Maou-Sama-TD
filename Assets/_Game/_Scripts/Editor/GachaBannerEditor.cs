using UnityEngine;
using UnityEditor;
using MaouSamaTD.Data;
using System.Collections.Generic;

namespace MaouSamaTD.Editor
{
    [CustomEditor(typeof(GachaBannerSO))]
    public class GachaBannerEditor : UnityEditor.Editor
    {
        private GUIStyle _headerStyle;
        private GUIStyle _tabStyle;
        private int _activeTab = 0;
        private readonly string[] _tabLabels = { "Identity", "Costs & Pool", "Rates & Pity", "UI/Details" };

        private bool _showClassicEditor = false;

        private void OnEnable()
        {
            _headerStyle = new GUIStyle();
            _headerStyle.fontSize = 18;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.alignment = TextAnchor.MiddleLeft;
            _headerStyle.normal.textColor = Color.white;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GachaBannerSO banner = (GachaBannerSO)target;

            // 1. Classic Editor Switch (Button at Top)
            if (!_showClassicEditor)
            {
                if (GUILayout.Button("Switch to Classic Inspector", GUILayout.Height(25)))
                {
                    _showClassicEditor = true;
                }
            }
            else
            {
                if (GUILayout.Button("Return to Custom Editor", GUILayout.Height(25)))
                {
                    _showClassicEditor = false;
                }
                EditorGUILayout.Space(10);
                base.OnInspectorGUI();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Space(5);

            // 2. Tab Selection & Content
            _activeTab = GUILayout.Toolbar(_activeTab, _tabLabels);
            EditorGUILayout.Space(10);

            switch (_activeTab)
            {
                case 0: DrawIdentityTab(); break;
                case 1: DrawCostsTab(); break;
                case 2: DrawRatesTab(); break;
                case 3: DrawUIDetailsTab(); break;
            }

            EditorGUILayout.Space(20);

            // 3. Banner Preview Art
            DrawBannerArtPreview(banner);

            EditorGUILayout.Space(10);

            // 4. Tab Image Preview (New)
            DrawTabImagePreview(banner);

            EditorGUILayout.Space(10);

            // 5. Designer Specifications (Bottom-most)
            EditorGUILayout.HelpBox("DESIGNER SPECIFICATIONS:\n" +
                                    "• Tab Image: 200 x 100\n" +
                                    "• Banner Image: 1024 x 768\n" +
                                    "• Full Banner Card Panel: 1450 x 768", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBannerArtPreview(GachaBannerSO banner)
        {
            if (banner.BannerArt != null)
            {
                // Scaled down preview (200px height) and ScaleToFit to see the full image
                Rect rect = GUILayoutUtility.GetRect(Screen.width, 200);
                GUI.DrawTexture(rect, banner.BannerArt.texture, ScaleMode.ScaleToFit);
            }
        }

        private void DrawTabImagePreview(GachaBannerSO banner)
        {
            if (banner.TabImage != null)
            {
                EditorGUILayout.LabelField("Tab Icon Preview", EditorStyles.miniBoldLabel);
                // Tab image is 200x100, so we'll show it at a 100px height ScaleToFit
                Rect rect = GUILayoutUtility.GetRect(Screen.width, 100);
                GUI.DrawTexture(rect, banner.TabImage.texture, ScaleMode.ScaleToFit);
                EditorGUILayout.Space(5);
            }
        }

        private void DrawIdentityTab()
        {
            EditorGUILayout.LabelField("IDENTITY", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("BannerID"));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("BannerName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("BannerArt"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("TabImage"));
            
            EditorGUILayout.LabelField("Short Description");
            SerializedProperty desc = serializedObject.FindProperty("Description");
            desc.stringValue = EditorGUILayout.TextArea(desc.stringValue, GUILayout.Height(60));
        }

        private void DrawCostsTab()
        {
            EditorGUILayout.LabelField("SUMMON COSTS", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Currency"));
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SingleCost"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MultiCost"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("POOL SETUP", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Pool"));
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("FEATURED UNIT", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FeaturedUnitName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FeaturedUnitTitle"));
        }

        private void DrawRatesTab()
        {
            EditorGUILayout.LabelField("RATES (%)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LegendaryRate"), new GUIContent(" Legendary (6*)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MasterRate"), new GUIContent(" Master (5*)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("EliteRate"), new GUIContent(" Elite (4*)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("RareRate"), new GUIContent(" Rare (3*)"));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("PITY SETTINGS", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("HasPity"));
            if (serializedObject.FindProperty("HasPity").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("PityThreshold"));
            }
        }

        private void DrawUIDetailsTab()
        {
            EditorGUILayout.LabelField("UI DISPLAY STRINGS", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("Detailed Description (Popup Panel)");
            SerializedProperty detailedDesc = serializedObject.FindProperty("DetailedDescription");
            detailedDesc.stringValue = EditorGUILayout.TextArea(detailedDesc.stringValue, GUILayout.Height(100));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Probability Table (Details View)");
            SerializedProperty probDetails = serializedObject.FindProperty("ProbabilityDetails");
            probDetails.stringValue = EditorGUILayout.TextArea(probDetails.stringValue, GUILayout.Height(150));
        }
    }
}
