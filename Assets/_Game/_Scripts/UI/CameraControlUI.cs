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
        [SerializeField] private Toggle _centerToggle;

        [Header("Icons")]
        [SerializeField] private Image _lockIconImage;
        [SerializeField] private Sprite _lockedIcon;
        [SerializeField] private Sprite _unlockedIcon;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI _viewText; // "Isometric" or "Top Down"
        
        [Inject] private CameraManager _cameraManager;

        public void Init()
        {
            if (_lockButton != null)
                _lockButton.onClick.AddListener(OnLockClicked);
            
            if (_viewButton != null)
                _viewButton.onClick.AddListener(OnViewClicked);

            if (_centerToggle != null)
            {
                _centerToggle.isOn = _cameraManager != null && _cameraManager.CenterOnMap;
                _centerToggle.onValueChanged.AddListener(OnCenterToggleChanged);
            }
            
            UpdateUI();
        }

        private void Update()
        {
            if (_cameraManager != null)
            {
                UpdateUI();
            }
        }
        
        private void OnLockClicked()
        {
            if (_cameraManager == null) return;
            _cameraManager.ToggleLock();
            UpdateUI();
        }

        private void OnViewClicked()
        {
            if (_cameraManager == null) return;
            _cameraManager.ToggleView();
            UpdateUI();
        }

        private void OnCenterToggleChanged(bool isOn)
        {
            if (_cameraManager == null) return;
            _cameraManager.CenterOnMap = isOn;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_cameraManager == null) return;

            // Update Lock Icon
            if (_lockIconImage != null)
            {
                if (_cameraManager.IsLocked)
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
                _viewButton.interactable = _cameraManager.IsLocked;
            }

            // Update View Text
            if (_viewText != null)
            {
                _viewText.text = _cameraManager.CurrentMode == CameraManager.ViewMode.Isometric ? "Isometric" : "Top Down";
            }
            
            // Update Center Toggle State
            if (_centerToggle != null)
            {
                _centerToggle.interactable = _cameraManager.IsLocked;
                 // Don't set isOn here every frame to avoid loop, though .isOn check probably handles it.
                 // Better to only set if different
                 if (_centerToggle.isOn != _cameraManager.CenterOnMap)
                 {
                     _centerToggle.isOn = _cameraManager.CenterOnMap;
                 }
            }
        }
    }
}
