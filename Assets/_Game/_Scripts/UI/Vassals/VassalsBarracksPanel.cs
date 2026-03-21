using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MaouSamaTD.Data;
using MaouSamaTD.UI.MainMenu;
using Zenject;
using DG.Tweening;

namespace MaouSamaTD.UI.Vassals
{
    /// Management page for all owned units (Vassals).
    /// Handles inspection, leveling, upgrading, single-selection, and multi-selection modes.
    /// </summary>
    public class VassalsBarracksPanel : MonoBehaviour, IUIController
    {
        public enum OperationMode { View, SingleSelect, MultiSelect }

        [Header("Roots")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private UnitCardUI _cardPrefab;
        [SerializeField] private GameObject _sortContainer;
        [SerializeField] private GameObject _filterContainer;
        [SerializeField] private TextMeshProUGUI _unitCountText;

        [Header("Multi-Select UI")]
        [SerializeField] private Button _btnConfirmSelection;
        [SerializeField] private TextMeshProUGUI _multiSelectCountText;

        [Header("Layout Animation")]
        [SerializeField] private RectTransform _scrollViewRect;
        [SerializeField] private float _expandedPaddingLeft = 0f;
        [SerializeField] private float _squeezedPaddingLeft = 400f;
        [SerializeField] private float _paddingTop = 100f;
        [SerializeField] private float _paddingBottom = 0f;

        [Header("Sub Panels")]
        [SerializeField] private VassalDetailPanel _inspectorPanel; // Side Bar
        public UnitInspectorFullScreenUI _fullScreenInspector;

        [Header("Buttons")]
        [SerializeField] private Button _btnLevelUp;
        [SerializeField] private Button _btnPromote;
        [SerializeField] private Button _btnClose;

        private List<UnitCardUI> _spawnedCards = new List<UnitCardUI>();

        // Operational State
        private OperationMode _currentMode = OperationMode.View;
        private System.Action<int, string> _onSingleSelectComplete;
        private int _currentSlotIndex = -1;

        private System.Action<List<string>> _onMultiSelectComplete;
        private List<string> _tempSelectedIds = new List<string>();
        private int _maxMultiSelectLimit = 12;

        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;
        public Button CloseButton => _btnClose;
        public Button LevelUpButton => _btnLevelUp;

        public void Awake()
        {
            if (_btnClose == null)
            {
                var btnTr = transform.Find("Header/Back_MissionReady_Btn");
                if (btnTr != null) _btnClose = btnTr.GetComponent<Button>();
            }

            if (_btnConfirmSelection != null) _btnConfirmSelection.onClick.AddListener(OnConfirmMultiSelection);
        }

        public void Open()
        {
            _currentMode = OperationMode.View;
            if (_visualRoot != null) _visualRoot.SetActive(true);
            
            // Connect inspector close button if not already
            if (_inspectorPanel != null && _inspectorPanel.CloseButton != null)
            {
                _inspectorPanel.CloseButton.onClick.RemoveAllListeners();
                _inspectorPanel.CloseButton.onClick.AddListener(() => _inspectorPanel.Close());
            }

            UpdateMultiSelectUI();
            RefreshInventory();
        }

        public void OpenForSingleSelect(int slotIndex, System.Action<int, string> onComplete)
        {
            _currentMode = OperationMode.SingleSelect;
            _currentSlotIndex = slotIndex;
            _onSingleSelectComplete = onComplete;
            
            if (_inspectorPanel != null) _inspectorPanel.SetLayout(true); // Left side for selection

            if (_visualRoot != null) _visualRoot.SetActive(true);
            UpdateMultiSelectUI();
            RefreshInventory();
        }

        public void OpenForMultiSelect(List<string> currentIds, int maxLimit, System.Action<List<string>> onComplete)
        {
            _currentMode = OperationMode.MultiSelect;
            _maxMultiSelectLimit = maxLimit;
            _onMultiSelectComplete = onComplete;

            if (_inspectorPanel != null) _inspectorPanel.SetLayout(true); // Left side for selection

            _tempSelectedIds = new List<string>(currentIds);
            _tempSelectedIds.RemoveAll(string.IsNullOrEmpty);

            if (_visualRoot != null) _visualRoot.SetActive(true);
            UpdateMultiSelectUI();
            RefreshInventory();
        }
    
        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            if (_inspectorPanel != null) _inspectorPanel.Close();
            UpdateScrollRectLayout(false);
        }
    
        public bool RequestClose()
        {
            // If inspector is open, close it first
            if (_inspectorPanel != null && _inspectorPanel.VisualRoot != null && _inspectorPanel.VisualRoot.activeSelf)
            {
                _inspectorPanel.Close();
                UpdateScrollRectLayout(false);
                return false;
            }

            return true;
        }

        public void ResetState()
        {
            if (_inspectorPanel != null) 
            {
                _inspectorPanel.ResetState();
                _inspectorPanel.SetLayout(false); // Default to right side
            }
            _currentMode = OperationMode.View;
            _tempSelectedIds.Clear();
            _onSingleSelectComplete = null;
            _onMultiSelectComplete = null;
        }

        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;

        public void RefreshInventory()
        {
            if (_cardContainer == null || _cardPrefab == null) return;

            // Get owned units
            List<string> ownedIDs = new List<string>();
            if (_saveManager != null && _saveManager.CurrentData != null)
            {
                ownedIDs = _saveManager.CurrentData.UnlockedUnits;
            }

            // Reuse/Spawn cards
            while (_spawnedCards.Count < ownedIDs.Count)
            {
                var card = Instantiate(_cardPrefab, _cardContainer);
                _spawnedCards.Add(card);
            }

            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                var card = _spawnedCards[i];
                if (i < ownedIDs.Count)
                {
                    var unit = MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase?.GetUnitByID(ownedIDs[i]);
                    if (unit != null)
                    {
                        card.gameObject.SetActive(true);
                        card.Setup(unit, OnCardClicked);
                    }
                    else card.gameObject.SetActive(false);
                }
                else card.gameObject.SetActive(false);
            }

            if (_unitCountText != null)
                _unitCountText.text = $"VASSALS: {ownedIDs.Count}";

            UpdateCardSelectionStates();
        }

        private void UpdateMultiSelectUI()
        {
            bool isMulti = _currentMode == OperationMode.MultiSelect;
            if (_btnConfirmSelection != null) _btnConfirmSelection.gameObject.SetActive(isMulti);
            if (_multiSelectCountText != null) _multiSelectCountText.gameObject.SetActive(isMulti);
            UpdateCountText();
        }

        private void UpdateCountText()
        {
            if (_currentMode == OperationMode.MultiSelect && _multiSelectCountText != null)
            {
                _multiSelectCountText.text = $"{_tempSelectedIds.Count}/{_maxMultiSelectLimit}";
            }
        }

        private void UpdateCardSelectionStates()
        {
            foreach (var card in _spawnedCards)
            {
                if (card.isActiveAndEnabled && card.Data != null)
                {
                    int index = -1;
                    if (_currentMode == OperationMode.MultiSelect && _tempSelectedIds.Contains(card.Data.UniqueID))
                    {
                        index = _tempSelectedIds.IndexOf(card.Data.UniqueID);
                    }
                    card.SetSelectionState(index);
                }
            }
        }

        private void OnCardClicked(UnitCardUI card)
        {
            if (_currentMode == OperationMode.View)
            {
                if (_fullScreenInspector != null)
                {
                    _fullScreenInspector.Open(card.Data);
                    UIFlowManager.Instance.OpenPanel(_fullScreenInspector);
                }
                else if (_inspectorPanel != null)
                {
                    _inspectorPanel.Open(card.Data);
                    UpdateScrollRectLayout(true);
                }
            }
            else if (_currentMode == OperationMode.SingleSelect)
            {
                if (_inspectorPanel != null)
                {
                    _inspectorPanel.Open(card.Data);
                    UpdateScrollRectLayout(true);
                }
                
                // We keep the selection logic separate or maybe add a "Select" button to the side-bar?
                // For now, let's keep the card click as selection too, but show the side-bar.
                _onSingleSelectComplete?.Invoke(_currentSlotIndex, card.Data.UniqueID);
                UIFlowManager.Instance.GoBack();
            }
            else if (_currentMode == OperationMode.MultiSelect)
            {
                if (_inspectorPanel != null)
                {
                    _inspectorPanel.Open(card.Data);
                    UpdateScrollRectLayout(true);
                }

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
                UpdateCardSelectionStates();
                UpdateCountText();
            }
        }

        private void OnConfirmMultiSelection()
        {
            _onMultiSelectComplete?.Invoke(_tempSelectedIds);
            UIFlowManager.Instance.GoBack();
        }

        private void UpdateScrollRectLayout(bool isDetailsOpen)
        {
            if (_scrollViewRect == null) return;
            DOTween.Kill(_scrollViewRect);

            float targetLeft = isDetailsOpen ? _squeezedPaddingLeft : _expandedPaddingLeft;
            float targetRight = 0f; 
            
            Vector2 targetMin = new Vector2(targetLeft, _paddingBottom);
            Vector2 targetMax = new Vector2(-targetRight, -_paddingTop);

            DOTween.To(() => _scrollViewRect.offsetMin, x => _scrollViewRect.offsetMin = x, targetMin, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
            DOTween.To(() => _scrollViewRect.offsetMax, x => _scrollViewRect.offsetMax = x, targetMax, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
        }
    }
}
