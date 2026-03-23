using System;
using UnityEngine;

namespace MaouSamaTD.Units
{
    [System.Serializable]
    public struct RarityStatGrowth
    {
        public UnitRarity Rarity;
        [Tooltip("Extra HP gained per level for this rarity.")]
        public float HpGrowthPerLevel;
        [Tooltip("Extra ATK gained per level for this rarity.")]
        public float AtkGrowthPerLevel;
        [Tooltip("Extra DEF gained per level for this rarity.")]
        public float DefGrowthPerLevel;
    }

    [System.Serializable]
    public struct ClassStatMultipliers
    {
        public UnitClass ClassType;
        public string OverrideClassName; // E.g., if you want the UI to say something else
        public Sprite ClassIcon;
        
        [Header("Class Base Multipliers")]
        public float BaseHpMultiplier;
        public float BaseAtkMultiplier;
        public float BaseDefMultiplier;

        [Header("Rarity (Star) Growth")]
        public RarityStatGrowth[] RarityGrowths;
    }

    [CreateAssetMenu(fileName = "ClassScalingData", menuName = "MaouSamaTD/Class Scaling Data")]
    public class ClassScalingData : MaouSamaTD.Core.GameDataSO
    {
        public string AssetLabel;
        public ClassStatMultipliers[] ClassScalings;

        public bool TryGetMultipliers(UnitClass classType, out ClassStatMultipliers result)
        {
            result = default;
            if (ClassScalings == null) return false;
            
            foreach (var scaling in ClassScalings)
            {
                if (scaling.ClassType == classType)
                {
                    result = scaling;
                    return true;
                }
            }
            return false;
        }

        public bool TryGetGrowth(UnitClass classType, UnitRarity rarity, out float hpGrowth, out float atkGrowth, out float defGrowth)
        {
            hpGrowth = 0f; atkGrowth = 0f; defGrowth = 0f;

            if (ClassScalings == null) return false;
            
            foreach (var scaling in ClassScalings)
            {
                if (scaling.ClassType == classType)
                {
                    hpGrowth += scaling.BaseHpMultiplier;
                    atkGrowth += scaling.BaseAtkMultiplier;
                    defGrowth += scaling.BaseDefMultiplier;

                    if (scaling.RarityGrowths != null)
                    {
                        foreach (var rarityGrowth in scaling.RarityGrowths)
                        {
                            if (rarityGrowth.Rarity == rarity)
                            {
                                hpGrowth += rarityGrowth.HpGrowthPerLevel;
                                atkGrowth += rarityGrowth.AtkGrowthPerLevel;
                                defGrowth += rarityGrowth.DefGrowthPerLevel;
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
