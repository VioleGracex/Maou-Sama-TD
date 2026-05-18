using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.UI.Vassals;

public class BuildChambersUIPage : MonoBehaviour
{
    [MenuItem("Tools/Build Chambers Page UI")]
    public static void Build()
    {
        GameObject parent = GameObject.Find("Page_Content_Area");
        if (parent == null)
        {
            Debug.LogError("Page_Content_Area not found!");
            return;
        }

        // Remove old if exists
        Transform old = parent.transform.Find("ChambersPage");
        if (old != null) DestroyImmediate(old.gameObject);

        GameObject page = new GameObject("ChambersPage", typeof(RectTransform), typeof(Image), typeof(ChambersPageUI));
        page.transform.SetParent(parent.transform, false);
        RectTransform pageRect = page.GetComponent<RectTransform>();
        pageRect.anchorMin = Vector2.zero;
        pageRect.anchorMax = Vector2.one;
        pageRect.sizeDelta = Vector2.zero;
        page.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 1f);

        ChambersPageUI script = page.GetComponent<ChambersPageUI>();

        // LEFT PANEL
        GameObject leftPanel = new GameObject("LeftPanel", typeof(RectTransform));
        leftPanel.transform.SetParent(page.transform, false);
        RectTransform leftRect = leftPanel.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0.35f, 1);
        leftRect.offsetMin = new Vector2(0, 0);
        leftRect.offsetMax = new Vector2(0, -150);

        // Search Bar & Sort
        GameObject searchBox = new GameObject("SearchBox", typeof(RectTransform), typeof(Image));
        searchBox.transform.SetParent(leftPanel.transform, false);
        RectTransform searchRect = searchBox.GetComponent<RectTransform>();
        searchRect.anchorMin = new Vector2(0, 1);
        searchRect.anchorMax = new Vector2(1, 1);
        searchRect.sizeDelta = new Vector2(0, 50);
        searchRect.anchoredPosition = new Vector2(0, -25);
        
        TMP_InputField searchInput = searchBox.AddComponent<TMP_InputField>();
        script.searchInputField = searchInput;

        GameObject scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(leftPanel.transform, false);
        RectTransform scrollRect = scrollGo.GetComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(10, 10);
        scrollRect.offsetMax = new Vector2(-10, -80);
        
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.sizeDelta = Vector2.zero;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.sizeDelta = new Vector2(0, 100);
        contentRect.pivot = new Vector2(0.5f, 1);
        
        scrollGo.GetComponent<ScrollRect>().viewport = vpRect;
        scrollGo.GetComponent<ScrollRect>().content = contentRect;
        script.characterListContent = content.transform;

        // RIGHT PANEL
        GameObject rightPanel = new GameObject("RightPanel", typeof(RectTransform));
        rightPanel.transform.SetParent(page.transform, false);
        RectTransform rightRect = rightPanel.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.35f, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.offsetMin = new Vector2(0, 0);
        rightRect.offsetMax = new Vector2(0, -150);

        // Waist Up Image
        GameObject bgName = new GameObject("BgName", typeof(RectTransform), typeof(TextMeshProUGUI));
        bgName.transform.SetParent(rightPanel.transform, false);
        RectTransform bgNameRect = bgName.GetComponent<RectTransform>();
        bgNameRect.anchorMin = Vector2.zero;
        bgNameRect.anchorMax = Vector2.one;
        bgNameRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI bgText = bgName.GetComponent<TextMeshProUGUI>();
        bgText.fontSize = 200;
        bgText.color = new Color(1,1,1,0.05f);
        bgText.alignment = TextAlignmentOptions.Center;
        bgText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        script.bgNameText = bgText;

        GameObject waistUp = new GameObject("WaistUpImage", typeof(RectTransform), typeof(Image));
        waistUp.transform.SetParent(rightPanel.transform, false);
        RectTransform waistRect = waistUp.GetComponent<RectTransform>();
        waistRect.anchorMin = new Vector2(0, 0);
        waistRect.anchorMax = new Vector2(1, 1);
        waistRect.sizeDelta = Vector2.zero;
        waistRect.offsetMin = new Vector2(100, 200);
        waistRect.offsetMax = new Vector2(-100, -100);
        waistUp.GetComponent<Image>().preserveAspect = true;
        script.waistUpImage = waistUp.GetComponent<Image>();

        // Info Overlay
        GameObject subtitle = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subtitle.transform.SetParent(rightPanel.transform, false);
        RectTransform subRect = subtitle.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 0.5f);
        subRect.anchorMax = new Vector2(0.5f, 0.5f);
        subRect.anchoredPosition = new Vector2(0, 100);
        subRect.sizeDelta = new Vector2(400, 50);
        TextMeshProUGUI subTxt = subtitle.GetComponent<TextMeshProUGUI>();
        subTxt.alignment = TextAlignmentOptions.Center;
        subTxt.fontSize = 20;
        script.subtitleText = subTxt;

        GameObject charName = new GameObject("CharName", typeof(RectTransform), typeof(TextMeshProUGUI));
        charName.transform.SetParent(rightPanel.transform, false);
        RectTransform nameRect = charName.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 0.5f);
        nameRect.anchorMax = new Vector2(0.5f, 0.5f);
        nameRect.anchoredPosition = new Vector2(0, 50);
        nameRect.sizeDelta = new Vector2(400, 80);
        TextMeshProUGUI nameTxt = charName.GetComponent<TextMeshProUGUI>();
        nameTxt.alignment = TextAlignmentOptions.Center;
        nameTxt.fontSize = 60;
        nameTxt.fontStyle = FontStyles.Bold | FontStyles.Italic;
        script.characterNameText = nameTxt;

        GameObject quote = new GameObject("Quote", typeof(RectTransform), typeof(TextMeshProUGUI));
        quote.transform.SetParent(rightPanel.transform, false);
        RectTransform quoteRect = quote.GetComponent<RectTransform>();
        quoteRect.anchorMin = new Vector2(0.5f, 0.5f);
        quoteRect.anchorMax = new Vector2(0.5f, 0.5f);
        quoteRect.anchoredPosition = new Vector2(0, -20);
        quoteRect.sizeDelta = new Vector2(400, 60);
        TextMeshProUGUI quoteTxt = quote.GetComponent<TextMeshProUGUI>();
        quoteTxt.alignment = TextAlignmentOptions.Center;
        quoteTxt.fontStyle = FontStyles.Italic;
        script.quoteText = quoteTxt;

        // Bottom Section
        GameObject bottomPanel = new GameObject("BottomPanel", typeof(RectTransform));
        bottomPanel.transform.SetParent(rightPanel.transform, false);
        RectTransform botRect = bottomPanel.GetComponent<RectTransform>();
        botRect.anchorMin = new Vector2(0, 0);
        botRect.anchorMax = new Vector2(1, 0);
        botRect.anchoredPosition = new Vector2(0, 100);
        botRect.sizeDelta = new Vector2(-100, 150);

        GameObject bondMag = new GameObject("BondMagnitude", typeof(RectTransform), typeof(TextMeshProUGUI));
        bondMag.transform.SetParent(bottomPanel.transform, false);
        RectTransform bondMagRect = bondMag.GetComponent<RectTransform>();
        bondMagRect.anchoredPosition = new Vector2(-200, 20);
        bondMagRect.sizeDelta = new Vector2(100, 80);
        script.bondMagnitudeText = bondMag.GetComponent<TextMeshProUGUI>();

        GameObject buttonsPanel = new GameObject("ButtonsPanel", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        buttonsPanel.transform.SetParent(bottomPanel.transform, false);
        RectTransform btnPanelRect = buttonsPanel.GetComponent<RectTransform>();
        btnPanelRect.anchorMin = new Vector2(0, 0);
        btnPanelRect.anchorMax = new Vector2(1, 0);
        btnPanelRect.anchoredPosition = new Vector2(0, -60);
        btnPanelRect.sizeDelta = new Vector2(0, 60);

        GameObject btn1 = new GameObject("BtnRestore", typeof(RectTransform), typeof(Image), typeof(Button));
        btn1.transform.SetParent(buttonsPanel.transform, false);
        script.restoreVigorBtn = btn1.GetComponent<Button>();

        GameObject btn2 = new GameObject("BtnBestow", typeof(RectTransform), typeof(Image), typeof(Button));
        btn2.transform.SetParent(buttonsPanel.transform, false);
        script.bestowOfferingBtn = btn2.GetComponent<Button>();

        GameObject btn3 = new GameObject("BtnAudience", typeof(RectTransform), typeof(Image), typeof(Button));
        btn3.transform.SetParent(buttonsPanel.transform, false);
        script.privateAudienceBtn = btn3.GetComponent<Button>();

        page.SetActive(false);
        Debug.Log("Chambers UI created!");
        
        EditorUtility.SetDirty(parent);
    }
}
