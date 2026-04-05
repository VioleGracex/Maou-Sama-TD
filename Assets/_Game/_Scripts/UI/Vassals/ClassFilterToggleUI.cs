using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MaouSamaTD.UI.Vassals
{
    public class ClassFilterToggleUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("Components")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _classIconImage;
        [SerializeField] private TextMeshProUGUI _allLabel;

        [Header("Colors (Default mapped for easy setup)")]
        [SerializeField] private Color _activeColor = new Color(0.24f, 0.61f, 0.9f, 1f);
        [SerializeField] private Color _inactiveColor = new Color(0.15f, 0.15f, 0.15f, 1f);

        public System.Action OnClicked;
        private bool _isActive;

        public void Setup(Sprite icon, string label)
        {
            if (_classIconImage)
            {
                _classIconImage.sprite = icon;
                _classIconImage.enabled = icon != null;
                _classIconImage.preserveAspect = true;
            }

            if (_allLabel)
            {
                _allLabel.text = label ?? string.Empty;
                _allLabel.enabled = !string.IsNullOrEmpty(label);
            }
        }

        public void SetActiveState(bool isActive)
        {
            _isActive = isActive;
            if (_backgroundImage)
            {
                _backgroundImage.color = _isActive ? _activeColor : _inactiveColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked?.Invoke();
        }
    }
}
