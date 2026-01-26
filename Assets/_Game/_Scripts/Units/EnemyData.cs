using UnityEngine;

namespace MaouSamaTD.Units
{
    public enum EnemyMovementType
    {
        Ground,
        Flying
    }

    public enum EnemyCollisionType
    {
        BlockedByPlayer,
        IgnorePlayer
    }

    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "MaouSamaTD/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string EnemyName;
        public Sprite EnemySprite;

        [Header("Stats")]
        public float MaxHp = 50f;
        public float MoveSpeed = 2f;
        public float AttackPower = 5f; // Duration damage or hit damage?
        public float AttackInterval = 1.0f; 
        public float DamageToPlayerBase = 1f;

        [Header("Behavior")]
        public EnemyMovementType MovementType;
        public EnemyCollisionType CollisionType;

        [Header("Rewards")]
        public int CurrencyReward = 10;
        
        [Header("Visuals")]
        public Color Tint = Color.white; // Optional tint
    }
}
