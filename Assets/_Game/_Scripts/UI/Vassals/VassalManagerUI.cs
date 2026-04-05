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

namespace MaouSamaTD.UI.Vassals
{
    /// <summary>
    /// Management page for all owned units (Vassals).
    /// Handles inspection, filtering, and standalone selection for squad assignment.
    /// </summary>
    public class VassalManagerUI : MonoBehaviour, IUIController
    {
        public enum OperationMode { View, SingleSelect, MultiSelect }

        [Header("Roots")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private UnitCardUI _cardPrefab; // Use UnitCardUI directly for inventory items
        [SerializeField] private Sprite _removalIcon;
        [SerializeField] private ClassScalingData _classScalingData;
        [SerializeField] private GameObject _filterContainer;
        [SerializeField] private ClassFilterToggleUI _classTogglePrefab;
        [SerializeField] private Transform _classFilterRoot;

        private struct FilterToggleEntry
        {
            public UnitClass? Class;
            public ClassFilterToggleUI Toggle;
        }
        private List<FilterToggleEntry> _filterToggles = new List<FilterToggleEntry>();

        [Header("Selection & Navigation")]
        [SerializeField] private Button _btnConfirmSelection;
        [SerializeField] private Button _btnCancel;

        [Header("Layout Animation")]
        [SerializeField] private RectTransform _scrollViewRect;
        [SerializeField] private float _expandedPaddingLeft = 0f;
        [SerializeField] private float _squeezedPaddingLeft = 400f;
        [SerializeField] private float _paddingTop = 100f;
        [SerializeField] private float _paddingBottom = 0f;

        [Header("Sub Panels")]
        [SerializeField] private VassalDetailPanel _inspectorPanel; // Side Bar
        public UnitInspectorFullScreenUI _fullScreenInspector;

        [Header("Debug")]
        [SerializeField] private bool _debug = true;

        private GenericListView<UnitData, UnitCardUI> _listView;

        // Operational State
        private OperationMode _currentMode = OperationMode.View;
        private System.Action<int, string> _onSingleSelectComplete;
        private int _currentSlotIndex = -1;

        private System.Action<List<string>> _onMultiSelectComplete;
        private List<string> _tempSelectedIds = new List<string>();
        private List<string> _currentCohortUnitIDs = new List<string>(); // Tracks the current squad for highlighting
        private int _maxMultiSelectLimit = 12;

        private UnitClass? _currentClassFilter = null;
        private bool _filtersInitialized = false;

        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true; // Essential for "Back" button to return from selection to squad
        [SerializeField] private NavigationFeatures _navFeatures = NavigationFeatures.BackButton | NavigationFeatures.CitadelButton;
        public NavigationFeatures ConfiguredNavFeatures => _navFeatures;

        public void Awake()
        {
            if (_btnConfirmSelection != null) _btnConfirmSelection.onClick.AddListener(OnConfirmMultiSelection);
            if (_btnCancel != null) _btnCancel.onClick.AddListener(() => UIFlowManager.Instance.GoBack());

            // Clean the container once on Awake to remove design-time artifacts
            if (_cardContainer != null)
            {
                foreach (Transform child in _cardContainer)
                {
                    if (child.gameObject == _cardPrefab.gameObject) continue;
                    Destroy(child.gameObject);
                }
            }

            _listView = new GenericListView<UnitData, UnitCardUI>(_cardContainer, _cardPrefab);
            
            InitializeClassFilters();
        }

        public void Open()
        {
            _currentMode = OperationMode.View;
            if (_visualRoot != null) _visualRoot.SetActive(true);
            
            // Ensure parent page is also active if we are managed by one
            if (transform.parent != null) transform.parent.gameObject.SetActive(true);

            if (_fullScreenInspector != null) _fullScreenInspector.gameObject.SetActive(false);
            
            // Connect inspector close button if not already
            if (_inspectorPanel != null && _inspectorPanel.CloseButton != null)
            {
                _inspectorPanel.CloseButton.onClick.RemoveAllListeners();
                _inspectorPanel.CloseButton.onClick.AddListener(() => _inspectorPanel.Close());
            }

            UpdateMultiSelectUI();
            RefreshInventory();
        }

        public void OpenForSingleSelect(int slotIndex, System.Action<int, string> onComplete, List<string> currentCohort = null)
        {
            _currentMode = OperationMode.SingleSelect;
            _currentSlotIndex = slotIndex;
            _onSingleSelectComplete = onComplete;
            
            // Store the current cohort for highlighting in the inventory
            _currentCohortUnitIDs = currentCohort != null ? new List<string>(currentCohort) : new List<string>();
            
            if (_inspectorPanel != null) _inspectorPanel.SetLayout(true); // Left side for selection

            if (_visualRoot != null) _visualRoot.SetActive(true);
            // Ensure parent page is active for selection overlay
            if (transform.parent != null) transform.parent.gameObject.SetActive(true);

            if (_fullScreenInspector != null) _fullScreenInspector.gameObject.SetActive(false);
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
            // Ensure parent page is active for selection overlay
            if (transform.parent != null) transform.parent.gameObject.SetActive(true);

            if (_fullScreenInspector != null) _fullScreenInspector.gameObject.SetActive(false);
            UpdateMultiSelectUI();
            RefreshInventory();
        }
    
        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            if (_inspectorPanel != null) _inspectorPanel.Close();
            if (_fullScreenInspector != null) _fullScreenInspector.Close();
            // If we are a child of a page, don't necessarily disable the parent unless we are the main page
            UpdateScrollRectLayout(false);
        }

        public void Hide()
        {
             if (_visualRoot != null) _visualRoot.SetActive(false);
        }
    
        public bool RequestClose()
        {
            // If full screen inspector is active, close it and return false to intercept the navigation
            if (_fullScreenInspector != null && _fullScreenInspector.VisualRoot != null && _fullScreenInspector.VisualRoot.activeSelf)
            {
                if (_debug) Debug.Log("[VassalManager] Intercepting GoBack: Closing Full Screen Inspector.");
                _fullScreenInspector.Close();
                return false;
            }

            // If side inspector is open, close it first
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
            // We no longer force _currentMode to View here, as OpenForSingleSelect
            // might be called right before OpenPanel triggers this reset.
            _tempSelectedIds.Clear();
            _currentCohortUnitIDs.Clear();
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
                Debug.Log($"[VassalManagerUI] Preheated {ownedIDs.Count} unit data references.");
            }
        }

        public void InitializeClassFilters()
        {
            if (_filtersInitialized) return;
            if (_classFilterRoot == null || _classTogglePrefab == null || _classScalingData == null) return;

            // Clear all existing children (leftovers from editor or previous runs)
            foreach (Transform child in _classFilterRoot)
            {
                Destroy(child.gameObject);
            }
            _filterToggles.Clear();

            // 1. Create "ALL" Toggle
            CreateFilterToggle(null, null, "ALL");

            // 2. Create class-specific toggles
            foreach (var scaling in _classScalingData.ClassScalings)
            {
                CreateFilterToggle(scaling.ClassType, scaling.ClassIcon);
            }

            _filtersInitialized = true;
            UpdateFilterVisuals();
        }

        private void CreateFilterToggle(UnitClass? classType, Sprite icon, string label = null)
        {
            var filterObj = Instantiate(_classTogglePrefab, _classFilterRoot);
            filterObj.gameObject.name = classType.HasValue ? $"Filter_{classType.Value}" : "Filter_All";

            filterObj.Setup(icon, label);
            filterObj.OnClicked = () => {
                _currentClassFilter = classType;
                UpdateFilterVisuals();
                RefreshInventory();
            };

            _filterToggles.Add(new FilterToggleEntry { Class = classType, Toggle = filterObj });
        }

        private void UpdateFilterVisuals()
        {
            foreach (var entry in _filterToggles)
            {
                if (entry.Toggle != null)
                {
                    entry.Toggle.SetActiveState(entry.Class == _currentClassFilter);
                }
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
                    if (unit != null)
                    {
                        // Apply Filter
                        if (_currentClassFilter.HasValue && unit.Class != _currentClassFilter.Value)
                            continue;

                        ownedUnits.Add(unit);
                    }
                }
            }

            // Insert a "NONE" slot if we are in single-select mode to allow unsetting a cohort slot
            if (_currentMode == OperationMode.SingleSelect)
            {
                ownedUnits.Insert(0, null);
                Debug.Log($"[VassalManagerUI] Inserted NONE card. Total units: {ownedUnits.Count}");
            }
            else
            {
                Debug.Log($"[VassalManagerUI] Skipping NONE card (Mode: {_currentMode}). Total units: {ownedUnits.Count}");
            }

            // Use the optimized list view and ensure stats are fresh per Source of Truth refactor
            _listView.UpdateContent(ownedUnits, OnCardClicked, false, (card, unit) => {
                if (unit == null)
                {
                    card.SetupNone(_removalIcon, (comp) => OnCardClicked(comp as UnitCardUI));
                    card.name = "UnitCard_Removal";
                }
                else
                {
                    // Update scaling right before display to ensure accuracy
                    unit.RefreshStats(_classScalingData);
                    
                    var cardMode = (_currentMode == OperationMode.SingleSelect || _currentMode == OperationMode.MultiSelect) 
                        ? CardInteractionMode.Select 
                        : CardInteractionMode.Inspect;
                        
                    card.Setup(unit, cardMode, (comp) => OnCardClicked(comp as UnitCardUI));
                    card.name = $"UnitCard_{unit.UnitName}";
                }
            });

            UpdateCardSelectionStates();
        }

        private void UpdateMultiSelectUI()
        {
            bool isSelectionMode = _currentMode != OperationMode.View;
            bool isMulti = _currentMode == OperationMode.MultiSelect;
            
            if (_btnConfirmSelection != null) _btnConfirmSelection.gameObject.SetActive(isMulti);
            if (_btnCancel != null) _btnCancel.gameObject.SetActive(isSelectionMode);
        }

        private void UpdateCardSelectionStates()
        {
            foreach (var card in _listView.ActiveItems)
            {
                if (card == null) continue;

                if (card.Data == null)
                {
                    card.SetSelectionState(-1);
                    continue;
                }

                string unitID = card.Data.UniqueID;

                if (_currentMode == OperationMode.MultiSelect)
                {
                    int index = _tempSelectedIds.IndexOf(unitID);
                    card.SetSelectionState(index);
                }
                else if (_currentMode == OperationMode.SingleSelect)
                {
                    // If in single select, show selection number (if in cohort) but hide the checkmark
                    int indexInCohort = _currentCohortUnitIDs.IndexOf(unitID);
                    card.SetSelectionState(indexInCohort, showCheckmark: false);
                }
                else
                {
                    card.SetSelectionState(-1);
                }
            }
        }

        private void OnCardClicked(UnitCardUI cardUI)
        {
            if (cardUI == null) return;
            var data = cardUI.Data;

            if (_currentMode == OperationMode.View)
            {
                if (data == null) return; // Cannot view "None"

                // Always use Full Screen Inspector for View mode, per user refinement
                if (_fullScreenInspector != null)
                {
                    if (_debug) Debug.Log($"[VassalManager] Inspecting unit: {data.UnitName}");
                    _fullScreenInspector.Open(data);
                    // UIFlowManager.Instance.OpenPanel(_fullScreenInspector); // REMOVED: Now managed as a child-state
                }
                else
                {
                    Debug.LogWarning("[VassalManagerUI] Full screen inspector not assigned! Falling back to sidebar.");
                    if (_inspectorPanel != null)
                    {
                        _inspectorPanel.Open(data);
                        // UpdateScrollRectLayout(true); // Disable sidebar animation
                    }
                }
            }
            if (_currentMode == OperationMode.SingleSelect)
            {
                // Direct assignment: skip sidebar inspector and close immediately
                string unitID = data != null ? data.UniqueID : string.Empty;
                Debug.Log($"[VassalManagerUI] SELECTING: '{(data != null ? data.UnitName : "NONE")}' (ID: '{unitID}') for SlotIndex: {_currentSlotIndex}");
                
                _onSingleSelectComplete?.Invoke(_currentSlotIndex, unitID);
                UIFlowManager.Instance.GoBack();
            }
            else if (_currentMode == OperationMode.MultiSelect)
            {
                // Multi-select still uses side panel for quick toggling info if needed, but we keep it simpler
                if (_inspectorPanel != null)
                {
                    _inspectorPanel.Open(data);
                    // UpdateScrollRectLayout(true); // Disable sidebar animation
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
            // Disable sidebar squeeze animation per user request
            return;

            /*
            if (_scrollViewRect == null) return;
            DOTween.Kill(_scrollViewRect);

            float targetLeft = isDetailsOpen ? _squeezedPaddingLeft : _expandedPaddingLeft;
            float targetRight = 0f; 
            
            Vector2 targetMin = new Vector2(targetLeft, _paddingBottom);
            Vector2 targetMax = new Vector2(-targetRight, -_paddingTop);

            DOTween.To(() => _scrollViewRect.offsetMin, x => _scrollViewRect.offsetMin = x, targetMin, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
            DOTween.To(() => _scrollViewRect.offsetMax, x => _scrollViewRect.offsetMax = x, targetMax, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
            */
        }
    }
}
