using UnityEngine;
using MaouSamaTD.Data;
using MaouSamaTD.Units;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace MaouSamaTD.Managers
{
    public enum DuplicateAction
    {
        Sacrifice, // Level up / Potential
        Liquify    // Gold / Blood Crest
    }

    public class GachaManager : global::UnityEngine.MonoBehaviour
    {
        [Inject] private SaveManager _saveManager;
        [Inject] private EconomyManager _economyManager;
        [Inject] private UnitDatabase _unitDatabase;

        public event System.Action<List<UnitInventoryEntry>> OnSummonCompleted;
        public event System.Action OnPoolsReady;

        public List<UnitInventoryEntry> LastSummonResults { get; private set; }
        public bool IsPoolReady { get; private set; }

        private Dictionary<UnitRarity, List<UnitData>> _rarityPools = new Dictionary<UnitRarity, List<UnitData>>();
        private List<AsyncOperationHandle<IList<UnitData>>> _loadingHandles = new List<AsyncOperationHandle<IList<UnitData>>>();

        public void InitializePools(GachaPoolSO gachaPool)
        {
            IsPoolReady = false;
            
            // Clear existing
            _rarityPools.Clear();
            foreach (var handle in _loadingHandles)
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }
            _loadingHandles.Clear();

            // Load for each rarity
            foreach (UnitRarity rarity in System.Enum.GetValues(typeof(UnitRarity)))
            {
                string label = gachaPool.GetLabelByRarity(rarity);
                var handle = Addressables.LoadAssetsAsync<UnitData>((object)label, null);
                _loadingHandles.Add(handle);
                
                handle.Completed += (op) => {
                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        _rarityPools[rarity] = op.Result.ToList();
                        CheckPoolsReady();
                    }
                };
            }
        }

        private void CheckPoolsReady()
        {
            if (_rarityPools.Count == System.Enum.GetValues(typeof(UnitRarity)).Length)
            {
                IsPoolReady = true;
                OnPoolsReady?.Invoke();
                Debug.Log("[GachaManager] Dynamic pools loaded from Addressables labels.");
            }
        }

        public bool CanSummon(GachaBannerSO banner, bool isMulti)
        {
            int cost = isMulti ? banner.MultiCost : banner.SingleCost;
            if (banner.Currency == GachaCurrencyType.Gold)
                return _economyManager.Gold >= cost;
            else
                return _economyManager.BloodCrest >= cost;
        }

        public void Summon(GachaBannerSO banner, bool isMulti)
        {
            int count = isMulti ? 10 : 1;
            int cost = isMulti ? banner.MultiCost : banner.SingleCost;

            bool success = banner.Currency == GachaCurrencyType.Gold 
                ? _economyManager.TrySpendGold(cost) 
                : _economyManager.TrySpendBloodCrest(cost);

            if (!success) return;

            List<UnitInventoryEntry> results = new List<UnitInventoryEntry>();
            for (int i = 0; i < count; i++)
            {
                UnitData drawnUnit = RollUnit(banner);
                string unitID = drawnUnit.UniqueID ?? drawnUnit.name;
                UnitInventoryEntry entry = new UnitInventoryEntry(unitID);
                
                // Duplicate Detection
                bool alreadyOwned = _saveManager.CurrentData.UnlockedUnits.Contains(unitID);
                if (alreadyOwned)
                {
                    entry.IsDuplicate = true;
                    var (gold, crest) = GetCompensationValues(drawnUnit.Rarity);
                    entry.CompensationGold = gold;
                    entry.CompensationBloodCrest = crest;
                    
                    _economyManager.AddGold(gold);
                    _economyManager.AddBloodCrest(crest);
                    
                    // Note: We still add it to inventory for Arknights-style Potential system
                    // or the user might want it auto-removed. The user mentioned "compensation image"
                    // and "liquify" in the code, but also "Duplicates (already owned)".
                    // We'll keep it in inventory for now as the current system adds it.
                }

                results.Add(entry);
                
                // Add to inventory
                _saveManager.CurrentData.UnitInventory.Add(entry);
                if (!alreadyOwned)
                {
                    _saveManager.CurrentData.UnlockedUnits.Add(unitID);
                }
            }

            _saveManager.Save();
            LastSummonResults = results;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            MaouSamaTD.Testing.GameStateExporter.PushEvent($"GachaSummoned:{count}");
#endif
            OnSummonCompleted?.Invoke(results);
        }

        private (int gold, int bloodCrest) GetCompensationValues(UnitRarity rarity)
        {
            return rarity switch
            {
                UnitRarity.Legendary => (5000, 200),
                UnitRarity.Master => (2000, 50),
                UnitRarity.Elite => (500, 10),
                UnitRarity.Rare => (200, 2),
                UnitRarity.Uncommon => (100, 1),
                _ => (50, 0)
            };
        }

        private UnitData RollUnit(GachaBannerSO banner)
        {
            if (!IsPoolReady) 
            {
                Debug.LogError("[GachaManager] Attempted to roll before pools were ready!");
                return null;
            }

            float roll = Random.Range(0f, 100f);
            UnitRarity rarity;

            if (roll < banner.LegendaryRate) rarity = UnitRarity.Legendary;
            else if (roll < banner.LegendaryRate + banner.MasterRate) rarity = UnitRarity.Master;
            else if (roll < banner.LegendaryRate + banner.MasterRate + banner.EliteRate) rarity = UnitRarity.Elite;
            else rarity = UnitRarity.Rare; 

            if (_rarityPools.TryGetValue(rarity, out var pool) && pool.Count > 0)
            {
                return pool[Random.Range(0, pool.Count)];
            }

            // Fallback to any available unit if specific rarity pool is empty
            var fallbackPool = _rarityPools.Values.FirstOrDefault(p => p.Count > 0);
            return fallbackPool != null ? fallbackPool[Random.Range(0, fallbackPool.Count)] : null;
        }

        public void ProcessDuplicate(UnitInventoryEntry duplicate, UnitInventoryEntry target, DuplicateAction action)
        {
            if (duplicate.InstanceID == target.InstanceID) return;
            if (duplicate.UnitID != target.UnitID) return;

            if (action == DuplicateAction.Sacrifice)
            {
                // Logic: Level up or Potential
                target.Potential++;
                target.Experience += 500; // Example flat XP
                // Check level up logic here if implemented
            }
            else if (action == DuplicateAction.Liquify)
            {
                // Logic: Gain currency based on rarity
                UnitData data = _unitDatabase.GetUnitByID(duplicate.UnitID);
                int refund = GetLiquificationValue(data.Rarity);
                _economyManager.AddBloodCrest(refund); 
            }

            // Remove duplicate from inventory
            _saveManager.CurrentData.UnitInventory.Remove(duplicate);
            _saveManager.Save();
        }

        private int GetLiquificationValue(UnitRarity rarity)
        {
            return rarity switch
            {
                UnitRarity.Legendary => 100,
                UnitRarity.Master => 25,
                UnitRarity.Elite => 10,
                _ => 1
            };
        }
    }
}
