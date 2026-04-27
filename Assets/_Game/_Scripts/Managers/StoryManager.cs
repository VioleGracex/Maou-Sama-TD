using UnityEngine;
using System;
using System.Collections;
using Zenject;
using MaouSamaTD.Story;
using MaouSamaTD.Managers;

namespace MaouSamaTD.Managers
{
    public class StoryManager : MonoBehaviour
    {
        [Inject] private MaouSamaTD.UI.Tutorial.DialogueUI _dialogueUI;
        
        [Inject] private GameManager _gameManager;

        public bool IsPlaying { get; private set; }

        public void PlayStory(StoryDataSO story, Action onComplete)
        {
            if (story == null || story.Lines == null || story.Lines.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            IsPlaying = true;
            _gameManager.SetSpeed(0);

            _dialogueUI.ShowStory(story, () => {
                IsPlaying = false;
                _gameManager.SetSpeed(1);
                onComplete?.Invoke();
            });
        }
    }
}
