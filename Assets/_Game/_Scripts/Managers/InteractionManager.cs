using UnityEngine;
using MaouSamaTD.Grid;
using MaouSamaTD.UI;
using Zenject;

namespace MaouSamaTD.Managers
{
    public class InteractionManager : MonoBehaviour
    {
        #region Enums
        public enum SelectionMode
        {
            ClickUnit,    // Strict Unit Collider
            ClickTile,    // Tile Occupancy
            ClosestInRange // Mobile-friendly, finds closest unit in range
        }
        #endregion

        #region Events
        public event System.Action<Tile> OnTileHovered;
        public event System.Action<Tile> OnTileClicked;
        #endregion

        #region State Fields
        // Mode Settings
        [Header("Interaction Settings")]
        [SerializeField] private SelectionMode _selectionMode = SelectionMode.ClickTile;
        [SerializeField] private float _selectionRange = 1.0f; // For ClosestInRange mode

        // Placement State
        public bool IsDragging { get; private set; }
        private Units.UnitData _draggedUnitData;
        private Units.UnitData _selectedUnitData; // For click-toggle mode (Placement)

        // Hover/Selection Tracking
        private Tile _currentHoverTile;
        private Units.UnitBase _currentHoverUnit;
        private GameObject _ghostObject;
        #endregion

        #region Visual Settings
        [Header("Visual Settings")]
        [SerializeField] private Color _validGlowColor = Color.green;
        [SerializeField] private Color _invalidGlowColor = Color.red;
        [SerializeField] private Color _rangeIndicatorColor = new Color(0, 0, 1, 0.3f);
        #endregion

        #region Dependencies
        private Camera _mainCamera;
        [Inject] private GridManager _gridManager;
        [Inject] private UnitInspectorUI _unitInspectorUI;
        [Inject] private CurrencyManager _currencyManager;
        [Inject] private DeploymentUI _deploymentUI;
        #endregion

        #region Initialization
        public void Init()
        {
            _mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
        }
        #endregion

        #region Main Loop
        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            // 1. Get Input Position
            Vector2 screenPos;
            bool isPressDown;
            GetInputState(out screenPos, out isPressDown);

            // 2. Perform Raycast
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            Tile hitTile = GetTileFromRay(ray);

            // 3. Update Hover State
            HandleHover(hitTile);

            // 4. Update Ghost (if dragging)
            UpdateGhost(hitTile);

            // 5. Handle Clicks / Actions
            if (isPressDown)
            {
                if (IsDragging)
                {
                    // Dragging typically ends on release, not press down.
                    // Implementation depends on if drag is "Active Hold" or "Click-Move-Click".
                    // Assuming Hold based on EndDrag being external usually.
                }
                else if (_selectedUnitData != null && hitTile != null)
                {
                    // Placement Click
                    HandlePlacementInput(hitTile);
                }
                else
                {
                    // Selection / Inspection Click
                    HandleSelectionInput(ray, hitTile);
                }
            }
            else
            {
                // Clear state if clicked off-map?
                if (isPressDown && hitTile == null && _selectedUnitData != null)
                {
                    DeselectUnit();
                }
            }
        }
        #endregion

        #region Input Helpers
        private void GetInputState(out Vector2 screenPos, out bool isPressDown)
        {
            screenPos = Vector2.zero;
            isPressDown = false;

            if (UnityEngine.InputSystem.Pointer.current != null)
            {
                screenPos = UnityEngine.InputSystem.Pointer.current.position.ReadValue();
                isPressDown = UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame;
            }
        }

        private Tile GetTileFromRay(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                // Fallback to Grid Coordinate check if direct component missing (unlikely if Grid is robust)
                if (tile == null && _gridManager != null)
                {
                    Vector2Int coord = _gridManager.WorldToGridCoordinates(hit.point);
                    tile = _gridManager.GetTileAt(coord);
                }
                return tile;
            }
            return null;
        }
        #endregion

        #region Logic Handlers
        private void HandleHover(Tile tile)
        {
            // Tile Event
            if (tile != _currentHoverTile)
            {
                _currentHoverTile = tile;
                if (tile != null) OnTileHovered?.Invoke(tile);
            }

            // Unit Outline Hover Logic
            Units.UnitBase newHoverUnit = (tile != null) ? tile.Occupant : null;

            if (newHoverUnit != _currentHoverUnit)
            {
                // Clear old
                if (_currentHoverUnit != null)
                {
                    _currentHoverUnit.SetHighlight(false, Color.white);
                }
                
                _currentHoverUnit = newHoverUnit;
                
                // Set new
                if (_currentHoverUnit != null)
                {
                    _currentHoverUnit.SetHighlight(true, Color.white);
                }
            }
        }

        private void HandlePlacementInput(Tile tile)
        {
             // Try to place the unit we have selected from UI
             TryPlaceUnit(tile, _selectedUnitData);
             OnTileClicked?.Invoke(tile);
        }

        private void HandleSelectionInput(Ray ray, Tile hitTile)
        {
            // Logic varies by Mode
            Units.PlayerUnit targetUnit = null;

            switch (_selectionMode)
            {
                case SelectionMode.ClickTile:
                    if (hitTile != null && hitTile.Occupant is Units.PlayerUnit pUnit)
                    {
                        targetUnit = pUnit;
                    }
                    else if (hitTile != null)
                    {
                        OnTileClicked?.Invoke(hitTile); // Just a tile click
                    }
                    break;

                case SelectionMode.ClickUnit:
                    // Strict Raycast for Unit Layer/Collider
                    // Assuming units have colliders and are on a layer suitable for Raycast.
                    // If Raycast hit the Tile first (which it likely did in GetTileFromRay), we might need to check Occupant or verify layers.
                    // Using hitTile.Occupant is cleaner if the Grid is the single source of truth.
                    
                    // But if user wants "Click on Sprite", we might raycast for Sprites specifically?
                    // Let's stick to Tile Occupant for reliability, OR add a secondary Raycast for "UnitLayer".
                    
                    if (Physics.Raycast(ray, out RaycastHit unitHit, 100f, LayerMask.GetMask("Units", "Default")))
                    {
                        var hitUnit = unitHit.collider.GetComponent<Units.PlayerUnit>();
                        // Also check parent as collider might be on child
                        if (hitUnit == null) hitUnit = unitHit.collider.GetComponentInParent<Units.PlayerUnit>();
                        
                        targetUnit = hitUnit;
                    }
                    break;

                case SelectionMode.ClosestInRange:
                    // Find closest unit to the Ray hit point
                    if (Physics.Raycast(ray, out RaycastHit groundHit))
                    {
                        targetUnit = FindClosestUnit(groundHit.point, _selectionRange);
                    }
                    break;
            }

            if (targetUnit != null)
            {
                Debug.Log($"Selected Unit via {_selectionMode}: {targetUnit.name}");
                if (_unitInspectorUI != null) _unitInspectorUI.Show(targetUnit);
            }
        }
        
        private Units.PlayerUnit FindClosestUnit(Vector3 point, float range)
        {
            float closestDist = range * range; // Sqr check
            Units.PlayerUnit closest = null;
            
            // Inefficient? Iterate all tiles? Or Deployed List?
            // DeploymentUI tracks Deployed Units. But InteractionManager dependency direction?
            // DeploymentUI depends on InteractionManager? (No, InteractionManager injects DeploymentUI).
            // But we can't easily access the list unless public.
            // Safer: Iterate Grid Tiles (if grid isn't massive). Or FindObjectsOfType (Slow).
            // Grid iteration is safest if sparse.
            
            foreach(var tile in _gridManager.GetAllTiles())
            {
                if (tile.Occupant is Units.PlayerUnit pUnit)
                {
                    float distSqr = (pUnit.transform.position - point).sqrMagnitude;
                    if (distSqr < closestDist)
                    {
                        closestDist = distSqr;
                        closest = pUnit;
                    }
                }
            }
            
            return closest;
        }
        #endregion

        #region Placement Logic
        public void SelectUnit(Units.UnitData data)
        {
            if (IsDragging) return;
            _selectedUnitData = data;
            IsDragging = false; 
            UpdateTileVisuals();
        }

        public void DeselectUnit()
        {
            _selectedUnitData = null;
            UpdateTileVisuals();
        }

        public void StartDrag(Units.UnitData data)
        {
            IsDragging = true;
            _draggedUnitData = data;
            _selectedUnitData = null;

            if (_ghostObject != null) Destroy(_ghostObject);
            
            // Create Ghost with Sprite instead of Cube
            // User requirement: "not a blue box but off colored so we understand this unit is what we dragging"
            
            // 1. Create a Unit Container (mimicking Unit Prefab structure loosely or just a billboard)
            _ghostObject = new GameObject("DragGhost");
            
            // 2. Add Visuals Child (Billboard behavior if needed, or just flat if TopDown/Iso logic allows)
            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(_ghostObject.transform, false);
            visuals.transform.localPosition = Vector3.up * 1f; // Lift slightly to match Unit height (center)

            // 3. Add SpriteRenderer
            SpriteRenderer sr = visuals.AddComponent<SpriteRenderer>();
            sr.sprite = data.UnitSprite; // Use unit sprite
            
            // Optional: Billboard script if camera rotates? 
            // For now, assuming fixed perspective or billboard script added. 
            // Let's rely on modifying transform.rotation in UpdateGhost if needed or add Billboard component if we can find it.
            // Since we can't easily find "Utils.Billboard" dynamically reliably without knowing namespace (we know it: MaouSamaTD.Utils.Billboard)
            visuals.AddComponent<MaouSamaTD.Utils.Billboard>(); 

            // Initialize Color (Ghostly)
            sr.color = new Color(1f, 1f, 1f, 0.6f); 

            UpdateTileVisuals();
        }

        public void EndDrag(bool place)
        {
            if (place && _currentHoverTile != null)
            {
                TryPlaceUnit(_currentHoverTile, _draggedUnitData);
            }

            IsDragging = false;
            _draggedUnitData = null;
            if (_ghostObject != null) Destroy(_ghostObject);
            _currentHoverTile = null;
            
            UpdateTileVisuals();
        }

        private void TryPlaceUnit(Tile tile, Units.UnitData unitData)
        {
            if (_currencyManager == null || _deploymentUI == null) return;

            bool canAfford = _currencyManager.CanAfford(unitData.DeploymentCost);
            bool validTile = IsTileValidForUnit(tile, unitData);

            if (canAfford && validTile && !tile.IsOccupied)
            {
                _deploymentUI.SpawnUnit(tile, unitData);
                DeselectUnit();
            }
            else
            {
                Debug.Log("Invalid Placement or not enough funds.");
                if (_selectedUnitData != null) DeselectUnit();
            }
        }
        #endregion

        #region Helpers & Visuals
        private void UpdateGhost(Tile tile)
        {
            if (IsDragging && _ghostObject != null) // Allow dragging even off-tile to keep tracking mouse?
            {
                // Raycast again to get ground point if tile is null?
                // For now, if tile is null, we can try to track mouse on Plane(Vector3.up, 0)
                
                Vector3 targetPos = Vector3.zero;
                bool validPosition = false;

                if (tile != null)
                {
                    targetPos = tile.transform.position;
                    validPosition = IsTileValidForUnit(tile, _draggedUnitData) && !tile.IsOccupied;
                }
                else
                {
                    // Fallback raycast to ground plane
                    Ray ray = _mainCamera.ScreenPointToRay(UnityEngine.InputSystem.Pointer.current.position.ReadValue());
                    Plane ground = new Plane(Vector3.up, 0);
                    if (ground.Raycast(ray, out float enter))
                    {
                        targetPos = ray.GetPoint(enter);
                        // Snap logic? Or smooth? 
                        // If off-grid, it's invalid
                        validPosition = false; 
                    }
                }

                _ghostObject.transform.position = Vector3.Lerp(_ghostObject.transform.position, targetPos, Time.deltaTime * 20f); 

                var sr = _ghostObject.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    Color targetColor = validPosition ? _validGlowColor : _invalidGlowColor;
                    // Keep Alpha
                    targetColor.a = 0.6f; 
                    sr.color = targetColor;
                }
            }
        }

        private bool IsTileValidForUnit(Tile tile, Units.UnitData unit)
        {
            if (unit == null) return false;
            if (unit.ViableTiles == null || unit.ViableTiles.Count == 0) return false;
            return unit.ViableTiles.Contains(tile.Type);
        }

        private void UpdateTileVisuals()
        {
            if (_gridManager == null) return;

            Units.UnitData activeUnit = IsDragging ? _draggedUnitData : _selectedUnitData;
            bool isActive = activeUnit != null;
            Tile rangeCenterTile = _currentHoverTile;

            foreach (var tile in _gridManager.GetAllTiles())
            {
                if (isActive)
                {
                    bool isValidType = IsTileValidForUnit(tile, activeUnit);
                    bool isOccupied = tile.IsOccupied;
                    bool isValidPlacement = isValidType && !isOccupied;

                    bool isInRange = false;
                    if (rangeCenterTile != null)
                    {
                         float dist = Vector2Int.Distance(tile.Coordinate, rangeCenterTile.Coordinate);
                         if (dist <= activeUnit.Range) isInRange = true;
                    }

                    if (tile == rangeCenterTile)
                    {
                         Color color = isValidPlacement ? _validGlowColor : _invalidGlowColor;
                         tile.SetHighlight(true, color);
                    }
                    else if (isValidPlacement)
                    {
                        if (isInRange)
                        {
                            tile.SetHighlight(true, _rangeIndicatorColor);
                        }
                        else
                        {
                            tile.SetHighlight(true, _validGlowColor * 0.5f);
                        }
                    }
                    else
                    {
                        tile.SetHighlight(true, _invalidGlowColor);
                    }
                }
                else
                {
                    tile.SetHighlight(false, Color.black);
                }
            }
        }
        #endregion
    }
}
