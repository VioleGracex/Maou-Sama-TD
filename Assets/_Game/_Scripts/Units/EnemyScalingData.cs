using System;
using UnityEngine;

namespace MaouSamaTD.Units
{
    [System.Serializable]
    public struct EnemyStatMultipliers
    {
        public UnitClass EnemyType;
        public string OverrideName;
        public Sprite Icon;
        
        [Header("Base Multipliers")]
        public float BaseHpMultiplier;
        public float BaseAtkMultiplier;
        public float BaseDefMultiplier;

        [Header("Difficulty (Star) Growth")]
        public RarityStatGrowth[] DifficultyGrowths;
    }

    [CreateAssetMenu(fileName = "EnemyScalingData", menuName = "MaouSamaTD/Enemy Scaling Data")]
    public class EnemyScalingData : MaouSamaTD.Core.GameDataSO
    {
        public string AssetLabel;
        public EnemyStatMultipliers[] EnemyScalings;

        public bool TryGetMultipliers(UnitClass enemyType, out EnemyStatMultipliers result)
        {
            result = default;
            if (EnemyScalings == null) return false;
            
            foreach (var scaling in EnemyScalings)
            {
                if (scaling.EnemyType == enemyType)
                {
                    result = scaling;
                    return true;
                }
            }
            return false;
        }

        public bool TryGetGrowth(UnitClass enemyType, UnitRarity difficulty, out float hpGrowth, out float atkGrowth, out float defGrowth)
        {
            hpGrowth = 0f; atkGrowth = 0f; defGrowth = 0f;

            if (EnemyScalings == null) return false;
            
            foreach (var scaling in EnemyScalings)
            {
                if (scaling.EnemyType == enemyType)
                {
                    hpGrowth += scaling.BaseHpMultiplier;
                    atkGrowth += scaling.BaseAtkMultiplier;
                    defGrowth += scaling.BaseDefMultiplier;

                    if (scaling.DifficultyGrowths != null)
                    {
                        foreach (var growth in scaling.DifficultyGrowths)
                        {
                            if (growth.Rarity == difficulty)
                            {
                                hpGrowth += growth.HpGrowthPerLevel;
                                atkGrowth += growth.AtkGrowthPerLevel;
                                defGrowth += growth.DefGrowthPerLevel;
                                break;
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
