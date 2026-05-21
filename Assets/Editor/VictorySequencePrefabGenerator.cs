using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class VictorySequencePrefabGenerator
{
    [MenuItem("Tools/MaouSamaTD/Generate XPCard Prefab")]
    public static void GenerateXPCardPrefab()
    {
        string folder = "Assets/_Game/Prefabs/UI/Battle";
        if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs/UI/Battle"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs/UI"))
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "UI");
            AssetDatabase.CreateFolder("Assets/_Game/Prefabs/UI", "Battle");
        }

        // ---- XPCard Prefab ----
        // Layout: Avatar on left, level + XP slider on right
        // Overall size driven by parent GridLayoutGroup cell size
        var card = new GameObject("XPCard", typeof(RectTransform), typeof(Image));
        var cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(280, 90);
        var cardBg = card.GetComponent<Image>();
        cardBg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

        // --- Avatar (left side) ---
        var avatarObj = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
        avatarObj.transform.SetParent(card.transform, false);
        var avatarImg = avatarObj.GetComponent<Image>();
        avatarImg.preserveAspect = true;
        avatarImg.color = Color.white;
        var avatarRect = avatarObj.GetComponent<RectTransform>();
        avatarRect.anchorMin = new Vector2(0f, 0.05f);
        avatarRect.anchorMax = new Vector2(0.3f, 0.95f);
        avatarRect.sizeDelta = Vector2.zero;
        avatarRect.offsetMin = new Vector2(4, 4);
        avatarRect.offsetMax = new Vector2(0, -4);

        // --- Right content container ---
        var rightObj = new GameObject("RightContent", typeof(RectTransform));
        rightObj.transform.SetParent(card.transform, false);
        var rightRect = rightObj.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.32f, 0.05f);
        rightRect.anchorMax = new Vector2(0.98f, 0.95f);
        rightRect.sizeDelta = Vector2.zero;

        // --- Level Text (top-right area) ---
        var lvlObj = new GameObject("LevelText", typeof(RectTransform), typeof(TextMeshProUGUI));
        lvlObj.transform.SetParent(rightObj.transform, false);
        var lvlRect = lvlObj.GetComponent<RectTransform>();
        lvlRect.anchorMin = new Vector2(0f, 0.55f);
        lvlRect.anchorMax = new Vector2(0.45f, 1f);
        lvlRect.sizeDelta = Vector2.zero;
        var lvlTMP = lvlObj.GetComponent<TextMeshProUGUI>();
        lvlTMP.text = "Lv 1";
        lvlTMP.fontSize = 24;
        lvlTMP.fontStyle = FontStyles.Bold;
        lvlTMP.color = Color.white;
        lvlTMP.alignment = TextAlignmentOptions.Left;
        lvlTMP.enableAutoSizing = true;
        lvlTMP.fontSizeMin = 12;
        lvlTMP.fontSizeMax = 24;

        // --- XP Text (top-right of right area) ---
        var xpTextObj = new GameObject("XPText", typeof(RectTransform), typeof(TextMeshProUGUI));
        xpTextObj.transform.SetParent(rightObj.transform, false);
        var xpTextRect = xpTextObj.GetComponent<RectTransform>();
        xpTextRect.anchorMin = new Vector2(0.5f, 0.55f);
        xpTextRect.anchorMax = new Vector2(1f, 1f);
        xpTextRect.sizeDelta = Vector2.zero;
        var xpTMP = xpTextObj.GetComponent<TextMeshProUGUI>();
        xpTMP.text = "+100 XP";
        xpTMP.fontSize = 20;
        xpTMP.color = new Color(0.3f, 1f, 0.5f);
        xpTMP.alignment = TextAlignmentOptions.Right;
        xpTMP.enableAutoSizing = true;
        xpTMP.fontSizeMin = 10;
        xpTMP.fontSizeMax = 20;

        // --- Slider Background (bottom-right area) ---
        var sliderBgObj = new GameObject("SliderBg", typeof(RectTransform), typeof(Image));
        sliderBgObj.transform.SetParent(rightObj.transform, false);
        var sliderBgImg = sliderBgObj.GetComponent<Image>();
        sliderBgImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        var sliderBgRect = sliderBgObj.GetComponent<RectTransform>();
        sliderBgRect.anchorMin = new Vector2(0f, 0.1f);
        sliderBgRect.anchorMax = new Vector2(1f, 0.45f);
        sliderBgRect.sizeDelta = Vector2.zero;

        // --- Slider Fill (inside slider bg) ---
        var sliderFillObj = new GameObject("SliderFill", typeof(RectTransform), typeof(Image));
        sliderFillObj.transform.SetParent(sliderBgObj.transform, false);
        var sliderFillImg = sliderFillObj.GetComponent<Image>();
        sliderFillImg.color = new Color(1f, 0.65f, 0.1f); // Orange/amber fill
        var sliderFillRect = sliderFillObj.GetComponent<RectTransform>();
        sliderFillRect.anchorMin = Vector2.zero;
        sliderFillRect.anchorMax = new Vector2(0.5f, 1f); // 50% fill as default
        sliderFillRect.pivot = new Vector2(0f, 0.5f);
        sliderFillRect.sizeDelta = Vector2.zero;

        // --- XP Ratio Text (on top of slider) ---
        var xpRatioObj = new GameObject("XPRatioText", typeof(RectTransform), typeof(TextMeshProUGUI));
        xpRatioObj.transform.SetParent(sliderBgObj.transform, false);
        var xpRatioRect = xpRatioObj.GetComponent<RectTransform>();
        xpRatioRect.anchorMin = Vector2.zero;
        xpRatioRect.anchorMax = Vector2.one;
        xpRatioRect.sizeDelta = Vector2.zero;
        var xpRatioTMP = xpRatioObj.GetComponent<TextMeshProUGUI>();
        xpRatioTMP.text = "72/500";
        xpRatioTMP.fontSize = 14;
        xpRatioTMP.color = Color.white;
        xpRatioTMP.alignment = TextAlignmentOptions.Center;
        xpRatioTMP.enableAutoSizing = true;
        xpRatioTMP.fontSizeMin = 8;
        xpRatioTMP.fontSizeMax = 14;

        string cardPath = folder + "/XPCard.prefab";
        PrefabUtility.SaveAsPrefabAsset(card, cardPath);
        GameObject.DestroyImmediate(card);
        Debug.Log($"[VictorySequencePrefabGenerator] Created XPCard prefab at {cardPath}");
    }

    [MenuItem("Tools/MaouSamaTD/Fix Wave Text Size")]
    public static void FixWaveTextSize()
    {
        // Find WaveNumberText in scene
        var allTMP = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (var tmp in allTMP)
        {
            if (tmp.gameObject.name == "WaveNumberText")
            {
                // Remove ContentSizeFitter if present
                var csf = tmp.GetComponent<ContentSizeFitter>();
                if (csf != null)
                {
                    Debug.Log("[Fix] Removing ContentSizeFitter from WaveNumberText");
                    Undo.DestroyObjectImmediate(csf);
                }

                // Set proper font size constraints
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 10;
                tmp.fontSizeMax = 18;
                tmp.fontSize = 18;

                // Fix RectTransform to have proper size
                var rect = tmp.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 30f);

                EditorUtility.SetDirty(tmp.gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(tmp.gameObject.scene);
                Debug.Log("[Fix] Fixed WaveNumberText size: fontSizeMax=18, height=30, removed ContentSizeFitter");
                break;
            }
        }
    }
}
