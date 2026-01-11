using UnityEngine;
using MaouSamaTD.Grid;

namespace MaouSamaTD.Managers
{
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        public event System.Action<Tile> OnTileHovered;
        public event System.Action<Tile> OnTileClicked;

        private Camera _mainCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Assuming Tiles have colliders and are on a layer we hit
                // Or we can just calculate grid pos from hit.point if plane is flat.
                // For now, let's use GridManager's WorldToGrid conversion if we hit the ground plane.
                
                // If we hit a Tile component directly
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile == null)
                {
                     // Fallback to world pos calculation
                     Vector2Int coord = GridManager.Instance.WorldToGridCoordinates(hit.point);
                     tile = GridManager.Instance.GetTileAt(coord);
                }

                if (tile != null)
                {
                    OnTileHovered?.Invoke(tile);
                    
                    if (Input.GetMouseButtonDown(0))
                    {
                        OnTileClicked?.Invoke(tile);
                    }
                }
            }
        }
    }
}
