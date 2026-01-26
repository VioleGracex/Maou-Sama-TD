using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Grid;

namespace MaouSamaTD.Units
{
    public class EnemyUnit : UnitBase
    {
        private EnemyData _enemyData;
        public EnemyData EnemyData => _enemyData;

        // Runtime stats override if needed, otherwise use base
        
        private Queue<Tile> _path;
        private Tile _targetTile;
        private bool _isMoving = false;
        private PlayerUnit _blockedBy = null;

        public static System.Collections.Generic.List<EnemyUnit> ActiveEnemies = new System.Collections.Generic.List<EnemyUnit>();

        private void OnEnable()
        {
            ActiveEnemies.Add(this);
        }

        private void OnDisable()
        {
            ActiveEnemies.Remove(this);
        }

        public void Initialize(EnemyData data)
        {
            _enemyData = data;
            
            // Set Base Stats
            _maxHp = data.MaxHp;
            _currentHp = _maxHp;
            _attackPower = data.AttackPower;
            _attackInterval = data.AttackInterval;
            
            name = data.EnemyName;
            
            UpdateVisuals();
        }

        protected override void UpdateVisuals()
        {
            if (_enemyData == null) return;

            if (_enemyData.EnemySprite != null)
            {
                _spriteRenderer.sprite = _enemyData.EnemySprite;
                _spriteRenderer.color = _enemyData.Tint;
                _spriteRenderer.enabled = true;
                if (_textFallback != null) _textFallback.gameObject.SetActive(false);
            }
            else
            {
                _spriteRenderer.enabled = false;
                if (_textFallback != null)
                {
                    _textFallback.gameObject.SetActive(true);
                    if (!string.IsNullOrEmpty(_enemyData.EnemyName))
                        _textFallback.text = _enemyData.EnemyName.Substring(0, 1).ToUpper();
                    else
                        _textFallback.text = "E";
                    _textFallback.color = Color.red; 
                }
            }
        }

        public override float Range => 0.5f; 

        public void SetPath(Queue<Tile> path)
        {
            _path = path;
            if (_path != null && _path.Count > 0)
            {
                _targetTile = _path.Dequeue();
                _isMoving = true;
            }
        }

        protected override void UpdateInternal()
        {
            base.UpdateInternal();
            
            if (_blockedBy != null)
            {
                if (_blockedBy == null || _blockedBy.CurrentHp <= 0) // Blocker died
                {
                    ReleaseBlock();
                }
                else
                {
                    HandleAttack(_blockedBy);
                    return;
                }
            }

            if (_isMoving && _targetTile != null)
            {
                MoveTowardsTarget();
            }
        }

        private void HandleAttack(UnitBase target)
        {
            if (Time.time >= _lastAttackTime + _attackInterval)
            {
                _lastAttackTime = Time.time;
                target.TakeDamage(_attackPower);
            }
        }

        private void MoveTowardsTarget()
        {
            if (_enemyData == null) return;

            // Collision / Blocking Logic
            if (_enemyData.MovementType != EnemyMovementType.Flying && 
                _enemyData.CollisionType == EnemyCollisionType.BlockedByPlayer)
            {
                // Check if we are about to enter a tile with a blocker
                if (_targetTile != null && _targetTile.IsOccupied && _targetTile.Occupant is PlayerUnit player)
                {
                    // Check distance
                    float distToBlocker = Vector3.Distance(transform.position, player.transform.position);
                    if (distToBlocker < 0.8f) // Slightly larger than range to stop "before" passing through? Or at overlapping.
                    {
                        // Check if player can block
                        // For simply logic: if Occupied, it blocks. 
                        // Advanced: Use player.BlockCount vs enemies blocked.
                        
                        SetBlockedBy(player);
                        return;
                    }
                }
            }

            Vector3 targetPos = _targetTile.transform.position;
            Vector3 currentPos = transform.position;
            Vector3 dir = (targetPos - currentPos).normalized;
            dir.y = 0; 

            transform.position += dir * _enemyData.MoveSpeed * Time.deltaTime;

            if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(targetPos.x, 0, targetPos.z)) < 0.1f)
            {
                if (_path.Count > 0)
                {
                    _targetTile = _path.Dequeue();
                }
                else
                {
                    ReachedExit();
                }
            }
        }

        private void ReachedExit()
        {
            _isMoving = false;
            Debug.Log($"Enemy reached exit! Dealing {_enemyData.DamageToPlayerBase} damage.");
            GridManager gm = FindObjectOfType<GridManager>(); 
            if (gm != null) gm.SetTileType(_targetTile.Coordinate, TileType.Exit); 
            
            Destroy(gameObject);
        }

        public void SetBlockedBy(PlayerUnit blocker)
        {
            _blockedBy = blocker;
            _isMoving = false;
        }

        public void ReleaseBlock()
        {
            _blockedBy = null;
            _isMoving = true;
        }
    }
}
