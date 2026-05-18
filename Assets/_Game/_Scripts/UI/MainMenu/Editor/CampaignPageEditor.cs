using UnityEngine;
using UnityEditor;
using MaouSamaTD.UI.MainMenu;
using MaouSamaTD.Levels;
using System.Collections.Generic;

namespace MaouSamaTD.UI.MainMenu.Editor
{
    [CustomEditor(typeof(CampaignPage))]
    public class CampaignPageEditor : UnityEditor.Editor
    {
        private CampaignPage _campaignPage;
        private int _sourceIndex = 0;
        private int _targetIndex = 0;

        private void OnEnable()
        {
            _campaignPage = (CampaignPage)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("🔮 Demonic Map Coordinates Editor", EditorStyles.boldLabel);

            if (GUILayout.Button("🔄 Refresh Map Nodes", GUILayout.Height(30)))
            {
                _campaignPage.Refresh();
                EditorUtility.SetDirty(_campaignPage);
            }

            EditorGUILayout.Space(10);
            
            var levels = _campaignPage.AllLevels;
            if (levels != null && levels.Count > 0)
            {
                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.LabelField("⛓️ Quick Node Connector", EditorStyles.miniBoldLabel);

                string[] levelNames = new string[levels.Count];
                for (int i = 0; i < levels.Count; i++)
                {
                    levelNames[i] = $"{levels[i].LevelID} - {levels[i].LevelName}";
                }

                if (_sourceIndex >= levels.Count) _sourceIndex = 0;
                if (_targetIndex >= levels.Count) _targetIndex = 0;

                _sourceIndex = EditorGUILayout.Popup("Source Level", _sourceIndex, levelNames);
                _targetIndex = EditorGUILayout.Popup("Target Level", _targetIndex, levelNames);

                EditorGUILayout.Space(5);
                if (GUILayout.Button("Toggle Connection (Connect/Disconnect)", GUILayout.Height(25)))
                {
                    if (_sourceIndex >= 0 && _sourceIndex < levels.Count &&
                        _targetIndex >= 0 && _targetIndex < levels.Count)
                    {
                        var sourceLvl = levels[_sourceIndex];
                        var targetLvl = levels[_targetIndex];

                        if (sourceLvl != targetLvl)
                        {
                            Undo.RecordObject(sourceLvl, "Toggle Node Connection");
                            if (sourceLvl.ConnectedLevels == null)
                            {
                                sourceLvl.ConnectedLevels = new List<LevelData>();
                            }

                            if (sourceLvl.ConnectedLevels.Contains(targetLvl))
                            {
                                sourceLvl.ConnectedLevels.Remove(targetLvl);
                                Debug.Log($"[CampaignPageEditor] Disconnected: {sourceLvl.LevelID} and {targetLvl.LevelID}");
                            }
                            else
                            {
                                sourceLvl.ConnectedLevels.Add(targetLvl);
                                Debug.Log($"[CampaignPageEditor] Connected: {sourceLvl.LevelID} -> {targetLvl.LevelID}");
                            }

                            EditorUtility.SetDirty(sourceLvl);
                            AssetDatabase.SaveAssets();
                            _campaignPage.Refresh();
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Connection Error", "Cannot connect a node to itself.", "OK");
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("✨ Professional Workflow:\n" +
                                    "1. Keep CampaignPage selected.\n" +
                                    "2. In the Scene view, drag the cyan handles to reposition level nodes.\n" +
                                    "3. Splines & curves update immediately!\n" +
                                    "4. Supports Ctrl+Z (Undo) seamlessly.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            if (_campaignPage == null) return;

            var container = _campaignPage.LevelContainer;
            if (container == null) return;

            var buttons = _campaignPage.SpawnedButtons;
            if (buttons == null || buttons.Count == 0) return;

            foreach (var btn in buttons)
            {
                if (btn == null || btn.LevelDataForCallback == null) continue;

                var rect = btn.GetComponent<RectTransform>();
                if (rect == null) continue;

                // Current world position in scene
                Vector3 worldPos = rect.position;

                float size = HandleUtility.GetHandleSize(worldPos) * 0.12f;

                // Let's draw a handle at worldPos
                EditorGUI.BeginChangeCheck();

                Handles.color = new Color(0f, 0.8f, 1.0f, 0.8f); // Demonic glowing cyan
                var fmh_136_21_639147550679331565 = Quaternion.identity; Vector3 newWorldPos = Handles.FreeMoveHandle(
                    btn.GetInstanceID(),
                    worldPos,
                    size,
                    Vector3.zero,
                    Handles.CircleHandleCap
                );

                // Draw label above handle
                Handles.Label(worldPos + Vector3.up * size * 1.5f, btn.LevelDataForCallback.LevelID, EditorStyles.boldLabel);

                if (EditorGUI.EndChangeCheck())
                {
                    // Convert back to local space relative to the container
                    Vector3 localPos = container.InverseTransformPoint(newWorldPos);
                    Vector2 anchoredPos = new Vector2(localPos.x, localPos.y);

                    // Clamp to the 2048x1143 coordinate map boundaries
                    anchoredPos.x = Mathf.Clamp(anchoredPos.x, 0f, 2048f);
                    anchoredPos.y = Mathf.Clamp(anchoredPos.y, 0f, 1143f);

                    // Snap to round numbers for pixel perfection
                    anchoredPos.x = Mathf.Round(anchoredPos.x);
                    anchoredPos.y = Mathf.Round(anchoredPos.y);

                    // Undo support
                    Undo.RecordObject(btn.LevelDataForCallback, "Reposition Level Node");
                    
                    btn.LevelDataForCallback.CampaignMapPosition = anchoredPos;
                    rect.anchoredPosition = anchoredPos;

                    EditorUtility.SetDirty(btn.LevelDataForCallback);

                    // Real-time spline redraw in scene
                    _campaignPage.RedrawSplinesOnly();
                }
            }
        }
    }
}
