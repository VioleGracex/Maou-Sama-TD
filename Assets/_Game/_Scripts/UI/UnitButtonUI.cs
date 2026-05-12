using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Units;
using Zenject;
using DG.Tweening;
using UnityEngine.EventSystems;

namespace MaouSamaTD.UI
{
    public class UnitButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Visual Settings")]
        [SerializeField] private Color _deployedBgColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color _deployedIconColor = new Color(0.7f, 0.7f, 0.7f, 0.9f);
        [SerializeField] private float _cooldownBgMultiplier = 0.6f;
        [SerializeField] private Color _cooldownIconColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private Color _insufficientFundsBgColor = new Color(0.4f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color _insufficientFundsIconColor = new Color(0.5f, 0.35f, 0.35f, 0.9f);
        [SerializeField] private Color _selectedBgColor = Color.green;
        [SerializeField] private Color _selectedIconColor = Color.yellow;
        [SerializeField] private Color _idleIconColor = Color.white;
        
        [Header("References")]
        [SerializeField] private Image _unitIcon;       
        [SerializeField] private Image _classIcon;      
        [SerializeField] private Image _background;     
        [SerializeField] private Image _cooldownOverlay;
        [SerializeField] private TextMeshProUGUI _nameText; 
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button _button;
        [SerializeField] private Button _retreatButton;
        [SerializeField] private ClassScalingData _classScalingData; // Fallbacks to Resources if null
        [SerializeField] private Slider _hpSlider;
        
        private UnitDragHandler _dragHandler;
        private UnitData _data;
        private bool _isSelected; 
        private bool _lastCanAfford = true;
        private bool _lastIsDeployed = false;
        private bool _lastIsCoolingDown = false;
        private bool _isHovered = false;
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
                _button.transition = Selectable.Transition.None; 
            }

            if (_retreatButton != null)
            {
                _retreatButton.onClick.RemoveAllListeners();
                _retreatButton.onClick.AddListener(OnRetreatButtonClicked);
                RefreshRetreatButton();
            }

            if (_background == null) _background = GetComponent<Image>();

            UpdateVisuals();
            UpdateCooldown(0); 

            // Smooth pop-in animation, even when Time.timeScale = 0
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.4f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
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
                var imageType = _data.ButtonImageType;
                
                // Check if we have a global override active in the UnitDatabase
                if (MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase != null && 
                    MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase.UseGlobalButtonOverride)
                {
                    imageType = MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase.GlobalButtonImageType;
                }

                _unitIcon.sprite = _data.GetSprite(imageType);
                _unitIcon.enabled = _unitIcon.sprite != null;
            }

            if (_classIcon != null)
            {
                if (_classScalingData == null)
                    _classScalingData = MaouSamaTD.Core.AppEntryPoint.LoadedScalingData;

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
            UpdateState(_lastCanAfford, _lastIsDeployed, _lastIsCoolingDown);
        }

        public void UpdateState(bool canAfford, bool isDeployed, bool isCoolingDown)
        {
            _lastCanAfford = canAfford;
            _lastIsDeployed = isDeployed;
            _lastIsCoolingDown = isCoolingDown;

            bool isBusy = isDeployed || isCoolingDown;
            bool isInteractable = !isBusy && canAfford;

            if (_button != null) _button.interactable = isInteractable;
            if (_dragHandler != null) _dragHandler.SetInteractable(isInteractable);

            // Differentiate cost text color
            if (_costText != null)
            {
                // Vibrant red for better visibility
                _costText.color = canAfford ? Color.white : new Color(1f, 0.2f, 0.2f);
            }

            if (_background != null)
            {
                Color baseColor = GetClassColor(_data.Class);

                if (isCoolingDown)
                {
                    _background.color = baseColor * _cooldownBgMultiplier; 
                    if (_unitIcon != null) _unitIcon.color = _cooldownIconColor; 
                }
                else if (isDeployed)
                {
                    // Already Deployed
                    _background.color = _deployedBgColor;
                    if (_unitIcon != null) _unitIcon.color = _deployedIconColor; 
                }
                else
                {
                    if (!canAfford)
                    {
                        // Insufficient Seals
                        _background.color = _insufficientFundsBgColor; 
                        if (_unitIcon != null) _unitIcon.color = _insufficientFundsIconColor;
                    }
                    else
                    {
                        _background.color = _isSelected ? _selectedBgColor : baseColor; 
                        if (_unitIcon != null)
                        {
                            _unitIcon.color = _isSelected ? _selectedIconColor : _idleIconColor;
                        }
                    }
                }
            }
            
            RefreshRetreatButton();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            RefreshRetreatButton();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            RefreshRetreatButton();
        }

        private void RefreshRetreatButton()
        {
            if (_retreatButton != null)
            {
                _retreatButton.gameObject.SetActive(_lastIsDeployed && _isHovered);
            }
        }

        private void OnRetreatButtonClicked()
        {
            if (_data == null) return;
            
            // Find the active unit instance to retreat
            // This usually requires a reference to the active units in the scene
            // DeploymentUI or InteractionManager usually tracks this.
            
            // For now, we'll use a static message or find it via tag/type
            // Better: DeploymentUI has OnUnitRetreated, but we need to tell it WHICH instance.
            // Actually, PlayerUnit has OnRetreat event.
            
            var deploymentUI = Object.FindAnyObjectByType<DeploymentUI>();
            if (deploymentUI != null)
            {
                deploymentUI.RetreatUnitByData(_data);
            }
        }

        public void UpdateHpSlider(float ratio)
        {
            if (_hpSlider != null)
            {
                _hpSlider.value = ratio;
                _hpSlider.gameObject.SetActive(true);
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
