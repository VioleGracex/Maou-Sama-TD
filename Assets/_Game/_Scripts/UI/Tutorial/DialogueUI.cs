using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using MaouSamaTD.Tutorial;
using MaouSamaTD.Managers;
using MaouSamaTD.UI;
using MaouSamaTD.Story;
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
        [SerializeField] private Image _middlePortrait;
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

        [Header("Background")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private CanvasGroup _fullScreenDim;

        private System.Action _onComplete;
        private List<DialogueLine> _currentLines;
        private List<MaouSamaTD.Story.StoryLine> _currentStoryLines;
        private bool _isStoryMode;
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
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 3000; // Much higher than blocker (999)
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
            _currentStoryLines = null;
            _isStoryMode = false;
            _currentIndex = 0;
            _currentStyle = data.Style;
            _charsPerSecond = data.CharactersPerSecond > 0 ? data.CharactersPerSecond : 30f;
            
            _fullScreenPanel?.SetActive(_currentStyle == DialogueStyle.FullScreen);
            _miniTopPanel?.SetActive(_currentStyle == DialogueStyle.MiniTop);

            ApplyBackground(DialogueBackground.None);

            var isFull = _currentStyle == DialogueStyle.FullScreen;
            if (_fullNextButton != null) _fullNextButton.gameObject.SetActive(isFull);
            if (_fullSkipButton != null) _fullSkipButton.gameObject.SetActive(isFull);
            if (_miniNextButton != null) _miniNextButton.gameObject.SetActive(!isFull);
            if (_miniSkipButton != null) _miniSkipButton.gameObject.SetActive(!isFull);
            
            CheckAndShowNextLine();
        }

        public void ShowStory(StoryDataSO data, System.Action onComplete = null)
        {
            Debug.Log($"[story] ShowStory called: {data?.name}");
            if (data == null || data.Lines == null || data.Lines.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _onComplete = onComplete;
            _currentLines = null;
            _currentStoryLines = data.Lines;
            _isStoryMode = true;
            _currentIndex = 0;
            _currentStyle = DialogueStyle.FullScreen;
            _bgType = DialogueBackground.FullScreenDim; // Default to dim for story intro
            _charsPerSecond = 30f;

            _fullScreenPanel?.SetActive(true);
            _miniTopPanel?.SetActive(false);

            ApplyBackground(DialogueBackground.FullScreenDim);

            if (_fullNextButton != null) _fullNextButton.gameObject.SetActive(true);
            if (_fullSkipButton != null) _fullSkipButton.gameObject.SetActive(true);

            CheckAndShowNextLine();
        }

        private void CheckAndShowNextLine()
        {
            if (_isStoryMode)
            {
                if (_currentStoryLines == null || _currentIndex >= _currentStoryLines.Count)
                {
                    Hide();
                    return;
                }
                ShowStoryLine(_currentStoryLines[_currentIndex]);
            }
            else
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
        }

        private void ShowStoryLine(MaouSamaTD.Story.StoryLine line)
        {
            Debug.Log($"[story] ShowLine: {line.SpeakerName}");
            var speakerText = ActiveSpeakerText;
            var contentText = ActiveContentText;

            if (speakerText != null) speakerText.text = line.SpeakerName;
            
            if (_backgroundImage != null)
            {
                _backgroundImage.sprite = line.Background;
                _backgroundImage.enabled = line.Background != null;
            }

            ApplyBackground(line.BackgroundOverlay);

            // Portraits
            UpdatePortrait(_leftPortrait, line.PortraitLeft, line.Focus == MaouSamaTD.Story.PortraitFocus.Left || line.Focus == MaouSamaTD.Story.PortraitFocus.All);
            UpdatePortrait(_middlePortrait, line.PortraitMiddle, line.Focus == MaouSamaTD.Story.PortraitFocus.Middle || line.Focus == MaouSamaTD.Story.PortraitFocus.All);
            UpdatePortrait(_rightPortrait, line.PortraitRight, line.Focus == MaouSamaTD.Story.PortraitFocus.Right || line.Focus == MaouSamaTD.Story.PortraitFocus.All);

            StartTyping(line.DialogueText);
        }

        private void UpdatePortrait(Image image, Sprite sprite, bool isFocused)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.gameObject.SetActive(sprite != null);
            if (sprite != null)
            {
                image.color = isFocused ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }

        private void StartTyping(string text)
        {
            var contentText = ActiveContentText;
            if (contentText != null)
            {
                _isTyping = true;
                contentText.text = text;
                contentText.maxVisibleCharacters = 0;
                
                float duration = text.Length / _charsPerSecond;
                _typingTween = DOTween.To(() => 0, x => contentText.maxVisibleCharacters = x, text.Length, duration)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true)
                    .OnComplete(() => _isTyping = false);
            }
            else
            {
                _isTyping = false;
            }
        }

        private void ShowLine(DialogueLine line)
        {
            Debug.Log($"[tutorial] ShowLine: {line.SpeakerName} - {line.Text}");
            var speakerText = ActiveSpeakerText;
            var contentText = ActiveContentText;

            if (speakerText != null) speakerText.text = line.SpeakerName;
            
            ApplyBackground(line.Background);

            if (_currentStyle == DialogueStyle.FullScreen)
            {
                // Portraits
                UpdatePortrait(_leftPortrait, line.LeftPortrait, line.Focus == MaouSamaTD.Tutorial.PortraitFocus.Left || line.Focus == MaouSamaTD.Tutorial.PortraitFocus.All);
                
                UpdatePortrait(_middlePortrait, line.CenterPortrait, line.Focus == MaouSamaTD.Tutorial.PortraitFocus.Center || line.Focus == MaouSamaTD.Tutorial.PortraitFocus.All);
                UpdatePortrait(_rightPortrait, line.RightPortrait, line.Focus == MaouSamaTD.Tutorial.PortraitFocus.Right || line.Focus == MaouSamaTD.Tutorial.PortraitFocus.All);
            }
            else
            {
                if (_miniTopPortrait != null)
                {
                    // MiniTop usually only shows one. Let's use Center, then Left, then Right as priority.
                    Sprite s = line.CenterPortrait != null ? line.CenterPortrait : (line.LeftPortrait != null ? line.LeftPortrait : line.RightPortrait);
                    _miniTopPortrait.gameObject.SetActive(s != null);
                    _miniTopPortrait.sprite = s;
                }
            }

            StartTyping(line.Text);
        }

        private void ApplyBackground(DialogueBackground type)
        {
            _bgType = type;
            // If we are in a tutorial, TutorialManager handles the blocker state and targets.
            // DialogueUI should not interfere with it to avoid clearing world highlights or causing flickering.
            if (_tutorialManager != null && _tutorialManager.IsInTutorial)
            {
                if (_backgroundImage != null) _backgroundImage.enabled = false;
                return;
            }

            // Reset
            if (_bgType != DialogueBackground.UIBlocker)
            {
                _uiBlocker?.HideBlocker();
            }
            
            if (_backgroundImage != null) _backgroundImage.enabled = false;

            switch (_bgType)
            {
                case DialogueBackground.UIBlocker:
                    RectTransform dialogueRT = _currentStyle == DialogueStyle.FullScreen ? 
                        _fullScreenPanel.GetComponent<RectTransform>() : 
                        _miniTopPanel.GetComponent<RectTransform>();
                        
                    if (_uiBlocker != null)
                    {
                        _uiBlocker.ShowBlockerWithTarget(dialogueRT);
                    }
                    break;
                case DialogueBackground.FullScreenDim:
                    if (_fullScreenDim != null)
                    {
                        _fullScreenDim.gameObject.SetActive(true);
                        var img = _fullScreenDim.GetComponent<UnityEngine.UI.Image>();
                        if (img != null) img.color = Color.black;
                        _fullScreenDim.alpha = 0;
                        _fullScreenDim.DOFade(0.7f, 0.3f).SetUpdate(true);
                    }
                    break;
            }
        }

        public void OnNextClicked()
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
