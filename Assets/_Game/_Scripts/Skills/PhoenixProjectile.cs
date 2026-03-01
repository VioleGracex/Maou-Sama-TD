using UnityEngine;
using MaouSamaTD.Units;
using System.Collections.Generic;

namespace MaouSamaTD.Skills
{
    public class PhoenixProjectile : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _speed = 10f;
        [SerializeField] private float _damage = 50f;
        [SerializeField] private float _lifetime = 5f;
        [SerializeField] private float _hitRadius = 1.5f;
        [SerializeField] private LayerMask _enemyLayer;

        private Vector3 _direction;
        private PlayerUnit _owner;
        private HashSet<EnemyUnit> _hitEnemies = new HashSet<EnemyUnit>();
        private float _spawnTime;

        public void Initialize(PlayerUnit owner, Vector3 direction, float damage, float speed)
        {
            _owner = owner;
            _direction = direction.normalized;
            _damage = damage;
            _speed = speed;
            _spawnTime = Time.time;

            // Rotate to face direction
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void Update()
        {
            if (Time.time > _spawnTime + _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += _direction * _speed * Time.deltaTime;

            // Simple overlap check for "line" feel (using a sphere at current pos)
            Collider[] hits = Physics.OverlapSphere(transform.position, _hitRadius, _enemyLayer);
            foreach (var hit in hits)
            {
                EnemyUnit enemy = hit.GetComponent<EnemyUnit>();
                if (enemy != null && !_hitEnemies.Contains(enemy))
                {
                    enemy.TakeDamage(_damage, _owner);
                    _hitEnemies.Add(enemy);
                    Debug.Log($"[tutorial] Phoenix hit {enemy.name}");
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _hitRadius);
        }
    }
}
