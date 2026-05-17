using UnityEngine;

namespace MaouSamaTD.Units
{
    public enum EnemyMovementType
    {
        Ground,
        Flying,
        Mixed
    }

    public enum EnemyCollisionType
    {
        BlockedByUnits,
        IgnoreUnits
    }

    public enum EnemyEvasionType
    {
        None,
        BypassBlockers, // Ignores all blockers
        AttackBehind,   // Teleports/Moves behind blocker instead of stopping
        IgnoreIfTargetAttacking // Only blocked if target is idle/defending, ignores if target is attacking
    }

    public enum EnemyTargetingPriority
    {
        ReachExit,    // Prioritizes moving to exit; only stops if physically blocked
        KillUnits     // Stops to kill any target in range, even if not blocking the path
    }

    public enum ExitDamageType
    {
        Value,
        Percentage
    }

    [System.Flags]
    public enum TargetableGround
    {
        None = 0,
        LowGround = 1 << 0,
        HighGround = 1 << 1
    }

    public enum EnemyCategory
    {
        None    = 0, // Will use fallback logic when dropping loot
        Shadow  = 1,
        Bandit  = 2,
        Animal  = 3,
        Golem   = 4,
        Undead  = 5,
        Demon   = 6,
    }

    public enum EnemyRank
    {
        Normal  = 0,
        Elite   = 1,
        Boss    = 2,
    }

    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "Maou-Sama-TD/Enemy Data")]
    public class EnemyData : MaouSamaTD.Core.GameDataSO
    {
        [Header("Identity")]
        public string EnemyName;
        public Sprite EnemySprite; // Chibi / In-game
        public Sprite FullBodyArt; // Portrait / Full Body
        public Sprite FullSplashArt; // Splash / Full Screen
        public bool IsBoss; // Boss unit special handling
        public RuntimeAnimatorController AnimatorController;

        [Header("Stats")]
        public float MaxHp = 50f;
        public float MoveSpeed = 2f;
        public float BlocksPerSecond => MoveSpeed; // Visual helper for design
        public float AttackPower = 5f; // Duration damage or hit damage?
        public float AttackInterval = 1.0f; 
        public float AttackRange = 0.5f;
        
        [Tooltip("The type of damage this enemy deals.")]
        public DamageType DamageType = DamageType.Melee;

        [Tooltip("Damage dealt to the Player's Nexus health when this unit reaches the exit.")]
        public float ExitDamage = 1f;
        
        [Tooltip("Whether the ExitDamage is an absolute value or a percentage of the Nexus Max HP.")]
        public ExitDamageType ExitDamageType = ExitDamageType.Value;

        [Header("Combat Pattern")]
        public AttackPattern AttackPattern = AttackPattern.All;
        public System.Collections.Generic.List<Vector2Int> CustomPatternOffsets = new System.Collections.Generic.List<Vector2Int>();

        [Header("Behavior")]
        [Tooltip("Ground: Standard pathing. Flying: High-ground/Aerial pathing (Can be blocked by high-ground units).")]
        public EnemyMovementType MovementType;
        
        [Tooltip("BlockedByUnits: Stops when encountering a player unit. IgnoreUnits: Phasing behavior.")]
        public EnemyCollisionType CollisionType;
        
        [Tooltip("None: Standard behavior. BypassBlockers: Ignores physical obstruction. AttackBehind: Teleports behind blocker.")]
        public EnemyEvasionType EvasionType;
        
        [Tooltip("ReachExit: Prioritizes movement. KillUnits: Stops to attack any unit in range.")]
        public EnemyTargetingPriority TargetingPriority = EnemyTargetingPriority.ReachExit;

        [Tooltip("CHECKED: Only stops to attack if physically blocked in its path. UNCHECKED: Stops and attacks any unit within its pattern/range (diagonal, adjacent, etc).")]
        public bool OnlyAttackIfBlocked;
        
        [Tooltip("Which types of ground tiles can this unit target and attack?")]
        public TargetableGround GroundAttackTargets = TargetableGround.LowGround;
        
        public int PhasingCharges = 0;
        public System.Collections.Generic.List<DamageType> Immunities = new System.Collections.Generic.List<DamageType>();
        
        [Header("Abilities")]
        public System.Collections.Generic.List<EnemyAbility> Abilities = new System.Collections.Generic.List<EnemyAbility>();

        [Header("Rewards")]
        public int CurrencyReward = 10;

        [Header("Classification")]
        [Tooltip("Which category of enemy this is. Used for loot drops and rank-up material assignment. Set to None to use automatic fallback.")]
        public EnemyCategory Category = EnemyCategory.None;

        [Tooltip("Rank of this enemy. Bosses always drop premium loot. Set to Boss here OR check IsBoss above.")]
        public EnemyRank Rank = EnemyRank.Normal;

        /// <summary>
        /// Returns the effective category for loot. Falls back to Golem for bosses, Bandit for standards.
        /// </summary>
        public EnemyCategory GetEffectiveCategory()
        {
            if (Category != EnemyCategory.None) return Category;
            return (IsBoss || Rank == EnemyRank.Boss) ? EnemyCategory.Golem : EnemyCategory.Bandit;
        }

        /// <summary>
        /// Returns the effective rank. IsBoss always overrides to Boss rank.
        /// </summary>
        public EnemyRank GetEffectiveRank()
        {
            if (IsBoss) return EnemyRank.Boss;
            return Rank;
        }

        [Header("Visuals")]
        public Color Tint = Color.white; // Optional tint
        public float VisualYOffset = 0f; // Offset for sprite height (e.g. to stand on top of tiles)
        public float BaseVisualHeight = 1f; // Base height to lift sprite (default 1 to sit on tile)
        public float HpBarYOffset = 2f; // New field to control HP bar float height
    }
}
