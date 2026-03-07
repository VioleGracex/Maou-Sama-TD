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
        [SerializeField] private float _startDelay = 1.0f; // Fallback delay if no animator
        [SerializeField] private float _hitWidth = 1.0f; // Width of the grid lane
        [SerializeField] private LayerMask _enemyLayer;

        [Header("Components")]
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Debug")]
        [SerializeField] private bool _showDebugLogs = true;

        private Vector3 _direction;
        private PlayerUnit _owner;
        private HashSet<EnemyUnit> _hitEnemies = new HashSet<EnemyUnit>();
        private float _spawnTime;
        private bool _hasStartedDash = false;
        private bool _isFlipped;

        // Lane tracking
        private bool _isColumn; // True if constant X, varying Z
        private float _laneCoordinate; // The fixed X or Z coordinate

        public override void Execute(PlayerUnit owner, Vector3 direction)
        {
            _owner = owner;
            _direction = direction.normalized;
            _spawnTime = Time.time;

            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (_animator != null) _animator.Play("Rise", 0, 0f);

            // Determine if we are on a Column or Row based on direction
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

            // Orientation logic: Sprites face Left by default.
            // "flip it horizontallty if going right" (x > 0)
            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipX = _direction.x > 0;
            }
            
            // Still allow small rotation for lane alignment if needed, 
            // but the user specifically asked for flipping for "facing".
            float angle = Mathf.Atan2(_direction.z, _direction.x) * Mathf.Rad2Deg;
            
            // If movement is horizontal, we use flipX. If movement is vertical, 
            // we might still need rotation or a different sprite.
            // For now, let's keep it simple: Rotate to face movement, then flip if right.
            // Wait, if we use flipX, we should probably NOT rotate 180 degrees.
            // Offset 180 makes it face movement direction IF it faces Left by default.
            // Let's refine:
            transform.rotation = Quaternion.Euler(90, angle + 180f, 0); 
            
            if (_showDebugLogs) Debug.Log($"[Phoenix] Executing. FlipX: {(_spriteRenderer != null ? _spriteRenderer.flipX.ToString() : "N/A")}");
        }

        private void Update()
        {
            float elapsed = Time.time - _spawnTime;

            if (elapsed > _lifetime + _startDelay + 2f) // Extra buffer
            {
                Destroy(gameObject);
                return;
            }

            // Waiting in place during Rise phase
            if (!_hasStartedDash)
            {
                bool isAnimationDone = false;
                if (_animator != null)
                {
                    var animState = _animator.GetCurrentAnimatorStateInfo(0);
                    // If we are in Rise state, wait for it to finish.
                    // If we already finished it or switched, continue.
                    if (animState.IsName("Rise"))
                    {
                        if (animState.normalizedTime < 0.95f) return; // Still rising
                        isAnimationDone = true;
                    }
                    else if (animState.IsName("Dash"))
                    {
                        isAnimationDone = true; // Already dashed?
                    }
                    else
                    {
                        // Some other state, maybe transition is slow. Wait for delay.
                        if (elapsed < _startDelay) return;
                        isAnimationDone = true;
                    }
                }
                else
                {
                    if (elapsed < _startDelay) return;
                    isAnimationDone = true;
                }

                if (isAnimationDone)
                {
                    _hasStartedDash = true;
                    if (_animator != null) _animator.Play("Dash", 0, 0f);
                    if (_showDebugLogs) Debug.Log("[Phoenix] Rise animation finished, starting Dash phase");
                }
            }

            transform.position += _direction * _speed * Time.deltaTime;

            // Damage Logic: Hit all enemies in the designated lane
            foreach (var enemy in EnemyUnit.ActiveEnemies.ToArray())
            {
                if (enemy == null || _hitEnemies.Contains(enemy)) continue;

                Vector3 enemyPos = enemy.transform.position;
                bool inLane = false;

                if (_isColumn)
                {
                    if (Mathf.Abs(enemyPos.x - _laneCoordinate) < _hitWidth)
                    {
                        if (Vector3.Dot(_direction, enemyPos - transform.position) < 0) 
                        {
                            inLane = true;
                        }
                    }
                }
                else
                {
                    if (Mathf.Abs(enemyPos.z - _laneCoordinate) < _hitWidth)
                    {
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
