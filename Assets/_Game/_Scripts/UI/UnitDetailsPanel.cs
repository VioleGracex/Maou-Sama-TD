using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Detailed inspector for a selected unit. 4 tabs:
    ///   0 Stats | 1 Skills | 2 Resonance (duplicate-unlock nodes) | 3 Skins
    /// </summary>
    public class UnitDetailsPanel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Panel Animation")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private float _hiddenX      = -500f;
        [SerializeField] private float _shownX       = 0f;
        [SerializeField] private float _animDuration = 0.3f;
        [SerializeField] private Ease  _animEase     = Ease.OutQuad;

        [Header("Header (shared)")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _rarityText;
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
        [SerializeField] private TextMeshProUGUI _resText;
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
        [SerializeField] private Transform  _resonanceNodeContainer;
        [SerializeField] private GameObject _resonanceNodePrefab;

        [Header("Skins Tab")]
        [SerializeField] private Transform  _skinsContainer;
        [SerializeField] private GameObject _skinCardPrefab;

        [Header("Tab Accent Colors")]
        [SerializeField] private Color _tabActiveColor   = new Color(0f, 0.85f, 0.85f, 1f);
        [SerializeField] private Color _tabInactiveColor = new Color(0.18f, 0.18f, 0.18f, 1f);

        #endregion

        #region State
        public bool IsOpen { get; private set; }
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
            if (_tabStats)     _tabStats.onClick.AddListener(()     => SwitchTab(0));
            if (_tabSkills)    _tabSkills.onClick.AddListener(()    => SwitchTab(1));
            if (_tabResonance) _tabResonance.onClick.AddListener(() => SwitchTab(2));
            if (_tabSkins)     _tabSkins.onClick.AddListener(()     => SwitchTab(3));
        }
        #endregion

        #region Public API
        public void Show(UnitData unitData)
        {
            if (unitData == null) return;
            _currentUnit = unitData;
            if (_visualRoot != null) _visualRoot.SetActive(true);
            PopulateHeader(unitData);
            SwitchTab(0);
            if (!IsOpen)
            {
                _panelRect.DOKill();
                _panelRect.DOAnchorPosX(_shownX, _animDuration).SetEase(_animEase).SetUpdate(true);
                IsOpen = true;
            }
        }

        public void Hide()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _panelRect.DOKill();
            _panelRect.DOAnchorPosX(_hiddenX, _animDuration).SetEase(_animEase).SetUpdate(true)
                .OnComplete(() => { if (_visualRoot != null) _visualRoot.SetActive(false); });
        }
        #endregion

        #region Tab Logic
        private void SwitchTab(int index)
        {
            _activeTab = index;
            if (_contentStats)     _contentStats.SetActive(index == 0);
            if (_contentSkills)    _contentSkills.SetActive(index == 1);
            if (_contentResonance) _contentResonance.SetActive(index == 2);
            if (_contentSkins)     _contentSkins.SetActive(index == 3);

            UpdateTabVisuals();

            if (_currentUnit == null) return;
            switch (index)
            {
                case 0: PopulateStats(_currentUnit);     break;
                case 1: PopulateSkills(_currentUnit);    break;
                case 2: PopulateResonance(_currentUnit); break;
                case 3: PopulateSkins(_currentUnit);     break;
            }
        }

        private void UpdateTabVisuals()
        {
            ApplyTabStyle(_tabStats,     0);
            ApplyTabStyle(_tabSkills,    1);
            ApplyTabStyle(_tabResonance, 2);
            ApplyTabStyle(_tabSkins,     3);
        }

        private void ApplyTabStyle(Button btn, int idx)
        {
            if (btn == null) return;
            bool active = (idx == _activeTab);
            var img = btn.GetComponent<Image>();
            if (img) img.color = active ? _tabActiveColor : _tabInactiveColor;
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.color = active ? Color.black : new Color(0.65f, 0.65f, 0.65f);
        }
        #endregion

        #region Populate — Header
        private void PopulateHeader(UnitData u)
        {
            if (_nameText)   _nameText.text   = u.UnitName?.ToUpper();
            if (_rarityText) _rarityText.text = $"{u.Rarity.ToString().ToUpper()} CLASS SOUL";
            if (_classIcon && u.UnitIcon != null)
            {
                _classIcon.sprite = u.UnitIcon;
                _classIcon.color  = Color.white;
            }
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
        #endregion

        #region Populate — Stats
        private void PopulateStats(UnitData u)
        {
            if (_hpText)    _hpText.text    = u.MaxHp.ToString("0");
            if (_atkText)   _atkText.text   = u.AttackPower.ToString("0");
            if (_defText)   _defText.text   = u.Defense.ToString("0");
            if (_resText)   _resText.text   = u.Defense.ToString("0");
            if (_rangeText) _rangeText.text = u.Range.ToString("0.0");
            if (_blockText) _blockText.text = u.BlockCount.ToString();
            if (_costText)  _costText.text  = u.DeploymentCost.ToString();
            if (_rangeGrid) _rangeGrid.Visualize(u.AttackPattern, u.Range);
        }
        #endregion

        #region Populate — Skills
        private void PopulateSkills(UnitData u)
        {
            SetSkill(_passiveIcon, _passiveName, _passiveDesc, "PASSIVE", u.PassiveSkill?.Description ?? "—", u.PassiveSkill?.Icon);

            if (u.UltimateSkill != null)
                SetSkill(_activeIcon, _activeName, _activeDesc,
                         u.UltimateSkill.SkillName?.ToUpper(), u.UltimateSkill.Description, u.UltimateSkill.Icon);
            else
                SetSkill(_activeIcon, _activeName, _activeDesc, "ACTIVE", "No active skill.", null);

            SetSkill(_ultimateIcon, _ultimateName, _ultimateDesc, "ULTIMATE", u.UltimateSkill?.Description ?? "—", u.UltimateSkill?.Icon);
        }

        private void SetSkill(Image icon, TextMeshProUGUI nameTxt, TextMeshProUGUI descTxt,
                              string name, string desc, Sprite sprite)
        {
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.color  = sprite != null ? Color.white : new Color(0.25f, 0.25f, 0.25f);
            }
            if (nameTxt != null) nameTxt.text = name;
            if (descTxt != null) descTxt.text = desc;
        }
        #endregion

        #region Populate — Resonance
        private void PopulateResonance(UnitData u)
        {
            if (_resonanceNodeContainer == null || _resonanceNodePrefab == null) return;
            foreach (Transform c in _resonanceNodeContainer) Destroy(c.gameObject);

            bool hasNodes = u.AscensionNodes != null && u.AscensionNodes.Count > 0;
            if (!hasNodes)
            {
                SpawnPlaceholderNode("No resonance nodes defined for this unit yet.");
                return;
            }

            for (int i = 0; i < u.AscensionNodes.Count; i++)
            {
                var node = u.AscensionNodes[i];
                var go   = Instantiate(_resonanceNodePrefab, _resonanceNodeContainer);

                SetTMPText(go, "Txt_TierLabel", node.TierLabel ?? $"NODE TIER {(i + 1):D2}");
                string nameStr = node.IsAwakening
                    ? $"AWAKENING: {node.NodeName?.ToUpper()}"
                    : node.NodeName?.ToUpper();
                SetTMPText(go, "Txt_NodeName", nameStr);
                SetTMPText(go, "Txt_NodeDesc",  node.NodeDescription);

                var iconT = go.transform.Find("Img_NodeIcon");
                if (iconT != null)
                {
                    var img = iconT.GetComponent<Image>();
                    if (img != null) { img.sprite = node.NodeIcon; img.color = node.NodeIcon != null ? Color.white : Color.clear; }
                }

                var awakT = go.transform.Find("Indicator_Awakening");
                if (awakT != null) awakT.gameObject.SetActive(node.IsAwakening);
            }
        }

        private void SpawnPlaceholderNode(string msg)
        {
            var go = Instantiate(_resonanceNodePrefab, _resonanceNodeContainer);
            SetTMPText(go, "Txt_TierLabel", "—");
            SetTMPText(go, "Txt_NodeName",  string.Empty);
            SetTMPText(go, "Txt_NodeDesc",  msg);
        }
        #endregion

        #region Populate — Skins
        private void PopulateSkins(UnitData u)
        {
            if (_skinsContainer == null || _skinCardPrefab == null) return;
            foreach (Transform c in _skinsContainer) Destroy(c.gameObject);

            // Default skin
            AddSkinCard(u.UnitIcon);

            if (u.AlternateSkins != null)
            {
                foreach (var skin in u.AlternateSkins)
                {
                    if (skin != null) AddSkinCard(skin.Icon);
                }
            }
        }

        private void AddSkinCard(Sprite s)
        {
            var go = Instantiate(_skinCardPrefab, _skinsContainer);
            var img = go.GetComponentInChildren<Image>(true);
            if (img != null) { img.sprite = s; img.color = s != null ? Color.white : new Color(0.2f, 0.2f, 0.2f); }
        }
        #endregion

        #region Helpers
        private static void SetTMPText(GameObject root, string childName, string value)
        {
            var t = root.transform.Find(childName);
            if (t == null) return;
            var txt = t.GetComponent<TextMeshProUGUI>();
            if (txt != null) txt.text = value ?? string.Empty;
        }
        #endregion
    }
}
