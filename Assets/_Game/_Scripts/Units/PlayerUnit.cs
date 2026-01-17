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
            if (_data != null)
            {
                if (_currentCharge >= MaxCharge)
                {
                    Debug.Log($"Used Skill: {_data.SkillName}!");
                    // Implement skill logic here later
                    
                    // Consume Charge
                    _currentCharge = 0f;
                }
                else
                {
                    Debug.Log($"Not enough charge! ({_currentCharge}/{MaxCharge})");
                }
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




        protected override void UpdateInternal()
        {
             base.UpdateInternal();
             
             // Passive Charge Generation
             if (_data != null && _currentCharge < MaxCharge)
             {
                 _currentCharge += _data.ChargePerSecond * Time.deltaTime;
                 if (_currentCharge > MaxCharge) _currentCharge = MaxCharge;
             }
        }

        // Color coding for debug
        private void OnDrawGizmos()
        {
            Gizmos.color = _unitClass == UnitClass.Melee ? Color.blue : Color.yellow;
            Gizmos.DrawSphere(transform.position + Vector3.up * 1f, 0.3f);
        }

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
