using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using MaouSamaTD.Tutorial;
using MaouSamaTD.Managers;
using MaouSamaTD.UI;
using Zenject;
using DG.Tweening;

namespace MaouSamaTD.UI.Tutorial
{
    public class DialogueUI : MonoBehaviour
    {
        [Inject] private UIPopupBlocker _uiBlocker;
        [Inject] private GameManager _gameManager;
        [Inject] private TutorialManager _tutorialManager;
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

        [Header("Background Dim")]
        [SerializeField] private CanvasGroup _fullScreenDim;

        private System.Action _onComplete;
        private List<DialogueLine> _currentLines;
        private int _currentIndex;
        private bool _isTyping;
        private float _charsPerSecond = 30f;
        private DialogueBackground _bgType;
        private Tween _typingTween;
        private DialogueStyle _currentStyle;
        private int _lastClickFrame = -1;

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
            Debug.Log("[tutorial] DialogueUI Awake - Initializing listeners...");

            // Ensure Canvas is top-level overlay
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;
            }
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

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

            // Add click-to-advance on panels
            AddPanelClickListener(_fullScreenPanel);
            AddPanelClickListener(_miniTopPanel);
        }

        private void AddPanelClickListener(GameObject panel)
        {
            if (panel == null) return;
            var trigger = panel.GetComponent<EventTrigger>() ?? panel.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener((data) => OnNextClicked());
            trigger.triggers.Add(entry);
        }

        public void ShowDialogue(DialogueData data, System.Action onComplete = null)
        {
            Debug.Log($"[tutorial] ShowDialogue called with style: {data?.Style}");
            if (data == null || data.Lines == null || data.Lines.Count == 0)
            {
                Debug.LogWarning("[tutorial] DialogueData is empty or null!");
                onComplete?.Invoke();
                return;
            }

            _onComplete = onComplete;
            _currentLines = data.Lines;
            _currentIndex = 0;
            _currentStyle = data.Style;
            _bgType = data.Background;
            _charsPerSecond = data.CharactersPerSecond > 0 ? data.CharactersPerSecond : 30f;
            
            _fullScreenPanel?.SetActive(_currentStyle == DialogueStyle.FullScreen);
            _miniTopPanel?.SetActive(_currentStyle == DialogueStyle.MiniTop);

            ApplyBackground();

            var isFull = _currentStyle == DialogueStyle.FullScreen;
            if (_fullNextButton != null) _fullNextButton.gameObject.SetActive(isFull);
            if (_fullSkipButton != null) _fullSkipButton.gameObject.SetActive(isFull);
            if (_miniNextButton != null) _miniNextButton.gameObject.SetActive(!isFull);
            if (_miniSkipButton != null) _miniSkipButton.gameObject.SetActive(!isFull);
            
            CheckAndShowNextLine();
        }

        private void CheckAndShowNextLine()
        {
            if (_currentLines == null || _currentIndex >= _currentLines.Count)
            {
                Hide();
                return;
            }

            var line = _currentLines[_currentIndex];
            if (string.IsNullOrEmpty(line.Text))
            {
                Debug.Log($"[tutorial] Skipping empty line at index {_currentIndex}");
                _currentIndex++;
                CheckAndShowNextLine();
                return;
            }

            ShowLine(line);
        }

        private void ShowLine(DialogueLine line)
        {
            Debug.Log($"[tutorial] ShowLine: {line.SpeakerName} - {line.Text}");
            var speakerText = ActiveSpeakerText;
            var contentText = ActiveContentText;

            if (speakerText != null) speakerText.text = line.SpeakerName;
            if (contentText != null) contentText.text = "";
            
            if (_currentStyle == DialogueStyle.FullScreen)
            {
                // Portraits
                if (line.PortraitOnLeft)
                {
                    if (_leftPortrait != null) 
                    { 
                        _leftPortrait.gameObject.SetActive(line.SpeakerPortrait != null); 
                        _leftPortrait.sprite = line.SpeakerPortrait; 
                    }
                    if (_rightPortrait != null) _rightPortrait.gameObject.SetActive(false);
                }
                else
                {
                    if (_rightPortrait != null) 
                    { 
                        _rightPortrait.gameObject.SetActive(line.SpeakerPortrait != null); 
                        _rightPortrait.sprite = line.SpeakerPortrait; 
                    }
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
                contentText.text = line.Text;
                contentText.maxVisibleCharacters = 0;
                
                float duration = line.Text.Length / _charsPerSecond;
                _typingTween = DOTween.To(() => 0, x => contentText.maxVisibleCharacters = x, line.Text.Length, duration)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true)
                    .OnComplete(() => _isTyping = false);
            }
            else
            {
                _isTyping = false;
            }
        }

        private void ApplyBackground()
        {
            // Reset
            _uiBlocker?.HideBlocker();
            if (_fullScreenDim != null) _fullScreenDim.gameObject.SetActive(false);

            switch (_bgType)
            {
                case DialogueBackground.UIBlocker:
                    RectTransform dialogueRT = _currentStyle == DialogueStyle.FullScreen ? 
                        _fullScreenPanel.GetComponent<RectTransform>() : 
                        _miniTopPanel.GetComponent<RectTransform>();
                        
                    if (_uiBlocker != null)
                    {
                        _uiBlocker.ShowBlockerWithTarget(dialogueRT); // Ensures blocker is shown, targets added
                    }
                    break;
                case DialogueBackground.FullScreenDim:
                    if (_fullScreenDim != null)
                    {
                        _fullScreenDim.gameObject.SetActive(true);
                        _fullScreenDim.alpha = 0;
                        _fullScreenDim.DOFade(0.7f, 0.3f).SetUpdate(true);
                    }
                    break;
            }
        }

        private void OnNextClicked()
        {
            if (Time.frameCount == _lastClickFrame) return;
            _lastClickFrame = Time.frameCount;

            Debug.Log("[tutorial] Next Button Clicked.");
            if (_isTyping)
            {
                _typingTween?.Kill();
                if (ActiveContentText != null) ActiveContentText.maxVisibleCharacters = ActiveContentText.text.Length;
                _isTyping = false;
                return;
            }

            _currentIndex++;
            CheckAndShowNextLine();
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
            
            if (_fullScreenDim != null) _fullScreenDim.gameObject.SetActive(false);
            
            // Clean up our mask impact
            RectTransform dialogueRT = _currentStyle == DialogueStyle.FullScreen ? 
                        _fullScreenPanel.GetComponent<RectTransform>() : 
                        _miniTopPanel.GetComponent<RectTransform>();
            _uiBlocker?.RemoveTarget(dialogueRT);

            // During tutorial, don't hide blocker as TutorialManager controls it
            bool isInTutorial = _tutorialManager != null && _tutorialManager.IsInTutorial;
            if (!isInTutorial)
            {
                _uiBlocker?.HideBlocker();
            }

            this.gameObject.SetActive(false);

            // Safe callback invocation
            var callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }
    }
}
