using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.UI.MainMenu;

namespace MaouSamaTD.Editor
{
    public class LoadingScreenGenerator
    {
        [MenuItem("Tools/Maou Sama TD/UI Components/Generate Loading Screen UI (Standalone)")]
        public static void GenerateLoadingScreen()
        {
             Canvas canvas = Object.FindFirstObjectByType<Canvas>();
             if (canvas == null)
             {
                 Debug.LogError("No Canvas found!");
                 return;
             }
             
             GameObject loadingObj = GetOrCreatePanel(canvas.transform, "LoadingScreen_Root", new Color(0.05f, 0.05f, 0.05f, 1f));
             RectTransform rt = loadingObj.GetComponent<RectTransform>();
             rt.anchorMin = Vector2.zero;
             rt.anchorMax = Vector2.one;
             rt.offsetMin = Vector2.zero;
             rt.offsetMax = Vector2.zero;
             
             // Background Image for Splash
             GameObject bgObj = new GameObject("Background_Splash");
             bgObj.transform.SetParent(loadingObj.transform, false);
             RectTransform bgRt = bgObj.AddComponent<RectTransform>();
             bgRt.anchorMin = Vector2.zero;
             bgRt.anchorMax = Vector2.one;
             bgRt.offsetMin = Vector2.zero;
             bgRt.offsetMax = Vector2.zero;
             Image bgImg = bgObj.AddComponent<Image>();
             bgImg.color = Color.black; // Start dark until loaded
             
             Button clearBtn = CreateButton(loadingObj.transform, "ClearCacheButton", "Clear Cache", 100, -50);
             RectTransform cbRt = clearBtn.GetComponent<RectTransform>();
             cbRt.anchorMin = new Vector2(0, 1);
             cbRt.anchorMax = new Vector2(0, 1);
             cbRt.anchoredPosition = new Vector2(100, -50);
             
             // Base progress bar representation
             GameObject barObj = new GameObject("ProgressBar_Slider");
             barObj.transform.SetParent(loadingObj.transform, false);
             RectTransform barRt = barObj.AddComponent<RectTransform>();
             barRt.anchorMin = new Vector2(0.1f, 0.05f);
             barRt.anchorMax = new Vector2(0.9f, 0.05f);
             barRt.sizeDelta = new Vector2(0, 20);
             barRt.anchoredPosition = new Vector2(0, 20);
             Image barImg = barObj.AddComponent<Image>();
             barImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
             
             Slider sliderObj = barObj.AddComponent<Slider>();
             
             GameObject fillArea = new GameObject("Fill Area");
             fillArea.transform.SetParent(barObj.transform, false);
             RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
             fillAreaRt.anchorMin = Vector2.zero;
             fillAreaRt.anchorMax = Vector2.one;
             fillAreaRt.sizeDelta = Vector2.zero;
             
             GameObject fill = new GameObject("Fill");
             fill.transform.SetParent(fillArea.transform, false);
             RectTransform fillRt = fill.AddComponent<RectTransform>();
             fillRt.anchorMin = Vector2.zero;
             fillRt.anchorMax = Vector2.one;
             fillRt.sizeDelta = Vector2.zero;
             Image fillImg = fill.AddComponent<Image>();
             fillImg.color = Color.white;
             
             sliderObj.targetGraphic = barImg;
             sliderObj.fillRect = fillRt;
             
             CreateText(loadingObj.transform, "LoreText", "The world is dark...", 0, 80);
             RectTransform loreRt = loadingObj.transform.Find("LoreText").GetComponent<RectTransform>();
             loreRt.anchorMin = new Vector2(0.1f, 0.1f);
             loreRt.anchorMax = new Vector2(0.9f, 0.2f);
             loreRt.sizeDelta = Vector2.zero;
             loreRt.anchoredPosition = Vector2.zero;
             
             CreateText(loadingObj.transform, "VersionText", "Ver: 1.0.0", -80, 50);
             RectTransform verRt = loadingObj.transform.Find("VersionText").GetComponent<RectTransform>();
             verRt.anchorMin = new Vector2(1, 0);
             verRt.anchorMax = new Vector2(1, 0);
             verRt.anchoredPosition = new Vector2(-100, 30);
             verRt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomRight;
             
             // Confirm Popup Window
             GameObject confirmRoot = GetOrCreatePanel(loadingObj.transform, "ConfirmPopup_Root", new Color(0f, 0f, 0f, 0.8f));
             confirmRoot.SetActive(false);
             CreateText(confirmRoot.transform, "ConfirmText", "Are you sure you want to clear all data?", 0, 80);
             Button confirmYesBtn = CreateButton(confirmRoot.transform, "YesButton", "YES", -100, -50);
             Button confirmNoBtn = CreateButton(confirmRoot.transform, "NoButton", "NO", 100, -50);
             
             Button startBtn = CreateButton(loadingObj.transform, "StartButton", "START GAME", 0, 0);
             RectTransform stRt = startBtn.GetComponent<RectTransform>();
             stRt.anchorMin = new Vector2(0.5f, 0.2f);
             stRt.anchorMax = new Vector2(0.5f, 0.2f);
             stRt.anchoredPosition = Vector2.zero;
             
             LoadingScreenPanel lsp = loadingObj.GetComponent<LoadingScreenPanel>();
             if (lsp == null) lsp = loadingObj.AddComponent<LoadingScreenPanel>();
             
             SerializedObject lspSo = new SerializedObject(lsp);
             SetProperty(lspSo, "_progressBar", sliderObj);
             SetProperty(lspSo, "_loreText", loadingObj.transform.Find("LoreText").GetComponent<TextMeshProUGUI>());
             SetProperty(lspSo, "_versionText", verRt.GetComponent<TextMeshProUGUI>());
             SetProperty(lspSo, "_clearCacheButton", clearBtn);
             SetProperty(lspSo, "_startButton", startBtn);
             SetProperty(lspSo, "_visualRoot", loadingObj);
             SetProperty(lspSo, "_backgroundImage", bgImg);
             SetProperty(lspSo, "_confirmWindowRoot", confirmRoot);
             SetProperty(lspSo, "_confirmYesButton", confirmYesBtn);
             SetProperty(lspSo, "_confirmNoButton", confirmNoBtn);
             lspSo.ApplyModifiedProperties();
             
             Debug.Log("Generated Standalone Loading Screen UI Panel.");
        }

        private static GameObject GetOrCreatePanel(Transform parent, string name, Color color)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = panel.AddComponent<Image>();
            img.color = color;

            Undo.RegisterCreatedObjectUndo(panel, "Create Panel");
            return panel;
        }

        private static Button CreateButton(Transform parent, string name, string label, float x, float y)
        {
            Transform existing = parent.Find(name);
            if (existing) return existing.GetComponent<Button>();

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(160, 50);

            Image img = go.AddComponent<Image>();
            img.color = Color.white;

            Button btn = go.AddComponent<Button>();
            
            // Text Label
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(go.transform, false);
            RectTransform txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;

            return btn;
        }

        private static void CreateText(Transform parent, string name, string content, float x, float y)
        {
            if (parent.Find(name)) return;

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(400, 100);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = 48;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private static void SetProperty(SerializedObject so, string propName, Object value)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }
    }
}
