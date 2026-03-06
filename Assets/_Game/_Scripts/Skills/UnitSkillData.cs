using UnityEngine;

namespace MaouSamaTD.Skills
{
    [CreateAssetMenu(fileName = "NewUnitSkill", menuName = "MaouSamaTD/Skills/Unit Skill")]
    public class UnitSkillData : SkillBase
    {
        [Header("Unit Costs")]
        public float ChargeCost = 100f; // SP/Charge needed

        [Header("Stat Upgrades (Per Unit Level)")]
        public float BonusHpPerLevel = 0f;
        public float BonusAtkPerLevel = 0f;
        public float BonusDefPerLevel = 0f;

        [Header("Assets")]
        public GameObject UltimatePrefab;
        public Color UltimateColor = Color.red; // Banner color
        public Color TitleBgColor = Color.black;
        public Color SkillNameBgColor = Color.black;
    }
}
