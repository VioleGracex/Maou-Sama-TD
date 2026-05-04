using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MaouSamaTD.Managers;
using MaouSamaTD.UI;
using MaouSamaTD.UI.Tutorial;
using MaouSamaTD.Skills;
using MaouSamaTD.Units;
using UnityEngine.UI;
using System.Linq;

namespace MaouSamaTD.Testing
{
    public class TutorialAutoPlayer : MonoBehaviour
    {
        [Header("Settings")]
        public bool AutoPlayOnStart = false;
        public bool IsAutoPlaying = false;
        public float ActionDelay = 1.0f;
        public float DialogueDelay = 0.5f;

        private TutorialManager _tutorialManager;
        private DialogueManager _dialogueManager;
        private StoryManager _storyManager;
        private InteractionManager _interactionManager;
        private SkillManager _skillManager;
        private DeploymentUI _deploymentUI;
        private GameManager _gameManager;

        private Coroutine _playRoutine;

        private void Start()
        {
            _tutorialManager = FindFirstObjectByType<TutorialManager>();
            _dialogueManager = FindFirstObjectByType<DialogueManager>();
            _storyManager = FindFirstObjectByType<StoryManager>();
            _interactionManager = FindFirstObjectByType<InteractionManager>();
            _skillManager = FindFirstObjectByType<SkillManager>();
            _deploymentUI = FindFirstObjectByType<DeploymentUI>();
            _gameManager = FindFirstObjectByType<GameManager>();

            if (AutoPlayOnStart)
            {
                StartAutoPlay();
            }
        }

        public void StartAutoPlay()
        {
            if (_playRoutine != null) StopCoroutine(_playRoutine);
            _playRoutine = StartCoroutine(AutoPlayRoutine());
            Debug.Log("[AutoPlayer] Tutorial Auto-Play Started.");
        }

        private IEnumerator AutoPlayRoutine()
        {
            while (_tutorialManager != null && _tutorialManager.IsInTutorial)
            {
                // 1. Handle Dialogue / Story
                bool dialogueActive = (_dialogueManager != null && _dialogueManager.IsDialogueActive) ||
                                     (_storyManager != null && _storyManager.IsPlaying);

                if (dialogueActive)
                {
                    yield return new WaitForSeconds(DialogueDelay);
                    Debug.Log("[AutoPlayer] Advancing Dialogue/Story...");
                    
                    // Find the DialogueUI via manager if possible, or search for it
                    var dialogueUI = FindFirstObjectByType<MaouSamaTD.UI.Tutorial.DialogueUI>();
                    if (dialogueUI != null && dialogueUI.gameObject.activeInHierarchy)
                    {
                        dialogueUI.OnNextClicked();
                    }
                }

                // 2. Handle Actions
                string actionKey = _tutorialManager.GetCurrentStepActionKey();
                if (!string.IsNullOrEmpty(actionKey) && _tutorialManager.IsWaitingForAction(actionKey))
                {
                    yield return new WaitForSeconds(ActionDelay);
                    Debug.Log($"[AutoPlayer] Executing Action: {actionKey}");
                    ExecuteTutorialAction(actionKey);
                }

                yield return new WaitForSeconds(0.5f);
            }
            
            Debug.Log("[AutoPlayer] Tutorial Auto-Play Finished.");
        }

        private void ExecuteTutorialAction(string actionKey)
        {
            switch (actionKey)
            {
                case "UnitPlaced":
                    SimulateUnitPlacement();
                    break;
                case "RiteMenuOpened":
                    SimulateRiteMenuOpen();
                    break;
                case "SkillUsed":
                    SimulateSkillUsage();
                    break;
                default:
                    // If we don't know how to simulate it, just trigger it to skip
                    _tutorialManager.OnActionTriggered(actionKey);
                    break;
            }
        }

        private void SimulateUnitPlacement()
        {
            // Find allowed tiles from TutorialManager
            var tiles = _tutorialManager.GetRequiredPlacementTiles();
            if (tiles != null && tiles.Count > 0)
            {
                // In a real simulation we'd drag the button to the tile.
                // For this tester, we just trigger the success.
                _tutorialManager.OnActionTriggered("UnitPlaced");
            }
        }

        private void SimulateRiteMenuOpen()
        {
            // Find the toggle button
            var toggle = GameObject.Find("SovereignRiteToggle")?.GetComponent<Button>();
            if (toggle != null) toggle.onClick.Invoke();
            else _tutorialManager.OnActionTriggered("RiteMenuOpened");
        }

        private void SimulateSkillUsage()
        {
            // This is complex as it requires targeting.
            // For the purpose of the automatic tester, we bypass the click and trigger the action.
            _tutorialManager.OnActionTriggered("SkillUsed");
        }
    }
}
