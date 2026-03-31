using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace SwiftSelect
{
    public class SwiftSelectSetupWindow : EditorWindow
    {
        private GUIStyle _headerStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _presetButtonStyle;
        private GUIStyle _modifierButtonStyle;
        private GUIStyle _sectionHeaderStyle;
        private bool _isRecording;
        private bool _showUsage = false;

        [MenuItem("Tools/Swift Select/Setup", false, 0)]
        public static void ShowWindow()
        {
            SwiftSelectSetupWindow window = GetWindow<SwiftSelectSetupWindow>("Swift Select Setup");
            window.minSize = new Vector2(440, 680);
            window.Show();
        }

        private void OnEnable()
        {
            // Don't call InitStyles here, wait for OnGUI
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle();
            _headerStyle.normal.background = MakeTex(1, 1, new Color(0.12f, 0.12f, 0.12f));

            _titleStyle = new GUIStyle(EditorStyles.boldLabel);
            _titleStyle.fontSize = 24;
            _titleStyle.normal.textColor = new Color(0.2f, 0.7f, 1f);
            _titleStyle.alignment = TextAnchor.MiddleCenter;

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            _sectionHeaderStyle.fontSize = 14;
            _sectionHeaderStyle.margin = new RectOffset(0, 0, 10, 5);

            _presetButtonStyle = new GUIStyle(GUI.skin.button);
            _presetButtonStyle.fontSize = 12;
            _presetButtonStyle.padding = new RectOffset(10, 10, 10, 10);

            _modifierButtonStyle = new GUIStyle(GUI.skin.button);
            _modifierButtonStyle.fontSize = 11;
        }

        private void DrawPresetButton(string label, string id, Color activeColor)
        {
            bool isActive = EditorPrefs.GetString("SwiftSelect_LastPreset", "Standard") == id;
            if (isActive) GUI.backgroundColor = activeColor;
            
            if (GUILayout.Button(label, _presetButtonStyle, GUILayout.Height(55)))
            {
                SwiftSelectSettings.ApplyPreset(id);
                EditorPrefs.SetString("SwiftSelect_LastPreset", id);
            }
            
            GUI.backgroundColor = Color.white;
        }

        private void OnGUI()
        {
            if (_headerStyle == null) InitStyles();

            // Header
            Rect headerRect = EditorGUILayout.BeginVertical(_headerStyle, GUILayout.Height(80));
            EditorGUILayout.Space(20);
            GUILayout.Label("SWIFT SELECT", _titleStyle);
            GUILayout.Label("Created by OuikiDev 2026", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(20, 20, 15, 15) });

            // Presets
            GUILayout.Label("QUICK PRESETS", _sectionHeaderStyle);
            EditorGUILayout.BeginHorizontal();
            
            DrawPresetButton("STANDARD\n(Ctrl+Shift)", "Standard", new Color(0.3f, 0.7f, 1f));
            DrawPresetButton("PRO\n(Alt+Shift)", "Pro", new Color(1f, 0.6f, 0f));
            DrawPresetButton("MINIMAL\n(Shift)", "Minimalist", new Color(0.6f, 1f, 0.3f));
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0.3f, 0.3f, 0.3f));
            EditorGUILayout.Space(15);

            // Controls
            GUILayout.Label("SHORTCUT CONFIGURATION", _sectionHeaderStyle);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Modifier Keys:", GUILayout.Width(100));
            
            DrawModifierToggle("Ctrl", EventModifiers.Control);
            DrawModifierToggle("Shift", EventModifiers.Shift);
            DrawModifierToggle("Alt", EventModifiers.Alt);
            
            #if UNITY_EDITOR_OSX
            DrawModifierToggle("Cmd", EventModifiers.Command);
            #endif
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(104);
            
            string recordLabel = _isRecording ? "Listening for Keys... (Esc to stop)" : "Click to Record Shortcut";
            GUI.backgroundColor = _isRecording ? new Color(1f, 0.4f, 0.4f) : Color.white;
            if (GUILayout.Button(recordLabel, GUILayout.Height(25)))
            {
                _isRecording = !_isRecording;
                GUIUtility.keyboardControl = 0; // Clear focus
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (_isRecording)
            {
                Event evt = Event.current;
                if (evt.isKey || evt.isMouse)
                {
                    if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
                    {
                        _isRecording = false;
                        Repaint();
                    }
                    else if (evt.modifiers != EventModifiers.None)
                    {
                        SwiftSelectSettings.ModifierKeys = evt.modifiers;
                        EditorPrefs.SetString("SwiftSelect_LastPreset", "Custom");
                        Repaint();
                    }
                }
            }
            
            EditorGUILayout.HelpBox("Use these keys + Click to trigger the selection popup.", MessageType.Info);

            EditorGUILayout.Space(15);

            // Selection & Visuals
            GUILayout.Label("BEHAVIOR & VISUALS", _sectionHeaderStyle);
            
            EditorGUI.BeginChangeCheck();
            SwiftSelectSettings.FocusOnSelect = EditorGUILayout.Toggle("Auto-Focus on Select", SwiftSelectSettings.FocusOnSelect);
            SwiftSelectSettings.ShowIcons = EditorGUILayout.Toggle("Display Object Icons", SwiftSelectSettings.ShowIcons);
            SwiftSelectSettings.ShowOnlyActive = EditorGUILayout.Toggle("Show Only Active Objects", SwiftSelectSettings.ShowOnlyActive);
            SwiftSelectSettings.HighlightColor = EditorGUILayout.ColorField("Highlight Color", SwiftSelectSettings.HighlightColor);
            SwiftSelectSettings.ExcludeLayers = EditorGUILayout.MaskField("Exclude Layers", SwiftSelectSettings.ExcludeLayers, InternalEditorUtility.layers);
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString("SwiftSelect_LastPreset", "Custom");
            }

            EditorGUILayout.Space(20);
            
            // Usage Instructions (Accordion)
            _showUsage = EditorGUILayout.BeginFoldoutHeaderGroup(_showUsage, "HOW TO USE");
            if (_showUsage)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("• <b>CLICK</b>: Shortcut + Left Click to pick one.", GetRichLabel());
                GUILayout.Label("• <b>MULTI</b>: Shortcut + Right Click to keep open.", GetRichLabel());
                GUILayout.Label("• <b>HOVER</b>: Hover over items to highlight them.", GetRichLabel());
                GUILayout.Label("• <b>KEYS</b>: Use ↑ ↓ arrow keys to navigate.", GetRichLabel());
                GUILayout.Label("• <b>ACTION</b>: Press Enter or Click to select.", GetRichLabel());
                GUILayout.Label("• <b>ALL</b>: Click header to select all.", GetRichLabel());
                GUILayout.Label("• <b>EXIT</b>: Press Escape or click outside to close.", GetRichLabel());
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            GUILayout.FlexibleSpace();

            // Footer
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0.3f, 0.3f, 0.3f));
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            SwiftSelectSettings.ShowOnStartup = EditorGUILayout.ToggleLeft("Show this window on startup", SwiftSelectSettings.ShowOnStartup);
            if (GUILayout.Button("Reset", GUILayout.Width(60))) SwiftSelectSettings.ResetToDefaults();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            if (GUILayout.Button("SAVE & FINISH", GUILayout.Height(45)))
            {
                SwiftSelectSettings.IsFirstRun = false;
                Close();
            }

            EditorGUILayout.EndVertical();
        }

        private GUIStyle GetRichLabel()
        {
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.richText = true;
            return style;
        }

        private void DrawModifierToggle(string label, EventModifiers modifier)
        {
            bool isActive = (SwiftSelectSettings.ModifierKeys & modifier) != 0;
            GUI.backgroundColor = isActive ? new Color(0.3f, 0.7f, 1f) : Color.white;
            
            if (GUILayout.Toggle(isActive, label, "Button", GUILayout.Width(55), GUILayout.Height(25)) != isActive)
            {
                if (isActive) 
                    SwiftSelectSettings.ModifierKeys &= ~modifier;
                else 
                    SwiftSelectSettings.ModifierKeys |= modifier;

                EditorPrefs.SetString("SwiftSelect_LastPreset", "Custom");
            }
            GUI.backgroundColor = Color.white;
        }

        private void OnDestroy()
        {
            if (_headerStyle != null && _headerStyle.normal.background != null)
            {
                DestroyImmediate(_headerStyle.normal.background);
            }
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.hideFlags = HideFlags.HideAndDontSave;
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
