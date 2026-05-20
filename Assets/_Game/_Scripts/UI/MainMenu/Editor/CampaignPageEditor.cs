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

        private void OnEnable()
        {
            _campaignPage = (CampaignPage)target;
            AutoAssignFields(false); // Silent auto-assign on enable
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("🔮 Demonic Map Coordinates Editor", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 Refresh Map Nodes", GUILayout.Height(30)))
            {
                _campaignPage.Refresh();
                EditorUtility.SetDirty(_campaignPage);
            }

            if (GUILayout.Button("🔌 Auto-Assign Missing UI", GUILayout.Height(30)))
            {
                AutoAssignFields(true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("✨ Professional Workflow:\n" +
                                    "1. Keep CampaignPage selected.\n" +
                                    "2. In the Scene view, drag the cyan handles to reposition level nodes.\n" +
                                    "3. Splines & curves update immediately!\n" +
                                    "4. Supports Ctrl+Z (Undo) seamlessly.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        private void AutoAssignFields(bool logSuccess)
        {
            if (_campaignPage == null) return;

            bool isDirty = false;

            var propRoot = serializedObject.FindProperty("_sidebarRoot");
            if (propRoot != null && propRoot.objectReferenceValue == null)
            {
                var root = GameObject.Find("LeftSidebar");
                if (root == null)
                {
                    var t = _campaignPage.transform.Find("LeftSidebar");
                    if (t != null) root = t.gameObject;
                }
                if (root != null)
                {
                    propRoot.objectReferenceValue = root;
                    isDirty = true;
                }
            }

            var propContainer = serializedObject.FindProperty("_sidebarContentContainer");
            if (propContainer != null && propContainer.objectReferenceValue == null)
            {
                var content = GameObject.Find("LeftSidebar/ScrollView/Viewport/Content");
                if (content == null)
                {
                    var t = _campaignPage.transform.Find("LeftSidebar/ScrollView/Viewport/Content");
                    if (t == null) t = _campaignPage.transform.Find("LeftSidebar/Viewport/Content");
                    if (t != null) content = t.gameObject;
                }
                if (content != null)
                {
                    propContainer.objectReferenceValue = content.transform;
                    isDirty = true;
                }
            }

            var propZoomIn = serializedObject.FindProperty("_zoomInButton");
            if (propZoomIn != null && propZoomIn.objectReferenceValue == null)
            {
                var btnGo = GameObject.Find("ZoomContainer/ZoomInButton");
                if (btnGo == null) btnGo = GameObject.Find("ZoomInButton");
                if (btnGo == null)
                {
                    var t = _campaignPage.transform.Find("ZoomContainer/ZoomInButton");
                    if (t == null) t = _campaignPage.transform.Find("ZoomInButton");
                    if (t != null) btnGo = t.gameObject;
                }
                if (btnGo != null)
                {
                    var btn = btnGo.GetComponent<UnityEngine.UI.Button>();
                    if (btn != null)
                    {
                        propZoomIn.objectReferenceValue = btn;
                        isDirty = true;
                    }
                }
            }

            var propZoomOut = serializedObject.FindProperty("_zoomOutButton");
            if (propZoomOut != null && propZoomOut.objectReferenceValue == null)
            {
                var btnGo = GameObject.Find("ZoomContainer/ZoomOutButton");
                if (btnGo == null) btnGo = GameObject.Find("ZoomOutButton");
                if (btnGo == null)
                {
                    var t = _campaignPage.transform.Find("ZoomContainer/ZoomOutButton");
                    if (t == null) t = _campaignPage.transform.Find("ZoomOutButton");
                    if (t != null) btnGo = t.gameObject;
                }
                if (btnGo != null)
                {
                    var btn = btnGo.GetComponent<UnityEngine.UI.Button>();
                    if (btn != null)
                    {
                        propZoomOut.objectReferenceValue = btn;
                        isDirty = true;
                    }
                }
            }

            var propPrefab = serializedObject.FindProperty("_sidebarItemPrefab");
            if (propPrefab != null && propPrefab.objectReferenceValue == null)
            {
                var guids = AssetDatabase.FindAssets("StageLevel_Prefab t:GameObject");
                if (guids.Length == 0)
                {
                    guids = AssetDatabase.FindAssets("t:GameObject SidebarLevelItem");
                }
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null)
                    {
                        var comp = go.GetComponent<SidebarLevelItem>();
                        if (comp != null)
                        {
                            propPrefab.objectReferenceValue = comp;
                            isDirty = true;
                        }
                    }
                }
            }

            if (isDirty)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_campaignPage);
                if (logSuccess)
                {
                    Debug.Log("[CampaignPageEditor] Automatically assigned missing CampaignPage UI fields from the scene!");
                }
            }
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
                Vector3 newWorldPos = Handles.FreeMoveHandle(
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
