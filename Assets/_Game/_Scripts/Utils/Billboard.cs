using UnityEngine;

namespace MaouSamaTD.Utils
{
    /// <summary>
    /// Makes a transform face the camera.
    /// Optimized for isometric and perspective cameras.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        public enum BillboardMode
        {
            CameraRotation, // Align with camera's rotation (Best for Isometric/Ortho)
            LookAtCamera    // Face the camera's position (Best for Perspective)
        }

        [SerializeField] private BillboardMode _mode = BillboardMode.CameraRotation;
        [SerializeField] private bool _lockX = false;
        [SerializeField] private bool _lockY = false;
        [SerializeField] private bool _lockZ = false;
        
        [Tooltip("If true, only updates once in Start/OnEnable. Use if camera is static.")]
        [SerializeField] private bool _isStatic = false;

        private Transform _camTransform;

        private void Awake()
        {
            if (Camera.main != null)
                _camTransform = Camera.main.transform;
            else
                Debug.LogWarning("[Billboard] No Main Camera found in scene.");
        }

        private void OnEnable()
        {
            UpdateRotation();
        }

        private void Start()
        {
            UpdateRotation();
        }

        private void LateUpdate()
        {
            if (!_isStatic)
            {
                UpdateRotation();
            }
        }

        public void UpdateRotation()
        {
            if (_camTransform == null)
            {
                if (Camera.main != null) _camTransform = Camera.main.transform;
                else return;
            }

            if (_mode == BillboardMode.CameraRotation)
            {
                ApplyRotation(_camTransform.rotation);
            }
            else
            {
                Vector3 direction = _camTransform.position - transform.position;
                if (direction != Vector3.zero)
                {
                    ApplyRotation(Quaternion.LookRotation(-direction));
                }
            }
        }

        private void ApplyRotation(Quaternion targetRotation)
        {
            Vector3 euler = targetRotation.eulerAngles;
            Vector3 currentEuler = transform.eulerAngles;

            if (_lockX) euler.x = currentEuler.x;
            if (_lockY) euler.y = currentEuler.y;
            if (_lockZ) euler.z = currentEuler.z;

            transform.rotation = Quaternion.Euler(euler);
        }
    }
}