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
        [Tooltip("Global scale for walls (X=Thick, Y=Height, Z=Length per block)")]
        [SerializeField] private Vector3 _wallScale = Vector3.one;

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
                        var manualTile = _mapData.ManualLayoutData.Find(t => t.Coordinate == coord);
                        if (manualTile.Coordinate == coord) // Found
                        {
                            // Convert Levels.TileType to Grid.TileType if they are still separate, 
                            // but I will unify them soon. For now, let's assume they map or I'll fix Tile.cs next.
                            type = (TileType)manualTile.Type; 
                        }
                    }
                    else
                    {
                        bool isHighGround = Random.value < _highGroundChance;
                        if (y == 0 || y == height - 1) isHighGround = true;
                        type = isHighGround ? TileType.HighGround : TileType.Walkable;
                    }

                    var tile = _gridManager.CreateTile(coord, type);
                    if (tile == null) continue;

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
                // Spawn and Exit points are now part of ManualLayoutData, 
                // but we might still have explicit SpawnPoints/ExitPoints lists for other logic?
                // Actually, let's check if we still need to set types based on the old lists.
                // The old code had:
                // foreach (var spawn in _mapData.SpawnPoints) _gridManager.SetTileType(spawn, TileType.Spawn);
                // foreach (var exit in _mapData.ExitPoints) _gridManager.SetTileType(exit, TileType.Exit);
                
                // If SpawnPoint/ExitPoint are in the ManualLayoutData, they are already set.
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
            Vector3 globalWallScale = _wallScale;
            GameObject wallPrefab = _wallPrefab;
            Material wallMaterial = _wallMaterial;
            bool wallNorth = _wallNorth;
            bool wallSouth = _wallSouth;
            bool wallEast = _wallEast;
            bool wallWest = _wallWest;

            Vector3 globalWallOffset = Vector3.zero;
            bool seamlessCorners = true;

            if (_mapData != null)
            {
                globalWallScale = _mapData.WallVisuals.WallScale;
                globalWallOffset = _mapData.WallVisuals.WallOffset;
                seamlessCorners = _mapData.WallVisuals.SeamlessCorners;
                wallPrefab = _mapData.WallVisuals.WallPrefab;
                wallMaterial = _mapData.WallVisuals.WallMaterial;
                wallNorth = _mapData.Walls.North;
                wallSouth = _mapData.Walls.South;
                wallEast = _mapData.Walls.East;
                wallWest = _mapData.Walls.West;
            }

            bool cascadeHoles = _mapData != null ? _mapData.WallCascadeOnHoles : true;

            float wallRealHeight = cellSize * globalWallScale.y;
            float yPos = wallRealHeight / 2f; 

            Transform wallContainer = _gridManager.WallContainer;
            if (wallContainer == null)
            {
                 _gridManager.Init();
                 wallContainer = _gridManager.WallContainer;
            }

            void CreateWallBlock(int x, int y, Vector3 scaleMultiplier, Vector3 additionalOffset, WallSide side, int index, Texture2D sideTexture)
            {
                float basePosX = x * cellSize;
                float basePosZ = y * cellSize;

                // Adjust position to keep inner face flush with grid when scale is changed.
                // If SeamlessCorners is true: 
                //    Shift wall inward by (1-thickness)*0.5 so it stays stuck to tiles (no extrude)
                //    but centers on grid lines for full 1.0 scale.
                if (seamlessCorners)
                {
                    float shiftX = (1f - scaleMultiplier.x) * cellSize * 0.5f;
                    float shiftZ = (1f - scaleMultiplier.z) * cellSize * 0.5f;

                    if (side == WallSide.North) basePosX = x * cellSize - shiftX;
                    else if (side == WallSide.South) basePosX = x * cellSize + shiftX;
                    else if (side == WallSide.West) basePosZ = y * cellSize - shiftZ;
                    else if (side == WallSide.East) basePosZ = y * cellSize + shiftZ;
                }
                else
                {
                    if (side == WallSide.North) basePosX = (x - 0.5f) * cellSize + (scaleMultiplier.x * cellSize * 0.5f);
                    else if (side == WallSide.South) basePosX = (x + 0.5f) * cellSize - (scaleMultiplier.x * cellSize * 0.5f);
                    else if (side == WallSide.West) basePosZ = (y - 0.5f) * cellSize + (scaleMultiplier.z * cellSize * 0.5f);
                    else if (side == WallSide.East) basePosZ = (y + 0.5f) * cellSize - (scaleMultiplier.z * cellSize * 0.5f);
                }

                Vector3 pos = new Vector3(basePosX, yPos, basePosZ) + globalWallOffset + additionalOffset;
                GameObject wall;
                
                if (wallPrefab != null)
                {
                    wall = Instantiate(wallPrefab, wallContainer);
                    wall.transform.position = pos;
                }
                else
                {
                    wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.transform.SetParent(wallContainer, false);
                    wall.transform.localPosition = pos;
                    wall.name = $"Wall_{side}_{index}";
                }

                wall.transform.localScale = Vector3.Scale(new Vector3(cellSize, cellSize, cellSize), scaleMultiplier);

                if (wallMaterial != null)
                {
                    var renderer = wall.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = wallMaterial;
                        // Fix Texture Stretching
                        Vector3 worldScale = wall.transform.lossyScale;
                        float horizontalTiling = (scaleMultiplier.x > scaleMultiplier.z) ? worldScale.x : worldScale.z;
                        renderer.material.mainTextureScale = new Vector2(horizontalTiling, worldScale.y);
                    }
                }

                // Apply Individual Wall Overrides
                if (_mapData != null)
                {
                    int wallOvIdx = _mapData.WallOverrides.FindIndex(o => o.Side == side && o.Index == index);
                    if (wallOvIdx != -1)
                    {
                        var wallOverride = _mapData.WallOverrides[wallOvIdx];
                        // Texture Override
                        if (wallOverride.TextureOverride != null)
                        {
                            var renderer = wall.GetComponentInChildren<Renderer>();
                            if (renderer != null) renderer.material.mainTexture = wallOverride.TextureOverride;
                        }
                        else if (sideTexture != null)
                        {
                            var renderer = wall.GetComponentInChildren<Renderer>();
                            if (renderer != null) renderer.material.mainTexture = sideTexture;
                        }

                        // Decoration Overrides
                        if (wallOverride.Decorations != null)
                        {
                            foreach (var deco in wallOverride.Decorations)
                            {
                                if (deco.Prefab == null) continue;
                                GameObject d = Instantiate(deco.Prefab, wall.transform);
                                d.transform.localPosition = deco.Offset;
                                d.transform.localRotation = Quaternion.Euler(deco.Rotation);
                                d.transform.localScale = deco.Scale;
                            }
                        }
                    }
                    else if (sideTexture != null)
                    {
                        var renderer = wall.GetComponentInChildren<Renderer>();
                        if (renderer != null) renderer.material.mainTexture = sideTexture;
                    }
                }

                _generatedWalls.Add(wall);
            }

            if (wallNorth)
            {
                // North = top in editor, at grid x=Width, runs along Y (Z world axis)
                Vector3 sideScale = globalWallScale;
                Vector3 sideOffset = Vector3.zero;
                Texture2D sideTexture = null;

                if (_mapData != null)
                {
                    var sOv = _mapData.SideVisualOverrides.Find(o => o.Side == WallSide.North);
                    if (sOv.OverrideScale) sideScale = sOv.Scale;
                    if (sOv.OverrideOffset) sideOffset = sOv.Offset;
                    sideTexture = sOv.TextureOverride;
                }

                for (int y = 0; y < _gridManager.Height; y++)
                {
                    if (!cascadeHoles)
                    {
                        var tile = _gridManager.GetTileAt(new Vector2Int(_gridManager.Width - 1, y));
                        if (tile != null && tile.Type == TileType.None) continue;
                    }

                    Vector3 finalScale = sideScale;
                    Vector3 finalOffset = sideOffset;

                    if (_mapData != null)
                    {
                        int idx = _mapData.WallOverrides.FindIndex(o => o.Side == WallSide.North && o.Index == y);
                        if (idx != -1)
                        {
                            var o = _mapData.WallOverrides[idx];
                            if (o.OverrideScale) finalScale = o.Scale;
                            if (o.OverrideOffset) finalOffset = o.Offset;
                        }
                    }
                    CreateWallBlock(_gridManager.Width, y, finalScale, finalOffset, WallSide.North, y, sideTexture);
                }
            }

            if (wallSouth)
            {
                // South = bottom in editor, at grid x=-1, runs along Y (Z world axis)
                Vector3 sideScale = globalWallScale;
                Vector3 sideOffset = Vector3.zero;
                Texture2D sideTexture = null;

                if (_mapData != null)
                {
                    var sOv = _mapData.SideVisualOverrides.Find(o => o.Side == WallSide.South);
                    if (sOv.OverrideScale) sideScale = sOv.Scale;
                    if (sOv.OverrideOffset) sideOffset = sOv.Offset;
                    sideTexture = sOv.TextureOverride;
                }

                for (int y = 0; y < _gridManager.Height; y++)
                {
                    if (!cascadeHoles)
                    {
                        var tile = _gridManager.GetTileAt(new Vector2Int(0, y));
                        if (tile != null && tile.Type == TileType.None) continue;
                    }

                    Vector3 finalScale = sideScale;
                    Vector3 finalOffset = sideOffset;

                    if (_mapData != null)
                    {
                        int idx = _mapData.WallOverrides.FindIndex(o => o.Side == WallSide.South && o.Index == y);
                        if (idx != -1)
                        {
                            var o = _mapData.WallOverrides[idx];
                            if (o.OverrideScale) finalScale = o.Scale;
                            if (o.OverrideOffset) finalOffset = o.Offset;
                        }
                    }
                    CreateWallBlock(-1, y, finalScale, finalOffset, WallSide.South, y, sideTexture);
                }
            }

            if (wallWest)
            {
                // West = left in editor, at grid y=Height, runs along X
                Vector3 sideScale = new Vector3(globalWallScale.z, globalWallScale.y, globalWallScale.x);
                Vector3 sideOffset = Vector3.zero;
                Texture2D sideTexture = null;

                if (_mapData != null)
                {
                    var sOv = _mapData.SideVisualOverrides.Find(o => o.Side == WallSide.West);
                    if (sOv.OverrideScale) sideScale = sOv.Scale;
                    if (sOv.OverrideOffset) sideOffset = sOv.Offset;
                    sideTexture = sOv.TextureOverride;
                }

                for (int x = -1; x <= _gridManager.Width; x++)
                {
                    if (!cascadeHoles)
                    {
                        int adjX = Mathf.Clamp(x, 0, _gridManager.Width - 1);
                        var tile = _gridManager.GetTileAt(new Vector2Int(adjX, _gridManager.Height - 1));
                        if (tile != null && tile.Type == TileType.None) continue;
                    }

                    Vector3 finalScale = sideScale;
                    Vector3 finalOffset = sideOffset;

                    // Seamless Corners: Shorten the corner-most segments to match thickness
                    if (seamlessCorners)
                    {
                        if (x == -1 || x == _gridManager.Width)
                        {
                            finalScale.x = globalWallScale.x; // Make it square (length = thickness)
                            
                            // Align corner block to the outward/inward edge of the wall strip
                            float cornerShift = (1f - globalWallScale.x) * cellSize * 0.5f;
                            if (x == -1) finalOffset.x += cornerShift; // Northwest corner
                            else finalOffset.x -= cornerShift;        // Northeast corner
                        }
                    }

                    if (_mapData != null)
                    {
                        int idx = _mapData.WallOverrides.FindIndex(o => o.Side == WallSide.West && o.Index == x);
                        if (idx != -1)
                        {
                            var o = _mapData.WallOverrides[idx];
                            if (o.OverrideScale) finalScale = o.Scale;
                            if (o.OverrideOffset) finalOffset = o.Offset;
                        }
                    }
                    CreateWallBlock(x, _gridManager.Height, finalScale, finalOffset, WallSide.West, x, sideTexture);
                }
            }

            if (wallEast)
            {
                // East = right in editor, at grid y=-1, runs along X
                Vector3 sideScale = new Vector3(globalWallScale.z, globalWallScale.y, globalWallScale.x);
                Vector3 sideOffset = Vector3.zero;
                Texture2D sideTexture = null;

                if (_mapData != null)
                {
                    var sOv = _mapData.SideVisualOverrides.Find(o => o.Side == WallSide.East);
                    if (sOv.OverrideScale) sideScale = sOv.Scale;
                    if (sOv.OverrideOffset) sideOffset = sOv.Offset;
                    sideTexture = sOv.TextureOverride;
                }

                for (int x = -1; x <= _gridManager.Width; x++)
                {
                    if (!cascadeHoles)
                    {
                        int adjX = Mathf.Clamp(x, 0, _gridManager.Width - 1);
                        var tile = _gridManager.GetTileAt(new Vector2Int(adjX, 0));
                        if (tile != null && tile.Type == TileType.None) continue;
                    }

                    Vector3 finalScale = sideScale;
                    Vector3 finalOffset = sideOffset;

                    // Seamless Corners: Shorten the corner-most segments to match thickness
                    if (seamlessCorners)
                    {
                        if (x == -1 || x == _gridManager.Width)
                        {
                            finalScale.x = globalWallScale.x; // Make it square (length = thickness)

                            // Align corner block to the outward/inward edge of the wall strip
                            float cornerShift = (1f - globalWallScale.x) * cellSize * 0.5f;
                            if (x == -1) finalOffset.x += cornerShift; // Southwest corner
                            else finalOffset.x -= cornerShift;        // Southeast corner
                        }
                    }

                    if (_mapData != null)
                    {
                        int idx = _mapData.WallOverrides.FindIndex(o => o.Side == WallSide.East && o.Index == x);
                        if (idx != -1)
                        {
                            var o = _mapData.WallOverrides[idx];
                            if (o.OverrideScale) finalScale = o.Scale;
                            if (o.OverrideOffset) finalOffset = o.Offset;
                        }
                    }
                    CreateWallBlock(x, -1, finalScale, finalOffset, WallSide.East, x, sideTexture);
                }
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

                _gridManager.SetTileType(start, TileType.SpawnPoint);
                _gridManager.SetTileType(closestExit, TileType.ExitPoint);
                
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
                _gridManager.SetTileType(coord, TileType.SpawnPoint);
                Debug.Log($"Added Spawn Point at {coord}");
                GenerateMap();
            }
        }

        public void AddExitPoint(Vector2Int coord)
        {
            if (!_exitPoints.Contains(coord))
            {
                _exitPoints.Add(coord);
                _gridManager.SetTileType(coord, TileType.ExitPoint);
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

            // Sync Wall Settings
            _wallNorth = data.Walls.North;
            _wallSouth = data.Walls.South;
            _wallEast = data.Walls.East;
            _wallWest = data.Walls.West;
            _wallScale = data.WallVisuals.WallScale;
            // Note: _wallOffset and _seamlessCorners are not yet serializable fields in GridGenerator 
            // but we use them locally in GenerateWalls.
            // For now, they come directly from MapData.
            
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
            
            // Wall Settings
            newData.Walls = new WallSettings {
                North = _wallNorth, South = _wallSouth, East = _wallEast, West = _wallWest
            };
            newData.WallVisuals = new WallVisualSettings {
                WallMaterial = _wallMaterial, WallPrefab = _wallPrefab,
                WallScale = _wallScale,
                WallOffset = Vector3.zero, // Default when extracted
                SeamlessCorners = true     // Default when extracted
            };

            // Populate from current Grid if available
            foreach (var tile in _gridManager.GetAllTiles())
            {
                // Unify all layouts into ManualLayoutData
                newData.ManualLayoutData.Add(new TileLayoutData
                {
                    Coordinate = tile.Coordinate,
                    Type = (MaouSamaTD.Levels.TileType)tile.Type
                });

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
