using UnityEngine;
using UnityEngine.EventSystems;

namespace MaouSamaTD.UI.MainMenu
{
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class CampaignMapZoomPan : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        [Header("Zoom Settings")]
        [SerializeField] private float _minZoom = 0.5f;
        [SerializeField] private float _maxZoom = 4.0f;
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

        private bool _minZoomInitialized = false;

        public float MinZoom
        {
            get => _minZoom;
            set => _minZoom = value;
        }

        public float MaxZoom
        {
            get => _maxZoom;
            set => _maxZoom = value;
        }

        private void EnsureInitialized()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
                _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _rectTransform.sizeDelta = new Vector2(2048f, 1143f);
            }
            if (_parentRectTransform == null && _rectTransform != null)
                _parentRectTransform = _rectTransform.parent as RectTransform;
            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();

            // Only compute the fill-screen minimum once on first init, when sizes are valid
            if (!_minZoomInitialized && _parentRectTransform != null && _rectTransform != null)
            {
                float parentWidth = _parentRectTransform.rect.width;
                float parentHeight = _parentRectTransform.rect.height;
                float mapWidth = _rectTransform.rect.width;
                float mapHeight = _rectTransform.rect.height;

                if (parentWidth > 0f && parentHeight > 0f && mapWidth > 0f && mapHeight > 0f)
                {
                    float minZoomX = parentWidth / mapWidth;
                    float minZoomY = parentHeight / mapHeight;
                    // fillMin = minimum to cover the screen; _minZoom is user setting floor
                    float fillMin = Mathf.Max(minZoomX, minZoomY);
                    // Only override if inspector value is too small (would show black bars)
                    if (_minZoom < fillMin) _minZoom = fillMin;
                    _minZoomInitialized = true;
                }
            }
        }

        private void Awake()
        {
            EnsureInitialized();

            // Enable EnhancedTouchSupport for high-precision pinch-to-zoom on touch screens
            try
            {
                if (!UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled)
                {
                    UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CampaignMapZoomPan] Could not enable EnhancedTouchSupport: {ex.Message}");
            }

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

            // Enforce minZoom floor but never fight user input by resetting _targetZoom here
            if (_currentZoom < _minZoom)
            {
                _currentZoom = _minZoom;
                if (_targetZoom < _minZoom) _targetZoom = _minZoom;
                _rectTransform.localScale = new Vector3(_currentZoom, _currentZoom, 1f);
            }

            HandleZoomInput();

            // Smoothly interpolate current zoom to target zoom
            if (Mathf.Abs(_currentZoom - _targetZoom) > 0.001f)
            {
                _currentZoom = Mathf.SmoothDamp(_currentZoom, _targetZoom, ref _zoomVelocity, _zoomSmoothTime);
                _rectTransform.localScale = new Vector3(_currentZoom, _currentZoom, 1f);
            }

            UpdateMovement();
        }

        private void HandleZoomInput()
        {
            float zoomDelta = 0f;
            Vector2 zoomCenterScreen = Vector2.zero;
            bool hasZoomInput = false;

            // 1. Check Multi-touch (Pinch-to-Zoom)
            if (UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled &&
                UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count >= 2)
            {
                var touch0 = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];
                var touch1 = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[1];

                float currentDist = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);
                float prevDist = Vector2.Distance(touch0.screenPosition - touch0.delta, touch1.screenPosition - touch1.delta);

                if (prevDist > 0f)
                {
                    float deltaDist = currentDist - prevDist;
                    // Scale pinch sensitivity
                    zoomDelta = deltaDist * 0.005f;
                    zoomCenterScreen = (touch0.screenPosition + touch1.screenPosition) * 0.5f;
                    hasZoomInput = Mathf.Abs(zoomDelta) > 0.0001f;
                }
            }
            // 2. Check Mouse Scroll Wheel (supporting BOTH new and legacy input systems)
            float scrollDelta = 0f;
            bool scrollInputDetected = false;

            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                var val = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue();
                if (Mathf.Abs(val.y) > 0.01f)
                {
                    // Scale scroll y value down
                    scrollDelta = val.y * 0.0005f;
                    scrollInputDetected = true;
                }
            }



            if (scrollInputDetected)
            {
                zoomDelta = scrollDelta;
                if (UnityEngine.InputSystem.Pointer.current != null)
                {
                    zoomCenterScreen = UnityEngine.InputSystem.Pointer.current.position.ReadValue();
                }
                else if (UnityEngine.InputSystem.Mouse.current != null)
                {
                    zoomCenterScreen = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                }
                else
                {
                    zoomCenterScreen = new Vector2(Screen.width / 2f, Screen.height / 2f);
                }
                hasZoomInput = true;
            }

            if (hasZoomInput)
            {
                Vector2 localZoomCenter;
                Camera uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRectTransform, zoomCenterScreen, uiCamera, out localZoomCenter))
                {
                    ZoomRelative(zoomDelta * _zoomSensitivity * 30f, localZoomCenter);
                }
            }
        }

        private void ZoomRelative(float zoomDelta, Vector2 localZoomCenter)
        {
            Vector2 beforeZoomLocal = (localZoomCenter - _rectTransform.anchoredPosition) / _currentZoom;

            _targetZoom = Mathf.Clamp(_targetZoom + zoomDelta, _minZoom, _maxZoom);

            // Calculate target position based on new zoom target to center it perfectly
            Vector2 afterZoomLocal = beforeZoomLocal * _targetZoom;
            _targetPosition = localZoomCenter - afterZoomLocal;
        }

        public void ZoomIn()
        {
            EnsureInitialized();
            ZoomRelative(0.25f, Vector2.zero); // Center of screen
        }

        public void ZoomOut()
        {
            EnsureInitialized();
            ZoomRelative(-0.25f, Vector2.zero);
        }

        /// <summary>Set zoom from normalized 0-1 value mapping minZoom..maxZoom.</summary>
        public void SetZoomNormalized(float normalizedValue)
        {
            EnsureInitialized();
            float targetZoom = Mathf.Lerp(_minZoom, _maxZoom, normalizedValue);
            _targetZoom = Mathf.Clamp(targetZoom, _minZoom, _maxZoom);
            _currentZoom = _targetZoom;
            _rectTransform.localScale = new Vector3(_currentZoom, _currentZoom, 1f);
            _zoomVelocity = 0f;
        }

        /// <summary>Returns current zoom as 0-1 normalized relative to min/max zoom.</summary>
        public float GetZoomNormalized()
        {
            EnsureInitialized();
            if (Mathf.Approximately(_maxZoom, _minZoom)) return 0f;
            return Mathf.InverseLerp(_minZoom, _maxZoom, _currentZoom);
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