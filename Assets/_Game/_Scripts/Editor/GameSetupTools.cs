using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.UI;

namespace MaouSamaTD.Editor
{
    public class GameSetupTools : EditorWindow
    {
        [MenuItem("Tools/MaouSamaTD/Generate UI Panels")]
        public static void GeneratePanels()
        {
            GameControlUI gameUI = FindObjectOfType<GameControlUI>();
            if (gameUI == null)
            {
                Debug.LogError("Could not find GameControlUI in the scene!");
                return;
            }

            Canvas canvas = gameUI.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }
            
            if (canvas == null)
            {
                Debug.LogError("No Canvas found! Please create a Canvas first.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(gameUI, "Setup UI Panels");

            // 1. Create Panels
            GameObject victoryPanel = GetOrCreatePanel(canvas.transform, "VictoryPanel", new Color(0, 0.5f, 0, 0.9f));
            GameObject losePanel = GetOrCreatePanel(canvas.transform, "LosePanel", new Color(0.5f, 0, 0, 0.9f));
            GameObject pausePanel = GetOrCreatePanel(canvas.transform, "PausePanel", new Color(0, 0, 0, 0.8f));
            GameObject confirmPanel = GetOrCreatePanel(canvas.transform, "ConfirmationPanel", new Color(0.2f, 0.2f, 0.2f, 0.95f));

            // 2. Victory Content
            CreateText(victoryPanel.transform, "Title", "VICTORY!", 0, 100);
            Button winKy = CreateButton(victoryPanel.transform, "RestartButton", "Restart", 0, -50);

            // 3. Lose Content
            CreateText(losePanel.transform, "Title", "GAME OVER", 0, 100);
            Button loseKy = CreateButton(losePanel.transform, "RestartButton", "Try Again", 0, -50);

            // 4. Pause Content
            CreateText(pausePanel.transform, "Title", "PAUSED", 0, 150);
            Button resumeBtn = CreateButton(pausePanel.transform, "ResumeButton", "Resume", 0, 50);
            Button retreatBtn = CreateButton(pausePanel.transform, "RetreatButton", "Retreat", 0, -50);

            // 5. Confirm Content
            CreateText(confirmPanel.transform, "Title", "Are you sure?", 0, 50);
            Button yesBtn = CreateButton(confirmPanel.transform, "YesButton", "Yes", -100, -50);
            Button noBtn = CreateButton(confirmPanel.transform, "NoButton", "No", 100, -50);

            // 6. Assign to GameControlUI
            SerializedObject so = new SerializedObject(gameUI);
            
            SetProperty(so, "_winPanel", victoryPanel);
            SetProperty(so, "_losePanel", losePanel);
            SetProperty(so, "_pauseOverlay", pausePanel);
            SetProperty(so, "_confirmationPanel", confirmPanel);

            SetProperty(so, "_resumeButton", resumeBtn);
            SetProperty(so, "_retreatButton", retreatBtn);
            
            SetProperty(so, "_confirmYesButton", yesBtn);
            SetProperty(so, "_confirmNoButton", noBtn);

            SetProperty(so, "_winRestartButton", winKy);
            SetProperty(so, "_loseRestartButton", loseKy);

            so.ApplyModifiedProperties();
            
            // Hide them by default
            victoryPanel.SetActive(false);
            losePanel.SetActive(false);
            pausePanel.SetActive(false);
            confirmPanel.SetActive(false);

            Debug.Log("UI Panels Generated and Assigned successfully!");
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
