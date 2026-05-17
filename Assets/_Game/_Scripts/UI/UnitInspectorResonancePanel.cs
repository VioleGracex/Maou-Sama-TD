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
        [SerializeField] private TextMeshProUGUI _txtCurrentStars;
        [SerializeField] private TextMeshProUGUI _txtNextStars;
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

            if (_txtCurrentStars) _txtCurrentStars.text = $"★ {currentStars}";
            if (_txtNextStars)    _txtNextStars.text    = nextStars <= 6 ? $"→ ★ {nextStars}" : "MAX";

            if (nextStars > 6)
            {
                if (_btnPromote)      _btnPromote.interactable = false;
                if (_txtPromoteStatus) _txtPromoteStatus.text = "Already at max rank!";
                return;
            }

            int starIdx      = nextStars - 1; // index into arrays (1-based target → 0-based idx)
            int goldCost     = PromoteGoldCosts[starIdx];
            int primaryNeed  = PromotePrimaryCounts[starIdx];
            int secondaryNeed = PromoteSecondaryCounts[starIdx];

            var (primaryID, secondaryID) = GetClassMaterials(_currentUnit.Class);

            int ownedGold      = _economyManager != null ? _economyManager.Gold : (_saveManager.CurrentData?.Gold ?? 0);
            int ownedPrimary   = _saveManager.GetItemCount(primaryID);
            int ownedSecondary = _saveManager.GetItemCount(secondaryID);

            if (_txtPromoteGoldCost)    _txtPromoteGoldCost.text    = $"{ownedGold:N0} / {goldCost:N0} Gold";
            if (_txtPrimaryMatName)     _txtPrimaryMatName.text     = FriendlyMaterialName(primaryID);
            if (_txtPrimaryMatCount)    _txtPrimaryMatCount.text    = $"{ownedPrimary} / {primaryNeed}";
            if (_txtSecondaryMatName)   _txtSecondaryMatName.text   = FriendlyMaterialName(secondaryID);
            if (_txtSecondaryMatCount)  _txtSecondaryMatCount.text  = $"{ownedSecondary} / {secondaryNeed}";

            bool canPromote = ownedGold >= goldCost
                           && ownedPrimary >= primaryNeed
                           && ownedSecondary >= secondaryNeed;

            if (_btnPromote)      _btnPromote.interactable = canPromote;
            if (_txtPromoteStatus) _txtPromoteStatus.text  = canPromote ? "Ready to Promote!" : "Not enough resources.";
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
            int primaryNeed   = PromotePrimaryCounts[starIdx];
            int secondaryNeed = PromoteSecondaryCounts[starIdx];
            var (primaryID, secondaryID) = GetClassMaterials(_currentUnit.Class);

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
            if (!_saveManager.RemoveItem(primaryID, primaryNeed) ||
                !_saveManager.RemoveItem(secondaryID, secondaryNeed))
            {
                // Refund gold on material failure
                if (_economyManager != null) _economyManager.AddGold(goldCost);
                else _saveManager.AddGold(goldCost);
                Debug.LogWarning("[ResonancePanel] Promote failed: insufficient materials.");
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
                string label    = nodes != null && i < nodes.Count && nodes[i] != null
                                    ? nodes[i].NodeDescription
                                    : $"Resonance Node {i + 1}";

                if (_nodeEntryPrefab == null) continue;
                var go   = Instantiate(_nodeEntryPrefab, _nodesScrollRect.content);
                var item = go.GetComponent<NodeEntryUI>();
                if (item != null)
                {
                    int capturedIdx = i;
                    item.Setup(label, isUnlocked, canUnlock, () => OnUnlockNode(capturedIdx, entry, item));
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

        private static (string primary, string secondary) GetClassMaterials(UnitClass unitClass)
        {
            return unitClass switch
            {
                UnitClass.Vanguard or UnitClass.Bastion =>
                    ("mat_bandit_insignia", "mat_golem_core"),
                UnitClass.Ranger or UnitClass.Gunner =>
                    ("mat_animal_fang", "mat_bandit_insignia"),
                UnitClass.Executioner or UnitClass.Assassin =>
                    ("mat_animal_fang", "mat_bandit_insignia"),
                UnitClass.Warlock or UnitClass.Sage or UnitClass.Support
                    or UnitClass.Necromancer or UnitClass.Architect =>
                    ("mat_shadow_essence", "mat_golem_core"),
                UnitClass.Overlord =>
                    ("mat_shadow_essence", "mat_animal_fang"),
                _ =>
                    ("mat_bandit_insignia", "mat_golem_core"),
            };
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

    /// <summary>Prefab component for a single Resonance Node row.</summary>
    public class NodeEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _txtLabel;
        [SerializeField] private Image _nodeIcon;
        [SerializeField] private Button _btnUnlock;
        [SerializeField] private Color _colorLocked   = new Color(0.4f, 0.4f, 0.4f);
        [SerializeField] private Color _colorUnlocked = new Color(0.9f, 0.7f, 0.2f);

        public void Setup(string label, bool unlocked, bool canUnlock, System.Action onUnlock)
        {
            if (_txtLabel) _txtLabel.text = unlocked ? $"✦ {label}  (+5%)" : $"◌ {label}  (Locked)";
            if (_nodeIcon) _nodeIcon.color = unlocked ? _colorUnlocked : _colorLocked;
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
