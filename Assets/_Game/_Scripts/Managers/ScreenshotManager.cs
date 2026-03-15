using UnityEngine;
using System.IO;
using DG.Tweening;

namespace MaouSamaTD.Managers
{
    /// <summary>
    /// Captures high-resolution screenshots for development progress and social media.
    /// Default key: F12
    /// </summary>
    public class ScreenshotManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string _folderName = "Screenshots";
        [SerializeField] private KeyCode _captureKey = KeyCode.F12;
        [SerializeField] private int _superSize = 1; // Increase for higher res

        [Header("UI Feedback")]
        [SerializeField] private CanvasGroup _flashOverlay;
        [SerializeField] private float _flashDuration = 0.2f;

        private void Update()
        {
            if (Input.GetKeyDown(_captureKey))
            {
                Capture();
            }
        }

        public void Capture()
        {
            string directory = Path.Combine(Application.persistentDataPath, _folderName);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string fileName = $"MaouSamaTD_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string fullPath = Path.Combine(directory, fileName);

            ScreenCapture.CaptureScreenshot(fullPath, _superSize);
            Debug.Log($"[ScreenshotManager] Screenshot saved to: {fullPath}");

            if (_flashOverlay != null)
            {
                TriggerFlash();
            }
        }

        private void TriggerFlash()
        {
            _flashOverlay.alpha = 1f;
            _flashOverlay.DOFade(0f, _flashDuration).SetEase(Ease.OutQuad);
        }
    }
}
