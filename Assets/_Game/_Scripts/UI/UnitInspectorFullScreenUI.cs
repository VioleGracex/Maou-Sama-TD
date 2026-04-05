using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using MaouSamaTD.Units;
using MaouSamaTD.Data;
using System.Collections.Generic;
using System.Linq;
using MaouSamaTD.Progression;
using Assets.SimpleLocalization.Scripts;
using Zenject;
using MaouSamaTD.Managers;
using MaouSamaTD.UI.Vassals;

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
        public bool AddsToHistory => false;
        [SerializeField] private NavigationFeatures _navFeatures = NavigationFeatures.BackButton | NavigationFeatures.CitadelButton;
        public NavigationFeatures ConfiguredNavFeatures => _navFeatures;

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

        [Header("Level Up Page References")]
        [SerializeField] private ScrollRect _duplicatesScrollRect;
        [SerializeField] private GameObject _duplicateItemPrefab;
        [SerializeField] private TextMeshProUGUI _xpMeterValueText;
        [SerializeField] private Button _btnConfirmLevelUp;

        [Header("Tab Content Roots")]
        [SerializeField] private GameObject _contentStats;
        [SerializeField] private GameObject _contentSkills;
        [SerializeField] private GameObject _contentResonance;
        [SerializeField] private GameObject _contentSkins;
        [SerializeField] private GameObject _contentXP;

        [Header("Skins Page References")]
        [SerializeField] private SkinInfiniteScroll _skinInfiniteScroll;
        [SerializeField] private ScrollRect _skinPurchaseScroll;   // Horizontal buy list
        [SerializeField] private Image _skinSplashPreview;        // Full-screen art
        [SerializeField] private Animator _skinChibiPreview;       // Idle animator
        [SerializeField] private TextMeshProUGUI _skinNameText;
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

        [Header("Debug")]
        [SerializeField] private bool _debug = true;

        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;

        private UnitData _currentUnit;
        private List<UnitInventoryEntry> _selectedDuplicates = new List<UnitInventoryEntry>();
        private List<UnitData.SkinData> _skinDataList = new List<UnitData.SkinData>();
        private UnitData _lastSkinUnit; // For lazy loading check

        private void Start()
        {
            if (_btnClose) _btnClose.onClick.AddListener(() => UIFlowManager.Instance.GoBack());
            if (_btnHome) _btnHome.onClick.AddListener(() => UIFlowManager.Instance.ClearHistory(true, true));
            
            // Interaction points to open specific sub-panels
            if (_btnEXP) _btnEXP.onClick.AddListener(() => SwitchTab(4)); // XP Panel
            if (_btnLevelUp) _btnLevelUp.onClick.AddListener(() => SwitchTab(4));
            if (_btnSkins) _btnSkins.onClick.AddListener(() => SwitchTab(3)); // Skins Panel
            if (_btnPromote) _btnPromote.onClick.AddListener(() => SwitchTab(2)); // Resonance/Promote Panel
            if (_btnUpgradeSkill) _btnUpgradeSkill.onClick.AddListener(() => SwitchTab(1)); // Skills Panel
            if (_btnChamber) _btnChamber.onClick.AddListener(OnChamberClicked);
            if (_btnConfirmLevelUp) _btnConfirmLevelUp.onClick.AddListener(PerformLevelUp);
            
            if (_skinInfiniteScroll) _skinInfiniteScroll.OnSelectionChanged += OnSkinScrollSelectionChanged;

            // Initial Localization
            LocalizeUI();
        }

        private void OnSkinScrollSelectionChanged(int index)
        {
            if (_skinDataList != null && index >= 0 && index < _skinDataList.Count)
            {
                SelectSkin(_skinDataList[index]);
            }
            
            // Per User: "show price only on active skin card ... hide for others on side"
            UpdateSkinItemsStatus(index);
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
            bool unitChanged = _currentUnit != unit;
            _currentUnit = unit;
            
            PopulateHeader(unit);
            
            // Lazy load skins immediately if unit changed, so page is ready
            if (unitChanged) RefreshSkinsPage();
            
            SwitchTab(0); // Default to Stats
            if (_debug) Debug.Log($"[UnitInspector] Opening for: {unit.UnitName}");
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

            if (_debug) Debug.Log("[UnitInspector] Visuals hidden via Close().");
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

            // if (index == 3) RefreshSkinsPage(); // Now handled in Open() for "lazy" immediate access
            if (index == 4) RefreshXPPage();
        }

        private void RefreshXPPage()
        {
            if (_currentUnit == null || _saveManager == null || _saveManager.CurrentData == null) return;
            
            _selectedDuplicates.Clear();
            if (_btnConfirmLevelUp) _btnConfirmLevelUp.interactable = false;

            // Clear duplicates list
            if (_duplicatesScrollRect != null && _duplicatesScrollRect.content != null)
            {
                foreach (Transform child in _duplicatesScrollRect.content) Destroy(child.gameObject);
                
                // Fetch duplicates from PlayerData (Inventory)
                var inventory = _saveManager.CurrentData.UnitInventory;
                // We use unit.name to match UnitID in inventory
                // Note: GetInstanceID() is internal, we might need a better way to find the "current instance" 
                // but for now we filter all instances of same UnitID.
                var duplicates = inventory.FindAll(entry => entry.UnitID == _currentUnit.name);

                foreach (var entry in duplicates)
                {
                    if (_duplicateItemPrefab == null) continue;
                    GameObject go = Instantiate(_duplicateItemPrefab, _duplicatesScrollRect.content);
                    var item = go.GetComponent<VassalDuplicateItemUI>();
                    if (item != null)
                    {
                        item.Setup(entry, _currentUnit.GetSprite(UnitData.UnitImageType.Avatar), (e) => OnDuplicateSelected(e, item));
                    }
                }
            }

            // Update XP Meter text if available
            int req = ProgressionLogic.GetRequiredXP(_currentUnit.Level);
            if (_xpMeterValueText) _xpMeterValueText.text = $"{_currentUnit.Experience} / {req}";
        }

        private void OnDuplicateSelected(UnitInventoryEntry entry, VassalDuplicateItemUI item)
        {
            if (_selectedDuplicates.Contains(entry))
            {
                _selectedDuplicates.Remove(entry);
                item.SetSelected(false);
            }
            else
            {
                _selectedDuplicates.Add(entry);
                item.SetSelected(true);
            }

            if (_btnConfirmLevelUp) _btnConfirmLevelUp.interactable = _selectedDuplicates.Count > 0;
        }

        private void PerformLevelUp()
        {
            if (_currentUnit == null || _selectedDuplicates.Count == 0) return;

            // Per User: "level up change skins upgrade units"
            // Consumption logic: each duplicate gives fixed XP or based on duplicate level
            int totalXPGain = _selectedDuplicates.Count * 500; // Placeholder value
            
            ProgressionLogic.AddXP(_currentUnit, totalXPGain);
            
            // Remove consumed duplicates from inventory
            foreach (var entry in _selectedDuplicates)
            {
                _saveManager.CurrentData.UnitInventory.Remove(entry);
                // Also increment potential if desired, but here we focus on XP
            }

            _saveManager.Save();
            
            Debug.Log($"[Vassals] Level Up performed! Gained {totalXPGain} XP. Consumed {_selectedDuplicates.Count} duplicates.");
            
            RefreshProgressionUI();
            RefreshXPPage();
        }

        #region Skins Page Methods
        private UnitData.SkinData _selectedSkin;

        private void RefreshSkinsPage()
        {
            if (_currentUnit == null) return;
            
            // Optimization: Only rebuild if it's a different unit
            if (_lastSkinUnit == _currentUnit) 
            {
                // Just update the "Equipped" checkmarks on existing items
                UpdateSkinItemsStatus();
                return;
            }
            _lastSkinUnit = _currentUnit;

            _skinDataList.Clear();
            List<GameObject> items = new List<GameObject>();

            Transform content = (_skinInfiniteScroll != null) ? _skinInfiniteScroll.Content : null;
            if (content == null) return;

            // Clear items
            foreach (Transform child in content) Destroy(child.gameObject);
                
            // Add Base Skin first (represented as null in the skin mapping)
            _skinDataList.Add(null);
            items.Add(CreateSkinItem(null, content)); 

            foreach (var skin in _currentUnit.Skins)
            {
                if (skin != null)
                {
                    _skinDataList.Add(skin);
                    items.Add(CreateSkinItem(skin, content));
                }
            }

            if (_skinInfiniteScroll)
            {
                _skinInfiniteScroll.Initialize(items);
            }

            // Default selection based on currently equipped skin
            int equippedIndex = _skinDataList.FindIndex(s => s != null ? s.SkinID == _currentUnit.EquippedSkinID : string.IsNullOrEmpty(_currentUnit.EquippedSkinID));
            if (equippedIndex >= 0)
            {
                SelectSkin(_skinDataList[equippedIndex]);
                UpdateSkinItemsStatus(equippedIndex);
            }
        }

        private void UpdateSkinItemsStatus(int activeIndex = -1)
        {
            if (_skinInfiniteScroll == null || _skinInfiniteScroll.Content == null) return;
            
            int i = 0;
            foreach (Transform child in _skinInfiniteScroll.Content)
            {
                var cardUI = child.GetComponent<SkinCardUI>();
                if (cardUI != null)
                {
                    // Update highlighting (active/inactive)
                    if (activeIndex != -1)
                    {
                        cardUI.SetHighlighted(i == activeIndex);
                    }
                    
                    // Re-set the equipped status in case it changed
                    // Since we don't store indices in the cards, we can derive it from the name if needed,
                    // but RefreshSkinsPage usually handles the initial state.
                }
                i++;
            }
        }

        private GameObject CreateSkinItem(UnitData.SkinData skin, Transform parent)
        {
            if (_skinItemPrefab == null) return null;
            var go = Instantiate(_skinItemPrefab, parent);
            var cardUI = go.GetComponent<SkinCardUI>();
            if (cardUI != null)
            {
                string theme = (skin != null) ? skin.SkinThemeName : "Default";
                Sprite icon = (skin != null) ? skin.Avatar : _currentUnit.BaseSkin.Avatar;
                int price = (skin != null) ? skin.UnlockCost : 0;
                string skinID = (skin != null) ? skin.SkinID : null;
                bool isLocked = !string.IsNullOrEmpty(skinID) && !_currentUnit.IsSkinUnlocked(skinID);
                
                // cardUI handles the visual layout of name, portrait, equipped/locked, and PRICE
                bool isEquipped = skinID == _currentUnit.EquippedSkinID || (string.IsNullOrEmpty(skinID) && string.IsNullOrEmpty(_currentUnit.EquippedSkinID));
                cardUI.SetState(theme, icon, isEquipped, isLocked, price);
            }
            return go;
        }

        private void SelectSkin(UnitData.SkinData skin)
        {
            _selectedSkin = skin;
            bool isBase = (skin == null);

            // Update Text & Assets
            if (_skinSplashPreview) 
                _skinSplashPreview.sprite = isBase ? _currentUnit.BaseSkin.FullSplashArt : skin.FullSplashArt;
            
            if (_skinNameText) 
                _skinNameText.text = isBase ? _currentUnit.UnitName.ToUpper() : (skin != null ? skin.SkinThemeName.ToUpper() : "DEFAULT");
            
            // Visual Preview (Chibi)
            if (_skinChibiPreview != null)
            {
                var img = _skinChibiPreview.GetComponent<Image>();
                if (img) img.sprite = isBase ? _currentUnit.BaseSkin.Chibi : skin.Chibi;
                
                // Update Animator
                _skinChibiPreview.runtimeAnimatorController = isBase ? _currentUnit.BaseSkin.AnimatorController : skin.AnimatorController;
            }

            if (_btnApplySkin)
            {
                _btnApplySkin.onClick.RemoveAllListeners();
                _btnApplySkin.onClick.AddListener(OnApplySkinClicked);
                
                var txt = _btnApplySkin.GetComponentInChildren<TextMeshProUGUI>();
                string skinID = skin != null ? skin.SkinID : null;
                bool alreadyEquipped = (_currentUnit.EquippedSkinID == skinID) || (string.IsNullOrEmpty(skinID) && string.IsNullOrEmpty(_currentUnit.EquippedSkinID));
                bool isUnlocked = _currentUnit.IsSkinUnlocked(skinID);
                
                if (alreadyEquipped) txt.text = "EQUIPPED";
                else if (!isUnlocked) txt.text = "UNLOCK";
                else txt.text = "APPLY";
                _btnApplySkin.interactable = !alreadyEquipped;
            }
        }

        private void OnApplySkinClicked()
        {
            if (_currentUnit == null) return;
            _currentUnit.EquippedSkinID = _selectedSkin != null ? _selectedSkin.SkinID : null;
            Debug.Log($"[Skins] Applied skin: {(_selectedSkin != null ? _selectedSkin.SkinThemeName : "Default")}");
            
            PopulateHeader(_currentUnit); 
            RefreshSkinsPage(); // Refresh selection icons on cards
        }
        #endregion

        private void PopulateHeader(UnitData u)
        {
            if (_nameText) _nameText.text = u.UnitName.ToUpper();
            if (_rarityText) _rarityText.text = u.Rarity.ToString().ToUpper();
            
            // Per User: Show full body cutout in the main portrait art
            if (_portraitImage) _portraitImage.sprite = u.GetSprite(UnitData.UnitImageType.FullSprite);
            
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
