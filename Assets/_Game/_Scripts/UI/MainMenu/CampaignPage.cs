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
        [SerializeField] private CohortSelectionUI _cohortSelectionUI; 
        [SerializeField] private BriefingPanel _briefingPanel;
        
        [Inject] private SaveManager _saveManager;

        private void Start()
        {
            if (_saveManager != null)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            // Clean up old buttons
            foreach(Transform child in _levelContainer)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < _allLevels.Count; i++)
            {
                LevelData level = _allLevels[i];
                var btn = Instantiate(_levelButtonPrefab, _levelContainer);
                
                bool isLocked = !IsLevelUnlocked(level, i);
                int stars = GetLevelStars(level);
                
                btn.Setup(level, i, isLocked, stars, OnLevelClicked);
            }
        }

        private bool IsLevelUnlocked(LevelData level, int index)
        {
            if (index == 0) return true; // First level always unlocked
            
            // Check if previous level is completed
            var prevLevel = _allLevels[index - 1];
            return _saveManager.IsLevelCompleted(prevLevel.LevelID);
        }
        
        private int GetLevelStars(LevelData level)
        {
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
                // Fallback direct
                OnBriefingEngage(level);
            }
        }
        
        private void OnBriefingEngage(LevelData level)
        {
            if (_cohortSelectionUI != null)
            {
                _cohortSelectionUI.Open(level);
                // gameObject.SetActive(false); // Optional hide CampaignPage
            }
        }
    }
}
