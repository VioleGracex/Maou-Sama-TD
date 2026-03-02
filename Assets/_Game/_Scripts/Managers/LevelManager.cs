using UnityEngine;
using Zenject;
using MaouSamaTD.Levels;

namespace MaouSamaTD.Managers
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private LevelData _levelData;
        
        [Inject] private GameManager _gameManager;
        [Inject] private GameSelectionState _gameSelectionState;
        [Inject] private TutorialManager _tutorialManager;
        [Inject] private EnemyManager _enemyManager;
        [Inject] private CurrencyManager _currencyManager;

        #region Lifecycle
        private void Start()
        {
            Debug.Log("[LevelManager] Start triggered.");
            if (_gameManager == null) 
            {
                Debug.LogError("[LevelManager] GameManager is NULL!");
                return;
            }

            LevelData dataToLoad = _levelData;

            // Prioritize GameSelectionState if valid
            if (_gameSelectionState != null && _gameSelectionState.SelectedLevel != null)
            {
                Debug.Log($"[LevelManager] Using selected level: {_gameSelectionState.SelectedLevel.LevelName}");
                dataToLoad = _gameSelectionState.SelectedLevel;
                _levelData = dataToLoad;
            }

            if (dataToLoad != null)
            {
                Debug.Log($"[LevelManager] Loading LevelData: {dataToLoad.LevelName}");
                _gameManager.LoadLevelData(dataToLoad);
                
                if (_currencyManager != null)
                {
                    _currencyManager.Init(dataToLoad);
                }

                bool hasTutorial = dataToLoad.HasTutorial && dataToLoad.TutorialData != null;

                if (_enemyManager != null && dataToLoad != null)
                {
                    float gracePeriod = dataToLoad.GracePeriod;
                    Debug.Log($"[LevelManager] Initializing Enemy Manager. Tutorial Active: {hasTutorial}");
                    // If tutorial is active, DON'T start immediately. TutorialManager will trigger waves.
                    _enemyManager.Initialize(dataToLoad.Waves, gracePeriod, !hasTutorial);
                }

                // Trigger tutorial if enabled for this level
                if (hasTutorial)
                {
                    Debug.Log($"[LevelManager] Level has tutorial: {dataToLoad.TutorialData.name}. Starting...");
                    _tutorialManager.StartTutorial(dataToLoad.TutorialData);
                }
            }
            else
            {
                Debug.LogWarning("[LevelManager] No LevelData found!");
            }
        }
        #endregion
    }
}
