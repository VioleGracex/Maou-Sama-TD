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
                    // Continue to center logic if needed, but usually Attack stops movement.
                    // If we want to center while attacking: Check distance to center.
                    // But _isMoving is false.
                    // We can enable specific "centering" move?
                    // Proper fix: When Blocked, we check if we are AT Center.
                    // If not, we allow movement to center.
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
                    // Fix: Before stopping, ensure we are centered on the CURRENT tile (or the one we are entering).
                    // Actually, if we are entering a tile with a player, we should stop at the EDGE or CENTER of PREVIOUS?
                    // Usually Center of PREVIOUS (Current).
                    // If _targetTile is the one with the Player, we haven't reached it yet.
                    // So we should Stop moving to _targetTile and stay on Current.
                    
                    // But if we are "Moving Towards Target" (L127), we are interpolating.
                    // If we act now, we freeze in place.
                    
                    // Logic: Retarget to Center of Tile we are CLOSEST to (or currently occupying).
                    // And SetBlockedBy to stop Logic AFTER reaching it.
                    
                    // Simple Fix: Just set blocked. The user complained about "adding to range".
                    // If we stop early, we are further away -> Less Range usage? No, range is from Unit.
                    // If we stop at edge, we might be closer to target than center?
                    // User said: "position of enemies when they stop should be center of tile"
                    
                    // Force Snap? No, visual glitch.
                    // Let's change target to Current Position's Tile Center.
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
            // Fix: Do not stop immediately if not centered.
            // Check distance to center of current tile?
            // Or just Snap for now as requested? 
            // "so it does not add to their range and make a hidden variable" implies consisteny is key.
            // Snapping is consistent.
            
            // Allow finishing the move to center?
            // If I set _isMoving = false, Update stops.
            // Let's Snap to Grid Center of current position.
            
            GridManager grid = FindObjectOfType<GridManager>();
            if (grid != null)
            {
                 Vector2Int coord = grid.WorldToGridCoordinates(transform.position);
                 Vector3 center = grid.GridToWorldPosition(coord);
                 // Preserve Y
                 transform.position = new Vector3(center.x, transform.position.y, center.z);
            }

            _isMoving = false;
        }

        public void ReleaseBlock()
        {
            _blockedBy = null;
            _isMoving = true;
        }
    }
}
