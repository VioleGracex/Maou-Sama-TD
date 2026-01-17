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
        [SerializeField] private Image _unitIcon;       // The sprite of the unit
        [SerializeField] private Image _classIcon;      // The icon for the class
        [SerializeField] private Image _background;     // The button background
        [SerializeField] private Image _cooldownOverlay;// Displays cooldown progress (Fill 1 -> 0)
        [SerializeField] private TextMeshProUGUI _nameText; 
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button _button;

        private UnitDragHandler _dragHandler;
        private UnitData _data;
        [Inject] private DiContainer _container;

        public UnitData Data => _data;

        public void Initialize(UnitData data)
        {
            _data = data;
            _dragHandler = GetComponent<UnitDragHandler>();
            _button = GetComponent<Button>();
            _background = GetComponent<Image>();

            // Ensure DragHandler exists and init it
            if (_dragHandler == null)
            {
                _dragHandler = gameObject.AddComponent<UnitDragHandler>();
                // Dynamic AddComponent needs manual injection!
                 if (_container != null) _container.Inject(_dragHandler);
            }
            // If it existed, it was already injected by InstantiatePrefab
            
            _dragHandler.Initialize(data);
            
            // Wire Button Click to DragHandler (since Button consumes clicks)
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => 
                {
                    if (_dragHandler != null) _dragHandler.OnPointerClick(null);
                });
            }

            // Auto-find components if missing (helper for upgrading prefabs)
            if (_nameText == null)
            {
                var t = transform.Find("NameText");
                if (t != null) _nameText = t.GetComponent<TextMeshProUGUI>();
            }
            if (_costText == null)
            {
                var t = transform.Find("CostText");
                if (t != null) _costText = t.GetComponent<TextMeshProUGUI>();
            }
            if (_unitIcon == null)
            {
                var t = transform.Find("UnitIcon");
                if (t != null) _unitIcon = t.GetComponent<Image>();
            }
            if (_classIcon == null)
            {
                var t = transform.Find("ClassIcon");
                if (t != null) _classIcon = t.GetComponent<Image>();
            }
            if (_cooldownOverlay == null)
            {
                var t = transform.Find("CooldownOverlay");
                if (t != null) _cooldownOverlay = t.GetComponent<Image>();
            }

            // Fallback for legacy "InfoText"
            if (_nameText == null && _costText == null)
            {
                 var oldText = GetComponentInChildren<TextMeshProUGUI>();
                 if (oldText != null) _nameText = oldText; // Reuse as name/combo fallback
            }
            
            // Assume the main Image component is the background
            if (_background == null) _background = GetComponent<Image>();

            UpdateVisuals();
            UpdateCooldown(0); // Init hidden
        }

        private void UpdateVisuals()
        {
            if (_data == null) return;

            // Update Text
            if (_nameText != null) _nameText.text = _data.UnitName;
            if (_costText != null) _costText.text = _data.DeploymentCost.ToString();

            // Legacy fallback if only one text field exists
            if (_nameText != null && _costText == null)
            {
                 _nameText.text = $"<b>{_data.UnitName}</b>\n<color=yellow>{_data.DeploymentCost}</color>";
                 _nameText.alignment = TextAlignmentOptions.Center;
            }

            // Update Unit Icon
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

            // Update Class Icon (Placeholder logic for now, or could load from Resources/AssetDatabase if we had a manager)
            // For now, enable if sprite is manually assigned, otherwise disable
            if (_classIcon != null)
            {
                 // Logic to set sprite based on class would go here
                 // _classIcon.sprite = ...
                 // _classIcon.enabled = _classIcon.sprite != null;
            }

            // Update Background Color based on Class
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

        public void UpdateState(bool canAfford, bool isDeployed, bool isCoolingDown)
        {
            // Interactable condition
            // Cannot interact if: Deployed OR Cooldown active
            // Afford check matters for visual dimming, but interactability is blocked by cooldown too.
            
            bool isBusy = isDeployed || isCoolingDown;
            bool isInteractable = !isBusy && canAfford;

            if (_button != null) _button.interactable = isInteractable;
            if (_dragHandler != null) _dragHandler.SetInteractable(isInteractable);

            // Visual Feedback
            if (_background != null)
            {
                Color baseColor = GetClassColor(_data.Class);

                if (isDeployed)
                {
                    _background.color = Color.gray;
                    if (_unitIcon != null) _unitIcon.color = Color.gray; 
                }
                else if (isCoolingDown)
                {
                     // Maybe distinct visual for cooldown? 
                     // Or just rely on overlay + disabled state
                     _background.color = baseColor * 0.7f; 
                     if (_unitIcon != null) _unitIcon.color = Color.gray; 
                }
                else if (!canAfford)
                {
                    _background.color = baseColor * 0.5f; // Dimmed
                    if (_unitIcon != null) _unitIcon.color = Color.white; 
                }
                else
                {
                    _background.color = baseColor; // Normal
                    if (_unitIcon != null) _unitIcon.color = Color.white;
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
