using UnityEngine;
using System;
using System.Collections;
using Zenject;
using MaouSamaTD.Story;
using MaouSamaTD.UI.Story;

namespace MaouSamaTD.Managers
{
    public class StoryManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StoryUI _storyUI;
        
        public void Setup(StoryUI ui)
        {
            _storyUI = ui;
        }

        [Inject] private GameManager _gameManager;

        public bool IsPlaying { get; private set; }

        private StoryDataSO _currentStory;
        private Action _onComplete;
        private int _currentLineIndex;

        public void PlayStory(StoryDataSO story, Action onComplete)
        {
            if (story == null || story.Lines == null || story.Lines.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _currentStory = story;
            _onComplete = onComplete;
            _currentLineIndex = 0;
            IsPlaying = true;

            _storyUI.Show(true);
            _storyUI.ContinueButton.onClick.RemoveAllListeners();
            _storyUI.ContinueButton.onClick.AddListener(NextLine);

            _gameManager.SetSpeed(0);
            DisplayCurrentLine();
        }

        private void DisplayCurrentLine()
        {
            if (_currentLineIndex < _currentStory.Lines.Count)
            {
                _storyUI.SetLine(_currentStory.Lines[_currentLineIndex]);
            }
            else
            {
                FinishStory();
            }
        }

        public void NextLine()
        {
            _currentLineIndex++;
            DisplayCurrentLine();
        }

        private void FinishStory()
        {
            IsPlaying = false;
            _storyUI.Show(false);
            _storyUI.ContinueButton.onClick.RemoveAllListeners();
            
            _gameManager.SetSpeed(1);
            _onComplete?.Invoke();
        }
    }
}
