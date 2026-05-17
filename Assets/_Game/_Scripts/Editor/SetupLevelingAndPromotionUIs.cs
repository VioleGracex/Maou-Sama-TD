using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using MaouSamaTD.UI;

namespace MaouSamaTD.Editor
{
    public class SetupLevelingAndPromotionUIs : EditorWindow
    {
        [MenuItem("Tools/Setup Leveling And Promotion UIs")]
        public static void RunSetup()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name != "Home_New")
            {
                Debug.LogError("[Setup] Please open the 'Home_New' scene before running this setup.");
                return;
            }

            Debug.Log("[Setup] Starting Leveling and Promotion UI Setup...");

            // 1. Ensure Entry Prefabs Exist
            GameObject memoryPrefab = EnsureMemoryEntryPrefab();
            GameObject nodePrefab = EnsureNodeEntryPrefab();

            // 2. Locate inspector root in scene (supporting inactive objects)
            GameObject inspectorRoot = null;
            foreach (var root in activeScene.GetRootGameObjects())
            {
                if (root.name == "UnitInspector_FullScreen_UI")
                {
                    inspectorRoot = root;
                    break;
                }
                var allTransforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTransforms)
                {
                    if (t.name == "UnitInspector_FullScreen_UI")
                    {
                        inspectorRoot = t.gameObject;
                        break;
                    }
                }
                if (inspectorRoot != null) break;
            }

            if (inspectorRoot == null)
            {
                Debug.LogError("[Setup] 'UnitInspector_FullScreen_UI' not found in active scene!");
                return;
            }

            var mainContent = inspectorRoot.transform.Find("Main_Content");
            if (mainContent == null)
            {
                Debug.LogError("[Setup] 'Main_Content' not found under 'UnitInspector_FullScreen_UI'!");
                return;
            }

            // Find font to match styles
            TMP_FontAsset mainFont = null;
            var text = mainContent.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) mainFont = text.font;

            // 3. Clear/Setup Unit_Leveling_Page
            Transform levelingPage = mainContent.Find("Unit_Leveling_Page");
            if (levelingPage == null)
            {
                var go = new GameObject("Unit_Leveling_Page", typeof(RectTransform));
                go.transform.SetParent(mainContent, false);
                levelingPage = go.transform;
            }
            SetupLevelingPageUI(levelingPage.gameObject, mainFont);

            // 4. Clear/Setup Content_Resonance
            Transform resonancePage = mainContent.Find("Content_Resonance");
            if (resonancePage == null)
            {
                var go = new GameObject("Content_Resonance", typeof(RectTransform));
                go.transform.SetParent(mainContent, false);
                resonancePage = go.transform;
            }
            SetupResonancePageUI(resonancePage.gameObject, mainFont);

            // 5. Ensure Managers & Component Wiring
            SetupManagersAndWiring(inspectorRoot, levelingPage, resonancePage, memoryPrefab, nodePrefab);

            // 6. Save Scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[Setup] Successfully setup Leveling and Promotion UIs, wired references, and saved the scene!");
        }

        private static GameObject EnsureMemoryEntryPrefab()
        {
            string dir = "Assets/_Game/Art/UI";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = dir + "/MemoryEntryPrefab.prefab";

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;

            // Create temporary instance to save as prefab
            var root = new GameObject("MemoryEntryPrefab", typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 80);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.18f, 0.85f);

            var titleObj = new GameObject("TitleText", typeof(RectTransform));
            titleObj.transform.SetParent(root.transform, false);
            var title = titleObj.AddComponent<TextMeshProUGUI>();
            title.fontSize = 18;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.95f, 0.75f, 0.3f);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0, 0.5f);
            titleRect.sizeDelta = new Vector2(200, 30);

            var bodyObj = new GameObject("BodyText", typeof(RectTransform));
            bodyObj.transform.SetParent(root.transform, false);
            var body = bodyObj.AddComponent<TextMeshProUGUI>();
            body.fontSize = 14;
            body.color = Color.white;
            var bodyRect = bodyObj.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.05f, 0.15f);
            bodyRect.anchorMax = new Vector2(0.65f, 0.45f);
            bodyRect.sizeDelta = Vector2.zero;

            var btnObj = new GameObject("UnlockButton", typeof(RectTransform));
            btnObj.transform.SetParent(root.transform, false);
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.85f, 0.4f, 0.1f);
            var btn = btnObj.AddComponent<Button>();
            var btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.95f, 0.5f);
            btnRect.anchorMax = new Vector2(0.95f, 0.5f);
            btnRect.pivot = new Vector2(1f, 0.5f);
            btnRect.sizeDelta = new Vector2(100, 40);

            var btnTextObj = new GameObject("Text", typeof(RectTransform));
            btnTextObj.transform.SetParent(btnObj.transform, false);
            var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "UNLOCK";
            btnText.fontSize = 14;
            btnText.fontStyle = FontStyles.Bold;
            btnText.alignment = TextAlignmentOptions.Center;
            var btRect = btnTextObj.GetComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;

            var lockObj = new GameObject("LockOverlay", typeof(RectTransform));
            lockObj.transform.SetParent(root.transform, false);
            var lockImg = lockObj.AddComponent<Image>();
            lockImg.color = new Color(0, 0, 0, 0.5f);
            var lockRect = lockObj.GetComponent<RectTransform>();
            lockRect.anchorMin = Vector2.zero;
            lockRect.anchorMax = Vector2.one;
            lockRect.sizeDelta = Vector2.zero;
            lockObj.SetActive(false);

            var comp = root.AddComponent<MemoryEntryUI>();
            var so = new SerializedObject(comp);
            so.FindProperty("_txtTitle").objectReferenceValue = title;
            so.FindProperty("_txtBody").objectReferenceValue = body;
            so.FindProperty("_lockOverlay").objectReferenceValue = lockObj;
            so.FindProperty("_btnUnlock").objectReferenceValue = btn;
            so.ApplyModifiedProperties();

            prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
            return prefab;
        }

        private static GameObject EnsureNodeEntryPrefab()
        {
            string dir = "Assets/_Game/Art/UI";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = dir + "/NodeEntryPrefab.prefab";

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;

            var root = new GameObject("NodeEntryPrefab", typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 60);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

            var iconObj = new GameObject("NodeIcon", typeof(RectTransform));
            iconObj.transform.SetParent(root.transform, false);
            var icon = iconObj.AddComponent<Image>();
            icon.color = Color.gray;
            var iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.05f, 0.5f);
            iconRect.anchorMax = new Vector2(0.05f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(30, 30);

            var labelObj = new GameObject("LabelText", typeof(RectTransform));
            labelObj.transform.SetParent(root.transform, false);
            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.fontSize = 16;
            label.color = Color.white;
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.15f, 0.5f);
            labelRect.anchorMax = new Vector2(0.65f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(250, 35);

            var btnObj = new GameObject("UnlockButton", typeof(RectTransform));
            btnObj.transform.SetParent(root.transform, false);
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.9f, 0.65f, 0.1f);
            var btn = btnObj.AddComponent<Button>();
            var btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.95f, 0.5f);
            btnRect.anchorMax = new Vector2(0.95f, 0.5f);
            btnRect.pivot = new Vector2(1f, 0.5f);
            btnRect.sizeDelta = new Vector2(100, 36);

            var btnTextObj = new GameObject("Text", typeof(RectTransform));
            btnTextObj.transform.SetParent(btnObj.transform, false);
            var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "ACTIVATE";
            btnText.fontSize = 12;
            btnText.fontStyle = FontStyles.Bold;
            btnText.alignment = TextAlignmentOptions.Center;
            var btRect = btnTextObj.GetComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;

            var comp = root.AddComponent<NodeEntryUI>();
            var so = new SerializedObject(comp);
            so.FindProperty("_txtLabel").objectReferenceValue = label;
            so.FindProperty("_nodeIcon").objectReferenceValue = icon;
            so.FindProperty("_btnUnlock").objectReferenceValue = btn;
            so.ApplyModifiedProperties();

            prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
            return prefab;
        }

        private static void SetupLevelingPageUI(GameObject page, TMP_FontAsset font)
        {
            // Clear existing
            for (int i = page.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(page.transform.GetChild(i).gameObject);
            }

            page.SetActive(false);
            var rect = page.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // Transparent glassmorphic bg overlay
            var bg = page.GetComponent<Image>();
            if (bg == null) bg = page.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.96f);

            // ── TOP HEADER ──
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(page.transform, false);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.9f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.sizeDelta = Vector2.zero;

            // Back button
            var backBtnObj = new GameObject("Back_Button", typeof(RectTransform));
            backBtnObj.transform.SetParent(header.transform, false);
            var backImg = backBtnObj.AddComponent<Image>();
            backImg.color = new Color(0.3f, 0.3f, 0.38f, 0.6f);
            var backBtn = backBtnObj.AddComponent<Button>();
            var backRect = backBtnObj.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.03f, 0.5f);
            backRect.anchorMax = new Vector2(0.03f, 0.5f);
            backRect.pivot = new Vector2(0f, 0.5f);
            backRect.sizeDelta = new Vector2(150, 45);

            var backText = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            backText.transform.SetParent(backBtnObj.transform, false);
            backText.font = font;
            backText.text = "← BACK TO STATS";
            backText.fontSize = 16;
            backText.fontStyle = FontStyles.Bold;
            backText.alignment = TextAlignmentOptions.Center;
            backText.color = Color.white;
            var btRect = backText.GetComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;

            // Main Title
            var title = new GameObject("Title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            title.transform.SetParent(header.transform, false);
            title.font = font;
            title.text = "VASSAL LEVEL UP";
            title.fontSize = 32;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.95f, 0.75f, 0.3f);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(400, 50);

            // ── MAIN LAYOUT ──
            var layout = new GameObject("MainLayout", typeof(RectTransform));
            layout.transform.SetParent(page.transform, false);
            var layoutRect = layout.GetComponent<RectTransform>();
            layoutRect.anchorMin = new Vector2(0.03f, 0.05f);
            layoutRect.anchorMax = new Vector2(0.97f, 0.88f);
            layoutRect.sizeDelta = Vector2.zero;

            // LEFT PANEL: Portrait and level progress
            var leftPanel = new GameObject("LeftPanel", typeof(RectTransform));
            leftPanel.transform.SetParent(layout.transform, false);
            var leftRect = leftPanel.GetComponent<RectTransform>();
            leftRect.anchorMin = Vector2.zero;
            leftRect.anchorMax = new Vector2(0.35f, 1f);
            leftRect.sizeDelta = Vector2.zero;

            // Frame BG
            var leftBGObj = new GameObject("BG", typeof(RectTransform));
            leftBGObj.transform.SetParent(leftPanel.transform, false);
            var leftBGImg = leftBGObj.AddComponent<Image>();
            leftBGImg.color = new Color(0.12f, 0.12f, 0.18f, 0.8f);
            var lbRect = leftBGObj.GetComponent<RectTransform>();
            lbRect.anchorMin = Vector2.zero;
            lbRect.anchorMax = Vector2.one;
            lbRect.sizeDelta = Vector2.zero;

            // Portrait Mirror
            var portObj = new GameObject("Portrait_Art", typeof(RectTransform));
            portObj.transform.SetParent(leftPanel.transform, false);
            var portImg = portObj.AddComponent<Image>();
            portImg.color = Color.white;
            portImg.preserveAspect = true;
            var portRect = portObj.GetComponent<RectTransform>();
            portRect.anchorMin = new Vector2(0.05f, 0.35f);
            portRect.anchorMax = new Vector2(0.95f, 0.95f);
            portRect.sizeDelta = Vector2.zero;

            // Level Preview Hex
            var hexObj = new GameObject("LevelPreview", typeof(RectTransform));
            hexObj.transform.SetParent(leftPanel.transform, false);
            var hexImg = hexObj.AddComponent<Image>();
            hexImg.color = new Color(0.2f, 0.2f, 0.28f, 0.9f);
            var hexRect = hexObj.GetComponent<RectTransform>();
            hexRect.anchorMin = new Vector2(0.5f, 0.3f);
            hexRect.anchorMax = new Vector2(0.5f, 0.3f);
            hexRect.pivot = new Vector2(0.5f, 0.5f);
            hexRect.sizeDelta = new Vector2(280, 50);

            var lvlPreviewTxt = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            lvlPreviewTxt.transform.SetParent(hexObj.transform, false);
            lvlPreviewTxt.font = font;
            lvlPreviewTxt.text = "Lv. 15 → 16";
            lvlPreviewTxt.fontSize = 24;
            lvlPreviewTxt.fontStyle = FontStyles.Bold;
            lvlPreviewTxt.alignment = TextAlignmentOptions.Center;
            lvlPreviewTxt.color = new Color(0.95f, 0.75f, 0.3f);
            var lpRect = lvlPreviewTxt.GetComponent<RectTransform>();
            lpRect.anchorMin = Vector2.zero;
            lpRect.anchorMax = Vector2.one;
            lpRect.sizeDelta = Vector2.zero;

            // XP Bar Background
            var xpBarBgObj = new GameObject("XPBar_Background", typeof(RectTransform));
            xpBarBgObj.transform.SetParent(leftPanel.transform, false);
            var xpBarBg = xpBarBgObj.AddComponent<Image>();
            xpBarBg.color = new Color(0.15f, 0.15f, 0.22f, 1f);
            var xpBarBgRect = xpBarBgObj.GetComponent<RectTransform>();
            xpBarBgRect.anchorMin = new Vector2(0.1f, 0.18f);
            xpBarBgRect.anchorMax = new Vector2(0.9f, 0.24f);
            xpBarBgRect.sizeDelta = Vector2.zero;

            // Preview XP fill (added XP)
            var xpAddFillObj = new GameObject("XP_Add_Fill", typeof(RectTransform));
            xpAddFillObj.transform.SetParent(xpBarBgObj.transform, false);
            var xpAddFill = xpAddFillObj.AddComponent<Image>();
            xpAddFill.color = new Color(0.85f, 0.4f, 0.15f, 0.6f);
            xpAddFill.type = Image.Type.Filled;
            xpAddFill.fillMethod = Image.FillMethod.Horizontal;
            xpAddFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            var xafRect = xpAddFillObj.GetComponent<RectTransform>();
            xafRect.anchorMin = Vector2.zero;
            xafRect.anchorMax = Vector2.one;
            xafRect.sizeDelta = Vector2.zero;

            // Current XP fill
            var xpCurrentFillObj = new GameObject("XP_Current_Fill", typeof(RectTransform));
            xpCurrentFillObj.transform.SetParent(xpBarBgObj.transform, false);
            var xpCurrentFill = xpCurrentFillObj.AddComponent<Image>();
            xpCurrentFill.color = new Color(0.95f, 0.75f, 0.3f, 1f);
            xpCurrentFill.type = Image.Type.Filled;
            xpCurrentFill.fillMethod = Image.FillMethod.Horizontal;
            xpCurrentFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            var xcfRect = xpCurrentFillObj.GetComponent<RectTransform>();
            xcfRect.anchorMin = Vector2.zero;
            xcfRect.anchorMax = Vector2.one;
            xcfRect.sizeDelta = Vector2.zero;

            // XP Gain Text
            var xpGainTxt = new GameObject("XPGainText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            xpGainTxt.transform.SetParent(leftPanel.transform, false);
            xpGainTxt.font = font;
            xpGainTxt.text = "+1,200 XP";
            xpGainTxt.fontSize = 18;
            xpGainTxt.fontStyle = FontStyles.Bold;
            xpGainTxt.alignment = TextAlignmentOptions.Center;
            xpGainTxt.color = new Color(0.3f, 0.95f, 0.4f);
            var xgRect = xpGainTxt.GetComponent<RectTransform>();
            xgRect.anchorMin = new Vector2(0.5f, 0.13f);
            xgRect.anchorMax = new Vector2(0.5f, 0.13f);
            xgRect.pivot = new Vector2(0.5f, 0.5f);
            xgRect.sizeDelta = new Vector2(200, 30);

            // XP Meter Value Text
            var xpValueTxt = new GameObject("XPMeterText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            xpValueTxt.transform.SetParent(leftPanel.transform, false);
            xpValueTxt.font = font;
            xpValueTxt.text = "1,200 / 5,000";
            xpValueTxt.fontSize = 14;
            xpValueTxt.alignment = TextAlignmentOptions.Center;
            xpValueTxt.color = Color.white;
            var xvRect = xpValueTxt.GetComponent<RectTransform>();
            xvRect.anchorMin = new Vector2(0.5f, 0.08f);
            xvRect.anchorMax = new Vector2(0.5f, 0.08f);
            xvRect.pivot = new Vector2(0.5f, 0.5f);
            xvRect.sizeDelta = new Vector2(250, 30);


            // RIGHT PANEL: Grid and button controls
            var rightPanel = new GameObject("RightPanel", typeof(RectTransform));
            rightPanel.transform.SetParent(layout.transform, false);
            var rightRect = rightPanel.GetComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.37f, 0f);
            rightRect.anchorMax = new Vector2(1f, 1f);
            rightRect.sizeDelta = Vector2.zero;

            // Frame BG
            var rightBGObj = new GameObject("BG", typeof(RectTransform));
            rightBGObj.transform.SetParent(rightPanel.transform, false);
            var rightBGImg = rightBGObj.AddComponent<Image>();
            rightBGImg.color = new Color(0.12f, 0.12f, 0.18f, 0.8f);
            var rbBG = rightBGObj.GetComponent<RectTransform>();
            rbBG.anchorMin = Vector2.zero;
            rbBG.anchorMax = Vector2.one;
            rbBG.sizeDelta = Vector2.zero;

            // Duplicates Info/Instruction Text
            var infoTxt = new GameObject("InfoText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            infoTxt.transform.SetParent(rightPanel.transform, false);
            infoTxt.font = font;
            infoTxt.text = "Select XP Cores and duplicate Vassals to upgrade";
            infoTxt.fontSize = 16;
            infoTxt.color = new Color(0.8f, 0.8f, 0.85f);
            var infoRect = infoTxt.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.05f, 0.92f);
            infoRect.anchorMax = new Vector2(0.95f, 0.98f);
            infoRect.sizeDelta = Vector2.zero;

            // Unified scroll list
            var scrollObj = new GameObject("DuplicatesScrollRect", typeof(RectTransform));
            scrollObj.transform.SetParent(rightPanel.transform, false);
            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            var sRect = scrollObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.05f, 0.25f);
            sRect.anchorMax = new Vector2(0.95f, 0.90f);
            sRect.sizeDelta = Vector2.zero;

            // Viewport
            var viewObj = new GameObject("Viewport", typeof(RectTransform));
            viewObj.transform.SetParent(scrollObj.transform, false);
            var viewImg = viewObj.AddComponent<Image>();
            viewImg.color = Color.clear;
            var mask = viewObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var vRect = viewObj.GetComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;
            vRect.sizeDelta = Vector2.zero;
            scroll.viewport = vRect;

            // Content
            var contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(viewObj.transform, false);
            var contentGrid = contentObj.AddComponent<GridLayoutGroup>();
            contentGrid.cellSize = new Vector2(150, 210);
            contentGrid.spacing = new Vector2(15, 15);
            contentGrid.padding = new RectOffset(10, 10, 10, 10);
            contentGrid.constraint = GridLayoutGroup.Constraint.Flexible;
            var cRect = contentObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1f);
            cRect.sizeDelta = new Vector2(0, 300);
            scroll.content = cRect;
            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var itemPaths = new string[] {
                "Assets/_Game/Art/Items/xp_core_common.png",
                "Assets/_Game/Art/Items/xp_core_rare.png",
                "Assets/_Game/Art/Items/xp_core_epic.png",
                "Assets/_Game/Art/Items/xp_core_legendary.png"
            };
            var itemTitles = new string[] { "Common Core", "Rare Core", "Epic Core", "Legendary Core" };
            var itemColors = new Color[] {
                new Color(0.12f, 0.25f, 0.15f, 1f),
                new Color(0.12f, 0.18f, 0.3f, 1f),
                new Color(0.2f, 0.12f, 0.3f, 1f),
                new Color(0.3f, 0.22f, 0.12f, 1f)
            };

            for (int i = 0; i < 4; i++)
            {
                var card = new GameObject($"PlaceholderCard_{i}", typeof(RectTransform));
                card.transform.SetParent(cRect, false);
                var cardImg = card.AddComponent<Image>();
                cardImg.color = itemColors[i];

                var outline = card.AddComponent<Outline>();
                outline.effectColor = new Color(0.85f, 0.85f, 0.95f, 0.15f);
                outline.effectDistance = new Vector2(1, 1);

                var iconObj = new GameObject("Icon", typeof(RectTransform));
                iconObj.transform.SetParent(card.transform, false);
                var iconImg = iconObj.AddComponent<Image>();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(itemPaths[i]);
                if (sprite != null) iconImg.sprite = sprite;
                var iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.15f, 0.35f);
                iconRect.anchorMax = new Vector2(0.85f, 0.85f);
                iconRect.sizeDelta = Vector2.zero;

                var titleTxt = new GameObject("Title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                titleTxt.transform.SetParent(card.transform, false);
                titleTxt.text = itemTitles[i];
                titleTxt.fontSize = 13;
                titleTxt.fontStyle = FontStyles.Bold;
                titleTxt.alignment = TextAlignmentOptions.Center;
                titleTxt.color = Color.white;
                var cardTitleRect = titleTxt.GetComponent<RectTransform>();
                cardTitleRect.anchorMin = new Vector2(0.05f, 0.18f);
                cardTitleRect.anchorMax = new Vector2(0.95f, 0.3f);
                cardTitleRect.sizeDelta = Vector2.zero;

                var countTxt = new GameObject("Count", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                countTxt.transform.SetParent(card.transform, false);
                countTxt.text = "x99";
                countTxt.fontSize = 11;
                countTxt.alignment = TextAlignmentOptions.Center;
                countTxt.color = new Color(0.85f, 0.85f, 0.85f);
                var cardCountRect = countTxt.GetComponent<RectTransform>();
                cardCountRect.anchorMin = new Vector2(0.05f, 0.05f);
                cardCountRect.anchorMax = new Vector2(0.95f, 0.16f);
                cardCountRect.sizeDelta = Vector2.zero;
            }

            // ── BOTTOM CONTROLS ──
            var bottomCtrls = new GameObject("BottomControls", typeof(RectTransform));
            bottomCtrls.transform.SetParent(rightPanel.transform, false);
            var bcRect = bottomCtrls.GetComponent<RectTransform>();
            bcRect.anchorMin = new Vector2(0.05f, 0.03f);
            bcRect.anchorMax = new Vector2(0.95f, 0.22f);
            bcRect.sizeDelta = Vector2.zero;

            // Auto Add Button
            var autoBtnObj = new GameObject("AutoAdd_Button", typeof(RectTransform));
            autoBtnObj.transform.SetParent(bottomCtrls.transform, false);
            var autoImg = autoBtnObj.AddComponent<Image>();
            autoImg.color = new Color(0.2f, 0.45f, 0.35f);
            var autoBtn = autoBtnObj.AddComponent<Button>();
            var abRect = autoBtnObj.GetComponent<RectTransform>();
            abRect.anchorMin = new Vector2(0f, 0.5f);
            abRect.anchorMax = new Vector2(0f, 0.5f);
            abRect.pivot = new Vector2(0f, 0.5f);
            abRect.sizeDelta = new Vector2(200, 60);

            var autoTxt = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            autoTxt.transform.SetParent(autoBtnObj.transform, false);
            autoTxt.font = font;
            autoTxt.text = "AUTO SELECT";
            autoTxt.fontSize = 16;
            autoTxt.fontStyle = FontStyles.Bold;
            autoTxt.alignment = TextAlignmentOptions.Center;
            autoTxt.color = Color.white;
            var atRect = autoTxt.GetComponent<RectTransform>();
            atRect.anchorMin = Vector2.zero;
            atRect.anchorMax = Vector2.one;
            atRect.sizeDelta = Vector2.zero;

            // Confirm Level Up Button
            var confirmBtnObj = new GameObject("ConfirmLevelUp_Button", typeof(RectTransform));
            confirmBtnObj.transform.SetParent(bottomCtrls.transform, false);
            var confirmImg = confirmBtnObj.AddComponent<Image>();
            confirmImg.color = new Color(0.85f, 0.65f, 0.15f);
            var confirmBtn = confirmBtnObj.AddComponent<Button>();
            var cbRect = confirmBtnObj.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(1f, 0.5f);
            cbRect.anchorMax = new Vector2(1f, 0.5f);
            cbRect.pivot = new Vector2(1f, 0.5f);
            cbRect.sizeDelta = new Vector2(300, 60);

            var confirmTxt = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            confirmTxt.transform.SetParent(confirmBtnObj.transform, false);
            confirmTxt.font = font;
            confirmTxt.text = "CONFIRM LEVEL UP";
            confirmTxt.fontSize = 18;
            confirmTxt.fontStyle = FontStyles.Bold;
            confirmTxt.alignment = TextAlignmentOptions.Center;
            confirmTxt.color = new Color(0.1f, 0.1f, 0.15f);
            var ctRect = confirmTxt.GetComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;

            // Collision Back Button trigger SwitchTab(0)
            backBtn.onClick.AddListener(() =>
            {
                var fullUI = coordinatorInstance;
                if (fullUI != null)
                {
                    fullUI.RequestClose(); // returns to tab 0
                }
            });

            // ── Auto-Add Settings Root (Dummy Collapsible settings) ──
            var autoRoot = new GameObject("AutoAddSettingsRoot", typeof(RectTransform));
            autoRoot.transform.SetParent(bottomCtrls.transform, false);
            autoRoot.SetActive(false);
            var arRect = autoRoot.GetComponent<RectTransform>();
            arRect.anchorMin = new Vector2(0f, 1f);
            arRect.anchorMax = new Vector2(0f, 1f);
            arRect.sizeDelta = new Vector2(10, 10); // Hidden dummy
        }

        private static void SetupResonancePageUI(GameObject page, TMP_FontAsset font)
        {
            // Clear existing
            for (int i = page.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(page.transform.GetChild(i).gameObject);
            }

            page.SetActive(false);
            var rect = page.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var bg = page.GetComponent<Image>();
            if (bg == null) bg = page.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.96f);

            // ── TOP HEADER ──
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(page.transform, false);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.9f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.sizeDelta = Vector2.zero;

            // Back button
            var backBtnObj = new GameObject("Back_Button", typeof(RectTransform));
            backBtnObj.transform.SetParent(header.transform, false);
            var backImg = backBtnObj.AddComponent<Image>();
            backImg.color = new Color(0.3f, 0.3f, 0.38f, 0.6f);
            var backBtn = backBtnObj.AddComponent<Button>();
            var backRect = backBtnObj.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.03f, 0.5f);
            backRect.anchorMax = new Vector2(0.03f, 0.5f);
            backRect.pivot = new Vector2(0f, 0.5f);
            backRect.sizeDelta = new Vector2(150, 45);

            var backText = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            backText.transform.SetParent(backBtnObj.transform, false);
            backText.font = font;
            backText.text = "← BACK TO STATS";
            backText.fontSize = 16;
            backText.fontStyle = FontStyles.Bold;
            backText.alignment = TextAlignmentOptions.Center;
            backText.color = Color.white;
            var btRect = backText.GetComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;

            // Title
            var title = new GameObject("Title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            title.transform.SetParent(header.transform, false);
            title.font = font;
            title.text = "RESONANCE & PROMOTION";
            title.fontSize = 32;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.95f, 0.75f, 0.3f);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(500, 50);

            // ── SUB-TAB SELECTION BUTTONS ──
            var subTabs = new GameObject("SubTabs", typeof(RectTransform));
            subTabs.transform.SetParent(page.transform, false);
            var subTabsRect = subTabs.GetComponent<RectTransform>();
            subTabsRect.anchorMin = new Vector2(0.15f, 0.8f);
            subTabsRect.anchorMax = new Vector2(0.85f, 0.88f);
            subTabsRect.sizeDelta = Vector2.zero;
            var hlg = subTabs.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            var tabPromoteObj = CreateTabBtn(subTabs, "PROMOTION", font);
            var tabMemoriesObj = CreateTabBtn(subTabs, "MEMORIES", font);
            var tabNodesObj = CreateTabBtn(subTabs, "RESONANCE NODES", font);

            // ── SUB-TAB ROOT PANELS CONTAINER ──
            var rootPanel = new GameObject("RootsContainer", typeof(RectTransform));
            rootPanel.transform.SetParent(page.transform, false);
            var rpRect = rootPanel.GetComponent<RectTransform>();
            rpRect.anchorMin = new Vector2(0.05f, 0.05f);
            rpRect.anchorMax = new Vector2(0.95f, 0.77f);
            rpRect.sizeDelta = Vector2.zero;

            // ── ROOT PROMOTE ──
            var rootPromote = new GameObject("RootPromote", typeof(RectTransform));
            rootPromote.transform.SetParent(rpRect.transform, false);
            var rPrmRect = rootPromote.GetComponent<RectTransform>();
            rPrmRect.anchorMin = Vector2.zero;
            rPrmRect.anchorMax = Vector2.one;
            rPrmRect.sizeDelta = Vector2.zero;

            // Star Rating flow
            var flowObj = new GameObject("StarFlow", typeof(RectTransform));
            flowObj.transform.SetParent(rootPromote.transform, false);
            var fRect = flowObj.GetComponent<RectTransform>();
            fRect.anchorMin = new Vector2(0.5f, 0.7f);
            fRect.anchorMax = new Vector2(0.5f, 0.7f);
            fRect.pivot = new Vector2(0.5f, 0.5f);
            fRect.sizeDelta = new Vector2(600, 100);

            // Current Stars container
            var currStars = new GameObject("CurrentStars", typeof(RectTransform));
            currStars.transform.SetParent(flowObj.transform, false);
            var csRect = currStars.GetComponent<RectTransform>();
            csRect.anchorMin = new Vector2(0f, 0f);
            csRect.anchorMax = new Vector2(0.42f, 1f);
            csRect.sizeDelta = Vector2.zero;
            var csLayout = currStars.AddComponent<HorizontalLayoutGroup>();
            csLayout.spacing = 6;
            csLayout.childAlignment = TextAnchor.MiddleRight;
            csLayout.childControlWidth = false;
            csLayout.childControlHeight = false;

            // Transition Arrow
            var arrowObj = new GameObject("ArrowText", typeof(RectTransform));
            arrowObj.transform.SetParent(flowObj.transform, false);
            var arrowTxt = arrowObj.AddComponent<TextMeshProUGUI>();
            arrowTxt.font = font;
            arrowTxt.text = "→";
            arrowTxt.fontSize = 42;
            arrowTxt.fontStyle = FontStyles.Bold;
            arrowTxt.alignment = TextAlignmentOptions.Center;
            arrowTxt.color = new Color(0.95f, 0.5f, 0.15f);
            var arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(0.42f, 0f);
            arrowRect.anchorMax = new Vector2(0.58f, 1f);
            arrowRect.sizeDelta = Vector2.zero;

            // Next Stars container
            var nextStars = new GameObject("NextStars", typeof(RectTransform));
            nextStars.transform.SetParent(flowObj.transform, false);
            var nsRect = nextStars.GetComponent<RectTransform>();
            nsRect.anchorMin = new Vector2(0.58f, 0f);
            nsRect.anchorMax = new Vector2(1f, 1f);
            nsRect.sizeDelta = Vector2.zero;
            var nsLayout = nextStars.AddComponent<HorizontalLayoutGroup>();
            nsLayout.spacing = 6;
            nsLayout.childAlignment = TextAnchor.MiddleLeft;
            nsLayout.childControlWidth = false;
            nsLayout.childControlHeight = false;

            var starSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icons/UI_Icon_Star_Full.png");
            var starEmptySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icons/UI_Icon_Star_Empty.png");

            // Add placeholder stars in CurrentStars (3 full stars, 3 empty stars)
            for (int i = 0; i < 6; i++)
            {
                var s = new GameObject($"PlaceholderStar_{i}", typeof(RectTransform));
                s.transform.SetParent(currStars.transform, false);
                var img = s.AddComponent<Image>();
                img.sprite = (i < 3) ? starSprite : starEmptySprite;
                s.GetComponent<RectTransform>().sizeDelta = new Vector2(36, 36);
            }

            // Add placeholder stars in NextStars (4 full stars, 2 empty stars)
            for (int i = 0; i < 6; i++)
            {
                var s = new GameObject($"PlaceholderStar_{i}", typeof(RectTransform));
                s.transform.SetParent(nextStars.transform, false);
                var img = s.AddComponent<Image>();
                img.sprite = (i < 4) ? starSprite : starEmptySprite;
                s.GetComponent<RectTransform>().sizeDelta = new Vector2(36, 36);
            }

            // Material grid box
            var matBoxObj = new GameObject("MaterialGrid", typeof(RectTransform));
            matBoxObj.transform.SetParent(rootPromote.transform, false);
            var mbRect = matBoxObj.GetComponent<RectTransform>();
            mbRect.anchorMin = new Vector2(0.15f, 0.25f);
            mbRect.anchorMax = new Vector2(0.85f, 0.55f);
            mbRect.sizeDelta = Vector2.zero;

            var primaryMatName = new GameObject("PrimaryMatName", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            primaryMatName.transform.SetParent(matBoxObj.transform, false);
            primaryMatName.font = font;
            primaryMatName.text = "Golem Core\nShadow Essence\nBandit Insignia";
            primaryMatName.fontSize = 18;
            primaryMatName.fontStyle = FontStyles.Bold;
            primaryMatName.alignment = TextAlignmentOptions.Left;
            primaryMatName.color = Color.white;
            var pmnRect = primaryMatName.GetComponent<RectTransform>();
            pmnRect.anchorMin = new Vector2(0.05f, 0.1f);
            pmnRect.anchorMax = new Vector2(0.45f, 0.9f);
            pmnRect.sizeDelta = Vector2.zero;

            var primaryMatCount = new GameObject("PrimaryMatCount", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            primaryMatCount.transform.SetParent(matBoxObj.transform, false);
            primaryMatCount.font = font;
            primaryMatCount.text = "12 / 10\n4 / 5\n8 / 15";
            primaryMatCount.fontSize = 18;
            primaryMatCount.fontStyle = FontStyles.Bold;
            primaryMatCount.alignment = TextAlignmentOptions.Right;
            primaryMatCount.color = new Color(0.95f, 0.75f, 0.3f);
            var pmcRect = primaryMatCount.GetComponent<RectTransform>();
            pmcRect.anchorMin = new Vector2(0.05f, 0.1f);
            pmcRect.anchorMax = new Vector2(0.45f, 0.9f);
            pmcRect.sizeDelta = Vector2.zero;

            var secMatName = new GameObject("SecondaryMatName", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            secMatName.transform.SetParent(matBoxObj.transform, false);
            secMatName.font = font;
            secMatName.text = "Beast Fang\nDemon Horn\nDragon Scale";
            secMatName.fontSize = 18;
            secMatName.fontStyle = FontStyles.Bold;
            secMatName.alignment = TextAlignmentOptions.Left;
            secMatName.color = Color.white;
            var smnRect = secMatName.GetComponent<RectTransform>();
            smnRect.anchorMin = new Vector2(0.55f, 0.1f);
            smnRect.anchorMax = new Vector2(0.95f, 0.9f);
            smnRect.sizeDelta = Vector2.zero;

            var secMatCount = new GameObject("SecondaryMatCount", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            secMatCount.transform.SetParent(matBoxObj.transform, false);
            secMatCount.font = font;
            secMatCount.text = "9 / 5\n1 / 2\n0 / 1";
            secMatCount.fontSize = 18;
            secMatCount.fontStyle = FontStyles.Bold;
            secMatCount.alignment = TextAlignmentOptions.Right;
            secMatCount.color = new Color(0.95f, 0.75f, 0.3f);
            var smcRect = secMatCount.GetComponent<RectTransform>();
            smcRect.anchorMin = new Vector2(0.55f, 0.1f);
            smcRect.anchorMax = new Vector2(0.95f, 0.9f);
            smcRect.sizeDelta = Vector2.zero;

            // Gold/Currency Box
            var goldCostTxt = new GameObject("GoldCostText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            goldCostTxt.transform.SetParent(rootPromote.transform, false);
            goldCostTxt.font = font;
            goldCostTxt.text = "Gold Cost: 1,500 / 3,000";
            goldCostTxt.fontSize = 20;
            goldCostTxt.fontStyle = FontStyles.Bold;
            goldCostTxt.alignment = TextAlignmentOptions.Center;
            goldCostTxt.color = new Color(0.95f, 0.75f, 0.3f);
            var gctRect = goldCostTxt.GetComponent<RectTransform>();
            gctRect.anchorMin = new Vector2(0.5f, 0.18f);
            gctRect.anchorMax = new Vector2(0.5f, 0.18f);
            gctRect.pivot = new Vector2(0.5f, 0.5f);
            gctRect.sizeDelta = new Vector2(500, 40);

            // Promote Button
            var prmBtnObj = new GameObject("PromoteButton", typeof(RectTransform));
            prmBtnObj.transform.SetParent(rootPromote.transform, false);
            var prmBtnImg = prmBtnObj.AddComponent<Image>();
            prmBtnImg.color = new Color(0.85f, 0.5f, 0.1f);
            var prmBtn = prmBtnObj.AddComponent<Button>();
            var pbRect = prmBtnObj.GetComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0.5f, 0.08f);
            pbRect.anchorMax = new Vector2(0.5f, 0.08f);
            pbRect.pivot = new Vector2(0.5f, 0.5f);
            pbRect.sizeDelta = new Vector2(300, 60);

            var prmBtnText = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            prmBtnText.transform.SetParent(prmBtnObj.transform, false);
            prmBtnText.font = font;
            prmBtnText.text = "PROMOTE VASSAL";
            prmBtnText.fontSize = 18;
            prmBtnText.fontStyle = FontStyles.Bold;
            prmBtnText.alignment = TextAlignmentOptions.Center;
            prmBtnText.color = new Color(0.1f, 0.1f, 0.15f);
            var pbtRect = prmBtnText.GetComponent<RectTransform>();
            pbtRect.anchorMin = Vector2.zero;
            pbtRect.anchorMax = Vector2.one;
            pbtRect.sizeDelta = Vector2.zero;

            // Status Text
            var statusTxt = new GameObject("PromoteStatusText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            statusTxt.transform.SetParent(rootPromote.transform, false);
            statusTxt.font = font;
            statusTxt.text = "";
            statusTxt.fontSize = 14;
            statusTxt.alignment = TextAlignmentOptions.Center;
            statusTxt.color = Color.white;
            var stRect = statusTxt.GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0.5f, 0.02f);
            stRect.anchorMax = new Vector2(0.5f, 0.02f);
            stRect.pivot = new Vector2(0.5f, 0.5f);
            stRect.sizeDelta = new Vector2(400, 30);


            // ── ROOT MEMORIES ──
            var rootMemories = new GameObject("RootMemories", typeof(RectTransform));
            rootMemories.transform.SetParent(rpRect.transform, false);
            rootMemories.SetActive(false);
            var rMemRect = rootMemories.GetComponent<RectTransform>();
            rMemRect.anchorMin = Vector2.zero;
            rMemRect.anchorMax = Vector2.one;
            rMemRect.sizeDelta = Vector2.zero;

            var memScrollObj = new GameObject("MemoriesScrollRect", typeof(RectTransform));
            memScrollObj.transform.SetParent(rootMemories.transform, false);
            var memScroll = memScrollObj.AddComponent<ScrollRect>();
            memScroll.horizontal = false;
            memScroll.vertical = true;
            var msRect = memScrollObj.GetComponent<RectTransform>();
            msRect.anchorMin = new Vector2(0.05f, 0.05f);
            msRect.anchorMax = new Vector2(0.95f, 0.95f);
            msRect.sizeDelta = Vector2.zero;

            // Viewport
            var mViewObj = new GameObject("Viewport", typeof(RectTransform));
            mViewObj.transform.SetParent(memScrollObj.transform, false);
            mViewObj.AddComponent<Image>().color = Color.clear;
            mViewObj.AddComponent<Mask>().showMaskGraphic = false;
            var mvRect = mViewObj.GetComponent<RectTransform>();
            mvRect.anchorMin = Vector2.zero;
            mvRect.anchorMax = Vector2.one;
            mvRect.sizeDelta = Vector2.zero;
            memScroll.viewport = mvRect;

            // Content
            var mContentObj = new GameObject("Content", typeof(RectTransform));
            mContentObj.transform.SetParent(mViewObj.transform, false);
            var mContentLayout = mContentObj.AddComponent<VerticalLayoutGroup>();
            mContentLayout.spacing = 10;
            mContentLayout.padding = new RectOffset(10, 10, 10, 10);
            mContentLayout.childControlHeight = false;
            mContentLayout.childForceExpandHeight = false;
            var mcRect = mContentObj.GetComponent<RectTransform>();
            mcRect.anchorMin = new Vector2(0, 1);
            mcRect.anchorMax = new Vector2(1, 1);
            mcRect.pivot = new Vector2(0.5f, 1f);
            mcRect.sizeDelta = new Vector2(0, 300);
            memScroll.content = mcRect;
            var mcsf = mContentObj.AddComponent<ContentSizeFitter>();
            mcsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;


            // ── ROOT NODES ──
            var rootNodes = new GameObject("RootNodes", typeof(RectTransform));
            rootNodes.transform.SetParent(rpRect.transform, false);
            rootNodes.SetActive(false);
            var rNodRect = rootNodes.GetComponent<RectTransform>();
            rNodRect.anchorMin = Vector2.zero;
            rNodRect.anchorMax = Vector2.one;
            rNodRect.sizeDelta = Vector2.zero;

            var nodeSummary = new GameObject("NodeSummaryText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            nodeSummary.transform.SetParent(rootNodes.transform, false);
            nodeSummary.font = font;
            nodeSummary.text = "Nodes Unlocked: 0 / 6  |  Stat Bonus: +0%";
            nodeSummary.fontSize = 18;
            nodeSummary.fontStyle = FontStyles.Bold;
            nodeSummary.alignment = TextAlignmentOptions.Center;
            nodeSummary.color = new Color(0.95f, 0.75f, 0.3f);
            var nsSummaryRect = nodeSummary.GetComponent<RectTransform>();
            nsSummaryRect.anchorMin = new Vector2(0.5f, 0.95f);
            nsSummaryRect.anchorMax = new Vector2(0.5f, 0.95f);
            nsSummaryRect.pivot = new Vector2(0.5f, 0.5f);
            nsSummaryRect.sizeDelta = new Vector2(600, 30);

            var nodScrollObj = new GameObject("NodesScrollRect", typeof(RectTransform));
            nodScrollObj.transform.SetParent(rootNodes.transform, false);
            var nodScroll = nodScrollObj.AddComponent<ScrollRect>();
            nodScroll.horizontal = false;
            nodScroll.vertical = true;
            var nnsRect = nodScrollObj.GetComponent<RectTransform>();
            nnsRect.anchorMin = new Vector2(0.05f, 0.05f);
            nnsRect.anchorMax = new Vector2(0.95f, 0.90f);
            nnsRect.sizeDelta = Vector2.zero;

            // Viewport
            var nViewObj = new GameObject("Viewport", typeof(RectTransform));
            nViewObj.transform.SetParent(nodScrollObj.transform, false);
            nViewObj.AddComponent<Image>().color = Color.clear;
            nViewObj.AddComponent<Mask>().showMaskGraphic = false;
            var nvRect = nViewObj.GetComponent<RectTransform>();
            nvRect.anchorMin = Vector2.zero;
            nvRect.anchorMax = Vector2.one;
            nvRect.sizeDelta = Vector2.zero;
            nodScroll.viewport = nvRect;

            // Content
            var nContentObj = new GameObject("Content", typeof(RectTransform));
            nContentObj.transform.SetParent(nViewObj.transform, false);
            var nContentLayout = nContentObj.AddComponent<VerticalLayoutGroup>();
            nContentLayout.spacing = 10;
            nContentLayout.padding = new RectOffset(10, 10, 10, 10);
            nContentLayout.childControlHeight = false;
            nContentLayout.childForceExpandHeight = false;
            var ncRect = nContentObj.GetComponent<RectTransform>();
            ncRect.anchorMin = new Vector2(0, 1);
            ncRect.anchorMax = new Vector2(1, 1);
            ncRect.pivot = new Vector2(0.5f, 1f);
            ncRect.sizeDelta = new Vector2(0, 300);
            nodScroll.content = ncRect;
            var ncsf = nContentObj.AddComponent<ContentSizeFitter>();
            ncsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Back click triggers SwitchTab(0)
            backBtn.onClick.AddListener(() =>
            {
                var fullUI = coordinatorInstance;
                if (fullUI != null)
                {
                    fullUI.RequestClose();
                }
            });
        }

        private static GameObject CreateTabBtn(GameObject parent, string text, TMP_FontAsset font)
        {
            var go = new GameObject($"BtnTab_{text}", typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.18f, 0.25f, 0.8f);
            var btn = go.AddComponent<Button>();

            var textObj = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            textObj.transform.SetParent(go.transform, false);
            textObj.font = font;
            textObj.text = text;
            textObj.fontSize = 16;
            textObj.fontStyle = FontStyles.Bold;
            textObj.alignment = TextAlignmentOptions.Center;
            textObj.color = Color.white;
            var tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            return go;
        }

        private static UnitInspectorFullScreenUI coordinatorInstance
        {
            get { return GameObject.FindObjectOfType<UnitInspectorFullScreenUI>(); }
        }

        private static void SetupManagersAndWiring(GameObject inspectorRoot, Transform levelingPage, Transform resonancePage, GameObject memoryPrefab, GameObject nodePrefab)
        {
            var coordinator = inspectorRoot.GetComponent<UnitInspectorFullScreenUI>();
            if (coordinator == null)
            {
                var mgr = GameObject.Find("UnitInspector_Manager");
                if (mgr != null) coordinator = mgr.GetComponent<UnitInspectorFullScreenUI>();
            }

            if (coordinator == null)
            {
                Debug.LogError("[Setup] Failed to locate UnitInspectorFullScreenUI coordinator component.");
                return;
            }

            var soCoord = new SerializedObject(coordinator);

            // Locate manager GameObjects
            var managersRoot = GameObject.Find("Managers/Vassals_Manager/UnitInspector_Manager");
            if (managersRoot == null)
            {
                Debug.LogError("[Setup] 'Managers/Vassals_Manager/UnitInspector_Manager' not found!");
                return;
            }

            // 1. Setup XP Panel
            var xpManager = managersRoot.transform.Find("XP_Manager");
            if (xpManager == null)
            {
                var go = new GameObject("XP_Manager");
                go.transform.SetParent(managersRoot.transform, false);
                xpManager = go.transform;
            }
            var xpPanel = xpManager.GetComponent<UnitInspectorXPPanel>();
            if (xpPanel == null) xpPanel = xpManager.gameObject.AddComponent<UnitInspectorXPPanel>();

            var soXP = new SerializedObject(xpPanel);
            soXP.FindProperty("_duplicatesScrollRect").objectReferenceValue = levelingPage.Find("MainLayout/RightPanel/DuplicatesScrollRect").GetComponent<ScrollRect>();
            soXP.FindProperty("_txtDuplicatesInfo").objectReferenceValue = levelingPage.Find("MainLayout/RightPanel/InfoText").GetComponent<TextMeshProUGUI>();
            soXP.FindProperty("_xpMeterValueText").objectReferenceValue = levelingPage.Find("MainLayout/LeftPanel/XPMeterText").GetComponent<TextMeshProUGUI>();
            soXP.FindProperty("_txtXpGain").objectReferenceValue = levelingPage.Find("MainLayout/LeftPanel/XPGainText").GetComponent<TextMeshProUGUI>();
            soXP.FindProperty("_txtLevelPreview").objectReferenceValue = levelingPage.Find("MainLayout/LeftPanel/LevelPreview/Text").GetComponent<TextMeshProUGUI>();
            soXP.FindProperty("_btnConfirmLevelUp").objectReferenceValue = levelingPage.Find("MainLayout/RightPanel/BottomControls/ConfirmLevelUp_Button").GetComponent<Button>();
            soXP.FindProperty("_btnAutoAdd").objectReferenceValue = levelingPage.Find("MainLayout/RightPanel/BottomControls/AutoAdd_Button").GetComponent<Button>();
            soXP.FindProperty("_autoAddSettingsRoot").objectReferenceValue = levelingPage.Find("MainLayout/RightPanel/BottomControls/AutoAddSettingsRoot").gameObject;
            soXP.FindProperty("_portraitImage").objectReferenceValue = levelingPage.Find("MainLayout/LeftPanel/Portrait_Art").GetComponent<Image>();
            soXP.FindProperty("_xpCurrentFill").objectReferenceValue = levelingPage.Find("MainLayout/LeftPanel/XPBar_Background/XP_Current_Fill").GetComponent<Image>();
            soXP.FindProperty("_xpAddFill").objectReferenceValue = levelingPage.Find("MainLayout/LeftPanel/XPBar_Background/XP_Add_Fill").GetComponent<Image>();
            soXP.ApplyModifiedProperties();

            // 2. Setup Resonance Panel
            var resManager = managersRoot.transform.Find("Resonance_Manager");
            if (resManager == null)
            {
                var go = new GameObject("Resonance_Manager");
                go.transform.SetParent(managersRoot.transform, false);
                resManager = go.transform;
            }
            var resPanel = resManager.GetComponent<UnitInspectorResonancePanel>();
            if (resPanel == null) resPanel = resManager.gameObject.AddComponent<UnitInspectorResonancePanel>();

            var soRes = new SerializedObject(resPanel);
            soRes.FindProperty("_btnTabPromote").objectReferenceValue = resonancePage.Find("SubTabs/BtnTab_PROMOTION").GetComponent<Button>();
            soRes.FindProperty("_btnTabMemories").objectReferenceValue = resonancePage.Find("SubTabs/BtnTab_MEMORIES").GetComponent<Button>();
            soRes.FindProperty("_btnTabNodes").objectReferenceValue = resonancePage.Find("SubTabs/BtnTab_RESONANCE NODES").GetComponent<Button>();

            soRes.FindProperty("_rootPromote").objectReferenceValue = resonancePage.Find("RootsContainer/RootPromote").gameObject;
            soRes.FindProperty("_rootMemories").objectReferenceValue = resonancePage.Find("RootsContainer/RootMemories").gameObject;
            soRes.FindProperty("_rootNodes").objectReferenceValue = resonancePage.Find("RootsContainer/RootNodes").gameObject;

            soRes.FindProperty("_txtCurrentStars").objectReferenceValue = resonancePage.Find("RootsContainer/RootPromote/StarFlow/CurrentStars").GetComponent<RectTransform>();
            soRes.FindProperty("_txtNextStars").objectReferenceValue = resonancePage.Find("RootsContainer/RootPromote/StarFlow/NextStars").GetComponent<RectTransform>();
            soRes.FindProperty("_starFullSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icons/UI_Icon_Star_Full.png");
            soRes.FindProperty("_starEmptySprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icons/UI_Icon_Star_Empty.png");
            soRes.FindProperty("_txtPromoteGoldCost").objectReferenceValue = resonancePage.Find("RootsContainer/RootPromote/GoldCostText").GetComponent<TextMeshProUGUI>();

            soRes.FindProperty("_txtPrimaryMatName").objectReferenceValue = resonancePage.Find("RootsContainer/RootPromote/MaterialGrid/PrimaryMatName").GetComponent<TextMeshProUGUI>();
            soRes.FindProperty("_txtPrimaryMatCount").objectReferenceValue = resonancePage.Find("RootsContainer/RootPromote/MaterialGrid/PrimaryMatCount").GetComponent<TextMeshProUGUI>();
            soRes.FindProperty("_txtSecondaryMatName").objectReferenceValue = resonancePage.Find("RootsContainer/RootPromote/MaterialGrid/SecondaryMatName").GetComponent<TextMeshProUGUI>();
            soRes.FindProperty("_txtSecondaryMatCount").objectReferenceValue = resonancePage.Find("RootsContainer/RootPromote/MaterialGrid/SecondaryMatCount").GetComponent<TextMeshProUGUI>();

            soRes.FindProperty("_btnPromote").objectReferenceValue = resonancePage.Find("RootsContainer/RootPromote/PromoteButton").GetComponent<Button>();
            soRes.FindProperty("_txtPromoteStatus").objectReferenceValue = resonancePage.Find("RootsContainer/RootPromote/PromoteStatusText").GetComponent<TextMeshProUGUI>();

            soRes.FindProperty("_memoriesScrollRect").objectReferenceValue = resonancePage.Find("RootsContainer/RootMemories/MemoriesScrollRect").GetComponent<ScrollRect>();
            soRes.FindProperty("_memoryEntryPrefab").objectReferenceValue = memoryPrefab;

            soRes.FindProperty("_nodesScrollRect").objectReferenceValue = resonancePage.Find("RootsContainer/RootNodes/NodesScrollRect").GetComponent<ScrollRect>();
            soRes.FindProperty("_nodeEntryPrefab").objectReferenceValue = nodePrefab;
            soRes.FindProperty("_txtNodeSummary").objectReferenceValue = resonancePage.Find("RootsContainer/RootNodes/NodeSummaryText").GetComponent<TextMeshProUGUI>();

            soRes.ApplyModifiedProperties();

            // 3. Wire sub-managers back to Coordinator
            soCoord.FindProperty("_xpPanel").objectReferenceValue = xpPanel;
            soCoord.FindProperty("_resonancePanel").objectReferenceValue = resPanel;
            
            // Wire Content Roots
            soCoord.FindProperty("_contentResonance").objectReferenceValue = resonancePage.gameObject;
            soCoord.FindProperty("_contentXP").objectReferenceValue = levelingPage.gameObject; // Tab 4 now sets Leveling Page active!
            
            soCoord.ApplyModifiedProperties();
        }
    }
}
