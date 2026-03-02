using UnityEngine;
using System.Collections.Generic;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Centralized navigation state machine for managing full-screen UI panels 
    /// (e.g., Campaign -> Briefing -> Mission Readiness -> Barracks).
    /// </summary>
    public class UIFlowManager : MonoBehaviour
    {
        #region  Singleton
        private static UIFlowManager _instance;
        public static UIFlowManager Instance 
        { 
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<UIFlowManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("UIFlowManager_AutoInstance");
                        _instance = go.AddComponent<UIFlowManager>();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region State
        private Stack<IUIController> _panelStack = new Stack<IUIController>();
        #endregion

        #region Initialization
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Attempts to open a new panel. If it AddsToHistory, the previous panel is hidden 
        /// and it gets pushed to the stack. If not, it simply opens as an overlay.
        /// </summary>
        public void OpenPanel(IUIController newPanel)
        {
            if (newPanel == null) return;

            if (newPanel.AddsToHistory)
            {
                if (_panelStack.Count > 0)
                {
                    var currentTop = _panelStack.Peek();
                    if (currentTop != null)
                    {
                        currentTop.Close(); 
                    }
                }

                _panelStack.Push(newPanel);
            }

            newPanel.ResetState();
            newPanel.Open();
        }

        /// <summary>
        /// Pops the current panel and returns to the previous one in history.
        /// </summary>
        public void GoBack()
        {
            if (_panelStack.Count <= 1)
            {
                Debug.LogWarning("[UIFlowManager] No more history to go back to. Ignoring.");
                return;
            }

            var closingPanel = _panelStack.Pop();
            if (closingPanel != null)
            {
                closingPanel.Close();
            }

            var previousPanel = _panelStack.Peek();
            if (previousPanel != null)
            {
                previousPanel.Open();
            }
        }
        
        /// <summary>
        /// Completely clears the history stack and closes everything.
        /// </summary>
        public void ClearHistory(bool closeCurrent = true)
        {
            if (closeCurrent && _panelStack.Count > 0)
            {
                var current = _panelStack.Pop();
                if (current != null) current.Close();
            }
            _panelStack.Clear();
        }
        #endregion
    }
}
