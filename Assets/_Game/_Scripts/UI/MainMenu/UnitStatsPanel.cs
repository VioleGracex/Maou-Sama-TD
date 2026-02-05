using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI.MainMenu
{
    public class UnitStatsPanel : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image _unitPortrait;
        [SerializeField] private Image _classIcon;
        [SerializeField] private TextMeshProUGUI _unitNameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _atkText;
        [SerializeField] private TextMeshProUGUI _defText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _blockText;
        [SerializeField] private TextMeshProUGUI _respawnText;
        [SerializeField] private TextMeshProUGUI _skillNameText;
        [SerializeField] private TextMeshProUGUI _skillDescText;

        public void Setup(UnitData unit)
        {
            if (unit == null)
            {
                // Clear or Hide
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (_unitNameText) _unitNameText.text = unit.UnitName;
            if (_unitPortrait) 
            {
                _unitPortrait.sprite = unit.UnitSprite;
                _unitPortrait.gameObject.SetActive(unit.UnitSprite != null);
            }
            
            // Basic Stats
            if (_hpText) _hpText.text = $"{unit.MaxHp}";
            if (_atkText) _atkText.text = $"{unit.AttackPower}";
            if (_defText) _defText.text = $"{unit.Defense}";
            if (_costText) _costText.text = $"{unit.DeploymentCost}";
            if (_blockText) _blockText.text = $"{unit.BlockCount}";
            if (_respawnText) _respawnText.text = $"{unit.RespawnTime}s";

            // Level (Placeholder for now, standard units Level 1)
            if (_levelText) _levelText.text = "Lv 1"; // Future: Fetch from SaveManager

            // Skill
            if (unit.Skill != null)
            {
                if (_skillNameText) _skillNameText.text = unit.Skill.SkillName;
                if (_skillDescText) _skillDescText.text = unit.Skill.Description;
            }
            else
            {
                if (_skillNameText) _skillNameText.text = "None";
                if (_skillDescText) _skillDescText.text = "";
            }
        }
    }
}
