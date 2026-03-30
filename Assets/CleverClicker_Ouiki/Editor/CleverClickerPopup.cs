using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace CleverClicker.Ouiki
{
    public class CleverClickerPopup : EditorWindow
    {
        private List<GameObject> _allObjects;
        private List<GameObject> _filteredObjects;
        private Vector2 _scrollPosition;
        private int _selectedIndex = 0;
        private bool _isMultiSelect = false;
        private HashSet<GameObject> _selectedSet = new HashSet<GameObject>();

        public static void ShowPopup(Vector2 position, List<GameObject> objects, bool multiSelect)
        {
            if (objects == null || objects.Count == 0) return;

            CleverClickerPopup window = CreateInstance<CleverClickerPopup>();
            window._allObjects = objects;
            window.UpdateFilteredList();
            
            window._selectedIndex = 0;
            window._isMultiSelect = multiSelect;
            
            if (multiSelect)
            {
                window._selectedSet = new HashSet<GameObject>(Selection.gameObjects);
            }

            // Calculate window size based on number of objects
            float height = Mathf.Min(window._filteredObjects.Count * 22f + 80f, 400f);
            window.position = new Rect(position.x, position.y, 250f, height);
            window.ShowAsDropDown(new Rect(position.x, position.y, 1, 1), new Vector2(250f, height));
        }

        private void UpdateFilteredList()
        {
            if (_allObjects == null) return;

            if (CleverClickerSettings.ShowOnlyActive)
            {
                _filteredObjects = _allObjects.Where(obj => obj != null && obj.activeInHierarchy).ToList();
            }
            else
            {
                _filteredObjects = new List<GameObject>(_allObjects);
            }

            if (_selectedIndex >= _filteredObjects.Count)
                _selectedIndex = Mathf.Max(0, _filteredObjects.Count - 1);
        }

        private void OnGUI()
        {
            if (_filteredObjects == null || _filteredObjects.Count == 0)
            {
                if (_allObjects != null && !CleverClickerSettings.ShowOnlyActive)
                {
                    // If we have objects but they are all hidden by filter, we might want to stay open
                    // but since we checked (allObjects != null && !ShowOnlyActive), it means list is actually empty
                }
                else if (_allObjects != null && _allObjects.Count > 0)
                {
                    // Stay open to allow unchecking "Active Only"
                }
                else
                {
                    Close();
                    return;
                }
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

            // FILTER SETTING
            EditorGUI.BeginChangeCheck();
            bool onlyActive = EditorGUILayout.ToggleLeft("Active Only", CleverClickerSettings.ShowOnlyActive);
            if (EditorGUI.EndChangeCheck())
            {
                CleverClickerSettings.ShowOnlyActive = onlyActive;
                UpdateFilteredList();
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < _filteredObjects.Count; i++)
            {
                var obj = _filteredObjects[i];
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
                    _selectedIndex = Mathf.Min(_filteredObjects.Count - 1, _selectedIndex + 1);
                    e.Use();
                    Repaint();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (_filteredObjects != null && _selectedIndex >= 0 && _selectedIndex < _filteredObjects.Count)
                    {
                        SelectObject(_filteredObjects[_selectedIndex]);
                        Close();
                        e.Use();
                    }
                    break;
                case KeyCode.Escape:
                    Close();
                    e.Use();
                    break;
                default:
                    if (e.keyCode >= KeyCode.A && e.keyCode <= KeyCode.Z)
                    {
                        NavigateByLetter((char)('a' + (e.keyCode - KeyCode.A)));
                        e.Use();
                    }
                    else if (e.keyCode >= KeyCode.Alpha0 && e.keyCode <= KeyCode.Alpha9)
                    {
                        NavigateByLetter((char)('0' + (e.keyCode - KeyCode.Alpha0)));
                        e.Use();
                    }
                    break;
            }
        }

        private void NavigateByLetter(char letter)
        {
            if (_filteredObjects == null || _filteredObjects.Count == 0) return;

            string searchLetter = letter.ToString().ToLower();
            
            // Find current match and look for next one (cycle)
            int startIndex = (_selectedIndex + 1) % _filteredObjects.Count;
            
            for (int i = 0; i < _filteredObjects.Count; i++)
            {
                int index = (startIndex + i) % _filteredObjects.Count;
                var obj = _filteredObjects[index];
                if (obj != null && obj.name.ToLower().StartsWith(searchLetter))
                {
                    _selectedIndex = index;
                    ScrollToSelected();
                    Repaint();
                    break;
                }
            }
        }

        private void ScrollToSelected()
        {
            // Simple visual feedback for keyboard navigation
            _scrollPosition.y = Mathf.Max(0, (_selectedIndex * 20f) - 60f); 
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
            if (_filteredObjects != null)
            {
                Selection.objects = _filteredObjects.Cast<Object>().ToArray();
                if (_isMultiSelect)
                {
                    _selectedSet = new HashSet<GameObject>(_filteredObjects);
                }
            }
        }
    }
}
