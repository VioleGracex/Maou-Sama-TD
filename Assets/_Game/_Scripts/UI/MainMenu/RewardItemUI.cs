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
                layout.spacing = 8f; // Beautiful gap between icon and text
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            var fitter = GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            if (_iconImage != null && icon != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = true;
            }
            else if (_iconImage != null)
            {
                _iconImage.enabled = false;
            }

            if (_quantityText != null)
            {
                _quantityText.text = quantity;
                _quantityText.enableWordWrapping = false;
                _quantityText.overflowMode = TextOverflowModes.Overflow;
            }
        }
    }
}
