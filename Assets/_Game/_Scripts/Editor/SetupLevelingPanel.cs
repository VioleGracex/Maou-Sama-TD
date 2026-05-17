using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using MaouSamaTD.UI;
using MaouSamaTD.Data;

public static class SetupLevelingPanel
{
    [MenuItem("Tools/Setup Leveling Panel")]
    public static void Run()
    {
        // 1. Create LevelingCardUI Prefab
        GameObject root = new GameObject("LevelingCardPrefab");
        var rect = root.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 220);
        
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        var cardBtn = root.AddComponent<Button>();
        
        var ui = root.AddComponent<LevelingCardUI>();
        ui.BackgroundImage = bg;
        ui.CardButton = cardBtn;

        // Icon
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(root.transform, false);
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.2f, 0.4f);
        iconRect.anchorMax = new Vector2(0.8f, 0.9f);
        iconRect.sizeDelta = Vector2.zero;
        var iconImg = iconObj.AddComponent<Image>();
        ui.IconImage = iconImg;

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(root.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.25f);
        titleRect.anchorMax = new Vector2(1, 0.35f);
        titleRect.sizeDelta = Vector2.zero;
        var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        titleTxt.fontSize = 18;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;
        ui.TitleText = titleTxt;

        // AvailableTxt
        var availObj = new GameObject("AvailableText");
        availObj.transform.SetParent(root.transform, false);
        var availRect = availObj.AddComponent<RectTransform>();
        availRect.anchorMin = new Vector2(0, 0);
        availRect.anchorMax = new Vector2(1, 0.2f);
        availRect.sizeDelta = Vector2.zero;
        var availTxt = availObj.AddComponent<TextMeshProUGUI>();
        availTxt.fontSize = 16;
        availTxt.alignment = TextAlignmentOptions.Center;
        ui.AvailableText = availTxt;

        // SelOverlay
        var selOverlayObj = new GameObject("SelectedOverlay");
        selOverlayObj.transform.SetParent(root.transform, false);
        var selOverlayRect = selOverlayObj.AddComponent<RectTransform>();
        selOverlayRect.anchorMin = Vector2.zero;
        selOverlayRect.anchorMax = Vector2.one;
        selOverlayRect.sizeDelta = Vector2.zero;
        var selImg = selOverlayObj.AddComponent<Image>();
        selImg.color = new Color(0, 1, 0, 0.3f);
        ui.SelectedOverlay = selOverlayObj;
        
        var selTxtObj = new GameObject("SelectedText");
        selTxtObj.transform.SetParent(selOverlayObj.transform, false);
        var selTxtRect = selTxtObj.AddComponent<RectTransform>();
        selTxtRect.anchorMin = Vector2.zero;
        selTxtRect.anchorMax = Vector2.one;
        selTxtRect.sizeDelta = Vector2.zero;
        var selTxt = selTxtObj.AddComponent<TextMeshProUGUI>();
        selTxt.fontSize = 50;
        selTxt.fontStyle = FontStyles.Bold;
        selTxt.alignment = TextAlignmentOptions.Center;
        selTxt.color = Color.white;
        ui.SelectedText = selTxt;

        // Minus Btn
        var minusObj = new GameObject("MinusBtn");
        minusObj.transform.SetParent(root.transform, false);
        var minusRect = minusObj.AddComponent<RectTransform>();
        minusRect.anchorMin = new Vector2(0.7f, 0.7f);
        minusRect.anchorMax = new Vector2(1, 1);
        minusRect.sizeDelta = Vector2.zero;
        var minusBg = minusObj.AddComponent<Image>();
        minusBg.color = new Color(1, 0, 0, 0.8f);
        var minusBtn = minusObj.AddComponent<Button>();
        
        var minusTxtObj = new GameObject("Text");
        minusTxtObj.transform.SetParent(minusObj.transform, false);
        var mtRect = minusTxtObj.AddComponent<RectTransform>();
        mtRect.anchorMin = Vector2.zero;
        mtRect.anchorMax = Vector2.one;
        mtRect.sizeDelta = Vector2.zero;
        var mtTxt = minusTxtObj.AddComponent<TextMeshProUGUI>();
        mtTxt.text = "-";
        mtTxt.fontSize = 30;
        mtTxt.alignment = TextAlignmentOptions.Center;
        
        ui.MinusButtonObj = minusObj;
        ui.MinusButton = minusBtn;

        // Hide overlay and minus
        selOverlayObj.SetActive(false);
        minusObj.SetActive(false);

        // Save Prefab
        if (!AssetDatabase.IsValidFolder("Assets/_Game/Art/UI"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Art"))
                AssetDatabase.CreateFolder("Assets/_Game", "Art");
            AssetDatabase.CreateFolder("Assets/_Game/Art", "UI");
        }
        
        string prefabPath = "Assets/_Game/Art/UI/LevelingCardPrefab.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        GameObject.DestroyImmediate(root);

        // 2. Load Item Configs
        ItemConfigSO[] configs = new ItemConfigSO[4];
        configs[0] = AssetDatabase.LoadAssetAtPath<ItemConfigSO>("Assets/_Game/Data/Items/xp_core_common.asset");
        configs[1] = AssetDatabase.LoadAssetAtPath<ItemConfigSO>("Assets/_Game/Data/Items/xp_core_rare.asset");
        configs[2] = AssetDatabase.LoadAssetAtPath<ItemConfigSO>("Assets/_Game/Data/Items/xp_core_epic.asset");
        configs[3] = AssetDatabase.LoadAssetAtPath<ItemConfigSO>("Assets/_Game/Data/Items/xp_core_legendary.asset");

        // 3. Find Panel and Assign
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/Home_New.unity");
        UnitInspectorXPPanel panel = GameObject.FindObjectOfType<UnitInspectorXPPanel>(true);
        if (panel != null)
        {
            var so = new SerializedObject(panel);
            so.FindProperty("_levelingCardPrefab").objectReferenceValue = savedPrefab;
            
            var configsProp = so.FindProperty("_xpCoreConfigs");
            configsProp.arraySize = 4;
            configsProp.GetArrayElementAtIndex(0).objectReferenceValue = configs[0];
            configsProp.GetArrayElementAtIndex(1).objectReferenceValue = configs[1];
            configsProp.GetArrayElementAtIndex(2).objectReferenceValue = configs[2];
            configsProp.GetArrayElementAtIndex(3).objectReferenceValue = configs[3];
            so.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("Successfully assigned leveling prefab and configs to UnitInspectorXPPanel!");
        }
        else
        {
            Debug.LogError("Could not find UnitInspectorXPPanel in Home_New scene!");
        }
    }
}
