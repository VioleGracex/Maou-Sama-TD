using UnityEngine;
using MaouSamaTD.UI;
using MaouSamaTD.Grid;

namespace MaouSamaTD.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private DeploymentUI _deploymentUI;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private InteractionManager _interactionManager;
        [SerializeField] private CurrencyManager _currencyManager;

        private void Awake()
        {
            Instance = this;
        }

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
                // Grid might auto-init in Start if map exists, or we call it here
                _gridManager.GenerateTestMap(); 
            }

            // 2. Init UI
            if (_deploymentUI != null)
            {
                _deploymentUI.Initialize();
            }

            // 3. Init Currency (Start with some cash)
            if (_currencyManager != null)
            {
                _currencyManager.AddSeals(50); // Starting cash
            }

            Debug.Log("GameManager: Initialization Complete.");
        }
    }
}
