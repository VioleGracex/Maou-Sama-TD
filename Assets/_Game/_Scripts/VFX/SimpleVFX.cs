using UnityEngine;

namespace MaouSamaTD.VFX
{
    public class SimpleVFX : MonoBehaviour
    {
        [SerializeField] private float _lifetime = 1f;
        [SerializeField] private bool _useParticleDuration = false;

        private void Start()
        {
            if (_useParticleDuration)
            {
                var ps = GetComponent<ParticleSystem>();
                if (ps != null) _lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            }
            Destroy(gameObject, _lifetime);
        }
    }
}
