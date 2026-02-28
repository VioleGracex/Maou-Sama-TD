using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Managers;
using MaouSamaTD.Units;

namespace MaouSamaTD.Skills
{
    public class SkillManager : MonoBehaviour
    {
        #region Fields
        // Runtime State
        private List<SovereignRiteData> _availableSkills = new List<SovereignRiteData>();
        private Dictionary<SovereignRiteData, float> _cooldowns = new Dictionary<SovereignRiteData, float>();
        
        [Zenject.Inject] private CurrencyManager _currencyManager;
        #endregion

        #region Public API
        public void Init(List<SovereignRiteData> skills)
        {
            _availableSkills.Clear();
            _cooldowns.Clear();

            if (skills != null)
            {
                _availableSkills.AddRange(skills);
            }
            Debug.Log($"[SkillManager] Initialized with {_availableSkills.Count} rites.");
        }

        public void ResetAllCooldowns()
        {
            _cooldowns.Clear();
            Debug.Log("[SkillManager] All cooldowns reset.");
        }

        public void ForceSetReady(SovereignRiteData skill)
        {
            if (skill == null) return;
            _cooldowns.Remove(skill);
            Debug.Log($"[SkillManager] Forced skill {skill.SkillName} to be ready.");
        }

        public bool IsSkillReady(SovereignRiteData skill)
        {
            if (skill == null) return false;
            
            // Check Cooldown
            if (_cooldowns.ContainsKey(skill))
            {
                if (Time.time < _cooldowns[skill]) return false;
            }

            // Check Cost
            if (_currencyManager != null)
            {
                if (_currencyManager.CurrentSeals < skill.SealCost) return false;
            }

            return true;
        }

        public float GetCooldownProgress(SovereignRiteData skill)
        {
            if (skill == null || !_cooldowns.ContainsKey(skill)) return 0f;
            
            float end = _cooldowns[skill];
            float remaining = end - Time.time;
            if (remaining <= 0) return 0f;
            
            return remaining / skill.Cooldown;
        }

        // Renamed/Reloaded for SovereignRites specific
        public bool TryExecuteRite(SovereignRiteData skill, Vector3 targetPosition, UnitBase targetUnit)
        {
            if (!IsSkillReady(skill)) return false;

            // Validate Target
            if (!IsTargetValid(skill, targetUnit))
            {
                Debug.Log($"[SkillManager] Target Invalid for skill {skill.SkillName}");
                return false;
            }

            // Consume Cost
            if (_currencyManager != null)
            {
                if (!_currencyManager.TrySpendSeals(skill.SealCost)) return false;
            }

            // Apply Cooldown
            _cooldowns[skill] = Time.time + skill.Cooldown;

            // Execute Logic
            ApplySkillEffect(skill, targetPosition, targetUnit);

            return true;
        }
        #endregion

        #region Internal Logic
        private void Update()
        {
            // Optional: Tick active buffs?
        }

        private bool IsTargetValid(SkillBase skill, UnitBase targetUnit)
        {
            // AOE is usually ground target or unit center, always valid if clicked in bounds (handled by raycast)
            // But if we want to enforce "AOE must hit something", we'd check overlap, but user said "if aoe just anywhere"
            if (skill.Radius > 0) return true;

            // Single Target Logic
            if (skill.Radius <= 0)
            {
                if (targetUnit == null) return false; // Must have a unit

                if (skill.EffectType == SkillEffectType.Buff)
                {
                    // Buffs only on Player/Friends
                    return targetUnit is PlayerUnit;
                }
                else if (skill.EffectType == SkillEffectType.Damage)
                {
                    // Damage only on Enemies
                    return targetUnit is EnemyUnit;
                }
            }
            
            return true;
        }

        private void ApplySkillEffect(SkillBase skill, Vector3 pos, UnitBase unit)
        {
            // Spawn VFX
            if (skill.HitVFX != null)
            {
                Instantiate(skill.HitVFX, pos, Quaternion.identity);
            }

            if (skill.Radius > 0)
            {
                // AOE
                ApplyAreaEffect(skill, pos);
            }
            else
            {
                // Single Target
                if (unit != null)
                {
                    ApplyEffectToUnit(skill, unit);
                }
                else if (skill.TargetType == SkillTargetType.Ground)
                {
                    // Ground effect
                }
            }
            
            Debug.Log($"[SkillManager] Executed Skill/Rite: {skill.SkillName}");
        }

        private void ApplyAreaEffect(SkillBase skill, Vector3 center)
        {
            Collider[] hits = Physics.OverlapSphere(center, skill.Radius);
            foreach (var hit in hits)
            {
                UnitBase u = hit.GetComponent<UnitBase>();
                if (u != null)
                {
                    ApplyEffectToUnit(skill, u);
                }
            }
        }

        private void ApplyEffectToUnit(SkillBase skill, UnitBase unit)
        {
            if (unit == null) return;

            bool isEnemy = unit is EnemyUnit;
            bool isPlayer = unit is PlayerUnit;

            if (skill.EffectType == SkillEffectType.Damage)
            {
                if (isEnemy)
                {
                    unit.TakeDamage(skill.Value);
                }
            }
            else if (skill.EffectType == SkillEffectType.Buff)
            {
                if (isPlayer)
                {
                    unit.Heal(skill.Value); 
                }
            }
        }
        #endregion
    }
}
