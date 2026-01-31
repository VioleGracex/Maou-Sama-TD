using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Units;
using Zenject;

namespace MaouSamaTD.UI
{
    public class UnitButtonUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _unitIcon;       
        [SerializeField] private Image _classIcon;      
        [SerializeField] private Image _background;     
        [SerializeField] private Image _cooldownOverlay;
        [SerializeField] private TextMeshProUGUI _nameText; 
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button _button;
        
        private UnitDragHandler _dragHandler;
        private UnitData _data;
        private bool _isSelected; 
        [Inject] private DiContainer _container;

        public UnitData Data => _data;

        public void Initialize(UnitData data)
        {
            _data = data;
            _dragHandler = GetComponent<UnitDragHandler>();
            _button = GetComponent<Button>();
            _background = GetComponent<Image>();

            if (_dragHandler == null)
            {
                _dragHandler = gameObject.AddComponent<UnitDragHandler>();
                 if (_container != null) _container.Inject(_dragHandler);
            }
            
            _dragHandler.Initialize(data);
            
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => 
                {
                    if (_dragHandler != null) _dragHandler.OnPointerClick(null);
                });
            }

            if (_background == null) _background = GetComponent<Image>();

            UpdateVisuals();
            UpdateCooldown(0); 
        }

        private void UpdateVisuals()
        {
            if (_data == null) return;

            if (_nameText != null) _nameText.text = _data.UnitName;
            if (_costText != null) _costText.text = _data.DeploymentCost.ToString();

            if (_nameText != null && _costText == null)
            {
                 _nameText.text = $"<b>{_data.UnitName}</b>\n<color=yellow>{_data.DeploymentCost}</color>";
                 _nameText.alignment = TextAlignmentOptions.Center;
            }

            if (_unitIcon != null)
            {
                if (_data.UnitIcon != null)
                {
                    _unitIcon.sprite = _data.UnitIcon;
                    _unitIcon.enabled = true;
                }
                else
                {
                    _unitIcon.sprite = _data.UnitSprite;
                    _unitIcon.enabled = _data.UnitSprite != null;
                }
            }

            if (_classIcon != null)
            {
//
            }

            if (_background != null)
            {
                if (_data.Class == UnitClass.Melee) _background.color = new Color(0.8f, 0.4f, 0.4f);
                else if (_data.Class == UnitClass.Ranged) _background.color = new Color(0.4f, 0.4f, 0.8f);
                else if (_data.Class == UnitClass.Healer) _background.color = new Color(0.4f, 0.8f, 0.4f);
            }
        }

        public void UpdateCooldown(float progress)
        {
            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.fillAmount = progress;
                _cooldownOverlay.enabled = progress > 0;
            }
        }

        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            
            if (_background != null)
            {
                _background.color = isSelected ? Color.green : Color.white; 
            }
            
            if (_unitIcon != null)
            {
               if (isSelected) _unitIcon.color = Color.yellow;
               else _unitIcon.color = Color.white;
            }
        }

        public void UpdateState(bool canAfford, bool isDeployed, bool isCoolingDown)
        {
            bool isBusy = isDeployed || isCoolingDown;
            bool isInteractable = !isBusy && canAfford;

            if (_button != null) _button.interactable = isInteractable;
            if (_dragHandler != null) _dragHandler.SetInteractable(isInteractable);

            if (_background != null)
            {
                Color baseColor = GetClassColor(_data.Class);

                if (isCoolingDown)
                {
                     _background.color = baseColor; 
                     if (_unitIcon != null) _unitIcon.color = Color.gray; 
                }
                else if (isDeployed)
                {
                    _background.color = Color.gray;
                    if (_unitIcon != null) _unitIcon.color = Color.gray; 
                }
                else
                {
                    if (!canAfford)
                    {
                        _background.color = baseColor * 0.5f; 
                    }
                    else
                    {
                        _background.color = baseColor; 
                    }

                    if (_unitIcon != null)
                    {
                        if (_isSelected) _unitIcon.color = Color.yellow;
                        else if (!canAfford) _unitIcon.color = Color.white; 
                        else _unitIcon.color = Color.white;
                    }
                }
            }
        }

        private Color GetClassColor(UnitClass unitClass)
        {
            switch (unitClass)
            {
                case UnitClass.Melee: return new Color(0.8f, 0.4f, 0.4f);
                case UnitClass.Ranged: return new Color(0.4f, 0.4f, 0.8f);
                case UnitClass.Healer: return new Color(0.4f, 0.8f, 0.4f);
                default: return Color.white;
            }
        }
    }
}
