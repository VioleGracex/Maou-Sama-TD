using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Skills;
using DG.Tweening;
using Zenject;
using MaouSamaTD.Managers;

namespace MaouSamaTD.UI.Skills
{
    public class SkillPanelUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private Transform _buttonContainer;
        [SerializeField] private SkillButtonUI _buttonPrefab;
        private List<SovereignRiteData> _skillsToDisplay = new List<SovereignRiteData>();

        [Header("Animation")]
        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private UnityEngine.UI.Button _toggleButton;
        [SerializeField] private float _hideOffset = 300f; // Distance to move right
        
        [Inject] private SkillManager _skillManager;
        [Inject] private InteractionManager _interactionManager;
        [Inject] private BattleCurrencyManager _currencyManager;
        [Inject] private MaouSamaTD.Managers.GameSelectionState _gameSelectionState;
        
        private List<SkillButtonUI> _spawnedButtons = new List<SkillButtonUI>();
        private bool _isVisible = false; // Default: Docked/Hidden
        private Vector2 _visiblePos;

        private void OnEnable()
        {
            if (_toggleButton != null)
                _toggleButton.onClick.AddListener(ToggleVisibility);
        }

        private void OnDisable()
        {
            if (_toggleButton != null)
                _toggleButton.onClick.RemoveListener(ToggleVisibility);
        }

        private void Start()
        {
            CheckTutorialDock();

            if (_panelRect != null) 
            {
                _visiblePos = _panelRect.anchoredPosition;
                // Force initial position to Hidden (Docked)
                _panelRect.anchoredPosition = _visiblePos + new Vector2(_hideOffset, 0); 
            }
            if (_toggleButton != null)
            {
                 _toggleButton.gameObject.name = "SovereignRiteToggle";
                 var txt = _toggleButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                 if (txt != null) txt.text = "Show"; // Initial state is Hidden, so button says "Show"
            }
        }

        private void CheckTutorialDock()
        {
            if (_gameSelectionState != null && _gameSelectionState.SelectedLevel != null)
            {
                if (_gameSelectionState.SelectedLevel.LevelIndex == 1 || _gameSelectionState.SelectedLevel.LevelID == "1-1")
                {
                    // Fully dock and disable
                    if (_toggleButton != null) _toggleButton.gameObject.SetActive(false);
                    gameObject.SetActive(false);
                }
            }
        }

        public void Init(List<SovereignRiteData> skills)
        {
            CheckTutorialDock();

            if (skills == null || skills.Count == 0)
            {
                if (_toggleButton != null) _toggleButton.gameObject.SetActive(false);
                gameObject.SetActive(false);
            }

            if (!gameObject.activeSelf) return;

            _skillsToDisplay.Clear();
            if (skills != null)
            {
                _skillsToDisplay.AddRange(skills);
            }
            Refresh();
        }

        public void Refresh()
        {
            // Clear old
            foreach (var btn in _spawnedButtons) Destroy(btn.gameObject);
            _spawnedButtons.Clear();

            // Spawn new
            foreach (var skill in _skillsToDisplay)
            {
                if (skill == null) continue;
                
                var btn = Instantiate(_buttonPrefab, _buttonContainer);
                btn.Initialize(skill, _skillManager, _interactionManager, _currencyManager);
                
                // Name the button based on skill type for Tutorial Targeting
                string btnName = "SkillButton_Unknown";
                string effectStr = skill.EffectType.ToString();
                
                if (effectStr.Contains("AOE")) btnName = "SkillButton_AOE";
                else if (effectStr.Contains("ST_Buff")) btnName = "SkillButton_BUFF";
                else if (effectStr.Contains("ST_")) btnName = "SkillButton_ST";
                
                btn.gameObject.name = btnName;

                _spawnedButtons.Add(btn);
            }
        }

        public void ToggleVisibility()
        {
            if (_panelRect == null) return;
            
            _isVisible = !_isVisible;
            
            // Move Right on Hide
            Vector2 targetPos = _isVisible ? _visiblePos : _visiblePos + new Vector2(_hideOffset, 0);
            
            _panelRect.DOAnchorPos(targetPos, 0.3f).SetEase(Ease.OutBack);
            
            // Fix: Update Text
            if (_toggleButton != null)
            {
                var txt = _toggleButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = _isVisible ? "Hide" : "Show"; 
                }
            }
        }
    }
}
