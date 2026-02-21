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
        #region Variables
        [Header("UI Components")]
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => false;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _rewardValueText;
        [SerializeField] private Button _engageButton;
        
        [Header("Animation")]
        [SerializeField] private float _animDuration = 0.3f;
        [SerializeField] private Ease _animEase = Ease.OutBack;

        private LevelData _currentLevel;
        private Action<LevelData> _onEngageClicked;
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (_engageButton != null)
            {
                _engageButton.onClick.AddListener(OnEngage);
            }
        }
        #endregion

        #region Public Methods
        public void Setup(LevelData level, Action<LevelData> onEngageCallback)
        {
            _currentLevel = level;
            _onEngageClicked = onEngageCallback;

            if (_titleText != null) _titleText.text = level.LevelName;
            if (_descriptionText != null) _descriptionText.text = level.Description;
            if (_rewardValueText != null) _rewardValueText.text = level.RewardCurrency.ToString();
        }

        public void Open()
        {
            if (_visualRoot == null)
            {
                Debug.LogError($"[UIFlow] {gameObject.name} (BriefingPanel) cannot open! _visualRoot is not assigned in the Inspector.");
                return;
            }
            _visualRoot.SetActive(true);
            
            _visualRoot.transform.localScale = Vector3.zero;
            _visualRoot.transform.DOScale(Vector3.one, _animDuration).SetEase(_animEase).SetUpdate(true);
        }

        public void Close()
        {
            if (_visualRoot == null || !_visualRoot.activeSelf) return;

            _visualRoot.transform.DOScale(Vector3.zero, _animDuration / 2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => {
                _visualRoot.SetActive(false);
            });
        }
        #endregion

        #region Private Methods
        private void OnEngage()
        {
            Close();
            _onEngageClicked?.Invoke(_currentLevel);
        }
        #endregion
    }
}
