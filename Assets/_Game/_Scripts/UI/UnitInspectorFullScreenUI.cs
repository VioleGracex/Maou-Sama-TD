using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using MaouSamaTD.Units;
using MaouSamaTD.Progression;
using Assets.SimpleLocalization.Scripts;
using Zenject;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Full-screen unit inspector for the Vassals page (when opened from Home).
    /// </summary>
    public class UnitInspectorFullScreenUI : MonoBehaviour, IUIController
    {
        [Header("UI Controller Architecture")]
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;

        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.3f;

        [Header("Header Info")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _rarityText;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Transform _starsRoot;
        [SerializeField] private TextMeshProUGUI _expText;

        [Header("Combat Stats")]
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _atkText;
        [SerializeField] private TextMeshProUGUI _defText;
        [SerializeField] private TextMeshProUGUI _resText;
        [SerializeField] private TextMeshProUGUI _blockText;
        [SerializeField] private TextMeshProUGUI _aspdText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _amityText;
        [SerializeField] private TextMeshProUGUI _levelMaxText;
        [SerializeField] private Image _levelFillImage;

        [Header("Unit Detail Icons")]
        [SerializeField] private Image _classIcon;
        [SerializeField] private RangePatternUI _rangeGridIcon;
        [SerializeField] private TextMeshProUGUI _rarityTextLabel; // e.g. "UR"
        [SerializeField] private TextMeshProUGUI _tagRangeText;   // Melee/Ranged
        [SerializeField] private TextMeshProUGUI _tagRoleText;    // Tank/DPS

        [Header("Skill Slots")]
        [SerializeField] private Image[] _skillSlots;

        [Header("Tab Content Roots")]
        [SerializeField] private GameObject _contentStats;
        [SerializeField] private GameObject _contentSkills;
        [SerializeField] private GameObject _contentResonance;
        [SerializeField] private GameObject _contentSkins;
        [SerializeField] private GameObject _contentXP;

        [Header("Skins Page References")]
        [SerializeField] private ScrollRect _skinScrollRect;       // Vertical thumbnails
        [SerializeField] private ScrollRect _skinPurchaseScroll;   // Horizontal buy list
        [SerializeField] private Image _skinSplashPreview;        // Full-screen art
        [SerializeField] private Animator _skinChibiPreview;       // Idle animator
        [SerializeField] private TextMeshProUGUI _skinNameText;
        [SerializeField] private TextMeshProUGUI _skinBrandText;
        [SerializeField] private Button _btnApplySkin;
        [SerializeField] private GameObject _skinItemPrefab;

        [Header("Navigation Buttons")]
        [SerializeField] private Button _btnHome;
        [SerializeField] private Button _btnClose;
        [SerializeField] private Button _btnEXP; // Level/XP Holder button
        [SerializeField] private Button _btnUpgradeSkill;
        [SerializeField] private Button _btnSkins;
        [SerializeField] private Button _btnPromote; // Used for Star Advancement
        [SerializeField] private Button _btnLevelUp;
        [SerializeField] private Button _btnChamber;

        private UnitData _currentUnit;

        private void Start()
        {
            if (_btnClose) _btnClose.onClick.AddListener(Close);
            
            // Interaction points to open specific sub-panels
            if (_btnEXP) _btnEXP.onClick.AddListener(() => SwitchTab(4)); // XP Panel
            if (_btnLevelUp) _btnLevelUp.onClick.AddListener(() => SwitchTab(4));
            if (_btnSkins) _btnSkins.onClick.AddListener(() => SwitchTab(3)); // Skins Panel
            if (_btnPromote) _btnPromote.onClick.AddListener(() => SwitchTab(2)); // Resonance/Promote Panel
            if (_btnUpgradeSkill) _btnUpgradeSkill.onClick.AddListener(() => SwitchTab(1)); // Skills Panel
            if (_btnChamber) _btnChamber.onClick.AddListener(OnChamberClicked);
            
            // Initial Localization
            LocalizeUI();
        }

        private void OnChamberClicked()
        {
            if (_currentUnit == null) return;
            Debug.Log($"[UnitInspector] Chamber clicked for unit: {_currentUnit.UnitName} (GID: {_currentUnit.GetInstanceID()})");
            // Placeholder for opening Chamber window and selecting this unit
        }

        private void LocalizeUI()
        {
            if (_btnUpgradeSkill) 
            {
                var txt = _btnUpgradeSkill.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = LocalizationManager.Localize("Vassals.Inspector.UpgradeSkills");
            }
            if (_btnSkins)
            {
                var txt = _btnSkins.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = LocalizationManager.Localize("Vassals.Inspector.Skins");
            }
            if (_btnPromote)
            {
                var txt = _btnPromote.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = LocalizationManager.Localize("Vassals.Inspector.Promote");
            }
        }

        public void Open(UnitData unit)
        {
            _currentUnit = unit;
            PopulateHeader(unit);
            SwitchTab(0); // Default to Stats
            Open();
        }

        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0;
                _canvasGroup.DOFade(1, _fadeDuration).SetUpdate(true);
            }
        }

        public void Close()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.DOFade(0, _fadeDuration).SetUpdate(true).OnComplete(() => 
                {
                    if (_visualRoot != null) _visualRoot.SetActive(false);
                });
            }
            else
            {
                if (_visualRoot != null) _visualRoot.SetActive(false);
            }
        }

        public void ResetState()
        {
            // Reset to defaults
        }

        public bool RequestClose() => true;

        private void SwitchTab(int index)
        {
            if (_contentStats) _contentStats.SetActive(index == 0);
            if (_contentSkills) _contentSkills.SetActive(index == 1);
            if (_contentResonance) _contentResonance.SetActive(index == 2);
            if (_contentSkins) _contentSkins.SetActive(index == 3);
            if (_contentXP) _contentXP.SetActive(index == 4);

            // Per User: Hide Citadel (Home) button in Skin page
            if (_btnHome) _btnHome.gameObject.SetActive(index != 3);

            if (index == 3) RefreshSkinsPage();
        }

        #region Skins Page Methods
        private UnitSkinData _selectedSkin;

        private void RefreshSkinsPage()
        {
            if (_currentUnit == null) return;
            
            // Clear items
            if (_skinScrollRect != null && _skinScrollRect.content != null)
            {
                foreach (Transform child in _skinScrollRect.content) Destroy(child.gameObject);
                
                // Add Default Skin first
                CreateSkinItem(null); 

                foreach (var skin in _currentUnit.AlternateSkins)
                {
                    if (skin != null) CreateSkinItem(skin);
                }
            }

            // Default selection
            SelectSkin(_currentUnit.EquippedSkin);
        }

        private void CreateSkinItem(UnitSkinData skin)
        {
            if (_skinItemPrefab == null) return;
            var go = Instantiate(_skinItemPrefab, _skinScrollRect.content);
            var item = go.GetComponent<UnitSkinItemUI>(); // Assuming we'll create this helper
            if (item != null)
            {
                item.Setup(skin, () => SelectSkin(skin));
            }
            else
            {
                // Basic setup if no helper script yet
                var img = go.GetComponentInChildren<Image>();
                if (img) img.sprite = (skin != null) ? skin.Icon : _currentUnit.UnitAvatar;
                var btn = go.GetComponent<Button>();
                if (btn) btn.onClick.AddListener(() => SelectSkin(skin));
            }
        }

        private void SelectSkin(UnitSkinData skin)
        {
            _selectedSkin = skin;
            bool isDeault = (skin == null);

            if (_skinSplashPreview) _skinSplashPreview.sprite = isDeault ? _currentUnit.UnitSplashArt : skin.SplashArt;
            if (_skinNameText) _skinNameText.text = isDeault ? "DEFAULT" : skin.SkinName;
            if (_skinBrandText) _skinBrandText.text = isDeault ? _currentUnit.UnitTitle : skin.BrandName;
            
            // Chibi Idle Animation
            if (_skinChibiPreview != null)
            {
                // Toggle active if missing, but ideally we just swap animator or sprite
                var sr = _skinChibiPreview.GetComponent<Image>();
                if (sr) sr.sprite = isDeault ? _currentUnit.UnitChibi : skin.Chibi;
            }

            if (_btnApplySkin)
            {
                _btnApplySkin.onClick.RemoveAllListeners();
                _btnApplySkin.onClick.AddListener(OnApplySkinClicked);
                
                // Show "Apply" if not equipped, "Equipped" otherwise
                var txt = _btnApplySkin.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = (_currentUnit.EquippedSkin == skin) ? "EQUIPPED" : "APPLY";
                _btnApplySkin.interactable = (_currentUnit.EquippedSkin != skin);
            }
        }

        private void OnApplySkinClicked()
        {
            if (_currentUnit == null) return;
            _currentUnit.EquippedSkin = _selectedSkin;
            Debug.Log($"[Skins] Applied skin: {(_selectedSkin != null ? _selectedSkin.SkinName : "Default")}");
            
            // Refresh visuals on other tabs if needed
            PopulateHeader(_currentUnit); 
            RefreshSkinsPage(); // Refresh button states
        }
        #endregion

        private void PopulateHeader(UnitData u)
        {
            if (_nameText) _nameText.text = u.UnitName.ToUpper();
            if (_rarityText) _rarityText.text = u.Rarity.ToString().ToUpper();
            if (_portraitImage) _portraitImage.sprite = u.GetCurrentVisualArt();
            
            RefreshProgressionUI();
            RefreshUnitDetails();
        }

        private void RefreshProgressionUI()
        {
            if (_currentUnit == null) return;

            if (_levelText) _levelText.text = $"{_currentUnit.Level}";
            if (_levelMaxText) _levelMaxText.text = $"/ {_currentUnit.MaxLevel}";
            
            int req = ProgressionLogic.GetRequiredXP(_currentUnit.Level);
            float progress = (float)_currentUnit.Experience / req;
            
            if (_levelFillImage) _levelFillImage.fillAmount = progress;
            if (_expText) _expText.text = $"{_currentUnit.Experience} / {req}";

            if (_starsRoot != null)
            {
                for (int i = 0; i < _starsRoot.childCount; i++)
                {
                    Transform star = _starsRoot.GetChild(i);
                    Image img = star.GetComponent<Image>();
                    if (img == null) img = star.GetComponentInChildren<Image>();
                    if (img) img.color = i < _currentUnit.StarRating ? new Color(1f, 0.8f, 0f) : new Color(0.2f, 0.2f, 0.2f);
                }
            }

            // Update Combat Stats
            if (_hpText) _hpText.text = _currentUnit.MaxHp.ToString("F0");
            if (_atkText) _atkText.text = _currentUnit.AttackPower.ToString("F0");
            if (_defText) _defText.text = _currentUnit.Defense.ToString("F0");
            if (_resText) _resText.text = _currentUnit.Resistance.ToString("F0");
            if (_blockText) _blockText.text = _currentUnit.BlockCount.ToString();
            if (_aspdText) _aspdText.text = GetASPDLabel(_currentUnit.AttackInterval);
            if (_costText) _costText.text = _currentUnit.DeploymentCost.ToString();
            if (_amityText) _amityText.text = $"{(_currentUnit.Amity * 100):F0}%";

            // Button Interactability
            if (_btnPromote) _btnPromote.interactable = (_currentUnit.Level == _currentUnit.MaxLevel && _currentUnit.StarRating < 6);
            if (_btnLevelUp) _btnLevelUp.interactable = (_currentUnit.Level < _currentUnit.MaxLevel);
        }

        private string GetASPDLabel(float interval)
        {
            if (interval < 0.8f) return "Very Fast";
            if (interval < 1.1f) return "Fast";
            if (interval < 1.5f) return "Normal";
            if (interval < 2.0f) return "Slow";
            return "Very Slow";
        }

        private void RefreshUnitDetails()
        {
            if (_currentUnit == null) return;

            // Rarity Label (UR, SSR, etc.)
            if (_rarityTextLabel) 
            {
                string r = _currentUnit.Rarity switch
                {
                    UnitRarity.Common => "C",
                    UnitRarity.Uncommon => "UC",
                    UnitRarity.Rare => "R",
                    UnitRarity.Elite => "SR",
                    UnitRarity.Master => "SSR",
                    UnitRarity.Legendary => "UR",
                    _ => "SSR"
                };
                _rarityTextLabel.text = r;
            }

            // Damage Type Label (Melee / Ranged)
            if (_tagRangeText) 
            {
                _tagRangeText.text = _currentUnit.DamageType == DamageType.Melee ? "MELEE" : "RANGED";
            }
            
            if (_rangeGridIcon) _rangeGridIcon.SetPattern(_currentUnit.AttackPattern, (int)_currentUnit.Range);
            
            // Tactical Role Mapping (Tank, DPS, Support)
            if (_tagRoleText)
            {
                string role = _currentUnit.Class switch
                {
                    UnitClass.Bastion => "TANK",
                    UnitClass.Vanguard => "TANK",
                    UnitClass.Sage or UnitClass.Support or UnitClass.Architect or UnitClass.Necromancer => "SUPPORT",
                    _ => "DPS"
                };
                _tagRoleText.text = role;
            }

            // Skills (Passive, Active, Ultimate)
            RefreshSkillSlot(0, _currentUnit.PassiveSkill);
            RefreshSkillSlot(1, _currentUnit.ActiveSkill);
            RefreshSkillSlot(2, _currentUnit.UltimateSkill);
        }

        private void RefreshSkillSlot(int index, MaouSamaTD.Skills.UnitSkillData data)
        {
            if (_skillSlots == null || index < 0 || index >= _skillSlots.Length) return;
            
            bool hasSkill = data != null;
            _skillSlots[index].gameObject.SetActive(true); // Keep slot on, just toggle icon
            if (hasSkill)
            {
                _skillSlots[index].sprite = data.Icon;
                _skillSlots[index].color = Color.white;
            }
            else
            {
                _skillSlots[index].sprite = null;
                _skillSlots[index].color = new Color(0,0,0,0);
            }
        }
    }
}
