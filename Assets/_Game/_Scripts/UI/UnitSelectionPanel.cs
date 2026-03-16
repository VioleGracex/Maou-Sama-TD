using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zenject;
using DG.Tweening;

namespace MaouSamaTD.UI
{
    public class UnitSelectionPanel : MonoBehaviour, IUIController
    {
        #region Variables
        [Header("UI Elements")]
        [SerializeField] private GameObject _visualRoot; 
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;
        [SerializeField] private Button _backButton;
        [SerializeField] private Transform _unitListContainer;
        [SerializeField] private Transform _filterContainer;
        [SerializeField] private UnitDetailsPanel _detailsPanel;
        
        [Header("Layout Animation")]
        [SerializeField] private RectTransform _scrollViewRect;
        [SerializeField] private float _paddingTop = 100f;
        [SerializeField] private float _paddingBottom = 0f;
        [SerializeField] private float _expandedPaddingLeft = 0f;
        [SerializeField] private float _squeezedPaddingLeft = 400f;
        [SerializeField] private float _expandedPaddingRight = 0f;
        [SerializeField] private float _squeezedPaddingRight = 120f;
        [SerializeField] private float _animDuration = 0.3f;
        [SerializeField] private DG.Tweening.Ease _animEase = DG.Tweening.Ease.OutQuad;

        private bool _isFilterOpen = true;
        
        [Header("Data")]
        [SerializeField] private GameObject _unitCardPrefab; 



        public enum SortType { Level, Rarity, AcquisitionDate }
        
        [Header("Sorting & Filtering")]
        private SortType _currentSort = SortType.Level;
        private List<MaouSamaTD.Units.UnitClass> _activeClassFilters = new List<MaouSamaTD.Units.UnitClass>();

        private MaouSamaTD.Data.PlayerData _playerData;

        public event System.Action<int, string> OnUnitSelected;
        private int _currentSlotIndex;

        private float _lastClickTime = 0f;
        private MaouSamaTD.UI.MainMenu.UnitCardUI _lastClickedCard = null;
        private bool _isDetailsOpen = false;

        private List<MaouSamaTD.UI.MainMenu.UnitCardUI> _spawnedCards = new List<MaouSamaTD.UI.MainMenu.UnitCardUI>();

        [Header("Multi-Select")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TMPro.TextMeshProUGUI _countText;
        
        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;
        
        private MaouSamaTD.Levels.LevelData _currentLevel;
        private const int MaxSquadSize = 12;

        private bool _isMultiSelectMode = false;
        private List<string> _tempSelectedIds = new List<string>();
        private System.Action<List<string>> _onMultiSelectConfirmed;
        private int _maxMultiSelectLimit = 12;
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (_backButton) _backButton.onClick.AddListener(() => UIFlowManager.Instance.GoBack());
            if (_confirmButton) _confirmButton.onClick.AddListener(OnConfirmClicked);

            _playerData = new MaouSamaTD.Data.PlayerData();
        }
        #endregion

        #region Public Methods

        public void Open()
        {
            if (_visualRoot == null)
            {
                Debug.LogError($"[UIFlow] {gameObject.name} (UnitSelectionPanel) cannot open! _visualRoot is not assigned in the Inspector.");
                return;
            }
            _visualRoot.SetActive(true);
            
            int totalUnits = (MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase != null) ? MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase.AllUnits.Count : 0;
            Debug.Log($"[UnitSelection] Opening Barracks. Total units found: {totalUnits}");
            
            RefreshInventory();
            UpdateScrollRectLayout(false);
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
            ResetState();
        }

        public void OpenMultiSelect(List<string> currentIds, int maxLimit, System.Action<List<string>> onConfirmed)
        {
            _isMultiSelectMode = true;
            _maxMultiSelectLimit = maxLimit;
            _onMultiSelectConfirmed = onConfirmed;
            
            _tempSelectedIds = new List<string>(currentIds);
            _tempSelectedIds.RemoveAll(string.IsNullOrEmpty);

            if (_confirmButton) _confirmButton.gameObject.SetActive(true);

            if (_visualRoot != null) _visualRoot.SetActive(true);
            RefreshInventory();
            UpdateInventorySelectionVisuals();
            UpdateCountText();
        }

        public void ResetState()
        {
            _isMultiSelectMode = false;
            _lastClickedCard = null;
            
            if (_detailsPanel != null) _detailsPanel.Hide();
            _isDetailsOpen = false;
            
            UpdateScrollRectLayout(false);
        }

        public bool RequestClose() => true;

        #endregion

        #region Private Methods
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
                    OnUnitSelected.Invoke(_currentSlotIndex, _lastClickedCard.Data.UniqueID);
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
            if (MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase == null) return;

            // Only show units the player actually owns
            List<string> ownedIDs = new List<string>();
            if (_saveManager != null && _saveManager.CurrentData != null && _saveManager.CurrentData.UnlockedUnits != null)
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

            switch (_currentSort)
            {
                case SortType.Level:
                    filteredUnits.Sort((a, b) => b.Level.CompareTo(a.Level));
                    break;
                case SortType.Rarity:
                    filteredUnits.Sort((a, b) => b.Rarity.CompareTo(a.Rarity));
                    break;
                case SortType.AcquisitionDate:
                    filteredUnits.Sort((a, b) => a.AcquisitionDate.CompareTo(b.AcquisitionDate));
                    break;
            }

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
                         if (_tempSelectedIds.Contains(card.Data.UniqueID))
                         {
                             index = _tempSelectedIds.IndexOf(card.Data.UniqueID);
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
                if (OnUnitSelected != null)
                {
                    OnUnitSelected.Invoke(_currentSlotIndex, card.Data.UniqueID);
                    UIFlowManager.Instance.GoBack();
                }
            }
            else
            {
                _lastClickedCard = card;
                _lastClickTime = Time.unscaledTime;
                
                if (_confirmButton) _confirmButton.gameObject.SetActive(true);
                
                ShowDetails(card);
            }
        }

        private void OnMultiSelectUnitClicked(MaouSamaTD.UI.MainMenu.UnitCardUI card)
        {
             if (_lastClickedCard == card && Time.unscaledTime - _lastClickTime < 0.3f)
             {
                 string id = card.Data.UniqueID;
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
            
            UpdateInventorySelectionVisuals();
        }

        private void UpdateScrollRectLayout(bool animate = true)
        {
            if (_scrollViewRect == null) return;
            DG.Tweening.DOTween.Kill(_scrollViewRect);

            float targetLeft = _isDetailsOpen ? _squeezedPaddingLeft : _expandedPaddingLeft;
            float targetRight = _isFilterOpen ? _squeezedPaddingRight : _expandedPaddingRight;
            
            Vector2 targetMin = new Vector2(targetLeft, _paddingBottom);
            Vector2 targetMax = new Vector2(-targetRight, -_paddingTop);

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
        #endregion
    }
}