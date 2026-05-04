using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Grid;
using MaouSamaTD.Levels;
using MaouSamaTD.Managers;

namespace MaouSamaTD.Units
{
    public class EnemyUnit : UnitBase
    {
        private EnemyData _enemyData;
        public EnemyData EnemyData => _enemyData;
        public Vector2Int GoalCoord { get; set; } = new Vector2Int(-1, -1);

        private GridManager _gridManager;

        private Queue<Tile> _path;
        private Tile _targetTile;
        private bool _isMoving = false;
        private bool _isCentering = false;
        private PlayerUnit _blockedBy = null;
        private PlayerUnit _attackTarget = null;
        
        private bool _isCharmed = false;
        private float _charmTimer = 0f;
        private Stack<Tile> _retreatPath = new Stack<Tile>();
        private int _currentPhasingCharges;
        private List<EnemyAbility> _runtimeAbilities = new List<EnemyAbility>();

        public static System.Collections.Generic.List<EnemyUnit> ActiveEnemies = new System.Collections.Generic.List<EnemyUnit>();
        public static System.Action<EnemyUnit> OnAnyEnemyRemoved;

        private void OnEnable()
        {
            ActiveEnemies.Add(this);
        }

        private void OnDisable()
        {
            ActiveEnemies.Remove(this);
            OnAnyEnemyRemoved?.Invoke(this);
        }

        public int WaveIndex { get; private set; }
        public void Initialize(EnemyData data, int waveIndex, int enemyIndex)
        {
            _enemyData = data;
            WaveIndex = waveIndex;
            
            _maxHp = data.MaxHp;
            _currentHp = _maxHp;
            _attackPower = data.AttackPower;
            _attackInterval = data.AttackInterval;
            _defense = 0f; // Reset defense as EnemyData doesn't have it yet
            _currentPhasingCharges = data.PhasingCharges;
            
            Immunities.Clear();
            if (data.Immunities != null)
            {
                Immunities.AddRange(data.Immunities);
            }
            
            gameObject.name = $"Enemy_{data.EnemyName}_W{waveIndex}_O{enemyIndex}";
            
            UpdateVisuals();
            InitializeAbilities();
        }

        private void InitializeAbilities()
        {
            _runtimeAbilities.Clear();
            if (_enemyData != null && _enemyData.Abilities != null)
            {
                foreach (var abilitySource in _enemyData.Abilities)
                {
                    if (abilitySource == null) continue;
                    EnemyAbility instance = Instantiate(abilitySource);
                    instance.OnInitialize(this);
                    _runtimeAbilities.Add(instance);
                }
            }
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

            if (_animator != null)
            {
                if (_enemyData.AnimatorController != null)
                {
                    _animator.runtimeAnimatorController = _enemyData.AnimatorController;
                    _animator.Play("Idle", 0, 0f);
                }
                else
                {
                    Destroy(_animator);
                    _animator = null;
                }
            }

            // Apply HP Bar height from EnemyData
            if (_hpBarRoot != null)
            {
                _hpBarRoot.localPosition = new Vector3(0, _enemyData.HpBarYOffset, 0);
            }
            else if (_hpFillImage != null && _hpFillImage.canvas != null)
            {
                // Fallback for older prefabs where root isn't assigned
                _hpFillImage.canvas.transform.localPosition = new Vector3(0, _enemyData.HpBarYOffset, 0);
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
        public override void TakeDamage(float amount, UnitBase attacker = null, DamageType damageType = DamageType.Melee, bool isSkill = false)
        {
            float hpBefore = _currentHp;
            base.TakeDamage(amount, attacker, damageType, isSkill);
            float actualDamage = hpBefore - _currentHp;

            if (actualDamage > 0)
            {
                foreach (var ability in _runtimeAbilities)
                {
                    ability.OnTakeDamage(this, actualDamage, damageType);
                }
            }
        }

        public override void Die(UnitBase attacker = null)
        {
            foreach (var ability in _runtimeAbilities)
            {
                ability.OnDeath(this);
            }
            base.Die(attacker);
        }

        public void RecalculatePath()
        {
            var gridMgr = FindFirstObjectByType<GridManager>();
            if (gridMgr == null || _enemyData == null) return;

            Vector2Int startValues = gridMgr.WorldToGridCoordinates(transform.position);
            Vector2Int goal = GoalCoord.x != -1 ? GoalCoord : gridMgr.ExitPoint;

            Queue<Tile> newPath = gridMgr.GetPath(startValues, goal, _enemyData.MovementType);
            
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
            if (_isDead) return;
            base.UpdateInternal();

            foreach (var ability in _runtimeAbilities)
            {
                ability.OnTick(this);
            }

            if (_isCharmed)
            {
                _charmTimer -= Time.deltaTime;
                if (_charmTimer <= 0)
                {
                    _isCharmed = false;
                    _spriteRenderer.color = _enemyData != null ? _enemyData.Tint : Color.white;
                    RecalculatePath(); // Find way back to exit
                }
            }
            
            // Re-evaluating blockers/targets
            if (!_isCharmed)
            {
                if (_blockedBy != null)
                {
                    if (_blockedBy.CurrentHp <= 0 || _blockedBy.IsDead)
                    {
                        ReleaseBlock();
                    }
                    else
                    {
                        HandleAttack(_blockedBy);
                        FaceTarget(_blockedBy.transform.position);
                        // Stay blocked
                    }
                }
                else
                {
                    // Scan for targets even while moving
                    if (ScanForTarget(out PlayerUnit nextTarget))
                    {
                        bool shouldSwitch = false;
                        if (_attackTarget == null)
                        {
                            shouldSwitch = true;
                        }
                        else if (nextTarget != _attackTarget)
                        {
                            // Priority logic for switching
                            var curPos = _gridManager.WorldToGridCoordinates(_attackTarget.transform.position);
                            var curTile = _gridManager.GetTileAt(curPos);
                            var nextPos = _gridManager.WorldToGridCoordinates(nextTarget.transform.position);
                            var nextTile = _gridManager.GetTileAt(nextPos);
                            
                            bool curIsHigh = curTile != null && curTile.IsHighGround;
                            bool nextIsHigh = nextTile != null && nextTile.IsHighGround;
                            
                            if (nextIsHigh && !curIsHigh) shouldSwitch = true;
                            else if (GetDamageFrom(nextTarget) > GetDamageFrom(_attackTarget) + 20f) shouldSwitch = true;
                            else if (!IsTargetInPattern(_gridManager.WorldToGridCoordinates(transform.position), curPos, _enemyData.AttackPattern, Range)) shouldSwitch = true;
                        }

                        if (shouldSwitch) _attackTarget = nextTarget;

                        if (_attackTarget != null)
                        {
                            Vector2Int myPos = _gridManager.WorldToGridCoordinates(transform.position);
                            Vector2Int targetPos = _gridManager.WorldToGridCoordinates(_attackTarget.transform.position);
                            
                            if (IsTargetInPattern(myPos, targetPos, _enemyData.AttackPattern, Range))
                            {
                                HandleAttack(_attackTarget);
                                FaceTarget(_attackTarget.transform.position);
                                
                                // If melee and not flying, stop to attack? 
                                // User said 'able to attack if on same tile', suggesting they might keep moving.
                                // For now, let's keep them moving unless blocked.
                            }
                        }
                    }
                    else
                    {
                        _attackTarget = null;
                    }
                }
            }

            if (_isMoving)
            {
                 MoveTowardsTarget();
            }

            // Abilities are already ticked at the start of UpdateInternal
        }

        private bool ScanForTarget(out PlayerUnit target)
        {
            target = null;
            if (_gridManager == null) _gridManager = FindFirstObjectByType<GridManager>();
            if (_gridManager == null) return false;

            Collider[] hits = Physics.OverlapSphere(transform.position, Range);
            PlayerUnit bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var hit in hits)
            {
                var unit = hit.GetComponent<PlayerUnit>();
                if (unit != null && unit.CurrentHp > 0 && !unit.IsDead)
                {
                    Vector2Int myPos = _gridManager.WorldToGridCoordinates(transform.position);
                    Vector2Int targetPos = _gridManager.WorldToGridCoordinates(unit.transform.position);
                    
                    // Flying units only attack high ground, UNLESS they are on the same tile as the target (passing through)
                    var targetTile = _gridManager.GetTileAt(targetPos);
                    if (_enemyData.MovementType == EnemyMovementType.Flying)
                    {
                        if (myPos != targetPos && (targetTile == null || !targetTile.IsHighGround))
                        {
                            continue;
                        }
                    }

                    if (IsTargetInPattern(myPos, targetPos, _enemyData != null ? _enemyData.AttackPattern : AttackPattern.All, Range))
                    {
                        float score = 0;
                        
                        // Priority 1: High Ground
                        if (targetTile != null && targetTile.IsHighGround)
                            score += 2000f;
                            
                        // Priority 2: Same Lane (Row or Column)
                        if (myPos.x == targetPos.x || myPos.y == targetPos.y)
                            score += 500f;

                        // Priority 3: Damage Aggro (Weight based on damage taken)
                        score += GetDamageFrom(unit);

                        // Priority 4: Proximity (closer is better)
                        score -= Vector3.Distance(transform.position, unit.transform.position);

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestTarget = unit;
                        }
                    }
                }
            }

            if (bestTarget != null)
            {
                target = bestTarget;
                return true;
            }
            return false;
        }

        protected override void RegisterAttacker(UnitBase attacker)
        {
            if (attacker is PlayerUnit player && player.CurrentHp > 0)
            {
                // Aggro logic: if we don't have a target, or if the current target isn't on high ground but the attacker is
                if (_attackTarget == null && _blockedBy == null)
                {
                    _attackTarget = player;
                    if (_isMoving) InitiateCentering();
                }
                else if (_attackTarget != null)
                {
                    var attPos = _gridManager.WorldToGridCoordinates(player.transform.position);
                    var attTile = _gridManager.GetTileAt(attPos);
                    var curPos = _gridManager.WorldToGridCoordinates(_attackTarget.transform.position);
                    var curTile = _gridManager.GetTileAt(curPos);

                    bool attIsHigh = attTile != null && attTile.IsHighGround;
                    bool curIsHigh = curTile != null && curTile.IsHighGround;

                    // Switch if attacker is on high ground and current target isn't
                    if (attIsHigh && !curIsHigh)
                    {
                        _attackTarget = player;
                    }
                    else
                    {
                        // Check if the attacker has dealt enough damage to "earn" a target switch
                        float damageFromAttacker = GetDamageFrom(player);
                        float damageFromCurrent = GetDamageFrom(_attackTarget);
                        
                        if (damageFromAttacker > damageFromCurrent + 20f)
                        {
                            _attackTarget = player;
                        }
                    }
                }
            }
        }

         private void FaceTarget(Vector3 targetPos)
         {
              if (_spriteRenderer == null) return;

              float diff = targetPos.x - transform.position.x;
              if (Mathf.Abs(diff) < 0.05f) return;

              bool isTargetRight = diff > 0;
              
              Vector3 currentScale = transform.localScale;
              // Default facing is Left (+1). To face Right, use -1.
              currentScale.x = isTargetRight ? -1f : 1f;
              transform.localScale = currentScale;
              
              if (_showDebugLogs) Debug.Log($"[Facing] {gameObject.name} facing {(isTargetRight ? "Right (+x)" : "Left (-x)")}. Target X: {targetPos.x:F2}, My X: {transform.position.x:F2}");
         }

        private void HandleAttack(UnitBase target)
        {
            if (target == null) return;
            if (Time.deltaTime <= 0f) return; // Don't attack while time is paused
            if (Time.time >= _lastAttackTime + _attackInterval)
            {
                _lastAttackTime = Time.time;
                if (_animator != null) _animator.Play("Attack", 0, 0f);
                target.TakeDamage(_attackPower, this, DamageType.Melee);

                foreach (var ability in _runtimeAbilities)
                {
                    ability.OnAttack(this, target as UnitBase);
                }
            }
        }

        private void MoveTowardsTarget()
        {
            if (_enemyData == null || _targetTile == null) return;
            if (Time.deltaTime <= 0f) return; // Game paused — skip movement and avoid log spam

            // 1. Check for range-based targets if not already centering/blocked
            // BYPASS: If we have phasing charges or Bypass evasion, we ignore units to reach the exit
            // NEW: If OnlyAttackIfBlocked is true, we ONLY target if we are actually blocked (see block detection below)
            bool isPhasing = _currentPhasingCharges > 0 || _enemyData.EvasionType == EnemyEvasionType.BypassBlockers;
            
            if (!_isCentering && _blockedBy == null && !_isCharmed && !_enemyData.OnlyAttackIfBlocked)
            {
                if (ScanForTarget(out PlayerUnit target))
                {
                    _attackTarget = target;
                    
                    // Only stop (InitiateCentering) if we are NOT phasing/bypassing
                    // This allows the boss to attack Ignis while passing through him.
                    if (!isPhasing)
                    {
                        InitiateCentering();
                        return;
                    }
                }
            }

            // 2. Check for blockers in moving path
            if (!_isCentering && _enemyData.MovementType != EnemyMovementType.Flying && 
                _enemyData.CollisionType == EnemyCollisionType.BlockedByPlayer && !_isCharmed)
            {
                if (_targetTile.IsOccupied && _targetTile.Occupant is PlayerUnit player)
                {
                    bool canEvade = false;
                    
                    if (_enemyData.EvasionType == EnemyEvasionType.BypassBlockers) 
                    {
                        canEvade = true;
                    }
                    else if (_enemyData.EvasionType == EnemyEvasionType.IgnoreIfTargetAttacking && player.IsAttacking())
                    {
                        canEvade = true;
                    }
                    else if (_currentPhasingCharges > 0)
                    {
                        canEvade = true;
                    }

                    if (canEvade)
                    {
                        if (_showDebugLogs) Debug.Log($"{gameObject.name} Evasion/Phasing active: passing through {player.name}...");
                    }
                    else if (_enemyData.EvasionType == EnemyEvasionType.AttackBehind)
                    {
                        // Logic: Jump over the blocker to the next tile in path
                        if (_path.Count > 0)
                        {
                            Grid.Tile nextTile = _path.Dequeue(); // This is the tile the player is on
                            if (_path.Count > 0)
                            {
                                Grid.Tile jumpTile = _path.Dequeue(); // This is the tile BEHIND the player
                                transform.position = jumpTile.transform.position; // Teleport
                                _targetTile = jumpTile;
                                if (_showDebugLogs) Debug.Log($"{gameObject.name} Teleported behind {player.name} to {jumpTile.Coordinate}!");
                                return;
                            }
                        }
                    }
                    else
                    {
                        _blockedBy = player;
                        player.NotifyEncounter(); // Trigger reach tutorial logic
                        InitiateCentering();
                        return;
                    }
                }
            }

            Vector3 targetPos = _targetTile.transform.position;
            float speedMultiplier = _isCharmed ? 0.5f : 1f; // Move slower when charmed
            float step = _enemyData.MoveSpeed * speedMultiplier * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

            Vector3 dir = (targetPos - transform.position);
            if (Mathf.Abs(dir.x) > 0.05f)
            {
                 FaceTarget(transform.position + (_isCharmed ? -dir : dir));
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

                // If we just reached an occupied tile while phasing, decrement charges
                if (_targetTile.IsOccupied && _targetTile.Occupant is PlayerUnit && _currentPhasingCharges > 0)
                {
                    _currentPhasingCharges--;
                    if (_showDebugLogs) Debug.Log($"{gameObject.name} Passed through unit! Charges left: {_currentPhasingCharges}");
                    
                    if (_currentPhasingCharges <= 0)
                    {
                        // Becomes vulnerable again
                        if (Immunities.Contains(DamageType.Melee))
                        {
                            Immunities.Remove(DamageType.Melee);
                            if (_showDebugLogs) Debug.Log($"{gameObject.name} Phasing ended. MELEE IMMUNITY REMOVED (Vulnerable)!");
                        }
                    }
                }

                if (_isCharmed)
                {
                    if (_retreatPath.Count > 0)
                    {
                        _targetTile = _retreatPath.Pop();
                    }
                    else
                    {
                        // Reached a spawn? Just stop until charm wears off
                        _isMoving = false;
                    }
                }
                else if (_path.Count > 0)
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
            if (_showDebugLogs) Debug.Log($"Enemy reached exit! Dealing {(int)_enemyData.DamageToPlayerBase} damage.");
            
            var gm = FindFirstObjectByType<MaouSamaTD.Managers.GameManager>();
            if (gm != null)
            {
                gm.EnemyEscaped(this);
            }

            GridManager gridMgr = FindFirstObjectByType<GridManager>(); 
            if (gridMgr != null) gridMgr.SetTileType(_targetTile.Coordinate, TileType.ExitPoint); 
            
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

        public void ApplyCharm(float duration)
        {
            _isCharmed = true;
            _charmTimer = duration;
            _isMoving = true;
            _isCentering = false;
            _blockedBy = null;
            _attackTarget = null;
            
            // Visual feedback
            _spriteRenderer.color = new Color(1f, 0.5f, 0.8f, 1f); // Pinkish tint

            // Calculate retreat path (reverse of current position to start/spawn)
            if (_gridManager == null) _gridManager = FindFirstObjectByType<GridManager>();
            if (_gridManager != null)
            {
                Vector2Int currentCoord = _gridManager.WorldToGridCoordinates(transform.position);
                // Simple version: just use BFS but aim for nearest spawn point instead of exit
                Queue<Tile> retreatQueue = _gridManager.GetPath(currentCoord, _gridManager.SpawnPoint, _enemyData.MovementType, true);
                if (retreatQueue != null)
                {
                    _retreatPath.Clear();
                    // We need a Stack because GetPath gives tiles in order (Spawn -> Exit), 
                    // and we want them in order (Current -> Spawn).
                    // Actually GetPath returns start -> end. 
                    // If we pass current -> spawn, it returns current's neighbor -> spawn.
                    // So we can just use the queue but we need to dequeue carefully.
                    // Wait, stacks are better if we want to reverse a path. 
                    // If GetPath gives [T1, T2, T3] (where T3 is spawn), then it's already in the right order for retreat.
                    
                    // Let's re-use the Queue but call it _retreatQueue for clarity?
                    // Or keep it simple and just use the same Stack logic.
                    List<Tile> tiles = new List<Tile>(retreatQueue);
                    _retreatPath.Clear();
                    tiles.Reverse(); // So spawn is at bottom
                    foreach(var t in tiles) _retreatPath.Push(t);
                    
                    if (_retreatPath.Count > 0)
                        _targetTile = _retreatPath.Pop();
                }
            }
        }

        public void SetPhasingCharges(int charges)
        {
            _currentPhasingCharges = charges;
            if (_showDebugLogs) Debug.Log($"{gameObject.name} Phasing Charges set to: {charges}");
            
            if (charges > 0)
            {
                // Clear any engagement so it continues moving immediately
                _attackTarget = null;
                _blockedBy = null;
                _isMoving = true;
                _isCentering = false;
                
                // If it was attacking, it might be in an attack animation, but it will resume moving on next tick
                if (_animator != null)
                {
                     _animator.SetBool("IsAttacking", false);
                }
                
                RecalculatePath();
            }
        }
    }
}
