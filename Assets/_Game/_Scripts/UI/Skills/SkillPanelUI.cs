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
        [SerializeField] private List<SkillData> _skillsToDisplay; // Could be dynamic later

        [Header("Animation")]
        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private bool _isDockedOnRight = true;
        
        [Inject] private SkillManager _skillManager;
        [Inject] private InteractionManager _interactionManager;
        
        private List<SkillButtonUI> _spawnedButtons = new List<SkillButtonUI>();
        private bool _isVisible = true;

        private void Start()
        {
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
                btn.Initialize(skill, _skillManager, _interactionManager);
                _spawnedButtons.Add(btn);
            }
        }

        public void ToggleVisibility()
        {
            _isVisible = !_isVisible;
            float targetX = _isVisible ? 0 : (_isDockedOnRight ? _panelRect.rect.width : -_panelRect.rect.width);
            
            _panelRect.DOAnchorPosX(targetX, 0.3f).SetEase(Ease.OutBack);
        }
    }
}
