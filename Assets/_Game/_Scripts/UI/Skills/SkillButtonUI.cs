using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Skills;
using MaouSamaTD.Managers;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace MaouSamaTD.UI.Skills
{
    public class SkillButtonUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI References")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _cooldownOverlay;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _skillNameText;
        [SerializeField] private Image _sealsFillBar;
        [SerializeField] private TextMeshProUGUI _cooldownText;
        [SerializeField] private GameObject _lockOverlay;
        [SerializeField] private GameObject _toggledGlow;
        
        private Material _glowMat;
        private static readonly int CustomTimeProp = Shader.PropertyToID("_CustomTime");
        private static readonly int RandomOffsetProp = Shader.PropertyToID("_RandomOffset");

        private SovereignRiteData _data;
        private SkillManager _manager;
        private BattleCurrencyManager _currencyManager;
        private InteractionManager _interactionManager;
        private SkillPanelUI _panel;

        private Vector2 _dragStartPos;
        private bool _isDraggingForSwap = false;

        public void Initialize(SovereignRiteData data, SkillManager manager, InteractionManager interactionManager, BattleCurrencyManager currencyManager, SkillPanelUI panel)
        {
            _data = data;
            _manager = manager;
            _interactionManager = interactionManager;
            _currencyManager = currencyManager;
            _panel = panel;

            if (_data != null)
            {
                if (_iconImage != null)
                {
                    _iconImage.sprite = _data.Icon;
                    _iconImage.gameObject.SetActive(_data.Icon != null);
                }
                if (_costText != null) _costText.text = _data.SealCost.ToString();
                if (_skillNameText != null) _skillNameText.text = _data.SkillName;

                if (_toggledGlow != null)
                {
                    var img = _toggledGlow.GetComponent<Image>();
                    if (img != null)
                    {
                        // Clone the material so each button can have a unique random offset
                        img.material = new Material(img.material);
                        _glowMat = img.material;
                        _glowMat.SetFloat(RandomOffsetProp, Random.value * 10f);
                    }
                }
            }
        }

        private void Update()
        {
            if (_data == null || _manager == null) return;

            // Shader Animation (Unscaled)
            if (_toggledGlow != null && _toggledGlow.activeSelf && _glowMat != null)
            {
                _glowMat.SetFloat(CustomTimeProp, Time.unscaledTime);
            }

            // 1. Cooldown Logic
            float cooldownRemaining = _manager.GetRemainingCooldown(_data);
            bool isOnCooldown = cooldownRemaining > 0;

            if (_cooldownOverlay != null)
            {
                float cooldownProgress = _manager.GetCooldownProgress(_data);
                _cooldownOverlay.fillAmount = isOnCooldown ? cooldownProgress : 0f;
            }

            if (_cooldownText != null)
            {
                _cooldownText.gameObject.SetActive(isOnCooldown);
                if (isOnCooldown)
                {
                    _cooldownText.text = cooldownRemaining.ToString("F1");
                }
            }

            // 2. Seal Cost Logic
            int currentSeals = _currencyManager != null ? _currencyManager.CurrentSeals : 0;
            float sealsProgress = 0f;
            if (_data.SealCost > 0)
            {
                sealsProgress = Mathf.Clamp01((float)currentSeals / _data.SealCost);
            }
            else
            {
                sealsProgress = 1f;
            }

            if (_sealsFillBar != null)
            {
                _sealsFillBar.fillAmount = sealsProgress;
                
                // Debug log to catch the 0 issue if it persists
                if (sealsProgress <= 0 && currentSeals > 0) 
                    Debug.LogWarning($"[SkillButtonUI] {_data.SkillName} sealsProgress is 0 but currentSeals={currentSeals}. Cost={_data.SealCost}");
            }

            // 3. Lock Overlay Logic
            bool canAfford = currentSeals >= _data.SealCost;
            bool isReady = !isOnCooldown && canAfford;
            
            if (_costText != null)
            {
                // Vibrant red for better visibility
                _costText.color = canAfford ? Color.white : new Color(1f, 0.2f, 0.2f);
            }
            
            bool permanentlyLocked = _currencyManager != null && _currencyManager.MaxSeals < _data.SealCost;

            if (_lockOverlay != null)
            {
                _lockOverlay.SetActive(permanentlyLocked);
            }

            // 4. Selection Glow: Only show if selected AND ready
            bool isSelected = _interactionManager != null && _interactionManager.SelectedSkill == _data;
            if (_toggledGlow != null) _toggledGlow.SetActive(isSelected && isReady);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_data == null || _interactionManager == null) return;

            // Simple Toggle Logic:
            // 1. If not selected -> Select (Shows Desc + Enters Targeting)
            // 2. If already selected -> Deselect (Back to neutral)
            
            bool isCurrentSelected = (_interactionManager.SelectedSkill == _data);

            if (!isCurrentSelected)
            {
                // Left Click: Select and enter targeting mode immediately
                _interactionManager.SelectSkill(_data);
            }
            else
            {
                // Click again: Cancel
                _interactionManager.DeselectSkill();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_data == null || _interactionManager == null) return;
            if (_manager != null && !_manager.IsSkillReady(_data)) return;

            // Dragging activates targeting immediately
            if (_interactionManager.SelectedSkill != _data)
            {
                _interactionManager.SelectSkill(_data);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            // InteractionManager handles map preview automatically
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Release-to-cast is handled by InteractionManager.Update
        }
    }
}
