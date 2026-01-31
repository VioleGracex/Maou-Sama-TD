using TMPro;
using MaouSamaTD.Utils;
using UnityEngine;

namespace MaouSamaTD.Units
{
    public enum UnitClass
    {
        Melee,  // Blocks enemies, placed on Low Ground
                Healer,   // Restores health
Ranged  // Deals damage from afar, placed on High Ground
    }

    public enum AttackPattern
    {
        Vertical,       // |
        Horizontal,     // -
        Diagonal,       // X
        Cross,          // + (Vertical + Horizontal)
        All             // * (Surrounding 8 tiles)
    }

    public class PlayerUnit : UnitBase
    {
        public event System.Action<PlayerUnit> OnRetreat;

        [Header("Player Unit Stats")]
        [SerializeField] private UnitClass _unitClass;
        [SerializeField] private int _deploymentCost = 10;
        
        // Blocked Enemies tracking could go here

        public UnitClass UnitClass => _unitClass;
        public int BlockCount => _data != null ? _data.BlockCount : 1;
        public int DeploymentCost => _deploymentCost;
        
        public Grid.Tile CurrentTile { get; set; }

        private float _currentCharge;
        public float CurrentCharge => _currentCharge;
        public float MaxCharge => _data != null ? _data.MaxCharge : 100f;

        public void UseSkill()
        {
            if (_data != null && _data.Skill != null)
            {
                // Use Skill.ChargeCost
                float cost = _data.Skill.ChargeCost;
                
                if (_currentCharge >= cost)
                {
                    Debug.Log($"Used Skill: {_data.Skill.SkillName}!");
                    
                    // Consume Charge
                    _currentCharge -= cost;
                    
                    // TODO: Actual effect execution using _data.Skill properties
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
        
        public void AddCharge(float amount)
        {
            if (_data == null) return;
            _currentCharge = Mathf.Min(_currentCharge + amount, MaxCharge);
        }

        [Header("Visuals")]
        [SerializeField] private UnityEngine.UI.Image _hpBarFill; // World Space Canvas Image
        [SerializeField] private Billboard _billboard;

        public override void Initialize(UnitData data)
        {
            base.Initialize(data);
            _unitClass = data.Class;
            _deploymentCost = data.DeploymentCost;
            
            // Listen to health changes
            OnHealthChanged += UpdateHealthBar;

            UpdateVisuals(data);
        }
        
        private void OnDestroy()
        {
            OnHealthChanged -= UpdateHealthBar;
        }

        private void UpdateHealthBar(float pct)
        {
            if (_hpBarFill != null)
                _hpBarFill.fillAmount = pct;
        }

        private void UpdateVisuals(UnitData data)
        {
            // Ensure components exist if not assigned
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            // We expect _hpBarFill to be assigned via Prefab or created in a specific Canvas hierarchy
            // If it's null, we might be in trouble since creating a canvas from scratch here is verbose.
            // But we can try to find it.
            if (_hpBarFill == null) _hpBarFill = GetComponentInChildren<UnityEngine.UI.Image>();
            
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




        // Cache cachedGrid for Update loop
        private Grid.GridManager _gridManager;

        protected override void UpdateInternal()
        {
             base.UpdateInternal();
             
             // Passive Charge Generation
             if (_data != null && _currentCharge < MaxCharge)
             {
                 _currentCharge += _data.ChargePerSecond * Time.deltaTime;
                 if (_currentCharge > MaxCharge) _currentCharge = MaxCharge;
             }

             // Attack Logic
             if (Time.time >= _lastAttackTime + _attackInterval)
             {
                 Attack();
             }
        }

        private void Attack()
        {
            if (_gridManager == null) _gridManager = FindObjectOfType<Grid.GridManager>();
            if (_gridManager == null) return;

            Vector2Int myPos;
            if (CurrentTile != null) myPos = CurrentTile.Coordinate;
            else myPos = _gridManager.WorldToGridCoordinates(transform.position);

            // Fetch attack pattern and range
            AttackPattern pattern = _data != null ? _data.AttackPattern : AttackPattern.All;
            float range = Range;

            // Find valid targets
            // Optimization: In a real game, usage of spatial hash or quadtree is preferred.
            // Here we iterate all active enemies since count is low (TD usually < 100)
            
            foreach (var enemy in EnemyUnit.ActiveEnemies)
            {
                if (enemy == null || enemy.CurrentHp <= 0) continue;

                Vector2Int enemyPos = _gridManager.WorldToGridCoordinates(enemy.transform.position);
                
                if (IsTargetInPattern(myPos, enemyPos, pattern, range))
                {
                    // Hit the enemy
                     enemy.TakeDamage(AttackPower);
                     _lastAttackTime = Time.time;
                     
                     // If we want single target vs multi target, we break here.
                     // Assuming splash/multi-target for now based on "Pattern" implies area?
                     // Or just "Range of validity"?
                     // Usually TD units attack 1 target within range unless "AoE".
                     // However, "Beam" patterns often hit all. 
                     // Let's assume Single Target (First found) for standard, or All for AOE?
                     // The prompt didn't specify. I will assume Single Target for now to be safe, unless "All" pattern?
                     // Actually, "AttackPattern" usually defines "Range Shape".
                     // Let's hit the CLOSEST one within logic?
                     // Or just hit one.
                     return; // Single target attack per interval
                }
            }
        }

        private bool IsTargetInPattern(Vector2Int origin, Vector2Int target, AttackPattern pattern, float range)
        {
            int dx = Mathf.Abs(origin.x - target.x);
            int dy = Mathf.Abs(origin.y - target.y);
            int iRange = Mathf.CeilToInt(range);

            if (dx > iRange || dy > iRange) return false;

            switch (pattern)
            {
                case AttackPattern.Vertical:
                    return dx == 0 && dy <= iRange;
                case AttackPattern.Horizontal:
                    return dy == 0 && dx <= iRange;
                case AttackPattern.Cross:
                    return (dx == 0 && dy <= iRange) || (dy == 0 && dx <= iRange);
                case AttackPattern.Diagonal:
                    return dx == dy && dx <= iRange;
                case AttackPattern.All:
                    // Using Chebyshev distance (Square) for "All Surroundings"
                    return dx <= iRange && dy <= iRange; 
                default:
                    return false;
            }
        }

        // Color coding for debug
        /* private void OnDrawGizmos()
        {
            Gizmos.color = _unitClass == UnitClass.Melee ? Color.blue : Color.yellow;
            Gizmos.DrawSphere(transform.position + Vector3.up * 1f, 0.3f);
        } */

        public void Retreat()
        {
            // 1. Clear Tile Occupancy
            if (CurrentTile != null)
            {
                CurrentTile.SetOccupant(null); 
                CurrentTile = null;
            }

            // 2. Trigger Death Event (so Manager knows) or separate OnRetreat?
            // For now, OnDeath handles removal from lists if any.
            // If we have a specific "OnRetreat" event, we can add it.
            // But usually destroying the object is enough for unity checks.
            
            // 2. Trigger Event
            OnRetreat?.Invoke(this);
            
            // 3. Destroy
            Destroy(gameObject);
        }
    }
}
