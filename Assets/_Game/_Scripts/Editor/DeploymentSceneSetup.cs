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
        TextMeshPro tmpro = visuals.AddComponent<TextMeshPro>();

        // Defaults
        tmpro.alignment = TextAlignmentOptions.Center;
        tmpro.fontSize = 5;
        tmpro.text = "?";
        tmpro.rectTransform.sizeDelta = new Vector2(2, 2);

        // Assign refs
        SerializedObject so = new SerializedObject(unit);
        so.FindProperty("_spriteRenderer").objectReferenceValue = sr;
        so.FindProperty("_textRenderer").objectReferenceValue = tmpro;
        so.FindProperty("_billboard").objectReferenceValue = billboard;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = unitObj;
        Debug.Log("Created PlayerUnit Structure.");
    }
}
