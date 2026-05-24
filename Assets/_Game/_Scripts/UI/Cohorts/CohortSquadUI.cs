using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using MaouSamaTD.Data;
using MaouSamaTD.Skills;
using MaouSamaTD.UI.MainMenu;
using Assets.SimpleLocalization.Scripts;
using Zenject;
using MaouSamaTD.UI.Cohorts;
using MaouSamaTD.UI;
using MaouSamaTD.UI.Vassals;
using MaouSamaTD.Core;

namespace MaouSamaTD.UI.Cohorts
{
    /// <summary>
    /// Standalone team/loadout editor. Manages the 12 squad slots for a cohort.
    /// Includes "Dirty State" tracking and unsaved changes prompts.
    /// </summary>
    public class CohortSquadUI : MonoBehaviour, IUIController
    {
        #region Variables
        [Header("UI Controller Architecture")]
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;
        [SerializeField] private NavigationFeatures _navFeatures = NavigationFeatures.BackButton | NavigationFeatures.CitadelButton;
        public NavigationFeatures ConfiguredNavFeatures => _navFeatures;
        [SerializeField] private TMPro.TextMeshProUGUI _titleText;
        [SerializeField] private VassalManagerUI _vassalInventoryController;

        [Header("Cohort Slots")]
        [SerializeField] private List<CohortSlot> _squadSlots = new List<CohortSlot>();
        [SerializeField] private MaouSamaTD.Units.ClassScalingData _classScalingData;

        [Header("Cohort Selection")]
        [SerializeField] private Button[] _cohortButtons;
        
        [Header("Actions")]
        public Button _actionButton; // Unified Save / Start Battle
        public TMPro.TextMeshProUGUI _actionButtonText;
        [SerializeField] private Button _removeAllButton;
        public Button _autoMakeSquadButton;

        [Header("Locked Mode")]
        [SerializeField] private GameObject _noEditBlocker;
        [SerializeField] private Button _selectMultipleButton;

        [Header("Unsaved Changes Popup")]
        [SerializeField] private GameObject _unsavedChangesPopup;
        [SerializeField] private Button _confirmLeaveButton;
        [SerializeField] private Button _cancelLeaveButton;

        [Header("Sovereign Rites Tab")]
        [SerializeField] private Button _vassalsTabButton;
        [SerializeField] private Button _ritesTabButton;
        [SerializeField] private GameObject _vassalsPanel;
        [SerializeField] private GameObject _ritesPanel;
        [SerializeField] private GameObject _ritesTabBlocker; // View-only lock overlay on the rites tab
        [SerializeField] private List<CohortRiteSlot> _riteSlots = new List<CohortRiteSlot>();
        [SerializeField] private RectTransform _availableRitesContainer;
        [SerializeField] private GameObject _riteItemPrefab;
        
        [Header("Rites Filtering")]
        [SerializeField] private TMPro.TMP_InputField _searchField;
        [SerializeField] private TMPro.TMP_Dropdown _sortDropdown;
        [SerializeField] private UnityEngine.UI.Button[] _filterButtons;

        private string _activeRiteFilter = "All";
        private bool _isRitesLocked = false;

        [Header("Button Colors")]
        [SerializeField] private Color _highlightColor = new Color(1f, 0.82f, 0.12f); // Gold/Yellow
        [SerializeField] private Color _normalColor = Color.white;
        
        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;
        [Inject] private MaouSamaTD.Managers.GameSelectionState _selectionState;

        private MaouSamaTD.Levels.LevelData _currentLevel;
        private MaouSamaTD.Data.PlayerData _playerData;
        private int _viewingCohortIndex = 0;
        private List<string> _tempUnitIDs = new List<string>();
        private bool _isDirty = false;
        private bool _isReadinessMode = false;
        private bool _isLockedMode = false;
        #endregion

        #region Unity Methods
        private bool _initialized = false;

        private void Awake()
        {
            if (_vassalInventoryController == null)
            {
                _vassalInventoryController = GetComponentInChildren<VassalManagerUI>(true);
            }
        }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            if (_actionButton != null) _actionButton.onClick.AddListener(OnActionButtonClicked);
            if (_removeAllButton != null) _removeAllButton.onClick.AddListener(OnRemoveAllClicked);
            if (_autoMakeSquadButton != null) _autoMakeSquadButton.onClick.AddListener(OnAutoMakeSquadClicked);
            if (_selectMultipleButton != null) _selectMultipleButton.onClick.AddListener(OnSelectMultipleClicked);
            
            if (_confirmLeaveButton != null) _confirmLeaveButton.onClick.AddListener(OnConfirmLeave);
            if (_cancelLeaveButton != null) _cancelLeaveButton.onClick.AddListener(OnCancelLeave);

            if (_vassalsTabButton != null) _vassalsTabButton.onClick.AddListener(() => SwitchTab(true));
            if (_ritesTabButton != null) _ritesTabButton.onClick.AddListener(() => SwitchTab(false));

            for (int i = 0; i < _riteSlots.Count; i++)
            {
                int index = i;
                if (_riteSlots[i] != null)
                {
                    _riteSlots[i].OnRiteDropped += (slotIdx, data) => OnRiteDroppedInSlot(slotIdx, data);
                    _riteSlots[i].OnRiteCleared += (slotIdx) => OnRiteClearedFromSlot(slotIdx);
                }
            }

            InitializeData();
            SetupSlots();
            SetupCohortButtons();
        }
        #endregion

        #region Public Methods
        public void Open()
        {
            if (_visualRoot == null) return;
            _visualRoot.SetActive(true);
            
            _isRitesLocked = false;
            if (_ritesTabBlocker != null) _ritesTabBlocker.SetActive(false);
            SwitchTab(true); // Default to Vassals tab

            if (_titleText != null && !_isReadinessMode) 
                _titleText.text = LocalizationManager.Localize("Cohort.Title.Default");

            UpdateButtonsInteractable();
            RefreshUI();
        }

        public void OpenReadiness(MaouSamaTD.Levels.LevelData level)
        {
            if (_visualRoot == null) return;
            _visualRoot.SetActive(true);

            _isReadinessMode = true;
            _currentLevel = level;
            
            if (_titleText != null)
            {
                if (level != null)
                {
                    // LevelName could be a localization key or a literal name; Localize handles both
                    _titleText.text = LocalizationManager.Localize(level.LevelName); 
                }
                else
                {
                    _titleText.text = LocalizationManager.Localize("Cohort.Title.Readiness");
                }
            }

            InitializeData();

            _isLockedMode = level != null && level.IsCohortLocked;
            _isRitesLocked = level != null && level.IsRitesLocked;
            
            if (_ritesTabBlocker != null)
                _ritesTabBlocker.SetActive(_isRitesLocked);

            SwitchTab(true); // Default to Vassals tab

            bool hasPremade = level != null && level.PremadeCohort != null && level.PremadeCohort.Count > 0;

            if (hasPremade)
            {
                LoadPremadeCohort();
                _viewingCohortIndex = -1;
            }
            else
            {
                _viewingCohortIndex = _playerData.CurrentCohortIndex;
                LoadCohortToTemp(_viewingCohortIndex);
            }

            if (_noEditBlocker != null)
                _noEditBlocker.SetActive(_isLockedMode);

            UpdateButtonsInteractable();
            RefreshUI();
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            if (_unsavedChangesPopup != null) _unsavedChangesPopup.SetActive(false);
            
            // If we are closing the squad page, we should hide the inner selection panel too
            if (_vassalInventoryController != null) _vassalInventoryController.Hide();
        }

        public void Preheat()
        {
            InitializeData();
            LoadCohortToTemp(_playerData.CurrentCohortIndex);
            Debug.Log("[CohortSquadUI] Preheated cohort data and squad slots.");
        }

        public void ResetState()
        {
            _isDirty = false;
            _isReadinessMode = false;
            _isLockedMode = false;
            _isRitesLocked = false;
            if (_noEditBlocker != null) _noEditBlocker.SetActive(false);
            if (_ritesTabBlocker != null) _ritesTabBlocker.SetActive(false);
            
            InitializeData();
            LoadCohortToTemp(_playerData.CurrentCohortIndex);
            SwitchTab(true); // Reset to Vassals tab
        }

        public bool RequestClose()
        {
            if (_isDirty)
            {
                if (_unsavedChangesPopup != null)
                {
                    _unsavedChangesPopup.SetActive(true);
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region Private Methods
        private void InitializeData()
        {
            if (_saveManager != null)
            {
                _playerData = _saveManager.CurrentData;
            }

            if (_playerData == null)
            {
                _playerData = new MaouSamaTD.Data.PlayerData();
            }

            if (_playerData.Cohorts == null)
            {
                _playerData.Cohorts = new List<MaouSamaTD.Data.CohortData>();
            }

            if (_playerData.Cohorts.Count < 4)
            {
                int needed = 4 - _playerData.Cohorts.Count;
                for (int i = 0; i < needed; i++)
                {
                    _playerData.Cohorts.Add(new MaouSamaTD.Data.CohortData($"Cohort {_playerData.Cohorts.Count + 1}"));
                }
            }

            if (_playerData.CurrentCohortIndex < 0 || _playerData.CurrentCohortIndex >= _playerData.Cohorts.Count)
            {
                _playerData.CurrentCohortIndex = 0;
            }
        }

        private void SetupSlots()
        {
            for (int i = 0; i < _squadSlots.Count; i++)
            {
                var slot = _squadSlots[i];
                if (slot != null)
                {
                    slot.SetIndex(i);
                    slot.OnClick -= OnSlotClicked;
                    slot.OnClick += OnSlotClicked;
                }
            }
        }

        private void SetupCohortButtons()
        {
            if (_cohortButtons == null) return;
            for (int i = 0; i < _cohortButtons.Length; i++)
            {
                int index = i;
                if (_cohortButtons[i] != null)
                   _cohortButtons[i].onClick.AddListener(() => OnCohortButtonClicked(index));
            }
        }

        private void LoadCohortToTemp(int index)
        {
            _viewingCohortIndex = index;
            if (_playerData != null) _playerData.CurrentCohortIndex = index;
            
            var cohort = _playerData.Cohorts[index];
            _tempUnitIDs = new List<string>(cohort.UnitIDs);
            
            while (_tempUnitIDs.Count < 12) _tempUnitIDs.Add("");
            
            _isDirty = false;
            UpdateSaveButtonState();
        }

        private void LoadPremadeCohort()
        {
            _tempUnitIDs.Clear();
            if (_currentLevel == null || _currentLevel.PremadeCohort == null) return;

            foreach (var unit in _currentLevel.PremadeCohort)
            {
                if (unit == null) _tempUnitIDs.Add("");
                else _tempUnitIDs.Add(string.IsNullOrEmpty(unit.UniqueID) ? unit.name : unit.UniqueID);
            }
            while (_tempUnitIDs.Count < 12) _tempUnitIDs.Add("");
            
            _isDirty = false;
            UpdateSaveButtonState();
        }

        private void UpdateButtonsInteractable()
        {
            bool canEdit = !_isLockedMode;
            if (_cohortButtons != null)
            {
                foreach (var btn in _cohortButtons)
                    if (btn != null) btn.interactable = canEdit;
            }
            if (_removeAllButton != null) _removeAllButton.interactable = canEdit;
            if (_selectMultipleButton != null) _selectMultipleButton.interactable = canEdit;
            if (_autoMakeSquadButton != null) _autoMakeSquadButton.interactable = canEdit;
        }

        private void OnCohortButtonClicked(int index)
        {
            if (_isLockedMode) return;
            LoadCohortToTemp(index);
            RefreshUI();
        }

        private void OnSlotClicked(int index)
        {
            if (_isLockedMode && index < 11) return;
            if (index == 11 && _currentLevel != null && _currentLevel.IsAssistantLocked) return;

            if (_vassalInventoryController != null)
            {
                UIFlowManager.Instance.OpenPanel(_vassalInventoryController);
                _vassalInventoryController.OpenForSingleSelect(index, OnUnitSelected, _tempUnitIDs);
            }
        }

        private void OnUnitSelected(int slotIndex, string unitID)
        {
            if (slotIndex < _tempUnitIDs.Count)
            {
                if (_tempUnitIDs[slotIndex] != unitID)
                {
                    // Uniqueness Check: If this unit is already in another slot, clear that slot first
                    if (!string.IsNullOrEmpty(unitID))
                    {
                        int existingSlot = _tempUnitIDs.IndexOf(unitID);
                        if (existingSlot != -1 && existingSlot != slotIndex)
                        {
                            Debug.Log($"[CohortSquadUI] Moving '{unitID}' from Slot {existingSlot} to Slot {slotIndex}");
                            _tempUnitIDs[existingSlot] = ""; // Clear old slot
                        }
                    }

                    _tempUnitIDs[slotIndex] = unitID;
                    Debug.Log($"[CohortSquadUI] ASSIGN: Slot {slotIndex} = {unitID}");
                    MarkDirty();
                    SaveCohort(); // Auto-save on every change
                }
            }
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_playerData == null) return;

            for (int i = 0; i < _squadSlots.Count; i++)
            {
                if (i >= _tempUnitIDs.Count) break;
                
                var slot = _squadSlots[i];
                if (slot == null) continue;

                string unitID = "";
                bool isSlotLocked = false;

                // Safe retrieval of ID from temp list
                if (i < 12) 
                {
                    if (i < 11)
                    {
                        isSlotLocked = _isLockedMode;
                        unitID = (i < _tempUnitIDs.Count) ? _tempUnitIDs[i] : "";
                    }
                    else if (i == 11) // Assistant Slot
                    {
                        if (_isReadinessMode && _currentLevel != null && _currentLevel.IsAssistantLocked)
                        {
                            unitID = (_currentLevel.SupportAssistant != null) ? _currentLevel.SupportAssistant.UniqueID : "";
                            isSlotLocked = true;
                        }
                        else
                        {
                            unitID = (i < _tempUnitIDs.Count) ? _tempUnitIDs[i] : "";
                        }
                    }
                }

                if (string.IsNullOrEmpty(unitID))
                {
                    slot.SetEmpty();
                }
                else if (MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase != null)
                {
                    var unitData = MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase.GetUnitByID(unitID);
                    if (unitData != null) 
                    {
                        unitData.RefreshStats(_classScalingData);
                        Debug.Log($"[CohortSquadUI] Refresh SLOT {i} with unit '{unitData.UnitName}' (ID: {unitID})");
                        slot.SetUnit(unitData);
                    }
                    else 
                    {
                        Debug.LogWarning($"[CohortSquadUI] FAILED to find unitData for '{unitID}' in Database during RefreshUI at index {i}. Available unit count: {MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase.AllUnits.Count}");
                        slot.SetEmpty();
                    }
                }
                else
                {
                    Debug.LogError("[CohortSquadUI] RefreshUI failed because LoadedUnitDatabase is NULL");
                }

                var btn = slot.GetComponent<Button>();
                if (btn != null) btn.interactable = !isSlotLocked;
            }

            UpdateCohortButtonVisuals();
            UpdateSaveButtonState();
            RefreshRitesUI();
        }

        private void UpdateCohortButtonVisuals()
        {
            if (_cohortButtons == null) return;

            for (int i = 0; i < _cohortButtons.Length; i++)
            {
                var btn = _cohortButtons[i];
                if (btn == null) continue;

                var cb = btn.colors;
                // Highlight the currently viewed cohort button
                cb.normalColor = (i == _viewingCohortIndex) ? _highlightColor : _normalColor;
                cb.selectedColor = (i == _viewingCohortIndex) ? _highlightColor : _normalColor;
                btn.colors = cb;
            }
        }

        private void MarkDirty()
        {
            _isDirty = true;
            UpdateSaveButtonState();
        }

        private void UpdateSaveButtonState()
        {
            if (_actionButton == null) return;

            int unitCount = _tempUnitIDs.Count(id => !string.IsNullOrEmpty(id));

            if (_isReadinessMode)
            {
                if (_actionButton != null) _actionButton.gameObject.SetActive(true);

                if (_actionButtonText != null) 
                    _actionButtonText.text = LocalizationManager.Localize("Cohort.ActionButton.StartBattle");
                
                _actionButton.interactable = unitCount > 0;
            }
            else
            {
                // In Management mode, we auto-save, so hide the action button
                if (_actionButton != null) _actionButton.gameObject.SetActive(false);
            }

            Debug.Log($"[CohortSquadUI] Current Cohort Size: {unitCount} / 12");
        }

        private void OnActionButtonClicked()
        {
            if (_isReadinessMode) OnStartBattle();
            else SaveCohort();
        }

        private void SaveCohort()
        {
            if (!_isDirty || _viewingCohortIndex < 0 || _viewingCohortIndex >= _playerData.Cohorts.Count) return;

            var cohort = _playerData.Cohorts[_viewingCohortIndex];
            cohort.UnitIDs = new List<string>(_tempUnitIDs);
            
            if (_saveManager != null) _saveManager.Save();
            
            _isDirty = false;
            UpdateSaveButtonState();
            Debug.Log($"[CohortSquadUI] Cohort {_viewingCohortIndex + 1} saved.");
        }

        private void OnStartBattle()
        {
            if (_currentLevel == null) return;

            List<MaouSamaTD.Units.UnitData> selectedUnits = new List<MaouSamaTD.Units.UnitData>();
            for (int i = 0; i < 12; i++)
            {
                string id = (i < _tempUnitIDs.Count) ? _tempUnitIDs[i] : "";
                
                if (i == 11 && _currentLevel.IsAssistantLocked && _currentLevel.SupportAssistant != null)
                {
                    id = _currentLevel.SupportAssistant.UniqueID;
                }

                if (!string.IsNullOrEmpty(id))
                {
                    var unit = MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase?.GetUnitByID(id);
                    if (unit != null) selectedUnits.Add(unit);
                }
            }

            if (selectedUnits.Count == 0)
            {
                Debug.LogWarning("[CohortSquadUI] Cannot start battle with 0 units!");
                return;
            }

            if (_selectionState != null)
            {
                _selectionState.SetLevel(_currentLevel);
                _selectionState.SetCohort(selectedUnits);

                List<MaouSamaTD.Skills.SovereignRiteData> finalRites = new List<MaouSamaTD.Skills.SovereignRiteData>();
                if (_isRitesLocked && _currentLevel != null)
                {
                    var defaultList = _playerData.Gender == MaouSamaTD.Data.MaouGender.Male 
                        ? _currentLevel.MaleSovereignRites 
                        : _currentLevel.FemaleSovereignRites;
                    if (defaultList != null) finalRites.AddRange(defaultList);
                }
                else if (_viewingCohortIndex >= 0 && _viewingCohortIndex < _playerData.Cohorts.Count)
                {
                    var cohort = _playerData.Cohorts[_viewingCohortIndex];
                    if (cohort.SelectedRiteIDs != null)
                    {
                        foreach (var id in cohort.SelectedRiteIDs)
                        {
                            if (!string.IsNullOrEmpty(id))
                            {
                                var rite = MaouSamaTD.Core.AppEntryPoint.LoadedSovereignRiteDatabase?.GetRiteByID(id);
                                if (rite != null) finalRites.Add(rite);
                            }
                        }
                    }
                }
                _selectionState.SetSelectedRites(finalRites);
            }

            var loader = MaouSamaTD.UI.MainMenu.LoadingScreenPanel.Instance;
            if (loader != null) loader.LoadSceneTransition("BattleScene");
            else UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
        }

        private void OnBackClicked()
        {
            UIFlowManager.Instance.GoBack();
        }

        private void OnConfirmLeave()
        {
            _isDirty = false;
            if (_unsavedChangesPopup != null) _unsavedChangesPopup.SetActive(false);
            UIFlowManager.Instance.GoBack();
        }

        private void OnCancelLeave()
        {
            if (_unsavedChangesPopup != null) _unsavedChangesPopup.SetActive(false);
        }

        private void OnRemoveAllClicked()
        {
            if (_isLockedMode) return;

            bool wasModified = false;
            for (int i = 0; i < _tempUnitIDs.Count; i++)
            {
                if (i == 11 && _isReadinessMode && _currentLevel != null && _currentLevel.IsAssistantLocked) continue;

                if (!string.IsNullOrEmpty(_tempUnitIDs[i]))
                {
                    _tempUnitIDs[i] = "";
                    wasModified = true;
                }
            }
            
            if (wasModified) 
            {
                MarkDirty();
                SaveCohort(); // Auto-save on clear
            }
            RefreshUI();
        }

        private void OnAutoMakeSquadClicked()
        {
            if (_isLockedMode) return;

            if (_playerData == null) return;
            
            List<MaouSamaTD.Units.UnitData> candidates = new List<MaouSamaTD.Units.UnitData>();
            
            // Query from UnlockedUnits first
            if (_playerData.UnlockedUnits != null)
            {
                foreach (var id in _playerData.UnlockedUnits)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    var unit = MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase?.GetUnitByID(id);
                    if (unit != null && !candidates.Contains(unit))
                    {
                        candidates.Add(unit);
                    }
                }
            }
            
            // Also query from UnitInventory to be absolutely safe and robust!
            if (_playerData.UnitInventory != null)
            {
                foreach (var entry in _playerData.UnitInventory)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.UnitID)) continue;
                    var unit = MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase?.GetUnitByID(entry.UnitID);
                    if (unit != null && !candidates.Contains(unit))
                    {
                        candidates.Add(unit);
                    }
                }
            }

            // Sort by Rarity (descending) then Level (descending)
            candidates.Sort((a, b) =>
            {
                int compare = b.Rarity.CompareTo(a.Rarity);
                if (compare == 0) compare = b.Level.CompareTo(a.Level);
                if (compare == 0) compare = (a.UniqueID ?? "").CompareTo(b.UniqueID ?? "");
                return compare;
            });

            if (_tempUnitIDs == null) _tempUnitIDs = new List<string>();
            while (_tempUnitIDs.Count < 12) _tempUnitIDs.Add("");

            HashSet<string> lockedUnitIDs = new HashSet<string>();
            bool[] isSlotLocked = new bool[12];
            for (int i = 0; i < 12; i++)
            {
                bool isLocked = false;
                if (i < 11)
                {
                    isLocked = _isLockedMode;
                }
                else // i == 11
                {
                    isLocked = _isReadinessMode && _currentLevel != null && _currentLevel.IsAssistantLocked;
                }

                isSlotLocked[i] = isLocked;

                if (isLocked)
                {
                    string lockedID = "";
                    if (i == 11 && _currentLevel != null && _currentLevel.IsAssistantLocked && _currentLevel.SupportAssistant != null)
                    {
                        lockedID = _currentLevel.SupportAssistant.UniqueID;
                    }
                    else if (i < _tempUnitIDs.Count)
                    {
                        lockedID = _tempUnitIDs[i];
                    }

                    if (!string.IsNullOrEmpty(lockedID))
                    {
                        lockedUnitIDs.Add(lockedID);
                    }
                }
            }

            List<string> availableCandidates = new List<string>();
            foreach (var unit in candidates)
            {
                if (!lockedUnitIDs.Contains(unit.UniqueID))
                {
                    availableCandidates.Add(unit.UniqueID);
                }
            }

            List<string> nextTempUnitIDs = new List<string>();
            int candidateIndex = 0;
            for (int i = 0; i < 12; i++)
            {
                if (isSlotLocked[i])
                {
                    string lockedID = "";
                    if (i == 11 && _currentLevel != null && _currentLevel.IsAssistantLocked && _currentLevel.SupportAssistant != null)
                    {
                        lockedID = _currentLevel.SupportAssistant.UniqueID;
                    }
                    else if (i < _tempUnitIDs.Count)
                    {
                        lockedID = _tempUnitIDs[i];
                    }
                    nextTempUnitIDs.Add(lockedID);
                }
                else
                {
                    if (candidateIndex < availableCandidates.Count)
                    {
                        nextTempUnitIDs.Add(availableCandidates[candidateIndex]);
                        candidateIndex++;
                    }
                    else
                    {
                        nextTempUnitIDs.Add("");
                    }
                }
            }

            bool hasChanged = false;
            for (int i = 0; i < 12; i++)
            {
                string oldID = (i < _tempUnitIDs.Count) ? _tempUnitIDs[i] : "";
                string newID = nextTempUnitIDs[i];
                if (oldID != newID)
                {
                    hasChanged = true;
                    break;
                }
            }

            if (hasChanged)
            {
                _tempUnitIDs = nextTempUnitIDs;
                MarkDirty();
                SaveCohort();
                RefreshUI();
                Debug.Log("[CohortSquadUI] Auto Make Squad populated.");
            }
        }

        private void OnSelectMultipleClicked()
        {
            if (_isLockedMode) return;

            if (_vassalInventoryController != null)
            {
                UIFlowManager.Instance.OpenPanel(_vassalInventoryController);
                
                // Exclude the assistant slot (slot 11) from multi-select since it's managed separately or by level support
                int maxLimit = (_isReadinessMode && _currentLevel != null && _currentLevel.IsAssistantLocked) ? 11 : 12;
                
                // Get currently selected unit IDs in slots (filter out empty strings)
                List<string> currentSelected = new List<string>(_tempUnitIDs);
                currentSelected.RemoveAll(string.IsNullOrEmpty);

                _vassalInventoryController.OpenForMultiSelect(currentSelected, maxLimit, OnMultiSelectionComplete);
            }
        }

        private void OnMultiSelectionComplete(List<string> selectedIDs)
        {
            if (selectedIDs == null) return;

            // Save old assistant slot unit ID if it is locked/assistant mode is active
            string assistantID = "";
            bool isAssistantLocked = _isReadinessMode && _currentLevel != null && _currentLevel.IsAssistantLocked;
            if (isAssistantLocked && _tempUnitIDs.Count > 11)
            {
                assistantID = _tempUnitIDs[11];
            }

            // Clear temp list
            _tempUnitIDs.Clear();
            
            // Add selected units
            foreach (var id in selectedIDs)
            {
                if (_tempUnitIDs.Count < 11)
                {
                    _tempUnitIDs.Add(id);
                }
            }

            // Pad with empty strings up to slot 11
            while (_tempUnitIDs.Count < 11)
            {
                _tempUnitIDs.Add("");
            }

            // Add the assistant slot back
            if (isAssistantLocked)
            {
                _tempUnitIDs.Add(assistantID);
            }
            else if (selectedIDs.Count > 11)
            {
                _tempUnitIDs.Add(selectedIDs[11]);
            }
            else
            {
                _tempUnitIDs.Add("");
            }

            // Pad up to 12
            while (_tempUnitIDs.Count < 12)
            {
                _tempUnitIDs.Add("");
            }

            MarkDirty();
            SaveCohort();
            RefreshUI();
            Debug.Log("[CohortSquadUI] Multi selection applied to squad slots.");
        }

        private void SwitchTab(bool showVassals)
        {
            if (_vassalsPanel != null) _vassalsPanel.SetActive(showVassals);
            if (_ritesPanel != null) _ritesPanel.SetActive(!showVassals);

            if (_vassalsTabButton != null)
            {
                var cb = _vassalsTabButton.colors;
                cb.normalColor = showVassals ? _highlightColor : _normalColor;
                cb.selectedColor = showVassals ? _highlightColor : _normalColor;
                _vassalsTabButton.colors = cb;
            }

            if (_ritesTabButton != null)
            {
                var cb = _ritesTabButton.colors;
                cb.normalColor = !showVassals ? _highlightColor : _normalColor;
                cb.selectedColor = !showVassals ? _highlightColor : _normalColor;
                _ritesTabButton.colors = cb;
            }
        }

        private void RefreshRitesUI()
        {
            if (_playerData == null) return;

            // 1. Refresh active slots
            List<string> activeRiteIDs = new List<string>(new string[3] { "", "", "" });
            
            if (_isRitesLocked && _currentLevel != null)
            {
                var defaults = _playerData.Gender == MaouSamaTD.Data.MaouGender.Male 
                    ? _currentLevel.MaleSovereignRites 
                    : _currentLevel.FemaleSovereignRites;

                if (defaults != null)
                {
                    for (int i = 0; i < Mathf.Min(3, defaults.Count); i++)
                    {
                        if (defaults[i] != null) activeRiteIDs[i] = defaults[i].name;
                    }
                }
            }
            else if (_viewingCohortIndex >= 0 && _viewingCohortIndex < _playerData.Cohorts.Count)
            {
                var cohort = _playerData.Cohorts[_viewingCohortIndex];
                if (cohort.SelectedRiteIDs == null)
                {
                    cohort.SelectedRiteIDs = new List<string>(new string[3] { "", "", "" });
                }
                while (cohort.SelectedRiteIDs.Count < 3) cohort.SelectedRiteIDs.Add("");
                activeRiteIDs = cohort.SelectedRiteIDs;
            }

            for (int i = 0; i < _riteSlots.Count; i++)
            {
                if (i >= 3) break;
                var slot = _riteSlots[i];
                if (slot == null) continue;

                slot.Initialize(i, _isRitesLocked);

                string id = activeRiteIDs[i];
                if (string.IsNullOrEmpty(id))
                {
                    slot.SetRite(null);
                }
                else
                {
                    var rite = AppEntryPoint.LoadedSovereignRiteDatabase?.GetRiteByID(id);
                    slot.SetRite(rite);
                }
            }

            // 2. Refresh available rites pool
            if (_availableRitesContainer != null)
            {
                // Safely destroy existing placeholders/items immediately to prevent layout glitches
                for (int i = _availableRitesContainer.childCount - 1; i >= 0; i--)
                {
                    Transform child = _availableRitesContainer.GetChild(i);
                    child.SetParent(null); // Detach immediately so layout system ignores it this frame
                    Destroy(child.gameObject);
                }

                if (!_isRitesLocked && AppEntryPoint.LoadedSovereignRiteDatabase != null && _riteItemPrefab != null)
                {
                    var allRites = AppEntryPoint.LoadedSovereignRiteDatabase.AllRites;
                    if (allRites != null)
                    {
                        List<MaouSamaTD.Skills.SovereignRiteData> filteredRites = new List<MaouSamaTD.Skills.SovereignRiteData>();
                        string searchText = _searchField != null ? _searchField.text.ToLower() : "";
                        
                        foreach (var rite in allRites)
                        {
                            if (rite != null && rite.Archetype == _playerData.Gender)
                            {
                                bool passSearch = string.IsNullOrEmpty(searchText) || 
                                                  (!string.IsNullOrEmpty(rite.SkillName) && rite.SkillName.ToLower().Contains(searchText));
                                
                                bool passFilter = true;
                                if (_activeRiteFilter != "All")
                                {
                                    passFilter = rite.EffectType.ToString().Equals(_activeRiteFilter, System.StringComparison.OrdinalIgnoreCase);
                                }

                                if (passSearch && passFilter)
                                {
                                    filteredRites.Add(rite);
                                }
                            }
                        }

                        if (_sortDropdown != null)
                        {
                            switch (_sortDropdown.value)
                            {
                                case 1: // Name (A-Z)
                                    filteredRites.Sort((a, b) => string.Compare(a.SkillName, b.SkillName));
                                    break;
                                case 2: // Cost (Low-High)
                                    filteredRites.Sort((a, b) => a.SealCost.CompareTo(b.SealCost));
                                    break;
                                case 3: // Cost (High-Low)
                                    filteredRites.Sort((a, b) => b.SealCost.CompareTo(a.SealCost));
                                    break;
                            }
                        }

                        foreach (var rite in filteredRites)
                        {
                            GameObject itemObj = Instantiate(_riteItemPrefab, _availableRitesContainer);
                            itemObj.SetActive(true);
                            var itemUI = itemObj.GetComponent<CohortRiteItemUI>();
                            if (itemUI != null)
                            {
                                itemUI.Setup(rite, _isRitesLocked);
                            }
                        }
                    }
                }
            }
            
            UpdateFilterButtonVisuals();
        }

        private void UpdateFilterButtonVisuals()
        {
            if (_filterButtons == null) return;
            foreach (var btn in _filterButtons)
            {
                if (btn != null)
                {
                    var txt = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    bool isActive = false;
                    if (txt != null)
                    {
                        isActive = txt.text.Equals(_activeRiteFilter, System.StringComparison.OrdinalIgnoreCase);
                    }
                    
                    var outline = btn.GetComponent<UnityEngine.UI.Outline>();
                    if (outline != null)
                    {
                        outline.enabled = isActive;
                    }
                    else
                    {
                        var cb = btn.colors;
                        cb.normalColor = isActive ? _highlightColor : _normalColor;
                        cb.selectedColor = isActive ? _highlightColor : _normalColor;
                        btn.colors = cb;
                    }
                }
            }
        }
        
        private void OnRiteDroppedInSlot(int slotIndex, SovereignRiteData riteData)
        {
            if (_isLockedMode || _isRitesLocked || _viewingCohortIndex < 0 || _viewingCohortIndex >= _playerData.Cohorts.Count) return;

            var cohort = _playerData.Cohorts[_viewingCohortIndex];
            if (cohort.SelectedRiteIDs == null)
            {
                cohort.SelectedRiteIDs = new List<string>(new string[3] { "", "", "" });
            }
            while (cohort.SelectedRiteIDs.Count < 3) cohort.SelectedRiteIDs.Add("");

            string riteID = riteData.name;
            int existingSlot = cohort.SelectedRiteIDs.IndexOf(riteID);
            if (existingSlot != -1 && existingSlot != slotIndex)
            {
                cohort.SelectedRiteIDs[existingSlot] = "";
                if (existingSlot < _riteSlots.Count && _riteSlots[existingSlot] != null)
                {
                    _riteSlots[existingSlot].SetRite(null);
                }
            }

            cohort.SelectedRiteIDs[slotIndex] = riteID;
            MarkDirty();
            SaveCohort();
            RefreshRitesUI();
        }

        private void OnRiteClearedFromSlot(int slotIndex)
        {
            if (_isLockedMode || _isRitesLocked || _viewingCohortIndex < 0 || _viewingCohortIndex >= _playerData.Cohorts.Count) return;

            var cohort = _playerData.Cohorts[_viewingCohortIndex];
            if (cohort.SelectedRiteIDs != null && slotIndex < cohort.SelectedRiteIDs.Count)
            {
                cohort.SelectedRiteIDs[slotIndex] = "";
                MarkDirty();
                SaveCohort();
                RefreshRitesUI();
            }
        }
        #endregion
        private void InitializeFilters()
        {
            if (_searchField == null)
            {
                var sfGo = GameObject.Find("SearchInput");
                if (sfGo != null) _searchField = sfGo.GetComponent<TMPro.TMP_InputField>();
            }
            if (_sortDropdown == null)
            {
                var ddGo = GameObject.Find("SortDropdown");
                if (ddGo != null) _sortDropdown = ddGo.GetComponent<TMPro.TMP_Dropdown>();
            }
            if (_filterButtons == null || _filterButtons.Length == 0)
            {
                var tgGo = GameObject.Find("FilterToggles");
                if (tgGo != null) _filterButtons = tgGo.GetComponentsInChildren<UnityEngine.UI.Button>();
            }

            if (_searchField != null) _searchField.onValueChanged.AddListener((val) => RefreshRitesUI());
            if (_sortDropdown != null) _sortDropdown.onValueChanged.AddListener((val) => RefreshRitesUI());
            if (_filterButtons != null)
            {
                foreach (var btn in _filterButtons)
                {
                    if (btn != null)
                    {
                        var txt = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                        if (txt != null)
                        {
                            string filterName = txt.text;
                            btn.onClick.AddListener(() => {
                                _activeRiteFilter = filterName;
                                RefreshRitesUI();
                            });
                        }
                    }
                }
            }
        }
    }
}
