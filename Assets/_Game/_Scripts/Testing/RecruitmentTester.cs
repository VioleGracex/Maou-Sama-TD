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
    }
}
