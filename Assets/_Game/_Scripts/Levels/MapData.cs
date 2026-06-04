using System.Collections.Generic;
using UnityEngine;

namespace MaouSamaTD.Levels
{
    public enum TileType 
    { 
        None = 0, 
        Walkable = 1, 
        HighGround = 2, 
        DecoHighGround = 3, 
        SpawnPoint = 4, 
        ExitPoint = 5,
        LowTile = 6,
        NonWalkableDecor = 7,
        Wall = 8,
        SpawnPointHigh = 9,
        ExitPointHigh = 10
    }

    [System.Serializable]
    public struct TileLayoutData
    {
        public Vector2Int Coordinate;
        public TileType Type;
    }

    [System.Serializable]
    public struct SpawnPointData
    {
        public Vector2Int Coordinate;
        public int TargetExitIndex; // -1 for "Any/First"
    }

    [CreateAssetMenu(fileName = "NewMapData", menuName = "MaouSamaTD/Map Data")]
    public class MapData : MaouSamaTD.Core.GameDataSO
    {
        [Header("Map Settings")]
        [Tooltip("Seed for procedural generation")]
        public int MapSeed = 12345;
        
        [Tooltip("Grid Width Override (0 to use GridManager default)")]
        public int Width = 10;
        [Tooltip("Grid Height Override (0 to use GridManager default)")]
        public int Height = 5;

        [Range(0f, 1f)]
        public float HighGroundChance = 0.3f;

        [Header("Camera Settings")]
        [Tooltip("If true, automatically calculates the camera's default zoom size based on grid dimensions.")]
        public bool AutoCalculateDefaultZoom = true;

        [Tooltip("Custom default zoom size used if AutoCalculateDefaultZoom is false.")]
        public float CustomDefaultZoom = 4.15f;

        [Header("Manual Layout")]
        public bool UseManualLayout;
        public List<TileLayoutData> ManualLayoutData = new List<TileLayoutData>();

        [Header("Legacy/Special Points")]
        public List<SpawnPointData> SpawnPoints = new List<SpawnPointData>();
        public List<Vector2Int> ExitPoints = new List<Vector2Int>();

        [Header("Visuals")]
        public bool ShowPathing = false;
        public Material DefaultTileMaterial;
        public Texture2D DefaultTileTexture;
        [Header("Tile Wall Configuration (TileType.Wall)")]
        public WallVisualSettings TileWallVisuals = WallVisualSettings.Default;
        public List<TileVisualOverride> VisualOverrides = new List<TileVisualOverride>();

        [Header("Global Wall Settings")]
        public bool WallCascadeOnHoles = false;
        public WallSettings Walls = WallSettings.Default;
        public WallVisualSettings WallVisuals = WallVisualSettings.Default;
        public List<WallVisualOverride> WallOverrides = new List<WallVisualOverride>();
        public List<SideVisualOverride> SideVisualOverrides = new List<SideVisualOverride>();

        [Header("Environment & Void")]
        public EnvironmentSettings Environment = EnvironmentSettings.Default;

        [Header("Lighting")]
        public LightingSettings Lighting = LightingSettings.Default;
    }

    public enum CameraBackgroundMode
    {
        Skybox,
        SolidColor
    }

    [System.Serializable]
    public struct EnvironmentSettings
    {
        [Tooltip("If true, spawns the GlobalBackgroundPrefab below the grid")]
        public bool UseGlobalBackground;

        [Tooltip("Full background spawned below the grid (e.g. giant void quad or particle system)")]
        public GameObject GlobalBackgroundPrefab;

        [Tooltip("Height offset for the global background relative to the grid center.")]
        public float GlobalBackgroundHeightOffset;

        [Tooltip("If true, spawns TileFogPrefab in every grid coordinate that has TileType == None")]
        public bool FillVoidWithFog;

        [Tooltip("Prefab spawned on individual empty tiles")]
        public GameObject TileFogPrefab;

        [Tooltip("Height offset for the tile fog relative to the grid center.")]
        public float TileFogHeightOffset;

        [Header("Camera Background")]
        [Tooltip("Configure if the camera background is a Skybox or a Solid Color")]
        public CameraBackgroundMode CameraBackground;

        [Tooltip("The solid color used when CameraBackground is set to SolidColor")]
        public Color CameraBackgroundColor;

        [Tooltip("The skybox material used when CameraBackground is set to Skybox")]
        public Material SkyboxMaterial;

        public static EnvironmentSettings Default => new EnvironmentSettings
        {
            UseGlobalBackground = false,
            GlobalBackgroundHeightOffset = -1f,
            FillVoidWithFog = false,
            TileFogHeightOffset = -0.5f,
            CameraBackground = CameraBackgroundMode.Skybox,
            CameraBackgroundColor = new Color(0.05f, 0.05f, 0.05f),
            SkyboxMaterial = null
        };
    }

    [System.Serializable]
    public struct LightingSettings
    {
        public bool OverrideLighting;
        public Color AmbientColor;
        [Tooltip("Optional directional light prefab to spawn for this map (contains shadows/angle)")]
        public Light DirectionalLightPrefab;

        [Header("Directional Lights")]
        public bool EnableTopLight;
        public float TopLightIntensity;

        public bool EnableNorthIsometric;
        public float NorthIsometricIntensity;

        public bool EnableSouthIsometric;
        public float SouthIsometricIntensity;

        public bool EnableEastIsometric;
        public float EastIsometricIntensity;

        public bool EnableWestIsometric;
        public float WestIsometricIntensity;

        public static LightingSettings Default => new LightingSettings
        {
            OverrideLighting = false,
            AmbientColor = new Color(0.2f, 0.2f, 0.2f),
            EnableTopLight = false,
            TopLightIntensity = 1.0f,
            EnableNorthIsometric = false,
            NorthIsometricIntensity = 1.0f,
            EnableSouthIsometric = false,
            SouthIsometricIntensity = 1.0f,
            EnableEastIsometric = false,
            EastIsometricIntensity = 1.0f,
            EnableWestIsometric = false,
            WestIsometricIntensity = 1.0f
        };
    }

    public enum WallSide { North, South, East, West, NorthWest, NorthEast, SouthWest, SouthEast }

    [System.Serializable]
    public struct WallVisualOverride
    {
        public WallSide Side;
        public int Index;
        public Texture2D TextureOverride;
        public bool OverrideScale;
        public Vector3 Scale;
        public bool OverrideOffset;
        public Vector3 Offset;
        public List<DecorationData> Decorations;
    }

    [System.Serializable]
    public struct SideVisualOverride
    {
        public WallSide Side;
        public Texture2D TextureOverride;
        public bool OverrideScale;
        public Vector3 Scale;
        public bool OverrideOffset;
        public Vector3 Offset;
    }

    [System.Serializable]
    public struct WallSettings
    {
        public bool North;
        public bool South;
        public bool East;
        public bool West;
        public bool NW;
        public bool NE;
        public bool SW;
        public bool SE;

        public static WallSettings Default => new WallSettings 
        { 
            North = true, South = true, East = true, West = true,
            NW = true, NE = true, SW = true, SE = true
        };
    }

    [System.Serializable]
    public struct WallVisualSettings
    {
        public GameObject WallPrefab;
        public Material WallMaterial;
        public Vector3 WallScale;
        public Vector3 WallOffset;
        public bool SeamlessCorners;

        public static WallVisualSettings Default => new WallVisualSettings 
        { 
            WallScale = new Vector3(1.0f, 1.0f, 1.0f),
            WallOffset = Vector3.zero,
            SeamlessCorners = true
        };
    }

    [System.Serializable]
    public struct TileVisualOverride
    {
        public Vector2Int Coordinate;
        public Texture2D Texture;
        public List<DecorationData> Decorations;
    }

    [System.Serializable]
    public struct DecorationData
    {
        public GameObject Prefab;
        public Vector3 Offset;
        public Vector3 Rotation;
        public Vector3 Scale;

        public static DecorationData Default => new DecorationData 
        { 
            Scale = Vector3.one 
        };
    }
}
