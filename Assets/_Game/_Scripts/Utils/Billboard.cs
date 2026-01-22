using UnityEngine;

namespace MaouSamaTD.Utils
{
    public class Billboard : MonoBehaviour
    {
        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_mainCamera != null)
            {
                // transform.forward = _mainCamera.transform.forward; 
                // Using rotation copies the camera's full orientation (including Up vector), 
                // which is better for 2D sprites in 3D space to prevent flipping in Top-Down.
                transform.rotation = _mainCamera.transform.rotation;
            }
        }
    }
}