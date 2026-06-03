// ┌─────────────────────────────────────────────────────────────────────────┐
// │  GameStateExporter — DEVELOPMENT / EDITOR ONLY                          │
// │  This file is STRIPPED from Release builds automatically via the         │
// │  #if UNITY_EDITOR || DEVELOPMENT_BUILD guard below.                      │
// │  It must NEVER be included in a production/store build.                  │
// │  To test: Build Settings → uncheck "Development Build" → verify no       │
// │  game_state.json is created at runtime.                                  │
// └─────────────────────────────────────────────────────────────────────────┘
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MaouSamaTD.Managers;
using MaouSamaTD.UI.Tutorial;

namespace MaouSamaTD.Testing
{
    public class GameStateExporter : MonoBehaviour
    {
        private float _exportInterval = 0.15f;
        private float _timer = 0f;
        private string _jsonPath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            GameObject go = new GameObject("GameStateExporter");
            go.AddComponent<GameStateExporter>();
            DontDestroyOnLoad(go);
            Debug.Log("[GameStateExporter] Auto-initialized and set to DontDestroyOnLoad.");
        }

        private void Awake()
        {
            _jsonPath = FindTesterPath();
            Debug.Log($"[GameStateExporter] Configured json export path: {_jsonPath ?? "NOT FOUND"}");
        }

        private string FindTesterPath()
        {
            string current = Application.dataPath;
            for (int i = 0; i < 6; i++)
            {
                string testerDir = Path.Combine(current, "salavan");
                if (!Directory.Exists(testerDir))
                    testerDir = Path.Combine(current, "Tester");
                if (Directory.Exists(testerDir))
                {
                    return Path.Combine(testerDir, "game_state.json");
                }
                string siblingMaouTester = Path.Combine(current, "Maou-Sama-TD/salavan");
                if (!Directory.Exists(siblingMaouTester))
                    siblingMaouTester = Path.Combine(current, "Maou-Sama-TD/Tester");
                if (Directory.Exists(siblingMaouTester))
                {
                    return Path.Combine(siblingMaouTester, "game_state.json");
                }
                string parent = Path.GetDirectoryName(current);
                if (parent == current || string.IsNullOrEmpty(parent)) break;
                current = parent;
            }
            // Fallback
            string fallback = Path.Combine(Application.dataPath, "../salavan/game_state.json");
            if (!Directory.Exists(Path.GetDirectoryName(fallback)))
                fallback = Path.Combine(Application.dataPath, "../Tester/game_state.json");
            return fallback;
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(_jsonPath)) return;

            _timer += Time.deltaTime;
            if (_timer >= _exportInterval)
            {
                _timer = 0f;
                ExportState();
            }
        }

        private void ExportState()
        {
            try
            {
                var state = new Dictionary<string, object>();
                string activeScene = SceneManager.GetActiveScene().name;
                state["current_scene"] = activeScene;

                // Check dialogue status
                bool isDialogueActive = false;
                var dialogueMgr = FindFirstObjectByType<DialogueManager>();
                if (dialogueMgr != null)
                {
                    isDialogueActive = dialogueMgr.IsDialogueActive;
                }
                else
                {
                    var dialogueUI = FindFirstObjectByType<DialogueUI>();
                    if (dialogueUI != null)
                    {
                        isDialogueActive = dialogueUI.IsShowingDialogue;
                    }
                }
                state["is_dialogue_active"] = isDialogueActive;

                // Gather all active and visible buttons
                var buttonsData = new Dictionary<string, object>();
                var buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                
                foreach (var btn in buttons)
                {
                    if (btn == null || !btn.gameObject.activeInHierarchy || !btn.enabled || !btn.interactable)
                        continue;

                    var rt = btn.GetComponent<RectTransform>();
                    if (rt == null || rt.lossyScale == Vector3.zero)
                        continue;

                    // Check if it's clipped or hidden by CanvasGroup
                    var canvasGroups = btn.GetComponentsInParent<CanvasGroup>();
                    bool isHidden = false;
                    foreach (var cg in canvasGroups)
                    {
                        if (cg.alpha <= 0.01f)
                        {
                            isHidden = true;
                            break;
                        }
                    }
                    if (isHidden) continue;

                    // Get screen position
                    Vector3[] corners = new Vector3[4];
                    rt.GetWorldCorners(corners);
                    Vector3 centerWorld = (corners[0] + corners[2]) * 0.5f;

                    Canvas canvas = btn.GetComponentInParent<Canvas>();
                    Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
                    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, centerWorld);

                    // Convert to reference resolution 1280x720 top-left (0,0)
                    float rx = (screenPoint.x / Screen.width) * 1280f;
                    float ry = (1f - (screenPoint.y / Screen.height)) * 720f;
                    
                    // Width and height in reference resolution
                    float rw = (rt.rect.width * rt.lossyScale.x / Screen.width) * 1280f;
                    float rh = (rt.rect.height * rt.lossyScale.y / Screen.height) * 720f;

                    // Text extraction
                    string btnText = "";
                    var tmp = btn.GetComponentInChildren<TMPro.TMP_Text>();
                    if (tmp != null)
                    {
                        btnText = tmp.text;
                    }
                    else
                    {
                        var txt = btn.GetComponentInChildren<Text>();
                        if (txt != null)
                        {
                            btnText = txt.text;
                        }
                    }

                    string keyName = btn.gameObject.name;
                    
                    // Actual resolution coordinates (Screen.width x Screen.height)
                    float ax = screenPoint.x;
                    float ay = Screen.height - screenPoint.y;
                    float aw = rt.rect.width * rt.lossyScale.x;
                    float ah = rt.rect.height * rt.lossyScale.y;

                    // Minimum resolution coordinates (960x540)
                    float mx = (rx / 1280f) * 960f;
                    float my = (ry / 720f) * 540f;
                    float mw = (rw / 1280f) * 960f;
                    float mh = (rh / 720f) * 540f;

                    // Fullscreen resolution coordinates (Screen.currentResolution.width x Screen.currentResolution.height)
                    float fsw = Screen.currentResolution.width;
                    float fsh = Screen.currentResolution.height;
                    float fx = (rx / 1280f) * fsw;
                    float fy = (ry / 720f) * fsh;
                    float fw = (rw / 1280f) * fsw;
                    float fh = (rh / 720f) * fsh;

                    buttonsData[keyName] = new Dictionary<string, object> {
                        { "x", rx }, { "y", ry }, { "w", rw }, { "h", rh },
                        { "ax", ax }, { "ay", ay }, { "aw", aw }, { "ah", ah },
                        { "mx", mx }, { "my", my }, { "mw", mw }, { "mh", mh },
                        { "fx", fx }, { "fy", fy }, { "fw", fw }, { "fh", fh },
                        { "text", btnText }
                    };
                }

                state["buttons"] = buttonsData;
                state["resolution"] = new Dictionary<string, int> { { "width", Screen.width }, { "height", Screen.height } };

                string serializedJson = SerializeState(state);
                File.WriteAllText(_jsonPath, serializedJson);
            }
            catch (System.Exception)
            {
                // Silent fail
            }
        }

        private string SerializeState(Dictionary<string, object> state)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            sb.Append($"\"current_scene\":\"{state["current_scene"]}\",");
            sb.Append($"\"is_dialogue_active\":{(((bool)state["is_dialogue_active"]) ? "true" : "false")},");
            
            var res = (Dictionary<string, int>)state["resolution"];
            sb.Append($"\"resolution\":{{\"width\":{res["width"]},\"height\":{res["height"]}}},");
            
            sb.Append("\"buttons\":{");
            var btns = (Dictionary<string, object>)state["buttons"];
            bool first = true;
            foreach (var kvp in btns)
            {
                if (!first) sb.Append(",");
                first = false;
                var data = (Dictionary<string, object>)kvp.Value;
                string cleanKey = kvp.Key.Replace("\"", "\\\"");
                float x = (float)data["x"];
                float y = (float)data["y"];
                float w = (float)data["w"];
                float h = (float)data["h"];
                float ax = (float)data["ax"];
                float ay = (float)data["ay"];
                float aw = (float)data["aw"];
                float ah = (float)data["ah"];
                float mx = (float)data["mx"];
                float my = (float)data["my"];
                float mw = (float)data["mw"];
                float mh = (float)data["mh"];
                float fx = (float)data["fx"];
                float fy = (float)data["fy"];
                float fw = (float)data["fw"];
                float fh = (float)data["fh"];
                string text = (string)data["text"];
                
                string cleanText = text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
                
                sb.Append($"\"{cleanKey}\":{{");
                sb.Append($"\"x\":{x.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"y\":{y.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"w\":{w.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"h\":{h.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"ax\":{ax.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"ay\":{ay.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"aw\":{aw.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"ah\":{ah.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"mx\":{mx.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"my\":{my.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"mw\":{mw.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"mh\":{mh.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"fx\":{fx.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"fy\":{fy.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"fw\":{fw.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"fh\":{fh.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"text\":\"{cleanText}\"}}");
            }
            sb.Append("}");
            sb.Append("}");
            return sb.ToString();
        }
    }
} // namespace MaouSamaTD.Testing
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD
