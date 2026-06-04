using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Grid;
using MaouSamaTD.Levels;
using MaouSamaTD.Managers;
using MaouSamaTD.Utils;
using DG.Tweening;

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
        public int CurrentPhasingCharges => _currentPhasingCharges;
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
        public override float Range 
        {
            get
            {
                if (_enemyData == null) return 0f;
                float baseRange = _enemyData.AttackRange;
                float mult = 1f;
                if (_activeBuffs != null)
                {
                    foreach (var b in _activeBuffs) 
                        if (b.Stat == MaouSamaTD.Skills.SkillStatType.Range) mult *= b.Multiplier;
                }
                return baseRange * mult;
            }
        }

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
        public override System.Collections.Generic.List<Vector2Int> CustomPatternOffsets => _enemyData != null ? _enemyData.CustomPatternOffsets : null;

        public void SetPath(Queue<Tile> path)
        {
            _path = path;
            if (_path != null && _path.Count > 0)
            {
                _targetTile = _path.Dequeue();
                _isMoving = true;
                _isCentering = false;
                if (_enemyData != null && _enemyData.IsBoss) Debug.Log($"[EnemyUnit] {gameObject.name} (Boss) path set with {_path.Count + 1} tiles. Target: {_targetTile.Coordinate}");
            }
            else
            {
                if (_enemyData != null && _enemyData.IsBoss) Debug.LogWarning($"[EnemyUnit] {gameObject.name} (Boss) received an EMPTY or NULL path!");
                _isMoving = false;
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
            if (_isDead) return;

            // Trigger tutorial action if this is the boss
            if (_enemyData != null && _enemyData.EnemyName == "Abyssal Shade")
            {
                var tm = FindObjectOfType<TutorialManager>();
                if (tm != null)
                {
                    tm.OnActionTriggered("BossDead");
                }
            }

            foreach (var ability in _runtimeAbilities)
            {
                ability.OnDeath(this);
            }

            if (_blockedBy != null)
            {
                _blockedBy.UnregisterBlockedEnemy(this);
            }

            // ── Loot Drop Engine ─────────────────────────────────────────────
            if (_enemyData != null)
            {
                var saveManager = FindFirstObjectByType<SaveManager>();
                if (saveManager != null)
                    RollLootDrops(saveManager);
            }

            base.Die(attacker);
        }

        private void RollLootDrops(SaveManager saveManager)
        {
            var rank = _enemyData.GetEffectiveRank();
            var cat  = _enemyData.GetEffectiveCategory();

            // Attempt to load MaouLootConfig ScriptableObject
            MaouLootConfig lootConfig = Resources.Load<MaouLootConfig>("MaouLootConfig");
            if (lootConfig == null)
            {
#if UNITY_EDITOR
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:MaouLootConfig");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    lootConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<MaouLootConfig>(path);
                }
#endif
            }

            if (lootConfig != null)
            {
                // 1. Check Special Overrides first
                if (!string.IsNullOrEmpty(_enemyData.UniqueID))
                {
                    var specialOverride = lootConfig.SpecialOverrides.Find(o => o.EnemyUniqueID == _enemyData.UniqueID && o.EnableOverride);
                    if (specialOverride != null)
                    {
                        if (!string.IsNullOrEmpty(specialOverride.CustomMaterialID) && Random.value < specialOverride.CustomMaterialChance)
                        {
                            AwardLoot(saveManager, specialOverride.CustomMaterialID, specialOverride.CustomMaterialQuantity);
                            Debug.Log($"[Loot] Special Override Defeated: {_enemyData.EnemyName}! Obtained {specialOverride.CustomMaterialQuantity}x {specialOverride.CustomMaterialID}");
                        }
                        if (!string.IsNullOrEmpty(specialOverride.CustomXpCoreID) && Random.value < specialOverride.CustomXpCoreChance)
                        {
                            AwardLoot(saveManager, specialOverride.CustomXpCoreID, 1);
                            Debug.Log($"[Loot] Special Override Defeated: {_enemyData.EnemyName}! Obtained 1x {specialOverride.CustomXpCoreID}");
                        }
                        return; // Override completely handles loot
                    }
                }

                // 2. Fallback category logic
                if (cat == EnemyCategory.None)
                {
                    cat = lootConfig.FallbackCategory;
                }

                var settings = lootConfig.GetSettingsForCategory(cat);
                string matID = string.IsNullOrEmpty(settings.PrimaryMaterialID) ? CategoryToMaterialID(cat) : settings.PrimaryMaterialID;

                if (rank == EnemyRank.Boss)
                {
                    AwardLoot(saveManager, matID, settings.BossMaterialQuantity);
                    string bossCore = string.IsNullOrEmpty(settings.BossGuaranteedXpCoreID) ? "xp_core_legendary" : settings.BossGuaranteedXpCoreID;
                    AwardLoot(saveManager, bossCore, 1);
                    Debug.Log($"[Loot] Boss defeated: {_enemyData.EnemyName}! Obtained {settings.BossMaterialQuantity}x {matID} + 1x {bossCore}");
                }
                else if (rank == EnemyRank.Elite)
                {
                    float roll = Random.value;
                    if (roll < settings.EliteMaterialChance)
                    {
                        AwardLoot(saveManager, matID, settings.EliteMaterialQuantity);
                        Debug.Log($"[Loot] Defeated Elite {_enemyData.EnemyName}! Obtained {settings.EliteMaterialQuantity}x {matID}");
                    }
                    else if (roll < settings.EliteMaterialChance + settings.EliteXpCoreChance)
                    {
                        float xpRoll = Random.value;
                        float totalW = lootConfig.CommonWeight + lootConfig.RareWeight + lootConfig.EpicWeight;
                        float commonThresh = lootConfig.CommonWeight / totalW;
                        float rareThresh = (lootConfig.CommonWeight + lootConfig.RareWeight) / totalW;
                        
                        string xpID = xpRoll < commonThresh ? "xp_core_common"
                                    : xpRoll < rareThresh ? "xp_core_rare"
                                    : "xp_core_epic";
                                    
                        AwardLoot(saveManager, xpID, 1);
                        Debug.Log($"[Loot] Defeated Elite {_enemyData.EnemyName}! Obtained 1x {xpID}");
                    }
                }
                else // Normal
                {
                    float roll = Random.value;
                    if (roll < settings.NormalMaterialChance)
                    {
                        AwardLoot(saveManager, matID, 1);
                        Debug.Log($"[Loot] Defeated Normal {_enemyData.EnemyName}! Obtained 1x {matID}");
                    }
                    else if (roll < settings.NormalMaterialChance + settings.NormalXpCoreChance)
                    {
                        float xpRoll = Random.value;
                        float totalW = lootConfig.CommonWeight + lootConfig.RareWeight + lootConfig.EpicWeight;
                        float commonThresh = lootConfig.CommonWeight / totalW;
                        float rareThresh = (lootConfig.CommonWeight + lootConfig.RareWeight) / totalW;
                        
                        string xpID = xpRoll < commonThresh ? "xp_core_common"
                                    : xpRoll < rareThresh ? "xp_core_rare"
                                    : "xp_core_epic";
                                    
                        AwardLoot(saveManager, xpID, 1);
                        Debug.Log($"[Loot] Defeated Normal {_enemyData.EnemyName}! Obtained 1x {xpID}");
                    }
                }
            }
            else
            {
                // Fallback default logic
                string matID = CategoryToMaterialID(cat);
                if (rank == EnemyRank.Boss)
                {
                    AwardLoot(saveManager, matID, 3);
                    AwardLoot(saveManager, "xp_core_legendary", 1);
                    Debug.Log($"[Loot Fallback] Boss defeated: {_enemyData.EnemyName}! Obtained 3x {matID} + 1x xp_core_legendary");
                }
                else
                {
                    float roll = Random.value;
                    if (roll < 0.40f)
                    {
                        int qty = rank == EnemyRank.Elite ? 2 : 1;
                        AwardLoot(saveManager, matID, qty);
                        Debug.Log($"[Loot Fallback] Defeated {_enemyData.EnemyName}! Obtained {qty}x {matID}");
                    }
                    else if (roll < 0.60f)
                    {
                        float xpRoll = Random.value;
                        string xpID = xpRoll < 0.75f ? "xp_core_common"
                                    : xpRoll < 0.95f ? "xp_core_rare"
                                    : "xp_core_epic";
                        AwardLoot(saveManager, xpID, 1);
                        Debug.Log($"[Loot Fallback] Defeated {_enemyData.EnemyName}! Obtained 1x {xpID}");
                    }
                }
            }
        }

        private void AwardLoot(SaveManager saveManager, string itemID, int quantity)
        {
            var gameManager = FindFirstObjectByType<MaouSamaTD.Managers.GameManager>();
            if (gameManager != null)
            {
                gameManager.RegisterLoot(itemID, quantity);
            }
            else if (saveManager != null)
            {
                saveManager.AddItem(itemID, quantity);
            }

            // Spawn visual procedural flying loot animation on HUD
            var gameControlUI = FindFirstObjectByType<MaouSamaTD.UI.GameControlUI>();
            if (gameControlUI != null)
            {
                gameControlUI.SpawnLootFlyEffect(itemID, quantity, transform.position);
            }

            // Spawn World Drop Animation
            var settings = FindFirstObjectByType<SettingsManager>();
            if (settings == null || !settings.DisableLootAnimation)
            {
                // We'll spawn the prefab so size is controlled
                var prefab = Resources.Load<GameObject>("WorldLootDrop");
                if (prefab != null)
                {
                    GameObject dropVisual = Instantiate(prefab, transform.position, Quaternion.identity);
                    var dropComp = dropVisual.GetComponent<MaouSamaTD.VFX.WorldLootDropVisual>();
                    if (dropComp != null)
                    {
                        dropComp.Initialize(itemID, transform.position);
                    }
                }
                else
                {
                    // Fallback if prefab is missing
                    GameObject dropVisual = new GameObject($"LootDrop_{itemID}");
                    var dropComp = dropVisual.AddComponent<MaouSamaTD.VFX.WorldLootDropVisual>();
                    dropComp.Initialize(itemID, transform.position);
                }
            }
        }

        private static string CategoryToMaterialID(EnemyCategory cat)
        {
            return cat switch
            {
                EnemyCategory.Shadow => "mat_shadow_essence",
                EnemyCategory.Bandit => "mat_bandit_insignia",
                EnemyCategory.Animal => "mat_animal_fang",
                EnemyCategory.Golem  => "mat_golem_core",
                EnemyCategory.Undead => "mat_shadow_essence",  // Undead → same pool as Shadows
                EnemyCategory.Demon  => "mat_shadow_essence",  // Demons → same pool as Shadows
                _                    => "mat_bandit_insignia", // Fallback
            };
        }


        public void RecalculatePath(bool forceIgnore = false)
        {
            var gridMgr = FindFirstObjectByType<GridManager>();
            if (gridMgr == null || _enemyData == null) return;

            Vector2Int startValues = gridMgr.WorldToGridCoordinates(transform.position);
            Vector2Int goal = GoalCoord.x != -1 ? GoalCoord : gridMgr.ExitPoint;

            // Phasing logic: ignore occupants if we have charges, bypass evasion, or the collision type is set to ignore
            bool shouldIgnore = forceIgnore || 
                                _currentPhasingCharges > 0 || 
                                (_enemyData != null && (_enemyData.EvasionType == EnemyEvasionType.BypassBlockers || _enemyData.CollisionType == EnemyCollisionType.IgnoreUnits));
            
            Queue<Tile> newPath = gridMgr.GetPath(startValues, goal, _enemyData.MovementType, shouldIgnore);
            
            // If we couldn't find a path (e.g., player completely blocked the way), 
            // pathfind ignoring occupants so we at least walk up to the blocker and attack it!
            if ((newPath == null || newPath.Count == 0) && !shouldIgnore)
            {
                newPath = gridMgr.GetPath(startValues, goal, _enemyData.MovementType, true);
            }
            
            SetPath(newPath);
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

            if (_isMoving)
            {
                MoveTowardsTarget();
            }

            // Re-evaluating blockers/targets
            if (!_isCharmed && !_isMoving && !_isCentering)
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
                            
                            bool inRange = IsTargetInPattern(myPos, targetPos, _enemyData.AttackPattern, Range);
                            
                            // [STRICT MELEE ENGAGEMENT]
                            // All melee units must be forced to Manhattan distance matching their range to prevent diagonal engagement at range 1
                            if (inRange && _enemyData != null && _enemyData.DamageType == DamageType.Melee)
                            {
                                int manhattan = Mathf.Abs(myPos.x - targetPos.x) + Mathf.Abs(myPos.y - targetPos.y);
                                int maxManhattan = Mathf.FloorToInt(Range + 0.01f);
                                
                                if (manhattan > maxManhattan)
                                {
                                    inRange = false;
                                }
                                else if (_isMoving || _isCentering)
                                {
                                    // Melee units must be centered before they can trigger HandleAttack
                                    inRange = false; 
                                }
                            }

                            if (inRange)
                            {
                                HandleAttack(_attackTarget);
                                FaceTarget(_attackTarget.transform.position);
                                
                                // If not set to bypass defenders, stop moving to focus on attacking
                                if (!_enemyData.OnlyAttackIfBlocked)
                                {
                                    _isMoving = false;
                                    InitiateCentering();
                                }
                                else if (!_isMoving && _blockedBy == null && !_isCharmed)
                                {
                                    // If OnlyAttackIfBlocked is true, and we are stationary but not blocked, we must resume moving!
                                    _isMoving = true;
                                }
                            }
                            else if (!_isMoving && _blockedBy == null && !_isCharmed)
                            {
                                // Resume moving if we were stopped for combat but target is now out of range/not strictly adjacent
                                _isMoving = true;
                            }
                        }
                    }
                    else
                    {
                        _attackTarget = null;
                        // If we were stopped for combat but no target remains, resume moving
                        if (!_isMoving && _blockedBy == null && !_isCharmed && (_path != null && _path.Count > 0))
                        {
                            _isMoving = true;
                        }
                    }
                }
            }

            else if (_attackTarget == null && _blockedBy == null && _gridManager != null && !_isMoving)
            {
                // Orientation fallback: Face the exit if stationary and idle
                var exitTile = _gridManager.GetTileAt(_gridManager.ExitPoint);
                FaceTarget(exitTile != null ? exitTile.transform.position : transform.position + Vector3.right);
            }

            // Abilities are already ticked at the start of UpdateInternal
        }

        private bool ScanForTarget(out PlayerUnit target)
        {
            target = null;
            if (_gridManager == null) _gridManager = FindFirstObjectByType<GridManager>();
            if (_gridManager == null) return false;

            PlayerUnit bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var unit in PlayerUnit.ActiveUnits)
            {
                if (unit != null && unit.CurrentHp > 0 && !unit.IsDead)
                {
                    Vector2Int myPos = _gridManager.WorldToGridCoordinates(transform.position);
                    Vector2Int targetPos = _gridManager.WorldToGridCoordinates(unit.transform.position);
                    
                    // Flying units only attack high ground, UNLESS they are on the same tile as the target (passing through)
                    var targetTile = _gridManager.GetTileAt(targetPos);
                    bool isTargetHighGround = targetTile != null && targetTile.IsHighGround;

                    if (isTargetHighGround)
                    {
                        if (!(_enemyData.GroundAttackTargets.HasFlag(TargetableGround.HighGround)))
                        {
                            // If we can't attack high ground, we skip unless we are on the same tile (collision/passing)
                            if (myPos != targetPos) continue;
                        }
                    }
                    else
                    {
                        if (!(_enemyData.GroundAttackTargets.HasFlag(TargetableGround.LowGround)))
                        {
                            // If we can't attack low ground, we skip unless we are on the same tile
                            if (myPos != targetPos) continue;
                        }
                    }

                    if (_enemyData.MovementType == EnemyMovementType.Flying)
                    {
                        if (myPos != targetPos && !isTargetHighGround)
                        {
                            continue;
                        }
                    }

                    bool inRange = IsTargetInPattern(myPos, targetPos, _enemyData != null ? _enemyData.AttackPattern : AttackPattern.All, Range);
                    
                    // [STRICT MELEE ENGAGEMENT]
                    // Scan logic must also respect the Manhattan distance limit for all melee units
                    if (inRange && _enemyData != null && _enemyData.DamageType == DamageType.Melee)
                    {
                        int manhattan = Mathf.Abs(myPos.x - targetPos.x) + Mathf.Abs(myPos.y - targetPos.y);
                        int maxManhattan = Mathf.FloorToInt(Range + 0.01f);
                        if (manhattan > maxManhattan) inRange = false;
                    }

                    if (inRange)
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
                if (_gridManager == null) _gridManager = FindFirstObjectByType<GridManager>();

                // Aggro logic: if we don't have a target, or if the current target isn't on high ground but the attacker is
                if (_attackTarget == null && _blockedBy == null)
                {
                    _attackTarget = player;
                    
                    // Strictly do NOT stop or center if OnlyAttackIfBlocked is true (bypass/boss behavior)
                    if (_isMoving && _enemyData != null && !_enemyData.OnlyAttackIfBlocked)
                    {
                        // Check if the attacker is actually within our attack range and pattern
                        if (_gridManager != null)
                        {
                            Vector2Int myPos = _gridManager.WorldToGridCoordinates(transform.position);
                            Vector2Int targetPos = _gridManager.WorldToGridCoordinates(player.transform.position);
                            
                            bool inRange = IsTargetInPattern(myPos, targetPos, _enemyData.AttackPattern, Range);
                            
                            // Respect strict melee range/Manhattan checks
                            if (inRange && _enemyData.DamageType == DamageType.Melee)
                            {
                                int manhattan = Mathf.Abs(myPos.x - targetPos.x) + Mathf.Abs(myPos.y - targetPos.y);
                                int maxManhattan = Mathf.FloorToInt(Range + 0.01f);
                                if (manhattan > maxManhattan) inRange = false;
                            }

                            // Only stop if the attacker is actually within our range to engage
                            if (inRange)
                            {
                                InitiateCentering();
                            }
                        }
                        else
                        {
                            InitiateCentering();
                        }
                    }
                }
                else if (_attackTarget != null)
                {
                    var attPos = _gridManager.WorldToGridCoordinates(player.transform.position);
                    var attTile = _gridManager.GetTileAt(attPos);
                    var curPos = _gridManager.WorldToGridCoordinates(_attackTarget.transform.position);
                    var curTile = _gridManager.GetTileAt(curPos);

                    bool attIsHigh = attTile != null && attTile.IsHighGround;
                    bool curIsHigh = curTile != null && curTile.IsHighGround;

                    // Switch if target ground type is allowed and current isn't (or priority dictates)
                    bool canAttackNext = attIsHigh ? _enemyData.GroundAttackTargets.HasFlag(TargetableGround.HighGround) : _enemyData.GroundAttackTargets.HasFlag(TargetableGround.LowGround);
                    bool canAttackCur = curIsHigh ? _enemyData.GroundAttackTargets.HasFlag(TargetableGround.HighGround) : _enemyData.GroundAttackTargets.HasFlag(TargetableGround.LowGround);

                    if (canAttackNext && !canAttackCur)
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
               
               // If very close, check if we have a target or move direction to fall back on
               if (Mathf.Abs(diff) < 0.01f)
               {
                   if (_attackTarget != null) 
                       diff = _attackTarget.transform.position.x - transform.position.x;
                   else if (_targetTile != null)
                       diff = _targetTile.transform.position.x - transform.position.x;
               }

               if (Mathf.Abs(diff) < 0.01f) return;

               bool isTargetRight = diff > 0;
               
                if (_spriteRenderer != null)
                {
                    Vector3 spriteScale = _originalSpriteScale;
                    // Default facing is Left (+1). To face Right, use -1.
                    spriteScale.x = Mathf.Abs(_originalSpriteScale.x) * (isTargetRight ? -1f : 1f);
                    _spriteRenderer.transform.localScale = spriteScale;
                }
          }

        public override bool IsRanged()
        {
            if (_enemyData == null) return false;
            return _enemyData.DamageType == DamageType.Ranged ||
                   _enemyData.DamageType == DamageType.Magic;
        }

        protected override void HandleAttack(UnitBase target)
        {
            if (target == null) return;
            if (Time.deltaTime <= 0f) return; // Don't attack while time is paused
            if (Time.time >= _lastAttackTime + _attackInterval)
            {
                _lastAttackTime = Time.time;
                
                if (_enemyData != null && _enemyData.IsBoss && _gridManager != null)
                {
                    Vector2Int myCoord = _gridManager.WorldToGridCoordinates(transform.position);
                    Vector2Int targetCoord = _gridManager.WorldToGridCoordinates(target.transform.position);
                    int distance = Mathf.Abs(myCoord.x - targetCoord.x) + Mathf.Abs(myCoord.y - targetCoord.y);
                    float configuredRange = Range;
                    Debug.Log($"[BOSS DEBUG] Attacking! Boss Tile: {myCoord} | Target ({target.gameObject.name})  {targetCoord} | Tile Distance: {distance} | Configured Range: {configuredRange}");
                }
                
                base.HandleAttack(target);
                
                DamageType damageType = _enemyData != null ? _enemyData.DamageType : DamageType.Melee;

                if (IsRanged())
                {
                    string prefabName = (_enemyData != null && _enemyData.DamageType == DamageType.Ranged) 
                        ? "VFX/Arrow_Projectile" 
                        : "VFX/Magic_Projectile";

                    GameObject prefab = Resources.Load<GameObject>(prefabName);
                    if (prefab != null)
                    {
                        GameObject projObj = Instantiate(prefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
                        
                        // Enable billboarding on projectile
                        var billboard = projObj.GetComponent<Billboard>();
                        if (billboard == null)
                        {
                            billboard = projObj.AddComponent<Billboard>();
                        }
                        billboard.LockZ = true; // Lock Z so the projectile rotates towards its target in screen-space

                        var sr = projObj.GetComponentInChildren<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sortingLayerName = "UI";
                            sr.sortingOrder = 50;
                        }

                        var projComp = projObj.GetComponent<MaouSamaTD.VFX.BasicProjectile>();
                        if (projComp != null)
                        {
                            projComp.Launch(target, _attackPower, this, damageType);
                        }
                        else
                        {
                            target.TakeDamage(_attackPower, this, damageType);
                        }
                    }
                    else
                    {
                        target.TakeDamage(_attackPower, this, damageType);
                    }
                }
                else
                {
                    target.TakeDamage(_attackPower, this, damageType);

                    // Spawn melee slash effect
                    GameObject slashPrefab = Resources.Load<GameObject>("VFX/Melee_Slash_VFX");
                    if (slashPrefab != null)
                    {
                        Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), UnityEngine.Random.Range(-0.15f, 0.15f) + 0.8f, 0f);
                        GameObject slashObj = Instantiate(slashPrefab, target.transform.position + randomOffset, Quaternion.identity);
                        
                        // Enable billboarding on slash
                        if (slashObj.GetComponent<Billboard>() == null)
                        {
                            slashObj.AddComponent<Billboard>();
                        }
                        
                        // Make slashes bigger
                        slashObj.transform.localScale = Vector3.one * 1.8f;

                        var sr = slashObj.GetComponentInChildren<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sortingLayerName = "UI";
                            sr.sortingOrder = 50;
                        }
                    }
                }

                foreach (var ability in _runtimeAbilities)
                {
                    ability.OnAttack(this, target as UnitBase);
                }
            }
        }

        private void MoveTowardsTarget()
        {
            if (_enemyData == null || _targetTile == null) return;
            if (Time.deltaTime <= 0f) 
            {
                if (_enemyData.IsBoss && Time.frameCount % 120 == 0)
                {
                    Debug.LogWarning($"[EnemyUnit] {gameObject.name} (Boss) movement skipped because Time.deltaTime is 0 (TimeScale is likely 0).");
                }
                return;
            }

            // 1. Check for range-based targets if not already centering/blocked
            // BYPASS: If we have phasing charges or Bypass evasion, we ignore units to reach the exit
            // NEW: If OnlyAttackIfBlocked is true, we ONLY target if we are actually blocked (see block detection below)
            bool isPhasing = _currentPhasingCharges > 0 || 
                             _enemyData.EvasionType == EnemyEvasionType.BypassBlockers ||
                             _enemyData.CollisionType == EnemyCollisionType.IgnoreUnits;
            
            if (!_isCentering && _blockedBy == null && !_isCharmed && !_enemyData.OnlyAttackIfBlocked)
            {
                if (ScanForTarget(out PlayerUnit target))
                {
                    _attackTarget = target;
                    
                    // Stop to attack ONLY if:
                    // 1. Priority is KillUnits
                    // 2. OR we are physically blocked by this target (handled in collision check below)
                    // 3. AND we are not currently phasing/bypassing
                    
                    if (_enemyData.TargetingPriority == EnemyTargetingPriority.KillUnits && !isPhasing)
                    {
                        InitiateCentering();
                        return;
                    }
                }
            }

            // 2. Check for blockers in moving path
            if (!_isCentering && _enemyData.CollisionType == EnemyCollisionType.BlockedByUnits && !_isCharmed)
            {
                // Fix: Also check current tile in case we are already overlapping a unit that just became able to block (e.g. Ultimate)
                Vector2Int currentCoord = _gridManager.WorldToGridCoordinates(transform.position);
                Grid.Tile currentTile = _gridManager.GetTileAt(currentCoord);
                if (currentTile != null && currentTile.IsOccupied && currentTile.Occupant is PlayerUnit currentInTilePlayer && currentInTilePlayer.CanBlock())
                {
                     if (_enemyData.EvasionType != EnemyEvasionType.BypassBlockers && _currentPhasingCharges <= 0)
                     {
                         _blockedBy = currentInTilePlayer;
                         currentInTilePlayer.NotifyEncounter(this);
                         InitiateCentering();
                         return;
                     }
                }

                if (_targetTile.IsOccupied && _targetTile.Occupant is PlayerUnit player && player.CanBlock())
                {
                    bool canEvade = isPhasing;
                    
                    if (!canEvade && _enemyData.EvasionType == EnemyEvasionType.IgnoreIfTargetAttacking && player.IsAttacking())
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
                        player.NotifyEncounter(this); // Trigger reach tutorial logic
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
                        // Becomes vulnerable again ONLY if it was granted by phasing (not currently tracked explicitly, but let's check if it was in base data)
                        bool wasInBaseData = _enemyData != null && _enemyData.Immunities.Contains(DamageType.Melee);
                        if (Immunities.Contains(DamageType.Melee) && !wasInBaseData)
                        {
                            Immunities.Remove(DamageType.Melee);
                            if (_showDebugLogs) Debug.Log($"{gameObject.name} Phasing ended. TEMPORARY MELEE IMMUNITY REMOVED (Vulnerable)!");
                        }

                        // IMPORTANT: Recalculate path now that we can no longer phase through units
                        RecalculatePath();
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
            if (_showDebugLogs) Debug.Log($"Enemy reached exit! Dealing {(int)_enemyData.ExitDamage} damage.");
            
            var gm = FindFirstObjectByType<MaouSamaTD.Managers.GameManager>();
            if (gm != null)
            {
                gm.EnemyEscaped(this);
            }

            GridManager gridMgr = FindFirstObjectByType<GridManager>(); 
            if (gridMgr != null && _targetTile != null)
            {
                TileType currentType = _targetTile.Type;
                if (currentType != TileType.ExitPoint && currentType != TileType.ExitPointHigh)
                {
                    gridMgr.SetTileType(_targetTile.Coordinate, TileType.ExitPoint); 
                }
            } 
            
            var tm = FindFirstObjectByType<MaouSamaTD.Managers.TutorialManager>();
            if (tm != null && _enemyData != null)
            {
                tm.OnActionTriggered("EnemyReachedExit_" + _enemyData.EnemyName);
            }

            Destroy(gameObject);
        }

        public void SetBlockedBy(PlayerUnit blocker)
        {
            _blockedBy = blocker;
            InitiateCentering();
        }

        public void ReleaseBlock()
        {
            if (_blockedBy != null)
            {
                _blockedBy.UnregisterBlockedEnemy(this);
            }
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


        protected override Vector3 GetSpriteLocalPosition()
        {
            if (_enemyData == null) return new Vector3(0, 1f, 0);
            float baseHeight = _enemyData.BaseVisualHeight; 
            float finalY = baseHeight + _enemyData.VisualYOffset;
            return new Vector3(0, finalY, 0);
        }
    }
}
