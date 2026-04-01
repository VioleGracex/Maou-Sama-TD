using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using AiImageGenerator;

namespace AiImageGenerator.Editor
{
    [InitializeOnLoad]
    public class AiImageGeneratorWindow : EditorWindow
    {
        private AiImageGeneratorConfig _config;
        private int _currentTab = 0;
        private Vector2 _scrollPos;
        
        // Scene Scanning & Pagination
        private List<AiImageGenerator> _foundGenerators = new List<AiImageGenerator>();
        private int _pageIndex = 0;
        private const int _pageSize = 10;

        static AiImageGeneratorWindow()
        {
            EditorApplication.delayCall += InitializeOnStartup;
        }

        private static void InitializeOnStartup()
        {
            AiImageGeneratorConfig config = Resources.Load<AiImageGeneratorConfig>("AiImageGeneratorConfig");
            if (config != null && config.openOnStartup)
            {
                if (!SessionState.GetBool("AiImageGen_Window_Opened", false))
                {
                    ShowWindow();
                    SessionState.SetBool("AiImageGen_Window_Opened", true);
                }
            }
        }

        [MenuItem("Tools/AI Image Generation Rules")]
        public static void ShowWindow()
        {
            var window = GetWindow<AiImageGeneratorWindow>("AI Image Rules");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (_config == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:AiImageGeneratorConfig");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _config = AssetDatabase.LoadAssetAtPath<AiImageGeneratorConfig>(path);
                }
            }
        }

        private void OnFocus()
        {
            if (_currentTab == 4) ScanScene();
        }

        private void OnSelectionChange()
        {
            if (_currentTab == 4) ScanScene();
            Repaint();
        }

        private void OnGUI()
        {
            LoadConfig();

            if (_config == null)
            {
                DrawNoConfigUI();
                return;
            }

            DrawHeader();

            // Use hardcoded tabs in OnGUI to prevent stale serialization
            string[] tabNames = { "Setup", "Art Direction", "Palettes", "Styles", "History" };
            _currentTab = GUILayout.Toolbar(_currentTab, tabNames, GUILayout.Height(25));
            
            EditorGUILayout.BeginVertical();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            GUILayout.Space(10);

            switch (_currentTab)
            {
                case 0: DrawSetupTab(); break;
                case 1: DrawArtDirectionTab(); break;
                case 2: DrawPalettesTab(); break;
                case 3: DrawStylesTab(); break;
                case 4: DrawHistoryTab(); break;
            }

            GUILayout.Space(10);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_config);
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AI IMAGE GENERATOR", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("v1.2.0", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawNoConfigUI()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.HelpBox("No AiImageGeneratorConfig found in project. Please create one to manage your generation rules.", MessageType.Warning);
            if (GUILayout.Button("Create New Config Asset", GUILayout.Height(30)))
            {
                CreateNewConfig();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSetupTab()
        {
            var status = GetMCPStatus();

            // Storage Settings (Moved here)
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Global Storage & Naming", EditorStyles.boldLabel);
            _config.defaultSavePath = EditorGUILayout.TextField("Default Save Path", _config.defaultSavePath);
            _config.useAutomaticNaming = EditorGUILayout.Toggle("Auto Naming", _config.useAutomaticNaming);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Plugin Setup & Integration
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Integration Checklist", EditorStyles.boldLabel);
            
            DrawCheckItem("Unity MCP Plugin", "Required for bridge", status.pluginInstalled);
            DrawCheckItem("MCP Server", status.serverRunning ? $"Running on {status.url}" : "Stopped (Port 8080)", status.serverRunning);
            DrawCheckItem("Active Session", status.sessionActive ? $"Linked to '{status.sessionName}'" : "No active session", status.sessionActive);
            DrawCheckItem("ImageGen Workflows", "Checks for .agents/workflows", status.workflowsPresent);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Run Full System Diagnostic (Chat)", GUILayout.Height(25)))
            {
                Debug.Log("<color=cyan>[AI Image Gen]</color> Running system diagnostic. Please type: 'Verify system readiness' in the Antigravity chat.");
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Session Details
            if (status.pluginInstalled)
            {
                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.LabelField("Server Connection Info", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Protocol:", status.transport);
                EditorGUILayout.LabelField("Endpoint:", status.url);
                EditorGUILayout.LabelField("Active Profile:", status.sessionName);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            // AI Model Configuration
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("AI Model Configuration", EditorStyles.boldLabel);
            _config.selectedModel = (AiImageGeneratorConfig.GeminiModel)EditorGUILayout.EnumPopup("Refinement Model", _config.selectedModel);
            EditorGUILayout.HelpBox("Select the Gemini model used for project context analysis and prompt expansion. 'Pro' is smarter but 'Flash' is faster.", MessageType.None);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Tool Settings
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Interface Settings", EditorStyles.boldLabel);
            _config.openOnStartup = EditorGUILayout.Toggle("Open on Startup", _config.openOnStartup);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Installation Helper
            EditorGUILayout.BeginVertical("helpbox");
            if (!status.serverRunning)
            {
                EditorGUILayout.HelpBox("The Unity MCP server is not responding. Please open the 'MCP For Unity' window and click 'Start Server'.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("System appears healthy. If tools fail, check the Antigravity console for errors.", MessageType.Info);
            }
            
            if (GUILayout.Button("Get Latest Unity MCP (GitHub)", GUILayout.Height(25)))
            {
                Application.OpenURL("https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main");
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawArtDirectionTab()
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Core Project Context", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Describe your game's art style, theme, and setting here. This is automatically prepended to all prompts.", MessageType.None);
            _config.projectContext = EditorGUILayout.TextArea(_config.projectContext, GUILayout.Height(100));
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Global Negative Prompts", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Keywords to exclude from every generation (e.g. text, blurry, low-quality).", MessageType.None);
            _config.globalNegativePrompt = EditorGUILayout.TextArea(_config.globalNegativePrompt, GUILayout.Height(80));
            EditorGUILayout.EndVertical();
        }

        private struct MCPStatus
        {
            public bool pluginInstalled;
            public bool serverRunning;
            public bool sessionActive;
            public bool workflowsPresent;
            public string sessionName;
            public string url;
            public string transport;
        }

        private MCPStatus GetMCPStatus()
        {
            MCPStatus status = new MCPStatus();
            status.sessionName = "None";
            status.url = "http://localhost:8080";
            status.transport = "HTTP Local";

            // 1. Check Plugin Installation via Robust Reflection
            System.Type serverType = GetTypeFromAssemblies("MCPForUnity.MCPServer");
            status.pluginInstalled = serverType != null;

            if (status.pluginInstalled)
            {
                // 2. Check Server Running
                try {
                    var instanceProp = serverType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    var instance = instanceProp?.GetValue(null);
                    if (instance != null) {
                        var isRunningProp = serverType.GetProperty("IsRunning", BindingFlags.Public | BindingFlags.Instance);
                        status.serverRunning = (bool)(isRunningProp?.GetValue(instance) ?? false);
                        
                        var portProp = serverType.GetProperty("Port", BindingFlags.Public | BindingFlags.Instance);
                        int port = (int)(portProp?.GetValue(instance) ?? 8080);
                        status.url = $"http://localhost:{port}";
                    }
                } catch {}

                // 3. Check Session Info
                System.Type sessionType = GetTypeFromAssemblies("MCPForUnity.MCPSession");
                if (sessionType != null) {
                    try {
                        var activeProp = sessionType.GetProperty("IsSessionActive", BindingFlags.Public | BindingFlags.Static);
                        status.sessionActive = (bool)(activeProp?.GetValue(null) ?? false);
                        
                        var nameProp = sessionType.GetProperty("ActiveSessionInstanceName", BindingFlags.Public | BindingFlags.Static);
                        status.sessionName = (string)nameProp?.GetValue(null) ?? "None";
                    } catch {}
                }
            }

            // 4. Check Workflows
            status.workflowsPresent = System.IO.Directory.Exists(System.IO.Path.Combine(Application.dataPath, "../.agents/workflows"));

            return status;
        }

        private System.Type GetTypeFromAssemblies(string typeName)
        {
            var type = System.Type.GetType(typeName);
            if (type != null) return type;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null) return type;
            }
            return null;
        }

        private void DrawCheckItem(string title, string desc, bool status)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.color = status ? Color.green : Color.gray;
            EditorGUILayout.LabelField("●", GUILayout.Width(15));
            GUI.color = Color.white;
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField(desc, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHistoryTab()
        {
            // Section Toggle
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Active Queue", _currentHistorySubTab == 0 ? EditorStyles.miniButtonMid : EditorStyles.miniButton)) _currentHistorySubTab = 0;
            if (GUILayout.Button("Persistent Archive", _currentHistorySubTab == 1 ? EditorStyles.miniButtonMid : EditorStyles.miniButton)) _currentHistorySubTab = 1;
            EditorGUILayout.EndHorizontal();

            if (_currentHistorySubTab == 0)
            {
                DrawActiveQueue();
            }
            else
            {
                DrawPersistentArchive();
            }
        }

        private int _currentHistorySubTab = 0;
        private int _archivePageIndex = 0;

        private void DrawActiveQueue()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Live Scene Queue", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh Pipeline", GUILayout.Width(120))) ScanScene();
            EditorGUILayout.EndHorizontal();

            if (_foundGenerators.Count == 0)
            {
                EditorGUILayout.HelpBox("No active generators found in scene.", MessageType.Info);
                return;
            }

            // Pagination Header
            int totalPages = Mathf.CeilToInt((float)_foundGenerators.Count / _pageSize);
            EditorGUILayout.BeginHorizontal("box");
            if (GUILayout.Button("<", GUILayout.Width(30)) && _pageIndex > 0) _pageIndex--;
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Page {_pageIndex + 1} of {totalPages}");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(">", GUILayout.Width(30)) && _pageIndex < totalPages - 1) _pageIndex++;
            EditorGUILayout.EndHorizontal();

            int startCount = _pageIndex * _pageSize;
            int endCount = Mathf.Min(startCount + _pageSize, _foundGenerators.Count);

            for (int i = startCount; i < endCount; i++)
            {
                DrawGeneratorItem(_foundGenerators[i]);
            }
        }

        private void DrawPersistentArchive()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Persistent History (Log)", EditorStyles.boldLabel);
            if (GUILayout.Button("Clear History", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Clear History?", "This will delete all persistent generation records. This cannot be undone.", "Clear", "Cancel"))
                {
                    _config.history.Clear();
                    EditorUtility.SetDirty(_config);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_config.history == null || _config.history.Count == 0)
            {
                EditorGUILayout.HelpBox("No past generations recorded.", MessageType.Info);
                return;
            }

            // Pagination Header
            int totalPages = Mathf.CeilToInt((float)_config.history.Count / _pageSize);
            EditorGUILayout.BeginHorizontal("box");
            if (GUILayout.Button("<", GUILayout.Width(30)) && _archivePageIndex > 0) _archivePageIndex--;
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Page {_archivePageIndex + 1} of {totalPages} ({_config.history.Count} records)");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(">", GUILayout.Width(30)) && _archivePageIndex < totalPages - 1) _archivePageIndex++;
            EditorGUILayout.EndHorizontal();

            // Reverse iterate to show newest first
            int totalCount = _config.history.Count;
            int startCount = totalCount - 1 - (_archivePageIndex * _pageSize);
            int endCount = Mathf.Max(0, startCount - _pageSize + 1);

            for (int i = startCount; i >= endCount; i--)
            {
                var record = _config.history[i];
                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{record.timestamp}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"[{record.state}]", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField($"Object: {record.objectName}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Prompt: {record.prompt}", EditorStyles.wordWrappedLabel);
                
                if (!string.IsNullOrEmpty(record.resultPath))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Path: {record.resultPath}", EditorStyles.miniLabel);
                    if (GUILayout.Button("Select Asset", GUILayout.Width(100)))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<Object>(record.resultPath);
                        if (asset != null)
                        {
                            Selection.activeObject = asset;
                            EditorGUIUtility.PingObject(asset);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(2);
            }
        }

        private void DrawGeneratorItem(AiImageGenerator gen)
        {
            if (gen == null) return;
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();
            string stateColor = gen.state == AiImageGenerator.GenerationState.Pending ? "#ffea00" :
                               gen.state == AiImageGenerator.GenerationState.Generating ? "#00e5ff" :
                               gen.state == AiImageGenerator.GenerationState.Success ? "#00c853" :
                               gen.state == AiImageGenerator.GenerationState.Error ? "#ff5252" : "#9e9e9e";

            EditorGUILayout.LabelField($"<color={stateColor}>●</color> {gen.gameObject.name}", new GUIStyle(EditorStyles.boldLabel) { richText = true });
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Select", GUILayout.Width(60))) Selection.activeGameObject = gen.gameObject;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField($"Prompt: {gen.prompt}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        private void ScanScene()
        {
            _foundGenerators.Clear();
            _foundGenerators.AddRange(FindObjectsByType<AiImageGenerator>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            _pageIndex = 0;
        }

        private void CreateNewConfig()
        {
            _config = CreateInstance<AiImageGeneratorConfig>();
            if (!AssetDatabase.IsValidFolder("Assets/Plugins/AiImageGenerator/Resources"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Plugins/AiImageGenerator"))
                {
                   AssetDatabase.CreateFolder("Assets/Plugins", "AiImageGenerator");
                }
                AssetDatabase.CreateFolder("Assets/Plugins/AiImageGenerator", "Resources");
            }
            AssetDatabase.CreateAsset(_config, "Assets/Plugins/AiImageGenerator/Resources/AiImageGeneratorConfig.asset");
            AssetDatabase.SaveAssets();
        }

        private void DrawPalettesTab()
        {
            if (_config.palettes == null) _config.palettes = new List<ColorPalette>();

            for (int i = 0; i < _config.palettes.Count; i++)
            {
                EditorGUILayout.BeginVertical("helpbox");
                _config.palettes[i].name = EditorGUILayout.TextField("Palette Name", _config.palettes[i].name);
                
                if (_config.palettes[i].colors == null) _config.palettes[i].colors = new List<Color>();

                EditorGUILayout.BeginHorizontal();
                for (int j = 0; j < _config.palettes[i].colors.Count; j++)
                {
                    _config.palettes[i].colors[j] = EditorGUILayout.ColorField(GUIContent.none, _config.palettes[i].colors[j], false, false, false, GUILayout.Width(40));
                    if (j % 8 == 7) { EditorGUILayout.EndHorizontal(); EditorGUILayout.BeginHorizontal(); }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Color")) _config.palettes[i].colors.Add(Color.white);
                if (GUILayout.Button("Remove Last") && _config.palettes[i].colors.Count > 0) _config.palettes[i].colors.RemoveAt(_config.palettes[i].colors.Count - 1);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Delete Palette", GUILayout.Width(110)))
                {
                    _config.palettes.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }

            if (GUILayout.Button("Add New Palette", GUILayout.Height(30))) _config.palettes.Add(new ColorPalette());
        }

        private void DrawStylesTab()
        {
            if (_config.styles == null) _config.styles = new List<StylePreset>();

            for (int i = 0; i < _config.styles.Count; i++)
            {
                EditorGUILayout.BeginVertical("helpbox");
                _config.styles[i].name = EditorGUILayout.TextField("Style Name", _config.styles[i].name);
                EditorGUILayout.LabelField("Include Prompts:");
                _config.styles[i].includePrompts = EditorGUILayout.TextArea(_config.styles[i].includePrompts, GUILayout.Height(40));
                EditorGUILayout.LabelField("Exclude Prompts:");
                _config.styles[i].excludePrompts = EditorGUILayout.TextArea(_config.styles[i].excludePrompts, GUILayout.Height(40));

                if (GUILayout.Button("Delete Style", GUILayout.Width(110)))
                {
                    _config.styles.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }

            if (GUILayout.Button("Add New Style", GUILayout.Height(30))) _config.styles.Add(new StylePreset());
        }
    }
}
