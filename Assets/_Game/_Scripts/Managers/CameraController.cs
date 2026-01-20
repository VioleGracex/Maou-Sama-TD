using UnityEngine;
using DG.Tweening;
using Zenject;

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
        public ViewMode CurrentMode = ViewMode.Isometric;

        [Header("Settings")]
        [SerializeField] private float _moveSpeed = 20f;
        [SerializeField] private float _rotateSpeed = 100f;
        [SerializeField] private float _zoomSpeed = 10f;
        [SerializeField] private float _transitionDuration = 0.5f;

        [Header("View Profiles")]
        [SerializeField] private Vector3 _isometricRotation = new Vector3(50f, 90f, 0f);
        [SerializeField] private float _isometricZoom = 15f;
        
        [SerializeField] private Vector3 _topDownRotation = new Vector3(90f, 0f, 0f);
        [SerializeField] private float _topDownZoom = 20f;

        private Camera _cam;
        private Transform _targetTransform; // Center or pivot?
        
        // For simple orbit, we rotate the Camera's parent or the Camera itself if it has a pivot.
        // Assuming Camera is independent or we rotate around a point.
        // Let's implement a Pivot-based system or direct transform manipulation.
        // Direct transform manipulation around world center or current focus is easiest for a TD game.
        
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private float _targetZoom;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
            
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
            _targetZoom = _cam.orthographic ? _cam.orthographicSize : _cam.fieldOfView; // Assuming Ortho for Iso usually, but Perspective works too.

            // Set initial state
            SetView(CurrentMode, true);
        }

        private void Update()
        {
            HandleInput();
            UpdateCameraTransform();
        }

        [Inject] private InteractionManager _interactionManager;

        private void HandleInput()
        {
            // Toggle Lock
            if (Input.GetKeyDown(KeyCode.Space))
            {
                IsLocked = !IsLocked;
                if (IsLocked)
                {
                    // Return to current mode view
                    SetView(CurrentMode);
                }
            }

            // View Switching (Only when Locked)
            if (IsLocked && Input.GetKeyDown(KeyCode.Tab))
            {
                CurrentMode = (CurrentMode == ViewMode.Isometric) ? ViewMode.TopDown : ViewMode.Isometric;
                SetView(CurrentMode);
            }

            // Ignore input if Interacting with UI or Units
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            if (_interactionManager != null && _interactionManager.IsDragging) return;
            // Check touch pointer over UI
            if (Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;


            // Mobile Touch Logic
            if (Input.touchCount > 0)
            {
                HandleTouchInput();
            }
            else
            {
               HandleMouseInput();
            }
        }

        private void HandleTouchInput()
        {
             if (Input.touchCount == 1) // Pan
             {
                 if (IsLocked) // Allow Panning even if locked? Usually Camera is locked to center.
                 {
                    // If "Locked" means "Follow Focus", we shouldn't pan. 
                    // If "Locked" just means "Fixed Angle", we CAN pan. 
                    // Based on previous logic, Locked allowed limited interaction? 
                    // User said "lock unlock camera to allow rotation". So position might be free?
                    // Let's assume Panning is allowed always unless specifically disabled.
                 }
                 
                 Touch touch = Input.GetTouch(0);
                 if (touch.phase == TouchPhase.Moved)
                 {
                     Vector2 delta = touch.deltaPosition;
                     MoveCamera(-delta.x, -delta.y, 0.1f * _moveSpeed * Time.deltaTime); // Scale for touch sensitivity
                 }
             }
             else if (Input.touchCount == 2) // Zoom
             {
                 Touch touch0 = Input.GetTouch(0);
                 Touch touch1 = Input.GetTouch(1);
                 
                 Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
                 Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
                 
                 float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
                 float touchDeltaMag = (touch0.position - touch1.position).magnitude;
                 
                 float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
                 
                 ZoomCamera(deltaMagnitudeDiff * _zoomSpeed * 0.01f * Time.deltaTime);

                 // Optional: Twist (Rotation) if Unlocked
                 if (!IsLocked)
                 {
                    // Calculate angle delta?
                    // For simplicity, let's keep it to Pan/Zoom first. Twist is complex to separate from Pinch.
                 }
             }
        }
        
        private void HandleMouseInput()
        {
             // Unlocked: Rotation
            if (!IsLocked)
            {
                if (Input.GetMouseButton(1)) // Right Click Drag
                {
                    float mouseX = Input.GetAxis("Mouse X");
                    RotateCamera(mouseX);
                }
            }
            
            // Pan
            float h = Input.GetAxis("Horizontal"); // A/D
            float v = Input.GetAxis("Vertical");   // W/S
            
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                MoveCamera(h, v, _moveSpeed * Time.deltaTime);
            }
            
            // Scroll Zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                ZoomCamera(-scroll * _zoomSpeed * 10f * Time.deltaTime);
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

        private void UpdateCameraTransform()
        {
            // Smooth transitions handled by DOTween or Lerp
            // If using DOTween for transitions, we might not need manual Lerp in Update, 
            // but for "MoveTo" functionality it's good.
        }

        public void SetView(ViewMode mode, bool instant = false)
        {
            Vector3 targetRot = (mode == ViewMode.Isometric) ? _isometricRotation : _topDownRotation;
            
            // Adjust Zoom? 
            // If Orthographic
            
            if (instant)
            {
                transform.eulerAngles = targetRot;
                // Keep position? Or Reset position? 
                // Usually keep position but change Angle.
            }
            else
            {
                transform.DORotate(targetRot, _transitionDuration);
            }
        }


        public void FrameGrid(float centerX, float centerZ)
        {
            // Center the camera on the board
            _targetPosition = new Vector3(centerX, transform.position.y, centerZ);
            
            // Apply instant move
            transform.position = _targetPosition;
            
            // Ideally we also adjust zoom to fit, but centering is primary request
            // If we wanted to fit, we'd need width/height and extensive calcs.
        }
    }
}
