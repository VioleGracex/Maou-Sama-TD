using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.UI;
using MaouSamaTD.UI.MainMenu;

namespace MaouSamaTD.Editor
{
    public class GameSetupTools : EditorWindow
    {
        [MenuItem("Tools/Maou Sama TD/Generate UI Panels (InGame)")]
        public static void GeneratePanels()
        {

            GameControlUI gameUI = FindFirstObjectByType<GameControlUI>();
            if (gameUI == null)
            {
                Debug.LogError("Could not find GameControlUI in the scene!");
                return;
            }

            Canvas canvas = gameUI.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }
            
            if (canvas == null)
            {
                Debug.LogError("No Canvas found! Please create a Canvas first.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(gameUI, "Setup UI Panels");

            // 1. Create Panels
            GameObject victoryPanel = GetOrCreatePanel(canvas.transform, "VictoryPanel", new Color(0, 0.5f, 0, 0.9f));
            GameObject losePanel = GetOrCreatePanel(canvas.transform, "LosePanel", new Color(0.5f, 0, 0, 0.9f));
            GameObject pausePanel = GetOrCreatePanel(canvas.transform, "PausePanel", new Color(0, 0, 0, 0.8f));
            GameObject confirmPanel = GetOrCreatePanel(canvas.transform, "ConfirmationPanel", new Color(0.2f, 0.2f, 0.2f, 0.95f));

            // 2. Victory Content
            CreateText(victoryPanel.transform, "Title", "VICTORY!", 0, 100);
            Button winKy = CreateButton(victoryPanel.transform, "RestartButton", "Restart", -100, -50);
            Button winReturn = CreateButton(victoryPanel.transform, "ReturnButton", "Menu", 100, -50);


            // 3. Lose Content
            CreateText(losePanel.transform, "Title", "GAME OVER", 0, 100);
            Button loseKy = CreateButton(losePanel.transform, "RestartButton", "Try Again", -100, -50);
            Button loseReturn = CreateButton(losePanel.transform, "ReturnButton", "Menu", 100, -50);


            // 4. Pause Content
            CreateText(pausePanel.transform, "Title", "PAUSED", 0, 150);
            Button resumeBtn = CreateButton(pausePanel.transform, "ResumeButton", "Resume", 0, 50);
            Button retreatBtn = CreateButton(pausePanel.transform, "RetreatButton", "Retreat", 0, -50);

            // 5. Confirm Content
            CreateText(confirmPanel.transform, "Title", "Are you sure?", 0, 50);
            Button yesBtn = CreateButton(confirmPanel.transform, "YesButton", "Yes", -100, -50);
            Button noBtn = CreateButton(confirmPanel.transform, "NoButton", "No", 100, -50);

            // 6. Assign to GameControlUI
            SerializedObject so = new SerializedObject(gameUI);
            
            SetProperty(so, "_winPanel", victoryPanel);
            SetProperty(so, "_losePanel", losePanel);
            SetProperty(so, "_pauseOverlay", pausePanel);
            SetProperty(so, "_confirmationPanel", confirmPanel);

            SetProperty(so, "_resumeButton", resumeBtn);
            SetProperty(so, "_retreatButton", retreatBtn);
            
            SetProperty(so, "_confirmYesButton", yesBtn);
            SetProperty(so, "_confirmNoButton", noBtn);

            SetProperty(so, "_winRestartButton", winKy);
            SetProperty(so, "_loseRestartButton", loseKy);
            
            SetProperty(so, "_winReturnButton", winReturn);
            SetProperty(so, "_loseReturnButton", loseReturn);


            so.ApplyModifiedProperties();
            
            // Hide them by default
            victoryPanel.SetActive(false);
            losePanel.SetActive(false);
            pausePanel.SetActive(false);
            confirmPanel.SetActive(false);

            Debug.Log("UI Panels Generated and Assigned successfully!");
        }

        private static GameObject GetOrCreatePanel(Transform parent, string name, Color color)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = panel.AddComponent<Image>();
            img.color = color;

            Undo.RegisterCreatedObjectUndo(panel, "Create Panel");
            return panel;
        }

        [MenuItem("Tools/Maou Sama TD/Generate Cohort & Menu UI")]
        public static void GenerateMenuUI()
        {
             Canvas canvas = FindFirstObjectByType<Canvas>();
             if (canvas == null)
             {
                 Debug.LogError("No Canvas found!");
                 return;
             }
             
             // 1. Campaign Page (Parent)
             GameObject campaignPage = GetOrCreatePanel(canvas.transform, "Campaign_Page", new Color(0.1f, 0.1f, 0.1f, 1f));
             
             // Level Container (Scroll View usually, but for now a simple grid Grid)
             GameObject levelGrid = new GameObject("LevelGrid");
             levelGrid.transform.SetParent(campaignPage.transform, false);
             RectTransform gridRt = levelGrid.AddComponent<RectTransform>();
             gridRt.anchorMin = new Vector2(0.1f, 0.2f);
             gridRt.anchorMax = new Vector2(0.9f, 0.8f);
             gridRt.offsetMin = Vector2.zero;
             gridRt.offsetMax = Vector2.zero;
             GridLayoutGroup glg = levelGrid.AddComponent<GridLayoutGroup>();
             glg.cellSize = new Vector2(600, 100);
             glg.spacing = new Vector2(0, 20);
             glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
             glg.constraintCount = 1;
             
             // 2. Briefing Panel
             GameObject briefingPanel = GetOrCreatePanel(canvas.transform, "Briefing_Panel", new Color(0, 0, 0, 0.95f));
             // Hierarchy from User Request: BriefingIcon, BriefingBox, Separator, Start_Stage_Btn (Engage) -> we simplify to Text/Button
             CreateText(briefingPanel.transform, "Briefing_Title_Text", "Mission Title", -200, 200);
             CreateText(briefingPanel.transform, "Briefing_Desc_Text", "Mission Description...", 0, 50);
             CreateText(briefingPanel.transform, "Briefing_Reward_Value", "1000", 200, -100);
             Button engageBtn = CreateButton(briefingPanel.transform, "Start_Stage_Btn", "ENGAGE COHORT", 0, -200);
             
             // 3. Cohort Selection
             GameObject cohortPanel = GetOrCreatePanel(canvas.transform, "Cohort_Selection_Panel", new Color(0.05f, 0.05f, 0.05f, 1f));
             CreateText(cohortPanel.transform, "Title", "CHOOSE YOUR COHORT", 0, 300);
             GameObject slotsContainer = new GameObject("UnitSlots");
             slotsContainer.transform.SetParent(cohortPanel.transform, false);
             RectTransform slotsRt = slotsContainer.AddComponent<RectTransform>();
             slotsRt.anchoredPosition = Vector2.zero; // Center
             GridLayoutGroup slotGrid = slotsContainer.AddComponent<GridLayoutGroup>();
             slotGrid.cellSize = new Vector2(100, 100);
             slotGrid.spacing = new Vector2(20, 20);
             
             Button startBattleBtn = CreateButton(cohortPanel.transform, "StartBattleButton", "DEPLOY", 300, -300);
             Button backBtn = CreateButton(cohortPanel.transform, "BackButton", "CANCEL", -300, -300);
             
             // 4. Assign Components if they exist on the objects or add them
             // Add CampaignPage component to the parent if missing
             CampaignPage cp = campaignPage.GetComponent<CampaignPage>();
             if (cp == null) cp = campaignPage.AddComponent<CampaignPage>();
             
             // Add BriefingPanel
             BriefingPanel bp = briefingPanel.GetComponent<BriefingPanel>();
             if (bp == null) bp = briefingPanel.AddComponent<BriefingPanel>();
             
             // Link References using SerializedObject
             SerializedObject cpSo = new SerializedObject(cp);
             SetProperty(cpSo, "_levelContainer", levelGrid.transform);
             SetProperty(cpSo, "_briefingPanel", bp);
             // Removed CohortSelectionUI reference
             cpSo.ApplyModifiedProperties();
             
             SerializedObject bpSo = new SerializedObject(bp);
             SetProperty(bpSo, "_titleText", briefingPanel.transform.Find("Briefing_Title_Text").GetComponent<TextMeshProUGUI>());
             SetProperty(bpSo, "_descriptionText", briefingPanel.transform.Find("Briefing_Desc_Text").GetComponent<TextMeshProUGUI>());
             SetProperty(bpSo, "_rewardValueText", briefingPanel.transform.Find("Briefing_Reward_Value").GetComponent<TextMeshProUGUI>());
             SetProperty(bpSo, "_engageButton", engageBtn);
             bpSo.ApplyModifiedProperties();
             
             // CohortSelection properties removed
             
             Debug.Log("Generated Menu UI Structures.");
        }

        [MenuItem("Tools/Maou Sama TD/UI Components/Generate Unit Card Prefab")]
        public static void GenerateUnitCardPrefab()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            GameObject cardRoot = new GameObject("UnitCard_Prefab");
            if (canvas) cardRoot.transform.SetParent(canvas.transform, false);

            RectTransform rootRt = cardRoot.AddComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(120, 180); // Portrait size

            Image bg = cardRoot.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f);

            Button btn = cardRoot.AddComponent<Button>();
            bg.raycastTarget = true;

            // 1. Portrait (Fill)
            GameObject portrait = new GameObject("Portrait");
            portrait.transform.SetParent(cardRoot.transform, false);
            RectTransform pRt = portrait.AddComponent<RectTransform>();
            pRt.anchorMin = Vector2.zero;
            pRt.anchorMax = Vector2.one;
            pRt.offsetMin = new Vector2(5, 5);
            pRt.offsetMax = new Vector2(-5, -40); // Leave room for name at bottom
            
            Image pImg = portrait.AddComponent<Image>();
            pImg.color = Color.gray;
            pImg.preserveAspect = true;

            // 2. Name Bar
            GameObject nameBar = new GameObject("NameBar");
            nameBar.transform.SetParent(cardRoot.transform, false);
            RectTransform nRt = nameBar.AddComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0, 0); // Bottom
            nRt.anchorMax = new Vector2(1, 0.2f); // Bottom 20%
            nRt.offsetMin = Vector2.zero;
            nRt.offsetMax = Vector2.zero;
            
            Image nBg = nameBar.AddComponent<Image>();
            nBg.color = new Color(0,0,0,0.8f);

            GameObject nameTextObj = new GameObject("NameText");
            nameTextObj.transform.SetParent(nameBar.transform, false);
            TextMeshProUGUI nameTmp = nameTextObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "Unit Name";
            nameTmp.fontSize = 18;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = Color.white;
            
            // 3. Selection Overlay (Hidden by default)
            GameObject overlay = new GameObject("SelectionOverlay");
            overlay.transform.SetParent(cardRoot.transform, false);
            RectTransform oRt = overlay.AddComponent<RectTransform>();
            oRt.anchorMin = Vector2.zero;
            oRt.anchorMax = Vector2.one;
            
            Image oImg = overlay.AddComponent<Image>();
            oImg.color = new Color(0, 0.5f, 1f, 0.3f); // Blue tint
            
            GameObject numberObj = new GameObject("OrderNumber");
            numberObj.transform.SetParent(overlay.transform, false);
            TextMeshProUGUI numTmp = numberObj.AddComponent<TextMeshProUGUI>();
            numTmp.text = "1";
            numTmp.fontSize = 64;
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.color = Color.cyan;
            
            overlay.SetActive(false); // Hide initially

            // 4. Component
            UnitCardUI cardUI = cardRoot.AddComponent<UnitCardUI>();
            SerializedObject so = new SerializedObject(cardUI);
            SetProperty(so, "_portraitImage", pImg);
            SetProperty(so, "_nameText", nameTmp);
            SetProperty(so, "_selectedOverlay", overlay);
            SetProperty(so, "_selectionOrderText", numTmp);
            so.ApplyModifiedProperties();

            Debug.Log("Generated Unit Card Prefab Structure in Scene. Drag to Prefabs folder!");
        }

        private static void CreateText(Transform parent, string name, string content, float x, float y)

        {
            if (parent.Find(name)) return;

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(400, 100);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = 48;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        [MenuItem("Tools/Maou Sama TD/UI Components/Generate Squad Panel (12 Slots)")]
        public static void GenerateSquadPanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (!canvas) return;

            GameObject panel = GetOrCreatePanel(canvas.transform, "Squad_Panel", new Color(0.1f, 0.1f, 0.1f, 0.8f));
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.8f);
            rt.anchorMax = new Vector2(1, 1); // Top 20%
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            GameObject gridObj = new GameObject("SquadSlots_Grid");
            gridObj.transform.SetParent(panel.transform, false);
            RectTransform gridRt = gridObj.AddComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0.05f, 0.1f);
            gridRt.anchorMax = new Vector2(0.95f, 0.9f);
            
            GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(80, 80);
            glg.spacing = new Vector2(10, 0);
            glg.childAlignment = TextAnchor.MiddleCenter;
            
            Debug.Log("Generated Squad Panel.");
        }

        [MenuItem("Tools/Maou Sama TD/UI Components/Generate Unit Inventory (Scroll)")]
        public static void GenerateInventoryPanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (!canvas) return;

            GameObject scrollObj = GetOrCreatePanel(canvas.transform, "Unit_Inventory_Scroll", new Color(0.2f, 0.2f, 0.2f, 0.5f));
            RectTransform rt = scrollObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.3f, 0); // Right side 70%
            rt.anchorMax = new Vector2(1, 0.8f);
            
            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            viewport.AddComponent<RectMask2D>();
            RectTransform vRt = viewport.AddComponent<RectTransform>();
            vRt.anchorMin = Vector2.zero;
            vRt.anchorMax = Vector2.one;
            vRt.sizeDelta = Vector2.zero;
            
            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1);
            
            GridLayoutGroup glg = content.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(100, 150); // Portrait aspect
            glg.spacing = new Vector2(10, 10);
            glg.constraint = GridLayoutGroup.Constraint.Flexible;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = cRt;
            sr.viewport = vRt;

            Debug.Log("Generated Inventory Scroll View.");
        }

        [MenuItem("Tools/Maou Sama TD/UI Components/Generate Stats Panel (Sidebar)")]
        public static void GenerateStatsPanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (!canvas) return;

            GameObject panel = GetOrCreatePanel(canvas.transform, "Unit_Stats_Panel", new Color(0.1f, 0.15f, 0.2f, 0.95f));
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0.3f, 0.8f); // Left Sidebar
            
            UnitStatsPanel stats = panel.AddComponent<UnitStatsPanel>();

            VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 5;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childAlignment = TextAnchor.UpperLeft;

            // Generate & Link
            SerializedObject so = new SerializedObject(stats);

            // Helper local func
            TextMeshProUGUI AddStatText(string objName, string defaultText)
            {
                CreateText(panel.transform, objName, defaultText, 0, 0);
                return panel.transform.Find(objName).GetComponent<TextMeshProUGUI>();
            }

            SetProperty(so, "_unitNameText", AddStatText("NameText", "UNIT NAME"));
            SetProperty(so, "_levelText", AddStatText("LevelText", "Lv. 1"));
            
            // Stats Block
            SetProperty(so, "_hpText", AddStatText("HP_Text", "HP: 100"));
            SetProperty(so, "_atkText", AddStatText("ATK_Text", "ATK: 50"));
            SetProperty(so, "_defText", AddStatText("DEF_Text", "DEF: 10"));
            SetProperty(so, "_costText", AddStatText("Cost_Text", "Cost: 15"));
            SetProperty(so, "_blockText", AddStatText("Block_Text", "Block: 1"));
            SetProperty(so, "_respawnText", AddStatText("Respawn_Text", "Respawn: 10s"));
            
            SetProperty(so, "_skillNameText", AddStatText("SkillName", "Skill: None"));
            SetProperty(so, "_skillDescText", AddStatText("SkillDesc", "Description..."));

            so.ApplyModifiedProperties();

            Debug.Log("Generated Stats Sidebar with linked references.");
        }

        [MenuItem("Tools/Maou Sama TD/UI Components/Generate Filter Bar")]
        public static void GenerateFilterBar()
        {
             Canvas canvas = FindFirstObjectByType<Canvas>();
             if (!canvas) return;
             
             GameObject bar = GetOrCreatePanel(canvas.transform, "Filter_Bar", new Color(0,0,0,0.8f));
             RectTransform rt = bar.GetComponent<RectTransform>();
             rt.anchorMin = new Vector2(0.3f, 0.8f); // Below Squad, Above Inventory
             rt.anchorMax = new Vector2(1, 0.9f);
             
             HorizontalLayoutGroup hlg = bar.AddComponent<HorizontalLayoutGroup>();
             hlg.spacing = 20;
             hlg.childAlignment = TextAnchor.MiddleLeft;
             hlg.padding = new RectOffset(20,0,0,0);
             
             CreateButton(bar.transform, "Filter_Rarity", "Rarity", 0, 0);
             CreateButton(bar.transform, "Filter_Class", "Class", 0, 0);
             
             Debug.Log("Generated Filter Bar.");
        }

        private static Button CreateButton(Transform parent, string name, string label, float x, float y)
        {
            Transform existing = parent.Find(name);
            if (existing) return existing.GetComponent<Button>();

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(160, 50);

            Image img = go.AddComponent<Image>();
            img.color = Color.white;

            Button btn = go.AddComponent<Button>();
            
            // Text Label
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(go.transform, false);
            RectTransform txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;

            return btn;
        }

        private static void SetProperty(SerializedObject so, string propName, Object value)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }
        [MenuItem("Tools/Maou Sama TD/Create Wall Material", false, 52)]
        public static void CreateWallMaterial()
        {
            string path = "Assets/_Game/Art/Materials/GeneratedWall.mat";
            
            // Ensure directory
            System.IO.Directory.CreateDirectory("Assets/_Game/Art/Materials");
            
            // Find a standard shader (URP or Standard)
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            
            Material mat = new Material(shader);
            mat.color = Color.gray;
            
            // Check if exists
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            
            Selection.activeObject = mat;
            EditorGUIUtility.PingObject(mat);
            
            Debug.Log($"Created Wall Material at {path}. Please assign this to your GridGenerator.");
        }

        [MenuItem("Tools/Maou Sama TD/Create Path Material", false, 53)]
        public static void CreatePathMaterial()
        {
            string path = "Assets/_Game/Art/Materials/GeneratedPath.mat";
            
            // Ensure directory
            System.IO.Directory.CreateDirectory("Assets/_Game/Art/Materials");
            
            // Find Particles/Additive
            Shader shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null) shader = Shader.Find("Particles/Additive");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            
            Material mat = new Material(shader);
            
            // Check if exists
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            
            Selection.activeObject = mat;
            EditorGUIUtility.PingObject(mat);
            
            Debug.Log($"Created Path Material at {path}. Please assign this to your PathVisualizer.");
        }
    }
}
