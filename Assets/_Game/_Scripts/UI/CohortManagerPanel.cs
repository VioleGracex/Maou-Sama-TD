using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zenject;
using System.Linq;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Standalone team/loadout editor. Reuses logic from MissionReadinessPanel but without battle constraints.
    /// Includes "Dirty State" tracking and unsaved changes prompts.
    /// </summary>
    public class CohortManagerPanel : MonoBehaviour, IUIController
    {
        #region Variables
        [Header("UI Controller Architecture")]
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;
        [SerializeField] private UnitSelectionPanel _unitSelectionController;

        [Header("Cohort Slots")]
        [SerializeField] private List<UnitCardSlot> _slots = new List<UnitCardSlot>();

        [Header("Cohort Selection")]
        [SerializeField] private Button[] _cohortButtons;
        
        [Header("Actions")]
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _removeAllButton;
        [SerializeField] private Button _barracksButton;

        [Header("Confirmation UI")]
        [SerializeField] private GameObject _unsavedChangesPopup;
        [SerializeField] private Button _confirmExitButton;
        [SerializeField] private Button _cancelExitButton;

        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;

        private MaouSamaTD.Data.PlayerData _playerData;
        private int _viewingCohortIndex = 0;
        private List<string> _tempUnitIDs = new List<string>();
        private bool _isDirty = false;
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (_saveButton != null) _saveButton.onClick.AddListener(SaveCurrentCohort);
            if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);
            if (_removeAllButton != null) _removeAllButton.onClick.AddListener(OnRemoveAllClicked);
            if (_barracksButton != null) _barracksButton.onClick.AddListener(OnBarracksClicked);
            
            if (_confirmExitButton != null) _confirmExitButton.onClick.AddListener(OnConfirmDiscardExit);
            if (_cancelExitButton != null) _cancelExitButton.onClick.AddListener(() => _unsavedChangesPopup.SetActive(false));

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
            
            InitializeData();
            LoadCohortToTemp(_playerData.CurrentCohortIndex);
            RefreshUI();
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            if (_unsavedChangesPopup != null) _unsavedChangesPopup.SetActive(false);
            if (_unitSelectionController != null) _unitSelectionController.Close();
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
                }
                return false;
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
                // Fallback for safety/editor testing
                _playerData = new MaouSamaTD.Data.PlayerData();
                if (_playerData.Cohorts.Count == 0)
                {
                    for (int i = 0; i < 4; i++) _playerData.Cohorts.Add(new MaouSamaTD.Data.CohortData($"Cohort {i + 1}"));
                }
            }
        }

        private void SetupSlots()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
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
            
            // Ensure capacity (11 slots)
            while (_tempUnitIDs.Count < 11) _tempUnitIDs.Add("");
            
            _isDirty = false;
            UpdateSaveButtonState();
        }

        private void OnCohortButtonClicked(int index)
        {
            if (_isDirty)
            {
                // Optionally show prompt here if switching cohorts also counts as an exit
                // For now, let's keep it simple and just switch, overwriting temp
            }
            LoadCohortToTemp(index);
            RefreshUI();
        }

        private void OnSlotClicked(int index)
        {
            if (_unitSelectionController != null)
            {
                _unitSelectionController.Open(index, OnUnitSelected);
                UIFlowManager.Instance.OpenPanel(_unitSelectionController);
            }
        }

        private void OnUnitSelected(int slotIndex, string unitID)
        {
            if (slotIndex < _tempUnitIDs.Count)
            {
                if (_tempUnitIDs[slotIndex] != unitID)
                {
                    _tempUnitIDs[slotIndex] = unitID;
                    MarkDirty();
                }
            }
            RefreshUI();
        }

        private void RefreshUI()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (i >= _tempUnitIDs.Count) break;
                
                var slot = _slots[i];
                string unitID = _tempUnitIDs[i];

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
            }
        }

        private void MarkDirty()
        {
            _isDirty = true;
            UpdateSaveButtonState();
        }

        private void UpdateSaveButtonState()
        {
            if (_saveButton != null)
            {
                _saveButton.interactable = _isDirty;
            }
        }

        private void SaveCurrentCohort()
        {
            if (!_isDirty) return;

            var cohort = _playerData.Cohorts[_viewingCohortIndex];
            cohort.UnitIDs = new List<string>(_tempUnitIDs);
            
            if (_saveManager != null) _saveManager.Save();
            
            _isDirty = false;
            UpdateSaveButtonState();
            Debug.Log($"[CohortManager] Cohort {_viewingCohortIndex + 1} saved.");
        }

        private void OnBackClicked()
        {
            UIFlowManager.Instance.GoBack();
        }

        private void OnConfirmDiscardExit()
        {
            _isDirty = false;
            UIFlowManager.Instance.GoBack(true);
        }

        private void OnRemoveAllClicked()
        {
            bool wasModified = false;
            for (int i = 0; i < _tempUnitIDs.Count; i++)
            {
                if (!string.IsNullOrEmpty(_tempUnitIDs[i]))
                {
                    _tempUnitIDs[i] = "";
                    wasModified = true;
                }
            }
            
            if (wasModified) MarkDirty();
            RefreshUI();
        }

        private void OnBarracksClicked()
        {
            if (_unitSelectionController != null)
            {
                _unitSelectionController.OpenMultiSelect(_tempUnitIDs, 11, (selectedIds) => 
                {
                    // Check if actually changed
                    if (!Enumerable.SequenceEqual(_tempUnitIDs.Take(11), selectedIds.Take(11)))
                    {
                        for (int i = 0; i < 11; i++)
                        {
                            _tempUnitIDs[i] = i < selectedIds.Count ? selectedIds[i] : "";
                        }
                        MarkDirty();
                        RefreshUI();
                    }
                });
                UIFlowManager.Instance.OpenPanel(_unitSelectionController);
            }
        }
        #endregion
    }
}
