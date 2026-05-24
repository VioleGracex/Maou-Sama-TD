using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.UI.MainMenu;

namespace MaouSamaTD.UI
{
    public class UIFlowManager : MonoBehaviour
    {
        public static UIFlowManager Instance { get; private set; }

        [Header("Global UI References")]
        [SerializeField] public GameObject _backBtnRoot;
        [SerializeField] public GameObject _citadelBtnRoot;
        [Header("Global UI References")]
        public UINavigationOverlay NavigationOverlay;
        public UnitInspectorFullScreenUI UnitInspector;

        [SerializeField] private bool _debug = true;
        private Stack<IUIController> _panelStack = new Stack<IUIController>();
        private bool _isProcessing = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (_backBtnRoot != null)
            {
                var btn = _backBtnRoot.GetComponent<UnityEngine.UI.Button>();
                if (btn != null) btn.onClick.AddListener(() => GoBack());
            }

            if (_citadelBtnRoot != null)
            {
                var btn = _citadelBtnRoot.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => 
                    {
                        if (NavigationOverlay != null) NavigationOverlay.Toggle();
                    });
                }
            }

            UpdateGlobalButtons();
        }

        public void OpenPanel(IUIController newPanel)
        {
            if (newPanel == null) return;

            // Reset overlay whenever we change panels
            if (NavigationOverlay != null) NavigationOverlay.Hide();

            if (_isProcessing) return;
            _isProcessing = true;

            if (newPanel.AddsToHistory)
            {
                if (_panelStack.Count > 0)
                {
                    var currentTop = _panelStack.Peek();
                    bool isNested = IsChildOf(newPanel, currentTop);
                    
                    if (!isNested)
                    {
                        if (_debug) Debug.Log($"[UIFlow] Closing current top: {currentTop.GetType().Name}");
                        if (currentTop != null) currentTop.Close();
                    }
                    else
                    {
                        if (_debug) Debug.Log($"[UIFlow] New panel is NESTED. Keeping parent {currentTop.GetType().Name} active.");
                    }
                }
                _panelStack.Push(newPanel);
            }

            if (_debug) Debug.Log($"[UIFlow] Opening panel: {newPanel.GetType().Name}. Stack size: {_panelStack.Count}");
            newPanel.ResetState();
            newPanel.Open();

            _isProcessing = false;

            if (NavigationOverlay != null) NavigationOverlay.UpdateHighlight(newPanel.GetType());
            UpdateGlobalButtons();
        }

        public void GoBack(bool force = false)
        {
            if (_debug) Debug.Log($"[UIFlow] GoBack called. Force: {force}. Stack count: {_panelStack.Count}");
            // Reset overlay whenever we change panels
            if (NavigationOverlay != null) NavigationOverlay.Hide();

            if (_isProcessing) return;
            _isProcessing = true;

            if (_panelStack.Count <= 1)
            {
                if (_debug) Debug.Log("[UIFlow] GoBack called on root or empty stack. Clearing history.");
                _isProcessing = false; // Must reset before calling ClearHistory which also uses it
                var originalTopPanel = _panelStack.Count > 0 ? _panelStack.Peek() : null;
                ClearHistory(true, force);

                if (_panelStack.Count == 0 && originalTopPanel != null && !(originalTopPanel is HomeUIManager))
                {
                    var home = Object.FindAnyObjectByType<HomeUIManager>(FindObjectsInactive.Include);
                    if (home != null)
                    {
                        OpenPanel(home);
                    }
                }
                UpdateGlobalButtons();
                return;
            }

            var topPanel = _panelStack.Peek();
            if (!force && topPanel != null && !topPanel.RequestClose()) 
            {
                _isProcessing = false;
                return;
            }

            var closingPanel = _panelStack.Pop();
            if (_debug) Debug.Log($"[UIFlow] Popped: {closingPanel.GetType().Name}. Stack remaining: {_panelStack.Count}");
            
            // Reactivate the previous panel first to ensure continuous rendering and minimum resource interruption
            if (_panelStack.Count > 0)
            {
                var previousPanel = _panelStack.Peek();
                if (previousPanel != null)
                {
                    bool alreadyActive = previousPanel.VisualRoot != null && previousPanel.VisualRoot.activeInHierarchy;
                    if (!alreadyActive)
                    {
                        if (_debug) Debug.Log($"[UIFlow] Returning to: {previousPanel.GetType().Name}");
                        previousPanel.Open();
                    }
                    else
                    {
                        if (_debug) Debug.Log($"[UIFlow] Returning to already active parent: {previousPanel.GetType().Name}");
                        // Explicitly open/refresh it to ensure its sub-panels (like Main_Page) are properly set active
                        previousPanel.Open();
                    }
                    if (NavigationOverlay != null) NavigationOverlay.UpdateHighlight(previousPanel.GetType());
                }
            }

            // Deactivate the closing panel second
            if (closingPanel != null) closingPanel.Close();
            
            _isProcessing = false;
            UpdateGlobalButtons();
        }
        
        public void ClearHistory(bool closeCurrent = true, bool force = false)
        {
            // Reset overlay
            if (NavigationOverlay != null) NavigationOverlay.Hide();

            if (_panelStack.Count > 0)
            {
                if (closeCurrent)
                {
                    var top = _panelStack.Peek();
                    if (!force && top != null && !top.RequestClose()) return;
                }

                while (_panelStack.Count > 0)
                {
                    var panel = _panelStack.Pop();
                    if (panel != null)
                    {
                        panel.Close();
                    }
                }
            }
            _panelStack.Clear();
            UpdateGlobalButtons();
            if (NavigationOverlay != null) NavigationOverlay.UpdateHighlight(null);
        }

        private GameObject _navigationHolderCached;
        private GameObject GetNavigationHolder()
        {
            if (_navigationHolderCached != null) return _navigationHolderCached;

            if (_backBtnRoot != null)
            {
                Transform current = _backBtnRoot.transform;
                while (current != null)
                {
                    if (current.name == "NavigationHolder")
                    {
                        _navigationHolderCached = current.gameObject;
                        return _navigationHolderCached;
                    }
                    current = current.parent;
                }
            }

            // Fallback: search parents of this UIFlowManager
            Transform t = transform;
            while (t != null)
            {
                var found = t.Find("NavigationHolder");
                if (found != null)
                {
                    _navigationHolderCached = found.gameObject;
                    return _navigationHolderCached;
                }
                t = t.parent;
            }

            // Fallback: search from all root GameObjects of the active scene
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == "NavigationHolder")
                {
                    _navigationHolderCached = root;
                    return _navigationHolderCached;
                }
                var child = FindDeepChild(root.transform, "NavigationHolder");
                if (child != null)
                {
                    _navigationHolderCached = child.gameObject;
                    return _navigationHolderCached;
                }
            }

            return null;
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void UpdateGlobalButtons()
        {
            GameObject navigationHolder = GetNavigationHolder();

            if (_panelStack.Count == 0)
            {
                if (_backBtnRoot != null) _backBtnRoot.SetActive(false);
                if (_citadelBtnRoot != null) _citadelBtnRoot.SetActive(false);
                if (navigationHolder != null) navigationHolder.SetActive(false);
                return;
            }

            var top = _panelStack.Peek();
            var features = top.ConfiguredNavFeatures;

            bool showBack = (features & NavigationFeatures.BackButton) != 0;
            bool showCitadel = (features & NavigationFeatures.CitadelButton) != 0;

            if (_backBtnRoot != null) 
                _backBtnRoot.SetActive(showBack);
            
            if (_citadelBtnRoot != null) 
                _citadelBtnRoot.SetActive(showCitadel);

            if (navigationHolder != null)
            {
                navigationHolder.SetActive(showBack || showCitadel);
            }
        }

        public void UpdateNavigationFeatures(NavigationFeatures features)
        {
            bool showBack = (features & NavigationFeatures.BackButton) != 0;
            bool showCitadel = (features & NavigationFeatures.CitadelButton) != 0;

            if (_backBtnRoot != null) 
                _backBtnRoot.SetActive(showBack);
            
            if (_citadelBtnRoot != null) 
                _citadelBtnRoot.SetActive(showCitadel);

            GameObject navigationHolder = GetNavigationHolder();

            if (navigationHolder != null)
            {
                navigationHolder.SetActive(showBack || showCitadel);
            }
        }
        public NavigationFeatures GetCurrentNavFeatures()
        {
            if (_panelStack.Count == 0) return NavigationFeatures.None;
            return _panelStack.Peek().ConfiguredNavFeatures;
        }

        private bool IsChildOf(IUIController child, IUIController parent)
        {
            if (child == null || parent == null) return false;
            if (child.VisualRoot == null || parent.VisualRoot == null) return false;
            return child.VisualRoot.transform.IsChildOf(parent.VisualRoot.transform);
        }
    }
}
