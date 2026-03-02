using UnityEngine;
using MaouSamaTD.Units;

namespace MaouSamaTD.Skills
{
    /// <summary>
    /// Base class for all logic-driven ultimate effects attached to prefabs.
    /// This handles unique behaviors like projectiles, area buffs/heals, or global effects.
    /// </summary>
    public abstract class UltimateEffect : MonoBehaviour
    {
        /// <summary>
        /// Executes the ultimate effect.
        /// </summary>
        /// <param name="caster">The player unit that cast the skill.</param>
        /// <param name="direction">The direction chosen for the skill (if applicable).</param>
        public abstract void Execute(PlayerUnit caster, Vector3 direction);
    }
}
