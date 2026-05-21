using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MaouSamaTD.UI.MainMenu
{
    public class RewardItemUI : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _quantityText;

        public void Setup(Sprite icon, string quantity)
        {
            // Ensure no overlap using HorizontalLayoutGroup programmatically
            var layout = GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            if (layout != null)
            {
                layout.spacing = 6f; // Beautiful tight gap between icon and text
                layout.padding = new RectOffset(6, 6, 4, 4);
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            // Destroy ContentSizeFitter so that parent ScrollRect/Content layouts can force custom sizes
            var fitter = GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                if (Application.isPlaying) Destroy(fitter);
                else DestroyImmediate(fitter);
            }

            if (_iconImage != null && icon != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = true;
                
                var iconRect = _iconImage.rectTransform;
                if (iconRect != null)
                {
                    iconRect.sizeDelta = new Vector2(22f, 22f); // neat compact icon size
                }
            }
            else if (_iconImage != null)
            {
                _iconImage.enabled = false;
            }

            if (_quantityText != null)
            {
                _quantityText.text = quantity;
                _quantityText.enableWordWrapping = true;
                _quantityText.lineSpacing = 0.85f;
                _quantityText.overflowMode = TextOverflowModes.Overflow;
                _quantityText.fontSize = 11f; // compact font size
                
                var qtyRect = _quantityText.rectTransform;
                if (qtyRect != null)
                {
                    qtyRect.sizeDelta = new Vector2(36f, 20f); // clean tight text box bounds
                }
            }
        }
    }
}
