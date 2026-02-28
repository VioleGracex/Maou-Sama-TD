using UnityEngine;
using MaouSamaTD.Grid;
using MaouSamaTD.Units;
using MaouSamaTD.UI;
using MaouSamaTD.Utils;
using Zenject;

namespace MaouSamaTD.Managers.Interaction
{
    public class PlacementHandler
    {
        private GameObject _ghostObject;
        private GridManager _gridManager;
        private CurrencyManager _currencyManager;
        private DeploymentUI _deploymentUI;
        private Camera _mainCamera;

        private Color _validColor;
        private Color _invalidColor;

        public PlacementHandler(GridManager gridManager, CurrencyManager currencyManager, DeploymentUI deploymentUI, Camera camera, Color validColor, Color invalidColor)
        {
            _gridManager = gridManager;
            _currencyManager = currencyManager;
            _deploymentUI = deploymentUI;
            _mainCamera = camera;
            _validColor = validColor;
            _invalidColor = invalidColor;
        }

        public void CreateGhost(UnitData data)
        {
            if (_ghostObject != null) Object.Destroy(_ghostObject);

            _ghostObject = new GameObject("DragGhost");
            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(_ghostObject.transform, false);
            
            _ghostObject.layer = 2; // Ignore Raycast
            visuals.layer = 2;
            visuals.transform.localPosition = Vector3.up * 1f; 

            SpriteRenderer sr = visuals.AddComponent<SpriteRenderer>();
            sr.sprite = data.UnitSprite; 
            visuals.AddComponent<Billboard>(); 
            sr.color = new Color(1f, 1f, 1f, 0.6f); 
            sr.sortingOrder = 100;
        }

        public void DestroyGhost()
        {
            if (_ghostObject != null) Object.Destroy(_ghostObject);
        }

        public void UpdateGhost(Tile tile, UnitData data, bool isDragging, Vector2 screenPos)
        {
            if (_ghostObject == null || data == null) return;

            Vector3 targetPos = Vector3.zero;
            bool validPosition = false;

            if (tile != null)
            {
                targetPos = tile.transform.position;
                validPosition = IsTileValidForUnit(tile, data) && !tile.IsOccupied;
            }
            else
            {
                Ray ray = _mainCamera.ScreenPointToRay(screenPos);
                Plane ground = new Plane(Vector3.up, 0);
                if (ground.Raycast(ray, out float enter))
                {
                    targetPos = ray.GetPoint(enter);
                }
            }

            _ghostObject.transform.position = targetPos; 

            var sr = _ghostObject.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                Color targetColor = validPosition ? _validColor : _invalidColor;
                targetColor.a = 0.6f; 
                sr.color = targetColor;
            }
        }

        public bool TryPlaceUnit(Tile tile, UnitData unitData)
        {
            if (unitData == null || tile == null) return false;

            bool canAfford = _currencyManager.CanAfford(unitData.DeploymentCost);
            bool validTile = IsTileValidForUnit(tile, unitData);

            if (canAfford && validTile && !tile.IsOccupied)
            {
                _deploymentUI.SpawnUnit(tile, unitData);
                return true;
            }
            return false;
        }

        private System.Collections.Generic.List<Vector2Int> _allowedTiles = new System.Collections.Generic.List<Vector2Int>();

        public void SetAllowedTiles(System.Collections.Generic.List<Vector2Int> allowedTiles)
        {
            _allowedTiles = allowedTiles ?? new System.Collections.Generic.List<Vector2Int>();
        }

        public bool IsTileValidForUnit(Tile tile, UnitData unit)
        {
            if (unit == null || tile == null) return false;
            
            // Tutorial Lock
            if (_allowedTiles.Count > 0)
            {
                if (!_allowedTiles.Contains(tile.Coordinate)) return false;
            }

            if (unit.ViableTiles == null || unit.ViableTiles.Count == 0) return false;
            return unit.ViableTiles.Contains(tile.Type);
        }
    }
}
