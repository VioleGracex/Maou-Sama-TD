using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace MaouSamaTD.UI.Tutorial
{
    public class TutorialHandUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _handTransform;
        [SerializeField] private GameObject _panel;

        [SerializeField] private float _pulseAmount = 0.1f;
        [SerializeField] private float _pulseDuration = 0.5f;
        [SerializeField] private float _returnDuration = 0.5f;

        private void Awake()
        {
            // Ensure hand is always on top-most overlay
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;

            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
            
            if (_panel != null) _panel.SetActive(false);
        }

        private Vector3 _lastShowPos;
        private float _lastShowScale;
        private Sequence _pulseSeq;

        public void ShowAt(Vector2 screenPosition, float baseScale = 1f)
        {
            gameObject.SetActive(true);
            _panel.SetActive(true);

            // Stability check: if position and scale are almost identical, don't restart logic
            if (Vector3.Distance(_handTransform.position, (Vector3)screenPosition) < 0.1f && 
                Mathf.Abs(_lastShowScale - baseScale) < 0.01f && 
                _pulseSeq != null && _pulseSeq.IsActive())
            {
                return;
            }

            _lastShowPos = screenPosition;
            _lastShowScale = baseScale;
            _handTransform.position = screenPosition;
            
            // Pulse logic: relative to baseScale
            _handTransform.DOKill();
            _handTransform.localScale = Vector3.one * baseScale;
            
            _pulseSeq = DOTween.Sequence();
            _pulseSeq.Append(_handTransform.DOScale(baseScale + _pulseAmount, _pulseDuration).SetEase(Ease.InOutSine))
                    .Append(_handTransform.DOScale(baseScale, _returnDuration).SetEase(Ease.InOutSine))
                    .SetLoops(-1, LoopType.Restart)
                    .SetUpdate(true);
        }

        public void MoveHand(Vector2 start, Vector2 end, float targetScale = 1f)
        {
            gameObject.SetActive(true);
            _panel.SetActive(true);

            // Don't interrupt if we are already moving to the same destination
            if (_pulseSeq != null && _pulseSeq.IsActive() && Vector3.Distance(_handTransform.position, (Vector3)end) < 1f)
            {
                return;
            }

            _handTransform.DOKill();
            _handTransform.position = start;
            _handTransform.localScale = Vector3.one * targetScale;
            
            float distance = Vector2.Distance(start, end);
            float duration = Mathf.Clamp(distance / 500f, 0.5f, 2.0f); // Constant speed (approx 500px/s)
            
            _pulseSeq = DOTween.Sequence();
            _pulseSeq.Append(_handTransform.DOMove(end, duration).SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true);
        }

        public void MoveTo(Vector2 end, float targetScale = 1f, float duration = 0.3f)
        {
            gameObject.SetActive(true);
            _panel.SetActive(true);

            if (_pulseSeq != null && _pulseSeq.IsActive() && Vector3.Distance(_handTransform.position, (Vector3)end) < 1f)
            {
                return;
            }

            _handTransform.DOKill();
            _pulseSeq = DOTween.Sequence();
            _pulseSeq.Join(_handTransform.DOMove(end, duration).SetEase(Ease.OutSine))
                     .Join(_handTransform.DOScale(targetScale, duration).SetEase(Ease.OutSine))
                     .SetUpdate(true);
        }


        public void Hide()
        {
            _panel.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
