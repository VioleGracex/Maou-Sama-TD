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

    // Base class for all skills/rites - contains common display and cost info
    public abstract class SkillBase : MaouSamaTD.Core.GameDataSO
    {
        [Header("Display")]
        public string SkillName;
        [TextArea] public string Description;
        public Sprite Icon;
        
        [Header("Visuals")]
        public GameObject CastVFX; 
        public GameObject HitVFX;
        public Color RangeIndicatorColor = Color.red;
        public string AnimationTriggerName = "CastSkill";

        [Header("Audio")]
        public AudioClip CastSFX;
        public AudioClip HitSFX;
    }
}
