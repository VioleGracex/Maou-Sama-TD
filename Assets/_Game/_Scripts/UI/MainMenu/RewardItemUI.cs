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
            }
        }
    }
}
