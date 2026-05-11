using MaouSamaTD.Grid;
using MaouSamaTD.UI;
using UnityEngine;
using Zenject;
using MaouSamaTD.Levels;
using MaouSamaTD.Units;

namespace MaouSamaTD.Managers
{
    public class GameManager : MonoBehaviour
    {
        #region Fields
        [Inject] private DeploymentUI _deploymentUI;
        [Inject] private GridManager _gridManager;
        [Inject] private InteractionManager _interactionManager;
        [Inject] private BattleCurrencyManager _battleCurrencyManager;
        [Inject] private EconomyManager _economyManager;
        [Inject] private UnitInspectorUI _unitInspectorUI;
        [Inject] private CameraControlUI _cameraControlUI;
        [Inject] private CameraManager _cameraManager;
        [Inject] private EnemyManager _enemyManager;
        [Inject] private GridGenerator _gridGenerator;
        [InjectOptional] private MaouSamaTD.Skills.SkillManager _skillManager;
        [InjectOptional] private MaouSamaTD.UI.Skills.SkillPanelUI _skillPanelUI;
        [Inject] private StoryManager _storyManager;

        [Inject] private MaouSamaTD.Utils.PathVisualizer _pathVisualizer; 

        [Inject] private SaveManager _saveManager;
        [Inject] private GameSelectionState _gameSelectionState;
        
        [Header("References")]
        [SerializeField] private Material _pathMaterial;

        private LevelData _currentLevelData;
        public LevelData CurrentLevelData => _currentLevelData;

        public int NexusIntegrity { get; private set; }
        public int MaxNexusIntegrity { get; private set; } = 20;
        public int EnemiesPassedCount { get; private set; }
        public System.Action<int> OnNexusIntegrityChanged;
        public event System.Action OnVictory;
        public event System.Action OnGameOver;
        public event System.Action OnGameFinished;
        public event System.Action<float> OnSpeedChanged;

        public bool IsGameEnded { get; private set; } = false;
        public float CurrentSpeed { get; private set; } = 1f;
        public bool IsPaused { get; private set; } = false;
        public float TimeTaken { get; private set; } = 0f;
        public bool PreventDeathForTutorial { get; set; } = false;
        public int UnitsLostCount { get; private set; } = 0;
        #endregion

        #region Initialization
        [SerializeField] private MaouSamaTD.UI.MainMenu.LoadingScreenPanel _loadingScreen;

        public void LoadLevelData(LevelData levelData)
        {
            InitializeGame(levelData);
        }

        private void InitializeGame(LevelData levelData)
        {
            _currentLevelData = levelData;
            Debug.Log("[GameManager] Initializing Game...");
            SetSpeed(1f); // Reset TimeScale on Restart

            if (_gridGenerator != null && levelData.MapData != null)
            {
                Debug.Log($"[GameManager] Loading Level: {levelData.LevelName} (ID: {levelData.LevelID})");
                Debug.Log($"[GameManager] Loading MapData Asset: {levelData.MapData.name} ({levelData.MapData.Width}x{levelData.MapData.Height})");
                _gridGenerator.LoadMapData(levelData.MapData);
            }
            else if (levelData.MapData == null)
            {
                Debug.LogError($"[GameManager] LevelData '{levelData.LevelName}' has NO MapData assigned!");
            }

            if (_gridManager != null) 
            {
                _gridManager.Init();
                Debug.Log("[GameManager] GridManager Initialized.");
            }
            else
            {
                Debug.LogError("[GameManager] GridManager is NULL!");
            }

            if (_cameraManager != null)
            {
                 _cameraManager.Init();
                 
                 if (_gridManager != null && levelData.MapData != null)
                 {
                     var map = levelData.MapData;
                     if (map.Width > 0 && map.Height > 0)
                     {
                         _gridManager.Width = map.Width;
                         _gridManager.Height = map.Height;
                     }
                     
                    float centerX = (_gridManager.Width - 1) * _gridManager.CellSize / 2f;
                    float centerZ = (_gridManager.Height - 1) * _gridManager.CellSize / 2f;
                    _cameraManager.FrameGrid(centerX, centerZ);
                 }
                 Debug.Log("[GameManager] CameraManager Initialized.");
                 
                 if (_cameraControlUI != null)
                 {
                     _cameraControlUI.Init();
                     Debug.Log("[GameManager] CameraControlUI Initialized.");
                 }
                 else
                 {
                     Debug.LogWarning("[GameManager] CameraControlUI is NULL (or not injected).");
                 }
            }
            else
            {
                Debug.LogError("[GameManager] CameraManager is NULL!");
            }

            if (_pathVisualizer != null)
            {
                _pathVisualizer.Init(_pathMaterial);
            }

            if (_battleCurrencyManager != null)
            {
                _battleCurrencyManager.Init();
                Debug.Log("[GameManager] BattleCurrencyManager Initialized.");
            }
            else
            {
                Debug.LogError("[GameManager] BattleCurrencyManager is NULL!");
            }

            if (_deploymentUI != null)
            {
                System.Collections.Generic.List<UnitData> cohortToLoad = levelData.PremadeCohort;
                UnitData supportToLoad = levelData.SupportAssistant;
                string source = "LevelData Asset (Premade)";

                if (_gameSelectionState != null && _gameSelectionState.SelectedCohort != null && _gameSelectionState.SelectedCohort.Count > 0)
                {
                    cohortToLoad = _gameSelectionState.SelectedCohort;
                    supportToLoad = null; // Included in list
                    source = "GameSelectionState (Player Choice)";
                }

                Debug.Log($"[GameManager] Initializing DeploymentUI from {source}. Total units in cohort list: {cohortToLoad?.Count ?? 0}");
                
                if (cohortToLoad != null)
                {
                    for (int i = 0; i < cohortToLoad.Count; i++)
                    {
                        var u = cohortToLoad[i];
                        if (u != null)
                            Debug.Log($"[GameManager] Cohort Unit [{i}]: {u.UnitName} (ID: {u.UniqueID})");
                        else
                            Debug.LogWarning($"[GameManager] Cohort Unit [{i}] is NULL!");
                    }
                }

                _deploymentUI.Init(cohortToLoad, supportToLoad);
                Debug.Log("[GameManager] DeploymentUI Initialized.");
            }
            else
            {
                 Debug.LogWarning("[GameManager] DeploymentUI is NULL (or not injected). Cannot show unit bar!");
            }

            System.Collections.Generic.List<MaouSamaTD.Skills.SovereignRiteData> ritesToLoad = new System.Collections.Generic.List<MaouSamaTD.Skills.SovereignRiteData>();
            
            // Priority 1: Hand-picked rites from selection state (Normal Flow)
            if (_gameSelectionState != null && _gameSelectionState.SelectedRites != null && _gameSelectionState.SelectedRites.Count > 0)
            {
                ritesToLoad = _gameSelectionState.SelectedRites;
                Debug.Log($"[GameManager] Using {_gameSelectionState.SelectedRites.Count} rites from Selection State.");
            }
            // Priority 2: Fallback to LevelData defaults based on gender (Direct Scene Play with Save Data)
            else if (_saveManager != null && _saveManager.CurrentData != null)
            {
                ritesToLoad = _saveManager.CurrentData.Gender == MaouSamaTD.Data.MaouGender.Male 
                    ? levelData.MaleSovereignRites 
                    : levelData.FemaleSovereignRites;
                
                // Wide Fallback for Editor (if gender-specific list is empty but the other isn't)
                if ((ritesToLoad == null || ritesToLoad.Count == 0) && Application.isEditor)
                {
                    var otherRites = _saveManager.CurrentData.Gender == MaouSamaTD.Data.MaouGender.Male 
                        ? levelData.FemaleSovereignRites 
                        : levelData.MaleSovereignRites;
                    
                    if (otherRites != null && otherRites.Count > 0)
                    {
                        ritesToLoad = otherRites;
                        Debug.Log("[GameManager] Active gender's rite list was empty. Using 'Wide Fallback' to other gender's list for testing.");
                    }
                }
                Debug.Log($"[GameManager] Loaded {ritesToLoad.Count} rites using Save Data Gender fallback.");
            }
            // Priority 3: Hard Fallback (Direct Scene Play, No Save Data/Selection)
            else
            {
                // Default to Male if unknown or if Male list is available
                ritesToLoad = (levelData.MaleSovereignRites != null && levelData.MaleSovereignRites.Count > 0) 
                    ? levelData.MaleSovereignRites 
                    : levelData.FemaleSovereignRites;
                
                string source = (levelData.MaleSovereignRites != null && levelData.MaleSovereignRites.Count > 0) ? "Male" : "Female";
                Debug.Log($"[GameManager] No selection state or save data found. Using 'Hard Fallback' (Defaulting to {source} rites). Loaded: {ritesToLoad?.Count ?? 0}");
            }

            if (_skillManager != null)
            {
                _skillManager.Init(ritesToLoad);
                Debug.Log("[GameManager] SkillManager Initialized.");
            }

            if (_skillPanelUI != null)
            {
                _skillPanelUI.Init(ritesToLoad);
                Debug.Log("[GameManager] SkillPanelUI Initialized.");
            }
            
            if (_interactionManager != null)
            {
                _interactionManager.Init();
                Debug.Log("[GameManager] InteractionManager Initialized.");
            }
            else
            {
                Debug.LogError("[GameManager] InteractionManager is NULL!");
            }

            if (_unitInspectorUI != null)
            {
                _unitInspectorUI.Init();
                Debug.Log("[GameManager] UnitInspectorUI Initialized.");
            }
            
            Debug.Log("[GameManager] All Systems Initialized. Level Ready.");
            
            if (_enemyManager != null && levelData != null)
            {
                float gracePeriod = levelData.GracePeriod;
                Debug.Log($"[GameManager] Starting Enemy Manager with Grace Period: {gracePeriod}s");
                
                // Pass the enemy container from GridManager (which might have been found dynamically or assigned)
                _enemyManager.Initialize(levelData.Waves, _gridManager.EnemyContainer, gracePeriod);
            }
            else
            {
                if (_enemyManager == null) Debug.LogError("[GameManager] EnemyManager is NULL!");
                if (levelData == null) Debug.LogError("[GameManager] LevelData is NULL!");
            }

            MaxNexusIntegrity = 20; // Default or from level data
            NexusIntegrity = MaxNexusIntegrity;
            EnemiesPassedCount = 0;
            OnNexusIntegrityChanged?.Invoke(NexusIntegrity);

            // Signal the loading screen that the level is ready
            if (_loadingScreen != null) _loadingScreen.NotifyLevelReady();
        }
        #endregion

        #region Public API
        public void TakeBaseDamage(int amount)
        {
            if (IsGameEnded) return;

            NexusIntegrity -= amount;
            if (NexusIntegrity < 0) NexusIntegrity = 0;
            
            OnNexusIntegrityChanged?.Invoke(NexusIntegrity);
            
            Debug.Log($"[GameManager] Base taking damage! Nexus Integrity remaining: {NexusIntegrity}");

            if (NexusIntegrity <= 0)
            {
                CheckLoseConditions(LevelConditionType.BaseHPZero);
            }
        }

        public void EnemyEscaped(EnemyUnit enemy)
        {
            if (IsGameEnded) return;

            EnemiesPassedCount++;
            
            bool isBoss = enemy != null && enemy.EnemyData != null && enemy.EnemyData.IsBoss;
            int damage = enemy != null && enemy.EnemyData != null ? (int)enemy.EnemyData.ExitDamage : 1;

            Debug.Log($"[GameManager] Enemy escaped (Boss: {isBoss})! Total passed: {EnemiesPassedCount}");

            if (isBoss)
            {
                Debug.LogWarning("[GameManager] BOSS ESCAPED! Triggering Game Over.");
                NexusIntegrity = 0;
                OnNexusIntegrityChanged?.Invoke(NexusIntegrity);
                GameOver();
                return;
            }

            CheckLoseConditions(LevelConditionType.EnemiesPassedLimit);
            
            TakeBaseDamage(damage); 
        }

        private void CheckLoseConditions(LevelConditionType triggerType)
        {
            if (IsGameEnded) return;

            bool shouldLose = false;

            // Default behavior if no conditions defined
            if (_currentLevelData == null || _currentLevelData.LoseConditions.Count == 0)
            {
                if (triggerType == LevelConditionType.BaseHPZero && NexusIntegrity <= 0) shouldLose = true;
            }
            else
            {
                foreach (var condition in _currentLevelData.LoseConditions)
                {
                    switch (condition.Type)
                    {
                        case LevelConditionType.BaseHPZero:
                            if (NexusIntegrity <= 0) shouldLose = true;
                            break;
                        case LevelConditionType.EnemiesPassedLimit:
                            if (EnemiesPassedCount >= condition.Value) shouldLose = true;
                            break;
                    }
                    if (shouldLose) break;
                }
            }

            if (shouldLose)
            {
                GameOver();
            }
        }

        private void Update()
        {
            if (IsGameEnded || IsPaused) return;
            TimeTaken += Time.deltaTime;
        }

        public void ReportUnitLost()
        {
            UnitsLostCount++;
        }

        public void Victory()
        {
            Debug.Log($"[GameManager] Victory() called. IsGameEnded: {IsGameEnded}, HasStory: {_currentLevelData?.HasStory}, OutroStory: {_currentLevelData?.OutroStory != null}");
            if (IsGameEnded) return;
            IsGameEnded = true;
            Time.timeScale = 0f; // Freeze game timescale immediately upon victory
            MaouSamaTD.Battle.BattleLogManager.Instance.LogEvent(MaouSamaTD.Battle.BattleLogType.System, "Game", "", "Victory Achieved!", 0);
            OnGameFinished?.Invoke();
            Debug.Log("[GameManager] Victory is being processed...");
            
            var starResults = EvaluateStarConditions();
            int stars = 0;
            foreach (var res in starResults) if (res.IsAchieved) stars++;
            if (stars == 0) stars = 1; // Always at least 1 star for victory
            
            if (_saveManager != null && _currentLevelData != null)
            {
                _saveManager.LevelComplete(_currentLevelData.LevelID, stars);
                
                // Distribute Mission XP to all deployed units
                if (_deploymentUI != null && _currentLevelData != null)
                {
                    MaouSamaTD.Progression.ProgressionLogic.DistributeMissionXP(
                        new System.Collections.Generic.List<UnitData>(_deploymentUI.DeployedUnits), 
                        _currentLevelData.MissionXP
                    );
                }

                // Process all rewards for winning the level
                if (_currentLevelData.WinRewards != null)
                {
                    foreach (var reward in _currentLevelData.WinRewards)
                    {
                        if (reward.Type == MaouSamaTD.Data.RewardType.GoldCoins)
                        {
                            if (_economyManager != null) _economyManager.AddGold(reward.Amount);
                            else _saveManager.AddGold(reward.Amount);
                        }
                        else if (reward.Type == MaouSamaTD.Data.RewardType.BloodCrests)
                        {
                            if (_economyManager != null) _economyManager.AddBloodCrest(reward.Amount);
                            else _saveManager.AddBloodCrest(reward.Amount);
                        }
                    }
                }

                Debug.Log($"[GameManager] Progress Saved. Level: {_currentLevelData.LevelID}, Stars: {stars}");
            }

            if (_currentLevelData != null && _currentLevelData.HasStory && _currentLevelData.OutroStory != null)
            {
                Debug.Log("[GameManager] Playing Outro Story before calling OnVictory event...");
                _storyManager.PlayStory(_currentLevelData.OutroStory, () => 
                {
                    Debug.Log("[GameManager] Outro Story finished. Invoking OnVictory event...");
                    OnVictory?.Invoke();
                    SetSpeed(0);
                });
            }
            else
            {
                Debug.Log("[GameManager] No Outro Story. Invoking OnVictory event immediately...");
                OnVictory?.Invoke();
                SetSpeed(0);
            }
        }
        public bool IsTutorialTimeStop { get; private set; } = false;

        public void SetSpeed(float speed, bool isTutorialTimeStop = false)
        {
            CurrentSpeed = speed;
            IsTutorialTimeStop = isTutorialTimeStop;
            if (!IsPaused && !IsGameEnded)
            {
                Time.timeScale = CurrentSpeed;
            }
            if (speed == 0f)
            {
                FloatingTextManager.Instance?.DestroyAllActiveTexts();
            }
            OnSpeedChanged?.Invoke(speed);
        }

        public void TogglePause()
        {
            if (IsGameEnded) return;
            
            IsPaused = !IsPaused;
            if (IsPaused)
            {
                Time.timeScale = 0f;
                FloatingTextManager.Instance?.DestroyAllActiveTexts();
            }
            else
            {
                Time.timeScale = CurrentSpeed;
            }
        }
        #endregion

        #region Internal Logic
        private void GameOver()
        {
            Debug.Log($"[GameManager] GameOver() called. IsGameEnded: {IsGameEnded}, HasStory: {_currentLevelData?.HasStory}, OutroStory: {_currentLevelData?.OutroStory != null}");
            if (IsGameEnded) return;
            IsGameEnded = true;
            Time.timeScale = 0f; // Freeze game timescale immediately upon defeat
            MaouSamaTD.Battle.BattleLogManager.Instance.LogEvent(MaouSamaTD.Battle.BattleLogType.System, "Game", "", "Game Over - Defeat", 0);
            OnGameFinished?.Invoke();
            Debug.Log("[GameManager] GameOver is being processed...");
            
            if (_currentLevelData != null && _currentLevelData.HasStory && _currentLevelData.OutroStory != null)
            {
                Debug.Log("[GameManager] Playing Defeat Outro Story before calling OnGameOver event...");
                _storyManager.PlayStory(_currentLevelData.OutroStory, () => 
                {
                    Debug.Log("[GameManager] Defeat Outro Story finished. Invoking OnGameOver event...");
                    OnGameOver?.Invoke();
                    SetSpeed(0);
                });
            }
            else
            {
                Debug.Log("[GameManager] No Defeat Outro Story. Invoking OnGameOver event immediately...");
                OnGameOver?.Invoke();
                SetSpeed(0);
            }
        }
        #endregion
        public class StarResult
        {
            public string Description;
            public bool IsAchieved;
        }

        public System.Collections.Generic.List<StarResult> EvaluateStarConditions()
        {
            var results = new System.Collections.Generic.List<StarResult>();
            if (_currentLevelData == null) return results;

            foreach (var cond in _currentLevelData.StarConditions)
            {
                bool achieved = false;
                
                // Tutorial levels often auto-grant stars if specified
                if (_currentLevelData.HasTutorial && cond.AutoGrantInTutorial)
                {
                    achieved = true;
                }
                else
                {
                    switch (cond.Type)
                    {
                        case StarCondition.ConditionType.CompleteLevel:
                            achieved = true;
                            break;
                        case StarCondition.ConditionType.TimeLimit:
                            achieved = TimeTaken <= cond.TargetValue;
                            break;
                        case StarCondition.ConditionType.BaseHealth:
                            float hpPct = (float)NexusIntegrity / (float)MaxNexusIntegrity * 100f;
                            achieved = hpPct >= cond.TargetValue;
                            break;
                        case StarCondition.ConditionType.UnitLossLimit:
                            achieved = UnitsLostCount <= cond.TargetValue;
                            break;
                        case StarCondition.ConditionType.SpecificUnitSurvived:
                            // For now, if we don't have a specific ID, we assume pass if not implemented
                            achieved = true; 
                            break;
                    }
                }

                results.Add(new StarResult { Description = cond.Description, IsAchieved = achieved });
            }

            return results;
        }
    }
}
