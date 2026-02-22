namespace MaouSamaTD.UI
{
    /// <summary>
    /// Interface for full screen UI Panels managed by the UIFlowManager.
    /// </summary>
    public interface IUIController
    {
        UnityEngine.GameObject VisualRoot { get; }
        
        /// <summary>
        /// If true, opening this panel hides the previous panel and adds it to the FlowManager's back-history.
        /// If false, this panel simply opens as an overlay (popup) without affecting the back stack.
        /// </summary>
        bool AddsToHistory { get; }
        
        void Open();
        void Close();
        
        /// <summary>
        /// Resets the panel to its default, original state. Handled automatically by HomeUIManager/UIFlowManager.
        /// </summary>
        void ResetState();
    }
}
