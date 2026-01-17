using UnityEngine;


namespace MaouSamaTD.Grid
{
    public enum TileType
    {
        Walkable,   // Low ground, enemies walk here, melee units placed here
        HighGround, // Ranged units placed here, enemies cannot walk
        Spawn,      // Enemy spawn point
        Exit,       // Enemy target point
        Unwalkable  // Obstacle
    }

    public class Tile : MonoBehaviour
    {
        [Header("Grid Data")]
        [SerializeField] private Vector2Int _coordinate;
        [SerializeField] private TileType _type;

        [Header("State")]
        [SerializeField] private MaouSamaTD.Units.UnitBase _occupant;

        public Vector2Int Coordinate => _coordinate;
        public TileType Type => _type;
        public bool IsOccupied => _occupant != null;
        public MaouSamaTD.Units.UnitBase Occupant => _occupant;

        public void Initialize(Vector2Int coordinate, TileType type)
        {
            _coordinate = coordinate;
            _type = type;
            name = $"Tile_{coordinate.x}_{coordinate.y}";
        }

        public void SetOccupant(MaouSamaTD.Units.UnitBase unit)
        {
            _occupant = unit;
        }

        // Visuals
        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        public void SetHighlight(bool active, Color color)
        {
            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_propBlock);
            if (active)
            {
                _propBlock.SetColor(GlowColorId, color);
                _propBlock.SetFloat(GlowIntensityId, 1.5f); // Intensity
            }
            else
            {
                _propBlock.SetColor(GlowColorId, Color.black);
                _propBlock.SetFloat(GlowIntensityId, 0f);
            }
            _renderer.SetPropertyBlock(_propBlock);
        }

        // Visual debug to see tile type easily in Editor
        private void OnDrawGizmos()
        {
            Gizmos.color = GetTileColor();
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 0.1f, 1f));
        }

        private Color GetTileColor()
        {
            switch (_type)
            {
                case TileType.Walkable: return Color.white;
                case TileType.HighGround: return Color.cyan;
                case TileType.Spawn: return Color.red;
                case TileType.Exit: return Color.green;
                case TileType.Unwalkable: return Color.black;
                default: return Color.gray;
            }
        }

        // --- Editor Interaction ---

        private void OnValidate()
        {
            // Update visuals when Inspector changes
        }

        public void SetType(TileType newType)
        {
            if (_type == newType) return;

            // Handle cleanup of old type
            var generator = FindObjectOfType<GridGenerator>();
            if (generator != null)
            {
                 if (_type == TileType.Spawn && newType != TileType.Spawn) generator.RemoveSpawnPoint(_coordinate);
                 if (_type == TileType.Exit && newType != TileType.Exit) generator.RemoveExitPoint(_coordinate);
                 
                 // Handle new type addition
                 if (newType == TileType.Spawn) generator.AddSpawnPoint(_coordinate);
                 else if (newType == TileType.Exit) generator.AddExitPoint(_coordinate);
                 else 
                 {
                     // If just changing to Walkable/HighGround, generator doesn't track these lists explicitly
                     // but we should re-generate to update paths if we created an obstacle
                     // Use SetTileType on manager to ensure data consistency
                     var manager = FindObjectOfType<GridManager>();
                     
                     if (manager != null) manager.SetTileType(_coordinate, newType);
                 }
            }
            
            // Apply locally (GridManager.SetTileType usually calls Initialize which sets this, but let's be safe)
            _type = newType;
        }

        [ContextMenu("Set as Spawn Point")]
        private void SetAsSpawn()
        {
            SetType(TileType.Spawn);
        }

        [ContextMenu("Set as Exit Point")]
        private void SetAsExit()
        {
            SetType(TileType.Exit);
        }

        [ContextMenu("Set as Walkable")]
        private void SetAsWalkable()
        {
            SetType(TileType.Walkable);
        }

        [ContextMenu("Set as HighGround")]
        private void SetAsHighGround()
        {
            SetType(TileType.HighGround);
        }
    }
}
