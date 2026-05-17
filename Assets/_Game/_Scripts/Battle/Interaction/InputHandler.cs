using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using MaouSamaTD.Grid;
using MaouSamaTD.Units;

using Zenject;

namespace MaouSamaTD.Managers.Interaction
{
    public class InputHandler
    {
        private Camera _mainCamera;
        private GridManager _gridManager;

        public InputHandler(Camera camera, GridManager gridManager)
        {
            _mainCamera = camera;
            _gridManager = gridManager;
            EnhancedTouchSupport.Enable();
        }

        public void Enable()
        {
            EnhancedTouchSupport.Enable();
        }

        public void Disable()
        {
            EnhancedTouchSupport.Disable();
        }

        public bool GetPointerState(out Vector2 screenPos, out bool isPressDown, out bool isReleased, out bool isRightClick)
        {
            screenPos = Vector2.zero;
            isPressDown = false;
            isReleased = false;
            isRightClick = false;

            // Priority 1: Touch (Mobile)
            if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
            {
                var touch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];
                screenPos = touch.screenPosition;
                isPressDown = touch.began;
                isReleased = touch.ended;
                return true;
            }

            // Priority 2: Pointer (Mouse/Pen - PC)
            if (Pointer.current != null)
            {
                screenPos = Pointer.current.position.ReadValue();
                isPressDown = Pointer.current.press.wasPressedThisFrame;
                isReleased = Pointer.current.press.wasReleasedThisFrame;
                
                // Check Right Click for Cancel
                if (Mouse.current != null)
                {
                    isRightClick = Mouse.current.rightButton.wasPressedThisFrame;
                }
                
                return true;
            }

            return false;
        }

        public Tile GetTileFromScreenPos(Vector2 screenPos)
        {
            if (_mainCamera == null) return null;

            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile == null)
                {
                    UnitBase unit = hit.collider.GetComponent<UnitBase>() ?? hit.collider.GetComponentInParent<UnitBase>();
                    if (unit != null)
                    {
                        if (unit is PlayerUnit pu && pu.CurrentTile != null)
                        {
                            tile = pu.CurrentTile;
                        }
                        else if (_gridManager != null)
                        {
                            Vector2Int coord = _gridManager.WorldToGridCoordinates(unit.transform.position);
                            tile = _gridManager.GetTileAt(coord);
                        }
                    }
                }
                // Fallback to Grid Coordinate check
                if (tile == null && _gridManager != null)
                {
                    Vector2Int coord = _gridManager.WorldToGridCoordinates(hit.point);
                    tile = _gridManager.GetTileAt(coord);
                }
                return tile;
            }
            return null;
        }

        public Ray GetRayFromScreenPos(Vector2 screenPos)
        {
            return _mainCamera.ScreenPointToRay(screenPos);
        }
    }
}
