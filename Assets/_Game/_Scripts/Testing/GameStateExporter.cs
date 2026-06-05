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
using System.Security.Cryptography;
using System.Text;

namespace MaouSamaTD.Testing
{
    public class GameStateExporter : MonoBehaviour
    {
        private float _exportInterval = 0.15f;
        private float _timer = 0f;
        private string _jsonPath;
        private string _mapJsonPath;
        private bool _mapExported = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            GameObject go = new GameObject("GameStateExporter");
            go.AddComponent<GameStateExporter>();
            DontDestroyOnLoad(go);
            Debug.Log("[GameStateExporter] Auto-initialized and set to DontDestroyOnLoad.");
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
            {
                PushEvent($"SceneLoaded:{scene.name}");
                Debug.Log($"[AutoDebug] SceneLoaded: {scene.name}");
            };
        }

        private void Awake()
        {
            _jsonPath = FindTesterPath("game_state.json");
            _mapJsonPath = FindTesterPath("map_state.json");
            Debug.Log($"[GameStateExporter] Configured json export path: {_jsonPath ?? "NOT FOUND"}");
        }

        // ── Static Debug Event Queue ──────────────────────────────────────────
        private static readonly System.Collections.Generic.List<string> _eventQueue =
            new System.Collections.Generic.List<string>();
        private static readonly object _eventLock = new object();

        /// <summary>Push a timestamped debug event visible to Lua via game_state.json debug_events.</summary>
        public static void PushEvent(string eventName)
        {
            lock (_eventLock)
            {
                string stamped = $"{System.DateTime.UtcNow:HH:mm:ss} {eventName}";
                _eventQueue.Add(stamped);
                if (_eventQueue.Count > 30) _eventQueue.RemoveAt(0); // keep last 30
            }
        }

        private string FindTesterPath(string filename)
        {
            string current = Application.dataPath;
            for (int i = 0; i < 6; i++)
            {
                string testerDir = Path.Combine(current, "salavan");
                if (!Directory.Exists(testerDir))
                    testerDir = Path.Combine(current, "Tester");
                if (Directory.Exists(testerDir))
                {
                    return Path.Combine(testerDir, filename);
                }
                string siblingMaouTester = Path.Combine(current, "Maou-Sama-TD/salavan");
                if (!Directory.Exists(siblingMaouTester))
                    siblingMaouTester = Path.Combine(current, "Maou-Sama-TD/Tester");
                if (Directory.Exists(siblingMaouTester))
                {
                    return Path.Combine(siblingMaouTester, filename);
                }
                string parent = Path.GetDirectoryName(current);
                if (parent == current || string.IsNullOrEmpty(parent)) break;
                current = parent;
            }
            // Fallback
            string fallback = Path.Combine(Application.dataPath, $"../salavan/{filename}");
            if (!Directory.Exists(Path.GetDirectoryName(fallback)))
                fallback = Path.Combine(Application.dataPath, $"../Tester/{filename}");
            return fallback;
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(_jsonPath)) return;

            if (!_mapExported)
            {
                var tiles = FindObjectsByType<MaouSamaTD.Grid.Tile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (tiles != null && tiles.Length > 0)
                {
                    ExportMapState(tiles);
                    _mapExported = true;
                }
            }

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

                var elementsData = new Dictionary<string, object>();
                var legacyButtonsData = new Dictionary<string, object>();

                // 1. Canvases
                var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var canvas in canvases)
                {
                    if (canvas == null || !canvas.gameObject.activeInHierarchy || !canvas.enabled) continue;
                    var path = GetGameObjectPath(canvas.gameObject);
                    var data = GetElementData(canvas.gameObject, "Canvas", "", "", true);
                    if (data != null) elementsData[path] = data;
                }

                // 2. Buttons
                var buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var btn in buttons)
                {
                    if (btn == null || !btn.gameObject.activeInHierarchy || !btn.enabled) continue;
                    
                    string btnText = "";
                    var tmp = btn.GetComponentInChildren<TMPro.TMP_Text>();
                    if (tmp != null) btnText = tmp.text;
                    else
                    {
                        var txt = btn.GetComponentInChildren<Text>();
                        if (txt != null) btnText = txt.text;
                    }

                    var path = GetGameObjectPath(btn.gameObject);
                    var data = GetElementData(btn.gameObject, "Button", btnText, "", btn.interactable);
                    if (data != null)
                    {
                        elementsData[path] = data;
                        legacyButtonsData[btn.gameObject.name] = data;
                    }
                }

                // 3. Toggles
                var toggles = FindObjectsByType<Toggle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var toggle in toggles)
                {
                    if (toggle == null || !toggle.gameObject.activeInHierarchy || !toggle.enabled) continue;

                    string toggleText = "";
                    var tmp = toggle.GetComponentInChildren<TMPro.TMP_Text>();
                    if (tmp != null) toggleText = tmp.text;
                    else
                    {
                        var txt = toggle.GetComponentInChildren<Text>();
                        if (txt != null) toggleText = txt.text;
                    }

                    var path = GetGameObjectPath(toggle.gameObject);
                    var data = GetElementData(toggle.gameObject, "Toggle", toggleText, toggle.isOn ? "true" : "false", toggle.interactable);
                    if (data != null) elementsData[path] = data;
                }

                // 4. Legacy InputFields
                var inputFields = FindObjectsByType<InputField>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var input in inputFields)
                {
                    if (input == null || !input.gameObject.activeInHierarchy || !input.enabled) continue;
                    
                    var path = GetGameObjectPath(input.gameObject);
                    var data = GetElementData(input.gameObject, "InputField", "", input.text, input.interactable);
                    if (data != null) elementsData[path] = data;
                }

                // 5. TMPro InputFields
                var tmpInputFields = FindObjectsByType<TMPro.TMP_InputField>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var input in tmpInputFields)
                {
                    if (input == null || !input.gameObject.activeInHierarchy || !input.enabled) continue;
                    
                    var path = GetGameObjectPath(input.gameObject);
                    var data = GetElementData(input.gameObject, "InputField", "", input.text, input.interactable);
                    if (data != null) elementsData[path] = data;
                }

                // 6. UnitDragHandlers (Unit Cards)
                var unitHandlers = FindObjectsByType<MaouSamaTD.UI.UnitDragHandler>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var handler in unitHandlers)
                {
                    if (handler == null || !handler.gameObject.activeInHierarchy || !handler.enabled) continue;
                    
                    var path = GetGameObjectPath(handler.gameObject);
                    var data = GetElementData(handler.gameObject, "UnitCard", "", "", true);
                    if (data != null) elementsData[path] = data;
                }

                // 7. EventTriggers (Custom interactables)
                var eventTriggers = FindObjectsByType<UnityEngine.EventSystems.EventTrigger>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var trigger in eventTriggers)
                {
                    if (trigger == null || !trigger.gameObject.activeInHierarchy || !trigger.enabled) continue;
                    
                    var path = GetGameObjectPath(trigger.gameObject);
                    var data = GetElementData(trigger.gameObject, "EventTrigger", "", "", true);
                    if (data != null) elementsData[path] = data;
                }

                state["elements"] = elementsData;
                state["buttons"] = legacyButtonsData;
                state["resolution"] = new Dictionary<string, int> { { "width", Screen.width }, { "height", Screen.height } };

                // ── unit_button_names: explicit list of active unit button names ──
                var unitButtonNames = new System.Collections.Generic.List<string>();
                foreach (var handler in FindObjectsByType<MaouSamaTD.UI.UnitDragHandler>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (handler != null && handler.gameObject.activeInHierarchy)
                        unitButtonNames.Add(handler.gameObject.name);
                }
                state["unit_button_names"] = unitButtonNames;

                // ── debug_events: last 30 timestamped events ──
                lock (_eventLock)
                {
                    state["debug_events"] = new System.Collections.Generic.List<string>(_eventQueue);
                }

                string serializedJson = SerializeState(state);
                
                // Get key
                string key = GetCommandLineArg("-automation-key");
                if (string.IsNullOrEmpty(key))
                {
                    string keyPath = Path.Combine(Path.GetDirectoryName(_jsonPath), "key.txt");
                    if (File.Exists(keyPath))
                    {
                        key = File.ReadAllText(keyPath).Trim();
                    }
                }

                if (string.IsNullOrEmpty(key))
                {
                    return; // No key, do not write state
                }

                string encrypted = EncryptAES256(serializedJson, key);
                if (!string.IsNullOrEmpty(encrypted))
                {
                    string tempPath = _jsonPath + ".tmp";
                    File.WriteAllText(tempPath, encrypted);

                    // Check if the target file exists and delete it to mimic 'overwrite = true'
                    if (File.Exists(_jsonPath))
                    {
                        File.Delete(_jsonPath);
                    }

                    File.Move(tempPath, _jsonPath);
                }
            }
            catch (System.Exception)
            {
                // Silent fail
            }
        }

        private void ExportMapState(MaouSamaTD.Grid.Tile[] tiles)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append("{\"tiles\":[");
                bool first = true;
                foreach (var tile in tiles)
                {
                    if (tile == null || !tile.gameObject.activeInHierarchy || tile.Type == MaouSamaTD.Levels.TileType.None) continue;

                    if (!first) sb.Append(",");
                    first = false;

                    Vector3 pos = tile.transform.position;
                    // Precalculate screen coordinate using main camera
                    Camera cam = Camera.main;
                    float screenX = 0f;
                    float screenY = 0f;
                    if (cam != null)
                    {
                        Vector3 screenPoint = cam.WorldToScreenPoint(pos);
                        screenX = screenPoint.x;
                        screenY = Screen.height - screenPoint.y; // invert Y for UI space
                    }

                    sb.Append("{");
                    sb.Append($"\"id\":\"Tile_{tile.Coordinate.x}_{tile.Coordinate.y}\",");
                    sb.Append($"\"gridX\":{tile.Coordinate.x},");
                    sb.Append($"\"gridY\":{tile.Coordinate.y},");
                    sb.Append($"\"worldX\":{pos.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},");
                    sb.Append($"\"worldY\":{pos.y.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},");
                    sb.Append($"\"worldZ\":{pos.z.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},");
                    sb.Append($"\"screenX\":{screenX.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)},");
                    sb.Append($"\"screenY\":{screenY.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}");
                    sb.Append("}");
                }
                sb.Append("]}");

                // Get key
                string key = GetCommandLineArg("-automation-key");
                if (string.IsNullOrEmpty(key))
                {
                    string keyPath = Path.Combine(Path.GetDirectoryName(_mapJsonPath), "key.txt");
                    if (File.Exists(keyPath))
                    {
                        key = File.ReadAllText(keyPath).Trim();
                    }
                }

                if (!string.IsNullOrEmpty(key))
                {
                    string encrypted = EncryptAES256(sb.ToString(), key);
                    if (!string.IsNullOrEmpty(encrypted))
                    {
                        File.WriteAllText(_mapJsonPath, encrypted);
                    }
                }
            }
            catch (System.Exception)
            {
                // Silent fail
            }
        }

        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform;
            while (current.parent != null)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }
            return path;
        }

        private Dictionary<string, object> GetElementData(GameObject go, string type, string textVal, string stateVal, bool isInteractable)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null || rt.lossyScale == Vector3.zero)
                return null;

            if (!go.activeInHierarchy)
                return null;

            var canvasGroups = go.GetComponentsInParent<CanvasGroup>();
            foreach (var cg in canvasGroups)
            {
                if (cg.alpha <= 0.01f)
                {
                    return null;
                }
            }

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 centerWorld = (corners[0] + corners[2]) * 0.5f;

            Canvas canvas = go.GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, centerWorld);

            float rx = (screenPoint.x / Screen.width) * 1280f;
            float ry = (1f - (screenPoint.y / Screen.height)) * 720f;
            float rw = (rt.rect.width * rt.lossyScale.x / Screen.width) * 1280f;
            float rh = (rt.rect.height * rt.lossyScale.y / Screen.height) * 720f;

            float ax = screenPoint.x;
            float ay = Screen.height - screenPoint.y;
            float aw = rt.rect.width * rt.lossyScale.x;
            float ah = rt.rect.height * rt.lossyScale.y;

            float mx = (rx / 1280f) * 960f;
            float my = (ry / 720f) * 540f;
            float mw = (rw / 1280f) * 960f;
            float mh = (rh / 720f) * 540f;

            float fsw = Screen.currentResolution.width;
            float fsh = Screen.currentResolution.height;
            float fx = (rx / 1280f) * fsw;
            float fy = (ry / 720f) * fsh;
            float fw = (rw / 1280f) * fsw;
            float fh = (rh / 720f) * fsh;

            return new Dictionary<string, object> {
                { "id", go.name },
                { "path", GetGameObjectPath(go) },
                { "type", type },
                { "x", rx }, { "y", ry }, { "w", rw }, { "h", rh },
                { "ax", ax }, { "ay", ay }, { "aw", aw }, { "ah", ah },
                { "mx", mx }, { "my", my }, { "mw", mw }, { "mh", mh },
                { "fx", fx }, { "fy", fy }, { "fw", fw }, { "fh", fh },
                { "text", textVal ?? "" },
                { "value", stateVal ?? "" },
                { "visible", true },
                { "interactable", isInteractable }
            };
        }

        private string GetCommandLineArg(string name)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private string EncryptAES256(string plainText, string base64Key)
        {
            try
            {
                byte[] keyBytes = System.Convert.FromBase64String(base64Key);
                if (keyBytes.Length != 32)
                {
                    return null;
                }

                using (Aes aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.GenerateIV();
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream())
                    {
                        ms.Write(aes.IV, 0, aes.IV.Length);

                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        using (var sw = new StreamWriter(cs, Encoding.UTF8))
                        {
                            sw.Write(plainText);
                        }

                        return System.Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameStateExporter] Encryption error: {ex.Message}");
                return null;
            }
        }

        private string SerializeElementData(string cleanKey, Dictionary<string, object> data)
        {
            var sb = new System.Text.StringBuilder();
            string id = (string)data["id"];
            string path = (string)data["path"];
            string type = (string)data["type"];
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
            string value = (string)data["value"];
            bool visible = (bool)data["visible"];
            bool interactable = (bool)data["interactable"];

            string cleanId = id.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string cleanPath = path.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string cleanText = text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
            string cleanValue = value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

            sb.Append($"\"{cleanKey}\":{{");
            sb.Append($"\"id\":\"{cleanId}\",");
            sb.Append($"\"path\":\"{cleanPath}\",");
            sb.Append($"\"type\":\"{type}\",");
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
            sb.Append($"\"text\":\"{cleanText}\",");
            sb.Append($"\"value\":\"{cleanValue}\",");
            sb.Append($"\"visible\":{(visible ? "true" : "false")},");
            sb.Append($"\"interactable\":{(interactable ? "true" : "false")}}}");

            return sb.ToString();
        }

        private string SerializeState(Dictionary<string, object> state)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            sb.Append($"\"current_scene\":\"{state["current_scene"]}\",");
            sb.Append($"\"is_dialogue_active\":{(((bool)state["is_dialogue_active"]) ? "true" : "false")},");
            
            var res = (Dictionary<string, int>)state["resolution"];
            sb.Append($"\"resolution\":{{\"width\":{res["width"]},\"height\":{res["height"]}}},");
            
            // Serialize occupied tiles
            sb.Append("\"occupied_tiles\":[");
            var tiles = FindObjectsByType<MaouSamaTD.Grid.Tile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (tiles != null)
            {
                bool firstTile = true;
                foreach (var tile in tiles)
                {
                    if (tile.IsOccupied && tile.Occupant != null)
                    {
                        if (!firstTile) sb.Append(",");
                        firstTile = false;
                        sb.Append($"{{\"id\":\"Tile_{tile.Coordinate.x}_{tile.Coordinate.y}\",\"occupant\":\"{tile.Occupant.name}\"}}");
                    }
                }
            }
            sb.Append("],");

            // Serialize elements
            sb.Append("\"elements\":{");
            var elements = (Dictionary<string, object>)state["elements"];
            bool first = true;
            foreach (var kvp in elements)
            {
                if (!first) sb.Append(",");
                first = false;
                string cleanKey = kvp.Key.Replace("\"", "\\\"");
                sb.Append(SerializeElementData(cleanKey, (Dictionary<string, object>)kvp.Value));
            }
            sb.Append("},");

            // Serialize buttons
            sb.Append("\"buttons\":{");
            var btns = (Dictionary<string, object>)state["buttons"];
            first = true;
            foreach (var kvp in btns)
            {
                if (!first) sb.Append(",");
                first = false;
                string cleanKey = kvp.Key.Replace("\"", "\\\"");
                sb.Append(SerializeElementData(cleanKey, (Dictionary<string, object>)kvp.Value));
            }
            sb.Append("}");
            
            sb.Append("}");
            return sb.ToString();
        }
    }
}
#endif
