using UnityEngine;
using Zenject;
using MaouSamaTD.Levels;

namespace MaouSamaTD.Managers
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private LevelData _levelData;
        
        [Inject] private GameManager _gameManager;

        private void Start()
        {
            if (_levelData != null && _gameManager != null)
            {
                _gameManager.LoadLevelData(_levelData);
            }
            else
            {
                Debug.LogWarning("LevelManager: LevelData or GameManager is missing!");
            }
        }
    }
}
