using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.UI.MainMenu;

namespace MaouSamaTD.EditorTools
{
    public class AscensionPanelCreator
    {
        [MenuItem("Tools/Maou Sama TD/Create Ascension Panel")]
        public static void CreateAscensionPanel()
        {
            Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            // --- Root ---
            // Create main container
            GameObject rootObj = new GameObject("AscensionPanel", typeof(RectTransform), typeof(AscensionPanel));
            rootObj.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = rootObj.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;

            // --- Background ---
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(rootRect, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgObj.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 1f);

            // --- Header Texts ---
            GameObject headerSubObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerSubObj.transform.SetParent(rootRect, false);
            RectTransform subRect = headerSubObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 1f);
            subRect.anchorMax = new Vector2(0.5f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0, -150);
            subRect.sizeDelta = new Vector2(600, 40);
            var subText = headerSubObj.GetComponent<TextMeshProUGUI>();
            subText.text = "ANCIENT RITE AWAKENED";
            subText.alignment = TextAlignmentOptions.Center;
            subText.color = new Color(1f, 0.8f, 0.2f);
            subText.fontSize = 24;
            subText.fontStyle = FontStyles.Bold | FontStyles.Italic;

            GameObject headerMainObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerMainObj.transform.SetParent(rootRect, false);
            RectTransform mainRect = headerMainObj.GetComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0.5f, 1f);
            mainRect.anchorMax = new Vector2(0.5f, 1f);
            mainRect.pivot = new Vector2(0.5f, 1f);
            mainRect.anchoredPosition = new Vector2(0, -190);
            mainRect.sizeDelta = new Vector2(600, 60);
            var mainText = headerMainObj.GetComponent<TextMeshProUGUI>();
            mainText.text = "ASCEND AS <color=#FF3333>MAOU</color>";
            mainText.alignment = TextAlignmentOptions.Center;
            mainText.color = Color.white;
            mainText.fontSize = 48;
            mainText.fontStyle = FontStyles.Bold | FontStyles.Italic;

            // --- Class Cards Container ---
            GameObject cardsContainer = new GameObject("CardsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cardsContainer.transform.SetParent(rootRect, false);
            RectTransform ccRect = cardsContainer.GetComponent<RectTransform>();
            ccRect.anchorMin = new Vector2(0.5f, 0.5f);
            ccRect.anchorMax = new Vector2(0.5f, 0.5f);
            ccRect.pivot = new Vector2(0.5f, 0.5f);
            ccRect.anchoredPosition = new Vector2(0, 50);
            ccRect.sizeDelta = new Vector2(800, 400);
            
            var ccLayout = cardsContainer.GetComponent<HorizontalLayoutGroup>();
            ccLayout.childAlignment = TextAnchor.MiddleCenter;
            ccLayout.spacing = 50;
            ccLayout.childControlHeight = false;
            ccLayout.childControlWidth = false;

            // Tyrant Card
            GameObject card1 = CreateClassCard(cardsContainer.transform, "Card_Tyrant", "SOVEREIGN OF FORCE", "THE TYRANT", new Color(0.2f, 0.05f, 0.05f));
            GameObject card1Highlight = CreateHighlight(card1.transform, Color.red);
            var btn1 = card1.AddComponent<Button>();

            // Sovereign Card
            GameObject card2 = CreateClassCard(cardsContainer.transform, "Card_Sovereign", "SOVEREIGN OF GUILE", "THE SOVEREIGN", new Color(0.05f, 0.05f, 0.2f));
            GameObject card2Highlight = CreateHighlight(card2.transform, new Color(0.3f, 0.3f, 1f));
            var btn2 = card2.AddComponent<Button>();

            // --- Identity Input Area ---
            GameObject inputRoot = new GameObject("InputRoot", typeof(RectTransform), typeof(CanvasGroup));
            inputRoot.transform.SetParent(rootRect, false);
            RectTransform inRect = inputRoot.GetComponent<RectTransform>();
            inRect.anchorMin = new Vector2(0.5f, 0f);
            inRect.anchorMax = new Vector2(0.5f, 0f);
            inRect.pivot = new Vector2(0.5f, 0f);
            inRect.anchoredPosition = new Vector2(0, 150);
            inRect.sizeDelta = new Vector2(600, 200);

            // Inscribe Text
            GameObject inscribeObj = new GameObject("InscribeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            inscribeObj.transform.SetParent(inputRoot.transform, false);
            RectTransform insRect = inscribeObj.GetComponent<RectTransform>();
            insRect.anchorMin = new Vector2(0.5f, 1f);
            insRect.anchorMax = new Vector2(0.5f, 1f);
            insRect.pivot = new Vector2(0.5f, 1f);
            insRect.anchoredPosition = new Vector2(0, 0);
            insRect.sizeDelta = new Vector2(400, 30);
            var insText = inscribeObj.GetComponent<TextMeshProUGUI>();
            insText.text = "INSCRIBE TRUE NAME";
            insText.alignment = TextAlignmentOptions.Center;
            insText.color = new Color(1f, 0.8f, 0.2f);
            insText.fontSize = 18;
            insText.fontStyle = FontStyles.Bold;

            // Input Field Background
            GameObject inputBgObj = new GameObject("InputField", typeof(RectTransform), typeof(Image));
            inputBgObj.transform.SetParent(inputRoot.transform, false);
            RectTransform ibgRect = inputBgObj.GetComponent<RectTransform>();
            ibgRect.anchorMin = new Vector2(0.5f, 0.5f);
            ibgRect.anchorMax = new Vector2(0.5f, 0.5f);
            ibgRect.pivot = new Vector2(0.5f, 0.5f);
            ibgRect.anchoredPosition = new Vector2(0, 20);
            ibgRect.sizeDelta = new Vector2(400, 60);
            inputBgObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Input Field Text Area
            GameObject textAreaObj = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textAreaObj.transform.SetParent(inputBgObj.transform, false);
            RectTransform trRect = textAreaObj.GetComponent<RectTransform>();
            trRect.anchorMin = Vector2.zero;
            trRect.anchorMax = Vector2.one;
            trRect.offsetMin = new Vector2(20, 10);
            trRect.offsetMax = new Vector2(-60, -10);

            // Input Text
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(textAreaObj.transform, false);
            RectTransform txtRect = textObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;
            var textComp = textObj.GetComponent<TextMeshProUGUI>();
            textComp.fontSize = 32;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.color = Color.white;

            var inputField = inputBgObj.AddComponent<TMP_InputField>();
            inputField.textComponent = textComp;
            inputField.targetGraphic = inputBgObj.GetComponent<Image>();

            // Dice Button
            GameObject diceObj = new GameObject("DiceButton", typeof(RectTransform), typeof(Image), typeof(Button));
            diceObj.transform.SetParent(inputBgObj.transform, false);
            RectTransform diceRect = diceObj.GetComponent<RectTransform>();
            diceRect.anchorMin = new Vector2(1f, 0.5f);
            diceRect.anchorMax = new Vector2(1f, 0.5f);
            diceRect.pivot = new Vector2(1f, 0.5f);
            diceRect.anchoredPosition = new Vector2(-10, 0);
            diceRect.sizeDelta = new Vector2(40, 40);
            diceObj.GetComponent<Image>().color = Color.gray;
            var diceBtn = diceObj.GetComponent<Button>();

            // Arise Button
            GameObject ariseObj = new GameObject("AriseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            ariseObj.transform.SetParent(inputRoot.transform, false);
            RectTransform aRect = ariseObj.GetComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0.5f, 0f);
            aRect.anchorMax = new Vector2(0.5f, 0f);
            aRect.pivot = new Vector2(0.5f, 0f);
            aRect.anchoredPosition = new Vector2(0, -50);
            aRect.sizeDelta = new Vector2(250, 60);
            ariseObj.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 1f);

            GameObject ariseTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            ariseTextObj.transform.SetParent(ariseObj.transform, false);
            RectTransform atRect = ariseTextObj.GetComponent<RectTransform>();
            atRect.anchorMin = Vector2.zero;
            atRect.anchorMax = Vector2.one;
            atRect.sizeDelta = Vector2.zero;
            var aText = ariseTextObj.GetComponent<TextMeshProUGUI>();
            aText.text = "A R I S E !";
            aText.alignment = TextAlignmentOptions.Center;
            aText.color = Color.white;
            aText.fontSize = 28;
            aText.fontStyle = FontStyles.Bold | FontStyles.Italic;

            var ariseBtn = ariseObj.GetComponent<Button>();

            // --- Connect Script References ---
            var panelScript = rootObj.GetComponent<AscensionPanel>();
            
            // We use SerializedObject to set private fields
            SerializedObject so = new SerializedObject(panelScript);
            so.FindProperty("_visualRoot").objectReferenceValue = bgObj; // Or root itself, using bgObj for visual root is fine or a container
            // Actually, better to just let the script control the entire panel object visibility? 
            // The script currently sets _visualRoot.SetActive(true/false)
            so.FindProperty("_visualRoot").objectReferenceValue = rootObj; 

            so.FindProperty("_tyrantButton").objectReferenceValue = btn1;
            so.FindProperty("_sovereignButton").objectReferenceValue = btn2;
            so.FindProperty("_tyrantSelectedHighlight").objectReferenceValue = card1Highlight;
            so.FindProperty("_sovereignSelectedHighlight").objectReferenceValue = card2Highlight;
            
            so.FindProperty("_inputRootCanvasGroup").objectReferenceValue = inputRoot.GetComponent<CanvasGroup>();
            so.FindProperty("_nameInputField").objectReferenceValue = inputField;
            so.FindProperty("_diceButton").objectReferenceValue = diceBtn;
            so.FindProperty("_ariseButton").objectReferenceValue = ariseBtn;

            so.ApplyModifiedProperties();

            Selection.activeGameObject = rootObj;
            Debug.Log("Ascension Panel successfully generated! Remember to assign _homeScreenRoot in the inspector.");
        }

        private static GameObject CreateClassCard(Transform parent, string name, string subtextData, string titleData, Color bgColor)
        {
            GameObject cardObj = new GameObject(name, typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(parent, false);
            RectTransform rect = cardObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 400);
            cardObj.GetComponent<Image>().color = bgColor;

            // Faded Background Text
            GameObject bgTextObj = new GameObject("BGText", typeof(RectTransform), typeof(TextMeshProUGUI));
            bgTextObj.transform.SetParent(cardObj.transform, false);
            RectTransform bgtRect = bgTextObj.GetComponent<RectTransform>();
            bgtRect.anchorMin = Vector2.zero;
            bgtRect.anchorMax = Vector2.one;
            bgtRect.sizeDelta = Vector2.zero;
            var bgt = bgTextObj.GetComponent<TextMeshProUGUI>();
            bgt.text = titleData == "THE TYRANT" ? "TYRANT" : "SOVEREIGN";
            bgt.alignment = TextAlignmentOptions.Center;
            bgt.color = new Color(1f, 1f, 1f, 0.05f); // Very faded
            bgt.fontSize = 60;
            bgt.fontStyle = FontStyles.Bold;

            // Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(cardObj.transform, false);
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 0f);
            tRect.anchorMax = new Vector2(1f, 0f);
            tRect.pivot = new Vector2(0.5f, 0f);
            tRect.anchoredPosition = new Vector2(0, 60);
            tRect.sizeDelta = new Vector2(0, 40);
            var tText = titleObj.GetComponent<TextMeshProUGUI>();
            tText.text = titleData;
            tText.alignment = TextAlignmentOptions.Center;
            tText.color = Color.white;
            tText.fontSize = 32;
            tText.fontStyle = FontStyles.Italic | FontStyles.Bold;

            // Subtitle
            GameObject subObj = new GameObject("SubTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(cardObj.transform, false);
            RectTransform sRect = subObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 0f);
            sRect.anchorMax = new Vector2(1f, 0f);
            sRect.pivot = new Vector2(0.5f, 0f);
            sRect.anchoredPosition = new Vector2(0, 30);
            sRect.sizeDelta = new Vector2(0, 30);
            var sText = subObj.GetComponent<TextMeshProUGUI>();
            sText.text = subtextData;
            sText.alignment = TextAlignmentOptions.Center;
            sText.color = new Color(1f, 0.4f, 0.4f);
            sText.fontSize = 16;
            sText.fontStyle = FontStyles.Bold;

            return cardObj;
        }

        private static GameObject CreateHighlight(Transform parent, Color col)
        {
            GameObject hlObj = new GameObject("HighlightBorder", typeof(RectTransform), typeof(Image));
            hlObj.transform.SetParent(parent, false);
            RectTransform rect = hlObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            // Slightly larger than the card
            rect.sizeDelta = new Vector2(10, 10);
            
            // To make it an outline in script without a custom sprite, we can just use an image, but typically you'd use a 9-slice outline sprite or Outline component.
            // A quick hack for editor generation is adding an Outline component to an invisible image.
            var img = hlObj.GetComponent<Image>();
            img.color = new Color(0,0,0,0);
            
            var outline = hlObj.AddComponent<Outline>();
            outline.effectColor = col;
            outline.effectDistance = new Vector2(4, 4);

            hlObj.SetActive(false); // Default off
            return hlObj;
        }
    }
}
