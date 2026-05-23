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
        [SerializeField] private Transform _lootDestinationTarget;

        [Header("Victory Sequence UI - XP")]
        [SerializeField] private GameObject _xpSequenceVisualRoot;
        [SerializeField] private RectTransform _xpSequenceGrid;
        [SerializeField] private Button _xpBackgroundButton;
        [SerializeField] private TextMeshProUGUI _xpPromptText;
        [SerializeField] private GameObject _xpCardPrefab;

        [Header("Victory Sequence UI - Loot")]
        [SerializeField] private GameObject _lootSequenceVisualRoot;
        [SerializeField] private RectTransform _lootSequenceGrid;
        [SerializeField] private Button _lootBackgroundButton;
        [SerializeField] private TextMeshProUGUI _lootPromptText;
        [SerializeField] private Image _mvpPortrait;
        [SerializeField] private TextMeshProUGUI _mvpNameText;
        [SerializeField] private GameObject _lootCardPrefab;

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

            // Hide panels initially
            if (_lootSequenceVisualRoot != null) _lootSequenceVisualRoot.SetActive(false);
            if (_xpSequenceVisualRoot != null) _xpSequenceVisualRoot.SetActive(false);
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
                int remaining = _enemyManager.GetTotalSpawnedInWave(_enemyManager.CurrentWaveIndex);
                int total = _enemyManager.CurrentWaveTotalEnemies;

                // For normal levels, show current wave + active enemies
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
            DialogueUI dialogueUI = FindFirstObjectByType<DialogueUI>();
            if (_baseHPContainer != null)
            {
                bool showBaseHP = dialogueUI == null || !dialogueUI.IsShowingMiniDialogue;
                if (_baseHPContainer.activeSelf != showBaseHP)
                {
                    _baseHPContainer.SetActive(showBaseHP);
                }
            }

            // Disable speed and pause controls during dialogue and active tutorials only when the UI blocker is active
            bool isDialogueActive = dialogueUI != null && (dialogueUI.IsShowingDialogue || dialogueUI.IsShowingMiniDialogue);
            bool isTutorialActive = _tutorialManager != null && _tutorialManager.IsInTutorial && _uiBlocker != null && _uiBlocker.IsActive;
            bool disableControls = isDialogueActive || isTutorialActive;

            if (_speedButton != null)
                _speedButton.interactable = !disableControls;

            if (_pauseButton != null)
                _pauseButton.interactable = !disableControls;
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
            // Block ALL input immediately — prevents clicking SpeedButton or anything through the overlay.
            if (_uiBlocker != null) _uiBlocker.ShowFullBlocker();

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

            // Create Blur/Dark Overlay
            GameObject blurOverlay = new GameObject("VictoryBlurOverlay");
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) blurOverlay.transform.SetParent(canvas.transform, false);
            blurOverlay.transform.SetAsLastSibling();
            var blurRect = blurOverlay.AddComponent<RectTransform>();
            blurRect.anchorMin = Vector2.zero;
            blurRect.anchorMax = Vector2.one;
            blurRect.sizeDelta = Vector2.zero;
            var blurImg = blurOverlay.AddComponent<Image>();
            blurImg.color = new Color(0, 0, 0, 0);
            blurImg.DOFade(0.85f, 0.5f).SetUpdate(true);

            if (_stageClearBanner != null)
            {
                _stageClearBanner.transform.SetAsLastSibling();
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

            // Hide UI blocker so it doesn't double-dim and block input for XP/Loot sequences
            if (_uiBlocker != null)
            {
                _uiBlocker.HideBlocker(true);
            }

            // --- STAGE 2: XP PROGRESS SEQUENCE ---
            EnsureVictorySequenceUI();
            
            // Wait for all world drop animations to finish
            while (FindObjectsByType<MaouSamaTD.VFX.WorldLootDropVisual>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length > 0)
            {
                yield return null;
            }

            if (_xpSequenceVisualRoot != null)
            {
                _xpSequenceVisualRoot.SetActive(true);
                PopulateXPGrid();
                
                _victorySequenceTapped = false;
                while (!_victorySequenceTapped && 
                       !(UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame) &&
                       !(UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame))
                {
                    yield return null;
                }
                _xpSequenceVisualRoot.SetActive(false);
            }

            // Optional delay between panels
            yield return new WaitForSecondsRealtime(0.5f);

            // Show Loot & MVP Panel
            if (_lootSequenceVisualRoot != null)
            {
                _lootSequenceVisualRoot.SetActive(true);
                PopulateLootAndMVP();
                
                _victorySequenceTapped = false;
                while (!_victorySequenceTapped && 
                       !(UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame) &&
                       !(UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame))
                {
                    yield return null;
                }
                _lootSequenceVisualRoot.SetActive(false);
            }

            // --- STAGE 4: FINAL VICTORY PANEL ---
            if (_winPanel != null)
            {
                _winPanel.SetActive(true);
                _winPanel.transform.SetAsLastSibling();
                
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
            // Block all input immediately on click — prevents double-taps and clicking
            // through during the Addressables load delay before the loading screen appears.
            if (_uiBlocker != null) _uiBlocker.ShowFullBlocker();

            Time.timeScale = 1f;
            
            var currentLevel = _gameManager.CurrentLevelData;
            if (currentLevel == null) currentLevel = _selectionState?.SelectedLevel;

            int nextIndex = 1;
            if (currentLevel != null)
            {
                nextIndex = currentLevel.LevelIndex + 1;
            }

            // PRIORITY 1: Use Memory-Based LevelDatabase Lookup
            var levelDb = MaouSamaTD.Core.AppEntryPoint.LoadedLevelDatabase;
            if (currentLevel != null && levelDb != null)
            {
                var nextLevel = levelDb.AllLevels.Find(l => l.LevelIndex == nextIndex);
                if (nextLevel == null) nextLevel = levelDb.GetNextLevel(currentLevel);

                if (nextLevel != null)
                {
                    Debug.Log($"[GameControlUI] Successfully found next level '{nextLevel.LevelName}' in LevelDatabase!");
                    _selectionState.SetLevel(nextLevel);
                    ReloadScene();
                    return;
                }
            }

            // PRIORITY 2: Fallback to Addressables if not found in DB
            string primaryKey = $"Assets/_Game/Data/Levels/LevelData_Level{nextIndex}.asset";
            string fallbackKey = $"LevelData_Level{nextIndex}";

            Debug.Log($"[GameControlUI] Attempting to load next level via Addressables key: '{primaryKey}'");

            try
            {
                Addressables.LoadAssetAsync<LevelData>(primaryKey).Completed += handle =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
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
                            if (fallbackHandle.Status == AsyncOperationStatus.Succeeded && fallbackHandle.Result != null)
                            {
                                LevelData loadedLevel = fallbackHandle.Result;
                                Debug.Log($"[GameControlUI] Successfully loaded next level '{loadedLevel.LevelName}' via Addressables fallback key!");
                                _selectionState.SetLevel(loadedLevel);
                                ReloadScene();
                            }
                            else
                            {
                                Debug.LogError($"[GameControlUI] Failed to load next level via both Addressable keys. Returning to menu.");
                                ReturnToMenu();
                            }
                        };
                    }
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameControlUI] Addressables Exception: {ex.Message}. Returning to menu.");
                ReturnToMenu();
            }
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
             
             // Try to use loading screen transition
             var loader = FindFirstObjectByType<MaouSamaTD.UI.MainMenu.LoadingScreenPanel>(FindObjectsInactive.Include);
             if (loader != null)
             {
                 loader.LoadSceneTransition("Home_New");
             }
             else
             {
                 // Load Scene 0 (Home_New)
                 SceneManager.LoadScene(0);
             }
        }

        private void OnCancelRetreat()
        {
            if (_confirmationPanel != null) _confirmationPanel.SetActive(false);
        }

        private void ReloadScene()
        {
            DOTween.KillAll();
            Resources.UnloadUnusedAssets();
            
            var loader = FindFirstObjectByType<MaouSamaTD.UI.MainMenu.LoadingScreenPanel>(FindObjectsInactive.Include);
            if (loader != null)
            {
                loader.LoadSceneTransition(SceneManager.GetActiveScene().name);
            }
            else
            {
                string primaryKey = "Assets/_Game/Prefabs/UI/Common/LoadingScreen_Root.prefab";
                Addressables.InstantiateAsync(primaryKey).Completed += handle =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        var instantiatedLoader = handle.Result.GetComponent<MaouSamaTD.UI.MainMenu.LoadingScreenPanel>();
                        if (instantiatedLoader != null)
                        {
                            instantiatedLoader.LoadSceneTransition(SceneManager.GetActiveScene().name);
                        }
                        else
                        {
                            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                        }
                    }
                    else
                    {
                        Addressables.InstantiateAsync("LoadingScreen_Root").Completed += fallbackHandle =>
                        {
                            if (fallbackHandle.Status == AsyncOperationStatus.Succeeded)
                            {
                                var instantiatedLoader = fallbackHandle.Result.GetComponent<MaouSamaTD.UI.MainMenu.LoadingScreenPanel>();
                                if (instantiatedLoader != null)
                                {
                                    instantiatedLoader.LoadSceneTransition(SceneManager.GetActiveScene().name);
                                }
                                else
                                {
                                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                                }
                            }
                            else
                            {
                                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                            }
                        };
                    }
                };
            }
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
                int totalWaves = _enemyManager.TotalWaves;
                int remaining = _enemyManager.GetTotalSpawnedInWave(_enemyManager.CurrentWaveIndex);
                int totalEnemies = _enemyManager.CurrentWaveTotalEnemies;
                
                if (_enemyCountText != null)
                {
                    _waveText.text = $"Wave: {current} / {totalWaves}";
                    _enemyCountText.text = $"({remaining}/{totalEnemies})";
                }
                else
                {
                    _waveText.text = $"Wave: {current} / {totalWaves} ({remaining}/{totalEnemies})";
                }
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
        
        [Header("Victory Sequence")]
        private bool _victorySequenceTapped = false;

        private void EnsureVictorySequenceUI()
        {
            // Setup Background Buttons
            if (_xpBackgroundButton != null)
            {
                _xpBackgroundButton.onClick.RemoveAllListeners();
                _xpBackgroundButton.onClick.AddListener(() => _victorySequenceTapped = true);
            }
            if (_lootBackgroundButton != null)
            {
                _lootBackgroundButton.onClick.RemoveAllListeners();
                _lootBackgroundButton.onClick.AddListener(() => _victorySequenceTapped = true);
            }

            // Setup Prompts
            if (_xpPromptText != null)
            {
                _xpPromptText.DOFade(1f, 1f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            }
            if (_lootPromptText != null)
            {
                _lootPromptText.DOFade(1f, 1f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            }
        }

        private void PopulateXPGrid()
        {
            if (_xpSequenceGrid == null) return;
            foreach (Transform t in _xpSequenceGrid) Destroy(t.gameObject);

            if (_gameManager.DeployedUnitsXPInfo == null) return;

            foreach (var info in _gameManager.DeployedUnitsXPInfo)
            {
                if (info.Unit == null) continue;

                GameObject cardObj;
                if (_xpCardPrefab != null)
                {
                    cardObj = Instantiate(_xpCardPrefab, _xpSequenceGrid, false);
                }
                else
                {
                    // Fallback: create procedurally if prefab not assigned
                    cardObj = new GameObject("XPCard");
                    cardObj.transform.SetParent(_xpSequenceGrid, false);
                    var fallbackBg = cardObj.AddComponent<Image>();
                    fallbackBg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
                }

                // Find child components by name
                var avatarImg = FindChildImage(cardObj, "Avatar");
                var lvlText = FindChildTMP(cardObj, "LevelText");
                var xpText = FindChildTMP(cardObj, "XPText");
                var sliderFillRect = FindChildRect(cardObj, "SliderFill");
                var xpRatioText = FindChildTMP(cardObj, "XPRatioText");

                // Set Avatar (use Avatar image type, fallback to Chibi)
                if (avatarImg != null)
                {
                    var avatarSprite = info.Unit.GetSprite(MaouSamaTD.Units.UnitData.UnitImageType.Avatar);
                    if (avatarSprite == null) avatarSprite = info.Unit.GetSprite(MaouSamaTD.Units.UnitData.UnitImageType.Chibi);
                    avatarImg.sprite = avatarSprite;
                }

                // Set Level
                if (lvlText != null)
                    lvlText.text = $"Lv {info.OldLevel}";

                // Set XP gained
                if (xpText != null)
                    xpText.text = $"+{info.XPAwarded} XP";

                // Calculate XP ratios
                float oldReq = MaouSamaTD.Progression.ProgressionLogic.GetRequiredXP(info.OldLevel);
                float startRatio = oldReq > 0 ? info.OldXP / oldReq : 0f;

                // Set initial slider fill
                if (sliderFillRect != null)
                {
                    sliderFillRect.anchorMax = new Vector2(startRatio, 1);
                }

                // Set XP ratio text
                if (xpRatioText != null)
                    xpRatioText.text = $"{info.OldXP:0}/{oldReq:0}";

                // Animate the XP bar
                if (sliderFillRect != null)
                {
                    Sequence seq = DOTween.Sequence();
                    seq.SetDelay(0.5f);
                    seq.SetUpdate(true);
                    
                    int levelsGained = info.NewLevel - info.OldLevel;
                    if (levelsGained > 0)
                    {
                        // Capture for closure
                        var capturedLvl = lvlText;
                        var capturedFill = sliderFillRect;
                        var capturedRatio = xpRatioText;
                        int oldLv = info.OldLevel;
                        int newLv = info.NewLevel;
                        float newXP = info.NewXP;

                        seq.Append(capturedFill.DOAnchorMax(new Vector2(1f, 1f), 0.5f).SetUpdate(true).OnComplete(() => {
                            if (capturedLvl != null)
                            {
                                capturedLvl.text = $"Lv {oldLv + 1}";
                                capturedLvl.color = new Color(1f, 0.8f, 0.2f);
                                capturedLvl.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f).SetUpdate(true);
                            }
                            capturedFill.anchorMax = new Vector2(0, 1);
                        }));
                        
                        float newReq = MaouSamaTD.Progression.ProgressionLogic.GetRequiredXP(newLv);
                        float finalRatio = newReq > 0 ? newXP / newReq : 0f;
                        seq.Append(capturedFill.DOAnchorMax(new Vector2(finalRatio, 1f), 0.5f).SetUpdate(true).OnComplete(() => {
                            if (capturedRatio != null) capturedRatio.text = $"{newXP:0}/{newReq:0}";
                        }));
                    }
                    else
                    {
                        float newReq = MaouSamaTD.Progression.ProgressionLogic.GetRequiredXP(info.NewLevel);
                        float finalRatio = newReq > 0 ? info.NewXP / newReq : 0f;
                        var capturedRatio = xpRatioText;
                        float newXP = info.NewXP;
                        seq.Append(sliderFillRect.DOAnchorMax(new Vector2(finalRatio, 1f), 0.8f).SetUpdate(true).OnComplete(() => {
                            if (capturedRatio != null) capturedRatio.text = $"{newXP:0}/{newReq:0}";
                        }));
                    }
                }
            }
        }

        private void PopulateLootAndMVP()
        {
            var mvp = _gameManager.GetMVPUnit();
            if (mvp != null)
            {
                if (_mvpNameText != null)
                {
                    _mvpNameText.text = mvp.UnitName;
                    
                    // Slide in the name text and its background
                    Transform nameTransformToAnimate = _mvpNameText.transform;
                    if (_mvpNameText.transform.parent != null && _mvpNameText.transform.parent.name.Contains("BG"))
                    {
                        nameTransformToAnimate = _mvpNameText.transform.parent;
                    }

                    var rect = nameTransformToAnimate.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        float origX = rect.anchoredPosition.x;
                        rect.anchoredPosition = new Vector2(origX - 400, rect.anchoredPosition.y);
                        rect.DOAnchorPosX(origX, 0.6f).SetEase(Ease.OutBack).SetDelay(0.2f).SetUpdate(true);
                    }
                    
                    _mvpNameText.color = new Color(_mvpNameText.color.r, _mvpNameText.color.g, _mvpNameText.color.b, 0);
                    _mvpNameText.DOFade(1f, 0.6f).SetDelay(0.2f).SetUpdate(true);
                    
                    var bgImg = nameTransformToAnimate.GetComponent<Image>();
                    if (bgImg != null)
                    {
                        bgImg.color = new Color(bgImg.color.r, bgImg.color.g, bgImg.color.b, 0);
                        bgImg.DOFade(0.8f, 0.6f).SetDelay(0.2f).SetUpdate(true); // Assuming background isn't fully opaque
                    }
                }

                if (_mvpPortrait != null)
                {
                    var sprite = mvp.GetSprite(MaouSamaTD.Units.UnitData.UnitImageType.WaistUp);
                    if (sprite == null) sprite = mvp.GetSprite(MaouSamaTD.Units.UnitData.UnitImageType.Avatar);
                    if (sprite == null) sprite = mvp.GetSprite(MaouSamaTD.Units.UnitData.UnitImageType.Chibi);
                    _mvpPortrait.sprite = sprite;

                    // Slide in from left animation
                    var rect = _mvpPortrait.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        float origX = rect.anchoredPosition.x;
                        rect.anchoredPosition = new Vector2(origX - 600, rect.anchoredPosition.y);
                        rect.DOAnchorPosX(origX, 0.6f).SetEase(Ease.OutBack).SetUpdate(true);
                    }
                    else
                    {
                        _mvpPortrait.transform.localPosition = new Vector3(-600, 0, 0);
                        _mvpPortrait.transform.DOLocalMoveX(0, 0.6f).SetEase(Ease.OutBack).SetUpdate(true);
                    }
                    
                    _mvpPortrait.color = new Color(1, 1, 1, 0);
                    _mvpPortrait.DOFade(1f, 0.6f).SetUpdate(true);
                }
            }
            else
            {
                if (_mvpPortrait != null) _mvpPortrait.color = new Color(1, 1, 1, 0);
                if (_mvpNameText != null) _mvpNameText.color = new Color(1, 1, 1, 0);
                if (_mvpNameText != null && _mvpNameText.transform.parent != null && _mvpNameText.transform.parent.name.Contains("BG"))
                {
                    var bgImg = _mvpNameText.transform.parent.GetComponent<Image>();
                    if (bgImg != null) bgImg.color = new Color(1, 1, 1, 0);
                }
            }

            if (_gameManager.SessionLoot != null && _lootSequenceGrid != null)
            {
                // Clear existing loot grid placeholders
                foreach (Transform t in _lootSequenceGrid) Destroy(t.gameObject);

                foreach (var loot in _gameManager.SessionLoot)
                {
                    GameObject cardObj;
                    if (_lootCardPrefab != null)
                    {
                        cardObj = Instantiate(_lootCardPrefab, _lootSequenceGrid, false);
                        var cardUI = cardObj.GetComponent<LootCardUI>();
                        if (cardUI != null)
                        {
                            // Use Addressables to check if key exists first
                            var locOp = Addressables.LoadResourceLocationsAsync(loot.ItemID);
                            locOp.Completed += (locHandle) =>
                            {
                                if (locHandle.Status == AsyncOperationStatus.Succeeded && locHandle.Result.Count > 0)
                                {
                                    var op = Addressables.LoadAssetAsync<MaouSamaTD.Data.ItemConfigSO>(loot.ItemID);
                                    op.Completed += (handle) =>
                                    {
                                        if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                                        {
                                            cardUI.IconImage.color = Color.white; // Reset color from placeholder
                                            cardUI.IconImage.sprite = handle.Result.ItemIcon;
                                            cardUI.NameText.text = handle.Result.ItemName.ToUpper();
                                            cardUI.BackgroundImage.color = handle.Result.BackgroundColor;
                                            cardUI.NameText.color = handle.Result.TextColor;
                                            cardUI.QtyText.color = handle.Result.TextColor;
                                        }
                                        else
                                        {
                                            cardUI.NameText.text = loot.ItemID.Replace("xp_core_", "").Replace("mat_", "").Replace("_", " ").ToUpper();
                                        }
                                    };
                                }
                                else
                                {
                                    // Fallback if Addressables key doesn't exist
                                    cardUI.NameText.text = loot.ItemID.Replace("xp_core_", "").Replace("mat_", "").Replace("_", " ").ToUpper();
                                }
                            };
                            
                            cardUI.QtyText.text = $"x{loot.Quantity}";
                        }
                    }
                    else
                    {
                        // Fallback just in case
                        cardObj = new GameObject("LootCard");
                        cardObj.transform.SetParent(_lootSequenceGrid, false);
                        var layoutElement = cardObj.AddComponent<LayoutElement>();
                        layoutElement.preferredWidth = 110; layoutElement.preferredHeight = 140;
                    }

                    cardObj.transform.localScale = Vector3.zero;
                    cardObj.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetDelay(Random.Range(0f, 0.3f)).SetUpdate(true);
                }
            }
        }

        // Helper methods to find children by name in XPCard prefab
        private Image FindChildImage(GameObject parent, string childName)
        {
            var t = parent.transform.Find(childName);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private TextMeshProUGUI FindChildTMP(GameObject parent, string childName)
        {
            // Search direct children first
            var t = parent.transform.Find(childName);
            if (t != null) return t.GetComponent<TextMeshProUGUI>();
            
            // Search nested (e.g. RightContent/LevelText)
            foreach (Transform child in parent.transform)
            {
                var nested = child.Find(childName);
                if (nested != null) return nested.GetComponent<TextMeshProUGUI>();
            }
            return null;
        }

        private RectTransform FindChildRect(GameObject parent, string childName)
        {
            // Search direct children first
            var t = parent.transform.Find(childName);
            if (t != null) return t.GetComponent<RectTransform>();
            
            // Search nested (SliderFill is inside SliderBg which is inside RightContent)
            foreach (Transform child in parent.transform)
            {
                var nested = child.Find(childName);
                if (nested != null) return nested.GetComponent<RectTransform>();
                // Go one more level deep
                foreach (Transform grandchild in child)
                {
                    var deep = grandchild.Find(childName);
                    if (deep != null) return deep.GetComponent<RectTransform>();
                }
            }
            return null;
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

            // Use procedural diamond shape until the sprite loads
            innerIconObj.transform.localRotation = Quaternion.Euler(0, 0, 45);

            UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<MaouSamaTD.Data.ItemConfigSO>(itemID).Completed += (op) =>
            {
                if (op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded && op.Result != null && op.Result.ItemIcon != null)
                {
                    if (iconImage != null)
                    {
                        iconImage.sprite = op.Result.ItemIcon;
                        iconImage.color = Color.white;
                        innerIconObj.transform.localRotation = Quaternion.identity; // Reset rotation once sprite is set
                    }
                }
            };

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
                // DOPunchScale on the destination (e.g. Base HP Container) has been removed to avoid compounding scale bugs.
                
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
