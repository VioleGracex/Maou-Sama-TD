using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class CreateLootSequencePrefab
{
    [MenuItem("Tools/Antigravity/Create Loot Sequence Prefab")]
    public static void CreatePrefab()
    {
        GameObject _lootSequencePanel = new GameObject("LootSequencePanel");
        var lootRect = _lootSequencePanel.AddComponent<RectTransform>();
        lootRect.anchorMin = Vector2.zero; lootRect.anchorMax = Vector2.one;
        lootRect.sizeDelta = Vector2.zero;

        var visualRoot = new GameObject("VisualRoot");
        visualRoot.transform.SetParent(_lootSequencePanel.transform, false);
        var vrRect = visualRoot.AddComponent<RectTransform>();
        vrRect.anchorMin = Vector2.zero; vrRect.anchorMax = Vector2.one;
        vrRect.sizeDelta = Vector2.zero;

        // Dark blurred background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(visualRoot.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        var lootBg = bgObj.AddComponent<Image>();
        var defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (defaultSprite != null) lootBg.sprite = defaultSprite;
        lootBg.type = Image.Type.Sliced;
        lootBg.color = new Color(0.02f, 0.02f, 0.06f, 0.92f);

        // Background button to continue (should not block scroll view)
        var bgBtnObj = new GameObject("BackgroundButton");
        bgBtnObj.transform.SetParent(visualRoot.transform, false);
        var bgBtnRect = bgBtnObj.AddComponent<RectTransform>();
        bgBtnRect.anchorMin = Vector2.zero; bgBtnRect.anchorMax = Vector2.one;
        bgBtnRect.sizeDelta = Vector2.zero;
        var bgImg = bgBtnObj.AddComponent<Image>();
        bgImg.color = new Color(1, 1, 1, 0); // Transparent
        var bgBtn = bgBtnObj.AddComponent<Button>();

        // --- MVP Portrait (left side, behind banner) ---
        var mvpObj = new GameObject("MVPPortrait");
        mvpObj.transform.SetParent(visualRoot.transform, false);
        var _mvpPortrait = mvpObj.AddComponent<Image>();
        _mvpPortrait.preserveAspect = true;
        var mvpRect = _mvpPortrait.GetComponent<RectTransform>();
        // Portrait on left half
        mvpRect.anchorMin = new Vector2(-0.1f, 0.0f); mvpRect.anchorMax = new Vector2(0.5f, 1.0f);
        mvpRect.sizeDelta = Vector2.zero;

        // --- Banner Background (spans width, overlays MVP) ---
        var bannerObj = new GameObject("BannerBackground");
        bannerObj.transform.SetParent(_lootSequencePanel.transform, false);
        var bannerBgImg = bannerObj.AddComponent<Image>();
        bannerBgImg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f); // Dark banner like Arknights
        var bannerRect = bannerObj.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.0f, 0.15f); bannerRect.anchorMax = new Vector2(1.0f, 0.45f);
        bannerRect.sizeDelta = Vector2.zero;

        var leftPanel = new GameObject("LeftPanel");
        leftPanel.transform.SetParent(bannerObj.transform, false);
        var leftRect = leftPanel.AddComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0.05f, 0.05f); leftRect.anchorMax = new Vector2(0.35f, 0.95f);
        leftRect.sizeDelta = Vector2.zero;

        var levelText = new GameObject("LevelText").AddComponent<TextMeshProUGUI>();
        levelText.transform.SetParent(leftPanel.transform, false);
        levelText.text = "LEVEL 1\n<size=50%>Operation</size>";
        levelText.fontSize = 50;
        levelText.fontStyle = FontStyles.Bold;
        levelText.alignment = TextAlignmentOptions.BottomLeft;
        levelText.color = Color.white;
        var levelRect = levelText.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0, 0.5f); levelRect.anchorMax = new Vector2(1, 1f);
        levelRect.sizeDelta = Vector2.zero;

        var resultsText = new GameObject("ResultsText").AddComponent<TextMeshProUGUI>();
        resultsText.transform.SetParent(leftPanel.transform, false);
        resultsText.text = "Results";
        resultsText.fontSize = 60;
        resultsText.fontStyle = FontStyles.Bold;
        resultsText.alignment = TextAlignmentOptions.TopLeft;
        resultsText.color = Color.white;
        var resultsRect = resultsText.GetComponent<RectTransform>();
        resultsRect.anchorMin = new Vector2(0, 0f); resultsRect.anchorMax = new Vector2(1, 0.5f);
        resultsRect.sizeDelta = Vector2.zero;

        // --- Separator ---
        var separatorObj = new GameObject("Separator");
        separatorObj.transform.SetParent(bannerObj.transform, false);
        var separatorImg = separatorObj.AddComponent<Image>();
        separatorImg.color = new Color(1, 1, 1, 0.3f);
        var sepRect = separatorImg.GetComponent<RectTransform>();
        sepRect.anchorMin = new Vector2(0.38f, 0.2f); sepRect.anchorMax = new Vector2(0.385f, 0.8f);
        sepRect.sizeDelta = Vector2.zero;

        // --- Right Side (Horizontal ScrollView for Loot) ---
        var scrollViewObj = new GameObject("ScrollView");
        scrollViewObj.transform.SetParent(bannerObj.transform, false);
        var scrollRectTransform = scrollViewObj.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0.42f, 0.1f); scrollRectTransform.anchorMax = new Vector2(0.98f, 0.9f);
        scrollRectTransform.sizeDelta = Vector2.zero;
        var scrollRect = scrollViewObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        
        // Viewport
        var viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollViewObj.transform, false);
        var viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero; viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.pivot = new Vector2(0, 0.5f);
        var viewportMask = viewportObj.AddComponent<RectMask2D>(); // Mask items outside

        // Content (Loot Grid)
        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0.1f); contentRect.anchorMax = new Vector2(0, 0.9f);
        contentRect.pivot = new Vector2(0, 0.5f);
        contentRect.sizeDelta = new Vector2(0, 0);
        
        var layoutGroup = contentObj.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = 20;
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = false; // We set explicit width on children or use preferred
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = true;

        var sizeFitter = contentObj.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Assign to ScrollRect
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;

        // Tap prompt (bottom center)
        var lootPrompt = new GameObject("Prompt").AddComponent<TextMeshProUGUI>();
        lootPrompt.transform.SetParent(_lootSequencePanel.transform, false);
        lootPrompt.text = "Tap to continue...";
        lootPrompt.fontSize = 26;
        lootPrompt.alignment = TextAlignmentOptions.Center;
        lootPrompt.color = new Color(1, 1, 1, 0.4f);
        var lootPromptRect = lootPrompt.GetComponent<RectTransform>();
        lootPromptRect.anchorMin = new Vector2(0, 0.02f); lootPromptRect.anchorMax = new Vector2(1, 0.1f);
        lootPromptRect.sizeDelta = Vector2.zero;

        // Create LootCard Prefab
        var cardPrefabObj = CreateLootCardPrefab();
        
        // Add Placeholders for Visualization
        for (int i = 0; i < 4; i++)
        {
            var placeholder = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefabObj, contentRect.transform);
            var phUI = placeholder.GetComponent<MaouSamaTD.UI.LootCardUI>();
            if (phUI != null)
            {
                phUI.NameText.text = "ITEM " + (i + 1);
                phUI.QtyText.text = "x" + Random.Range(1, 10);
                phUI.IconImage.color = new Color(Random.value, Random.value, Random.value);
            }
        }
        
        // Set MVP Placeholder
        defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (defaultSprite != null) _mvpPortrait.sprite = defaultSprite;
        _mvpPortrait.color = new Color(1, 1, 1, 0.5f);

        // Save as Prefab
        if (!System.IO.Directory.Exists("Assets/_Game/Prefabs/UI"))
        {
            System.IO.Directory.CreateDirectory("Assets/_Game/Prefabs/UI");
        }
        
        Selection.activeObject = null;
        PrefabUtility.SaveAsPrefabAsset(_lootSequencePanel, "Assets/_Game/Prefabs/UI/LootSequencePanel.prefab");
        
        var panelToDestroy = _lootSequencePanel;
        EditorApplication.delayCall += () => 
        {
            if (panelToDestroy != null) GameObject.DestroyImmediate(panelToDestroy);
        };
        
        Debug.Log("LootSequencePanel prefab updated to match reference at Assets/_Game/Prefabs/UI/LootSequencePanel.prefab");
    }

    private static GameObject CreateLootCardPrefab()
    {
        var cardObj = new GameObject("LootCard");
        var cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(160, 200);
        
        var cardBg = cardObj.AddComponent<Image>();
        cardBg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
        
        var layoutElement = cardObj.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 160;
        layoutElement.preferredHeight = 200;

        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(cardObj.transform, false);
        var iconImg = iconObj.AddComponent<Image>();
        var iconRect = iconImg.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f); 
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0, -10f);
        iconRect.sizeDelta = new Vector2(120f, 120f);
        
        var nameText = new GameObject("NameText").AddComponent<TextMeshProUGUI>();
        nameText.transform.SetParent(cardObj.transform, false);
        nameText.text = "ITEM NAME";
        nameText.fontSize = 12;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 8;
        nameText.fontSizeMax = 12;
        var nameRect = nameText.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0f); nameRect.anchorMax = new Vector2(1, 0f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.sizeDelta = new Vector2(-10f, 35f);
        nameRect.anchoredPosition = new Vector2(0, 35f);

        var qtyText = new GameObject("QtyText").AddComponent<TextMeshProUGUI>();
        qtyText.transform.SetParent(cardObj.transform, false);
        qtyText.text = "x1";
        qtyText.fontSize = 18;
        qtyText.fontStyle = FontStyles.Bold;
        qtyText.alignment = TextAlignmentOptions.Center;
        var qtyRect = qtyText.GetComponent<RectTransform>();
        qtyRect.anchorMin = new Vector2(0, 0f); qtyRect.anchorMax = new Vector2(1, 0f);
        qtyRect.pivot = new Vector2(0.5f, 0f);
        qtyRect.sizeDelta = new Vector2(-10f, 35f);
        qtyRect.anchoredPosition = new Vector2(0, 0f);

        var cardUI = cardObj.AddComponent<MaouSamaTD.UI.LootCardUI>();
        cardUI.BackgroundImage = cardBg;
        cardUI.IconImage = iconImg;
        cardUI.NameText = nameText;
        cardUI.QtyText = qtyText;

        if (!System.IO.Directory.Exists("Assets/_Game/Prefabs/UI"))
            System.IO.Directory.CreateDirectory("Assets/_Game/Prefabs/UI");
            
        Selection.activeObject = null;
        var prefab = PrefabUtility.SaveAsPrefabAsset(cardObj, "Assets/_Game/Prefabs/UI/LootCard.prefab");
        
        var cardToDestroy = cardObj;
        EditorApplication.delayCall += () => 
        {
            if (cardToDestroy != null) GameObject.DestroyImmediate(cardToDestroy);
        };
        
        return prefab;
    }
}
