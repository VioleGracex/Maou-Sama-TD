using UnityEngine;
using MaouSamaTD.Units;

namespace MaouSamaTD.Progression
{
    public static class ProgressionLogic
    {
        /// <summary>
        /// Formula: XP_Next = 100 * (Level ^ 1.8)
        /// </summary>
        public static int GetRequiredXP(int level)
        {
            return Mathf.FloorToInt(100f * Mathf.Pow(level, 1.8f));
        }

        /// <summary>
        /// Calculate stat bonus based on stars.
        /// Each star adds a permanent multiplier to base stats.
        /// </summary>
        public static float GetStarStatMultiplier(int stars)
        {
            // Base is 1.0 at 1 star. +20% per star?
            return 1.0f + (stars - 1) * 0.2f;
        }

        public static void DistributeMissionXP(System.Collections.Generic.List<UnitData> deployedUnits, int missionTotalXP)
        {
            if (deployedUnits == null || deployedUnits.Count == 0) return;
            
            int xpPerUnit = missionTotalXP / deployedUnits.Count;
            foreach (var unit in deployedUnits)
            {
                AddXP(unit, xpPerUnit);
            }
        }

        public static void AddXP(UnitData unit, int amount)
        {
            if (unit.Level >= unit.MaxLevel) return;

            unit.Experience += amount;
            while (unit.Experience >= GetRequiredXP(unit.Level) && unit.Level < unit.MaxLevel)
            {
                unit.Experience -= GetRequiredXP(unit.Level);
                unit.Level++;
            }
        }
    }
}
