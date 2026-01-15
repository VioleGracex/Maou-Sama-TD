using UnityEngine;
using System;
using TMPro;

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
        [SerializeField] protected TextMeshProUGUI _textFallback; // Simple 3D Text legacy or TMPro if preferred, using legacy for simplicity unless TMPro is mandated
        
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
                _textFallback = textObj.AddComponent<TextMeshProUGUI>();
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
            // Billboard handled by component on Visuals
        }
        
        private void Update()
        {
            UpdateInternal();
        }

        protected void UpdateVisuals()
        {
            if (_data == null) return;

            if (_data.UnitSprite != null)
            {
                _spriteRenderer.sprite = _data.UnitSprite;
                _spriteRenderer.enabled = true;
                if (_textFallback != null) _textFallback.gameObject.SetActive(false);
            }
            else
            {
                _spriteRenderer.enabled = false;
                if (_textFallback != null)
                {
                    _textFallback.gameObject.SetActive(true);
                    // Use first letter
                    if (!string.IsNullOrEmpty(_data.UnitName))
                        _textFallback.text = _data.UnitName.Substring(0, 1).ToUpper();
                    else
                        _textFallback.text = "?";
                }
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
    }
}
