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
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TextMeshPro _textRenderer;
        [SerializeField] private Billboard _billboard;

        public override void Initialize(UnitData data)
        {
            base.Initialize(data);
            _unitClass = data.Class;
            _deploymentCost = data.DeploymentCost;

            UpdateVisuals(data);
        }

        private void UpdateVisuals(UnitData data)
        {
            // Ensure components exist if not assigned
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_textRenderer == null) _textRenderer = GetComponentInChildren<TextMeshPro>();
            if (_billboard == null) _billboard = GetComponentInChildren<Billboard>();

            // If still null, we might need to create them dynamically, 
            // but for now let's assume Prefab has them or we create a child object.
            if (_spriteRenderer == null || _textRenderer == null)
            {
                CreateVisualContainer();
            }

            if (data.UnitSprite != null)
            {
                if (_spriteRenderer != null) 
                {
                    _spriteRenderer.enabled = true;
                    _spriteRenderer.sprite = data.UnitSprite;
                }
                if (_textRenderer != null) _textRenderer.enabled = false;
            }
            else
            {
                if (_spriteRenderer != null) _spriteRenderer.enabled = false;
                if (_textRenderer != null) 
                {
                    _textRenderer.enabled = true;
                    _textRenderer.text = !string.IsNullOrEmpty(data.UnitName) ? data.UnitName.Substring(0, 1) : "?";
                    _textRenderer.fontSize = 5;
                    _textRenderer.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        private void CreateVisualContainer()
        {
            // Create a child object for visuals if it doesn't exist
            GameObject visualObj = new GameObject("Visuals");
            visualObj.transform.SetParent(transform);
            visualObj.transform.localPosition = Vector3.up * 1f; // Raise slightly above pivot

            _billboard = visualObj.AddComponent<Billboard>();
            if (_spriteRenderer == null) _spriteRenderer = visualObj.AddComponent<SpriteRenderer>();
            if (_textRenderer == null) _textRenderer = visualObj.AddComponent<TextMeshPro>();
           
            // Default settings
            _textRenderer.rectTransform.sizeDelta = new Vector2(2, 2);
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
