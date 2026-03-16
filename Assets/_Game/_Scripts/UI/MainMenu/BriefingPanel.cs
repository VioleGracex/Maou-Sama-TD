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
            Debug.Log($"[BriefingPanel] Setup called for level: {(level != null ? level.LevelName : "NULL")}");
            _currentLevel = level;
            _onEngageClicked = onEngageCallback;

            if (_titleText != null) _titleText.text = level.LevelName;
            if (_descriptionText != null) _descriptionText.text = level.Description;
            if (_rewardValueText != null) 
            {
                // Simple placeholder: display the first reward amount, or 0 if none.
                // You can expand this later to instantiate a prefab per reward!
                _rewardValueText.text = level.WinRewards != null && level.WinRewards.Count > 0 
                    ? level.WinRewards[0].Amount.ToString() 
                    : "0";
            }
        }

        public void Open()
        {
            Debug.Log($"[BriefingPanel] Open called.");
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
            Debug.Log($"[BriefingPanel] Close called. activeSelf: {(_visualRoot != null && _visualRoot.activeSelf)}");
            if (_visualRoot == null || !_visualRoot.activeSelf) return;

            _visualRoot.transform.DOScale(Vector3.zero, _animDuration / 2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => {
                _visualRoot.SetActive(false);
            });
        }

        public bool RequestClose() => true;

        public void ResetState()
        {
            // Reset state for Briefing Panel if needed
            _currentLevel = null;
            _onEngageClicked = null;
        }
        #endregion

        #region Private Methods
        private void OnEngage()
        {
            Debug.Log($"[BriefingPanel] OnEngage clicked! Launching level: {(_currentLevel != null ? _currentLevel.LevelName : "NULL")}");
            Close();
            _onEngageClicked?.Invoke(_currentLevel);
        }
        #endregion
    }
}
