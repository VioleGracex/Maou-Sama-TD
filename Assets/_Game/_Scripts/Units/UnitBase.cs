using UnityEngine;
using System;

namespace MaouSamaTD.Units
{
    public abstract class UnitBase : MonoBehaviour
    {
        [Header("Base Stats")]
        [SerializeField] protected float _maxHp = 100f;
        [SerializeField] protected float _currentHp;
        [SerializeField] protected float _attackPower = 10f;
        [SerializeField] protected float _attackInterval = 1f;
        [SerializeField] protected float _defense = 0f;

        [Header("Visuals")]
        [SerializeField] protected SpriteRenderer _spriteRenderer;
        [SerializeField] protected TextMesh _textFallback; // Simple 3D Text legacy or TMPro if preferred, using legacy for simplicity unless TMPro is mandated
        
        // Data derived from UnitData
        protected UnitData _data;

        public event Action OnDeath;
        public event Action<float> OnHealthChanged;

        protected float _lastAttackTime;
        private Transform _camTransform;

        protected virtual void Awake()
        {
            // Ensure we have visual components if not assigned
            if (_spriteRenderer == null)
            {
                GameObject spriteObj = new GameObject("Sprite");
                spriteObj.transform.SetParent(transform);
                spriteObj.transform.localPosition = new Vector3(0, 1, 0); // Raised up
                _spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
            }
            if (_textFallback == null)
            {
                GameObject textObj = new GameObject("TextFallback");
                textObj.transform.SetParent(transform);
                textObj.transform.localPosition = new Vector3(0, 1, 0);
                _textFallback = textObj.AddComponent<TextMesh>();
                _textFallback.anchor = TextAnchor.MiddleCenter;
                _textFallback.alignment = TextAlignment.Center;
                _textFallback.characterSize = 0.5f;
                _textFallback.fontSize = 20;
                _textFallback.color = Color.white;
            }
            
            _camTransform = Camera.main != null ? Camera.main.transform : null;
        }

        public virtual void Initialize(UnitData data)
        {
            _data = data;
            _maxHp = data.MaxHp;
            _currentHp = _maxHp;
            _attackPower = data.AttackPower;
            _attackInterval = data.AttackInterval;
            _defense = data.Defense;
            
            name = data.UnitName;

            UpdateVisuals();
        }

        protected virtual void UpdateInternal()
        {
            Billboard();
        }
        
        private void Update()
        {
            UpdateInternal();
        }

        private void Billboard()
        {
            if (_camTransform == null) 
            {
                _camTransform = Camera.main?.transform;
                if (_camTransform == null) return;
            }

            // Simple billboard: Look at camera
            // We rotate the visual part, or the whole object if movement is handled separately
            // Since this is a 3D world with 2D sprites, we usually want the sprite to face the camera.
            // Let's assume the SpriteRenderer is on a child object or we rotate this object's Y.
            // For now, let's rotate the whole object to face camera but keep upright? 
            // Or just rotate the Sprite/Text children. 
            
            // Let's rotate the Sprite and Text objects to face camera
            if (_spriteRenderer != null) 
                _spriteRenderer.transform.forward = _camTransform.forward;
            if (_textFallback != null)
                _textFallback.transform.forward = _camTransform.forward;
        }

        protected void UpdateVisuals()
        {
            if (_data == null) return;

            if (_data.UnitSprite != null)
            {
                _spriteRenderer.sprite = _data.UnitSprite;
                _spriteRenderer.enabled = true;
                _textFallback.gameObject.SetActive(false);
            }
            else
            {
                _spriteRenderer.enabled = false;
                _textFallback.gameObject.SetActive(true);
                // Use first letter
                if (!string.IsNullOrEmpty(_data.UnitName))
                    _textFallback.text = _data.UnitName.Substring(0, 1).ToUpper();
                else
                    _textFallback.text = "?";
            }
        }

        public virtual void TakeDamage(float amount)
        {
            float damageTaken = Mathf.Max(1, amount - _defense); // Minimum 1 damage
            _currentHp -= damageTaken;
            OnHealthChanged?.Invoke(_currentHp / _maxHp);

            if (_currentHp <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            OnDeath?.Invoke();
            Destroy(gameObject);
        }

        // Basic visual debug for HP
        protected virtual void OnGUI()
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
            if (screenPos.z > 0)
            {
                GUI.Label(new Rect(screenPos.x - 20, Screen.height - screenPos.y, 100, 20), $"{_currentHp}/{_maxHp}");
            }
        }
    }
}
