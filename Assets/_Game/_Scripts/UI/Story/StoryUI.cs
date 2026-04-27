using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Story;

namespace MaouSamaTD.UI.Story
{
    public class StoryUI : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] private GameObject _storyPanel;
        
        [Header("Background")]
        [SerializeField] private Image _backgroundImage;
        
        [Header("Portraits")]
        [SerializeField] private Image _portraitLeft;
        [SerializeField] private Image _portraitMiddle;
        [SerializeField] private Image _portraitRight;
        
        [Header("Dialogue Box")]
        [SerializeField] private TextMeshProUGUI _speakerNameText;
        [SerializeField] private TextMeshProUGUI _dialogueText;
        [SerializeField] private Button _continueButton;

        public void Setup(GameObject panel, Image bg, Image left, Image middle, Image right, TextMeshProUGUI speaker, TextMeshProUGUI dialogue, Button btn)
        {
            _storyPanel = panel;
            _backgroundImage = bg;
            _portraitLeft = left;
            _portraitMiddle = middle;
            _portraitRight = right;
            _speakerNameText = speaker;
            _dialogueText = dialogue;
            _continueButton = btn;
        }

        [Header("Settings")]
        [SerializeField] private Color _activeColor = Color.white;
        [SerializeField] private Color _inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        public void Show(bool show)
        {
            _storyPanel.SetActive(show);
        }

        public void SetLine(StoryLine line)
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.sprite = line.Background;
                _backgroundImage.enabled = line.Background != null;
            }

            UpdatePortrait(_portraitLeft, line.PortraitLeft, line.Focus == PortraitFocus.Left || line.Focus == PortraitFocus.All);
            UpdatePortrait(_portraitMiddle, line.PortraitMiddle, line.Focus == PortraitFocus.Middle || line.Focus == PortraitFocus.All);
            UpdatePortrait(_portraitRight, line.PortraitRight, line.Focus == PortraitFocus.Right || line.Focus == PortraitFocus.All);

            _speakerNameText.text = line.SpeakerName;
            _dialogueText.text = line.DialogueText;
        }

        private void UpdatePortrait(Image image, Sprite sprite, bool isFocused)
        {
            if (image == null) return;
            
            image.sprite = sprite;
            image.enabled = sprite != null;
            
            if (sprite != null)
            {
                image.color = isFocused ? _activeColor : _inactiveColor;
            }
        }

        public void OnContinueClicked()
        {
            // This will be called by the Manager
        }
        
        public Button ContinueButton => _continueButton;
    }
}
