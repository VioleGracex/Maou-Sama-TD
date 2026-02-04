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
        // [Inject] references instead of manual SerializeField
        [Inject] private DeploymentUI _deploymentUI;
        [Inject] private GridManager _gridManager;
        [Inject] private InteractionManager _interactionManager;
        [Inject] private CurrencyManager _currencyManager;
        [Inject] private UnitInspectorUI _unitInspectorUI;
        [Inject] private CameraControlUI _cameraControlUI; // Injected
        [Inject] private CameraManager _cameraManager;
        [Inject] private EnemyManager _enemyManager;
        [Inject] private GridGenerator _gridGenerator;

        [Inject] private MaouSamaTD.Utils.PathVisualizer _pathVisualizer; 

        // New Injects for Logic
        [Inject] private SaveManager _saveManager;
        [Inject] private GameSelectionState _gameSelectionState;
        
        private LevelData _currentLevelData; // Track current level


        public void LoadLevelData(LevelData levelData)
        {
            InitializeGame(levelData);
        }

        private void InitializeGame(LevelData levelData)
        {
            _currentLevelData = levelData;
            Debug.Log("GameManager: Initializing Game...");

            // 0. Setup Map (Before Grid Init)
            if (_gridGenerator != null && levelData.MapData != null)
            {
                Debug.Log($"GameManager: Loading MapData from {levelData.LevelName}");
                _gridGenerator.LoadMapData(levelData.MapData);
            }

            // 1. Init Grid
            if (_gridManager != null) 
            {
                _gridManager.Init();
                Debug.Log("GameManager: GridManager Initialized.");
            }
            else
            {
                Debug.LogError("GameManager: GridManager is NULL!");
            }

            // 2. Init Camera (After Grid)
            if (_cameraManager != null)
            {
                 // Ensure grid is ready before camera tries to anchor to it
                 _cameraManager.Init();
                 
                 if (_gridManager != null && levelData.MapData != null)
                 {
                     // Apply Map Settings from MapData
                     var map = levelData.MapData;
                     if (map.Width > 0 && map.Height > 0)
                     {
                         _gridManager.Width = map.Width;
                         _gridManager.Height = map.Height;
                     }
                     
                     // Pass other map generation params to GridGenerator via GridManager or find GridGenerator directly?
                     // Ideally GridManager holds only the live grid state. 
                     // We might need to inject GridGenerator here or let GridManager handle it if it holds the reference (which it usually doesn't).
                     // For now, let's assume GridGenerator will pull from GridManager or we set it manually if exposed.
                     
                     // Actually, GameManager typically orchestrates. If GridGenerator is bound, we can inject it.
                     // Checking if GridGenerator is injected... no.
                     // But we can rely on GridGenerator to exist or be found if we want to force regen.
                     
                     GridGenerator gridGen = FindAnyObjectByType<GridGenerator>();
                     if (gridGen != null)
                     {
                         // Update GridGenerator settings manually for now (since we don't have a clean "LoadMapConfig" on it yet)
                         // Or better: Use reflection/serialized object? No, too slow.
                         // IMPORTANT: GridGenerator usually has its own fields. We need to push data there if we want "GenerateMap" to use it.
                         // But wait, GridGenerator is MonoBehaviour. 
                         
                         // Let's assume we just want to set the seed and regen.
                         // To do this cleanly, GridGenerator needs a "LoadFromMapData" or similar.
                         // OR we just manually key in the vital stats if they are public. They are serialized private.
                         // Valid Refactor: Add 'LoadMapData' to GridGenerator. 
                     }
                    float centerX = (_gridManager.Width - 1) * _gridManager.CellSize / 2f;
                    float centerZ = (_gridManager.Height - 1) * _gridManager.CellSize / 2f;
                    _cameraManager.FrameGrid(centerX, centerZ);
                 }
                 Debug.Log("GameManager: CameraManager Initialized.");
                 
                 // Init Camera UI
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

            // 2.5 Path Visualizer
            if (_pathVisualizer != null)
            {
                _pathVisualizer.Init();
            }

            // 3. Init Currency (Start with some cash)
            if (_currencyManager != null)
            {
                _currencyManager.Init();
                Debug.Log("GameManager: CurrencyManager Initialized.");
            }
            else
            {
                Debug.LogError("GameManager: CurrencyManager is NULL!");
            }

            // 4. Init UI (Needs Currency & Grid ready potentially)
            if (_deploymentUI != null)
            {
                _deploymentUI.Init();
                Debug.Log("GameManager: DeploymentUI Initialized.");
            }
            else
            {
                 // DeploymentUI might be optional or handled elsewhere
                 Debug.Log("GameManager: DeploymentUI is NULL (or not injected).");
            }
            
            // 5. Init Interaction (Needs UI & Grid)
            if (_interactionManager != null)
            {
                _interactionManager.Init();
                Debug.Log("GameManager: InteractionManager Initialized.");
            }
            else
            {
                Debug.LogError("GameManager: InteractionManager is NULL!");
            }

            // 6. Init Unit Inspector
            if (_unitInspectorUI != null)
            {
                _unitInspectorUI.Init();
                Debug.Log("GameManager: UnitInspectorUI Initialized.");
            }
            
            Debug.Log("GameManager: All Systems Initialized. Level Ready.");
            // TODO: Hide Loading Screen Here
            
            // 7. Start Enemy Spawning (with Grace Period)
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

            // Initialize Lives
            PlayerLives = 20;
            OnLivesChanged?.Invoke(PlayerLives);
        }

        // Gameplay State
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
            SetSpeed(0); // Pause
        }

        public void Victory()
        {
            if (IsGameEnded) return;
            IsGameEnded = true;
            Debug.Log("Victory!");
            
            // Calculate Stars (Simple Logic: 20 lives = 3 stars, >10 = 2 stars, >0 = 1 star)
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

        // Time Control
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
    }
}
