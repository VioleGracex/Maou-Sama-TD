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
        All             // * (Surrounding 8 tiles)
    }

    public enum AttackType
    {
        SingleTarget,
        AreaOfEffect
    }

    public class PlayerUnit : UnitBase
    {
        public static System.Collections.Generic.List<PlayerUnit> ActiveUnits = new System.Collections.Generic.List<PlayerUnit>();
        public event System.Action<PlayerUnit> OnRetreat;

        [Header("Player Unit Stats")]
        [SerializeField] private UnitClass _unitClass;
        [SerializeField] private int _deploymentCost = 10;
        
        public UnitClass UnitClass => _unitClass;
        public int BlockCount => _data != null ? _data.BlockCount : 1;
        public int DeploymentCost => _deploymentCost;
        
        public Grid.Tile CurrentTile { get; set; }

        private float _currentCharge;
        public float CurrentCharge => _currentCharge;
        public float MaxCharge => _data != null ? _data.MaxCharge : 100f;
        public int KillCount { get; private set; }
        public int ReachCount { get; private set; }

        public void NotifyEncounter()
        {
            ReachCount++;
            Debug.Log($"[tutorial] {gameObject.name} encountered an enemy. Total reaches: {ReachCount}");
            Managers.TutorialManager tm = FindFirstObjectByType<Managers.TutorialManager>();
            if (tm != null) tm.OnActionTriggered("UnitReach");
        }

        public void IncrementKillCount()
        {
            KillCount++;
            Debug.Log($"[tutorial] {Data?.UnitName} now has {KillCount} kills.");
            Managers.TutorialManager tm = FindFirstObjectByType<Managers.TutorialManager>();
            if (tm != null) tm.OnActionTriggered("UnitKill"); 
        }

        public void UseSkill()
        {
            if (_data != null && _data.Skill != null)
            {
                float cost = _data.Skill.ChargeCost;
                
                if (_currentCharge >= cost)
                {
                    Debug.Log($"Used Skill: {_data.Skill.SkillName}!");
                    _currentCharge -= cost;
                    StartCoroutine(ExecuteUltimateRoutine());
                    
                    Managers.TutorialManager tm = FindFirstObjectByType<Managers.TutorialManager>();
                    if (tm != null) tm.OnActionTriggered("SkillUsed");
                }
                else
                {
                    Debug.Log($"Not enough charge! ({_currentCharge}/{cost})");
                }
            }
            else
            {
                 Debug.LogWarning("Cannot use skill: No UnitData or SkillData assigned.");
            }
        }

        private IEnumerator ExecuteUltimateRoutine()
        {
            if (_data.Skill == null || _data.Skill.UltimatePrefab == null)
            {
                Debug.LogWarning($"[tutorial] {Data?.UnitName} has no skill or ultimate prefab assigned!");
                yield break;
            }

            // Play Cut-In Animation
            if (MaouSamaTD.UI.UltimateCutInUI.Instance != null)
            {
                string uName = _data != null ? _data.UnitName : "Unknown";
                string uTitle = _data != null ? _data.UnitTitle : "Vassal";
                string sName = (_data != null && _data.Skill != null) ? _data.Skill.SkillName : "Ultimate";
                Color bColor = (_data != null && _data.Skill != null) ? _data.Skill.UltimateColor : Color.red;

                yield return MaouSamaTD.UI.UltimateCutInUI.Instance.PlayAnimation(uName, uTitle, sName, bColor);
            }

            Vector3 bestDir = FindBestUltimateDirection();
            GameObject projObj = Instantiate(_data.Skill.UltimatePrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
            
            var ultimateEffect = projObj.GetComponent<MaouSamaTD.Skills.UltimateEffect>();
            if (ultimateEffect != null)
            {
                ultimateEffect.Execute(this, bestDir);
            }
            else
            {
                Debug.LogError($"[Ultimate] Prefab on {_data.Skill.SkillName} is missing an UltimateEffect component!");
            }
            
            Debug.Log($"[tutorial] {Data?.UnitName} activated {projObj.name} towards {bestDir}");
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
            Debug.Log($"[tutorial] {gameObject.name} ultimate forcefully charged.");
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

            if (data.UnitSprite != null)
            {
                if (_spriteRenderer != null) 
                {
                    _spriteRenderer.enabled = true;
                    _spriteRenderer.sprite = data.UnitSprite;
                }
            }
        }

        [Zenject.Inject] private Managers.InteractionManager _interactionManager;
        private Grid.GridManager _gridManager;

        protected override void UpdateInternal()
        {
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
                if (enemy == null || enemy.CurrentHp <= 0) continue;

                Vector2Int enemyPos = _gridManager.WorldToGridCoordinates(enemy.transform.position);
                
                if (IsTargetInPattern(myPos, enemyPos, pattern, range))
                {
                     enemy.TakeDamage(AttackPower, this); // Pass self as attacker
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
