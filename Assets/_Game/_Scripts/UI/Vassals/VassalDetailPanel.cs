using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using MaouSamaTD.Units;
using Assets.SimpleLocalization.Scripts;

namespace MaouSamaTD.UI.Vassals
{
    /// <summary>
    /// Detailed inspection panel for a single vassal (unit).
    /// Slide-in panel with 4 tabs: Stats | Skills | Resonance | Skins.
    /// Replaces the old UnitInspectorPanel.
    /// </summary>
    public class VassalDetailPanel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Panel Animation")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private float _hiddenX      = 500f;   // off-screen right
        [SerializeField] private float _shownX       = 0f;
        [SerializeField] private float _animDuration = 0.28f;
        [SerializeField] private Ease  _animEase     = Ease.OutQuart;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _rarityText;
        [SerializeField] private Image           _portraitImage;
        [SerializeField] private Image           _classIcon;
        [SerializeField] private Transform       _starsRoot;

        [Header("Tab Buttons")]
        [SerializeField] private Button _tabStats;
        [SerializeField] private Button _tabSkills;
        [SerializeField] private Button _tabResonance;
        [SerializeField] private Button _tabSkins;

        [Header("Tab Content Roots")]
        [SerializeField] private GameObject _contentStats;
        [SerializeField] private GameObject _contentSkills;
        [SerializeField] private GameObject _contentResonance;
        [SerializeField] private GameObject _contentSkins;

        [Header("Stats Tab")]
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _atkText;
        [SerializeField] private TextMeshProUGUI _defText;
                [SerializeField] private TextMeshProUGUI _resistanceText;

        [SerializeField] private TextMeshProUGUI _rangeText;
        [SerializeField] private TextMeshProUGUI _blockText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private RangeGridVisualizer _rangeGrid;

        [Header("Skills Tab")]
        [SerializeField] private Image           _passiveIcon;
        [SerializeField] private TextMeshProUGUI _passiveName;
        [SerializeField] private TextMeshProUGUI _passiveDesc;
        [SerializeField] private Image           _activeIcon;
        [SerializeField] private TextMeshProUGUI _activeName;
        [SerializeField] private TextMeshProUGUI _activeDesc;
        [SerializeField] private Image           _ultimateIcon;
        [SerializeField] private TextMeshProUGUI _ultimateName;
        [SerializeField] private TextMeshProUGUI _ultimateDesc;

        [Header("Resonance Tab")]
        [SerializeField] private Transform  _resonanceContainer;
        [SerializeField] private GameObject _resonanceNodePrefab;

        [Header("Skins Tab")]
        [SerializeField] private Transform  _skinsContainer;
        [SerializeField] private GameObject _skinCardPrefab;

        [Header("Buttons")]
        [SerializeField] private Button _btnClose;
        [SerializeField] private Button _btnLevelUp;
        [SerializeField] private Button _btnSkins;
        [SerializeField] private Button _btnUpgradeSkills; // To open skins/skill page
        [SerializeField] private Button _btnPromote; // Rank up/Star promotion

        [Header("Tab Colors")]
        [SerializeField] private Color _tabActiveColor   = new Color(0f, 0.85f, 0.85f, 1f);
        [SerializeField] private Color _tabInactiveColor = new Color(0.12f, 0.12f, 0.14f, 1f);

        #endregion

        #region Properties & State
        public Button CloseButton   => _btnClose;
        public Button LevelUpButton => _btnLevelUp;
        public bool   IsOpen        { get; private set; }
        public GameObject VisualRoot => _visualRoot;

        private UnitData _currentUnit;
        private int      _activeTab = 0;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (_panelRect == null) _panelRect = GetComponent<RectTransform>();
            if (_panelRect != null)
                _panelRect.anchoredPosition = new Vector2(_hiddenX, _panelRect.anchoredPosition.y);
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        private void Start()
        {
            if (_btnClose)     _btnClose.onClick.AddListener(Close);
            if (_tabStats)     _tabStats.onClick.AddListener(()     => SwitchTab(0));
            if (_tabSkills)    _tabSkills.onClick.AddListener(()    => SwitchTab(1));
            if (_tabResonance) _tabResonance.onClick.AddListener(() => SwitchTab(2));
            if (_tabSkins)     _tabSkins.onClick.AddListener(()     => SwitchTab(3));

            if (_btnSkins)         _btnSkins.onClick.AddListener(()     => SwitchTab(3));
            if (_btnUpgradeSkills) _btnUpgradeSkills.onClick.AddListener(() => SwitchTab(3));
            if (_btnPromote)   _btnPromote.onClick.AddListener(OnPromoteClicked);
            
            // Initial Localization
            LocalizeUI();
        }

        private void LocalizeUI()
        {
            if (_tabStats) _tabStats.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Vassals.Inspector.Stats");
            if (_tabSkills) _tabSkills.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Vassals.Inspector.Skills");
            if (_tabResonance) _tabResonance.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Vassals.Inspector.Resonance");
            if (_tabSkins) _tabSkins.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Vassals.Inspector.Skins");

            if (_btnUpgradeSkills) _btnUpgradeSkills.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Vassals.Inspector.UpgradeSkills");
            if (_btnSkins) _btnSkins.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Vassals.Inspector.Skins");
            if (_btnPromote) _btnPromote.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Vassals.Inspector.Promote");
        }

        private void OnPromoteClicked()
        {
            Debug.Log("[VassalDetailPanel] Promote (Rank Up) clicked");
            // Integration with promotion system logic
        }
        #endregion

        #region Public API
        public void Open(UnitData unit)
        {
            if (unit == null) return;
            _currentUnit = unit;
            PopulateHeader(unit);
            SwitchTab(0);
            Open();
        }

        public void SetLayout(bool leftSide)
        {
            var rt = _visualRoot.GetComponent<RectTransform>();
            if (rt == null) return;

            if (leftSide)
            {
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 0.5f);
                _hiddenX = -500f; // off-screen left
                _shownX = 0f;
            }
            else
            {
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 0.5f);
                _hiddenX = 500f; // off-screen right
                _shownX = 0f;
            }
            
            rt.anchoredPosition = new Vector2(_hiddenX, rt.anchoredPosition.y);
        }

        public void Open()
        {
            if (IsOpen) return;

            if (_visualRoot != null) _visualRoot.SetActive(true);
            IsOpen = true;
            if (_panelRect != null)
            {
                _panelRect.DOKill();
                _panelRect.DOAnchorPosX(_shownX, _animDuration).SetEase(_animEase).SetUpdate(true);
            }
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (_panelRect != null)
            {
                _panelRect.DOKill();
                _panelRect.DOAnchorPosX(_hiddenX, _animDuration).SetEase(Ease.InQuart).SetUpdate(true)
                    .OnComplete(() => { if (_visualRoot != null) _visualRoot.SetActive(false); });
            }
            else if (_visualRoot != null)
            {
                _visualRoot.SetActive(false);
            }
        }

        public void ResetState() => Close();
        #endregion

        #region Tabs
        private void SwitchTab(int index)
        {
            _activeTab = index;
            if (_contentStats)     _contentStats.SetActive(index == 0);
            if (_contentSkills)    _contentSkills.SetActive(index == 1);
            if (_contentResonance) _contentResonance.SetActive(index == 2);
            if (_contentSkins)     _contentSkins.SetActive(index == 3);

            RefreshTabStyles();

            if (_currentUnit == null) return;
            switch (index)
            {
                case 0: PopulateStats(_currentUnit);     break;
                case 1: PopulateSkills(_currentUnit);    break;
                case 2: PopulateResonance(_currentUnit); break;
                case 3: PopulateSkins(_currentUnit);     break;
            }
        }

        private void RefreshTabStyles()
        {
            SetTabStyle(_tabStats,     0);
            SetTabStyle(_tabSkills,    1);
            SetTabStyle(_tabResonance, 2);
            SetTabStyle(_tabSkins,     3);
        }

        private void SetTabStyle(Button btn, int idx)
        {
            if (btn == null) return;
            bool on = idx == _activeTab;
            var img = btn.GetComponent<Image>();
            if (img) img.color = on ? _tabActiveColor : _tabInactiveColor;
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.color = on ? Color.black : new Color(0.6f, 0.6f, 0.6f);
        }
        #endregion

        #region Populate
        private void PopulateHeader(UnitData u)
        {
            if (_nameText)   _nameText.text   = u.UnitName?.ToUpper();
            if (_levelText)  _levelText.text  = $"LV. {u.Level}";
            if (_rarityText) _rarityText.text = u.Rarity.ToString().ToUpper();
            if (_portraitImage && u.UnitSprite) { _portraitImage.sprite = u.UnitSprite; _portraitImage.color = Color.white; }
            if (_classIcon && u.UnitIcon)        { _classIcon.sprite = u.UnitIcon;       _classIcon.color = Color.white; }
            if (_starsRoot != null)
            {
                int stars = (int)u.Rarity + 1;
                for (int i = 0; i < _starsRoot.childCount; i++)
                {
                    var img = _starsRoot.GetChild(i).GetComponent<Image>();
                    if (img) img.color = i < stars ? new Color(1f, 0.78f, 0f) : new Color(0.3f, 0.3f, 0.3f);
                }
            }
        }

        private void PopulateStats(UnitData u)
        {
            if (_hpText)    _hpText.text    = u.MaxHp.ToString("0");
            if (_atkText)   _atkText.text   = u.AttackPower.ToString("0");
            if (_defText)   _defText.text   = u.Defense.ToString("0");
            if (_resistanceText) _resistanceText.text = (u.Resistance * 100f).ToString("0") + "%";
            if (_rangeText) _rangeText.text = u.Range.ToString("0.0");
            if (_blockText) _blockText.text = u.BlockCount.ToString();
            if (_costText)  _costText.text  = u.DeploymentCost.ToString();
            if (_rangeGrid) _rangeGrid.Visualize(u.AttackPattern, u.Range);
        }

        private void PopulateSkills(UnitData u)
        {
            ApplySkill(_passiveIcon,  _passiveName,  _passiveDesc,  "PASSIVE",  "—", null);
            if (u.Skill != null)
                ApplySkill(_activeIcon, _activeName, _activeDesc, u.Skill.SkillName?.ToUpper(), u.Skill.Description, u.Skill.Icon);
            else
                ApplySkill(_activeIcon, _activeName, _activeDesc, "ACTIVE", "No active skill.", null);
            ApplySkill(_ultimateIcon, _ultimateName, _ultimateDesc, "ULTIMATE", "—", null);
        }

        private void ApplySkill(Image icon, TextMeshProUGUI nameTxt, TextMeshProUGUI descTxt, string name, string desc, Sprite sprite)
        {
            if (icon != null) { icon.sprite = sprite; icon.color = sprite ? Color.white : new Color(0.2f, 0.2f, 0.2f); }
            if (nameTxt) nameTxt.text = name;
            if (descTxt) descTxt.text = desc;
        }

        private void PopulateResonance(UnitData u)
        {
            if (_resonanceContainer == null || _resonanceNodePrefab == null) return;
            foreach (Transform c in _resonanceContainer) Destroy(c.gameObject);

            bool hasNodes = u.AscensionNodes != null && u.AscensionNodes.Count > 0;
            if (!hasNodes)
            {
                var ph = Instantiate(_resonanceNodePrefab, _resonanceContainer);
                SetChild(ph, "Txt_TierLabel", "—");
                SetChild(ph, "Txt_NodeDesc",  "No resonance nodes defined yet.");
                return;
            }

            for (int i = 0; i < u.AscensionNodes.Count; i++)
            {
                var node = u.AscensionNodes[i];
                var go   = Instantiate(_resonanceNodePrefab, _resonanceContainer);
                SetChild(go, "Txt_TierLabel", node.TierLabel ?? $"NODE TIER {(i+1):D2}");
                SetChild(go, "Txt_NodeName",  node.IsAwakening ? $"AWAKENING: {node.NodeName?.ToUpper()}" : node.NodeName?.ToUpper());
                SetChild(go, "Txt_NodeDesc",  node.NodeDescription);
                var awakT = go.transform.Find("Indicator_Awakening");
                if (awakT) awakT.gameObject.SetActive(node.IsAwakening);
                var iconT = go.transform.Find("Img_NodeIcon");
                if (iconT) { var img = iconT.GetComponent<Image>(); if (img) { img.sprite = node.NodeIcon; img.color = node.NodeIcon ? Color.white : Color.clear; } }
            }
        }

        private void PopulateSkins(UnitData u)
        {
            if (_skinsContainer == null || _skinCardPrefab == null) return;
            foreach (Transform c in _skinsContainer) Destroy(c.gameObject);

            var skins = new System.Collections.Generic.List<Sprite> { u.UnitSprite };
            if (u.AlternateSkins != null) skins.AddRange(u.AlternateSkins);
            foreach (var skin in skins)
            {
                var go  = Instantiate(_skinCardPrefab, _skinsContainer);
                var img = go.GetComponentInChildren<Image>(true);
                if (img) { img.sprite = skin; img.color = skin ? Color.white : new Color(0.2f, 0.2f, 0.2f); }
            }
        }
        #endregion

        #region Helpers
        private static void SetChild(GameObject root, string childName, string value)
        {
            var t = root.transform.Find(childName);
            if (t == null) return;
            var txt = t.GetComponent<TextMeshProUGUI>();
            if (txt) txt.text = value ?? string.Empty;
        }
        #endregion
    }
}
