using UnityEngine;
using DG.Tweening;

namespace MaouSamaTD.UI.Tutorial
{
    public class TutorialHandUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _handTransform;
        [SerializeField] private GameObject _panel;

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        public void ShowAt(Vector2 screenPosition)
        {
            gameObject.SetActive(true);
            _panel.SetActive(true);
            _handTransform.position = screenPosition;
            
            // Loop a small "tap" or "drag" animation
            _handTransform.DOKill();
            _handTransform.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        public void MoveHand(Vector2 start, Vector2 end)
        {
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
