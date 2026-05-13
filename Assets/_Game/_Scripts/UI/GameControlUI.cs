using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using MaouSamaTD.Managers;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using MaouSamaTD.Levels;
using Zenject;
using DG.Tweening;
using MaouSamaTD.UI.Tutorial;

namespace MaouSamaTD.UI
{
    public class GameControlUI : MonoBehaviour
    {
        [Header("Speed Control")]
        [SerializeField] private Button _speedButton;
        [SerializeField] private TextMeshProUGUI _speedText;
        [SerializeField] private GameObject _tacticalIndicator;
        
        [Header("Base HP")]
        [SerializeField] private GameObject _baseHPContainer;
        [SerializeField] private Image _hpFillImage;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _hpPercentageText;

        [Header("Status Tracking")]
        [SerializeField] private TextMeshProUGUI _waveText;
        [SerializeField] private TextMeshProUGUI _enemyCountText;
        [SerializeField] private TextMeshProUGUI _sealsText;

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
        [SerializeField] private Button _winNextButton;
        [SerializeField] private TextMeshProUGUI _levelTitleText;
        [SerializeField] private TextMeshProUGUI _clearTimeText;

        [Header("Stars & Results")]
        [SerializeField] private Transform _starConditionContainer;
        [SerializeField] private GameObject _starConditionPrefab;
        [SerializeField] private Sprite _starFullSprite;
        [SerializeField] private Sprite _starEmptySprite;
        [Header("Debug")]
        [SerializeField] private bool _showDebugLogs = true;

        [Header("HP Feedback Settings")]
        [SerializeField] private float _damageSlowMoDuration = 0.5f;
        [SerializeField] private float _damageSlowMoScale = 0.5f;
        [SerializeField] private float _cameraShakeIntensity = 0.15f;
        [SerializeField] private float _cameraShakeDuration = 0.25f;
        [SerializeField] private float _hpFillDuration = 0.4f;

        private int _lastHp = -1;

        [Inject] private GameManager _gameManager;
        [Inject] private MaouSamaTD.UI.UIPopupBlocker _uiBlocker;
        [Inject] private GameSelectionState _selectionState;

        [Inject(Optional = true)] private EnemyManager _enemyManager;
        [Inject(Optional = true)] private BattleCurrencyManager _currencyManager;
        [Inject(Optional = true)] private TutorialManager _tutorialManager;

        // Dynamic MaxNexusIntegrity is read from GameManager instead of a hardcoded constant

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
            if (_winNextButton != null) _winNextButton.onClick.AddListener(OnNextLevelClicked);


            if (_gameManager != null)
            {
                _gameManager.OnObjectiveHPChanged += UpdateHp;
                _gameManager.OnVictory += ShowWin;
                _gameManager.OnGameOver += ShowLose;
                _gameManager.OnSpeedChanged += (s) => UpdateUI();
                UpdateHp(_gameManager.ObjectiveHP);
            }

            // Hide panels initially, dynamic find as fallback if null
            if (_winPanel != null) _winPanel.SetActive(false);
            if (_losePanel != null) _losePanel.SetActive(false);
            if (_confirmationPanel != null) _confirmationPanel.SetActive(false);
            if (_pauseOverlay != null) _pauseOverlay.SetActive(false);


            UpdateUI();
        }


        private void Update()
        {
            if (_waveText != null && _enemyManager != null && _enemyManager.TotalWaves > 0)
            {
                int currentWave = _enemyManager.CurrentWaveIndex + 1;
                int totalWaves = _enemyManager.TotalWaves;
                int remaining = _enemyManager.CurrentWaveRemainingEnemies;
                int total = _enemyManager.CurrentWaveTotalEnemies;

                if (_enemyCountText != null)
                {
                    _waveText.text = $"Wave: {currentWave} / {totalWaves}";
                    _enemyCountText.text = $"({remaining}/{total})";
                }
                else
                {
                    _waveText.text = $"Wave: {currentWave} / {totalWaves} ({remaining}/{total})";
                }
            }
            else if (_waveText != null)
            {
                _waveText.text = "Wave: 1";
                if (_enemyCountText != null)
                {
                    _enemyCountText.text = "";
                }
            }
            if (_sealsText != null && _currencyManager != null)
            {
                _sealsText.text = $"{_currencyManager.CurrentSeals} / {_currencyManager.MaxSeals}";
            }

            // Auto-hide Top-Middle HP bar if Mini-Dialogue is active (to prevent overlapping)
            if (_baseHPContainer != null)
            {
                DialogueUI dialogueUI = FindFirstObjectByType<DialogueUI>();
                bool showBaseHP = dialogueUI == null || !dialogueUI.IsShowingMiniDialogue;
                if (_baseHPContainer.activeSelf != showBaseHP)
                {
                    _baseHPContainer.SetActive(showBaseHP);
                }
            }

            // Allow the speed button to be interactable even during tutorial time stops.
            // This ensures players can manually resume or change speed if they feel stuck.
            if (_speedButton != null)
                _speedButton.interactable = true;
        }

        private void OnDestroy()
        {
            if (_gameManager != null)
            {
                _gameManager.OnObjectiveHPChanged -= UpdateHp;
                _gameManager.OnVictory -= ShowWin;
                _gameManager.OnGameOver -= ShowLose;
                _gameManager.OnSpeedChanged -= (s) => UpdateUI();
            }
        }

        public void ShowWin()
        {
            Debug.Log($"[GameControlUI] ShowWin() event triggered! _winPanel is null? {_winPanel == null}");
            if (_uiBlocker != null) _uiBlocker.HideBlocker(true);
            
            if (_winPanel == null)
            {
                Debug.LogError("[GameControlUI] _winPanel is null! Please assign it in the Inspector.");
                return;
            }

            if (_winPanel != null)
            {
                Debug.Log($"[GameControlUI] Activating Victory Panel '{_winPanel.name}' (current activeSelf: {_winPanel.activeSelf})");
                
                // Trace parent hierarchy active states
                Transform p = _winPanel.transform.parent;
                while (p != null)
                {
                    Debug.Log($"[GameControlUI] Victory Panel Parent: '{p.name}', activeSelf: {p.gameObject.activeSelf}, activeInHierarchy: {p.gameObject.activeInHierarchy}");
                    p = p.parent;
                }
                
                string sceneName = SceneManager.GetActiveScene().name;
                int buildIndex = SceneManager.GetActiveScene().buildIndex;
                string levelTitle = $"LEVEL {buildIndex}: {sceneName.Replace("_", " ")}".ToUpper();

                var currentLevel = _gameManager.CurrentLevelData;
                if (currentLevel == null) currentLevel = _selectionState?.SelectedLevel;

                var levelDb = MaouSamaTD.Core.AppEntryPoint.LoadedLevelDatabase;
                var nextLevel = (levelDb != null && currentLevel != null) ? levelDb.AllLevels.Find(l => l.LevelIndex == currentLevel.LevelIndex + 1) : null;
                
                // If we don't find it by index+1, try the helper method in Database
                if (nextLevel == null && levelDb != null && currentLevel != null)
                {
                    nextLevel = levelDb.GetNextLevel(currentLevel);
                }

                if (_winNextButton != null)
                {
                    // Resolve LevelIndex (fallback to buildIndex if no data)
                    int levelIdx = currentLevel != null ? currentLevel.LevelIndex : buildIndex;
                    
                    bool isFirstLevel = levelIdx == 1;
                    bool isSecondLevel = levelIdx == 2;
                    
                    if (isFirstLevel) _winNextButton.gameObject.SetActive(true);
                    else if (isSecondLevel) _winNextButton.gameObject.SetActive(false);
                    else _winNextButton.gameObject.SetActive(nextLevel != null);

                    Debug.Log($"[GameControlUI] Next Level Button: {_winNextButton.gameObject.activeSelf} (Level Index: {levelIdx})");
                }

                _winPanel.SetActive(true);
                Debug.Log($"[GameControlUI] Activated _winPanel activeSelf is now: {_winPanel.activeSelf}");

                if (_levelTitleText != null)
                {
                    _levelTitleText.text = currentLevel != null ? currentLevel.LevelName.ToUpper() : levelTitle;
                }

                if (_clearTimeText != null)
                {
                    float time = _gameManager.TimeTaken;
                    int minutes = Mathf.FloorToInt(time / 60F);
                    int seconds = Mathf.FloorToInt(time % 60F);
                    _clearTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
                    Debug.Log($"[GameControlUI] Assigned Clear Time: {_clearTimeText.text} (Raw: {time})");
                }
                else
                {
                    Debug.LogWarning("[GameControlUI] Clear Time Text is NOT assigned in Inspector!");
                }

                PopulateStarConditions();
            }
            else
            {
                Debug.LogError("[GameControlUI] SHOW WIN FAILED: _winPanel is null and could not be resolved dynamically!");
            }
        }

        private void PopulateStarConditions()
        {
            if (_starConditionContainer == null || _starConditionPrefab == null) return;

            // Clear previous items
            foreach (Transform child in _starConditionContainer)
            {
                Destroy(child.gameObject);
            }

            var results = _gameManager.EvaluateStarConditions();
            Sequence starSeq = DOTween.Sequence();
            starSeq.SetUpdate(true); // Ensure it runs even if timeScale is 0

            for (int i = 0; i < results.Count; i++)
            {
                var res = results[i];
                GameObject item = Instantiate(_starConditionPrefab, _starConditionContainer);
                
                Image icon = item.GetComponentInChildren<Image>();
                TextMeshProUGUI text = item.GetComponentInChildren<TextMeshProUGUI>();

                if (text != null) text.text = res.Description;
                
                if (icon != null)
                {
                    icon.sprite = _starEmptySprite; // Start empty
                    icon.transform.localScale = Vector3.zero;

                    if (res.IsAchieved)
                    {
                        float delay = 0.5f + (i * 0.4f);
                        starSeq.InsertCallback(delay, () => {
                            if (icon != null) icon.sprite = _starFullSprite;
                        });
                        starSeq.Insert(delay, icon.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));
                        starSeq.Append(icon.transform.DOScale(1f, 0.1f));
                    }
                    else
                    {
                        // Even if not achieved, show empty star with a small fade or scale
                        float delay = 0.5f + (i * 0.4f);
                        starSeq.Insert(delay, icon.transform.DOScale(1f, 0.3f).SetEase(Ease.OutQuad));
                    }
                }
            }
        }

        private void OnNextLevelClicked()
        {
            Time.timeScale = 1f;
            
            var currentLevel = _gameManager.CurrentLevelData;
            if (currentLevel == null) currentLevel = _selectionState?.SelectedLevel;

            int nextIndex = 1;
            if (currentLevel != null)
            {
                nextIndex = currentLevel.LevelIndex + 1;
            }

            string primaryKey = $"Assets/_Game/Data/Levels/LevelData_Level{nextIndex}.asset";
            string fallbackKey = $"LevelData_Level{nextIndex}";

            Debug.Log($"[GameControlUI] Attempting to load next level via Addressables key: '{primaryKey}'");

            Addressables.LoadAssetAsync<LevelData>(primaryKey).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    LevelData loadedLevel = handle.Result;
                    Debug.Log($"[GameControlUI] Successfully loaded next level '{loadedLevel.LevelName}' via Addressables primary key!");
                    _selectionState.SetLevel(loadedLevel);
                    ReloadScene();
                }
                else
                {
                    Debug.LogWarning($"[GameControlUI] Failed to load next level via '{primaryKey}'. Trying fallback: '{fallbackKey}'...");
                    Addressables.LoadAssetAsync<LevelData>(fallbackKey).Completed += fallbackHandle =>
                    {
                        if (fallbackHandle.Status == AsyncOperationStatus.Succeeded)
                        {
                            LevelData loadedLevel = fallbackHandle.Result;
                            Debug.Log($"[GameControlUI] Successfully loaded next level '{loadedLevel.LevelName}' via Addressables fallback key!");
                            _selectionState.SetLevel(loadedLevel);
                            ReloadScene();
                        }
                        else
                        {
                            Debug.LogError($"[GameControlUI] Failed to load next level via both Addressable keys. Falling back to memory-based LevelDatabase lookup.");
                            
                            var levelDb = MaouSamaTD.Core.AppEntryPoint.LoadedLevelDatabase;
                            if (currentLevel != null && levelDb != null)
                            {
                                var nextLevel = levelDb.AllLevels.Find(l => l.LevelIndex == nextIndex);
                                if (nextLevel == null) nextLevel = levelDb.GetNextLevel(currentLevel);

                                if (nextLevel != null)
                                {
                                    _selectionState.SetLevel(nextLevel);
                                    ReloadScene();
                                    return;
                                }
                            }
                            
                            Debug.Log("[GameControlUI] No next level found, returning to menu.");
                            ReturnToMenu();
                        }
                    };
                }
            };
        }

        private void ShowLose()
        {
            Debug.Log($"[GameControlUI] ShowLose() event triggered! _losePanel is null? {_losePanel == null}");
            if (_uiBlocker != null) _uiBlocker.HideBlocker(true);
            
            if (_losePanel == null)
            {
                Debug.LogError("[GameControlUI] _losePanel is null! Please assign it in the Inspector.");
                return;
            }

            if (_losePanel != null)
            {
                Debug.Log($"[GameControlUI] Activating Defeat Panel '{_losePanel.name}' (current activeSelf: {_losePanel.activeSelf})");
                
                // Trace parent hierarchy active states
                Transform p = _losePanel.transform.parent;
                while (p != null)
                {
                    Debug.Log($"[GameControlUI] Defeat Panel Parent: '{p.name}', activeSelf: {p.gameObject.activeSelf}, activeInHierarchy: {p.gameObject.activeInHierarchy}");
                    p = p.parent;
                }
                _losePanel.SetActive(true);
                Debug.Log($"[GameControlUI] Activated _losePanel activeSelf is now: {_losePanel.activeSelf}");
            }
            else
            {
                Debug.LogError("[GameControlUI] SHOW LOSE FAILED: _losePanel is null and could not be resolved dynamically!");
            }
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
             // Clear all DOTween tweens to prevent memory leaks and exceptions
             DOTween.KillAll();
             // Clean up assets
             Resources.UnloadUnusedAssets();
             // Load Scene 0 (Home/Menu)
             SceneManager.LoadScene(0);
        }

        private void OnCancelRetreat()
        {
            if (_confirmationPanel != null) _confirmationPanel.SetActive(false);
        }

        private void ReloadScene()
        {
            DOTween.KillAll();
            Resources.UnloadUnusedAssets();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        


        private void UpdateHp(int integrity)
        {
            int maxIntegrity = _gameManager != null ? _gameManager.MaxObjectiveHP : 100;
            if (maxIntegrity <= 0) maxIntegrity = 100;

            float pct = (float)integrity / maxIntegrity;
            
            if (_hpFillImage != null)
            {
                _hpFillImage.DOFillAmount(pct, _hpFillDuration).SetUpdate(true);
            }

            // Trigger feedback if damage taken (and not just initialization)
            if (_lastHp != -1 && integrity < _lastHp)
            {
                TriggerDamageFeedback();
            }
            _lastHp = integrity;

            if (_hpText != null)
            {
                string protectedName = "Sovereign";
                if (_gameManager != null && _gameManager.CurrentLevelData != null)
                {
                    string key = _gameManager.CurrentLevelData.SovereignHpNameKey;
                    
                    // Level 2 (Tomb of Lilith): dynamically transition from "Tina" (SovereignHP_Level2)
                    // to "Sovereign" (SovereignHP_Default) once Lilith is unsealed (active in hierarchy)
                    if (_gameManager.CurrentLevelData.LevelID == "1-2")
                    {
                        bool lilithUnsealed = false;
                        var buttons = FindObjectsByType<MaouSamaTD.UI.UnitButtonUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                        foreach (var btn in buttons)
                        {
                            if (btn != null && btn.Data != null && btn.Data.UnitName == "Lilith")
                            {
                                if (btn.gameObject.activeInHierarchy)
                                {
                                    lilithUnsealed = true;
                                    break;
                                }
                            }
                        }

                        if (!lilithUnsealed)
                        {
                            key = "SovereignHP_Level2"; // Tina
                        }
                        else
                        {
                            key = "SovereignHP_Default"; // Sovereign
                        }
                    }

                    if (!string.IsNullOrEmpty(key))
                    {
                        if (Assets.SimpleLocalization.Scripts.LocalizationManager.HasKey(key))
                        {
                            protectedName = Assets.SimpleLocalization.Scripts.LocalizationManager.Localize(key);
                        }
                        else
                        {
                            protectedName = key;
                        }
                    }
                }
                
                // Format for objective hp name is just Name (e.g., Sovereign, Tina, Wagon etc etc)
                _hpText.text = protectedName;
            }

            if (_hpPercentageText != null)
            {
                _hpPercentageText.text = $"{Mathf.CeilToInt(pct * 100)}%";
            }
        }

        private void TriggerDamageFeedback()
        {
            // Camera Shake
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.DOComplete();
                mainCam.transform.DOShakePosition(_cameraShakeDuration, _cameraShakeIntensity).SetUpdate(true);
            }

            // Slow Motion (Hit-Stop effect)
            if (_gameManager != null && !_gameManager.IsPaused && !_gameManager.IsGameEnded)
            {
                float currentBaseSpeed = _gameManager.CurrentSpeed;
                Time.timeScale = _damageSlowMoScale;

                // Use DOTween DelayedCall to restore time scale
                DOVirtual.DelayedCall(_damageSlowMoDuration, () =>
                {
                    if (_gameManager != null && !_gameManager.IsPaused && !_gameManager.IsGameEnded)
                    {
                        Time.timeScale = _gameManager.CurrentSpeed;
                    }
                }, false).SetUpdate(true);
            }

            if (_showDebugLogs) Debug.Log("[GameControlUI] Damage feedback triggered (Slow-mo + Shake)");
        }

        private void OnSpeedClicked()
        {
            if (_gameManager == null) return;
            // Speed toggle is allowed during tutorial to prevent soft-locks/frustration.
            // SetSpeed(newSpeed) will clear the IsTutorialTimeStop state in GameManager.
            
            // Cycle: 1x -> 2x -> 0x -> 1x
            float newSpeed = 1f;
            if (_gameManager.CurrentSpeed >= 1f && _gameManager.CurrentSpeed < 2f) newSpeed = 2f;
            else if (_gameManager.CurrentSpeed >= 2f) newSpeed = 0f;
            else newSpeed = 1f;

            _gameManager.SetSpeed(newSpeed);
            UpdateUI();
        }


        private void OnPauseClicked()
        {
            if (_gameManager == null) return;
            // Pause is always allowed — player must be able to exit/retreat even during tutorial
            _gameManager.TogglePause();
            UpdateUI(); // Toggles overlay via IsPaused check
        }

        private void UpdateUI()
        {
            if (_gameManager == null) return;

            // Speed Text
            if (_speedText != null)
            {
                if (_gameManager.IsPaused)
                {
                    _speedText.text = "";
                }
                else
                {
                    _speedText.text = $"{_gameManager.CurrentSpeed}x";
                }
            }

            // Wave Text
            if (_waveText != null && _enemyManager != null)
            {
                // Display current wave (1-indexed)
                int current = _enemyManager.CurrentWaveIndex + 1;
                int total = _enemyManager.TotalWaves;
                _waveText.text = $"Wave: {current}/{total}";
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

            // Tactical Indicator
            if (_tacticalIndicator != null)
            {
                // Show only if speed is 0 but NOT paused
                _tacticalIndicator.SetActive(_gameManager.CurrentSpeed <= 0f && !_gameManager.IsPaused && !_gameManager.IsGameEnded);
            }
        }

        [Header("Tutorial Skip")]
        [SerializeField] private GameObject _tutorialSkipPrefab;

        public void ShowTutorialPrompt(System.Action onYes, System.Action onNo)
        {
            if (_tutorialSkipPrefab == null)
            {
                Debug.LogWarning("[GameControlUI] _tutorialSkipPrefab is not assigned! Tutorial skip prompt cannot be shown.");
                onYes?.Invoke(); // Fallback to playing tutorial
                return;
            }

            // Find MainCanvas to ensure UI renders correctly
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            
            Transform parent = canvas != null ? canvas.transform : this.transform;
            GameObject popup = Instantiate(_tutorialSkipPrefab, parent);
            popup.SetActive(true);
            popup.transform.SetAsLastSibling();


            // Find Buttons in children
            Button yesBtn = null;
            Button noBtn = null;
            
            Button[] buttons = popup.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (b.name.Contains("Tutorial")) yesBtn = b;
                else if (b.name.Contains("Myself")) noBtn = b;
            }

            if (yesBtn == null || noBtn == null)
            {
                Debug.LogError($"[GameControlUI] Could not find all buttons in TutorialSkip_Popup! Yes:{yesBtn!=null}, No:{noBtn!=null}");
                // Fallback: if buttons missing, just invoke onYes to not softlock
                if (yesBtn == null && noBtn == null) { onYes?.Invoke(); Destroy(popup); return; }
            }

            System.Action<System.Action> closePopup = (callback) =>
            {
                popup.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
                {
                    Destroy(popup);
                    callback?.Invoke();
                });
            };

            if (yesBtn != null)
            {
                yesBtn.onClick.AddListener(() =>
                {
                    if (_showDebugLogs) Debug.Log("[GameControlUI] User chose: Play Tutorial");
                    closePopup(onYes);
                });
            }

            if (noBtn != null)
            {
                noBtn.onClick.AddListener(() =>
                {
                    if (_showDebugLogs) Debug.Log("[GameControlUI] User chose: Skip Tutorial");
                    closePopup(onNo);
                });
            }
            
            // Open Animation
            popup.transform.localScale = Vector3.zero;
            popup.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        }

    }
}
