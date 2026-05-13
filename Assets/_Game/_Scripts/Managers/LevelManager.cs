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
        [Inject] private StoryManager _storyManager;
        [Inject] private EnemyManager _enemyManager;
        [Inject] private BattleCurrencyManager _currencyManager;
        [Inject] private Grid.GridManager _gridManager;
        [Inject] private MaouSamaTD.UI.DeploymentUI _deploymentUI;

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

                // Story Intro Check
                if (dataToLoad.HasStory && dataToLoad.IntroStory != null)
                {
                    Debug.Log($"[LevelManager] Level has intro story: {dataToLoad.IntroStory.name}. Starting...");
                    _storyManager.PlayStory(dataToLoad.IntroStory, () => OnIntroFinished(dataToLoad));
                }
                else
                {
                    OnIntroFinished(dataToLoad);
                }
            }
            else
            {
                Debug.LogWarning("[LevelManager] No LevelData found!");
            }
        }

        private void OnIntroFinished(LevelData dataToLoad)
        {
            bool hasTutorial = dataToLoad.HasTutorial && dataToLoad.TutorialData != null;

            if (hasTutorial)
            {
                // We show the choice popup from GameControlUI!
                MaouSamaTD.UI.GameControlUI ui = FindFirstObjectByType<MaouSamaTD.UI.GameControlUI>();
                if (ui != null)
                {
                    ui.ShowTutorialPrompt(
                        () => // Play Tutorial
                        {
                            InitializeLevel(dataToLoad, true);
                        },
                        () => // Skip Tutorial
                        {
                            InitializeLevel(dataToLoad, false);
                        }
                    );
                }
                else
                {
                    // Fallback if UI is missing
                    InitializeLevel(dataToLoad, true);
                }
            }
            else
            {
                InitializeLevel(dataToLoad, false);
            }
        }

        private void InitializeLevel(LevelData dataToLoad, bool playTutorial)
        {
            if (_enemyManager != null && dataToLoad != null)
            {
                float gracePeriod = dataToLoad.GracePeriod;
                Debug.Log($"[LevelManager] Initializing Enemy Manager. Tutorial Active: {playTutorial}");
                _enemyManager.Initialize(dataToLoad.Waves, _gridManager.EnemyContainer, gracePeriod, !playTutorial);
            }

            if (playTutorial && dataToLoad.HasTutorial && dataToLoad.TutorialData != null)
            {
                Debug.Log($"[LevelManager] Level has tutorial: {dataToLoad.TutorialData.name}. Starting...");
                _tutorialManager.StartTutorial(dataToLoad.TutorialData);
            }
            else
            {
                // If we skip the tutorial, and it is Level 2 (Tomb of Lilith), we must give Lilith to the player immediately!
                if (dataToLoad != null && (dataToLoad.LevelID == "1-2" || dataToLoad.LevelName.Contains("Level 2") || dataToLoad.LevelName.Contains("Lilith")))
                {
                    StartCoroutine(LoadAndAddLilithSkippedTutorial());
                }
            }
        }

        private System.Collections.IEnumerator LoadAndAddLilithSkippedTutorial()
        {
            Debug.Log("[LevelManager] Player skipped tutorial on Level 2. Loading Lilith dynamically...");
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<MaouSamaTD.Units.UnitData>("Char_Lilith_UnitData");
            yield return handle;

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                var lilithData = handle.Result;
                if (_deploymentUI != null)
                {
                    _deploymentUI.AddUnit(lilithData);
                    _deploymentUI.SetUnitButtonVisibility("Lilith", true);
                    Debug.Log($"[LevelManager] Successfully loaded and added Lilith to unit buttons.");
                }
                else
                {
                    Debug.LogWarning("[LevelManager] DeploymentUI is missing. Cannot add Lilith.");
                }
            }
            else
            {
                Debug.LogError("[LevelManager] Failed to load Lilith from Addressables!");
            }
        }
        #endregion
    }
}
