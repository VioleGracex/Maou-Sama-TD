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
                ClearHistory(true, force);
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
            
            if (closingPanel != null) closingPanel.Close();

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
                    }
                    if (NavigationOverlay != null) NavigationOverlay.UpdateHighlight(previousPanel.GetType());
                }
            }
            
            _isProcessing = false;
            UpdateGlobalButtons();
        }
        
        public void ClearHistory(bool closeCurrent = true, bool force = false)
        {
            // Reset overlay
            if (NavigationOverlay != null) NavigationOverlay.Hide();

            if (closeCurrent && _panelStack.Count > 0)
            {
                var top = _panelStack.Peek();
                if (!force && top != null && !top.RequestClose()) return;
                var current = _panelStack.Pop();
                if (current != null) current.Close();
            }
            _panelStack.Clear();
            UpdateGlobalButtons();
            if (NavigationOverlay != null) NavigationOverlay.UpdateHighlight(null);
        }

        private void UpdateGlobalButtons()
        {
            if (_panelStack.Count == 0)
            {
                if (_backBtnRoot != null) _backBtnRoot.SetActive(false);
                if (_citadelBtnRoot != null) _citadelBtnRoot.SetActive(false);
                return;
            }

            var top = _panelStack.Peek();
            var features = top.ConfiguredNavFeatures;

            if (_backBtnRoot != null) 
                _backBtnRoot.SetActive((features & NavigationFeatures.BackButton) != 0);
            
            if (_citadelBtnRoot != null) 
                _citadelBtnRoot.SetActive((features & NavigationFeatures.CitadelButton) != 0);
        }

        public void UpdateNavigationFeatures(NavigationFeatures features)
        {
            if (_backBtnRoot != null) 
                _backBtnRoot.SetActive((features & NavigationFeatures.BackButton) != 0);
            
            if (_citadelBtnRoot != null) 
                _citadelBtnRoot.SetActive((features & NavigationFeatures.CitadelButton) != 0);
        }
        private bool IsChildOf(IUIController child, IUIController parent)
        {
            if (child == null || parent == null) return false;
            if (child.VisualRoot == null || parent.VisualRoot == null) return false;
            return child.VisualRoot.transform.IsChildOf(parent.VisualRoot.transform);
        }
    }
}
