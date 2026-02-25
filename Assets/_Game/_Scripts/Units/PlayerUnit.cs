using TMPro;
using MaouSamaTD.Utils;
using UnityEngine;

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

        public void UseSkill()
        {
            if (_data != null && _data.Skill != null)
            {
                float cost = _data.Skill.ChargeCost;
                
                if (_currentCharge >= cost)
                {
                    Debug.Log($"Used Skill: {_data.Skill.SkillName}!");
                    _currentCharge -= cost;
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
        [SerializeField] private Billboard _billboard;

        public override void Initialize(UnitData data)
        {
            base.Initialize(data);
            _unitClass = data.Class;
            _deploymentCost = data.DeploymentCost;
            UpdateVisuals(data);
        }
        
        private void OnDestroy()
        {
            
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
            if (_gridManager == null) _gridManager = FindObjectOfType<Grid.GridManager>();
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
                     enemy.TakeDamage(AttackPower);
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
