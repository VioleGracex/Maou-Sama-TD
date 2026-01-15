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

            GameObject textObj = new GameObject("SealsText");
            textObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 28;
            txt.color = Color.white;
            txt.text = "AUTHORITY SEALS\n30";
            txt.alignment = TextAlignmentOptions.Center;
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.sizeDelta = Vector2.zero;
            
            // Assign to Manager via SerializedObject to avoid dirty scene issues
            SerializedObject so = new SerializedObject(uiManager);
            so.FindProperty("_authoritySealsText").objectReferenceValue = txt;
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

        Debug.Log("Deployment UI Configured (TMP + Images).");
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
        so.ApplyModifiedProperties();

        Selection.activeGameObject = btnObj;
        Debug.Log("Created Unit Button Prefab Structure (Icon, Name, Cost, Class, Cooldown).");
    }
}
