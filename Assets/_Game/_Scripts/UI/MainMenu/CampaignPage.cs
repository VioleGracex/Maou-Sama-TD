using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Levels;
using MaouSamaTD.Managers;
using Zenject;

namespace MaouSamaTD.UI.MainMenu
{
    public class CampaignPage : MonoBehaviour
    {
        [SerializeField] private Transform _levelContainer;
        [SerializeField] private LevelButton _levelButtonPrefab;
        [SerializeField] private List<LevelData> _allLevels; 
        [SerializeField] private BriefingPanel _briefingPanel;
        
        [SerializeField] private MaouSamaTD.UI.MissionReadinessPanel _missionReadinessUI;
        
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
            // Open Briefing Panel first
            if (_briefingPanel != null)
            {
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
            if (_missionReadinessUI != null)
            {
                _missionReadinessUI.Open(level);
                // gameObject.SetActive(false); // Optional hide CampaignPage
            }
            else
            {
                Debug.LogError("[CampaignPage] Mission Readiness UI is not assigned in CampaignPage!");
            }
        }
    }
}
