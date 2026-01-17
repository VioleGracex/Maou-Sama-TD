using UnityEngine;
using MaouSamaTD.Units;
using System.Collections.Generic;

namespace MaouSamaTD.Managers
{
    public class DebugManager : MonoBehaviour
    {
        public static DebugManager Instance { get; private set; }

        [Header("Global Test Values")]
        [SerializeField] private float _globalDamageAmount = 50f;
        [SerializeField] private float _globalHealAmount = 50f;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        [ContextMenu("Damage All Units")]
        public void DamageAllUnits()
        {
            PlayerUnit[] units = FindObjectsOfType<PlayerUnit>();
            foreach (var unit in units)
            {
                if (unit != null)
                {
                    unit.TakeDamage(_globalDamageAmount);
                }
            }
            Debug.Log($"Damaged {units.Length} units for {_globalDamageAmount} HP.");
        }

        [ContextMenu("Heal All Units")]
        public void HealAllUnits()
        {
            PlayerUnit[] units = FindObjectsOfType<PlayerUnit>();
            foreach (var unit in units)
            {
                if (unit != null)
                {
                    unit.Heal(_globalHealAmount);
                }
            }
            Debug.Log($"Healed {units.Length} units for {_globalHealAmount} HP.");
        }

        [ContextMenu("Retreat All Units")]
        public void RetreatAllUnits()
        {
            PlayerUnit[] units = FindObjectsOfType<PlayerUnit>();
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
    }
}
