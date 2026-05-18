using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using MaouSamaTD.Progression;
using MaouSamaTD.Units;
using MaouSamaTD.Data;
using MaouSamaTD.Managers;
using DG.Tweening;

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
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Image _xpCurrentFill;
        [SerializeField] private Image _xpAddFill;

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

        private Button _btnAddAll;
        private Button _btnDeselectAll;
        private bool _isAnimating = false;

        public void Initialize(SaveManager saveManager)
        {
            _saveManager = saveManager;
            if (_btnConfirmLevelUp) _btnConfirmLevelUp.onClick.AddListener(PerformLevelUp);
            if (_btnAutoAdd) _btnAutoAdd.onClick.AddListener(OnAutoAdd);

            // Opaque ScrollRect viewport to prevent transparent masking issues making cards invisible
            if (_duplicatesScrollRect != null && _duplicatesScrollRect.viewport != null)
            {
                var viewportImg = _duplicatesScrollRect.viewport.GetComponent<Image>();
                if (viewportImg != null)
                {
                    var c = viewportImg.color;
                    c.a = 1f;
                    viewportImg.color = c;
                }
            }

            SetupExtraPageButtons();
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

            ApplyInterItalicFont();

            if (_portraitImage)
            {
                _portraitImage.sprite = u.GetSprite(UnitData.UnitImageType.WaistUp);
                if (_portraitImage.sprite == null) _portraitImage.sprite = u.GetSprite(UnitData.UnitImageType.Chibi);
            }

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
                    
                    SetupPlusButton(ui, () => { _selectedCores[coreIndex]++; UpdatePreview(); }, () => _selectedCores[coreIndex] < availableCount);
                    SetupMinusButton(ui, () => { _selectedCores[coreIndex]--; UpdatePreview(); }, () => _selectedCores[coreIndex] > 0);

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
                        ui.IconImage.sprite = _currentUnit.GetSprite(UnitData.UnitImageType.Avatar);
                        if (ui.IconImage.sprite == null) ui.IconImage.sprite = _currentUnit.GetSprite(UnitData.UnitImageType.Chibi);
                        ui.IconImage.preserveAspect = true;
                        ui.IconImage.color = Color.white;
                    }
                    if (ui.TitleText != null) { ui.TitleText.text = "DUPLICATES"; ui.TitleText.color = new Color(1f, 0.4f, 0.4f); }
                    if (ui.SelectedOverlay != null) ui.SelectedOverlay.GetComponent<Image>().color = new Color(1, 0, 0, 0.4f);
                    
                    SetupPlusButton(ui, () => { _selectedDuplicatesCount++; UpdatePreview(); }, () => _selectedDuplicatesCount < totalCount);
                    SetupMinusButton(ui, () => { _selectedDuplicatesCount--; UpdatePreview(); }, () => _selectedDuplicatesCount > 0);

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
            iconImg.sprite = _currentUnit.GetSprite(UnitData.UnitImageType.Avatar);
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
            GetSimulatedLevelAndXP(_currentUnit.Level, _currentUnit.Experience, totalXP, _currentUnit.MaxLevel, out int newLevel, out int simXP, out int simReqXP);
            int reqXP = ProgressionLogic.GetRequiredXP(_currentUnit.Level);

            if (_xpMeterValueText) _xpMeterValueText.text = $"{simXP} / {simReqXP}";
            if (_txtXpGain)        _txtXpGain.text        = totalXP > 0 ? $"+{totalXP} XP" : "";
            if (_txtLevelPreview)  _txtLevelPreview.text  = newLevel > _currentUnit.Level
                                                            ? $"Lv {_currentUnit.Level} ➔ {newLevel}"
                                                            : $"Lv {_currentUnit.Level}";

            // Update XP fills smoothly using DOTween
            if (_xpCurrentFill)
            {
                _xpCurrentFill.DOKill();
                float targetFill = (newLevel > _currentUnit.Level) ? 0f : (float)_currentUnit.Experience / reqXP;
                _xpCurrentFill.DOFillAmount(targetFill, 0.25f).SetEase(Ease.OutQuad).SetUpdate(true);
            }
            if (_xpAddFill)
            {
                _xpAddFill.DOKill();
                float targetFill = (newLevel > _currentUnit.Level) ? (float)simXP / simReqXP : (float)(_currentUnit.Experience + totalXP) / reqXP;
                _xpAddFill.DOFillAmount(targetFill, 0.25f).SetEase(Ease.OutQuad).SetUpdate(true);
            }

            // Stats Preview (Genshin-style green text)
            EnsureStatsPreviewUI();
            if (_statsPreviewContainer != null)
            {
                float curHp = _currentUnit.CalculatedStats.MaxHp;
                float curAtk = _currentUnit.CalculatedStats.Attack;
                float curDef = _currentUnit.CalculatedStats.Defense;

                CalculateSimulatedStats(_currentUnit, newLevel, out float nextHp, out float nextAtk, out float nextDef);

                if (_txtHpPreview)
                {
                    if (newLevel > _currentUnit.Level)
                        _txtHpPreview.text = $"{curHp:F0} <color=#00FF00>➔ {nextHp:F0} (+{(nextHp - curHp):F0})</color>";
                    else
                        _txtHpPreview.text = $"{curHp:F0}";
                }

                if (_txtAtkPreview)
                {
                    if (newLevel > _currentUnit.Level)
                        _txtAtkPreview.text = $"{curAtk:F0} <color=#00FF00>➔ {nextAtk:F0} (+{(nextAtk - curAtk):F0})</color>";
                    else
                        _txtAtkPreview.text = $"{curAtk:F0}";
                }

                if (_txtDefPreview)
                {
                    if (newLevel > _currentUnit.Level)
                        _txtDefPreview.text = $"{curDef:F0} <color=#00FF00>➔ {nextDef:F0} (+{(nextDef - curDef):F0})</color>";
                    else
                        _txtDefPreview.text = $"{curDef:F0}";
                }
            }

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

            // Warning Check: Sacrificing duplicates when not all memories are unlocked
            if (_selectedDuplicatesCount > 0)
            {
                var entry = _saveManager.CurrentData.UnitInventory.Find(e => e.UnitID == _currentUnit.name && !e.IsDuplicate);
                var unlocked = entry?.UnlockedLores ?? new List<int> { 0 };
                int memCount = _currentUnit.LoreEntries != null ? _currentUnit.LoreEntries.Count : 0;
                int totalChambers = Mathf.Max(memCount, 5);

                if (unlocked.Count < totalChambers)
                {
                    ShowMemorySacrificeWarning(totalXP);
                    return;
                }
            }

            ExecutePerformLevelUp(totalXP);
        }

        private void ShowMemorySacrificeWarning(int totalXP)
        {
            var overlay = new GameObject("SacrificeWarningOverlay", typeof(RectTransform));
            overlay.transform.SetParent(this.transform, false);
            var rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var bgImg = overlay.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            var dialogBox = new GameObject("DialogBox", typeof(RectTransform));
            dialogBox.transform.SetParent(overlay.transform, false);
            var dbRect = dialogBox.GetComponent<RectTransform>();
            dbRect.anchorMin = new Vector2(0.5f, 0.5f);
            dbRect.anchorMax = new Vector2(0.5f, 0.5f);
            dbRect.pivot = new Vector2(0.5f, 0.5f);
            dbRect.sizeDelta = new Vector2(480, 260);

            var dbImg = dialogBox.AddComponent<Image>();
            dbImg.color = new Color(0.12f, 0.12f, 0.16f, 1f);
            var dbOutline = dialogBox.AddComponent<Outline>();
            dbOutline.effectColor = new Color(0.85f, 0.25f, 0.2f, 0.8f);
            dbOutline.effectDistance = new Vector2(2, 2);

            var title = new GameObject("Title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            title.transform.SetParent(dialogBox.transform, false);
            title.text = "⚠️ WARNING: LORE CHAMBERS LOCKED";
            title.fontSize = 20;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.95f, 0.3f, 0.25f);
            title.alignment = TextAlignmentOptions.Center;
            var tRect = title.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.05f, 0.8f);
            tRect.anchorMax = new Vector2(0.95f, 0.95f);
            tRect.sizeDelta = Vector2.zero;

            var body = new GameObject("Body", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            body.transform.SetParent(dialogBox.transform, false);
            body.text = "This Vassal still has locked Lore Chambers/Memories. Sacrificing their duplicates as leveling fodder will consume copies that are needed to unlock those chambers.\n\nDo you want to proceed with sacrificing them?";
            body.fontSize = 14;
            body.alignment = TextAlignmentOptions.Center;
            body.color = Color.white;
            var bRect = body.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.05f, 0.25f);
            bRect.anchorMax = new Vector2(0.95f, 0.75f);
            bRect.sizeDelta = Vector2.zero;

            // Confirm Button
            var btnConfirm = new GameObject("ConfirmBtn", typeof(RectTransform));
            btnConfirm.transform.SetParent(dialogBox.transform, false);
            var bcImg = btnConfirm.AddComponent<Image>();
            bcImg.color = new Color(0.8f, 0.2f, 0.15f, 1f);
            var bcBtn = btnConfirm.AddComponent<Button>();
            var bcTxt = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            bcTxt.transform.SetParent(btnConfirm.transform, false);
            bcTxt.text = "SACRIFICE";
            bcTxt.fontSize = 14;
            bcTxt.fontStyle = FontStyles.Bold;
            bcTxt.alignment = TextAlignmentOptions.Center;
            bcTxt.color = Color.white;
            var bctRect = bcTxt.GetComponent<RectTransform>();
            bctRect.anchorMin = Vector2.zero;
            bctRect.anchorMax = Vector2.one;
            bctRect.sizeDelta = Vector2.zero;
            var bcRect = btnConfirm.GetComponent<RectTransform>();
            bcRect.anchorMin = new Vector2(0.15f, 0.05f);
            bcRect.anchorMax = new Vector2(0.45f, 0.2f);
            bcRect.sizeDelta = Vector2.zero;

            bcBtn.onClick.AddListener(() =>
            {
                Destroy(overlay);
                ExecutePerformLevelUp(totalXP);
            });

            // Cancel Button
            var btnCancel = new GameObject("CancelBtn", typeof(RectTransform));
            btnCancel.transform.SetParent(dialogBox.transform, false);
            var blImg = btnCancel.AddComponent<Image>();
            blImg.color = new Color(0.25f, 0.25f, 0.3f, 1f);
            var blBtn = btnCancel.AddComponent<Button>();
            var blTxt = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            blTxt.transform.SetParent(btnCancel.transform, false);
            blTxt.text = "CANCEL";
            blTxt.fontSize = 14;
            blTxt.fontStyle = FontStyles.Bold;
            blTxt.alignment = TextAlignmentOptions.Center;
            blTxt.color = Color.white;
            var bltRect = blTxt.GetComponent<RectTransform>();
            bltRect.anchorMin = Vector2.zero;
            bltRect.anchorMax = Vector2.one;
            bltRect.sizeDelta = Vector2.zero;
            var blRect = btnCancel.GetComponent<RectTransform>();
            blRect.anchorMin = new Vector2(0.55f, 0.05f);
            blRect.anchorMax = new Vector2(0.85f, 0.2f);
            blRect.sizeDelta = Vector2.zero;

            blBtn.onClick.AddListener(() =>
            {
                Destroy(overlay);
            });
        }

        private void ExecutePerformLevelUp(int totalXP)
        {
            if (_isAnimating) return;
            StartCoroutine(AnimateXPProgression(_currentUnit.Level, _currentUnit.Experience, totalXP));
        }

        private void ApplyInterItalicFont()
        {
            if (_xpMeterValueText == null) return;
            _xpMeterValueText.fontStyle = FontStyles.Italic | FontStyles.Bold;
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var f in fonts)
            {
                if (f.name.Contains("Inter-Italic") || f.name.Contains("Inter_Italic"))
                {
                    _xpMeterValueText.font = f;
                    break;
                }
            }
        }

        private void SetupPlusButton(LevelingCardUI ui, System.Action onAdd, System.Func<bool> canAdd)
        {
            if (ui == null) return;
            if (ui.PlusButtonObj != null) Destroy(ui.PlusButtonObj);

            var plusObj = new GameObject("PlusBtn", typeof(RectTransform));
            plusObj.transform.SetParent(ui.transform, false);
            ui.PlusButtonObj = plusObj;

            var rect = plusObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.7f);
            rect.anchorMax = new Vector2(0.3f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var img = plusObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.65f, 0.3f, 0.9f);

            var btn = plusObj.AddComponent<Button>();
            ui.PlusButton = btn;

            var txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(plusObj.transform, false);
            var txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = "+";
            txt.fontSize = 24;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;

            var txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            var holdPlus = plusObj.AddComponent<PointerHoldTrigger>();
            holdPlus.OnClick = () => { if (canAdd()) onAdd(); };
            holdPlus.OnHoldTick = () => { if (canAdd()) onAdd(); };

            if (ui.CardButton != null)
            {
                ui.CardButton.onClick.RemoveAllListeners();
                var holdCard = ui.CardButton.gameObject.GetComponent<PointerHoldTrigger>();
                if (holdCard == null) holdCard = ui.CardButton.gameObject.AddComponent<PointerHoldTrigger>();
                holdCard.OnClick = () => { if (canAdd()) onAdd(); };
                holdCard.OnHoldTick = () => { if (canAdd()) onAdd(); };
            }
        }

        private void SetupMinusButton(LevelingCardUI ui, System.Action onMinus, System.Func<bool> canMinus)
        {
            if (ui == null || ui.MinusButton == null) return;
            ui.MinusButton.onClick.RemoveAllListeners();

            var holdMinus = ui.MinusButton.gameObject.GetComponent<PointerHoldTrigger>();
            if (holdMinus == null) holdMinus = ui.MinusButton.gameObject.AddComponent<PointerHoldTrigger>();
            holdMinus.OnClick = () => { if (canMinus()) onMinus(); };
            holdMinus.OnHoldTick = () => { if (canMinus()) onMinus(); };
        }

        private void AddAll()
        {
            if (_currentUnit == null || _saveManager == null) return;
            for (int i = 0; i < 4; i++)
            {
                int have = _saveManager.GetItemCount(CoreIDs[i]);
                _selectedCores[i] = have;
            }
            _selectedDuplicatesCount = _availableDuplicates.Count;
            UpdatePreview();
            foreach (var cb in _updateCallbacks) cb?.Invoke();
        }

        private void DeselectAll()
        {
            System.Array.Clear(_selectedCores, 0, 4);
            _selectedDuplicatesCount = 0;
            UpdatePreview();
            foreach (var cb in _updateCallbacks) cb?.Invoke();
        }

        private void SetupExtraPageButtons()
        {
            var controlsObj = GameObject.Find("MainCanvas/MainUIContainer/Page_Content_Area/Vassals_Page_UI/UnitInspector_FullScreen_UI/Unit_Leveling_Page/MainLayout/RightPanel/BottomControls");
            if (controlsObj == null) return;

            var existingAddAll = controlsObj.transform.Find("AddAll_Button");
            if (existingAddAll != null) Destroy(existingAddAll.gameObject);
            var existingDeselectAll = controlsObj.transform.Find("DeselectAll_Button");
            if (existingDeselectAll != null) Destroy(existingDeselectAll.gameObject);

            var autoAddTemplate = controlsObj.transform.Find("AutoAdd_Button");
            if (autoAddTemplate == null) return;

            var addAllObj = Instantiate(autoAddTemplate.gameObject, controlsObj.transform);
            addAllObj.name = "AddAll_Button";
            _btnAddAll = addAllObj.GetComponent<Button>();
            _btnAddAll.onClick.RemoveAllListeners();
            _btnAddAll.onClick.AddListener(AddAll);

            var addAllRect = addAllObj.GetComponent<RectTransform>();
            addAllRect.anchorMin = new Vector2(0f, 0.5f);
            addAllRect.anchorMax = new Vector2(0f, 0.5f);
            addAllRect.pivot = new Vector2(0f, 0.5f);
            addAllRect.sizeDelta = new Vector2(180, 60);
            addAllRect.anchoredPosition = new Vector2(220, 0);

            var addAllText = addAllObj.GetComponentInChildren<TextMeshProUGUI>();
            if (addAllText != null) { addAllText.text = "ADD ALL"; addAllText.fontSize = 16; }
            var addAllImage = addAllObj.GetComponent<Image>();
            if (addAllImage != null) addAllImage.color = new Color(0.18f, 0.45f, 0.7f, 1f);

            var deselectAllObj = Instantiate(autoAddTemplate.gameObject, controlsObj.transform);
            deselectAllObj.name = "DeselectAll_Button";
            _btnDeselectAll = deselectAllObj.GetComponent<Button>();
            _btnDeselectAll.onClick.RemoveAllListeners();
            _btnDeselectAll.onClick.AddListener(DeselectAll);

            var deselectRect = deselectAllObj.GetComponent<RectTransform>();
            deselectRect.anchorMin = new Vector2(0f, 0.5f);
            deselectRect.anchorMax = new Vector2(0f, 0.5f);
            deselectRect.pivot = new Vector2(0f, 0.5f);
            deselectRect.sizeDelta = new Vector2(180, 60);
            deselectRect.anchoredPosition = new Vector2(420, 0);

            var deselectText = deselectAllObj.GetComponentInChildren<TextMeshProUGUI>();
            if (deselectText != null) { deselectText.text = "DESELECT ALL"; deselectText.fontSize = 16; }
            var deselectImage = deselectAllObj.GetComponent<Image>();
            if (deselectImage != null) deselectImage.color = new Color(0.6f, 0.2f, 0.2f, 1f);
        }

        private void TriggerLevelUpVFX()
        {
            if (_xpMeterValueText == null) return;
            Vector3 spawnPos = _xpMeterValueText.transform.position;
            string[] symbols = { "✦", "★", "✦", "✨", "+1" };
            Color[] colors = { new Color(1f, 0.85f, 0.2f), new Color(1f, 0.6f, 0.1f), new Color(1f, 1f, 1f), new Color(0.3f, 1f, 0.4f) };

            for (int i = 0; i < 15; i++)
            {
                var symbol = symbols[Random.Range(0, symbols.Length)];
                var color = colors[Random.Range(0, colors.Length)];
                var go = new GameObject("XP_VFX_Particle", typeof(RectTransform));
                go.transform.SetParent(this.transform.parent, false);
                go.transform.position = spawnPos + new Vector3(Random.Range(-50f, 50f), Random.Range(-10f, 10f), 0);

                var txt = go.AddComponent<TextMeshProUGUI>();
                txt.text = symbol;
                txt.fontSize = Random.Range(20, 36);
                txt.fontStyle = FontStyles.Bold;
                txt.color = color;
                txt.alignment = TextAlignmentOptions.Center;

                StartCoroutine(AnimateVFXParticle(go.GetComponent<RectTransform>(), txt));
            }
        }

        private System.Collections.IEnumerator AnimateVFXParticle(RectTransform rect, TextMeshProUGUI txt)
        {
            float duration = Random.Range(0.5f, 0.8f);
            float elapsed = 0f;
            Vector2 velocity = new Vector2(Random.Range(-250f, 250f), Random.Range(100f, 400f));
            float gravity = -500f;
            Vector3 startScale = Vector3.one * Random.Range(0.5f, 1.2f);
            Vector3 endScale = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                velocity.y += gravity * Time.deltaTime;
                rect.anchoredPosition += velocity * Time.deltaTime;
                rect.localScale = Vector3.Lerp(startScale, endScale, t);
                txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, Mathf.Lerp(1f, 0f, t));
                yield return null;
            }
            Destroy(rect.gameObject);
        }

        private System.Collections.IEnumerator AnimateXPProgression(int startLevel, int startXP, int totalXPGain)
        {
            _isAnimating = true;
            if (_btnConfirmLevelUp) _btnConfirmLevelUp.interactable = false;
            if (_btnAutoAdd) _btnAutoAdd.interactable = false;
            if (_btnAddAll) _btnAddAll.interactable = false;
            if (_btnDeselectAll) _btnDeselectAll.interactable = false;

            int currentLevel = startLevel;
            int maxLevel = _currentUnit.MaxLevel;
            float animDuration = 1.0f;
            float elapsed = 0f;

            int totalStartXP = 0;
            for (int l = 1; l < startLevel; l++) totalStartXP += ProgressionLogic.GetRequiredXP(l);
            totalStartXP += startXP;

            int totalTargetXP = totalStartXP + totalXPGain;

            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animDuration);
                int currentTotalSimulated = Mathf.RoundToInt(Mathf.Lerp(totalStartXP, totalTargetXP, t));
                
                int simLevel = 1;
                int tempXP = currentTotalSimulated;
                while (simLevel < maxLevel)
                {
                    int req = ProgressionLogic.GetRequiredXP(simLevel);
                    if (tempXP >= req) { tempXP -= req; simLevel++; }
                    else break;
                }

                if (simLevel > currentLevel)
                {
                    currentLevel = simLevel;
                    TriggerLevelUpVFX();
                }

                int reqXPForSimLevel = ProgressionLogic.GetRequiredXP(simLevel);
                float pct = (float)tempXP / reqXPForSimLevel;

                if (_xpCurrentFill) _xpCurrentFill.fillAmount = pct;
                if (_xpAddFill) _xpAddFill.fillAmount = pct;
                if (_xpMeterValueText) _xpMeterValueText.text = $"{tempXP} / {reqXPForSimLevel}";
                if (_txtLevelPreview)
                {
                    _txtLevelPreview.text = simLevel > startLevel 
                        ? $"Lv {startLevel} → {simLevel}"
                        : $"Lv {startLevel}";
                }
                yield return null;
            }

            int finalLevel = SimulateLevelGain(startLevel, startXP, totalXPGain, maxLevel);
            int finalXP = (startXP + totalXPGain);
            for (int l = startLevel; l < finalLevel; l++) finalXP -= ProgressionLogic.GetRequiredXP(l);

            int finalReq = ProgressionLogic.GetRequiredXP(finalLevel);
            float finalPct = (float)finalXP / finalReq;
            
            if (_xpCurrentFill) _xpCurrentFill.fillAmount = finalPct;
            if (_xpAddFill) _xpAddFill.fillAmount = finalPct;
            if (_xpMeterValueText) _xpMeterValueText.text = $"{finalXP} / {finalReq}";
            if (_txtLevelPreview)
            {
                _txtLevelPreview.text = finalLevel > startLevel 
                    ? $"Lv {startLevel} ➔ {finalLevel}"
                    : $"Lv {startLevel}";
            }

            int oldLevel = _currentUnit.Level;
            float oldHp = _currentUnit.CalculatedStats.MaxHp;
            float oldAtk = _currentUnit.CalculatedStats.Attack;
            float oldDef = _currentUnit.CalculatedStats.Defense;

            ProgressionLogic.AddXP(_currentUnit, totalXPGain);
            for (int i = 0; i < 4; i++)
            {
                if (_selectedCores[i] > 0) _saveManager.RemoveItem(CoreIDs[i], _selectedCores[i]);
            }
            for (int i = 0; i < _selectedDuplicatesCount; i++)
            {
                if (i < _availableDuplicates.Count) _saveManager.CurrentData.UnitInventory.Remove(_availableDuplicates[i]);
            }

            _saveManager.Save();
            
            int finalLvlAfterXP = _currentUnit.Level;
            float finalHp = _currentUnit.CalculatedStats.MaxHp;
            float finalAtk = _currentUnit.CalculatedStats.Attack;
            float finalDef = _currentUnit.CalculatedStats.Defense;

            if (finalLvlAfterXP > oldLevel)
            {
                ShowLevelUpSuccessBanner(oldLevel, finalLvlAfterXP, oldHp, finalHp, oldAtk, finalAtk, oldDef, finalDef);
            }

            yield return new WaitForSeconds(0.3f);

            _isAnimating = false;
            Refresh(_currentUnit);

            if (_btnAutoAdd) _btnAutoAdd.interactable = true;
            if (_btnAddAll) _btnAddAll.interactable = true;
            if (_btnDeselectAll) _btnDeselectAll.interactable = true;
            if (_btnConfirmLevelUp) _btnConfirmLevelUp.interactable = true;
        }

        private GameObject _statsPreviewContainer;
        private TextMeshProUGUI _txtHpPreview;
        private TextMeshProUGUI _txtAtkPreview;
        private TextMeshProUGUI _txtDefPreview;

        private void EnsureStatsPreviewUI()
        {
            if (_statsPreviewContainer != null) return;

            var rightPanel = _duplicatesScrollRect != null ? _duplicatesScrollRect.transform.parent : null;
            if (rightPanel == null) return;

            // Check if already created procedurally
            var existing = rightPanel.Find("LevelingStatsPreviewContainer");
            if (existing != null)
            {
                _statsPreviewContainer = existing.gameObject;
                _txtHpPreview = _statsPreviewContainer.transform.Find("HpRow/ValueText")?.GetComponent<TextMeshProUGUI>();
                _txtAtkPreview = _statsPreviewContainer.transform.Find("AtkRow/ValueText")?.GetComponent<TextMeshProUGUI>();
                _txtDefPreview = _statsPreviewContainer.transform.Find("DefRow/ValueText")?.GetComponent<TextMeshProUGUI>();
                return;
            }

            // Create container
            _statsPreviewContainer = new GameObject("LevelingStatsPreviewContainer", typeof(RectTransform));
            _statsPreviewContainer.transform.SetParent(rightPanel, false);

            var rect = _statsPreviewContainer.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.25f);
            rect.anchorMax = new Vector2(0.95f, 0.45f);
            rect.sizeDelta = Vector2.zero;

            // Add background glassmorphic style
            var bgImg = _statsPreviewContainer.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
            
            var outline = _statsPreviewContainer.AddComponent<Outline>();
            outline.effectColor = new Color(0.9f, 0.65f, 0.2f, 0.5f);
            outline.effectDistance = new Vector2(1, 1);

            // Vertical Layout Group
            var vlg = _statsPreviewContainer.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(15, 15, 10, 10);
            vlg.spacing = 8;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Create Rows
            _txtHpPreview = CreatePreviewRow("Max HP", "HpRow");
            _txtAtkPreview = CreatePreviewRow("Attack", "AtkRow");
            _txtDefPreview = CreatePreviewRow("Defense", "DefRow");

            // Ensure proper hierarchy placement (above BottomControls)
            var bottomControls = rightPanel.Find("BottomControls");
            if (bottomControls != null)
            {
                _statsPreviewContainer.transform.SetSiblingIndex(bottomControls.GetSiblingIndex());
            }
        }

        private TextMeshProUGUI CreatePreviewRow(string statName, string rowName)
        {
            var row = new GameObject(rowName, typeof(RectTransform));
            row.transform.SetParent(_statsPreviewContainer.transform, false);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var nameTxtObj = new GameObject("LabelText", typeof(RectTransform));
            nameTxtObj.transform.SetParent(row.transform, false);
            var nameTxt = nameTxtObj.AddComponent<TextMeshProUGUI>();
            nameTxt.text = statName;
            nameTxt.fontSize = 15;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            nameTxt.alignment = TextAlignmentOptions.Left;

            var valTxtObj = new GameObject("ValueText", typeof(RectTransform));
            valTxtObj.transform.SetParent(row.transform, false);
            var valTxt = valTxtObj.AddComponent<TextMeshProUGUI>();
            valTxt.text = "---";
            valTxt.fontSize = 15;
            valTxt.fontStyle = FontStyles.Bold;
            valTxt.alignment = TextAlignmentOptions.Right;

            return valTxt;
        }

        private void GetSimulatedLevelAndXP(int startLvl, int startXp, int totalXpGain, int maxLvl, out int newLevel, out int simXP, out int simReqXP)
        {
            newLevel = startLvl;
            simXP = startXp + totalXpGain;
            simReqXP = ProgressionLogic.GetRequiredXP(newLevel);

            while (newLevel < maxLvl)
            {
                int req = ProgressionLogic.GetRequiredXP(newLevel);
                if (simXP >= req)
                {
                    simXP -= req;
                    newLevel++;
                    simReqXP = ProgressionLogic.GetRequiredXP(newLevel);
                }
                else
                {
                    break;
                }
            }
        }

        private void CalculateSimulatedStats(UnitData unit, int simLevel, out float maxHp, out float attack, out float defense)
        {
            int originalLevel = unit.Level;
            unit.Level = simLevel;
            unit.RefreshStats(MaouSamaTD.Core.AppEntryPoint.LoadedScalingData);
            maxHp = unit.CalculatedStats.MaxHp;
            attack = unit.CalculatedStats.Attack;
            defense = unit.CalculatedStats.Defense;
            unit.Level = originalLevel;
            unit.RefreshStats(MaouSamaTD.Core.AppEntryPoint.LoadedScalingData);
        }

        private void ShowLevelUpSuccessBanner(int oldLvl, int newLvl, float hp1, float hp2, float atk1, float atk2, float def1, float def2)
        {
            var overlay = new GameObject("LevelUpSuccessOverlay", typeof(RectTransform));
            overlay.transform.SetParent(this.transform.parent, false);
            var rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var bgImg = overlay.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.02f, 0.04f, 0.85f);

            var dialogBox = new GameObject("BannerCard", typeof(RectTransform));
            dialogBox.transform.SetParent(overlay.transform, false);
            var dbRect = dialogBox.GetComponent<RectTransform>();
            dbRect.anchorMin = new Vector2(0.5f, 0.5f);
            dbRect.anchorMax = new Vector2(0.5f, 0.5f);
            dbRect.pivot = new Vector2(0.5f, 0.5f);
            dbRect.sizeDelta = new Vector2(500, 320);
            dbRect.anchoredPosition = new Vector2(0, -500); // Start below for enter animation!

            var dbImg = dialogBox.AddComponent<Image>();
            dbImg.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
            var dbOutline = dialogBox.AddComponent<Outline>();
            dbOutline.effectColor = new Color(0.9f, 0.65f, 0.2f, 0.8f);
            dbOutline.effectDistance = new Vector2(2, 2);

            // Title
            var title = new GameObject("Title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            title.transform.SetParent(dialogBox.transform, false);
            title.text = "LEVEL UP!";
            title.fontSize = 32;
            title.fontStyle = FontStyles.Bold | FontStyles.Italic;
            title.color = new Color(0.95f, 0.7f, 0.2f);
            title.alignment = TextAlignmentOptions.Center;
            var tRect = title.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.05f, 0.75f);
            tRect.anchorMax = new Vector2(0.95f, 0.95f);
            tRect.sizeDelta = Vector2.zero;

            // Subtitle level change
            var lvlText = new GameObject("LvlText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            lvlText.transform.SetParent(dialogBox.transform, false);
            lvlText.text = $"Lv {oldLvl} ➔ <color=#00FF00>Lv {newLvl}</color>";
            lvlText.fontSize = 22;
            lvlText.fontStyle = FontStyles.Bold;
            lvlText.alignment = TextAlignmentOptions.Center;
            lvlText.color = Color.white;
            var lRect = lvlText.GetComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.05f, 0.6f);
            lRect.anchorMax = new Vector2(0.95f, 0.72f);
            lRect.sizeDelta = Vector2.zero;

            // Stats grid container
            var statsGrid = new GameObject("StatsGrid", typeof(RectTransform));
            statsGrid.transform.SetParent(dialogBox.transform, false);
            var sgRect = statsGrid.GetComponent<RectTransform>();
            sgRect.anchorMin = new Vector2(0.1f, 0.2f);
            sgRect.anchorMax = new Vector2(0.9f, 0.55f);
            sgRect.sizeDelta = Vector2.zero;

            var vlg = statsGrid.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            CreateRow(statsGrid.transform, "HP", hp1, hp2);
            CreateRow(statsGrid.transform, "ATK", atk1, atk2);
            CreateRow(statsGrid.transform, "DEF", def1, def2);

            // Close button
            var btnClose = new GameObject("CloseBtn", typeof(RectTransform));
            btnClose.transform.SetParent(dialogBox.transform, false);
            var bcImg = btnClose.AddComponent<Image>();
            bcImg.color = new Color(0.2f, 0.65f, 0.3f, 1f);
            var bcBtn = btnClose.AddComponent<Button>();
            var bcTxt = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            bcTxt.transform.SetParent(btnClose.transform, false);
            bcTxt.text = "CONFIRM";
            bcTxt.fontSize = 16;
            bcTxt.fontStyle = FontStyles.Bold;
            bcTxt.alignment = TextAlignmentOptions.Center;
            bcTxt.color = Color.white;
            var bctRect = bcTxt.GetComponent<RectTransform>();
            bctRect.anchorMin = Vector2.zero;
            bctRect.anchorMax = Vector2.one;
            bctRect.sizeDelta = Vector2.zero;
            var bcRect = btnClose.GetComponent<RectTransform>();
            bcRect.anchorMin = new Vector2(0.35f, 0.05f);
            bcRect.anchorMax = new Vector2(0.65f, 0.16f);
            bcRect.sizeDelta = Vector2.zero;

            bcBtn.onClick.AddListener(() =>
            {
                // Slide out on confirm using DOTween
                dbRect.DOAnchorPosY(-800f, 0.3f).SetEase(Ease.InBack).OnComplete(() => Destroy(overlay));
            });

            // Slide in animation using DOTween!
            dbRect.DOAnchorPosY(0f, 0.5f).SetEase(Ease.OutBack);
        }

        private void CreateRow(Transform parent, string stat, float v1, float v2)
        {
            var row = new GameObject(stat + "Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            var lbl = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            lbl.transform.SetParent(row.transform, false);
            lbl.text = stat;
            lbl.fontSize = 16;
            lbl.fontStyle = FontStyles.Bold;
            lbl.color = new Color(0.7f, 0.7f, 0.7f);
            lbl.alignment = TextAlignmentOptions.Left;

            var val = new GameObject("Val", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            val.transform.SetParent(row.transform, false);
            val.text = $"{v1:F0} ➔ <color=#00FF00>{v2:F0} (+{(v2 - v1):F0})</color>";
            val.fontSize = 16;
            val.fontStyle = FontStyles.Bold;
            val.alignment = TextAlignmentOptions.Right;
        }
    }
}
