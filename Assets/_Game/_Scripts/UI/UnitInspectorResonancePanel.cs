using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MaouSamaTD.Units;
using MaouSamaTD.Data;
using MaouSamaTD.Managers;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Manages Tab 2 (_contentResonance) with three sub-tabs:
    ///   0 - Promote  (Star Rank-Up via Gold + Class Materials)
    ///   1 - Memories (Lore/Chamber unlock using duplicates)
    ///   2 - Resonance Nodes (Ascension stat boosts using duplicates)
    ///
    /// Promotion Material Map:
    ///   Melee/Tank  (Vanguard, Bastion)             Primary=mat_bandit_insignia  Secondary=mat_golem_core
    ///   Phys/Range  (Ranger, Rogue)                 Primary=mat_animal_fang      Secondary=mat_bandit_insignia
    ///   Magic/Supp  (Warlock,Sage,Support,Necro)    Primary=mat_shadow_essence   Secondary=mat_golem_core
    ///
    /// Node stat formula: +5% HP/ATK/DEF per unlocked node (max 6 = +30%).
    /// Memory/Lore: Chamber 0 free, Chambers 1-4 cost 1 duplicate each.
    /// </summary>
    public class UnitInspectorResonancePanel : MonoBehaviour
    {
        // ── Promotion gold costs per target star ─────────────────────────────
        private static readonly int[] PromoteGoldCosts   = { 0, 1000, 3000, 8000, 20000, 50000 };
        private static readonly int[] PromotePrimaryCounts = { 0, 5, 10, 18, 30, 50 };
        private static readonly int[] PromoteSecondaryCounts = { 0, 2, 5, 10, 15, 25 };

        [Header("Sub-Tab Buttons")]
        [SerializeField] private Button _btnTabPromote;
        [SerializeField] private Button _btnTabMemories;
        [SerializeField] private Button _btnTabNodes;

        [Header("Sub-Tab Roots")]
        [SerializeField] private GameObject _rootPromote;
        [SerializeField] private GameObject _rootMemories;
        [SerializeField] private GameObject _rootNodes;

        // ── Promote Tab ───────────────────────────────────────────────────────
        [Header("Promote Tab")]
        [SerializeField] private RectTransform _txtCurrentStars;
        [SerializeField] private RectTransform _txtNextStars;
        [SerializeField] private Sprite _starFullSprite;
        [SerializeField] private Sprite _starEmptySprite;
        [SerializeField] private TextMeshProUGUI _txtPromoteGoldCost;
        [SerializeField] private TextMeshProUGUI _txtPrimaryMatName;
        [SerializeField] private TextMeshProUGUI _txtPrimaryMatCount;  // "Owned / Required"
        [SerializeField] private TextMeshProUGUI _txtSecondaryMatName;
        [SerializeField] private TextMeshProUGUI _txtSecondaryMatCount;
        [SerializeField] private Button _btnPromote;
        [SerializeField] private TextMeshProUGUI _txtPromoteStatus;

        // ── Memories Tab ──────────────────────────────────────────────────────
        [Header("Memories Tab")]
        [SerializeField] private ScrollRect _memoriesScrollRect;
        [SerializeField] private GameObject _memoryEntryPrefab;

        // ── Resonance Nodes Tab ───────────────────────────────────────────────
        [Header("Resonance Nodes Tab")]
        [SerializeField] private ScrollRect _nodesScrollRect;
        [SerializeField] private GameObject _nodeEntryPrefab;
        [SerializeField] private TextMeshProUGUI _txtNodeSummary;

        // ── State ─────────────────────────────────────────────────────────────
        private UnitData _currentUnit;
        private SaveManager _saveManager;
        private EconomyManager _economyManager;
        private int _activeSubTab = 0;
        private bool _isChamberMode = false;
        private GameObject _chamberContainer;
        private GameObject _storyPopupModal;

        // ── Init ──────────────────────────────────────────────────────────────
        public void Initialize(SaveManager saveManager, EconomyManager economyManager = null)
        {
            _saveManager = saveManager;
            _economyManager = economyManager;

            if (_btnTabPromote)   _btnTabPromote.onClick.AddListener(()  => SwitchSubTab(0));
            if (_btnTabMemories)  _btnTabMemories.onClick.AddListener(() => SwitchSubTab(1));
            if (_btnTabNodes)     _btnTabNodes.onClick.AddListener(()    => SwitchSubTab(2));
            if (_btnPromote)      _btnPromote.onClick.AddListener(OnPromoteClicked);
        }

        private void SwitchSubTab(int idx)
        {
            _activeSubTab = idx;
            if (_rootPromote)   _rootPromote.SetActive(idx == 0);
            if (_rootMemories)  _rootMemories.SetActive(idx == 1);
            if (_rootNodes)     _rootNodes.SetActive(idx == 2);
            RefreshSubTab();
        }

        // ── Refresh ───────────────────────────────────────────────────────────
        public void Refresh(UnitData u)
        {
            _currentUnit = u;
            if (u == null) return;
            SwitchSubTab(_activeSubTab); // refresh active tab
        }

        public void OpenAsChamber(UnitData u)
        {
            _currentUnit = u;
            _isChamberMode = true;
            var subTabs = transform.Find("SubTabs");
            if (subTabs != null) subTabs.gameObject.SetActive(false);
            var titleTxt = transform.Find("Header/Title")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (titleTxt != null) titleTxt.text = "MEMORIAL CHAMBERS";
            SwitchSubTab(1);
        }

        public void OpenAsResonance(UnitData u)
        {
            _currentUnit = u;
            _isChamberMode = false;
            var subTabs = transform.Find("SubTabs");
            if (subTabs != null) subTabs.gameObject.SetActive(true);
            if (_btnTabMemories) _btnTabMemories.gameObject.SetActive(false);
            var titleTxt = transform.Find("Header/Title")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (titleTxt != null) titleTxt.text = "VASSAL PROMOTION";
            if (_activeSubTab == 1) _activeSubTab = 0;
            SwitchSubTab(_activeSubTab);
        }

        private void RefreshSubTab()
        {
            switch (_activeSubTab)
            {
                case 0: RefreshPromote();   break;
                case 1: RefreshMemories();  break;
                case 2: RefreshNodes();     break;
            }
        }

        // ── Promote ───────────────────────────────────────────────────────────
        private void RefreshPromote()
        {
            if (_currentUnit == null || _saveManager == null) return;

            var entry = GetMainEntry();
            int currentStars = entry?.StarRating ?? _currentUnit.StarRating;
            int nextStars    = currentStars + 1;

            if (_txtCurrentStars) UpdateStarContainer(_txtCurrentStars, currentStars);
            if (_txtNextStars)    UpdateStarContainer(_txtNextStars, nextStars <= 6 ? nextStars : 6);

            var arrow = _txtCurrentStars != null ? _txtCurrentStars.parent.Find("ArrowText")?.gameObject : null;
            if (arrow != null) arrow.SetActive(nextStars <= 6);
            if (_txtNextStars) _txtNextStars.gameObject.SetActive(nextStars <= 6);

            if (nextStars > 6)
            {
                if (_btnPromote)      _btnPromote.interactable = false;
                if (_txtPromoteStatus) _txtPromoteStatus.text = "Already at max rank!";
                return;
            }

            int starIdx      = nextStars - 1; // index into arrays (1-based target → 0-based idx)
            int goldCost     = PromoteGoldCosts[starIdx];

            var scalingData  = MaouSamaTD.Core.AppEntryPoint.LoadedScalingData;
            var reqMats      = scalingData != null ? scalingData.GetRequiredMaterials(_currentUnit.Class) : System.Array.Empty<PromotionMaterialRequirement>();

            int ownedGold    = _economyManager != null ? _economyManager.Gold : (_saveManager.CurrentData?.Gold ?? 0);

            if (_txtPromoteGoldCost) _txtPromoteGoldCost.text = $"{ownedGold:N0} / {goldCost:N0} Gold";

            List<string> leftNames = new List<string>();
            List<string> leftCounts = new List<string>();
            List<string> rightNames = new List<string>();
            List<string> rightCounts = new List<string>();

            bool allMatsMet = true;

            for (int i = 0; i < reqMats.Length; i++)
            {
                var req = reqMats[i];
                int requiredAmount = req.BaseAmount * nextStars;
                int ownedAmount = _saveManager.GetItemCount(req.ItemID);
                if (ownedAmount < requiredAmount)
                {
                    allMatsMet = false;
                }

                string nameStr = FriendlyMaterialName(req.ItemID);
                string countStr = $"{ownedAmount} / {requiredAmount}";

                if (i % 2 == 0)
                {
                    leftNames.Add(nameStr);
                    leftCounts.Add(countStr);
                }
                else
                {
                    rightNames.Add(nameStr);
                    rightCounts.Add(countStr);
                }
            }

            if (_txtPrimaryMatName)     _txtPrimaryMatName.text     = string.Join("\n", leftNames);
            if (_txtPrimaryMatCount)    _txtPrimaryMatCount.text    = string.Join("\n", leftCounts);
            if (_txtSecondaryMatName)   _txtSecondaryMatName.text   = string.Join("\n", rightNames);
            if (_txtSecondaryMatCount)  _txtSecondaryMatCount.text  = string.Join("\n", rightCounts);

            bool canPromote = ownedGold >= goldCost && allMatsMet;

            if (_btnPromote)      _btnPromote.interactable = canPromote;
            if (_txtPromoteStatus) _txtPromoteStatus.text  = canPromote ? "Ready to Promote!" : "Not enough resources.";
        }

        private void UpdateStarContainer(RectTransform container, int count)
        {
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject($"Star_{i}", typeof(RectTransform));
                go.transform.SetParent(container, false);
                var img = go.AddComponent<Image>();
                img.sprite = (i < count) ? _starFullSprite : _starEmptySprite;
                var rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(36, 36);
            }
        }

        private void OnPromoteClicked()
        {
            if (_currentUnit == null || _saveManager == null) return;

            var entry = GetMainEntry();
            int currentStars = entry?.StarRating ?? _currentUnit.StarRating;
            int nextStars    = currentStars + 1;
            if (nextStars > 6) return;

            int starIdx       = nextStars - 1;
            int goldCost      = PromoteGoldCosts[starIdx];

            var scalingData   = MaouSamaTD.Core.AppEntryPoint.LoadedScalingData;
            var reqMats       = scalingData != null ? scalingData.GetRequiredMaterials(_currentUnit.Class) : System.Array.Empty<PromotionMaterialRequirement>();

            // First verify we have all materials
            bool hasAll = true;
            foreach (var req in reqMats)
            {
                int requiredAmount = req.BaseAmount * nextStars;
                if (_saveManager.GetItemCount(req.ItemID) < requiredAmount)
                {
                    hasAll = false;
                    break;
                }
            }

            if (!hasAll)
            {
                Debug.LogWarning("[ResonancePanel] Promote failed: insufficient materials.");
                return;
            }

            // Deduct gold
            if (_economyManager != null)
            {
                if (!_economyManager.TrySpendGold(goldCost)) return;
            }
            else
            {
                if (!_saveManager.SpendGold(goldCost)) return;
            }

            // Deduct materials
            bool deductSuccess = true;
            List<(string itemId, int amount)> deducted = new List<(string, int)>();
            foreach (var req in reqMats)
            {
                int requiredAmount = req.BaseAmount * nextStars;
                if (!_saveManager.RemoveItem(req.ItemID, requiredAmount))
                {
                    deductSuccess = false;
                    break;
                }
                deducted.Add((req.ItemID, requiredAmount));
            }

            if (!deductSuccess)
            {
                // Refund gold
                if (_economyManager != null) _economyManager.AddGold(goldCost);
                else _saveManager.AddGold(goldCost);

                // Refund already deducted items
                foreach (var item in deducted)
                {
                    _saveManager.AddItem(item.itemId, item.amount);
                }

                Debug.LogWarning("[ResonancePanel] Promote failed during material deduction.");
                return;
            }

            // Apply promotion
            _currentUnit.StarRating = nextStars;
            _currentUnit.Level      = 1;
            _currentUnit.Experience = 0;
            if (entry != null)
            {
                entry.StarRating  = nextStars;
                entry.Level       = 1;
                entry.Experience  = 0;
            }

            // Recalculate stats with new star rating
            _currentUnit.RefreshStats(MaouSamaTD.Core.AppEntryPoint.LoadedScalingData);
            _saveManager.Save();

            Debug.Log($"[ResonancePanel] {_currentUnit.UnitName} promoted to ★{nextStars}!");
            RefreshPromote();
        }

        // ── Memories ─────────────────────────────────────────────────────────
        // ── Memories & Chambers ──────────────────────────────────────────────
        private void RefreshMemories()
        {
            if (_currentUnit == null || _saveManager == null) return;

            if (_isChamberMode)
            {
                if (_memoriesScrollRect != null) _memoriesScrollRect.gameObject.SetActive(false);
                DrawChamberInterface();
            }
            else
            {
                if (_chamberContainer != null) _chamberContainer.SetActive(false);
                if (_memoriesScrollRect != null)
                {
                    _memoriesScrollRect.gameObject.SetActive(true);
                    if (_memoriesScrollRect.content != null)
                    {
                        foreach (Transform c in _memoriesScrollRect.content) Destroy(c.gameObject);

                        var entry = GetMainEntry();
                        var unlocked = entry?.UnlockedLores ?? new List<int> { 0 };

                        int memCount = _currentUnit.LoreEntries != null ? _currentUnit.LoreEntries.Count : 0;
                        int totalChambers = Mathf.Max(memCount, 5);

                        for (int i = 0; i < totalChambers; i++)
                        {
                            bool isUnlocked = unlocked.Contains(i);
                            string title    = memCount > i ? _currentUnit.LoreEntries[i].Title : $"Chamber {i}";
                            string body     = memCount > i && isUnlocked ? _currentUnit.LoreEntries[i].Content : "";
                            bool canUnlock  = !isUnlocked && HasDuplicate();

                            if (_memoryEntryPrefab == null) continue;
                            var go   = Instantiate(_memoryEntryPrefab, _memoriesScrollRect.content);
                            var item = go.GetComponent<MemoryEntryUI>();
                            if (item != null)
                            {
                                int capturedIdx = i;
                                item.Setup(title, body, isUnlocked, canUnlock, () => OnUnlockMemory(capturedIdx, entry, item));
                            }
                        }
                    }
                }
            }
        }

        private void OnUnlockMemory(int idx, UnitInventoryEntry entry, MemoryEntryUI item)
        {
            if (!ConsumeDuplicate()) return;
            if (entry != null && !entry.UnlockedLores.Contains(idx))
                entry.UnlockedLores.Add(idx);
            _saveManager.Save();
            RefreshMemories();
        }

        private TMP_FontAsset GetFont()
        {
            if (_txtPromoteStatus != null) return _txtPromoteStatus.font;
            if (_txtNodeSummary != null) return _txtNodeSummary.font;
            if (_txtPrimaryMatName != null) return _txtPrimaryMatName.font;
            if (_txtSecondaryMatName != null) return _txtSecondaryMatName.font;
            return null;
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<TextMeshProUGUI>();
            txt.font = GetFont();
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.text = content;
            txt.enableWordWrapping = true;
            return txt;
        }

        private void DrawChamberInterface()
        {
            if (_rootMemories == null || _currentUnit == null || _saveManager == null) return;

            if (_chamberContainer == null)
            {
                _chamberContainer = new GameObject("ChamberContainer", typeof(RectTransform));
                _chamberContainer.transform.SetParent(_rootMemories.transform, false);
                var containerRect = _chamberContainer.GetComponent<RectTransform>();
                containerRect.anchorMin = Vector2.zero;
                containerRect.anchorMax = Vector2.one;
                containerRect.sizeDelta = Vector2.zero;
            }

            _chamberContainer.SetActive(true);

            // Clear old children
            for (int i = _chamberContainer.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(_chamberContainer.transform.GetChild(i).gameObject);
            }

            var entry = GetMainEntry();
            int amity = entry?.Amity ?? 0;
            int vigor = entry?.Vigor ?? 100;

            // 55/45 split Columns
            var leftGo = new GameObject("LeftColumn", typeof(RectTransform));
            leftGo.transform.SetParent(_chamberContainer.transform, false);
            var leftRect = leftGo.GetComponent<RectTransform>();
            leftRect.anchorMin = Vector2.zero;
            leftRect.anchorMax = new Vector2(0.55f, 1f);
            leftRect.sizeDelta = Vector2.zero;

            var rightGo = new GameObject("RightColumn", typeof(RectTransform));
            rightGo.transform.SetParent(_chamberContainer.transform, false);
            var rightRect = rightGo.GetComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.55f, 0f);
            rightRect.anchorMax = Vector2.one;
            rightRect.sizeDelta = Vector2.zero;

            // ── Left Column Layout Group ──────────────────────────────────────
            var leftLayout = leftGo.AddComponent<VerticalLayoutGroup>();
            leftLayout.padding = new RectOffset(30, 30, 30, 30);
            leftLayout.spacing = 20;
            leftLayout.childAlignment = TextAnchor.UpperLeft;
            leftLayout.childControlHeight = false;
            leftLayout.childControlWidth = false;
            leftLayout.childForceExpandHeight = false;
            leftLayout.childForceExpandWidth = false;

            // 1. Title
            var titleTxt = CreateText(leftGo.transform, "Title", "CHAMBER PROGRESSION", 22, new Color(0.9f, 0.7f, 0.2f, 1f));
            titleTxt.fontStyle = FontStyles.Bold;

            // 2. Amity Section
            var amitySection = new GameObject("AmitySection", typeof(RectTransform));
            amitySection.transform.SetParent(leftGo.transform, false);
            var amitySecRect = amitySection.GetComponent<RectTransform>();
            amitySecRect.sizeDelta = new Vector2(480, 100);

            bool isOppositeGender = _saveManager.CurrentData.Gender != _currentUnit.Gender;
            string relationType = isOppositeGender ? "Opposite-Gender Romance" : "Same-Gender Platonic Pact";
            string relationName = (amity, isOppositeGender) switch
            {
                _ when amity >= 80 => isOppositeGender ? "💖 Soulmates (Romance)" : "🛡️ Sworn Brothers (Pact)",
                _ when amity >= 60 => isOppositeGender ? "🥰 Deep Affection" : "🤝 Inseparable Allies",
                _ when amity >= 40 => isOppositeGender ? "😊 Mutual Attraction" : "🤜 Camrades-in-Arms",
                _ when amity >= 20 => isOppositeGender ? "🌱 Budding Romance" : "⚔️ Sworn Partners",
                _ => "◌ Acquaintance"
            };

            var amityLbl = CreateText(amitySection.transform, "AmityLabel", $"Amity: {amity}% ({relationName})", 16, Color.white);
            amityLbl.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -10);

            // Progress bar background
            var barBg = new GameObject("AmityBarBg", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(amitySection.transform, false);
            var barBgRect = barBg.GetComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0f, 0.5f);
            barBgRect.anchorMax = new Vector2(0f, 0.5f);
            barBgRect.anchoredPosition = new Vector2(190, -10);
            barBgRect.sizeDelta = new Vector2(250, 20);
            barBg.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);

            var barFill = new GameObject("AmityBarFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            var barFillRect = barFill.GetComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = new Vector2((float)amity / 100f, 1f);
            barFillRect.sizeDelta = Vector2.zero;
            barFill.GetComponent<Image>().color = new Color(0.9f, 0.7f, 0.2f, 1f);

            // Offer Gift Button
            var btnGiftGo = new GameObject("BtnOfferGift", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGiftGo.transform.SetParent(amitySection.transform, false);
            var btnGiftRect = btnGiftGo.GetComponent<RectTransform>();
            btnGiftRect.anchoredPosition = new Vector2(0, -60);
            btnGiftRect.sizeDelta = new Vector2(440, 36);

            btnGiftGo.GetComponent<Image>().color = new Color(0.9f, 0.65f, 0.2f, 1f);
            var btnGift = btnGiftGo.GetComponent<Button>();

            var btnGiftTxt = CreateText(btnGiftGo.transform, "Text", $"Offer Gift (500 Gold, +10% Amity)", 14, Color.white, TextAlignmentOptions.Center);
            var btnGiftTxtRect = btnGiftTxt.GetComponent<RectTransform>();
            btnGiftTxtRect.anchorMin = Vector2.zero;
            btnGiftTxtRect.anchorMax = Vector2.one;
            btnGiftTxtRect.sizeDelta = Vector2.zero;

            int ownedGold = _economyManager != null ? _economyManager.Gold : (_saveManager.CurrentData?.Gold ?? 0);
            bool canAffordGift = ownedGold >= 500 && amity < 100;
            btnGift.interactable = canAffordGift;

            btnGift.onClick.AddListener(() =>
            {
                int cost = 500;
                bool success = _economyManager != null ? _economyManager.TrySpendGold(cost) : _saveManager.SpendGold(cost);
                if (success)
                {
                    if (entry != null)
                    {
                        entry.Amity = Mathf.Min(100, entry.Amity + 10);
                        _currentUnit.Amity = entry.Amity;
                        _saveManager.Save();
                        RefreshMemories();
                    }
                }
            });

            // 3. Vigor Section
            var vigorSection = new GameObject("VigorSection", typeof(RectTransform));
            vigorSection.transform.SetParent(leftGo.transform, false);
            var vigorSecRect = vigorSection.GetComponent<RectTransform>();
            vigorSecRect.sizeDelta = new Vector2(480, 110);

            var vigorLbl = CreateText(vigorSection.transform, "VigorLabel", $"Vigor: {vigor} / 100", 16, Color.white);
            vigorLbl.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -10);

            if (vigor < 100)
            {
                var debuffTxt = CreateText(vigorSection.transform, "DebuffText", "⚠️ VIGOR DEBUFF ACTIVE (-20% HP/ATK/DEF)", 12, new Color(1f, 0.3f, 0.3f, 1f));
                debuffTxt.fontStyle = FontStyles.Bold;
                debuffTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -32);
            }
            else
            {
                var healthyTxt = CreateText(vigorSection.transform, "HealthyText", "❇️ Vassal in Perfect Condition", 12, new Color(0.3f, 1f, 0.3f, 1f));
                healthyTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -32);
            }

            // Restore Vigor Button
            var btnVigorGo = new GameObject("BtnRestoreVigor", typeof(RectTransform), typeof(Image), typeof(Button));
            btnVigorGo.transform.SetParent(vigorSection.transform, false);
            var btnVigorRect = btnVigorGo.GetComponent<RectTransform>();
            btnVigorRect.anchoredPosition = new Vector2(0, -75);
            btnVigorRect.sizeDelta = new Vector2(440, 36);

            btnVigorGo.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.8f, 1f);
            var btnVigor = btnVigorGo.GetComponent<Button>();

            var btnVigorTxt = CreateText(btnVigorGo.transform, "Text", $"Restore Vigor (1,000 Gold)", 14, Color.white, TextAlignmentOptions.Center);
            var btnVigorTxtRect = btnVigorTxt.GetComponent<RectTransform>();
            btnVigorTxtRect.anchorMin = Vector2.zero;
            btnVigorTxtRect.anchorMax = Vector2.one;
            btnVigorTxtRect.sizeDelta = Vector2.zero;

            bool canRestoreVigor = ownedGold >= 1000 && vigor < 100;
            btnVigor.interactable = canRestoreVigor;

            btnVigor.onClick.AddListener(() =>
            {
                int cost = 1000;
                bool success = _economyManager != null ? _economyManager.TrySpendGold(cost) : _saveManager.SpendGold(cost);
                if (success)
                {
                    if (entry != null)
                    {
                        entry.Vigor = 100;
                        _currentUnit.Vigor = 100;
                        _currentUnit.RefreshStats(MaouSamaTD.Core.AppEntryPoint.LoadedScalingData);
                        _saveManager.Save();
                        RefreshMemories();
                    }
                }
            });

            // 4. Chamber Chronicle Title
            var chronTitle = CreateText(leftGo.transform, "ChronicleTitle", "CHAMBER CHRONICLES", 18, new Color(0.9f, 0.7f, 0.2f, 1f));
            chronTitle.fontStyle = FontStyles.Bold;

            // 5. Milestones Stories Rows
            int memCount = _currentUnit.LoreEntries != null ? _currentUnit.LoreEntries.Count : 0;
            for (int i = 0; i < 5; i++)
            {
                int milestoneIdx = i;
                int reqAmity = i * 20;
                bool isUnlocked = amity >= reqAmity;
                string roman = i switch { 0 => "I", 1 => "II", 2 => "III", 3 => "IV", 4 => "V", _ => (i + 1).ToString() };
                string chamberTitle = memCount > i ? _currentUnit.LoreEntries[i].Title : $"Chamber {roman}";
                string contentText = memCount > i ? _currentUnit.LoreEntries[i].Content : "No story content added yet.";

                var rowGo = new GameObject($"ChamberRow_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                rowGo.transform.SetParent(leftGo.transform, false);
                var rowRect = rowGo.GetComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(440, 50);

                var rowImg = rowGo.GetComponent<Image>();
                rowImg.color = isUnlocked ? new Color(0.12f, 0.12f, 0.18f, 0.8f) : new Color(0.08f, 0.08f, 0.1f, 0.4f);

                var rowOutline = rowGo.AddComponent<Outline>();
                rowOutline.effectColor = isUnlocked ? new Color(0.9f, 0.7f, 0.2f, 0.3f) : new Color(0.3f, 0.3f, 0.3f, 0.1f);
                rowOutline.effectDistance = new Vector2(1, 1);

                string titleDisplay = isUnlocked ? $"CHAMBER {roman} - {chamberTitle.ToUpper()}" : $"CHAMBER {roman} (LOCKED)";
                var rowTxt = CreateText(rowGo.transform, "TitleText", titleDisplay, 14, isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f));
                var rowTxtRect = rowTxt.GetComponent<RectTransform>();
                rowTxtRect.anchorMin = new Vector2(0f, 0.5f);
                rowTxtRect.anchorMax = new Vector2(0.8f, 0.5f);
                rowTxtRect.anchoredPosition = new Vector2(20, 0);

                var statusLabelText = isUnlocked ? "[READ]" : $"[{reqAmity}% AMITY]";
                var statusTxt = CreateText(rowGo.transform, "StatusText", statusLabelText, 12, isUnlocked ? new Color(0.3f, 1f, 0.3f, 1f) : new Color(0.7f, 0.3f, 0.3f, 1f), TextAlignmentOptions.Right);
                var statusTxtRect = statusTxt.GetComponent<RectTransform>();
                statusTxtRect.anchorMin = new Vector2(0.8f, 0.5f);
                statusTxtRect.anchorMax = new Vector2(1f, 0.5f);
                statusTxtRect.anchoredPosition = new Vector2(-20, 0);

                var rowBtn = rowGo.GetComponent<Button>();
                rowBtn.interactable = isUnlocked;

                if (isUnlocked)
                {
                    rowBtn.onClick.AddListener(() =>
                    {
                        ShowStoryPopup($"CHAMBER {roman}: {chamberTitle}", contentText);
                    });
                }
            }

            // ── Right Column Content (Vassal Art & Narrative Quote) ──────────
            var rightLayout = rightGo.AddComponent<VerticalLayoutGroup>();
            rightLayout.padding = new RectOffset(40, 40, 40, 40);
            rightLayout.spacing = 16;
            rightLayout.childAlignment = TextAnchor.UpperCenter;
            rightLayout.childControlHeight = false;
            rightLayout.childControlWidth = false;
            rightLayout.childForceExpandHeight = false;
            rightLayout.childForceExpandWidth = false;

            // 1. Vassal Portrait Image
            var portGo = new GameObject("WaistUpPortrait", typeof(RectTransform), typeof(Image));
            portGo.transform.SetParent(rightGo.transform, false);
            var portRect = portGo.GetComponent<RectTransform>();
            portRect.sizeDelta = new Vector2(360, 420);

            var portImg = portGo.GetComponent<Image>();
            var spriteVal = _currentUnit.GetSprite(UnitData.UnitImageType.WaistUp);
            if (spriteVal == null) spriteVal = _currentUnit.GetSprite(UnitData.UnitImageType.FullSprite);
            if (spriteVal == null) spriteVal = _currentUnit.GetSprite(UnitData.UnitImageType.Avatar);
            
            portImg.sprite = spriteVal;
            portImg.preserveAspect = true;

            // 2. Identity info
            var nameLbl = CreateText(rightGo.transform, "NameText", _currentUnit.UnitName.ToUpper(), 28, Color.white, TextAlignmentOptions.Center);
            nameLbl.fontStyle = FontStyles.Bold;

            var titleLbl = CreateText(rightGo.transform, "TitleText", _currentUnit.UnitTitle.ToUpper(), 16, new Color(0.9f, 0.7f, 0.2f, 1f), TextAlignmentOptions.Center);
            titleLbl.fontStyle = FontStyles.Italic;

            // Gender / Relation Badge
            string genderStr = $"Gender: {_currentUnit.Gender}  |  Player: {_saveManager.CurrentData.Gender}";
            var genderLbl = CreateText(rightGo.transform, "GenderText", genderStr, 12, new Color(0.7f, 0.7f, 0.7f, 1f), TextAlignmentOptions.Center);

            // 3. Dialogue Bubble Area
            var dialogueBg = new GameObject("DialogueBubble", typeof(RectTransform), typeof(Image));
            dialogueBg.transform.SetParent(rightGo.transform, false);
            var dialRect = dialogueBg.GetComponent<RectTransform>();
            dialRect.sizeDelta = new Vector2(360, 120);

            var dialImg = dialogueBg.GetComponent<Image>();
            dialImg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
            var dialOutline = dialogueBg.AddComponent<Outline>();
            dialOutline.effectColor = new Color(0.9f, 0.7f, 0.2f, 0.3f);
            dialOutline.effectDistance = new Vector2(1, 1);

            string quote = "";
            if (isOppositeGender)
            {
                quote = amity switch
                {
                    >= 80 => $"\"My heart is forever yours, my Sovereign. Through space, time, and gravity, I shall always stand by your side...\"",
                    >= 60 => $"\"Every battle is easier when I know I am protecting you... Your safety is my highest priority.\"",
                    >= 40 => $"\"Your presence warms me, my Lord... I am truly glad to have met you.\"",
                    >= 20 => $"\"I... I made this charm for you. It's not much, but I hope it keeps you safe in battle.\"",
                    _ => $"\"I am at your command, my Sovereign. Let us fight together.\""
                };
            }
            else
            {
                quote = amity switch
                {
                    >= 80 => $"\"We are one soul in two bodies, bound by a pact that defies time itself. Together, we are unstoppable.\"",
                    >= 60 => $"\"Our bond of camaraderie is unbreakable. I would gladly take a mortal blow for you in battle.\"",
                    >= 40 => $"\"We are brothers-in-arms. A solid shield for our cause, and a sword for our future!\"",
                    >= 20 => $"\"The pact is sealed, comrade. Let's make sure they remember the name of Maou-Sama!\"",
                    _ => $"\"The pact is sealed. I shall serve you faithfully in our grand campaign.\""
                };
            }

            var bubbleTxt = CreateText(dialogueBg.transform, "BubbleText", quote, 13, new Color(0.85f, 0.85f, 0.85f, 1f));
            bubbleTxt.fontStyle = FontStyles.Italic;
            var bubbleRect = bubbleTxt.GetComponent<RectTransform>();
            bubbleRect.anchorMin = Vector2.zero;
            bubbleRect.anchorMax = Vector2.one;
            bubbleRect.offsetMin = new Vector2(16, 16);
            bubbleRect.offsetMax = new Vector2(-16, -16);
        }

        private void ShowStoryPopup(string title, string content)
        {
            if (_storyPopupModal != null) Destroy(_storyPopupModal);

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            _storyPopupModal = new GameObject("StoryPopupModal", typeof(RectTransform), typeof(Image));
            _storyPopupModal.transform.SetParent(canvas.transform, false);
            var modalRect = _storyPopupModal.GetComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.sizeDelta = Vector2.zero;

            _storyPopupModal.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            var bgBtn = _storyPopupModal.AddComponent<Button>();
            bgBtn.onClick.AddListener(() => Destroy(_storyPopupModal));

            var box = new GameObject("PopupBox", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(_storyPopupModal.transform, false);
            var boxRect = box.GetComponent<RectTransform>();
            boxRect.sizeDelta = new Vector2(600, 700);
            box.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.96f);

            var outline = box.AddComponent<Outline>();
            outline.effectColor = new Color(0.9f, 0.7f, 0.2f, 1f);
            outline.effectDistance = new Vector2(2, 2);

            var titleTxt = CreateText(box.transform, "PopupTitle", title.ToUpper(), 24, new Color(0.9f, 0.7f, 0.2f, 1f), TextAlignmentOptions.Center);
            var titleRect = titleTxt.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -60);
            titleRect.sizeDelta = new Vector2(-80, 50);

            var scrollGo = new GameObject("StoryContentScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(box.transform, false);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(40, 120);
            scrollRect.offsetMax = new Vector2(-40, -120);

            var scroller = scrollGo.GetComponent<ScrollRect>();
            scroller.horizontal = false;
            scroller.vertical = true;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image));
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewport.GetComponent<Image>().color = Color.clear;
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
            contentGo.transform.SetParent(viewport.transform, false);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0, 500);

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(10, 10, 10, 10);

            scroller.viewport = viewportRect;
            scroller.content = contentRect;

            var descTxt = CreateText(contentRect, "PopupContent", content, 18, new Color(0.85f, 0.85f, 0.85f, 1f));
            descTxt.fontStyle = FontStyles.Normal;

            var btnGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(box.transform, false);
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0f);
            btnRect.anchorMax = new Vector2(0.5f, 0f);
            btnRect.anchoredPosition = new Vector2(0, 50);
            btnRect.sizeDelta = new Vector2(200, 45);

            btnGo.GetComponent<Image>().color = new Color(0.9f, 0.65f, 0.2f, 1f);
            var btn = btnGo.GetComponent<Button>();

            var btnTxt = CreateText(btnGo.transform, "Text", "CLOSE", 18, Color.white, TextAlignmentOptions.Center);
            var btnTxtRect = btnTxt.GetComponent<RectTransform>();
            btnTxtRect.anchorMin = Vector2.zero;
            btnTxtRect.anchorMax = Vector2.one;
            btnTxtRect.sizeDelta = Vector2.zero;

            btn.onClick.AddListener(() => Destroy(_storyPopupModal));
        }

        // ── Resonance Nodes (Honkai Star Rail Constellation Style) ───────────
        [Header("Resonance HSR Layout")]
        [SerializeField] private GameObject _hsrLayoutRoot;
        [SerializeField] private RectTransform _leftNodeContainer;
        [SerializeField] private RectTransform _rightDetailPanel;
        [SerializeField] private TextMeshProUGUI _rightTierText;
        [SerializeField] private TextMeshProUGUI _rightNameText;
        [SerializeField] private TextMeshProUGUI _rightStatusText;
        [SerializeField] private TextMeshProUGUI _rightDescText;
        [SerializeField] private Image _rightDupePreviewIcon;
        [SerializeField] private TextMeshProUGUI _rightCostText;
        [SerializeField] private Button _rightUnlockButton;
        [SerializeField] private Image _rightUnlockButtonImage;
        [SerializeField] private TextMeshProUGUI _rightUnlockButtonText;

        private int _selectedNodeIndex = 0;
        private Sprite _circleSprite;

        private bool IsHsrLayoutAssignedInInspector => _hsrLayoutRoot != null && !_hsrLayoutRoot.name.StartsWith("HSR_Nodes_Layout_Container_Procedural");

        private static readonly Vector2[] NodePositions = new Vector2[]
        {
            new Vector2(120, 100),
            new Vector2(300, 180),
            new Vector2(180, 320),
            new Vector2(380, 430),
            new Vector2(230, 560),
            new Vector2(420, 670)
        };

        private Sprite GetOrCreateCircleSprite()
        {
            if (_circleSprite == null)
            {
                int size = 128;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float radius = size / 2f;
                Vector2 center = new Vector2(radius, radius);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist < radius - 1)
                        {
                            float alpha = Mathf.Clamp01(radius - dist);
                            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                        }
                        else
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                    }
                }
                tex.Apply();
                _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            }
            return _circleSprite;
        }

        private void CreateUILine(RectTransform parent, Vector2 pA, Vector2 pB, Color color, float thickness)
        {
            var lineObj = new GameObject("ConstellationLine", typeof(RectTransform), typeof(Image));
            lineObj.transform.SetParent(parent, false);
            var img = lineObj.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            var rect = lineObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            
            Vector2 dir = pB - pA;
            float distance = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            rect.sizeDelta = new Vector2(distance, thickness);
            rect.anchoredPosition = pA + dir * 0.5f;
            rect.localRotation = Quaternion.Euler(0, 0, angle);
        }

        private int GetDuplicateCount()
        {
            if (_saveManager?.CurrentData == null || _currentUnit == null) return 0;
            string id = _currentUnit.name;
            return _saveManager.CurrentData.UnitInventory.FindAll(e => e.UnitID == id && e.IsDuplicate).Count;
        }

        private void RefreshNodes()
        {
            // Hide the old scroll rect and summary if they exist
            if (_nodesScrollRect != null) _nodesScrollRect.gameObject.SetActive(false);
            if (_txtNodeSummary != null) _txtNodeSummary.gameObject.SetActive(false);

            if (_currentUnit == null || _saveManager == null) return;

            if (IsHsrLayoutAssignedInInspector)
            {
                if (_hsrLayoutRoot != null)
                {
                    _hsrLayoutRoot.SetActive(true);
                }

                // Clear dynamically spawned lines/buttons in left node container
                if (_leftNodeContainer != null)
                {
                    for (int i = _leftNodeContainer.childCount - 1; i >= 0; i--)
                    {
                        Destroy(_leftNodeContainer.GetChild(i).gameObject);
                    }
                }
            }
            else
            {
                // Recreate dynamic visual root for HSR layout fallback
                if (_hsrLayoutRoot != null)
                {
                    Destroy(_hsrLayoutRoot);
                }

                _hsrLayoutRoot = new GameObject("HSR_Nodes_Layout_Container_Procedural", typeof(RectTransform));
                _hsrLayoutRoot.transform.SetParent(_rootNodes.transform, false);
                var layoutRect = _hsrLayoutRoot.GetComponent<RectTransform>();
                layoutRect.anchorMin = Vector2.zero;
                layoutRect.anchorMax = Vector2.one;
                layoutRect.sizeDelta = Vector2.zero;

                // Create Left Container for Constellation
                var leftGo = new GameObject("LeftContainer", typeof(RectTransform));
                leftGo.transform.SetParent(_hsrLayoutRoot.transform, false);
                _leftNodeContainer = leftGo.GetComponent<RectTransform>();
                _leftNodeContainer.anchorMin = Vector2.zero;
                _leftNodeContainer.anchorMax = new Vector2(0.55f, 1f);
                _leftNodeContainer.sizeDelta = Vector2.zero;
            }

            var entry = GetMainEntry();
            var unlocked = entry?.UnlockedNodes ?? new List<int>();

            // Draw Constellation Lines
            for (int i = 0; i < 5; i++)
            {
                Vector2 pA = NodePositions[i];
                Vector2 pB = NodePositions[i + 1];
                bool isGlow = unlocked.Contains(i) && unlocked.Contains(i + 1);
                Color lineColor = isGlow ? new Color(0.9f, 0.65f, 0.2f, 0.8f) : new Color(1f, 1f, 1f, 0.15f);
                float thickness = isGlow ? 4f : 2f;
                CreateUILine(_leftNodeContainer, pA, pB, lineColor, thickness);
            }

            // Spawn 6 Node Buttons
            var nodes = _currentUnit.AscensionNodes;
            int total = 6;
            for (int i = 0; i < total; i++)
            {
                int idx = i;
                bool isUnlocked = unlocked.Contains(i);
                bool isPriorUnlocked = i == 0 || unlocked.Contains(i - 1);
                bool hasDupe = HasDuplicate();
                bool canUnlock = !isUnlocked && isPriorUnlocked && hasDupe;

                var nodeBtnGo = new GameObject($"NodeButton_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                nodeBtnGo.transform.SetParent(_leftNodeContainer, false);
                var nodeBtnRect = nodeBtnGo.GetComponent<RectTransform>();
                nodeBtnRect.anchorMin = Vector2.zero;
                nodeBtnRect.anchorMax = Vector2.zero;
                nodeBtnRect.anchoredPosition = NodePositions[i];
                nodeBtnRect.sizeDelta = new Vector2(70, 70);

                var img = nodeBtnGo.GetComponent<Image>();
                img.sprite = GetOrCreateCircleSprite();
                img.color = isUnlocked ? new Color(0.9f, 0.7f, 0.2f, 1f) : 
                            (canUnlock ? new Color(0.5f, 0.7f, 1f, 1f) : new Color(0.2f, 0.2f, 0.25f, 1f));

                // Add nice inner dark circle
                var innerGo = new GameObject("InnerCircle", typeof(RectTransform), typeof(Image));
                innerGo.transform.SetParent(nodeBtnGo.transform, false);
                var innerRect = innerGo.GetComponent<RectTransform>();
                innerRect.anchorMin = Vector2.zero;
                innerRect.anchorMax = Vector2.one;
                innerRect.sizeDelta = new Vector2(-6, -6);
                var innerImg = innerGo.GetComponent<Image>();
                innerImg.sprite = GetOrCreateCircleSprite();
                innerImg.color = new Color(0.08f, 0.08f, 0.12f, 1f);

                // Add Node custom/default icon
                Sprite nodeIcon = (nodes != null && i < nodes.Count) ? nodes[i]?.NodeIcon : null;
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(innerGo.transform, false);
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.sizeDelta = new Vector2(-16, -16);
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite = nodeIcon != null ? nodeIcon : _starFullSprite;
                iconImg.color = isUnlocked ? new Color(0.9f, 0.7f, 0.2f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f);

                // If selected, add a glowing target highlight ring around it
                if (_selectedNodeIndex == i)
                {
                    var targetRing = new GameObject("SelectedHighlight", typeof(RectTransform), typeof(Image));
                    targetRing.transform.SetParent(nodeBtnGo.transform, false);
                    var targetRect = targetRing.GetComponent<RectTransform>();
                    targetRect.anchorMin = Vector2.zero;
                    targetRect.anchorMax = Vector2.one;
                    targetRect.sizeDelta = new Vector2(16, 16);
                    var targetImg = targetRing.GetComponent<Image>();
                    targetImg.sprite = GetOrCreateCircleSprite();
                    targetImg.color = new Color(0.9f, 0.7f, 0.2f, 0.4f);

                    var outline = targetRing.AddComponent<Outline>();
                    outline.effectColor = new Color(0.9f, 0.7f, 0.2f, 1f);
                    outline.effectDistance = new Vector2(2, 2);
                }

                // Node number tag
                var tagGo = new GameObject("NumberTag", typeof(RectTransform), typeof(TextMeshProUGUI));
                tagGo.transform.SetParent(nodeBtnGo.transform, false);
                var tagRect = tagGo.GetComponent<RectTransform>();
                tagRect.anchorMin = new Vector2(0.5f, 0f);
                tagRect.anchorMax = new Vector2(0.5f, 0f);
                tagRect.anchoredPosition = new Vector2(0, -18);
                tagRect.sizeDelta = new Vector2(40, 20);
                var tagTxt = tagGo.GetComponent<TextMeshProUGUI>();
                tagTxt.font = _txtCurrentStars?.parent?.GetComponentInChildren<TextMeshProUGUI>()?.font;
                tagTxt.fontSize = 14;
                tagTxt.alignment = TextAlignmentOptions.Center;
                tagTxt.text = (i + 1).ToString();
                tagTxt.color = isUnlocked ? new Color(0.9f, 0.7f, 0.2f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);

                // Add button click listener
                var nodeBtn = nodeBtnGo.GetComponent<Button>();
                nodeBtn.onClick.AddListener(() =>
                {
                    _selectedNodeIndex = idx;
                    RefreshNodes();
                });
            }

            if (IsHsrLayoutAssignedInInspector)
            {
                // Fetch details for the selected node index
                int selIdx = _selectedNodeIndex;
                string roman = selIdx switch { 0 => "I", 1 => "II", 2 => "III", 3 => "IV", 4 => "V", 5 => "VI", _ => (selIdx + 1).ToString() };
                
                string nodeTierLabel = (nodes != null && selIdx < nodes.Count && nodes[selIdx] != null && !string.IsNullOrEmpty(nodes[selIdx].TierLabel))
                                        ? nodes[selIdx].TierLabel
                                        : $"RESONANCE {roman}";
                                        
                string nodeName = (nodes != null && selIdx < nodes.Count && nodes[selIdx] != null && !string.IsNullOrEmpty(nodes[selIdx].NodeName))
                                    ? nodes[selIdx].NodeName
                                    : $"Resonance Node {selIdx + 1}";
                                    
                string nodeDesc = (nodes != null && selIdx < nodes.Count && nodes[selIdx] != null && !string.IsNullOrEmpty(nodes[selIdx].NodeDescription))
                                    ? nodes[selIdx].NodeDescription
                                    : "Increases Unit stats dynamically on deployment by 5%.";

                bool selectedIsUnlocked = unlocked.Contains(selIdx);
                bool selectedIsPriorUnlocked = selIdx == 0 || unlocked.Contains(selIdx - 1);
                int ownedDupes = GetDuplicateCount();
                bool selectedHasDupe = ownedDupes >= 1;

                if (_rightTierText != null) _rightTierText.text = nodeTierLabel.ToUpper();
                if (_rightNameText != null) _rightNameText.text = nodeName.ToUpper();
                if (_rightStatusText != null)
                {
                    _rightStatusText.text = selectedIsUnlocked ? "✦ ACTIVE" : "◌ LOCKED";
                    _rightStatusText.color = selectedIsUnlocked ? new Color(0.9f, 0.7f, 0.2f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
                }
                if (_rightDescText != null) _rightDescText.text = nodeDesc;
                if (_rightDupePreviewIcon != null)
                {
                    _rightDupePreviewIcon.sprite = _currentUnit.GetSprite(UnitData.UnitImageType.Avatar);
                    _rightDupePreviewIcon.preserveAspect = true;
                }
                if (_rightCostText != null)
                {
                    _rightCostText.text = $"Duplicate Shards: <color={(selectedHasDupe ? "green" : "red")}>{ownedDupes} / 1</color>";
                }

                // Configure Button State
                if (_rightUnlockButton != null)
                {
                    if (selectedIsUnlocked)
                    {
                        if (_rightUnlockButtonImage != null) _rightUnlockButtonImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);
                        if (_rightUnlockButtonText != null)
                        {
                            _rightUnlockButtonText.text = "✦ RESONANCE ACTIVE";
                            _rightUnlockButtonText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                        }
                        _rightUnlockButton.interactable = false;
                    }
                    else if (!selectedIsPriorUnlocked)
                    {
                        if (_rightUnlockButtonImage != null) _rightUnlockButtonImage.color = new Color(0.3f, 0.2f, 0.2f, 1f);
                        if (_rightUnlockButtonText != null)
                        {
                            _rightUnlockButtonText.text = "PREVIOUS NODE REQUIRED";
                            _rightUnlockButtonText.color = new Color(0.7f, 0.5f, 0.5f, 1f);
                        }
                        _rightUnlockButton.interactable = false;
                    }
                    else if (!selectedHasDupe)
                    {
                        if (_rightUnlockButtonImage != null) _rightUnlockButtonImage.color = new Color(0.3f, 0.2f, 0.2f, 1f);
                        if (_rightUnlockButtonText != null)
                        {
                            _rightUnlockButtonText.text = "REQUIRES DUPLICATE SHARD";
                            _rightUnlockButtonText.color = new Color(0.7f, 0.5f, 0.5f, 1f);
                        }
                        _rightUnlockButton.interactable = false;
                    }
                    else
                    {
                        if (_rightUnlockButtonImage != null) _rightUnlockButtonImage.color = new Color(0.9f, 0.65f, 0.2f, 1f);
                        if (_rightUnlockButtonText != null)
                        {
                            _rightUnlockButtonText.text = "ACTIVATE RESONANCE";
                            _rightUnlockButtonText.color = Color.white;
                        }
                        _rightUnlockButton.interactable = true;

                        _rightUnlockButton.onClick.RemoveAllListeners();
                        _rightUnlockButton.onClick.AddListener(() =>
                        {
                            OnUnlockNode(selIdx, entry);
                        });
                    }
                }
            }
            else
            {
                // Create Right Detail Panel
                var rightGo = new GameObject("RightDetailPanel", typeof(RectTransform), typeof(Image));
                rightGo.transform.SetParent(_hsrLayoutRoot.transform, false);
                _rightDetailPanel = rightGo.GetComponent<RectTransform>();
                _rightDetailPanel.anchorMin = new Vector2(0.58f, 0.05f);
                _rightDetailPanel.anchorMax = new Vector2(0.98f, 0.95f);
                _rightDetailPanel.sizeDelta = Vector2.zero;

                var panelBg = rightGo.GetComponent<Image>();
                panelBg.color = new Color(0.08f, 0.08f, 0.12f, 0.94f);

                var outline2 = rightGo.AddComponent<Outline>();
                outline2.effectColor = new Color(0.9f, 0.65f, 0.2f, 0.3f);
                outline2.effectDistance = new Vector2(1, 1);

                var rightLayout = rightGo.AddComponent<VerticalLayoutGroup>();
                rightLayout.padding = new RectOffset(40, 40, 40, 40);
                rightLayout.spacing = 24;
                rightLayout.childAlignment = TextAnchor.UpperLeft;
                rightLayout.childControlHeight = false;
                rightLayout.childControlWidth = false;
                rightLayout.childForceExpandHeight = false;
                rightLayout.childForceExpandWidth = false;

                // Fetch details for the selected node index
                int selIdx = _selectedNodeIndex;
                string roman = selIdx switch { 0 => "I", 1 => "II", 2 => "III", 3 => "IV", 4 => "V", 5 => "VI", _ => (selIdx + 1).ToString() };
                
                string nodeTierLabel = (nodes != null && selIdx < nodes.Count && nodes[selIdx] != null && !string.IsNullOrEmpty(nodes[selIdx].TierLabel))
                                        ? nodes[selIdx].TierLabel
                                        : $"RESONANCE {roman}";
                                        
                string nodeName = (nodes != null && selIdx < nodes.Count && nodes[selIdx] != null && !string.IsNullOrEmpty(nodes[selIdx].NodeName))
                                    ? nodes[selIdx].NodeName
                                    : $"Resonance Node {selIdx + 1}";
                                    
                string nodeDesc = (nodes != null && selIdx < nodes.Count && nodes[selIdx] != null && !string.IsNullOrEmpty(nodes[selIdx].NodeDescription))
                                    ? nodes[selIdx].NodeDescription
                                    : "Increases Unit stats dynamically on deployment by 5%.";

                bool selectedIsUnlocked = unlocked.Contains(selIdx);
                bool selectedIsPriorUnlocked = selIdx == 0 || unlocked.Contains(selIdx - 1);
                int ownedDupes = GetDuplicateCount();
                bool selectedHasDupe = ownedDupes >= 1;

                // 1. Tier Label
                var tierGo = new GameObject("TierText", typeof(RectTransform), typeof(TextMeshProUGUI));
                tierGo.transform.SetParent(rightGo.transform, false);
                var tierTxt = tierGo.GetComponent<TextMeshProUGUI>();
                tierTxt.font = _txtCurrentStars?.parent?.GetComponentInChildren<TextMeshProUGUI>()?.font;
                tierTxt.fontSize = 20;
                tierTxt.fontStyle = FontStyles.Bold;
                tierTxt.color = new Color(0.9f, 0.65f, 0.2f, 1f);
                tierTxt.text = nodeTierLabel.ToUpper();

                // 2. Name Text
                var nameGo = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
                nameGo.transform.SetParent(rightGo.transform, false);
                var nameTxt = nameGo.GetComponent<TextMeshProUGUI>();
                nameTxt.font = tierTxt.font;
                nameTxt.fontSize = 32;
                nameTxt.fontStyle = FontStyles.Bold;
                nameTxt.color = Color.white;
                nameTxt.text = nodeName.ToUpper();

                // 3. Status Badge
                var statusGo = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
                statusGo.transform.SetParent(rightGo.transform, false);
                var statusTxt = statusGo.GetComponent<TextMeshProUGUI>();
                statusTxt.font = tierTxt.font;
                statusTxt.fontSize = 18;
                statusTxt.fontStyle = FontStyles.Bold;
                statusTxt.text = selectedIsUnlocked ? "✦ ACTIVE" : "◌ LOCKED";
                statusTxt.color = selectedIsUnlocked ? new Color(0.9f, 0.7f, 0.2f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);

                // Divider Line
                var divGo = new GameObject("Divider", typeof(RectTransform), typeof(Image));
                divGo.transform.SetParent(rightGo.transform, false);
                var divRect = divGo.GetComponent<RectTransform>();
                divRect.sizeDelta = new Vector2(400, 2);
                var divImg = divGo.GetComponent<Image>();
                divImg.color = new Color(0.9f, 0.65f, 0.2f, 0.3f);

                // 4. Description Text
                var descGo = new GameObject("DescText", typeof(RectTransform), typeof(TextMeshProUGUI));
                descGo.transform.SetParent(rightGo.transform, false);
                var descTxt = descGo.GetComponent<TextMeshProUGUI>();
                descTxt.font = tierTxt.font;
                descTxt.fontSize = 18;
                descTxt.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                descTxt.enableWordWrapping = true;
                descTxt.text = nodeDesc;
                var descRect = descGo.GetComponent<RectTransform>();
                descRect.sizeDelta = new Vector2(480, 160);

                // 5. Requirements Holder
                var reqHolder = new GameObject("ReqHolder", typeof(RectTransform));
                reqHolder.transform.SetParent(rightGo.transform, false);
                var reqRect = reqHolder.GetComponent<RectTransform>();
                reqRect.sizeDelta = new Vector2(480, 70);

                // Duplicate Unit Face Icon Preview
                var previewIconGo = new GameObject("DupePreviewIcon", typeof(RectTransform), typeof(Image));
                previewIconGo.transform.SetParent(reqHolder.transform, false);
                var previewIcon = previewIconGo.GetComponent<Image>();
                previewIcon.sprite = _currentUnit.GetSprite(UnitData.UnitImageType.Avatar);
                previewIcon.preserveAspect = true;
                var previewRect = previewIconGo.GetComponent<RectTransform>();
                previewRect.anchorMin = new Vector2(0f, 0.5f);
                previewRect.anchorMax = new Vector2(0f, 0.5f);
                previewRect.anchoredPosition = new Vector2(30, 0);
                previewRect.sizeDelta = new Vector2(56, 56);

                // Round border for avatar
                var avatarOutline = previewIconGo.AddComponent<Outline>();
                avatarOutline.effectColor = new Color(0.9f, 0.65f, 0.2f, 0.4f);
                avatarOutline.effectDistance = new Vector2(1, 1);

                // Duplicate count status text
                var costGo = new GameObject("CostText", typeof(RectTransform), typeof(TextMeshProUGUI));
                costGo.transform.SetParent(reqHolder.transform, false);
                var costTxt = costGo.GetComponent<TextMeshProUGUI>();
                costTxt.font = tierTxt.font;
                costTxt.fontSize = 18;
                costTxt.alignment = TextAlignmentOptions.Left;
                costTxt.text = $"Duplicate Shards: <color={(selectedHasDupe ? "green" : "red")}>{ownedDupes} / 1</color>";
                var costRect = costGo.GetComponent<RectTransform>();
                costRect.anchorMin = new Vector2(0f, 0.5f);
                costRect.anchorMax = new Vector2(0f, 0.5f);
                costRect.anchoredPosition = new Vector2(240, 0);
                costRect.sizeDelta = new Vector2(250, 40);

                // Space
                var spacer = new GameObject("Spacer", typeof(RectTransform));
                spacer.transform.SetParent(rightGo.transform, false);
                spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(10, 20);

                // 6. Activation / Unlock Button
                var btnGo = new GameObject("UnlockButton", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(rightGo.transform, false);
                var btnRect = btnGo.GetComponent<RectTransform>();
                btnRect.sizeDelta = new Vector2(400, 60);

                var btnImg = btnGo.GetComponent<Image>();
                var btn = btnGo.GetComponent<Button>();

                var btnTxtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                btnTxtGo.transform.SetParent(btnGo.transform, false);
                var btnTxt = btnTxtGo.GetComponent<TextMeshProUGUI>();
                btnTxt.font = tierTxt.font;
                btnTxt.fontSize = 20;
                btnTxt.fontStyle = FontStyles.Bold;
                btnTxt.color = Color.white;
                btnTxt.alignment = TextAlignmentOptions.Center;
                var btnTxtRect = btnTxtGo.GetComponent<RectTransform>();
                btnTxtRect.anchorMin = Vector2.zero;
                btnTxtRect.anchorMax = Vector2.one;
                btnTxtRect.sizeDelta = Vector2.zero;

                var buttonShadow = btnGo.AddComponent<Shadow>();
                buttonShadow.effectColor = new Color(0, 0, 0, 0.5f);
                buttonShadow.effectDistance = new Vector2(2, -2);

                // Configure Button State
                if (selectedIsUnlocked)
                {
                    btnImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
                    btnTxt.text = "✦ RESONANCE ACTIVE";
                    btnTxt.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                    btn.interactable = false;
                }
                else if (!selectedIsPriorUnlocked)
                {
                    btnImg.color = new Color(0.3f, 0.2f, 0.2f, 1f);
                    btnTxt.text = "PREVIOUS NODE REQUIRED";
                    btnTxt.color = new Color(0.7f, 0.5f, 0.5f, 1f);
                    btn.interactable = false;
                }
                else if (!selectedHasDupe)
                {
                    btnImg.color = new Color(0.3f, 0.2f, 0.2f, 1f);
                    btnTxt.text = "REQUIRES DUPLICATE SHARD";
                    btnTxt.color = new Color(0.7f, 0.5f, 0.5f, 1f);
                    btn.interactable = false;
                }
                else
                {
                    btnImg.color = new Color(0.9f, 0.65f, 0.2f, 1f);
                    btnTxt.text = "ACTIVATE RESONANCE";
                    btnTxt.color = Color.white;
                    btn.interactable = true;

                    // Add glow border outline
                    var glow = btnGo.AddComponent<Outline>();
                    glow.effectColor = new Color(0.9f, 0.7f, 0.2f, 0.5f);
                    glow.effectDistance = new Vector2(2, 2);

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        OnUnlockNode(selIdx, entry);
                    });
                }
            }
        }

        private void OnUnlockNode(int idx, UnitInventoryEntry entry)
        {
            if (!ConsumeDuplicate()) return;
            if (entry != null && !entry.UnlockedNodes.Contains(idx))
                entry.UnlockedNodes.Add(idx);

            // Apply resonance node to runtime stat multiplier
            _currentUnit.UnlockedResonanceCount = entry?.UnlockedNodes.Count ?? 0;
            _currentUnit.RefreshStats(MaouSamaTD.Core.AppEntryPoint.LoadedScalingData);
            _saveManager.Save();

            Debug.Log($"[ResonancePanel] Node {idx} unlocked for {_currentUnit.UnitName}. " +
                      $"Total nodes: {_currentUnit.UnlockedResonanceCount} (+{_currentUnit.UnlockedResonanceCount * 5}%)");
            RefreshNodes();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private UnitInventoryEntry GetMainEntry()
        {
            if (_saveManager?.CurrentData == null || _currentUnit == null) return null;
            string id = _currentUnit.name;
            return _saveManager.CurrentData.UnitInventory.Find(e => e.UnitID == id && !e.IsDuplicate);
        }

        private bool HasDuplicate()
        {
            if (_saveManager?.CurrentData == null || _currentUnit == null) return false;
            string id = _currentUnit.name;
            return _saveManager.CurrentData.UnitInventory.Exists(e => e.UnitID == id && e.IsDuplicate);
        }

        private bool ConsumeDuplicate()
        {
            if (_saveManager?.CurrentData == null || _currentUnit == null) return false;
            string id  = _currentUnit.name;
            var dupe   = _saveManager.CurrentData.UnitInventory.Find(e => e.UnitID == id && e.IsDuplicate);
            if (dupe == null) { Debug.LogWarning("[ResonancePanel] No duplicate available."); return false; }
            _saveManager.CurrentData.UnitInventory.Remove(dupe);
            return true;
        }



        private static string FriendlyMaterialName(string id) => id switch
        {
            "mat_shadow_essence"   => "Shadow Essence",
            "mat_bandit_insignia"  => "Bandit Insignia",
            "mat_animal_fang"      => "Beast Fang",
            "mat_golem_core"       => "Golem Core",
            _                      => id,
        };
    }

}
