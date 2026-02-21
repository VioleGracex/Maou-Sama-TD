using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zenject;
using DG.Tweening;

namespace MaouSamaTD.UI
{
    public class UnitSelectionPanel : MonoBehaviour, IUIController
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject _visualRoot; 
        public GameObject VisualRoot => _visualRoot;
        [SerializeField] private Button _backButton;
        [SerializeField] private Transform _unitListContainer;
        [SerializeField] private Transform _filterContainer; // For class icons
        [SerializeField] private UnitDetailsPanel _detailsPanel; // Left side stats/skills panel
        
        [Header("Layout Animation")]
        [SerializeField] private RectTransform _scrollViewRect;
        [SerializeField] private float _paddingTop = 100f;
        [SerializeField] private float _paddingBottom = 0f;
        [SerializeField] private float _expandedPaddingLeft = 0f;
        [SerializeField] private float _squeezedPaddingLeft = 400f; // Width of details panel
        [SerializeField] private float _expandedPaddingRight = 0f;
        [SerializeField] private float _squeezedPaddingRight = 120f; // Width of filter panel
        [SerializeField] private float _animDuration = 0.3f;
        [SerializeField] private DG.Tweening.Ease _animEase = DG.Tweening.Ease.OutQuad;

        // Panel states
        private bool _isFilterOpen = true; // Assume docked by default
        
        [Header("Data")]
        [SerializeField] private GameObject _unitCardPrefab; 

        [Inject] private MaouSamaTD.Managers.GameManager _gameManager; 
        //[Inject] private PlayerData _playerData; // Need access to unlocked units

        public enum SortType { Level, Rarity, AcquisitionDate }
        
        [Header("Sorting & Filtering")]
        private SortType _currentSort = SortType.Level;
        private List<MaouSamaTD.Units.UnitClass> _activeClassFilters = new List<MaouSamaTD.Units.UnitClass>();

        // Temporary
        private MaouSamaTD.Data.PlayerData _playerData;

        public event System.Action<int, string> OnUnitSelected; // SlotIndex, UnitID
        private int _currentSlotIndex;

        // Interaction State
        private float _lastClickTime = 0f;
        private MaouSamaTD.UI.MainMenu.UnitCardUI _lastClickedCard = null;
        private bool _isDetailsOpen = false;



        // Object Pool for Cards
        private List<MaouSamaTD.UI.MainMenu.UnitCardUI> _spawnedCards = new List<MaouSamaTD.UI.MainMenu.UnitCardUI>();

        [Header("Multi-Select")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TMPro.TextMeshProUGUI _countText; // Optional: "Selected: 3/12"
        
        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;
        
        private MaouSamaTD.Levels.LevelData _currentLevel;
        private const int MaxSquadSize = 12;

        // Multi-Select State
        private bool _isMultiSelectMode = false;
        private List<string> _tempSelectedIds = new List<string>();
        private System.Action<List<string>> _onMultiSelectConfirmed;
        private int _maxMultiSelectLimit = 12;

        private void Start()
        {
            if (_backButton) _backButton.onClick.AddListener(() => UIFlowManager.Instance.GoBack());
            if (_confirmButton) _confirmButton.onClick.AddListener(OnConfirmClicked);

            // Mock PlayerData
            _playerData = new MaouSamaTD.Data.PlayerData();
        }

        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            
            RefreshInventory();
            UpdateScrollRectLayout(false); // Snap to correct padding instantly
        }

        public void Open(int slotIndex, System.Action<int, string> onUnitSelected)
        {
            _currentSlotIndex = slotIndex;
            OnUnitSelected = null;
            if (onUnitSelected != null)
            {
                OnUnitSelected += onUnitSelected;
            }
            if (_visualRoot != null) _visualRoot.SetActive(true);
            
            RefreshInventory();
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            _isMultiSelectMode = false;
            
            if (_detailsPanel != null) _detailsPanel.Hide();
            _isDetailsOpen = false;
            _lastClickedCard = null;
            
            UpdateScrollRectLayout(false);
        }

        public void OpenMultiSelect(List<string> currentIds, int maxLimit, System.Action<List<string>> onConfirmed)
        {
            _isMultiSelectMode = true;
            _maxMultiSelectLimit = maxLimit;
            _onMultiSelectConfirmed = onConfirmed;
            
            _tempSelectedIds = new List<string>(currentIds);
            // Remove null/empty
            _tempSelectedIds.RemoveAll(string.IsNullOrEmpty);

            if (_confirmButton) _confirmButton.gameObject.SetActive(true);

            if (_visualRoot != null) _visualRoot.SetActive(true);
            RefreshInventory();
            UpdateInventorySelectionVisuals();
            UpdateCountText();
        }

        private void OnConfirmClicked()
        {
            if (_isMultiSelectMode)
            {
                _onMultiSelectConfirmed?.Invoke(_tempSelectedIds);
                UIFlowManager.Instance.GoBack();
            }
            else
            {
                if (_lastClickedCard != null && OnUnitSelected != null)
                {
                    OnUnitSelected.Invoke(_currentSlotIndex, _lastClickedCard.Data.UnitID);
                    Close();
                }
            }
        }
        
        private void UpdateCountText()
        {
            if (_countText != null) 
                _countText.text = $"{_tempSelectedIds.Count}/{_maxMultiSelectLimit}";
        }
        

        public void SetSortField(int sortIndex)
        {
            if (System.Enum.IsDefined(typeof(SortType), sortIndex))
            {
                _currentSort = (SortType)sortIndex;
                RefreshInventory();
            }
        }

        public void ToggleClassFilter(MaouSamaTD.Units.UnitClass unitClass)
        {
            if (_activeClassFilters.Contains(unitClass))
            {
                _activeClassFilters.Remove(unitClass);
            }
            else
            {
                _activeClassFilters.Add(unitClass);
            }
            RefreshInventory();
        }

        private void RefreshInventory()
        {
            if (_unitListContainer == null) return;
            if (_gameManager == null || _gameManager.UnitDatabase == null) return;

            // 1. Filter
            var filteredUnits = new List<MaouSamaTD.Units.UnitData>();
            foreach (var unit in _gameManager.UnitDatabase.AllUnits)
            {
                if (unit == null) continue;
                if (_activeClassFilters.Count == 0 || _activeClassFilters.Contains(unit.Class))
                {
                    filteredUnits.Add(unit);
                }
            }

            // 2. Sort
            switch (_currentSort)
            {
                case SortType.Level:
                    filteredUnits.Sort((a, b) => b.Level.CompareTo(a.Level)); // Descending
                    break;
                case SortType.Rarity:
                    filteredUnits.Sort((a, b) => b.Rarity.CompareTo(a.Rarity)); // Descending
                    break;
                case SortType.AcquisitionDate:
                    filteredUnits.Sort((a, b) => a.AcquisitionDate.CompareTo(b.AcquisitionDate)); // Ascending
                    break;
            }

            // 3. Pool Assignment
            // Ensure we have enough physical cards spawned
            while (_spawnedCards.Count < filteredUnits.Count)
            {
                var cardObj = Instantiate(_unitCardPrefab, _unitListContainer);
                var cardUI = cardObj.GetComponent<MaouSamaTD.UI.MainMenu.UnitCardUI>();
                if (cardUI != null) _spawnedCards.Add(cardUI);
            }

            // Setup active cards and hide the rest
            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                var card = _spawnedCards[i];
                if (i < filteredUnits.Count)
                {
                    card.gameObject.SetActive(true);
                    card.transform.SetSiblingIndex(i); // Force visual sort order
                    
                    if (_isMultiSelectMode)
                        card.Setup(filteredUnits[i], OnMultiSelectUnitClicked);
                    else
                        card.Setup(filteredUnits[i], OnUnitCardClicked);
                }
                else
                {
                    card.gameObject.SetActive(false);
                }
            }

            UpdateInventorySelectionVisuals();
        }

        private void UpdateInventorySelectionVisuals()
        {
             if (_unitListContainer == null) return;

             foreach(Transform child in _unitListContainer)
             {
                 MaouSamaTD.UI.MainMenu.UnitCardUI card = child.GetComponent<MaouSamaTD.UI.MainMenu.UnitCardUI>();
                 if (card != null && card.Data != null)
                 {
                     int index = -1;
                     if (_isMultiSelectMode)
                     {
                         if (_tempSelectedIds.Contains(card.Data.UnitID))
                         {
                             index = _tempSelectedIds.IndexOf(card.Data.UnitID);
                         }
                     }
                     card.SetSelectionState(index);
                 }
             }
        }

        private void OnUnitCardClicked(MaouSamaTD.UI.MainMenu.UnitCardUI card)
        {
            if (_lastClickedCard == card && Time.unscaledTime - _lastClickTime < 0.3f)
            {
                // Double Click -> Confirm Selection Immediately
                if (OnUnitSelected != null)
                {
                    OnUnitSelected.Invoke(_currentSlotIndex, card.Data.UnitID);
                    UIFlowManager.Instance.GoBack();
                }
            }
            else
            {
                // Single Click -> View Details
                _lastClickedCard = card;
                _lastClickTime = Time.unscaledTime;
                
                if (_confirmButton) _confirmButton.gameObject.SetActive(true); // Ensure confirm is visible to click it instead
                
                ShowDetails(card);
            }
        }

        private void OnMultiSelectUnitClicked(MaouSamaTD.UI.MainMenu.UnitCardUI card)
        {
             if (_lastClickedCard == card && Time.unscaledTime - _lastClickTime < 0.3f)
             {
                 // Double Click -> Toggle inside Temp Array
                 string id = card.Data.UnitID;
                 if (_tempSelectedIds.Contains(id))
                 {
                     _tempSelectedIds.Remove(id);
                 }
                 else
                 {
                     if (_tempSelectedIds.Count < _maxMultiSelectLimit)
                     {
                         _tempSelectedIds.Add(id);
                     }
                 }
                 UpdateInventorySelectionVisuals();
                 UpdateCountText();
             }
             else
             {
                 // Single Click -> View Details
                 _lastClickedCard = card;
                 _lastClickTime = Time.unscaledTime;
                 
                 ShowDetails(card);
             }
        }

        private void ShowDetails(MaouSamaTD.UI.MainMenu.UnitCardUI card)
        {
            if (card == null || card.Data == null) return;

            if (_detailsPanel != null)
            {
                _detailsPanel.Show(card.Data);
            }

            if (!_isDetailsOpen && _scrollViewRect != null)
            {
                _isDetailsOpen = true;
                UpdateScrollRectLayout(true);
            }
            
            // Optionally, update visuals to show which one is currently "viewed"
            UpdateInventorySelectionVisuals();
        }

        private void UpdateScrollRectLayout(bool animate = true)
        {
            if (_scrollViewRect == null) return;
            DG.Tweening.DOTween.Kill(_scrollViewRect);

            float targetLeft = _isDetailsOpen ? _squeezedPaddingLeft : _expandedPaddingLeft;
            float targetRight = _isFilterOpen ? _squeezedPaddingRight : _expandedPaddingRight;
            
            Vector2 targetMin = new Vector2(targetLeft, _paddingBottom); // offsetMin = (Left, Bottom)
            Vector2 targetMax = new Vector2(-targetRight, -_paddingTop);   // offsetMax = (-Right, -Top)

            if (animate)
            {
                DG.Tweening.DOTween.To(() => _scrollViewRect.offsetMin, x => _scrollViewRect.offsetMin = x, targetMin, _animDuration).SetEase(_animEase).SetUpdate(true);
                DG.Tweening.DOTween.To(() => _scrollViewRect.offsetMax, x => _scrollViewRect.offsetMax = x, targetMax, _animDuration).SetEase(_animEase).SetUpdate(true);
            }
            else
            {
                _scrollViewRect.offsetMin = targetMin;
                _scrollViewRect.offsetMax = targetMax;
            }
        }
    }
}