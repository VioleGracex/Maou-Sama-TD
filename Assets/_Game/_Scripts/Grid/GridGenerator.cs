using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using MaouSamaTD.Levels;


namespace MaouSamaTD.Grid
{
    public class GridGenerator : MonoBehaviour
    {
        #region Settings
        [Header("Target")]
        [SerializeField] private GridManager _gridManager;
        [Header("Extraction Settings")]
        [SerializeField] private string _extractPath = "Assets/_Game/Data/Maps/";
        [SerializeField] private string _extractFileName = "NewMapData";

        // Dimensions are now taken from GridManager to avoid duplication


        [Header("Procedural Settings")]
        [SerializeField] private MapData _mapData;
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
        [SerializeField] private Material _wallMaterial; // Override Material

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
        [Button("Generate Map")]
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

            // Sync Dimensions from MapData if available
            if (_mapData != null)
            {
                _gridManager.Width = _mapData.Width;
                _gridManager.Height = _mapData.Height;
            }

            int width = _gridManager.Width;
            int height = _gridManager.Height;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int coord = new Vector2Int(x, y);
                    TileType type = TileType.Walkable;

                    if (_mapData != null && _mapData.UseManualLayout)
                    {
                        if (_mapData.ManualHighGround.Contains(coord)) type = TileType.HighGround;
                        else if (_mapData.DecoratedWalkable.Contains(coord)) type = TileType.DecoratedWalkable;
                        else if (_mapData.DecoratedHighGround.Contains(coord)) type = TileType.DecoratedHighGround;
                    }
                    else
                    {
                        bool isHighGround = Random.value < _highGroundChance;
                        if (y == 0 || y == height - 1) isHighGround = true;
                        type = isHighGround ? TileType.HighGround : TileType.Walkable;
                    }

                    var tile = _gridManager.CreateTile(coord, type);

                    // Apply Visual Overrides
                    if (_mapData != null)
                    {
                        foreach (var visualOverride in _mapData.VisualOverrides)
                        {
                            if (visualOverride.Coordinate == coord)
                            {
                                tile.ApplyVisualOverride(visualOverride.Texture, visualOverride.Decorations);
                                break;
                            }
                        }
                    }
                }
            }

            if (_mapData != null && _mapData.UseManualLayout)
            {
                // Skip random lane generation, rely on manual layout.
                // But we still want to mark Spawn and Exit points from MapData.
                foreach (var spawn in _mapData.SpawnPoints) _gridManager.SetTileType(spawn, TileType.Spawn);
                foreach (var exit in _mapData.ExitPoints) _gridManager.SetTileType(exit, TileType.Exit);
            }
            else
            {
                GenerateLanes();
            }

            if (_generateWalls)
            {
                GenerateWalls();
            }
        }

        [Button("Generate From Map Data")]
        public void GenerateFromMapData()
        {
            if (_mapData != null) LoadMapData(_mapData);
            else Debug.LogWarning("No MapData assigned to GridGenerator.");
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

                if (_wallMaterial != null)
                {
                    var renderer = wall.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = _wallMaterial;
                        // Fix Texture Stretching
                        Vector3 worldScale = wall.transform.lossyScale;
                        // Use X or Z for horizontal tiling depending on wall orientation
                        float horizontalTiling = (scaleMultiplier.x > scaleMultiplier.z) ? worldScale.x : worldScale.z;
                        renderer.material.mainTextureScale = new Vector2(horizontalTiling, worldScale.y);
                    }
                }

                _generatedWalls.Add(wall);
            }

            if (_wallSouth)
            {
                Vector3 scale = new Vector3(1f, _wallHeight, _wallWidth);
                for (int x = -1; x <= _gridManager.Width; x++) CreateWallBlock(x, -1, scale);
            }
            
            if (_wallNorth)
            {
                Vector3 scale = new Vector3(1f, _wallHeight, _wallWidth);
                for (int x = -1; x <= _gridManager.Width; x++) CreateWallBlock(x, _gridManager.Height, scale);
            }

            if (_wallWest)
            {
                Vector3 scale = new Vector3(_wallWidth, _wallHeight, 1f);
                for (int y = 0; y < _gridManager.Height; y++) CreateWallBlock(-1, y, scale);
            }

            if (_wallEast)
            {
                Vector3 scale = new Vector3(_wallWidth, _wallHeight, 1f);
                for (int y = 0; y < _gridManager.Height; y++) CreateWallBlock(_gridManager.Width, y, scale);
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

            if (currentSpawns.Count == 0) currentSpawns.Add(new Vector2Int(0, _gridManager.Height / 2));
            if (currentExits.Count == 0) currentExits.Add(new Vector2Int(_gridManager.Width - 1, _gridManager.Height / 2));

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
                
                // Markers are now handled by Tile.cs UpdateTypeVisuals
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
                
                if (path.Count > _gridManager.Width * _gridManager.Height) break;
            }
            return path;
        }
        #endregion

        #region Interaction
        [Button("Clear Map")]
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

        public void LoadMapData(MaouSamaTD.Levels.MapData data)
        {
            if (data == null) return;
            
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.Undo.RecordObject(this, "Load Map Data");
#endif

            _seed = data.MapSeed;
            _highGroundChance = data.HighGroundChance;
            _spawnPoints = new List<Vector2Int>(data.SpawnPoints);
            _exitPoints = new List<Vector2Int>(data.ExitPoints);
            
            if (_gridManager != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) UnityEditor.Undo.RecordObject(_gridManager, "Load Map Data");
#endif
                _gridManager.Width = data.Width;
                _gridManager.Height = data.Height;
            }
            
            GenerateMap();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                if (_gridManager != null) UnityEditor.EditorUtility.SetDirty(_gridManager);
            }
#endif
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

        #region Tools
        [Button("Extract New Map Data")]
        public void ExtractMapData()
        {
#if UNITY_EDITOR
            if (_gridManager == null)
            {
                Debug.LogError("GridManager is missing!");
                return;
            }

            // Create Instance
            var newData = ScriptableObject.CreateInstance<MaouSamaTD.Levels.MapData>();
            
            // Copy Settings
            newData.Width = _gridManager.Width;
            newData.Height = _gridManager.Height;
            newData.MapSeed = _seed;
            newData.HighGroundChance = _highGroundChance;
            newData.SpawnPoints = new List<Vector2Int>(_spawnPoints);
            newData.ExitPoints = new List<Vector2Int>(_exitPoints);

            // Populate from current Grid if available
            foreach (var tile in _gridManager.GetAllTiles())
            {
                if (tile.Type == TileType.DecoratedWalkable) newData.DecoratedWalkable.Add(tile.Coordinate);
                else if (tile.Type == TileType.DecoratedHighGround) newData.DecoratedHighGround.Add(tile.Coordinate);

                if (tile.OverriddenTexture != null || (tile.OverriddenDecorations != null && tile.OverriddenDecorations.Count > 0))
                {
                    newData.VisualOverrides.Add(new TileVisualOverride
                    {
                        Coordinate = tile.Coordinate,
                        Texture = tile.OverriddenTexture,
                        Decorations = tile.OverriddenDecorations
                    });
                }
            }

            // Ensure Path
            string folderPath = _extractPath;
            if (folderPath.EndsWith("/")) folderPath = folderPath.Substring(0, folderPath.Length - 1);
            
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }

            string fullPath = $"{folderPath}/{_extractFileName}.asset";
            
            // Generate unique path if exists
            fullPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(fullPath);

            UnityEditor.AssetDatabase.CreateAsset(newData, fullPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log($"Successfully extracted MapData to: {fullPath}");
            
            // Ping it
            UnityEditor.EditorGUIUtility.PingObject(newData);
#endif
        }
        #endregion
    }
}
