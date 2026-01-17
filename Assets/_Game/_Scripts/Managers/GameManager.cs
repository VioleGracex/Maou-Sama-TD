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
    }
}
