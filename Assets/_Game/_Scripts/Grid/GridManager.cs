using System.Collections.Generic;
using UnityEngine;


namespace MaouSamaTD.Grid
{
    public class GridManager : MonoBehaviour
    {
        #region Settings & References
        [Header("Grid Settings")]
        [SerializeField] private int _width = 10;
        [SerializeField] private int _height = 5;
        [SerializeField] private float _cellSize = 1f;

        [Header("References")]
        [SerializeField] private Tile _tilePrefab;
        [SerializeField] private Transform _gridContainer;
        [SerializeField] private Transform _wallContainer;

        private Dictionary<Vector2Int, Tile> _grid = new Dictionary<Vector2Int, Tile>();

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public Transform WallContainer => _wallContainer;
        #endregion

        #region Initialization
        public void Init()
        {
             if (_gridContainer == null)
            {
                var container = GameObject.Find("GridContainer");
                if (container == null) container = new GameObject("GridContainer");
                _gridContainer = container.transform;
            }

            if (_wallContainer == null)
            {
                var walls = transform.Find("Walls");
                if (walls == null) 
                {
                    walls = new GameObject("Walls").transform;
                    walls.SetParent(transform); 
                }
                _wallContainer = walls;
            }

            if (_gridContainer.childCount > 0)
            {
                var existingTiles = _gridContainer.GetComponentsInChildren<Tile>();
                foreach (var tile in existingTiles)
                {
                    if (!_grid.ContainsKey(tile.Coordinate))
                    {
                        _grid[tile.Coordinate] = tile;
                    }
                }
            }
            
            if (_grid.Count == 0 && Application.isPlaying)
            {
                 GenerateTestMap();
            }
        }
        #endregion
        
        #region Core
        public void GenerateTestMap()
        {
             // ...
        }

        public void CreateTile(Vector2Int coord, TileType type)
        {
            Vector3 position = new Vector3(coord.x * _cellSize, 0, coord.y * _cellSize);
            Tile tile = Instantiate(_tilePrefab, position, Quaternion.identity, _gridContainer);
            
            tile.transform.localScale = Vector3.one * _cellSize * 0.95f;
            
            tile.Initialize(coord, type);
            _grid[coord] = tile;
            
            if (type == TileType.HighGround)
                tile.transform.position += Vector3.up * 0.5f;
        }

        public void ClearGrid()
        {
            _grid.Clear();

            if (_gridContainer != null)
            {
                for (int i = _gridContainer.childCount - 1; i >= 0; i--)
                {
                    GameObject child = _gridContainer.GetChild(i).gameObject;
                    if (Application.isPlaying) Destroy(child);
                    else DestroyImmediate(child);
                }
            }

            if (_wallContainer != null)
            {
                 for (int i = _wallContainer.childCount - 1; i >= 0; i--)
                {
                    GameObject child = _wallContainer.GetChild(i).gameObject;
                    if (Application.isPlaying) Destroy(child);
                    else DestroyImmediate(child);
                }
            }
        }
        #endregion

        #region Type Access
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
        #endregion

        #region Helper
        public void SetTileType(Vector2Int coord, TileType type)
        {
            Tile tile = GetTileAt(coord);
            if (tile != null)
            {
                tile.Initialize(coord, type);
                
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

        public Vector3 GetGridCenter()
        {
            float centerX = (_width * _cellSize) / 2f - (_cellSize / 2f); 
            float centerY = (_height * _cellSize) / 2f - (_cellSize / 2f);
            return new Vector3(centerX, 0, centerY);
        }
        public Queue<Tile> GetPath(Vector2Int start, Vector2Int end, MaouSamaTD.Units.EnemyMovementType moveType)
        {
            if (!_grid.ContainsKey(start) || !_grid.ContainsKey(end)) return null;

            Queue<Tile> path = new Queue<Tile>();
            
            // Breadth First Search
            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            frontier.Enqueue(start);

            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            cameFrom[start] = start; // Marker

            bool found = false;

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();

                if (current == end)
                {
                    found = true;
                    break;
                }

                foreach (Vector2Int next in GetNeighbors(current, moveType))
                {
                    if (!cameFrom.ContainsKey(next))
                    {
                        frontier.Enqueue(next);
                        cameFrom[next] = current;
                    }
                }
            }

            if (found)
            {
                Vector2Int current = end;
                List<Tile> reversePath = new List<Tile>();
                while (current != start)
                {
                    reversePath.Add(_grid[current]);
                    current = cameFrom[current];
                }
                // reversePath.Add(_grid[start]); // Usually don't include start tile in path to traverse?
                // EnemyUnit expects next tile.
                
                reversePath.Reverse();
                foreach (var t in reversePath) path.Enqueue(t);
            }

            return path;
        }

        private IEnumerable<Vector2Int> GetNeighbors(Vector2Int current, MaouSamaTD.Units.EnemyMovementType moveType)
        {
            // Manhattan neighbors (4 directions)
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

            foreach (var dir in dirs)
            {
                Vector2Int next = current + dir;
                if (_grid.TryGetValue(next, out Tile tile))
                {
                    bool isWalkable = true;
                    
                    if (moveType == MaouSamaTD.Units.EnemyMovementType.Ground)
                    {
                        // Ground can walk on Walkable, Spawn, Exit.
                        // Cannot walk on Unwalkable, HighGround?
                        if (tile.Type == TileType.Unwalkable || tile.Type == TileType.HighGround) 
                            isWalkable = false;
                            
                        // If blocked by building?
                        // Usually pathfinding accounts for static walls (Unwalkable).
                        // Dynamic units (Towers) -> usually Towers are on HighGround or Walkable.
                        // If implementing mazing, we must check IsOccupied by Tower.
                        // Assuming Towers block path if placed on Walkable ground.
                        if (tile.IsOccupied && tile.Occupant is MaouSamaTD.Units.PlayerUnit)
                        {
                            // If tower is blocking?
                            // For now, assume yes if it's a "Melee" or "Blocker" tower? 
                            // Or all towers block?
                            // Let's assume Occupied = Blocked for pathfinding to avoid clipping.
                            isWalkable = false; 
                        }
                    }
                    // Flying ignores terrain/towers
                    
                    if (isWalkable) yield return next;
                }
            }
        }
        #endregion
    }
}
