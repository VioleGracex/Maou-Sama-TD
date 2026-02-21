using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zenject;

namespace MaouSamaTD.UI
{
    public class MissionReadinessPanel : MonoBehaviour, IUIController
    {
        [Header("UI Controller Architecture")]
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot;
        [SerializeField] private UnitSelectionPanel _unitSelectionController;

        [Header("Preassigned Cohort Slots")]
        [Tooltip("Assign your exactly 11 slots (10 regular, 1 extra) here from the scene hierarchy.")]
        [SerializeField] private List<UnitCardSlot> _preassignedSlots = new List<UnitCardSlot>();

        [Header("Cohort Selection")]
        [SerializeField] private Button[] _cohortButtons;
        
        [Header("Actions")]
        [SerializeField] private Button _startBattleButton;
        [SerializeField] private Button _backButton; // Optional return to briefing/campaign
        [SerializeField] private Button _removeAllButton;
        [SerializeField] private Button _barracksButton; // Multi-Select

        [Inject] private MaouSamaTD.Managers.GameManager _gameManager; 
        [Inject] private MaouSamaTD.Managers.GameSelectionState _selectionState;
        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;

        // Temporary local reference until injection is sorted
        private MaouSamaTD.Data.PlayerData _playerData;  
        private MaouSamaTD.Levels.LevelData _currentLevel;
        private bool _isLockedMode = false;
        private List<string> _lockedUnitIDs = new List<string>();

        private void Start()
        {
            if (_startBattleButton != null) _startBattleButton.onClick.AddListener(OnStartBattle);
            if (_backButton != null) _backButton.onClick.AddListener(() => UIFlowManager.Instance.GoBack());
            if (_removeAllButton != null) _removeAllButton.onClick.AddListener(OnRemoveAllClicked);
            if (_barracksButton != null) _barracksButton.onClick.AddListener(OnBarracksClicked);

            // Mock PlayerData if not found (for testing)
            if (_playerData == null)
            {
                 _playerData = new MaouSamaTD.Data.PlayerData(); 
                if (_playerData.Cohorts.Count == 0)
                {
                    for (int i = 0; i < 4; i++) _playerData.Cohorts.Add(new MaouSamaTD.Data.CohortData($"Cohort {i + 1}"));
                }
            }

            SetupSlots();
            SetupCohortButtons();
            RefreshUI();
        }

        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            RefreshUI();
        }

        public void Open(MaouSamaTD.Levels.LevelData level)
        {
            _currentLevel = level;
            
            _isLockedMode = false;
            _lockedUnitIDs.Clear();
            
            if (_currentLevel != null)
            {
                _isLockedMode = _currentLevel.IsCohortLocked;
                
                if (_currentLevel.PremadeCohort != null)
                {
                    foreach (var unit in _currentLevel.PremadeCohort)
                    {
                        _lockedUnitIDs.Add(unit != null ? unit.UnitID : "");
                    }
                }
                
                // Pad squad to 10
                int squadSize = 10;
                while (_lockedUnitIDs.Count < squadSize) _lockedUnitIDs.Add(""); 
                
                if (!_isLockedMode && _playerData != null && _playerData.Cohorts.Count > 0)
                {
                    var cohort = _playerData.Cohorts[_playerData.CurrentCohortIndex];
                    // Overwrite cohort with premade up to 10 slots
                    for(int i = 0; i < squadSize; i++)
                    {
                        if (cohort.UnitIDs.Count > i) cohort.UnitIDs[i] = _lockedUnitIDs[i];
                        else cohort.UnitIDs.Add(_lockedUnitIDs[i]); 
                    }
                }
            }

            // Let the external caller (BriefingManager) push this to Flow Manager
            // which will call Open() manually.
            SetUIState();
        }

        private void SetUIState()
        {
            bool interactable = !_isLockedMode;
            
            if (_cohortButtons != null)
            {
                foreach(var btn in _cohortButtons)
                {
                    if(btn != null) btn.interactable = interactable;
                }
            }
            
            if (_removeAllButton != null) _removeAllButton.interactable = interactable;
            if (_barracksButton != null) _barracksButton.interactable = interactable;
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        private void SetupSlots()
        {
            for (int i = 0; i < _preassignedSlots.Count; i++)
            {
                var slot = _preassignedSlots[i];
                if (slot != null)
                {
                    slot.SetIndex(i);
                    // Remove listener to prevent duplicates in case this is called twice somehow
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
                   _cohortButtons[i].onClick.AddListener(() => SwitchCohort(index));
            }
        }

        private void SwitchCohort(int index)
        {
            if (_playerData == null) return;
            _playerData.CurrentCohortIndex = index;
            RefreshUI();
        }

        private void OnSlotClicked(int index)
        {
            if (index < 10 && _isLockedMode) return; // Disable interaction in locked mode for squad
            if (index == 10 && _currentLevel != null && _currentLevel.IsAssistantLocked) return; // Disable for locked assistant

            // Open Unit Selection Panel via Flow Manager
            if (_unitSelectionController != null)
            {
                _unitSelectionController.Open(index, OnUnitSelected); 
                UIFlowManager.Instance.OpenPanel(_unitSelectionController);
            }
            else
            {
                Debug.LogError("UnitSelectionController reference not assigned in Inspector!");
            }
        }

        private void OnUnitSelected(int slotIndex, string unitID)
        {
            if (_playerData == null) return;
            var cohort = _playerData.Cohorts[_playerData.CurrentCohortIndex];
            cohort.UnitIDs[slotIndex] = unitID;
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_playerData == null) return;
            
            for (int i = 0; i < _preassignedSlots.Count; i++)
            {
                var slot = _preassignedSlots[i];
                if (slot == null) continue;

                string unitID = "";

                if (i < 10) // Squad slot
                {
                    if (_isLockedMode && _lockedUnitIDs.Count > i)
                    {
                        unitID = _lockedUnitIDs[i];
                    }
                    else if (_playerData.Cohorts.Count > 0)
                    {
                        var cohort = _playerData.Cohorts[_playerData.CurrentCohortIndex];
                        unitID = (i < cohort.UnitIDs.Count) ? cohort.UnitIDs[i] : "";
                    }
                }
                else if (i == 10) // Assistant slot
                {
                    if (_currentLevel != null && _currentLevel.IsAssistantLocked)
                    {
                        unitID = (_currentLevel.SupportAssistant != null) ? _currentLevel.SupportAssistant.UnitID : "";
                    }
                    else if (_playerData.Cohorts.Count > 0)
                    {
                        var cohort = _playerData.Cohorts[_playerData.CurrentCohortIndex];
                        unitID = (i < cohort.UnitIDs.Count) ? cohort.UnitIDs[i] : "";
                    }
                }

                if (string.IsNullOrEmpty(unitID))
                {
                    slot.SetEmpty();
                }
                else
                {
                    if (_gameManager != null && _gameManager.UnitDatabase != null)
                    {
                        var unitData = _gameManager.UnitDatabase.GetUnitByID(unitID);
                        slot.SetUnit(unitData);
                    }
                }
            }
        }

        private void OnStartBattle()
        {
            if (_currentLevel == null)
            {
                Debug.LogError("Cannot start battle: No level selected!");
                return;
            }

             // Gather units from current cohort or locked list
             List<MaouSamaTD.Units.UnitData> selectedUnits = new List<MaouSamaTD.Units.UnitData>();
             
             for (int i = 0; i < _preassignedSlots.Count; i++)
             {
                 string unitID = "";

                 if (i < 10) // Squad slot
                 {
                     if (_isLockedMode && _lockedUnitIDs.Count > i)
                     {
                         unitID = _lockedUnitIDs[i];
                     }
                     else if (_playerData != null && _playerData.Cohorts.Count > 0)
                     {
                         var cohort = _playerData.Cohorts[_playerData.CurrentCohortIndex];
                         unitID = (i < cohort.UnitIDs.Count) ? cohort.UnitIDs[i] : "";
                     }
                 }
                 else if (i == 10) // Assistant slot
                 {
                     if (_currentLevel != null && _currentLevel.IsAssistantLocked)
                     {
                         unitID = (_currentLevel.SupportAssistant != null) ? _currentLevel.SupportAssistant.UnitID : "";
                     }
                     else if (_playerData != null && _playerData.Cohorts.Count > 0)
                     {
                         var cohort = _playerData.Cohorts[_playerData.CurrentCohortIndex];
                         unitID = (i < cohort.UnitIDs.Count) ? cohort.UnitIDs[i] : "";
                     }
                 }

                 if (!string.IsNullOrEmpty(unitID) && _gameManager != null)
                 {
                     var unitData = _gameManager.UnitDatabase.GetUnitByID(unitID);
                     if (unitData != null) selectedUnits.Add(unitData);
                 }
             }

             if (selectedUnits.Count == 0)
             {
                 Debug.LogWarning("Starting battle with 0 units!");
                 // Return or allow?
             }

             if (_selectionState != null)
             {
                 _selectionState.SetLevel(_currentLevel);
                 _selectionState.SetCohort(selectedUnits);
             }

             UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
        }

        private void OnRemoveAllClicked()
        {
            if (_playerData == null) return;
            var cohort = _playerData.Cohorts[_playerData.CurrentCohortIndex];
            for (int i = 0; i < cohort.UnitIDs.Count; i++)
            {
                cohort.UnitIDs[i] = "";
            }
            RefreshUI();
        }

        private void OnBarracksClicked()
        {
             if (_unitSelectionController != null)
             {
                 var cohort = _playerData.Cohorts[_playerData.CurrentCohortIndex];
                 
                 
                 // Multi-select only operates on the 10 squad slots, not the assistant!
                 int squadSize = Mathf.Min(10, _preassignedSlots.Count);

                 _unitSelectionController.OpenMultiSelect(cohort.UnitIDs, squadSize, (selectedIds) => 
                 {
                     // Apply ordered selection strictly up to squad size
                     for(int i = 0; i < squadSize; i++)
                     {
                         if(i < selectedIds.Count)
                         { 
                             if (cohort.UnitIDs.Count > i) cohort.UnitIDs[i] = selectedIds[i];
                             else cohort.UnitIDs.Add(selectedIds[i]);
                         }
                         else 
                         {
                             if (cohort.UnitIDs.Count > i) cohort.UnitIDs[i] = "";
                             else cohort.UnitIDs.Add("");
                         }
                     }
                     RefreshUI();
                 });
                 UIFlowManager.Instance.OpenPanel(_unitSelectionController);
             }
             else
             {
                 Debug.LogError("UnitSelectionController reference not assigned!");
             }
        }
    }
}