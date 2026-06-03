using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using MaouSamaTD.Levels;


namespace MaouSamaTD.Grid
{
    public class GridGenerator : MonoBehaviour
    {
        #region Settings
        [Header("Editor View")]
        [SerializeField] private bool _showMapDataSettings = false;
        [SerializeField] private bool _showProceduralSettings = false;

        [Header("Target")]
        [SerializeField] private GridManager _gridManager;

        [ShowIf("_showMapDataSettings")]
        [Header("Map Data Input/Output")]
        [SerializeField] private MapData _mapData;
        [ShowIf("_showMapDataSettings")]
        [SerializeField] private string _extractPath = "Assets/_Game/Data/Maps/";
        [ShowIf("_showMapDataSettings")]
        [SerializeField] private string _extractFileName = "NewMapData";

        // Dimensions are now taken from GridManager to avoid duplication

        [ShowIf("_showProceduralSettings")]
        [Header("Procedural Settings")]
        [SerializeField] private bool _useSeed = true;
        [ShowIf("_showProceduralSettings")]
        [SerializeField] private int _seed = 12345;
        [ShowIf("_showProceduralSettings")]
        [Range(0f, 1f)] [SerializeField] private float _highGroundChance = 0.3f;

        [ShowIf("_showProceduralSettings")]
        [Header("Lanes")]
        [Min(1)] [SerializeField] private int _lanesPerConnection = 1;
        [ShowIf("_showProceduralSettings")]
        [Tooltip("If empty, default logic will be used (Left -> Right)")]
        [SerializeField] private List<SpawnPointData> _spawnPoints = new List<SpawnPointData>();
        [ShowIf("_showProceduralSettings")]
        [SerializeField] private List<Vector2Int> _exitPoints = new List<Vector2Int>();

        [ShowIf("_showProceduralSettings")]
        [Header("Visuals")]
        [SerializeField] private GameObject _startPrefab;
        [ShowIf("_showProceduralSettings")]
        [SerializeField] private GameObject _endPrefab;
        [ShowIf("_showProceduralSettings")]
        [SerializeField] private GameObject _wallPrefab;
        [ShowIf("_showProceduralSettings")]
        [SerializeField] private Material _wallMaterial; // Override Material

        [ShowIf("_showProceduralSettings")]
        [Header("Generation Settings")]
        [SerializeField] private bool _generateMapOnStart = true;
        [ShowIf("_showProceduralSettings")]
        [SerializeField] private bool _generateWalls = true;
        
        [ShowIf("_showProceduralSettings")]
        [Header("Wall Configuration")]
        [SerializeField] private bool _wallNorth = true;
        [ShowIf("_showProceduralSettings")]
        [SerializeField] private bool _wallSouth = true;
        [ShowIf("_showProceduralSettings")]
        [SerializeField] private bool _wallEast = true;
        [ShowIf("_showProceduralSettings")]
        [SerializeField] private bool _wallWest = true;
        [ShowIf("_showProceduralSettings")]
        [SerializeField] private bool _wallNW = true;
        [ShowIf("_showProceduralSettings")]
        [SerializeField] private bool _wallNE = true;
        [ShowIf("_showProceduralSettings")]
        [SerializeField] private bool _wallSW = true;
        [ShowIf("_showProceduralSettings")]
        [SerializeField] private bool _wallSE = true;
        
        [ShowIf("_showProceduralSettings")]
        [Header("Primitive Wall Settings")]
        [Tooltip("Global scale for walls (X=Thick, Y=Height, Z=Length per block)")]
        [SerializeField] private Vector3 _wallScale = Vector3.one;

        private List<GameObject> _generatedWalls = new List<GameObject>();
        private List<GameObject> _generatedEnvironmentObjects = new List<GameObject>();
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
        [ShowIf("_showProceduralSettings")]
        [Button("Generate Procedural Map")]
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
            ClearEnvironment();

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
                    
                    // Apply Defaults First
                    if (_mapData != null)
                    {
                        if (_mapData.DefaultTileMaterial != null) tile.SetMaterial(_mapData.DefaultTileMaterial);
                        if (_mapData.DefaultTileTexture != null) tile.ApplyVisualOverride(_mapData.DefaultTileTexture, null);
                    }

                    // If manually setting types, ensure they are in the lists if they are spawn/exit
                    if (type == TileType.SpawnPoint || type == TileType.SpawnPointHigh)
                    {
                        if (!_spawnPoints.Exists(s => s.Coordinate == coord))
                            _spawnPoints.Add(new SpawnPointData { Coordinate = coord, TargetExitIndex = -1 });
                    }
                    if (type == TileType.ExitPoint || type == TileType.ExitPointHigh)
                    {
                        if (!_exitPoints.Contains(coord))
                            _exitPoints.Add(coord);
                    }

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

            // Sync Spawn Mappings
            foreach (var spawn in _spawnPoints)
            {
                _gridManager.SetSpawnMapping(spawn.Coordinate, spawn.TargetExitIndex);
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

            GenerateEnvironment();
            GenerateLighting();
        }

        [ShowIf("_showMapDataSettings")]
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
                _wallNW = _mapData.Walls.NW;
                _wallNE = _mapData.Walls.NE;
                _wallSW = _mapData.Walls.SW;
                _wallSE = _mapData.Walls.SE;
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

            if (wallEast)
            {
                // East = Right side, at grid x=Width, runs along Y (Z world axis)
                Vector3 sideScale = globalWallScale;
                Vector3 sideOffset = Vector3.zero;
                Texture2D sideTexture = null;

                if (_mapData != null)
                {
                    var sOv = _mapData.SideVisualOverrides.Find(o => o.Side == WallSide.East);
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
                        int idx = _mapData.WallOverrides.FindIndex(o => o.Side == WallSide.East && o.Index == y);
                        if (idx != -1)
                        {
                            var o = _mapData.WallOverrides[idx];
                            if (o.OverrideScale) finalScale = o.Scale;
                            if (o.OverrideOffset) finalOffset = o.Offset;
                        }
                    }
                    CreateWallBlock(_gridManager.Width, y, finalScale, finalOffset, WallSide.East, y, sideTexture);
                }
            }

            if (wallWest)
            {
                // West = Left side, at grid x=-1, runs along Y (Z world axis)
                Vector3 sideScale = globalWallScale;
                Vector3 sideOffset = Vector3.zero;
                Texture2D sideTexture = null;

                if (_mapData != null)
                {
                    var sOv = _mapData.SideVisualOverrides.Find(o => o.Side == WallSide.West);
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
                        int idx = _mapData.WallOverrides.FindIndex(o => o.Side == WallSide.West && o.Index == y);
                        if (idx != -1)
                        {
                            var o = _mapData.WallOverrides[idx];
                            if (o.OverrideScale) finalScale = o.Scale;
                            if (o.OverrideOffset) finalOffset = o.Offset;
                        }
                    }
                    CreateWallBlock(-1, y, finalScale, finalOffset, WallSide.West, y, sideTexture);
                }
            }

            if (wallNorth)
            {
                // North = Forward side, at grid y=Height, runs along X
                Vector3 sideScale = new Vector3(globalWallScale.z, globalWallScale.y, globalWallScale.x);
                Vector3 sideOffset = Vector3.zero;
                Texture2D sideTexture = null;

                if (_mapData != null)
                {
                    var sOv = _mapData.SideVisualOverrides.Find(o => o.Side == WallSide.North);
                    if (sOv.OverrideScale) sideScale = sOv.Scale;
                    if (sOv.OverrideOffset) sideOffset = sOv.Offset;
                    sideTexture = sOv.TextureOverride;
                }

                for (int x = 0; x < _gridManager.Width; x++)
                {
                    if (!cascadeHoles)
                    {
                        var tile = _gridManager.GetTileAt(new Vector2Int(x, _gridManager.Height - 1));
                        if (tile != null && tile.Type == TileType.None) continue;
                    }

                    Vector3 finalScale = sideScale;
                    Vector3 finalOffset = sideOffset;

                    if (_mapData != null)
                    {
                        int idx = _mapData.WallOverrides.FindIndex(o => o.Side == WallSide.North && o.Index == x);
                        if (idx != -1)
                        {
                            var o = _mapData.WallOverrides[idx];
                            if (o.OverrideScale) finalScale = o.Scale;
                            if (o.OverrideOffset) finalOffset = o.Offset;
                        }
                    }
                    CreateWallBlock(x, _gridManager.Height, finalScale, finalOffset, WallSide.North, x, sideTexture);
                }
            }

            // Corners
            void CreateCorner(int x, int y, WallSide side, bool enabled)
            {
                if (!enabled) return;
                Vector3 finalScale = globalWallScale;
                Vector3 finalOffset = Vector3.zero;
                Texture2D sideTexture = null;

                if (_mapData != null)
                {
                    var sOv = _mapData.SideVisualOverrides.Find(o => o.Side == side);
                    if (sOv.OverrideScale) finalScale = sOv.Scale;
                    if (sOv.OverrideOffset) finalOffset = sOv.Offset;
                    sideTexture = sOv.TextureOverride;

                    int idx = _mapData.WallOverrides.FindIndex(o => o.Side == side && o.Index == 0);
                    if (idx != -1)
                    {
                        var o = _mapData.WallOverrides[idx];
                        if (o.OverrideScale) finalScale = o.Scale;
                        if (o.OverrideOffset) finalOffset = o.Offset;
                    }
                }
                CreateWallBlock(x, y, finalScale, finalOffset, side, 0, sideTexture);
            }

            CreateCorner(-1, _gridManager.Height, WallSide.NorthWest, _wallNW);
            CreateCorner(_gridManager.Width, _gridManager.Height, WallSide.NorthEast, _wallNE);
            CreateCorner(-1, -1, WallSide.SouthWest, _wallSW);
            CreateCorner(_gridManager.Width, -1, WallSide.SouthEast, _wallSE);

            if (wallSouth)
            {
                // South = Back side, at grid y=-1, runs along X
                Vector3 sideScale = new Vector3(globalWallScale.z, globalWallScale.y, globalWallScale.x);
                Vector3 sideOffset = Vector3.zero;
                Texture2D sideTexture = null;

                if (_mapData != null)
                {
                    var sOv = _mapData.SideVisualOverrides.Find(o => o.Side == WallSide.South);
                    if (sOv.OverrideScale) sideScale = sOv.Scale;
                    if (sOv.OverrideOffset) sideOffset = sOv.Offset;
                    sideTexture = sOv.TextureOverride;
                }

                for (int x = 0; x < _gridManager.Width; x++)
                {
                    if (!cascadeHoles)
                    {
                        var tile = _gridManager.GetTileAt(new Vector2Int(x, 0));
                        if (tile != null && tile.Type == TileType.None) continue;
                    }

                    Vector3 finalScale = sideScale;
                    Vector3 finalOffset = sideOffset;

                    if (_mapData != null)
                    {
                        int idx = _mapData.WallOverrides.FindIndex(o => o.Side == WallSide.South && o.Index == x);
                        if (idx != -1)
                        {
                            var o = _mapData.WallOverrides[idx];
                            if (o.OverrideScale) finalScale = o.Scale;
                            if (o.OverrideOffset) finalOffset = o.Offset;
                        }
                    }
                    CreateWallBlock(x, -1, finalScale, finalOffset, WallSide.South, x, sideTexture);
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

        private void GenerateEnvironment()
        {
            if (_mapData == null) return;
            
            var env = _mapData.Environment;
            
            // 1. Global Background
            if (env.UseGlobalBackground && env.GlobalBackgroundPrefab != null)
            {
                Vector3 center = _gridManager.GetGridCenter();
                center.y += env.GlobalBackgroundHeightOffset; // Dynamic height offset
                GameObject bg = Instantiate(env.GlobalBackgroundPrefab, center, env.GlobalBackgroundPrefab.transform.rotation, _gridManager.transform);
                bg.name = "GlobalBackground_Fog";
                _generatedEnvironmentObjects.Add(bg);
            }

            // 2. Tile Fog for Voids (Spawns flat horizontal quads on each TileType.None coordinate)
            if (env.FillVoidWithFog && env.TileFogPrefab != null)
            {
                float cellSize = _gridManager.CellSize;
                int width = _gridManager.Width;
                int height = _gridManager.Height;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Vector2Int coord = new Vector2Int(x, y);
                        // A coordinate is empty/void if there is no tile component instantiated on it
                        bool isNone = (_gridManager.GetTileAt(coord) == null);

                        if (isNone)
                        {
                            Vector3 pos = new Vector3(x * cellSize, env.TileFogHeightOffset, y * cellSize);
                            // Preserve the prefab's flat rotation (90 degrees around X)
                            Transform parentT = _gridManager.GridContainer != null ? _gridManager.GridContainer : _gridManager.transform;
                            GameObject fogObj = Instantiate(env.TileFogPrefab, pos, env.TileFogPrefab.transform.rotation, parentT);
                            fogObj.name = $"TileFog_{x}_{y}";
                            fogObj.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
                            _generatedEnvironmentObjects.Add(fogObj);
                        }
                    }
                }
            }

            // 3. Camera Background Settings
            ApplyCameraSettings(env);
        }

        private void ApplyCameraSettings(EnvironmentSettings env)
        {
#if UNITY_2023_1_OR_NEWER
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
#else
            Camera[] cameras = FindObjectsOfType<Camera>();
#endif
            if (env.CameraBackground == CameraBackgroundMode.Skybox)
            {
                RenderSettings.skybox = env.SkyboxMaterial;
            }

            foreach (var cam in cameras)
            {
                if (cam.cameraType == CameraType.Game)
                {
                    if (env.CameraBackground == CameraBackgroundMode.SolidColor)
                    {
                        cam.clearFlags = CameraClearFlags.SolidColor;
                        cam.backgroundColor = env.CameraBackgroundColor;
                    }
                    else
                    {
                        cam.clearFlags = CameraClearFlags.Skybox;
                    }
                }
            }
        }

        private void GenerateLighting()
        {
            if (_mapData == null) return;

            var lighting = _mapData.Lighting;
            if (lighting.OverrideLighting)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = lighting.AmbientColor;
            }

            if (lighting.DirectionalLightPrefab != null)
            {
                GameObject lightObj = Instantiate(lighting.DirectionalLightPrefab.gameObject, Vector3.zero, Quaternion.identity, _gridManager.transform);
                lightObj.name = "Level_DirectionalLight";
                _generatedEnvironmentObjects.Add(lightObj); // Tracked same as environment for cleanup
            }
        }

        private void ClearEnvironment()
        {
            foreach (var obj in _generatedEnvironmentObjects)
            {
                if (obj != null)
                {
                    if (Application.isPlaying) Destroy(obj);
                    else DestroyImmediate(obj);
                }
            }
            _generatedEnvironmentObjects.Clear();
        }
        #endregion

        #region Lanes & Pathing
        private void GenerateLanes()
        {
            List<SpawnPointData> currentSpawns = new List<SpawnPointData>(_spawnPoints);
            List<Vector2Int> currentExits = new List<Vector2Int>(_exitPoints);

            if (currentSpawns.Count == 0) currentSpawns.Add(new SpawnPointData { Coordinate = new Vector2Int(0, _gridManager.Height / 2), TargetExitIndex = -1 });
            if (currentExits.Count == 0) currentExits.Add(new Vector2Int(_gridManager.Width - 1, _gridManager.Height / 2));

            foreach (var startData in currentSpawns)
            {
                Vector2Int start = startData.Coordinate;
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
             ClearWalls();
             ClearEnvironment();
         }

        public void AddSpawnPoint(Vector2Int coord)
        {
            if (!_spawnPoints.Exists(s => s.Coordinate == coord))
            {
                _spawnPoints.Add(new SpawnPointData { Coordinate = coord, TargetExitIndex = -1 });
                // We keep the logic as SpawnPoint for auto-gen, or let it be whatever it was.
                // But if someone calls this, it usually forces the type.
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
            if (_spawnPoints.Exists(s => s.Coordinate == coord))
            {
                _spawnPoints.RemoveAll(s => s.Coordinate == coord);
                Debug.Log($"Removed Spawn Point at {coord}");
                GenerateMap();
            }
        }

        public void LoadMapData(MaouSamaTD.Levels.MapData data)
        {
            if (data == null) return;
            
            Debug.Log($"[GridGenerator] LoadMapData called with asset: {data.name} ({data.Width}x{data.Height})");

#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.Undo.RecordObject(this, "Load Map Data");
#endif

            _seed = data.MapSeed;
            _highGroundChance = data.HighGroundChance;
            _spawnPoints = new List<SpawnPointData>(data.SpawnPoints);
            _exitPoints = new List<Vector2Int>(data.ExitPoints);

            _mapData = data; // Ensure GenerateMap uses the newly loaded data

            // Sync Wall Settings
            _wallNorth = data.Walls.North;
            _wallSouth = data.Walls.South;
            _wallEast = data.Walls.East;
            _wallWest = data.Walls.West;
            _wallNW = data.Walls.NW;
            _wallNE = data.Walls.NE;
            _wallSW = data.Walls.SW;
            _wallSE = data.Walls.SE;
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

            if (_gridManager != null)
            {
                _gridManager.RecalculateBounds();
            }

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
        [ShowIf("_showMapDataSettings")]
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
            newData.SpawnPoints = new List<SpawnPointData>(_spawnPoints);
            newData.ExitPoints = new List<Vector2Int>(_exitPoints);
            
            // Wall Settings
            newData.Walls = new WallSettings {
                North = _wallNorth, South = _wallSouth, East = _wallEast, West = _wallWest,
                NW = _wallNW, NE = _wallNE, SW = _wallSW, SE = _wallSE
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
