using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Units; // Assuming UnitData is here
using System;

namespace MaouSamaTD.UI.MainMenu
{
    public class UnitCardUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private Image _portraitImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Image _classIconImage;
        [SerializeField] private TextMeshProUGUI _levelText;

        [Header("Selection Visuals")]
        [SerializeField] private GameObject _selectedOverlay; // The dark overlay when selected
        [SerializeField] private TextMeshProUGUI _selectionOrderText; // "1", "2", "3"
        [SerializeField] private GameObject _checkMarkIcon; // Optional checkmark

        private UnitData _data;
        private Action<UnitCardUI> _onClickCallback;

        public UnitData Data => _data;

        private void Start()
        {
            Button btn = GetComponent<Button>();
            if (btn)
            {
                btn.onClick.AddListener(OnClick);
            }
        }

        public void Setup(UnitData unit, Action<UnitCardUI> onClick)
        {
            _data = unit;
            _onClickCallback = onClick;

            // Visuals
            if (_nameText) _nameText.text = unit.UnitName;
            if (_portraitImage) 
            {
                _portraitImage.sprite = unit.UnitIcon; // Or UnitSprite if you prefer full body
                _portraitImage.gameObject.SetActive(unit.UnitIcon != null);
            }
            // If you have class icons, set them here
            // if (_classIconImage) _classIconImage.sprite = ...
            
            // Default State
            SetSelectionState(-1);
        }

        public void SetSelectionState(int selectionIndex)
        {
            bool isSelected = selectionIndex >= 0;

            if (_selectedOverlay) _selectedOverlay.SetActive(isSelected);
            
            if (_selectionOrderText)
            {
                _selectionOrderText.gameObject.SetActive(isSelected);
                if (isSelected)
                {
                    _selectionOrderText.text = (selectionIndex + 1).ToString();
                }
            }

            if (_checkMarkIcon) _checkMarkIcon.SetActive(isSelected);
        }

        private void OnClick()
        {
            _onClickCallback?.Invoke(this);
        }
    }
}
