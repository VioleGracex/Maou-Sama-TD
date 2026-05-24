using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.UI;

public class GenerateEnemyInspectorUI
{
    [MenuItem("Tools/MaouSama/Generate Enemy Inspector UI")]
    public static void Generate()
    {
        string prefabPath = "Assets/_Game/Prefabs/UI/Battle/Enemy_Inspector_UI.prefab";
        GameObject root = new GameObject("Enemy_Inspector_UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(EnemyInspectorUI));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(1920, 1080);
        
        GameObject panel = new GameObject("Stats_BG_Panel", typeof(RectTransform));
        panel.transform.SetParent(root.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0.5f); panelRect.anchorMax = new Vector2(1, 0.5f); panelRect.pivot = new Vector2(1, 0.5f);
        panelRect.anchoredPosition = new Vector2(-50, 0); panelRect.sizeDelta = new Vector2(420, 800);
        
        panel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
        var outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.2f, 0.2f, 1f); outline.effectDistance = new Vector2(2, -2);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 40, 20); vlg.spacing = 15;
        vlg.childControlHeight = true; vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = false;
        
        System.Func<string, Transform, GameObject> createUI = (name, parent) => {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        };

        System.Func<string, Transform, string, float, Color, TextAlignmentOptions, TextMeshProUGUI> createText = (name, parent, txt, size, color, align) => {
            var go = createUI(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = txt; tmp.fontSize = size; tmp.color = color; tmp.alignment = align; tmp.enableWordWrapping = true;
            return tmp;
        };

        // Close Button
        GameObject closeBtnObj = createUI("Close_Btn", panel.transform);
        var closeRect = closeBtnObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1); closeRect.anchorMax = new Vector2(1, 1); closeRect.pivot = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-10, -10); closeRect.sizeDelta = new Vector2(40, 40);
        closeBtnObj.AddComponent<Image>().color = new Color(1f, 0.3f, 0.3f, 1f);
        var closeBtn = closeBtnObj.AddComponent<Button>();
        createText("Text", closeBtnObj.transform, "X", 24, Color.white, TextAlignmentOptions.Center).GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);
        closeBtnObj.AddComponent<LayoutElement>().ignoreLayout = true;
        
        // 1. Header
        GameObject header = createUI("Header", panel.transform);
        var hLG = header.AddComponent<HorizontalLayoutGroup>();
        hLG.spacing = 15; hLG.childControlHeight = true; hLG.childControlWidth = true; hLG.childForceExpandHeight = false; hLG.childForceExpandWidth = false;
        
        GameObject classIcon = createUI("Class_Icon", header.transform);
        var iconLE = classIcon.AddComponent<LayoutElement>();
        iconLE.minWidth = 80; iconLE.minHeight = 80; iconLE.preferredWidth = 80; iconLE.preferredHeight = 80;
        var iconImg = classIcon.AddComponent<Image>();
        iconImg.color = new Color(0.3f, 0.2f, 0.2f, 1f);
        
        GameObject titleContainer = createUI("TitleContainer", header.transform);
        var titleLG = titleContainer.AddComponent<VerticalLayoutGroup>();
        titleLG.childControlHeight = true; titleLG.childControlWidth = true; titleLG.childForceExpandHeight = false; titleLG.childForceExpandWidth = true;
        
        var nameText = createText("Stats_Unit_Name_Txt", titleContainer.transform, "Corrupted Bandit", 28, Color.white, TextAlignmentOptions.Left);
        nameText.fontStyle = FontStyles.Bold;
        
        var subText = createText("Vassal_Stats_Txt", titleContainer.transform, "Enemy Stats - Ground", 16, new Color(1f, 0.4f, 0.4f, 1f), TextAlignmentOptions.Left);
        
        // 2. Vitality Section
        GameObject vitSection = createUI("Vitality_Section", panel.transform);
        var vitLG = vitSection.AddComponent<VerticalLayoutGroup>();
        vitLG.spacing = 10; vitLG.childControlHeight = true; vitLG.childControlWidth = true; vitLG.childForceExpandHeight = false; vitLG.childForceExpandWidth = true;
        
        GameObject vitRow = createUI("VitRow", vitSection.transform);
        var vRowLG = vitRow.AddComponent<HorizontalLayoutGroup>();
        vRowLG.childControlHeight = true; vRowLG.childControlWidth = true; vRowLG.childForceExpandHeight = false; vRowLG.childForceExpandWidth = false;
        
        createText("Vitality_Lbl", vitRow.transform, "VITALITY", 16, Color.gray, TextAlignmentOptions.Left);
        var spacer1 = createUI("Spacer", vitRow.transform);
        spacer1.AddComponent<LayoutElement>().flexibleWidth = 1;
        var hpText = createText("Stats_HP_Number_Txt", vitRow.transform, "350 / 350", 18, Color.white, TextAlignmentOptions.Right);
        hpText.fontStyle = FontStyles.Bold;
        
        GameObject hpBarBg = createUI("Stats_HPBar_BG", vitSection.transform);
        hpBarBg.AddComponent<LayoutElement>().minHeight = 15;
        hpBarBg.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 1f);
        
        GameObject hpBarFill = createUI("Stats_HPBar", hpBarBg.transform);
        var hpBarFillRect = hpBarFill.GetComponent<RectTransform>();
        hpBarFillRect.anchorMin = new Vector2(0, 0); hpBarFillRect.anchorMax = new Vector2(1, 1); hpBarFillRect.sizeDelta = Vector2.zero;
        var hpImg = hpBarFill.AddComponent<Image>();
        hpImg.color = new Color(1f, 0.2f, 0.2f, 1f); hpImg.type = Image.Type.Filled; hpImg.fillMethod = Image.FillMethod.Horizontal; hpImg.fillAmount = 1f;
        
        var statsComp = panel.AddComponent<EnemyInspectorStatsPanel>();
        
        // 3. Grid
        GameObject grid = createUI("StatsGrid", panel.transform);
        var gridGroup = grid.AddComponent<GridLayoutGroup>();
        gridGroup.cellSize = new Vector2(185, 70); gridGroup.spacing = new Vector2(10, 10);
        grid.AddComponent<LayoutElement>().minHeight = 150;
        
        System.Func<string, string, string, Color, TextMeshProUGUI> createStatBox = (bname, label, val, valColor) => {
            GameObject go = createUI(bname, grid.transform);
            go.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 1f);
            var slg = go.AddComponent<VerticalLayoutGroup>();
            slg.padding = new RectOffset(0, 0, 10, 10); slg.childControlHeight = true; slg.childControlWidth = true; slg.childForceExpandHeight = false; slg.childForceExpandWidth = true;
            createText(bname + "_Lbl", go.transform, label, 12, Color.gray, TextAlignmentOptions.Center);
            var v = createText(bname + "_Val", go.transform, val, 20, valColor, TextAlignmentOptions.Center);
            v.fontStyle = FontStyles.Bold;
            return v;
        };
        
        var atkVal = createStatBox("AtkBox", "ATTACK", "45", new Color(1f, 0.4f, 0.4f, 1f));
        var defVal = createStatBox("DefBox", "DEFENSE", "12", new Color(0.4f, 0.6f, 1f, 1f));
        var aspdVal = createStatBox("AspdBox", "ATK SPEED", "1.5/sec", new Color(1f, 0.9f, 0.4f, 1f));
        var rangeVal = createStatBox("RangeBox", "RANGE", "1.0", Color.white);
        
        // 4. Speed
        GameObject speedObj = createUI("SpeedLine", panel.transform);
        var sRowLG = speedObj.AddComponent<HorizontalLayoutGroup>();
        sRowLG.childControlHeight = true; sRowLG.childControlWidth = true; sRowLG.childForceExpandHeight = false; sRowLG.childForceExpandWidth = false;
        
        var speedText = createText("MoveSpeed_Txt", speedObj.transform, "Speed: 1.2 blk/s", 16, Color.gray, TextAlignmentOptions.Left);
        var spacer2 = createUI("Spacer2", speedObj.transform);
        spacer2.AddComponent<LayoutElement>().flexibleWidth = 1;
        var exitDmgText = createText("ExitDamage_Txt", speedObj.transform, "EXIT DMG: 1 HP", 16, new Color(1f, 0.3f, 0.3f, 1f), TextAlignmentOptions.Right);
        exitDmgText.fontStyle = FontStyles.Bold;
        
        // 5. Details
        GameObject details = createUI("DetailsPanel", panel.transform);
        var dLG = details.AddComponent<VerticalLayoutGroup>();
        dLG.padding = new RectOffset(15, 15, 15, 15); dLG.spacing = 10;
        dLG.childControlHeight = true; dLG.childControlWidth = true; dLG.childForceExpandHeight = false; dLG.childForceExpandWidth = true;
        details.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 1f);
        var detOutline = details.AddComponent<Outline>();
        detOutline.effectColor = new Color(1f, 0.5f, 0f, 1f);
        
        var detTitle = createText("Details_Title", details.transform, "ENEMY DETAILS", 18, new Color(1f, 0.6f, 0.2f, 1f), TextAlignmentOptions.TopLeft);
        detTitle.fontStyle = FontStyles.Bold;
        var detDesc = createText("Details_Desc", details.transform, "<color=#00FF00>Immunities:</color>\n- Immune to Stun\n\n<color=#FF8800>Abilities:</color>\nFrenzy: Gains attack speed.", 14, Color.white, TextAlignmentOptions.TopLeft);

        // 6. Range Pattern
        GameObject rpObj = createUI("RangePatternContainer", panel.transform);
        rpObj.AddComponent<LayoutElement>().minHeight = 80;
        rpObj.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 1f);
        var rpText = createText("RPText", rpObj.transform, "[ Attack Range Pattern Grid ]", 14, Color.gray, TextAlignmentOptions.Center);
        rpText.GetComponent<RectTransform>().anchorMin = Vector2.zero; rpText.GetComponent<RectTransform>().anchorMax = Vector2.one; rpText.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        var rpComp = rpObj.AddComponent<RangePatternUI>();
        
        closeBtnObj.transform.SetAsLastSibling();

        var so = new SerializedObject(statsComp);
        so.FindProperty("_atkText").objectReferenceValue = atkVal;
        so.FindProperty("_defText").objectReferenceValue = defVal;
        so.FindProperty("_aspdText").objectReferenceValue = aspdVal;
        so.FindProperty("_rangeText").objectReferenceValue = rangeVal;
        so.FindProperty("_moveSpeedText").objectReferenceValue = speedText;
        so.FindProperty("_exitDamageText").objectReferenceValue = exitDmgText;
        so.FindProperty("_ultimateSection").objectReferenceValue = details;
        so.FindProperty("_ultimateNameText").objectReferenceValue = detTitle;
        so.FindProperty("_ultimateDescText").objectReferenceValue = detDesc;
        so.FindProperty("_classIcon").objectReferenceValue = iconImg;
        so.FindProperty("_rangeGridIcon").objectReferenceValue = rpComp;
        so.ApplyModifiedProperties();
        
        var inspSo = new SerializedObject(root.GetComponent<EnemyInspectorUI>());
        inspSo.FindProperty("_panel").objectReferenceValue = panel;
        inspSo.FindProperty("_vassalStatsText").objectReferenceValue = subText;
        inspSo.FindProperty("_unitNameText").objectReferenceValue = nameText;
        inspSo.FindProperty("_hpNumberText").objectReferenceValue = hpText;
        inspSo.FindProperty("_hpBarImage").objectReferenceValue = hpImg;
        inspSo.FindProperty("_statsPanel").objectReferenceValue = statsComp;
        inspSo.FindProperty("_closeButton").objectReferenceValue = closeBtn;
        inspSo.FindProperty("_dmgText").objectReferenceValue = atkVal;
        inspSo.FindProperty("_rangeText").objectReferenceValue = rangeVal;
        inspSo.FindProperty("_rangePatternUI").objectReferenceValue = rpComp;
        inspSo.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        Debug.Log("Fixed Layout Generated Successfully.");
    }
}
