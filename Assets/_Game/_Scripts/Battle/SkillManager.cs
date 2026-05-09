using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MaouSamaTD.Managers;using MaouSamaTD.Managers;
using MaouSamaTD.Units;
using Zenject;

namespace MaouSamaTD.Skills
{
    public class SkillManager : MonoBehaviour
    {
        #region Fields
        // Runtime State
        private List<SovereignRiteData> _availableSkills = new List<SovereignRiteData>();
        public IReadOnlyList<SovereignRiteData> AvailableSkills => _availableSkills;
        private Dictionary<SovereignRiteData, float> _cooldowns = new Dictionary<SovereignRiteData, float>();
        
        [Inject] private BattleCurrencyManager _currencyManager;
        [Inject] private TutorialManager _tutorialManager;
        [Inject] private Grid.GridManager _gridManager;
        [Inject] private DialogueManager _dialogueManager;

        [Header("Debug")]
        [SerializeField] private bool _showDebugLogs = true;
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
            if (_showDebugLogs) Debug.Log($"[SkillManager] Initialized with {_availableSkills.Count} rites.");
        }

        public void ResetAllCooldowns()
        {
            _cooldowns.Clear();
            if (_showDebugLogs) Debug.Log("[SkillManager] All cooldowns reset.");
        }

        public void ForceSetReady(SovereignRiteData skill)
        {
            if (skill == null) return;
            _cooldowns.Remove(skill);
            if (_showDebugLogs) Debug.Log($"[SkillManager] Forced skill {skill.SkillName} to be ready.");
        }

        public bool IsSkillReady(SovereignRiteData skill)
        {
            if (skill == null) return false;
            
            // Check Cooldown
            if (_cooldowns.ContainsKey(skill))
            {
                if (_cooldowns[skill] > 0f) return false;
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
            
            float remaining = GetRemainingCooldown(skill);
            if (remaining <= 0) return 0f;
            
            return remaining / skill.Cooldown;
        }

        public float GetRemainingCooldown(SovereignRiteData skill)
        {
            if (skill == null || !_cooldowns.ContainsKey(skill)) return 0f;
            return Mathf.Max(0f, _cooldowns[skill]);
        }

        // Renamed/Reloaded for SovereignRites specific
        public bool TryExecuteRite(SovereignRiteData skill, Vector3 targetPosition, UnitBase targetUnit)
        {
            if (!IsSkillReady(skill)) return false;

            // Validate Target
            if (!IsTargetValid(skill, targetUnit))
            {
                if (_showDebugLogs) Debug.Log($"[SkillManager] Target Invalid for skill {skill.SkillName}");
                return false;
            }

            // Consume Cost
            if (_currencyManager != null)
            {
                if (!_currencyManager.TrySpendSeals(skill.SealCost)) return false;
            }

            // Apply Cooldown
            _cooldowns[skill] = skill.Cooldown;

            // Execute Logic
            MaouSamaTD.Battle.BattleLogManager.Instance.LogEvent(MaouSamaTD.Battle.BattleLogType.System, "Sovereign", "", $"Activating Rite: {skill.SkillName}", 0);
            ApplySkillEffect(skill, targetPosition, targetUnit);
            _tutorialManager?.OnActionTriggered("SkillUsed");

            if (_tutorialManager != null && _tutorialManager.IsInTutorial)
            {
                _tutorialManager.OnRiteUsed(skill, targetPosition, targetUnit);
            }

            return true;
        }
        #endregion

        #region Internal Logic
        private void Update()
        {
            // Update cooldowns based on scaled game time (Time.deltaTime) so that x1, x2 speeds scale them perfectly,
            // and pausing (timescale = 0) naturally freezes them!
            var keys = new List<SovereignRiteData>(_cooldowns.Keys);
            foreach (var key in keys)
            {
                if (_cooldowns[key] > 0f)
                {
                    _cooldowns[key] = Mathf.Max(0f, _cooldowns[key] - Time.deltaTime);
                }
            }
        }

        private bool IsTargetValid(SovereignRiteData skill, UnitBase targetUnit)
        {
            // Tile/AOE targeting is always valid if within grid (checked by InteractionManager)
            if (skill.TargetType == SkillTargetType.Tile || skill.Radius > 0) 
                return true;

            // TargetType.None = positional cast — the player clicked on a tile, not a specific unit.
            // Treat as valid regardless of whether a unit was under the cursor.
            if (skill.TargetType != SkillTargetType.Unit)
                return true;

            // Single Target Logic (TargetType.Unit)
            if (targetUnit == null) return false;

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
            
            return true;
        }

        private void ApplySkillEffect(SovereignRiteData skill, Vector3 pos, UnitBase unit)
        {
            // Persistence Logic: If persistent, we apply to tiles
            if (skill.Persistence == SkillPersistenceType.Persistent)
            {
                ApplyPersistentEffect(skill, pos);
                if (_showDebugLogs) Debug.Log($"[SkillManager] Executed Persistent Rite: {skill.SkillName} at {pos}");
                return;
            }

            // Spawn VFX (Instant)
            if (skill.BaseVisuals.HitVFX != null)
            {
                Instantiate(skill.BaseVisuals.HitVFX, pos, Quaternion.identity);
            }

            // AOE path: Tile-targeted, non-zero radius, or no specific unit targeted
            // (TargetType=None with null unit = player clicked on a tile = positional AOE)
            bool isPositionalCast = skill.TargetType == SkillTargetType.Tile ||
                                    skill.Radius > 0 ||
                                    unit == null;

            if (isPositionalCast)
            {
                ApplyAreaEffect(skill, pos);
            }
            else
            {
                // Explicit single-target: a specific unit was clicked
                ApplyEffectToUnit(skill, unit);
            }
            
            if (_showDebugLogs) Debug.Log($"[SkillManager] Executed Skill/Rite: {skill.SkillName}");
        }

        private void ApplyPersistentEffect(SovereignRiteData skill, Vector3 center)
        {
            // Single Tile Persistent
            if (skill.TargetType == SkillTargetType.Tile)
            {
                var coord = _gridManager.WorldToGridCoordinates(center);
                var tile = _gridManager.GetTileAt(coord);
                if (tile != null)
                {
                    tile.AddEffect(skill);
                }
            }
            else // Area Persistent
            {
                // Find all tiles in radius
                int iRadius = Mathf.CeilToInt(skill.Radius);
                var centerCoord = _gridManager.WorldToGridCoordinates(center);
                
                for (int x = -iRadius; x <= iRadius; x++)
                {
                    for (int y = -iRadius; y <= iRadius; y++)
                    {
                        var coord = centerCoord + new Vector2Int(x, y);
                        // Simple circular distance check in grid space (or world space)
                        var tile = _gridManager.GetTileAt(coord);
                        if (tile != null)
                        {
                            float dist = Vector3.Distance(tile.transform.position, center);
                            if (dist <= skill.Radius + 0.1f) // Small buffer
                            {
                                tile.AddEffect(skill);
                            }
                        }
                    }
                }
            }
        }

        private void ApplyAreaEffect(SovereignRiteData skill, Vector3 center)
        {
            // Removed the Mathf.Max(..., 2.0f) which was overriding your settings.
            float checkRadius = skill.Radius + 0.1f; 

            if (_showDebugLogs) Debug.Log($"[SkillManager] AOE check at {center}, radius={checkRadius}, shape={skill.AoeShape}, effect={skill.EffectType}");

            int hitCount = 0;

            if (skill.EffectType == SkillEffectType.Damage || skill.EffectType == SkillEffectType.Debuff)
            {
                // Copy list to avoid "Collection was modified" if units die/remove themselves during loop
                var targets = new List<EnemyUnit>(EnemyUnit.ActiveEnemies);
                foreach (var enemy in targets)
                {
                    if (enemy == null || enemy.IsDead) continue;
                    if (IsInShape(skill, center, enemy.transform.position, checkRadius))
                    {
                        if (_showDebugLogs) Debug.Log($"[SkillManager] AOE hit enemy: {enemy.gameObject.name} at {enemy.transform.position}");
                        ApplyEffectToUnit(skill, enemy);
                        hitCount++;
                    }
                }
            }

            if (skill.EffectType == SkillEffectType.Buff)
            {
                // Copy list to avoid "Collection was modified"
                var targets = new List<PlayerUnit>(PlayerUnit.ActiveUnits);
                foreach (var player in targets)
                {
                    if (player == null || player.IsDead) continue;
                    if (IsInShape(skill, center, player.transform.position, checkRadius))
                    {
                        if (_showDebugLogs) Debug.Log($"[SkillManager] AOE hit player: {player.gameObject.name} at {player.transform.position}");
                        ApplyEffectToUnit(skill, player);
                        hitCount++;
                    }
                }
            }

            if (_showDebugLogs) Debug.Log($"[SkillManager] AOE ({skill.AoeShape}) execution finished. Total hits: {hitCount}");
        }

        /// <summary>Returns true if worldPos is inside the given AoeShape centred on origin.</summary>
        private bool IsInShape(SovereignRiteData skill, Vector3 origin, Vector3 worldPos, float radius)
        {
            float worldDx = worldPos.x - origin.x;
            float worldDz = worldPos.z - origin.z;
            float absDx = Mathf.Abs(worldDx);
            float absDz = Mathf.Abs(worldDz);
            float threshold = skill.Radius + 0.6f;

            switch (skill.AoeShape)
            {
                case AoeShape.Circle:
                    return (worldDx * worldDx + worldDz * worldDz) <= threshold * threshold;

                case AoeShape.Square:
                    return absDx <= threshold && absDz <= threshold;

                case AoeShape.Cross:
                    return (absDx < 0.6f && absDz <= threshold) || (absDz < 0.6f && absDx <= threshold);

                case AoeShape.DiagonalX:
                    return Mathf.Abs(absDx - absDz) < 0.6f && absDx <= threshold;

                case AoeShape.Star:
                    bool isCross = (absDx < 0.6f && absDz <= threshold) || (absDz < 0.6f && absDx <= threshold);
                    bool isDiag = Mathf.Abs(absDx - absDz) < 0.6f && absDx <= threshold;
                    return isCross || isDiag;

                case AoeShape.Custom:
                    // High-precision check for custom offsets:
                    // Does the target world position fall within any of the offset tiles?
                    foreach (var offset in skill.CustomShapeOffsets)
                    {
                        // We assume tiles are 1x1 units in world space.
                        // We check if the distance from the worldPos to the center of the offset tile is small.
                        float targetX = origin.x + offset.x;
                        float targetZ = origin.z + offset.y;
                        if (Mathf.Abs(worldPos.x - targetX) < 0.6f && Mathf.Abs(worldPos.z - targetZ) < 0.6f)
                            return true;
                    }
                    // Also always check the center (0,0) for custom shapes to avoid dead zones 
                    // unless specifically excluded (but usually it's a bug).
                    if (absDx < 0.6f && absDz < 0.6f) return true;
                    return false;

                default:
                    return false;
            }
        }


        private void ApplyEffectToUnit(SovereignRiteData skill, UnitBase unit)
        {
            if (unit == null) return;

            bool isEnemy = unit is EnemyUnit;
            bool isPlayer = unit is PlayerUnit;

            if (skill.EffectType == SkillEffectType.Damage)
            {
                if (isEnemy)
                {
                    float finalDamage = skill.Value;

                    // Secret Tutorial Boost for Level 2 Boss
                    if (_tutorialManager != null && _tutorialManager.IsInTutorial)
                    {
                        if (unit is EnemyUnit enemyUnit && enemyUnit.EnemyData != null && enemyUnit.EnemyData.EnemyName == "Abyssal Shade")
                        {
                            finalDamage = unit.CurrentHp + 999; // Ensure one-shot
                            if (_showDebugLogs) Debug.Log("[SkillManager] Secret Tutorial Boost applied to Abyssal Shade!");
                        }
                    }
                    unit.TakeDamage(finalDamage, null, DamageType.Magic, true);
                }
            }
            else if (skill.EffectType == SkillEffectType.Buff)
            {
                if (isPlayer)
                {
                    if (skill.Duration > 0)
                    {
                        foreach (var mod in skill.Modifiers)
                        {
                            float multiplier = 1f + (mod.Value / 100f);
                            unit.ApplyBuff(skill.name + "_" + mod.Stat, mod.Stat, multiplier, skill.Duration);
                        }
                        
                        // Fallback for simple value if list is empty
                        if (skill.Modifiers.Count == 0 && skill.Value > 0)
                        {
                             // We don't have a stat here, so we might need a default or just skip
                        }
                    }
                }
            }
            else if (skill.EffectType == SkillEffectType.Debuff)
            {
                if (isEnemy)
                {
                    if (skill.Duration > 0)
                    {
                        foreach (var mod in skill.Modifiers)
                        {
                            float multiplier = 1f - (mod.Value / 100f);
                            unit.ApplyBuff(skill.name + "_" + mod.Stat, mod.Stat, multiplier, skill.Duration);
                        }
                    }
                }
            }
            
            if (_showDebugLogs) Debug.Log($"[SkillManager] Applied {skill.EffectType} to {unit.gameObject.name}");
        }
        #endregion
    }
}
