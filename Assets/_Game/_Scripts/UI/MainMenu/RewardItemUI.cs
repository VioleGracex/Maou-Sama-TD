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
            // Only add ContentSizeFitter if parent is NOT a full-width vertical group
            var parent = transform.parent;
            bool isFullWidth = parent != null && parent.name == "Container_OneTimeRewards";

            // Ensure no overlap using HorizontalLayoutGroup
            var layout = GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            if (layout != null)
            {
                layout.spacing = 8f; // Beautiful tight gap between icon and text
                layout.padding = new RectOffset(12, 12, 8, 8);
                layout.childAlignment = TextAnchor.MiddleLeft;
                
                // Let the layout group control child widths/heights
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = isFullWidth; // Force expand for full-width cards
                layout.childForceExpandHeight = false;
            }

            var fitter = GetComponent<ContentSizeFitter>();
            if (isFullWidth)
            {
                if (fitter != null)
                {
                    if (Application.isPlaying) Destroy(fitter);
                    else DestroyImmediate(fitter);
                }
            }
            else
            {
                if (fitter == null)
                {
                    fitter = gameObject.AddComponent<ContentSizeFitter>();
                }
                if (fitter != null)
                {
                    fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
            }

            if (_iconImage != null && icon != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = true;
                
                var iconLe = _iconImage.GetComponent<LayoutElement>();
                if (iconLe == null) iconLe = _iconImage.gameObject.AddComponent<LayoutElement>();
                iconLe.minWidth = 20f;
                iconLe.preferredWidth = 20f;
                iconLe.minHeight = 20f; // Let height be controlled by parent
                iconLe.preferredHeight = 20f;
            }
            else if (_iconImage != null)
            {
                _iconImage.enabled = false;
            }

            if (_quantityText != null)
            {
                _quantityText.text = quantity;
                _quantityText.enableWordWrapping = false; // Prevent unwanted wrapping on badges
                _quantityText.fontSize = 14f; // compact font size
                _quantityText.fontStyle = FontStyles.Bold;
                _quantityText.color = Color.white;
                
                var qtyLe = _quantityText.GetComponent<LayoutElement>();
                if (qtyLe == null) qtyLe = _quantityText.gameObject.AddComponent<LayoutElement>();
                qtyLe.flexibleWidth = 1f;
            }
        }
    }
}
