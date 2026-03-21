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

        private Stack<IUIController> _panelStack = new Stack<IUIController>();

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

            if (newPanel.AddsToHistory)
            {
                if (_panelStack.Count > 0)
                {
                    var currentTop = _panelStack.Peek();
                    if (currentTop != null) currentTop.Close();
                }
                _panelStack.Push(newPanel);
            }

            newPanel.ResetState();
            newPanel.Open();

            if (NavigationOverlay != null) NavigationOverlay.UpdateHighlight(newPanel.GetType());
            UpdateGlobalButtons();
        }

        public void GoBack(bool force = false)
        {
            // Reset overlay whenever we change panels
            if (NavigationOverlay != null) NavigationOverlay.Hide();

            if (_panelStack.Count <= 1)
            {
                ClearHistory(true, force);
                UpdateGlobalButtons();
                return;
            }

            var topPanel = _panelStack.Peek();
            if (!force && topPanel != null && !topPanel.RequestClose()) return;

            var closingPanel = _panelStack.Pop();
            if (closingPanel != null) closingPanel.Close();

            var previousPanel = _panelStack.Peek();
            if (previousPanel != null)
            {
                previousPanel.Open();
                if (NavigationOverlay != null) NavigationOverlay.UpdateHighlight(previousPanel.GetType());
            }
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
