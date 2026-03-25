using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using MaouSamaTD.Data;
using MaouSamaTD.UI.MainMenu;
using Assets.SimpleLocalization.Scripts;
using Zenject;
using MaouSamaTD.UI.Cohorts;
using MaouSamaTD.UI;
using MaouSamaTD.UI.Vassals;

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
        [SerializeField] private TMPro.TextMeshProUGUI _titleText;
        [SerializeField] private CohortManagerUI _cohortInventoryController;

        [Header("Cohort Slots")]
        [SerializeField] private List<UnitCardSlot> _squadSlots = new List<UnitCardSlot>();

        [Header("Cohort Selection")]
        [SerializeField] private Button[] _cohortButtons;
        
        [Header("Actions")]
        public Button _actionButton; // Unified Save / Start Battle
        public TMPro.TextMeshProUGUI _actionButtonText;
        [SerializeField] private Button _removeAllButton;

        [Header("Locked Mode")]
        [SerializeField] private GameObject _noEditBlocker;

        [Header("Unsaved Changes Popup")]
        [SerializeField] private GameObject _unsavedChangesPopup;
        [SerializeField] private Button _confirmLeaveButton;
        [SerializeField] private Button _cancelLeaveButton;
        
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
        private void Awake()
        {
            if (_cohortInventoryController == null)
            {
                _cohortInventoryController = GetComponentInChildren<CohortManagerUI>(true);
            }
        }

        private void Start()
        {
            if (_actionButton != null) _actionButton.onClick.AddListener(OnActionButtonClicked);
            if (_removeAllButton != null) _removeAllButton.onClick.AddListener(OnRemoveAllClicked);
            
            if (_confirmLeaveButton != null) _confirmLeaveButton.onClick.AddListener(OnConfirmLeave);
            if (_cancelLeaveButton != null) _cancelLeaveButton.onClick.AddListener(OnCancelLeave);

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
            
            _isReadinessMode = false;
            _isLockedMode = false;
            if (_titleText != null) _titleText.text = "COHORTS";
            if (_noEditBlocker != null) _noEditBlocker.SetActive(false);

            InitializeData();
            LoadCohortToTemp(_playerData.CurrentCohortIndex);
            
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
                _titleText.text = level != null ? level.LevelName : "MISSION READINESS";

            InitializeData();

            _isLockedMode = level != null && level.IsCohortLocked;
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
            if (_cohortInventoryController != null) _cohortInventoryController.Close();
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

            if (_cohortInventoryController != null)
            {
                _cohortInventoryController.OpenForSingleSelect(index, OnUnitSelected);
                UIFlowManager.Instance.OpenPanel(_cohortInventoryController);
            }
        }

        private void OnUnitSelected(int slotIndex, string unitID)
        {
            if (slotIndex < _tempUnitIDs.Count)
            {
                if (_tempUnitIDs[slotIndex] != unitID)
                {
                    _tempUnitIDs[slotIndex] = unitID;
                    _viewingCohortIndex = -1; 
                    MarkDirty();
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

                if (i < 11)
                {
                    isSlotLocked = _isLockedMode;
                    unitID = _tempUnitIDs[i];
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
                        unitID = _tempUnitIDs[i];
                    }
                }

                if (string.IsNullOrEmpty(unitID))
                {
                    slot.SetEmpty();
                }
                else if (MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase != null)
                {
                    var unitData = MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase.GetUnitByID(unitID);
                    if (unitData != null) slot.SetUnit(unitData);
                    else slot.SetEmpty();
                }

                var btn = slot.GetComponent<Button>();
                if (btn != null) btn.interactable = !isSlotLocked;
            }

            UpdateSaveButtonState();
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
                if (_actionButtonText != null) 
                    _actionButtonText.text = LocalizationManager.Localize("Cohort.ActionButton.StartBattle");
                
                _actionButton.interactable = unitCount > 0;
            }
            else
            {
                if (_actionButtonText != null) 
                    _actionButtonText.text = LocalizationManager.Localize("Cohort.ActionButton.Save");
                _actionButton.interactable = _isDirty;
            }
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
            }

            var loader = Object.FindFirstObjectByType<MaouSamaTD.UI.MainMenu.LoadingScreenPanel>(FindObjectsInactive.Include);
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
            
            if (wasModified) MarkDirty();
            RefreshUI();
        }
        #endregion
    }
}
