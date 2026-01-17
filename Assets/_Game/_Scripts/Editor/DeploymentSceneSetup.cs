using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using MaouSamaTD.UI;
using MaouSamaTD.Managers;
using TMPro;

public class DeploymentSceneSetup : Editor
{
    [MenuItem("Tools/MaouSamaTD/Setup Deployment UI")]
    public static void SetupUI()
    {
        // 1. Ensure Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MainCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Canvas Scaler Configuration
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
            
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        // 2. DeploymentUI Manager
        DeploymentUI uiManager = FindObjectOfType<DeploymentUI>();
        if (uiManager == null)
        {
             GameObject uiObj = new GameObject("DeploymentUI");
             uiManager = uiObj.AddComponent<DeploymentUI>();
             uiObj.transform.SetParent(canvas.transform, false);
        }

        // 3. Authority Seals Text & Panel
        if (uiManager.transform.Find("AuthorityPanel") == null)
        {
            GameObject panel = new GameObject("AuthorityPanel");
            panel.transform.SetParent(uiManager.transform, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(50, -50); // Adjusted padding
            rect.sizeDelta = new Vector2(250, 100);

            // Add Image background
            Image img = panel.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.8f);

            GameObject textObj = new GameObject("SealsTitle");
            textObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 20; // Slightly smaller title
            txt.color = Color.white;
            txt.text = "AUTHORITY SEALS";
            txt.rectTransform.anchorMin = new Vector2(0, 0.5f);
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.sizeDelta = Vector2.zero;
            txt.rectTransform.offsetMin = new Vector2(0, 0); 
            txt.rectTransform.offsetMax = new Vector2(0, -10);

            // Value Text (The one we update)
            GameObject valObj = new GameObject("SealsValue");
            valObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI valTxt = valObj.AddComponent<TextMeshProUGUI>();
            valTxt.fontSize = 32; // Bigger number
            valTxt.color = Color.yellow;
            valTxt.text = "30";
            valTxt.alignment = TextAlignmentOptions.Center;
            valTxt.rectTransform.anchorMin = Vector2.zero;
            valTxt.rectTransform.anchorMax = new Vector2(1, 0.6f);
            valTxt.rectTransform.sizeDelta = Vector2.zero;
            
            // Assign to Manager via SerializedObject to avoid dirty scene issues
            SerializedObject so = new SerializedObject(uiManager);
            so.FindProperty("_authoritySealsText").objectReferenceValue = valTxt;
            so.ApplyModifiedProperties();
        }

        // 4. Bottom Bar
         if (uiManager.transform.Find("UnitBar") == null)
        {
            GameObject bar = new GameObject("UnitBar");
            bar.transform.SetParent(uiManager.transform, false);
            RectTransform rect = bar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 50); // Lowered slightly
            rect.sizeDelta = new Vector2(900, 160);

            // Background Image
            Image img = bar.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
            
            bar.AddComponent<HorizontalLayoutGroup>();
            
            SerializedObject so = new SerializedObject(uiManager);
            so.FindProperty("_barContainer").objectReferenceValue = bar.transform;
            so.ApplyModifiedProperties();
        }

        // 5. Connect GameManager
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
             SerializedObject so = new SerializedObject(gm);
             so.FindProperty("_deploymentUI").objectReferenceValue = uiManager;
             so.ApplyModifiedProperties();
        }

        // 6. Unit Inspector UI
        UnitInspectorUI inspector = FindObjectOfType<UnitInspectorUI>();
        if (inspector == null)
        {
            GameObject inspObj = new GameObject("UnitInspector");
            inspObj.transform.SetParent(canvas.transform, false);
            inspector = inspObj.AddComponent<UnitInspectorUI>();
        }
        
        // Ensure Inspector Panel visuals
        if (inspector.transform.Find("InspectorPanel") == null)
        {
            GameObject panel = new GameObject("InspectorPanel");
            panel.transform.SetParent(inspector.transform, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0); // Bottom Right
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-20, 180); // Above unit bar? Or side?
            // Unit bar is at bottom center. Let's put inspector at Right Side? 
            rect.sizeDelta = new Vector2(300, 450);

            // BG
            Image img = panel.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // 1. Stats_Unit_Name_Txt
            GameObject nameObj = new GameObject("Stats_Unit_Name_Txt");
            nameObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
            nameTxt.text = "Unit Name";
            nameTxt.fontSize = 24;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.alignment = TextAlignmentOptions.Center;
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.9f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = new Vector2(0, -10);

            // 2. Vassal_Stats_Txt
            GameObject vassalStatsObj = new GameObject("Vassal_Stats_Txt");
            vassalStatsObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI vassalStatsTxt = vassalStatsObj.AddComponent<TextMeshProUGUI>();
            vassalStatsTxt.text = "Stats Summary";
            vassalStatsTxt.fontSize = 18;
            vassalStatsTxt.alignment = TextAlignmentOptions.TopLeft;
            RectTransform vsRect = vassalStatsObj.GetComponent<RectTransform>();
            vsRect.anchorMin = new Vector2(0.05f, 0.65f);
            vsRect.anchorMax = new Vector2(0.95f, 0.85f);
            vsRect.offsetMin = Vector2.zero;
            vsRect.offsetMax = Vector2.zero;

            // 3. Stats_Vitality_Txt (Label)
            GameObject vitObj = new GameObject("Stats_Vitality_Txt");
            vitObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI vitTxt = vitObj.AddComponent<TextMeshProUGUI>();
            vitTxt.text = "Vitality";
            vitTxt.fontSize = 16;
            vitTxt.color = Color.green;
            RectTransform vitRect = vitObj.GetComponent<RectTransform>();
            vitRect.anchorMin = new Vector2(0.05f, 0.6f);
            vitRect.anchorMax = new Vector2(0.5f, 0.65f);
            vitRect.offsetMin = Vector2.zero;
            vitRect.offsetMax = Vector2.zero;

            // 4. Stats_HPBar
            GameObject hpBarObj = new GameObject("Stats_HPBar");
            hpBarObj.transform.SetParent(panel.transform, false);
            Image hpBarBg = hpBarObj.AddComponent<Image>(); // BG for bar
            hpBarBg.color = Color.gray;
            RectTransform hpBarRect = hpBarObj.GetComponent<RectTransform>();
            hpBarRect.anchorMin = new Vector2(0.05f, 0.55f);
            hpBarRect.anchorMax = new Vector2(0.95f, 0.58f);
            hpBarRect.offsetMin = Vector2.zero;
            hpBarRect.offsetMax = Vector2.zero;
            
            // Should contain Fill Child, but for now we use this image as fill or bg?
            // Usually Bar has BG + Fill. Assume this is the Fill for simplicity based on naming?
            // "Stats_HPBar" sounds like the object itself. Let's make it the Fill Image directly
            // or add a child. Let's make it the Fill Image.
            hpBarBg.type = Image.Type.Filled;
            hpBarBg.fillMethod = Image.FillMethod.Horizontal;
            hpBarBg.color = Color.green;

            // 5. Stats_HP_Number_Txt
            GameObject hpNumObj = new GameObject("Stats_HP_Number_Txt");
            hpNumObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI hpNumTxt = hpNumObj.AddComponent<TextMeshProUGUI>();
            hpNumTxt.text = "100/100";
            hpNumTxt.fontSize = 16;
            hpNumTxt.alignment = TextAlignmentOptions.Right;
            RectTransform hpNumRect = hpNumObj.GetComponent<RectTransform>();
            hpNumRect.anchorMin = new Vector2(0.5f, 0.6f);
            hpNumRect.anchorMax = new Vector2(0.95f, 0.65f);
            hpNumRect.offsetMin = Vector2.zero;
            hpNumRect.offsetMax = Vector2.zero;

            // 6. LayoutStats (Container)
            GameObject layoutObj = new GameObject("LayoutStats");
            layoutObj.transform.SetParent(panel.transform, false);
            RectTransform layoutRect = layoutObj.AddComponent<RectTransform>();
            layoutRect.anchorMin = new Vector2(0.05f, 0.35f);
            layoutRect.anchorMax = new Vector2(0.95f, 0.5f);
            layoutRect.offsetMin = Vector2.zero;
            layoutRect.offsetMax = Vector2.zero;
            // Add Vertical Layout
            VerticalLayoutGroup vlg = layoutObj.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.spacing = 5;

            // 6a. Stats_Dmg_BG
            GameObject dmgBgObj = new GameObject("Stats_Dmg_BG");
            dmgBgObj.transform.SetParent(layoutObj.transform, false);
            Image dmgBg = dmgBgObj.AddComponent<Image>();
            dmgBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GameObject dmgTxtObj = new GameObject("Text");
            dmgTxtObj.transform.SetParent(dmgBgObj.transform, false);
            TextMeshProUGUI dmgTxt = dmgTxtObj.AddComponent<TextMeshProUGUI>();
            dmgTxt.text = "ATK: 10";
            dmgTxt.alignment = TextAlignmentOptions.Center;
            dmgTxt.fontSize = 18;
            dmgTxt.rectTransform.anchorMin = Vector2.zero;
            dmgTxt.rectTransform.anchorMax = Vector2.one;
            dmgTxt.rectTransform.sizeDelta = Vector2.zero;

            // 6b. Stats_Range_BG
            GameObject rngBgObj = new GameObject("Stats_Range_BG");
            rngBgObj.transform.SetParent(layoutObj.transform, false);
            Image rngBg = rngBgObj.AddComponent<Image>();
            rngBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GameObject rngTxtObj = new GameObject("Text");
            rngTxtObj.transform.SetParent(rngBgObj.transform, false);
            TextMeshProUGUI rngTxt = rngTxtObj.AddComponent<TextMeshProUGUI>();
            rngTxt.text = "RNG: 1";
            rngTxt.alignment = TextAlignmentOptions.Center;
            rngTxt.fontSize = 18;
            rngTxt.rectTransform.anchorMin = Vector2.zero;
            rngTxt.rectTransform.anchorMax = Vector2.one;
            rngTxt.rectTransform.sizeDelta = Vector2.zero;

            // 7. Ult_Btn
            GameObject ultBtnObj = new GameObject("Ult_Btn");
            ultBtnObj.transform.SetParent(panel.transform, false);
            Image ultBg = ultBtnObj.AddComponent<Image>();
            ultBg.color = Color.cyan;
            Button ultBtn = ultBtnObj.AddComponent<Button>();
            RectTransform ultRect = ultBtnObj.GetComponent<RectTransform>();
            ultRect.anchorMin = new Vector2(0.3f, 0.05f);
            ultRect.anchorMax = new Vector2(0.7f, 0.25f);
            ultRect.offsetMin = Vector2.zero;
            ultRect.offsetMax = Vector2.zero;

            // Ult Icon child
            GameObject ultIconObj = new GameObject("Icon");
            ultIconObj.transform.SetParent(ultBtnObj.transform, false);
            Image ultIcon = ultIconObj.AddComponent<Image>();
            RectTransform ultIconRect = ultIconObj.GetComponent<RectTransform>();
            ultIconRect.anchorMin = Vector2.zero;
            ultIconRect.anchorMax = Vector2.one;
            ultIconRect.offsetMin = new Vector2(5,5);
            ultIconRect.offsetMax = new Vector2(-5,-5);
            ultIcon.preserveAspect = true;

            // 7b. Charge Fill (Overlay/Replacement when not ready)
            GameObject ultChargeObj = new GameObject("Ult_Charge_Fill");
            ultChargeObj.transform.SetParent(panel.transform, false); // Same parent as Button
            Image ultChargeImg = ultChargeObj.AddComponent<Image>();
            ultChargeImg.color = new Color(1f, 1f, 0f, 0.5f); // Yellowish semi-transparent
            ultChargeImg.type = Image.Type.Filled;
            ultChargeImg.fillMethod = Image.FillMethod.Radial360; 
            RectTransform ultChargeRect = ultChargeObj.GetComponent<RectTransform>();
            // Copy Ult Button Rect
            ultChargeRect.anchorMin = new Vector2(0.3f, 0.05f);
            ultChargeRect.anchorMax = new Vector2(0.7f, 0.25f);
            ultChargeRect.offsetMin = Vector2.zero;
            ultChargeRect.offsetMax = Vector2.zero;

            // 7c. Charge Label
            GameObject ultLabelObj = new GameObject("Ult_Charge_Label");
            ultLabelObj.transform.SetParent(ultChargeObj.transform, false);
            TextMeshProUGUI ultLabel = ultLabelObj.AddComponent<TextMeshProUGUI>();
            ultLabel.text = "0%";
            ultLabel.alignment = TextAlignmentOptions.Center;
            ultLabel.fontSize = 20;
            ultLabel.color = Color.black;
            ultLabel.rectTransform.anchorMin = Vector2.zero;
            ultLabel.rectTransform.anchorMax = Vector2.one;
            ultLabel.rectTransform.sizeDelta = Vector2.zero;

            // Keep Retreat Button (Not in hierarchy image but needed)
            GameObject retreatBtnObj = new GameObject("RetreatButton");
            retreatBtnObj.transform.SetParent(panel.transform, false);
            Image retBg = retreatBtnObj.AddComponent<Image>();
            retBg.color = Color.red;
            Button retBtn = retreatBtnObj.AddComponent<Button>();
            RectTransform retRect = retreatBtnObj.GetComponent<RectTransform>();
            retRect.anchorMin = new Vector2(0.75f, 0.05f);
            retRect.anchorMax = new Vector2(0.95f, 0.15f); // Bottom right corner small?
            retRect.offsetMin = Vector2.zero;
            retRect.offsetMax = Vector2.zero;
             
            GameObject retTxtObj = new GameObject("Text");
            retTxtObj.transform.SetParent(retreatBtnObj.transform, false);
            TextMeshProUGUI retTxt = retTxtObj.AddComponent<TextMeshProUGUI>();
            retTxt.text = "R";
            retTxt.alignment = TextAlignmentOptions.Center;
            retTxt.rectTransform.anchorMin = Vector2.zero;
            retTxt.rectTransform.anchorMax = Vector2.one;
            retTxt.rectTransform.sizeDelta = Vector2.zero;


            // Close Button (Overlay on Panel)
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(panel.transform, false);
            Image closeBg = closeBtnObj.AddComponent<Image>();
            closeBg.color = Color.black; 
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            RectTransform closeRect = closeBtnObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.sizeDelta = new Vector2(30, 30);
            closeRect.anchoredPosition = new Vector2(-5, -5);
            
            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            TextMeshProUGUI closeTxt = closeTxtObj.AddComponent<TextMeshProUGUI>();
            closeTxt.text = "X";
            closeTxt.color = Color.white;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.rectTransform.anchorMin = Vector2.zero;
            closeTxt.rectTransform.anchorMax = Vector2.one;
            closeTxt.rectTransform.sizeDelta = Vector2.zero;


            // Assign Refs
            SerializedObject so = new SerializedObject(inspector);
            so.FindProperty("_panel").objectReferenceValue = panel;
            so.FindProperty("_unitNameText").objectReferenceValue = nameTxt;
            so.FindProperty("_vassalStatsText").objectReferenceValue = vassalStatsTxt;
            so.FindProperty("_vitalityLabelText").objectReferenceValue = vitTxt;
            so.FindProperty("_hpNumberText").objectReferenceValue = hpNumTxt;
            so.FindProperty("_hpBarImage").objectReferenceValue = hpBarBg;
            
            // New fields
            so.FindProperty("_dmgText").objectReferenceValue = dmgTxt;
            so.FindProperty("_rangeText").objectReferenceValue = rngTxt;
            
            so.FindProperty("_ultButton").objectReferenceValue = ultBtn;
            so.FindProperty("_ultChargeFill").objectReferenceValue = ultChargeImg;
            so.FindProperty("_ultChargeLabel").objectReferenceValue = ultLabel;
            
            so.FindProperty("_retreatButton").objectReferenceValue = retBtn;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtn;
            so.ApplyModifiedProperties();
        }

        Debug.Log("Deployment UI Configured (TMP + Images + Inspector).");
    }

    [MenuItem("Tools/MaouSamaTD/Create Unit Prefab")]
    public static void CreateUnitPrefab()
    {
        GameObject unitObj = new GameObject("PlayerUnit");
        MaouSamaTD.Units.PlayerUnit unit = unitObj.AddComponent<MaouSamaTD.Units.PlayerUnit>();
        
        BoxCollider box = unitObj.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0, 1, 0);
        box.size = new Vector3(1, 2, 1);

        GameObject visuals = new GameObject("Visuals");
        visuals.transform.SetParent(unitObj.transform, false);
        visuals.transform.localPosition = Vector3.up;

        var billboard = visuals.AddComponent<MaouSamaTD.Utils.Billboard>();
        SpriteRenderer sr = visuals.AddComponent<SpriteRenderer>();
        
        // World Space Canvas for HP
        GameObject canvasObj = new GameObject("UnitCanvas");
        canvasObj.transform.SetParent(visuals.transform, false);
        canvasObj.transform.localPosition = new Vector3(0, 0, 0); 
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Scale down significantly for World Space 1 unit = 1 meter
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1, 0.2f); // Width 1, Height 0.2
        canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 1f); // Adjust scale logic? Or keep resolution high.
        // Actually, usually World Space canvases are set to size like (100, 20) then scale 0.01.
        canvasRect.sizeDelta = new Vector2(100, 20);
        
        // Background
        GameObject bgObj = new GameObject("HP_BG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = Color.black;
        bgObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        bgObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
        bgObj.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        // Fill
        GameObject fillObj = new GameObject("HP_Fill");
        fillObj.transform.SetParent(canvasObj.transform, false);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = Color.green;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        fillObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
        fillObj.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        // Assign refs
        SerializedObject so = new SerializedObject(unit);
        so.FindProperty("_spriteRenderer").objectReferenceValue = sr;
        so.FindProperty("_hpBarFill").objectReferenceValue = fillImg;
        so.FindProperty("_billboard").objectReferenceValue = billboard;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = unitObj;
        Debug.Log("Created PlayerUnit Structure with HP Bar.");
    }

    [MenuItem("Tools/MaouSamaTD/Create Unit Button Prefab")]
    public static void CreateUnitButtonPrefab()
    {
        GameObject btnObj = new GameObject("UnitButton", typeof(RectTransform));
        
        // Components
        Image bgImg = btnObj.AddComponent<Image>(); // Background
        Button btn = btnObj.AddComponent<Button>();
        
        // Sizing (Default for a button in the bar)
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 140);

        // 1. Unit Icon (Center)
        GameObject iconObj = new GameObject("UnitIcon");
        iconObj.transform.SetParent(btnObj.transform, false);
        Image iconImg = iconObj.AddComponent<Image>();
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.3f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        // Make sure it preserves aspect ratio usually
        iconImg.preserveAspect = true;

        // 2. Class Icon (Top Left Corner)
        GameObject classObj = new GameObject("ClassIcon");
        classObj.transform.SetParent(btnObj.transform, false);
        Image classImg = classObj.AddComponent<Image>();
        RectTransform classRect = classObj.GetComponent<RectTransform>();
        classRect.anchorMin = new Vector2(0, 1);
        classRect.anchorMax = new Vector2(0, 1);
        classRect.pivot = new Vector2(0, 1);
        classRect.sizeDelta = new Vector2(30, 30);
        classRect.anchoredPosition = new Vector2(5, -5);

        // 3. Name Text (Top Center/Overlay?)
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
        nameTxt.alignment = TextAlignmentOptions.Top;
        nameTxt.fontSize = 18;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.text = "Unit Name";
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.85f);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        // 4. Cost Text (Bottom)
        GameObject costObj = new GameObject("CostText");
        costObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI costTxt = costObj.AddComponent<TextMeshProUGUI>();
        costTxt.alignment = TextAlignmentOptions.Center;
        costTxt.fontSize = 24;
        costTxt.color = Color.yellow;
        costTxt.text = "10";
        RectTransform costRect = costObj.GetComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0, 0);
        costRect.anchorMax = new Vector2(1, 0.25f);
        costRect.offsetMin = Vector2.zero;
        costRect.offsetMax = Vector2.zero;

        // 5. Cooldown Overlay (Full Stretch)
        GameObject cdObj = new GameObject("CooldownOverlay");
        cdObj.transform.SetParent(btnObj.transform, false);
        Image cdImg = cdObj.AddComponent<Image>();
        cdImg.color = new Color(0, 0, 0, 0.7f); // Dark overlay
        cdImg.type = Image.Type.Filled;
        cdImg.fillMethod = Image.FillMethod.Vertical;
        cdImg.fillOrigin = (int)Image.OriginVertical.Top; // Unfills from Bottom to Top if decreasing
        // cdImg.fillClockwise = false; // Not applicable for Vertical
        
        // User: "unfills amount 1 if unfilled cooldown is done". 
        // Usually fillAmount = progress (1 -> 0). So if progress is 1 (start), it's full. 
        // As it goes to 0, it unfills.
        cdImg.fillAmount = 0; // Start hidden
        
        RectTransform cdRect = cdObj.GetComponent<RectTransform>();
        cdRect.anchorMin = Vector2.zero;
        cdRect.anchorMax = Vector2.one;
        cdRect.offsetMin = Vector2.zero;
        cdRect.offsetMax = Vector2.zero;

        // Add UnitButtonUI
        UnitButtonUI btnUI = btnObj.AddComponent<UnitButtonUI>();
        // Assign refs via SerializedObject to be safe
        SerializedObject so = new SerializedObject(btnUI);
        so.FindProperty("_background").objectReferenceValue = bgImg;
        so.FindProperty("_unitIcon").objectReferenceValue = iconImg;
        so.FindProperty("_classIcon").objectReferenceValue = classImg;
        so.FindProperty("_nameText").objectReferenceValue = nameTxt;
        so.FindProperty("_costText").objectReferenceValue = costTxt;
        so.FindProperty("_cooldownOverlay").objectReferenceValue = cdImg;
        so.FindProperty("_button").objectReferenceValue = btn;
        
        // Add DragHandler
        btnObj.AddComponent<UnitDragHandler>();
        
        so.ApplyModifiedProperties();

        Selection.activeGameObject = btnObj;
        Debug.Log("Created Unit Button Prefab Structure (Icon, Name, Cost, Class, Cooldown).");
    }
}
