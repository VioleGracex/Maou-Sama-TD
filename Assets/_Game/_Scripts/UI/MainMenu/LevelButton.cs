using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Levels;
using System;
using UnityEngine.EventSystems;

namespace MaouSamaTD.UI.MainMenu
{
    [Serializable]
    public struct LevelDisplayData
    {
        public LevelData Level;
        public int Index;
        public bool IsLocked;
        public int StarCount;

        public string LevelID => Level != null ? Level.LevelID : string.Empty;
        public int Version => (IsLocked ? 1 : 0) ^ StarCount;
    }

    public class LevelButton : MonoBehaviour, MaouSamaTD.UI.Common.IListItem<LevelDisplayData>, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI _levelNameText;
        [SerializeField] private TextMeshProUGUI _levelNumberText; // e.g. "01"
        [SerializeField] private GameObject _lockedOverlay;
        [SerializeField] private GameObject[] _stars; // Array of star objects (e.g., 3 stars)
        [SerializeField] private Image[] _starImages; // Array of 3 star Image components
        [SerializeField] private Sprite _starFullSprite;  // UI_Icon_Star_Full
        [SerializeField] private Sprite _starEmptySprite; // UI_Icon_Star_Empty
        [SerializeField] private Button _button;
        
        private LevelDisplayData _displayData;
        private Action<LevelData> _onClick;

        private Image _nodeGlow;
        private Color _baseGlowColor;
        private bool _isSelected;
        private bool _isHovered;

        public LevelData LevelDataForCallback => _displayData.Level;
        public bool IsLocked => _displayData.IsLocked;

        // IListItem implementation
        public string GetContentID() => _displayData.LevelID;
        public int GetContentVersion() => _displayData.Version;

        public static string FormatLevelID(string levelID)
        {
            if (string.IsNullOrEmpty(levelID)) return levelID;
            
            if (levelID.StartsWith("1-"))
            {
                string suffix = levelID.Substring(2);
                if (int.TryParse(suffix, out int index))
                {
                    if (index <= 3)
                    {
                        return $"0-{index}";
                    }
                    else
                    {
                        return $"1-{index - 3}";
                    }
                }
            }
            return levelID;
        }

        public void Setup(LevelDisplayData data, Action<UnityEngine.Component> onClick = null)
        {
            if (onClick != null) _onClick = (levelData) => onClick(this);
            
            _displayData = data;
            var level = data.Level;
            
            if (_levelNameText != null) 
                _levelNameText.text = level.LevelName.ToUpper();
            
            // Dynamic resolving if not assigned
            if (_levelNumberText == null)
            {
                _levelNumberText = transform.Find("StageNum_Text")?.GetComponent<TextMeshProUGUI>();
                if (_levelNumberText == null)
                {
                    _levelNumberText = transform.Find("Canvas/StageNum_Text")?.GetComponent<TextMeshProUGUI>();
                }
            }

            if (_levelNumberText != null)
            {
                _levelNumberText.text = FormatLevelID(level.LevelID);
            }
            
            if (_lockedOverlay != null) 
                _lockedOverlay.SetActive(data.IsLocked);
            
            if (_button != null)
            {
                _button.interactable = !data.IsLocked;
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClicked);

                // Add hover colors for the node circle
                var colors = _button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.6f, 1f, 1f, 1f); // Cyan highlight on hover
                colors.pressedColor = new Color(0.4f, 0.8f, 1f, 1f);
                colors.selectedColor = new Color(0.5f, 0.9f, 1f, 1f);
                _button.colors = colors;
            }

            // Dynamic resolving for star images if not assigned
            if (_starImages == null || _starImages.Length == 0)
            {
                var starHolder = transform.Find("Stage_StarHolder");
                if (starHolder == null)
                {
                    starHolder = transform.Find("Canvas/Stage_StarHolder");
                }

                if (starHolder != null)
                {
                    starHolder.gameObject.SetActive(true);
                    var imgList = new System.Collections.Generic.List<Image>();
                    for (int i = 1; i <= 3; i++)
                    {
                        var starTrans = starHolder.Find($"Star_{i}");
                        if (starTrans != null)
                        {
                            var img = starTrans.GetComponent<Image>();
                            if (img != null) imgList.Add(img);
                        }
                    }
                    if (imgList.Count > 0)
                    {
                        _starImages = imgList.ToArray();
                    }
                }
            }

            // 1. If _starImages is empty but _stars is not, populate _starImages from _stars!
            if ((_starImages == null || _starImages.Length == 0) && _stars != null && _stars.Length > 0)
            {
                var imgList = new System.Collections.Generic.List<Image>();
                foreach (var starObj in _stars)
                {
                    if (starObj != null)
                    {
                        var img = starObj.GetComponent<Image>();
                        if (img != null) imgList.Add(img);
                    }
                }
                _starImages = imgList.ToArray();
            }

            // 2. Ensure all star GameObjects are ALWAYS active (so empty stars are visible),
            // and control the sprite on the Image components!
            if (_stars != null)
            {
                for (int i = 0; i < _stars.Length; i++)
                {
                    if (_stars[i] != null)
                    {
                        _stars[i].SetActive(true); // Keep them active so empty stars are visible!
                    }
                }
            }

            if (_starImages != null)
            {
                for (int i = 0; i < _starImages.Length; i++)
                {
                    if (_starImages[i] != null)
                    {
                        _starImages[i].gameObject.SetActive(true);
                        
                        bool isEarned = (i < data.StarCount);
                        
                        // Only override the sprite if we actually found the loaded sprites
                        if (_starFullSprite != null && _starEmptySprite != null)
                        {
                            _starImages[i].sprite = isEarned ? _starFullSprite : _starEmptySprite;
                            _starImages[i].color = Color.white; // Reset color so it doesn't tint the sprite yellow
                        }
                        else
                        {
                            // If no sprites were loaded via code, keep the prefab's sprite but tint it
                            _starImages[i].color = isEarned ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                        }
                    }
                }
            }
        }

        // Legacy Setup for compatibility if needed, but we'll try to refactor everything
        public void Setup(LevelData data, int index, bool isLocked, int starCount, Action<LevelData> onClick)
        {
            _onClick = onClick;
            Setup(new LevelDisplayData { Level = data, Index = index, IsLocked = isLocked, StarCount = starCount });
        }

        public void SetGlow(Image glowImg, Color baseColor)
        {
            _nodeGlow = glowImg;
            _baseGlowColor = baseColor;
            UpdateGlowColor();
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateGlowColor();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable) return;
            _isHovered = true;
            UpdateGlowColor();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable) return;
            _isHovered = false;
            UpdateGlowColor();
        }

        private void UpdateGlowColor()
        {
            if (_nodeGlow == null) return;
            
            if (_isSelected)
            {
                // Inspect / Selected color: Bright Gold/Yellow
                _nodeGlow.color = new Color(1f, 0.8f, 0.1f, 1f);
            }
            else if (_isHovered)
            {
                // Hover color: Bright Cyan/White
                _nodeGlow.color = new Color(0.8f, 1f, 1f, 0.9f);
            }
            else
            {
                // Default category color
                _nodeGlow.color = _baseGlowColor;
            }
        }

        private void OnClicked()
        {
            _onClick?.Invoke(_displayData.Level);
        }
    }
}
