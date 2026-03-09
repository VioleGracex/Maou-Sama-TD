using System.Collections.Generic;
using UnityEngine;

namespace MaouSamaTD.Levels
{
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
        public List<Vector2Int> ManualHighGround = new List<Vector2Int>();
        public List<Vector2Int> DecoratedWalkable = new List<Vector2Int>();
        public List<Vector2Int> DecoratedHighGround = new List<Vector2Int>();

        public List<Vector2Int> SpawnPoints = new List<Vector2Int>();
        public List<Vector2Int> ExitPoints = new List<Vector2Int>();
        
        [Header("Visuals")]
        public List<TileVisualOverride> VisualOverrides = new List<TileVisualOverride>();
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
