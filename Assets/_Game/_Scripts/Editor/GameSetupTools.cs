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
        [MenuItem("Tools/MaouSamaTD/Generate UI Panels (InGame)")]
        public static void GeneratePanels()
        {

            GameControlUI gameUI = FindObjectOfType<GameControlUI>();
            if (gameUI == null)
            {
                Debug.LogError("Could not find GameControlUI in the scene!");
                return;
            }

            Canvas canvas = gameUI.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
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

        [MenuItem("Tools/MaouSamaTD/Generate Cohort & Menu UI")]
        public static void GenerateMenuUI()
        {
             Canvas canvas = FindObjectOfType<Canvas>();
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
             
             // Add CohortSelectionUI
             CohortSelectionUI cs = cohortPanel.GetComponent<CohortSelectionUI>();
             if (cs == null) cs = cohortPanel.AddComponent<CohortSelectionUI>();
             
             // Link References using SerializedObject
             SerializedObject cpSo = new SerializedObject(cp);
             SetProperty(cpSo, "_levelContainer", levelGrid.transform);
             SetProperty(cpSo, "_briefingPanel", bp);
             SetProperty(cpSo, "_cohortSelectionUI", cs);
             cpSo.ApplyModifiedProperties();
             
             SerializedObject bpSo = new SerializedObject(bp);
             SetProperty(bpSo, "_titleText", briefingPanel.transform.Find("Briefing_Title_Text").GetComponent<TextMeshProUGUI>());
             SetProperty(bpSo, "_descriptionText", briefingPanel.transform.Find("Briefing_Desc_Text").GetComponent<TextMeshProUGUI>());
             SetProperty(bpSo, "_rewardValueText", briefingPanel.transform.Find("Briefing_Reward_Value").GetComponent<TextMeshProUGUI>());
             SetProperty(bpSo, "_engageButton", engageBtn);
             bpSo.ApplyModifiedProperties();
             
             SerializedObject csSo = new SerializedObject(cs);
             SetProperty(csSo, "_panel", cohortPanel);
             SetProperty(csSo, "_unitSlotsContainer", slotsContainer.transform);
             SetProperty(csSo, "_startBattleButton", startBattleBtn);
             SetProperty(csSo, "_backButton", backBtn);
             SetProperty(csSo, "_campaignPageObject", campaignPage);
             // csSo.ApplyModifiedProperties(); // Requires _unitSlotPrefab which we can't generate easily, user needs to assign.
             csSo.ApplyModifiedProperties();

             Debug.Log("Generated Menu UI Structures.");
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
    }
}
