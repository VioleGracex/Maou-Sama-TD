namespace MaouSamaTD.UI
{
    [System.Flags]
    public enum NavigationFeatures
    {
        None = 0,
        BackButton = 1 << 0,
        CitadelButton = 1 << 1
    }

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

        /// <summary>
        /// Specifies which global navigation features (Back, Citadel) are allowed on this particular page.
        /// </summary>
        NavigationFeatures ConfiguredNavFeatures { get; }
        
        void Open();
        void Close();
        
        /// <summary>
        /// Called when the user attempts to close/navigate away. 
        /// Returns true if the panel can close immediately. 
        /// Returns false if the panel blocks closing (e.g. to show a confirmation popup).
        /// </summary>
        bool RequestClose();

        /// <summary>
        /// Resets the panel to its default, original state. Handled automatically by HomeUIManager/UIFlowManager.
        /// </summary>
        void ResetState();

        /// <summary>
        /// Optional: Load data into memory without instantiating UI.
        /// </summary>
        void Preheat() { }
    }
}
