using MaouSamaTD.Grid;
using MaouSamaTD.UI;
using UnityEngine;
using Zenject;

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
        [Inject] private CameraController _cameraController;

        private void Start()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            Debug.Log("GameManager: Initializing Game...");

            // 1. Init Grid
            if (_gridManager != null) 
            {
                _gridManager.Init(); 
            }

            // 2. Init Currency (Start with some cash)
            if (_currencyManager != null)
            {
                _currencyManager.Init();
            }

            // 2b. Frame Camera
            if (_cameraController != null && _gridManager != null)
            {
                float centerX = (_gridManager.Width - 1) * _gridManager.CellSize / 2f;
                float centerZ = (_gridManager.Height - 1) * _gridManager.CellSize / 2f;
                _cameraController.FrameGrid(centerX, centerZ);
            }

            // 3. Init UI (Needs Currency & Grid ready potentially)
            if (_deploymentUI != null)
            {
                _deploymentUI.Init();
            }
            
            // 4. Init Interaction (Needs UI & Grid)
            if (_interactionManager != null)
            {
                _interactionManager.Init();
            }

            // 5. Init Unit Inspector
            if (_unitInspectorUI != null)
            {
                _unitInspectorUI.Init();
            }

            Debug.Log("GameManager: Initialization Complete.");
        }

        // Time Control
        public float CurrentSpeed { get; private set; } = 1f;
        public bool IsPaused { get; private set; } = false;

        public void SetSpeed(float speed)
        {
            CurrentSpeed = speed;
            if (!IsPaused)
            {
                Time.timeScale = CurrentSpeed;
            }
        }

        public void TogglePause()
        {
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
