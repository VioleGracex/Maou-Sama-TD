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

            var propBriefing = serializedObject.FindProperty("_briefingPanel");
            if (propBriefing != null && propBriefing.objectReferenceValue == null)
            {
                var briefing = GameObject.FindObjectOfType<BriefingPanel>(true);
                if (briefing == null)
                {
                    var t = _campaignPage.transform.parent != null ? _campaignPage.transform.parent.Find("Briefing_Panel") : null;
                    if (t == null) t = _campaignPage.transform.Find("Briefing_Panel");
                    if (t != null) briefing = t.GetComponent<BriefingPanel>();
                }
                if (briefing != null)
                {
                    propBriefing.objectReferenceValue = briefing;
                    isDirty = true;
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

            var levels = _campaignPage.AllLevels;
            if (levels == null || levels.Count == 0) return;

            // 1. Draw connections first in the background
            var levelsWithPositions = new Dictionary<LevelData, Vector2>();
            bool hasExplicitConnections = false;
            foreach (var level in levels)
            {
                if (level == null) continue;
                levelsWithPositions[level] = level.CampaignMapPosition;
                if (level.ConnectedLevels != null && level.ConnectedLevels.Count > 0)
                {
                    hasExplicitConnections = true;
                }
            }

            var drawnConnections = new HashSet<(string, string)>();

            if (hasExplicitConnections)
            {
                foreach (var level in levels)
                {
                    if (level == null || level.ConnectedLevels == null) continue;

                    foreach (var targetLvl in level.ConnectedLevels)
                    {
                        if (targetLvl == null) continue;
                        if (!levelsWithPositions.TryGetValue(targetLvl, out var targetPos)) continue;

                        string idA = level.LevelID;
                        string idB = targetLvl.LevelID;
                        var key = string.Compare(idA, idB, System.StringComparison.Ordinal) < 0 ? (idA, idB) : (idB, idA);

                        if (!drawnConnections.Contains(key))
                        {
                            Color lineColor = GetEditorCategoryColor(level.Category);
                            lineColor.a = 0.5f; // Semitransparent in editor for clarity
                            DrawEditorSpline(level.CampaignMapPosition, targetPos, lineColor, container);
                            drawnConnections.Add(key);
                        }
                    }
                }
            }
            else
            {
                // Fallback to sequential main story drawing exclusively between story stages
                List<LevelData> storyLevels = new List<LevelData>();
                foreach (var level in levels)
                {
                    if (level != null && level.Category == LevelCategory.MainStory)
                    {
                        storyLevels.Add(level);
                    }
                }
                storyLevels.Sort((a, b) => a.LevelIndex.CompareTo(b.LevelIndex));

                if (storyLevels.Count > 1)
                {
                    for (int i = 1; i < storyLevels.Count; i++)
                    {
                        var prev = storyLevels[i - 1];
                        var curr = storyLevels[i];
                        if (prev != null && curr != null &&
                            levelsWithPositions.TryGetValue(prev, out var prevPos) &&
                            levelsWithPositions.TryGetValue(curr, out var currPos))
                        {
                            Color lineColor = GetEditorCategoryColor(LevelCategory.MainStory);
                            lineColor.a = 0.5f;
                            DrawEditorSpline(prevPos, currPos, lineColor, container);
                        }
                    }
                }
            }

            // 2. Draw handles for each level node
            foreach (var level in levels)
            {
                if (level == null) continue;

                // Current world position in scene
                Vector3 worldPos = container.TransformPoint(new Vector3(level.CampaignMapPosition.x, level.CampaignMapPosition.y, 0f));

                float size = HandleUtility.GetHandleSize(worldPos) * 0.12f;

                // Color-code the handle based on category
                Handles.color = GetEditorCategoryColor(level.Category);

                EditorGUI.BeginChangeCheck();

                // FreeMoveHandle allows free dragging in 3D (which maps to 2D screen/rect plane)
                Vector3 newWorldPos = Handles.FreeMoveHandle(
                    level.GetInstanceID(),
                    worldPos,
                    size,
                    Vector3.zero,
                    Handles.CircleHandleCap
                );

                // Draw label above handle
                string labelText = $"{level.LevelID}\n{level.LevelName}";
                GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
                labelStyle.normal.textColor = GetEditorCategoryColor(level.Category);
                labelStyle.alignment = TextAnchor.UpperCenter;
                
                Handles.Label(worldPos + Vector3.up * size * 1.5f, labelText, labelStyle);

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
                    Undo.RecordObject(level, "Reposition Level Node");
                    
                    level.CampaignMapPosition = anchoredPos;

                    EditorUtility.SetDirty(level);
                }
            }
        }

        private void DrawEditorSpline(Vector2 startLocal, Vector2 endLocal, Color color, Transform container)
        {
            Vector2 dir = endLocal - startLocal;
            Vector2 perp = new Vector2(-dir.y, dir.x).normalized;
            float dist = dir.magnitude;
            float arcFactor = dist * 0.12f;
            Vector2 control = (startLocal + endLocal) * 0.5f + perp * arcFactor;

            int numSegments = Mathf.Max(5, Mathf.RoundToInt(dist / 22f));
            Vector3[] points = new Vector3[numSegments + 1];

            for (int i = 0; i <= numSegments; i++)
            {
                float t = (float)i / numSegments;
                Vector2 posLocal = (1f - t) * (1f - t) * startLocal + 2f * (1f - t) * t * control + t * t * endLocal;
                points[i] = container.TransformPoint(new Vector3(posLocal.x, posLocal.y, 0f));
            }

            Handles.color = color;
            Handles.DrawAAPolyLine(3f, points);
        }

        private Color GetEditorCategoryColor(LevelCategory category)
        {
            switch (category)
            {
                case LevelCategory.MainStory:
                    return new Color(0.1f, 0.8f, 1.0f, 0.9f); // Premium Glowing Cyan
                case LevelCategory.ResourceDungeon:
                    return new Color(1.0f, 0.75f, 0.15f, 0.9f); // Premium Glowing Amber
                case LevelCategory.RiteDungeon:
                    return new Color(0.85f, 0.35f, 1.0f, 0.9f); // Premium Glowing Purple
                case LevelCategory.VassalDungeon:
                    return new Color(1.0f, 0.3f, 0.3f, 0.9f); // Premium Glowing Red
                default:
                    return new Color(1.0f, 1.0f, 1.0f, 0.9f);
            }
        }
    }
}
