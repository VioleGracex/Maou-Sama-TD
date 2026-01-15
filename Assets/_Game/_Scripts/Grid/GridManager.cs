using System.Collections.Generic;
using UnityEngine;


namespace MaouSamaTD.Grid
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Grid Settings")]
        [SerializeField] private int _width = 10;
        [SerializeField] private int _height = 5;
        [SerializeField] private float _cellSize = 1f;
        


        [Header("References")]
        [SerializeField] private Tile _tilePrefab;
        [SerializeField] private Transform _gridContainer;

        private Dictionary<Vector2Int, Tile> _grid = new Dictionary<Vector2Int, Tile>();

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;

                private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_gridContainer == null)
            {
                var container = GameObject.Find("GridContainer");
                if (container == null) container = new GameObject("GridContainer");
                _gridContainer = container.transform;
            }
        }

        private void Start()
        {
            if (_grid.Count == 0 && Application.isPlaying)
            {
                // GenerateProceduralMap(); // Removed
            }
        }
        
        public void GenerateTestMap()
        {
             // ... (Keep existing if needed, or replace)
             // GenerateProceduralMap(); // Removed
        }

        public void CreateTile(Vector2Int coord, TileType type)
        {
            Vector3 position = new Vector3(coord.x * _cellSize, 0, coord.y * _cellSize);
            Tile tile = Instantiate(_tilePrefab, position, Quaternion.identity, _gridContainer);
            
            // Scale tile to match cell size
            tile.transform.localScale = Vector3.one * _cellSize;
            
            tile.Initialize(coord, type);
            _grid[coord] = tile;
            
            // Adjust height for High Ground visuals
            if (type == TileType.HighGround)
                tile.transform.position += Vector3.up * 0.5f;
        }

        public void ClearGrid()
        {
            foreach (var tile in _grid.Values)
            {
                if (tile != null)
                {
                    if (Application.isPlaying) Destroy(tile.gameObject);
                    else DestroyImmediate(tile.gameObject);
                }
            }
            _grid.Clear();
        }

        public Tile GetTileAt(Vector2Int coord)
        {
            if (_grid.TryGetValue(coord, out Tile tile))
            {
                return tile;
            }
            return null;
        }

        public IEnumerable<Tile> GetAllTiles()
        {
            return _grid.Values;
        }

        // Helper to modify grid after generation
        public void SetTileType(Vector2Int coord, TileType type)
        {
            Tile tile = GetTileAt(coord);
            if (tile != null)
            {
                // Re-initialize to update visuals if we add more visual logic later
                tile.Initialize(coord, type);
                
                // Temporary visual update for simple cube/tile debugging
                // (Ideally tiles would have models that change based on type)
                float yOffset = type == TileType.HighGround ? 0.5f : 0f;
                tile.transform.position = new Vector3(coord.x * _cellSize, yOffset, coord.y * _cellSize);
            }
        }

        public Vector2Int WorldToGridCoordinates(Vector3 worldPosition)
        {
             int x = Mathf.RoundToInt(worldPosition.x / _cellSize);
             int y = Mathf.RoundToInt(worldPosition.z / _cellSize);
             return new Vector2Int(x, y);
        }
        
        public Vector3 GridToWorldPosition(Vector2Int coord)
        {
            Tile tile = GetTileAt(coord);
            if(tile != null) return tile.transform.position;
            return new Vector3(coord.x * _cellSize, 0, coord.y * _cellSize);
        }
    }
}
