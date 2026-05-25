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

        [SerializeField] private Image _amityFillImage;
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

        private void Awake()
        {
            AutoBind();
        }

        private void AutoBind()
        {
            var rootUI = GetComponentInParent<UnitInspectorFullScreenUI>();
            Transform searchRoot = rootUI != null ? rootUI.transform : this.transform.root;

            var texts = searchRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                string n = t.name.ToLower();
                if (_atkText == null && n.Contains("atk")) _atkText = t;
                else if (_defText == null && n.Contains("def")) _defText = t;
                else if (_aspdText == null && n.Contains("aspd")) _aspdText = t;
                else if (_rangeText == null && n.Contains("range") && !n.Contains("tag") && !n.Contains("ulti")) _rangeText = t;
                else if (_amityLevelText == null && n.Contains("amity")) _amityLevelText = t;
                else if (_rarityTextLabel == null && n.Contains("rarity")) _rarityTextLabel = t;
                else if (_tagRangeText == null && n.Contains("range") && n.Contains("tag")) _tagRangeText = t;
                else if (_tagRoleText == null && n.Contains("role") && n.Contains("tag")) _tagRoleText = t;
                else if (_ultimateNameText == null && n.Contains("ulti") && n.Contains("name")) _ultimateNameText = t;
                else if (_ultimateDescText == null && n.Contains("ulti") && n.Contains("desc")) _ultimateDescText = t;
            }

            var images = searchRoot.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                string n = img.name.ToLower();
                if (_classIcon == null && n.Contains("class")) _classIcon = img;
                else if (_ultimateIcon == null && n.Contains("ulti") && n.Contains("icon")) _ultimateIcon = img;
                else if (_amityFillImage == null && n.Contains("amity") && (n.Contains("fill") || n.Contains("bar"))) _amityFillImage = img;
            }

            if (_amityFillImage == null)
            {
                // Fallback to finding a child named "Fill" under something with "Amity"
                foreach (var img in images)
                {
                    if (img.name == "Fill" && img.transform.parent != null && img.transform.parent.name.Contains("Amity"))
                    {
                        _amityFillImage = img;
                        break;
                    }
                }
            }
            
            var grids = searchRoot.GetComponentsInChildren<RangePatternUI>(true);
            foreach (var g in grids)
            {
                string n = g.name.ToLower();
                if (_ultimateRangeGrid == null && n.Contains("ulti")) _ultimateRangeGrid = g;
                else if (_rangeGridIcon == null && !n.Contains("ulti")) _rangeGridIcon = g;
            }

            if (_ultimateSection == null)
            {
                foreach (Transform t in searchRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.ToLower().Contains("ultimate") && t != this.transform && t.GetComponent<TextMeshProUGUI>() == null)
                    {
                        _ultimateSection = t.gameObject;
                        break;
                    }
                }
            }
        }

        public void Refresh(UnitData u)
        {
            if (u == null) return;

            ConfigureAdaptiveStats();

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
            if (_amityFillImage != null)
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

                _amityFillImage.fillAmount = progressInLevel;
                
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
                
                if (_ultimateDescText)
                {
                    string baseDesc = u.UltimateSkill.GetFormattedDescription();
                    string rangeStr = GetRangeDescription(u);
                    _ultimateDescText.text = $"{baseDesc}\n\n<color=#00FFFF>Range: {rangeStr}</color>";
                }

                if (_ultimateIcon) 
                {
                    _ultimateIcon.sprite = u.UltimateSkill.Icon;
                }
                
                // Range grid is hidden in favor of text description as requested
                if (_ultimateRangeGrid) _ultimateRangeGrid.gameObject.SetActive(false);
            }
            else
            {
                if (_ultimateSection) _ultimateSection.SetActive(false);
            }
        }

        private string GetRangeDescription(UnitData u)
        {
            if (u.AttackPattern == AttackPattern.Custom) return "All Map";
            
            string patternName = u.AttackPattern switch
            {
                AttackPattern.Vertical => "Column",
                AttackPattern.Horizontal => "Row",
                AttackPattern.Diagonal => "Diagonal",
                AttackPattern.Cross => "Cross",
                AttackPattern.All => "Surrounding",
                _ => "Target"
            };

            return $"{patternName} (Range: {u.Range:F0})";
        }

        private string GetASPDLabel(float interval)
        {
            if (interval <= 0) return "0/sec";
            float attacksPerSec = 1.0f / interval;
            return $"{attacksPerSec:F1}/sec";
        }

        private void ConfigureAdaptiveStats()
        {
            var allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in allTexts)
            {
                if (t == null) continue;
                if (t.name.Contains("Label") || t.name.Contains("Value") || t.name.Contains("Txt") || t.name.Contains("Text") || t.name.Contains("aspd") || t.name.Contains("atk") || t.name.Contains("def") || t.name.Contains("range"))
                {
                    t.enableAutoSizing = true;
                    t.fontSizeMin = 8;
                    t.fontSizeMax = Mathf.Min(t.fontSizeMax > 0 ? t.fontSizeMax : 20, 20);
                }
            }
        }
    }
}
