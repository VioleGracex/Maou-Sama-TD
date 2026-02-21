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
        public static UIFlowManager Instance { get; private set; }

        // The History Stack
        private Stack<IUIController> _panelStack = new Stack<IUIController>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Optionally DontDestroyOnLoad if this persists across all scenes, 
                // but usually better per-scene if UI setups differ heavily.
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Pushes a new panel to the front and hides the previous one (if any).
        /// </summary>
        public void OpenPanel(IUIController newPanel)
        {
            if (newPanel == null) return;

            // Pause/Hide current top panel
            if (_panelStack.Count > 0)
            {
                var currentTop = _panelStack.Peek();
                // Technically we just close it, but it stays in history
                if (currentTop != null)
                {
                    currentTop.Close(); 
                }
            }

            // Push and open new panel
            _panelStack.Push(newPanel);
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
                return; // Nothing to go back to, or we don't want to close the absolute root
            }

            // Pop and fully close current
            var closingPanel = _panelStack.Pop();
            if (closingPanel != null)
            {
                closingPanel.Close();
            }

            // Restore the previous panel
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
    }
}
