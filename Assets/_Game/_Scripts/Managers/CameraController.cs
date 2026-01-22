using UnityEngine;
using DG.Tweening;
using Zenject;
using UnityEngine.InputSystem;

namespace MaouSamaTD.Managers
{
    public class CameraController : MonoBehaviour
    {
        public enum ViewMode
        {
            Isometric,
            TopDown
        }

        [Header("State")]
        public bool IsLocked = true;
        public bool CenterOnMap = false;
        public ViewMode CurrentMode = ViewMode.Isometric;

        [Header("Settings")]
        [SerializeField] private float _moveSpeed = 20f;
        [SerializeField] private float _rotateSpeed = 100f;
        [SerializeField] private float _zoomSpeed = 10f;
        [SerializeField] private float _transitionDuration = 0.5f;

        [Header("View Profiles")]
        [SerializeField] private Vector3 _isometricRotation = new Vector3(50f, 90f, 0f);
        [SerializeField] private float _isometricZoom = 15f;
        
        [SerializeField] private Vector3 _topDownRotation = new Vector3(90f, 90f, 0f);
        [SerializeField] private float _topDownZoom = 20f;

        private Camera _cam;
        private Vector3 _targetPosition;
        [Inject] private Grid.GridManager _gridManager;
        
        private void Start()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;

            if (_gridManager == null) _gridManager = FindObjectOfType<Grid.GridManager>();
            
            _targetPosition = transform.position;

            // Set initial state
            SetView(CurrentMode, true);
        }

        private void Update()
        {
            HandleInput();
            if (IsLocked && CenterOnMap)
            {
                CenterCameraOnMap();
            }
            else
            {
                // Only running this when NOT centering
                UpdateManualCoords();
            }
        }

        [Inject] private InteractionManager _interactionManager;

        private void HandleInput()
        {
            // Toggle Lock
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                IsLocked = !IsLocked;
                if (IsLocked)
                {
                    // Return to current mode view
                    SetView(CurrentMode);
                }
            }

            // View Switching (Only when Locked)
            if (IsLocked && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                CurrentMode = (CurrentMode == ViewMode.Isometric) ? ViewMode.TopDown : ViewMode.Isometric;
                SetView(CurrentMode);
            }

            // Ignore input if Interacting with UI or Units
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            if (_interactionManager != null && _interactionManager.IsDragging) return;

             // Handle Inputs
             HandleMovement();
             HandleRotation();
             HandleZoom();
        }

        private void HandleMovement()
        {
            if (IsLocked && CenterOnMap) return;

            Vector2 input = Vector2.zero;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1;

            if (input.sqrMagnitude > 0.01f)
            {
                MoveCamera(input.x, input.y, _moveSpeed * Time.deltaTime);
            }
        }
        
        private void HandleRotation()
        {
            // Unlocked: Rotation
            if (!IsLocked && Mouse.current.rightButton.isPressed)
            {
                float mouseX = Mouse.current.delta.x.ReadValue() * 0.1f; // Adjust sensitivity
                RotateCamera(mouseX);
            }
        }

        private void HandleZoom()
        {
             // Scroll Zoom
            float scroll = Mouse.current.scroll.y.ReadValue();
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // Scroll values can be large, normalize/clamp slightly
                float zoomFactor = Mathf.Clamp(scroll, -1f, 1f); 
                ZoomCamera(-zoomFactor * _zoomSpeed * Time.deltaTime * 50f);
            }
        }

        private void MoveCamera(float x, float z, float speed)
        {
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            
            Vector3 move = (forward * z + right * x) * speed;
            transform.position += move; 
            _targetPosition = transform.position;
        }

        private void RotateCamera(float inputX)
        {
             Vector3 eulers = transform.eulerAngles;
             eulers.y += inputX * _rotateSpeed * Time.deltaTime;
             transform.rotation = Quaternion.Euler(eulers); 
        }

        private void CenterCameraOnMap()
        {
            if (_gridManager == null) return;
            Vector3 center = _gridManager.GetGridCenter();
            FrameGrid(center.x, center.z);
        }


        
        private void ZoomCamera(float delta)
        {
            if (_cam.orthographic)
            {
                _cam.orthographicSize += delta;
                _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, 5f, 50f);
            }
            else
            {
                _cam.fieldOfView += delta;
                _cam.fieldOfView = Mathf.Clamp(_cam.fieldOfView, 10f, 90f);
            }
        }

        public void ToggleLock()
        {
            IsLocked = !IsLocked;
            if (IsLocked)
            {
                SetView(CurrentMode);
            }
        }

        public void ToggleView()
        {
            if (IsLocked)
            {
                CurrentMode = (CurrentMode == ViewMode.Isometric) ? ViewMode.TopDown : ViewMode.Isometric;
                SetView(CurrentMode);
            }
        }

        public void SetView(ViewMode mode, bool instant = false)
        {
            Vector3 targetRot = (mode == ViewMode.Isometric) ? _isometricRotation : _topDownRotation;
            
            if (instant)
            {
                transform.eulerAngles = targetRot;
            }
            else
            {
                transform.DORotate(targetRot, _transitionDuration);
            }
        }


        private void AdjustCameraSize()
        {
            if (_cam == null) return;
            // Simple aspect ratio logic: If screen is "tall" (Portrait), increase size to show same width
            // If screen is "wide" (Landscape), we usually cover enough width.
            
            float targetAspect = 16f/9f; // Base design aspect
            float currentAspect = (float)Screen.width / Screen.height;
            
            if (currentAspect < targetAspect)
            {
                 // We are thinner than expected (e.g. mobile portrait). Scale up size.
                 // This ensures grid width fits
                 // (Not implemented continuously to save perf? Update is fine for now)
            }
        }

        public void FrameGrid(float centerX, float centerZ)
        {
            // Use current rotation settings to determine offset
            Vector3 rot = (CurrentMode == ViewMode.Isometric) ? _isometricRotation : _topDownRotation;
            
            float height = transform.position.y;
            if (height < 10f) height = 20f; // Ensure height
            if (height > 40f) height = 40f; 

            // Calculate offset based on Rotation X (Pitch)
            // Pitch 90 = TopDown (Tan is infinite, dist is 0)
            // Pitch 50 = Iso (Tan is ~1.2, dist exists)
            
            float pitchRad = rot.x * Mathf.Deg2Rad;
            // Dist on Ground Plane from Target to CameraXZ
            float dist = height / Mathf.Tan(pitchRad);
            
            // If TopDown (90 deg), dist is nearly 0.
            
            // But we also need to account for Y-Rotation (Yaw).
            // _isometricRotation default was (50, 90, 0) -> Yaw 90 means Facing +X?
            // If Yaw is 0, we face +Z.
            
            float yawRad = rot.y * Mathf.Deg2Rad;
            
            // Camera Pos = Target - (Forward * dist_hypotenuse)? 
            // Simple Trig:
            // DeltaZ = -cos(Yaw) * dist
            // DeltaX = -sin(Yaw) * dist
            
            float offsetX = -Mathf.Sin(yawRad) * dist;
            float offsetZ = -Mathf.Cos(yawRad) * dist;
            
            _targetPosition = new Vector3(centerX + offsetX, height, centerZ + offsetZ);
            
            // Apply immediately to avoid "drift" feeling when snapping
            transform.position = _targetPosition;
            if (IsLocked) transform.eulerAngles = rot;
        }

        public void SetPosition(float x, float z)
        {
            // Allow manual override only if CenterOnMap is false (or forced by UI)
            if (!CenterOnMap)
            {
                _targetPosition = new Vector3(x, transform.position.y, z);
                transform.position = _targetPosition;
            }
        }
        
        // Inspector Helper
        [Header("Inspector Controls")]
        [Tooltip("X = World X, Y = World Height, Z = World Z")]
        [SerializeField] private Vector3 _manualPosition;
        
        private Vector3 _lastManualPos;
        private void UpdateManualCoords()
        {
             if (CenterOnMap) return;
             
             // Detect Inspector Change
             if (_manualPosition != _lastManualPos)
             {
                 // Apply all 3: X, Y (Height), Z
                 _targetPosition = _manualPosition;
                 transform.position = _targetPosition;
                 _lastManualPos = _manualPosition;
             }
             else
             {
                 // Update inspector fields to match current pos
                 if (Vector3.Distance(_manualPosition, transform.position) > 0.1f)
                 {
                     _manualPosition = transform.position;
                     _lastManualPos = _manualPosition;
                 }
             }
        }
    }
}

