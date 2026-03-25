using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Units; // Assuming UnitData is here
using System;

namespace MaouSamaTD.UI.MainMenu
{
    public class UnitCardUI : MonoBehaviour, MaouSamaTD.UI.Common.IListItem<UnitData>
    {
        [Header("General")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private UnitData _data;

        [Header("Data")]
        [SerializeField] private ClassScalingData _classScalingData;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Image _classIconImage;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private RectTransform _starsContainer;

        [Header("Selection Visuals")]
        [SerializeField] private GameObject _selectedOverlay; // The dark overlay when selected
        [SerializeField] private TextMeshProUGUI _selectionOrderText; // "1", "2", "3"
        [SerializeField] private GameObject _checkMarkIcon; // Optional checkmark

        private Action<UnitCardUI> _onClickCallback;

        public UnitData Data => _data;

        // IListItem implementation
        public string GetContentID() => _data != null ? _data.UniqueID : string.Empty;
        public int GetContentVersion() => _data != null ? (_data.Level ^ _data.StarRating.GetHashCode() ^ _data.Experience) : 0;

        private void Start()
        {
            Button btn = GetComponent<Button>();
            if (btn)
            {
                btn.onClick.AddListener(OnClick);
            }
        }

        public void Setup(UnitData unit, Action<UnityEngine.Component> onClick = null)
        {
            _onClickCallback = onClick != null ? (card) => onClick(card) : null;
            InternalSetup(unit);
        }

        private void InternalSetup(UnitData unit)
        {
            Debug.Log($"[UnitCardUI] Setup called for unit: {(unit != null ? unit.UnitName : "NULL")}.");
            _data = unit;

            if (_visualRoot != null)
            {
                _visualRoot.SetActive(unit != null);
            }

            if (unit == null)
            {
                if (_starsContainer != null)
                {
                    for (int i = 0; i < _starsContainer.childCount; i++)
                        _starsContainer.GetChild(i).gameObject.SetActive(false);
                }
                
                SetSelectionState(-1);
                return;
            }

            // Visuals
            if (_nameText) _nameText.text = unit.UnitName;
            if (_portraitImage) 
            {
                _portraitImage.sprite = unit.GetSprite(unit.CardSlotImageType);
                _portraitImage.gameObject.SetActive(_portraitImage.sprite != null);
            }
            if (_classIconImage)
            {
                if (_classScalingData == null)
                    _classScalingData = Resources.Load<ClassScalingData>("ClassScalingData");

                if (_classScalingData != null && _classScalingData.TryGetMultipliers(unit.Class, out var multipliers))
                {
                    if (multipliers.ClassIcon != null)
                    {
                        _classIconImage.sprite = multipliers.ClassIcon;
                        _classIconImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        _classIconImage.gameObject.SetActive(false);
                    }
                }
                else
                {
                    _classIconImage.gameObject.SetActive(false);
                }
            }
            if (_levelText) 
            {
                _levelText.text = $"LV. {unit.Level}";
                _levelText.gameObject.SetActive(true);
            }
            
            if (_starsContainer != null)
            {
                int starCount = (int)unit.Rarity + 1; // Rarity is 0-indexed enum
                for (int i = 0; i < _starsContainer.childCount; i++)
                {
                    _starsContainer.GetChild(i).gameObject.SetActive(i < starCount);
                }
            }
            
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
