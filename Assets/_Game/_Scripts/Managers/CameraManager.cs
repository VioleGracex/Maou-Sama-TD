using UnityEngine;
using DG.Tweening;
using Zenject;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using NaughtyAttributes;
using MaouSamaTD.Levels;

namespace MaouSamaTD.Managers
{
    public class CameraManager : MonoBehaviour
    {
        #region Enums
        public enum ViewMode
        {
            Isometric,
            TopDown
        }
        #endregion

        #region Fields
        [Header("State")]
        public bool IsLocked = true;
        public bool CenterOnMap = true; 
        public ViewMode CurrentMode = ViewMode.Isometric;

        [Header("Cinemachine Integration")]
        [SerializeField] private CinemachineCamera _battleCamera;
        
        [Header("View Settings - Isometric")]
        [SerializeField] private float _isoRadius = 25f;
        [SerializeField] private float _isoVerticalAngle = 90f;
        [SerializeField] private bool _forceIsoHeading = false;
        [SerializeField] private float _isoHeading = 0f;

        [Header("View Settings - TopDown")]
        [SerializeField] private float _topDownRadius = 30f;
        [SerializeField] private float _topDownVerticalAngle = 180f;
        [SerializeField] private bool _forceTopDownHeading = false;
        [SerializeField] private float _topDownHeading = 0f; 

        [Header("View Settings - Orthographic (2D)")]
        [SerializeField] private float _isoOrthoSize = 4.15f;
        [SerializeField] private float _topDownOrthoSize = 6f;
        [SerializeField] private float _minOrthoSize = 2f;
        [SerializeField] private float _maxOrthoSize = 8f;

        [Header("Transition")]
        [SerializeField] private float _transitionDuration = 0.5f;

        [Header("Controls")]
        [SerializeField] private float _moveSpeed = 20f;
        [SerializeField] private float _rotateSpeed = 100f; 
        [SerializeField] private float _zoomSpeed = 2f;
        [SerializeField] private float _defaultZoom = 4.15f;
        [SerializeField] private float _minRadius = 10f;
        [SerializeField] private float _maxRadius = 60f;
        
        [Header("Testing")]
        [SerializeField] private Vector3 _testMapPosition;

        [Inject] private Grid.GridManager _gridManager;
        [Inject] private InteractionManager _interactionManager;
        
        private Transform _cameraAnchor;
        private CinemachineOrbitalFollow _cmOrbital;
        private Sequence _viewSequence;
        private Vector3 _isometricRotation;
        #endregion

        #region Lifecycle
        public void Init(MaouSamaTD.Levels.MapData mapData = null)
        {  
            if (_gridManager != null)
            {
                _gridManager.EnsureCameraAnchor();
                _cameraAnchor = _gridManager.CameraAnchor;
            }

            // Get Components
            _cmOrbital = _battleCamera.GetComponent<CinemachineOrbitalFollow>();
            
            // Ensure axes are not locked
            if (_cmOrbital != null)
            {
                var hAxis = _cmOrbital.HorizontalAxis;
                hAxis.Range = new Vector2(-180, 180);
                _cmOrbital.HorizontalAxis = hAxis;

                var vAxis = _cmOrbital.VerticalAxis;
                vAxis.Range = new Vector2(-180, 180);
                _cmOrbital.VerticalAxis = vAxis;
            }

            // Apply custom map zoom settings if available
            if (mapData != null)
            {
                if (mapData.AutoCalculateDefaultZoom)
                {
                    _defaultZoom = Mathf.Clamp(Mathf.Max(mapData.Width, mapData.Height) * 0.35f + 1.0f, 4.15f, 12f);
                }
                else
                {
                    _defaultZoom = mapData.CustomDefaultZoom;
                }
                _isoOrthoSize = _defaultZoom;
                _topDownOrthoSize = _defaultZoom;
                _maxOrthoSize = Mathf.Max(_maxOrthoSize, _defaultZoom * 1.5f);
            }

            // Assign Targets
            if (_cameraAnchor != null)
            {
                _battleCamera.Follow = _cameraAnchor;
                _battleCamera.LookAt = null; // UNHOOKED to absolutely prevent wobble and grid rotation
            }
            else
            {
                Debug.LogError("[CameraManager] CameraAnchor is still null after EnsureCameraAnchor call!");
            }

            // Store the initial rotation (which is your perfect Isometric angle from the inspector)
            _isometricRotation = _battleCamera.transform.eulerAngles;

            // Initial State
            SetView(CurrentMode, true);
            
            if (CenterOnMap && IsLocked)
            {
                ResetToCenter();
            }
            
            Debug.Log("[CameraManager] Initialized.");
        }

        private void Update()
        {
            if (!Application.isFocused) return;
            HandleInput();
        }
        #endregion

        #region Internal Logic
        [Header("Movement Settings")]
        [Tooltip("If true, W/A/S/D and panning moves relative to the screen. If false, it moves along the World X/Z axes.")]
        [SerializeField] private bool _screenRelativeMovement = true;

        [Header("Movement Limits")]
        [Tooltip("If true, limits the camera movement to stay near the map grid bounds.")]
        [SerializeField] private bool _limitMovementToMap = true;
        [Tooltip("Padding in world units added to the map boundaries.")]
        [SerializeField] private float _movementPadding = 5f;

        private void HandleInput()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame) ToggleLock();
                if (Keyboard.current.tabKey.wasPressedThisFrame) ToggleView();
            }
            
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            if (_interactionManager != null && _interactionManager.IsDragging) return;

            bool isPinching = false;

            // Touch Pinch Zoom
            if (!IsLocked && Touchscreen.current != null && Touchscreen.current.touches.Count >= 2)
            {
                var touch0 = Touchscreen.current.touches[0];
                var touch1 = Touchscreen.current.touches[1];

                if (touch0.press.isPressed && touch1.press.isPressed)
                {
                    isPinching = true;
                    Vector2 touch0PrevPos = touch0.position.ReadValue() - touch0.delta.ReadValue();
                    Vector2 touch1PrevPos = touch1.position.ReadValue() - touch1.delta.ReadValue();

                    float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
                    float currentMagnitude = (touch0.position.ReadValue() - touch1.position.ReadValue()).magnitude;

                    float difference = currentMagnitude - prevMagnitude;
                    
                    // Adjust the multiplier for touch zoom sensitivity
                    ZoomCamera(-difference * 0.02f); 
                }
            }

            // Mouse Panning (Middle/Right Click) or Touch Panning
            bool mousePan = Mouse.current != null && (Mouse.current.middleButton.isPressed || Mouse.current.rightButton.isPressed);
            bool touchPan = !isPinching && Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;

            if (!IsLocked && (mousePan || touchPan))
            {
                Vector2 delta = Vector2.zero;
                if (mousePan) delta = Mouse.current.delta.ReadValue();
                else if (touchPan) delta = Touchscreen.current.primaryTouch.delta.ReadValue();
                
                delta *= 0.02f;
                Vector3 move = new Vector3(-delta.x, 0, -delta.y);
                
                if (_screenRelativeMovement)
                {
                    float yaw = Camera.main != null ? Camera.main.transform.eulerAngles.y : 0f;
                    Quaternion q = Quaternion.Euler(0, yaw, 0);
                    move = q * move;
                }
                
                _cameraAnchor.position += move * _moveSpeed * Time.unscaledDeltaTime;
                ClampAnchorPosition();
            }

            // Zoom (Scroll Wheel)
            if (!IsLocked && Mouse.current != null && Mouse.current.scroll.ReadValue().y != 0)
            {
                // Use Sign to ensure consistent zoom steps regardless of mouse wheel hardware
                float scrollStep = Mathf.Sign(Mouse.current.scroll.ReadValue().y);
                ZoomCamera(-scrollStep);
            }

             HandleMovement();
        }

        private void HandleMovement()
        {
            if (IsLocked || CenterOnMap) return;
            if (_cameraAnchor == null) return;
            if (Keyboard.current == null) return;

            Vector2 input = Vector2.zero;
            // Use W/S/A/D or Arrows
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1;

            if (input.sqrMagnitude > 0.01f)
            {
                Vector3 move = new Vector3(input.x, 0, input.y);
                move.Normalize(); 

                if (_screenRelativeMovement)
                {
                    // Move relative to the camera's actual rotation
                    float yaw = Camera.main != null ? Camera.main.transform.eulerAngles.y : 0f;
                    Quaternion q = Quaternion.Euler(0, yaw, 0);
                    move = q * move;
                }
                
                _cameraAnchor.position += move * _moveSpeed * Time.unscaledDeltaTime;
                ClampAnchorPosition();
            }
        }

        private void ClampAnchorPosition()
        {
            if (!_limitMovementToMap || _cameraAnchor == null || _gridManager == null) return;

            float width = _gridManager.Width;
            float height = _gridManager.Height;
            float cellSize = _gridManager.CellSize;

            float padding = Mathf.Max(_movementPadding, Mathf.Max(width, height) * 0.2f);
            float minX = -padding;
            float maxX = (width - 1) * cellSize + padding;
            float minZ = -padding;
            float maxZ = (height - 1) * cellSize + padding;

            Vector3 pos = _cameraAnchor.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
            _cameraAnchor.position = pos;
        }
        
        private void RotateCamera(float delta)
        {
            if (_cmOrbital == null) return;
            
            // Kill any active tween if we take manual control
            if (_viewSequence != null && _viewSequence.IsActive()) _viewSequence.Kill();
            
            var hAxis = _cmOrbital.HorizontalAxis;
            hAxis.Value += delta * _rotateSpeed * Time.unscaledDeltaTime;
            _cmOrbital.HorizontalAxis = hAxis;
        }

        private void ZoomCamera(float delta)
        {
            if (IsLocked || _cmOrbital == null || _battleCamera == null) return;
            if (_viewSequence != null && _viewSequence.IsActive()) _viewSequence.Kill();
            
            // For Perspective
            float newRadius = Mathf.Clamp(_cmOrbital.Radius + delta * _zoomSpeed, _minRadius, _maxRadius);
            _cmOrbital.Radius = newRadius;

            // For Orthographic
            var lens = _battleCamera.Lens;
            float newOrthoSize = Mathf.Clamp(lens.OrthographicSize + delta * _zoomSpeed, _minOrthoSize, _maxOrthoSize);
            lens.OrthographicSize = newOrthoSize;
            _battleCamera.Lens = lens;
            
            if (CurrentMode == ViewMode.Isometric) 
            {
                _isoRadius = newRadius;
                _isoOrthoSize = newOrthoSize;
            }
            else 
            {
                _topDownRadius = newRadius;
                _topDownOrthoSize = newOrthoSize;
            }
        }
        #endregion

        #region Public API
        public void ToggleLock()
        {
            IsLocked = !IsLocked;
            CenterOnMap = IsLocked;
            if (IsLocked)
            {
                ResetToCenter(false);
            }
        }

        public void ToggleView()
        {
            CurrentMode = (CurrentMode == ViewMode.Isometric) ? ViewMode.TopDown : ViewMode.Isometric;
            SetView(CurrentMode);
        }

        public void SetView(ViewMode mode, bool immediate = false)
        {
            CurrentMode = mode;
            if (_cmOrbital == null) return;

            float targetRadius = (mode == ViewMode.Isometric) ? _isoRadius : _topDownRadius;
            float targetVertical = (mode == ViewMode.Isometric) ? _isoVerticalAngle : _topDownVerticalAngle;
            float targetOrthoSize = (mode == ViewMode.Isometric) ? _isoOrthoSize : _topDownOrthoSize;
            
            bool forceHeading = (mode == ViewMode.Isometric) ? _forceIsoHeading : _forceTopDownHeading;
            float targetHeading = (mode == ViewMode.Isometric) ? _isoHeading : _topDownHeading;
            
            // Rotate the camera to look straight down (90 degrees) for Top Down mode
            Vector3 targetRotation = (mode == ViewMode.Isometric) 
                ? _isometricRotation 
                : new Vector3(90f, _isometricRotation.y, _isometricRotation.z);

            if (_viewSequence != null && _viewSequence.IsActive()) _viewSequence.Kill();

            if (immediate)
            {
                _cmOrbital.Radius = targetRadius;
                
                var lens = _battleCamera.Lens;
                lens.OrthographicSize = targetOrthoSize;
                _battleCamera.Lens = lens;
                
                var vAxis = _cmOrbital.VerticalAxis;
                vAxis.Value = targetVertical;
                _cmOrbital.VerticalAxis = vAxis;

                if (forceHeading) 
                {
                    var hAxis = _cmOrbital.HorizontalAxis;
                    hAxis.Value = targetHeading;
                    _cmOrbital.HorizontalAxis = hAxis;
                }
                
                _battleCamera.transform.eulerAngles = targetRotation;
                
                if (_battleCamera != null) _battleCamera.PreviousStateIsValid = false;
            }
            else
            {
                _viewSequence = DOTween.Sequence().SetUpdate(true);
                
                _viewSequence.Join(DOTween.To(() => _cmOrbital.Radius, x => _cmOrbital.Radius = x, targetRadius, _transitionDuration));
                
                _viewSequence.Join(DOTween.To(() => _battleCamera.Lens.OrthographicSize, x => 
                {
                    var lens = _battleCamera.Lens;
                    lens.OrthographicSize = x;
                    _battleCamera.Lens = lens;
                }, targetOrthoSize, _transitionDuration));
                
                _viewSequence.Join(_battleCamera.transform.DORotate(targetRotation, _transitionDuration));
                
                _viewSequence.Join(DOTween.To(() => _cmOrbital.VerticalAxis.Value, x => 
                {
                    var vAxis = _cmOrbital.VerticalAxis;
                    vAxis.Value = x;
                    _cmOrbital.VerticalAxis = vAxis;
                }, targetVertical, _transitionDuration));
                
                if (forceHeading)
                {
                    // Calculate shortest path
                    float currentHeading = _cmOrbital.HorizontalAxis.Value;
                    float delta = Mathf.DeltaAngle(currentHeading, targetHeading);
                    float shortestTarget = currentHeading + delta;
                    
                    _viewSequence.Join(DOTween.To(() => _cmOrbital.HorizontalAxis.Value, x => 
                    {
                        var hAxis = _cmOrbital.HorizontalAxis;
                        hAxis.Value = x;
                        _cmOrbital.HorizontalAxis = hAxis;
                    }, shortestTarget, _transitionDuration));
                }
                
                _viewSequence.Join(_battleCamera.transform.DORotate(targetRotation, _transitionDuration));
            }
        }
        
        public void FrameGrid(float centerX, float centerZ, bool instant = false)
        {
             Vector3 newPos = new Vector3(centerX, 0, centerZ);
             if (instant)
             {
                 _cameraAnchor.position = newPos;
                 IsLocked = true;
                 CenterOnMap = true;
                 
                 var lens = _battleCamera.Lens;
                 lens.OrthographicSize = _defaultZoom;
                 _battleCamera.Lens = lens;
                 
                 if (CurrentMode == ViewMode.Isometric) _isoOrthoSize = _defaultZoom;
                 else _topDownOrthoSize = _defaultZoom;
             }
             else
             {
                 IsLocked = true;
                 CenterOnMap = true;
                 
                 // Smoothly move anchor using DOTween
                 _cameraAnchor.DOMove(newPos, _transitionDuration).SetEase(Ease.OutQuad).SetUpdate(true);
                 
                 // Smoothly reset zoom
                 DOTween.To(() => _battleCamera.Lens.OrthographicSize, x => 
                 {
                     var lens = _battleCamera.Lens;
                     lens.OrthographicSize = x;
                     _battleCamera.Lens = lens;
                 }, _defaultZoom, _transitionDuration).SetEase(Ease.OutQuad).SetUpdate(true).OnComplete(() => 
                 {
                     if (CurrentMode == ViewMode.Isometric) _isoOrthoSize = _defaultZoom;
                     else _topDownOrthoSize = _defaultZoom;
                 });
             }
        }

        public void CenterCameraOnMap(bool immediate = true)
        {
            ResetToCenter(immediate);
        }

        public void ResetToCenter(bool immediate = false)
        {
            if (_gridManager != null && _cameraAnchor != null)
            {
                Vector3 newPos = _gridManager.GetGridCenter();
                
                if (immediate)
                {
                    Vector3 oldPos = _cameraAnchor.position;
                    _cameraAnchor.position = newPos;
                    if (_battleCamera != null)
                    {
                        _battleCamera.OnTargetObjectWarped(_cameraAnchor, newPos - oldPos);
                        _battleCamera.PreviousStateIsValid = false;
                    }
                    
                    var lens = _battleCamera.Lens;
                    lens.OrthographicSize = _defaultZoom;
                    _battleCamera.Lens = lens;
                 
                    if (CurrentMode == ViewMode.Isometric) _isoOrthoSize = _defaultZoom;
                    else _topDownOrthoSize = _defaultZoom;
                }
                else
                {
                    // Smoothly move anchor using DOTween
                    _cameraAnchor.DOMove(newPos, _transitionDuration).SetEase(Ease.OutQuad).SetUpdate(true);
                    
                    // Smoothly reset zoom
                    DOTween.To(() => _battleCamera.Lens.OrthographicSize, x => 
                    {
                        var lens = _battleCamera.Lens;
                        lens.OrthographicSize = x;
                        _battleCamera.Lens = lens;
                    }, _defaultZoom, _transitionDuration).SetEase(Ease.OutQuad).SetUpdate(true).OnComplete(() => 
                    {
                        if (CurrentMode == ViewMode.Isometric) _isoOrthoSize = _defaultZoom;
                        else _topDownOrthoSize = _defaultZoom;
                    });
                }
            }
        }

        [Button("Apply Current View Settings")]
        private void Editor_ApplySettings()
        {
            if (_battleCamera == null) return;
            if (_cmOrbital == null) _cmOrbital = _battleCamera.GetComponent<CinemachineOrbitalFollow>();
            
            SetView(CurrentMode, true);
        }

        [Button("Center Camera On Map")]
        private void Editor_CenterCamera()
        {
            if (_cameraAnchor == null && _gridManager != null)
            {
                _cameraAnchor = _gridManager.CameraAnchor;
            }
            
            if (_cameraAnchor == null)
            {
                // Fallback for editor if Zenject hasn't run
                var gm = FindAnyObjectByType<Grid.GridManager>();
                if (gm != null)
                {
                    gm.EnsureCameraAnchor();
                    _cameraAnchor = gm.CameraAnchor;
                    _cameraAnchor.position = gm.GetGridCenter();
                    return;
                }
            }

            ResetToCenter();
        }

        [Button("Assign Anchor Targets")]
        private void Editor_AssignTargets()
        {
            if (_battleCamera == null) return;
            
            if (_cameraAnchor == null)
            {
                var gm = FindAnyObjectByType<Grid.GridManager>();
                if (gm != null)
                {
                    gm.EnsureCameraAnchor();
                    _cameraAnchor = gm.CameraAnchor;
                }
            }

            if (_cameraAnchor != null)
            {
                _battleCamera.Follow = _cameraAnchor;
                _battleCamera.LookAt = _cameraAnchor;
            }
        }

        [Button("Adjust Camera for Testing")]
        private void Editor_AdjustForTesting()
        {
            if (_cameraAnchor == null) Editor_AssignTargets();
            if (_cameraAnchor == null) return;

            IsLocked = false;
            CenterOnMap = false;
            _cameraAnchor.position = _testMapPosition;
            
            Debug.Log($"[CameraManager] Adjusted for testing. Position: {_testMapPosition}. Camera Unlocked, Auto-Center Disabled.");
        }

        [Button("Save Current as Test Position")]
        private void Editor_SaveCurrentAsTestPosition()
        {
            if (_cameraAnchor == null) Editor_AssignTargets();
            if (_cameraAnchor != null)
            {
                _testMapPosition = _cameraAnchor.position;
                Debug.Log($"[CameraManager] Saved current position {_testMapPosition} as test position.");
            }
        }
        public void Shake(float duration = 0.3f, float strength = 0.08f)
        {
            // Shake the camera itself, NOT the anchor. Shaking the anchor shifts the Cinemachine
            // follow target which makes the entire map appear to move. Shaking Camera.main directly
            // creates a screen-space vibration that reads as camera shake, not world movement.
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.DOKill(false);
                cam.transform.DOShakePosition(duration, strength, 12, 90, false, true);
            }
        }
        #endregion
    }
}
