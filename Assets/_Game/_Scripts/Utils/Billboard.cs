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
                transform.forward = _mainCamera.transform.forward;
            }
        }
    }
}