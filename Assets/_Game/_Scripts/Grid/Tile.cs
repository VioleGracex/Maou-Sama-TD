using NaughtyAttributes;
using UnityEngine;
using Zenject;

namespace MaouSamaTD.Grid
{
    public enum TileType
    {
        Walkable,   // Low ground, enemies walk here, melee units placed here
        HighGround, // Ranged units placed here, enemies cannot walk
        Spawn,              // Enemy spawn point
        Exit,               // Enemy target point
        Unwalkable,         // Obstacle
        DecoratedWalkable,  // Flat but unusable
        DecoratedHighGround // High ground but unusable
    }

    public class Tile : MonoBehaviour
    {
        [Header("Grid Data")]
        [SerializeField] private Vector2Int _coordinate;
        [SerializeField] private TileType _type;

        [Header("State")]
        [SerializeField] private MaouSamaTD.Units.UnitBase _occupant;
        
        private GameObject _markerObject;
        
        // Zenject Injections
        [Inject] private GridGenerator _gridGenerator;
        [Inject] private GridManager _gridManager;
        
        // Fallback properties for Editor usage where injection doesn't run
        private GridGenerator Generator 
        {
            get
            {
                if (_gridGenerator != null) return _gridGenerator;
                _gridGenerator = FindFirstObjectByType<GridGenerator>();
                return _gridGenerator;
            }
        }

        private GridManager Manager
        {
            get
            {
                if (_gridManager != null) return _gridManager;
                _gridManager = FindFirstObjectByType<GridManager>();
                return _gridManager;
            }
        }

        public Vector2Int Coordinate => _coordinate;
        public TileType Type => _type;
        public bool IsOccupied => _occupant != null;
        public MaouSamaTD.Units.UnitBase Occupant => _occupant;

        public void Initialize(Vector2Int coordinate, TileType type)
        {
            _coordinate = coordinate;
            _type = type;
            name = $"Tile_{coordinate.x}_{coordinate.y}";
            UpdateTypeVisuals();
        }

        public void SetOccupant(MaouSamaTD.Units.UnitBase unit)
        {
            _occupant = unit;
            if (Manager != null) Manager.NotifyGridStateChanged();
        }

        // Visuals
        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
        private static readonly int BorderWidthId = Shader.PropertyToID("_BorderWidth");
        private static readonly int UseFullFillId = Shader.PropertyToID("_UseFullFill");

        // Cache for optimization
        private bool _isHighlightRequested;
        private Color _lastColor;
        private bool _lastUseFullFill;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer == null) _renderer = GetComponentInChildren<Renderer>(); // Robust check
            
            if (_renderer == null)
            {
                Debug.LogError($"Tile {name} has no Renderer! Highlight will fail.");
            }
            
            _propBlock = new MaterialPropertyBlock();
        }

        [Header("Materials")]
        [SerializeField] private Material _defaultMaterial;
        [SerializeField] private Material _glowMaterial;

        public void SetHighlight(bool active, Color color, bool useFullFill = false)
        {
            if (_renderer == null) return;

            // Dirty check to avoid redundant and taxing SetPropertyBlock calls
            if (_isHighlightRequested == active && _lastColor == color && _lastUseFullFill == useFullFill)
                return;

            _isHighlightRequested = active;
            _lastColor = color;
            _lastUseFullFill = useFullFill;

            // Material Swap Logic
            if (active && _glowMaterial != null && _renderer.sharedMaterial != _glowMaterial)
            {
                _renderer.sharedMaterial = _glowMaterial;
            }
            else if (!active && _defaultMaterial != null && _renderer.sharedMaterial != _defaultMaterial)
            {
                _renderer.sharedMaterial = _defaultMaterial;
            }

            _renderer.GetPropertyBlock(_propBlock);
            
            if (active)
            {
                _propBlock.SetColor(GlowColorId, color);
                _propBlock.SetFloat(GlowIntensityId, 30f); 
                _propBlock.SetFloat(BorderWidthId, useFullFill ? 0.5f : 0.1f);
                _propBlock.SetFloat(UseFullFillId, useFullFill ? 1f : 0f);
            }
            else
            {
                _propBlock.SetColor(GlowColorId, Color.black);
                _propBlock.SetFloat(GlowIntensityId, 0f);
                _propBlock.SetFloat(BorderWidthId, 0.0f);
                _propBlock.SetFloat(UseFullFillId, 0f);
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
                case TileType.DecoratedWalkable: return new Color(0.7f, 0.7f, 1f); // Light Blue-ish
                case TileType.DecoratedHighGround: return new Color(0.3f, 0.3f, 0.3f); // Dark Gray
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
            var generator = Generator; // Use property for fallback
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
                     var manager = Manager; // Use property for fallback
                     
                     if (manager != null) manager.SetTileType(_coordinate, newType);
                 }
            }
            
            // Apply locally
            _type = newType;
            UpdateTypeVisuals();
        }

        private void UpdateTypeVisuals()
        {
            // 1. Reset Base Renderer (if we modified it previously, let's reset to white/default)
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", Color.white);
                _propBlock.SetColor("_Color", Color.white);
                _renderer.SetPropertyBlock(_propBlock);
            }

            // 2. Clear Existing Marker
            if (_markerObject != null)
            {
                if (Application.isPlaying) Destroy(_markerObject);
                else DestroyImmediate(_markerObject);
                _markerObject = null;
            }

            // 3. Create Marker based on Type
            if (_type == TileType.Spawn || _type == TileType.Exit)
            {
                 // Create Container
                 _markerObject = new GameObject($"{_type}_Marker");
                 _markerObject.transform.SetParent(transform);
                 _markerObject.transform.localPosition = Vector3.zero;
                 _markerObject.transform.localRotation = Quaternion.identity;
                 _markerObject.transform.localScale = Vector3.one;

                 // Settings
                 float height = 1.5f; 
                 float thickness = 0.05f; // Thin lines
                 float size = 0.9f; 
                 
                 Color markerColor = Color.white;
                 if (_type == TileType.Spawn)
                 {
                     markerColor = Color.red; 
                 }
                 else if (_type == TileType.Exit)
                 {
                     if (ColorUtility.TryParseHtmlString("#00D2D3", out Color c))
                     {
                         markerColor = c;
                         markerColor.a = 0.5f; 
                     }
                     else markerColor = Color.cyan;
                 }
                 
                 Material mat = new Material(Shader.Find("Sprites/Default"));
                 mat.color = markerColor;

                 // Build Wireframe Box (12 Edges)
                 float halfSize = size / 2f;
                 float halfThickness = thickness / 2f; // Not used directly in pos usually
                 
                 // 4 Vertical Pillars
                 // Corners: (+-half, centerH, +-half)
                 CreateEdge(new Vector3(halfSize, height/2, halfSize), new Vector3(thickness, height, thickness), mat);
                 CreateEdge(new Vector3(-halfSize, height/2, halfSize), new Vector3(thickness, height, thickness), mat);
                 CreateEdge(new Vector3(halfSize, height/2, -halfSize), new Vector3(thickness, height, thickness), mat);
                 CreateEdge(new Vector3(-halfSize, height/2, -halfSize), new Vector3(thickness, height, thickness), mat);
                 
                 // 4 Top Rims (y = height)
                 // X-Aligned
                 CreateEdge(new Vector3(0, height, halfSize), new Vector3(size, thickness, thickness), mat);
                 CreateEdge(new Vector3(0, height, -halfSize), new Vector3(size, thickness, thickness), mat);
                 // Z-Aligned
                 CreateEdge(new Vector3(halfSize, height, 0), new Vector3(thickness, thickness, size), mat);
                 CreateEdge(new Vector3(-halfSize, height, 0), new Vector3(thickness, thickness, size), mat);
                 
                 // 4 Bottom Rims (y = 0) - Optional, but "Outline" usually implies full box
                 /*
                 CreateEdge(new Vector3(0, 0, halfSize), new Vector3(size, thickness, thickness), mat);
                 CreateEdge(new Vector3(0, 0, -halfSize), new Vector3(size, thickness, thickness), mat);
                 CreateEdge(new Vector3(halfSize, 0, 0), new Vector3(thickness, thickness, size), mat);
                 CreateEdge(new Vector3(-halfSize, 0, 0), new Vector3(thickness, thickness, size), mat);
                 */
                 // User said "higher squares", maybe they want it to look like a volume?
                 // Let's include bottom for completeness as "Edges only".
                 CreateEdge(new Vector3(0, thickness/2, halfSize), new Vector3(size, thickness, thickness), mat);
                 CreateEdge(new Vector3(0, thickness/2, -halfSize), new Vector3(size, thickness, thickness), mat);
                 CreateEdge(new Vector3(halfSize, thickness/2, 0), new Vector3(thickness, thickness, size), mat);
                 CreateEdge(new Vector3(-halfSize, thickness/2, 0), new Vector3(thickness, thickness, size), mat);
            }
        }

        private void CreateEdge(Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = "Edge";
            edge.transform.SetParent(_markerObject.transform);
            edge.transform.localPosition = localPos;
            edge.transform.localScale = scale;
            
            if (Application.isPlaying) Destroy(edge.GetComponent<Collider>());
            else DestroyImmediate(edge.GetComponent<Collider>());
            
            var mr = edge.GetComponent<Renderer>();
            if (mr != null) mr.sharedMaterial = mat;
        }

        [Button("Set as Spawn Point")]
        private void SetAsSpawn()
        {
            SetType(TileType.Spawn);
        }

        [Button("Set as Exit Point")]
        private void SetAsExit()
        {
            SetType(TileType.Exit);
        }

        [Button("Set as Walkable")]
        private void SetAsWalkable()
        {
            SetType(TileType.Walkable);
        }

        [Button("Set as HighGround")]
        private void SetAsHighGround()
        {
            SetType(TileType.HighGround);
        }
    }
}
