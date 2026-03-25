using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MaouSamaTD.Data;
using MaouSamaTD.UI.MainMenu;
using Zenject;
using DG.Tweening;
using MaouSamaTD.Units;
using MaouSamaTD.UI;
using MaouSamaTD.UI.Vassals;
using MaouSamaTD.UI.Common;

namespace MaouSamaTD.UI.Cohorts
{
    /// <summary>
    /// Management page for all owned units (formerly Vassals, now referred to as Cohort Inventory).
    /// Handles inspection, leveling, upgrading, and selection for squad assignment.
    /// </summary>
    public class CohortManagerUI : MonoBehaviour, IUIController
    {
        public enum OperationMode { View, SingleSelect, MultiSelect }

        [Header("Roots")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private UnitCardUI _cardPrefab; // Use UnitCardUI directly for inventory items
        [SerializeField] private ClassScalingData _classScalingData;
        [SerializeField] private GameObject _filterContainer;

        [Header("Multi-Select UI")]
        [SerializeField] private Button _btnConfirmSelection;

        [Header("Layout Animation")]
        [SerializeField] private RectTransform _scrollViewRect;
        [SerializeField] private float _expandedPaddingLeft = 0f;
        [SerializeField] private float _squeezedPaddingLeft = 400f;
        [SerializeField] private float _paddingTop = 100f;
        [SerializeField] private float _paddingBottom = 0f;

        [Header("Sub Panels")]
        [SerializeField] private VassalDetailPanel _inspectorPanel; // Side Bar
        public UnitInspectorFullScreenUI _fullScreenInspector;

        private GenericListView<UnitData, UnitCardUI> _listView;

        // Operational State
        private OperationMode _currentMode = OperationMode.View;
        private System.Action<int, string> _onSingleSelectComplete;
        private int _currentSlotIndex = -1;

        private System.Action<List<string>> _onMultiSelectComplete;
        private List<string> _tempSelectedIds = new List<string>();
        private int _maxMultiSelectLimit = 12;

        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;

        public void Awake()
        {
            if (_btnConfirmSelection != null) _btnConfirmSelection.onClick.AddListener(OnConfirmMultiSelection);
            _listView = new GenericListView<UnitData, UnitCardUI>(_cardContainer, _cardPrefab);
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

        public void Preheat()
        {
            // Pre-load owned units from save
            if (_saveManager != null && _saveManager.CurrentData != null && MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase != null)
            {
                var ownedIDs = _saveManager.CurrentData.UnlockedUnits;
                foreach (var id in ownedIDs)
                {
                    // Accessing the database ensures the SOs are referenced/loaded if they weren't already
                    MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase.GetUnitByID(id);
                }
                Debug.Log($"[CohortManagerUI] Preheated {ownedIDs.Count} unit data references.");
            }
        }

        public void RefreshInventory()
        {
            if (_cardContainer == null || _cardPrefab == null) return;

            // Get owned units
            List<UnitData> ownedUnits = new List<UnitData>();
            if (_saveManager != null && _saveManager.CurrentData != null && MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase != null)
            {
                foreach (var id in _saveManager.CurrentData.UnlockedUnits)
                {
                    var unit = MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase.GetUnitByID(id);
                    if (unit != null) ownedUnits.Add(unit);
                }
            }

            // Use the optimized list view
            _listView.UpdateContent(ownedUnits, OnCardClicked);

            UpdateCardSelectionStates();
        }

        private void UpdateMultiSelectUI()
        {
            bool isMulti = _currentMode == OperationMode.MultiSelect;
            if (_btnConfirmSelection != null) _btnConfirmSelection.gameObject.SetActive(isMulti);
        }

        private void UpdateCardSelectionStates()
        {
            foreach (var card in _listView.ActiveItems)
            {
                if (card.Data != null)
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

        private void OnCardClicked(UnitCardUI cardUI)
        {
            if (cardUI == null || cardUI.Data == null) return;
            var data = cardUI.Data;

            if (_currentMode == OperationMode.View)
            {
                if (_fullScreenInspector != null)
                {
                    _fullScreenInspector.Open(data);
                    UIFlowManager.Instance.OpenPanel(_fullScreenInspector);
                }
                else if (_inspectorPanel != null)
                {
                    _inspectorPanel.Open(data);
                    UpdateScrollRectLayout(true);
                }
            }
            else if (_currentMode == OperationMode.SingleSelect)
            {
                if (_inspectorPanel != null)
                {
                    _inspectorPanel.Open(data);
                    UpdateScrollRectLayout(true);
                }
                
                _onSingleSelectComplete?.Invoke(_currentSlotIndex, data.UniqueID);
                UIFlowManager.Instance.GoBack();
            }
            else if (_currentMode == OperationMode.MultiSelect)
            {
                if (_inspectorPanel != null)
                {
                    _inspectorPanel.Open(data);
                    UpdateScrollRectLayout(true);
                }

                string id = data.UniqueID;
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
