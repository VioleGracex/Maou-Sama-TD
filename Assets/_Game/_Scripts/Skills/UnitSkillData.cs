using UnityEngine;

namespace MaouSamaTD.Skills
{
    [CreateAssetMenu(fileName = "NewUnitSkill", menuName = "MaouSamaTD/Skills/Unit Skill")]
    public class UnitSkillData : SkillBase
    {
        [Header("System References")]
        public MaouSamaTD.Units.UnitData OwnerUnit; // Linked to this unit for skin ID logic

        [System.Serializable]
        public struct SkillSkinOverride
        {
            public string SkinID;
            public SkillVisuals Visuals;
        }

        [Header("Unit Costs")]
        public float ChargeCost = 100f; // SP/Charge needed

        [Header("Stat Upgrades (Per Unit Level)")]
        public float BonusHpPerLevel = 0f;
        public float BonusAtkPerLevel = 0f;
        public float BonusDefPerLevel = 0f;

        [Header("Skins & Overrides")]
        public System.Collections.Generic.List<SkillSkinOverride> SkinOverrides = new System.Collections.Generic.List<SkillSkinOverride>();

        public SkillVisuals GetVisuals(string equippedSkinID)
        {
            if (!string.IsNullOrEmpty(equippedSkinID))
            {
                foreach (var sOverride in SkinOverrides)
                {
                    if (sOverride.SkinID == equippedSkinID) return sOverride.Visuals;
                }
            }
            return BaseVisuals;
        }
    }
}
