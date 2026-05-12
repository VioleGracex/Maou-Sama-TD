using UnityEngine;
using DG.Tweening;

namespace MaouSamaTD.VFX
{
    public class GroundEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private float _lifetime = 2f;
        [SerializeField] private bool _fadeInOut = true;

        public void Initialize(Color color, float lifetime)
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer != null) _spriteRenderer.color = color;
            _lifetime = lifetime;

            if (_fadeInOut && _spriteRenderer != null)
            {
                Color c = _spriteRenderer.color;
                c.a = 0;
                _spriteRenderer.color = c;
                
                // Using simple lerp in Update if DOTween is not preferred here, 
                // but DOTween is already used in the project.
                DG.Tweening.Sequence s = DG.Tweening.DOTween.Sequence();
                s.Append(_spriteRenderer.DOFade(1f, 0.2f));
                s.AppendInterval(_lifetime - 0.4f);
                s.Append(_spriteRenderer.DOFade(0f, 0.2f));
                s.OnComplete(() => Destroy(gameObject));
            }
            else
            {
                Destroy(gameObject, _lifetime);
            }
        }
    }
}
