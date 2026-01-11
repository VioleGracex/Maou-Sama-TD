using UnityEngine;
using UnityEngine.UI;

namespace MaouSamaTD.Managers
{
    public class CanvasSetup : MonoBehaviour
    {
        // One-time setup helper
        [ContextMenu("Setup Canvas")]
        public void Setup()
        {
            var canvas = new GameObject("Canvas");
            canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();
        }
    }
}