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
        [Inject] private DeploymentUI _deploymentUI;
        [Inject] private GridManager _gridManager;
        [Inject] private InteractionManager _interactionManager;
        [Inject] private CurrencyManager _currencyManager;
        [Inject] private UnitInspectorUI _unitInspectorUI;
        [Inject] private CameraControlUI _cameraControlUI;
        [Inject] private CameraManager _cameraManager;
        [Inject] private EnemyManager _enemyManager;
        [Inject] private GridGenerator _gridGenerator;

        [Inject] private MaouSamaTD.Utils.PathVisualizer _pathVisualizer; 

        [Inject] private SaveManager _saveManager;
        [Inject] private GameSelectionState _gameSelectionState;
        
        [Header("Test Mode")]
        [SerializeField] private bool _testMode = false;
        [SerializeField] private LevelData _testLevelData;
        
        [Header("References")]
        [SerializeField] private Material _pathMaterial;

        private LevelData _currentLevelData;

        public void LoadLevelData(LevelData levelData)
        {
            InitializeGame(levelData);
        }

        private void InitializeGame(LevelData levelData)
        {
            _currentLevelData = levelData;
            _currentLevelData = levelData;
            Debug.Log("GameManager: Initializing Game...");
            SetSpeed(1f); // Reset TimeScale on Restart

            if (_gridGenerator != null && levelData.MapData != null)
            {
                Debug.Log($"GameManager: Loading MapData from {levelData.LevelName}");
                _gridGenerator.LoadMapData(levelData.MapData);
            }

            if (_gridManager != null) 
            {
                _gridManager.Init();
                Debug.Log("GameManager: GridManager Initialized.");
            }
            else
            {
                Debug.LogError("GameManager: GridManager is NULL!");
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
                 Debug.Log("GameManager: CameraManager Initialized.");
                 
                 if (_cameraControlUI != null)
                 {
                     _cameraControlUI.Init();
                     Debug.Log("GameManager: CameraControlUI Initialized.");
                 }
                 else
                 {
                     Debug.LogWarning("GameManager: CameraControlUI is NULL (or not injected).");
                 }
            }
            else
            {
                Debug.LogError("GameManager: CameraManager is NULL!");
            }

            if (_pathVisualizer != null)
            {
                _pathVisualizer.Init(_pathMaterial);
            }

            if (_currencyManager != null)
            {
                _currencyManager.Init();
                Debug.Log("GameManager: CurrencyManager Initialized.");
            }
            else
            {
                Debug.LogError("GameManager: CurrencyManager is NULL!");
            }

            if (_deploymentUI != null)
            {
                _deploymentUI.Init();
                Debug.Log("GameManager: DeploymentUI Initialized.");
            }
            else
            {
                 Debug.Log("GameManager: DeploymentUI is NULL (or not injected).");
            }
            
            if (_interactionManager != null)
            {
                _interactionManager.Init();
                Debug.Log("GameManager: InteractionManager Initialized.");
            }
            else
            {
                Debug.LogError("GameManager: InteractionManager is NULL!");
            }

            if (_unitInspectorUI != null)
            {
                _unitInspectorUI.Init();
                Debug.Log("GameManager: UnitInspectorUI Initialized.");
            }
            
            Debug.Log("GameManager: All Systems Initialized. Level Ready.");
            
            if (_enemyManager != null && levelData != null)
            {
                float gracePeriod = levelData.GracePeriod;
                Debug.Log($"GameManager: Starting Enemy Manager with Grace Period: {gracePeriod}s");
                _enemyManager.Initialize(levelData.Waves, gracePeriod);
            }
            else
            {
                if (_enemyManager == null) Debug.LogError("GameManager: EnemyManager is NULL!");
                if (levelData == null) Debug.LogError("GameManager: LevelData is NULL!");
            }

            PlayerLives = 20;
            OnLivesChanged?.Invoke(PlayerLives);
        }

        public int PlayerLives { get; private set; }
        public System.Action<int> OnLivesChanged;
        public event System.Action OnVictory;
        public event System.Action OnGameOver;

        public bool IsGameEnded { get; private set; } = false;

        public void TakeBaseDamage(int amount)
        {
            if (IsGameEnded) return;

            PlayerLives -= amount;
            if (PlayerLives < 0) PlayerLives = 0;
            
            OnLivesChanged?.Invoke(PlayerLives);
            
            Debug.Log($"Base taking damage! Lives remaining: {PlayerLives}");

            if (PlayerLives <= 0)
            {
                GameOver();
            }
        }

        private void GameOver()
        {
            if (IsGameEnded) return;
            IsGameEnded = true;
            Debug.Log("Game Over!");
            OnGameOver?.Invoke();
            SetSpeed(0);
        }

        public void Victory()
        {
            if (IsGameEnded) return;
            IsGameEnded = true;
            Debug.Log("Victory!");
            
            int stars = 1;
            if (PlayerLives >= 20) stars = 3;
            else if (PlayerLives >= 10) stars = 2;
            
            if (_saveManager != null && _currentLevelData != null)
            {
                _saveManager.LevelComplete(_currentLevelData.LevelID, stars);
                _saveManager.AddCurrency(_currentLevelData.RewardCurrency);
                Debug.Log($"[GameManager] Progress Saved. Level: {_currentLevelData.LevelID}, Stars: {stars}");
            }

            OnVictory?.Invoke();
            SetSpeed(0);
        }

        public float CurrentSpeed { get; private set; } = 1f;
        public bool IsPaused { get; private set; } = false;

        public void SetSpeed(float speed)
        {
            CurrentSpeed = speed;
            if (!IsPaused && !IsGameEnded)
            {
                Time.timeScale = CurrentSpeed;
            }
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
        private void Start()
        {
            if (_testMode && _testLevelData != null)
            {
                Debug.LogWarning($"GameManager: Test Mode Active! Loading Test Level: {_testLevelData.LevelName}");
                InitializeGame(_testLevelData);
            }
            else if (_gameSelectionState != null && _gameSelectionState.SelectedLevel != null)
            {
                Debug.Log($"GameManager: Loading Selected Level: {_gameSelectionState.SelectedLevel.LevelName}");
                InitializeGame(_gameSelectionState.SelectedLevel);
            }
            else
            {
                Debug.LogError("GameManager: No Level Data found! (Nor Test Mode active)");
            }
        }
    }
}
