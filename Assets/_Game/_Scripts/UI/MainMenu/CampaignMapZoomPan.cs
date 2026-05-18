using UnityEngine;
using UnityEngine.EventSystems;

namespace MaouSamaTD.UI.MainMenu
{
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class CampaignMapZoomPan : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        [Header("Zoom Settings")]
        [SerializeField] private float _minZoom = 0.5f;
        [SerializeField] private float _maxZoom = 2.0f;
        [SerializeField] private float _zoomSensitivity = 0.08f;
        [SerializeField] private float _zoomSmoothTime = 0.08f;

        [Header("Pan Settings")]
        [SerializeField] private float _dragSpeed = 1.0f;
        [SerializeField] private bool _useInertia = true;
        [SerializeField] private float _inertiaDecay = 4.0f;

        private RectTransform _rectTransform;
        private RectTransform _parentRectTransform;
        private Canvas _canvas;

        private float _targetZoom = 1.0f;
        private float _currentZoom = 1.0f;
        private float _zoomVelocity = 0.0f;

        private Vector2 _targetPosition;
        private Vector2 _currentPosition;
        private Vector2 _dragVelocity;

        private bool _isDragging = false;

        private void EnsureInitialized()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
                
                // Configure RectTransform for Zoom & Pan
                _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _rectTransform.sizeDelta = new Vector2(2048f, 1143f);
            }
            if (_parentRectTransform == null && _rectTransform != null)
            {
                _parentRectTransform = _rectTransform.parent as RectTransform;
            }
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            // Dynamically calculate the absolute minimum zoom to ensure the map always covers the screen
            if (_parentRectTransform != null && _rectTransform != null)
            {
                float minZoomX = _parentRectTransform.rect.width / _rectTransform.rect.width;
                float minZoomY = _parentRectTransform.rect.height / _rectTransform.rect.height;
                _minZoom = Mathf.Max(minZoomX, minZoomY);
            }
        }

        private void Awake()
        {
            EnsureInitialized();

            _targetPosition = _rectTransform.anchoredPosition;
            _currentPosition = _targetPosition;
            
            // Ensure initial zoom is at least minZoom
            _targetZoom = Mathf.Max(_rectTransform.localScale.x, _minZoom);
            _currentZoom = _targetZoom;
            _rectTransform.localScale = new Vector3(_currentZoom, _currentZoom, 1f);

            ClampPosition();
        }

        private void Update()
        {
            EnsureInitialized();

            // Adjust current zoom if minZoom changes dynamically (e.g. screen/editor resize)
            if (_currentZoom < _minZoom)
            {
                _currentZoom = _minZoom;
                _targetZoom = Mathf.Max(_targetZoom, _minZoom);
                _rectTransform.localScale = new Vector3(_currentZoom, _currentZoom, 1f);
            }

            HandleZoomInput();
            UpdateMovement();
        }

        private void HandleZoomInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.005f)
            {
                // Zoom towards mouse pointer
                Vector2 localMousePos;
                Camera uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
                
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRectTransform, Input.mousePosition, uiCamera, out localMousePos))
                {
                    Vector2 beforeZoomLocal = (localMousePos - _rectTransform.anchoredPosition) / _currentZoom;

                    _targetZoom = Mathf.Clamp(_targetZoom + scroll * _zoomSensitivity * 12f, _minZoom, _maxZoom);

                    _currentZoom = Mathf.SmoothDamp(_currentZoom, _targetZoom, ref _zoomVelocity, _zoomSmoothTime);
                    _rectTransform.localScale = new Vector3(_currentZoom, _currentZoom, 1f);

                    Vector2 afterZoomLocal = beforeZoomLocal * _currentZoom;
                    _targetPosition = localMousePos - afterZoomLocal;
                }
            }
        }

        private void UpdateMovement()
        {
            if (!_isDragging && _useInertia && _dragVelocity.sqrMagnitude > 0.01f)
            {
                _targetPosition += _dragVelocity * Time.deltaTime;
                _dragVelocity = Vector2.Lerp(_dragVelocity, Vector2.zero, _inertiaDecay * Time.deltaTime);
            }

            // Smooth position interpolation for premium cinematic feel
            _currentPosition = Vector2.Lerp(_currentPosition, _targetPosition, 16f * Time.deltaTime);
            _rectTransform.anchoredPosition = _currentPosition;

            ClampPosition();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _dragVelocity = Vector2.zero;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _dragVelocity = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            EnsureInitialized();
            Camera uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
            
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRectTransform, eventData.position, uiCamera, out Vector2 currentPoint) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRectTransform, eventData.position - eventData.delta, uiCamera, out Vector2 lastPoint))
            {
                Vector2 delta = currentPoint - lastPoint;
                _targetPosition += delta * _dragSpeed;
                _dragVelocity = delta / Time.deltaTime;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
        }

        private void ClampPosition()
        {
            EnsureInitialized();
            if (_parentRectTransform == null) return;

            Vector2 parentSize = _parentRectTransform.rect.size;
            Vector2 mapSize = _rectTransform.rect.size * _currentZoom;

            // Clamping boundaries to make sure the map covers the screen completely (no black gaps)
            float limitX = Mathf.Max(0f, (mapSize.x - parentSize.x) * 0.5f);
            float limitY = Mathf.Max(0f, (mapSize.y - parentSize.y) * 0.5f);

            Vector2 pos = _rectTransform.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, -limitX, limitX);
            pos.y = Mathf.Clamp(pos.y, -limitY, limitY);

            _rectTransform.anchoredPosition = pos;
            _targetPosition = pos;
        }

        /// <summary>
        /// Centers the map smoothly on a specific coordinate (from bottom-left 2048x1143 space).
        /// </summary>
        public void FocusOnPosition(Vector2 bottomLeftPos)
        {
            EnsureInitialized();
            Vector2 mapSize = _rectTransform.rect.size;
            Vector2 centerRelativePos = bottomLeftPos - (mapSize * 0.5f);

            // Ensure zoom is at least minZoom
            if (_currentZoom < _minZoom)
            {
                _currentZoom = _minZoom;
                _rectTransform.localScale = new Vector3(_currentZoom, _currentZoom, 1f);
            }

            _targetPosition = -centerRelativePos * _currentZoom;
            _currentPosition = _targetPosition;
            _rectTransform.anchoredPosition = _targetPosition;
            _dragVelocity = Vector2.zero;

            ClampPosition();
        }
    }
}