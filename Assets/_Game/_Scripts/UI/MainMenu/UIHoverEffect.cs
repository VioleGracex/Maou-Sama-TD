using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace MaouSamaTD.UI.MainMenu
{
    [RequireComponent(typeof(Image))]
    public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Background Transition")]
        [SerializeField] private Color _normalBgColor = new Color(0.08f, 0.08f, 0.1f, 0.8f);
        [SerializeField] private Color _hoverBgColor = new Color(0.09f, 0.09f, 0.13f, 0.95f);

        [Header("Outline Transition")]
        [SerializeField] private Color _normalOutlineColor = new Color(1f, 0.75f, 0.15f, 0.12f);
        [SerializeField] private Color _hoverOutlineColor = new Color(1f, 0.75f, 0.15f, 0.3f);

        [Header("Scale Transition")]
        [SerializeField] private float _hoverScale = 1.02f;
        [SerializeField] private float _duration = 0.15f;

        private Image _bgImage;
        private Outline _outline;
        private Coroutine _transitionCoroutine;
        private Vector3 _originalScale = Vector3.one;

        private void Awake()
        {
            _bgImage = GetComponent<Image>();
            _outline = GetComponent<Outline>();
            _originalScale = transform.localScale;

            // Apply base/normal states immediately
            if (_bgImage != null) _bgImage.color = _normalBgColor;
            if (_outline != null) _outline.effectColor = _normalOutlineColor;
        }

        private void OnEnable()
        {
            // Reset to default states on enable
            transform.localScale = _originalScale;
            if (_bgImage != null) _bgImage.color = _normalBgColor;
            if (_outline != null) _outline.effectColor = _normalOutlineColor;
        }

        public void Configure(Color normalBg, Color hoverBg, Color normalOutline, Color hoverOutline, float hoverScale, float duration = 0.15f)
        {
            _normalBgColor = normalBg;
            _hoverBgColor = hoverBg;
            _normalOutlineColor = normalOutline;
            _hoverOutlineColor = hoverOutline;
            _hoverScale = hoverScale;
            _duration = duration;

            _bgImage = GetComponent<Image>();
            _outline = GetComponent<Outline>();
            
            if (_bgImage != null) _bgImage.color = _normalBgColor;
            if (_outline != null) _outline.effectColor = _normalOutlineColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            StopTransition();
            _transitionCoroutine = StartCoroutine(Transition(true));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopTransition();
            _transitionCoroutine = StartCoroutine(Transition(false));
        }

        private void StopTransition()
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }
        }

        private IEnumerator Transition(bool isHover)
        {
            float elapsed = 0f;
            Color startBg = _bgImage != null ? _bgImage.color : _normalBgColor;
            Color targetBg = isHover ? _hoverBgColor : _normalBgColor;

            Color startOutline = _outline != null ? _outline.effectColor : _normalOutlineColor;
            Color targetOutline = isHover ? _hoverOutlineColor : _normalOutlineColor;

            Vector3 startScale = transform.localScale;
            Vector3 targetScale = isHover ? _originalScale * _hoverScale : _originalScale;

            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _duration);
                
                // Smooth ease out curve for micro-animation
                float easedT = 1f - (1f - t) * (1f - t);

                if (_bgImage != null) _bgImage.color = Color.Lerp(startBg, targetBg, easedT);
                if (_outline != null) _outline.effectColor = Color.Lerp(startOutline, targetOutline, easedT);
                transform.localScale = Vector3.Lerp(startScale, targetScale, easedT);

                yield return null;
            }

            // Enforce final values
            if (_bgImage != null) _bgImage.color = targetBg;
            if (_outline != null) _outline.effectColor = targetOutline;
            transform.localScale = targetScale;
        }
    }
}
