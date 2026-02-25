using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI
{
    public class UnitDetailsPanel : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private GameObject _visualRoot; 
        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private float _hiddenX = -500f;
        [SerializeField] private float _shownX = 0f;
        [SerializeField] private float _animDuration = 0.3f;
        [SerializeField] private Ease _animEase = Ease.OutQuad;

        [Header("UI Details")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Image _classIcon;
        
        [Header("Stats")]
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
        
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (_panelRect == null) _panelRect = GetComponent<RectTransform>();
            
            // Start hidden
            _panelRect.anchoredPosition = new Vector2(_hiddenX, _panelRect.anchoredPosition.y);
            if (_visualRoot != null) _visualRoot.SetActive(false);
            IsOpen = false;
        }

        public void Show(UnitData unitData)
        {
            if (unitData == null) return;
            
            if (_visualRoot != null) _visualRoot.SetActive(true);

            // Populate UI - Identity
            if (_nameText) _nameText.text = unitData.UnitName;
            if (_levelText) _levelText.text = $"LV {unitData.Level}";
            
            // Populate Stats
            if (_hpText) _hpText.text = unitData.MaxHp.ToString("0");
            if (_atkText) _atkText.text = unitData.AttackPower.ToString("0");
            if (_defText) _defText.text = unitData.Defense.ToString("0");
            if (_rangeText) _rangeText.text = unitData.Range.ToString("0.0") + " Tiles";
            if (_blockText) _blockText.text = unitData.BlockCount.ToString();
            if (_costText) _costText.text = unitData.DeploymentCost.ToString();
            
            // Populate Skills (Assuming UnitData has these fields later, mocking for now)
            SetSkillUI(_passiveIcon, _passiveName, _passiveDesc, "Passive", "Effect details...");
            
            if (unitData.Skill != null)
            {
                // Assuming this is Active for now
                SetSkillUI(_activeIcon, _activeName, _activeDesc, unitData.Skill.SkillName, unitData.Skill.Description, unitData.Skill.Icon);
            }
            else
            {
                SetSkillUI(_activeIcon, _activeName, _activeDesc, "None", "No Active Skill");
            }
            
            SetSkillUI(_ultimateIcon, _ultimateName, _ultimateDesc, "Ultimate", "Ultimate desc...", null);

            // Animate In
            if (!IsOpen)
            {
                _panelRect.DOKill();
                _panelRect.DOAnchorPosX(_shownX, _animDuration).SetEase(_animEase).SetUpdate(true);
                IsOpen = true;
            }
        }

        private void SetSkillUI(Image icon, TextMeshProUGUI nameTxt, TextMeshProUGUI descTxt, string name, string desc, Sprite sprite = null)
        {
            if (icon) {
                 icon.sprite = sprite;
                 icon.color = sprite != null ? Color.white : new Color(0.2f,0.2f,0.2f,1f); // Grey out if missing
            }
            if (nameTxt) nameTxt.text = name;
            if (descTxt) descTxt.text = desc;
        }

        public void Hide()
        {
            if (!IsOpen) return;
            IsOpen = false;

            _panelRect.DOKill();
            _panelRect.DOAnchorPosX(_hiddenX, _animDuration).SetEase(_animEase).SetUpdate(true)
                .OnComplete(() => { if (_visualRoot != null) _visualRoot.SetActive(false); });
        }
    }
}
