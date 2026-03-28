using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace CleverClicker.Ouiki
{
    public class CleverClickerPopup : EditorWindow
    {
        private List<GameObject> _objects;
        private Vector2 _scrollPosition;
        private int _selectedIndex = 0;
        private bool _isFirstFrame = true;
        private bool _isMultiSelect = false;
        private HashSet<GameObject> _selectedSet = new HashSet<GameObject>();

        public static void ShowPopup(Vector2 position, List<GameObject> objects, bool multiSelect)
        {
            if (objects == null || objects.Count == 0) return;

            CleverClickerPopup window = CreateInstance<CleverClickerPopup>();
            window._objects = objects;
            window._selectedIndex = 0;
            window._isMultiSelect = multiSelect;
            
            if (multiSelect)
            {
                window._selectedSet = new HashSet<GameObject>(Selection.gameObjects);
            }

            // Calculate window size based on number of objects
            float height = Mathf.Min(objects.Count * 22f + 40f, 400f);
            window.position = new Rect(position.x, position.y, 250f, height);
            window.ShowAsDropDown(new Rect(position.x, position.y, 1, 1), new Vector2(250f, height));
        }

        private void OnGUI()
        {
            if (_objects == null || _objects.Count == 0)
            {
                Close();
                return;
            }

            // Handle Keyboard Input
            HandleKeyboardInput();

            // Small dynamic title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.miniLabel);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            GUILayout.Label(_isMultiSelect ? "SELECT MULTIPLE" : "SELECT OBJECT", titleStyle);

            // Header - Select All
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.normal.textColor = Color.white;
            headerStyle.hover.textColor = Color.yellow;

            if (GUILayout.Button("Select ALL Objects", headerStyle, GUILayout.Height(25)))
            {
                SelectAll();
                Close();
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < _objects.Count; i++)
            {
                var obj = _objects[i];
                if (obj == null) continue;

                Rect itemRect = EditorGUILayout.GetControlRect(false, 20);
                bool isSelected = (i == _selectedIndex);

                // Highlight background
                bool isActualSelected = _isMultiSelect ? _selectedSet.Contains(obj) : (i == _selectedIndex);
                if (isSelected || itemRect.Contains(Event.current.mousePosition) || isActualSelected)
                {
                    Color bgColor = isSelected ? new Color(0.15f, 0.35f, 0.6f) : (isActualSelected ? new Color(0.2f, 0.4f, 0.2f) : new Color(0.3f, 0.3f, 0.3f));
                    EditorGUI.DrawRect(itemRect, bgColor);
                    
                    if (itemRect.Contains(Event.current.mousePosition))
                    {
                        _selectedIndex = i;
                        CleverClickerManager.SetHoverHighlight(obj);
                    }
                }

                // Draw content
                GUIContent content = EditorGUIUtility.ObjectContent(obj, typeof(GameObject));
                if (!CleverClickerSettings.ShowIcons) content.image = null;

                EditorGUI.LabelField(new Rect(itemRect.x + 5, itemRect.y, itemRect.width - 10, itemRect.height), content);

                // Handle Click
                if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
                {
                    if (_isMultiSelect)
                    {
                        ToggleMultiSelect(obj);
                    }
                    else
                    {
                        SelectObject(obj);
                        Close();
                    }
                    Event.current.Use();
                }
            }

            EditorGUILayout.EndScrollView();

            // Repaint continuously to handle hover effects smoothly
            if (mouseOverWindow == this) Repaint();
        }

        private void OnDestroy()
        {
            CleverClickerManager.SetHoverHighlight(null);
        }

        private void HandleKeyboardInput()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;

            switch (e.keyCode)
            {
                case KeyCode.UpArrow:
                    _selectedIndex = Mathf.Max(0, _selectedIndex - 1);
                    e.Use();
                    Repaint();
                    break;
                case KeyCode.DownArrow:
                    _selectedIndex = Mathf.Min(_objects.Count - 1, _selectedIndex + 1);
                    e.Use();
                    Repaint();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (_objects != null && _selectedIndex >= 0 && _selectedIndex < _objects.Count)
                    {
                        SelectObject(_objects[_selectedIndex]);
                        Close();
                        e.Use();
                    }
                    break;
                case KeyCode.Escape:
                    Close();
                    e.Use();
                    break;
            }
        }

        private void SelectObject(GameObject obj)
        {
            if (obj == null) return;
            Selection.activeGameObject = obj;
            if (CleverClickerSettings.FocusOnSelect)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
            EditorGUIUtility.PingObject(obj);
        }

        private void ToggleMultiSelect(GameObject obj)
        {
            if (_selectedSet.Contains(obj))
                _selectedSet.Remove(obj);
            else
                _selectedSet.Add(obj);

            Selection.objects = _selectedSet.Cast<Object>().ToArray();
        }

        private void SelectAll()
        {
            if (_objects != null)
            {
                Selection.objects = _objects.Cast<Object>().ToArray();
                if (_isMultiSelect)
                {
                    _selectedSet = new HashSet<GameObject>(_objects);
                }
            }
        }
    }
}
