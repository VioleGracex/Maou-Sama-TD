using UnityEngine;

namespace MaouSamaTD.Skills
{
    public enum SkillTargetType
    {
        Unit,       // Targets a single unit (Friendly or Enemy depending on effect)
        Ground,     // Targets a position on the grid
        None        // Instant global effect (if needed)
    }

    public enum SkillEffectType
    {
        Damage,
        Buff,
        Debuff
        // Summon?
    }

    [CreateAssetMenu(fileName = "NewSkillData", menuName = "MaouSamaTD/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("Display")]
        public string SkillName;
        [TextArea] public string Description;
        public Sprite Icon;

        [Header("Costs")]
        public int Cost = 10;
        public float Cooldown = 10f;

        [Header("Targeting")]
        public SkillTargetType TargetType;
        public float Range = 100f; // Global usually, but maybe limited
        public float Radius = 0f;  // AoE Radius (0 = single target)

        [Header("Effect")]
        public SkillEffectType EffectType;
        public float Value; // Damage amount or Buff value
        public float Duration; // 0 for instant
        
        [Header("Visuals")]
        public GameObject CastVFX; // Prefab to spawn on cast
        public GameObject HitVFX;  // Prefab to spawn on hit
        public Color RangeIndicatorColor = Color.red;
    }
}
