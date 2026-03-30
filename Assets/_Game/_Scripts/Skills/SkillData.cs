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
    }

    [System.Serializable]
    public struct SkillVisuals
    {
        public GameObject UltimatePrefab;
        public GameObject CastVFX;
        public GameObject HitVFX;
        public AudioClip CastSFX;
        public AudioClip HitSFX;
        public Color UltimateColor;
        public Color TitleBgColor;
        public Color SkillNameBgColor;
        public Color RangeIndicatorColor;
        public string AnimationTriggerName;
    }

    // Base class for all skills/rites - contains common display and cost info
    public abstract class SkillBase : MaouSamaTD.Core.GameDataSO
    {
        public string SkillName;
        [TextArea] public string Description;
        public Sprite Icon;
        
        public SkillVisuals BaseVisuals;
    }
}
