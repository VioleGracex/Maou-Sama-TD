namespace MaouSamaTD.UI
{
    /// <summary>
    /// Interface for full screen UI Panels managed by the UIFlowManager.
    /// </summary>
    public interface IUIController
    {
        UnityEngine.GameObject VisualRoot { get; }
        void Open();
        void Close();
    }
}
