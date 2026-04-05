using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Levels;
using MaouSamaTD.Managers;
using MaouSamaTD.UI.Common;
using Zenject;

namespace MaouSamaTD.UI.MainMenu
{
    public class CampaignPage : MonoBehaviour, IUIController
    {
        [Header("UI Controller Architecture")]
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;
        [SerializeField] private NavigationFeatures _navFeatures = NavigationFeatures.BackButton | NavigationFeatures.CitadelButton;
        public NavigationFeatures ConfiguredNavFeatures => _navFeatures;

        [Header("References")]
        [SerializeField] private Transform _levelContainer;
        [SerializeField] private LevelButton _levelButtonPrefab;
        [SerializeField] private List<LevelData> _allLevels;
        [SerializeField] private BriefingPanel _briefingPanel;
        
        [SerializeField] private MaouSamaTD.UI.Cohorts.CohortSquadUI _cohortSquadUI;
        
        [Inject] private SaveManager _saveManager;

        private GenericListView<LevelDisplayData, LevelButton> _listView;

        private void OnEnable()
        {
            Refresh();
        }

        private void Awake()
        {
            _listView = new GenericListView<LevelDisplayData, LevelButton>(_levelContainer, _levelButtonPrefab);
        }

        private void Start()
        {
            Debug.Log("[CampaignPage]start");
            Refresh();
        }

        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            // Explicitly close sub-panels/overlays when the main page closes
            if (_briefingPanel != null) _briefingPanel.Close();
            if (_cohortSquadUI != null) _cohortSquadUI.Close();
        }

        public bool RequestClose() => true;

        public void Preheat()
        {
            // Ensure levels are loaded into memory
            if (_allLevels == null || _allLevels.Count == 0)
            {
                Debug.Log("[CampaignPage] Preheating: No levels assigned in inspector, checking database...");
                // In a real scenario, we might pull from a static database reference
                // _allLevels = AppEntryPoint.LoadedLevelDatabase.AllLevels;
            }
            
            // Validate save manager status
            if (_saveManager != null)
            {
                var data = _saveManager.CurrentData;
                Debug.Log($"[CampaignPage] Preheating: Save data loaded. Player: {data?.PlayerName}");
            }
        }

        public void ResetState()
        {
            // The user wanted pages to look fresh. For the Campaign Page, that means maybe clearing the briefing if it was open.
            if (_briefingPanel != null)
            {
                // Call hide/close on briefing if needed, but UIFlowManager handles full-screen pages.
                // Resetting scrollbars or similar visual states can go here in the future.
            }
        }

        public void Refresh()
        {
            if (_levelContainer == null || _levelButtonPrefab == null || _allLevels == null)
            {
                Debug.LogWarning("[CampaignPage] Missing references! Cannot spawn level buttons.");
                return;
            }

            Debug.Log($"[CampaignPage] Starting Refresh. Total levels in list: {_allLevels.Count}");

            List<LevelDisplayData> displayDataList = new List<LevelDisplayData>();
            for (int i = 0; i < _allLevels.Count; i++)
            {
                LevelData level = _allLevels[i];
                if (level == null) continue;

                displayDataList.Add(new LevelDisplayData
                {
                    Level = level,
                    Index = i,
                    IsLocked = !IsLevelUnlocked(level, i),
                    StarCount = GetLevelStars(level)
                });
            }

            _listView.UpdateContent(displayDataList, (btnComp) => {
                var btn = btnComp as LevelButton;
                if (btn != null) OnLevelClicked(btn.LevelDataForCallback); 
            });
        }

        private bool IsLevelUnlocked(LevelData level, int index)
        {
            if (index == 0) return true; // First level always unlocked
            
            // Check if previous level is completed
            var prevLevel = _allLevels[index - 1];
            if (prevLevel == null) return false;
            
            if (_saveManager == null) return false; // Fallback if SaveManager is missing
            
            return _saveManager.IsLevelCompleted(prevLevel.LevelID);
        }
        
        private int GetLevelStars(LevelData level)
        {
             if (level == null || _saveManager == null || _saveManager.CurrentData == null) return 0;

             var entry = _saveManager.CurrentData.LevelStars.Find(x => x.LevelID == level.LevelID);
             // Verify if we actually found it (default struct check)
             if (entry.LevelID == level.LevelID) return entry.Stars;
             return 0;
        }

        private void OnLevelClicked(LevelData level)
        {
            // Open Briefing as a popup window
            if (_briefingPanel != null)
            {
                MaouSamaTD.UI.UIFlowManager.Instance.OpenPanel(_briefingPanel);
                _briefingPanel.Setup(level, OnBriefingEngage);
            }
            else
            {
                Debug.LogWarning("[CampaignPage] Briefing Panel is null! Using fallback.");
                // Fallback direct
                OnBriefingEngage(level);
            }
        }
        private void OnBriefingEngage(LevelData level)
        {
            if (_cohortSquadUI != null)
            {
                // Give cohortSquadUI history priority so it hides campaign
                MaouSamaTD.UI.UIFlowManager.Instance.OpenPanel(_cohortSquadUI);

                // Ensure the scripts (CohortSquadUI, etc.) aren't deactivated when CampaignPage closes.
                GameObject readinessManager = _cohortSquadUI.gameObject;
                if (readinessManager.transform.parent != null && readinessManager.transform.parent.gameObject == gameObject)
                {
                    readinessManager.transform.SetParent(transform.parent, true);
                }

                Transform parent = transform.parent;
                if (parent != null)
                {
                    foreach (Transform child in transform)
                    {
                        if (child.GetComponent<IUIController>() != null)
                        {
                            child.SetParent(parent, true);
                        }
                    }
                }

                // Call OpenReadiness to initialize pre-battle constraints
                _cohortSquadUI.OpenReadiness(level);
            }
            else
            {
                Debug.LogError("[CampaignPage] Cohort Manager UI is not assigned in CampaignPage!");
            }
        }
    }
}
