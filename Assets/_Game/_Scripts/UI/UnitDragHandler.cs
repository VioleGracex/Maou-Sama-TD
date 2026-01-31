using UnityEngine;
using UnityEngine.EventSystems;
using MaouSamaTD.Managers;
using MaouSamaTD.Units;
using Zenject;

namespace MaouSamaTD.UI
{
    public class UnitDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler
    {
        private UnitData _data;
        private bool _isInteractable = true;
        private bool _wasSelectedOnStart; 
        private float _pointerDownTime;
        private const float DragThreshold = 0.2f;
        // We use this because OnBeginDrag fires on jittery clicks, killing selection.

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
            // Toggle selection mode
            if (_interactionManager != null) _interactionManager.SelectUnit(_data);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isInteractable || _data == null) return;
            
            // Store state before Drag clears it
            _wasSelectedOnStart = (_interactionManager != null && _interactionManager.SelectedUnitData == _data);

            if (_interactionManager != null) _interactionManager.StartDrag(_data);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Just required interface for EndDrag to work properly in Unity UI
        }

        public void OnEndDrag(PointerEventData eventData)
        {
             if (!_isInteractable) return;
             
             float duration = Time.unscaledTime - _pointerDownTime;
             
             // Treat as Click/Toggle if:
             // 1. Released back on button (Safe check)
             // 2. OR Duration was very short (Fast Flick)
             if (eventData.pointerEnter == gameObject || duration < DragThreshold)
             {
                 // FIRST: Cancel the Drag in Manager so IsDragging = false
                 if (_interactionManager != null) _interactionManager.EndDrag(false);

                 // Treat as Click/Toggle
                 if (_wasSelectedOnStart)
                 {
                     // Was selected -> Now Toggle Off
                     if (_interactionManager != null) _interactionManager.DeselectUnit();
                 }
                 else
                 {
                     // Was NOT selected -> Now Toggle On (Restore selection)
                     if (_interactionManager != null) _interactionManager.SelectUnit(_data);
                 }
             }
             else
             {
                 // Authentic Drag -> Place
                 if (_interactionManager != null) _interactionManager.EndDrag(true);
             }
        }
    }
}
