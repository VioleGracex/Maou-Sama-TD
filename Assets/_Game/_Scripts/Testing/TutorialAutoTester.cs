using UnityEngine;
using System.Collections;
using System.Linq;
using MaouSamaTD.Managers;
using MaouSamaTD.UI.Skills;
using MaouSamaTD.UI.Tutorial;
using UnityEngine.UI;

namespace MaouSamaTD.Testing
{
    public class TutorialAutoTester : MonoBehaviour
    {
        private TutorialManager _tutorialManager;
        private GameManager _gameManager;
        private InteractionManager _interactionManager;
        private DialogueManager _dialogueManager;
        
        public bool IsAutoPlaying = false;

        private void Start()
        {
            _tutorialManager = FindFirstObjectByType<TutorialManager>();
            _gameManager = FindFirstObjectByType<GameManager>();
            _interactionManager = FindFirstObjectByType<InteractionManager>();
            _dialogueManager = FindFirstObjectByType<DialogueManager>();
        }

        private void Update()
        {
            if (!IsAutoPlaying) return;

            // Auto-advance dialogue
            if (_dialogueManager != null && _dialogueManager.gameObject.activeInHierarchy)
            {
                // Let's assume there is a Next() or clicking the screen advances dialogue
                // Usually we can just simulate a click on the dialogue blocker/panel.
                // If it's a UI button, we can find it.
                var btn = _dialogueManager.GetComponentInChildren<Button>();
                if (btn != null && btn.interactable)
                {
                    btn.onClick.Invoke();
                }
            }

            if (_tutorialManager == null || !_tutorialManager.IsInTutorial) return;

            string action = _tutorialManager.GetCurrentStepActionKey();
            if (!string.IsNullOrEmpty(action) && _tutorialManager.IsWaitingForAction(action))
            {
                StartCoroutine(ExecuteAction(action));
            }
        }
        
        private IEnumerator ExecuteAction(string actionKey)
        {
            if (!_tutorialManager.IsWaitingForAction(actionKey)) yield break;
            
            yield return new WaitForSeconds(0.5f); // small delay to mimic human reaction

            if (actionKey == "UnitPlaced")
            {
                // Find what to place
                var reqs = _tutorialManager.GetRequiredPlacementTiles();
                if (reqs != null && reqs.Count > 0)
                {
                    var targetCoord = reqs[0];
                    // Just pretend we dragged it and placed it
                    if (_interactionManager != null)
                    {
                        // We need to know which unit to place. 
                        // It's the one in the DeploymentUI that is currently highlighted.
                        // Let's trigger the action directly if we can't simulate easily
                        _tutorialManager.OnActionTriggered("UnitPlaced");
                    }
                }
                else
                {
                    _tutorialManager.OnActionTriggered("UnitPlaced");
                }
            }
            else if (actionKey == "RiteMenuOpened")
            {
                _tutorialManager.OnActionTriggered("RiteMenuOpened");
            }
            else if (actionKey == "SkillUsed")
            {
                _tutorialManager.OnActionTriggered("SkillUsed");
            }
        }
    }
}
