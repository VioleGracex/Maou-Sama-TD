using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Grid;

namespace MaouSamaTD.Units
{
    public class EnemyUnit : UnitBase
    {
        private EnemyData _enemyData;
        public EnemyData EnemyData => _enemyData;

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
            }
        }

        public void RecalculatePath()
        {
            var gridMgr = FindObjectOfType<GridManager>();
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
               }
            }
        }

        protected override void UpdateInternal()
        {
            base.UpdateInternal();
            
            if (_blockedBy != null)
            {
                if (_blockedBy == null || _blockedBy.CurrentHp <= 0)
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

        private bool ScanForTarget()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, Range);
            foreach (var hit in hits)
            {
                var unit = hit.GetComponent<PlayerUnit>();
                if (unit != null && unit.CurrentHp > 0)
                {
                    HandleAttack(unit);
                    FaceTarget(unit.transform.position);
                    return true;
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
             _spriteRenderer.flipX = isTargetRight;
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

            if (_blockedBy == null)
            {
               if (ScanForTarget()) 
               {
                   return; 
               }
            }

            if (_enemyData.MovementType != EnemyMovementType.Flying && 
                _enemyData.CollisionType == EnemyCollisionType.BlockedByPlayer)
            {
                if (_targetTile != null && _targetTile.IsOccupied && _targetTile.Occupant is PlayerUnit player)
                {
                    SetBlockedBy(player);
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

            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
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
            Debug.Log($"Enemy reached exit! Dealing {(int)_enemyData.DamageToPlayerBase} damage.");
            
            Managers.GameManager gm = FindObjectOfType<Managers.GameManager>();
            if (gm != null)
            {
                gm.TakeBaseDamage(Mathf.RoundToInt(_enemyData.DamageToPlayerBase));
            }

            GridManager gridMgr = FindObjectOfType<GridManager>(); 
            if (gridMgr != null) gridMgr.SetTileType(_targetTile.Coordinate, TileType.Exit); 
            
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
