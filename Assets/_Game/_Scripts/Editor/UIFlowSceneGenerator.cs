using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using MaouSamaTD.UI;
using MaouSamaTD.UI.MainMenu;

namespace MaouSamaTD.EditorTools
{
    public class UIFlowSceneGenerator : EditorWindow
    {
        [MenuItem("Tools/Maou Sama TD/Generate UI Flow Sandbox")]
        public static void GenerateScene()
        {
            // 1. Create a new empty scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            newScene.name = "Test_UIFlowManager";

            // Camera
            GameObject camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f);

            // Event System
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Canvas
            GameObject canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // UIFlowManager
            GameObject flowObj = new GameObject("UIFlowManager");
            var flowManager = flowObj.AddComponent<UIFlowManager>();

            // --- Panel 1: Briefing ---
            var briefing = CreateMockPanel<BriefingPanel>(canvasObj.transform, "Briefing", new Color(0.2f, 0.3f, 0.4f, 1f));
            var bSO = new SerializedObject(briefing.Script);
            bSO.FindProperty("_visualRoot").objectReferenceValue = briefing.Visuals;
            bSO.ApplyModifiedProperties();

            // --- Panel 2: Readiness ---
            var readiness = CreateMockPanel<CohortManagerPanel>(canvasObj.transform, "MissionReadiness", new Color(0.4f, 0.2f, 0.3f, 1f));
            var rSO = new SerializedObject(readiness.Script);
            rSO.FindProperty("_visualRoot").objectReferenceValue = readiness.Visuals;
            rSO.ApplyModifiedProperties();

            // --- Panel 3: Barracks ---
            var barracks = CreateMockPanel<MaouSamaTD.UI.Vassals.VassalsBarracksPanel>(canvasObj.transform, "Barracks", new Color(0.3f, 0.4f, 0.2f, 1f));
            var baSO = new SerializedObject(barracks.Script);
            baSO.FindProperty("_visualRoot").objectReferenceValue = barracks.Visuals;
            baSO.ApplyModifiedProperties();

            // Hook up "Next" buttons (Since these panels don't natively expose simple triggers without payloads, we simulate mock payloads)
            // But we actually just want a clean pure UI test. So we'll inject generic buttons under their visuals that bypass the payloads and just call FlowManager.

            // Briefing -> Readiness
            AddNavigationButton(briefing.Visuals.transform, "Go to Readiness", new Vector2(0, -100), () => 
            {
                UIFlowManager.Instance.OpenPanel(readiness.Script);
            });

            // Readiness -> Barracks
            AddNavigationButton(readiness.Visuals.transform, "Go to Barracks", new Vector2(0, -100), () => 
            {
                UIFlowManager.Instance.OpenPanel(barracks.Script);
            });

            // Make sure everything is hidden initially
            briefing.Visuals.SetActive(false);
            readiness.Visuals.SetActive(false);
            barracks.Visuals.SetActive(false);
            
            // Auto open Briefing
            AddNavigationButton(canvasObj.transform, "START UI FLOW (Open Briefing)", new Vector2(0, 0), () => 
            {
                UIFlowManager.Instance.OpenPanel(briefing.Script);
            });

            EditorSceneManager.MarkSceneDirty(newScene);
            Debug.Log("<color=green>UI Flow Sandbox Scene successfully generated! Press Play and click START UI FLOW.</color>");
        }

        private class MockPanelData<T> where T : MonoBehaviour
        {
            public T Script;
            public GameObject Visuals;
        }

        private static MockPanelData<T> CreateMockPanel<T>(Transform parent, string name, Color bgColor) where T : MonoBehaviour
        {
            GameObject controller = new GameObject($"{name}Controller", typeof(T));
            controller.transform.SetParent(parent, false);

            GameObject visuals = new GameObject($"{name}Visuals", typeof(RectTransform), typeof(Image));
            visuals.transform.SetParent(controller.transform, false);
            
            var vRect = visuals.GetComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;
            vRect.sizeDelta = Vector2.zero;
            visuals.GetComponent<Image>().color = bgColor;

            // Title
            GameObject textObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(visuals.transform, false);
            var text = textObj.GetComponent<TextMeshProUGUI>();
            text.text = name;
            text.fontSize = 72;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 300);

            // Back Button
            AddNavigationButton(visuals.transform, "<- Back", new Vector2(0, -300), () => 
            {
                UIFlowManager.Instance.GoBack();
            });

            return new MockPanelData<T> 
            {
                Script = controller.GetComponent<T>(),
                Visuals = visuals
            };
        }

        private static void AddNavigationButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = new GameObject($"Btn_{label.Replace(" ", "")}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 80);
            rect.anchoredPosition = position;
            btnObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var btn = btnObj.GetComponent<Button>();
            
            // Store the action persistently using a proxy script in real playmode, 
            // but for Editor gen we'll attach a tiny runtime script to handle it since AddListener closures don't serialize.
            var proxy = btnObj.AddComponent<MockButtonActionProxy>();
            proxy.ActionName = label;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            var textInfo = textObj.GetComponent<TextMeshProUGUI>();
            textInfo.text = label;
            textInfo.fontSize = 32;
            textInfo.alignment = TextAlignmentOptions.Center;
            textInfo.color = Color.white;
            textObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 80);
        }
    }

    // Helper to survive Editor -> Playmode transition
    public class MockButtonActionProxy : MonoBehaviour
    {
        public string ActionName;

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() => 
            {
                if (ActionName.Contains("Back")) UIFlowManager.Instance.GoBack();
                else if (ActionName.Contains("START")) FindFirstObjectByType<BriefingPanel>().Open();
                else if (ActionName.Contains("Readiness")) UIFlowManager.Instance.OpenPanel(FindFirstObjectByType<CohortManagerPanel>());
                else if (ActionName.Contains("Barracks")) UIFlowManager.Instance.OpenPanel(FindFirstObjectByType<MaouSamaTD.UI.Vassals.VassalsBarracksPanel>());
            });
        }
    }
}
