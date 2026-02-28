using UnityEngine;
using MaouSamaTD.Grid;
using MaouSamaTD.UI;
using MaouSamaTD.Units;
using MaouSamaTD.Skills;
using MaouSamaTD.Managers.Interaction;
using UnityEngine.EventSystems;
using Zenject;

namespace MaouSamaTD.Managers
{
    public class InteractionManager : MonoBehaviour
    {
        #region Events
        public event System.Action<Tile> OnTileHovered;
        public event System.Action<Tile> OnTileClicked;
        #endregion

        #region Serialized Settings
        [Header("Interaction Settings")]
        [SerializeField] private SelectionHandler.SelectionMode _selectionMode = SelectionHandler.SelectionMode.ClickTile;
        [SerializeField] private float _selectionRange = 1.0f;

        [Header("Visual Settings")]
        [SerializeField] private Color _validGlowColor = Color.green;
        [SerializeField] private Color _invalidGlowColor = Color.red;
        [SerializeField] private Color _rangeIndicatorColor = new Color(0, 0, 1, 0.3f);
        [Space]
        [SerializeField] private bool _useFullFillForPlacement = false;
        [SerializeField] private bool _useFullFillForRange = true;
        [SerializeField] private bool _useFullFillForSkills = false;
        #endregion

        #region State
        public bool IsDragging { get; private set; }
        private UnitData _activeUnitData; // Shared between dragging and placement mode
        private SovereignRiteData _selectedSkill;
        private bool _isSkillTargeting;
        
        private Tile _currentHoverTile;
        private UnitBase _currentHoverUnit;
        private PlayerUnit _inspectedPlayerUnit;
        public UnitData SelectedUnitData => _activeUnitData;
        #endregion

        #region Handlers
        private InputHandler _inputHandler;
        private SelectionHandler _selectionHandler;
        private PlacementHandler _placementHandler;
        private TileVisualsHandler _tileVisualsHandler;
        #endregion

        #region Dependencies
        [Inject] private GridManager _gridManager;
        [Inject] private UnitInspectorUI _unitInspectorUI;
        [Inject] private CurrencyManager _currencyManager;
        [Inject] private DeploymentUI _deploymentUI;
        [Inject] private SkillManager _skillManager;
        #endregion

        #region Lifecycle
        public void Init()
        {
            Camera mainCam = Camera.main;
            _inputHandler = new InputHandler(mainCam, _gridManager);
            _selectionHandler = new SelectionHandler(_gridManager, mainCam);
            _placementHandler = new PlacementHandler(_gridManager, _currencyManager, _deploymentUI, mainCam, _validGlowColor, _invalidGlowColor);
            _tileVisualsHandler = new TileVisualsHandler(_gridManager);

            SyncVisualSettings();

            if (_unitInspectorUI != null)
            {
                _unitInspectorUI.OnPanelHidden += () => 
                {
                    _inspectedPlayerUnit = null;
                    UpdateTileVisuals();
                };
            }
            Debug.Log("[InteractionManager] Initialized.");
        }

        private void OnEnable() => _inputHandler?.Enable();
        private void OnDisable() => _inputHandler?.Disable();

        private void Update()
        {
            if (_inputHandler == null) return;

            if (_inputHandler.GetPointerState(out Vector2 screenPos, out bool isPressDown, out bool isRightClick))
            {
                if (isRightClick)
                {
                    CancelAllActions();
                    return;
                }

                Tile hitTile = _inputHandler.GetTileFromScreenPos(screenPos);
                HandleHover(hitTile);
                _placementHandler.UpdateGhost(hitTile, _activeUnitData, IsDragging, screenPos);

                if (isPressDown && !EventSystem.current.IsPointerOverGameObject())
                {
                    ProcessAction(hitTile, _inputHandler.GetRayFromScreenPos(screenPos));
                }
            }
        }
        #endregion

        #region Public API
        public void SelectUnit(UnitData data)
        {
            if (_activeUnitData == data) 
            { 
                DeselectUnit(); 
                return; 
            }
            _activeUnitData = data;
            IsDragging = false;
            _unitInspectorUI.Hide();
            _inspectedPlayerUnit = null;
            _placementHandler.CreateGhost(data);
            _deploymentUI.UpdateSelectionHighlight(data);
            UpdateTileVisuals();
        }

        public void DeselectUnit()
        {
            _activeUnitData = null;
            IsDragging = false;
            _placementHandler.DestroyGhost();
            _deploymentUI.UpdateSelectionHighlight(null);
            UpdateTileVisuals();
        }

        public void StartDrag(UnitData data)
        {
            _activeUnitData = data;
            IsDragging = true;
            _unitInspectorUI.Hide();
            _inspectedPlayerUnit = null;
            _placementHandler.CreateGhost(data);
            UpdateTileVisuals();
        }

        public void EndDrag(bool place)
        {
            if (place && _currentHoverTile != null)
            {
                _placementHandler.TryPlaceUnit(_currentHoverTile, _activeUnitData);
            }
            DeselectUnit();
        }

        public void SelectSkill(SovereignRiteData skill)
        {
            if (_selectedSkill == skill && _isSkillTargeting) { DeselectSkill(); return; }
            DeselectUnit();
            _selectedSkill = skill;
            _isSkillTargeting = true;
            UpdateTileVisuals();
        }

        public void DeselectSkill()
        {
            _isSkillTargeting = false;
            _selectedSkill = null;
            UpdateTileVisuals();
        }

        public void UpdateTileVisuals()
        {
            SyncVisualSettings();
            _tileVisualsHandler.UpdateVisuals(_activeUnitData, IsDragging, _isSkillTargeting, _selectedSkill, _currentHoverTile, _inspectedPlayerUnit);
        }

        public void SetPlacementRestriction(System.Collections.Generic.List<Vector2Int> allowedTiles)
        {
            _placementHandler.SetAllowedTiles(allowedTiles);
        }

        public void NotifyUnitRemoved(PlayerUnit unit)
        {
            if (_inspectedPlayerUnit == unit)
            {
                _inspectedPlayerUnit = null;
                _unitInspectorUI?.Hide();
                UpdateTileVisuals();
            }
        }
        #endregion

        #region Internal Logic
        private void HandleHover(Tile tile)
        {
            if (tile != _currentHoverTile)
            {
                _currentHoverTile = tile;
                if (tile != null) OnTileHovered?.Invoke(tile);
                UpdateTileVisuals();
            }

            // Unit Outline Hover Logic
            UnitBase newHoverUnit = null;
            if (_inputHandler.GetPointerState(out Vector2 screenPos, out _, out _))
            {
                Ray ray = _inputHandler.GetRayFromScreenPos(screenPos);
                if (Physics.Raycast(ray, out RaycastHit unitHit, 100f, LayerMask.GetMask("Units", "Default")))
                {
                    newHoverUnit = unitHit.collider.GetComponent<UnitBase>() ?? unitHit.collider.GetComponentInParent<UnitBase>();
                }
            }

            if (newHoverUnit == null && tile != null) newHoverUnit = tile.Occupant;

            if (newHoverUnit != _currentHoverUnit)
            {
                if (_currentHoverUnit != null) _currentHoverUnit.SetHighlight(false, Color.white);
                _currentHoverUnit = newHoverUnit;
                if (_currentHoverUnit != null) _currentHoverUnit.SetHighlight(true, Color.white);
            }
        }

        private void ProcessAction(Tile hitTile, Ray ray)
        {
            if (IsDragging) return; // EndDrag handles release elsewhere if needed, but here we trigger on press

            if (_isSkillTargeting)
            {
                HandleSkillInput(ray);
            }
            else if (_activeUnitData != null && hitTile != null)
            {
                if (_placementHandler.TryPlaceUnit(hitTile, _activeUnitData))
                {
                    DeselectUnit();
                }
                OnTileClicked?.Invoke(hitTile);
            }
            else if (hitTile != null)
            {
                PlayerUnit target = _selectionHandler.FindTargetUnit(ray, hitTile, _selectionMode, _selectionRange);
                if (target != null)
                {
                    _inspectedPlayerUnit = target;
                    _unitInspectorUI.Show(target);
                }
                else
                {
                    _inspectedPlayerUnit = null;
                    _unitInspectorUI.Hide();
                    OnTileClicked?.Invoke(hitTile);
                }
                UpdateTileVisuals();
            }
            else
            {
                CancelAllActions();
            }
        }

        private void HandleSkillInput(Ray ray)
        {
            if (_selectedSkill == null) return;
            
            Vector3 targetPos = Vector3.zero;
            UnitBase targetUnit = null;
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~LayerMask.GetMask("Ignore Raycast")))
            {
                targetUnit = hit.collider.GetComponent<UnitBase>();
                targetPos = new Vector3(hit.point.x, 0, hit.point.z);
            }
            
            if (targetUnit == null)
            {
                 Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                 if (groundPlane.Raycast(ray, out float enter)) targetPos = ray.GetPoint(enter);
            }

            if (_skillManager.TryExecuteRite(_selectedSkill, targetPos, targetUnit))
            {
                DeselectSkill();
            }
        }

        private void CancelAllActions()
        {
            DeselectUnit();
            DeselectSkill();
            _inspectedPlayerUnit = null;
            _unitInspectorUI?.Hide();
            UpdateTileVisuals();
        }

        private void SyncVisualSettings()
        {
            if (_tileVisualsHandler == null) return;
            _tileVisualsHandler.RangeColor = _rangeIndicatorColor;
            _tileVisualsHandler.ValidColor = _validGlowColor;
            _tileVisualsHandler.InvalidColor = _invalidGlowColor;
            _tileVisualsHandler.UseFullFillRange = _useFullFillForRange;
            _tileVisualsHandler.UseFullFillPlacement = _useFullFillForPlacement;
            _tileVisualsHandler.UseFullFillSkills = _useFullFillForSkills;
        }
        #endregion
    }
}
