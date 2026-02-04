using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Levels;
using System;

namespace MaouSamaTD.UI.MainMenu
{
    public class BriefingPanel : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _rewardValueText;
        [SerializeField] private Button _engageButton;
        
        private LevelData _currentLevel;
        private Action<LevelData> _onEngageClicked;

        private void Start()
        {
            if (_engageButton != null)
            {
                _engageButton.onClick.AddListener(OnEngage);
            }
        }

        public void Setup(LevelData level, Action<LevelData> onEngageCallback)
        {
            _currentLevel = level;
            _onEngageClicked = onEngageCallback;

            if (_titleText != null) _titleText.text = level.LevelName; // Or use "1-1 THE OBSIDIAN..." format if preferred
            if (_descriptionText != null) _descriptionText.text = level.Description;
            if (_rewardValueText != null) _rewardValueText.text = level.RewardCurrency.ToString();

            gameObject.SetActive(true);
        }

        private void OnEngage()
        {
            _onEngageClicked?.Invoke(_currentLevel);
            // Optionally hide self or let the manager handle it
            gameObject.SetActive(false);
        }
        
        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
