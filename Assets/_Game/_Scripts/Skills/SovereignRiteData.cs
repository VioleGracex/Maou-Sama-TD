using UnityEngine;
using MaouSamaTD.Data;

namespace MaouSamaTD.Skills
{
    /// <summary>How the AOE impact area is shaped at the target point.</summary>
    public enum AoeShape
    {
        Circle,     // All tiles within Radius distance (round blast)
        Square,     // All tiles inside the Radius × Radius bounding box
        Cross,      // Cardinal only: same row OR same column as target (+ shape)
        DiagonalX,  // Diagonal only: tiles on X-diagonals through target
        Star,       // Cross + DiagonalX combined (* shape)
        Custom,     // User-defined tile offsets
    }

    [CreateAssetMenu(fileName = "NewSovereignRite", menuName = "MaouSamaTD/Skills/Sovereign Rite")]
    public class SovereignRiteData : SkillBase
    {
        [Header("Identity")]
        public MaouGender Archetype;

        [Header("Global Costs")]
        public int SealCost = 50; 
        public float Cooldown = 30f; 

        [Header("Targeting")]
        public SkillTargetType TargetType;
        public float Range = 100f; 
        public float Radius = 0f;
        [Tooltip("Shape of the AOE impact area. Only matters when Radius > 0 (or for Custom).")]
        public AoeShape AoeShape = AoeShape.Circle;
        
        [HideInInspector]
        public System.Collections.Generic.List<Vector2Int> CustomShapeOffsets = new System.Collections.Generic.List<Vector2Int>();

        [Header("Effect")]
        public SkillEffectType EffectType;
        public System.Collections.Generic.List<StatModifier> Modifiers = new System.Collections.Generic.List<StatModifier>();
        public SkillPersistenceType Persistence;
        [Tooltip("Base value for Damage effect, or a global multiplier if Modifiers list is empty.")]
        public float Value; 
        public float Duration;

        [Header("VFX")]
        [Tooltip("Particle prefab spawned at the target unit/tile when this rite is applied.")]
        public GameObject BuffVFXPrefab;

        /// <summary>
        /// Checks if a target tile coordinate is within the AOE shape relative to an origin tile coordinate.
        /// </summary>
        public bool IsInShape(Vector2Int origin, Vector2Int target)
        {
            Vector2Int offset = target - origin;
            float dx = offset.x;
            float dy = offset.y;
            float dist = offset.magnitude;

            switch (AoeShape)
            {
                case AoeShape.Circle:
                    // Using a small epsilon to ensure grid-snapped distance works correctly
                    return dist <= Radius + 0.1f;
                
                case AoeShape.Square:
                    return Mathf.Abs(dx) <= Radius && Mathf.Abs(dy) <= Radius;
                
                case AoeShape.Cross:
                    return dx == 0 || dy == 0;
                
                case AoeShape.DiagonalX:
                    return Mathf.Abs(dx) == Mathf.Abs(dy);
                
                case AoeShape.Star:
                    return dx == 0 || dy == 0 || Mathf.Abs(dx) == Mathf.Abs(dy);
                
                case AoeShape.Custom:
                    // For custom shapes, we just check if the offset is in our defined list
                    return CustomShapeOffsets.Exists(o => o.x == offset.x && o.y == offset.y);
                
                default:
                    return dist <= Radius + 0.1f;
            }
        }
    }
}

