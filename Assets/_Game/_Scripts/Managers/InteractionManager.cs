using UnityEngine;
using MaouSamaTD.Grid;
using MaouSamaTD.UI;

namespace MaouSamaTD.Managers
{
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        public event System.Action<Tile> OnTileHovered;
        public event System.Action<Tile> OnTileClicked;
        
        // Placement Events
        public bool IsDragging { get; private set; }
        private Units.UnitData _draggedUnitData;
        private GameObject _ghostObject;
        private Tile _currentHoverTile;

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

        public void StartDrag(Units.UnitData data)
        {
            IsDragging = true;
            _draggedUnitData = data;
            
            // Create visual ghost
            if (_ghostObject != null) Destroy(_ghostObject);
            // Simple cube for now
            _ghostObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(_ghostObject.GetComponent<Collider>());
            _ghostObject.transform.localScale = Vector3.one * 0.8f; 
        }

        public void EndDrag(bool place)
        {
            if (place && _currentHoverTile != null)
            {
                // Validate placement (Currency, Tile Type)
                bool canAfford = CurrencyManager.Instance.CanAfford(_draggedUnitData.DeploymentCost);
                bool validTile = IsTileValidForUnit(_currentHoverTile, _draggedUnitData);

                if (canAfford && validTile && !_currentHoverTile.IsOccupied)
                {
                    DeploymentUI.Instance.SpawnUnit(_currentHoverTile, _draggedUnitData);
                }
                else
                {
                    Debug.Log("Invalid Placement or not enough funds.");
                }
            }

            IsDragging = false;
            _draggedUnitData = null;
            if (_ghostObject != null) Destroy(_ghostObject);
            _currentHoverTile = null;
        }

        private bool IsTileValidForUnit(Tile tile, Units.UnitData unit)
        {
            if (unit.ViableTiles == null || unit.ViableTiles.Count == 0) return false;
            return unit.ViableTiles.Contains(tile.Type);
        }

        private void HandleInput()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile == null)
                {
                     Vector2Int coord = GridManager.Instance.WorldToGridCoordinates(hit.point);
                     tile = GridManager.Instance.GetTileAt(coord);
                }

                if (tile != null)
                {
                    _currentHoverTile = tile;
                    OnTileHovered?.Invoke(tile);
                    
                    if (IsDragging && _ghostObject != null)
                    {
                        // Snap ghost
                        _ghostObject.transform.position = tile.transform.position + Vector3.up * 0.5f;
                        
                        // Colorize Validity
                        bool valid = IsTileValidForUnit(tile, _draggedUnitData) && !tile.IsOccupied;
                        var rend = _ghostObject.GetComponent<Renderer>();
                        if (rend != null)
                        {
                            rend.material.color = valid ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
                        }
                    }

                    if (Input.GetMouseButtonDown(0) && !IsDragging)
                    {
                        OnTileClicked?.Invoke(tile);
                    }
                }
            }
        }
    }
}
