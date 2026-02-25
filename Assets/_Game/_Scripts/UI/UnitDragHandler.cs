using UnityEngine;
using UnityEngine.EventSystems;
using MaouSamaTD.Managers;
using MaouSamaTD.Units;
using Zenject;

namespace MaouSamaTD.UI
{
    public class UnitDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerClickHandler
    {
        private UnitData _data;
        private bool _isInteractable = true;
        private bool _wasSelectedOnStart; 
        private float _pointerDownTime;
        private const float DragThreshold = 0.2f;

        [Inject] private InteractionManager _interactionManager;
        [Inject] private Grid.GridManager _gridManager;

        public void Initialize(UnitData data)
        {
            _data = data;
        }

        public void SetInteractable(bool interactable)
        {
            _isInteractable = interactable;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            _pointerDownTime = Time.unscaledTime;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isInteractable || _data == null) return;

            // Enter placement mode only on Double Click
            if (eventData.clickCount >= 2)
            {
                // Ensure any active drag visuals are cleared if they haven't been
                _interactionManager?.EndDrag(false);

                // Toggle placement mode
                bool isCurrentlySelected = (_interactionManager != null && _interactionManager.SelectedUnitData == _data);
                if (isCurrentlySelected)
                {
                    _interactionManager?.DeselectUnit();
                }
                else
                {
                    _interactionManager?.SelectUnit(_data);
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isInteractable || _data == null) return;
            
            // Store state before Drag potentially changes it
            _wasSelectedOnStart = (_interactionManager != null && _interactionManager.SelectedUnitData == _data);

            if (_interactionManager != null) _interactionManager.StartDrag(_data);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Required for IEndDragHandler
        }

        public void OnEndDrag(PointerEventData eventData)
        {
             if (!_isInteractable) return;
             
             // If we released on the button, it was likely a click (handled by OnPointerClick)
             // or a jittery start of a drag that should be canceled.
             bool releasedOnButton = eventData.pointerEnter == gameObject;

             if (releasedOnButton)
             {
                 // Cancel the Drag visuals
                 if (_interactionManager != null) _interactionManager.EndDrag(false);
             }
             else
             {
                 // Authentic Drag -> Place
                 if (_interactionManager != null) _interactionManager.EndDrag(true);
             }
        }
    }
}
