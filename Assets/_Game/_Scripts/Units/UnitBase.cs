using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using MaouSamaTD.Managers;
using MaouSamaTD.Battle;

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
        
        public float MaxHp => Mathf.Ceil(_maxHp);
        public float CurrentHp => Mathf.Ceil(_currentHp);
        public float AttackPower 
        {
            get
            {
                float mult = 1f;
                if (_activeBuffs != null)
                {
                    foreach (var b in _activeBuffs) 
                        if (b.Stat == MaouSamaTD.Skills.SkillStatType.Attack) mult *= b.Multiplier;
                }
                return Mathf.Ceil(_attackPower * mult);
            }
        }
        public float Defense 
        {
            get
            {
                float mult = 1f;
                if (_activeBuffs != null)
                {
                    foreach (var b in _activeBuffs) 
                        if (b.Stat == MaouSamaTD.Skills.SkillStatType.Defense) mult *= b.Multiplier;
                }
                return Mathf.Ceil(_defense * mult);
            }
        }

        public float AttackInterval 
        {
            get
            {
                float mult = 1f;
                if (_activeBuffs != null)
                {
                    foreach (var b in _activeBuffs) 
                        if (b.Stat == MaouSamaTD.Skills.SkillStatType.AttackSpeed) mult *= (1f / b.Multiplier); // Higher speed = lower interval
                }

                // Perfect Vigor (100) grants a very small +5% attack speed buff (decreases attack interval)
                if (_data != null && _data.Vigor == 100)
                {
                    mult *= (1f / 1.05f);
                }

                return _attackInterval * mult;
            }
        }

        public virtual float Range 
        {
            get
            {
                float baseRange = _data != null ? _data.Range : 0f;
                float mult = 1f;
                if (_activeBuffs != null)
                {
                    foreach (var b in _activeBuffs) 
                        if (b.Stat == MaouSamaTD.Skills.SkillStatType.Range) mult *= b.Multiplier;
                }
                return Mathf.Ceil(baseRange * mult);
            }
        }
        
        protected bool _isDead = false;
        public bool IsDead => _isDead;
        
        public virtual bool IsAttacking()
        {
            if (_animator == null) return false;
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            return state.IsName("Attack");
        }

        [Header("Visuals")]
        [SerializeField] protected SpriteRenderer _spriteRenderer;
        [SerializeField] protected Animator _animator;
        [SerializeField] protected TextMeshProUGUI _textFallback; 
        [SerializeField] protected Image _hpFillImage;
        [SerializeField] protected TextMeshProUGUI _hpText;
        [SerializeField] protected RectTransform _hpBarRoot;
        
        [Header("Effects")]
        [SerializeField] protected ParticleSystem _healParticle;
        
        protected UnitData _data;
        public UnitData Data => _data;

        public event Action OnDeath;
        public event Action<float> OnHealthChanged;

        protected float _lastAttackTime;
        public float TotalDamageDealt { get; protected set; } = 0f;
        private Transform _camTransform;
        protected Vector3 _originalSpriteScale = Vector3.one;

        private MaterialPropertyBlock _mpb;
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineEnabledId = Shader.PropertyToID("_OutlineEnabled");

        [Header("Debug")]
        [SerializeField] protected bool _showDebugLogs = true;

        protected System.Collections.Generic.List<BuffInstance> _activeBuffs = new System.Collections.Generic.List<BuffInstance>();
        public System.Collections.Generic.List<BuffInstance> ActiveBuffs => _activeBuffs;

        protected System.Collections.Generic.Dictionary<UnitBase, float> _damageTakenByAttacker = new System.Collections.Generic.Dictionary<UnitBase, float>();
        public float GetDamageFrom(UnitBase attacker) => (attacker != null && _damageTakenByAttacker.ContainsKey(attacker)) ? _damageTakenByAttacker[attacker] : 0f;

        public virtual System.Collections.Generic.List<Vector2Int> CustomPatternOffsets => _data != null ? _data.CustomPatternOffsets : null;

        [System.Serializable]
        public class BuffInstance
        {
            public string BuffID;
            public MaouSamaTD.Skills.SkillStatType Stat;
            public float Multiplier = 1f;
            public float Duration;
            public float RemainingTime;
        }

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

            if (_spriteRenderer != null)
            {
                _originalSpriteScale = _spriteRenderer.transform.localScale;
            }
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
            if (data.CalculatedStats.MaxHp > 0)
            {
                _maxHp = data.CalculatedStats.MaxHp;
                _attackPower = data.CalculatedStats.Attack;
                _defense = data.CalculatedStats.Defense;
            }
            else
            {
                _maxHp = data.MaxHp;
                _attackPower = data.AttackPower;
                _defense = data.Defense;
            }
            _currentHp = _maxHp;
            _attackInterval = data.AttackInterval;
            
            name = data.UnitName;

            if (_hpFillImage != null)
            {
                _hpFillImage.fillAmount = 1f;
                // Setup Billboard on HP Canvas if it's world space
                if (_hpFillImage.canvas != null && _hpFillImage.canvas.renderMode == RenderMode.WorldSpace)
                {
                    var canvas = _hpFillImage.canvas;
                    canvas.worldCamera = Camera.main;
                    canvas.sortingOrder = 1000; // Force high sorting order to render on top of map textures
                    SetupBillboard(canvas.gameObject);
                }
            }

            if (_hpText != null)
            {
                _hpText.text = $"{Mathf.CeilToInt(_currentHp)} / {Mathf.CeilToInt(_maxHp)}";
                
                // If the health bar is in World Space (i.e. on the field), hide the pixelated text to keep visuals premium
                if (_hpText.canvas != null && _hpText.canvas.renderMode == RenderMode.WorldSpace)
                {
                    _hpText.gameObject.SetActive(false);
                }
            }

            UpdateHPBarColor();
            GenerateHPNotches();
            UpdateVisuals();
        }

        protected virtual void UpdateHPBarColor()
        {
            if (_hpFillImage == null) return;

            if (this is PlayerUnit)
            {
                // Vassal Green: beautiful premium emerald green
                _hpFillImage.color = new Color(0.18f, 0.77f, 0.44f); // #2ecc71 emerald
            }
            else if (this is EnemyUnit enemy)
            {
                if (enemy.EnemyData != null && enemy.EnemyData.IsBoss)
                {
                    // Boss Pulsing Amber
                    _hpFillImage.color = new Color(1f, 0.6f, 0f); 
                }
                else
                {
                    // Enemy Red: vibrant premium crimson/coral red
                    _hpFillImage.color = new Color(0.9f, 0.22f, 0.27f); 
                }
            }
        }

        protected virtual void GenerateHPNotches()
        {
            if (_hpFillImage == null) return;

            Transform notchContainer = _hpFillImage.transform.Find("NotchContainer");
            if (notchContainer != null)
            {
                Destroy(notchContainer.gameObject);
            }

            GameObject containerObj = new GameObject("NotchContainer");
            notchContainer = containerObj.transform;
            notchContainer.SetParent(_hpFillImage.transform, false);
            
            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;
            containerRect.anchoredPosition = Vector2.zero;

            float maxHp = MaxHp;
            if (maxHp <= 0) return;

            float notchInterval = 100f;
            if (maxHp >= 500f) notchInterval = 250f;
            if (maxHp >= 1000f) notchInterval = 500f;

            int notchCount = Mathf.FloorToInt(maxHp / notchInterval);
            if (notchCount <= 0 || notchCount > 30) return;

            for (int i = 1; i <= notchCount; i++)
            {
                float pct = (i * notchInterval) / maxHp;
                if (pct >= 0.98f) continue;

                GameObject notchObj = new GameObject($"Notch_{i}");
                notchObj.transform.SetParent(notchContainer, false);

                Image notchImage = notchObj.AddComponent<Image>();
                notchImage.color = new Color(0f, 0f, 0f, 0.4f);

                RectTransform notchRect = notchObj.GetComponent<RectTransform>();
                notchRect.anchorMin = new Vector2(pct, 0f);
                notchRect.anchorMax = new Vector2(pct, 1f);
                notchRect.pivot = new Vector2(0.5f, 0.5f);
                notchRect.sizeDelta = new Vector2(1.5f, 0f);
                notchRect.anchoredPosition = Vector2.zero;
            }
        }

        protected virtual void LateUpdate()
        {
            // Update sorting order based on Y position to fix isometric depth issues
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
            }

            // 1. Distance compensation to keep world space health bar a constant screen size
            if (_hpBarRoot != null && Camera.main != null)
            {
                float distance = Vector3.Distance(Camera.main.transform.position, _hpBarRoot.position);
                float referenceDistance = 10f;
                float scale = distance / referenceDistance;
                scale = Mathf.Clamp(scale, 0.4f, 2.5f);
                _hpBarRoot.localScale = new Vector3(scale, scale, 1f);
            }

            // 2. Boss HP Bar Pulsing Amber Effect
            if (this is EnemyUnit enemyBoss && enemyBoss.EnemyData != null && enemyBoss.EnemyData.IsBoss && _hpFillImage != null)
            {
                float pulse = 0.8f + Mathf.PingPong(Time.time * 2f, 0.2f);
                _hpFillImage.color = new Color(1f, 0.6f, 0f) * pulse;
            }
        }

        protected virtual void UpdateInternal()
        {
            // Update Buffs
            if (_activeBuffs != null && _activeBuffs.Count > 0)
            {
                for (int i = _activeBuffs.Count - 1; i >= 0; i--)
                {
                    _activeBuffs[i].RemainingTime -= Time.deltaTime;
                    if (_activeBuffs[i].RemainingTime <= 0)
                    {
                        var buff = _activeBuffs[i];
                        _activeBuffs.RemoveAt(i);
                        
                        BattleLogManager.Instance.LogEvent(BattleLogType.BuffExpired, "", gameObject.name, $"Buff Expired: {buff.BuffID}", 0);
                        
                        if (_showDebugLogs) Debug.Log($"[Buff] Buff expired on {gameObject.name}");
                    }
                }
            }
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
            
            // Set up animator
            if (_animator != null)
            {
                var controller = _data.GetAnimatorController();
                if (controller != null)
                {
                    _animator.runtimeAnimatorController = controller;
                    _animator.Play("Idle", 0, 0f);
                }
                else
                {
                    // No controller? Remove the animator component to save resources as requested
                    Destroy(_animator);
                    _animator = null;
                }
            }

            // Set up sprite or fallback
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

        public bool IsCastingUltimate { get; set; } = false;

        [Header("Combat (Dynamics)")]
        public System.Collections.Generic.List<DamageType> Immunities = new System.Collections.Generic.List<DamageType>();
        public bool PreventDeathForTutorial { get; set; } = false;

        public virtual void TakeDamage(float amount, UnitBase attacker = null, DamageType damageType = DamageType.Melee, bool isSkill = false)
        {
            if (_isDead) return;

            float finalAmount = amount;

            // Apply Ultimate Damage Resistance if casting
            if (IsCastingUltimate && _data != null && _data.UltimateDamageResistance > 0)
            {
                float reduction = finalAmount * _data.UltimateDamageResistance;
                if (_showDebugLogs) Debug.Log($"[Ultimate Resistance] {gameObject.name} reduced damage by {reduction} ({_data.UltimateDamageResistance * 100}%).");
                finalAmount -= reduction;
            }

            // Skills/Ultimates bypass regular damage type immunities
            if (!isSkill && Immunities.Contains(damageType))
            {
                if (_showDebugLogs) Debug.Log($"[Immunity] {gameObject.name} is immune to {damageType}! Damage nullified.");
                finalAmount = 0;
            }

            float damageTaken = finalAmount > 0f ? Mathf.Ceil(Mathf.Max(1f, finalAmount - Defense)) : 0f; 

            if (PreventDeathForTutorial && _currentHp - damageTaken <= 1f)
            {
                damageTaken = Mathf.Max(0, _currentHp - 1f);
            }

            if (_showDebugLogs) Debug.Log($"[Damage] {gameObject.name} taking {damageTaken} ({amount} {damageType} - {_defense} def, isSkill: {isSkill}, after resistance: {finalAmount}). HP: {_currentHp} -> {_currentHp - damageTaken}");
            _currentHp -= damageTaken;
            
            BattleLogManager.Instance.LogEvent(BattleLogType.Damage, attacker != null ? attacker.gameObject.name : "Unknown", gameObject.name, "Damage Taken", damageTaken);
            
            if (attacker != null)
            {
                attacker.RecordDamageDealt(damageTaken);
                
                if (!_damageTakenByAttacker.ContainsKey(attacker)) _damageTakenByAttacker[attacker] = 0f;
                _damageTakenByAttacker[attacker] += damageTaken;

                RegisterAttacker(attacker);
            }
            
            if (_hpFillImage != null)
            {
                 _hpFillImage.fillAmount = _currentHp / _maxHp;
            }

            if (_hpText != null)
            {
                _hpText.text = $"{Mathf.CeilToInt(_currentHp)} / {Mathf.CeilToInt(_maxHp)}";
            }

            if (_spriteRenderer != null)
            {
                // Kill previous to prevent stacking offsets
                _spriteRenderer.DOKill(false);
                _spriteRenderer.transform.DOKill(false);
                
                // Return to base position (in case kill didn't reset it perfectly due to stacking)
                _spriteRenderer.transform.localPosition = GetSpriteLocalPosition();
                
                // Restore original non-squished scale, keeping the sign of x-scale for facing direction
                Vector3 currentScale = _spriteRenderer.transform.localScale;
                float currentSignX = Mathf.Sign(currentScale.x);
                Vector3 targetScale = _originalSpriteScale;
                targetScale.x = Mathf.Abs(_originalSpriteScale.x) * currentSignX;
                _spriteRenderer.transform.localScale = targetScale;

                _spriteRenderer.color = Color.white;
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
                Die(attacker);
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
            
            BattleLogManager.Instance.LogEvent(BattleLogType.Heal, "Unknown", gameObject.name, "Healing", amount);

            if (_hpFillImage != null)
            {
                 _hpFillImage.fillAmount = _currentHp / _maxHp;
            }

            if (_hpText != null)
            {
                _hpText.text = $"{Mathf.CeilToInt(_currentHp)} / {Mathf.CeilToInt(_maxHp)}";
            }
            
            if (FloatingTextManager.Instance != null)
            {
                FloatingTextManager.Instance.ShowHeal(transform.position, amount);
            }
            
            OnHealthChanged?.Invoke(_currentHp / _maxHp);
        }

        public virtual void SetHpRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            _currentHp = _maxHp * ratio;
            if (_hpFillImage != null)
            {
                _hpFillImage.fillAmount = ratio;
            }
            if (_hpText != null)
            {
                _hpText.text = $"{Mathf.CeilToInt(_currentHp)} / {Mathf.CeilToInt(_maxHp)}";
            }
            OnHealthChanged?.Invoke(ratio);
        }

        public virtual void ApplyBuff(string id, MaouSamaTD.Skills.SkillStatType stat, float multiplier, float duration)
        {
            if (_activeBuffs == null) _activeBuffs = new System.Collections.Generic.List<BuffInstance>();

            // Non-stacking logic: If a buff for this STAT already exists, we only keep the strongest one.
            var existingSameStat = _activeBuffs.Find(b => b.Stat == stat);
            if (existingSameStat != null)
            {
                // If same source, refresh duration and update multiplier
                if (existingSameStat.BuffID == id)
                {
                    existingSameStat.RemainingTime = duration;
                    existingSameStat.Multiplier = multiplier;
                    return;
                }
                else
                {
                    // Different source. Replace if the new one is stronger or equal.
                    if (multiplier >= existingSameStat.Multiplier)
                    {
                        _activeBuffs.Remove(existingSameStat);
                    }
                    else
                    {
                        // New one is weaker, ignore it.
                        return;
                    }
                }
            }
            _activeBuffs.Add(new BuffInstance 
            { 
                BuffID = id, 
                Stat = stat,
                Multiplier = multiplier, 
                Duration = duration, 
                RemainingTime = duration 
            });

            BattleLogManager.Instance.LogEvent(BattleLogType.BuffApplied, "Ability", gameObject.name, $"Buff Applied: {id} ({stat})", multiplier);

            if (_showDebugLogs) Debug.Log($"[Buff] Applied: {id} to {gameObject.name} (Stat: {stat}, Mult: {multiplier}, Duration: {duration}s)");
            
            if (_healParticle != null) _healParticle.Play(); 
        }

        public virtual bool IsRanged()
        {
            return false;
        }

        protected virtual void HandleAttack(UnitBase target)
        {
            if (target == null) return;
            
            bool playedAnimation = false;
            // Handle Animation fallback
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                if (_animator.HasState(0, Animator.StringToHash("Attack")))
                {
                    _animator.Play("Attack", 0, 0f);
                    playedAnimation = true;
                }
            }
            
            if (!playedAnimation && _spriteRenderer != null)
            {
                if (IsRanged())
                {
                    // Ranged units do not do the physical punch/lunge bump fallback.
                    // Instead, they use a soft, satisfying scale-recoil animation.
                    // Kill previous to prevent stacking on AoE units
                    _spriteRenderer.transform.DOKill(true);
                    
                    // Reset scale before starting new punch to prevent distortion
                    Vector3 currentScale = _spriteRenderer.transform.localScale;
                    float currentSignX = Mathf.Sign(currentScale.x);
                    Vector3 targetScale = _originalSpriteScale;
                    targetScale.x = Mathf.Abs(_originalSpriteScale.x) * currentSignX;
                    _spriteRenderer.transform.localScale = targetScale;

                    _spriteRenderer.transform.DOPunchScale(new Vector3(-0.1f, 0.1f, 0f), 0.15f, 1, 0.5f);
                }
                else
                {
                    // Melee bump towards target fallback
                    _spriteRenderer.transform.DOKill(true);
                    Vector3 originalPos = GetSpriteLocalPosition();
                    Vector3 worldDir = (target.transform.position - transform.position).normalized * 0.3f;
                    _spriteRenderer.transform.DOLocalMove(originalPos + new Vector3(worldDir.x, worldDir.y, 0), 0.1f).SetLoops(2, LoopType.Yoyo);
                }
            }
        }

        public virtual void Die(UnitBase attacker = null)
        {
            if (_isDead) return;
            _isDead = true;

            string attackerLabel = "Unknown";
            if (attacker != null)
            {
                attackerLabel = attacker.gameObject.name;
                // If the attacker is a player unit, try to append the ultimate skill name for clarity
                if (attacker is PlayerUnit pu && pu.Data != null && pu.Data.UltimateSkill != null)
                {
                    attackerLabel = $"{pu.Data.UnitName} ({pu.Data.UltimateSkill.SkillName})";
                }
            }
            BattleLogManager.Instance.LogEvent(BattleLogType.Death, attackerLabel, gameObject.name, "Unit Died", 0);

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
            bool playedDeathAnim = false;
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                // Try playing common death state names
                if (_animator.HasState(0, Animator.StringToHash("Die")))
                {
                    _animator.Play("Die", 0, 0f);
                    playedDeathAnim = true;
                }
                else if (_animator.HasState(0, Animator.StringToHash("Death")))
                {
                    _animator.Play("Death", 0, 0f);
                    playedDeathAnim = true;
                }
                
                if (playedDeathAnim)
                {
                    StartCoroutine(DelayedDestroy(_animator));
                    return;
                }
            }
            
            if (!playedDeathAnim && _spriteRenderer != null)
            {
                // Improved DOTween Fallback: Shake, then fade and shrink
                Sequence seq = DOTween.Sequence();
                seq.Append(_spriteRenderer.transform.DOShakePosition(0.3f, 0.1f));
                seq.Join(_spriteRenderer.DOColor(Color.red, 0.3f));
                seq.Append(_spriteRenderer.DOFade(0f, 0.5f));
                seq.Join(transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));
                seq.OnComplete(() => Destroy(gameObject));
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

        public bool IsTargetInPattern(Vector2Int origin, Vector2Int target, AttackPattern pattern, float range)
        {
            int dx = Mathf.Abs(origin.x - target.x);
            int dy = Mathf.Abs(origin.y - target.y);
            int iRange = Mathf.FloorToInt(range + 0.01f);

            // Always allow attacking if on the same tile
            if (dx == 0 && dy == 0) return true;

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
                    var offsets = CustomPatternOffsets;
                    if (offsets == null) return false;
                    Vector2Int offset = target - origin;
                    return offsets.Contains(offset);
                default:
                    return false;
            }
        }
        public virtual void RecordDamageDealt(float amount)
        {
            TotalDamageDealt += amount;
            if (this is PlayerUnit player && player.Data != null)
            {
                var gm = UnityEngine.Object.FindFirstObjectByType<MaouSamaTD.Managers.GameManager>();
                if (gm != null) gm.RegisterDamageDealt(player.Data, amount);
            }
        }

        protected virtual void RegisterAttacker(UnitBase attacker)
        {
            // Subclasses can implement aggro logic here
        }

        protected virtual Vector3 GetSpriteLocalPosition()
        {
            return new Vector3(0, 1f, 0);
        }
    }
}
