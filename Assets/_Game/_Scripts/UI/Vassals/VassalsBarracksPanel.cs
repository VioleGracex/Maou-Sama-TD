using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MaouSamaTD.Data;
using MaouSamaTD.UI.MainMenu;

namespace MaouSamaTD.UI.Vassals
{
    /// <summary>
    /// Management page for all owned units (Vassals).
    /// Handles inspection, leveling, and upgrading.
    /// </summary>
    public class VassalsBarracksPanel : MonoBehaviour, IUIController
    {
        [Header("Roots")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private UnitCardUI _cardPrefab;
        [SerializeField] private GameObject _sortContainer;
        [SerializeField] private GameObject _filterContainer;
        [SerializeField] private TextMeshProUGUI _unitCountText;

        [Header("Sub Panels")]
        [SerializeField] private UnitInspectorPanel _inspectorPanel;

        [Header("Buttons")]
        [SerializeField] private Button _btnLevelUp;
        [SerializeField] private Button _btnPromote;
        [SerializeField] private Button _btnClose;

        private List<UnitCardUI> _spawnedCards = new List<UnitCardUI>();

        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;
        public Button CloseButton => _btnClose;
        public Button LevelUpButton => _btnLevelUp;

        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            
            // Connect inspector close button if not already
            if (_inspectorPanel != null && _inspectorPanel.CloseButton != null)
            {
                _inspectorPanel.CloseButton.onClick.RemoveAllListeners();
                _inspectorPanel.CloseButton.onClick.AddListener(() => _inspectorPanel.Close());
            }

            RefreshInventory();
        }
    
        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            if (_inspectorPanel != null) _inspectorPanel.Close();
        }
    
        public bool RequestClose()
        {
            // If inspector is open, close it first
            if (_inspectorPanel != null && _inspectorPanel.VisualRoot != null && _inspectorPanel.VisualRoot.activeSelf)
            {
                _inspectorPanel.Close();
                return false;
            }

            UIFlowManager.Instance.GoBack();
            return true;
        }

        public void ResetState()
        {
            if (_inspectorPanel != null) _inspectorPanel.ResetState();
        }

        public void RefreshInventory()
        {
            // Clear existing
            foreach (var card in _spawnedCards)
            {
                if (card != null) card.gameObject.SetActive(false);
            }

            // Logic to fetch units from PlayerData and spawn/reuse cards
            // This will be expanded in the next steps
            Debug.Log("[VassalsBarracksPanel] Refreshing Inventory...");
        }

        private void OnCardClicked(UnitCardUI card)
        {
            if (_inspectorPanel != null)
            {
                _inspectorPanel.Open(card.Data);
            }
        }
    }
}
