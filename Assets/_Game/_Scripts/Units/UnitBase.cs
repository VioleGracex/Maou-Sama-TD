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
        
        protected bool _isDead = false;
        public bool IsDead => _isDead;

        [Header("Visuals")]
        [SerializeField] protected SpriteRenderer _spriteRenderer;
        [SerializeField] protected Animator _animator;
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

            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            
            if (_data.GetSprite(UnitData.UnitImageType.Chibi) != null)
            {
                if (_spriteRenderer != null) 
                {
                    _spriteRenderer.enabled = true;
                    _spriteRenderer.sprite = _data.GetSprite(UnitData.UnitImageType.Chibi);
                }
                if (_textFallback != null) _textFallback.gameObject.SetActive(false);
            }
            else
            {
                if (_spriteRenderer != null) _spriteRenderer.enabled = false;
                if (_textFallback != null)
                {
                    _textFallback.gameObject.SetActive(true);
                    if (!string.IsNullOrEmpty(_data.UnitName))
                        _textFallback.text = _data.UnitName.Substring(0, 1).ToUpper();
                    else
                        _textFallback.text = "?";
                }
                if (_animator != null && _data.GetAnimatorController() != null)
                {
                    _animator.runtimeAnimatorController = _data.GetAnimatorController();
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

        [Header("Combat (Dynamics)")]
        public System.Collections.Generic.List<DamageType> Immunities = new System.Collections.Generic.List<DamageType>();

        public virtual void TakeDamage(float amount, UnitBase attacker = null, DamageType damageType = DamageType.Melee, bool isSkill = false)
        {
            if (_isDead) return;

            float finalAmount = amount;

            // Skills/Ultimates bypass regular damage type immunities
            if (!isSkill && Immunities.Contains(damageType))
            {
                if (_showDebugLogs) Debug.Log($"[Immunity] {gameObject.name} is immune to {damageType}! Damage nullified.");
                finalAmount = 0;
            }

            float damageTaken = Mathf.Max(finalAmount > 0 ? 1 : 0, finalAmount - _defense); 
            if (_showDebugLogs) Debug.Log($"[Damage] {gameObject.name} taking {damageTaken} ({amount} {damageType} - {_defense} def, isSkill: {isSkill}). HP: {_currentHp} -> {_currentHp - damageTaken}");
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
            if (_isDead) return;
            _isDead = true;

            if (_showDebugLogs) Debug.Log($"[Death] {gameObject.name} has died.");

            // Disable interactions immediately
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var c in colliders) c.enabled = false;

            var colliders2D = GetComponentsInChildren<Collider2D>();
            foreach (var c in colliders2D) c.enabled = false;

            // Stop visual effects
            if (_hpFillImage != null && _hpFillImage.canvas != null)
                _hpFillImage.canvas.gameObject.SetActive(false);

            if (_textFallback != null)
                _textFallback.gameObject.SetActive(false);

            OnDeath?.Invoke();

            // Handle Animation
            if (_animator != null)
            {
                // Try playing common death state names
                _animator.Play("Die", 0, 0f);
                _animator.Play("Death", 0, 0f);
                
                StartCoroutine(DelayedDestroy(_animator));
                return;
            }

            Destroy(gameObject);
        }

        private System.Collections.IEnumerator DelayedDestroy(Animator animator)
        {
            // Give a bit of time for the transition to start
            yield return new WaitForSeconds(0.1f);

            float timeout = 5f; // Hard limit for death animation
            float elapsed = 0.1f;

            while (elapsed < timeout)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                // If we are not in a death-related state anymore after starting, or if the animation finished
                if (!state.IsName("Die") && !state.IsName("Death") && elapsed > 0.5f)
                    break;

                if (state.normalizedTime >= 1.0f && !animator.IsInTransition(0))
                    break;

                yield return null;
                elapsed += Time.deltaTime;
            }

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
                case AttackPattern.Custom:
                    if (_data == null || _data.CustomPatternOffsets == null) return false;
                    Vector2Int offset = target - origin;
                    return _data.CustomPatternOffsets.Contains(offset);
                default:
                    return false;
            }
        }
    }
}
