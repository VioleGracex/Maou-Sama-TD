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
        private BattleCurrencyManager _currencyManager;
        private InteractionManager _interactionManager;

        public void Initialize(SovereignRiteData data, SkillManager manager, InteractionManager interactionManager, BattleCurrencyManager currencyManager)
        {
            _data = data;
            _manager = manager;
            _interactionManager = interactionManager;
            _currencyManager = currencyManager;

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

            // 1. Cooldown & Cost Overlay Logic
            float cooldownProgress = _manager.GetCooldownProgress(_data);
            float fillAmount = 0f;

            if (cooldownProgress > 0)
            {
                fillAmount = cooldownProgress;
                if (_lockOverlay != null) _lockOverlay.SetActive(true);
            }
            else
            {
                // Ready / Energy Phase
                int currentSeals = _currencyManager != null ? _currencyManager.CurrentSeals : 999;
                
                if (currentSeals >= _data.SealCost)
                    fillAmount = 1f;
                else if (_data.SealCost > 0)
                    fillAmount = (float)currentSeals / _data.SealCost;
                else
                    fillAmount = 1f;

                // Lock only if not fully affordable
                bool canAfford = currentSeals >= _data.SealCost;
                if (_lockOverlay != null) _lockOverlay.SetActive(!canAfford); 
            }

            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.fillAmount = fillAmount;
            }
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
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }
    }
}
