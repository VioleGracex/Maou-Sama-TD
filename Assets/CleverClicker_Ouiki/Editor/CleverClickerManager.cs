using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace CleverClicker.Ouiki
{
    [InitializeOnLoad]
    public static class CleverClickerManager
    {
        private static GameObject _hoveredObject;

        static CleverClickerManager()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.delayCall += () => {
                if (CleverClickerSettings.IsFirstRun || CleverClickerSettings.ShowOnStartup)
                    CleverClickerSetupWindow.ShowWindow();
            };
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            // Trigger Selection Popup
            if (e.type == EventType.MouseDown && (e.button == 0 || e.button == 1) && (e.modifiers == CleverClickerSettings.ModifierKeys))
            {
                List<Object> pickedObjects = new List<Object>();
                HandleUtility.PickAllObjects(e.mousePosition, pickedObjects);
                
                var validObjects = pickedObjects
                    .OfType<GameObject>()
                    .Where(obj => (CleverClickerSettings.ExcludeLayers & (1 << obj.layer)) == 0)
                    .Distinct()
                    .ToList();

                if (validObjects.Count > 0)
                {
                    Vector2 screenPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
                    CleverClickerPopup.ShowPopup(screenPos, validObjects, e.button == 1);
                    e.Use();
                }
            }

            // Draw Highlight for hovered object
            if (_hoveredObject != null)
            {
                DrawHighlight(_hoveredObject, CleverClickerSettings.HighlightColor);
            }
        }

        public static void SetHoverHighlight(GameObject obj)
        {
            _hoveredObject = obj;
            SceneView.RepaintAll();
        }

        private static void DrawHighlight(GameObject obj, Color color)
        {
            if (obj == null) return;

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            Handles.color = color;

            foreach (var r in renderers)
            {
                if (r == null) continue;
                Vector3 center = r.bounds.center;
                Vector3 size = r.bounds.size;
                Handles.DrawWireCube(center, size);
                Handles.DrawSolidRectangleWithOutline(GetBoundsVertices(r.bounds), new Color(color.r, color.g, color.b, 0.1f), color);
            }
        }

        private static Vector3[] GetBoundsVertices(Bounds b)
        {
            // Simple 2D approximation or just use wireframes for 3D
            return new Vector3[] {
                new Vector3(b.min.x, b.min.y, b.min.z),
                new Vector3(b.max.x, b.min.y, b.min.z),
                new Vector3(b.max.x, b.max.y, b.min.z),
                new Vector3(b.min.x, b.max.y, b.min.z)
            };
        }
    }
}
