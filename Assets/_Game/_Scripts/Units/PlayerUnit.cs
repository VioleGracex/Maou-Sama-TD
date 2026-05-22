using TMPro;
using MaouSamaTD.Utils;
using UnityEngine;
using System.Collections;
using DG.Tweening;

namespace MaouSamaTD.Units
{
    // Enums are now in their own files (UnitClass.cs)

    public enum AttackPattern
    {
        Vertical,       // |
        Horizontal,     // -
        Diagonal,       // X
        Cross,          // + (Vertical + Horizontal)
        All,            // * (Surrounding 8 tiles)
        Custom          // Visual Grid Pattern
    }

    public enum AttackType
    {
        SingleTarget,
        AreaOfEffect
    }

    public class PlayerUnit : UnitBase
    {
        // Removed shadowed _data field to fix null reference bugs
        
        public static System.Collections.Generic.List<PlayerUnit> ActiveUnits = new System.Collections.Generic.List<PlayerUnit>();
        public event System.Action<PlayerUnit> OnRetreat;

        [Header("Player Unit Stats")]
        [SerializeField] private UnitClass _unitClass;
        [SerializeField] private int _deploymentCost = 10;
        
        public UnitClass UnitClass => _unitClass;
        public int BlockCount => Data != null ? Data.BlockCount : 1;
        public int DeploymentCost => _deploymentCost;
        
        public Grid.Tile CurrentTile { get; set; }

        private float _currentCharge;
        public float CurrentCharge => _currentCharge;
        public float MaxCharge => Data != null ? Data.MaxCharge : 100f;
        public int KillCount { get; private set; }
        public int ReachCount { get; private set; }

        private System.Collections.Generic.List<EnemyUnit> _currentlyBlockedEnemies = new System.Collections.Generic.List<EnemyUnit>();
        public bool CanBlock() => true;

        public void NotifyEncounter(EnemyUnit enemy)
        {
            if (enemy != null && !_currentlyBlockedEnemies.Contains(enemy))
            {
                _currentlyBlockedEnemies.Add(enemy);
            }
            
            ReachCount++;
            if (_showDebugLogs) Debug.Log($"[Ultimate] {gameObject.name} encountered {enemy?.gameObject.name}. Blocking: {_currentlyBlockedEnemies.Count}/{BlockCount}");
            Managers.TutorialManager tm = FindFirstObjectByType<Managers.TutorialManager>();
            if (tm != null) tm.OnActionTriggered("UnitReach");
        }

        public void UnregisterBlockedEnemy(EnemyUnit enemy)
        {
            if (_currentlyBlockedEnemies.Contains(enemy))
            {
                _currentlyBlockedEnemies.Remove(enemy);
            }
        }

        public void IncrementKillCount()
        {
            KillCount++;
            if (_showDebugLogs) Debug.Log($"[Ultimate] {Data?.UnitName} now has {KillCount} kills.");
            Managers.TutorialManager tm = FindFirstObjectByType<Managers.TutorialManager>();
            if (tm != null) tm.OnActionTriggered("UnitKill"); 
        }

        public void UseSkill()
        {
            if (Data != null && Data.UltimateSkill != null)
            {
                float cost = Data.UltimateSkill.ChargeCost;
                
                if (_currentCharge >= cost)
                {
                    if (_showDebugLogs) Debug.Log($"[Ultimate] Used Skill: {Data.UltimateSkill.SkillName}!");
                    _currentCharge -= cost;
                    StartCoroutine(ExecuteUltimateRoutine());
                    
                    Managers.TutorialManager tm = FindFirstObjectByType<Managers.TutorialManager>();
                    if (tm != null) tm.OnActionTriggered("SkillUsed");
                }
                else
                {
                    if (_showDebugLogs) Debug.Log($"[Ultimate] Not enough charge! ({_currentCharge}/{cost})");
                }
            }
            else
            {
                 if (_showDebugLogs) Debug.LogWarning("[Ultimate] Cannot use skill: No UnitData or SkillData assigned.");
            }
        }

        private IEnumerator ExecuteUltimateRoutine()
        {
            if (Data == null)
            {
                Debug.LogError("[Ultimate] PlayerUnit Data is NULL at runtime!");
                yield break;
            }

            if (Data.UltimateSkill == null)
            {
                Debug.LogError($"[Ultimate] {Data.UnitName} has no Skill Data assigned in IgnisUnitData.asset!");
                yield break;
            }

            var visuals = Data.UltimateSkill.GetVisuals(Data.EquippedSkinID);

            if (visuals.UltimatePrefab == null)
            {
                Debug.LogError($"[Ultimate] {Data.UnitName} Skill [{Data.UltimateSkill.SkillName}] exists, but UltimatePrefab is NULL! GUID check needed.");
                yield break;
            }

            if (_showDebugLogs) Debug.Log($"[Ultimate] STARTING sequence for {Data.UnitName}. Prefab: {visuals.UltimatePrefab.name}");

            IsCastingUltimate = true;

            // Start Cut-In Animation
            if (MaouSamaTD.UI.UltimateCutInUI.Instance != null)
            {
                string uName = Data.UnitName;
                string uTitle = Data.UnitTitle;
                string sName = Data.UltimateSkill.SkillName;
                Color bColor = visuals.UltimateColor;
                Color tBgColor = visuals.TitleBgColor;
                Color tTextColor = visuals.TitleTextColor;
                Color sBgColor = visuals.SkillNameBgColor;
                Color sTextColor = visuals.SkillNameTextColor;
                Color nTextColor = visuals.NameTextColor;
                Sprite portrait = Data.GetSprite(UnitData.UnitImageType.WaistUp);

                if (_showDebugLogs) Debug.Log($"[Ultimate] Triggering Cut-In Animation for {uName}...");
                // Wait for the full animation sequence to complete
                yield return MaouSamaTD.UI.UltimateCutInUI.Instance.PlayAnimation(uName, uTitle, sName, bColor, tBgColor, tTextColor, sBgColor, sTextColor, nTextColor, portrait);
            }
            else
            {
                if (_showDebugLogs) Debug.LogWarning("[Ultimate] UltimateCutInUI.Instance is MISSING. Skipping animation.");
            }

            if (_animator != null) _animator.Play("Ultimate", 0, 0f);
            
            Vector3 bestDir = FindBestUltimateDirection();
            if (_showDebugLogs) Debug.Log($"[Ultimate] Spawning prefab: {visuals.UltimatePrefab.name} towards {bestDir}");

            GameObject projObj = Instantiate(visuals.UltimatePrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
            
            var ultimateEffect = projObj.GetComponent<MaouSamaTD.Skills.UltimateEffect>();
            if (ultimateEffect != null)
            {
                ultimateEffect.Execute(this, bestDir);
                if (_showDebugLogs) Debug.Log($"[Ultimate] {projObj.name} EXECUTED successfully.");
                
                // Wait for the local ultimate animation to finish before going back to idle
                yield return new WaitForSeconds(1.5f);
                
                IsCastingUltimate = false;
                if (_animator != null && !_isDead)
                {
                    _animator.Play("Idle", 0, 0f);
                }
            }
            else
            {
                Debug.LogError($"[Ultimate] Prefab on {Data.UltimateSkill.SkillName} is missing an UltimateEffect component!");
                IsCastingUltimate = false;
            }
        }

        private Vector3 FindBestUltimateDirection()
        {
            // Align with Grid Axes: Right (+X), Left (-X)
            Vector3[] directions = { 
                Vector3.right,  
                Vector3.left    
            };

            Vector3 bestDir = directions[0];
            int maxEnemies = -1;

            foreach (var dir in directions)
            {
                int count = 0;
                foreach (var enemy in EnemyUnit.ActiveEnemies)
                {
                    if (enemy == null) continue;
                    Vector3 toEnemy = enemy.transform.position - transform.position;
                    float projection = Vector3.Dot(toEnemy, dir);
                    float perpendicularDist = Vector3.Cross(toEnemy, dir).magnitude;

                    // Lane: 20 units long, 1.5 units wide (roughly 1 grid cell width)
                    if (projection > 0 && projection < 20f && perpendicularDist < 1.5f)
                    {
                        count++;
                    }
                }

                if (count > maxEnemies)
                {
                    maxEnemies = count;
                    bestDir = dir;
                }
            }
            return bestDir;
        }
        
        public void AddCharge(float amount)
        {
            if (_data == null) return;
            _currentCharge = Mathf.Min(_currentCharge + amount, MaxCharge);
        }

        public void ForceChargeUltimate()
        {
            if (_data == null) return;
            _currentCharge = MaxCharge;
            if (_showDebugLogs) Debug.Log($"[tutorial] {gameObject.name} ultimate forcefully charged.");
        }

        [Header("Visuals")]
        [SerializeField] private Billboard _billboard;

        public override void Initialize(UnitData data)
        {
            base.Initialize(data);
            
            // Safe guard: double the stats if the base initializer fell back to raw/non-doubled stats
            if (data.CalculatedStats.MaxHp <= 0)
            {
                _maxHp *= 2f;
                _currentHp = _maxHp;
                _attackPower *= 2f;
                _defense *= 2f;
            }

            if (!ActiveUnits.Contains(this)) ActiveUnits.Add(this);
            _unitClass = data.Class;
            _deploymentCost = data.DeploymentCost;
            
            // Set dynamic name for tutorial targeting (e.g., Unit_Ignis)
            gameObject.name = "Unit_" + data.UnitName;
            
            // Base handles Sprite and Animator (including destruction if missing)
            // No need for separate UpdateVisuals call here
            
            // Face nearest spawn point automatically upon deployment
            if (_gridManager == null) _gridManager = FindFirstObjectByType<Grid.GridManager>();
            if (_gridManager != null && _gridManager.SpawnPoints != null && _gridManager.SpawnPoints.Count > 0)
            {
                var closestSpawnCoord = _gridManager.SpawnPoints[0].Coordinate;
                var unitCoord = CurrentTile != null ? CurrentTile.Coordinate : _gridManager.WorldToGridCoordinates(transform.position);
                float minDist = Vector2.Distance(unitCoord, closestSpawnCoord);

                for (int i = 1; i < _gridManager.SpawnPoints.Count; i++)
                {
                    float dist = Vector2.Distance(unitCoord, _gridManager.SpawnPoints[i].Coordinate);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestSpawnCoord = _gridManager.SpawnPoints[i].Coordinate;
                    }
                }
                
                bool isTargetRight = closestSpawnCoord.x > unitCoord.x;
                if (_spriteRenderer != null)
                {
                    Vector3 spriteScale = _originalSpriteScale;
                    // Default facing is Left (+1). To face Right, use -1.
                    spriteScale.x = Mathf.Abs(_originalSpriteScale.x) * (isTargetRight ? -1f : 1f);
                    _spriteRenderer.transform.localScale = spriteScale;
                }
            }
        }
        
        private void OnDestroy()
        {
            ActiveUnits.Remove(this);
            // Release any remaining blocks to avoid leaking enemy state
            var blockedArray = _currentlyBlockedEnemies.ToArray();
            foreach (var enemy in blockedArray)
            {
                if (enemy != null) enemy.ReleaseBlock();
            }
            _currentlyBlockedEnemies.Clear();
        }

        protected override void UpdateVisuals()
        {
            base.UpdateVisuals();
            
            if (_data == null) return;

            // Ensure Billboard is assigned (PlayerUnit specific field)
            if (_billboard == null) _billboard = GetComponentInChildren<Billboard>();
        }

        [Zenject.Inject] private Managers.InteractionManager _interactionManager;
        private Grid.GridManager _gridManager;

        protected override void UpdateInternal()
        {
             if (_isDead) return;
             base.UpdateInternal();
             
             if (_data != null && _currentCharge < MaxCharge)
             {
                 _currentCharge += _data.ChargePerSecond * Time.deltaTime;
                 if (_currentCharge > MaxCharge) _currentCharge = MaxCharge;
             }

             if (Time.time >= _lastAttackTime + _attackInterval)
             {
                 Attack();
             }
        }

        private void Attack()
        {
            if (_gridManager == null) _gridManager = FindFirstObjectByType<Grid.GridManager>();
            if (_gridManager == null) return;

            Vector2Int myPos;
            if (CurrentTile != null) myPos = CurrentTile.Coordinate;
            else myPos = _gridManager.WorldToGridCoordinates(transform.position);

            AttackPattern pattern = _data != null ? _data.AttackPattern : AttackPattern.All;
            AttackType type = _data != null ? _data.AttackType : AttackType.SingleTarget;
            float range = Range;

            bool attacked = false;

            var enemies = EnemyUnit.ActiveEnemies.ToArray();

            bool canAttackFlying = _data == null || _data.CanAttackFlying;

            EnemyUnit bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.CurrentHp <= 0 || enemy.IsDead) continue;

                // Skip flying enemies if this unit cannot attack them
                if (!canAttackFlying && enemy.EnemyData != null &&
                    enemy.EnemyData.MovementType == EnemyMovementType.Flying)
                    continue;

                Vector2Int enemyPos = _gridManager.WorldToGridCoordinates(enemy.transform.position);
                
                if (IsTargetInPattern(myPos, enemyPos, pattern, range))
                {
                    var tile = _gridManager.GetTileAt(enemyPos);
                    
                    // Disallow ground melee units from attacking high ground tiles
                    if (!IsRanged() && tile != null && tile.IsHighGround)
                    {
                        bool iAmHighGround = CurrentTile != null && CurrentTile.IsHighGround;
                        if (!iAmHighGround)
                        {
                            continue; // Melee on low ground cannot reach high ground
                        }
                    }

                    float score = 0;
                    
                    // Priority 1: High Ground (Flyers are usually high ground, or enemies on specific tiles)
                    if (tile != null && tile.IsHighGround)
                        score += 2000f;
                        
                    // Priority 2: Same Lane (Row or Column)
                    Vector2Int myCoord = _gridManager.WorldToGridCoordinates(transform.position);
                    if (enemyPos.x == myCoord.x || enemyPos.y == myCoord.y)
                        score += 500f;

                    // Priority 3: Damage Aggro (Weight based on damage taken)
                    score += GetDamageFrom(enemy);

                    // Priority 4: Last Attacker (Aggro Bonus)
                    if (enemy == _lastAttacker)
                        score += 500f;

                    // Priority 5: Proximity
                    score -= Vector3.Distance(transform.position, enemy.transform.position);

                    if (type == AttackType.AreaOfEffect)
                    {
                        // AOE hits everyone in pattern
                        ExecuteAttackOn(enemy);
                        attacked = true;
                    }
                    else if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = enemy;
                    }
                }
            }

            if (type == AttackType.SingleTarget && bestTarget != null)
            {
                ExecuteAttackOn(bestTarget);
                attacked = true;
            }

            if (attacked)
            {
                _lastAttackTime = Time.time;
            }
            else
            {
                // If we didn't attack and are not in an ultimate, return to idle if we were attacking
                if (_animator != null && !_isDead)
                {
                    var state = _animator.GetCurrentAnimatorStateInfo(0);
                    if (state.IsName("Attack") && state.normalizedTime >= 1.0f)
                    {
                        _animator.Play("Idle", 0, 0f);
                    }
                    else if (!state.IsName("Attack") && !state.IsName("Ultimate") && !state.IsName("Idle"))
                    {
                        // Fallback reset for any other non-looping state that got stuck
                        _animator.Play("Idle", 0, 0f);
                    }
                }
            }
        }

        private void FaceTarget(Vector3 targetPos)
        {
             if (_spriteRenderer == null) return;
             bool isTargetRight = targetPos.x > transform.position.x;
             if (_spriteRenderer != null)
             {
                 Vector3 spriteScale = _originalSpriteScale;
                 // Default facing is Left (+1). To face Right, use -1.
                 spriteScale.x = Mathf.Abs(_originalSpriteScale.x) * (isTargetRight ? -1f : 1f);
                 _spriteRenderer.transform.localScale = spriteScale;
             }
        }



        public void Retreat()
        {
            // Disallow retreat during tutorial
            Managers.TutorialManager tm = FindFirstObjectByType<Managers.TutorialManager>();
            if (tm != null && tm.IsInTutorial)
            {
                if (_showDebugLogs) Debug.Log("[Retreat] Retreat is disabled during tutorial.");
                return;
            }

            _currentHp = 0;
            
            if (CurrentTile != null)
            {
                CurrentTile.SetOccupant(null); 
                CurrentTile = null;
            }

            OnRetreat?.Invoke(this);
            if (_interactionManager != null) _interactionManager.NotifyUnitRemoved(this);
            
            var gm = FindFirstObjectByType<Managers.GameManager>();
            if (gm != null) gm.ReportUnitLost();

            Destroy(gameObject);
        }

        public override bool IsRanged()
        {
            if (_data != null)
            {
                if (_data.DamageType == DamageType.Ranged || _data.DamageType == DamageType.Magic)
                    return true;
                
                if (_data.Class == UnitClass.Ranger ||
                    _data.Class == UnitClass.Warlock ||
                    _data.Class == UnitClass.Sage ||
                    _data.Class == UnitClass.Support ||
                    _data.Class == UnitClass.Gunner)
                    return true;
            }
            
            return _unitClass == UnitClass.Ranger ||
                   _unitClass == UnitClass.Warlock ||
                   _unitClass == UnitClass.Sage ||
                   _unitClass == UnitClass.Support ||
                   _unitClass == UnitClass.Gunner;
        }

        private void ExecuteAttackOn(EnemyUnit target)
        {
            HandleAttack(target);
            DamageType dType = _data != null ? _data.DamageType : DamageType.Melee;
            FaceTarget(target.transform.position);

            bool isOnHighGround = CurrentTile != null && CurrentTile.IsHighGround;
            bool shouldShootProjectile = IsRanged() || isOnHighGround;

            if (shouldShootProjectile)
            {
                string prefabName = "VFX/Magic_Projectile"; // default fallback
                UnitClass currentClass = _data != null ? _data.Class : _unitClass;

                if (currentClass == UnitClass.Ranger) prefabName = "VFX/Arrow_Projectile";
                else if (currentClass == UnitClass.Gunner) prefabName = "VFX/Bullet_Projectile";
                else if (currentClass == UnitClass.Warlock || currentClass == UnitClass.Sage || currentClass == UnitClass.Support) prefabName = "VFX/Magic_Projectile";

                GameObject prefab = Resources.Load<GameObject>(prefabName);
                if (prefab != null)
                {
                    GameObject projObj = Instantiate(prefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                    
                    // Enable billboarding on projectile
                    var billboard = projObj.GetComponent<Billboard>();
                    if (billboard == null)
                    {
                        billboard = projObj.AddComponent<Billboard>();
                    }
                    billboard.LockZ = true; // Lock Z so the projectile rotates towards its target in screen-space

                    var projComp = projObj.GetComponent<MaouSamaTD.VFX.BasicProjectile>();
                    if (projComp != null)
                    {
                        projComp.Launch(target, AttackPower, this, dType);
                    }
                    else
                    {
                        target.TakeDamage(AttackPower, this, dType);
                    }
                }
                else
                {
                    target.TakeDamage(AttackPower, this, dType);
                }
            }
            else
            {
                // Melee unit not on high ground
                target.TakeDamage(AttackPower, this, dType);

                // Spawn melee slash effect
                GameObject slashPrefab = Resources.Load<GameObject>("VFX/Melee_Slash_VFX");
                if (slashPrefab != null)
                {
                    Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), UnityEngine.Random.Range(-0.15f, 0.15f) + 0.5f, 0f);
                    GameObject slashObj = Instantiate(slashPrefab, target.transform.position + randomOffset, Quaternion.identity);
                    
                    // Enable billboarding on slash
                    if (slashObj.GetComponent<Billboard>() == null)
                    {
                        slashObj.AddComponent<Billboard>();
                    }
                    
                    // Make slashes bigger
                    slashObj.transform.localScale = Vector3.one * 1.8f;
                }
            }
        }

        private EnemyUnit _lastAttacker;
        protected override void RegisterAttacker(UnitBase attacker)
        {
            if (attacker is EnemyUnit enemy)
            {
                _lastAttacker = enemy;
            }
        }

        public override void Die(UnitBase attacker = null)
        {
            if (CurrentTile != null)
            {
                CurrentTile.SetOccupant(null);
                CurrentTile = null;
            }

            OnRetreat?.Invoke(this);

            if (_interactionManager != null) _interactionManager.NotifyUnitRemoved(this);
            
            var gm = FindFirstObjectByType<Managers.GameManager>();

            if (Data != null)
            {
                var saveManager = FindFirstObjectByType<MaouSamaTD.Managers.SaveManager>();
                if (saveManager != null && saveManager.CurrentData != null)
                {
                    string unitId = Data.name;
                    var entry = saveManager.CurrentData.UnitInventory.Find(e => e.UnitID == unitId && !e.IsDuplicate);
                    if (entry != null)
                    {
                        // Check if they have died multiple times in this same level to escalate penalty
                        int previousDeaths = 0;
                        if (gm != null)
                        {
                            previousDeaths = gm.GetUnitDeathCount(unitId);
                        }

                        int penalty = 20; // First death
                        if (previousDeaths == 1) penalty = 30; // Second death
                        else if (previousDeaths >= 2) penalty = 40; // Third+ death

                        entry.Vigor = Mathf.Max(0, entry.Vigor - penalty);
                        Data.Vigor = entry.Vigor;
                        if (MaouSamaTD.Core.AppEntryPoint.LoadedScalingData != null)
                        {
                            Data.RefreshStats(MaouSamaTD.Core.AppEntryPoint.LoadedScalingData);
                        }
                        saveManager.Save();
                        Debug.Log($"[PlayerUnit] {Data.UnitName} has died. Previous deaths this level: {previousDeaths}. Vigor reduced by {penalty} to {entry.Vigor}/100.");
                    }
                }
            }

            if (gm != null)
            {
                if (Data != null) gm.ReportUnitLost(Data.name);
                else gm.ReportUnitLost();
            }

            var tm = FindFirstObjectByType<Managers.TutorialManager>();
            if (tm != null && Data != null) tm.OnActionTriggered("UnitDied_" + Data.UnitName);

            base.Die();
        }
    }
}
