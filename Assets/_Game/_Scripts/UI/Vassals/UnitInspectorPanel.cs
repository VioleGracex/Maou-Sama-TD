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
        [SerializeField] private Slider _xpSlider;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _atkText;
        [SerializeField] private TextMeshProUGUI _defText;

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
            
            // Stats (Mock implementation for now)
            if (_hpText) _hpText.text = unit.MaxHp.ToString("F0");
            if (_atkText) _atkText.text = unit.AttackPower.ToString("F0");
            if (_defText) _defText.text = unit.Defense.ToString("F0");
        }
    }
}
