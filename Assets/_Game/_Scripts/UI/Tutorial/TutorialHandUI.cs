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

        public void ShowAt(Vector2 screenPosition, float baseScale = 1f)
        {
            Debug.Log($"[tutorial] Hand ShowAt: {screenPosition}, scale: {baseScale}");
            gameObject.SetActive(true);
            _panel.SetActive(true);
            _handTransform.position = screenPosition;
            
            // Pulse logic: relative to baseScale
            _handTransform.DOKill();
            _handTransform.localScale = Vector3.one * baseScale;
            
            Sequence pulseSeq = DOTween.Sequence();
            pulseSeq.Append(_handTransform.DOScale(baseScale + _pulseAmount, _pulseDuration).SetEase(Ease.InOutSine))
                    .Append(_handTransform.DOScale(baseScale, _returnDuration).SetEase(Ease.InOutSine))
                    .SetLoops(-1, LoopType.Restart)
                    .SetUpdate(true);
        }

        public void MoveHand(Vector2 start, Vector2 end, float targetScale = 1f)
        {
            Debug.Log($"[tutorial] Hand MoveHand: from {start} to {end} scale: {targetScale}");
            gameObject.SetActive(true);
            _panel.SetActive(true);
            _handTransform.DOKill();
            _handTransform.position = start;
            _handTransform.localScale = Vector3.one * targetScale;
            _handTransform.DOMove(end, 1.5f)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }


        public void Hide()
        {
            _panel.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
