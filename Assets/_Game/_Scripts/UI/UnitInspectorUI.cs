using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Units;
using MaouSamaTD.Managers;
using DG.Tweening;
using Zenject;

namespace MaouSamaTD.UI
{
    public class UnitInspectorUI : MonoBehaviour
    {
        [Header("UI References (Hierarchy Match)")]
        [SerializeField] private GameObject _panel; // Stats_BG_Panel
        [SerializeField] private TextMeshProUGUI _vassalStatsText; // Vassal_Stats_Txt
        [SerializeField] private TextMeshProUGUI _unitNameText; // Stats_Unit_Name_Txt
        [SerializeField] private TextMeshProUGUI _vitalityLabelText; // Stats_Vitality_Txt
        [SerializeField] private TextMeshProUGUI _hpNumberText; // Stats_HP_Number_Txt
        [SerializeField] private Image _hpBarImage; // Stats_HPBar
        
        [SerializeField] private TextMeshProUGUI _dmgText; // Inside Stats_Dmg_BG
        [SerializeField] private TextMeshProUGUI _rangeText; // Inside Stats_Range_BG
        
        [Header("Buttons")]
        [SerializeField] private Button _ultButton; // Ult_Btn
        [SerializeField] private Button _retreatButton; // Keeps existing functionality
        [SerializeField] private Button _closeButton;
        
        // Helper to access skill icon if it's on the button
        private Image _skillIcon;

        private PlayerUnit _selectedUnit;
        
        [Inject] private DeploymentUI _deploymentUI;

        private void Awake()
        {

            if (_panel != null) 
            {
                _panel.SetActive(false);
                _panel.transform.localScale = Vector3.zero; // Start hidden
            }
            
            if (_retreatButton) _retreatButton.onClick.AddListener(OnRetreatClicked);
            if (_ultButton) 
            {
                _ultButton.onClick.AddListener(OnSkillClicked);
            }
            if (_closeButton) _closeButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            if (_retreatButton) _retreatButton.onClick.RemoveListener(OnRetreatClicked);
            if (_ultButton) _ultButton.onClick.RemoveListener(OnSkillClicked);
            if (_closeButton) _closeButton.onClick.RemoveListener(Hide);
        }

        public void Show(PlayerUnit unit)
        {
            if (unit == null) return;
            _selectedUnit = unit;
            
            if (_panel != null)
            {
                _panel.SetActive(true);
                _panel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            } 
            
            UpdateVisuals();
        }

        public void Hide()
        {
            if (_panel != null && _panel.activeSelf)
            {
                _panel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => 
                {
                    _panel.SetActive(false);
                    _selectedUnit = null;
                });
            }
            else
            {
                _selectedUnit = null;
            }
        }

        private void Update()
        {
            if (_selectedUnit != null && _panel.activeSelf)
            {
                // Update dynamic stats if needed (like current HP)
                // For now, static update on Show is mostly enough, unless HP changes visibly here.
            }
        }

        private void UpdateVisuals()
        {
            if (_selectedUnit == null) return;
            UnitData data = _selectedUnit.Data;

            if (_unitNameText != null) _unitNameText.text = data.UnitName;

            // HP Bar & Number
            if (_hpNumberText != null) 
                _hpNumberText.text = $"{_selectedUnit.CurrentHp}/{_selectedUnit.MaxHp}";
            
            if (_hpBarImage != null && _selectedUnit.MaxHp > 0)
                _hpBarImage.fillAmount = _selectedUnit.CurrentHp / _selectedUnit.MaxHp;

            // General Stats
            if (_vassalStatsText != null)
            {
                 // Keep summary if needed, or clear it if specialized texts handle it
                 _vassalStatsText.text = "Stats Summary";
            }
            
            if (_dmgText != null) _dmgText.text = $"ATK: {_selectedUnit.AttackPower}";
            if (_rangeText != null) _rangeText.text = $"RNG: {_selectedUnit.Range}";

            // Ult Button Icon
            if (_skillIcon != null)
            {
                _skillIcon.sprite = data.SkillIcon;
                _skillIcon.enabled = data.SkillIcon != null;
            }
        }

        private void OnRetreatClicked()
        {
            if (_selectedUnit != null)
            {
                // Call DeploymentUI to handle retreat (cooldowns, etc)
                if (_deploymentUI != null)
                {
                    _deploymentUI.RetreatUnitInstance(_selectedUnit);
                }
                Hide();
            }
        }

        private void OnSkillClicked()
        {
            if (_selectedUnit != null)
            {
                _selectedUnit.UseSkill();
                // Add Cooldown/Feedback later
            }
        }
    }
}
