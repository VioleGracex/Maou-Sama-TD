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

            // Orientation logic: Fixed (90, 90, 0) rotation.
            // This aligns World Z (Left/Right) with Local X.
            // This aligns World X (Up/Down) with Local -Y.
            transform.rotation = Quaternion.Euler(90, 90, 0); 

            Vector3 scale = transform.localScale;
            
            // Z-axis flipping (World Z = Local X)
            // Left is -Z, Right is +Z.
            // User: "phoenix image is looking left side (-Z) if it dashes right (+Z)... we flip it -1"
            if (Mathf.Abs(_direction.z) > 0.1f)
            {
                // If moving Left (-Z), flip to look Left (Negative Local X Scale)
                scale.x = _direction.z < 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            }

            // X-axis flipping (World X = Local -Y)
            if (Mathf.Abs(_direction.x) > 0.1f)
            {
                // If moving in +X, we flip scale.y
                scale.y = _direction.x > 0 ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y);
            }

            transform.localScale = scale;
            
            if (_showDebugLogs) Debug.Log($"[Phoenix] Executing. Direction: {_direction}, Scale: {transform.localScale}, Rot: {transform.rotation.eulerAngles}");
        }
        
        private bool _isWaitingAfterRise = false;
        private float _waitStartTime;

        private void Update()
        {
            float elapsed = Time.time - _spawnTime;

            if (elapsed > _lifetime + _startDelay + 5f) // Extra buffer
            {
                Destroy(gameObject);
                return;
            }

            // Phase 1 & 2: Rise and Stay in place
            if (!_hasStartedDash)
            {
                if (!_isWaitingAfterRise)
                {
                    if (_animator != null)
                    {
                        var animState = _animator.GetCurrentAnimatorStateInfo(0);
                        // If we are in Rise state, wait for it to finish.
                        if (animState.IsName("Rise"))
                        {
                            if (animState.normalizedTime < 0.95f) return; // Keep waiting
                        }
                    }
                    else
                    {
                        // Fallback if no animator
                        if (elapsed < _startDelay) return;
                    }

                    // Animation done, start the 1s delay
                    _isWaitingAfterRise = true;
                    _waitStartTime = Time.time;
                    if (_showDebugLogs) Debug.Log("[Phoenix] Rise animation done. Staying in place for 1s...");
                    return;
                }
                else
                {
                    // Phase 2: Wait 1s
                    if (Time.time - _waitStartTime < 1.0f) return;

                    // Phase 2 done, start Dash
                    _hasStartedDash = true;
                    if (_animator != null) _animator.Play("Dash", 0, 0f);
                    if (_showDebugLogs) Debug.Log("[Phoenix] Wait over. Starting Dash!");
                }
            }

            // Phase 3: Move and Damage
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
