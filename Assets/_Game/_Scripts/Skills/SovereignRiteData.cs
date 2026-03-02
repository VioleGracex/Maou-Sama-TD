using UnityEngine;

namespace MaouSamaTD.Skills
{
    [CreateAssetMenu(fileName = "NewSovereignRite", menuName = "MaouSamaTD/Skills/Sovereign Rite")]
    public class SovereignRiteData : SkillBase
    {
        [Header("Global Costs")]
        public int SealCost = 50; 
        public float Cooldown = 30f; 

        [Header("Targeting")]
        public SkillTargetType TargetType;
        public float Range = 100f; 
        public float Radius = 0f;

        [Header("Effect")]
        public SkillEffectType EffectType;
        public float Value; 
        public float Duration;
    }
}
