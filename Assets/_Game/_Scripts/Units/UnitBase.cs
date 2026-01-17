using UnityEngine;
using System;
using TMPro;
using DG.Tweening; // To handle Flash
using MaouSamaTD.Managers;

using MaouSamaTD.Managers; // Added namespace for Manager

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
        
        // Public Accessors
        public float MaxHp => _maxHp;
        public float CurrentHp => _currentHp;
        public float AttackPower => _attackPower;
        public float Defense => _defense;
        public virtual float Range => _data != null ? _data.Range : 0f; // Default linkage to data, can be overridden

        [Header("Visuals")]
        [SerializeField] protected SpriteRenderer _spriteRenderer;
        [SerializeField] protected TextMeshProUGUI _textFallback; // Simple 3D Text legacy or TMPro if preferred, using legacy for simplicity unless TMPro is mandated
        
        [Header("Effects")]
        [SerializeField] protected GameObject _healParticlePrefab;

        [Header("Debug")]
        [SerializeField] private float _debugDamageVal = 10f;
        [SerializeField] private float _debugHealVal = 10f;

        [ContextMenu("Debug Damage")]
        private void DebugTakeDamage() => TakeDamage(_debugDamageVal);

        [ContextMenu("Debug Heal")]
        private void DebugHeal() => Heal(_debugHealVal);
        
        // Data derived from UnitData
        protected UnitData _data;
        public UnitData Data => _data;

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
            
            // Visuals: Flash Red
            if (_spriteRenderer != null)
            {
                _spriteRenderer.DOColor(Color.red, 0.1f).OnComplete(() => _spriteRenderer.DOColor(Color.white, 0.1f));
            }

            // Show Floating Text
            if (FloatingTextManager.Instance != null)
            {
                // Simple logic: if damage > attackPower * 1.5 (arbitrary) -> Crit? 
                // Or just pass false for now unless we have crit logic.
                bool isCrit = damageTaken > _attackPower * 1.5f; 
                FloatingTextManager.Instance.ShowDamage(transform.position, damageTaken, isCrit);
            }

            OnHealthChanged?.Invoke(_currentHp / _maxHp);

            if (_currentHp <= 0)
            {
                Die();
            }
        }

        public virtual void Heal(float amount)
        {
            if (_currentHp >= _maxHp || amount <= 0) return;

            _currentHp = Mathf.Min(_currentHp + amount, _maxHp);
            
            // Visuals
            if (FloatingTextManager.Instance != null)
            {
                FloatingTextManager.Instance.ShowHeal(transform.position, amount);
            }

            if (_healParticlePrefab != null)
            {
                Instantiate(_healParticlePrefab, transform.position, Quaternion.identity);
            }
            
            OnHealthChanged?.Invoke(_currentHp / _maxHp);
        }

        protected virtual void Die()
        {
            OnDeath?.Invoke();
            Destroy(gameObject);
        }
    }
}
