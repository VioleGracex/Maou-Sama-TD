using MaouSamaTD.Grid;
using MaouSamaTD.UI;
using UnityEngine;
using Zenject;
using MaouSamaTD.Levels;
using MaouSamaTD.Units;
using System.Linq;
using System.Collections.Generic;

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

        public int ObjectiveHP { get; private set; }
        public int MaxObjectiveHP { get; private set; } = 100;
        public int EnemiesPassedCount { get; private set; }
        public System.Action<int> OnObjectiveHPChanged;
        public int NexusIntegrity => ObjectiveHP;
        public int MaxNexusIntegrity => MaxObjectiveHP;
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

        private System.Collections.Generic.Dictionary<UnitData, float> _unitDamageDealt = new System.Collections.Generic.Dictionary<UnitData, float>();
        public System.Collections.Generic.Dictionary<UnitData, float> UnitDamageDealt => _unitDamageDealt;
        
        public System.Collections.Generic.List<LootDropRecord> SessionLoot { get; private set; } = new System.Collections.Generic.List<LootDropRecord>();
        public System.Collections.Generic.List<XPProgressInfo> DeployedUnitsXPInfo { get; private set; } = new System.Collections.Generic.List<XPProgressInfo>();
        private System.Collections.Generic.Dictionary<string, int> _unitDeathCounts = new System.Collections.Generic.Dictionary<string, int>();
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
            TimeTaken = 0f; // Reset level timer on start/restart
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

            MaxObjectiveHP = levelData != null ? levelData.SovereignMaxHp : 100;
            if (MaxObjectiveHP <= 0) MaxObjectiveHP = 100;
            ObjectiveHP = MaxObjectiveHP;
            EnemiesPassedCount = 0;
            OnObjectiveHPChanged?.Invoke(ObjectiveHP);
            OnNexusIntegrityChanged?.Invoke(ObjectiveHP);

            // Signal the loading screen that the level is ready
            if (_loadingScreen != null) _loadingScreen.NotifyLevelReady();
        }
        #endregion

        #region Public API
        public void TakeBaseDamage(int amount)
        {
            if (IsGameEnded) return;

            ObjectiveHP -= amount;
            if (ObjectiveHP < 0) ObjectiveHP = 0;
            
            OnObjectiveHPChanged?.Invoke(ObjectiveHP);
            OnNexusIntegrityChanged?.Invoke(ObjectiveHP);
            
            Debug.Log($"[GameManager] Base taking damage! Objective HP remaining: {ObjectiveHP}");

            if (ObjectiveHP <= 0)
            {
                CheckLoseConditions(LevelConditionType.BaseHPZero);
            }
        }

        public void EnemyEscaped(EnemyUnit enemy)
        {
            if (IsGameEnded) return;

            EnemiesPassedCount++;
            
            bool isBoss = enemy != null && enemy.EnemyData != null && enemy.EnemyData.IsBoss;
            int damage = 1;
            
            if (enemy != null && enemy.EnemyData != null)
            {
                if (enemy.EnemyData.ExitDamageType == ExitDamageType.Percentage)
                {
                    damage = Mathf.CeilToInt(MaxObjectiveHP * (enemy.EnemyData.ExitDamage / 100f));
                }
                else
                {
                    damage = (int)enemy.EnemyData.ExitDamage;
                }
            }

            Debug.Log($"[GameManager] Enemy escaped (Boss: {isBoss})! Total passed: {EnemiesPassedCount}");

            if (isBoss)
            {
                Debug.LogWarning("[GameManager] BOSS ESCAPED! Triggering Game Over.");
                ObjectiveHP = 0;
                OnObjectiveHPChanged?.Invoke(ObjectiveHP);
                OnNexusIntegrityChanged?.Invoke(ObjectiveHP);
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
                if (triggerType == LevelConditionType.BaseHPZero && ObjectiveHP <= 0) shouldLose = true;
            }
            else
            {
                foreach (var condition in _currentLevelData.LoseConditions)
                {
                    switch (condition.Type)
                    {
                        case LevelConditionType.BaseHPZero:
                            if (ObjectiveHP <= 0) shouldLose = true;
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
            TimeTaken += Time.unscaledDeltaTime;
        }

        public void ReportUnitLost()
        {
            UnitsLostCount++;
        }

        public void ReportUnitLost(string unitId)
        {
            UnitsLostCount++;
            if (_unitDeathCounts.ContainsKey(unitId))
            {
                _unitDeathCounts[unitId]++;
            }
            else
            {
                _unitDeathCounts[unitId] = 1;
            }
        }

        public int GetUnitDeathCount(string unitId)
        {
            return _unitDeathCounts.TryGetValue(unitId, out int count) ? count : 0;
        }

        public void Victory()
        {
            Debug.Log($"[GameManager] Victory() called. IsGameEnded: {IsGameEnded}, HasStory: {_currentLevelData?.HasStory}, OutroStory: {_currentLevelData?.OutroStory != null}");
            if (IsGameEnded) return;
            IsGameEnded = true;

            // Trigger Cinematic Kill Effects
            if (_cameraManager != null)
            {
                _cameraManager.Shake(0.6f, 0.4f);
            }
            SetSpeed(0.2f); // Slow motion effect for final kill cinematic

            MaouSamaTD.Battle.BattleLogManager.Instance.LogEvent(MaouSamaTD.Battle.BattleLogType.System, "Game", "", "Victory Achieved!", 0);
            OnGameFinished?.Invoke();
            Debug.Log("[GameManager] Victory is being processed...");
            
            var starResults = EvaluateStarConditions();
            int stars = 0;
            foreach (var res in starResults) if (res.IsAchieved) stars++;
            if (stars == 0) stars = 1; // Always at least 1 star for victory
            
            if (_saveManager != null && _currentLevelData != null)
            {
                bool isFirstClear = _saveManager.CurrentData != null && !_saveManager.CurrentData.CompletedLevels.Contains(_currentLevelData.LevelID);
                _saveManager.LevelComplete(_currentLevelData.LevelID, stars);
                
                // Distribute Mission XP to all deployed units
                DeployedUnitsXPInfo.Clear();
                if (_deploymentUI != null && _currentLevelData != null)
                {
                    // Snapshot before distribution
                    foreach (var u in _deploymentUI.DeployedUnits)
                    {
                        if (u != null)
                        {
                            DeployedUnitsXPInfo.Add(new XPProgressInfo
                            {
                                Unit = u, OldLevel = u.Level, OldXP = u.Experience, XPAwarded = _currentLevelData.MissionXP
                            });
                        }
                    }

                    MaouSamaTD.Progression.ProgressionLogic.DistributeMissionXP(
                        new System.Collections.Generic.List<UnitData>(_deploymentUI.DeployedUnits), 
                        _currentLevelData.MissionXP
                    );

                    // MVP Bonus
                    var mvp = GetMVPUnit();
                    if (mvp != null)
                    {
                        int bonusXp = Mathf.RoundToInt(_currentLevelData.MissionXP * 0.5f);
                        MaouSamaTD.Progression.ProgressionLogic.AddXP(mvp, bonusXp);
                        mvp.Amity += 10f; // Bonus Amity
                        
                        var mvpInfo = DeployedUnitsXPInfo.Find(x => x.Unit == mvp);
                        if (mvpInfo != null)
                        {
                            mvpInfo.XPAwarded += bonusXp;
                        }
                    }

                    // Update New XP Values
                    for (int i = 0; i < DeployedUnitsXPInfo.Count; i++)
                    {
                        DeployedUnitsXPInfo[i].NewLevel = DeployedUnitsXPInfo[i].Unit.Level;
                        DeployedUnitsXPInfo[i].NewXP = DeployedUnitsXPInfo[i].Unit.Experience;
                    }
                }

                // Roll Level Stage Loot
                if (_currentLevelData.StageLootConfig != null)
                {
                    foreach (var loot in _currentLevelData.StageLootConfig)
                    {
                        if (UnityEngine.Random.value <= loot.DropChance)
                        {
                            int qty = UnityEngine.Random.Range(loot.MinQuantity, loot.MaxQuantity + 1);
                            if (qty > 0)
                            {
                                RegisterLoot(loot.ItemID, qty);
                            }
                        }
                    }
                }

                // Process all rewards for winning the level
                if (_currentLevelData.WinRewards != null)
                {
                    foreach (var reward in _currentLevelData.WinRewards)
                    {
                        if (reward.Type == MaouSamaTD.Data.RewardType.GoldCoins)
                        {
                            RegisterLoot("gold_coins", reward.Amount);
                            if (_economyManager != null) _economyManager.AddGold(reward.Amount);
                            else _saveManager.AddGold(reward.Amount);
                        }
                        else if (reward.Type == MaouSamaTD.Data.RewardType.BloodCrests)
                        {
                            if (isFirstClear)
                            {
                                RegisterLoot("blood_crests", reward.Amount);
                                if (_economyManager != null) _economyManager.AddBloodCrest(reward.Amount);
                                else _saveManager.AddBloodCrest(reward.Amount);
                            }
                            else
                            {
                                Debug.Log($"[GameManager] Level {_currentLevelData.LevelID} already cleared. Skipping repeat premium reward of {reward.Amount} BloodCrests.");
                            }
                        }
                        else if (reward.Type == MaouSamaTD.Data.RewardType.Gems)
                        {
                            if (isFirstClear)
                            {
                                RegisterLoot("gems", reward.Amount);
                                if (_economyManager != null) _economyManager.AddBloodCrest(reward.Amount); // Or whichever premium API exists
                                else _saveManager.AddBloodCrest(reward.Amount);
                            }
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
                    // SetSpeed(0); // REMOVED: Controlled by UI sequence now
                });
            }
            else
            {
                Debug.Log("[GameManager] No Outro Story. Invoking OnVictory event immediately...");
                OnVictory?.Invoke();
                // SetSpeed(0); // REMOVED: Controlled by UI sequence now
            }
        }
        public bool IsTutorialTimeStop { get; private set; } = false;

        public void SetSpeed(float speed, bool isTutorialTimeStop = false)
        {
            CurrentSpeed = speed;
            IsTutorialTimeStop = isTutorialTimeStop;
            if (!IsPaused && (!IsGameEnded || speed == 0f))
            {
                Time.timeScale = CurrentSpeed;
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
            Debug.Log($"[GameManager] GameOver() called. IsGameEnded: {IsGameEnded}");
            if (IsGameEnded) return;
            IsGameEnded = true;
            Time.timeScale = 0f; // Freeze game timescale immediately upon defeat
            MaouSamaTD.Battle.BattleLogManager.Instance.LogEvent(MaouSamaTD.Battle.BattleLogType.System, "Game", "", "Game Over - Defeat", 0);
            OnGameFinished?.Invoke();
            Debug.Log("[GameManager] GameOver is being processed. Invoking OnGameOver event immediately...");
            
            OnGameOver?.Invoke();
            SetSpeed(0);
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
                            float hpPct = (float)ObjectiveHP / (float)MaxObjectiveHP * 100f;
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
        public void RegisterLoot(string itemID, int qty)
        {
            if (string.IsNullOrEmpty(itemID) || qty <= 0) return;
            
            var existing = SessionLoot.Find(x => x.ItemID == itemID);
            if (existing != null) existing.Quantity += qty;
            else SessionLoot.Add(new LootDropRecord { ItemID = itemID, Quantity = qty });

            // Only add to save manager if it's a standard inventory item (gold/blood crests are handled separately)
            if (itemID != "gold_coins" && itemID != "blood_crests")
            {
                if (_saveManager != null)
                {
                    _saveManager.AddItem(itemID, qty);
                    _saveManager.Save();
                }
            }
        }

        public void RegisterDamageDealt(UnitData unit, float damage)
        {
            if (unit == null) return;
            if (_unitDamageDealt.ContainsKey(unit)) _unitDamageDealt[unit] += damage;
            else _unitDamageDealt[unit] = damage;
        }

        public UnitData GetMVPUnit()
        {
            UnitData mvp = null;
            float maxDamage = -1f;

            foreach (var kvp in _unitDamageDealt)
            {
                if (kvp.Value > maxDamage)
                {
                    maxDamage = kvp.Value;
                    mvp = kvp.Key;
                }
            }

            // Fallback 1: first deployed unit
            if (mvp == null && _deploymentUI != null && _deploymentUI.DeployedUnits.Any())
            {
                foreach (var u in _deploymentUI.DeployedUnits) { if (u != null) { mvp = u; break; } }
            }

            // Fallback 2: first selected cohort
            if (mvp == null && _gameSelectionState != null && _gameSelectionState.SelectedCohort != null)
            {
                foreach (var u in _gameSelectionState.SelectedCohort) { if (u != null) { mvp = u; break; } }
            }

            return mvp;
        }
    }

    public class XPProgressInfo
    {
        public UnitData Unit;
        public int OldLevel;
        public int OldXP;
        public int NewLevel;
        public int NewXP;
        public int XPAwarded;
    }

    [System.Serializable]
    public class LootDropRecord
    {
        public string ItemID;
        public int Quantity;
    }
}
