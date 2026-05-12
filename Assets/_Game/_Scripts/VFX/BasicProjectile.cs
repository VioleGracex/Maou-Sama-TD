using UnityEngine;
using MaouSamaTD.Units;

namespace MaouSamaTD.VFX
{
    public class BasicProjectile : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _speed = 10f;
        [SerializeField] private float _hitDistance = 0.2f;
        [SerializeField] private bool _rotateTowardsTarget = true;
        
        [Header("VFX")]
        [SerializeField] private GameObject _hitVFXPrefab;

        private UnitBase _target;
        private float _damage;
        private UnitBase _attacker;
        private DamageType _damageType;
        private bool _hasHit = false;

        public void Launch(UnitBase target, float damage, UnitBase attacker, DamageType damageType)
        {
            _target = target;
            _damage = damage;
            _attacker = attacker;
            _damageType = damageType;
            
            // Initial rotation
            if (_rotateTowardsTarget && _target != null)
            {
                UpdateRotation();
            }
        }

        private void Update()
        {
            if (_hasHit) return;

            if (_target == null || _target.IsDead)
            {
                // Target lost, just fizzle out or continue in last direction?
                // For now, destroy.
                Destroy(gameObject);
                return;
            }

            Vector3 targetPos = _target.transform.position + Vector3.up * 0.5f; // Aim for center
            Vector3 dir = (targetPos - transform.position).normalized;
            
            transform.position += dir * _speed * Time.deltaTime;

            if (_rotateTowardsTarget)
            {
                UpdateRotation();
            }

            if (Vector3.Distance(transform.position, targetPos) < _hitDistance)
            {
                Hit();
            }
        }

        private void UpdateRotation()
        {
            if (_target == null) return;
            Vector3 dir = (_target.transform.position + Vector3.up * 0.5f - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        private void Hit()
        {
            if (_hasHit) return;
            _hasHit = true;

            if (_target != null && !_target.IsDead)
            {
                _target.TakeDamage(_damage, _attacker, _damageType);
            }

            if (_hitVFXPrefab != null)
            {
                Instantiate(_hitVFXPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
