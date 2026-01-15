using UnityEngine;
using UnityEngine.EventSystems;
using MaouSamaTD.Managers;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI
{
    public class UnitDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private UnitData _data;
        private bool _isInteractable = true;

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
            InteractionManager.Instance.SelectUnit(_data);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isInteractable || _data == null) return;
            
            // Tell Manager we are dragging this unit
            Grid.GridManager.Instance.GenerateTestMap(); // Ensure grid is ready if needed, mostly redundant safe-guard
            InteractionManager.Instance.StartDrag(_data);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Just required interface for EndDrag to work properly in Unity UI
        }

        public void OnEndDrag(PointerEventData eventData)
        {
             // Check if we dropped on map (Manager handles "place" logic if mouse is over map)
             InteractionManager.Instance.EndDrag(true);
        }
    }
}
