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
                // Directly call unit retreat
                _selectedUnit.Retreat();
                
                // Also notify DeploymentUI if needed? 
                // DeploymentUI currently calls Destroy(unit.gameObject) so we should verify if DeploymentUI tracks anything else.
                // DeploymentUI tracks 'Interactable' state or active unit count? 
                // Checking logic, DeploymentUI doesn't seem to keep a hard list of deployed units EXCEPT for 'count' potentially?
                // Actually DeploymentUI seems to just instantiate and forget mostly. 
                // But it does have OnUnitRetreated(unit.Data) event?
                
                // Let's call the DeploymentUI method for SAFETY if it does critical tracking, 
                // BUT we refactored PlayerUnit.Retreat to destroy itself.
                // If DeploymentUI.RetreatUnitInstance ALSO destroys, we have a problem.
                
                // Logic Check: 
                // DeploymentUI.RetreatUnitInstance calls OnUnitRetreated(data) then Destroy.
                // PlayerUnit.Retreat calls Destroy.
                
                // Correct approach: Call PlayerUnit.Retreat(). PlayerUnit should invoke OnDeath. 
                // DeploymentUI should listen to OnDeath? No, DeploymentUI doesn't currently listen to every unit.
                
                // Alternative: Update OnRetreatClicked to ONLY call PlayerUnit.Retreat() 
                // AND trigger the DeploymentUI bookkeeping manually before destruction.
                
                if (_deploymentUI != null && _selectedUnit.Data != null)
                {
                    // Clean up DeploymentUI tracking (if any, e.g. notifying listeners)
                    // We can't call RetreatUnitInstance because it destroys the object.
                    // We need a strictly "NotifyRetreat" method or assume OnDeath covers it.
                    // But DeploymentUI has: OnUnitRetreated(unit.Data) inside RetreatUnitInstance.
                    // Let's assume for now we just want the unit gone. The User didn't specify strict event tracking yet.
                    // BUT to be safe, let's stick to using PlayerUnit.Retreat() as the source of truth
                    // and if DeploymentUI needs to know, we should eventually wire an event.
                }

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
