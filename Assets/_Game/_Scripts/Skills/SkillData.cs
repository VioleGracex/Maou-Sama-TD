using UnityEngine;

namespace MaouSamaTD.Skills
{
    public enum SkillTargetType
    {
        Unit,       // Targets a single unit (Friendly or Enemy depending on effect)
        Tile,       // Targets a specific block/tile on the grid
        None        // Instant global effect (if needed)
    }

    public enum SkillEffectType
    {
        Damage,
        Buff,
        Debuff,
        Zone // Persistent area effect
    }

    public enum SkillStatType
    {
        None,
        Attack,
        Defense,
        AttackSpeed,
        Range,
        Health,
        MovementSpeed // Future proofing
    }

    [System.Serializable]
    public struct StatModifier
    {
        public SkillStatType Stat;
        [Tooltip("Value in percentage. 50 means +50% for buffs or -50% for debuffs.")]
        public float Value;
    }

    public enum SkillPersistenceType
    {
        [Tooltip("One-time effect applied to units in the target area immediately.")]
        Instant,
        
        [Tooltip("Creates a lingering zone on the grid tiles. Any unit currently on or entering these tiles during the duration will receive the effect.")]
        Persistent
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
        public Color TitleTextColor;
        public Color NameBgColor;
        public Color NameTextColor;
        public Color SkillNameBgColor;
        public Color SkillNameTextColor;
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
