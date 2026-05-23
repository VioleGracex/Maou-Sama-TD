using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class FixBriefingPanelLayout
{
    [MenuItem("Tools/Fix Briefing Panel Layout")]
    public static void Fix()
    {
        string path = "Assets/_Game/Prefabs/UI/campaign/BriefingPanel.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
        
        Transform visualRoot = prefabRoot.transform.Find("VisualRoot");
        if (visualRoot != null)
        {
            // 1. Add VerticalLayoutGroup to VisualRoot
            var vlg = visualRoot.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = visualRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(40, 40, 40, 40);
            vlg.spacing = 15f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // 3. Fix TextHolderTitle (Header area)
            Transform textHolder = visualRoot.Find("TextHolderTitle");
            if (textHolder != null)
            {
                var thVlg = textHolder.GetComponent<VerticalLayoutGroup>();
                if (thVlg != null)
                {
                    thVlg.childControlWidth = true;
                    thVlg.childForceExpandWidth = true;
                }
            }

            // 4. Ensure BriefingScrollView expands flexibly
            Transform scrollView = visualRoot.Find("BriefingScrollView");
            if (scrollView != null)
            {
                var le = scrollView.GetComponent<LayoutElement>();
                if (le == null) le = scrollView.gameObject.AddComponent<LayoutElement>();
                le.flexibleHeight = 1f; // Take remaining space!
            }

            // 5. Create BottomButtonsGroup
            Transform bottomGroup = visualRoot.Find("BottomButtonsGroup");
            if (bottomGroup == null)
            {
                GameObject bg = new GameObject("BottomButtonsGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                bg.transform.SetParent(visualRoot, false);
                bottomGroup = bg.transform;
                var hlg = bg.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 50f;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childAlignment = TextAnchor.MiddleCenter;

                var leBG = bg.GetComponent<LayoutElement>();
                leBG.minHeight = 80f;
                leBG.preferredHeight = 80f;
            }

            // Move buttons to BottomButtonsGroup
            Transform prevBtn = visualRoot.Find("PrevLevelButton");
            if (prevBtn != null) prevBtn.SetParent(bottomGroup, false);

            Transform engageBtn = visualRoot.Find("Briefing_Engage_Button");
            if (engageBtn != null) engageBtn.SetParent(bottomGroup, false);

            Transform nextBtn = visualRoot.Find("NextLevelButton");
            if (nextBtn != null) nextBtn.SetParent(bottomGroup, false);

            // Reorder hierarchy inside VisualRoot so things stack correctly:
            // Top: TextHolderTitle
            // Middle: BriefingScrollView
            // Bottom: RewardsContainer, then BottomButtonsGroup
            if (textHolder != null) textHolder.SetSiblingIndex(0);
            Transform sep = visualRoot.Find("BriefingSeprator");
            if (sep != null) sep.SetSiblingIndex(1);
            if (scrollView != null) scrollView.SetSiblingIndex(2);
            Transform rewards = visualRoot.Find("RewardsContainer");
            if (rewards != null) rewards.SetSiblingIndex(3);
            bottomGroup.SetSiblingIndex(4);

            // 6. Handle CloseButton (ignore layout, keep floating top-right)
            Transform closeBtn = visualRoot.Find("CloseButton");
            if (closeBtn != null)
            {
                var leClose = closeBtn.GetComponent<LayoutElement>();
                if (leClose == null) leClose = closeBtn.gameObject.AddComponent<LayoutElement>();
                leClose.ignoreLayout = true;
                
                // Ensure anchors are top-right
                var rtClose = closeBtn.GetComponent<RectTransform>();
                rtClose.anchorMin = new Vector2(1, 1);
                rtClose.anchorMax = new Vector2(1, 1);
                rtClose.pivot = new Vector2(1, 1);
                rtClose.anchoredPosition = new Vector2(25, 25); // Push it slightly outside the window border
            }

            // Save and unload
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            Debug.Log("BriefingPanel Layout Fixed!");
        }
    }
}
