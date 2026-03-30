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
        [SerializeField] private Image _portraitImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Image _classIconImage;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private RectTransform _starsContainer;
        [SerializeField] private Button _button;

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
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnClick);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_button != null) _button.enabled = interactable;
            
            // Set all background images to raycastTarget = interactable
            // This allows clicks to "pass through" to the button behind if disabled
            if (_portraitImage) _portraitImage.raycastTarget = interactable;
            if (_classIconImage) _classIconImage.raycastTarget = interactable;
            
            // Background image usually on VisualRoot or this object
            var img = GetComponent<Image>();
            if (img != null) img.raycastTarget = interactable;
            
            if (_visualRoot != null)
            {
                var rootImg = _visualRoot.GetComponent<Image>();
                if (rootImg != null) rootImg.raycastTarget = interactable;
            }
        }

        public void Setup(UnitData unit, Action<UnityEngine.Component> onClick = null)
        {
            _onClickCallback = onClick != null ? (card) => onClick(card) : null;
            InternalSetup(unit);
        }

        /// <summary>
        /// Explicit setup for a "None" or "Removal" card with an icon override.
        /// </summary>
        public void SetupNone(Sprite icon, Action<UnityEngine.Component> onClick = null)
        {
            _onClickCallback = onClick != null ? (card) => onClick(card) : null;
            InternalSetup(null, icon);
        }

        [Obsolete("Use Setup(unit, onClick) instead. ClassScalingData is now handled by UnitData (Source of Truth).")]
        public void Setup(UnitData unit, ClassScalingData scalingData, Action<UnityEngine.Component> onClick = null)
        {
            Setup(unit, onClick);
        }
    
        private void InternalSetup(UnitData unit, Sprite iconOverride = null)
        {
            _data = unit;
            UpdateVisuals(unit, iconOverride);
        }

        public void UpdateVisuals(UnitData unit, Sprite iconOverride = null)
        {
            if (_visualRoot != null) 
            {
                _visualRoot.SetActive(true);
            }
            else 
            {
                Debug.LogWarning($"[UnitCardUI] {gameObject.name} is missing _visualRoot reference!");
            }

            // Determine which icon to use for the None state
            Sprite displayIcon = iconOverride;

            // NONE / REMOVAL special case
            if (unit == null)
            {
                if (_nameText) _nameText.text = ""; 
                
                if (_portraitImage) 
                {
                    _portraitImage.sprite = displayIcon;
                    _portraitImage.gameObject.SetActive(displayIcon != null);
                    _portraitImage.preserveAspect = true;
                    _portraitImage.raycastTarget = false;
                }
                
                if (_classIconImage) _classIconImage.gameObject.SetActive(false);
                if (_levelText) _levelText.gameObject.SetActive(false);
                
                if (_starsContainer != null)
                {
                    for (int i = 0; i < _starsContainer.childCount; i++)
                        _starsContainer.GetChild(i).gameObject.SetActive(false);
                }
                
                SetSelectionState(-1);
                return;
            }

            var stats = unit.CalculatedStats;

            // Visuals
            if (_nameText) _nameText.text = unit.UnitName;
            if (_portraitImage) 
            {
                var portrait = unit.GetSprite(UnitData.UnitImageType.WaistUp);
                _portraitImage.sprite = portrait;
                _portraitImage.gameObject.SetActive(portrait != null);
                
                string vrStatus = (_visualRoot != null) ? $"Root:{_visualRoot.activeSelf}" : "Root:MISSING";
                Debug.Log($"[UnitCardUI] {gameObject.name} (ID:{gameObject.GetInstanceID()}) Setup '{unit.UnitName}'. {vrStatus}, Portrait:{(portrait != null ? portrait.name : "NULL")}");
            }

            if (_classIconImage)
            {
                _classIconImage.sprite = stats.ClassIcon;
                _classIconImage.gameObject.SetActive(stats.ClassIcon != null);
            }

            if (_levelText) 
            {
                _levelText.text = $"LV. {unit.Level}";
                _levelText.gameObject.SetActive(true);
            }
            
            if (_starsContainer != null)
            {
                int starCount = (int)unit.Rarity + 1;
                for (int i = 0; i < _starsContainer.childCount; i++)
                {
                    _starsContainer.GetChild(i).gameObject.SetActive(i < starCount);
                }
            }
            
            SetSelectionState(-1);
        }

        public void SetSelectionState(int selectionIndex, bool showCheckmark = true)
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

            if (_checkMarkIcon) _checkMarkIcon.SetActive(isSelected && showCheckmark);
        }

        private void OnClick()
        {
            _onClickCallback?.Invoke(this);
        }
    }
}
