using System.Collections.Generic;
using UnityEngine;

namespace MaouSamaTD.Levels
{
    public enum TileType 
    { 
        None, 
        Walkable, 
        HighGround, 
        DecoWalkable, 
        DecoHighGround, 
        SpawnPoint, 
        ExitPoint,
        Hole,
        LowTile,
        NonWalkableDecor
    }

    [System.Serializable]
    public struct TileLayoutData
    {
        public Vector2Int Coordinate;
        public TileType Type;
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

        [Header("Manual Layout")]
        public bool UseManualLayout;
        public List<TileLayoutData> ManualLayoutData = new List<TileLayoutData>();

        [Header("Legacy/Special Points")]
        public List<Vector2Int> SpawnPoints = new List<Vector2Int>();
        public List<Vector2Int> ExitPoints = new List<Vector2Int>();

        [Header("Visuals")]
        public List<TileVisualOverride> VisualOverrides = new List<TileVisualOverride>();

        [Header("Global Wall Settings")]
        public bool WallCascadeOnHoles = true;
        public WallSettings Walls = WallSettings.Default;
        public WallVisualSettings WallVisuals = WallVisualSettings.Default;
        public List<WallVisualOverride> WallOverrides = new List<WallVisualOverride>();
        public List<SideVisualOverride> SideVisualOverrides = new List<SideVisualOverride>();
    }

    public enum WallSide { North, South, East, West }

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

        public static WallSettings Default => new WallSettings 
        { 
            North = true, South = true, East = true, West = true 
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
