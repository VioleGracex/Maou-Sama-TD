using UnityEngine;
using MaouSamaTD.Grid;
using MaouSamaTD.UI;
using Zenject;

namespace MaouSamaTD.Managers
{
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        public event System.Action<Tile> OnTileHovered;
        public event System.Action<Tile> OnTileClicked;
        
        // Placement Events
        public bool IsDragging { get; private set; }
        private Units.UnitData _draggedUnitData;
        private Units.UnitData _selectedUnitData; // For click-toggle mode
        
        private GameObject _ghostObject;
        private Tile _currentHoverTile;

        private Camera _mainCamera;

        [Header("Visual Settings")]
        [SerializeField] private Color _validGlowColor = Color.green;
        [SerializeField] private Color _invalidGlowColor = Color.red;
        [SerializeField] private Color _rangeIndicatorColor = new Color(0, 0, 1, 0.3f); // Blueish
        
        [Inject] private UnitInspectorUI _unitInspectorUI;
        [Inject] private CurrencyManager _currencyManager; // Injecting explicitly to fix NRE potential
        [Inject] private DeploymentUI _deploymentUI; // Injecting explicitly

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            HandleInput();
        }

        public void SelectUnit(Units.UnitData data)
        {
            if (IsDragging) return; // Drag overrides valid selection
            _selectedUnitData = data;
            IsDragging = false; 
            Debug.Log($"Selected Unit: {data.UnitName}");
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
            
            // If we were selected, deselect to avoid conflict or visual weirdness
            _selectedUnitData = null;

            // Create visual ghost
            if (_ghostObject != null) Destroy(_ghostObject);
            // Simple cube for now
            _ghostObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(_ghostObject.GetComponent<Collider>());
            _ghostObject.transform.localScale = Vector3.one * 0.8f; 
            
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
            // Safety Checks (Fix for NRE)
            if (_currencyManager == null)
            {
                Debug.LogError("CurrencyManager Instance is null!");
                return;
            }
            if (_deploymentUI == null)
            {
                Debug.LogError("DeploymentUI Instance is null!");
                return;
            }

            // Validate placement (Currency, Tile Type)
            bool canAfford = _currencyManager.CanAfford(unitData.DeploymentCost);
            bool validTile = IsTileValidForUnit(tile, unitData);

            if (canAfford && validTile && !tile.IsOccupied)
            {
                _deploymentUI.SpawnUnit(tile, unitData);
                // Optionally deselect after placement? 
                // User said "while toggled logic", implied single placement? 
                // Usually TD games allow multi-placement or single. Let's default to single for now.
                DeselectUnit();
            }
            else
            {
                Debug.Log("Invalid Placement or not enough funds.");
                // If in selection mode and clicked invalid -> Cancel
                if (_selectedUnitData != null) DeselectUnit();
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
            var grid = GridManager.Instance;
            if (grid == null) return;

            Units.UnitData activeUnit = IsDragging ? _draggedUnitData : _selectedUnitData;
            bool isActive = activeUnit != null;

            // Center of range calculation (Ghost or Cursor)
            Tile rangeCenterTile = _currentHoverTile;

            foreach (var tile in grid.GetAllTiles())
            {
                if (isActive)
                {
                    bool isValidType = IsTileValidForUnit(tile, activeUnit);
                    bool isOccupied = tile.IsOccupied;
                    bool isValidPlacement = isValidType && !isOccupied;

                    // Range Check
                    bool isInRange = false;
                    if (rangeCenterTile != null)
                    {
                        float dist = Vector2Int.Distance(tile.Coordinate, rangeCenterTile.Coordinate);
                        if (dist <= activeUnit.Range) isInRange = true;
                    }

                    // Priority:
                    // 1. Hovered Target (Valid/Invalid)
                    // 2. Invalid (Global Red?) - User said "invalid tiles should glow red". 
                    //    If we assume "Invalid Place" logic is more important than range.
                    //    However, usually range is shown AROUND the valid placement.
                    
                    // Logic update:
                    // If tile is the hovered tile: Show Valid(Green) or Invalid(Red).
                    // If tile is NOT hovered but is InRange: Show Range(Blue).
                    // If tile is NOT hovered and NOT InRange:
                    //     User wanted "invalid tiles glow red". 
                    //     If we make ALL invalid tiles red, map looks very red. 
                    //        Maybe only invalid tiles IN RANGE? Or Global?
                    //     Let's stick to: Hovered = Green/Red. Range = Blue.
                    //     If user wants ALL invalid tiles to be red, we can add that.
                    //     Re-reading: "the invalid tiles should glow red". 
                    //     Let's interpret this as: Tiles you CANNOT place on (Wrong type/Occupied) should be Red.
                    //     Tiles you CAN place on (Valid) should be Green (or default/Range color?).
                    
                    if (tile == rangeCenterTile)
                    {
                        // The target tile
                         Color color = isValidPlacement ? _validGlowColor : _invalidGlowColor;
                         tile.SetHighlight(true, color);
                    }
                    else if (isValidPlacement)
                    {
                        // Valid but not hovered. Green? Or Off? 
                        // Maybe Green to show "You can place here".
                        // And Range Color if in Range?
                        // Let's mix: Range Overrides Valid Indicator? 
                        
                        if (isInRange)
                        {
                            tile.SetHighlight(true, _rangeIndicatorColor);
                        }
                        else
                        {
                            // Show it's valid placement option?
                            tile.SetHighlight(true, _validGlowColor * 0.5f); // Dimmer green?
                        }
                    }
                    else
                    {
                        // Invalid placement
                         tile.SetHighlight(true, _invalidGlowColor);
                    }
                }
                else
                {
                    tile.SetHighlight(false, Color.black);
                }
            }
        }

        private void OnEnable()
        {
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
        }

        private void HandleInput()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            // Use Enhanced Touch for Pointer/Touch input
            var fingers = UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers;
            
            // Pointer approach
            Vector2 screenPos = Vector2.zero;
            bool isPressing = false;
            bool isPressDown = false;

            if (UnityEngine.InputSystem.Pointer.current != null)
            {
                 screenPos = UnityEngine.InputSystem.Pointer.current.position.ReadValue();
                 isPressing = UnityEngine.InputSystem.Pointer.current.press.isPressed;
                 isPressDown = UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame;
            }
            
            // Raycast
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile == null)
                {
                     Vector2Int coord = GridManager.Instance.WorldToGridCoordinates(hit.point);
                     tile = GridManager.Instance.GetTileAt(coord);
                }

                if (tile != null)
                {
                    _currentHoverTile = tile;
                    OnTileHovered?.Invoke(tile);
                    
                    if (IsDragging && _ghostObject != null)
                    {
                        // Snap ghost
                        _ghostObject.transform.position = tile.transform.position + Vector3.up * 0.5f;
                        
                        // Colorize Validity on Ghost (Optional, since tiles glow now)
                        // Keeping for extra feedback
                        bool valid = IsTileValidForUnit(tile, _draggedUnitData) && !tile.IsOccupied;
                        var rend = _ghostObject.GetComponent<Renderer>();
                        if (rend != null)
                        {
                            rend.material.color = valid ? _validGlowColor : _invalidGlowColor;
                        }
                    }

                    if (isPressDown)
                    {
                        if (IsDragging)
                        {
                           // Drag click logic?? Usually dragging uses Hold. 
                           // If using click-drag, this might be end. 
                           // But UnitDragHandler handles EndDrag. 
                           // So we ignore click if dragging here usually.
                        }
                        else if (_selectedUnitData != null)
                        {
                            // Click to Place
                            TryPlaceUnit(tile, _selectedUnitData);
                            OnTileClicked?.Invoke(tile); // Trigger event too
                        }
                        else if (tile.IsOccupied && tile.Occupant != null && tile.Occupant is Units.PlayerUnit playerUnit)
                        {
                            // Select Deployed Unit
                            Debug.Log($"Selected Deployed Unit: {playerUnit.name}");
                            
                            if (_unitInspectorUI != null)
                                _unitInspectorUI.Show(playerUnit);
                        }
                        else
                        {
                            OnTileClicked?.Invoke(tile);
                        }
                    }
                }
            }
            else
            {
                // Clicked nowhere/off-map
                if (isPressDown && !IsDragging && _selectedUnitData != null)
                {
                    DeselectUnit();
                }
            }
        }
    }
}
