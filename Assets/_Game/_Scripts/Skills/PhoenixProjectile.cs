using UnityEngine;
using MaouSamaTD.Units;
using System.Collections.Generic;
using System.Linq;

namespace MaouSamaTD.Skills
{
    public class PhoenixProjectile : UltimateEffect
    {
        [Header("Projectile Settings")]
        [SerializeField] private float _speed = 15f;
        [SerializeField] private float _damage = 150f;
        [SerializeField] private float _lifetime = 8f;
        [SerializeField] private float _hitWidth = 1.0f; // Width of the grid lane
        [SerializeField] private LayerMask _enemyLayer;

        [Header("Debug")]
        [SerializeField] private bool _showDebugLogs = true;

        private Vector3 _direction;
        private PlayerUnit _owner;
        private HashSet<EnemyUnit> _hitEnemies = new HashSet<EnemyUnit>();
        private float _spawnTime;

        // Lane tracking
        private bool _isColumn; // True if constant X, varying Z
        private float _laneCoordinate; // The fixed X or Z coordinate

        public override void Execute(PlayerUnit owner, Vector3 direction)
        {
            _owner = owner;
            _direction = direction.normalized;
            _spawnTime = Time.time;

            // Determine if we are on a Column or Row based on direction
            // If movement is mostly along Z, it's a Column (constant X)
            // If movement is mostly along X, it's a Row (constant Z)
            if (Mathf.Abs(_direction.z) > Mathf.Abs(_direction.x))
            {
                _isColumn = true;
                _laneCoordinate = transform.position.x;
            }
            else
            {
                _isColumn = false;
                _laneCoordinate = transform.position.z;
            }

            // Rotate Sprite (Facing Left by default -> offset 180)
            float angle = Mathf.Atan2(_direction.z, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(90, angle + 180f, 0); // 90 on X for top-down visibility
            
            if (_showDebugLogs) Debug.Log($"[Phoenix] Executing on {(_isColumn ? "Column X=" : "Row Z=")}{_laneCoordinate}");
        }

        private void Update()
        {
            if (Time.time > _spawnTime + _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 prevPos = transform.position;
            transform.position += _direction * _speed * Time.deltaTime;

            // Damage Logic: Hit all enemies in the designated lane
            foreach (var enemy in EnemyUnit.ActiveEnemies.ToArray())
            {
                if (enemy == null || _hitEnemies.Contains(enemy)) continue;

                Vector3 enemyPos = enemy.transform.position;
                bool inLane = false;
                float progress = 0;

                if (_isColumn)
                {
                    // Check if enemy is in the same column (X)
                    if (Mathf.Abs(enemyPos.x - _laneCoordinate) < _hitWidth)
                    {
                        // Check if phoenix has reached the enemy's Z
                        float distToEnemy = enemyPos.z - transform.position.z;
                        if (Vector3.Dot(_direction, enemyPos - transform.position) < 0) // Phoenix has passed it or is at it
                        {
                            inLane = true;
                        }
                    }
                }
                else
                {
                    // Check if enemy is in the same row (Z)
                    if (Mathf.Abs(enemyPos.z - _laneCoordinate) < _hitWidth)
                    {
                        // Check progress
                        if (Vector3.Dot(_direction, enemyPos - transform.position) < 0)
                        {
                            inLane = true;
                        }
                    }
                }

                if (inLane)
                {
                    enemy.TakeDamage(_damage, _owner, DamageType.Magic, true);
                    _hitEnemies.Add(enemy);
                    if (_showDebugLogs) Debug.Log($"[Phoenix] Lane hit {enemy.name}");
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _hitWidth);
        }
    }
}
