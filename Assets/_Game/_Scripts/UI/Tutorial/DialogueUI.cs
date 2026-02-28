using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MaouSamaTD.Tutorial;
using Zenject;
using DG.Tweening;

namespace MaouSamaTD.UI.Tutorial
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("Full Screen Layout")]
        [SerializeField] private GameObject _fullScreenPanel;
        [SerializeField] private TextMeshProUGUI _fullSpeakerText;
        [SerializeField] private TextMeshProUGUI _fullContentText;
        [SerializeField] private Image _leftPortrait;
        [SerializeField] private Image _rightPortrait;

        [Header("Mini Top Layout")]
        [SerializeField] private GameObject _miniTopPanel;
        [SerializeField] private TextMeshProUGUI _miniTopSpeakerText;
        [SerializeField] private TextMeshProUGUI _miniTopContentText;
        [SerializeField] private Image _miniTopPortrait;

        [Header("Full Screen Controls")]
        [SerializeField] private Button _fullNextButton;
        [SerializeField] private Button _fullSkipButton;

        [Header("Mini Top Controls")]
        [SerializeField] private Button _miniNextButton;
        [SerializeField] private Button _miniSkipButton;

        private System.Action _onComplete;
        private List<DialogueLine> _currentLines;
        private int _currentIndex;
        private bool _isTyping;
        private string _fullText;
        private Tween _typingTween;
        private DialogueStyle _currentStyle;

        private TextMeshProUGUI ActiveContentText 
        {
            get
            {
                if (_currentStyle == DialogueStyle.MiniTop) return _miniTopContentText;
                return _fullContentText;
            }
        }

        private TextMeshProUGUI ActiveSpeakerText 
        {
            get
            {
                if (_currentStyle == DialogueStyle.MiniTop) return _miniTopSpeakerText;
                return _fullSpeakerText;
            }
        }

        private void Awake()
        {
            Debug.Log("[DialogueUI] Awake - Initializing listeners...");
            if (_fullScreenPanel != null) _fullScreenPanel.SetActive(false);
            if (_miniTopPanel != null) _miniTopPanel.SetActive(false);
            
            if (_fullNextButton != null) 
            {
                _fullNextButton.onClick.RemoveAllListeners();
                _fullNextButton.onClick.AddListener(OnNextClicked);
            }
            if (_fullSkipButton != null) 
            {
                _fullSkipButton.onClick.RemoveAllListeners();
                _fullSkipButton.onClick.AddListener(SkipAll);
            }

            if (_miniNextButton != null) 
            {
                _miniNextButton.onClick.RemoveAllListeners();
                _miniNextButton.onClick.AddListener(OnNextClicked);
            }
            if (_miniSkipButton != null) 
            {
                _miniSkipButton.onClick.RemoveAllListeners();
                _miniSkipButton.onClick.AddListener(SkipAll);
            }
        }

        public void ShowDialogue(DialogueData data, System.Action onComplete = null)
        {
            Debug.Log($"[DialogueUI] ShowDialogue called with style: {data?.Style}");
            if (data == null || data.Lines == null || data.Lines.Count == 0)
            {
                Debug.LogWarning("[DialogueUI] DialogueData is empty or null!");
                onComplete?.Invoke();
                return;
            }

            _onComplete = onComplete;
            _currentLines = data.Lines;
            _currentIndex = 0;
            _currentStyle = data.Style;
            
            _fullScreenPanel?.SetActive(_currentStyle == DialogueStyle.FullScreen);
            _miniTopPanel?.SetActive(_currentStyle == DialogueStyle.MiniTop);

            var isFull = _currentStyle == DialogueStyle.FullScreen;
            if (_fullNextButton != null) _fullNextButton.gameObject.SetActive(isFull);
            if (_fullSkipButton != null) _fullSkipButton.gameObject.SetActive(isFull);
            if (_miniNextButton != null) _miniNextButton.gameObject.SetActive(!isFull);
            if (_miniSkipButton != null) _miniSkipButton.gameObject.SetActive(!isFull);
            
            ShowLine(_currentLines[_currentIndex]);
        }

        private void ShowLine(DialogueLine line)
        {
            Debug.Log($"[DialogueUI] ShowLine: {line.SpeakerName} - {line.Text}");
            var speakerText = ActiveSpeakerText;
            var contentText = ActiveContentText;

            if (speakerText != null) speakerText.text = line.SpeakerName;
            _fullText = line.Text;
            if (contentText != null) contentText.text = "";
            
            if (_currentStyle == DialogueStyle.FullScreen)
            {
                // Portraits
                if (line.PortraitOnLeft)
                {
                    if (_leftPortrait != null) { _leftPortrait.gameObject.SetActive(true); _leftPortrait.sprite = line.SpeakerPortrait; }
                    if (_rightPortrait != null) _rightPortrait.gameObject.SetActive(false);
                }
                else
                {
                    if (_rightPortrait != null) { _rightPortrait.gameObject.SetActive(true); _rightPortrait.sprite = line.SpeakerPortrait; }
                    if (_leftPortrait != null) _leftPortrait.gameObject.SetActive(false);
                }
            }
            else
            {
                if (_miniTopPortrait != null)
                {
                    _miniTopPortrait.gameObject.SetActive(line.SpeakerPortrait != null);
                    _miniTopPortrait.sprite = line.SpeakerPortrait;
                }
            }

            // Typing effect
            if (contentText != null)
            {
                _isTyping = true;
                _typingTween = DOTween.To(() => 0, x => contentText.text = _fullText.Substring(0, x), _fullText.Length, 0.5f)
                    .SetEase(Ease.Linear)
                    .OnComplete(() => _isTyping = false);
            }
            else
            {
                _isTyping = false;
            }
        }

        private void OnNextClicked()
        {
            Debug.Log("[DialogueUI] Next Button Clicked.");
            if (_isTyping)
            {
                _typingTween?.Kill();
                if (ActiveContentText != null) ActiveContentText.text = _fullText;
                _isTyping = false;
                return;
            }

            _currentIndex++;
            if (_currentIndex < _currentLines.Count)
            {
                ShowLine(_currentLines[_currentIndex]);
            }
            else
            {
                Hide();
            }
        }

        private void SkipAll()
        {
            Hide();
        }

        private void Hide()
        {
            _fullScreenPanel?.SetActive(false);
            _miniTopPanel?.SetActive(false);
            if (_fullNextButton != null) _fullNextButton.gameObject.SetActive(false);
            if (_fullSkipButton != null) _fullSkipButton.gameObject.SetActive(false);
            if (_miniNextButton != null) _miniNextButton.gameObject.SetActive(false);
            if (_miniSkipButton != null) _miniSkipButton.gameObject.SetActive(false);
            _onComplete?.Invoke();
            _onComplete = null;
        }
    }
}
