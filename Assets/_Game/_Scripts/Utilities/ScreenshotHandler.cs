using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.IO;

namespace MaouSamaTD.Utilities
{
    public class ScreenshotHandler : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string _folderName = "Screenshots";

        [Header("Visual Feedback")]
        [SerializeField] public CanvasGroup _flashOverlay;
        [SerializeField] private float _flashDuration = 0.4f;

        private void Update()
        {
            // Use New Input System for hotkey detection
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f11Key.wasPressedThisFrame || keyboard.printScreenKey.wasPressedThisFrame)
            {
                TriggerCapture();
            }
        }

        public void TriggerCapture()
        {
            string folderPath = Path.Combine(Application.dataPath, "..", _folderName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"MaouSamaTD_{timestamp}.png";
            string filePath = Path.Combine(folderPath, fileName);

            // Capture the screenshot
            ScreenCapture.CaptureScreenshot(filePath);

            Debug.Log($"<color=cyan>[Screenshot]</color> Frame captured. Saved to: <b>{filePath}</b>");

            // Visual feedback - flash white and fade out
            if (_flashOverlay != null)
            {
                _flashOverlay.DOKill();
                _flashOverlay.alpha = 1f;
                _flashOverlay.DOFade(0f, _flashDuration).SetEase(Ease.OutCubic).SetUpdate(true);
            }
        }
    }
}
