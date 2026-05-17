using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MaouSamaTD.UI.Treasury
{
    public class VaultItemCardUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _quantityText;
        [SerializeField] private Button _cardButton;
        [SerializeField] private CanvasGroup _canvasGroup;

        public Image BackgroundImage => _backgroundImage;
        public Image IconImage => _iconImage;
        public TextMeshProUGUI NameText => _nameText;
        public TextMeshProUGUI QuantityText => _quantityText;
        public Button CardButton => _cardButton;

        public void Setup(string name, Sprite icon, int quantity, Color bgColor, System.Action onClickAction)
        {
            if (_nameText != null) _nameText.text = name;
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0.2f);
            }
            if (_quantityText != null) _quantityText.text = $"{quantity}";
            if (_backgroundImage != null) _backgroundImage.color = bgColor;

            if (_cardButton != null)
            {
                _cardButton.onClick.RemoveAllListeners();
                if (onClickAction != null)
                {
                    _cardButton.onClick.AddListener(() => onClickAction.Invoke());
                }
            }

            // Translucent/grayed-out if owned quantity is 0
            float targetAlpha = (quantity <= 0) ? 0.5f : 1.0f;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = targetAlpha;
            }
            else
            {
                var cg = gameObject.GetComponent<CanvasGroup>();
                if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
                cg.alpha = targetAlpha;
            }
        }
    }
}
