using UnityEngine;
using UnityEngine.EventSystems;
using MaouSamaTD.Managers;
using MaouSamaTD.Units;
using Zenject;

namespace MaouSamaTD.UI
{
    public class UnitDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private UnitData _data;
        private bool _isInteractable = true;

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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isInteractable || _data == null) return;
            // Toggle selection mode
            if (_interactionManager != null) _interactionManager.SelectUnit(_data);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isInteractable || _data == null) return;
            
            // Tell Manager we are dragging this unit
            // Grid.GridManager.Instance.GenerateTestMap(); // Removed redundant call or needs injection if strictly needed
            if (_gridManager != null) _gridManager.GenerateTestMap(); 
            
            if (_interactionManager != null) _interactionManager.StartDrag(_data);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Just required interface for EndDrag to work properly in Unity UI
        }

        public void OnEndDrag(PointerEventData eventData)
        {
             // Check if we dropped on map (Manager handles "place" logic if mouse is over map)
             if (_interactionManager != null) _interactionManager.EndDrag(true);
        }
    }
}
