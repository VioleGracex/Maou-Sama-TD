using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using MaouSamaTD.Editor.Story;

namespace MaouSamaTD.Tutorial
{
    [CustomEditor(typeof(TutorialDataSO))]
    public class TutorialDataSOEditor : UnityEditor.Editor
    {
        private ReorderableList _list;
        private int _selectedIndex = 0;
        private Vector2 _listScroll;

        // Clipboard — stored as JSON of a TutorialStep
        private static string _clipboardJson = null;
        private static string _clipboardName  = null;

        private void OnEnable() => BuildList();

        // ── Strip "N: " prefix from stored names ──────────────────────
        private static string StripPrefix(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            var m = System.Text.RegularExpressions.Regex.Match(raw, @"^\d{1,3}:\s*");
            return m.Success ? raw.Substring(m.Length) : raw;
        }

        // ── Clipboard helpers ──────────────────────────────────────────
        private void CopyStep(TutorialDataSO data, int index)
        {
            if (index < 0 || index >= data.Steps.Count) return;
            _clipboardJson = JsonUtility.ToJson(data.Steps[index]);
            _clipboardName = data.Steps[index].StepName;
        }

        private void PasteAfter(TutorialDataSO data, SerializedProperty stepsProp, int afterIndex)
        {
            if (_clipboardJson == null) return;

            int at = afterIndex + 1;
            // Insert and overwrite with pasted data
            stepsProp.InsertArrayElementAtIndex(at);
            serializedObject.ApplyModifiedProperties();

            // Overwrite the new entry via JsonUtility on the real object
            var pasted = new TutorialStep();
            JsonUtility.FromJsonOverwrite(_clipboardJson, pasted);
            pasted.StepName = $"Copy of {_clipboardName}";
            data.Steps[at] = pasted;

            EditorUtility.SetDirty(data);
            serializedObject.Update();
            _selectedIndex = at;
            _list.index = at;
        }

        // ── Build ReorderableList ──────────────────────────────────────
        private void BuildList()
        {
            var stepsProp = serializedObject.FindProperty("Steps");
            _list = new ReorderableList(serializedObject, stepsProp,
                draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);

            _list.drawHeaderCallback = r =>
                EditorGUI.LabelField(r, "Tutorial Steps  (drag ≡ to reorder)", EditorStyles.boldLabel);

            _list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var el    = stepsProp.GetArrayElementAtIndex(index);
                string raw   = el.FindPropertyRelative("StepName").stringValue;
                string clean = StripPrefix(raw);
                if (string.IsNullOrEmpty(clean)) clean = "Step";

                rect.y     += 2f;
                rect.height = EditorGUIUtility.singleLineHeight;

                // Coloured index badge
                Rect badge = new Rect(rect.x, rect.y, 28f, rect.height);
                // Delete button
                Rect deleteRect = new Rect(rect.x + rect.width - 25, rect.y, 20, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(deleteRect, "❌", EditorStyles.miniButton))
                {
                    if (EditorUtility.DisplayDialog("Delete Step", $"Are you sure you want to delete step '{raw}'?", "Yes", "No"))
                    {
                        _list.serializedProperty.DeleteArrayElementAtIndex(index);
                        serializedObject.ApplyModifiedProperties();
                        return;
                    }
                }
                
                Rect label = new Rect(rect.x + 32f, rect.y, rect.width - 32f - 30f, rect.height);

                GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment  = TextAnchor.MiddleCenter,
                    fontStyle  = FontStyle.Bold,
                    normal     = { textColor = new Color(0.5f, 0.8f, 1f) }
                };
                GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment  = TextAnchor.MiddleLeft,
                    clipping   = TextClipping.Clip,
                    fontStyle  = isActive ? FontStyle.Bold : FontStyle.Normal
                };

                EditorGUI.LabelField(badge, index.ToString(), badgeStyle);
                EditorGUI.LabelField(label, clean, nameStyle);
            };

            _list.elementHeight       = EditorGUIUtility.singleLineHeight + 4f;
            _list.onSelectCallback    = l => _selectedIndex = l.index;

            _list.onAddCallback = l =>
            {
                int at = l.index >= 0 ? l.index + 1 : stepsProp.arraySize;
                stepsProp.InsertArrayElementAtIndex(at);
                stepsProp.GetArrayElementAtIndex(at)
                    .FindPropertyRelative("StepName").stringValue = "New Step";
                serializedObject.ApplyModifiedProperties();
                l.index        = at;
                _selectedIndex = at;
            };

            _list.onRemoveCallback = l =>
            {
                if (EditorUtility.DisplayDialog("Delete Step",
                    $"Delete step {l.index}?", "Delete", "Cancel"))
                {
                    ReorderableList.defaultBehaviours.DoRemoveButton(l);
                    _selectedIndex = Mathf.Clamp(_selectedIndex, 0, stepsProp.arraySize - 1);
                }
            };
        }

        // ── Main draw ─────────────────────────────────────────────────
        public override void OnInspectorGUI()
        {
            TutorialDataSO data = (TutorialDataSO)target;
            serializedObject.Update();

            // Toggle
            GUIStyle toggle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 12 };
            toggle.normal.textColor = data.ShowCustomEditor ? new Color(0.1f, 0.75f, 0.2f) : Color.gray;
            if (GUILayout.Button(data.ShowCustomEditor
                    ? "Switch to Default Editor" : "Switch to Custom Editor",
                    toggle, GUILayout.Height(30)))
            {
                data.ShowCustomEditor = !data.ShowCustomEditor;
                EditorUtility.SetDirty(data);
            }

            EditorGUILayout.Space(4);

            if (!data.ShowCustomEditor)
            {
                DrawDefaultInspector();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (_list == null) BuildList();

            var stepsProp = serializedObject.FindProperty("Steps");

            if (stepsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No steps defined.", MessageType.Info);
                if (GUILayout.Button("Add First Step"))
                {
                    stepsProp.InsertArrayElementAtIndex(0);
                    stepsProp.GetArrayElementAtIndex(0)
                        .FindPropertyRelative("StepName").stringValue = "New Step";
                    _selectedIndex = 0;
                }
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // ── Draggable list ─────────────────────────────────────────
            float listHeight = Mathf.Min(_list.elementHeight * stepsProp.arraySize + 54f, 320f);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Height(listHeight));
            _list.DoLayoutList();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);

            // ── Edit Panel ─────────────────────────────────────────────
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, stepsProp.arraySize - 1);
            _list.index    = _selectedIndex;

            var   selected  = stepsProp.GetArrayElementAtIndex(_selectedIndex);
            string selClean = StripPrefix(selected.FindPropertyRelative("StepName").stringValue);
            if (string.IsNullOrEmpty(selClean)) selClean = "Step";

            GUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField($"[{_selectedIndex}]  {selClean}", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(selected, true);
            DrawStepWarnings(selected);
            EditorGUILayout.Space(8);

            // ── Copy / Paste / Duplicate row ───────────────────────────
            GUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(1f, 0.85f, 0.3f);
            if (GUILayout.Button("📋 Copy", GUILayout.Height(26)))
                CopyStep(data, _selectedIndex);

            bool hasClip = _clipboardJson != null;
            GUI.enabled = hasClip;
            GUI.backgroundColor = hasClip ? new Color(0.4f, 1f, 0.6f) : Color.gray;
            if (GUILayout.Button($"📌 Paste After" + (hasClip ? $"  [{_clipboardName}]" : ""), GUILayout.Height(26)))
            {
                PasteAfter(data, stepsProp, _selectedIndex);
                serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI();
                return;
            }
            GUI.enabled = true;

            GUI.backgroundColor = new Color(0.7f, 0.7f, 1f);
            if (GUILayout.Button("⧉ Duplicate", GUILayout.Height(26)))
            {
                CopyStep(data, _selectedIndex);
                PasteAfter(data, stepsProp, _selectedIndex);
                serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI();
                return;
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            // ── Dialogue test ──────────────────────────────────────────
            var dlg = selected.FindPropertyRelative("Dialogue");
            if (dlg?.objectReferenceValue != null)
            {
                EditorGUILayout.Space(4);
                GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
                if (GUILayout.Button("TEST DIALOGUE", GUILayout.Height(28)))
                    DialogueTesterWindow.ShowWithAsset((ScriptableObject)dlg.objectReferenceValue);
                GUI.backgroundColor = Color.white;
            }

            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStepWarnings(SerializedProperty stepProp)
        {
            var useBlocker = stepProp.FindPropertyRelative("UseBlocker").boolValue;
            var fullBlocker = stepProp.FindPropertyRelative("FullBlocker").boolValue;
            var dialogue = stepProp.FindPropertyRelative("Dialogue").objectReferenceValue as DialogueData;

            if (dialogue != null)
            {
                bool dialogueHasBlocker = false;
                bool dialogueHasDim = false;
                foreach (var line in dialogue.Lines)
                {
                    if (line.Background == DialogueBackground.UIBlocker) dialogueHasBlocker = true;
                    if (line.Background == DialogueBackground.FullScreenDim) dialogueHasDim = true;
                }

                if (dialogueHasBlocker && useBlocker)
                {
                    EditorGUILayout.HelpBox("INFO: Both Tutorial Step and Dialogue have 'UI Blocker' enabled. The dialogue box will be automatically added as a hole in the blocker.", MessageType.Info);
                }
                else if (dialogueHasBlocker && !useBlocker)
                {
                    EditorGUILayout.HelpBox("WARNING: Dialogue line requests a UI Blocker, but Tutorial Step has 'Use Blocker' OFF. The blocker will activate ONLY while the dialogue is shown.", MessageType.Warning);
                }

                if (dialogueHasDim)
                {
                    EditorGUILayout.HelpBox("INFO: Dialogue has 'Full Screen Dim' enabled. This will dim the entire screen during dialogue.", MessageType.Info);
                }
            }

            if (useBlocker && fullBlocker)
            {
                EditorGUILayout.HelpBox("FULL BLOCKER: No world or UI holes will be cut (except the dialogue box). Best for pure story moments.", MessageType.None);
            }
            else if (useBlocker)
            {
                EditorGUILayout.HelpBox("UI BLOCKER: Holes will be cut for TargetUI and TargetTiles. Game world is dimmed and blocked.", MessageType.None);
            }
        }
    }
}
