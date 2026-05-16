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

            // Dynamically parent to MainCanvas if found
            GameObject mainCanvasGO = GameObject.FindWithTag("MainCanvas");
            if (mainCanvasGO != null && transform.parent != mainCanvasGO.transform)
            {
                transform.SetParent(mainCanvasGO.transform, false);
                transform.SetAsLastSibling();
            }
        }

        private void OnDisable()
        {
            KillActiveSequence();
        }

        private Vector3 _lastShowPos;
        private float _lastShowScale;
        private Sequence _pulseSeq;
        private Vector2 _currentTargetStart;
        private Vector2 _currentTargetEnd;

        private void KillActiveSequence()
        {
            if (_handTransform != null)
            {
                _handTransform.DOKill();
            }
            if (_pulseSeq != null)
            {
                _pulseSeq.Kill();
                _pulseSeq = null;
            }
        }

        public void ShowAt(Vector2 screenPosition, float baseScale = 1f)
        {
            gameObject.SetActive(true);
            if (_panel != null) _panel.SetActive(true);

            // Stability check: if position is extremely close and scale is same, do nothing
            float dist = Vector2.Distance(_handTransform.position, screenPosition);
            if (dist < 0.1f && Mathf.Abs(_lastShowScale - baseScale) < 0.01f && _pulseSeq != null && _pulseSeq.IsActive())
            {
                return;
            }

            // If we are already active and pulsing, and the target position changed slightly (e.g., unit moving, camera jitter)
            if (dist < 15f && _pulseSeq != null && _pulseSeq.IsActive())
            {
                // Smoothly slide/cling to the target position without killing and restarting the pulsing scale sequence
                _handTransform.position = screenPosition;
                _lastShowScale = baseScale;
                return;
            }

            // If position changed significantly, hide, teleport, and show instead of gliding across the screen
            if (dist >= 15f && _pulseSeq != null && _pulseSeq.IsActive() && gameObject.activeInHierarchy)
            {
                KillActiveSequence();
                _pulseSeq = DOTween.Sequence();
                
                // 1. Shrink down quickly
                _pulseSeq.Append(_handTransform.DOScale(0f, 0.15f).SetEase(Ease.InQuad))
                         .AppendCallback(() =>
                         {
                             // 2. Snap to new position while invisible
                             _handTransform.position = screenPosition;
                             _lastShowScale = baseScale;
                         })
                         .AppendInterval(0.05f)
                         // 3. Grow back at the new position
                         .Append(_handTransform.DOScale(baseScale, 0.2f).SetEase(Ease.OutBack))
                         .OnComplete(() =>
                         {
                             // 4. Resume the continuous pulse animation loop
                             KillActiveSequence();
                             _pulseSeq = DOTween.Sequence();
                             _pulseSeq.Append(_handTransform.DOScale(baseScale + _pulseAmount, _pulseDuration).SetEase(Ease.InOutSine))
                                      .Append(_handTransform.DOScale(baseScale, _returnDuration).SetEase(Ease.InOutSine))
                                      .SetLoops(-1, LoopType.Restart)
                                      .SetUpdate(true);
                         })
                         .SetUpdate(true);
                return;
            }

            // First time showing or fallback: instant placement & pulse setup
            _lastShowScale = baseScale;
            _handTransform.position = screenPosition;
            _currentTargetEnd = screenPosition;

            KillActiveSequence();
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
            if (_panel != null) _panel.SetActive(true);

            // If we are already animating a move from the same start to the same end, let it play!
            if (_pulseSeq != null && _pulseSeq.IsActive() && 
                Vector2.Distance(_currentTargetStart, start) < 5f && 
                Vector2.Distance(_currentTargetEnd, end) < 5f)
            {
                return;
            }

            _currentTargetStart = start;
            _currentTargetEnd = end;
            _lastShowScale = targetScale;

            KillActiveSequence();
            
            // Set initial state: slightly enlarged, hovering over button
            _handTransform.position = start;
            _handTransform.localScale = Vector3.one * (targetScale * 1.1f);
            
            _pulseSeq = DOTween.Sequence();
            
            // 1. Press down (scale down to mimic grabbing)
            _pulseSeq.Append(_handTransform.DOScale(targetScale * 0.85f, 0.35f).SetEase(Ease.OutBack))
                     .AppendInterval(0.1f); // Brief tactile pause before dragging
            
            // 2. Drag to destination (using InOutCubic for realistic weight, inertia, and smooth deceleration)
            float distance = Vector2.Distance(start, end);
            float dragDuration = Mathf.Clamp(distance / 200f, 1.2f, 2.0f); // Deliberate speed (200px/s) for ultimate readability
            
            _pulseSeq.Append(_handTransform.DOMove(end, dragDuration).SetEase(Ease.InOutCubic));
            
            // 3. Release (scale up slightly to mimic dropping/letting go)
            _pulseSeq.Append(_handTransform.DOScale(targetScale * 1.1f, 0.3f).SetEase(Ease.OutSine))
                     .AppendInterval(0.15f);
                     
            // 4. Fade out smoothly (scale to 0 so there is NO jarring teleport back to start!)
            _pulseSeq.Append(_handTransform.DOScale(0f, 0.25f).SetEase(Ease.InSine));
            
            // 5. Instantly teleport back to start while invisible
            _pulseSeq.AppendCallback(() => {
                _handTransform.position = start;
            });
            
            // 6. Fade back in/scale up over start position to restart loop
            _pulseSeq.Append(_handTransform.DOScale(targetScale * 1.1f, 0.25f).SetEase(Ease.OutSine))
                     .AppendInterval(0.2f); // Brief idle delay before repeating

            _pulseSeq.SetLoops(-1, LoopType.Restart)
                     .SetUpdate(true);
        }

        public void MoveTo(Vector2 end, float targetScale = 1f, float duration = 0.3f)
        {
            gameObject.SetActive(true);
            if (_panel != null) _panel.SetActive(true);

            // If we are already moving towards the same destination, don't interrupt
            if (_pulseSeq != null && _pulseSeq.IsActive() && Vector2.Distance(_currentTargetEnd, end) < 5f)
            {
                return;
            }

            _currentTargetStart = _handTransform.position;
            _currentTargetEnd = end;
            _lastShowScale = targetScale;

            KillActiveSequence();
            _pulseSeq = DOTween.Sequence();
            _pulseSeq.Join(_handTransform.DOMove(end, duration).SetEase(Ease.OutSine))
                     .Join(_handTransform.DOScale(targetScale, duration).SetEase(Ease.OutSine))
                     .SetUpdate(true);
        }

        public void Hide()
        {
            KillActiveSequence();
            _currentTargetStart = Vector2.zero;
            _currentTargetEnd = Vector2.zero;
            _lastShowScale = 0f;
            if (_panel != null) _panel.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
