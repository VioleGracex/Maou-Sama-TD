#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.UI.MainMenu;

public class CreateBriefingPrefabs
{
    [MenuItem("Maou-TD/UI/Create Briefing Prefabs")]
    public static void CreatePrefabs()
    {
        string prefabsPath = "Assets/_Game/Prefabs/UI/campaign/";
        if (!AssetDatabase.IsValidFolder(prefabsPath.TrimEnd('/')))
        {
            Debug.LogError($"Path does not exist: {prefabsPath}");
            return;
        }

        // 1. Create BriefingSeparator_Prefab
        CreateSeparatorPrefab(prefabsPath);

        // 2. Modify RewardItem_Prefab
        ModifyRewardItemPrefab(prefabsPath);

        // 3. Create MonsterCard_Prefab
        CreateMonsterCardPrefab(prefabsPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Briefing Prefabs generated and modified successfully!");
    }

    private static void CreateSeparatorPrefab(string path)
    {
        string prefabPath = path + "BriefingSeparator_Prefab.prefab";
        
        GameObject sepGo = new GameObject("BriefingSeparator", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var rect = sepGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 2);
        
        var img = sepGo.GetComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.35f, 0.5f); // Subtle greyish blue line
        
        var le = sepGo.GetComponent<LayoutElement>();
        le.preferredHeight = 2f;
        le.minHeight = 2f;

        PrefabUtility.SaveAsPrefabAsset(sepGo, prefabPath);
        GameObject.DestroyImmediate(sepGo);
    }

    private static void ModifyRewardItemPrefab(string path)
    {
        string prefabPath = path + "RewardItem_Prefab.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("RewardItem_Prefab not found at " + prefabPath);
            return;
        }

        string tempPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab);
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(tempPath))
        {
            GameObject root = editingScope.prefabContentsRoot;
            
            // Find Quantity text
            var quantityText = root.GetComponentInChildren<TextMeshProUGUI>();
            if (quantityText != null)
            {
                quantityText.enableWordWrapping = false;
                quantityText.text = "999"; // Placeholder
            }
            
            // Find Icon and set placeholder
            // We just leave the icon as it is, maybe set color to yellow so it's visible if sprite is null
            var iconImg = root.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImg != null && iconImg.sprite == null)
            {
                iconImg.color = new Color(1f, 0.8f, 0.2f, 1f); // Gold color placeholder
            }
        }
    }

    private static void CreateMonsterCardPrefab(string path)
    {
        string prefabPath = path + "MonsterCard_Prefab.prefab";
        
        GameObject cardGo = new GameObject("MonsterCard_Prefab", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(MonsterCardUI));
        
        var cardRect = cardGo.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(0f, 75f);
        
        var cardLayoutElement = cardGo.GetComponent<LayoutElement>();
        cardLayoutElement.preferredHeight = 75f;
        cardLayoutElement.minHeight = 75f;
        
        var cardImg = cardGo.GetComponent<Image>();
        cardImg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);
        
        var cardOutline = cardGo.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.92f, 0.3f, 0.29f, 0.35f);
        cardOutline.effectDistance = new Vector2(1f, 1f);
        
        var cardLayout = cardGo.AddComponent<HorizontalLayoutGroup>();
        cardLayout.spacing = 12f;
        cardLayout.padding = new RectOffset(10, 10, 8, 8);
        cardLayout.childAlignment = TextAnchor.MiddleLeft;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = false;
        cardLayout.childForceExpandHeight = false;

        // Circular Chibi Image Frame
        GameObject chibiFrameGo = new GameObject("ChibiFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        chibiFrameGo.transform.SetParent(cardGo.transform, false);
        var fRect = chibiFrameGo.GetComponent<RectTransform>();
        fRect.sizeDelta = new Vector2(50f, 50f);
        
        var frameLayoutElement = chibiFrameGo.GetComponent<LayoutElement>();
        frameLayoutElement.preferredWidth = 50f;
        frameLayoutElement.preferredHeight = 50f;
        frameLayoutElement.minWidth = 50f;
        frameLayoutElement.minHeight = 50f;
        
        var fImg = chibiFrameGo.GetComponent<Image>();
        fImg.color = new Color(0.08f, 0.08f, 0.1f, 1f);
        var fOutline = chibiFrameGo.AddComponent<Outline>();
        fOutline.effectColor = new Color(0.97f, 0.79f, 0.14f, 0.4f);
        fOutline.effectDistance = new Vector2(1f, 1f);

        GameObject chibiGo = new GameObject("Chibi", typeof(RectTransform), typeof(Image));
        chibiGo.transform.SetParent(chibiFrameGo.transform, false);
        var chibiRect = chibiGo.GetComponent<RectTransform>();
        chibiRect.anchorMin = Vector2.zero;
        chibiRect.anchorMax = Vector2.one;
        chibiRect.sizeDelta = Vector2.zero;
        var chibiImg = chibiGo.GetComponent<Image>();
        chibiImg.color = new Color(0.3f, 0f, 0.3f, 1f); // Purple placeholder
        chibiImg.preserveAspect = true;
        
        // Text & Tactical Info Container
        GameObject infoGo = new GameObject("InfoContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        infoGo.transform.SetParent(cardGo.transform, false);
        var infoRect = infoGo.GetComponent<RectTransform>();
        infoRect.sizeDelta = new Vector2(240f, 55f);
        
        var infoLayoutElement = infoGo.GetComponent<LayoutElement>();
        infoLayoutElement.preferredWidth = 240f;
        infoLayoutElement.flexibleWidth = 1f;
        
        var infoLayout = infoGo.GetComponent<VerticalLayoutGroup>();
        infoLayout.spacing = 2f;
        infoLayout.childAlignment = TextAnchor.MiddleLeft;
        infoLayout.childControlWidth = true;
        infoLayout.childControlHeight = true;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = false;

        // Title Line
        GameObject titleLineGo = new GameObject("TitleLine", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        titleLineGo.transform.SetParent(infoGo.transform, false);
        var tlRect = titleLineGo.GetComponent<RectTransform>();
        tlRect.sizeDelta = new Vector2(0f, 18f);

        var tlLayout = titleLineGo.GetComponent<HorizontalLayoutGroup>();
        tlLayout.spacing = 6f;
        tlLayout.childAlignment = TextAnchor.MiddleLeft;
        tlLayout.childControlWidth = true;
        tlLayout.childControlHeight = true;
        tlLayout.childForceExpandWidth = false;
        tlLayout.childForceExpandHeight = false;

        // Enemy Name
        GameObject nameGo = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGo.transform.SetParent(titleLineGo.transform, false);
        var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
        nameTmp.text = "Lesser Shadow";
        nameTmp.fontSize = 16f;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = Color.white;
        nameTmp.enableWordWrapping = false;

        // Rank Badge Container
        GameObject rankBadgeGo = new GameObject("RankBadge", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rankBadgeGo.transform.SetParent(titleLineGo.transform, false);
        
        var rankImg = rankBadgeGo.GetComponent<Image>();
        rankImg.color = new Color(0.73f, 0.73f, 0.73f, 0.1f);
        var rankOutline = rankBadgeGo.GetComponent<Outline>();
        rankOutline.effectColor = new Color(0.73f, 0.73f, 0.73f, 0.2f);
        rankOutline.effectDistance = new Vector2(1f, 1f);

        var rankHlg = rankBadgeGo.GetComponent<HorizontalLayoutGroup>();
        rankHlg.spacing = 0f;
        rankHlg.padding = new RectOffset(6, 6, 2, 2);
        rankHlg.childAlignment = TextAnchor.MiddleCenter;
        rankHlg.childControlWidth = true;
        rankHlg.childControlHeight = true;
        rankHlg.childForceExpandWidth = false;
        rankHlg.childForceExpandHeight = false;

        GameObject rankTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        rankTextGo.transform.SetParent(rankBadgeGo.transform, false);
        var rankTmp = rankTextGo.GetComponent<TextMeshProUGUI>();
        rankTmp.text = "NORMAL";
        rankTmp.fontSize = 11.5f;
        rankTmp.fontStyle = FontStyles.Bold;
        rankTmp.alignment = TextAlignmentOptions.Center;
        rankTmp.enableWordWrapping = false;

        // Movement badge Container
        GameObject moveBadgeGo = new GameObject("MoveBadge", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        moveBadgeGo.transform.SetParent(titleLineGo.transform, false);

        var moveImg = moveBadgeGo.GetComponent<Image>();
        moveImg.color = new Color(0f, 1f, 0.8f, 0.1f);
        var moveOutline = moveBadgeGo.GetComponent<Outline>();
        moveOutline.effectColor = new Color(0f, 1f, 0.8f, 0.2f);
        moveOutline.effectDistance = new Vector2(1f, 1f);

        var moveHlg = moveBadgeGo.GetComponent<HorizontalLayoutGroup>();
        moveHlg.spacing = 0f;
        moveHlg.padding = new RectOffset(6, 6, 2, 2);
        moveHlg.childAlignment = TextAnchor.MiddleCenter;
        moveHlg.childControlWidth = true;
        moveHlg.childControlHeight = true;
        moveHlg.childForceExpandWidth = false;
        moveHlg.childForceExpandHeight = false;

        GameObject moveTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        moveTextGo.transform.SetParent(moveBadgeGo.transform, false);
        var moveTmp = moveTextGo.GetComponent<TextMeshProUGUI>();
        moveTmp.text = "GROUND";
        moveTmp.fontSize = 11.5f;
        moveTmp.fontStyle = FontStyles.Bold;
        moveTmp.alignment = TextAlignmentOptions.Center;
        moveTmp.enableWordWrapping = false;

        // Stats Line
        GameObject statsGo = new GameObject("StatsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statsGo.transform.SetParent(infoGo.transform, false);
        var statsTmp = statsGo.GetComponent<TextMeshProUGUI>();
        statsTmp.text = "<color=#888888>HP: <color=white>50</color>  |  Speed: <color=white>2.0</color>  |  Power: <color=white>5.0</color></color>";
        statsTmp.fontSize = 13.5f;
        statsTmp.enableWordWrapping = false;

        // Assign to MonsterCardUI
        var ui = cardGo.GetComponent<MonsterCardUI>();
        var serializedObject = new SerializedObject(ui);
        serializedObject.FindProperty("_chibiImage").objectReferenceValue = chibiImg;
        serializedObject.FindProperty("_nameText").objectReferenceValue = nameTmp;
        serializedObject.FindProperty("_rankBadgeText").objectReferenceValue = rankTmp;
        serializedObject.FindProperty("_moveBadgeText").objectReferenceValue = moveTmp;
        serializedObject.FindProperty("_statsText").objectReferenceValue = statsTmp;
        serializedObject.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(cardGo, prefabPath);
        GameObject.DestroyImmediate(cardGo);
    }
}
#endif
