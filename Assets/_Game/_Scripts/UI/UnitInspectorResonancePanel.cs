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
            var subTabs = transform.Find("SubTabs");
            if (subTabs != null) subTabs.gameObject.SetActive(false);
            var titleTxt = transform.Find("Header/Title")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (titleTxt != null) titleTxt.text = "MEMORIAL CHAMBERS";
            SwitchSubTab(1);
        }

        public void OpenAsResonance(UnitData u)
        {
            _currentUnit = u;
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
        private void RefreshMemories()
        {
            if (_memoriesScrollRect == null || _memoriesScrollRect.content == null) return;
            foreach (Transform c in _memoriesScrollRect.content) Destroy(c.gameObject);

            if (_currentUnit == null || _saveManager == null) return;

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

        private void OnUnlockMemory(int idx, UnitInventoryEntry entry, MemoryEntryUI item)
        {
            if (!ConsumeDuplicate()) return;
            if (entry != null && !entry.UnlockedLores.Contains(idx))
                entry.UnlockedLores.Add(idx);
            _saveManager.Save();
            RefreshMemories();
        }

        // ── Resonance Nodes ───────────────────────────────────────────────────
        private void RefreshNodes()
        {
            if (_nodesScrollRect == null || _nodesScrollRect.content == null) return;
            foreach (Transform c in _nodesScrollRect.content) Destroy(c.gameObject);

            if (_currentUnit == null || _saveManager == null) return;

            var entry   = GetMainEntry();
            var unlocked = entry?.UnlockedNodes ?? new List<int>();
            int unlockedCount = unlocked.Count;

            if (_txtNodeSummary) _txtNodeSummary.text =
                $"Nodes Unlocked: {unlockedCount}/6  |  Stat Bonus: +{unlockedCount * 5}% HP/ATK/DEF";

            var nodes = _currentUnit.AscensionNodes;
            int total = nodes != null ? Mathf.Min(nodes.Count, 6) : 6;

            for (int i = 0; i < total; i++)
            {
                bool isUnlocked = unlocked.Contains(i);
                bool canUnlock  = !isUnlocked && (i == 0 || unlocked.Contains(i - 1)) && HasDuplicate();
                
                string roman = i switch { 0 => "I", 1 => "II", 2 => "III", 3 => "IV", 4 => "V", 5 => "VI", _ => (i + 1).ToString() };
                
                string tier = nodes != null && i < nodes.Count && nodes[i] != null && !string.IsNullOrEmpty(nodes[i].TierLabel)
                                ? nodes[i].TierLabel
                                : $"RESONANCE {roman}";
                                
                string name = nodes != null && i < nodes.Count && nodes[i] != null && !string.IsNullOrEmpty(nodes[i].NodeName)
                                ? nodes[i].NodeName
                                : $"Resonance Node {i + 1}";
                                
                string desc = nodes != null && i < nodes.Count && nodes[i] != null && !string.IsNullOrEmpty(nodes[i].NodeDescription)
                                ? nodes[i].NodeDescription
                                : "Increases Unit stats dynamically on deployment.";
                                
                Sprite icon = nodes != null && i < nodes.Count && nodes[i] != null ? nodes[i].NodeIcon : null;

                if (_nodeEntryPrefab == null) continue;
                var go   = Instantiate(_nodeEntryPrefab, _nodesScrollRect.content);
                var item = go.GetComponent<NodeEntryUI>();
                if (item != null)
                {
                    int capturedIdx = i;
                    item.SetupRich(tier, name, desc, isUnlocked, canUnlock, () => OnUnlockNode(capturedIdx, entry, item), icon);
                }
            }
        }

        private void OnUnlockNode(int idx, UnitInventoryEntry entry, NodeEntryUI item)
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

    // ── Lightweight UI entry components ────────────────────────────────────────

    /// <summary>Prefab component for a single Lore/Memory chamber row.</summary>
    public class MemoryEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _txtTitle;
        [SerializeField] private TextMeshProUGUI _txtBody;
        [SerializeField] private GameObject _lockOverlay;
        [SerializeField] private Button _btnUnlock;

        public void Setup(string title, string body, bool unlocked, bool canUnlock, System.Action onUnlock)
        {
            if (_txtTitle) _txtTitle.text = title;
            if (_txtBody)  _txtBody.text  = unlocked ? body : "???  Unlock: 1 Duplicate";
            if (_lockOverlay) _lockOverlay.SetActive(!unlocked);
            if (_btnUnlock)
            {
                _btnUnlock.gameObject.SetActive(!unlocked);
                _btnUnlock.interactable = canUnlock;
                _btnUnlock.onClick.RemoveAllListeners();
                _btnUnlock.onClick.AddListener(() => onUnlock());
            }
        }
    }

    /// <summary>Prefab component for a single Resonance Node row in Honkai Star Rail style.</summary>
    public class NodeEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _txtTierLabel; // e.g. "NODE TIER 01" or "RESONANCE I"
        [SerializeField] private TextMeshProUGUI _txtNodeName; // e.g. "SOVEREIGN HEART"
        [SerializeField] private TextMeshProUGUI _txtDescription; // e.g. "Increases HP by 5%"
        [SerializeField] private TextMeshProUGUI _txtStatus; // e.g. "✦ ACTIVE" or "◌ LOCKED"
        [SerializeField] private Image _nodeIcon;
        [SerializeField] private Button _btnUnlock;
        
        [SerializeField] private Color _colorLocked   = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private Color _colorUnlocked = new Color(0.9f, 0.7f, 0.2f, 1f);

        // Compatibility method to prevent compilation errors if old code calls Setup
        public void Setup(string label, bool unlocked, bool canUnlock, System.Action onUnlock)
        {
            SetupRich("RESONANCE", label, "", unlocked, canUnlock, onUnlock, null);
        }

        public void SetupRich(string tierLabel, string nodeName, string desc, bool unlocked, bool canUnlock, System.Action onUnlock, Sprite icon)
        {
            if (_txtTierLabel) _txtTierLabel.text = string.IsNullOrEmpty(tierLabel) ? "RESONANCE" : tierLabel.ToUpper();
            if (_txtNodeName) _txtNodeName.text = string.IsNullOrEmpty(nodeName) ? "SOVEREIGN BOND" : nodeName.ToUpper();
            
            if (_txtDescription) 
            {
                _txtDescription.text = desc;
                _txtDescription.color = unlocked ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
            }

            if (_txtStatus)
            {
                _txtStatus.text = unlocked ? "✦ ACTIVE" : "◌ LOCKED";
                _txtStatus.color = unlocked ? _colorUnlocked : _colorLocked;
            }

            if (_nodeIcon)
            {
                if (icon != null)
                {
                    _nodeIcon.sprite = icon;
                    _nodeIcon.gameObject.SetActive(true);
                }
                _nodeIcon.color = unlocked ? _colorUnlocked : _colorLocked;
            }

            if (_btnUnlock)
            {
                _btnUnlock.gameObject.SetActive(!unlocked);
                _btnUnlock.interactable = canUnlock;
                _btnUnlock.onClick.RemoveAllListeners();
                _btnUnlock.onClick.AddListener(() => onUnlock());
            }
        }
    }
}
