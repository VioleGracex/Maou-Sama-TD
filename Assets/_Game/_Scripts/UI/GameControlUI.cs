using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
        
        [Header("Base HP")]
        [SerializeField] private Image _hpFillImage;
        [SerializeField] private TextMeshProUGUI _hpText;

        [Header("Pause Control")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private GameObject _pauseOverlay; 
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _retreatButton;
        [SerializeField] private Button _restartButton; // New Restart Button

        
        [Header("Confirmation")]
        [SerializeField] private GameObject _confirmationPanel;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;

        [Header("Game Over / Win")]
        [SerializeField] private GameObject _winPanel;
        [SerializeField] private GameObject _losePanel;
        [SerializeField] private Button _winRestartButton;
        [SerializeField] private Button _loseRestartButton;
        [Header("New Navigation")]
        [SerializeField] private Button _winReturnButton;
        [SerializeField] private Button _loseReturnButton;


        [Inject] private GameManager _gameManager;

        private const float MaxBaseLives = 20f; 

        private void Start()
        {
            if (_speedButton != null) _speedButton.onClick.AddListener(OnSpeedClicked);
            if (_pauseButton != null) _pauseButton.onClick.AddListener(OnPauseClicked);
            
            if (_resumeButton != null) _resumeButton.onClick.AddListener(OnPauseClicked); // Resume is just TogglePause
            if (_retreatButton != null) _retreatButton.onClick.AddListener(OnRetreatClicked);
            if (_restartButton != null) _restartButton.onClick.AddListener(ReloadScene); // Use existing ReloadScene method

            
            if (_confirmYesButton != null) _confirmYesButton.onClick.AddListener(OnConfirmRetreat);
            if (_confirmNoButton != null) _confirmNoButton.onClick.AddListener(OnCancelRetreat);
            
            if (_winRestartButton != null) _winRestartButton.onClick.AddListener(ReloadScene);
            if (_loseRestartButton != null) _loseRestartButton.onClick.AddListener(ReloadScene);
            if (_winReturnButton != null) _winReturnButton.onClick.AddListener(ReturnToMenu);
            if (_loseReturnButton != null) _loseReturnButton.onClick.AddListener(ReturnToMenu);


            if (_gameManager != null)
            {
                _gameManager.OnLivesChanged += UpdateHp;
                _gameManager.OnVictory += ShowWin;
                _gameManager.OnGameOver += ShowLose;
                UpdateHp(_gameManager.PlayerLives);
            }

            // Hide panels initially
            if (_winPanel != null) _winPanel.SetActive(false);
            if (_losePanel != null) _losePanel.SetActive(false);
            if (_confirmationPanel != null) _confirmationPanel.SetActive(false);
            if (_pauseOverlay != null) _pauseOverlay.SetActive(false);

            UpdateUI();
        }

        private void OnDestroy()
        {
            if (_gameManager != null)
            {
                _gameManager.OnLivesChanged -= UpdateHp;
                _gameManager.OnVictory -= ShowWin;
                _gameManager.OnGameOver -= ShowLose;
            }
        }

        private void ShowWin()
        {
            if (_winPanel != null) _winPanel.SetActive(true);
        }

        private void ShowLose()
        {
            if (_losePanel != null) _losePanel.SetActive(true);
        }

        private void OnRetreatClicked()
        {
            if (_confirmationPanel != null) _confirmationPanel.SetActive(true);
            else OnConfirmRetreat(); // Fallback if no panel
        }

        private void OnConfirmRetreat()
        {
            // Reload current scene or load Main Menu
            // Retreat = Go back to Menu
            ReturnToMenu();
        }
        
        private void ReturnToMenu()
        {
             // Resume time just in case
             Time.timeScale = 1f;
             // Load Scene 0 (Home/Menu)
             SceneManager.LoadScene(0);
        }

        private void OnCancelRetreat()
        {
            if (_confirmationPanel != null) _confirmationPanel.SetActive(false);
        }

        private void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void UpdateHp(int lives)
        {
            float pct = (float)lives / MaxBaseLives;
            
            if (_hpFillImage != null)
            {
                _hpFillImage.fillAmount = pct;
            }

            if (_hpText != null)
            {
                _hpText.text = $"{Mathf.CeilToInt(pct * 100)}%";
            }
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
            UpdateUI(); // Toggles overlay via IsPaused check
        }

        private void UpdateUI()
        {
            if (_gameManager == null) return;

            // Speed Text
            if (_speedText != null)
            {
                _speedText.text = $"{_gameManager.CurrentSpeed}x";
            }

            // Pause Overlay Logic
            if (_pauseOverlay != null)
            {
                // Only show pause overlay if paused AND not confirming retreat (optional, usually overlay is behind confirmation)
                // Actually, if we are paused, show overlay. 
                // Retreat Confirmation might be ON TOP of overlay.
                bool showPause = _gameManager.IsPaused;
                
                // If Game Over or Victory, maybe hide Pause Overlay?
                if (_gameManager.IsGameEnded) showPause = false;

                _pauseOverlay.SetActive(showPause);
            }
        }
    }
}
