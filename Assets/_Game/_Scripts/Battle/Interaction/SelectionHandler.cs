using UnityEngine;
using MaouSamaTD.Grid;
using MaouSamaTD.Units;

namespace MaouSamaTD.Managers.Interaction
{
    public class SelectionHandler
    {
        public enum SelectionMode
        {
            ClickUnit,    // Strict Unit Collider
            ClickTile,    // Tile Occupancy
            ClosestInRange // Mobile-friendly, finds closest unit in range
        }

        private GridManager _gridManager;
        private Camera _mainCamera;

        public SelectionHandler(GridManager gridManager, Camera camera)
        {
            _gridManager = gridManager;
            _mainCamera = camera;
        }

        public PlayerUnit FindTargetUnit(Ray ray, Tile hitTile, SelectionMode mode, float selectionRange)
        {
            switch (mode)
            {
                case SelectionMode.ClickTile:
                    if (hitTile != null && hitTile.Occupant is PlayerUnit pUnit)
                        return pUnit;
                    break;

                case SelectionMode.ClickUnit:
                    if (Physics.Raycast(ray, out RaycastHit unitHit, 100f, LayerMask.GetMask("Units", "Default")))
                    {
                        var hitUnit = unitHit.collider.GetComponent<PlayerUnit>();
                        if (hitUnit == null) hitUnit = unitHit.collider.GetComponentInParent<PlayerUnit>();
                        return hitUnit;
                    }
                    break;

                case SelectionMode.ClosestInRange:
                    if (Physics.Raycast(ray, out RaycastHit groundHit))
                    {
                        return FindClosestUnit(groundHit.point, selectionRange);
                    }
                    break;
            }
            return null;
        }

        public PlayerUnit FindClosestUnit(Vector3 point, float range)
        {
            float closestDist = range * range;
            PlayerUnit closest = null;
            
            foreach (var tile in _gridManager.GetAllTiles())
            {
                if (tile.Occupant is PlayerUnit pUnit)
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
    }
}
