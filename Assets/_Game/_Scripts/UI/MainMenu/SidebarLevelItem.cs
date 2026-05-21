using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using MaouSamaTD.Levels;

namespace MaouSamaTD.UI.MainMenu
{
    public class SidebarLevelItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private Image _accentBar;
        [SerializeField] private Button _button;

        [Header("Star References")]
        [SerializeField] private Image[] _stars;
        [SerializeField] private Sprite _starFullSprite;
        [SerializeField] private Sprite _starEmptySprite;

        public void Setup(LevelData level, bool isUnlocked, bool isPlaced, bool isCompleted, Action onClick)
        {
            if (_nameText != null)
            {
                _nameText.text = $"{LevelButton.FormatLevelID(level.LevelID)} {level.LevelName}";
                _nameText.color = isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.7f);
                
                var nameRect = _nameText.GetComponent<RectTransform>();
                if (nameRect != null)
                {
                    nameRect.offsetMax = new Vector2(-65f, nameRect.offsetMax.y);
                }
            }

            if (_statusText != null)
            {
                _statusText.enableWordWrapping = false;
                _statusText.overflowMode = TextOverflowModes.Overflow;
                var rectTrans = _statusText.GetComponent<RectTransform>();
                if (rectTrans != null)
                {
                    rectTrans.sizeDelta = new Vector2(60f, 30f);
                }

                string statusString = "";
                if (isCompleted)
                {
                    statusString = "<color=#FFD700>Done</color>"; // Gold check representation
                }
                else if (!isUnlocked)
                {
                    statusString = "<color=#777777>[L]</color>"; // Lock representation
                }
                else
                {
                    statusString = isPlaced ? "<color=#FFD700>></color>" : ""; // Placed (Gold Triangle) vs Unplaced (Dot)
                }
                _statusText.text = statusString;
            }

            if (_accentBar != null)
            {
                // Accent bar color uses Gold/Crimson theme
                _accentBar.color = isPlaced ? new Color(0.97f, 0.79f, 0.14f, 0.9f) : new Color(0.5f, 0.5f, 0.5f, 0.3f);
            }

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => onClick?.Invoke());
            }

            // High-fidelity Star Ratings rendering in Sidebar
            if (_stars != null && _stars.Length > 0)
            {
                int starsCount = 0;
                if (isUnlocked)
                {
                    var saveManager = FindObjectOfType<MaouSamaTD.Managers.SaveManager>();
                    if (saveManager != null && saveManager.CurrentData != null)
                    {
                        if (saveManager.CurrentData.LevelStars != null)
                        {
                            var starData = saveManager.CurrentData.LevelStars.Find(s => s.LevelID == level.LevelID);
                            if (starData.LevelID != null)
                            {
                                starsCount = starData.Stars;
                            }
                        }
                        
                        if (starsCount == 0 && saveManager.CurrentData.CompletedLevels != null && saveManager.CurrentData.CompletedLevels.Contains(level.LevelID))
                        {
                            starsCount = 3;
                        }
                    }
                }

                for (int i = 0; i < _stars.Length; i++)
                {
                    if (_stars[i] != null)
                    {
                        _stars[i].gameObject.SetActive(isUnlocked);
                        if (isUnlocked)
                        {
                            _stars[i].sprite = (i < starsCount) ? _starFullSprite : _starEmptySprite;
                        }
                    }
                }
            }
        }
    }
}
