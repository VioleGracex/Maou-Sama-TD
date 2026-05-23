using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.UI.MainMenu;

namespace MaouSamaTD.Editor
{
    public class FixBriefingPanelLayoutPerfect
    {
        [MenuItem("Tools/Fix and Perfect Briefing Panel")]
        public static void FixAll()
        {
            // Clear current selection to prevent internal Unity Hierarchy window exceptions (HierarchyNode not found)
            Selection.activeObject = null;
            
            string prefabPath = "Assets/_Game/Prefabs/UI/campaign/BriefingPanel.prefab";
            
            // 1. Fix the Sub-Prefabs (MonsterCard and RewardItem) referenced inside the BriefingPanel
            GameObject tempRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (tempRoot != null)
            {
                var bp = tempRoot.GetComponent<MaouSamaTD.UI.MainMenu.BriefingPanel>();
                if (bp != null)
                {
                    // Fix MonsterCard Prefab
                    var monsterCardField = bp.GetType().GetField("_monsterCardPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (monsterCardField != null)
                    {
                        var mcUi = monsterCardField.GetValue(bp) as MaouSamaTD.UI.MainMenu.MonsterCardUI;
                        if (mcUi != null)
                        {
                            string mcPath = AssetDatabase.GetAssetPath(mcUi.gameObject);
                            if (!string.IsNullOrEmpty(mcPath))
                            {
                                GameObject mcRoot = PrefabUtility.LoadPrefabContents(mcPath);
                                if (mcRoot != null)
                                {
                                    Debug.Log($"[FixBriefingPanelLayoutPerfect] Fixing MonsterCard prefab at {mcPath}...");
                                    FixMonsterCardPrefabHierarchy(mcRoot.transform);
                                    PrefabUtility.SaveAsPrefabAsset(mcRoot, mcPath);
                                    PrefabUtility.UnloadPrefabContents(mcRoot);
                                    Debug.Log("[FixBriefingPanelLayoutPerfect] MonsterCard prefab successfully fixed!");
                                }
                            }
                        }
                    }

                    // Fix RewardItem Prefab
                    var rewardField = bp.GetType().GetField("_rewardPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (rewardField != null)
                    {
                        var rewardUi = rewardField.GetValue(bp) as MaouSamaTD.UI.MainMenu.RewardItemUI;
                        if (rewardUi != null)
                        {
                            string rewardPath = AssetDatabase.GetAssetPath(rewardUi.gameObject);
                            if (!string.IsNullOrEmpty(rewardPath))
                            {
                                GameObject rRoot = PrefabUtility.LoadPrefabContents(rewardPath);
                                if (rRoot != null)
                                {
                                    Debug.Log($"[FixBriefingPanelLayoutPerfect] Fixing RewardItem prefab at {rewardPath}...");
                                    FixRewardItemPrefabHierarchy(rRoot.transform);
                                    PrefabUtility.SaveAsPrefabAsset(rRoot, rewardPath);
                                    PrefabUtility.UnloadPrefabContents(rRoot);
                                    Debug.Log("[FixBriefingPanelLayoutPerfect] RewardItem prefab successfully fixed!");
                                }
                            }
                        }
                    }
                }
                PrefabUtility.UnloadPrefabContents(tempRoot);
            }

            // 2. Fix the Main Prefab
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot != null)
            {
                Debug.Log("[FixBriefingPanelLayoutPerfect] Fixing Prefab...");
                FixHierarchy(prefabRoot.transform, true);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                Debug.Log("[FixBriefingPanelLayoutPerfect] Prefab successfully fixed and saved!");
            }
            else
            {
                Debug.LogError($"[FixBriefingPanelLayoutPerfect] Failed to load prefab at {prefabPath}");
            }

            // 3. Fix the Instance in the Active Scene if it exists
            var bpInScene = Object.FindAnyObjectByType<MaouSamaTD.UI.MainMenu.BriefingPanel>();
            if (bpInScene != null)
            {
                Debug.Log($"[FixBriefingPanelLayoutPerfect] Fixing scene instance: {bpInScene.gameObject.name}");
                Undo.RegisterCompleteObjectUndo(bpInScene.gameObject, "Fix Briefing Panel Scene Instance");
                
                FixHierarchy(bpInScene.transform, false);
                
                // Clean up orphaned duplicate buttons at the root of BriefingPanel if they exist
                Transform extraEngage = bpInScene.transform.Find("Briefing_Engage_Buttx");
                if (extraEngage != null) Object.DestroyImmediate(extraEngage.gameObject);

                Transform extraPrev = bpInScene.transform.Find("PrevLevelButton");
                if (extraPrev != null) Object.DestroyImmediate(extraPrev.gameObject);

                Transform extraNext = bpInScene.transform.Find("NextLevelButton");
                if (extraNext != null) Object.DestroyImmediate(extraNext.gameObject);
                
                EditorUtility.SetDirty(bpInScene.gameObject);
                Debug.Log("[FixBriefingPanelLayoutPerfect] Scene instance successfully fixed!");
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static T EnsureLayoutGroup<T>(GameObject go) where T : LayoutGroup
        {
            T lg = go.GetComponent<T>();
            if (lg == null)
            {
                // Destroy any other layout group first
                var oldLg = go.GetComponent<LayoutGroup>();
                if (oldLg != null) Object.DestroyImmediate(oldLg, true);
                lg = go.AddComponent<T>();
            }
            return lg;
        }

        private static void FixHierarchy(Transform root, bool isPrefab)
        {
            Transform visualRoot = root.Find("VisualRoot");
            if (visualRoot == null)
            {
                Debug.LogError("[FixBriefingPanelLayoutPerfect] VisualRoot not found!");
                return;
            }

            // Locate the Scroll View, Viewport and Content first so we know where to parent Title & Separator
            Transform scrollView = visualRoot.Find("BriefingScrollView");
            if (scrollView == null)
            {
                Debug.LogError("[FixBriefingPanelLayoutPerfect] BriefingScrollView not found!");
                return;
            }
            Transform viewport = scrollView.Find("Viewport");
            if (viewport == null)
            {
                Debug.LogError("[FixBriefingPanelLayoutPerfect] Viewport not found!");
                return;
            }
            Transform content = viewport.Find("Content");
            if (content == null)
            {
                Debug.LogError("[FixBriefingPanelLayoutPerfect] Content not found!");
                return;
            }

            // Group all Hierarchy restructuring (SetParent calls) upfront to prevent sequential MissingReferenceException
            string[] scrollableElements = new string[] {
                "TextHolderTitle",
                "BriefingSeprator",
                "BriefingSeparator",
                "DescriptionText",
                "Header_Enemies",
                "Container_Enemies",
                "Header_OneTimeRewards",
                "Container_OneTimeRewards",
                "Header_ReplayRewards",
                "Container_ReplayRewards",
                "Header_StageDrops",
                "Container_StageDrops",
                "Header_Conditions",
                "Container_Conditions"
            };

            foreach (string name in scrollableElements)
            {
                Transform t = visualRoot.Find(name);
                if (t != null)
                {
                    t.SetParent(content, false);
                }
            }

            // Reparent bottom buttons to BottomButtonsGroup
            Transform bottomButtonsGroup = visualRoot.Find("BottomButtonsGroup");
            if (bottomButtonsGroup != null)
            {
                string[] buttonNames = new string[] {
                    "PrevLevelButton",
                    "Briefing_Engage_Button",
                    "Briefing_Engage_Buttx",
                    "NextLevelButton"
                };
                foreach (string bName in buttonNames)
                {
                    Transform btn = visualRoot.Find(bName);
                    if (btn != null)
                    {
                        btn.SetParent(bottomButtonsGroup, false);
                    }
                }
            }

            // Clean up legacy floaters
            Transform rContainer = visualRoot.Find("RewardsContainer");
            if (rContainer == null && content != null) rContainer = content.Find("RewardsContainer");
            if (rContainer != null) Object.DestroyImmediate(rContainer.gameObject);

            Transform rContainerGroup = visualRoot.Find("RewardsContainerGroup");
            if (rContainerGroup == null && content != null) rContainerGroup = content.Find("RewardsContainerGroup");
            if (rContainerGroup != null) Object.DestroyImmediate(rContainerGroup.gameObject);

            // =======================================================================
            // RE-LOOKUP ALL KEY TRANSFORMS FROM THE ROOT AFTER HIERARCHY MUTATIONS!
            // This is crucial to avoid MissingReferenceException in Prefab Stage!
            // =======================================================================
            visualRoot = root.Find("VisualRoot");
            scrollView = visualRoot.Find("BriefingScrollView");
            viewport = scrollView.Find("Viewport");
            content = viewport.Find("Content");
            bottomButtonsGroup = visualRoot.Find("BottomButtonsGroup");

            Transform titleHolder = content.Find("TextHolderTitle");
            Transform briefingSep = content.Find("BriefingSeprator");
            if (briefingSep == null) briefingSep = content.Find("BriefingSeparator");
            
            Transform closeBtn = visualRoot.Find("CloseButton");

            // VisualRoot Dimensions (480x800) & Glassmorphic Styling
            var visualRect = visualRoot.GetComponent<RectTransform>();
            if (visualRect != null)
            {
                visualRect.sizeDelta = new Vector2(480f, 800f);
            }

            var visualRootImg = visualRoot.GetComponent<Image>();
            if (visualRootImg != null)
            {
                visualRootImg.color = new Color(0.05f, 0.05f, 0.06f, 0.95f); // Deep dark glassmorphism #0c0c10
                var outline = visualRoot.GetComponent<Outline>();
                if (outline == null) outline = visualRoot.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.75f, 0.15f, 0.25f); // Subtle gold glow border #FFBF26
                outline.effectDistance = new Vector2(2f, 2f);
            }

            // Explicitly set VisualRoot's Vertical Layout Group to ensure consistency
            var visualVlg = EnsureLayoutGroup<VerticalLayoutGroup>(visualRoot.gameObject);
            visualVlg.spacing = 16f; // Spacing between ScrollView and BottomButtonsGroup
            visualVlg.padding = new RectOffset(32, 32, 32, 32); // Premium spacious side margins
            visualVlg.childAlignment = TextAnchor.UpperCenter;
            visualVlg.childControlWidth = true;
            visualVlg.childControlHeight = true;
            visualVlg.childForceExpandWidth = true;
            visualVlg.childForceExpandHeight = false;

            // TextHolderTitle layout spacing and category badge configurations
            if (titleHolder != null)
            {
                // Delete "Sov_Link_Text" that is a direct child of TextHolderTitle (Legacy label)
                for (int i = titleHolder.childCount - 1; i >= 0; i--)
                {
                    Transform child = titleHolder.GetChild(i);
                    if (child.name == "Sov_Link_Text" && child.parent == titleHolder)
                    {
                        Debug.Log($"[FixBriefingPanelLayoutPerfect] Destroying legacy direct child: {child.name}");
                        Object.DestroyImmediate(child.gameObject);
                    }
                }

                var titleVlg = EnsureLayoutGroup<VerticalLayoutGroup>(titleHolder.gameObject);
                titleVlg.spacing = 8f;
                titleVlg.childControlWidth = true;
                titleVlg.childControlHeight = true;
                titleVlg.childForceExpandWidth = true;
                titleVlg.childForceExpandHeight = false;
                titleVlg.padding = new RectOffset(0, 0, 0, 0);

                // Configure CategoryHeaderGroup
                Transform catGroup = titleHolder.Find("CategoryHeaderGroup");
                if (catGroup != null)
                {
                    var catHlg = EnsureLayoutGroup<HorizontalLayoutGroup>(catGroup.gameObject);
                    catHlg.spacing = 8f;
                    catHlg.childAlignment = TextAnchor.MiddleLeft;
                    catHlg.childControlWidth = false;
                    catHlg.childControlHeight = false;
                    catHlg.childForceExpandWidth = false;
                    catHlg.childForceExpandHeight = false;

                    Transform iconTrans = catGroup.Find("Icon");
                    if (iconTrans != null)
                    {
                        var iconRect = iconTrans.GetComponent<RectTransform>();
                        if (iconRect != null)
                        {
                            iconRect.sizeDelta = new Vector2(18f, 18f);
                        }
                        var iconLe = iconTrans.GetComponent<LayoutElement>();
                        if (iconLe == null) iconLe = iconTrans.gameObject.AddComponent<LayoutElement>();
                        iconLe.minWidth = 18f;
                        iconLe.preferredWidth = 18f;
                        iconLe.minHeight = 18f;
                        iconLe.preferredHeight = 18f;
                        iconLe.flexibleWidth = 0f;
                        iconLe.flexibleHeight = 0f;
                    }
                    
                    Transform textTrans = catGroup.Find("Sov_Link_Text");
                    if (textTrans == null) textTrans = catGroup.Find("Text");
                    if (textTrans != null)
                    {
                        var textTmp = textTrans.GetComponent<TextMeshProUGUI>();
                        if (textTmp != null)
                        {
                            textTmp.fontSize = 11f;
                            textTmp.fontStyle = FontStyles.Bold;
                            textTmp.characterSpacing = 2.0f; // Premium kerning
                        }
                    }
                }

                // Configure Briefing_Title_Text (Level Title)
                Transform titleTextTrans = titleHolder.Find("Briefing_Title_Text");
                if (titleTextTrans != null)
                {
                    var tmp = titleTextTrans.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        tmp.enableAutoSizing = false;
                        tmp.fontSize = 24f; // Matched HTML 24px
                        tmp.enableWordWrapping = true;
                        tmp.overflowMode = TextOverflowModes.Overflow;
                        tmp.fontStyle = FontStyles.Bold;
                    }
                }

                // Configure RecommendedLevelText
                Transform recLevelTextTrans = titleHolder.Find("RecommendedLevelText");
                if (recLevelTextTrans != null)
                {
                    var recTmp = recLevelTextTrans.GetComponent<TextMeshProUGUI>();
                    if (recTmp != null)
                    {
                        recTmp.fontSize = 14f;
                        recTmp.color = new Color(0.58f, 0.64f, 0.72f, 1f); // Outfit text-muted #94A3B8
                        recTmp.fontStyle = FontStyles.Normal;
                    }
                }
            }

            // BriefingSeparator configuration (Divider line)
            if (briefingSep != null)
            {
                var sepImg = briefingSep.GetComponent<Image>();
                if (sepImg != null)
                {
                    sepImg.color = new Color(1f, 0.75f, 0.15f, 0.4f); // #FFBF26 at 0.4 opacity
                }
                var sepRect = briefingSep.GetComponent<RectTransform>();
                if (sepRect != null)
                {
                    sepRect.sizeDelta = new Vector2(sepRect.sizeDelta.x, 1f); // 1px height
                }
                var sepLe = briefingSep.GetComponent<LayoutElement>();
                if (sepLe == null) sepLe = briefingSep.gameObject.AddComponent<LayoutElement>();
                sepLe.minHeight = 1f;
                sepLe.preferredHeight = 1f;
                sepLe.flexibleHeight = 0f;
            }

            // Configure BottomButtonsGroup with HorizontalLayoutGroup
            if (bottomButtonsGroup != null)
            {
                var hlg = EnsureLayoutGroup<HorizontalLayoutGroup>(bottomButtonsGroup.gameObject);
                hlg.spacing = 20f;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.padding = new RectOffset(0, 0, 10, 0); // Spacing from divider

                var rect = bottomButtonsGroup.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(0f, 60f);
                }

                var groupLe = bottomButtonsGroup.GetComponent<LayoutElement>();
                if (groupLe == null) groupLe = bottomButtonsGroup.gameObject.AddComponent<LayoutElement>();
                groupLe.minHeight = 60f;
                groupLe.preferredHeight = 60f;
                groupLe.flexibleHeight = 0f;

                // Previous Button
                Transform pBtn = bottomButtonsGroup.Find("PrevLevelButton");
                ConfigureButton(pBtn, 48f, 48f);
                if (pBtn != null)
                {
                    var pBtnImg = pBtn.GetComponent<Image>();
                    if (pBtnImg != null) pBtnImg.color = new Color(0.09f, 0.09f, 0.12f, 0.95f); // #18181E
                    var outline = pBtn.GetComponent<Outline>();
                    if (outline == null) outline = pBtn.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(1f, 0.75f, 0.15f, 0.35f); // #FFBF26 at 0.35 opacity
                    outline.effectDistance = new Vector2(1f, 1f);
                    
                    var icon = pBtn.Find("Icon")?.GetComponent<Image>();
                    if (icon != null) icon.color = new Color(1f, 0.75f, 0.15f, 1f); // Gold arrows
                }

                // Engage/Replay Button
                Transform eBtn = bottomButtonsGroup.Find("Briefing_Engage_Button");
                if (eBtn == null) eBtn = bottomButtonsGroup.Find("Briefing_Engage_Buttx");
                ConfigureButton(eBtn, 220f, 48f);
                if (eBtn != null)
                {
                    eBtn.name = "Briefing_Engage_Button";
                    var eBtnImg = eBtn.GetComponent<Image>();
                    if (eBtnImg != null) eBtnImg.color = new Color(0.85f, 0.47f, 0.02f, 1f); // #D97706 warm gold-orange
                    var outline = eBtn.GetComponent<Outline>();
                    if (outline == null) outline = eBtn.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(1f, 0.75f, 0.15f, 1f); // #FFBF26 border
                    outline.effectDistance = new Vector2(2f, 2f);

                    var tmp = eBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        tmp.fontStyle = FontStyles.Bold;
                        tmp.fontSize = 14f;
                        tmp.characterSpacing = 2.0f; // Premium kerning
                        tmp.color = Color.white;
                    }
                }

                // Next Button
                Transform nBtn = bottomButtonsGroup.Find("NextLevelButton");
                ConfigureButton(nBtn, 48f, 48f);
                if (nBtn != null)
                {
                    var nBtnImg = nBtn.GetComponent<Image>();
                    if (nBtnImg != null) nBtnImg.color = new Color(0.09f, 0.09f, 0.12f, 0.95f); // #18181E
                    var outline = nBtn.GetComponent<Outline>();
                    if (outline == null) outline = nBtn.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(1f, 0.75f, 0.15f, 0.35f); // #FFBF26 at 0.35 opacity
                    outline.effectDistance = new Vector2(1f, 1f);

                    var icon = nBtn.Find("Icon")?.GetComponent<Image>();
                    if (icon != null) icon.color = new Color(1f, 0.75f, 0.15f, 1f); // Gold arrows
                }
            }

            // Configure BriefingScrollView & Viewport & Content
            if (scrollView != null)
            {
                var scrollLe = scrollView.GetComponent<LayoutElement>();
                if (scrollLe == null) scrollLe = scrollView.gameObject.AddComponent<LayoutElement>();
                scrollLe.minHeight = 200f;
                scrollLe.preferredHeight = 500f; // Expanded preferred height
                scrollLe.flexibleHeight = 1f;

                if (viewport != null)
                {
                    var viewRect = viewport.GetComponent<RectTransform>();
                    if (viewRect != null)
                    {
                        viewRect.anchorMin = Vector2.zero;
                        viewRect.anchorMax = Vector2.one;
                        viewRect.sizeDelta = Vector2.zero;
                        viewRect.anchoredPosition = Vector2.zero;
                    }

                    // Replace standard stencil Mask with RectMask2D to prevent URP stencil/shader incompatibility (invisibility)
                    var oldMask = viewport.GetComponent<Mask>();
                    if (oldMask != null)
                    {
                        Object.DestroyImmediate(oldMask, true);
                    }
                    var viewportImg = viewport.GetComponent<Image>();
                    if (viewportImg != null)
                    {
                        viewportImg.color = new Color(0f, 0f, 0f, 0f); // Transparent raycast target
                    }
                    var rectMask = viewport.GetComponent<RectMask2D>();
                    if (rectMask == null)
                    {
                        rectMask = viewport.gameObject.AddComponent<RectMask2D>();
                    }

                    if (content != null)
                    {
                        var contentRect = content.GetComponent<RectTransform>();
                        if (contentRect != null)
                        {
                            contentRect.anchorMin = new Vector2(0f, 1f);
                            contentRect.anchorMax = new Vector2(1f, 1f);
                            contentRect.pivot = new Vector2(0.5f, 1f);
                            contentRect.anchoredPosition = Vector2.zero;
                            contentRect.sizeDelta = new Vector2(0f, contentRect.sizeDelta.y);
                        }

                        var contentVlg = EnsureLayoutGroup<VerticalLayoutGroup>(content.gameObject);
                        contentVlg.spacing = 24f; // Increased spacing for ultimate spacious breathing room
                        contentVlg.childControlWidth = true;
                        contentVlg.childControlHeight = true;
                        contentVlg.childForceExpandWidth = true;
                        contentVlg.childForceExpandHeight = false;
                        contentVlg.padding = new RectOffset(12, 12, 12, 12);

                        var contentCsf = content.GetComponent<ContentSizeFitter>();
                        if (contentCsf == null) contentCsf = content.gameObject.AddComponent<ContentSizeFitter>();
                        contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                        contentCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                        // ENFORCE EXACT ORDER OF CHILDREN IN CONTENT FOR SPACIOUS SCROLLING FLOW
                        int index = 0;
                        if (titleHolder != null) { titleHolder.SetSiblingIndex(index++); }
                        if (briefingSep != null) { briefingSep.SetSiblingIndex(index++); }
                        
                        Transform descTrans = content.Find("DescriptionText");
                        if (descTrans != null) { descTrans.SetSiblingIndex(index++); }

                        string[] sections = new string[] {
                            "Header_Enemies", "Container_Enemies",
                            "Header_OneTimeRewards", "Container_OneTimeRewards",
                            "Header_ReplayRewards", "Container_ReplayRewards",
                            "Header_StageDrops", "Container_StageDrops",
                            "Header_Conditions", "Container_Conditions"
                        };

                        foreach (var sectionName in sections)
                        {
                            Transform childTrans = content.Find(sectionName);
                            if (childTrans != null)
                            {
                                childTrans.SetSiblingIndex(index++);
                            }
                        }

                        // Configure DescriptionText
                        if (descTrans != null)
                        {
                            var descTmp = descTrans.GetComponent<TextMeshProUGUI>();
                            if (descTmp != null)
                            {
                                descTmp.fontSize = 15f; // Matched HTML 15px
                                descTmp.lineSpacing = 8f; // 1.5x height
                                descTmp.color = new Color(0.8f, 0.84f, 0.88f, 0.95f); // #CBD5E1 Outfit text
                                descTmp.fontStyle = FontStyles.Italic;
                            }
                        }

                        // Configure sub-containers and headers to match HTML (without brackets, with horizontal line next to it)
                        ConfigureSectionHeader(content.Find("Header_Enemies"), "EXPECTED HOSTILE FORCES");
                        ConfigureSectionHeader(content.Find("Header_OneTimeRewards"), "STAR OBJECTIVES & BONUS");
                        ConfigureSectionHeader(content.Find("Header_ReplayRewards"), "REPLAY VICTORY REWARDS");
                        ConfigureSectionHeader(content.Find("Header_StageDrops"), "STAGE DROPS");
                        ConfigureSectionHeader(content.Find("Header_Conditions"), "MISSION CONDITIONS");

                        // 1. Expected Hostile Forces (Container_Enemies) - spacing of 16f to avoid card overlapping on hover
                        Transform containerEnemies = content.Find("Container_Enemies");
                        if (containerEnemies != null)
                        {
                            var vlg = EnsureLayoutGroup<VerticalLayoutGroup>(containerEnemies.gameObject);
                            vlg.spacing = 16f; // Spacious spacing
                            vlg.padding = new RectOffset(12, 12, 12, 12); // Premium padding
                            vlg.childControlWidth = true;
                            vlg.childControlHeight = false;
                            vlg.childForceExpandWidth = true;
                            vlg.childForceExpandHeight = false;
                            
                            var csf = containerEnemies.GetComponent<ContentSizeFitter>();
                            if (csf == null) csf = containerEnemies.gameObject.AddComponent<ContentSizeFitter>();
                            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                        }

                        // 2. Star Objectives & Bonus (Container_OneTimeRewards)
                        Transform containerOneTime = content.Find("Container_OneTimeRewards");
                        if (containerOneTime != null)
                        {
                            var vlg = EnsureLayoutGroup<VerticalLayoutGroup>(containerOneTime.gameObject);
                            vlg.spacing = 16f; // Spacious spacing
                            vlg.padding = new RectOffset(12, 12, 12, 12); // Premium padding
                            vlg.childControlWidth = true;
                            vlg.childControlHeight = false;
                            vlg.childForceExpandWidth = true;
                            vlg.childForceExpandHeight = false;

                            var csf = containerOneTime.GetComponent<ContentSizeFitter>();
                            if (csf == null) csf = containerOneTime.gameObject.AddComponent<ContentSizeFitter>();
                            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                        }

                        // 3. Replay Victory Rewards (Container_ReplayRewards)
                        Transform containerReplay = content.Find("Container_ReplayRewards");
                        if (containerReplay != null)
                        {
                            var hlg = EnsureLayoutGroup<HorizontalLayoutGroup>(containerReplay.gameObject);
                            hlg.spacing = 12f; // Spacious spacing
                            hlg.padding = new RectOffset(12, 12, 12, 12); // Premium padding
                            hlg.childControlWidth = false;
                            hlg.childControlHeight = false;
                            hlg.childForceExpandWidth = false;
                            hlg.childForceExpandHeight = false;
                            hlg.childAlignment = TextAnchor.MiddleLeft;

                            var csf = containerReplay.GetComponent<ContentSizeFitter>();
                            if (csf == null) csf = containerReplay.gameObject.AddComponent<ContentSizeFitter>();
                            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                        }

                        // 4. Stage Drops (Container_StageDrops)
                        Transform containerDrops = content.Find("Container_StageDrops");
                        if (containerDrops != null)
                        {
                            var hlg = EnsureLayoutGroup<HorizontalLayoutGroup>(containerDrops.gameObject);
                            hlg.spacing = 12f; // Spacious spacing
                            hlg.padding = new RectOffset(12, 12, 12, 12); // Premium padding
                            hlg.childControlWidth = false;
                            hlg.childControlHeight = false;
                            hlg.childForceExpandWidth = false;
                            hlg.childForceExpandHeight = false;
                            hlg.childAlignment = TextAnchor.MiddleLeft;

                            var csf = containerDrops.GetComponent<ContentSizeFitter>();
                            if (csf == null) csf = containerDrops.gameObject.AddComponent<ContentSizeFitter>();
                            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                        }

                        // 5. Mission Conditions (Container_Conditions)
                        Transform containerConditions = content.Find("Container_Conditions");
                        if (containerConditions != null)
                        {
                            var vlg = EnsureLayoutGroup<VerticalLayoutGroup>(containerConditions.gameObject);
                            vlg.spacing = 12f; // Spacious spacing
                            vlg.padding = new RectOffset(16, 16, 16, 16); // Highly premium padding
                            vlg.childControlWidth = true;
                            vlg.childControlHeight = false;
                            vlg.childForceExpandWidth = true;
                            vlg.childForceExpandHeight = false;

                            var csf = containerConditions.GetComponent<ContentSizeFitter>();
                            if (csf == null) csf = containerConditions.gameObject.AddComponent<ContentSizeFitter>();
                            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                            var condImg = containerConditions.GetComponent<Image>();
                            if (condImg == null) condImg = containerConditions.gameObject.AddComponent<Image>();
                            condImg.color = new Color(0.94f, 0.27f, 0.27f, 0.03f); // Red glassmorphism rgba(239, 68, 68, 0.03)
                            
                            var outline = containerConditions.GetComponent<Outline>();
                            if (outline == null) outline = containerConditions.gameObject.AddComponent<Outline>();
                            outline.effectColor = new Color(0.94f, 0.27f, 0.27f, 0.12f); // Red border outline rgba(239, 68, 68, 0.12)
                            outline.effectDistance = new Vector2(1f, 1f);

                            Transform winCondTrans = containerConditions.Find("Win_Condition_Text");
                            if (winCondTrans != null)
                            {
                                var winTmp = winCondTrans.GetComponent<TextMeshProUGUI>();
                                if (winTmp != null)
                                {
                                    winTmp.fontSize = 14.5f;
                                    winTmp.color = Color.white;
                                    winTmp.fontStyle = FontStyles.Normal;
                                    winTmp.text = "<color=#10B981>● <b>WIN:</b></color> Defeat 3 Waves of Enemies";
                                }
                            }
                            Transform loseCondTrans = containerConditions.Find("Lose_Condition_Text");
                            if (loseCondTrans != null)
                            {
                                var loseTmp = loseCondTrans.GetComponent<TextMeshProUGUI>();
                                if (loseTmp != null)
                                {
                                    loseTmp.fontSize = 14.5f;
                                    loseTmp.color = Color.white;
                                    loseTmp.fontStyle = FontStyles.Normal;
                                    loseTmp.text = "<color=#EF4444>● <b>LOSE:</b></color> Your core health reaches 0";
                                }
                            }
                        }
                    }
                }
            }

            // Ensure CloseButton ignores layout and sits cleanly in top-right corner
            closeBtn = visualRoot.Find("CloseButton");
            if (closeBtn != null)
            {
                var le = closeBtn.GetComponent<LayoutElement>();
                if (le == null) le = closeBtn.gameObject.AddComponent<LayoutElement>();
                le.ignoreLayout = true;

                var rect = closeBtn.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(1f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 1f);
                    rect.anchoredPosition = new Vector2(-20f, -20f); // Spaced from corner
                    rect.sizeDelta = new Vector2(32f, 32f);
                }

                var closeImg = closeBtn.GetComponent<Image>();
                if (closeImg != null)
                {
                    closeImg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f); // #14141A
                    var outline = closeBtn.GetComponent<Outline>();
                    if (outline == null) outline = closeBtn.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(1f, 1f, 1f, 0.25f);
                    outline.effectDistance = new Vector2(1f, 1f);
                }

                // Ensure a centered premium mathematical cross "✕" text is configured inside the Close Button
                Transform closeTextTrans = closeBtn.Find("Text");
                if (closeTextTrans == null) closeTextTrans = closeBtn.Find("Text (TMP)");
                if (closeTextTrans == null)
                {
                    var childTmp = closeBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (childTmp != null) closeTextTrans = childTmp.transform;
                }
                if (closeTextTrans != null)
                {
                    try
                    {
                        var nameTest = closeTextTrans.gameObject.name;
                    }
                    catch
                    {
                        closeTextTrans = null;
                    }
                }
                if (closeTextTrans == null)
                {
                    GameObject textGo = new GameObject("Text (TMP)");
                    var targetScene = closeBtn.gameObject.scene;
                    if (targetScene.IsValid() && targetScene.isLoaded)
                    {
                        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(textGo, targetScene);
                    }
                    closeTextTrans = textGo.transform;
                    closeTextTrans.SetParent(closeBtn, false);
                }

                var closeTmp = closeTextTrans.GetComponent<TextMeshProUGUI>();
                if (closeTmp == null)
                {
                    closeTmp = closeTextTrans.gameObject.AddComponent<TextMeshProUGUI>();
                    closeTextTrans = closeBtn.Find("Text (TMP)");
                }
                closeTmp.text = "✕"; // Premium mathematical cross
                closeTmp.fontSize = 14f;
                closeTmp.fontStyle = FontStyles.Bold;
                closeTmp.alignment = TextAlignmentOptions.Center;
                closeTmp.color = Color.white;

                var textRect = closeTextTrans.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                    textRect.anchoredPosition = Vector2.zero;
                }
            }
        }

        private static void FixMonsterCardPrefabHierarchy(Transform root)
        {
            // Root card layout & styling
            var cardImg = root.GetComponent<Image>();
            if (cardImg != null)
            {
                cardImg.color = new Color(0.08f, 0.08f, 0.1f, 0.8f); // rgba(20, 20, 26, 0.8)
                var outline = root.GetComponent<Outline>();
                if (outline == null) outline = root.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.75f, 0.15f, 0.12f); // rgba(255, 191, 38, 0.12)
                outline.effectDistance = new Vector2(1f, 1f);
            }

            var hover = root.GetComponent<UIHoverEffect>();
            if (hover == null) hover = root.gameObject.AddComponent<UIHoverEffect>();
            if (hover != null)
            {
                hover.Configure(
                    new Color(0.08f, 0.08f, 0.1f, 0.8f),      // Normal BG
                    new Color(0.09f, 0.09f, 0.13f, 0.95f),    // Hover BG
                    new Color(1f, 0.75f, 0.15f, 0.12f),       // Normal Outline
                    new Color(1f, 0.75f, 0.15f, 0.3f),        // Hover Outline
                    1.02f
                );
            }

            var hlg = EnsureLayoutGroup<HorizontalLayoutGroup>(root.gameObject);
            hlg.spacing = 12f;
            hlg.padding = new RectOffset(12, 12, 12, 12);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var le = root.GetComponent<LayoutElement>();
            if (le == null) le = root.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 76f;
            le.preferredHeight = 76f;
            le.flexibleHeight = 0f;

            // ChibiFrame
            Transform frame = root.Find("ChibiFrame");
            if (frame != null)
            {
                var frameImg = frame.GetComponent<Image>();
                if (frameImg != null)
                {
                    frameImg.color = new Color(0.18f, 0.15f, 0.12f, 1f); // radial dark bronze simulation
                    var outline = frame.GetComponent<Outline>();
                    if (outline == null) outline = frame.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(1f, 0.75f, 0.15f, 0.4f); // gold outline
                    outline.effectDistance = new Vector2(1f, 1f);
                }
                var frameRect = frame.GetComponent<RectTransform>();
                if (frameRect != null)
                {
                    frameRect.sizeDelta = new Vector2(48f, 48f);
                }
                var frameLe = frame.GetComponent<LayoutElement>();
                if (frameLe == null) frameLe = frame.gameObject.AddComponent<LayoutElement>();
                frameLe.minWidth = 48f;
                frameLe.preferredWidth = 48f;
                frameLe.minHeight = 48f;
                frameLe.preferredHeight = 48f;
                frameLe.flexibleWidth = 0f;
                frameLe.flexibleHeight = 0f;

                Transform chibi = frame.Find("Chibi");
                if (chibi != null)
                {
                    var chibiRect = chibi.GetComponent<RectTransform>();
                    if (chibiRect != null)
                    {
                        chibiRect.anchorMin = new Vector2(0.5f, 0.5f);
                        chibiRect.anchorMax = new Vector2(0.5f, 0.5f);
                        chibiRect.pivot = new Vector2(0.5f, 0.5f);
                        chibiRect.anchoredPosition = Vector2.zero;
                        chibiRect.sizeDelta = new Vector2(36f, 36f);
                    }
                }
            }

            // InfoContainer
            Transform info = root.Find("InfoContainer");
            if (info != null)
            {
                var vlg = EnsureLayoutGroup<VerticalLayoutGroup>(info.gameObject);
                vlg.spacing = 4f;
                vlg.padding = new RectOffset(0, 0, 0, 0);
                vlg.childAlignment = TextAnchor.MiddleLeft;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                var infoLe = info.GetComponent<LayoutElement>();
                if (infoLe == null) infoLe = info.gameObject.AddComponent<LayoutElement>();
                infoLe.flexibleWidth = 1f;

                Transform titleLine = info.Find("TitleLine");
                if (titleLine != null)
                {
                    var titleHlg = EnsureLayoutGroup<HorizontalLayoutGroup>(titleLine.gameObject);
                    titleHlg.spacing = 8f;
                    titleHlg.childAlignment = TextAnchor.MiddleLeft;
                    titleHlg.childControlWidth = true;
                    titleHlg.childControlHeight = true;
                    titleHlg.childForceExpandWidth = false;
                    titleHlg.childForceExpandHeight = false;

                    Transform nameText = titleLine.Find("NameText");
                    if (nameText != null)
                    {
                        var nameTmp = nameText.GetComponent<TextMeshProUGUI>();
                        if (nameTmp != null)
                        {
                            nameTmp.fontSize = 16f;
                            nameTmp.fontStyle = FontStyles.Bold;
                            nameTmp.color = Color.white;
                        }
                    }

                    Transform rankBadge = titleLine.Find("RankBadge");
                    var rankTmp = RestructureBadge(rankBadge, "RankBadge");

                    Transform moveBadge = titleLine.Find("MoveBadge");
                    var moveTmp = RestructureBadge(moveBadge, "MoveBadge");

                    // Re-bind the serialized properties on MonsterCardUI
                    var ui = root.GetComponent<MonsterCardUI>();
                    if (ui != null)
                    {
                        var serializedObject = new SerializedObject(ui);
                        if (rankTmp != null)
                        {
                            serializedObject.FindProperty("_rankBadgeText").objectReferenceValue = rankTmp;
                        }
                        if (moveTmp != null)
                        {
                            serializedObject.FindProperty("_moveBadgeText").objectReferenceValue = moveTmp;
                        }
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                Transform statsText = info.Find("StatsText");
                if (statsText != null)
                {
                    var statsTmp = statsText.GetComponent<TextMeshProUGUI>();
                    if (statsTmp != null)
                    {
                        statsTmp.fontSize = 13.5f;
                        statsTmp.color = new Color(0.58f, 0.64f, 0.72f, 1f); // #94A3B8 Outfit text-muted
                    }
                }
            }
        }

        private static void FixRewardItemPrefabHierarchy(Transform root)
        {
            var cardImg = root.GetComponent<Image>();
            if (cardImg != null)
            {
                cardImg.color = new Color(0.12f, 0.12f, 0.15f, 0.85f); // rgba(30, 30, 38, 0.85)
                var outline = root.GetComponent<Outline>();
                if (outline == null) outline = root.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.75f, 0.15f, 0.2f); // rgba(255, 191, 38, 0.2)
                outline.effectDistance = new Vector2(1f, 1f);
            }

            var hover = root.GetComponent<UIHoverEffect>();
            if (hover == null) hover = root.gameObject.AddComponent<UIHoverEffect>();
            if (hover != null)
            {
                hover.Configure(
                    new Color(0.12f, 0.12f, 0.15f, 0.85f),     // Normal BG
                    new Color(0.14f, 0.14f, 0.19f, 0.95f),     // Hover BG
                    new Color(1f, 0.75f, 0.15f, 0.2f),         // Normal Outline
                    new Color(1f, 0.75f, 0.15f, 0.6f),         // Hover Outline
                    1.03f
                );
            }

            var hlg = EnsureLayoutGroup<HorizontalLayoutGroup>(root.gameObject);
            hlg.spacing = 8f;
            hlg.padding = new RectOffset(12, 12, 8, 8);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            Transform icon = root.Find("Icon");
            if (icon != null)
            {
                var iconRect = icon.GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    iconRect.sizeDelta = new Vector2(24f, 24f);
                }
                var iconLe = icon.GetComponent<LayoutElement>();
                if (iconLe == null) iconLe = icon.gameObject.AddComponent<LayoutElement>();
                iconLe.minWidth = 24f;
                iconLe.preferredWidth = 24f;
                iconLe.minHeight = 24f;
                iconLe.preferredHeight = 24f;
                iconLe.flexibleWidth = 0f;
                iconLe.flexibleHeight = 0f;

                var iconImg = icon.GetComponent<Image>();
                if (iconImg != null)
                {
                    iconImg.preserveAspect = true;
                }
            }

            Transform quantity = root.Find("Quantity");
            if (quantity != null)
            {
                var qtyTmp = quantity.GetComponent<TextMeshProUGUI>();
                if (qtyTmp != null)
                {
                    qtyTmp.fontSize = 11.5f;
                    qtyTmp.fontStyle = FontStyles.Bold;
                    qtyTmp.color = Color.white;
                }
            }
        }

        private static void ConfigureSectionHeader(Transform headerTrans, string defaultText)
        {
            if (headerTrans == null) return;
            
            // 1. Restructure: Check if Text component is directly on headerTrans.
            // If so, move it to a child GameObject named "Text" so the parent HorizontalLayoutGroup works correctly.
            Transform childTextTrans = headerTrans.Find("Text");
            TextMeshProUGUI childTmp = null;

            if (childTextTrans != null)
            {
                childTmp = childTextTrans.GetComponent<TextMeshProUGUI>();
            }

            if (childTmp == null)
            {
                var oldTmp = headerTrans.GetComponent<TextMeshProUGUI>();
                string oldText = oldTmp != null ? oldTmp.text : defaultText;
                float oldFontSize = oldTmp != null ? oldTmp.fontSize : 14f;

                GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(headerTrans, false);
                childTmp = textGo.GetComponent<TextMeshProUGUI>();

                childTmp.text = oldText;
                childTmp.fontSize = oldFontSize;
                childTmp.fontStyle = FontStyles.Bold;
                childTmp.alignment = TextAlignmentOptions.Left;

                if (oldTmp != null)
                {
                    Object.DestroyImmediate(oldTmp, true);
                }
            }

            // Clean up legacy brackets from text if they exist
            if (childTmp != null)
            {
                string text = childTmp.text;
                if (text.StartsWith("[") && text.EndsWith("]"))
                {
                    childTmp.text = text.Substring(1, text.Length - 2).Trim();
                }
                childTmp.fontSize = 17f; // Set a nice premium larger size!
                childTmp.fontStyle = FontStyles.Bold;
                childTmp.characterSpacing = 1.5f;
                childTmp.color = new Color(1f, 0.75f, 0.15f, 1f); // Gold #FFBF26
                childTmp.alignment = TextAlignmentOptions.Left;
                childTmp.enableWordWrapping = false;

                var textLe = childTmp.GetComponent<LayoutElement>();
                if (textLe == null) textLe = childTmp.gameObject.AddComponent<LayoutElement>();
                textLe.flexibleWidth = 0f;
            }

            // 2. Ensure HorizontalLayoutGroup for the header container so the line sits next to the text
            var hlg = EnsureLayoutGroup<HorizontalLayoutGroup>(headerTrans.gameObject);
            hlg.spacing = 10f; // Premium spacious gap between text and line
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 3. Ensure a horizontal line image exists next to the text (mimicking HTML CSS ::after)
            Transform lineTrans = headerTrans.Find("HeaderLine");
            if (lineTrans == null)
            {
                GameObject lineGo = new GameObject("HeaderLine");
                var targetScene = headerTrans.gameObject.scene;
                if (targetScene.IsValid() && targetScene.isLoaded)
                {
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lineGo, targetScene);
                }
                lineTrans = lineGo.transform;
                lineTrans.SetParent(headerTrans, false);
            }

            // Re-find in case parenting triggered a rebuild
            lineTrans = headerTrans.Find("HeaderLine");
            if (lineTrans != null)
            {
                var lineImg = lineTrans.GetComponent<Image>();
                if (lineImg == null) lineImg = lineTrans.gameObject.AddComponent<Image>();
                
                // Solid gold line with visible 0.35f opacity next to the text!
                lineImg.color = new Color(1f, 0.75f, 0.15f, 0.35f); 

                var lineLe = lineTrans.GetComponent<LayoutElement>();
                if (lineLe == null) lineLe = lineTrans.gameObject.AddComponent<LayoutElement>();
                
                lineLe.minHeight = 2f; // Make the line thicker and premium (2px)
                lineLe.preferredHeight = 2f;
                lineLe.flexibleWidth = 1f; // Stretches to fill the rest of the row!
                lineLe.flexibleHeight = 0f;
            }

            // Enforce correct sibling index ordering: Text on the left, HeaderLine on the right
            if (childTmp != null)
            {
                childTmp.transform.SetSiblingIndex(0);
            }
            if (lineTrans != null)
            {
                lineTrans.SetSiblingIndex(1);
            }
        }

        private static void ConfigureButton(Transform btnTrans, float width, float height)
        {
            if (btnTrans == null) return;
            
            var rect = btnTrans.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(width, height);
            }

            var le = btnTrans.GetComponent<LayoutElement>();
            if (le == null) le = btnTrans.gameObject.AddComponent<LayoutElement>();
            le.minWidth = width;
            le.preferredWidth = width;
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;
            le.ignoreLayout = false;
        }

        private static TextMeshProUGUI RestructureBadge(Transform badgeTrans, string badgeName)
        {
            if (badgeTrans == null) return null;

            // 1. Check if it already has the restructure (a child named "Text")
            Transform childTextTrans = badgeTrans.Find("Text");
            TextMeshProUGUI childTmp = null;

            if (childTextTrans != null)
            {
                childTmp = childTextTrans.GetComponent<TextMeshProUGUI>();
            }

            // If not restructured yet, let's do it!
            if (childTmp == null)
            {
                // A. Get existing TMPro component
                var oldTmp = badgeTrans.GetComponent<TextMeshProUGUI>();
                string oldText = oldTmp != null ? oldTmp.text : "";
                float oldFontSize = oldTmp != null ? oldTmp.fontSize : 11.5f;
                if (oldFontSize < 11.5f) oldFontSize = 11.5f;

                // B. Create the child text GameObject
                GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(badgeTrans, false);
                childTmp = textGo.GetComponent<TextMeshProUGUI>();

                // C. Copy settings
                childTmp.text = oldText;
                childTmp.fontSize = oldFontSize;
                childTmp.fontStyle = FontStyles.Bold;
                childTmp.alignment = TextAlignmentOptions.Center;
                childTmp.enableWordWrapping = false;

                // D. Destroy the old TMPro component on the parent
                if (oldTmp != null)
                {
                    Object.DestroyImmediate(oldTmp, true);
                }
            }

            // 2. Setup the parent badge container (Image, Outline, HorizontalLayoutGroup, LayoutElement)
            var img = badgeTrans.GetComponent<Image>();
            if (img == null) img = badgeTrans.gameObject.AddComponent<Image>();
            
            // Set simple default color (MonsterCardUI will override this anyway)
            img.color = new Color(0.73f, 0.73f, 0.73f, 0.1f);

            var outline = badgeTrans.GetComponent<Outline>();
            if (outline == null) outline = badgeTrans.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.73f, 0.73f, 0.73f, 0.2f);
            outline.effectDistance = new Vector2(1f, 1f);

            var hlg = EnsureLayoutGroup<HorizontalLayoutGroup>(badgeTrans.gameObject);
            hlg.spacing = 0f;
            hlg.padding = new RectOffset(6, 6, 2, 2);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var le = badgeTrans.GetComponent<LayoutElement>();
            if (le == null) le = badgeTrans.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            return childTmp;
        }

        private static T AddComponentToChild<T>(Transform parent, string childName, ref Transform childTrans) where T : Component
        {
            if (childTrans == null) return null;
            T component = childTrans.GetComponent<T>();
            if (component == null)
            {
                component = childTrans.gameObject.AddComponent<T>();
                // Retrieving a fresh reference to childTrans from parent is mandatory in Prefab Stage
                // because adding components dynamically triggers a rebuilding of stage components,
                // which invalidates the original transform reference.
                childTrans = parent.Find(childName);
            }
            return component;
        }
    }
}
