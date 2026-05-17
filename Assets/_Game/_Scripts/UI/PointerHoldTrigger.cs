using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace MaouSamaTD.UI
{
    public class PointerHoldTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action OnHoldTick;
        public Action OnClick;

        private bool _isDown = false;
        private float _downTime = 0f;
        private float _lastTickTime = 0f;
        private const float InitialDelay = 0.4f;
        private const float TickInterval = 0.08f;
        private bool _hasTicked = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDown = true;
            _downTime = Time.time;
            _lastTickTime = Time.time;
            _hasTicked = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isDown && !_hasTicked)
            {
                OnClick?.Invoke();
            }
            ResetHold();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ResetHold();
        }

        private void ResetHold()
        {
            _isDown = false;
        }

        private void Update()
        {
            if (!_isDown) return;

            if (Time.time - _downTime >= InitialDelay)
            {
                if (Time.time - _lastTickTime >= TickInterval)
                {
                    _hasTicked = true;
                    OnHoldTick?.Invoke();
                    _lastTickTime = Time.time;
                }
            }
        }
    }
}
