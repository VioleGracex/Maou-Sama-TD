using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.UI;

namespace MaouSamaTD.EditorTools
{
    public class BarracksPanelCreator
    {
        [MenuItem("Tools/Maou Sama TD/Create Barracks Panel")]
        public static void CreateBarracksPanel()
        {
            Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            // Main Panel Controller
            GameObject panelObject = new GameObject("BarracksController", typeof(RectTransform), typeof(MaouSamaTD.UI.Vassals.VassalsBarracksPanel));
            panelObject.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            // Main Panel Visuals (The part that actually turns on/off)
            GameObject panelVisuals = new GameObject("BarracksVisuals", typeof(RectTransform), typeof(Image));
            panelVisuals.transform.SetParent(panelRect, false);
            RectTransform visualRect = panelVisuals.GetComponent<RectTransform>();
            visualRect.anchorMin = Vector2.zero;
            visualRect.anchorMax = Vector2.one;
            visualRect.sizeDelta = Vector2.zero;
            panelVisuals.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f); // Dark background

            var selectionScript = panelObject.GetComponent<MaouSamaTD.UI.Vassals.VassalsBarracksPanel>();

            // 1. Details Panel (Left Sidebar)
            GameObject detailsObject = new GameObject("DetailsController", typeof(RectTransform), typeof(UnitDetailsPanel));
            detailsObject.transform.SetParent(visualRect, false);
            RectTransform detailsRect = detailsObject.GetComponent<RectTransform>();
            // Start anchored safely somewhere top left
            detailsRect.anchorMin = new Vector2(0, 1);
            detailsRect.anchorMax = new Vector2(0, 1);
            detailsRect.pivot = new Vector2(0, 1);
            
            GameObject detailsVisualObject = new GameObject("DetailsVisuals", typeof(RectTransform), typeof(Image));
            detailsVisualObject.transform.SetParent(detailsRect, false);
            RectTransform detailsVisualRect = detailsVisualObject.GetComponent<RectTransform>();
            detailsVisualRect.anchorMin = new Vector2(0, 0); // Need to adjust this layout specifically for the visual root now
            detailsVisualRect.anchorMax = new Vector2(0, 1);
            detailsVisualRect.pivot = new Vector2(0, 0.5f);
            detailsVisualRect.sizeDelta = new Vector2(400, 0); // 400 width
            detailsVisualObject.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 1f);
            
            // Add a VerticalLayoutGroup to Details Visuals
            var dVlg = detailsVisualObject.AddComponent<VerticalLayoutGroup>();
            dVlg.padding = new RectOffset(20, 20, 20, 20);
            dVlg.spacing = 15;
            dVlg.childControlHeight = false;

            // Identity Header
            GameObject nameObj = new GameObject("NameLevelObj", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(detailsVisualObject.transform, false);
            nameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 50);
            var nt = nameObj.GetComponent<TextMeshProUGUI>();
            nt.text = "Unit Name - LV 1";
            nt.fontSize = 36;
            nt.alignment = TextAlignmentOptions.TopLeft;

            // Stats Block (Grid Layout)
            GameObject statsObj = new GameObject("StatsGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            statsObj.transform.SetParent(detailsVisualObject.transform, false);
            statsObj.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 150);
            var grid = statsObj.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(170, 40);
            grid.spacing = new Vector2(10, 10);
            
            string[] statNames = { "HP: 100", "ATK: 10", "DEF: 0", "RNG: 1.0", "BLK: 1", "CST: 10" };
            GameObject[] statTexts = new GameObject[statNames.Length];
            for (int i = 0; i < statNames.Length; i++)
            {
                statTexts[i] = new GameObject($"Stat_{i}", typeof(RectTransform), typeof(TextMeshProUGUI));
                statTexts[i].transform.SetParent(statsObj.transform, false);
                var st = statTexts[i].GetComponent<TextMeshProUGUI>();
                st.text = statNames[i];
                st.fontSize = 24;
            }

            // Skills Rows
            string[] skillNames = { "Passive", "Active", "Ultimate" };
            GameObject[] skillIcons = new GameObject[3];
            GameObject[] skillTitles = new GameObject[3];
            GameObject[] skillDescs = new GameObject[3];

            for (int i = 0; i < 3; i++)
            {
                GameObject rowObj = new GameObject($"SkillRow_{skillNames[i]}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                rowObj.transform.SetParent(detailsVisualObject.transform, false);
                rowObj.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 100);
                var rowHg = rowObj.GetComponent<HorizontalLayoutGroup>();
                rowHg.spacing = 15;
                rowHg.childControlWidth = false;

                // Square Icon
                skillIcons[i] = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                skillIcons[i].transform.SetParent(rowObj.transform, false);
                skillIcons[i].GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);
                skillIcons[i].GetComponent<Image>().color = Color.gray;

                // Text Container
                GameObject txtCont = new GameObject("TextCont", typeof(RectTransform), typeof(VerticalLayoutGroup));
                txtCont.transform.SetParent(rowObj.transform, false);
                txtCont.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 80);
                
                skillTitles[i] = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
                skillTitles[i].transform.SetParent(txtCont.transform, false);
                var tTmp = skillTitles[i].GetComponent<TextMeshProUGUI>();
                tTmp.text = skillNames[i];
                tTmp.fontSize = 28;

                skillDescs[i] = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
                skillDescs[i].transform.SetParent(txtCont.transform, false);
                var dTmp = skillDescs[i].GetComponent<TextMeshProUGUI>();
                dTmp.text = "Skill description...";
                dTmp.fontSize = 18;
                dTmp.textWrappingMode  = TextWrappingModes.Normal;
            }

            // 2. Scroll View (Middle)
            GameObject scrollObject = new GameObject("UnitScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollObject.transform.SetParent(visualRect, false);
            RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(0, 0);       // Left, Bottom
            scrollRect.offsetMax = new Vector2(-120, -100); // Right, Top
            scrollObject.GetComponent<Image>().color = new Color(0, 0, 0, 0); // Transparent

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollRect, false);
            RectTransform viewRect = viewport.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.sizeDelta = Vector2.zero;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 500);
            
            var contentGrid = content.GetComponent<GridLayoutGroup>();
            contentGrid.cellSize = new Vector2(150, 200);
            contentGrid.spacing = new Vector2(20, 20);
            contentGrid.padding = new RectOffset(20, 20, 20, 20);
            
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var scrollRectComp = scrollObject.GetComponent<ScrollRect>();
            scrollRectComp.content = contentRect;
            scrollRectComp.viewport = viewRect;
            scrollRectComp.horizontal = false;
            scrollRectComp.vertical = true;
            scrollRectComp.movementType = ScrollRect.MovementType.Elastic;

            // 3. Filter Actions (Top Right - Sorting)
            GameObject sortContainer = new GameObject("SortContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            sortContainer.transform.SetParent(visualRect, false);
            RectTransform sortRect = sortContainer.GetComponent<RectTransform>();
            sortRect.anchorMin = new Vector2(1, 1);
            sortRect.anchorMax = new Vector2(1, 1);
            sortRect.pivot = new Vector2(1, 1);
            sortRect.anchoredPosition = new Vector2(-150, -20); // Leave room for right scroll
            sortRect.sizeDelta = new Vector2(600, 60);
            
            var sortHg = sortContainer.GetComponent<HorizontalLayoutGroup>();
            sortHg.childAlignment = TextAnchor.MiddleRight;
            sortHg.spacing = 15;

            // Sort Buttons
            string[] sortNames = { "Level", "Rarity", "Date" };
            for(int i=0; i<sortNames.Length; i++)
            {
                GameObject btnSort = new GameObject($"BtnSort_{sortNames[i]}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnSort.transform.SetParent(sortContainer.transform, false);
                btnSort.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 60);
                btnSort.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f); 
                
                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(btnSort.transform, false);
                var rt = txtObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
                var tmp = txtObj.GetComponent<TextMeshProUGUI>();
                tmp.text = sortNames[i];
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }

            // 4. Class Filters (Right Side Vertical Scroll)
            GameObject classScrollObj = new GameObject("ClassFilterScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            classScrollObj.transform.SetParent(visualRect, false);
            RectTransform classRect = classScrollObj.GetComponent<RectTransform>();
            classRect.anchorMin = new Vector2(1, 0); // Anchor Right Stretch
            classRect.anchorMax = new Vector2(1, 1);
            classRect.pivot = new Vector2(1, 0.5f);
            classRect.anchoredPosition = new Vector2(0, 0);
            classRect.sizeDelta = new Vector2(120, 0); // Narrow vertical bar
            classScrollObj.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f); 

            GameObject cvp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            cvp.transform.SetParent(classRect, false);
            var cvpRect = cvp.GetComponent<RectTransform>();
            cvpRect.anchorMin = Vector2.zero; cvpRect.anchorMax = Vector2.one; cvpRect.sizeDelta = Vector2.zero;
            cvp.GetComponent<Mask>().showMaskGraphic = false;

            GameObject cContent = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            cContent.transform.SetParent(cvp.transform, false);
            var ccRect = cContent.GetComponent<RectTransform>();
            ccRect.anchorMin = new Vector2(0, 1); ccRect.anchorMax = new Vector2(1, 1); ccRect.pivot = new Vector2(0.5f, 1);
            ccRect.sizeDelta = new Vector2(0, 500);
            
            var cvg = cContent.GetComponent<VerticalLayoutGroup>();
            cvg.childAlignment = TextAnchor.UpperCenter;
            cvg.spacing = 15;
            cvg.padding = new RectOffset(10, 10, 120, 20); // Top padding to clear sort bar
            
            var cfitter = cContent.GetComponent<ContentSizeFitter>();
            cfitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var classSr = classScrollObj.GetComponent<ScrollRect>();
            classSr.content = ccRect;
            classSr.viewport = cvpRect;
            classSr.horizontal = false;
            classSr.vertical = true;

            // Generate Mock Toggles
            string[] classes = System.Enum.GetNames(typeof(MaouSamaTD.Units.UnitClass));
            foreach(var cName in classes)
            {
                GameObject tglObj = new GameObject($"Toggle_{cName}", typeof(RectTransform), typeof(Image), typeof(Toggle));
                tglObj.transform.SetParent(ccRect, false);
                tglObj.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);
                tglObj.GetComponent<Image>().color = Color.gray; // Background
                
                GameObject checkObj = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
                checkObj.transform.SetParent(tglObj.transform, false);
                var chkRt = checkObj.GetComponent<RectTransform>();
                chkRt.anchorMin = Vector2.zero; chkRt.anchorMax = Vector2.one; chkRt.sizeDelta = new Vector2(-10, -10);
                checkObj.GetComponent<Image>().color = Color.white;
                
                var tgl = tglObj.GetComponent<Toggle>();
                tgl.targetGraphic = tglObj.GetComponent<Image>();
                tgl.graphic = checkObj.GetComponent<Image>();
                tgl.isOn = false;
            }

            // 5. Confirm / Cancel Buttons (Bottom Right)
            GameObject actionsObject = new GameObject("ActionsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            actionsObject.transform.SetParent(visualRect, false);
            RectTransform actionsRect = actionsObject.GetComponent<RectTransform>();
            actionsRect.anchorMin = new Vector2(1, 0); // Anchor Bottom Right
            actionsRect.anchorMax = new Vector2(1, 0);
            actionsRect.pivot = new Vector2(1, 0);
            actionsRect.anchoredPosition = new Vector2(-150, 20); // Push left to avoid overlapping class scroll
            actionsRect.sizeDelta = new Vector2(400, 80);
            var actionsHg = actionsObject.GetComponent<HorizontalLayoutGroup>();
            actionsHg.childAlignment = TextAnchor.MiddleRight;
            actionsHg.spacing = 20;

            GameObject btnConfirm = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnConfirm.transform.SetParent(actionsRect, false);
            btnConfirm.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 80);
            btnConfirm.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f, 1f); // Blue
            
            GameObject btnCancel = new GameObject("CancelButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnCancel.transform.SetParent(actionsRect, false);
            btnCancel.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 80);
            btnCancel.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark Grey

            // Attempt to hook up scripts loosely
            var serializedObj = new SerializedObject(selectionScript);
            serializedObj.FindProperty("_visualRoot").objectReferenceValue = panelVisuals;
            // Removed obsolete property mappings
            // serializedObj.FindProperty("_unitListContainer").objectReferenceValue = content;
            // serializedObj.FindProperty("_filterContainer").objectReferenceValue = cContent; // Point to Class Toggles container
            
            var detailsSpt = detailsObject.GetComponent<UnitDetailsPanel>();
            var detSO = new SerializedObject(detailsSpt);
            detSO.FindProperty("_panelRect").objectReferenceValue = detailsVisualRect;  // Tell panel to animate its new root visual object
            detSO.FindProperty("_visualRoot").objectReferenceValue = detailsVisualObject;
            detSO.FindProperty("_nameText").objectReferenceValue = nt;
            // Map the stats
            detSO.FindProperty("_hpText").objectReferenceValue = statTexts[0].GetComponent<TextMeshProUGUI>();
            detSO.FindProperty("_atkText").objectReferenceValue = statTexts[1].GetComponent<TextMeshProUGUI>();
            detSO.FindProperty("_defText").objectReferenceValue = statTexts[2].GetComponent<TextMeshProUGUI>();
            detSO.FindProperty("_rangeText").objectReferenceValue = statTexts[3].GetComponent<TextMeshProUGUI>();
            detSO.FindProperty("_blockText").objectReferenceValue = statTexts[4].GetComponent<TextMeshProUGUI>();
            detSO.FindProperty("_costText").objectReferenceValue = statTexts[5].GetComponent<TextMeshProUGUI>();
            // Map the skills
            detSO.FindProperty("_passiveIcon").objectReferenceValue = skillIcons[0].GetComponent<Image>();
            detSO.FindProperty("_passiveName").objectReferenceValue = skillTitles[0].GetComponent<TextMeshProUGUI>();
            detSO.FindProperty("_passiveDesc").objectReferenceValue = skillDescs[0].GetComponent<TextMeshProUGUI>();
            
            detSO.FindProperty("_activeIcon").objectReferenceValue = skillIcons[1].GetComponent<Image>();
            detSO.FindProperty("_activeName").objectReferenceValue = skillTitles[1].GetComponent<TextMeshProUGUI>();
            detSO.FindProperty("_activeDesc").objectReferenceValue = skillDescs[1].GetComponent<TextMeshProUGUI>();

            detSO.FindProperty("_ultimateIcon").objectReferenceValue = skillIcons[2].GetComponent<Image>();
            detSO.FindProperty("_ultimateName").objectReferenceValue = skillTitles[2].GetComponent<TextMeshProUGUI>();
            detSO.FindProperty("_ultimateDesc").objectReferenceValue = skillDescs[2].GetComponent<TextMeshProUGUI>();
            detSO.ApplyModifiedProperties();

            // Removed obsolete scroll and map bindings for new Vassals Barracks
            // serializedObj.FindProperty("_detailsPanel").objectReferenceValue = detailsSpt;
            // serializedObj.FindProperty("_scrollViewRect").objectReferenceValue = scrollRect;
            // serializedObj.FindProperty("_paddingTop").floatValue = 100f; 
            // serializedObj.FindProperty("_paddingBottom").floatValue = 0f; 
            // serializedObj.FindProperty("_squeezedPaddingLeft").floatValue = 400f; 
            // serializedObj.FindProperty("_expandedPaddingLeft").floatValue = 0f;
            // serializedObj.FindProperty("_squeezedPaddingRight").floatValue = 120f; 
            // serializedObj.FindProperty("_expandedPaddingRight").floatValue = 0f;
            // serializedObj.FindProperty("_confirmButton").objectReferenceValue = btnConfirm.GetComponent<Button>();
            // serializedObj.FindProperty("_backButton").objectReferenceValue = btnCancel.GetComponent<Button>();
            serializedObj.ApplyModifiedProperties();

            EditorGUIUtility.PingObject(panelObject);
            Selection.activeGameObject = panelObject;
            
            Debug.Log("[Maou Sama TD] Barracks Panel successfully generated!");
        }
    }
}
