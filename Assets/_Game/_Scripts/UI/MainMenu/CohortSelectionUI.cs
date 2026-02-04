using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MaouSamaTD.Levels;
using MaouSamaTD.Units;
using MaouSamaTD.Managers;
using Zenject;
using UnityEngine.SceneManagement;

namespace MaouSamaTD.UI.MainMenu
{
    public class CohortSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _unitSlotsContainer; // Where selected units go
        [SerializeField] private Transform _unitInventoryContainer; // Where available units go
        [SerializeField] private Button _startBattleButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private GameObject _campaignPageObject;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject _unitSlotPrefab; // A simple icon wrapper

        [Inject] private GameSelectionState _selectionState;
        [Inject] private SaveManager _saveManager;
        
        private LevelData _currentLevel;
        private List<UnitData> _currentCohort = new List<UnitData>();
        
        // Hardcoded for now, or from GameSettings
        private const int MaxCohortSize = 4;

        private void Start()
        {
           if (_startBattleButton) _startBattleButton.onClick.AddListener(OnStartBattle);
           if (_backButton) _backButton.onClick.AddListener(OnBack);
           if (_panel) _panel.SetActive(false);
        }

        public void Open(LevelData level)
        {
            _currentLevel = level;
            if (_panel) _panel.SetActive(true);
            
            if (_campaignPageObject) _campaignPageObject.SetActive(false);

            _currentCohort.Clear();

            // Check if Level enforces a cohort
            if (level.PremadeCohort != null && level.PremadeCohort.Count > 0)
            {
                // Force load
                _currentCohort.AddRange(level.PremadeCohort);
                SetupUIForPremade();
            }
            else
            {
                // Load player's last cohort or empty
                // For MVP, start empty or random available
                SetupUIForSelection();
            }
        }

        private void SetupUIForPremade()
        {
            // Display units but disable removal/adding
            UpdateSlotVisuals();
            // Hide inventory or disable it
            if (_unitInventoryContainer) _unitInventoryContainer.gameObject.SetActive(false);
        }

        private void SetupUIForSelection()
        {
            UpdateSlotVisuals();
            if (_unitInventoryContainer) 
            {
                _unitInventoryContainer.gameObject.SetActive(true);
                RefreshInventory();
            }
        }

        private void RefreshInventory()
        {
            // Populate _unitInventoryContainer with unlocked units from SaveManager
            // For now, we assume we have a list of AllUnits in a ScriptableObject registry or similar to lookup from IDs.
            // Since we don't have that valid registry yet, we will skip implementation or use a placeholder.
            // TODO: Implement UnitRegistry to fetch UnitData from IDs in SaveManager.UnlockedUnits
        }

        private void UpdateSlotVisuals()
        {
            if (_unitSlotsContainer == null) return;

            // Clear
            foreach (Transform child in _unitSlotsContainer) Destroy(child.gameObject);

            // Spawn
            foreach (var unit in _currentCohort)
            {
                var slot = Instantiate(_unitSlotPrefab, _unitSlotsContainer);
                // Setup slot icon...
            }
        }

        private void OnStartBattle()
        {
            if (_currentCohort.Count == 0) 
            {
                Debug.LogWarning("Cannot start battle with 0 units!");
                return; 
            }

            _selectionState.SetLevel(_currentLevel);
            _selectionState.SetCohort(_currentCohort);
            
            SceneManager.LoadScene("BattleScene"); 
        }

        private void OnBack()
        {
            if (_panel) _panel.SetActive(false);
            if (_campaignPageObject) _campaignPageObject.SetActive(true);
        }
    }
}
