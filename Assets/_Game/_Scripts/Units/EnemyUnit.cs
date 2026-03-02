using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Grid;

namespace MaouSamaTD.Units
{
    public class EnemyUnit : UnitBase
    {
        private EnemyData _enemyData;
        public EnemyData EnemyData => _enemyData;

        private GridManager _gridManager;

        private Queue<Tile> _path;
        private Tile _targetTile;
        private bool _isMoving = false;
        private bool _isCentering = false;
        private PlayerUnit _blockedBy = null;
        private PlayerUnit _attackTarget = null;

        public static System.Collections.Generic.List<EnemyUnit> ActiveEnemies = new System.Collections.Generic.List<EnemyUnit>();

        private void OnEnable()
        {
            ActiveEnemies.Add(this);
        }

        private void OnDisable()
        {
            ActiveEnemies.Remove(this);
        }

        public void Initialize(EnemyData data, int waveIndex, int enemyIndex)
        {
            _enemyData = data;
            
            _maxHp = data.MaxHp;
            _currentHp = _maxHp;
            _attackPower = data.AttackPower;
            _attackInterval = data.AttackInterval;
            
            gameObject.name = $"Enemy_{data.EnemyName}_W{waveIndex}_O{enemyIndex}";
            
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

            if (_spriteRenderer != null && _spriteRenderer.transform != transform)
            {
                float baseHeight = _enemyData.BaseVisualHeight; 
                float finalY = baseHeight + _enemyData.VisualYOffset;
                _spriteRenderer.transform.localPosition = new Vector3(0, finalY, 0);
            }
        }

        public override float Range => _enemyData != null ? _enemyData.AttackRange : 1f; 

        public void SetPath(Queue<Tile> path)
        {
            _path = path;
            if (_path != null && _path.Count > 0)
            {
                _targetTile = _path.Dequeue();
                _isMoving = true;
                _isCentering = false;
            }
        }

        public void RecalculatePath()
        {
            var gridMgr = FindFirstObjectByType<GridManager>();
            if (gridMgr == null || _enemyData == null) return;

            Vector2Int startValues = gridMgr.WorldToGridCoordinates(transform.position);

            Queue<Tile> newPath = gridMgr.GetPath(startValues, gridMgr.ExitPoint, _enemyData.MovementType);
            
            if (newPath != null && newPath.Count > 0)
            {
               _path = newPath;
               if (_path.Count > 0)
               {
                   _targetTile = _path.Dequeue();
                   _isMoving = true;
                   _isCentering = false;
               }
            }
        }

        protected override void UpdateInternal()
        {
            base.UpdateInternal();
            
            // Re-evaluating blockers/targets while moving vs while stopped
            if (!_isMoving)
            {
                if (_blockedBy != null)
                {
                    if (_blockedBy == null || _blockedBy.CurrentHp <= 0)
                    {
                        ReleaseBlock();
                    }
                    else
                    {
                        HandleAttack(_blockedBy);
                        FaceTarget(_blockedBy.transform.position);
                    }
                    return;
                }

                if (_attackTarget != null)
                {
                    if (_attackTarget == null || _attackTarget.CurrentHp <= 0)
                    {
                        _attackTarget = null;
                        _isMoving = true;
                    }
                    else
                    {
                        HandleAttack(_attackTarget);
                        FaceTarget(_attackTarget.transform.position);
                        
                        // Periodic re-scan while attacking to ensure they are still in pattern/range
                        if (!ScanForTarget(out PlayerUnit nextTarget) || nextTarget != _attackTarget)
                        {
                            _attackTarget = nextTarget;
                        }
                    }
                    return;
                }

                // If stopped but not blocked/targeting, resume move
                _isMoving = true;
            }

            if (_isMoving)
            {
                 MoveTowardsTarget();
            }
        }

        private bool ScanForTarget(out PlayerUnit target)
        {
            target = null;
            if (_gridManager == null) _gridManager = FindFirstObjectByType<GridManager>();
            if (_gridManager == null) return false;

            Collider[] hits = Physics.OverlapSphere(transform.position, Range);
            foreach (var hit in hits)
            {
                var unit = hit.GetComponent<PlayerUnit>();
                if (unit != null && unit.CurrentHp > 0)
                {
                    Vector2Int myPos = _gridManager.WorldToGridCoordinates(transform.position);
                    Vector2Int targetPos = _gridManager.WorldToGridCoordinates(unit.transform.position);

                    if (IsTargetInPattern(myPos, targetPos, _enemyData != null ? _enemyData.AttackPattern : AttackPattern.All, Range))
                    {
                        target = unit;
                        return true;
                    }
                }
            }
            return false;
        }

        private void FaceTarget(Vector3 targetPos)
        {
             if (_spriteRenderer == null) return;

             float diff = targetPos.x - transform.position.x;
             if (Mathf.Abs(diff) < 0.05f) return;

             bool isTargetRight = diff > 0;
             _spriteRenderer.flipX = !isTargetRight; // Corrected flip for orientation
        }

        private void HandleAttack(UnitBase target)
        {
            if (target == null) return;
            if (Time.time >= _lastAttackTime + _attackInterval)
            {
                _lastAttackTime = Time.time;
                target.TakeDamage(_attackPower);
            }
        }

        private void MoveTowardsTarget()
        {
            if (_enemyData == null || _targetTile == null) return;

            // 1. Check for range-based targets if not already centering/blocked
            if (!_isCentering && _blockedBy == null)
            {
                if (ScanForTarget(out PlayerUnit target))
                {
                    _attackTarget = target;
                    InitiateCentering();
                    return;
                }
            }

            // 2. Check for blockers in moving path
            if (!_isCentering && _enemyData.MovementType != EnemyMovementType.Flying && 
                _enemyData.CollisionType == EnemyCollisionType.BlockedByPlayer)
            {
                if (_targetTile.IsOccupied && _targetTile.Occupant is PlayerUnit player)
                {
                    _blockedBy = player;
                    player.NotifyEncounter(); // Trigger reach tutorial logic
                    InitiateCentering();
                    return;
                }
            }

            Vector3 targetPos = _targetTile.transform.position;
            float step = _enemyData.MoveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

            Vector3 dir = (targetPos - transform.position);
            if (Mathf.Abs(dir.x) > 0.05f)
            {
                 FaceTarget(transform.position + dir);
            }

            if (Vector3.Distance(transform.position, targetPos) < 0.005f)
            {
                transform.position = targetPos; // Final snap

                if (_isCentering)
                {
                    _isMoving = false;
                    _isCentering = false;
                    return;
                }

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

        private void InitiateCentering()
        {
            if (_gridManager == null) _gridManager = FindFirstObjectByType<GridManager>();
            if (_gridManager != null)
            {
                Vector2Int coord = _gridManager.WorldToGridCoordinates(transform.position);
                _targetTile = _gridManager.GetTileAt(coord);
                _isCentering = true;
            }
            else
            {
                _isMoving = false;
            }
        }

        private void ReachedExit()
        {
            _isMoving = false;
            Debug.Log($"Enemy reached exit! Dealing {(int)_enemyData.DamageToPlayerBase} damage.");
            
            Managers.GameManager gm = FindFirstObjectByType<Managers.GameManager>();
            if (gm != null)
            {
                gm.TakeBaseDamage(Mathf.RoundToInt(_enemyData.DamageToPlayerBase));
            }

            GridManager gridMgr = FindFirstObjectByType<GridManager>(); 
            if (gridMgr != null) gridMgr.SetTileType(_targetTile.Coordinate, TileType.Exit); 
            
            Destroy(gameObject);
        }

        public void SetBlockedBy(PlayerUnit blocker)
        {
            _blockedBy = blocker;
            InitiateCentering();
        }

        public void ReleaseBlock()
        {
            _blockedBy = null;
            _isMoving = true;
        }
    }
}
