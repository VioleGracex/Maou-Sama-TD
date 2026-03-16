using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI.Vassals
{
    /// <summary>
    /// Detailed inspection panel for a single unit.
    /// Shows stats, skills, and provides upgrade options.
    /// </summary>
    public class UnitInspectorPanel : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private GameObject _visualRoot;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Image _classIcon;
        [SerializeField] private RangeGridVisualizer _rangeGrid;
        
        [Header("Stats")]
        [SerializeField] private Slider _xpSlider;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _atkText;
        [SerializeField] private TextMeshProUGUI _defText;
        [SerializeField] private TextMeshProUGUI _rangeText;
        [SerializeField] private TextMeshProUGUI _blockText;
        [SerializeField] private TextMeshProUGUI _costText;

        [Header("Passive Skill")]
        [SerializeField] private Image _passiveIcon;
        [SerializeField] private TextMeshProUGUI _passiveName;
        [SerializeField] private TextMeshProUGUI _passiveDesc;

        [Header("Active Skill")]
        [SerializeField] private Image _activeIcon;
        [SerializeField] private TextMeshProUGUI _activeName;
        [SerializeField] private TextMeshProUGUI _activeDesc;

        [Header("Ultimate Skill")]
        [SerializeField] private Image _ultimateIcon;
        [SerializeField] private TextMeshProUGUI _ultimateName;
        [SerializeField] private TextMeshProUGUI _ultimateDesc;

        [Header("Buttons")]
        [SerializeField] private Button _btnLevelUp;
        [SerializeField] private Button _btnPromote;
        [SerializeField] private Button _btnClose;

        public GameObject VisualRoot => _visualRoot;
        public Button CloseButton => _btnClose;
        public Button LevelUpButton => _btnLevelUp;

        public void Open(UnitData unit)
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            Setup(unit);
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        public void ResetState()
        {
            Close();
        }

        private void Setup(UnitData unit)
        {
            if (unit == null) return;

            if (_nameText) _nameText.text = unit.UnitName;
            if (_portraitImage) _portraitImage.sprite = unit.UnitIcon;
            if (_levelText) _levelText.text = $"LV. {unit.Level}";
            
            // Stats
            if (_hpText) _hpText.text = unit.MaxHp.ToString("F0");
            if (_atkText) _atkText.text = unit.AttackPower.ToString("F0");
            if (_defText) _defText.text = unit.Defense.ToString("F0");
            if (_rangeText) _rangeText.text = unit.Range.ToString("0.0") + " Tiles";
            if (_blockText) _blockText.text = unit.BlockCount.ToString();
            if (_costText) _costText.text = unit.DeploymentCost.ToString();
            
            if (_rangeGrid != null) _rangeGrid.Visualize(unit.AttackPattern, unit.Range);

            // Populate Skills
            SetSkillUI(_passiveIcon, _passiveName, _passiveDesc, "Passive", "Effect details...", null);
            
            if (unit.Skill != null)
            {
                SetSkillUI(_activeIcon, _activeName, _activeDesc, unit.Skill.SkillName, unit.Skill.Description, unit.Skill.Icon);
            }
            else
            {
                SetSkillUI(_activeIcon, _activeName, _activeDesc, "None", "No Active Skill", null);
            }
            
            SetSkillUI(_ultimateIcon, _ultimateName, _ultimateDesc, "Ultimate", "Ultimate desc...", null);
        }

        private void SetSkillUI(Image icon, TextMeshProUGUI nameTxt, TextMeshProUGUI descTxt, string name, string desc, Sprite sprite)
        {
            if (icon) {
                 icon.sprite = sprite;
                 icon.color = sprite != null ? Color.white : new Color(0.2f,0.2f,0.2f,1f);
            }
            if (nameTxt) nameTxt.text = name;
            if (descTxt) descTxt.text = desc;
        }
    }
}
