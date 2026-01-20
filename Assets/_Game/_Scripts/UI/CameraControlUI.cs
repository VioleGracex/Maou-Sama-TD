using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Managers;
using Zenject;

namespace MaouSamaTD.UI
{
    public class CameraControlUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _lockButton;
        [SerializeField] private Button _viewButton;

        [Header("Icons")]
        [SerializeField] private Image _lockIconImage;
        [SerializeField] private Sprite _lockedIcon;
        [SerializeField] private Sprite _unlockedIcon;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI _viewText; // "Isometric" or "Top Down"
        
        [Inject] private CameraController _cameraController;

        private void Start()
        {
            // Fallback find if not injected
            if (_cameraController == null)
            {
                _cameraController = FindObjectOfType<CameraController>();
            }

            if (_lockButton != null)
                _lockButton.onClick.AddListener(OnLockClicked);
            
            if (_viewButton != null)
                _viewButton.onClick.AddListener(OnViewClicked);

            UpdateUI();
        }

        private void Update()
        {
            if (_cameraController != null)
            {
                UpdateUI();
            }
        }

        private void OnLockClicked()
        {
            if (_cameraController == null) return;
            _cameraController.ToggleLock();
            UpdateUI();
        }

        private void OnViewClicked()
        {
            if (_cameraController == null) return;
            _cameraController.ToggleView();
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_cameraController == null) return;

            // Update Lock Icon
            if (_lockIconImage != null)
            {
                if (_cameraController.IsLocked)
                {
                    if (_lockedIcon != null) _lockIconImage.sprite = _lockedIcon;
                    _lockIconImage.color = _lockedIcon != null ? Color.white : Color.green; // Fallback color
                }
                else
                {
                    if (_unlockedIcon != null) _lockIconImage.sprite = _unlockedIcon;
                    _lockIconImage.color = _unlockedIcon != null ? Color.white : Color.yellow; // Fallback color
                }
            }

            // Update View Button State
            if (_viewButton != null)
            {
                _viewButton.interactable = _cameraController.IsLocked;
            }

            // Update View Text
            if (_viewText != null)
            {
                _viewText.text = _cameraController.CurrentMode == CameraController.ViewMode.Isometric ? "Isometric" : "Top Down";
            }
        }
    }
}
