using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zenject;
using DG.Tweening;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Standalone Barracks/Gallery for viewing and managing all owned Vassals.
    /// Reuses logic from UnitSelectionPanel but optimized for collection management.
    /// </summary>
    public class VassalBarracksPanel : MonoBehaviour, IUIController
    {
        #region Variables
        [Header("UI Elements")]
        [SerializeField] private GameObject _visualRoot; 
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _homeButton;
        [SerializeField] private Transform _unitListContainer;
        [SerializeField] private Transform _filterContainer;
        
        [Header("Data & Prefabs")]
        [SerializeField] private GameObject _unitCardPrefab;

        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;
        
        private List<MaouSamaTD.UI.MainMenu.UnitCardUI> _spawnedCards = new List<MaouSamaTD.UI.MainMenu.UnitCardUI>();
        
        // Sorting/Filtering state
        public enum SortType { Level, Rarity, AcquisitionDate, Name }
        private SortType _currentSort = SortType.Level;
        private List<MaouSamaTD.Units.UnitClass> _activeClassFilters = new List<MaouSamaTD.Units.UnitClass>();
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (_backButton) _backButton.onClick.AddListener(() => UIFlowManager.Instance.GoBack());
            if (_homeButton) _homeButton.onClick.AddListener(GoToHome);
        }
        #endregion

        #region Public Methods
        public void Open()
        {
            if (_visualRoot == null) return;
            _visualRoot.SetActive(true);
            
            RefreshInventory();
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        public void ResetState()
        {
            // Clear filters or selection state if needed
        }

        public bool RequestClose() => true;
        #endregion

        #region Navigation Logic
        private void GoToHome()
        {
            UIFlowManager.Instance.ClearHistory();
            // Assuming Home is handled by another controller or simply by clearing history
            // and returning to the base state of the MainMenu scene.
        }

        private void OnUnitClicked(MaouSamaTD.UI.MainMenu.UnitCardUI card)
        {
            // Future: Open VassalDetailPanel
            Debug.Log($"[VassalBarracks] Unit clicked: {card.Data.UnitName}. Redirecting to details...");
            // if (_detailPanel != null) _detailPanel.Show(card.Data);
        }
        #endregion

        #region Inventory Logic
        private void RefreshInventory()
        {
            if (_unitListContainer == null) return;
            if (MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase == null) return;

            // Get owned units from SaveManager
            List<string> ownedIDs = new List<string>();
            if (_saveManager != null && _saveManager.CurrentData != null)
            {
                ownedIDs = _saveManager.CurrentData.UnlockedUnits;
            }

            var filteredUnits = new List<MaouSamaTD.Units.UnitData>();
            foreach (var id in ownedIDs)
            {
                var unit = MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase.GetUnitByID(id);
                if (unit == null) continue;
                if (_activeClassFilters.Count == 0 || _activeClassFilters.Contains(unit.Class))
                {
                    filteredUnits.Add(unit);
                }
            }

            // Sort
            switch (_currentSort)
            {
                case SortType.Level:
                    filteredUnits.Sort((a, b) => b.Level.CompareTo(a.Level));
                    break;
                case SortType.Rarity:
                    filteredUnits.Sort((a, b) => b.Rarity.CompareTo(a.Rarity));
                    break;
                case SortType.Name:
                    filteredUnits.Sort((a, b) => a.UnitName.CompareTo(b.UnitName));
                    break;
            }

            // Pool/Instantiate cards
            while (_spawnedCards.Count < filteredUnits.Count)
            {
                var cardObj = Instantiate(_unitCardPrefab, _unitListContainer);
                var cardUI = cardObj.GetComponent<MaouSamaTD.UI.MainMenu.UnitCardUI>();
                if (cardUI != null) _spawnedCards.Add(cardUI);
            }

            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                var card = _spawnedCards[i];
                if (i < filteredUnits.Count)
                {
                    card.gameObject.SetActive(true);
                    card.transform.SetSiblingIndex(i);
                    card.Setup(filteredUnits[i], OnUnitClicked);
                }
                else
                {
                    card.gameObject.SetActive(false);
                }
            }
        }
        #endregion
    }
}
