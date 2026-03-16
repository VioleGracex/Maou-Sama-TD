using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zenject;
using MaouSamaTD.Core;

namespace MaouSamaTD.UI
{
    public class MissionReadinessPanel : MonoBehaviour, IUIController
    {
        #region Variables
        [Header("UI Controller Architecture")]
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;
        [SerializeField] private TMPro.TextMeshProUGUI _titleText;
        [SerializeField] private UnitSelectionPanel _unitSelectionController;

        [Header("Preassigned Cohort Slots")]
        [Tooltip("Assign your exactly 11 slots (10 regular, 1 extra) here from the scene hierarchy.")]
        [SerializeField] private List<UnitCardSlot> _preassignedSlots = new List<UnitCardSlot>();

        [Header("Cohort Selection")]
        [SerializeField] private Button[] _cohortButtons;
        
        [Header("Actions")]
        [SerializeField] private Button _startBattleButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _removeAllButton;
        [SerializeField] private Button _barracksButton;
        [SerializeField] private Button _resetButton;

        [Header("Locked Mode")]
        [SerializeField] private GameObject _noEditBlocker;


        [Inject] private MaouSamaTD.Managers.GameSelectionState _selectionState;
        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;

        private MaouSamaTD.Data.PlayerData _playerData;  
        private MaouSamaTD.Levels.LevelData _currentLevel;
        
        private List<string> _currentSquadUnitIDs = new List<string>();
        private int _selectedCohortIndex = -1; // -1 = Premade/Custom, 0-3 = Saved Cohorts
        private bool _isLockedMode = false;
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (_startBattleButton != null) _startBattleButton.onClick.AddListener(OnStartBattle);
            if (_backButton != null) _backButton.onClick.AddListener(() => UIFlowManager.Instance.GoBack());
            if (_removeAllButton != null) _removeAllButton.onClick.AddListener(OnRemoveAllClicked);
            if (_barracksButton != null) _barracksButton.onClick.AddListener(OnBarracksClicked);
            if (_resetButton != null) _resetButton.onClick.AddListener(ResetToDefault);

            InitializePlayerData();
            SetupSlots();
            SetupCohortButtons();
        }
        #endregion

        #region Public Methods
        public void Open()
        {
            if (_visualRoot == null) return;
            _visualRoot.SetActive(true);
            RefreshUI();
        }

        public void Open(MaouSamaTD.Levels.LevelData level)
        {
            if (_visualRoot == null) return;
            _visualRoot.SetActive(true);

            _currentLevel = level;
            InitializePlayerData();
            
            if (_titleText != null)
                _titleText.text = level != null ? level.LevelName : "MISSION READINESS";

            // Initialize Temporary Squad
            _currentSquadUnitIDs.Clear();
            _isLockedMode = level != null && level.IsCohortLocked;

            bool hasPremade = level != null && level.PremadeCohort != null && level.PremadeCohort.Count > 0;
            
            if (hasPremade)
            {
                LoadPremadeCohort();
                _selectedCohortIndex = -1;
            }
            else
            {
                _selectedCohortIndex = _playerData.CurrentCohortIndex;
                LoadSavedCohort(_selectedCohortIndex);
            }

            if (_noEditBlocker != null)
                _noEditBlocker.SetActive(_isLockedMode);

            UpdateButtonsInteractable();
            RefreshUI();
        }

        public void ResetToDefault()
        {
            if (_currentLevel == null) return;
            
            if (_currentLevel.PremadeCohort != null && _currentLevel.PremadeCohort.Count > 0)
            {
                LoadPremadeCohort();
                _selectedCohortIndex = -1;
            }
            else
            {
                LoadSavedCohort(_playerData.CurrentCohortIndex);
                _selectedCohortIndex = _playerData.CurrentCohortIndex;
            }
            RefreshUI();
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        public void ResetState() { }

        public bool RequestClose() => true;
        #endregion

        #region Private Methods
        private void InitializePlayerData()
        {
            if (_saveManager != null) _playerData = _saveManager.CurrentData;
            
            if (_playerData == null)
            {
                _playerData = new MaouSamaTD.Data.PlayerData();
            }

            if (_playerData.Cohorts == null) _playerData.Cohorts = new List<MaouSamaTD.Data.CohortData>();
            if (_playerData.Cohorts.Count < 4)
            {
                for (int i = _playerData.Cohorts.Count; i < 4; i++)
                    _playerData.Cohorts.Add(new MaouSamaTD.Data.CohortData($"Cohort {i + 1}"));
            }
        }

        private void LoadPremadeCohort()
        {
            _currentSquadUnitIDs.Clear();
            if (_currentLevel == null || _currentLevel.PremadeCohort == null) return;

            foreach (var unit in _currentLevel.PremadeCohort)
            {
                if (unit == null) _currentSquadUnitIDs.Add("");
                else _currentSquadUnitIDs.Add(string.IsNullOrEmpty(unit.UniqueID) ? unit.name : unit.UniqueID);
            }
            while (_currentSquadUnitIDs.Count < 12) _currentSquadUnitIDs.Add("");
        }

        private void LoadSavedCohort(int index)
        {
            _currentSquadUnitIDs.Clear();
            if (index < 0 || index >= _playerData.Cohorts.Count) return;

            var cohort = _playerData.Cohorts[index];
            _currentSquadUnitIDs = new List<string>(cohort.UnitIDs);
            while (_currentSquadUnitIDs.Count < 12) _currentSquadUnitIDs.Add("");
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
            if (_barracksButton != null) _barracksButton.interactable = canEdit;
        }

        private void SetupSlots()
        {
            for (int i = 0; i < _preassignedSlots.Count; i++)
            {
                if (_preassignedSlots[i] != null)
                {
                    _preassignedSlots[i].SetIndex(i);
                    _preassignedSlots[i].OnClick -= OnSlotClicked;
                    _preassignedSlots[i].OnClick += OnSlotClicked;
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
                    _cohortButtons[i].onClick.AddListener(() => SwitchCohort(index));
            }
        }

        private void SwitchCohort(int index)
        {
            if (_isLockedMode) return;
            _selectedCohortIndex = index;
            LoadSavedCohort(index);
            RefreshUI();
        }

        private void OnSlotClicked(int index)
        {
            if (_isLockedMode && index < 11) return;
            if (index == 11 && _currentLevel != null && _currentLevel.IsAssistantLocked) return;

            if (_unitSelectionController != null)
            {
                _unitSelectionController.Open(index, OnUnitSelected);
                UIFlowManager.Instance.OpenPanel(_unitSelectionController);
            }
        }

        private void OnUnitSelected(int slotIndex, string unitID)
        {
            if (slotIndex < _currentSquadUnitIDs.Count)
            {
                _currentSquadUnitIDs[slotIndex] = unitID;
                _selectedCohortIndex = -1; // Now it's a "Custom" configuration
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            if (_playerData == null) return;

            for (int i = 0; i < _preassignedSlots.Count; i++)
            {
                var slot = _preassignedSlots[i];
                if (slot == null) continue;

                string unitID = "";
                bool isSlotLocked = false;

                if (i < 11)
                {
                    isSlotLocked = _isLockedMode;
                    if (i < _currentSquadUnitIDs.Count) unitID = _currentSquadUnitIDs[i];
                }
                else if (i == 11) // Assistant Slot
                {
                    if (_currentLevel != null && _currentLevel.IsAssistantLocked)
                    {
                        unitID = (_currentLevel.SupportAssistant != null) ? _currentLevel.SupportAssistant.UniqueID : "";
                        isSlotLocked = true;
                    }
                    else if (i < _currentSquadUnitIDs.Count)
                    {
                        unitID = _currentSquadUnitIDs[i];
                    }
                }

                if (string.IsNullOrEmpty(unitID))
                {
                    slot.SetEmpty();
                }
                else
                {
                    var unitData = AppEntryPoint.LoadedUnitDatabase?.GetUnitByID(unitID);
                    if (unitData != null) slot.SetUnit(unitData);
                    else slot.SetEmpty();
                }

                var btn = slot.GetComponent<Button>();
                if (btn != null) btn.interactable = !isSlotLocked;
            }
        }

        private void OnStartBattle()
        {
            if (_currentLevel == null) return;

            List<MaouSamaTD.Units.UnitData> selectedUnits = new List<MaouSamaTD.Units.UnitData>();
            for (int i = 0; i < 12; i++)
            {
                string id = (i < _currentSquadUnitIDs.Count) ? _currentSquadUnitIDs[i] : "";
                
                // Handle Forced Assistant Override
                if (i == 11 && _currentLevel.IsAssistantLocked && _currentLevel.SupportAssistant != null)
                {
                    id = _currentLevel.SupportAssistant.UniqueID;
                }

                if (!string.IsNullOrEmpty(id))
                {
                    var unit = AppEntryPoint.LoadedUnitDatabase?.GetUnitByID(id);
                    if (unit != null) selectedUnits.Add(unit);
                }
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

        private void OnRemoveAllClicked()
        {
            if (_isLockedMode) return;
            for (int i = 0; i < _currentSquadUnitIDs.Count; i++)
            {
                 // Don't remove if locked assistant
                if (i == 11 && _currentLevel != null && _currentLevel.IsAssistantLocked) continue;
                _currentSquadUnitIDs[i] = "";
            }
            _selectedCohortIndex = -1;
            RefreshUI();
        }

        private void OnBarracksClicked()
        {
            if (_unitSelectionController != null)
            {
                int squadSize = Mathf.Min(11, _preassignedSlots.Count);
                _unitSelectionController.OpenMultiSelect(_currentSquadUnitIDs, squadSize, (selectedIds) =>
                {
                    for (int i = 0; i < squadSize; i++)
                    {
                        _currentSquadUnitIDs[i] = i < selectedIds.Count ? selectedIds[i] : "";
                    }
                    _selectedCohortIndex = -1;
                    RefreshUI();
                });
                UIFlowManager.Instance.OpenPanel(_unitSelectionController);
            }
        }
        #endregion
    }
}