using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI
{
    public class EnemyInspectorStatsPanel : MonoBehaviour
    {
        [Header("Combat Stats")]
        [SerializeField] private TextMeshProUGUI _atkText;
        [SerializeField] private TextMeshProUGUI _defText;
        [SerializeField] private TextMeshProUGUI _aspdText;
        [SerializeField] private TextMeshProUGUI _rangeText;
        
        [Header("Enemy Specific Stats")]
        [SerializeField] private TextMeshProUGUI _moveSpeedText;
        [SerializeField] private TextMeshProUGUI _exitDamageText;

        [Header("Unit Detail Icons")]
        [SerializeField] private Image _classIcon;
        [SerializeField] private RangePatternUI _rangeGridIcon;
        [SerializeField] private TextMeshProUGUI _rarityTextLabel; // Rank
        [SerializeField] private TextMeshProUGUI _tagRangeText;   // Damage Type
        [SerializeField] private TextMeshProUGUI _tagRoleText;    // Movement Type

        [Header("Abilities Section")]
        [SerializeField] private GameObject _ultimateSection;
        [SerializeField] private TextMeshProUGUI _ultimateNameText;
        [SerializeField] private TextMeshProUGUI _ultimateDescText;
        [SerializeField] private Image _ultimateIcon;
        [SerializeField] private RangePatternUI _ultimateRangeGrid;

        public void Refresh(EnemyUnit e)
        {
            if (e == null || e.EnemyData == null) return;
            var data = e.EnemyData;

            ConfigureAdaptiveStats();

            if (_atkText) _atkText.text = e.AttackPower.ToString("F0");
            if (_defText) _defText.text = e.Defense.ToString("F0");
            if (_aspdText) _aspdText.text = GetASPDLabel(e.AttackInterval);
            if (_rangeText) _rangeText.text = e.Range.ToString("F1");
            
            if (_moveSpeedText) _moveSpeedText.text = data.MoveSpeed.ToString("F1") + " blk/s";
            
            if (_exitDamageText)
            {
                string suffix = data.ExitDamageType == ExitDamageType.Percentage ? "% HP" : " HP";
                _exitDamageText.text = $"EXIT DMG: {data.ExitDamage}{suffix}";
            }

            if (_rarityTextLabel) 
            {
                _rarityTextLabel.text = data.Rank.ToString().ToUpper();
            }

            if (_tagRangeText) 
            {
                _tagRangeText.text = data.DamageType == DamageType.Melee ? "MELEE" : (data.DamageType == DamageType.Ranged ? "RANGED" : "MAGIC");
            }
            
            if (_rangeGridIcon) _rangeGridIcon.SetPattern(data.AttackPattern, (int)e.Range);
            
            if (_tagRoleText)
            {
                _tagRoleText.text = data.MovementType.ToString().ToUpper();
            }

            // Abilities & Immunities Info
            if (_ultimateSection) _ultimateSection.SetActive(true);
            
            string abilityText = "";
            
            if (e.Immunities != null && e.Immunities.Count > 0)
            {
                abilityText += $"<color=#00FF00><b>Immunities:</b></color>\n";
                foreach (var imm in e.Immunities)
                {
                    abilityText += $"- {imm.ToString()}\n";
                }
                abilityText += "\n";
            }
            
            if (data.Abilities != null && data.Abilities.Count > 0)
            {
                abilityText += $"<color=#FF8800><b>Abilities:</b></color>\n";
                foreach (var ab in data.Abilities)
                {
                    if (ab != null) abilityText += $"<b>{ab.name.Replace("EnemyAbility_", "").Replace("_", " ")}:</b> {ab.GetType().Name}\n";
                }
            }
            else
            {
                if (e.Immunities == null || e.Immunities.Count == 0)
                {
                    abilityText = "No special abilities or immunities.";
                }
            }

            if (_ultimateNameText) _ultimateNameText.text = "ENEMY DETAILS";
            if (_ultimateDescText) _ultimateDescText.text = abilityText.Trim();
            
            // Hide specific ultimate/icon stuff
            if (_ultimateIcon) _ultimateIcon.gameObject.SetActive(false);
            if (_ultimateRangeGrid) _ultimateRangeGrid.gameObject.SetActive(false);
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
