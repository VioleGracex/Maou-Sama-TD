using UnityEngine;
using MaouSamaTD.Units;
using System.Collections.Generic;

namespace MaouSamaTD.Skills
{
    public class VoidCaressEffect : UltimateEffect
    {
        [Header("Settings")]
        [SerializeField] private float _damage = 50f;
        [SerializeField] private float _radius = 3f;
        [SerializeField] private float _charmDuration = 3f;
        [SerializeField] private float _charmChance = 0.5f; // 50% chance to charm

        public override void Execute(PlayerUnit caster, Vector3 direction)
        {
            // Perform overlap sphere to find enemies
            Collider[] hits = Physics.OverlapSphere(caster.transform.position, _radius);
            
            HashSet<EnemyUnit> processedEnemies = new HashSet<EnemyUnit>();
            
            foreach (var hit in hits)
            {
                EnemyUnit enemy = hit.GetComponent<EnemyUnit>();
                if (enemy != null && !processedEnemies.Contains(enemy))
                {
                    processedEnemies.Add(enemy);
                    
                    // Deal damage
                    enemy.TakeDamage(_damage);
                    
                    // On-hit chance to charm
                    if (Random.value <= _charmChance)
                    {
                        enemy.ApplyCharm(_charmDuration);
                    }
                }
            }
        }
    }
}
