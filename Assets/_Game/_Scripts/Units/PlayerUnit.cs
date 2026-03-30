using TMPro;
using MaouSamaTD.Utils;
using UnityEngine;
using System.Collections;

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

        public void NotifyEncounter()
        {
            ReachCount++;
            if (_showDebugLogs) Debug.Log($"[Ultimate] {gameObject.name} encountered an enemy. Total reaches: {ReachCount}");
            Managers.TutorialManager tm = FindFirstObjectByType<Managers.TutorialManager>();
            if (tm != null) tm.OnActionTriggered("UnitReach");
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

            // Start Cut-In Animation
            if (MaouSamaTD.UI.UltimateCutInUI.Instance != null)
            {
                string uName = Data.UnitName;
                string uTitle = Data.UnitTitle;
                string sName = Data.UltimateSkill.SkillName;
                Color bColor = visuals.UltimateColor;
                Color tBgColor = visuals.TitleBgColor;
                Color sBgColor = visuals.SkillNameBgColor;
                Sprite portrait = Data.GetSprite(UnitData.UnitImageType.WaistUp);

                if (_showDebugLogs) Debug.Log($"[Ultimate] Triggering Cut-In Animation for {uName}...");
                // Wait for the full animation sequence (Slide In -> Hold -> Slide Out) to complete
                yield return MaouSamaTD.UI.UltimateCutInUI.Instance.PlayAnimation(uName, uTitle, sName, bColor, tBgColor, sBgColor, portrait);
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
                // Usually ultimate animations have a fixed duration or we can wait for state end
                yield return new WaitForSeconds(1.5f); // Approximation or wait for state
                if (_animator != null && !_isDead)
                {
                    _animator.Play("Idle", 0, 0f);
                }
            }
            else
            {
                Debug.LogError($"[Ultimate] Prefab on {_data.UltimateSkill.SkillName} is missing an UltimateEffect component!");
            }
        }

        private Vector3 FindBestUltimateDirection()
        {
            // Align with Grid Axes: Forward (+Z), Back (-Z), Right (+X), Left (-X)
            Vector3[] directions = { 
                Vector3.forward, 
                Vector3.back,    
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
            if (!ActiveUnits.Contains(this)) ActiveUnits.Add(this);
            _unitClass = data.Class;
            _deploymentCost = data.DeploymentCost;
            
            // Set dynamic name for tutorial targeting (e.g., Unit_Ignis)
            gameObject.name = "Unit_" + data.UnitName;
            
            UpdateVisuals(data);
        }
        
        private void OnDestroy()
        {
            ActiveUnits.Remove(this);
        }

        private void UpdateVisuals(UnitData data)
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            if (_billboard == null) _billboard = GetComponentInChildren<Billboard>();

            if (data.GetSprite(UnitData.UnitImageType.Chibi) != null)
            {
                if (_spriteRenderer != null) 
                {
                    _spriteRenderer.enabled = true;
                    _spriteRenderer.sprite = data.GetSprite(UnitData.UnitImageType.Chibi);
                }
            }

            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_animator != null && data.GetAnimatorController() != null)
            {
                _animator.runtimeAnimatorController = data.GetAnimatorController();
            }
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

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.CurrentHp <= 0 || enemy.IsDead) continue;

                Vector2Int enemyPos = _gridManager.WorldToGridCoordinates(enemy.transform.position);
                
                if (IsTargetInPattern(myPos, enemyPos, pattern, range))
                {
                     if (_animator != null) _animator.Play("Attack", 0, 0f);
                     DamageType dType = _data != null ? _data.DamageType : DamageType.Melee;
                     enemy.TakeDamage(AttackPower, this, dType); // Pass self as attacker and damage type
                     attacked = true;
                     FaceTarget(enemy.transform.position);

                     if (type == AttackType.SingleTarget)
                     {
                         break;
                     }
                }
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
             _spriteRenderer.flipX = !isTargetRight; 
        }



        public void Retreat()
        {
            _currentHp = 0;
            
            if (CurrentTile != null)
            {
                CurrentTile.SetOccupant(null); 
                CurrentTile = null;
            }

            OnRetreat?.Invoke(this);
            if (_interactionManager != null) _interactionManager.NotifyUnitRemoved(this);
            
            Destroy(gameObject);
        }

        protected override void Die()
        {
            if (CurrentTile != null)
            {
                CurrentTile.SetOccupant(null);
                CurrentTile = null;
            }

            if (_interactionManager != null) _interactionManager.NotifyUnitRemoved(this);
            base.Die();
        }
    }
}
