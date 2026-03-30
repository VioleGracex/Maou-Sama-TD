using UnityEngine;
using UnityEngine.UI;
using System;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Item UI for a single skin in the selection scroll.
    /// handles thumbnail display and selection callbacks.
    /// </summary>
    public class UnitSkinItemUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Button _button;
        [SerializeField] private TMPro.TextMeshProUGUI _fullNameText;
        [SerializeField] private GameObject _selectionIcon; // Marks "Currently Equipped"

        [SerializeField] private CanvasGroup _canvasGroup;

        public void Setup(string characterName, string themeName, Sprite icon, Action onSelect)
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (_icon) _icon.sprite = icon;
            if (_fullNameText) _fullNameText.text = string.IsNullOrEmpty(themeName) ? characterName : $"{themeName} {characterName}";

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => onSelect?.Invoke());
            }
        }
        
        public void SetEquippedStatus(bool isEquipped)
        {
            if (_selectionIcon) _selectionIcon.SetActive(isEquipped);
        }
    }
}
