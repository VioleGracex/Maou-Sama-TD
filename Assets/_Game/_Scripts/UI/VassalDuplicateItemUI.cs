using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Data;
using System;

namespace MaouSamaTD.UI
{
    public class VassalDuplicateItemUI : MonoBehaviour
    {
        [SerializeField] private Image _unitIcon;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private GameObject _selectionMarker;
        [SerializeField] private Button _btnSelect;

        private UnitInventoryEntry _entry;
        private Action<UnitInventoryEntry> _onSelect;

        private void Awake()
        {
            // Auto-wire if not set in inspector
            if (!_unitIcon) _unitIcon = transform.Find("BG/Portrait")?.GetComponent<Image>();
            if (!_levelText) _levelText = transform.Find("BG/TopArea/LevelText")?.GetComponent<TextMeshProUGUI>();
            if (!_selectionMarker) _selectionMarker = transform.Find("SelectionOverlay")?.gameObject;
            if (!_btnSelect) _btnSelect = GetComponent<Button>();
        }

        public void Setup(UnitInventoryEntry entry, Sprite icon, Action<UnitInventoryEntry> onSelect)
        {
            _entry = entry;
            _onSelect = onSelect;

            if (_unitIcon) _unitIcon.sprite = icon;
            if (_levelText) _levelText.text = $"Lv.{entry.Level}";
            
            if (_btnSelect)
            {
                _btnSelect.onClick.RemoveAllListeners();
                _btnSelect.onClick.AddListener(() => _onSelect?.Invoke(_entry));
            }
            
            SetSelected(false);
        }

        public void SetSelected(bool isSelected)
        {
            if (_selectionMarker) _selectionMarker.SetActive(isSelected);
        }
    }
}
