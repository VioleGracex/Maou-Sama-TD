using System.Collections.Generic;
using UnityEngine;
using MaouSamaTD.Levels;
using Zenject;

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
        
        [Inject] private DiContainer _container;

        public int Width 
        { 
            get => _width; 
            set 
            {
                _width = value;
#if UNITY_EDITOR
                if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
#endif
            } 
        }
        public int Height 
        { 
            get => _height; 
            set 
            {
                _height = value;
#if UNITY_EDITOR
                if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
        public float CellSize => _cellSize;
        public Transform WallContainer => _wallContainer;
        public Transform CameraAnchor { get; private set; }
        
        // Spawn/Exit Points
        public Vector2Int SpawnPoint { get; private set; }
        public List<SpawnPointData> SpawnPoints { get; private set; } = new List<SpawnPointData>();
        public Vector2Int ExitPoint { get; private set; }
        public List<Vector2Int> ExitPoints { get; private set; } = new List<Vector2Int>();
        
        public Vector2Int GetTargetExitForSpawn(Vector2Int spawnCoord)
        {
            var spawnData = SpawnPoints.Find(s => s.Coordinate == spawnCoord);
            if (spawnData.Coordinate == spawnCoord && spawnData.TargetExitIndex >= 0 && spawnData.TargetExitIndex < ExitPoints.Count)
            {
                return ExitPoints[spawnData.TargetExitIndex];
            }
            return ExitPoint; // Default fallback
        }

        public void SetSpawnMapping(Vector2Int spawnCoord, int exitIndex)
        {
            int idx = SpawnPoints.FindIndex(s => s.Coordinate == spawnCoord);
            if (idx != -1)
            {
                var s = SpawnPoints[idx];
                s.TargetExitIndex = exitIndex;
                SpawnPoints[idx] = s;
            }
            else
            {
                SpawnPoints.Add(new SpawnPointData { Coordinate = spawnCoord, TargetExitIndex = exitIndex });
            }
        }
        #endregion

        #region Initialization

        public void Init()
        {
            if (_gridContainer == null)
            {
                // Fallback or Error? User said: "add in inspector i will assign grid container and wall container dont find them"
                // So we do nothing if null, maybe log warning if critical?
                Debug.LogWarning("GridManager: GridContainer is not assigned in Inspector!");
            }

            if (_wallContainer == null)
            {
                Debug.LogWarning("GridManager: WallContainer is not assigned in Inspector!");
            }

            if (_gridContainer.childCount > 0)
            {
                var existingTiles = _gridContainer.GetComponentsInChildren<Tile>();
                foreach (var tile in existingTiles)
                {
                    // For existing tiles (pre-placed), we might need to queue injection if not auto-injected by context
                    // But typically InstantiatePrefabForComponent handles new ones.
                    // Existing ones might need manual injection if they weren't part of scene context initial inject
                    if (_container != null) _container.Inject(tile);
                    
                    // Recalculate coordinate based on actual position
                    Vector2Int realCoord = WorldToGridCoordinates(tile.transform.position);
                    
                    if (tile.Type == TileType.None)
                    {
                        if (Application.isPlaying) Destroy(tile.gameObject);
                        else DestroyImmediate(tile.gameObject);
                        continue;
                    }

                    tile.Initialize(realCoord, tile.Type);

                if (!_grid.ContainsKey(realCoord))
                    {
                        _grid[realCoord] = tile;
                    }
                }
            }
            
            if (_grid.Count == 0 && Application.isPlaying)
            {
                 GenerateTestMap();
            }

            EnsureCameraAnchor(); // Ensure logic created
            
            // Sync settings with actual found tiles (Runs after load OR generation)
            // Sync settings with actual found tiles (Runs after load OR generation)
            RecalculateBounds();
            
            // Find Spawn/Exit
            FindSpawnAndExit();
        }

        private void FindSpawnAndExit()
        {
            // Default Fallback
            SpawnPoint = new Vector2Int(0, 0);
            SpawnPoints.Clear();
            ExitPoints.Clear();
            ExitPoint = new Vector2Int(_width - 1, _height - 1);

            if (_grid.Count > 0)
            {
                foreach (var kvp in _grid)
                {
                    if (kvp.Value.Type == TileType.SpawnPoint || kvp.Value.Type == TileType.SpawnPointHigh)
                    {
                        if (SpawnPoints.Count == 0) SpawnPoint = kvp.Key;
                        SpawnPoints.Add(new SpawnPointData { Coordinate = kvp.Key, TargetExitIndex = -1 });
                    }
                    if (kvp.Value.Type == TileType.ExitPoint || kvp.Value.Type == TileType.ExitPointHigh)
                    {
                        if (ExitPoints.Count == 0) ExitPoint = kvp.Key;
                        ExitPoints.Add(kvp.Key);
                    }
                }
            }
            
            if (SpawnPoints.Count == 0) SpawnPoints.Add(new SpawnPointData { Coordinate = SpawnPoint, TargetExitIndex = -1 });
            if (ExitPoints.Count == 0) ExitPoints.Add(ExitPoint);

            Debug.Log($"GridManager: Found {SpawnPoints.Count} Spawns, {ExitPoints.Count} Exits");
        }
        #endregion
        
        #region Core
        public void GenerateTestMap()
        {
            // Simple 10x5 loop if needed, or leave empty if map is pre-made
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    CreateTile(new Vector2Int(x, y), TileType.Walkable);
                }
            }
        }

        public Tile CreateTile(Vector2Int coord, TileType type)
        {
            if (type == TileType.None) 
            {
                if (_grid.ContainsKey(coord)) _grid.Remove(coord);
                return null;
            }

            Vector3 position = new Vector3(coord.x * _cellSize, 0, coord.y * _cellSize);
            
            Tile tile;
            if (_container != null)
            {
                tile = _container.InstantiatePrefabForComponent<Tile>(_tilePrefab, position, Quaternion.identity, _gridContainer);
            }
            else
            {
                // Fallback for non-Zenject usage (e.g. Editor tests outside runtime if needed)
                 tile = Instantiate(_tilePrefab, position, Quaternion.identity, _gridContainer);
            }
            
            tile.transform.localScale = Vector3.one * _cellSize;
            
            tile.Initialize(coord, type);
            _grid[coord] = tile;
            
            if (type == TileType.HighGround || type == TileType.DecoHighGround || type == TileType.NonWalkableDecor || type == TileType.Wall || type == TileType.SpawnPointHigh || type == TileType.ExitPointHigh)
                tile.transform.position += Vector3.up * 0.5f;
            else if (type == TileType.LowTile)
                tile.transform.position += Vector3.down * 0.2f;

            return tile;
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

        #region Camera Anchor
        public void EnsureCameraAnchor()
        {
            if (CameraAnchor == null)
            {
                var anchor = GameObject.Find("CameraAnchor");
                if (anchor == null) anchor = new GameObject("CameraAnchor");
                
                CameraAnchor = anchor.transform;
            }
            
            // Always update position when this is called, to ensure it matches current grid
            CameraAnchor.position = GetGridCenter();
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

            if (type == TileType.None)
            {
                if (tile != null)
                {
                    if (Application.isPlaying) Destroy(tile.gameObject);
                    else DestroyImmediate(tile.gameObject);
                    _grid.Remove(coord);
                    NotifyGridStateChanged();
                }
                return;
            }

            if (tile == null)
            {
                tile = CreateTile(coord, type);
            }

            if (tile != null)
            {
                tile.Initialize(coord, type);
                
                bool isHigh = type == TileType.HighGround || type == TileType.DecoHighGround || type == TileType.NonWalkableDecor || type == TileType.Wall || type == TileType.SpawnPointHigh || type == TileType.ExitPointHigh;
                float yOffset = isHigh ? 0.5f : 0f;
                
                // Low Tile special case: move it down slightly
                if (type == TileType.LowTile) yOffset = -0.2f;

                tile.transform.position = new Vector3(coord.x * _cellSize, yOffset, coord.y * _cellSize);
                NotifyGridStateChanged();
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
            if (_grid.Count == 0)
            {
                // Fallback if no tiles
                float cx = (_width - 1) * _cellSize / 2f; 
                float cy = (_height - 1) * _cellSize / 2f;
                return new Vector3(cx, 0, cy);
            }

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (var coord in _grid.Keys)
            {
                if (coord.x < minX) minX = coord.x;
                if (coord.x > maxX) maxX = coord.x;
                if (coord.y < minY) minY = coord.y;
                if (coord.y > maxY) maxY = coord.y;
            }

            // Center is average of min and max extent
            float centerX = (minX + maxX) * _cellSize / 2f;
            float centerY = (minY + maxY) * _cellSize / 2f;
            
            return new Vector3(centerX, 0, centerY);
        }

        [ContextMenu("Recalculate Bounds from Children")]
        public void RecalculateBounds()
        {
            // If called from editor context menu, we might need to find tiles manually if _grid is empty
            if (_grid.Count == 0)
            {
                var tiles = GetComponentsInChildren<Tile>();
                foreach (var t in tiles)
                {
                    Vector2Int c = WorldToGridCoordinates(t.transform.position);
                     if (!_grid.ContainsKey(c)) _grid[c] = t;
                }
            }

            if (_grid.Count == 0) return;

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (var coord in _grid.Keys)
            {
                if (coord.x < minX) minX = coord.x;
                if (coord.x > maxX) maxX = coord.x;
                if (coord.y < minY) minY = coord.y;
                if (coord.y > maxY) maxY = coord.y;
            }

            // Update Width/Height to match the extent (approximate)
            int newWidth = (maxX - minX) + 1;
            int newHeight = (maxY - minY) + 1;
            
            // Only update if larger? Or strictly match?
            // Strictly match is better for "Sync".
            _width = newWidth;
            _height = newHeight;
            
            Debug.Log($"Grid Bounds Recalculated: {_width}x{_height}");
        }
        
        private void OnDrawGizmos()
        {
            // 1. Configured Bounds (Yellow)
            // Based on Width/Height settings, assuming generation starts at (0,0) world space (current implementation)
            float configW = _width * _cellSize;
            float configH = _height * _cellSize;
            float configCX = (_width - 1) * _cellSize / 2f;
            float configCY = (_height - 1) * _cellSize / 2f;
            Vector3 configCenter = new Vector3(configCX, 0, configCY);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(configCenter, new Vector3(configW, 0.1f, configH));
            Gizmos.DrawSphere(configCenter, 0.3f);

            #if UNITY_EDITOR
            UnityEditor.Handles.Label(configCenter + Vector3.up * 2f, $"Config: {_width}x{_height}\nCenter: {configCenter}");
            #endif

            // 2. Actual Bounds (Cyan) from Tiles
            if (_grid.Count > 0)
            {
                int minX = int.MaxValue;
                int maxX = int.MinValue;
                int minY = int.MaxValue;
                int maxY = int.MinValue;

                foreach (var coord in _grid.Keys)
                {
                    if (coord.x < minX) minX = coord.x;
                    if (coord.x > maxX) maxX = coord.x;
                    if (coord.y < minY) minY = coord.y;
                    if (coord.y > maxY) maxY = coord.y;
                }

                // Calculate geometry center of the present tiles
                float actualCX = (minX + maxX) * _cellSize / 2f;
                float actualCY = (minY + maxY) * _cellSize / 2f;
                Vector3 actualCenter = new Vector3(actualCX, 0, actualCY);

                // Calculate total size based on extent
                float actualW = (maxX - minX + 1) * _cellSize;
                float actualH = (maxY - minY + 1) * _cellSize;

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(actualCenter, new Vector3(actualW, 0.2f, actualH));
                Gizmos.DrawSphere(actualCenter, 0.2f);

                Gizmos.color = Color.red;
                Gizmos.DrawRay(actualCenter, Vector3.up * 5f); // Show center clearly

                #if UNITY_EDITOR
                UnityEditor.Handles.Label(actualCenter + Vector3.up * 3f, $"Actual: {actualW:F1}x{actualH:F1}\nCenter: {actualCenter}");
                #endif
            }
        }
        public event System.Action OnGridStateChanged;
        public void NotifyGridStateChanged() => OnGridStateChanged?.Invoke();

        public Queue<Tile> GetPath(Vector2Int start, Vector2Int end, MaouSamaTD.Units.EnemyMovementType moveType, bool ignoreOccupants = false)
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

                foreach (Vector2Int next in GetNeighbors(current, moveType, ignoreOccupants))
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

        private IEnumerable<Vector2Int> GetNeighbors(Vector2Int current, MaouSamaTD.Units.EnemyMovementType moveType, bool ignoreOccupants)
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
                        // Ground cannot walk on high ground or obstacles
                        if (tile.Type == TileType.NonWalkableDecor || 
                            tile.Type == TileType.HighGround || 
                            tile.Type == TileType.DecoHighGround ||
                            tile.Type == TileType.SpawnPointHigh ||
                            tile.Type == TileType.ExitPointHigh ||
                            tile.Type == TileType.None ||
                            tile.Type == TileType.Wall ||
                            tile.Type == TileType.LowTile) // Adding LowTile as obstacle for ground if it's meant to be a pit/gap
                            isWalkable = false;
                    }
                    else if (moveType == MaouSamaTD.Units.EnemyMovementType.Flying)
                    {
                        // Flying MUST be on high ground or special tiles (cannot be on normal walkable tiles)
                        // Unless "Mixed" is specified.
                        if (tile.Type == TileType.Walkable || 
                            tile.Type == TileType.DecoWalkable ||
                            tile.Type == TileType.None)
                            isWalkable = false;
                    }
                    // Mixed can walk on both Ground and HighGround
                    else if (moveType == MaouSamaTD.Units.EnemyMovementType.Mixed)
                    {
                        if (tile.Type == TileType.None || tile.Type == TileType.Wall)
                            isWalkable = false;
                    }
                    
                    if (isWalkable && !ignoreOccupants && tile.IsOccupied && tile.Occupant is MaouSamaTD.Units.PlayerUnit)
                    {
                        // Towers block ground and mixed units, but flyers pass through
                        if (moveType != MaouSamaTD.Units.EnemyMovementType.Flying)
                        {
                            isWalkable = false;
                        }
                    }
                    
                    if (isWalkable) yield return next;
                }
            }
        }
        #endregion
    }
}
