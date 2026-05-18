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

        // ── Resonance Nodes (Honkai Star Rail Constellation Style) ───────────
        private GameObject _hsrLayoutRoot;
        private RectTransform _leftNodeContainer;
        private RectTransform _rightDetailPanel;
        private int _selectedNodeIndex = 0;
        private Sprite _circleSprite;

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

            // Recreate visual root for HSR layout
            if (_hsrLayoutRoot != null)
            {
                Destroy(_hsrLayoutRoot);
            }

            _hsrLayoutRoot = new GameObject("HSR_Nodes_Layout_Container", typeof(RectTransform));
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
            bool selectedCanUnlock = !selectedIsUnlocked && selectedIsPriorUnlocked && selectedHasDupe;

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
