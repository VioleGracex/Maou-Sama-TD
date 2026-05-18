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

        [Header("Sorting")]
        [SerializeField] private Button _btnSortLevel;
        [SerializeField] private Button _btnSortRarity;
        [SerializeField] private Button _btnSortDate;
        [SerializeField] private Button _btnSortName;

        [Header("Debug")]
        [SerializeField] private bool _debug = true;

        private GenericListView<UnitData, UnitCardUI> _listView;

        public enum SortMode { Level, Rarity, Date, Name }
        private SortMode _currentSortMode = SortMode.Level;

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
        private string _nameFilter = "";
        private TMP_InputField _searchBarField;

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
            
            if (_inspectorPanel != null)
            {
                _inspectorPanel.OnLevelUpRequest += HandleInspectorLevelUpRequest;
                _inspectorPanel.OnPromoteRequest += HandleInspectorPromoteRequest;
            }

            CreateSearchInputField();
            EnsureSortButtonsWired();
            InitializeClassFilters();
        }

        private void EnsureSortButtonsWired()
        {
            if (_visualRoot == null) return;

            // Auto-wire sort buttons if currently null
            if (_btnSortLevel == null || _btnSortRarity == null || _btnSortDate == null || _btnSortName == null)
            {
                var btns = _visualRoot.GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    if (b.name == "BtnSort_Level" || b.name.Contains("SortLevel") || b.name.Contains("Sort_Level"))
                        _btnSortLevel = b;
                    else if (b.name == "BtnSort_Rarity" || b.name.Contains("SortRarity") || b.name.Contains("Sort_Rarity"))
                        _btnSortRarity = b;
                    else if (b.name == "BtnSort_Date" || b.name.Contains("SortDate") || b.name.Contains("Sort_Date"))
                        _btnSortDate = b;
                    else if (b.name == "BtnSort_Name" || b.name.Contains("SortName") || b.name.Contains("Sort_Name"))
                        _btnSortName = b;
                }
            }


            if (_btnSortLevel != null)
            {
                _btnSortLevel.onClick.RemoveAllListeners();
                _btnSortLevel.onClick.AddListener(() => { _currentSortMode = SortMode.Level; RefreshInventory(); UpdateSortButtonVisuals(); });
            }
            if (_btnSortRarity != null)
            {
                _btnSortRarity.onClick.RemoveAllListeners();
                _btnSortRarity.onClick.AddListener(() => { _currentSortMode = SortMode.Rarity; RefreshInventory(); UpdateSortButtonVisuals(); });
            }
            if (_btnSortDate != null)
            {
                _btnSortDate.onClick.RemoveAllListeners();
                _btnSortDate.onClick.AddListener(() => { _currentSortMode = SortMode.Date; RefreshInventory(); UpdateSortButtonVisuals(); });
            }
            if (_btnSortName != null)
            {
                _btnSortName.onClick.RemoveAllListeners();
                _btnSortName.onClick.AddListener(() => { _currentSortMode = SortMode.Name; RefreshInventory(); UpdateSortButtonVisuals(); });
            }

            UpdateSortButtonVisuals();
        }

        public void Open()
        {
            _currentMode = OperationMode.View;
            if (_visualRoot != null)
            {
                _visualRoot.SetActive(true);
                var mainPage = _visualRoot.transform.Find("Main_Page");
                if (mainPage != null) mainPage.gameObject.SetActive(true);
            }
            
            if (transform.parent != null) transform.parent.gameObject.SetActive(true);

            if (_fullScreenInspector != null) _fullScreenInspector.Close();
            
            // Connect inspector close button if not already
            if (_inspectorPanel != null && _inspectorPanel.CloseButton != null)
            {
                _inspectorPanel.CloseButton.onClick.RemoveAllListeners();
                _inspectorPanel.CloseButton.onClick.AddListener(() => _inspectorPanel.Close());
            }

            EnsureSortButtonsWired();
            UpdateMultiSelectUI();

            _nameFilter = "";
            if (_searchBarField != null) _searchBarField.text = "";

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

            if (_visualRoot != null)
            {
                _visualRoot.SetActive(true);
                var mainPage = _visualRoot.transform.Find("Main_Page");
                if (mainPage != null) mainPage.gameObject.SetActive(true);
            }
            // Ensure parent page is active for selection overlay
            if (transform.parent != null) transform.parent.gameObject.SetActive(true);

            if (_fullScreenInspector != null) _fullScreenInspector.Close();
            
            EnsureSortButtonsWired();
            UpdateMultiSelectUI();

            _nameFilter = "";
            if (_searchBarField != null) _searchBarField.text = "";

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

            if (_visualRoot != null)
            {
                _visualRoot.SetActive(true);
                var mainPage = _visualRoot.transform.Find("Main_Page");
                if (mainPage != null) mainPage.gameObject.SetActive(true);
            }
            // Ensure parent page is active for selection overlay
            if (transform.parent != null) transform.parent.gameObject.SetActive(true);

            if (_fullScreenInspector != null) _fullScreenInspector.Close();
            
            EnsureSortButtonsWired();
            UpdateMultiSelectUI();

            _nameFilter = "";
            if (_searchBarField != null) _searchBarField.text = "";

            RefreshInventory();
        }
    
        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            if (_inspectorPanel != null) _inspectorPanel.Close();
            if (_fullScreenInspector != null) _fullScreenInspector.Close();
            
            // Fix: set parent Vassals_Page_UI inactive when closing
            if (transform.parent != null)
            {
                transform.parent.gameObject.SetActive(false);
            }
            
            UpdateScrollRectLayout(false);
        }

        public void Hide()
        {
             if (_visualRoot != null) _visualRoot.SetActive(false);
        }
    
        public bool RequestClose()
        {

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

                        // Apply Name Filter
                        if (!string.IsNullOrEmpty(_nameFilter) && !unit.UnitName.ToLower().Contains(_nameFilter.ToLower()))
                            continue;

                        ownedUnits.Add(unit);
                    }
                }
            }

            // Apply sorting
            if (_currentSortMode == SortMode.Level)
            {
                ownedUnits.Sort((a, b) => {
                    int compare = b.Level.CompareTo(a.Level);
                    if (compare == 0) compare = b.Rarity.CompareTo(a.Rarity);
                    if (compare == 0) compare = (a.UnitName ?? "").CompareTo(b.UnitName ?? "");
                    return compare;
                });
            }
            else if (_currentSortMode == SortMode.Rarity)
            {
                ownedUnits.Sort((a, b) => {
                    int compare = b.Rarity.CompareTo(a.Rarity);
                    if (compare == 0) compare = b.Level.CompareTo(a.Level);
                    if (compare == 0) compare = (a.UnitName ?? "").CompareTo(b.UnitName ?? "");
                    return compare;
                });
            }
            else if (_currentSortMode == SortMode.Date)
            {
                if (_saveManager != null && _saveManager.CurrentData != null)
                {
                    var unlockedList = _saveManager.CurrentData.UnlockedUnits;
                    ownedUnits.Sort((a, b) => {
                        int idxA = GetUnlockedIndex(unlockedList, a);
                        int idxB = GetUnlockedIndex(unlockedList, b);
                        return idxB.CompareTo(idxA); // Newest (latest in list) first
                    });
                }
            }
            else if (_currentSortMode == SortMode.Name)
            {
                ownedUnits.Sort((a, b) => {
                    int compare = (a.UnitName ?? "").CompareTo(b.UnitName ?? "");
                    if (compare == 0) compare = b.Level.CompareTo(a.Level);
                    if (compare == 0) compare = b.Rarity.CompareTo(a.Rarity);
                    return compare;
                });
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
                    OpenInspector(data, 0);
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

        private void OpenInspector(UnitData data, int tabIndex)
        {
            if (_fullScreenInspector != null)
            {
                if (_debug) Debug.Log($"[VassalManager] OpenInspector for {data.UnitName}, Tab {tabIndex}");
                
                // Disable Main_Page to save GPU rendering, draw calls, RAM, and CPU power
                var mainPage = _visualRoot.transform.Find("Main_Page");
                if (mainPage != null)
                {
                    mainPage.gameObject.SetActive(false);
                    if (_debug) Debug.Log("[VassalManager] Deactivated Main_Page to optimize rendering.");
                }

                _fullScreenInspector.SetUnit(data);
                UIFlowManager.Instance.OpenPanel(_fullScreenInspector);
                _fullScreenInspector.SwitchTab(tabIndex);
            }
        }

        private void HandleInspectorLevelUpRequest(UnitData unit)
        {
            if (unit == null) return;
            OpenInspector(unit, 4); // Tab 4 is Level Up / XP
        }

        private void HandleInspectorPromoteRequest(UnitData unit)
        {
            if (unit == null) return;
            OpenInspector(unit, 2); // Tab 2 is Resonance / Promotion
        }

        private int GetUnlockedIndex(List<string> unlockedList, UnitData unit)
        {
            if (unlockedList == null || unit == null) return -1;
            
            for (int i = 0; i < unlockedList.Count; i++)
            {
                string id = unlockedList[i];
                if (string.Equals(unit.UniqueID, id, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(unit.name, id, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(unit.UnitName, id, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(unit.name.Replace("Char_", "").Replace("_UnitData", ""), id, System.StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private void UpdateSortButtonVisuals()
        {
            SetSortButtonColor(_btnSortLevel, _currentSortMode == SortMode.Level);
            SetSortButtonColor(_btnSortRarity, _currentSortMode == SortMode.Rarity);
            SetSortButtonColor(_btnSortDate, _currentSortMode == SortMode.Date);
            SetSortButtonColor(_btnSortName, _currentSortMode == SortMode.Name);
        }

        private void SetSortButtonColor(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = active ? new Color(0.9f, 0.65f, 0.2f, 1f) : new Color(0.15f, 0.15f, 0.2f, 0.8f);
            }
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.color = active ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.8f);
            }
        }

        private void CreateSearchInputField()
        {
            if (_visualRoot == null) return;
            var mainPage = _visualRoot.transform.Find("Main_Page");
            if (mainPage == null) return;

            // Check if already created
            if (mainPage.Find("SearchBarContainer") != null) return;

            // 1. Create Container
            var container = new GameObject("SearchBarContainer", typeof(RectTransform));
            container.transform.SetParent(mainPage, false);
            var rect = container.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-750f, 0f);
            rect.sizeDelta = new Vector2(300f, 60f);

            // 2. Add Background Glassmorphic Style
            var bgImg = container.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
            
            var outline = container.AddComponent<Outline>();
            outline.effectColor = new Color(0.9f, 0.65f, 0.2f, 0.5f);
            outline.effectDistance = new Vector2(1, 1);

            // 3. Add Input Field Component
            _searchBarField = container.AddComponent<TMP_InputField>();

            // 4. Create Text Area (Viewport)
            var textArea = new GameObject("TextArea", typeof(RectTransform));
            textArea.transform.SetParent(container.transform, false);
            var taRect = textArea.GetComponent<RectTransform>();
            taRect.anchorMin = Vector2.zero;
            taRect.anchorMax = Vector2.one;
            taRect.sizeDelta = new Vector2(-20, -10); // padding

            // RectMask2D for clipping
            textArea.AddComponent<RectMask2D>();

            // 5. Create Placeholder Text
            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGo.transform.SetParent(textArea.transform, false);
            var pRect = placeholderGo.GetComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.sizeDelta = Vector2.zero;
            var pText = placeholderGo.AddComponent<TextMeshProUGUI>();
            pText.text = "Search by name...";
            pText.fontSize = 18;
            pText.fontStyle = FontStyles.Italic;
            pText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            pText.alignment = TextAlignmentOptions.MidlineLeft;

            // 6. Create Text Component
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(textArea.transform, false);
            var tRect = textGo.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            var textComp = textGo.AddComponent<TextMeshProUGUI>();
            textComp.fontSize = 18;
            textComp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            textComp.alignment = TextAlignmentOptions.MidlineLeft;

            // Connect input field references
            _searchBarField.textViewport = taRect;
            _searchBarField.textComponent = textComp;
            _searchBarField.placeholder = pText;

            // Wire callback
            _searchBarField.onValueChanged.AddListener((val) =>
            {
                _nameFilter = val;
                RefreshInventory();
            });
        }
    }
}
