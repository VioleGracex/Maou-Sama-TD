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

        #region Lifecycle
        private void Start()
        {
            if (_gameManager == null) return;

            LevelData dataToLoad = _levelData;

            // Prioritize GameSelectionState if valid
            if (_gameSelectionState != null && _gameSelectionState.SelectedLevel != null)
            {
                dataToLoad = _gameSelectionState.SelectedLevel;
                _levelData = dataToLoad;
            }

            if (dataToLoad != null)
            {
                _gameManager.LoadLevelData(dataToLoad);
            }
            else
            {
                Debug.LogWarning("[LevelManager] No LevelData found!");
            }
        }
        #endregion
    }
}
