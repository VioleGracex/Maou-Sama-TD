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
        [Inject] private CurrencyManager _currencyManager;
        
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
            if (_panelRect != null) 
            {
                _visiblePos = _panelRect.anchoredPosition;
                // Force initial position to Hidden (Docked)
                _panelRect.anchoredPosition = _visiblePos + new Vector2(_hideOffset, 0); 
            }
            if (_toggleButton != null)
            {
                 var txt = _toggleButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                 if (txt != null) txt.text = "Show"; // Initial state is Hidden, so button says "Show"
            }
        }

        public void Init(List<SovereignRiteData> skills)
        {
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
                    // If IS Visible, we want to Hide -> Button says "Hide" ?? 
                    // Or Button says what it DOES?
                    // Usually button says action.
                    // If Visible, Action is Hide.
                    // If Hidden, Action is Show.
                    txt.text = _isVisible ? "Hide" : "Show"; 
                }
            }
        }
    }
}
