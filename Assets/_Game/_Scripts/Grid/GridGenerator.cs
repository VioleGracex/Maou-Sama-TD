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

        [Header("Visuals")]
        [SerializeField] private GameObject _startPrefab;
        [SerializeField] private GameObject _endPrefab;
        [SerializeField] private GameObject _wallPrefab;

        [Header("Generation Settings")]
        [SerializeField] private bool _generateMapOnStart = true;
        [SerializeField] private bool _generateWalls = true;
        
        [Header("Wall Configuration")]
        [SerializeField] private bool _wallNorth = true;
        [SerializeField] private bool _wallSouth = true;
        [SerializeField] private bool _wallEast = true;
        [SerializeField] private bool _wallWest = true;
        
        [Header("Primitive Wall Settings")]
        [Tooltip("Width (Thickness) of the wall relative to cell size")]
        [SerializeField] private float _wallWidth = 1.0f;
        [Tooltip("Height of the wall relative to cell size")]
        [SerializeField] private float _wallHeight = 1.0f;

        private List<GameObject> _generatedWalls = new List<GameObject>();

        private void Awake()
        {
            if (_gridManager == null) _gridManager = GetComponent<GridManager>();
        }

        private void Start()
        {
            if (Application.isPlaying && _generateMapOnStart)
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
            ClearWalls();

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

            // 3. Generate Walls (if enabled)
            if (_generateWalls)
            {
                GenerateWalls();
            }
        }

        private void GenerateWalls()
        {
            float cellSize = _gridManager.CellSize;
            // Center yPos based on height. 
            // Standard cube is 1 unit high centered at 0.5. 
            // We scale Y by _wallHeight.
            // If _wallHeight=1, RealHeight=cellSize. Center is cellSize/2.
            float wallRealHeight = cellSize * _wallHeight;
            float yPos = wallRealHeight / 2f; 

            Transform wallContainer = _gridManager.transform.Find("Walls");
            if (wallContainer == null)
            {
                wallContainer = new GameObject("Walls").transform;
                wallContainer.SetParent(_gridManager.transform);
                wallContainer.localPosition = Vector3.zero;
            }

            // Helper for creation
            void CreateWallBlock(int x, int y, Vector3 scaleMultiplier)
            {
                Vector3 pos = new Vector3(x * cellSize, yPos, y * cellSize);
                GameObject wall;
                
                if (_wallPrefab != null)
                {
                    wall = Instantiate(_wallPrefab, wallContainer);
                    wall.transform.position = pos;
                }
                else
                {
                    wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.transform.SetParent(wallContainer, false);
                    wall.transform.position = pos;
                    wall.name = $"Wall_{x}_{y}";
                    // Scale based on cell size and multipliers
                    wall.transform.localScale = Vector3.Scale(new Vector3(cellSize, cellSize, cellSize), scaleMultiplier);
                }
                _generatedWalls.Add(wall);
            }

            // South (-1 y) - Runs along X, so thickness is Z
            if (_wallSouth)
            {
                Vector3 scale = new Vector3(1f, _wallHeight, _wallWidth);
                for (int x = -1; x <= _width; x++) CreateWallBlock(x, -1, scale);
            }
            
            // North (_height y) - Runs along X
            if (_wallNorth)
            {
                Vector3 scale = new Vector3(1f, _wallHeight, _wallWidth);
                for (int x = -1; x <= _width; x++) CreateWallBlock(x, _height, scale);
            }

            // West (-1 x) - Runs along Z, so thickness is X
            if (_wallWest)
            {
                Vector3 scale = new Vector3(_wallWidth, _wallHeight, 1f);
                for (int y = 0; y < _height; y++) CreateWallBlock(-1, y, scale);
            }

            // East (_width x) - Runs along Z
            if (_wallEast)
            {
                Vector3 scale = new Vector3(_wallWidth, _wallHeight, 1f);
                for (int y = 0; y < _height; y++) CreateWallBlock(_width, y, scale);
            }
        }

        private void ClearWalls()
        {
            // Destroy tracked walls
            foreach (var wall in _generatedWalls)
            {
                if (wall != null)
                {
                    if (Application.isPlaying) Destroy(wall);
                    else DestroyImmediate(wall);
                }
            }
            _generatedWalls.Clear();

            // Also clean up container children just in case
            if (_gridManager != null)
            {
                Transform wallContainer = _gridManager.transform.Find("Walls");
                if (wallContainer != null)
                {
                    // Loop backwards to destroy
                    for (int i = wallContainer.childCount - 1; i >= 0; i--)
                    {
                         GameObject go = wallContainer.GetChild(i).gameObject;
                         if (Application.isPlaying) Destroy(go);
                         else DestroyImmediate(go);
                    }
                }
            }
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
                
                // Add Visual Markers (Cylinders/Cubes)
                // Add Visual Markers (Cylinders/Cubes)
                CreateMarker(start, _startPrefab, "StartMarker", PrimitiveType.Cylinder, Color.green);
                CreateMarker(closestExit, _endPrefab, "EndMarker", PrimitiveType.Cube, Color.red);
            }
        }
        

        
        private void CreateMarker(Vector2Int coord, GameObject prefab, string name, PrimitiveType fallbackShape, Color fallbackColor)
        {
             Tile t = _gridManager.GetTileAt(coord);
             if (t != null)
             {
                 // Clear existing markers on this tile if any
                 // (In case multiple calls stack, or if tile reused)
                 foreach(Transform child in t.transform)
                 {
                     if (child.name == "StartMarker" || child.name == "EndMarker")
                         DestroyImmediate(child.gameObject);
                 }

                 GameObject marker;
                 if (prefab != null)
                 {
                     marker = Instantiate(prefab, t.transform);
                 }
                 else
                 {
                     marker = GameObject.CreatePrimitive(fallbackShape);
                     marker.transform.SetParent(t.transform, false);
                     marker.transform.localScale = new Vector3(0.8f, (fallbackShape == PrimitiveType.Cylinder ? 0.1f : 0.8f), 0.8f);
                     marker.GetComponent<Renderer>().material.color = fallbackColor;
                     DestroyImmediate(marker.GetComponent<Collider>());
                 }
                 
                 marker.name = name;
                 if (prefab == null) marker.transform.localPosition = Vector3.up * 0.5f;
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
