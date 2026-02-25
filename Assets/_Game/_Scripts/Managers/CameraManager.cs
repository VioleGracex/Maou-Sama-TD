using UnityEngine;
using DG.Tweening;
using Zenject;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

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

        [Header("Transition")]
        [SerializeField] private float _transitionDuration = 0.5f;

        [Header("Controls")]
        [SerializeField] private float _moveSpeed = 20f;
        [SerializeField] private float _rotateSpeed = 100f; 

        [Inject] private Grid.GridManager _gridManager;
        [Inject] private InteractionManager _interactionManager;
        
        private Transform _cameraAnchor;
        private CinemachineOrbitalFollow _cmOrbital;
        private Sequence _viewSequence;
        #endregion

        #region Lifecycle
        public void Init()
        {  
            if (_gridManager != null)
            {
                _gridManager.EnsureCameraAnchor();
                _cameraAnchor = _gridManager.CameraAnchor;
            }

            // Get Components
            _cmOrbital = _battleCamera.GetComponent<CinemachineOrbitalFollow>();

            // Assign Targets
            if (_cameraAnchor != null)
            {
                _battleCamera.Follow = _cameraAnchor;
                _battleCamera.LookAt = _cameraAnchor;
            }
            else
            {
                Debug.LogError("[CameraManager] CameraAnchor is still null after EnsureCameraAnchor call!");
            }

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
        private void HandleInput()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                ToggleLock();
            }

            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                ToggleView();
            }
            
            // Mouse Rotation (Right Click)
            if (!IsLocked && Mouse.current.rightButton.isPressed)
            {
                float mouseX = Mouse.current.delta.x.ReadValue() * 0.1f;
                RotateCamera(mouseX);
            }

            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            if (_interactionManager != null && _interactionManager.IsDragging) return;

             HandleMovement();
        }

        private void HandleMovement()
        {
            if (IsLocked || CenterOnMap) return;
            if (_cameraAnchor == null) return;

            Vector2 input = Vector2.zero;
            // Use W/S/A/D or Arrows
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1;

            if (input.sqrMagnitude > 0.01f)
            {
                // Move relative to the camera's orbital rotation
                float yaw = 0f;
                if (_cmOrbital != null)
                {
                    yaw = _cmOrbital.HorizontalAxis.Value;
                }
                else if (Camera.main != null)
                {
                     yaw = Camera.main.transform.eulerAngles.y;
                }
                
                Quaternion q = Quaternion.Euler(0, yaw, 0);
                Vector3 move = q * new Vector3(input.x, 0, input.y);
                move.Normalize(); 
                
                _cameraAnchor.position += move * _moveSpeed * Time.deltaTime;
            }
        }
        
        private void RotateCamera(float delta)
        {
            if (_cmOrbital == null) return;
            
            // Kill any active tween if we take manual control
            if (_viewSequence != null && _viewSequence.IsActive()) _viewSequence.Kill();
            
            _cmOrbital.HorizontalAxis.Value += delta * _rotateSpeed * Time.deltaTime;
        }
        #endregion

        #region Public API
        public void ToggleLock()
        {
            IsLocked = !IsLocked;
            if (IsLocked)
            {
                ResetToCenter();
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
            
            bool forceHeading = (mode == ViewMode.Isometric) ? _forceIsoHeading : _forceTopDownHeading;
            float targetHeading = (mode == ViewMode.Isometric) ? _isoHeading : _topDownHeading;

            if (_viewSequence != null && _viewSequence.IsActive()) _viewSequence.Kill();

            if (immediate)
            {
                _cmOrbital.Radius = targetRadius;
                _cmOrbital.VerticalAxis.Value = targetVertical;
                if (forceHeading) _cmOrbital.HorizontalAxis.Value = targetHeading;
            }
            else
            {
                _viewSequence = DOTween.Sequence();
                
                _viewSequence.Join(DOTween.To(() => _cmOrbital.Radius, x => _cmOrbital.Radius = x, targetRadius, _transitionDuration));
                _viewSequence.Join(DOTween.To(() => _cmOrbital.VerticalAxis.Value, x => _cmOrbital.VerticalAxis.Value = x, targetVertical, _transitionDuration));
                
                if (forceHeading)
                {
                    // Calculate shortest path
                    float currentHeading = _cmOrbital.HorizontalAxis.Value;
                    float delta = Mathf.DeltaAngle(currentHeading, targetHeading);
                    float shortestTarget = currentHeading + delta;
                    
                    _viewSequence.Join(DOTween.To(() => _cmOrbital.HorizontalAxis.Value, x => _cmOrbital.HorizontalAxis.Value = x, shortestTarget, _transitionDuration));
                }
            }
        }
        
        public void FrameGrid(float centerX, float centerZ)
        {
             if (_cameraAnchor != null)
             {
                 _cameraAnchor.position = new Vector3(centerX, 0, centerZ);
                 IsLocked = true;
                 CenterOnMap = true;
             }
        }

        public void CenterCameraOnMap(bool immediate = true)
        {
            ResetToCenter();
        }

        public void ResetToCenter()
        {
            if (_gridManager != null && _cameraAnchor != null)
            {
                _cameraAnchor.position = _gridManager.GetGridCenter();
            }
        }
        #endregion
    }
}
