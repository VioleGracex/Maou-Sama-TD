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
        
        [Header("Ultimate Charge")]
        [SerializeField] private Image _ultChargeParent; // New: Fill Image for charging
        [SerializeField] private Image _ultChargeFill; // New: Fill Image for charging
        [SerializeField] private TextMeshProUGUI _ultChargeLabel; // New: Text for charging %
        
        [Header("Buttons")]
        [SerializeField] private Button _ultButton; // Ult_Btn
        [SerializeField] private Button _retreatButton; // Keeps existing functionality
        [SerializeField] private Button _closeButton;
        
        // Helper to access skill icon if it's on the button
        private Image _skillIcon;

        private PlayerUnit _selectedUnit;
        
        [Inject] private DeploymentUI _deploymentUI;

        public void Init()
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
                // Dynamic updates per frame using 500ms equivalent or just frame update
                // For charge bars, frame update is smoother
                UpdateChargeVisuals();
            }
        }
        
        private void UpdateChargeVisuals()
        {
            if (_selectedUnit == null || _selectedUnit.Data == null) return;
            
            float current = _selectedUnit.CurrentCharge;
            float max = _selectedUnit.MaxCharge;
            bool isFull = current >= max;

            // Toggle Button vs Charge Display
            if (_ultButton)
            {
                // If we want to hide the button object entirely or just disable it?
                // User said: "if full charge we show button... if empty we show image fill and label"
                // Assuming "show button" means the interactable part.
                
                // Let's assume the Button Object contains the button interaction. 
                // And we have a separate "Charging" value object (Fill + Label).
                // Or maybe they overlap. 
                
                // If they are separate:
                _ultButton.gameObject.SetActive(isFull);
            }
            
            if (_ultChargeParent)
            {
                _ultChargeParent.gameObject.SetActive(!isFull);
                if (!isFull && max > 0)
                {
                    _ultChargeFill.fillAmount = current / max;
                }
            }
            
            if (_ultChargeLabel)
            {
                _ultChargeLabel.gameObject.SetActive(!isFull);
                if (!isFull && max > 0)
                {
                    _ultChargeLabel.text = $"{(current/max):P0} Charging Skill";
                }
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
