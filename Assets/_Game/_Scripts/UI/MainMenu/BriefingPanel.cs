using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Levels;
using System;
using DG.Tweening;

namespace MaouSamaTD.UI.MainMenu
{
    public class BriefingPanel : MonoBehaviour, IUIController
    {
        [Header("UI Components")]
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _rewardValueText;
        [SerializeField] private Button _engageButton;
        
        [Header("Animation")]
        [SerializeField] private float _animDuration = 0.3f;
        [SerializeField] private Ease _animEase = Ease.OutBack;

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

            // We do not call Open() here, we let the external system (like CampaignPage) 
            // call UIFlowManager.Instance.OpenPanel(this) later, which then calls Open().
        }

        public void Open()
        {
            if (_visualRoot == null) return;
            _visualRoot.SetActive(true);
            
            // DOTween pop-up animation
            _visualRoot.transform.localScale = Vector3.zero;
            _visualRoot.transform.DOScale(Vector3.one, _animDuration).SetEase(_animEase).SetUpdate(true);
        }

        private void OnEngage()
        {
            _onEngageClicked?.Invoke(_currentLevel);
            // Let the callback (usually MissionReadinessManager open) handle what comes next!
        }
        
        public void Close()
        {
            if (_visualRoot == null || !_visualRoot.activeSelf) return;

            _visualRoot.transform.DOScale(Vector3.zero, _animDuration / 2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => {
                _visualRoot.SetActive(false);
            });
        }
    }
}
