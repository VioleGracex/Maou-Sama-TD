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
        BlockedByPlayer,
        IgnorePlayer
    }

    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "MaouSamaTD/Enemy Data")]
    public class EnemyData : MaouSamaTD.Core.GameDataSO
    {
        [Header("Identity")]
        public string EnemyName;
        public Sprite EnemySprite;
        public RuntimeAnimatorController AnimatorController;

        [Header("Stats")]
        public float MaxHp = 50f;
        public float MoveSpeed = 2f;
        public float AttackPower = 5f; // Duration damage or hit damage?
        public float AttackInterval = 1.0f; 
        public float AttackRange = 0.5f;
        public float DamageToPlayerBase = 1f;

        [Header("Combat Pattern")]
        public AttackPattern AttackPattern = AttackPattern.All;

        [Header("Behavior")]
        public EnemyMovementType MovementType;
        public EnemyCollisionType CollisionType;
        public int PhasingCharges = 0;
        public System.Collections.Generic.List<DamageType> Immunities = new System.Collections.Generic.List<DamageType>();

        [Header("Rewards")]
        public int CurrencyReward = 10;
        
        [Header("Visuals")]
        public Color Tint = Color.white; // Optional tint
        public float VisualYOffset = 0f; // Offset for sprite height (e.g. to stand on top of tiles)
        public float BaseVisualHeight = 1f; // Base height to lift sprite (default 1 to sit on tile)
        public float HpBarYOffset = 2f; // New field to control HP bar float height
    }
}
