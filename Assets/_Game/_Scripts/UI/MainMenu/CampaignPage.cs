using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Levels;
using MaouSamaTD.Managers;
using Zenject;

namespace MaouSamaTD.UI.MainMenu
{
    public class CampaignPage : MonoBehaviour, IUIController
    {
        [Header("UI Controller Architecture")]
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;

        [Header("References")]
        [SerializeField] private Transform _levelContainer;
        [SerializeField] private LevelButton _levelButtonPrefab;
        [SerializeField] private List<LevelData> _allLevels;
        [SerializeField] private BriefingPanel _briefingPanel;
        
        [SerializeField] private MaouSamaTD.UI.CohortManagerPanel _cohortManagerUI;
        
        [Inject] private SaveManager _saveManager;

        private void OnEnable()
        {
            Refresh();
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
            if (_cohortManagerUI != null) _cohortManagerUI.Close();
        }

        public bool RequestClose() => true;

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
                Debug.LogWarning("[CampaignPage] Missing references! Cannot spawn level buttons. Container: " + (_levelContainer != null) + ", Prefab: " + (_levelButtonPrefab != null) + ", Levels: " + (_allLevels != null));
                return;
            }

            if (_saveManager == null)
            {
                Debug.LogWarning("[CampaignPage] SaveManager is null! Stages will spawn but unlock states might be inaccurate.");
            }

            Debug.Log($"[CampaignPage] Starting Refresh. Total levels in list: {_allLevels.Count}");

            // Clean up old buttons but keep the prefab if it's a child template
            foreach(Transform child in _levelContainer)
            {
                if (child.gameObject == _levelButtonPrefab.gameObject)
                {
                    child.gameObject.SetActive(false);
                    continue;
                }
                Destroy(child.gameObject);
            }

            int spawnedCount = 0;

            for (int i = 0; i < _allLevels.Count; i++)
            {
                LevelData level = _allLevels[i];
                if (level == null)
                {
                    Debug.LogWarning($"[CampaignPage] Level at index {i} is null in the _allLevels list!");
                    continue;
                }

                var btn = Instantiate(_levelButtonPrefab, _levelContainer);
                btn.gameObject.SetActive(true); // Ensure new instances are active
                
                bool isLocked = !IsLevelUnlocked(level, i);
                int stars = GetLevelStars(level);
                
                btn.Setup(level, i, isLocked, stars, OnLevelClicked);
                spawnedCount++;
            }
            
            Debug.Log($"[CampaignPage] Spawned levels from 0 to {spawnedCount}");
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
            if (_cohortManagerUI != null)
            {
                // Give cohortManagerUI history priority so it hides campaign
                MaouSamaTD.UI.UIFlowManager.Instance.OpenPanel(_cohortManagerUI);

                // Ensure the scripts (CohortManagerPanel, etc.) aren't deactivated when CampaignPage closes.
                GameObject readinessManager = _cohortManagerUI.gameObject;
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
                _cohortManagerUI.OpenReadiness(level);
            }
            else
            {
                Debug.LogError("[CampaignPage] Cohort Manager UI is not assigned in CampaignPage!");
            }
        }
    }
}
