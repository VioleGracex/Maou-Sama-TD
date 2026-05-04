using UnityEngine;
using MaouSamaTD.Skills;

namespace MaouSamaTD.Grid
{
    [System.Serializable]
    public class TileEffect
    {
        public string SkillID;
        public SkillEffectType Type;
        public SkillStatType Stat; // Deprecated but keeping for simple logic if needed
        public System.Collections.Generic.List<StatModifier> Modifiers;
        public float Value;
        public float Duration;
        public float RemainingTime;
        public GameObject VFXInstance;

        public bool IsExpired => RemainingTime <= 0;

        public TileEffect(SovereignRiteData skill, GameObject vfx = null)
        {
            SkillID = skill.name;
            Type = skill.EffectType;
            Modifiers = new System.Collections.Generic.List<StatModifier>(skill.Modifiers);
            Value = skill.Value;
            Duration = skill.Duration;
            RemainingTime = skill.Duration;
            VFXInstance = vfx;
        }
    }
}
