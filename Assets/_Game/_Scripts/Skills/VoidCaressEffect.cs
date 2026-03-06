using UnityEngine;
using MaouSamaTD.Units;
using System.Collections.Generic;

namespace MaouSamaTD.Skills
{
    public class VoidCaressEffect : UltimateEffect
    {
        [Header("Settings")]
        [SerializeField] private float _damagePerTick = 15f;
        [SerializeField] private float _radius = 3f;
        [SerializeField] private float _duration = 4f;
        [SerializeField] private float _tickInterval = 0.5f;
        [SerializeField] private float _charmDuration = 3f;
        [SerializeField] private float _charmChance = 0.2f; // Lower chance per tick since it hits multiple times

        private PlayerUnit _owner;

        public override void Execute(PlayerUnit caster, Vector3 direction)
        {
            _owner = caster;
            StartCoroutine(DamageRoutine());
        }

        private System.Collections.IEnumerator DamageRoutine()
        {
            float elapsed = 0;
            while (elapsed < _duration)
            {
                ApplyTick();
                yield return new WaitForSeconds(_tickInterval);
                elapsed += _tickInterval;
            }
            
            // Auto destroy after duration
            Destroy(gameObject, 0.5f);
        }

        private void ApplyTick()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _radius);
            HashSet<EnemyUnit> processedEnemies = new HashSet<EnemyUnit>();
            
            foreach (var hit in hits)
            {
                EnemyUnit enemy = hit.GetComponent<EnemyUnit>();
                if (enemy != null && !processedEnemies.Contains(enemy))
                {
                    processedEnemies.Add(enemy);
                    
                    // Deal damage
                    enemy.TakeDamage(_damagePerTick, _owner, DamageType.Magic, true);
                    
                    // On-hit chance to charm
                    if (Random.value <= _charmChance)
                    {
                        enemy.ApplyCharm(_charmDuration);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0, 1, 0.3f);
            Gizmos.DrawSphere(transform.position, _radius);
        }
    }
}
