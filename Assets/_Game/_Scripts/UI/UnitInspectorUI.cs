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
        
        [Header("Range Shape")]
        [SerializeField] private RangePatternUI _rangePatternUI;

        [Header("Ultimate Charge")]
        [SerializeField] private Image _ultChargeParent;
        [SerializeField] private Image _ultChargeFill;
        [SerializeField] private TextMeshProUGUI _ultChargeLabel;
        
        [Header("Buttons")]
        [SerializeField] private Button _ultButton; // Ult_Btn
        [SerializeField] private Button _retreatButton; // Keeps existing functionality
        [SerializeField] private Button _closeButton;
        
        // Helper to access skill icon if it's on the button
        private Image _skillIcon;
        private PlayerUnit _selectedUnit;
        public event System.Action OnPanelHidden;
        
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
            if (_selectedUnit != null) _selectedUnit.SetHighlight(false, Color.white); // Clear old

            _selectedUnit = unit;
            if (_selectedUnit != null)
            {
                _selectedUnit.SetHighlight(true, Color.yellow); // Highlight new
                UpdateVisuals();
                if (_panel != null)
                {
                    _panel.SetActive(true);
                    _panel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack); // Animate In
                }
            }
            else
            {
                Hide(); // If unit is null, hide the inspector
            }
        }

        public void Hide()
        {
            OnPanelHidden?.Invoke();

            if (_panel != null && _panel.activeSelf)
            {
                _panel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => 
                {
                    _panel.SetActive(false);
                    if (_selectedUnit != null) _selectedUnit.SetHighlight(false, Color.white); // Clear highlight
                    _selectedUnit = null;
                });
            }
            else
            {
                if (_selectedUnit != null) _selectedUnit.SetHighlight(false, Color.white); // Clear highlight
                _selectedUnit = null;
            }
        }

        private void Update()
        {
            if (_selectedUnit != null && _panel.activeSelf)
            {
                // Dynamic updates per frame using 500ms equivalent or just frame update
                // For charge bars, frame update is smoother
                // Dynamic updates per frame using 500ms equivalent or just frame update
                // For charge bars, frame update is smoother
                UpdateChargeVisuals();
                UpdateVisuals(); // Fix: Update HP and other stats live
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
            if (_rangeText != null) _rangeText.text = $"{data.Range}";

            if (_rangePatternUI != null)
            {
                _rangePatternUI.SetPattern(data.AttackPattern, (int)data.Range);
            }

            // Ult Button Icon
            if (_skillIcon != null)
            {
                var icon = data.Skill != null ? data.Skill.Icon : null;
                _skillIcon.sprite = icon;
                _skillIcon.enabled = icon != null;
            }
        }

       

        private void OnRetreatClicked()
        {
            if (_selectedUnit != null)
            {
                _selectedUnit.Retreat();
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
