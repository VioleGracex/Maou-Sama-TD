using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace MaouSamaTD.UI.Tutorial
{
    public class TutorialHandUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _handTransform;
        [SerializeField] private GameObject _panel;

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

                public void ShowAt(Vector2 screenPosition)
        {
            Debug.Log($"[tutorial] Hand ShowAt: {screenPosition}");
            gameObject.SetActive(true);
            _panel.SetActive(true);
            _handTransform.position = screenPosition;
            
            // Loop a small "tap" or "drag" animation
            _handTransform.DOKill();
            _handTransform.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        public void MoveHand(Vector2 start, Vector2 end)
        {
            Debug.Log($"[tutorial] Hand MoveHand: from {start} to {end}");
            gameObject.SetActive(true);
            _panel.SetActive(true);
            _handTransform.DOKill();
            _handTransform.position = start;
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
