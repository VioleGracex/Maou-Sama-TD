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
        [Header("Player Unit Stats")]
        [SerializeField] private UnitClass _unitClass;
        [SerializeField] private int _deploymentCost = 10;
        
        // Blocked Enemies tracking could go here

        public UnitClass UnitClass => _unitClass;
        public int BlockCount => _data != null ? _data.BlockCount : 1;
        public int DeploymentCost => _deploymentCost;

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
             // Additional Player Unit logic
        }

        // Color coding for debug
        private void OnDrawGizmos()
        {
            Gizmos.color = _unitClass == UnitClass.Melee ? Color.blue : Color.yellow;
            Gizmos.DrawSphere(transform.position + Vector3.up * 1f, 0.3f);
        }
    }
}
