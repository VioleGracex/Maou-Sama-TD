using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zenject;

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


        [Inject] private MaouSamaTD.Managers.GameSelectionState _selectionState;
        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;

        private MaouSamaTD.Data.PlayerData _playerData;  
        private MaouSamaTD.Levels.LevelData _currentLevel;
        private bool _isLockedMode = false;
        private List<string> _lockedUnitIDs = new List<string>();
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (_startBattleButton != null) _startBattleButton.onClick.AddListener(OnStartBattle);
            if (_backButton != null) _backButton.onClick.AddListener(() => UIFlowManager.Instance.GoBack());
            if (_removeAllButton != null) _removeAllButton.onClick.AddListener(OnRemoveAllClicked);
            if (_barracksButton != null) _barracksButton.onClick.AddListener(OnBarracksClicked);

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
        #endregion

        #region Public Methods
        public void Open()
        {
            if (_visualRoot == null)
            {
                Debug.LogError($"[UIFlow] {gameObject.name} (MissionReadinessPanel) cannot open! _visualRoot is not assigned in the Inspector.");
                return;
            }
            _visualRoot.SetActive(true);
            RefreshUI();
        }

        public void Open(MaouSamaTD.Levels.LevelData level)
        {
            if (_visualRoot == null)
            {
                Debug.LogError($"[UIFlow] {gameObject.name} (MissionReadinessPanel) cannot open! _visualRoot is not assigned in the Inspector.");
                return;
            }
            _visualRoot.SetActive(true);

            _currentLevel = level;
            
            _isLockedMode = false;
            _lockedUnitIDs.Clear();
            
            if (_titleText != null)
            {
                _titleText.text = level != null ? level.LevelName : "MISSION READINESS";
            }
            
            if (_currentLevel != null)
            {
                _isLockedMode = _currentLevel.IsCohortLocked || (_currentLevel.PremadeCohort != null && _currentLevel.PremadeCohort.Count > 0);
                Debug.Log($"[MissionReadinessPanel] Opening level '{_currentLevel.LevelName}'. IsLockedMode: {_isLockedMode}");
                
                if (_currentLevel.PremadeCohort != null && _currentLevel.PremadeCohort.Count > 0)
                {
                    Debug.Log($"[MissionReadinessPanel] Premade cohort found. Units count: {_currentLevel.PremadeCohort.Count}");
                    foreach (var unit in _currentLevel.PremadeCohort)
                    {
                        string idToUse = "";
                        if (unit != null) 
                        {
                            idToUse = string.IsNullOrEmpty(unit.UniqueID) ? unit.name : unit.UniqueID;
                            Debug.Log($"[MissionReadinessPanel] Processing Cohort Unit: SO_Name={unit.name}, ID={unit.UniqueID}, IDToUse={idToUse}, Name={unit.UnitName}");
                        }
                        _lockedUnitIDs.Add(idToUse);
                    }
                }
                else
                {
                    Debug.Log($"[MissionReadinessPanel] Free cohort mode.");
                }
                
                int squadSize = 10;
                while (_lockedUnitIDs.Count < squadSize) _lockedUnitIDs.Add(""); 
                
                if (!_isLockedMode && _playerData != null && _playerData.Cohorts.Count > 0)
                {
                    var cohort = _playerData.Cohorts[_playerData.CurrentCohortIndex];
                    for(int i = 0; i < squadSize; i++)
                    {
                        if (cohort.UnitIDs.Count > i) cohort.UnitIDs[i] = _lockedUnitIDs[i];
                        else cohort.UnitIDs.Add(_lockedUnitIDs[i]); 
                    }
                }
            }

            SetUIState();
            RefreshUI();
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        public void ResetState()
        {
            // Specifically requested to not wipe the loaded data when re-opening
            // _currentLevel = null;
            // _isLockedMode = false;
            // if (_lockedUnitIDs != null) _lockedUnitIDs.Clear();
        }
        #endregion

        #region Private Methods
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
            
            // Slots interactivity is handled per-slot in RefreshUI based on locked assignments
        }

        private void SetupSlots()
        {
            for (int i = 0; i < _preassignedSlots.Count; i++)
            {
                var slot = _preassignedSlots[i];
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
            if (index < 10 && _isLockedMode) return;
            if (index == 10 && _currentLevel != null && _currentLevel.IsAssistantLocked) return;

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
                bool isSlotLocked = false;

                if (i < 10)
                {
                    if (_isLockedMode)
                    {
                        isSlotLocked = true;
                        if (_lockedUnitIDs.Count > i && !string.IsNullOrEmpty(_lockedUnitIDs[i]))
                        {
                            unitID = _lockedUnitIDs[i];
                        }
                    }
                    else if (_playerData.Cohorts.Count > 0)
                    {
                        var cohort = _playerData.Cohorts[_playerData.CurrentCohortIndex];
                        unitID = (i < cohort.UnitIDs.Count) ? cohort.UnitIDs[i] : "";
                    }
                }
                else if (i == 10) // Assistant Slot
                {
                    if (_currentLevel != null && _currentLevel.IsAssistantLocked)
                    {
                        unitID = (_currentLevel.SupportAssistant != null) ? _currentLevel.SupportAssistant.UniqueID : "";
                        isSlotLocked = true;
                    }
                    else if (_playerData.Cohorts.Count > 0)
                    {
                        var cohort = _playerData.Cohorts[_playerData.CurrentCohortIndex];
                        // Assuming 11th slot in cohort data, or handle independently if it doesn't exist
                        unitID = (i < cohort.UnitIDs.Count) ? cohort.UnitIDs[i] : "";
                    }
                }

                if (string.IsNullOrEmpty(unitID))
                {
                    slot.SetEmpty();
                    
                    Button slotBtn = slot.GetComponent<Button>();
                    if (slotBtn != null) slotBtn.interactable = !isSlotLocked;
                }
                else
                {
                    if (MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase != null)
                    {
                        var unitData = MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase.GetUnitByID(unitID);
                        if (unitData != null)
                        {
                            slot.SetUnit(unitData);
                            // Debug.Log($"[MissionReadinessPanel] Slot {i} populated with unit: {unitData.UnitName}");
                        }
                        else
                        {
                            Debug.LogError($"[MissionReadinessPanel] Slot {i} requested unitID '{unitID}' but it was not found in UnitDatabase.");
                            slot.SetEmpty();
                        }
                        
                        Button slotBtn = slot.GetComponent<Button>();
                        if (slotBtn != null) slotBtn.interactable = !isSlotLocked;
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

             List<MaouSamaTD.Units.UnitData> selectedUnits = new List<MaouSamaTD.Units.UnitData>();
             
             for (int i = 0; i < _preassignedSlots.Count; i++)
             {
                 string unitID = "";

                 if (i < 10)
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
                 else if (i == 10)
                 {
                     if (_currentLevel != null && _currentLevel.IsAssistantLocked)
                     {
                         unitID = (_currentLevel.SupportAssistant != null) ? _currentLevel.SupportAssistant.UniqueID : "";
                     }
                     else if (_playerData != null && _playerData.Cohorts.Count > 0)
                     {
                         var cohort = _playerData.Cohorts[_playerData.CurrentCohortIndex];
                         unitID = (i < cohort.UnitIDs.Count) ? cohort.UnitIDs[i] : "";
                     }
                 }

                 if (!string.IsNullOrEmpty(unitID) && MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase != null)
                 {
                     var unitData = MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase.GetUnitByID(unitID);
                     if (unitData != null) selectedUnits.Add(unitData);
                 }
             }

             if (selectedUnits.Count == 0)
             {
                 Debug.LogWarning("Starting battle with 0 units!");
             }

             if (_selectionState != null)
             {
                 _selectionState.SetLevel(_currentLevel);
                 _selectionState.SetCohort(selectedUnits);
             }

             var loader = Object.FindFirstObjectByType<MaouSamaTD.UI.MainMenu.LoadingScreenPanel>(FindObjectsInactive.Include);
             if (loader != null)
             {
                 loader.LoadSceneTransition("BattleScene");
             }
             else
             {
                 UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
             }
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
                 
                 int squadSize = Mathf.Min(10, _preassignedSlots.Count);

                 _unitSelectionController.OpenMultiSelect(cohort.UnitIDs, squadSize, (selectedIds) => 
                 {
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
        #endregion
    }
}