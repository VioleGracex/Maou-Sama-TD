using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MaouSamaTD.UI.Vassals
{
    /// <summary>
    /// UI controller for a single skin card in the vassal detail skins tab.
    /// Manages name title, portrait, and state indicators (equipped/locked).
    /// </summary>
    public class SkinCardUI : MonoBehaviour
    {
        [Header("State Indicators")]
        [SerializeField] private TextMeshProUGUI _skinNameText;
        [SerializeField] private Image           _portraitImage;
        [SerializeField] private GameObject      _equippedRoot;
        [SerializeField] private GameObject      _lockedRoot;

        [Header("Price Display")]
        [SerializeField] private GameObject      _priceRoot;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private Image           _currencyIcon;

        [Header("Settings")]
        [SerializeField] private Color _lockedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _ownedColor  = Color.white;

        [Header("Input")]
        [SerializeField] private Button _btnClickOverlay;

        public event System.Action<SkinCardUI> OnCardClicked;

        private void Awake()
        {
            if (_btnClickOverlay) _btnClickOverlay.onClick.AddListener(() => OnCardClicked?.Invoke(this));
        }

        /// <summary>
        /// Sets the visual state of the skin card.
        /// </summary>
        /// <param name="skinName">Name of the skin (e.g. 'Pool Party').</param>
        /// <param name="sprite">The avatar/portrait sprite.</param>
        /// <param name="isEquipped">Whether this skin is currently equipped.</param>
        /// <param name="isLocked">Whether the user does NOT own this skin yet.</param>
        /// <param name="price">Price of the skin.</param>
        /// <param name="isPremium">Whether it costs BloodCrest instead of Gold.</param>
        public void SetState(string skinName, Sprite sprite, bool isEquipped, bool isLocked, int price = 0, bool isPremium = false)
        {
            if (_skinNameText)   _skinNameText.text = skinName?.ToUpper();
            if (_portraitImage)
            {
                _portraitImage.sprite = sprite;
                _portraitImage.color  = isLocked ? _lockedColor : _ownedColor;
            }

            // PER USER: "if locked Equipped_Indicator_Root turn off"
            if (_equippedRoot) _equippedRoot.SetActive(isEquipped && !isLocked);
            if (_lockedRoot)   _lockedRoot.SetActive(isLocked);
            
            if (_priceText) _priceText.text = price > 0 ? price.ToString() : "FREE";
            
            // Show price if not refined yet or if user wants it always visible on card
            if (_priceRoot) _priceRoot.SetActive(price > 0 && isLocked);
        }

        public void SetHighlighted(bool isActive)
        {
            // Per User: "hide on side, show only on active"
            // If locked, we might want to show price even if not focused, 
            // but for now we follow the SetState logic.
        }

        public void SetEquipped(bool isEquipped)
        {
            // Only show equipped if it's actually unlocked
            bool curLocked = _lockedRoot != null && _lockedRoot.activeSelf;
            if (_equippedRoot) _equippedRoot.SetActive(isEquipped && !curLocked);
        }

        public void SetLocked(bool isLocked)
        {
            if (_lockedRoot) _lockedRoot.SetActive(isLocked);
            // PER USER: "fix price is turned off and when unlocked"
            if (!isLocked && _priceRoot) _priceRoot.SetActive(false);

            if (_portraitImage) _portraitImage.color = isLocked ? _lockedColor : _ownedColor;
        }
    }
}
