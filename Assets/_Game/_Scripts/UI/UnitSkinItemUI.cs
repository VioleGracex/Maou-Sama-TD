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
        [SerializeField] private GameObject _selectionHighlight;

        public void Setup(UnitSkinData skin, Action onSelect)
        {
            // If skin is null, it's the default skin
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => onSelect?.Invoke());
            }
        }
        
        public void SetIcon(Sprite sprite)
        {
            if (_icon) _icon.sprite = sprite;
        }

        public void SetSelected(bool isSelected)
        {
            if (_selectionHighlight) _selectionHighlight.SetActive(isSelected);
        }
    }
}
