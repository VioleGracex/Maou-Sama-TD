using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace NarrativeStudio
{
    public class NarrativeStudioWindow : EditorWindow
    {
        private string _rootDir = "Assets/docs~";
        private int _currentTab = 0;
        private string[] _tabs = { "Game Bible", "Node Storyboard", "Documents (.md)", "Image Gallery", "Scratchpad" };

        // Game Bible
        private GameBibleData _bibleData;
        private Vector2 _bibleScroll;

        // Node Storyboard
        private StoryBoardWorkspace _workspaceData;
        private int _selectedBoardIndex = 0;
        
        // Node Editor State
        private Vector2 _panOffset = Vector2.zero;
        private float _zoom = 1.0f;
        private StoryElement _draggingNode = null;
        private StoryElement _linkingFromNode = null;
        private Vector2 _dragOffset;
        private Vector2 _mousePosition;

        // Documents
        private Vector2 _docListScroll;
        private Vector2 _docContentScroll;
        private string _selectedDocPath;
        private string _docContent;
        private bool _isDocDirty;

        // Image Gallery
        private Vector2 _galleryScroll;
        private List<string> _imagePaths = new List<string>();
        private Texture2D _previewTexture;
        
        // Scratchpad
        private string _scratchpad;

        [MenuItem("Tools/Narrative Studio (Node Editor)")]
        public static void ShowWindow()
        {
            var window = GetWindow<NarrativeStudioWindow>("Narrative Studio");
            window.minSize = new Vector2(1000, 700);
            window.Show();
        }

        private void OnEnable()
        {
            _scratchpad = EditorPrefs.GetString("NarrativeStudio_Scratchpad", "Jot down ideas...");
            _rootDir = EditorPrefs.GetString("NarrativeStudio_RootDir", "Assets/docs~");
            
            if (!Directory.Exists(_rootDir))
            {
                try { Directory.CreateDirectory(_rootDir); } catch { }
            }
            LoadAllData();
            RefreshImageGallery();
        }

        private void OnDisable()
        {
            EditorPrefs.SetString("NarrativeStudio_Scratchpad", _scratchpad);
            EditorPrefs.SetString("NarrativeStudio_RootDir", _rootDir);
            SaveCurrentDoc();
            SaveAllData();
            
            if (_previewTexture != null) DestroyImmediate(_previewTexture);
        }

        private void LoadAllData()
        {
            string biblePath = Path.Combine(_rootDir, "game_bible.json");
            if (File.Exists(biblePath))
                _bibleData = JsonUtility.FromJson<GameBibleData>(File.ReadAllText(biblePath));
            if (_bibleData == null) _bibleData = new GameBibleData();

            string workspacePath = Path.Combine(_rootDir, "story_workspace.json");
            if (File.Exists(workspacePath))
                _workspaceData = JsonUtility.FromJson<StoryBoardWorkspace>(File.ReadAllText(workspacePath));
            if (_workspaceData == null || _workspaceData.boards == null || _workspaceData.boards.Count == 0)
            {
                _workspaceData = new StoryBoardWorkspace();
                _workspaceData.boards = new List<Board> { new Board { id = System.Guid.NewGuid().ToString(), name = "Main Story Arc" } };
            }
        }

        private void SaveAllData()
        {
            if (_bibleData != null)
                File.WriteAllText(Path.Combine(_rootDir, "game_bible.json"), JsonUtility.ToJson(_bibleData, true));
            
            if (_workspaceData != null)
                File.WriteAllText(Path.Combine(_rootDir, "story_workspace.json"), JsonUtility.ToJson(_workspaceData, true));
        }

        private void OnGUI()
        {
            DrawHeader();
            GUILayout.Space(5);
            
            GUIStyle tabStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };
            _currentTab = GUILayout.Toolbar(_currentTab, _tabs, tabStyle, GUILayout.Height(35));
            GUILayout.Space(5);

            Event e = Event.current;
            _mousePosition = e.mousePosition;

            switch (_currentTab)
            {
                case 0: DrawGameBibleTab(); break;
                case 1: DrawNodeStoryboardTab(e); break;
                case 2: DrawDocumentsTab(); break;
                case 3: DrawImageGalleryTab(); break;
                case 4: DrawScratchpadTab(); break;
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Change Root Folder", EditorStyles.toolbarButton, GUILayout.Width(130)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Narrative Root", _rootDir, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _rootDir = path.StartsWith(Application.dataPath) ? "Assets" + path.Substring(Application.dataPath.Length) : path;
                    LoadAllData();
                    RefreshImageGallery();
                    _selectedDocPath = null;
                }
            }
            GUILayout.Label($" Project Data Root: {_rootDir}", EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save All Progress", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                SaveCurrentDoc();
                SaveAllData();
                EditorPrefs.SetString("NarrativeStudio_Scratchpad", _scratchpad);
                Debug.Log("[NarrativeStudio] All Data Saved.");
            }
            EditorGUILayout.EndHorizontal();
        }

        // --- 1. GAME BIBLE TAB ---
        private void DrawGameBibleTab()
        {
            _bibleScroll = EditorGUILayout.BeginScrollView(_bibleScroll);
            
            EditorGUILayout.LabelField("Project Identity & Lore", new GUIStyle(EditorStyles.largeLabel) { fontStyle = FontStyle.Bold, fontSize = 18 });
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical("box");
            _bibleData.gameTitle = EditorGUILayout.TextField("Game Title", _bibleData.gameTitle);
            _bibleData.gameGenre = EditorGUILayout.TextField("Genre / Type", _bibleData.gameGenre);
            EditorGUILayout.LabelField("Core Premise / Logline");
            _bibleData.logline = EditorGUILayout.TextArea(_bibleData.logline, GUILayout.Height(50));
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
            DrawStringList("Themes & Pillars", _bibleData.themes);
            DrawStringList("Factions", _bibleData.factions);
            
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Key Characters (Components)", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Add Character", GUILayout.Width(150)))
            {
                _bibleData.characters.Add(new CharacterEntry { name = "New Character" });
            }
            for (int i = 0; i < _bibleData.characters.Count; i++)
            {
                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.BeginHorizontal();
                _bibleData.characters[i].name = EditorGUILayout.TextField("Name", _bibleData.characters[i].name, EditorStyles.boldLabel);
                GUI.color = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    _bibleData.characters.RemoveAt(i);
                    GUI.color = Color.white;
                    break;
                }
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
                
                _bibleData.characters[i].role = EditorGUILayout.TextField("Role/Class", _bibleData.characters[i].role);
                EditorGUILayout.LabelField("Description:");
                _bibleData.characters[i].description = EditorGUILayout.TextArea(_bibleData.characters[i].description, GUILayout.Height(40));
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawStringList(string title, List<string> list)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(30))) list.Add("New Entry");
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                list[i] = EditorGUILayout.TextField(list[i]);
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    list.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        // --- 2. NODE STORYBOARD TAB ---
        private void DrawNodeStoryboardTab(Event e)
        {
            EditorGUILayout.BeginHorizontal();

            // Left Sidebar - Board Selection
            EditorGUILayout.BeginVertical("box", GUILayout.Width(200));
            EditorGUILayout.LabelField("Boards (Flows)", EditorStyles.boldLabel);
            if (GUILayout.Button("+ New Board"))
            {
                _workspaceData.boards.Add(new Board { id = System.Guid.NewGuid().ToString(), name = "New Arc" });
                SaveAllData();
            }
            GUILayout.Space(5);
            for (int i = 0; i < _workspaceData.boards.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUIStyle btnStyle = (_selectedBoardIndex == i) ? new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.2f, 0.6f, 1f) } } : GUI.skin.button;
                if (GUILayout.Button(_workspaceData.boards[i].name, btnStyle))
                {
                    _selectedBoardIndex = i;
                    GUI.FocusControl(null);
                }
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("Delete Board", "Are you sure?", "Yes", "No"))
                    {
                        _workspaceData.boards.RemoveAt(i);
                        if (_selectedBoardIndex >= _workspaceData.boards.Count) _selectedBoardIndex = Mathf.Max(0, _workspaceData.boards.Count - 1);
                        SaveAllData();
                        break;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            // Canvas Area
            if (_workspaceData.boards.Count > 0 && _selectedBoardIndex < _workspaceData.boards.Count)
            {
                Board currentBoard = _workspaceData.boards[_selectedBoardIndex];
                
                Rect canvasRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                GUI.Box(canvasRect, "", "CurveEditorBackground");

                HandleNodeEvents(e, currentBoard, canvasRect);

                GUI.BeginGroup(canvasRect);
                
                // Draw Connections
                foreach (var node in currentBoard.elements)
                {
                    if (node.childrenIds != null)
                    {
                        foreach (string childId in node.childrenIds)
                        {
                            StoryElement child = currentBoard.elements.Find(n => n.id == childId);
                            if (child != null)
                            {
                                DrawNodeConnection(node.rect, child.rect);
                            }
                        }
                    }
                }

                // Draw Link Line (if linking)
                if (_linkingFromNode != null)
                {
                    Handles.DrawBezier(
                        _linkingFromNode.rect.center + _panOffset, 
                        e.mousePosition, 
                        _linkingFromNode.rect.center + _panOffset + Vector2.right * 50f, 
                        e.mousePosition - Vector2.right * 50f, 
                        Color.cyan, null, 2f);
                    GUI.changed = true;
                }

                // Draw Nodes
                BeginWindows();
                for (int i = 0; i < currentBoard.elements.Count; i++)
                {
                    StoryElement node = currentBoard.elements[i];
                    Rect displayRect = new Rect(node.rect.x + _panOffset.x, node.rect.y + _panOffset.y, node.rect.width, node.rect.height);
                    displayRect = GUI.Window(i, displayRect, (id) => DrawNodeWindow(id, currentBoard), node.title);
                    node.rect = new Rect(displayRect.x - _panOffset.x, displayRect.y - _panOffset.y, displayRect.width, displayRect.height);
                }
                EndWindows();
                
                GUI.EndGroup();
                
                // Overlay Header
                GUILayout.BeginArea(new Rect(canvasRect.x + 10, canvasRect.y + 10, 300, 30));
                currentBoard.name = EditorGUILayout.TextField(currentBoard.name, new GUIStyle(EditorStyles.toolbarTextField) { fontSize = 14, fontStyle = FontStyle.Bold });
                GUILayout.EndArea();

                // Save if GUI changed
                if (GUI.changed) Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNodeConnection(Rect rect1, Rect rect2)
        {
            Vector2 start = new Vector2(rect1.x + rect1.width, rect1.y + rect1.height / 2) + _panOffset;
            Vector2 end = new Vector2(rect2.x, rect2.y + rect2.height / 2) + _panOffset;
            
            Handles.DrawBezier(
                start, end,
                start + Vector2.right * 50f,
                end - Vector2.right * 50f,
                Color.white, null, 3f);
        }

        private void DrawNodeWindow(int id, Board board)
        {
            StoryElement node = board.elements[id];

            EditorGUILayout.BeginVertical();
            node.title = EditorGUILayout.TextField(node.title, EditorStyles.boldLabel);
            node.content = EditorGUILayout.TextArea(node.content, new GUIStyle(EditorStyles.textArea) { wordWrap = true }, GUILayout.ExpandHeight(true));
            
            // Delete & Link Actions inside the window as a small toolbar
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Link", EditorStyles.miniButtonLeft))
            {
                _linkingFromNode = node;
            }
            if (GUILayout.Button("Del", EditorStyles.miniButtonRight))
            {
                board.elements.Remove(node);
                // Remove references to this node
                foreach(var n in board.elements) n.childrenIds.Remove(node.id);
                SaveAllData();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUI.DragWindow();
        }

        private void HandleNodeEvents(Event e, Board board, Rect canvasRect)
        {
            if (e.type == EventType.MouseDown)
            {
                // Right Click on Canvas to Add Node
                if (e.button == 1 && canvasRect.Contains(e.mousePosition))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Add Story Node"), false, () => {
                        board.elements.Add(new StoryElement {
                            id = System.Guid.NewGuid().ToString(),
                            title = "New Node",
                            rect = new Rect(e.mousePosition.x - canvasRect.x - _panOffset.x, e.mousePosition.y - canvasRect.y - _panOffset.y, 200, 150)
                        });
                        SaveAllData();
                    });
                    menu.ShowAsContext();
                    e.Use();
                }
                
                // Middle click or Alt+Left Click for panning
                if (e.button == 2 || (e.button == 0 && e.alt))
                {
                    _dragOffset = e.mousePosition - _panOffset;
                    e.Use();
                }
                
                // Left click resolving Link
                if (e.button == 0 && _linkingFromNode != null)
                {
                    // Check if clicked on another node
                    bool hit = false;
                    foreach(var node in board.elements)
                    {
                        Rect displayRect = new Rect(node.rect.x + _panOffset.x + canvasRect.x, node.rect.y + _panOffset.y + canvasRect.y, node.rect.width, node.rect.height);
                        if (displayRect.Contains(e.mousePosition) && node != _linkingFromNode)
                        {
                            if (!_linkingFromNode.childrenIds.Contains(node.id))
                            {
                                _linkingFromNode.childrenIds.Add(node.id);
                                SaveAllData();
                            }
                            hit = true;
                            break;
                        }
                    }
                    _linkingFromNode = null;
                    e.Use();
                }
            }
            
            if (e.type == EventType.MouseDrag)
            {
                if (e.button == 2 || (e.button == 0 && e.alt))
                {
                    _panOffset = e.mousePosition - _dragOffset;
                    GUI.changed = true;
                }
            }
        }

        // --- 3. DOCUMENTS TAB ---
        private void DrawDocumentsTab()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Markdown Files");
            if (GUILayout.Button("+ New", EditorStyles.toolbarButton, GUILayout.Width(50))) CreateNewDoc();
            EditorGUILayout.EndHorizontal();

            _docListScroll = EditorGUILayout.BeginScrollView(_docListScroll, "box");
            if (Directory.Exists(_rootDir))
            {
                string[] files = Directory.GetFiles(_rootDir, "*.md", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    string relativePath = file.Replace(_rootDir, "").TrimStart('/', '\\');
                    GUIStyle style = (_selectedDocPath == file) ? new GUIStyle(EditorStyles.boldLabel) { normal = new GUIStyleState() { textColor = new Color(0.3f, 0.7f, 1f) } } : EditorStyles.label;
                    
                    if (GUILayout.Button(relativePath, style))
                    {
                        SaveCurrentDoc();
                        _selectedDocPath = file;
                        _docContent = File.ReadAllText(file);
                        _isDocDirty = false;
                        GUI.FocusControl(null);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            if (!string.IsNullOrEmpty(_selectedDocPath) && File.Exists(_selectedDocPath))
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label(Path.GetFileName(_selectedDocPath), EditorStyles.boldLabel);
                if (_isDocDirty) GUILayout.Label("*Unsaved", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Save Document", EditorStyles.toolbarButton, GUILayout.Width(100))) SaveCurrentDoc();
                EditorGUILayout.EndHorizontal();

                _docContentScroll = EditorGUILayout.BeginScrollView(_docContentScroll);
                EditorGUI.BeginChangeCheck();
                
                GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true, fontSize = 13 };
                _docContent = EditorGUILayout.TextArea(_docContent, textAreaStyle, GUILayout.ExpandHeight(true));
                if (EditorGUI.EndChangeCheck()) _isDocDirty = true;
                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select or create a markdown file to edit.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void CreateNewDoc()
        {
            string path = EditorUtility.SaveFilePanel("Create New Markdown", _rootDir, "new_chapter", "md");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, "# New Document\n");
                _selectedDocPath = path;
                _docContent = File.ReadAllText(path);
                _isDocDirty = false;
            }
        }

        private void SaveCurrentDoc()
        {
            if (!string.IsNullOrEmpty(_selectedDocPath) && _isDocDirty)
            {
                File.WriteAllText(_selectedDocPath, _docContent);
                _isDocDirty = false;
            }
        }

        // --- 4. IMAGE GALLERY TAB ---
        private void RefreshImageGallery()
        {
            _imagePaths.Clear();
            if (Directory.Exists(_rootDir))
            {
                var files = Directory.GetFiles(_rootDir, "*.*", SearchOption.AllDirectories)
                    .Where(s => s.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) || 
                                s.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase) || 
                                s.EndsWith(".jpeg", System.StringComparison.OrdinalIgnoreCase));
                _imagePaths.AddRange(files);
            }
        }

        private void DrawImageGalleryTab()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Image Assets");
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60))) RefreshImageGallery();
            EditorGUILayout.EndHorizontal();

            _galleryScroll = EditorGUILayout.BeginScrollView(_galleryScroll, "box");
            if (_imagePaths.Count == 0) GUILayout.Label("No images found in root folder.", EditorStyles.miniLabel);
            
            foreach (string path in _imagePaths)
            {
                string relativePath = path.Replace(_rootDir, "").TrimStart('/', '\\');
                if (GUILayout.Button(relativePath, EditorStyles.label))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    if (_previewTexture != null) DestroyImmediate(_previewTexture);
                    _previewTexture = new Texture2D(2, 2);
                    _previewTexture.LoadImage(bytes);
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box");
            if (_previewTexture != null)
            {
                GUILayout.Label($"Resolution: {_previewTexture.width}x{_previewTexture.height}", EditorStyles.boldLabel);
                Rect rect = GUILayoutUtility.GetAspectRect((float)_previewTexture.width / _previewTexture.height);
                GUI.DrawTexture(rect, _previewTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select an image to view concept art.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        // --- 5. SCRATCHPAD TAB ---
        private void DrawScratchpadTab()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Scratchpad (Auto-saved)", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            GUIStyle style = new GUIStyle(EditorStyles.textArea) { wordWrap = true, fontSize = 14 };
            _scratchpad = EditorGUILayout.TextArea(_scratchpad, style, GUILayout.ExpandHeight(true));
        }
    }

    // --- DATA STRUCTURES ---

    [System.Serializable]
    public class GameBibleData
    {
        public string gameTitle = "My New Game";
        public string gameGenre = "RPG";
        public string logline = "";
        public List<string> themes = new List<string>();
        public List<string> factions = new List<string>();
        public List<CharacterEntry> characters = new List<CharacterEntry>();
    }

    [System.Serializable]
    public class CharacterEntry
    {
        public string name;
        public string role;
        public string description;
    }

    [System.Serializable]
    public class StoryElement
    {
        public string id;
        public string title;
        public string content = "";
        public Rect rect = new Rect(100, 100, 200, 150);
        public List<string> childrenIds = new List<string>();
    }

    [System.Serializable]
    public class Board
    {
        public string id;
        public string name;
        public List<StoryElement> elements = new List<StoryElement>();
    }

    [System.Serializable]
    public class StoryBoardWorkspace
    {
        public List<Board> boards = new List<Board>();
    }
}
