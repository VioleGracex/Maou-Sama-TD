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
        
        private Units.UnitData _selectedUnitData; 
        public Units.UnitData SelectedUnitData => _selectedUnitData;


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
                    // Dragging typically ends on release.
                }
                else if (_selectedUnitData != null && hitTile != null)
                {
                    HandlePlacementInput(hitTile);
                }
                else
                {
                    HandleSelectionInput(ray, hitTile);
                }
            }
            
            // Right Click Cancel (New)
            if (UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (_selectedUnitData != null) DeselectUnit();
                else if (_currentHoverUnit != null) _unitInspectorUI.Hide();
            }
            // Left Click on Empty Space outside range/unit -> Cancel?
            // If we clicked and hitTile is null, it means we clicked off-grid.
            else if (isPressDown && hitTile == null)
            {
                 if (_selectedUnitData != null) DeselectUnit();
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
        #region Logic Handlers
        private void HandleHover(Tile tile)
        {
            // Tile Event
            if (tile != _currentHoverTile)
            {
                _currentHoverTile = tile;
                if (tile != null) OnTileHovered?.Invoke(tile);
                UpdateTileVisuals();
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
             if (_selectedUnitData != null)
             {
                 if (tile.Occupant != null)
                 {
                     // User clicked a unit while in placement mode -> Cancel?
                     // Or maybe they want to inspect? 
                     // Requirement: "cant click on unit again or empty space to cancel placement unit mode"
                     
                     // If clicking same tile twice? or just any occupied tile?
                     // Let's cancel if invalid placement anyway.
                     DeselectUnit();
                 }
                 else
                 {
                     TryPlaceUnit(tile, _selectedUnitData);
                 }
                 OnTileClicked?.Invoke(tile);
             }
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
                    if (Physics.Raycast(ray, out RaycastHit unitHit, 100f, LayerMask.GetMask("Units", "Default")))
                    {
                        var hitUnit = unitHit.collider.GetComponent<Units.PlayerUnit>();
                        if (hitUnit == null) hitUnit = unitHit.collider.GetComponentInParent<Units.PlayerUnit>();
                        targetUnit = hitUnit;
                    }
                    break;

                case SelectionMode.ClosestInRange:
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
            else
            {
                // Clicked Empty Space/Non-Unit
                // Ensure inspector closes if we click nothing
                if (_unitInspectorUI != null) _unitInspectorUI.Hide();
            }
        }
        
        private Units.PlayerUnit FindClosestUnit(Vector3 point, float range)
        {
            float closestDist = range * range; // Sqr check
            Units.PlayerUnit closest = null;
            
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
            // Toggle off if same unit selected
            if (_selectedUnitData == data)
            {
                DeselectUnit();
                return;
            }

            if (IsDragging) return;
            _selectedUnitData = data;
            IsDragging = false; 
            
            // Create Ghost Immediately for "Click-Place" mode
            CreateGhost(data);
            
            if (_deploymentUI != null) _deploymentUI.UpdateSelectionHighlight(data);
            
            UpdateTileVisuals();
        }

        public void DeselectUnit()
        {
            _selectedUnitData = null;
            if (_ghostObject != null) Destroy(_ghostObject);
            if (_deploymentUI != null) _deploymentUI.UpdateSelectionHighlight(null);
            UpdateTileVisuals();
        }

        public void StartDrag(Units.UnitData data)
        {
            IsDragging = true;
            _draggedUnitData = data;
            _selectedUnitData = null;

            if (_ghostObject != null) Destroy(_ghostObject);
            CreateGhost(data);

            UpdateTileVisuals();
        }
        
        private void CreateGhost(Units.UnitData data)
        {
            _ghostObject = new GameObject("DragGhost");
            
            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(_ghostObject.transform, false);
            // Lift HIGHER to avoid Z-Overdraw with TileGlow/HighGround
            // Original was 1f, increasing to 2f for safety against 0.5f/1f high ground + Shader offset
            visuals.transform.localPosition = Vector3.up * 2f; 

            SpriteRenderer sr = visuals.AddComponent<SpriteRenderer>();
            sr.sprite = data.UnitSprite; 
            
            visuals.AddComponent<MaouSamaTD.Utils.Billboard>(); 

            sr.color = new Color(1f, 1f, 1f, 0.6f); 
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
            if (unitData == null) return;

            bool canAfford = _currencyManager.CanAfford(unitData.DeploymentCost);
            bool validTile = IsTileValidForUnit(tile, unitData);

            if (canAfford && validTile && !tile.IsOccupied)
            {
                _deploymentUI.SpawnUnit(tile, unitData);
                // Keep selected for multi-placement? Or Deselect? 
                // User said "cant click on unit again... to cancel", implying they stay in mode until cancel.
                // But usually TD games select once. Let's Deselect to be safe, easy to re-click.
                DeselectUnit();
            }
            else
            {
                Debug.Log("Invalid Placement or not enough funds.");
                // Provide feedback?
            }
        }
        #endregion

        #region Helpers & Visuals
        private void UpdateGhost(Tile tile)
        {
            // Works for both Dragging AND Selected state now
            if ((IsDragging || _selectedUnitData != null) && _ghostObject != null) 
            {
                Vector3 targetPos = Vector3.zero;
                bool validPosition = false;
                Units.UnitData activeData = IsDragging ? _draggedUnitData : _selectedUnitData;

                if (tile != null)
                {
                    targetPos = tile.transform.position;
                    validPosition = IsTileValidForUnit(tile, activeData) && !tile.IsOccupied;
                }
                else
                {
                    // Float off-grid
                    Ray ray = _mainCamera.ScreenPointToRay(UnityEngine.InputSystem.Pointer.current.position.ReadValue());
                    Plane ground = new Plane(Vector3.up, 0);
                    if (ground.Raycast(ray, out float enter))
                    {
                        targetPos = ray.GetPoint(enter);
                        validPosition = false; 
                    }
                }

                _ghostObject.transform.position = Vector3.Lerp(_ghostObject.transform.position, targetPos, Time.deltaTime * 20f); 

                var sr = _ghostObject.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    Color targetColor = validPosition ? _validGlowColor : _invalidGlowColor;
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
                    
                    // Show Range from Hover Tile
                    if (rangeCenterTile != null && activeUnit.Range > 0)
                    {
                         float dist = Vector2.Distance(
                             new Vector2(tile.Coordinate.x, tile.Coordinate.y), 
                             new Vector2(rangeCenterTile.Coordinate.x, rangeCenterTile.Coordinate.y));
                         
                         if (dist <= activeUnit.Range) isInRange = true;
                    }

                    if (tile == rangeCenterTile)
                    {
                         Color color = isValidPlacement ? _validGlowColor : _invalidGlowColor;
                         tile.SetHighlight(true, color);
                    }
                    else if (isInRange)
                    {
                        // Range Indicator Override
                        tile.SetHighlight(true, _rangeIndicatorColor);
                    }
                    else if (isValidPlacement)
                    {
                        tile.SetHighlight(true, _validGlowColor * 0.4f);
                    }
                    else
                    {
                        tile.SetHighlight(false, Color.black);
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
#endregion
}
