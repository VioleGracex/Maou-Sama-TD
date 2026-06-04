using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace AutomatedTesting.Editor
{
    public class UIAutomatorWindow : EditorWindow
    {
        [System.Serializable]
        public class UIElementEntry
        {
            public string Path;
            public string Type;
            public Vector2 Coordinates;

            public UIElementEntry(string path, string type, Vector2 coordinates)
            {
                Path = path;
                Type = type;
                Coordinates = coordinates;
            }
        }

        [System.Serializable]
        public class UIConfigWrapper
        {
            public List<UIElementEntry> entries = new List<UIElementEntry>();
        }

        private int currentTab = 0;
        private string[] tabHeaders = { "Capture Mode (Runtime)", "Static Mode (Editor)" };

        private List<SceneAsset> targetScenes = new List<SceneAsset>();
        private List<DefaultAsset> targetFolders = new List<DefaultAsset>();
        private List<GameObject> targetPrefabs = new List<GameObject>();

        private string exportPath = "Assets/UIConfig.json";
        
        // Log of captured elements
        private List<UIElementEntry> capturedElements = new List<UIElementEntry>();
        private Vector2 scrollPos;

        private string searchQuery = "";
        private bool useTwoColumns = true;
        private Dictionary<string, bool> groupFoldouts = new Dictionary<string, bool>();

        private bool showSettings = false;
        private bool captureButtons = true;
        private bool captureToggles = true;
        private bool captureSliders = true;
        private bool captureInputFields = true;
        private bool captureDropdowns = true;

        private bool isAutoCapturing = false;
        private double nextCaptureTime = 0;

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (isAutoCapturing && Application.isPlaying && EditorApplication.timeSinceStartup > nextCaptureTime)
            {
                ExtractRuntime(silent: true);
                nextCaptureTime = EditorApplication.timeSinceStartup + 0.5; // Every 0.5 seconds
                Repaint();
            }
        }

        [MenuItem("Tools/Testing/UI Automator Extractor")]
        public static void ShowWindow()
        {
            GetWindow<UIAutomatorWindow>("UI Automator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Automated UI Coordinate Extractor", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Export Path Configuration
            EditorGUILayout.BeginHorizontal();
            exportPath = EditorGUILayout.TextField("Export Path", exportPath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string path = EditorUtility.SaveFilePanel("Save Export File", "Assets", "UIConfig", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    // Make it relative to project folder if possible
                    if (path.StartsWith(Application.dataPath))
                    {
                        exportPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        exportPath = path;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            
            showSettings = EditorGUILayout.Foldout(showSettings, "Capture Settings & Filters", true, EditorStyles.foldoutHeader);
            if (showSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Capture these UI elements:", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                captureButtons = EditorGUILayout.ToggleLeft("Buttons", captureButtons, GUILayout.Width(100));
                captureToggles = EditorGUILayout.ToggleLeft("Toggles", captureToggles, GUILayout.Width(100));
                captureSliders = EditorGUILayout.ToggleLeft("Sliders", captureSliders, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                captureInputFields = EditorGUILayout.ToggleLeft("Input Fields", captureInputFields, GUILayout.Width(100));
                captureDropdowns = EditorGUILayout.ToggleLeft("Dropdowns", captureDropdowns, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(10);
            
            // Tabs
            currentTab = GUILayout.Toolbar(currentTab, tabHeaders);
            GUILayout.Space(10);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            if (currentTab == 0)
            {
                DrawCaptureMode();
            }
            else
            {
                DrawStaticMode();
            }
            
            GUILayout.Space(20);
            DrawCaptureLog();

            EditorGUILayout.EndScrollView();
            
            GUILayout.Space(10);

            // Save Config Button
            if (capturedElements.Count > 0)
            {
                if (GUILayout.Button($"Export {capturedElements.Count} Captured Elements", GUILayout.Height(30)))
                {
                    ExportData();
                }
                
                if (GUILayout.Button("Clear Capture Log", GUILayout.Height(20)))
                {
                    if (EditorUtility.DisplayDialog("Clear Log", "Are you sure you want to clear the captured elements?", "Yes", "No"))
                    {
                        capturedElements.Clear();
                    }
                }
            }
        }

        private void DrawCaptureMode()
        {
            EditorGUILayout.HelpBox("Run this while in Play Mode to scan currently active Canvas elements. Captures append to the log below.", MessageType.Info);
            
            GUI.enabled = Application.isPlaying;
            
            EditorGUILayout.BeginHorizontal();
            if (!isAutoCapturing)
            {
                if (GUILayout.Button("Start Auto-Capture", GUILayout.Height(40)))
                {
                    isAutoCapturing = true;
                    nextCaptureTime = 0;
                }
            }
            else
            {
                if (GUILayout.Button("Pause Auto-Capture", GUILayout.Height(40)))
                {
                    isAutoCapturing = false;
                }
            }
            
            if (GUILayout.Button("Manual Capture", GUILayout.Height(40)))
            {
                ExtractRuntime(silent: false);
            }
            EditorGUILayout.EndHorizontal();
            
            GUI.enabled = true;

            if (isAutoCapturing)
            {
                EditorGUILayout.HelpBox("🔴 AUTO-CAPTURE RUNNING... Navigate through your UI to capture elements.", MessageType.Warning);
            }
        }

        private void DrawCaptureLog()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Capture Log", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            useTwoColumns = EditorGUILayout.ToggleLeft("2-Column Layout", useTwoColumns, GUILayout.Width(130));
            EditorGUILayout.EndHorizontal();

            searchQuery = EditorGUILayout.TextField("Search:", searchQuery);
            GUILayout.Space(10);

            if (capturedElements.Count == 0)
            {
                EditorGUILayout.HelpBox("No elements captured yet.", MessageType.None);
                return;
            }

            Dictionary<string, List<UIElementEntry>> groups = new Dictionary<string, List<UIElementEntry>>();
            for (int i = 0; i < capturedElements.Count; i++)
            {
                var entry = capturedElements[i];
                if (!string.IsNullOrEmpty(searchQuery) && 
                    !entry.Path.ToLower().Contains(searchQuery.ToLower()) && 
                    !entry.Type.ToLower().Contains(searchQuery.ToLower()))
                {
                    continue; // Skip due to search
                }

                string rootContext = "Unknown";
                int slashIndex = entry.Path.IndexOf('/');
                if (slashIndex > 0)
                {
                    rootContext = entry.Path.Substring(0, slashIndex);
                }

                if (!groups.ContainsKey(rootContext))
                {
                    groups[rootContext] = new List<UIElementEntry>();
                }
                groups[rootContext].Add(entry);
            }

            foreach (var group in groups)
            {
                if (!groupFoldouts.ContainsKey(group.Key))
                {
                    groupFoldouts[group.Key] = false; // default collapsed
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                groupFoldouts[group.Key] = EditorGUILayout.Foldout(groupFoldouts[group.Key], $"{group.Key} ({group.Value.Count} elements)", true, EditorStyles.foldoutHeader);

                if (groupFoldouts[group.Key])
                {
                    if (useTwoColumns)
                    {
                        for (int i = 0; i < group.Value.Count; i += 2)
                        {
                            EditorGUILayout.BeginHorizontal();
                            DrawElementEntry(group.Value[i]);
                            if (i + 1 < group.Value.Count)
                            {
                                DrawElementEntry(group.Value[i + 1]);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    else
                    {
                        for (int i = 0; i < group.Value.Count; i++)
                        {
                            DrawElementEntry(group.Value[i]);
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawElementEntry(UIElementEntry entry)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.BeginHorizontal();
            // Show the full path so it can be edited without breaking the context prefix
            entry.Path = EditorGUILayout.TextField(entry.Path, GUILayout.MinWidth(150), GUILayout.ExpandWidth(true));
            entry.Type = EditorGUILayout.TextField(entry.Type, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X:", GUILayout.Width(15));
            entry.Coordinates.x = EditorGUILayout.FloatField(entry.Coordinates.x, GUILayout.Width(50));
            GUILayout.Space(5);
            GUILayout.Label("Y:", GUILayout.Width(15));
            entry.Coordinates.y = EditorGUILayout.FloatField(entry.Coordinates.y, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(35)))
            {
                capturedElements.Remove(entry);
                GUIUtility.ExitGUI(); // Prevent layout errors during loop
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStaticMode()
        {
            EditorGUILayout.HelpBox("Static extraction processes scenes and prefabs without running the game. Prefab extraction uses a mock Canvas and provides approximate coordinates.", MessageType.Warning);

            // Scenes
            EditorGUILayout.LabelField("Scenes to Scan:", EditorStyles.boldLabel);
            for (int i = 0; i < targetScenes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                targetScenes[i] = (SceneAsset)EditorGUILayout.ObjectField(targetScenes[i], typeof(SceneAsset), false);
                if (GUILayout.Button("X", GUILayout.Width(20))) targetScenes.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Add Scene")) targetScenes.Add(null);

            GUILayout.Space(10);

            // Folders
            EditorGUILayout.LabelField("Folders to Scan (For Prefabs):", EditorStyles.boldLabel);
            for (int i = 0; i < targetFolders.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                targetFolders[i] = (DefaultAsset)EditorGUILayout.ObjectField(targetFolders[i], typeof(DefaultAsset), false);
                if (GUILayout.Button("X", GUILayout.Width(20))) targetFolders.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Add Folder")) targetFolders.Add(null);

            GUILayout.Space(10);

            // Prefabs
            EditorGUILayout.LabelField("Specific Prefabs to Scan:", EditorStyles.boldLabel);
            for (int i = 0; i < targetPrefabs.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                targetPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(targetPrefabs[i], typeof(GameObject), false);
                if (GUILayout.Button("X", GUILayout.Width(20))) targetPrefabs.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Add Prefab")) targetPrefabs.Add(null);

            GUILayout.Space(20);

            if (GUILayout.Button("Extract Statically to Log", GUILayout.Height(40)))
            {
                ExtractStatically();
            }
        }

        private void ExtractStatically()
        {
            List<UIElementEntry> tempDict = new List<UIElementEntry>();

            string currentScenePath = EditorSceneManager.GetActiveScene().path;

            // 1. Process Scenes
            foreach (var sceneAsset in targetScenes)
            {
                if (sceneAsset == null) continue;
                string path = AssetDatabase.GetAssetPath(sceneAsset);
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Canvas.ForceUpdateCanvases();
                ExtractFromRoots(scene.GetRootGameObjects(), tempDict, scene.name);
            }

            // 2. Process Folders for Prefabs
            List<GameObject> prefabsToProcess = new List<GameObject>(targetPrefabs);
            foreach (var folder in targetFolders)
            {
                if (folder == null) continue;
                string folderPath = AssetDatabase.GetAssetPath(folder);
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
                foreach (string guid in guids)
                {
                    string pPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
                    if (prefab != null && !prefabsToProcess.Contains(prefab))
                    {
                        prefabsToProcess.Add(prefab);
                    }
                }
            }

            if (prefabsToProcess.Count > 0)
            {
                Scene mockScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject canvasObj = new GameObject("MockCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                foreach (var prefab in prefabsToProcess)
                {
                    if (prefab == null) continue;
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasObj.transform);
                    Canvas.ForceUpdateCanvases();
                    ExtractFromRoots(new GameObject[] { instance }, tempDict, "Prefab_" + prefab.name);
                    DestroyImmediate(instance);
                }
            }

            if (!string.IsNullOrEmpty(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }

            AppendToLog(tempDict);
        }

        private void ExtractRuntime(bool silent = false)
        {
            List<UIElementEntry> tempDict = new List<UIElementEntry>();
            
            Scene activeScene = SceneManager.GetActiveScene();
            ExtractFromRoots(activeScene.GetRootGameObjects(), tempDict, activeScene.name);
            
            GameObject temp = new GameObject();
            Object.DontDestroyOnLoad(temp);
            Scene dontDestroyScene = temp.scene;
            Object.DestroyImmediate(temp);
            
            if (dontDestroyScene.IsValid())
            {
                ExtractFromRoots(dontDestroyScene.GetRootGameObjects(), tempDict, "DontDestroyOnLoad");
            }

            AppendToLog(tempDict, silent);
        }

        private void ExtractFromRoots(GameObject[] roots, List<UIElementEntry> data, string contextPrefix)
        {
            foreach (GameObject root in roots)
            {
                Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
                foreach (Selectable selectable in selectables)
                {
                    RectTransform rect = selectable.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        string type = selectable.GetType().Name;
                        
                        // Apply filters
                        if (!captureButtons && type.Contains("Button")) continue;
                        if (!captureToggles && type.Contains("Toggle")) continue;
                        if (!captureSliders && type.Contains("Slider")) continue;
                        if (!captureInputFields && type.Contains("Input")) continue;
                        if (!captureDropdowns && type.Contains("Dropdown")) continue;

                        Vector2 screenPos = GetScreenCoordinates(rect);
                        string path = GetGameObjectPath(selectable.gameObject);
                        string key = $"{contextPrefix}/{path}";

                        
                        if (!data.Exists(e => e.Path == key))
                        {
                            data.Add(new UIElementEntry(key, type, screenPos));
                        }
                    }
                }
            }
        }

        private Vector2 GetScreenCoordinates(RectTransform rectTransform)
        {
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas == null) return Vector2.zero;

            // If in Play mode and canvas has a camera, we can rely on standard WorldToScreenPoint
            if (Application.isPlaying && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
            {
                Vector3 worldCenter = rectTransform.TransformPoint(rectTransform.rect.center);
                return RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldCenter);
            }
            else
            {
                // Most robust way to get screen coordinates relative to the Canvas size, ignoring world space.
                // This correctly returns coordinates for inactive objects inside mock canvases.
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                
                Vector3[] corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);
                
                for (int i = 0; i < 4; i++)
                {
                    corners[i] = canvasRect.InverseTransformPoint(corners[i]);
                }
                
                Vector3 center = (corners[0] + corners[2]) / 2f;
                
                // At this point, 'center' is relative to the Canvas pivot.
                // If Canvas pivot is (0.5, 0.5), bottom-left is (-width/2, -height/2).
                // We want screen coordinates where bottom-left is (0,0).
                float screenX = center.x + (canvasRect.rect.width * canvasRect.pivot.x);
                float screenY = center.y + (canvasRect.rect.height * canvasRect.pivot.y);
                
                return new Vector2(screenX, screenY);
            }
        }

        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        private void AppendToLog(List<UIElementEntry> newData, bool silent = false)
        {
            int addedCount = 0;
            foreach (var kvp in newData)
            {
                // Prevent exact duplicates
                if (!capturedElements.Exists(e => e.Path == kvp.Path))
                {
                    capturedElements.Add(new UIElementEntry(kvp.Path, kvp.Type, kvp.Coordinates));
                    addedCount++;
                }
            }
            if (addedCount > 0 && !silent)
            {
                Debug.Log($"Appended {addedCount} new elements to the capture log.");
            }
        }

        private void ExportData()
        {
            UIConfigWrapper wrapper = new UIConfigWrapper();
            wrapper.entries = capturedElements;
            string json = JsonUtility.ToJson(wrapper, true);

            File.WriteAllText(exportPath, json);
            AssetDatabase.Refresh();
            
            Debug.Log($"Successfully exported {capturedElements.Count} UI coordinates to {exportPath}");
        }
    }
}
