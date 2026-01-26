using System.Collections.Generic;
using UnityEngine;


namespace MaouSamaTD.Grid
{
    public class GridGenerator : MonoBehaviour
    {
        #region Settings
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
        #endregion

        #region Lifecycle
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
        #endregion

        #region Map Generation
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

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Vector2Int coord = new Vector2Int(x, y);
                    bool isHighGround = Random.value < _highGroundChance;
                    
                    if (y == 0 || y == _height - 1) isHighGround = true;

                    TileType type = isHighGround ? TileType.HighGround : TileType.Walkable;
                    _gridManager.CreateTile(coord, type);
                }
            }

            GenerateLanes();

            if (_generateWalls)
            {
                GenerateWalls();
            }
        }
        #endregion

        #region Walls
        private void GenerateWalls()
        {
            float cellSize = _gridManager.CellSize;
            float wallRealHeight = cellSize * _wallHeight;
            float yPos = wallRealHeight / 2f; 

            Transform wallContainer = _gridManager.WallContainer;
            if (wallContainer == null)
            {
                 _gridManager.Init();
                 wallContainer = _gridManager.WallContainer;
            }

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
                    wall.transform.localPosition = pos;
                    wall.name = $"Wall_{x}_{y}";
                    wall.transform.localScale = Vector3.Scale(new Vector3(cellSize, cellSize, cellSize), scaleMultiplier);
                }
                _generatedWalls.Add(wall);
            }

            if (_wallSouth)
            {
                Vector3 scale = new Vector3(1f, _wallHeight, _wallWidth);
                for (int x = -1; x <= _width; x++) CreateWallBlock(x, -1, scale);
            }
            
            if (_wallNorth)
            {
                Vector3 scale = new Vector3(1f, _wallHeight, _wallWidth);
                for (int x = -1; x <= _width; x++) CreateWallBlock(x, _height, scale);
            }

            if (_wallWest)
            {
                Vector3 scale = new Vector3(_wallWidth, _wallHeight, 1f);
                for (int y = 0; y < _height; y++) CreateWallBlock(-1, y, scale);
            }

            if (_wallEast)
            {
                Vector3 scale = new Vector3(_wallWidth, _wallHeight, 1f);
                for (int y = 0; y < _height; y++) CreateWallBlock(_width, y, scale);
            }
        }

        private void ClearWalls()
        {
            foreach (var wall in _generatedWalls)
            {
                if (wall != null)
                {
                    if (Application.isPlaying) Destroy(wall);
                    else DestroyImmediate(wall);
                }
            }
            _generatedWalls.Clear();
        }
        #endregion

        #region Lanes & Pathing
        private void GenerateLanes()
        {
            List<Vector2Int> currentSpawns = new List<Vector2Int>(_spawnPoints);
            List<Vector2Int> currentExits = new List<Vector2Int>(_exitPoints);

            if (currentSpawns.Count == 0) currentSpawns.Add(new Vector2Int(0, _height / 2));
            if (currentExits.Count == 0) currentExits.Add(new Vector2Int(_width - 1, _height / 2));

            foreach (var start in currentSpawns)
            {
                Vector2Int closestExit = GetClosestExit(start, currentExits);
                
                for (int i = 0; i < _lanesPerConnection; i++)
                {
                    List<Vector2Int> path = GeneratePath(start, closestExit);
                    
                    foreach (var p in path)
                    {
                         _gridManager.SetTileType(p, TileType.Walkable);
                    }
                }

                _gridManager.SetTileType(start, TileType.Spawn);
                _gridManager.SetTileType(closestExit, TileType.Exit);
                
                CreateMarker(start, _startPrefab, "StartMarker", PrimitiveType.Cylinder, Color.green);
                CreateMarker(closestExit, _endPrefab, "EndMarker", PrimitiveType.Cube, Color.red);
            }
        }
        
        private void CreateMarker(Vector2Int coord, GameObject prefab, string name, PrimitiveType fallbackShape, Color fallbackColor)
        {
             Tile t = _gridManager.GetTileAt(coord);
             if (t != null)
             {
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
            if (exits.Count == 0) return start;

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
            
            while (current != end)
            {
                int diffX = end.x - current.x;
                int diffY = end.y - current.y;

                bool moveX = false;

                if (diffX != 0 && diffY != 0)
                {
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
                
                if (path.Count > _width * _height) break;
            }
            return path;
        }
        #endregion

        #region Interaction
        [ContextMenu("Clear Map")]
        public void ClearMap()
        {
            if (_gridManager != null) _gridManager.ClearGrid();
        }

        public void AddSpawnPoint(Vector2Int coord)
        {
            if (!_spawnPoints.Contains(coord))
            {
                _spawnPoints.Add(coord);
                _gridManager.SetTileType(coord, TileType.Spawn);
                Debug.Log($"Added Spawn Point at {coord}");
                GenerateMap();
            }
        }

        public void AddExitPoint(Vector2Int coord)
        {
            if (!_exitPoints.Contains(coord))
            {
                _exitPoints.Add(coord);
                _gridManager.SetTileType(coord, TileType.Exit);
                Debug.Log($"Added Exit Point at {coord}");
                GenerateMap();
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
        #endregion
    }
}
