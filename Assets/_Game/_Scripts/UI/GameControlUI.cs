using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Managers;
using Zenject;

namespace MaouSamaTD.UI
{
    public class GameControlUI : MonoBehaviour
    {
        [Header("Speed Control")]
        [SerializeField] private Button _speedButton;
        [SerializeField] private TextMeshProUGUI _speedText;

        [Header("Pause Control")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private GameObject _pauseOverlay; // The full screen overlay
        [SerializeField] private TextMeshProUGUI _pauseText; // "PAUSED" text if separate

        [Inject] private GameManager _gameManager;

        private void Start()
        {
            if (_gameManager == null)
            {
               _gameManager = FindObjectOfType<GameManager>();
            }

            if (_speedButton != null)
                _speedButton.onClick.AddListener(OnSpeedClicked);
            
            if (_pauseButton != null)
                _pauseButton.onClick.AddListener(OnPauseClicked);

            UpdateUI();
        }

        private void OnSpeedClicked()
        {
            if (_gameManager == null) return;
            
            // Toggle between 1x and 2x
            float newSpeed = (_gameManager.CurrentSpeed >= 2f) ? 1f : 2f;
            _gameManager.SetSpeed(newSpeed);
            UpdateUI();
        }

        private void OnPauseClicked()
        {
            if (_gameManager == null) return;
            _gameManager.TogglePause();
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_gameManager == null) return;

            // Speed Text
            if (_speedText != null)
            {
                _speedText.text = $"{_gameManager.CurrentSpeed}x";
            }

            // Pause Overlay
            if (_pauseOverlay != null)
            {
                _pauseOverlay.SetActive(_gameManager.IsPaused);
            }
            
            // Optional: Pause Button Text/Icon change?
            // Usually Pause button stays "Pause" or turns to "Resume"
            // For now simplest is just overlay.
        }
    }
}
