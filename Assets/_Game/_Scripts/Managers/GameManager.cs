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
        [Inject] private CurrencyManager _currencyManager;
        [Inject] private UnitInspectorUI _unitInspectorUI;
        [Inject] private CameraControlUI _cameraControlUI;
        [Inject] private CameraManager _cameraManager;
        [Inject] private EnemyManager _enemyManager;
        [Inject] private GridGenerator _gridGenerator;
        [InjectOptional] private MaouSamaTD.Skills.SkillManager _skillManager;
        [InjectOptional] private MaouSamaTD.UI.Skills.SkillPanelUI _skillPanelUI;

        [Inject] private MaouSamaTD.Utils.PathVisualizer _pathVisualizer; 

        [Inject] private SaveManager _saveManager;
        [Inject] private GameSelectionState _gameSelectionState;
        
        [Header("References")]
        [SerializeField] private Material _pathMaterial;

        private LevelData _currentLevelData;

        public int PlayerLives { get; private set; }
        public System.Action<int> OnLivesChanged;
        public event System.Action OnVictory;
        public event System.Action OnGameOver;
        public event System.Action<float> OnSpeedChanged;

        public bool IsGameEnded { get; private set; } = false;
        public float CurrentSpeed { get; private set; } = 1f;
        public bool IsPaused { get; private set; } = false;
        #endregion

        #region Initialization
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
                Debug.Log($"[GameManager] Loading MapData from {levelData.LevelName}");
                _gridGenerator.LoadMapData(levelData.MapData);
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

            if (_currencyManager != null)
            {
                _currencyManager.Init();
                Debug.Log("[GameManager] CurrencyManager Initialized.");
            }
            else
            {
                Debug.LogError("[GameManager] CurrencyManager is NULL!");
            }

            if (_deploymentUI != null)
            {
                _deploymentUI.Init(levelData.PremadeCohort, levelData.SupportAssistant);
                Debug.Log("[GameManager] DeploymentUI Initialized.");
            }
            else
            {
                 Debug.Log("[GameManager] DeploymentUI is NULL (or not injected).");
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
                _enemyManager.Initialize(levelData.Waves, gracePeriod);
            }
            else
            {
                if (_enemyManager == null) Debug.LogError("[GameManager] EnemyManager is NULL!");
                if (levelData == null) Debug.LogError("[GameManager] LevelData is NULL!");
            }

            PlayerLives = 20;
            OnLivesChanged?.Invoke(PlayerLives);
        }
        #endregion

        #region Public API
        public void TakeBaseDamage(int amount)
        {
            if (IsGameEnded) return;

            PlayerLives -= amount;
            if (PlayerLives < 0) PlayerLives = 0;
            
            OnLivesChanged?.Invoke(PlayerLives);
            
            Debug.Log($"[GameManager] Base taking damage! Lives remaining: {PlayerLives}");

            if (PlayerLives <= 0)
            {
                GameOver();
            }
        }

        public void Victory()
        {
            if (IsGameEnded) return;
            IsGameEnded = true;
            Debug.Log("[GameManager] Victory!");
            
            int stars = 1;
            if (PlayerLives >= 20) stars = 3;
            else if (PlayerLives >= 10) stars = 2;
            
            if (_saveManager != null && _currentLevelData != null)
            {
                _saveManager.LevelComplete(_currentLevelData.LevelID, stars);
                
                // Process all rewards for winning the level
                if (_currentLevelData.WinRewards != null)
                {
                    foreach (var reward in _currentLevelData.WinRewards)
                    {
                        if (reward.Type == MaouSamaTD.Levels.RewardType.GoldCoins)
                        {
                            _saveManager.AddCurrency(reward.Amount);
                        }
                        else if (reward.Type == MaouSamaTD.Levels.RewardType.BloodCrests)
                        {
                            Debug.Log($"[GameManager] Earned {reward.Amount} Blood Crests!");
                        }
                    }
                }

                Debug.Log($"[GameManager] Progress Saved. Level: {_currentLevelData.LevelID}, Stars: {stars}");
            }

            OnVictory?.Invoke();
            SetSpeed(0);
        }

        public void SetSpeed(float speed)
        {
            CurrentSpeed = speed;
            if (!IsPaused && !IsGameEnded)
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
            if (IsGameEnded) return;
            IsGameEnded = true;
            Debug.Log("[GameManager] Game Over!");
            OnGameOver?.Invoke();
            SetSpeed(0);
        }
        #endregion
    }
}
