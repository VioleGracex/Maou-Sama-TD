using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
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
        
        public float MaxHp => _maxHp;
        public float CurrentHp => _currentHp;
        public float AttackPower => _attackPower;
        public float Defense => _defense;
        public virtual float Range => _data != null ? _data.Range : 0f;

        [Header("Visuals")]
        [SerializeField] protected SpriteRenderer _spriteRenderer;
        [SerializeField] protected TextMeshProUGUI _textFallback; 
        [SerializeField] protected Image _hpFillImage;
        
        [Header("Effects")]
        [SerializeField] protected ParticleSystem _healParticle;
        
        protected UnitData _data;
        public UnitData Data => _data;

        public event Action OnDeath;
        public event Action<float> OnHealthChanged;

        protected float _lastAttackTime;
        private Transform _camTransform;

        private MaterialPropertyBlock _mpb;
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineEnabledId = Shader.PropertyToID("_OutlineEnabled");

        [Header("Debug")]
        [SerializeField] protected bool _showDebugLogs = true;

        protected virtual void Awake()
        {
            _mpb = new MaterialPropertyBlock();

            if (_spriteRenderer == null)
            {
                GameObject spriteObj = new GameObject("Sprite");
                spriteObj.transform.SetParent(transform);
                spriteObj.transform.localPosition = new Vector3(0, 1, 0); 
                _spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
            }

            // Ensure Billboard on Sprite if it's a child
            if (_spriteRenderer.transform != transform)
            {
                SetupBillboard(_spriteRenderer.gameObject);
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

            if (_textFallback.transform != transform)
            {
                SetupBillboard(_textFallback.gameObject);
            }
            
            _camTransform = Camera.main != null ? Camera.main.transform : null;
        }

        private void SetupBillboard(GameObject target)
        {
            if (target.GetComponent<MaouSamaTD.Utils.Billboard>() == null)
            {
                target.AddComponent<MaouSamaTD.Utils.Billboard>();
            }
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

            if (_hpFillImage != null)
            {
                _hpFillImage.fillAmount = 1f;
                // Setup Billboard on HP Canvas if it's world space
                if (_hpFillImage.canvas != null && _hpFillImage.canvas.renderMode == RenderMode.WorldSpace)
                {
                    _hpFillImage.canvas.worldCamera = Camera.main;
                    SetupBillboard(_hpFillImage.canvas.gameObject);
                }
            }

            UpdateVisuals();
        }

        protected virtual void UpdateInternal()
        {
            // Unit specific logic here
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

        public virtual void TakeDamage(float amount, UnitBase attacker = null)
        {
            float damageTaken = Mathf.Max(1, amount - _defense); 
            if (_showDebugLogs) Debug.Log($"[Damage] {gameObject.name} taking {damageTaken} ({amount} - {_defense} def). HP: {_currentHp} -> {_currentHp - damageTaken}");
            _currentHp -= damageTaken;
            
            if (_hpFillImage != null)
            {
                 _hpFillImage.fillAmount = _currentHp / _maxHp;
            }

            if (_spriteRenderer != null)
            {
                // Kill previous to prevent stacking offsets
                _spriteRenderer.DOKill(true); // Complete active tweens and reset to target
                _spriteRenderer.transform.DOKill(true);
                
                // Return to base position (in case kill didn't reset it perfectly due to stacking)
                _spriteRenderer.transform.localPosition = new Vector3(0, 1f, 0); // Correct for UnitBase default

                _spriteRenderer.DOColor(Color.red, 0.1f).OnComplete(() => _spriteRenderer.DOColor(Color.white, 0.1f));
                _spriteRenderer.transform.DOShakePosition(0.2f, 0.15f, 15, 90f, false, true);
            }

            if (FloatingTextManager.Instance != null)
            {
                bool isCrit = damageTaken > _attackPower * 1.5f; 
                FloatingTextManager.Instance.ShowDamage(transform.position, damageTaken, isCrit);
            }

            OnHealthChanged?.Invoke(_currentHp / _maxHp);

            if (_currentHp <= 0)
            {
                if (attacker is PlayerUnit player) player.IncrementKillCount();
                Die();
            }
        }

        public virtual void Heal(float amount)
        {
            if (amount <= 0) return;

            if (_healParticle != null)
            {
                _healParticle.Play();
            }

            if(_currentHp >= _maxHp) return;

            _currentHp = Mathf.Min(_currentHp + amount, _maxHp);
            
            if (_hpFillImage != null)
            {
                 _hpFillImage.fillAmount = _currentHp / _maxHp;
            }
            
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

        protected bool IsTargetInPattern(Vector2Int origin, Vector2Int target, AttackPattern pattern, float range)
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
                    return dx <= iRange && dy <= iRange; 
                default:
                    return false;
            }
        }
    }
}
