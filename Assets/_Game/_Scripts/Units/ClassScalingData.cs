using System;
using UnityEngine;

namespace MaouSamaTD.Units
{
    [System.Serializable]
    public struct PromotionMaterialRequirement
    {
        [Tooltip("The ID of the loot item required to rank up / promote this class.")]
        public string ItemID;
        [Tooltip("Base material cost, which is multiplied by the target star rank (e.g., 5 * star).")]
        public int BaseAmount;
    }

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

        [Header("Promotion Requirements")]
        [Tooltip("List of items required to rank up / promote this class.")]
        public PromotionMaterialRequirement[] RequiredMaterials;

        [Header("Rarity (Star) Growth")]
        public RarityStatGrowth[] RarityGrowths;
    }

    [CreateAssetMenu(fileName = "ClassScalingData", menuName = "MaouSamaTD/Class Scaling Data")]
    public class ClassScalingData : MaouSamaTD.Core.GameDataSO
    {
        public string AssetLabel;
        public ClassStatMultipliers[] ClassScalings;

        public PromotionMaterialRequirement[] GetRequiredMaterials(UnitClass classType)
        {
            if (TryGetMultipliers(classType, out var result) && result.RequiredMaterials != null)
            {
                var validMats = new System.Collections.Generic.List<PromotionMaterialRequirement>();
                foreach (var req in result.RequiredMaterials)
                {
                    if (!string.IsNullOrEmpty(req.ItemID))
                    {
                        validMats.Add(req);
                    }
                }
                if (validMats.Count > 0)
                {
                    return validMats.ToArray();
                }
            }
            // Fallback based on class
            string defaultItem = classType switch
            {
                UnitClass.Bastion => "mat_golem_core",
                UnitClass.Vanguard => "mat_bandit_insignia",
                UnitClass.Executioner => "mat_animal_fang",
                UnitClass.Ranger => "mat_animal_fang",
                UnitClass.Warlock => "mat_shadow_essence",
                UnitClass.Sage => "mat_shadow_essence",
                UnitClass.Gunner => "mat_bandit_insignia",
                UnitClass.Assassin => "mat_bandit_insignia",
                _ => "mat_bandit_insignia" // Default Fallback
            };

            return new PromotionMaterialRequirement[]
            {
                new PromotionMaterialRequirement { ItemID = defaultItem, BaseAmount = 5 }
            };
        }



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
