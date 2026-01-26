using UnityEngine;
using System;
using TMPro;
using DG.Tweening; // To handle Flash
using MaouSamaTD.Managers;



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
        [SerializeField] protected ParticleSystem _healParticle;
        
        // Data derived from UnitData
        protected UnitData _data;
        public UnitData Data => _data;

        public event Action OnDeath;
        public event Action<float> OnHealthChanged;

        protected float _lastAttackTime;
        private Transform _camTransform;

        // Outline Logic
        private MaterialPropertyBlock _mpb;
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineEnabledId = Shader.PropertyToID("_OutlineEnabled");

        protected virtual void Awake()
        {
            _mpb = new MaterialPropertyBlock();

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

        protected virtual void UpdateVisuals()
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

        public void SetHighlight(bool active, Color color)
        {
            if (_spriteRenderer == null) return;

            _spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(OutlineEnabledId, active ? 1f : 0f);
            if (active)
            {
                _mpb.SetColor(OutlineColorId, color);
            }
            _spriteRenderer.SetPropertyBlock(_mpb);
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
            if (amount <= 0) return;

            // Always play particle if assigned
            if (_healParticle != null)
            {
                _healParticle.Play();
            }

            // If already at max HP, skip actual healing and text
            if(_currentHp >= _maxHp) return;

            _currentHp = Mathf.Min(_currentHp + amount, _maxHp);
            
            // Visuals: Only show text if we actually healed
            if (FloatingTextManager.Instance != null)
            {
                FloatingTextManager.Instance.ShowHeal(transform.position, amount);
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
