using System.Collections.Generic;
using UnityEngine;


namespace MaouSamaTD.Grid
{
    public class GridGenerator : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private GridManager _gridManager;

        [Header("Dimensions")]
        [SerializeField] private int _width = 15;
        [SerializeField] private int _height = 10;

        [Header("Procedural Settings")]
        [SerializeField] private bool _useSeed = true;
        [SerializeField] private int _seed = 12345;
        [Range(0f, 1f)] [SerializeField] private float _highGroundChance = 0.3f;

        [Header("Lanes")]
        [Min(1)] [SerializeField] private int _lanesPerConnection = 1;
        [Tooltip("If empty, default logic will be used (Left -> Right)")]
        [SerializeField] private List<Vector2Int> _spawnPoints = new List<Vector2Int>();
        [SerializeField] private List<Vector2Int> _exitPoints = new List<Vector2Int>();

        private void Awake()
        {
            if (_gridManager == null) _gridManager = GetComponent<GridManager>();
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                GenerateMap();
            }
        }

        [ContextMenu("Generate Map")]
        public void GenerateMap()
        {
            if (_gridManager == null)
            {
                Debug.LogError("GridManager reference missing!");
                return;
            }

            if (_useSeed) Random.InitState(_seed);
            else
            {
                _seed = System.Environment.TickCount;
                Random.InitState(_seed);
            }

            _gridManager.ClearGrid();
            // In a real scenario, we might want to resize the grid data structure in Manager
            // For now, we will just tell Manager to create tiles at specific coordinates.

            // 1. Generate Base Grid (All Walkable temporarily)
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Vector2Int coord = new Vector2Int(x, y);
                    // Determine High Ground Randomly
                    bool isHighGround = Random.value < _highGroundChance;
                    
                    // Simple "Edge" logic for variation
                    if (y == 0 || y == _height - 1) isHighGround = true;

                    TileType type = isHighGround ? TileType.HighGround : TileType.Walkable;
                    _gridManager.CreateTile(coord, type);
                }
            }

            // 2. Generate Lanes (Paths from Spawns to Exits)
            GenerateLanes();
        }

        private void GenerateLanes()
        {
            // Use temporary lists to avoid dirtying the Inspector lists with auto-gen data
            List<Vector2Int> currentSpawns = new List<Vector2Int>(_spawnPoints);
            List<Vector2Int> currentExits = new List<Vector2Int>(_exitPoints);

            // Default setup if no points defined
            if (currentSpawns.Count == 0) currentSpawns.Add(new Vector2Int(0, _height / 2));
            if (currentExits.Count == 0) currentExits.Add(new Vector2Int(_width - 1, _height / 2));

            // Create paths for each spawn to the nearest exit
            // Create paths for each spawn to the nearest exit
            foreach (var start in currentSpawns)
            {
                Vector2Int closestExit = GetClosestExit(start, currentExits);
                
                // Generate multiple lanes if requested
                for (int i = 0; i < _lanesPerConnection; i++)
                {
                    List<Vector2Int> path = GeneratePath(start, closestExit);

                    // Apply path to grid (Force Walkable on path)
                    foreach (var p in path)
                    {
                         _gridManager.SetTileType(p, TileType.Walkable); // Carve path through mountains
                    }
                }

                // Set Start/End visuals LAST to override walkable
                _gridManager.SetTileType(start, TileType.Spawn);
                _gridManager.SetTileType(closestExit, TileType.Exit);
            }
        }
        
        private Vector2Int GetClosestExit(Vector2Int start, List<Vector2Int> exits)
        {
            if (exits.Count == 0) return start; // Fallback

            Vector2Int best = exits[0];
            float minDist = Vector2Int.Distance(start, best);

            foreach(var exit in exits)
            {
                float dist = Vector2Int.Distance(start, exit);
                if (dist < minDist)
                {
                    minDist = dist;
                    best = exit;
                }
            }
            return best;
        }



        private List<Vector2Int> GeneratePath(Vector2Int start, Vector2Int end)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int current = start;
            path.Add(current);

            // Robust "Random Walker" that always moves closer to target but with some wiggle
            // but guarantees arrival
            
            while (current != end)
            {
                // Determine direction
                int diffX = end.x - current.x;
                int diffY = end.y - current.y;

                bool moveX = false;

                if (diffX != 0 && diffY != 0)
                {
                    // Randomly choose axis, but favor the longer one?
                    moveX = Random.value > 0.5f; 
                }
                else if (diffX != 0) moveX = true;
                else moveX = false;
                
                if (moveX)
                {
                    current.x += System.Math.Sign(diffX);
                }
                else
                {
                    current.y += System.Math.Sign(diffY);
                }
                
                if (!path.Contains(current)) path.Add(current);
                
                // Safety break
                if (path.Count > _width * _height) break;
            }
            return path;
        }

        [ContextMenu("Clear Map")]
        public void ClearMap()
        {
            if (_gridManager != null) _gridManager.ClearGrid();
        }

        // --- Interaction API for Tiles ---

        public void AddSpawnPoint(Vector2Int coord)
        {
            if (!_spawnPoints.Contains(coord))
            {
                _spawnPoints.Add(coord);
                // Force update tile visual
                _gridManager.SetTileType(coord, TileType.Spawn);
                Debug.Log($"Added Spawn Point at {coord}");
                GenerateMap(); // Auto-regenerate
            }
        }

        public void AddExitPoint(Vector2Int coord)
        {
            if (!_exitPoints.Contains(coord))
            {
                _exitPoints.Add(coord);
                // Force update tile visual
                _gridManager.SetTileType(coord, TileType.Exit);
                Debug.Log($"Added Exit Point at {coord}");
                Debug.Log($"Added Exit Point at {coord}");
                GenerateMap(); // Auto-regenerate
            }
        }

        public void RemoveSpawnPoint(Vector2Int coord)
        {
            if (_spawnPoints.Contains(coord))
            {
                _spawnPoints.Remove(coord);
                Debug.Log($"Removed Spawn Point at {coord}");
                GenerateMap();
            }
        }

        public void RemoveExitPoint(Vector2Int coord)
        {
            if (_exitPoints.Contains(coord))
            {
                _exitPoints.Remove(coord);
                Debug.Log($"Removed Exit Point at {coord}");
                GenerateMap();
            }
        }
    }
}
