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
        [SerializeField] private ClassScalingData _classScalingData; // Fallbacks to Resources if null
        
        private UnitDragHandler _dragHandler;
        private UnitData _data;
        private bool _isSelected; 
        [Inject] private DiContainer _container;

        public UnitData Data => _data;

        public void Initialize(UnitData data)
        {
            _data = data;
            gameObject.name = $"UnitButton_{data.UnitName}";
            
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
                // Handled by UnitDragHandler
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
                _unitIcon.sprite = _data.GetSprite(_data.ButtonImageType);
                _unitIcon.enabled = _unitIcon.sprite != null;
            }

            if (_classIcon != null)
            {
                if (_classScalingData == null)
                    _classScalingData = Resources.Load<ClassScalingData>("ClassScalingData");

                if (_classScalingData != null && _classScalingData.TryGetMultipliers(_data.Class, out var multipliers))
                {
                    if (multipliers.ClassIcon != null)
                    {
                        _classIcon.sprite = multipliers.ClassIcon;
                        _classIcon.enabled = true;
                    }
                    else
                    {
                        _classIcon.enabled = false;
                    }
                }
                else
                {
                    _classIcon.enabled = false;
                }
            }

            if (_background != null)
            {
                _background.color = GetClassColor(_data.Class);
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
                case UnitClass.Bastion:
                case UnitClass.Vanguard:
                case UnitClass.Executioner:
                    return new Color(0.8f, 0.4f, 0.4f); // Reddish for Melee/Physical
                case UnitClass.Ranger:
                case UnitClass.Warlock:
                case UnitClass.Gunner:
                    return new Color(0.4f, 0.4f, 0.8f); // Bluish for Ranged/Magic
                case UnitClass.Sage:
                case UnitClass.Support:
                    return new Color(0.4f, 0.8f, 0.4f); // Greenish for Casters/Support
                case UnitClass.Architect:
                case UnitClass.Necromancer:
                case UnitClass.Assassin:
                    return new Color(0.6f, 0.4f, 0.8f); // Purplish for Utility/Special
                default: return Color.white;
            }
        }
    }
}
