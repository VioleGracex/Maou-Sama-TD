#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using TMPro;
using System.Reflection;
using MaouSamaTD.UI.Cohorts;

namespace MaouSamaTD.Editor
{
    public static class RebuildRitesLayout
    {
        private static Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                Transform found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        [MenuItem("Maou-TD/UI/Rebuild Rites Layout")]
        public static void Rebuild()
        {
            string scenePath = "Assets/_Game/Scenes/Home_New.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("Failed to open scene: " + scenePath);
                return;
            }

            var cohortUI = Object.FindAnyObjectByType<CohortSquadUI>();
            if (cohortUI == null)
            {
                Debug.LogError("CohortSquadUI not found in scene!");
                return;
            }

            // Get private fields of CohortSquadUI via reflection
            var squadType = typeof(CohortSquadUI);
            var vassalsPanelField = squadType.GetField("_vassalsPanel", BindingFlags.NonPublic | BindingFlags.Instance);
            var ritesPanelField = squadType.GetField("_ritesPanel", BindingFlags.NonPublic | BindingFlags.Instance);
            var containerField = squadType.GetField("_availableRitesContainer", BindingFlags.NonPublic | BindingFlags.Instance);
            var prefabField = squadType.GetField("_riteItemPrefab", BindingFlags.NonPublic | BindingFlags.Instance);

            var vassalsPanel = vassalsPanelField.GetValue(cohortUI) as GameObject;
            var ritesPanel = ritesPanelField.GetValue(cohortUI) as GameObject;
            var availableRitesContainer = containerField.GetValue(cohortUI) as RectTransform;
            var riteItemPrefab = prefabField.GetValue(cohortUI) as GameObject;

            if (ritesPanel == null || availableRitesContainer == null || riteItemPrefab == null)
            {
                Debug.LogError("Required references on CohortSquadUI are null!");
                return;
            }

            Debug.Log("Starting Sovereign Rites UI Rebuild...");

            GameObject centerSlotsGo = vassalsPanel;
            GameObject centerRitesGo = null;
            Transform cohortPageUI = ritesPanel.transform.parent;

            // Idempotent Restructuring
            if (ritesPanel.name == "Rites_Panel")
            {
                centerSlotsGo = ritesPanel.transform.parent.gameObject;
                centerSlotsGo.name = "Center_Slots";
                cohortPageUI = centerSlotsGo.transform.parent;

                Transform centerRitesTrans = cohortPageUI.Find("Center_Rites");
                if (centerRitesTrans == null)
                {
                    centerRitesGo = new GameObject("Center_Rites", typeof(RectTransform));
                    centerRitesGo.transform.SetParent(cohortPageUI, false);
                    centerRitesTrans = centerRitesGo.transform;
                }
                else
                {
                    centerRitesGo = centerRitesTrans.gameObject;
                }

                // Align Sibling Index
                centerRitesTrans.SetSiblingIndex(centerSlotsGo.transform.GetSiblingIndex() + 1);

                // Move children from Rites_Panel to Center_Rites
                Transform tempSlots1 = ritesPanel.transform.Find("RiteSlots_Container");
                Transform tempBlocker1 = ritesPanel.transform.Find("RitesNoEditBlocker");

                if (tempSlots1 != null) tempSlots1.SetParent(centerRitesTrans, false);
                if (availableRitesContainer != null) availableRitesContainer.SetParent(centerRitesTrans, false);
                if (tempBlocker1 != null) tempBlocker1.SetParent(centerRitesTrans, false);

                // Destroy old Rites_Panel
                Object.DestroyImmediate(ritesPanel);
                ritesPanel = centerRitesGo;
            }
            else if (ritesPanel.name == "Center_Rites")
            {
                centerRitesGo = ritesPanel;
                Transform centerSlotsTrans = cohortPageUI.Find("Center_Slots");
                if (centerSlotsTrans == null) centerSlotsTrans = cohortPageUI.Find("Center");
                if (centerSlotsTrans != null)
                {
                    centerSlotsGo = centerSlotsTrans.gameObject;
                    centerSlotsGo.name = "Center_Slots";
                }
            }
            else
            {
                // Fallback / First time setup if fields were not bound yet
                Transform centerTrans = cohortPageUI.Find("Center");
                if (centerTrans == null) centerTrans = FindChildRecursive(cohortPageUI, "Center");
                if (centerTrans != null)
                {
                    centerSlotsGo = centerTrans.gameObject;
                    centerSlotsGo.name = "Center_Slots";
                    
                    centerRitesGo = new GameObject("Center_Rites", typeof(RectTransform));
                    centerRitesGo.transform.SetParent(cohortPageUI, false);
                    centerRitesGo.transform.SetSiblingIndex(centerSlotsGo.transform.GetSiblingIndex() + 1);
                    
                    Transform tempSlots2 = centerSlotsGo.transform.Find("RiteSlots_Container");
                    Transform tempBlocker2 = centerSlotsGo.transform.Find("RitesNoEditBlocker");
                    Transform tempAvailable2 = centerSlotsGo.transform.Find("AvailableRites_Container");

                    if (tempSlots2 != null) tempSlots2.SetParent(centerRitesGo.transform, false);
                    if (tempAvailable2 != null) tempAvailable2.SetParent(centerRitesGo.transform, false);
                    if (tempBlocker2 != null) tempBlocker2.SetParent(centerRitesGo.transform, false);

                    ritesPanel = centerRitesGo;
                    availableRitesContainer = tempAvailable2 as RectTransform;
                }
            }

            if (centerSlotsGo == null || centerRitesGo == null)
            {
                Debug.LogError("Failed to resolve Center_Slots or Center_Rites!");
                return;
            }

            // 1. Synchronize Center_Rites RectTransform with Center_Slots
            var centerSlotsRt = centerSlotsGo.GetComponent<RectTransform>();
            var centerRitesRt = centerRitesGo.GetComponent<RectTransform>();
            centerRitesRt.anchorMin = centerSlotsRt.anchorMin;
            centerRitesRt.anchorMax = centerSlotsRt.anchorMax;
            centerRitesRt.pivot = centerSlotsRt.pivot;
            centerRitesRt.sizeDelta = centerSlotsRt.sizeDelta;
            centerRitesRt.anchoredPosition = centerSlotsRt.anchoredPosition;

            // 2. Configure HorizontalLayoutGroup on Center_Rites
            var ritesHlg = centerRitesGo.GetComponent<HorizontalLayoutGroup>();
            if (ritesHlg == null) ritesHlg = centerRitesGo.AddComponent<HorizontalLayoutGroup>();
            ritesHlg.padding = new RectOffset(60, 60, 40, 40);
            ritesHlg.spacing = 40;
            ritesHlg.childAlignment = TextAnchor.MiddleCenter;
            ritesHlg.childControlWidth = true;
            ritesHlg.childControlHeight = true;
            ritesHlg.childForceExpandWidth = false;
            ritesHlg.childForceExpandHeight = true;

            // 3. Configure Children under Center_Rites
            Transform riteSlotsContainer = centerRitesGo.transform.Find("RiteSlots_Container");
            Transform blockerTrans = centerRitesGo.transform.Find("RitesNoEditBlocker");

            if (riteSlotsContainer != null)
            {
                var slotsRt = riteSlotsContainer.GetComponent<RectTransform>();
                slotsRt.sizeDelta = new Vector2(500f, 750f);

                var slotsLe = riteSlotsContainer.GetComponent<LayoutElement>();
                if (slotsLe == null) slotsLe = riteSlotsContainer.gameObject.AddComponent<LayoutElement>();
                slotsLe.preferredWidth = 500f;
                slotsLe.flexibleWidth = 0f;
            }

            if (availableRitesContainer != null)
            {
                availableRitesContainer.sizeDelta = new Vector2(1100f, 750f);

                var availableLe = availableRitesContainer.GetComponent<LayoutElement>();
                if (availableLe == null) availableLe = availableRitesContainer.gameObject.AddComponent<LayoutElement>();
                availableLe.preferredWidth = 1100f;
                availableLe.flexibleWidth = 1f;
            }

            if (blockerTrans != null)
            {
                var blockerLe = blockerTrans.GetComponent<LayoutElement>();
                if (blockerLe == null) blockerLe = blockerTrans.gameObject.AddComponent<LayoutElement>();
                blockerLe.ignoreLayout = true;

                var blockerRt = blockerTrans.GetComponent<RectTransform>();
                blockerRt.anchorMin = new Vector2(0f, 0f);
                blockerRt.anchorMax = new Vector2(1f, 1f);
                blockerRt.offsetMin = Vector2.zero;
                blockerRt.offsetMax = Vector2.zero;
            }

            // 4. Configure ScrollRect and Viewport/Content
            Transform scrollRect = availableRitesContainer.Find("ScrollRect");
            if (scrollRect == null) scrollRect = availableRitesContainer;
            
            var scrollRectComp = scrollRect.GetComponent<ScrollRect>();
            if (scrollRectComp != null)
            {
                scrollRectComp.horizontal = false;
                scrollRectComp.vertical = true;
            }

            Transform content = scrollRect.Find("Viewport/Content");
            if (content == null) content = scrollRect.Find("Content");

            if (content != null)
            {
                var grid = content.GetComponent<GridLayoutGroup>();
                if (grid != null) Object.DestroyImmediate(grid);

                var vlg = content.GetComponent<VerticalLayoutGroup>();
                if (vlg == null) vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();

                vlg.padding = new RectOffset(10, 10, 10, 10);
                vlg.spacing = 15;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                var csf = content.GetComponent<ContentSizeFitter>();
                if (csf == null) csf = content.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            // 5. Redesign RiteItem_Prefab card (COMMENTED OUT TO PREVENT BREAKING PREFAB)
            /*
            Undo.RecordObject(riteItemPrefab, "Rebuild RiteItem Prefab");
            var itemRt = riteItemPrefab.GetComponent<RectTransform>();
            itemRt.sizeDelta = new Vector2(1000f, 120f);

            var itemVlg = riteItemPrefab.GetComponent<VerticalLayoutGroup>();
            if (itemVlg == null) itemVlg = riteItemPrefab.AddComponent<VerticalLayoutGroup>();
            itemVlg.spacing = 0;
            itemVlg.childControlWidth = true;
            itemVlg.childControlHeight = false;
            itemVlg.childForceExpandWidth = true;
            itemVlg.childForceExpandHeight = false;

            var itemCsf = riteItemPrefab.GetComponent<ContentSizeFitter>();
            if (itemCsf == null) itemCsf = riteItemPrefab.AddComponent<ContentSizeFitter>();
            itemCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            itemCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var itemLe = riteItemPrefab.GetComponent<LayoutElement>();
            if (itemLe == null) itemLe = riteItemPrefab.AddComponent<LayoutElement>();
            itemLe.minHeight = 120f;
            itemLe.preferredHeight = 120f;

            // Create or configure TopRow
            Transform topRow = riteItemPrefab.transform.Find("TopRow");
            GameObject topRowGo;
            if (topRow == null)
            {
                topRowGo = new GameObject("TopRow", typeof(RectTransform));
                topRowGo.transform.SetParent(riteItemPrefab.transform, false);
                topRow = topRowGo.transform;
            }
            else
            {
                topRowGo = topRow.gameObject;
            }

            var topRowRt = topRowGo.GetComponent<RectTransform>();
            topRowRt.anchorMin = new Vector2(0f, 1f);
            topRowRt.anchorMax = new Vector2(1f, 1f);
            topRowRt.pivot = new Vector2(0.5f, 1f);
            topRowRt.sizeDelta = new Vector2(0f, 80f);

            var topRowHlg = topRowGo.GetComponent<HorizontalLayoutGroup>();
            if (topRowHlg == null) topRowHlg = topRowGo.AddComponent<HorizontalLayoutGroup>();
            topRowHlg.padding = new RectOffset(15, 15, 5, 5);
            topRowHlg.spacing = 15;
            topRowHlg.childAlignment = TextAnchor.MiddleLeft;
            topRowHlg.childControlWidth = false;
            topRowHlg.childControlHeight = false;
            topRowHlg.childForceExpandWidth = false;
            topRowHlg.childForceExpandHeight = false;

            // Move Icon and TitleContainer to TopRow
            Transform icon = riteItemPrefab.transform.Find("Icon");
            if (icon != null) icon.SetParent(topRow, false);
            else icon = topRow.Find("Icon");

            Transform titleContainer = riteItemPrefab.transform.Find("Text");
            if (titleContainer != null)
            {
                titleContainer.name = "TitleContainer";
                titleContainer.SetParent(topRow, false);
            }
            else
            {
                titleContainer = topRow.Find("TitleContainer");
                if (titleContainer == null)
                {
                    titleContainer = topRow.Find("Text");
                    if (titleContainer != null) titleContainer.name = "TitleContainer";
                }
            }

            if (icon != null)
            {
                var iconRt = icon.GetComponent<RectTransform>();
                iconRt.sizeDelta = new Vector2(50f, 50f);
                var iconLe = icon.GetComponent<LayoutElement>();
                if (iconLe == null) iconLe = icon.gameObject.AddComponent<LayoutElement>();
                iconLe.minWidth = 50f;
                iconLe.minHeight = 50f;
                iconLe.preferredWidth = 50f;
                iconLe.preferredHeight = 50f;
            }

            if (titleContainer != null)
            {
                var titleRt = titleContainer.GetComponent<RectTransform>();
                var titleVlg = titleContainer.GetComponent<VerticalLayoutGroup>();
                if (titleVlg == null) titleVlg = titleContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                titleVlg.spacing = 2;
                titleVlg.childControlWidth = true;
                titleVlg.childControlHeight = true;
                titleVlg.childForceExpandWidth = true;
                titleVlg.childForceExpandHeight = false;

                var titleLe = titleContainer.GetComponent<LayoutElement>();
                if (titleLe == null) titleLe = titleContainer.gameObject.AddComponent<LayoutElement>();
                titleLe.flexibleWidth = 1f;
            }

            // Create or configure ExpandArrowText
            Transform expandArrow = topRow.Find("ExpandArrowText");
            TextMeshProUGUI arrowTextComp;
            if (expandArrow == null)
            {
                var arrowGo = new GameObject("ExpandArrowText", typeof(RectTransform), typeof(TextMeshProUGUI));
                arrowGo.transform.SetParent(topRow, false);
                expandArrow = arrowGo.transform;
                arrowTextComp = arrowGo.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                arrowTextComp = expandArrow.GetComponent<TextMeshProUGUI>();
            }
            arrowTextComp.text = "▼";
            arrowTextComp.fontSize = 18f;
            arrowTextComp.alignment = TextAlignmentOptions.Center;
            arrowTextComp.color = Color.white;

            var arrowLe = expandArrow.GetComponent<LayoutElement>();
            if (arrowLe == null) arrowLe = expandArrow.gameObject.AddComponent<LayoutElement>();
            arrowLe.preferredWidth = 30f;
            arrowLe.preferredHeight = 30f;

            // Create or configure DetailsPanel
            Transform detailsPanel = riteItemPrefab.transform.Find("DetailsPanel");
            GameObject detailsPanelGo;
            if (detailsPanel == null)
            {
                detailsPanelGo = new GameObject("DetailsPanel", typeof(RectTransform));
                detailsPanelGo.transform.SetParent(riteItemPrefab.transform, false);
                detailsPanel = detailsPanelGo.transform;
            }
            else
            {
                detailsPanelGo = detailsPanel.gameObject;
            }

            var detailsVlg = detailsPanelGo.GetComponent<VerticalLayoutGroup>();
            if (detailsVlg == null) detailsVlg = detailsPanelGo.AddComponent<VerticalLayoutGroup>();
            detailsVlg.padding = new RectOffset(15, 15, 5, 5);
            detailsVlg.spacing = 5;
            detailsVlg.childControlWidth = true;
            detailsVlg.childControlHeight = true;
            detailsVlg.childForceExpandWidth = true;
            detailsVlg.childForceExpandHeight = false;

            var detailsCsf = detailsPanelGo.GetComponent<ContentSizeFitter>();
            if (detailsCsf == null) detailsCsf = detailsPanelGo.AddComponent<ContentSizeFitter>();
            detailsCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            detailsCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Create StatsText
            Transform statsText = detailsPanel.Find("StatsText");
            TextMeshProUGUI statsTextComp;
            if (statsText == null)
            {
                var statsGo = new GameObject("StatsText", typeof(RectTransform), typeof(TextMeshProUGUI));
                statsGo.transform.SetParent(detailsPanel, false);
                statsText = statsGo.transform;
                statsTextComp = statsGo.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                statsTextComp = statsText.GetComponent<TextMeshProUGUI>();
            }
            statsTextComp.fontSize = 14f;
            statsTextComp.color = new Color(1f, 0.9f, 0.5f, 1f);
            statsTextComp.alignment = TextAlignmentOptions.Left;

            // Create DescriptionText
            Transform descText = detailsPanel.Find("DescriptionText");
            TextMeshProUGUI descTextComp;
            if (descText == null)
            {
                var descGo = new GameObject("DescriptionText", typeof(RectTransform), typeof(TextMeshProUGUI));
                descGo.transform.SetParent(detailsPanel, false);
                descText = descGo.transform;
                descTextComp = descGo.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                descTextComp = descText.GetComponent<TextMeshProUGUI>();
            }
            descTextComp.fontSize = 14f;
            descTextComp.color = Color.white;
            descTextComp.enableWordWrapping = true;
            descTextComp.alignment = TextAlignmentOptions.Left;

            // Bind fields via reflection
            var itemUI = riteItemPrefab.GetComponent<CohortRiteItemUI>();
            var itemType = typeof(CohortRiteItemUI);

            var iconField = itemType.GetField("_iconImage", BindingFlags.NonPublic | BindingFlags.Instance);
            var nameField = itemType.GetField("_nameText", BindingFlags.NonPublic | BindingFlags.Instance);
            var costField = itemType.GetField("_costText", BindingFlags.NonPublic | BindingFlags.Instance);

            var oldIcon = iconField.GetValue(itemUI) as Image;
            var oldName = nameField.GetValue(itemUI) as TextMeshProUGUI;
            var oldCost = costField.GetValue(itemUI) as TextMeshProUGUI;

            if (oldIcon == null && icon != null) oldIcon = icon.GetComponent<Image>();
            if (oldName == null && titleContainer != null && titleContainer.childCount > 0) oldName = titleContainer.GetChild(0).GetComponent<TextMeshProUGUI>();
            if (oldCost == null && titleContainer != null && titleContainer.childCount > 1) oldCost = titleContainer.GetChild(1).GetComponent<TextMeshProUGUI>();

            iconField.SetValue(itemUI, oldIcon);
            nameField.SetValue(itemUI, oldName);
            costField.SetValue(itemUI, oldCost);

            var detailsField = itemType.GetField("_detailsPanel", BindingFlags.NonPublic | BindingFlags.Instance);
            var statsField = itemType.GetField("_statsText", BindingFlags.NonPublic | BindingFlags.Instance);
            var descField = itemType.GetField("_descriptionText", BindingFlags.NonPublic | BindingFlags.Instance);
            var arrowField = itemType.GetField("_expandArrowText", BindingFlags.NonPublic | BindingFlags.Instance);

            detailsField.SetValue(itemUI, detailsPanelGo);
            statsField.SetValue(itemUI, statsTextComp);
            descField.SetValue(itemUI, descTextComp);
            arrowField.SetValue(itemUI, arrowTextComp);

            // Make details panel active by default
            detailsPanelGo.SetActive(true);
            */

            // Bind values on CohortSquadUI
            vassalsPanelField.SetValue(cohortUI, centerSlotsGo);
            ritesPanelField.SetValue(cohortUI, centerRitesGo);
            containerField.SetValue(cohortUI, availableRitesContainer);

            // Save and apply changes
            EditorUtility.SetDirty(riteItemPrefab);
            EditorUtility.SetDirty(cohortUI);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Sovereign Rites UI Rebuild Complete!");
        }


    }
}
#endif
