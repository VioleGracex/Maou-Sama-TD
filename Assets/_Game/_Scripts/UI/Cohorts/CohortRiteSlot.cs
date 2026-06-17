using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using MaouSamaTD.Skills;

namespace MaouSamaTD.UI.Cohorts
{
    public class CohortRiteSlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public static CohortRiteSlot SelectedSlot { get; private set; }

        private static readonly Color HighlightColor = new Color(1f, 0.82f, 0.12f); // Gold
        private static readonly Color DefaultColor = new Color(0.100f, 0.100f, 0.130f); // Dark grey

        [Header("Filled State UI")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _cooldownText;
        [SerializeField] private GameObject _filledContainer;

        [Header("Empty State UI")]
        [SerializeField] private GameObject _emptyContainer;
        [SerializeField] private TextMeshProUGUI _emptySlotText;

        [Header("Actions")]
        [SerializeField] private Button _clearButton;

        public SovereignRiteData RiteData { get; private set; }
        public int SlotIndex { get; private set; }
        private bool _isLocked = false;

        public event Action<int, SovereignRiteData> OnRiteDropped;
        public event Action<int> OnRiteCleared;

        public void Initialize(int slotIndex, bool isLocked)
        {
            SlotIndex = slotIndex;
            _isLocked = isLocked;

            if (_emptySlotText != null)
            {
                _emptySlotText.text = $"+ Empty Sovereign Rite Slot {slotIndex + 1}";
            }

            if (_clearButton != null)
            {
                _clearButton.onClick.RemoveAllListeners();
                _clearButton.onClick.AddListener(ClearSlot);
                _clearButton.gameObject.SetActive(!_isLocked && RiteData != null);
            }
        }

        public void SetRite(SovereignRiteData data)
        {
            RiteData = data;
            
            if (data == null)
            {
                if (_emptyContainer != null) _emptyContainer.SetActive(true);
                if (_filledContainer != null) _filledContainer.SetActive(false);
                if (_clearButton != null) _clearButton.gameObject.SetActive(false);
                return;
            }

            if (_emptyContainer != null) _emptyContainer.SetActive(false);
            if (_filledContainer != null) _filledContainer.SetActive(true);

            if (_iconImage != null)
            {
                if (data.Icon != null)
                {
                    _iconImage.sprite = data.Icon;
                    _iconImage.enabled = true;
                }
                else
                {
                    _iconImage.enabled = false;
                }
            }
            if (_nameText != null) _nameText.text = data.SkillName;
            if (_descriptionText != null) _descriptionText.text = data.GetFormattedDescription();
            if (_costText != null) _costText.text = $"{data.SealCost} Seals";
            if (_cooldownText != null) _cooldownText.text = $"{data.Cooldown:F0}s CD";

            if (_clearButton != null)
            {
                _clearButton.gameObject.SetActive(!_isLocked);
            }
        }

        public void ClearSlot()
        {
            if (_isLocked) return;
            RiteData = null;
            SetRite(null);
            OnRiteCleared?.Invoke(SlotIndex);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_isLocked) return;

            _previewData = null; // Clear preview

            GameObject droppedObj = eventData.pointerDrag;
            if (droppedObj == null) return;

            CohortRiteItemUI riteItem = droppedObj.GetComponent<CohortRiteItemUI>();
            if (riteItem != null && riteItem.RiteData != null)
            {
                SetRite(riteItem.RiteData);
                OnRiteDropped?.Invoke(SlotIndex, riteItem.RiteData);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isLocked) return;

            // Clear on right click
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                ClearSlot();
                return;
            }

            if (SelectedSlot == this)
            {
                Deselect();
            }
            else
            {
                if (SelectedSlot != null) SelectedSlot.Deselect();
                Select();
            }
        }

        public void Select()
        {
            SelectedSlot = this;
            var img = GetComponent<Image>();
            if (img != null) img.color = HighlightColor;
        }

        public void Deselect()
        {
            if (SelectedSlot == this) SelectedSlot = null;
            var img = GetComponent<Image>();
            if (img != null) img.color = DefaultColor;
        }

        public void SelectRite(SovereignRiteData data)
        {
            if (_isLocked) return;
            SetRite(data);
            OnRiteDropped?.Invoke(SlotIndex, data);
        }

        private SovereignRiteData _previewData = null;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isLocked) return;

            GameObject draggedObj = eventData.pointerDrag;
            if (draggedObj != null)
            {
                CohortRiteItemUI riteItem = draggedObj.GetComponent<CohortRiteItemUI>();
                if (riteItem != null && riteItem.RiteData != null)
                {
                    _previewData = riteItem.RiteData;
                    SetPreviewRite(_previewData);

                    // Hide the dragged visual representation
                    if (riteItem.DraggedVisual != null)
                        riteItem.DraggedVisual.SetActive(false);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_previewData != null)
            {
                _previewData = null;
                SetRite(RiteData); // Restore actual data

                GameObject draggedObj = eventData.pointerDrag;
                if (draggedObj != null)
                {
                    CohortRiteItemUI riteItem = draggedObj.GetComponent<CohortRiteItemUI>();
                    if (riteItem != null && riteItem.DraggedVisual != null)
                    {
                        riteItem.DraggedVisual.SetActive(true);
                    }
                }
            }
        }

        private void SetPreviewRite(SovereignRiteData data)
        {
            if (data == null) return;

            if (_emptyContainer != null) _emptyContainer.SetActive(false);
            if (_filledContainer != null) _filledContainer.SetActive(true);

            if (_iconImage != null)
            {
                if (data.Icon != null)
                {
                    _iconImage.sprite = data.Icon;
                    _iconImage.enabled = true;
                }
                else
                {
                    _iconImage.enabled = false;
                }
            }
            if (_nameText != null) _nameText.text = data.SkillName;
            if (_descriptionText != null) _descriptionText.text = data.GetFormattedDescription();
            if (_costText != null) _costText.text = $"{data.SealCost} Seals";
            if (_cooldownText != null) _cooldownText.text = $"{data.Cooldown:F0}s CD";
            
            // Note: We don't want the clear button showing on a preview
            if (_clearButton != null) _clearButton.gameObject.SetActive(false);
        }
    }
}
