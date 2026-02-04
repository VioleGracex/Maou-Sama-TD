using UnityEngine;
using MaouSamaTD.Units;
using System.Collections.Generic;
using NaughtyAttributes;
using MaouSamaTD.Levels; // Added

namespace MaouSamaTD.Managers
{
    public class DebugManager : MonoBehaviour
    {
        [Header("Global Test Values")]
        [SerializeField] private float _globalDamageAmount = 50f;
        [SerializeField] private float _globalHealAmount = 50f;
        [field: SerializeField] public EnemyData TestEnemyData { get; private set; }

        [Button("Damage All Units")]
        public void DamageAllUnits()
        {
            PlayerUnit[] units = FindObjectsByType<PlayerUnit>(FindObjectsSortMode.None);
            foreach (var unit in units)
            {
                if (unit != null)
                {
                    unit.TakeDamage(_globalDamageAmount);
                }
            }
            Debug.Log($"Damaged {units.Length} units for {_globalDamageAmount} HP.");
        }

        [Button("Heal All Units")]
        public void HealAllUnits()
        {
            PlayerUnit[] units = FindObjectsByType<PlayerUnit>(FindObjectsSortMode.None);
            foreach (var unit in units)
            {
                if (unit != null)
                {
                    unit.Heal(_globalHealAmount);
                }
            }
            Debug.Log($"Healed {units.Length} units for {_globalHealAmount} HP.");
        }

        [Button("Retreat All Units")]
        public void RetreatAllUnits()
        {
            PlayerUnit[] units = FindObjectsByType<PlayerUnit>(FindObjectsSortMode.None);
            int count = units.Length;
            foreach (var unit in units)
            {
                if (unit != null)
                {
                    unit.Retreat();
                }
            }
            Debug.Log($"Retreated {count} units.");
        }

        [Button("Spawn Test Unit")]
        public void SpawnTestUnit()
        {
            if (TestEnemyData == null)
            {
                Debug.LogWarning("DebugManager: No TestEnemyData assigned!");
                return;
            }

            var enemyManager = FindAnyObjectByType<EnemyManager>();
            if (enemyManager != null)
            {
                enemyManager.SpawnEnemy(TestEnemyData);
            }
            else
            {
                Debug.LogError("DebugManager: No EnemyManager found!");
            }
        }
    }
}
