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
                if (btn != null) btn.onClick.AddListener(() => 
                {
                    if (NavigationOverlay != null) NavigationOverlay.Toggle();
                });
            }
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
                    if (_debug) Debug.Log($"[UIFlow] Closing current top: {currentTop.GetType().Name}");
                    if (currentTop != null) currentTop.Close();
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
                    if (_debug) Debug.Log($"[UIFlow] Returning to: {previousPanel.GetType().Name}");
                    previousPanel.Open();
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
                // Root/Home state
                if (_backBtnRoot != null) _backBtnRoot.SetActive(false);
                if (_citadelBtnRoot != null) _citadelBtnRoot.SetActive(false);
                return;
            }

            var top = _panelStack.Peek();
            bool isHome = top is HomeUIManager;
            bool isSettings = top.GetType().Name.Contains("Settings"); // Use flexible check

            if (isHome)
            {
                if (_backBtnRoot != null) _backBtnRoot.SetActive(false);
                if (_citadelBtnRoot != null) _citadelBtnRoot.SetActive(false);
            }
            else if (isSettings)
            {
                if (_backBtnRoot != null) _backBtnRoot.SetActive(true);
                if (_citadelBtnRoot != null) _citadelBtnRoot.SetActive(false);
            }
            else
            {
                // Any other page
                if (_backBtnRoot != null) _backBtnRoot.SetActive(true);
                if (_citadelBtnRoot != null) _citadelBtnRoot.SetActive(true);
            }
        }
    }
}
