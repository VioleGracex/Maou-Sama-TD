using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class CreateXPSequencePrefab
{
    [MenuItem("Tools/Antigravity/Create XP Sequence Prefab")]
    public static void CreatePrefab()
    {
        var _xpSequencePanel = new GameObject("XPSequencePanel");
        var xpRect = _xpSequencePanel.AddComponent<RectTransform>();
        xpRect.anchorMin = Vector2.zero; xpRect.anchorMax = Vector2.one;
        xpRect.sizeDelta = Vector2.zero;
        
        var visualRoot = new GameObject("VisualRoot");
        visualRoot.transform.SetParent(_xpSequencePanel.transform, false);
        var vrRect = visualRoot.AddComponent<RectTransform>();
        vrRect.anchorMin = Vector2.zero; vrRect.anchorMax = Vector2.one;
        vrRect.sizeDelta = Vector2.zero;

        // Dark blurred background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(visualRoot.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        var xpBg = bgObj.AddComponent<Image>();
        var defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (defaultSprite != null) xpBg.sprite = defaultSprite;
        xpBg.type = Image.Type.Sliced;
        xpBg.color = new Color(0.02f, 0.02f, 0.06f, 0.92f);

        // Click to continue button
        var xpBtn = visualRoot.AddComponent<Button>();
        var xpBtnColors = xpBtn.colors;
        xpBtnColors.normalColor = new Color(1, 1, 1, 0);
        xpBtnColors.highlightedColor = new Color(1, 1, 1, 0);
        xpBtnColors.pressedColor = new Color(1, 1, 1, 0);
        xpBtn.colors = xpBtnColors;
        
        // Ensure button uses a transparent target graphic to catch clicks
        var btnGraphic = visualRoot.AddComponent<Image>();
        btnGraphic.color = new Color(1, 1, 1, 0);
        xpBtn.targetGraphic = btnGraphic;

        // Title
        var xpTitle = new GameObject("Title").AddComponent<TextMeshProUGUI>();
        xpTitle.transform.SetParent(_xpSequencePanel.transform, false);
        xpTitle.text = "COHORT EXPERIENCE";
        xpTitle.fontSize = 48;
        xpTitle.fontStyle = FontStyles.Bold;
        xpTitle.alignment = TextAlignmentOptions.Center;
        xpTitle.color = new Color(1f, 0.8f, 0.2f);
        xpTitle.enableAutoSizing = true;
        xpTitle.fontSizeMin = 24;
        xpTitle.fontSizeMax = 48;
        var xpTitleRect = xpTitle.GetComponent<RectTransform>();
        xpTitleRect.anchorMin = new Vector2(0.1f, 0.85f); xpTitleRect.anchorMax = new Vector2(0.9f, 0.95f);
        xpTitleRect.sizeDelta = Vector2.zero;

        // Decorative line under title
        var lineObj = new GameObject("TitleLine", typeof(RectTransform), typeof(Image));
        lineObj.transform.SetParent(visualRoot.transform, false);
        lineObj.GetComponent<Image>().color = new Color(1f, 0.8f, 0.2f, 0.4f);
        var lineRect = lineObj.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.2f, 0.845f); lineRect.anchorMax = new Vector2(0.8f, 0.85f);
        lineRect.sizeDelta = Vector2.zero;

        // XP Grid Container (Horizontal scroll view essentially)
        var gridObj = new GameObject("XPGrid");
        gridObj.transform.SetParent(visualRoot.transform, false);
        var _xpSequenceGrid = gridObj.AddComponent<RectTransform>();
        _xpSequenceGrid.anchorMin = new Vector2(0.1f, 0.1f); _xpSequenceGrid.anchorMax = new Vector2(0.9f, 0.8f);
        _xpSequenceGrid.sizeDelta = Vector2.zero;
        
        var layoutGroup = gridObj.AddComponent<GridLayoutGroup>();
        layoutGroup.cellSize = new Vector2(280, 90);
        layoutGroup.spacing = new Vector2(20, 15);
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;

        // Tap prompt (bottom center)
        var xpPrompt = new GameObject("Prompt").AddComponent<TextMeshProUGUI>();
        xpPrompt.transform.SetParent(visualRoot.transform, false);
        xpPrompt.text = "Tap to view loot...";
        xpPrompt.fontSize = 26;
        xpPrompt.alignment = TextAlignmentOptions.Center;
        xpPrompt.color = new Color(1, 1, 1, 0.4f);
        var xpPromptRect = xpPrompt.GetComponent<RectTransform>();
        xpPromptRect.anchorMin = new Vector2(0, 0.02f); xpPromptRect.anchorMax = new Vector2(1, 0.1f);
        xpPromptRect.sizeDelta = Vector2.zero;

        // Add Placeholders
        var xpCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/Battle/XPCard.prefab");
        if (xpCardPrefab == null)
            xpCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/XPCard.prefab"); // Fallback

        if (xpCardPrefab != null)
        {
            for (int i = 0; i < 4; i++)
            {
                var placeholder = (GameObject)PrefabUtility.InstantiatePrefab(xpCardPrefab, _xpSequenceGrid);
            }
        }
        else
        {
            Debug.LogWarning("XPCard.prefab not found at Assets/_Game/Prefabs/UI/Battle/XPCard.prefab. Could not spawn placeholders.");
        }

        if (!System.IO.Directory.Exists("Assets/_Game/Prefabs/UI"))
            System.IO.Directory.CreateDirectory("Assets/_Game/Prefabs/UI");
            
        Selection.activeObject = null;
        PrefabUtility.SaveAsPrefabAsset(_xpSequencePanel, "Assets/_Game/Prefabs/UI/XPSequencePanel.prefab");
        
        var panelToDestroy = _xpSequencePanel;
        EditorApplication.delayCall += () => 
        {
            if (panelToDestroy != null) GameObject.DestroyImmediate(panelToDestroy);
        };
        
        Debug.Log("XPSequencePanel prefab created successfully!");
    }
}
