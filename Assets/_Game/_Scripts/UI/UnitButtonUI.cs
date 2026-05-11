using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Units;
using Zenject;
using DG.Tweening;

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
        [SerializeField] private Button _retreatButton;
        [SerializeField] private ClassScalingData _classScalingData; // Fallbacks to Resources if null
        [SerializeField] private Slider _hpSlider;
        
        private UnitDragHandler _dragHandler;
        private UnitData _data;
        private bool _isSelected; 
        private bool _lastCanAfford = true;
        private bool _lastIsDeployed = false;
        private bool _lastIsCoolingDown = false;
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
                _retreatButton.gameObject.SetActive(false);
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
                _costText.color = canAfford ? Color.white : Color.red;
            }

            if (_background != null)
            {
                Color baseColor = GetClassColor(_data.Class);

                if (isCoolingDown)
                {
                    _background.color = baseColor * 0.6f; 
                    if (_unitIcon != null) _unitIcon.color = new Color(0.4f, 0.4f, 0.4f, 1f); 
                }
                else if (isDeployed)
                {
                    // Already Deployed: Desaturated Gray
                    _background.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                    if (_unitIcon != null) _unitIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.7f); 
                    if (_retreatButton != null) _retreatButton.gameObject.SetActive(true);
                }
                else
                {
                    if (_retreatButton != null) _retreatButton.gameObject.SetActive(false);
                    if (!canAfford)
                    {
                        // Insufficient Seals: Darkened/Reddish Tint
                        _background.color = new Color(0.4f, 0.15f, 0.15f, 1f); 
                        if (_unitIcon != null) _unitIcon.color = new Color(0.5f, 0.35f, 0.35f, 0.9f);
                    }
                    else
                    {
                        _background.color = _isSelected ? Color.green : baseColor; 
                        if (_unitIcon != null)
                        {
                            if (_isSelected) _unitIcon.color = Color.yellow;
                            else _unitIcon.color = Color.white;
                        }
                    }
                }
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
            if (_hpSlider == null)
            {
                CreateProceduralHpSlider();
            }

            if (_hpSlider != null)
            {
                _hpSlider.value = ratio;
                _hpSlider.gameObject.SetActive(true);
            }
        }

        private void CreateProceduralHpSlider()
        {
            GameObject sliderObj = new GameObject("HP_Slider_Procedural", typeof(RectTransform));
            sliderObj.transform.SetParent(this.transform, false);
            
            RectTransform rect = sliderObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0f);
            rect.anchorMax = new Vector2(0.95f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 4f);
            rect.sizeDelta = new Vector2(0f, 6f);

            Image bgImage = sliderObj.AddComponent<Image>();
            bgImage.color = new Color(0.12f, 0.12f, 0.14f, 0.85f);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
            fillObj.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.sizeDelta = Vector2.zero;

            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.1f, 0.85f, 0.55f, 0.95f);

            _hpSlider = sliderObj.AddComponent<Slider>();
            _hpSlider.interactable = false;
            _hpSlider.transition = Selectable.Transition.None;
            _hpSlider.navigation = new Navigation { mode = Navigation.Mode.None };
            _hpSlider.fillRect = fillRect;
            _hpSlider.minValue = 0f;
            _hpSlider.maxValue = 1f;
            _hpSlider.value = 1f;
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
