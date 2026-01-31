using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Skills;
using MaouSamaTD.Managers;
using UnityEngine.EventSystems;

namespace MaouSamaTD.UI.Skills
{
    public class SkillButtonUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _cooldownOverlay;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _skillNameText;
        [SerializeField] private GameObject _lockOverlay;

        private SovereignRiteData _data;
        private SkillManager _manager;
        private InteractionManager _interactionManager;

        public void Initialize(SovereignRiteData data, SkillManager manager, InteractionManager interactionManager)
        {
            _data = data;
            _manager = manager;
            _interactionManager = interactionManager;

            if (_data != null)
            {
                if (_iconImage != null) _iconImage.sprite = _data.Icon;
                if (_costText != null) _costText.text = _data.SealCost.ToString();
                if (_skillNameText != null) _skillNameText.text = _data.SkillName;
            }
        }

        private void Update()
        {
            if (_data == null || _manager == null) return;

            // 1. Cooldown Update
            float progress = _manager.GetCooldownProgress(_data);
            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.fillAmount = progress;
            }

            // 2. Cost Check
            bool isReady = _manager.IsSkillReady(_data);
            // ... Visual updates
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_data == null) return;
            if (_manager != null && !_manager.IsSkillReady(_data))
            {
                return;
            }

            // Select Skill for Targeting
            if (_interactionManager != null)
            {
                _interactionManager.SelectSkill(_data);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Show Tooltip
            // TooltipManager.Show(_data.Description);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Hide Tooltip
        }
    }
}
