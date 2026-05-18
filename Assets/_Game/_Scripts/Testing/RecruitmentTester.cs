using UnityEngine;
using MaouSamaTD.Managers;
using MaouSamaTD.Data;
using MaouSamaTD.Units;
using Zenject;
using NaughtyAttributes;

namespace MaouSamaTD.Testing
{
    public class RecruitmentTester : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _goldToAdd = 10000;
        [SerializeField] private int _bloodCrestsToAdd = 1000;

        [Header("References")]
        [SerializeField] private UnitDatabase _unitDatabase;

        private EconomyManager _economyManager;
        private SaveManager _saveManager;

        [Inject]
        public void Construct(EconomyManager economyManager, SaveManager saveManager)
        {
            _economyManager = economyManager;
            _saveManager = saveManager;
        }

        [Button("Add Gold")]
        public void AddGold()
        {
            if (_economyManager == null)
            {
                Debug.LogError("[RecruitmentTester] EconomyManager not injected!");
                return;
            }
            _economyManager.AddGold(_goldToAdd);
            Debug.Log($"[RecruitmentTester] Added {_goldToAdd} Gold.");
        }

        [Button("Add Blood Crests")]
        public void AddBloodCrest()
        {
            if (_economyManager == null)
            {
                Debug.LogError("[RecruitmentTester] EconomyManager not injected!");
                return;
            }
            _economyManager.AddBloodCrest(_bloodCrestsToAdd);
            Debug.Log($"[RecruitmentTester] Added {_bloodCrestsToAdd} Blood Crests.");
        }

        [Button("Unlock All Characters")]
        public void UnlockAllCharacters()
        {
            if (_saveManager == null)
            {
                Debug.LogError("[RecruitmentTester] SaveManager not injected!");
                return;
            }

            if (_unitDatabase == null)
            {
                Debug.LogError("[RecruitmentTester] UnitDatabase reference missing!");
                return;
            }

            int count = 0;
            foreach (var unit in _unitDatabase.AllUnits)
            {
                if (unit == null || string.IsNullOrEmpty(unit.UnitName)) continue;
                
                // Note: UnlockUnit also adds to inventory if not present
                _saveManager.UnlockUnit(unit.UnitName);
                count++;
            }

                        _saveManager.Save();
            Debug.Log($"[RecruitmentTester] Successfully unlocked {count} unique characters.");
        }

        [Button("Add XP Cores (+50 All)")]
        public void AddAllXPCores()
        {
            if (_saveManager == null) return;
            _saveManager.AddItem("xp_core_common", 50);
            _saveManager.AddItem("xp_core_rare", 50);
            _saveManager.AddItem("xp_core_epic", 50);
            _saveManager.AddItem("xp_core_legendary", 50);
            _saveManager.Save();
            Debug.Log("[RecruitmentTester] Added 50 of each XP Core tier to inventory.");
        }

        [Button("Add Promotion Materials (+50 All)")]
        public void AddAllPromotionMaterials()
        {
            if (_saveManager == null) return;
            _saveManager.AddItem("mat_shadow_essence", 50);
            _saveManager.AddItem("mat_bandit_insignia", 50);
            _saveManager.AddItem("mat_animal_fang", 50);
            _saveManager.AddItem("mat_golem_core", 50);
            _saveManager.Save();
            Debug.Log("[RecruitmentTester] Added 50 of all promotion materials to inventory.");
        }

        [Button("Max Level All Vassals (Level 99)")]
        public void MaxLevelAllVassals()
        {
            if (_saveManager == null || _saveManager.CurrentData == null) return;
            int count = 0;
            foreach (var entry in _saveManager.CurrentData.UnitInventory)
            {
                if (entry == null) continue;
                entry.Level = 99;
                entry.Experience = 0;
                count++;
            }
            _saveManager.Save();
            Debug.Log($"[RecruitmentTester] Set {count} vassals in inventory to Level 99.");
        }
    }
}
