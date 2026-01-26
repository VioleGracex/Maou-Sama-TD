using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Managers;
using MaouSamaTD.Units;

namespace MaouSamaTD.Skills
{
    public class SkillManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private List<SkillData> _availableSkills;

        // Runtime State
        private Dictionary<SkillData, float> _cooldowns = new Dictionary<SkillData, float>();
        
        [Zenject.Inject] private CurrencyManager _currencyManager;

        private void Update()
        {
            // Optional: Tick active buffs?
        }

        public bool IsSkillReady(SkillData skill)
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
                if (_currencyManager.CurrentSeals < skill.Cost) return false;
            }

            return true;
        }

        public float GetCooldownProgress(SkillData skill)
        {
            if (skill == null || !_cooldowns.ContainsKey(skill)) return 0f;
            
            float end = _cooldowns[skill];
            float remaining = end - Time.time;
            if (remaining <= 0) return 0f;
            
            return remaining / skill.Cooldown;
        }

        public bool TryExecuteSkill(SkillData skill, Vector3 targetPosition, UnitBase targetUnit)
        {
            if (!IsSkillReady(skill)) return false;

            // Consume Cost (Assuming atomic check/spend in TrySpendSeals, but we double check)
            if (_currencyManager != null)
            {
                if (!_currencyManager.TrySpendSeals(skill.Cost)) return false;
            }

            // Apply Cooldown
            _cooldowns[skill] = Time.time + skill.Cooldown;

            // Execute Logic
            ApplySkillEffect(skill, targetPosition, targetUnit);

            return true;
        }

        private void ApplySkillEffect(SkillData skill, Vector3 pos, UnitBase unit)
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
                    // Ground effect? E.g. Trap? For now just visual if no logic.
                }
            }
            
            Debug.Log($"Executed Skill: {skill.SkillName}");
        }

        private void ApplyAreaEffect(SkillData skill, Vector3 center)
        {
            // Find targets in radius
            // For simplicity, checking all units. Optimization: Spatial Hash or Physics.OverlapSphere
            
            Collider[] hits = Physics.OverlapSphere(center, skill.Radius);
            foreach (var hit in hits)
            {
                UnitBase u = hit.GetComponent<UnitBase>();
                // Only specific layers?
                // Currently all UnitBase have colliders usually.
                if (u != null)
                {
                    ApplyEffectToUnit(skill, u);
                }
            }
        }

        private void ApplyEffectToUnit(SkillData skill, UnitBase unit)
        {
            if (unit == null) return;

            // Friendly Fire check?
            bool isEnemy = unit is EnemyUnit;
            bool isPlayer = unit is PlayerUnit;

            // Logic matrix based on EffectType?
            // "Lightning" -> Damage -> Target Enemy?
            // "Empower" -> Buff -> Target Ally?
            
            // Allow explicit target filtering in Data eventually, but for now:
            
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
                    unit.Heal(skill.Value); // Placeholder for Buff, treating as Heal or we need BuffSystem
                    // Todo: Implement temporary stats buff
                }
            }
        }
    }
}
