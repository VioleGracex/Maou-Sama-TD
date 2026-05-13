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
        [SerializeField] private TextMeshProUGUI _atkText;
        [SerializeField] private TextMeshProUGUI _defText;
        [SerializeField] private TextMeshProUGUI _aspdText;
        [SerializeField] private TextMeshProUGUI _rangeText;

        [SerializeField] private Slider _amitySlider;
        [SerializeField] private TextMeshProUGUI _amityLevelText;

        [Header("Unit Detail Icons")]
        [SerializeField] private Image _classIcon;
        [SerializeField] private RangePatternUI _rangeGridIcon;
        [SerializeField] private TextMeshProUGUI _rarityTextLabel; // e.g. "UR"
        [SerializeField] private TextMeshProUGUI _tagRangeText;   // Melee/Ranged
        [SerializeField] private TextMeshProUGUI _tagRoleText;    // Tank/DPS

        [Header("Ultimate Info Section")]
        [SerializeField] private GameObject _ultimateSection;
        [SerializeField] private TextMeshProUGUI _ultimateNameText;
        [SerializeField] private TextMeshProUGUI _ultimateDescText;
        [SerializeField] private Image _ultimateIcon;
        [SerializeField] private RangePatternUI _ultimateRangeGrid;

        public void Refresh(UnitData u)
        {
            if (u == null) return;

            // Stats Listing
            float displayHp = u.CalculatedStats.MaxHp > 0 ? u.CalculatedStats.MaxHp : u.MaxHp * 2f;
            float displayAtk = u.CalculatedStats.Attack > 0 ? u.CalculatedStats.Attack : u.AttackPower * 2f;
            float displayDef = u.CalculatedStats.Defense > 0 ? u.CalculatedStats.Defense : u.Defense * 2f;

            if (_atkText) _atkText.text = displayAtk.ToString("F0");
            if (_defText) _defText.text = displayDef.ToString("F0");
            if (_aspdText) _aspdText.text = GetASPDLabel(u.AttackInterval);
            if (_rangeText) _rangeText.text = u.Range.ToString("F1");
            
            // Note: _hpText is handled by the main manager usually, but we can set it if wired
            // if (_hpText) _hpText.text = displayHp.ToString("F0");
            
            // Amity Slider Logic
            if (_amitySlider != null)
            {
                // Assuming 5 levels of Amity, each 20%
                float levelStep = 0.2f;
                int level = Mathf.FloorToInt(u.Amity / levelStep) + 1;
                level = Mathf.Clamp(level, 1, 5);
                
                float progressInLevel = (u.Amity % levelStep) / levelStep;
                if (u.Amity >= 1.0f) 
                {
                    level = 5;
                    progressInLevel = 1.0f;
                }

                _amitySlider.value = progressInLevel;
                
                if (_amityLevelText)
                {
                    if (u.Amity >= 1.0f)
                    {
                        _amityLevelText.text = "LEVEL MAX";
                    }
                    else
                    {
                        float totalPoints = 100f; // Scale to 100 for display
                        float pointsInLevel = (u.Amity % levelStep) * totalPoints;
                        float pointsToNext = (levelStep * totalPoints) - pointsInLevel;
                        
                        _amityLevelText.text = $"LEVEL {level} ({pointsToNext:F0} to next)";
                    }
                }
            }

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

            // Ultimate Skill Info
            if (u.UltimateSkill != null)
            {
                if (_ultimateSection) _ultimateSection.SetActive(true);
                if (_ultimateNameText) _ultimateNameText.text = u.UltimateSkill.SkillName?.ToUpper();
                if (_ultimateDescText) _ultimateDescText.text = u.UltimateSkill.GetFormattedDescription();
                if (_ultimateIcon) 
                {
                    var visuals = u.UltimateSkill.GetVisuals(u.EquippedSkinID);
                    _ultimateIcon.sprite = u.UltimateSkill.Icon;
                }
                if (_ultimateRangeGrid)
                {
                    // For ultimate range, we might need a custom pattern or just use a default
                    // Assuming ultimate range is fixed or handled similarly
                    _ultimateRangeGrid.SetPattern(u.AttackPattern, (int)u.Range); // Placeholder logic
                }
            }
            else
            {
                if (_ultimateSection) _ultimateSection.SetActive(false);
            }
        }

        private string GetASPDLabel(float interval)
        {
            if (interval <= 0) return "0/sec";
            float attacksPerSec = 1.0f / interval;
            return $"{attacksPerSec:F1}/sec";
        }
    }
}
