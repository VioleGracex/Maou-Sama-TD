using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;
using MaouSamaTD.Managers;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using MaouSamaTD.Levels;
using Zenject;
using DG.Tweening;
using MaouSamaTD.UI.Tutorial;
using MaouSamaTD.Data;

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

        [Header("Stage Clear Banner")]
        [SerializeField] private RectTransform _stageClearBanner;
        [SerializeField] private float _bannerDuration = 2.5f;
        [Header("Debug")]
        [SerializeField] private bool _showDebugLogs = true;

        [Header("HP Feedback Settings")]
        [SerializeField] private float _damageSlowMoDuration = 0.5f;
        [SerializeField] private float _damageSlowMoScale = 0.5f;
        [SerializeField] private float _cameraShakeIntensity = 0.15f;
        [SerializeField] private float _cameraShakeDuration = 0.25f;
        [SerializeField] private float _hpFillDuration = 0.4f;

        [Header("Loot Drop Animation Settings")]
        [SerializeField] private System.Collections.Generic.List<ItemConfigSO> _lootItemConfigs = new System.Collections.Generic.List<ItemConfigSO>();
        [SerializeField] private Transform _lootDestinationTarget;

        private int _lastHp = -1;

        [Inject] private GameManager _gameManager;
        [Inject] private MaouSamaTD.UI.UIPopupBlocker _uiBlocker;
        [Inject] private GameSelectionState _selectionState;

        [Inject(Optional = true)] private EnemyManager _enemyManager;
        [Inject(Optional = true)] private BattleCurrencyManager _currencyManager;
        [Inject(Optional = true)] private TutorialManager _tutorialManager;
        [Inject(Optional = true)] private SettingsManager _settingsManager;

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
                }

                ShowWinPanel();
            }
            else
            {
                Debug.LogError("[GameControlUI] SHOW WIN FAILED: _winPanel is null and could not be resolved dynamically!");
            }
        }

        public void ShowWinPanel()
        {
            if (_showDebugLogs) Debug.Log("[GameControlUI] Showing Win Panel.");
            
            StartCoroutine(StageClearSequence());
        }

        private IEnumerator StageClearSequence()
        {
            // Close active gameplay panels instantly to avoid post-battle overlaps
            var skillPanel = FindFirstObjectByType<MaouSamaTD.UI.Skills.SkillPanelUI>();
            if (skillPanel != null && skillPanel.IsVisible)
            {
                skillPanel.ToggleVisibility();
            }
            var unitInspector = FindFirstObjectByType<MaouSamaTD.UI.UnitInspectorUI>();
            if (unitInspector != null && unitInspector.IsPanelActive)
            {
                unitInspector.Hide();
            }
            var fullScreenInspector = FindFirstObjectByType<MaouSamaTD.UI.UnitInspectorFullScreenUI>();
            if (fullScreenInspector != null && fullScreenInspector.VisualRoot != null && fullScreenInspector.VisualRoot.activeSelf)
            {
                fullScreenInspector.Close();
            }

            if (_stageClearBanner != null)
            {
                _stageClearBanner.gameObject.SetActive(true);
                
                // Arknights style: slightly tilted and centered
                _stageClearBanner.localRotation = Quaternion.Euler(0, 0, -3.5f);
                
                Vector2 originalPos = _stageClearBanner.anchoredPosition;
                _stageClearBanner.anchoredPosition = new Vector2(0, 1000); // Start off-screen top
                _stageClearBanner.localScale = Vector3.one * 0.8f; // Start slightly smaller for "pop"

                // Apply premium shadow styling to children texts
                var texts = _stageClearBanner.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var t in texts)
                {
                    Shadow shadow = t.GetComponent<Shadow>();
                    if (shadow == null) shadow = t.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0, 0, 0, 0.7f);
                    shadow.effectDistance = new Vector2(8f, -8f);
                }
                
                // Animate to center with impact
                _stageClearBanner.DOAnchorPos(Vector2.zero, 0.8f).SetUpdate(true).SetEase(Ease.OutBack);
                _stageClearBanner.DOScale(1f, 0.8f).SetUpdate(true).SetEase(Ease.OutBack);

                yield return new WaitForSecondsRealtime(_bannerDuration);
                
                // Exit animation: Slide down off-screen
                _stageClearBanner.DOAnchorPosY(-1000, 0.7f).SetUpdate(true).SetEase(Ease.InBack);
                yield return new WaitForSecondsRealtime(0.7f);
                
                _stageClearBanner.gameObject.SetActive(false);
                _stageClearBanner.anchoredPosition = originalPos;
                _stageClearBanner.localRotation = Quaternion.identity;
            }

            // --- STAGE 2: XP PROGRESS SEQUENCE ---
            EnsureVictorySequenceUI();
            
            if (_xpSequencePanel != null)
            {
                _xpSequencePanel.SetActive(true);
                PopulateXPGrid();
                
                _victorySequenceTapped = false;
                yield return new WaitUntil(() => _victorySequenceTapped);
                _xpSequencePanel.SetActive(false);
            }

            // --- STAGE 3: LOOT AND MVP SEQUENCE ---
            if (_lootSequencePanel != null)
            {
                _lootSequencePanel.SetActive(true);
                PopulateLootAndMVP();
                
                _victorySequenceTapped = false;
                yield return new WaitUntil(() => _victorySequenceTapped);
                _lootSequencePanel.SetActive(false);
            }

            // --- STAGE 4: FINAL VICTORY PANEL ---
            if (_winPanel != null)
            {
                _winPanel.SetActive(true);
                _winPanel.transform.localScale = Vector3.zero;
                _winPanel.transform.DOScale(1f, 0.5f).SetUpdate(true).SetEase(Ease.OutBack);
                
                // Now that the win panel is up, we can finally stop time
                _gameManager.SetSpeed(0f);
            }
            
            if (_levelTitleText != null && _gameManager.CurrentLevelData != null)
                _levelTitleText.text = _gameManager.CurrentLevelData.LevelName;

            if (_clearTimeText != null)
            {
                float time = _gameManager.TimeTaken;
                int minutes = Mathf.FloorToInt(time / 60);
                int seconds = Mathf.FloorToInt(time % 60);
                _clearTimeText.text = $"Clear Time: {minutes:00}:{seconds:00}";
            }

            // Populate Star Conditions
            PopulateStarConditions();
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


        public Button PauseButton => _pauseButton;
        public Button SpeedButton => _speedButton;

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
            GameObject canvasGo = GameObject.FindWithTag("MainCanvas");
            Canvas canvas = canvasGo != null ? canvasGo.GetComponent<Canvas>() : FindFirstObjectByType<Canvas>();
            
            Transform parent = canvas != null ? canvas.transform : this.transform;
            GameObject popup = Instantiate(_tutorialSkipPrefab, parent);
            popup.SetActive(true);
            popup.transform.SetAsLastSibling();

            // Safety: Ensure it hasn't collapsed to 0 width/height
            RectTransform popupRT = popup.GetComponent<RectTransform>();
            if (popupRT != null)
            {
                if (popupRT.sizeDelta.x <= 0) popupRT.sizeDelta = new Vector2(500, popupRT.sizeDelta.y);
                if (popupRT.sizeDelta.y <= 0) popupRT.sizeDelta = new Vector2(popupRT.sizeDelta.x, 300);
                popupRT.localScale = Vector3.one;
                popupRT.anchoredPosition = Vector2.zero;
            }


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
                Transform dialogTrans = popup.transform.Find("TutorialSkip_Dialog");
                if (dialogTrans != null)
                {
                    dialogTrans.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
                    {
                        Destroy(popup);
                        callback?.Invoke();
                    });
                }
                else
                {
                    popup.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
                    {
                        Destroy(popup);
                        callback?.Invoke();
                    });
                }
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
            Transform openDialogTrans = popup.transform.Find("TutorialSkip_Dialog");
            if (openDialogTrans != null)
            {
                openDialogTrans.localScale = Vector3.zero;
                openDialogTrans.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
            }
            else
            {
                popup.transform.localScale = Vector3.zero;
                popup.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        #region Victory Sequence Dynamic UI
        
        private GameObject _xpSequencePanel;
        private RectTransform _xpSequenceGrid;
        private GameObject _lootSequencePanel;
        private RectTransform _lootSequenceGrid;
        private UnityEngine.UI.Image _mvpPortrait;
        private TextMeshProUGUI _mvpNameText;
        private bool _victorySequenceTapped = false;

        private void EnsureVictorySequenceUI()
        {
            if (_xpSequencePanel != null && _lootSequencePanel != null) return;

            Transform parentCanvas = _winPanel != null ? _winPanel.transform.parent : transform;

            // 1. Create XP Panel
            _xpSequencePanel = new GameObject("XPSequencePanel");
            _xpSequencePanel.transform.SetParent(parentCanvas, false);
            var xpRect = _xpSequencePanel.AddComponent<RectTransform>();
            xpRect.anchorMin = Vector2.zero; xpRect.anchorMax = Vector2.one;
            xpRect.sizeDelta = Vector2.zero;
            
            var xpBg = _xpSequencePanel.AddComponent<Image>();
            xpBg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

            var xpBtn = _xpSequencePanel.AddComponent<Button>();
            var xpBtnColors = xpBtn.colors;
            xpBtnColors.normalColor = new Color(1, 1, 1, 0); // Transparent
            xpBtnColors.highlightedColor = new Color(1, 1, 1, 0);
            xpBtnColors.pressedColor = new Color(1, 1, 1, 0);
            xpBtn.colors = xpBtnColors;
            xpBtn.onClick.AddListener(() => _victorySequenceTapped = true);

            var xpTitle = new GameObject("Title").AddComponent<TextMeshProUGUI>();
            xpTitle.transform.SetParent(_xpSequencePanel.transform, false);
            xpTitle.text = "COHORT EXPERIENCE";
            xpTitle.fontSize = 60;
            xpTitle.fontStyle = FontStyles.Bold;
            xpTitle.alignment = TextAlignmentOptions.Center;
            xpTitle.color = new Color(1f, 0.8f, 0.2f);
            var xpTitleRect = xpTitle.GetComponent<RectTransform>();
            xpTitleRect.anchorMin = new Vector2(0, 0.85f); xpTitleRect.anchorMax = new Vector2(1, 0.95f);
            xpTitleRect.sizeDelta = Vector2.zero;

            var xpGridObj = new GameObject("Grid");
            xpGridObj.transform.SetParent(_xpSequencePanel.transform, false);
            _xpSequenceGrid = xpGridObj.AddComponent<RectTransform>();
            _xpSequenceGrid.anchorMin = new Vector2(0.1f, 0.15f); _xpSequenceGrid.anchorMax = new Vector2(0.9f, 0.8f);
            _xpSequenceGrid.sizeDelta = Vector2.zero;
            var layout = xpGridObj.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(240, 120);
            layout.spacing = new Vector2(30, 30);
            layout.childAlignment = TextAnchor.MiddleCenter;

            var xpPrompt = new GameObject("Prompt").AddComponent<TextMeshProUGUI>();
            xpPrompt.transform.SetParent(_xpSequencePanel.transform, false);
            xpPrompt.text = "Tap to continue...";
            xpPrompt.fontSize = 30;
            xpPrompt.alignment = TextAlignmentOptions.Center;
            xpPrompt.color = new Color(1, 1, 1, 0.5f);
            var xpPromptRect = xpPrompt.GetComponent<RectTransform>();
            xpPromptRect.anchorMin = new Vector2(0, 0); xpPromptRect.anchorMax = new Vector2(1, 0.1f);
            xpPromptRect.sizeDelta = Vector2.zero;
            xpPrompt.DOFade(1f, 1f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);

            _xpSequencePanel.SetActive(false);

            // 2. Create Loot Panel
            _lootSequencePanel = new GameObject("LootSequencePanel");
            _lootSequencePanel.transform.SetParent(parentCanvas, false);
            var lootRect = _lootSequencePanel.AddComponent<RectTransform>();
            lootRect.anchorMin = Vector2.zero; lootRect.anchorMax = Vector2.one;
            lootRect.sizeDelta = Vector2.zero;

            var lootBg = _lootSequencePanel.AddComponent<Image>();
            lootBg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

            var lootBtn = _lootSequencePanel.AddComponent<Button>();
            lootBtn.colors = xpBtnColors; // Transparent
            lootBtn.onClick.AddListener(() => _victorySequenceTapped = true);

            var mvpObj = new GameObject("MVPPortrait");
            mvpObj.transform.SetParent(_lootSequencePanel.transform, false);
            _mvpPortrait = mvpObj.AddComponent<Image>();
            _mvpPortrait.preserveAspect = true;
            var mvpRect = _mvpPortrait.GetComponent<RectTransform>();
            mvpRect.anchorMin = new Vector2(0.05f, 0.1f); mvpRect.anchorMax = new Vector2(0.45f, 0.9f);
            mvpRect.sizeDelta = Vector2.zero;

            var mvpNameObj = new GameObject("MVPName");
            mvpNameObj.transform.SetParent(mvpObj.transform, false);
            _mvpNameText = mvpNameObj.AddComponent<TextMeshProUGUI>();
            _mvpNameText.fontSize = 50;
            _mvpNameText.fontStyle = FontStyles.Bold;
            _mvpNameText.alignment = TextAlignmentOptions.BottomLeft;
            _mvpNameText.color = new Color(1f, 0.8f, 0.2f);
            var mvpNameRect = _mvpNameText.GetComponent<RectTransform>();
            mvpNameRect.anchorMin = new Vector2(0, 0); mvpNameRect.anchorMax = new Vector2(1, 0.15f);
            mvpNameRect.sizeDelta = Vector2.zero;

            var mvpTag = new GameObject("MVPTag").AddComponent<TextMeshProUGUI>();
            mvpTag.transform.SetParent(mvpObj.transform, false);
            mvpTag.text = "M V P";
            mvpTag.fontSize = 70;
            mvpTag.fontStyle = FontStyles.Bold | FontStyles.Italic;
            mvpTag.alignment = TextAlignmentOptions.TopLeft;
            mvpTag.color = new Color(1f, 0.2f, 0.3f);
            var mvpTagRect = mvpTag.GetComponent<RectTransform>();
            mvpTagRect.anchorMin = new Vector2(0, 0.85f); mvpTagRect.anchorMax = new Vector2(1, 1);
            mvpTagRect.sizeDelta = Vector2.zero;

            var lootGridObj = new GameObject("LootGrid");
            lootGridObj.transform.SetParent(_lootSequencePanel.transform, false);
            _lootSequenceGrid = lootGridObj.AddComponent<RectTransform>();
            _lootSequenceGrid.anchorMin = new Vector2(0.5f, 0.15f); _lootSequenceGrid.anchorMax = new Vector2(0.95f, 0.80f);
            _lootSequenceGrid.sizeDelta = Vector2.zero;
            var lootLayout = lootGridObj.AddComponent<GridLayoutGroup>();
            lootLayout.cellSize = new Vector2(120, 160);
            lootLayout.spacing = new Vector2(20, 20);
            lootLayout.childAlignment = TextAnchor.UpperLeft;

            var lootTitle = new GameObject("LootTitle").AddComponent<TextMeshProUGUI>();
            lootTitle.transform.SetParent(_lootSequencePanel.transform, false);
            lootTitle.text = "BATTLE REWARDS";
            lootTitle.fontSize = 50;
            lootTitle.fontStyle = FontStyles.Bold;
            lootTitle.alignment = TextAlignmentOptions.BottomLeft;
            lootTitle.color = Color.white;
            var lootTitleRect = lootTitle.GetComponent<RectTransform>();
            lootTitleRect.anchorMin = new Vector2(0.5f, 0.85f); lootTitleRect.anchorMax = new Vector2(0.95f, 0.95f);
            lootTitleRect.sizeDelta = Vector2.zero;

            var lootPrompt = new GameObject("Prompt").AddComponent<TextMeshProUGUI>();
            lootPrompt.transform.SetParent(_lootSequencePanel.transform, false);
            lootPrompt.text = "Tap to continue...";
            lootPrompt.fontSize = 30;
            lootPrompt.alignment = TextAlignmentOptions.Center;
            lootPrompt.color = new Color(1, 1, 1, 0.5f);
            var lootPromptRect = lootPrompt.GetComponent<RectTransform>();
            lootPromptRect.anchorMin = new Vector2(0, 0); lootPromptRect.anchorMax = new Vector2(1, 0.1f);
            lootPromptRect.sizeDelta = Vector2.zero;
            lootPrompt.DOFade(1f, 1f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);

            _lootSequencePanel.SetActive(false);
        }

        private void PopulateXPGrid()
        {
            foreach (Transform t in _xpSequenceGrid) Destroy(t.gameObject);

            if (_gameManager.DeployedUnitsXPInfo == null) return;

            foreach (var info in _gameManager.DeployedUnitsXPInfo)
            {
                if (info.Unit == null) continue;

                var cardObj = new GameObject("XPCard");
                cardObj.transform.SetParent(_xpSequenceGrid, false);
                var cardBg = cardObj.AddComponent<Image>();
                cardBg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

                var avatarObj = new GameObject("Avatar");
                avatarObj.transform.SetParent(cardObj.transform, false);
                var avatarImg = avatarObj.AddComponent<Image>();
                avatarImg.sprite = info.Unit.GetSprite(MaouSamaTD.Units.UnitData.UnitImageType.Chibi);
                avatarImg.preserveAspect = true;
                var avatarRect = avatarImg.GetComponent<RectTransform>();
                avatarRect.anchorMin = new Vector2(0.05f, 0.1f); avatarRect.anchorMax = new Vector2(0.35f, 0.9f);
                avatarRect.sizeDelta = Vector2.zero;

                var lvlText = new GameObject("LvlText").AddComponent<TextMeshProUGUI>();
                lvlText.transform.SetParent(cardObj.transform, false);
                lvlText.text = $"Lv {info.OldLevel}";
                lvlText.fontSize = 24;
                lvlText.color = Color.white;
                var lvlRect = lvlText.GetComponent<RectTransform>();
                lvlRect.anchorMin = new Vector2(0.4f, 0.6f); lvlRect.anchorMax = new Vector2(0.95f, 0.9f);
                lvlRect.sizeDelta = Vector2.zero;

                var xpText = new GameObject("XPText").AddComponent<TextMeshProUGUI>();
                xpText.transform.SetParent(cardObj.transform, false);
                xpText.text = $"+{info.XPAwarded} XP";
                xpText.fontSize = 20;
                xpText.color = new Color(0.3f, 1f, 0.5f);
                var xpRect = xpText.GetComponent<RectTransform>();
                xpRect.anchorMin = new Vector2(0.4f, 0.4f); xpRect.anchorMax = new Vector2(0.95f, 0.6f);
                xpRect.sizeDelta = Vector2.zero;

                var sliderBgObj = new GameObject("SliderBg");
                sliderBgObj.transform.SetParent(cardObj.transform, false);
                var sliderBg = sliderBgObj.AddComponent<Image>();
                sliderBg.color = new Color(0, 0, 0, 0.5f);
                var sBgRect = sliderBg.GetComponent<RectTransform>();
                sBgRect.anchorMin = new Vector2(0.4f, 0.2f); sBgRect.anchorMax = new Vector2(0.95f, 0.35f);
                sBgRect.sizeDelta = Vector2.zero;

                var sliderFillObj = new GameObject("SliderFill");
                sliderFillObj.transform.SetParent(sliderBgObj.transform, false);
                var sliderFill = sliderFillObj.AddComponent<Image>();
                sliderFill.color = new Color(0.2f, 0.6f, 1f);
                var sFillRect = sliderFill.GetComponent<RectTransform>();
                sFillRect.anchorMin = new Vector2(0, 0); sFillRect.anchorMax = new Vector2(0, 1);
                sFillRect.pivot = new Vector2(0, 0.5f);
                sFillRect.sizeDelta = Vector2.zero;

                float oldReq = MaouSamaTD.Progression.ProgressionLogic.GetRequiredXP(info.OldLevel);
                float startRatio = info.OldXP / oldReq;
                sFillRect.anchorMax = new Vector2(startRatio, 1);

                Sequence seq = DOTween.Sequence();
                seq.SetDelay(0.5f);
                
                int levelsGained = info.NewLevel - info.OldLevel;
                if (levelsGained > 0)
                {
                    seq.Append(sFillRect.DOAnchorMax(new Vector2(1f, 1f), 0.5f).SetUpdate(true).OnComplete(() => {
                        lvlText.text = $"Lv {info.OldLevel + 1}!";
                        lvlText.color = new Color(1f, 0.8f, 0.2f);
                        lvlText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f).SetUpdate(true);
                        sFillRect.anchorMax = new Vector2(0, 1);
                    }));
                    
                    float newReq = MaouSamaTD.Progression.ProgressionLogic.GetRequiredXP(info.NewLevel);
                    float finalRatio = info.NewXP / newReq;
                    seq.Append(sFillRect.DOAnchorMax(new Vector2(finalRatio, 1f), 0.5f).SetUpdate(true));
                }
                else
                {
                    float newReq = MaouSamaTD.Progression.ProgressionLogic.GetRequiredXP(info.NewLevel);
                    float finalRatio = info.NewXP / newReq;
                    seq.Append(sFillRect.DOAnchorMax(new Vector2(finalRatio, 1f), 0.8f).SetUpdate(true));
                }
            }
        }

        private void PopulateLootAndMVP()
        {
            foreach (Transform t in _lootSequenceGrid) Destroy(t.gameObject);

            var mvp = _gameManager.GetMVPUnit();
            if (mvp != null)
            {
                _mvpPortrait.sprite = mvp.GetSprite(MaouSamaTD.Units.UnitData.UnitImageType.WaistUp);
                if (_mvpPortrait.sprite == null) _mvpPortrait.sprite = mvp.GetSprite(MaouSamaTD.Units.UnitData.UnitImageType.Chibi);
                _mvpNameText.text = mvp.UnitName;
                
                _mvpPortrait.transform.localPosition = new Vector3(-600, 0, 0);
                _mvpPortrait.transform.DOLocalMoveX(0, 0.6f).SetEase(Ease.OutBack).SetUpdate(true);
                _mvpPortrait.color = new Color(1, 1, 1, 0);
                _mvpPortrait.DOFade(1f, 0.6f).SetUpdate(true);
            }
            else
            {
                _mvpPortrait.color = new Color(1, 1, 1, 0);
                _mvpNameText.text = "NO MVP";
            }

            if (_gameManager.SessionLoot != null)
            {
                foreach (var loot in _gameManager.SessionLoot)
                {
                    var cardObj = new GameObject("LootCard");
                    cardObj.transform.SetParent(_lootSequenceGrid, false);
                    var cardBg = cardObj.AddComponent<Image>();
                    cardBg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
                    
                    var iconObj = new GameObject("Icon");
                    iconObj.transform.SetParent(cardObj.transform, false);
                    var iconImg = iconObj.AddComponent<Image>();
                    
                    // Simple placeholder colors/sprites based on ID
                    iconImg.color = loot.ItemID.Contains("gold") ? Color.yellow : 
                                    loot.ItemID.Contains("crest") ? Color.red : 
                                    loot.ItemID.Contains("xp_core") ? new Color(0.3f, 1f, 0.5f) :
                                    loot.ItemID.Contains("mat") ? new Color(0.7f, 0.3f, 1f) : Color.cyan;
                                    
                    var iconRect = iconImg.GetComponent<RectTransform>();
                    iconRect.anchorMin = new Vector2(0.2f, 0.3f); iconRect.anchorMax = new Vector2(0.8f, 0.9f);
                    iconRect.sizeDelta = Vector2.zero;
                    
                    var nameText = new GameObject("NameText").AddComponent<TextMeshProUGUI>();
                    nameText.transform.SetParent(cardObj.transform, false);
                    nameText.text = loot.ItemID.Replace("xp_core_", "").Replace("mat_", "").Replace("_", " ").ToUpper();
                    nameText.fontSize = 14;
                    nameText.alignment = TextAlignmentOptions.Center;
                    var nameRect = nameText.GetComponent<RectTransform>();
                    nameRect.anchorMin = new Vector2(0, 0.15f); nameRect.anchorMax = new Vector2(1, 0.3f);
                    nameRect.sizeDelta = Vector2.zero;

                    var qtyText = new GameObject("QtyText").AddComponent<TextMeshProUGUI>();
                    qtyText.transform.SetParent(cardObj.transform, false);
                    qtyText.text = $"x{loot.Quantity}";
                    qtyText.fontSize = 20;
                    qtyText.fontStyle = FontStyles.Bold;
                    qtyText.alignment = TextAlignmentOptions.Center;
                    var qtyRect = qtyText.GetComponent<RectTransform>();
                    qtyRect.anchorMin = new Vector2(0, 0); qtyRect.anchorMax = new Vector2(1, 0.15f);
                    qtyRect.sizeDelta = Vector2.zero;

                    cardObj.transform.localScale = Vector3.zero;
                    cardObj.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetDelay(Random.Range(0f, 0.3f)).SetUpdate(true);
                }
            }
        }
        #endregion

        #region Loot Flying Effects
        public void SpawnLootFlyEffect(string itemID, int quantity, Vector3 worldPosition)
        {
            if (_settingsManager != null && _settingsManager.DisableLootAnimation)
                return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 screenPos = cam.WorldToScreenPoint(worldPosition);

            // Create Canvas GameObject for procedural premium visual
            GameObject effectObj = new GameObject("ProceduralLootEffect", typeof(RectTransform));
            effectObj.transform.SetParent(this.transform, false);

            var rectTransform = effectObj.GetComponent<RectTransform>();
            rectTransform.position = screenPos;
            rectTransform.sizeDelta = new Vector2(80, 80);

            var canvasGroup = effectObj.AddComponent<CanvasGroup>();

            // 1. Glowing outer glassmorphic container
            var bgObj = new GameObject("BgGlow", typeof(RectTransform));
            bgObj.transform.SetParent(effectObj.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);

            var outline = bgObj.AddComponent<Outline>();
            Color glowColor = new Color(0.95f, 0.75f, 0.3f, 1f); // Gold Amber
            string itemName = "Loot";

            if (itemID == "gold_coins")
            {
                glowColor = new Color(1f, 0.85f, 0f, 1f);
                itemName = "Gold";
            }
            else if (itemID == "blood_crests")
            {
                glowColor = new Color(0.85f, 0.08f, 0.23f, 1f);
                itemName = "Blood Crest";
            }
            else if (itemID.Contains("common"))
            {
                glowColor = new Color(0.2f, 0.85f, 0.3f, 1f);
                itemName = "Common Core";
            }
            else if (itemID.Contains("rare"))
            {
                glowColor = new Color(0.1f, 0.6f, 1f, 1f);
                itemName = "Rare Core";
            }
            else if (itemID.Contains("epic"))
            {
                glowColor = new Color(0.68f, 0.25f, 0.95f, 1f);
                itemName = "Epic Core";
            }
            else if (itemID.Contains("legendary"))
            {
                glowColor = new Color(1f, 0.55f, 0f, 1f);
                itemName = "Legendary Core";
            }
            else if (itemID.Contains("shadow_essence"))
            {
                glowColor = new Color(0.5f, 0f, 0.5f, 1f);
                itemName = "Shadow Essence";
            }
            else if (itemID.Contains("bandit_insignia"))
            {
                glowColor = new Color(0.7f, 0.45f, 0.25f, 1f);
                itemName = "Bandit Insignia";
            }
            else if (itemID.Contains("animal_fang"))
            {
                glowColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                itemName = "Animal Fang";
            }
            else if (itemID.Contains("golem_core"))
            {
                glowColor = new Color(0f, 0.85f, 0.85f, 1f);
                itemName = "Golem Core";
            }

            outline.effectColor = glowColor;
            outline.effectDistance = new Vector2(2, 2);

            // 2. Icon visual inside
            var innerIconObj = new GameObject("Icon", typeof(RectTransform));
            innerIconObj.transform.SetParent(effectObj.transform, false);
            var iconRect = innerIconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.25f, 0.35f);
            iconRect.anchorMax = new Vector2(0.75f, 0.85f);
            iconRect.sizeDelta = Vector2.zero;

            var iconImage = innerIconObj.AddComponent<Image>();
            iconImage.color = glowColor;

            Sprite matchedSprite = null;
            if (_lootItemConfigs != null)
            {
                var found = _lootItemConfigs.Find(c => c != null && c.name == itemID);
                if (found != null && found.ItemIcon != null)
                {
                    matchedSprite = found.ItemIcon;
                }
            }

            if (matchedSprite != null)
            {
                iconImage.sprite = matchedSprite;
                iconImage.color = Color.white;
            }
            else
            {
                // Procedural rotated diamond
                innerIconObj.transform.localRotation = Quaternion.Euler(0, 0, 45);
            }

            // 3. Text Label at bottom showing qty
            var textObj = new GameObject("QtyText", typeof(RectTransform));
            textObj.transform.SetParent(effectObj.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.05f);
            textRect.anchorMax = new Vector2(0.95f, 0.35f);
            textRect.sizeDelta = Vector2.zero;

            var textLabel = textObj.AddComponent<TextMeshProUGUI>();
            textLabel.text = $"+{quantity}";
            textLabel.fontSize = 16;
            textLabel.fontStyle = FontStyles.Bold;
            textLabel.alignment = TextAlignmentOptions.Center;
            textLabel.color = Color.white;
            textLabel.outlineColor = Color.black;
            textLabel.outlineWidth = 0.2f;

            // 4. Physical pop & bounce animation onto tile
            float randX = Random.Range(-60f, 60f);
            float randY = Random.Range(-30f, 30f);
            Vector3 popPos = effectObj.transform.localPosition + new Vector3(randX, randY + 60f, 0f);
            Vector3 bouncePos = effectObj.transform.localPosition + new Vector3(randX, randY - 15f, 0f);

            effectObj.transform.localScale = Vector3.zero;

            Sequence lootSeq = DOTween.Sequence();
            lootSeq.SetUpdate(true); // Ensure it runs even when timescale is manipulated
            
            // Spawn pop-up
            lootSeq.Append(effectObj.transform.DOLocalMove(popPos, 0.22f).SetEase(Ease.OutQuad));
            lootSeq.Join(effectObj.transform.DOScale(1.2f, 0.22f).SetEase(Ease.OutBack));

            // Drop bounce down
            lootSeq.Append(effectObj.transform.DOLocalMove(bouncePos, 0.2f).SetEase(Ease.InQuad));
            lootSeq.Join(effectObj.transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutBounce));

            // Hover on tile
            lootSeq.AppendInterval(0.35f);

            // Fly away looted!
            Transform destination = _lootDestinationTarget;
            if (destination == null && _waveText != null)
                destination = _waveText.transform;

            Vector3 destLocal;
            if (destination != null)
            {
                destLocal = this.transform.InverseTransformPoint(destination.position);
            }
            else
            {
                destLocal = new Vector3(Screen.width * 0.45f, Screen.height * 0.45f, 0f);
            }

            lootSeq.Append(effectObj.transform.DOLocalMove(destLocal, 0.75f).SetEase(Ease.InBack));
            lootSeq.Join(effectObj.transform.DOScale(0.25f, 0.75f).SetEase(Ease.InQuad));
            lootSeq.Join(canvasGroup.DOFade(0.1f, 0.75f));

            // Arrived trigger burst
            lootSeq.OnComplete(() =>
            {
                if (destination != null)
                {
                    destination.DOPunchScale(new Vector3(1.15f, 1.15f, 1.15f), 0.15f, 5, 0.5f);
                }

                // Satisfying star scatter particles
                for (int i = 0; i < 6; i++)
                {
                    GameObject pStar = new GameObject("BurstStar", typeof(RectTransform));
                    pStar.transform.SetParent(this.transform, false);
                    var pRect = pStar.GetComponent<RectTransform>();
                    pRect.localPosition = destLocal;
                    pRect.sizeDelta = new Vector2(10, 10);
                    pRect.localRotation = Quaternion.Euler(0, 0, 45);

                    var pImg = pStar.AddComponent<Image>();
                    pImg.color = glowColor;

                    var pGroup = pStar.AddComponent<CanvasGroup>();

                    float angle = i * 60f * Mathf.Deg2Rad;
                    Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                    float dist = Random.Range(25f, 50f);
                    Vector3 targetPos = destLocal + direction * dist;

                    pStar.transform.DOLocalMove(targetPos, 0.35f).SetEase(Ease.OutQuad);
                    pStar.transform.DOScale(0f, 0.35f).SetEase(Ease.InQuad);
                    pGroup.DOFade(0f, 0.35f).OnComplete(() => Destroy(pStar));
                }

                Destroy(effectObj);
            });
        }
        #endregion

    }
}
