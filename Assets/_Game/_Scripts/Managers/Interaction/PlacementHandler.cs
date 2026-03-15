using UnityEngine;
using MaouSamaTD.Grid;
using MaouSamaTD.Units;
using MaouSamaTD.UI;
using MaouSamaTD.Utils;

namespace MaouSamaTD.Managers.Interaction
{
    public class PlacementHandler
    {
        private GameObject _ghostObject;
        private GridManager _gridManager;
        private BattleCurrencyManager _currencyManager;
        private DeploymentUI _deploymentUI;
        private Camera _mainCamera;

        private Color _validColor;
        private Color _invalidColor;

        public string LastRejectionReason { get; private set; }

        public PlacementHandler(GridManager gridManager, BattleCurrencyManager currencyManager, DeploymentUI deploymentUI, Camera camera, Color validColor, Color invalidColor)
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
                bool isOccupied = tile.IsOccupied;
                bool isValid = IsTileValidForUnit(tile, data);
                
                if (isOccupied) LastRejectionReason = "Tile is OCCUPIED";
                validPosition = isValid && !isOccupied;
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

                if (_gridManager != null && _gridManager.SpawnPoints != null && _gridManager.SpawnPoints.Count > 0)
                {
                    Vector2Int unitCoord = _gridManager.WorldToGridCoordinates(targetPos);
                    Vector2Int closestSpawn = _gridManager.SpawnPoints[0].Coordinate;
                    float minDist = Vector2.Distance(unitCoord, closestSpawn);
                    
                    for (int i = 1; i < _gridManager.SpawnPoints.Count; i++)
                    {
                        float dist = Vector2.Distance(unitCoord, _gridManager.SpawnPoints[i].Coordinate);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestSpawn = _gridManager.SpawnPoints[i].Coordinate;
                        }
                    }

                    sr.flipX = closestSpawn.x < unitCoord.x;
                }
            }
        }

        public bool TryPlaceUnit(Tile tile, UnitData unitData)
        {
            if (unitData == null || tile == null) return false;

            bool canAfford = _currencyManager != null ? _currencyManager.CanAfford(unitData.DeploymentCost) : true;
            bool validTile = IsTileValidForUnit(tile, unitData);

            if (canAfford && validTile && !tile.IsOccupied)
            {
                _deploymentUI.SpawnUnit(tile, unitData);
                return true;
            }
            return false;
        }

        private System.Collections.Generic.List<Vector2Int> _allowedTiles = new System.Collections.Generic.List<Vector2Int>();
        public System.Collections.Generic.List<Vector2Int> AllowedTiles => _allowedTiles;

        public void SetAllowedTiles(System.Collections.Generic.List<Vector2Int> allowedTiles)
        {
            _allowedTiles = allowedTiles ?? new System.Collections.Generic.List<Vector2Int>();
        }

        public bool IsTileValidForUnit(Tile tile, UnitData unit)
        {
            if (unit == null) { LastRejectionReason = "Unit Data is NULL"; return false; }
            if (tile == null) { LastRejectionReason = "Tile is NULL"; return false; }
            
            if (_allowedTiles.Count > 0)
            {
                bool isAllowed = _allowedTiles.Contains(tile.Coordinate);
                if (!isAllowed) 
                {
                    LastRejectionReason = $"Tutorial Restriction: Allowed {string.Join(", ", _allowedTiles)}";
                    return false;
                }
            }

            if (unit.ViableTiles == null || unit.ViableTiles.Count == 0) 
            {
                LastRejectionReason = "Unit has NO ViableTiles defined!";
                return false;
            }

            bool typeValid = unit.ViableTiles.Contains(tile.Type);
            if (!typeValid) LastRejectionReason = $"Tile Type {tile.Type} not in unit's ViableTiles list";
            
            return typeValid;
        }
    }
}
