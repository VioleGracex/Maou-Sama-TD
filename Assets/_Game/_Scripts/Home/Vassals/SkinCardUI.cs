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

        [Header("Settings")]
        [SerializeField] private Color _lockedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _ownedColor  = Color.white;

        /// <summary>
        /// Sets the visual state of the skin card.
        /// </summary>
        /// <param name="skinName">Name of the skin (e.g. 'Pool Party').</param>
        /// <param name="sprite">The avatar/portrait sprite.</param>
        /// <param name="isEquipped">Whether this skin is currently equipped.</param>
        /// <param name="isLocked">Whether the user does NOT own this skin yet.</param>
        /// <param name="price">Price of the skin.</param>
        public void SetState(string skinName, Sprite sprite, bool isEquipped, bool isLocked, int price = 0)
        {
            if (_skinNameText)   _skinNameText.text = skinName?.ToUpper();
            if (_portraitImage)
            {
                _portraitImage.sprite = sprite;
                _portraitImage.color  = isLocked ? _lockedColor : _ownedColor;
            }

            if (_equippedRoot) _equippedRoot.SetActive(isEquipped);
            if (_lockedRoot)   _lockedRoot.SetActive(isLocked);
            
            if (_priceText) _priceText.text = price > 0 ? price.ToString() : "FREE";
            
            // Hide price by default, only shown when highlighted
            if (_priceRoot) _priceRoot.SetActive(false);
        }

        public void SetHighlighted(bool isActive)
        {
            if (_priceRoot) _priceRoot.SetActive(isActive);
            
            // Per User: "hide on side, show only on active"
            // This GameObject toggle handles that requirement.
        }
    }
}
