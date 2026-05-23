using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class FixBriefingPanelLayoutComplete
{
    [MenuItem("Tools/Fix Briefing Panel Layout Complete")]
    public static void Fix()
    {
        string path = "Assets/_Game/Prefabs/UI/campaign/BriefingPanel.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
        
        Transform visualRoot = prefabRoot.transform.Find("VisualRoot");
        if (visualRoot != null)
        {
            // 1. VisualRoot Layout
            var vlg = visualRoot.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = visualRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(40, 40, 40, 40);
            vlg.spacing = 20f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true; // MUST BE TRUE for flexible height to push!
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // 2. TextHolderTitle
            Transform textHolder = visualRoot.Find("TextHolderTitle");
            if (textHolder != null)
            {
                var thVlg = textHolder.GetComponent<VerticalLayoutGroup>();
                if (thVlg != null)
                {
                    thVlg.childControlWidth = true;
                    thVlg.childControlHeight = true; // Ensure texts stack nicely
                    thVlg.childForceExpandWidth = true;
                    thVlg.childForceExpandHeight = false;
                }
            }

            // 3. BriefingScrollView
            Transform scrollView = visualRoot.Find("BriefingScrollView");
            if (scrollView != null)
            {
                var le = scrollView.GetComponent<LayoutElement>();
                if (le == null) le = scrollView.gameObject.AddComponent<LayoutElement>();
                le.flexibleHeight = 1f; // Take remaining space
                le.minHeight = 200f; // Give it a minimum height

                // Fix Content inside ScrollView
                Transform viewport = scrollView.Find("Viewport");
                if (viewport != null)
                {
                    Transform content = viewport.Find("Content");
                    if (content != null)
                    {
                        var cvlg = content.GetComponent<VerticalLayoutGroup>();
                        if (cvlg == null) cvlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
                        cvlg.padding = new RectOffset(10, 10, 10, 10);
                        cvlg.spacing = 15f;
                        cvlg.childControlWidth = true;
                        cvlg.childControlHeight = true; // ESSENTIAL for stacking children properly
                        cvlg.childForceExpandWidth = true;
                        cvlg.childForceExpandHeight = false;
                        cvlg.childAlignment = TextAnchor.UpperLeft;

                        var csf = content.GetComponent<ContentSizeFitter>();
                        if (csf != null)
                        {
                            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                        }

                        // Fix nested containers
                        string[] containers = { "Container_Enemies", "Container_OneTimeRewards", "Container_ReplayRewards", "Container_StageDrops", "Container_Conditions" };
                        foreach (string cName in containers)
                        {
                            Transform c = content.Find(cName);
                            if (c != null)
                            {
                                var hlg = c.GetComponent<HorizontalLayoutGroup>();
                                if (hlg != null)
                                {
                                    hlg.childControlWidth = false; // Keep prefab widths
                                    hlg.childControlHeight = true; // Allow them to take height
                                    hlg.childForceExpandWidth = false;
                                    hlg.childForceExpandHeight = false;
                                    hlg.spacing = 10f;
                                }
                                var cvlgNested = c.GetComponent<VerticalLayoutGroup>();
                                if (cvlgNested != null)
                                {
                                    cvlgNested.childControlWidth = true;
                                    cvlgNested.childControlHeight = true;
                                    cvlgNested.childForceExpandWidth = true;
                                    cvlgNested.childForceExpandHeight = false;
                                    cvlgNested.spacing = 10f;
                                }
                            }
                        }
                    }
                }
            }

            // 4. RewardsContainer (If it exists, make sure it has flexible/preferred height)
            Transform rewards = visualRoot.Find("RewardsContainer");
            if (rewards != null)
            {
                var le = rewards.GetComponent<LayoutElement>();
                if (le == null) le = rewards.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 100f; // Ensure it has space
            }

            // 5. BottomButtonsGroup
            Transform bottomGroup = visualRoot.Find("BottomButtonsGroup");
            if (bottomGroup != null)
            {
                var le = bottomGroup.GetComponent<LayoutElement>();
                if (le == null) le = bottomGroup.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 60f;
                le.preferredHeight = 60f;

                var hlg = bottomGroup.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null)
                {
                    hlg.childControlHeight = true;
                    hlg.childControlWidth = false;
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                }
            }

            // Reorder safely
            if (textHolder != null) textHolder.SetSiblingIndex(0);
            Transform sep = visualRoot.Find("BriefingSeprator");
            if (sep != null) sep.SetSiblingIndex(1);
            if (scrollView != null) scrollView.SetSiblingIndex(2);
            if (rewards != null) rewards.SetSiblingIndex(3);
            if (bottomGroup != null) bottomGroup.SetSiblingIndex(4);
            
            // 6. Close Button
            Transform closeBtn = visualRoot.Find("CloseButton");
            if (closeBtn != null)
            {
                var leClose = closeBtn.GetComponent<LayoutElement>();
                if (leClose == null) leClose = closeBtn.gameObject.AddComponent<LayoutElement>();
                leClose.ignoreLayout = true;
                
                var rtClose = closeBtn.GetComponent<RectTransform>();
                rtClose.anchorMin = new Vector2(1, 1);
                rtClose.anchorMax = new Vector2(1, 1);
                rtClose.pivot = new Vector2(1, 1);
                rtClose.anchoredPosition = new Vector2(-20, -20); // Inward from top right!
            }

            // Run PopulatePlaceholders using reflection
            var script = prefabRoot.GetComponent<MaouSamaTD.UI.MainMenu.BriefingPanel>();
            if (script != null)
            {
                var method = script.GetType().GetMethod("PopulatePlaceholders", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(script, null);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            Debug.Log("BriefingPanel Complete Layout Fixed!");
        }
    }
}
