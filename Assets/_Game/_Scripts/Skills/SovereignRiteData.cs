using UnityEngine;

namespace MaouSamaTD.Skills
{
    [CreateAssetMenu(fileName = "NewSovereignRite", menuName = "MaouSamaTD/Skills/Sovereign Rite")]
    public class SovereignRiteData : SkillBase
    {
        [Header("Global Costs")]
        public int SealCost = 50; // Currency Cost
        public float Cooldown = 30f; // Global Cooldown in seconds
    }
}
