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
        [SerializeField] private UnitStatsPanel _statsPanel;
        [SerializeField] private Transform _squadSlotsContainer; // 12 Slots for selected
        [SerializeField] private Transform _inventoryContentTransform; // Inventory Grid Content
        [SerializeField] private ScrollRect _inventoryScrollRect; // The Scroll View

        [SerializeField] private Button _startBattleButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private GameObject _campaignPageObject;
        
        [Header("Prefabs")]
        [SerializeField] private UnitCardUI _unitCardPrefab; // Updated from GameObject
        [SerializeField] private GameObject _emptySlotPrefab; // For the summary row if needed

        [Inject] private GameSelectionState _selectionState;
        [Inject] private SaveManager _saveManager;
        
        private LevelData _currentLevel;
        private List<UnitData> _currentSquad = new List<UnitData>();
        private List<UnitData> _allUnlockedUnits = new List<UnitData>(); // Placeholder cache
        
        // Filters
        private UnitRarity? _filterRarity = null;
        private UnitClass? _filterClass = null;
        
        private const int MaxSquadSize = 12;

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

            _currentSquad.Clear();

            // Check if Level enforces a cohort
            if (level.PremadeCohort != null && level.PremadeCohort.Count > 0)
            {
                // Force load
                _currentSquad.AddRange(level.PremadeCohort);
                SetupUIForPremade();
            }
            else
            {
                // Load player's last cohort or empty
                // For MVP, start empty or random available
                SetupUIForSelection();
            }
        }

        // Filter Methods linked to UI Buttons
        public void SetRarityFilter(int rarityIndex) // -1 for All
        {
            if (rarityIndex < 0) _filterRarity = null;
            else _filterRarity = (UnitRarity)rarityIndex;
            RefreshInventory();
        }

        public void SetClassFilter(int classIndex) // -1 for All
        {
             if (classIndex < 0) _filterClass = null;
             else _filterClass = (UnitClass)classIndex;
             RefreshInventory();
        }

        // --- Logic ---

        private void SetupUIForPremade()
        {
            UpdateSquadVisuals();
            if (_inventoryScrollRect) _inventoryScrollRect.gameObject.SetActive(false);
            if (_statsPanel) _statsPanel.gameObject.SetActive(false);
        }

        private void SetupUIForSelection()
        {
            UpdateSquadVisuals();
            if (_inventoryScrollRect) 
            {
                _inventoryScrollRect.gameObject.SetActive(true);
                // Load units (Mockup: In real app, load from SaveManager -> Addressables/Resources)
                // For now, assume _allUnlockedUnits is populated or empty.
                RefreshInventory();
            }
        }

        private void RefreshInventory()
        {
            if (_inventoryContentTransform == null) return;
            
            // Clear current inventory view
            foreach (Transform child in _inventoryContentTransform) Destroy(child.gameObject);

            // Filter and Spawn
            foreach (var unit in _allUnlockedUnits)
            {
                // Apply Filters
                if (_filterRarity.HasValue && unit.Rarity != _filterRarity.Value) continue;
                if (_filterClass.HasValue && unit.Class != _filterClass.Value) continue;

                var cardObj = Instantiate(_unitCardPrefab, _inventoryContentTransform);
                cardObj.Setup(unit, OnUnitCardClicked);
                
                // Update its initial visual state based on if it's already in the squad
                int index = _currentSquad.IndexOf(unit);
                cardObj.SetSelectionState(index);
            }
        }

        private void UpdateInventorySelectionVisuals()
        {
            if (_inventoryContentTransform == null) return;

            foreach(Transform child in _inventoryContentTransform)
            {
                UnitCardUI card = child.GetComponent<UnitCardUI>();
                if (card != null && card.Data != null)
                {
                    int index = _currentSquad.IndexOf(card.Data);
                    card.SetSelectionState(index);
                }
            }
        }

        private void UpdateSquadVisuals()
        {
             if (_squadSlotsContainer == null) return;
             // Clear
             foreach(Transform child in _squadSlotsContainer) Destroy(child.gameObject);
             
             // Reuse logic to show simple icons row at top (if desired)
             // For now, mirroring the "Selected Units" row at the bottom/top
             for (int i=0; i < MaxSquadSize; i++)
             {
                 if (i < _currentSquad.Count)
                 {
                     var unit = _currentSquad[i];
                     // We can use a simplified small icon for the top bar
                     if (_emptySlotPrefab)
                     {
                        var slot = Instantiate(_emptySlotPrefab, _squadSlotsContainer);
                        Image icon = slot.GetComponent<Image>();
                        if (icon) icon.sprite = unit.UnitIcon;
                        // Click to remove?
                        Button btn = slot.GetComponent<Button>();
                        if(btn) btn.onClick.AddListener(()=>OnSquadUnitClicked(unit));
                     }
                 }
                 else
                 {
                     if (_emptySlotPrefab)
                     {
                         var slot = Instantiate(_emptySlotPrefab, _squadSlotsContainer);
                         Image icon = slot.GetComponent<Image>();
                         if (icon) icon.color = new Color(0,0,0,0.5f); // Empty
                     }
                 }
             }
        }

        private void OnUnitCardClicked(UnitCardUI card)
        {
            UnitData unit = card.Data;
            
            // Show Stats
            if (_statsPanel) _statsPanel.Setup(unit);

            // Toggle Logic
            if (_currentSquad.Contains(unit))
            {
                // Deselect
                _currentSquad.Remove(unit);
            }
            else
            {
                // Select if room
                if (_currentSquad.Count < MaxSquadSize)
                {
                    _currentSquad.Add(unit);
                }
            }

            // Update ALL cards because indices might shift
            UpdateInventorySelectionVisuals();
            UpdateSquadVisuals();
        }

        private void OnSquadUnitClicked(UnitData unit)
        {
             // Show Stats
            if (_statsPanel) _statsPanel.Setup(unit);
            
            // Remove logic
            _currentSquad.Remove(unit);
            UpdateInventorySelectionVisuals();
            UpdateSquadVisuals();
        }

        private void OnStartBattle()
        {
            if (_currentSquad.Count == 0) 
            {
                Debug.LogWarning("Cannot start battle with 0 units!");
                return; 
            }

            _selectionState.SetLevel(_currentLevel);
            _selectionState.SetCohort(_currentSquad);
            
            SceneManager.LoadScene("BattleScene"); 
        }

        private void OnBack()
        {
            if (_panel) _panel.SetActive(false);
            if (_campaignPageObject) _campaignPageObject.SetActive(true);
        }
    }
}
