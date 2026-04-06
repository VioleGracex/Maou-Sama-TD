using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Handles the combat stats listing and tactical semantic icons (Class, Range, Role).
    /// </summary>
    public class UnitInspectorStatsPanel : MonoBehaviour
    {
        [Header("Combat Stats")]
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _atkText;
        [SerializeField] private TextMeshProUGUI _defText;
        [SerializeField] private TextMeshProUGUI _resText;
        [SerializeField] private TextMeshProUGUI _blockText;
        [SerializeField] private TextMeshProUGUI _aspdText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _amityText;

        [Header("Unit Detail Icons")]
        [SerializeField] private Image _classIcon;
        [SerializeField] private RangePatternUI _rangeGridIcon;
        [SerializeField] private TextMeshProUGUI _rarityTextLabel; // e.g. "UR"
        [SerializeField] private TextMeshProUGUI _tagRangeText;   // Melee/Ranged
        [SerializeField] private TextMeshProUGUI _tagRoleText;    // Tank/DPS

        public void Refresh(UnitData u)
        {
            if (u == null) return;

            // Stats Listing
            if (_hpText) _hpText.text = u.MaxHp.ToString("F0");
            if (_atkText) _atkText.text = u.AttackPower.ToString("F0");
            if (_defText) _defText.text = u.Defense.ToString("F0");
            if (_resText) _resText.text = u.Resistance.ToString("F0");
            if (_blockText) _blockText.text = u.BlockCount.ToString();
            if (_aspdText) _aspdText.text = GetASPDLabel(u.AttackInterval);
            if (_costText) _costText.text = u.DeploymentCost.ToString();
            if (_amityText) _amityText.text = $"{(u.Amity * 100):F0}%";

            // Rarity Label
            if (_rarityTextLabel) 
            {
                _rarityTextLabel.text = u.Rarity.GetShortName();
            }

            // Damage Type Label
            if (_tagRangeText) 
            {
                _tagRangeText.text = u.DamageType == DamageType.Melee ? "MELEE" : "RANGED";
            }
            
            // Attack Pattern
            if (_rangeGridIcon) _rangeGridIcon.SetPattern(u.AttackPattern, (int)u.Range);
            
            // Tactical Role
            if (_tagRoleText)
            {
                string role = u.Class switch
                {
                    UnitClass.Bastion => "TANK",
                    UnitClass.Vanguard => "TANK",
                    UnitClass.Sage or UnitClass.Support or UnitClass.Architect or UnitClass.Necromancer => "SUPPORT",
                    _ => "DPS"
                };
                _tagRoleText.text = role;
            }
        }

        private string GetASPDLabel(float interval)
        {
            if (interval < 0.8f) return "Very Fast";
            if (interval < 1.1f) return "Fast";
            if (interval < 1.5f) return "Normal";
            if (interval < 2.0f) return "Slow";
            return "Very Slow";
        }
    }
}
