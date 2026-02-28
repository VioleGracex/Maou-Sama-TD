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
                
                // Trigger tutorial if enabled for this level
                if (dataToLoad.HasTutorial && dataToLoad.TutorialData != null)
                {
                    Debug.Log($"[LevelManager] Level has tutorial: {dataToLoad.TutorialData.name}. Starting...");
                    _tutorialManager.StartTutorial(dataToLoad.TutorialData);
                }
                else
                {
                    Debug.Log($"[LevelManager] No tutorial for this level. HasTutorial: {dataToLoad.HasTutorial}, Data: {dataToLoad.TutorialData}");
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
