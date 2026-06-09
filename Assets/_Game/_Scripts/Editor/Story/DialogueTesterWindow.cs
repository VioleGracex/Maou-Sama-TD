using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using MaouSamaTD.Story;
using MaouSamaTD.Tutorial;
using System.Linq;

namespace MaouSamaTD.Editor.Story
{
    public class DialogueTesterWindow : EditorWindow
    {
        private List<ScriptableObject> allDialogueAssets = new List<ScriptableObject>();
        private ScriptableObject selectedAsset;
        private Vector2 sideScrollPos;
        private Vector2 mainScrollPos;
        private int selectedLineIndex = 0;

        public enum AssetFilter { Both, Story, Tutorial }
        private AssetFilter currentFilter = AssetFilter.Both;

        // UI State
        private bool isMenuOpen = true;
        private float menuAnimValue = 1f; 
        private float sideMenuWidth = 250f;
        private bool isResizing = false;
        private bool wasDragging = false;
        private bool isFullScreen = false;
        private float previewScale = 0.35f; 

        private GUIStyle dialogueBoxStyle;
        private GUIStyle speakerNameStyle;
        private GUIStyle focusHighlightStyle;
        private GUIStyle toolbarButtonStyle;

        [MenuItem("Maou-TD/Dialogue Tester")]
        public static void ShowWindow()
        {
            var window = GetWindow<DialogueTesterWindow>("Dialogue Tester");
            window.TrySelectCurrentSelection();
        }

        public static void ShowWithAsset(ScriptableObject asset)
        {
            var window = GetWindow<DialogueTesterWindow>("Dialogue Tester");
            window.selectedAsset = asset;
            window.selectedLineIndex = 0;
            window.Repaint();
        }

        private void OnEnable()
        {
            RefreshAssetList();
            EditorApplication.update += UpdateAnimation;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateAnimation;
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            if (TrySelectCurrentSelection())
            {
                Repaint();
            }
        }

        private bool TrySelectCurrentSelection()
        {
            if (Selection.activeObject is StoryDataSO || Selection.activeObject is DialogueData)
            {
                selectedAsset = (ScriptableObject)Selection.activeObject;
                selectedLineIndex = 0;
                return true;
            }
            return false;
        }

        private void UpdateAnimation()
        {
            float target = isMenuOpen ? 1f : 0f;
            if (Mathf.Abs(menuAnimValue - target) > 0.001f)
            {
                // Slower animation: 0.1f -> 0.04f
                menuAnimValue = Mathf.Lerp(menuAnimValue, target, 0.04f);
                Repaint();
            }
        }

        private void RefreshAssetList()
        {
            allDialogueAssets.Clear();
            
            IEnumerable<ScriptableObject> stories = new List<ScriptableObject>();
            IEnumerable<ScriptableObject> tutorials = new List<ScriptableObject>();

            if (currentFilter == AssetFilter.Both || currentFilter == AssetFilter.Story)
            {
                stories = AssetDatabase.FindAssets("t:StoryDataSO")
                    .Select(guid => AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)));
            }
            
            if (currentFilter == AssetFilter.Both || currentFilter == AssetFilter.Tutorial)
            {
                tutorials = AssetDatabase.FindAssets("t:DialogueData")
                    .Select(guid => AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            allDialogueAssets = stories.Concat(tutorials)
                .Where(a => a != null)
                .OrderBy(a => a.name)
                .ToList();
        }

        private void OnGUI()
        {
            InitializeStyles();

            if (isFullScreen)
            {
                DrawFullScreenMode();
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // Side Menu
            if (menuAnimValue > 0.01f)
            {
                DrawSideMenu();
            }

            // Separator/Toggle Button - Wider and more comfortable
            DrawMenuToggle();

            // Main Editor
            DrawMainEditor();

            EditorGUILayout.EndHorizontal();
        }

        private void InitializeStyles()
        {
            if (dialogueBoxStyle == null)
            {
                dialogueBoxStyle = new GUIStyle(EditorStyles.helpBox);
                dialogueBoxStyle.fontSize = 14;
                dialogueBoxStyle.padding = new RectOffset(10, 10, 10, 10);
                dialogueBoxStyle.wordWrap = true;

                speakerNameStyle = new GUIStyle(EditorStyles.boldLabel);
                speakerNameStyle.fontSize = 16;
                speakerNameStyle.normal.textColor = new Color(0.8f, 0.6f, 1f); 

                focusHighlightStyle = new GUIStyle();
                focusHighlightStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.5f));

                toolbarButtonStyle = new GUIStyle(EditorStyles.miniButton);
                toolbarButtonStyle.fixedHeight = 25;
            }
        }

        private void DrawSideMenu()
        {
            float width = sideMenuWidth * menuAnimValue;
            EditorGUILayout.BeginVertical(GUILayout.Width(width), GUILayout.ExpandHeight(true));
            
            if (menuAnimValue > 0.8f) 
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("DIALOGUE ASSETS", EditorStyles.whiteMiniLabel);
                
                EditorGUI.BeginChangeCheck();
                currentFilter = (AssetFilter)GUILayout.Toolbar((int)currentFilter, new string[] { "Both", "Story", "Tutorial" });
                if (EditorGUI.EndChangeCheck()) RefreshAssetList();

                if (GUILayout.Button("Refresh List", GUILayout.Height(25))) RefreshAssetList();
                
                EditorGUILayout.Space(2);
                sideScrollPos = EditorGUILayout.BeginScrollView(sideScrollPos, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, "box");
                foreach (var asset in allDialogueAssets)
                {
                    if (asset == null) continue;
                    
                    Rect itemRect = EditorGUILayout.BeginHorizontal();
                    
                    bool isSelected = selectedAsset == asset;
                    GUI.backgroundColor = isSelected ? new Color(0.2f, 0.7f, 0.7f) : Color.white;
                    string prefix = asset is StoryDataSO ? "[Story] " : "[Tutorial] ";
                    
                    GUIStyle itemStyle = new GUIStyle(GUI.skin.button);
                    itemStyle.alignment = TextAnchor.MiddleLeft;
                    itemStyle.fontSize = 11;
                    
                    // Fixed width for button to leave room for trash icon even when names are long
                    float labelWidth = (sideMenuWidth * menuAnimValue) - 65; 
                    if (GUILayout.Button(prefix + asset.name, itemStyle, GUILayout.Height(30), GUILayout.Width(labelWidth)))
                    {
                        selectedAsset = asset;
                        selectedLineIndex = 0;
                        GUI.FocusControl(null);
                    }
                    
                    // Right click menu
                    if (Event.current.type == EventType.ContextClick && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Ping Asset"), false, () => EditorGUIUtility.PingObject(asset));
                        menu.AddItem(new GUIContent("Select in Project"), false, () => Selection.activeObject = asset);
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Delete Asset"), false, () => DeleteAsset(asset));
                        menu.ShowAsContext();
                        Event.current.Use();
                    }
                    
                    GUI.backgroundColor = Color.white;
                    if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), GUILayout.Width(28), GUILayout.Height(30)))
                    {
                        DeleteAsset(asset);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DeleteAsset(ScriptableObject asset)
        {
            if (EditorUtility.DisplayDialog("Delete Asset", $"Are you sure you want to delete '{asset.name}'? This cannot be undone.", "Delete", "Cancel"))
            {
                string path = AssetDatabase.GetAssetPath(asset);
                AssetDatabase.DeleteAsset(path);
                RefreshAssetList();
                if (selectedAsset == asset) selectedAsset = null;
                GUIUtility.ExitGUI();
            }
        }

        private void DrawMenuToggle()
        {
            // Wider handle for better "dock" feel
            Rect toggleRect = GUILayoutUtility.GetRect(22, position.height);
            GUI.Box(toggleRect, "", EditorStyles.helpBox);
            
            // Resize handling
            EditorGUIUtility.AddCursorRect(toggleRect, MouseCursor.ResizeHorizontal);
            
            if (Event.current.type == EventType.MouseDown && toggleRect.Contains(Event.current.mousePosition))
            {
                isResizing = true;
            }
            
            if (isResizing)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    sideMenuWidth = Mathf.Clamp(Event.current.mousePosition.x, 150, 600);
                    wasDragging = true;
                    Repaint();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    isResizing = false;
                    if (wasDragging)
                    {
                        wasDragging = false;
                        Event.current.Use(); // Consume event if we were dragging
                    }
                }
            }

            // Click to toggle (only if not resizing)
            if (!isResizing && Event.current.type == EventType.MouseUp && toggleRect.Contains(Event.current.mousePosition))
            {
                // Simple toggle logic moved here to avoid button consuming events
                isMenuOpen = !isMenuOpen;
                Event.current.Use();
            }

            GUI.Label(toggleRect, isMenuOpen ? "◀" : "▶", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawMainEditor()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // Toolbar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Full Screen Mode", EditorStyles.toolbarButton)) isFullScreen = true;
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Preview Scale:", GUILayout.Width(90));
            previewScale = EditorGUILayout.Slider(previewScale, 0.15f, 0.6f, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            if (selectedAsset == null)
            {
                EditorGUILayout.HelpBox("Select a Story or Tutorial asset from the left menu or Project view.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            mainScrollPos = EditorGUILayout.BeginScrollView(mainScrollPos, false, true);

            // Visual Preview Area
            DrawVisualPreview(false);

            // Line Navigation
            DrawLineNavigation();

            // Line Editor
            DrawLineEditor();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawFullScreenMode()
        {
            DrawVisualPreview(true);
            
            // Overlay Controls
            Rect exitRect = new Rect(20, 20, 150, 40);
            if (GUI.Button(exitRect, "Exit [Esc]", EditorStyles.whiteLargeLabel))
            {
                isFullScreen = false;
            }

            // Keyboard support
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                isFullScreen = false;
                Event.current.Use();
            }

            // Navigation Overlay
            Rect navRect = new Rect(position.width / 2 - 120, position.height - 80, 240, 60);
            GUI.Box(navRect, "", dialogueBoxStyle);
            GUILayout.BeginArea(navRect);
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("◀", GUILayout.Width(100), GUILayout.Height(50))) 
                selectedLineIndex = Mathf.Max(0, selectedLineIndex - 1);
            
            GUILayout.FlexibleSpace();

            int lineCount = GetLineCount();
            bool isLastLine = selectedLineIndex >= lineCount - 1;
            string nextLabel = isLastLine ? "FINISH" : "▶";
            GUI.backgroundColor = isLastLine ? new Color(1f, 0.4f, 0.4f) : Color.white;

            if (GUILayout.Button(nextLabel, GUILayout.Width(100), GUILayout.Height(50))) 
            {
                if (isLastLine)
                {
                    isFullScreen = false;
                }
                else
                {
                    selectedLineIndex++;
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private int GetLineCount()
        {
            if (selectedAsset is StoryDataSO story) return story.Lines.Count;
            if (selectedAsset is DialogueData tutorial) return tutorial.Lines.Count;
            return 0;
        }

        private void DrawVisualPreview(bool full)
        {
            if (selectedAsset == null) return;
            
            string speaker = "";
            string text = "";
            Sprite portraitLeft = null;
            Sprite portraitMiddle = null;
            Sprite portraitRight = null;
            Sprite background = null;
            MaouSamaTD.Tutorial.PortraitFocus focus = MaouSamaTD.Tutorial.PortraitFocus.All;

            if (selectedAsset is StoryDataSO story)
            {
                if (story.Lines.Count == 0) return;
                var line = story.Lines[selectedLineIndex];
                speaker = line.SpeakerName;
                text = line.DialogueText;
                portraitLeft = line.PortraitLeft;
                portraitMiddle = line.PortraitMiddle;
                portraitRight = line.PortraitRight;
                background = line.Background;
                focus = (MaouSamaTD.Tutorial.PortraitFocus)(int)line.Focus;
            }
            else if (selectedAsset is DialogueData tutorial)
            {
                if (tutorial.Lines.Count == 0) return;
                var line = tutorial.Lines[selectedLineIndex];
                speaker = line.SpeakerName;
                text = line.Text;
                portraitLeft = line.LeftPortrait;
                portraitMiddle = line.CenterPortrait;
                portraitRight = line.RightPortrait;
                focus = line.Focus;
            }

            float drawWidth = full ? position.width : position.width - (265 * menuAnimValue);
            float drawHeight = drawWidth * previewScale;
            
            Rect previewRect = GUILayoutUtility.GetRect(drawWidth, drawHeight);
            if (full) previewRect = new Rect(0, 0, position.width, position.height);

            EditorGUI.DrawRect(previewRect, Color.black);

            // 1. Draw Background
            if (background != null)
            {
                GUI.DrawTexture(previewRect, background.texture, ScaleMode.ScaleAndCrop);
            }

            // 2. Draw Portraits
            float portraitWidth = previewRect.width * 0.25f;
            float portraitHeight = previewRect.height * 0.85f;
            float yOffset = previewRect.y + (previewRect.height - portraitHeight);

            DrawPortrait(new Rect(previewRect.x + 20, yOffset, portraitWidth, portraitHeight), 
                portraitLeft, focus == MaouSamaTD.Tutorial.PortraitFocus.Left || focus == MaouSamaTD.Tutorial.PortraitFocus.All || (selectedAsset is StoryDataSO && (int)focus == (int)MaouSamaTD.Story.PortraitFocus.Left));
            
            DrawPortrait(new Rect(previewRect.x + (previewRect.width - portraitWidth) / 2, yOffset, portraitWidth, portraitHeight), 
                portraitMiddle, focus == MaouSamaTD.Tutorial.PortraitFocus.Center || focus == MaouSamaTD.Tutorial.PortraitFocus.All || (selectedAsset is StoryDataSO && (int)focus == (int)MaouSamaTD.Story.PortraitFocus.Middle));
            
            DrawPortrait(new Rect(previewRect.xMax - portraitWidth - 20, yOffset, portraitWidth, portraitHeight), 
                portraitRight, focus == MaouSamaTD.Tutorial.PortraitFocus.Right || focus == MaouSamaTD.Tutorial.PortraitFocus.All || (selectedAsset is StoryDataSO && (int)focus == (int)MaouSamaTD.Story.PortraitFocus.Right));

            // 3. Draw Dialogue Overlay
            float boxWidth = previewRect.width * 0.8f;
            float boxHeight = full ? 150 : 80;
            Rect dialogueRect = new Rect(previewRect.x + (previewRect.width - boxWidth) / 2, previewRect.yMax - boxHeight - 20, boxWidth, boxHeight);
            
            GUI.Box(dialogueRect, "", dialogueBoxStyle);
            GUI.Label(new Rect(dialogueRect.x + 15, dialogueRect.y + 5, 200, 25), speaker, speakerNameStyle);
            GUI.Label(new Rect(dialogueRect.x + 15, dialogueRect.y + 30, dialogueRect.width - 30, boxHeight - 40), text, dialogueBoxStyle);
        }

        private void DrawPortrait(Rect rect, Sprite sprite, bool isFocused)
        {
            if (sprite == null) return;
            Color oldColor = GUI.color;
            if (!isFocused) GUI.color = new Color(0.4f, 0.4f, 0.4f, 1f); 
            GUI.DrawTexture(rect, sprite.texture, ScaleMode.ScaleToFit);
            GUI.color = oldColor;
        }

        private void DrawLineNavigation()
        {
            int lineCount = GetLineCount();
            if (lineCount == 0) return;

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal("box");
            if (GUILayout.Button("<<", GUILayout.Width(40))) selectedLineIndex = 0;
            if (GUILayout.Button("<", GUILayout.Width(40))) selectedLineIndex = Mathf.Max(0, selectedLineIndex - 1);
            EditorGUILayout.LabelField($"Line {selectedLineIndex + 1} / {lineCount}", EditorStyles.centeredGreyMiniLabel);
            if (GUILayout.Button(">", GUILayout.Width(40))) selectedLineIndex = Mathf.Min(lineCount - 1, selectedLineIndex + 1);
            if (GUILayout.Button(">>", GUILayout.Width(40))) selectedLineIndex = lineCount - 1;
            
            GUILayout.FlexibleSpace();
            if (selectedAsset is StoryDataSO story)
            {
                if (GUILayout.Button("+ Add Line"))
                {
                    story.Lines.Insert(selectedLineIndex + 1, new StoryLine());
                    EditorUtility.SetDirty(story);
                }
                if (GUILayout.Button("- Del Line"))
                {
                    if (story.Lines.Count > 1)
                    {
                        story.Lines.RemoveAt(selectedLineIndex);
                        selectedLineIndex = Mathf.Clamp(selectedLineIndex, 0, story.Lines.Count - 1);
                        EditorUtility.SetDirty(story);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLineEditor()
        {
            int lineCount = GetLineCount();
            if (lineCount == 0) return;

            // Unified Line Access
            string speaker = "";
            string text = "";
            
            if (selectedAsset is StoryDataSO story)
            {
                var line = story.Lines[selectedLineIndex];
                speaker = line.SpeakerName;
                text = line.DialogueText;
            }
            else if (selectedAsset is DialogueData tutorial)
            {
                var line = tutorial.Lines[selectedLineIndex];
                speaker = line.SpeakerName;
                text = line.Text;
            }

            // Scene Integration Section
            EditorGUILayout.BeginVertical("helpBox");
            EditorGUILayout.LabelField("Scene Integration (Live Preview)", EditorStyles.boldLabel);
            var sceneUI = (MaouSamaTD.UI.Tutorial.DialogueUI)EditorGUILayout.ObjectField("Scene Dialogue UI", FindAnyObjectByType<MaouSamaTD.UI.Tutorial.DialogueUI>(), typeof(MaouSamaTD.UI.Tutorial.DialogueUI), true);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Auto-Assign UI Fields"))
            {
                if (sceneUI != null) AutoAssignUI(sceneUI);
            }
            if (GUILayout.Button("Apply to Scene UI", GUILayout.Height(25)))
            {
                if (sceneUI != null) ApplyToScene(sceneUI);
            }
            if (Application.isPlaying)
            {
                if (GUILayout.Button("PLAY IN GAME", GUILayout.Height(25)))
                {
                    if (sceneUI != null)
                    {
                        if (selectedAsset is DialogueData playAsset) sceneUI.ShowDialogue(playAsset);
                        else if (selectedAsset is StoryDataSO playStoryAsset) sceneUI.ShowStory(playStoryAsset);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Quick Edit (Active Line)", EditorStyles.boldLabel);
            
            if (selectedAsset is StoryDataSO storyAsset)
            {
                var line = storyAsset.Lines[selectedLineIndex];
                line.SpeakerName = EditorGUILayout.TextField("Speaker Name", line.SpeakerName);
                line.DialogueText = EditorGUILayout.TextArea(line.DialogueText, GUILayout.Height(60));
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
                line.Background = (Sprite)EditorGUILayout.ObjectField("Background", line.Background, typeof(Sprite), false);
                
                EditorGUILayout.BeginHorizontal();
                line.PortraitLeft = (Sprite)EditorGUILayout.ObjectField("Left", line.PortraitLeft, typeof(Sprite), false);
                line.PortraitMiddle = (Sprite)EditorGUILayout.ObjectField("Middle", line.PortraitMiddle, typeof(Sprite), false);
                line.PortraitRight = (Sprite)EditorGUILayout.ObjectField("Right", line.PortraitRight, typeof(Sprite), false);
                EditorGUILayout.EndHorizontal();

                line.Focus = (MaouSamaTD.Story.PortraitFocus)EditorGUILayout.EnumPopup("Focus Position", line.Focus);

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Events & Audio", EditorStyles.boldLabel);
                line.EventID = EditorGUILayout.TextField("Event ID", line.EventID);
                line.VoiceClip = (AudioClip)EditorGUILayout.ObjectField("Voice Clip", line.VoiceClip, typeof(AudioClip), false);
            }
            else if (selectedAsset is DialogueData tutorialAsset)
            {
                var line = tutorialAsset.Lines[selectedLineIndex];
                line.SpeakerName = EditorGUILayout.TextField("Speaker Name", line.SpeakerName);
                line.Text = EditorGUILayout.TextArea(line.Text, GUILayout.Height(60));
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                line.LeftPortrait = (Sprite)EditorGUILayout.ObjectField("Left", line.LeftPortrait, typeof(Sprite), false);
                line.CenterPortrait = (Sprite)EditorGUILayout.ObjectField("Center", line.CenterPortrait, typeof(Sprite), false);
                line.RightPortrait = (Sprite)EditorGUILayout.ObjectField("Right", line.RightPortrait, typeof(Sprite), false);
                EditorGUILayout.EndHorizontal();

                line.Focus = (MaouSamaTD.Tutorial.PortraitFocus)EditorGUILayout.EnumPopup("Focus Position", line.Focus);

                EditorGUILayout.Space(5);
                line.EventID = EditorGUILayout.TextField("Event ID", line.EventID);
                
                tutorialAsset.Lines[selectedLineIndex] = line; // Struct update
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(selectedAsset);
                AssetDatabase.SaveAssets();
            }
            
            if (GUILayout.Button("Save Asset", GUILayout.Height(30)))
            {
                AssetDatabase.SaveAssets();
                ShowNotification(new GUIContent("Saved!"));
            }
        }

        private void AutoAssignUI(MaouSamaTD.UI.Tutorial.DialogueUI ui)
        {
            var so = new SerializedObject(ui);
            var images = ui.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            var texts = ui.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);

            void AssignIfMatch(string propName, string searchName, Component[] components)
            {
                var prop = so.FindProperty(propName);
                if (prop.objectReferenceValue != null) return;
                var comp = components.FirstOrDefault(c => c.name.Contains(searchName, System.StringComparison.OrdinalIgnoreCase));
                if (comp != null) prop.objectReferenceValue = comp;
            }

            AssignIfMatch("_leftPortrait", "LeftPortrait", images);
            AssignIfMatch("_middlePortrait", "MiddlePortrait", images);
            AssignIfMatch("_rightPortrait", "RightPortrait", images);
            AssignIfMatch("_fullSpeakerText", "SpeakerText", texts);
            AssignIfMatch("_fullContentText", "ContentText", texts);
            AssignIfMatch("_fullScreenPanel", "FullScreenPanel", images.Select(i => i.transform).ToArray()); 
            AssignIfMatch("_backgroundImage", "StoryBackground", images);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ui);
        }

        private void ApplyToScene(MaouSamaTD.UI.Tutorial.DialogueUI ui)
        {
            var so = new SerializedObject(ui);
            
            void SetImage(string propName, Sprite sprite, bool active)
            {
                var prop = so.FindProperty(propName);
                if (prop == null) return;
                var img = (UnityEngine.UI.Image)prop.objectReferenceValue;
                if (img != null)
                {
                    img.gameObject.SetActive(active && sprite != null);
                    img.sprite = sprite;
                    img.color = Color.white; 
                }
            }

            void SetText(string propName, string text)
            {
                var prop = so.FindProperty(propName);
                if (prop == null) return;
                var tmp = (TMPro.TextMeshProUGUI)prop.objectReferenceValue;
                if (tmp != null) tmp.text = text;
            }

            GameObject fullPanel = (GameObject)so.FindProperty("_fullScreenPanel").objectReferenceValue;
            GameObject miniPanel = (GameObject)so.FindProperty("_miniTopPanel").objectReferenceValue;

            if (selectedAsset is StoryDataSO story)
            {
                var line = story.Lines[selectedLineIndex];
                if (fullPanel != null) fullPanel.SetActive(true);
                if (miniPanel != null) miniPanel.SetActive(false);

                SetImage("_leftPortrait", line.PortraitLeft, line.Focus == MaouSamaTD.Story.PortraitFocus.Left || line.Focus == MaouSamaTD.Story.PortraitFocus.All);
                SetImage("_middlePortrait", line.PortraitMiddle, line.Focus == MaouSamaTD.Story.PortraitFocus.Middle || line.Focus == MaouSamaTD.Story.PortraitFocus.All);
                SetImage("_rightPortrait", line.PortraitRight, line.Focus == MaouSamaTD.Story.PortraitFocus.Right || line.Focus == MaouSamaTD.Story.PortraitFocus.All);
                SetImage("_backgroundImage", line.Background, line.Background != null);

                SetText("_fullSpeakerText", line.SpeakerName);
                SetText("_fullContentText", line.DialogueText);
            }
            else if (selectedAsset is DialogueData tutorial)
            {
                var line = tutorial.Lines[selectedLineIndex];
                bool isFull = tutorial.Style == DialogueStyle.FullScreen;

                if (fullPanel != null) fullPanel.SetActive(isFull);
                if (miniPanel != null) miniPanel.SetActive(!isFull);

                if (isFull)
                {
                    SetImage("_leftPortrait", line.LeftPortrait, line.Focus == MaouSamaTD.Tutorial.PortraitFocus.Left || line.Focus == MaouSamaTD.Tutorial.PortraitFocus.All);
                    SetImage("_middlePortrait", line.CenterPortrait, line.Focus == MaouSamaTD.Tutorial.PortraitFocus.Center || line.Focus == MaouSamaTD.Tutorial.PortraitFocus.All);
                    SetImage("_rightPortrait", line.RightPortrait, line.Focus == MaouSamaTD.Tutorial.PortraitFocus.Right || line.Focus == MaouSamaTD.Tutorial.PortraitFocus.All);
                    
                    SetText("_fullSpeakerText", line.SpeakerName);
                    SetText("_fullContentText", line.Text);
                }
                else
                {
                    // MiniTop usually only shows one. Let's use Center, then Left, then Right as priority.
                    Sprite s = line.CenterPortrait != null ? line.CenterPortrait : (line.LeftPortrait != null ? line.LeftPortrait : line.RightPortrait);
                    SetImage("_miniTopPortrait", s, true);
                    SetText("_miniTopSpeakerText", line.SpeakerName);
                    SetText("_miniTopContentText", line.Text);
                }
            }

            so.ApplyModifiedProperties();
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
