using UnityEngine;

namespace MaouSamaTD.Skills
{
    [CreateAssetMenu(fileName = "NewUnitSkill", menuName = "MaouSamaTD/Skills/Unit Skill")]
    public class UnitSkillData : SkillBase
    {
        [Header("Unit Costs")]
        public float ChargeCost = 100f; // SP/Charge needed
    }
}
