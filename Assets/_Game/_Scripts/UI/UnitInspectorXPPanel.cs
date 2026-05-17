using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MaouSamaTD.Progression;
using MaouSamaTD.Units;
using MaouSamaTD.Data;
using MaouSamaTD.Managers;

namespace MaouSamaTD.UI
{
    public class UnitInspectorXPPanel : MonoBehaviour
    {
        public const int XP_DUPLICATE     = 5000;
        public const int XP_COMMON        = 100;
        public const int XP_RARE          = 500;
        public const int XP_EPIC          = 2000;
        public const int XP_LEGENDARY     = 10000;

        private static readonly string[] CoreIDs  = { "xp_core_common", "xp_core_rare", "xp_core_epic", "xp_core_legendary" };
        private static readonly int[]    CoreXPs   = { XP_COMMON, XP_RARE, XP_EPIC, XP_LEGENDARY };
        private static readonly string[] CoreLabels = { "Common Core", "Rare Core", "Epic Core", "Legendary Core" };

        [Header("Item Configs & Prefabs")]
        [SerializeField] private ItemConfigSO[] _xpCoreConfigs;
        [SerializeField] private GameObject _levelingCardPrefab;

        [Header("Legacy Grid (Ignored)")]
        [SerializeField] private TextMeshProUGUI _txtCommonCount;
        [SerializeField] private TextMeshProUGUI _txtRareCount;
        [SerializeField] private TextMeshProUGUI _txtEpicCount;
        [SerializeField] private TextMeshProUGUI _txtLegendaryCount;
        [SerializeField] private TextMeshProUGUI _txtCommonSelected;
        [SerializeField] private TextMeshProUGUI _txtRareSelected;
        [SerializeField] private TextMeshProUGUI _txtEpicSelected;
        [SerializeField] private TextMeshProUGUI _txtLegendarySelected;
        [SerializeField] private Button[] _btnCoreAdd;
        [SerializeField] private Button[] _btnCoreMinus;

        [Header("Unified Card Grid")]
        [SerializeField] private ScrollRect _duplicatesScrollRect;
        [SerializeField] private GameObject _duplicateItemPrefab;
        [SerializeField] private TextMeshProUGUI _txtDuplicatesInfo;

        [Header("XP Preview & Confirm")]
        [SerializeField] private TextMeshProUGUI _xpMeterValueText;
        [SerializeField] private TextMeshProUGUI _txtXpGain;
        [SerializeField] private TextMeshProUGUI _txtLevelPreview;
        [SerializeField] private Button _btnConfirmLevelUp;

        [Header("Auto-Add")]
        [SerializeField] private Button _btnAutoAdd;
        [SerializeField] private GameObject _autoAddSettingsRoot;
        [SerializeField] private Toggle _tglPrioritizeDupes;
        [SerializeField] private Toggle _tglStopAtCap;
        [SerializeField] private TMP_Dropdown _ddRarityLimit;

        private UnitData _currentUnit;
        private SaveManager _saveManager;
        private int[] _selectedCores = new int[4];
        private int _selectedDuplicatesCount = 0;
        private List<UnitInventoryEntry> _availableDuplicates = new List<UnitInventoryEntry>();
        private bool _autoSettingsVisible = false;
        private List<System.Action> _updateCallbacks = new List<System.Action>();

        public void Initialize(SaveManager saveManager)
        {
            _saveManager = saveManager;
            if (_btnConfirmLevelUp) _btnConfirmLevelUp.onClick.AddListener(PerformLevelUp);
            if (_btnAutoAdd) _btnAutoAdd.onClick.AddListener(OnAutoAdd);
        }

        public void Refresh(UnitData u)
        {
            _currentUnit = u;
            System.Array.Clear(_selectedCores, 0, 4);
            _selectedDuplicatesCount = 0;
            _availableDuplicates.Clear();
            _updateCallbacks.Clear();
            
            if (_autoAddSettingsRoot) _autoAddSettingsRoot.SetActive(false);
            _autoSettingsVisible = false;

            if (u == null || _saveManager == null || _saveManager.CurrentData == null) return;

            RebuildCardsList();
            UpdatePreview();
            
            foreach (var cb in _updateCallbacks) cb?.Invoke();
        }

        private void RebuildCardsList()
        {
            if (_duplicatesScrollRect == null || _duplicatesScrollRect.content == null) return;

            foreach (Transform child in _duplicatesScrollRect.content) Destroy(child.gameObject);

            var layout = _duplicatesScrollRect.content.GetComponent<GridLayoutGroup>();
            if (layout == null) layout = _duplicatesScrollRect.content.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(160, 220);
            layout.spacing = new Vector2(15, 15);
            layout.childAlignment = TextAnchor.UpperLeft;

            for (int i = 0; i < 4; i++)
            {
                int have = _saveManager.GetItemCount(CoreIDs[i]);
                if (have > 0)
                {
                    CreateCoreCard(i, have);
                }
            }

            _availableDuplicates = _saveManager.CurrentData.UnitInventory.FindAll(
                e => e.UnitID == _currentUnit.name && e.IsDuplicate);

            if (_txtDuplicatesInfo) _txtDuplicatesInfo.text = $"Select items below to convert to XP. Duplicates yield {XP_DUPLICATE} XP.";

            if (_availableDuplicates.Count > 0)
            {
                CreateDuplicateCardGroup(_availableDuplicates.Count);
            }
        }

        private void CreateCoreCard(int coreIndex, int availableCount)
        {
            Sprite iconSprite = null;
            Color bgColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            string title = CoreLabels[coreIndex];
            
            if (_xpCoreConfigs != null && coreIndex < _xpCoreConfigs.Length && _xpCoreConfigs[coreIndex] != null)
            {
                iconSprite = _xpCoreConfigs[coreIndex].ItemIcon;
                bgColor = _xpCoreConfigs[coreIndex].BackgroundColor;
                title = _xpCoreConfigs[coreIndex].ItemName;
            }

            if (_levelingCardPrefab != null)
            {
                var cardObj = Instantiate(_levelingCardPrefab, _duplicatesScrollRect.content);
                var ui = cardObj.GetComponent<LevelingCardUI>();
                if (ui != null)
                {
                    if (ui.BackgroundImage != null) ui.BackgroundImage.color = bgColor;
                    if (ui.IconImage != null)
                    {
                        if (iconSprite != null) { ui.IconImage.sprite = iconSprite; ui.IconImage.color = Color.white; }
                        else { ui.IconImage.color = coreIndex == 0 ? Color.white : coreIndex == 1 ? Color.blue : coreIndex == 2 ? Color.magenta : new Color(1f, 0.8f, 0.2f); }
                    }
                    if (ui.TitleText != null) ui.TitleText.text = title;
                    
                    ui.CardButton.onClick.AddListener(() => {
                        if (_selectedCores[coreIndex] < availableCount) { _selectedCores[coreIndex]++; UpdatePreview(); }
                    });
                    ui.MinusButton.onClick.AddListener(() => {
                        if (_selectedCores[coreIndex] > 0) { _selectedCores[coreIndex]--; UpdatePreview(); }
                    });

                    _updateCallbacks.Add(() => {
                        int sel = _selectedCores[coreIndex];
                        if (ui.SelectedOverlay != null) ui.SelectedOverlay.SetActive(sel > 0);
                        if (ui.SelectedText != null) ui.SelectedText.text = sel.ToString();
                        if (ui.AvailableText != null) ui.AvailableText.text = $"Remaining: {availableCount - sel}";
                        if (ui.MinusButtonObj != null) ui.MinusButtonObj.SetActive(sel > 0);
                    });
                }
                return;
            }

            // Fallback Procedural
            var fbObj = new GameObject($"CoreCard_{CoreIDs[coreIndex]}");
            fbObj.transform.SetParent(_duplicatesScrollRect.content, false);
            var cardBg = fbObj.AddComponent<Image>(); cardBg.color = bgColor;
            var btn = fbObj.AddComponent<Button>();
            
            var iconObj = new GameObject("Icon"); iconObj.transform.SetParent(fbObj.transform, false);
            var iconImg = iconObj.AddComponent<Image>();
            if (iconSprite != null) { iconImg.sprite = iconSprite; }
            else { iconImg.color = coreIndex == 0 ? Color.white : coreIndex == 1 ? Color.blue : coreIndex == 2 ? Color.magenta : new Color(1f, 0.8f, 0.2f); }
            var iconRect = iconImg.GetComponent<RectTransform>(); iconRect.anchorMin = new Vector2(0.2f, 0.4f); iconRect.anchorMax = new Vector2(0.8f, 0.9f); iconRect.sizeDelta = Vector2.zero;

            var titleTxt = new GameObject("Title").AddComponent<TextMeshProUGUI>(); titleTxt.transform.SetParent(fbObj.transform, false);
            titleTxt.text = title; titleTxt.fontSize = 18; titleTxt.fontStyle = FontStyles.Bold; titleTxt.alignment = TextAlignmentOptions.Center;
            var titleRect = titleTxt.GetComponent<RectTransform>(); titleRect.anchorMin = new Vector2(0, 0.25f); titleRect.anchorMax = new Vector2(1, 0.35f); titleRect.sizeDelta = Vector2.zero;

            var availTxt = new GameObject("AvailTxt").AddComponent<TextMeshProUGUI>(); availTxt.transform.SetParent(fbObj.transform, false);
            availTxt.fontSize = 16; availTxt.alignment = TextAlignmentOptions.Center;
            var availRect = availTxt.GetComponent<RectTransform>(); availRect.anchorMin = new Vector2(0, 0); availRect.anchorMax = new Vector2(1, 0.2f); availRect.sizeDelta = Vector2.zero;

            var selOverlay = new GameObject("SelOverlay"); selOverlay.transform.SetParent(fbObj.transform, false);
            var selImg = selOverlay.AddComponent<Image>(); selImg.color = new Color(0, 1, 0, 0.3f);
            var selRect = selImg.GetComponent<RectTransform>(); selRect.anchorMin = Vector2.zero; selRect.anchorMax = Vector2.one; selRect.sizeDelta = Vector2.zero;
            
            var selTxt = new GameObject("SelTxt").AddComponent<TextMeshProUGUI>(); selTxt.transform.SetParent(selOverlay.transform, false);
            selTxt.fontSize = 50; selTxt.fontStyle = FontStyles.Bold; selTxt.alignment = TextAlignmentOptions.Center; selTxt.color = Color.white;
            var selTxtRect = selTxt.GetComponent<RectTransform>(); selTxtRect.anchorMin = Vector2.zero; selTxtRect.anchorMax = Vector2.one; selTxtRect.sizeDelta = Vector2.zero;

            var minusObj = new GameObject("MinusBtn"); minusObj.transform.SetParent(fbObj.transform, false);
            var minusImg = minusObj.AddComponent<Image>(); minusImg.color = new Color(1, 0, 0, 0.8f);
            var minusRect = minusImg.GetComponent<RectTransform>(); minusRect.anchorMin = new Vector2(0.7f, 0.7f); minusRect.anchorMax = new Vector2(1, 1); minusRect.sizeDelta = Vector2.zero;
            var minusBtn = minusObj.AddComponent<Button>();
            var minusTxt = new GameObject("MinusTxt").AddComponent<TextMeshProUGUI>(); minusTxt.transform.SetParent(minusObj.transform, false);
            minusTxt.text = "-"; minusTxt.fontSize = 30; minusTxt.alignment = TextAlignmentOptions.Center;
            var mtRect = minusTxt.GetComponent<RectTransform>(); mtRect.anchorMin = Vector2.zero; mtRect.anchorMax = Vector2.one; mtRect.sizeDelta = Vector2.zero;

            btn.onClick.AddListener(() => { if (_selectedCores[coreIndex] < availableCount) { _selectedCores[coreIndex]++; UpdatePreview(); } });
            minusBtn.onClick.AddListener(() => { if (_selectedCores[coreIndex] > 0) { _selectedCores[coreIndex]--; UpdatePreview(); } });

            _updateCallbacks.Add(() => {
                int sel = _selectedCores[coreIndex];
                selOverlay.SetActive(sel > 0);
                selTxt.text = sel.ToString();
                availTxt.text = $"Remaining: {availableCount - sel}";
                minusObj.SetActive(sel > 0);
            });
        }

        private void CreateDuplicateCardGroup(int totalCount)
        {
            if (_levelingCardPrefab != null)
            {
                var cardObj = Instantiate(_levelingCardPrefab, _duplicatesScrollRect.content);
                var ui = cardObj.GetComponent<LevelingCardUI>();
                if (ui != null)
                {
                    if (ui.BackgroundImage != null) ui.BackgroundImage.color = new Color(0.3f, 0.1f, 0.1f, 0.9f);
                    if (ui.IconImage != null)
                    {
                        ui.IconImage.sprite = _currentUnit.GetSprite(UnitData.UnitImageType.WaistUp);
                        if (ui.IconImage.sprite == null) ui.IconImage.sprite = _currentUnit.GetSprite(UnitData.UnitImageType.Chibi);
                        ui.IconImage.preserveAspect = true;
                        ui.IconImage.color = Color.white;
                    }
                    if (ui.TitleText != null) { ui.TitleText.text = "DUPLICATES"; ui.TitleText.color = new Color(1f, 0.4f, 0.4f); }
                    if (ui.SelectedOverlay != null) ui.SelectedOverlay.GetComponent<Image>().color = new Color(1, 0, 0, 0.4f);
                    
                    ui.CardButton.onClick.AddListener(() => {
                        if (_selectedDuplicatesCount < totalCount) { _selectedDuplicatesCount++; UpdatePreview(); }
                    });
                    ui.MinusButton.onClick.AddListener(() => {
                        if (_selectedDuplicatesCount > 0) { _selectedDuplicatesCount--; UpdatePreview(); }
                    });

                    _updateCallbacks.Add(() => {
                        int sel = _selectedDuplicatesCount;
                        if (ui.SelectedOverlay != null) ui.SelectedOverlay.SetActive(sel > 0);
                        if (ui.SelectedText != null) ui.SelectedText.text = sel.ToString();
                        if (ui.AvailableText != null) ui.AvailableText.text = $"Remaining: {totalCount - sel}";
                        if (ui.MinusButtonObj != null) ui.MinusButtonObj.SetActive(sel > 0);
                    });
                }
                return;
            }

            // Fallback Procedural
            var fbObj = new GameObject("DupeCardGroup");
            fbObj.transform.SetParent(_duplicatesScrollRect.content, false);
            var cardBg = fbObj.AddComponent<Image>(); cardBg.color = new Color(0.3f, 0.1f, 0.1f, 0.9f);
            var btn = fbObj.AddComponent<Button>();
            
            var iconObj = new GameObject("Icon"); iconObj.transform.SetParent(fbObj.transform, false);
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = _currentUnit.GetSprite(UnitData.UnitImageType.WaistUp);
            if (iconImg.sprite == null) iconImg.sprite = _currentUnit.GetSprite(UnitData.UnitImageType.Chibi);
            iconImg.preserveAspect = true;
            var iconRect = iconImg.GetComponent<RectTransform>(); iconRect.anchorMin = new Vector2(0.05f, 0.25f); iconRect.anchorMax = new Vector2(0.95f, 0.95f); iconRect.sizeDelta = Vector2.zero;

            var titleTxt = new GameObject("Title").AddComponent<TextMeshProUGUI>(); titleTxt.transform.SetParent(fbObj.transform, false);
            titleTxt.text = "DUPLICATES"; titleTxt.fontSize = 18; titleTxt.fontStyle = FontStyles.Bold; titleTxt.color = new Color(1f, 0.4f, 0.4f); titleTxt.alignment = TextAlignmentOptions.Center;
            var titleRect = titleTxt.GetComponent<RectTransform>(); titleRect.anchorMin = new Vector2(0, 0.25f); titleRect.anchorMax = new Vector2(1, 0.35f); titleRect.sizeDelta = Vector2.zero;

            var availTxt = new GameObject("AvailTxt").AddComponent<TextMeshProUGUI>(); availTxt.transform.SetParent(fbObj.transform, false);
            availTxt.fontSize = 16; availTxt.alignment = TextAlignmentOptions.Center;
            var availRect = availTxt.GetComponent<RectTransform>(); availRect.anchorMin = new Vector2(0, 0); availRect.anchorMax = new Vector2(1, 0.2f); availRect.sizeDelta = Vector2.zero;

            var selOverlay = new GameObject("SelOverlay"); selOverlay.transform.SetParent(fbObj.transform, false);
            var selImg = selOverlay.AddComponent<Image>(); selImg.color = new Color(1, 0, 0, 0.4f);
            var selRect = selImg.GetComponent<RectTransform>(); selRect.anchorMin = Vector2.zero; selRect.anchorMax = Vector2.one; selRect.sizeDelta = Vector2.zero;
            
            var selTxt = new GameObject("SelTxt").AddComponent<TextMeshProUGUI>(); selTxt.transform.SetParent(selOverlay.transform, false);
            selTxt.fontSize = 50; selTxt.fontStyle = FontStyles.Bold; selTxt.alignment = TextAlignmentOptions.Center; selTxt.color = Color.white;
            var selTxtRect = selTxt.GetComponent<RectTransform>(); selTxtRect.anchorMin = Vector2.zero; selTxtRect.anchorMax = Vector2.one; selTxtRect.sizeDelta = Vector2.zero;

            var minusObj = new GameObject("MinusBtn"); minusObj.transform.SetParent(fbObj.transform, false);
            var minusImg = minusObj.AddComponent<Image>(); minusImg.color = new Color(1, 0, 0, 0.8f);
            var minusRect = minusImg.GetComponent<RectTransform>(); minusRect.anchorMin = new Vector2(0.7f, 0.7f); minusRect.anchorMax = new Vector2(1, 1); minusRect.sizeDelta = Vector2.zero;
            var minusBtn = minusObj.AddComponent<Button>();
            var minusTxt = new GameObject("MinusTxt").AddComponent<TextMeshProUGUI>(); minusTxt.transform.SetParent(minusObj.transform, false);
            minusTxt.text = "-"; minusTxt.fontSize = 30; minusTxt.alignment = TextAlignmentOptions.Center;
            var mtRect = minusTxt.GetComponent<RectTransform>(); mtRect.anchorMin = Vector2.zero; mtRect.anchorMax = Vector2.one; mtRect.sizeDelta = Vector2.zero;

            btn.onClick.AddListener(() => { if (_selectedDuplicatesCount < totalCount) { _selectedDuplicatesCount++; UpdatePreview(); } });
            minusBtn.onClick.AddListener(() => { if (_selectedDuplicatesCount > 0) { _selectedDuplicatesCount--; UpdatePreview(); } });

            _updateCallbacks.Add(() => {
                int sel = _selectedDuplicatesCount;
                selOverlay.SetActive(sel > 0);
                selTxt.text = sel.ToString();
                availTxt.text = $"Remaining: {totalCount - sel}";
                minusObj.SetActive(sel > 0);
            });
        }

        private void UpdatePreview()
        {
            if (_currentUnit == null) return;

            int totalXP = CalculateTotalXP();
            int newLevel = SimulateLevelGain(_currentUnit.Level, _currentUnit.Experience, totalXP, _currentUnit.MaxLevel);
            int reqXP    = ProgressionLogic.GetRequiredXP(_currentUnit.Level);

            if (_xpMeterValueText) _xpMeterValueText.text = $"{_currentUnit.Experience} / {reqXP}";
            if (_txtXpGain)        _txtXpGain.text        = totalXP > 0 ? $"+{totalXP} XP" : "";
            if (_txtLevelPreview)  _txtLevelPreview.text  = newLevel > _currentUnit.Level
                                                            ? $"Lv {_currentUnit.Level} → {newLevel}"
                                                            : $"Lv {_currentUnit.Level}";

            if (_btnConfirmLevelUp) _btnConfirmLevelUp.interactable = totalXP > 0;
            
            foreach (var cb in _updateCallbacks) cb?.Invoke();
        }

        private int CalculateTotalXP()
        {
            int total = _selectedDuplicatesCount * XP_DUPLICATE;
            for (int i = 0; i < 4; i++)
                total += _selectedCores[i] * CoreXPs[i];
            return total;
        }

        private static int SimulateLevelGain(int startLevel, int startXP, int addedXP, int maxLevel)
        {
            int level = startLevel;
            int xp    = startXP + addedXP;
            while (level < maxLevel)
            {
                int req = ProgressionLogic.GetRequiredXP(level);
                if (xp >= req) { xp -= req; level++; }
                else break;
            }
            return level;
        }

        private void OnAutoAdd()
        {
            _autoSettingsVisible = !_autoSettingsVisible;
            if (_autoAddSettingsRoot) _autoAddSettingsRoot.SetActive(_autoSettingsVisible);
            if (!_autoSettingsVisible) RunAutoAdd();
        }

        private void RunAutoAdd()
        {
            if (_currentUnit == null || _saveManager == null) return;

            bool prioDupes  = _tglPrioritizeDupes != null && _tglPrioritizeDupes.isOn;
            bool stopAtCap  = _tglStopAtCap        != null && _tglStopAtCap.isOn;
            int  rarityLim  = _ddRarityLimit        != null ? _ddRarityLimit.value : 3;

            System.Array.Clear(_selectedCores, 0, 4);
            _selectedDuplicatesCount = 0;

            int target  = GetXPNeededForCap();
            if (target <= 0) { UpdatePreview(); return; }

            int accumulated = 0;

            if (prioDupes)
            {
                int available = _availableDuplicates.Count;
                int needed = stopAtCap ? Mathf.CeilToInt((float)target / XP_DUPLICATE) : available;
                _selectedDuplicatesCount = Mathf.Min(available, needed);
                accumulated += _selectedDuplicatesCount * XP_DUPLICATE;
            }

            for (int i = rarityLim; i >= 0 && accumulated < target; i--)
            {
                int have  = _saveManager.GetItemCount(CoreIDs[i]);
                int need  = Mathf.CeilToInt((float)(target - accumulated) / CoreXPs[i]);
                int use   = Mathf.Min(have, need);
                _selectedCores[i] = use;
                accumulated += use * CoreXPs[i];
            }

            if (!prioDupes)
            {
                int remainingTarget = target - accumulated;
                if (!stopAtCap || remainingTarget > 0)
                {
                    int available = _availableDuplicates.Count;
                    int needed = stopAtCap ? Mathf.CeilToInt((float)remainingTarget / XP_DUPLICATE) : available;
                    _selectedDuplicatesCount = Mathf.Min(available, needed);
                    accumulated += _selectedDuplicatesCount * XP_DUPLICATE;
                }
            }

            UpdatePreview();
        }

        private int GetXPNeededForCap()
        {
            if (_currentUnit == null) return 0;
            int level = _currentUnit.Level;
            int xp    = _currentUnit.Experience;
            int total = 0;
            while (level < _currentUnit.MaxLevel)
            {
                int req = ProgressionLogic.GetRequiredXP(level);
                total += (req - xp);
                xp = 0;
                level++;
            }
            return total;
        }

        private void PerformLevelUp()
        {
            if (_currentUnit == null) return;
            int totalXP = CalculateTotalXP();
            if (totalXP <= 0) return;

            ProgressionLogic.AddXP(_currentUnit, totalXP);

            for (int i = 0; i < 4; i++)
            {
                if (_selectedCores[i] > 0)
                    _saveManager.RemoveItem(CoreIDs[i], _selectedCores[i]);
            }

            for (int i = 0; i < _selectedDuplicatesCount; i++)
            {
                if (i < _availableDuplicates.Count)
                    _saveManager.CurrentData.UnitInventory.Remove(_availableDuplicates[i]);
            }

            _saveManager.Save();
            Refresh(_currentUnit);

            Debug.Log($"[XPPanel] Level up! Gained {totalXP} XP. New level: {_currentUnit.Level}");
        }
    }
}
