using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zenject;
using MaouSamaTD.Levels;
using MaouSamaTD.UI;
using MaouSamaTD.UI.Tutorial;
using MaouSamaTD.Tutorial;

namespace MaouSamaTD.Managers
{
    public class TutorialManager : MonoBehaviour
    {
        [Inject] private DialogueManager _dialogueManager;
        [Inject] private GameManager _gameManager;
        [Inject] private Grid.GridManager _gridManager;
        [Inject] private InteractionManager _interactionManager;
        [Inject] private TutorialHandUI _handUI;
        [Inject] private UIPopupBlocker _uiBlocker;

        public bool IsInTutorial { get; private set; }
        private TutorialDataSO _activeTutorial;
        private int _currentStepIndex = -1;
        private bool _waitingForAction = false;
        private string _waitingActionKey;

        public void StartTutorial(TutorialDataSO data)
        {
            Debug.Log($"[TutorialManager] StartTutorial called for: {data?.name}");
            if (IsInTutorial)
            {
                Debug.LogWarning("[TutorialManager] Tutorial already in progress!");
                return;
            }
            if (data == null)
            {
                Debug.LogError("[TutorialManager] TutorialDataSO is NULL!");
                return;
            }

            _activeTutorial = data;
            IsInTutorial = true;
            _currentStepIndex = 0;
            
            EnsureUIComponentsActive();
            
            Debug.Log($"[TutorialManager] Starting Tutorial Routine with {data.Steps.Count} steps.");
            StartCoroutine(TutorialRoutine());
        }

        private void EnsureUIComponentsActive()
        {
            if (_dialogueManager != null) _dialogueManager.gameObject.SetActive(true);
            if (_handUI != null) _handUI.gameObject.SetActive(true);
            if (_uiBlocker != null) _uiBlocker.gameObject.SetActive(true);
        }

        private IEnumerator TutorialRoutine()
        {
            while (_currentStepIndex < _activeTutorial.Steps.Count)
            {
                TutorialStep step = _activeTutorial.Steps[_currentStepIndex];
                
                if (step.DelayBefore > 0)
                {
                    Debug.Log($"[TutorialManager] Delaying for {step.DelayBefore}s before step {step.StepName}");
                    yield return new WaitForSecondsRealtime(step.DelayBefore);
                }

                Debug.Log($"[TutorialManager] >>> Executing Step [{_currentStepIndex}]: {step.StepName} ({step.Type})");

                switch (step.Type)
                {
                    case TutorialStepType.DialogueOnly:
                        _gameManager.SetSpeed(0);
                        bool dialogueDone = false;
                        _dialogueManager.StartDialogue(step.Dialogue, () => 
                        {
                            Debug.Log($"[TutorialManager] Dialogue completed for step: {step.StepName}");
                            dialogueDone = true;
                        });
                        yield return new WaitUntil(() => dialogueDone);
                        _uiBlocker.HideBlocker(); // Ensure blocker is hidden after dialogue if needed
                        break;

                    case TutorialStepType.HighlightUI:
                        _gameManager.SetSpeed(0);
                        HandleUIHighlight(step);
                        bool uiDialogueDone = false;
                        _dialogueManager.StartDialogue(step.Dialogue, () => 
                        {
                            Debug.Log($"[TutorialManager] UI Highlight Dialogue completed for step: {step.StepName}");
                            uiDialogueDone = true;
                        });
                        yield return new WaitUntil(() => uiDialogueDone);
                        _uiBlocker.HideBlocker();
                        break;

                    case TutorialStepType.HighlightTile:
                        _gameManager.SetSpeed(0);
                        _uiBlocker.ShowBlockerWithWorldHighlight(GetWorldPosForTile(step.TargetTile), 1.0f);
                        _handUI.ShowAt(GetScreenPosForTile(step.TargetTile));
                        bool tileDialogueDone = false;
                        _dialogueManager.StartDialogue(step.Dialogue, () => 
                        {
                            Debug.Log($"[TutorialManager] Tile Highlight Dialogue completed for step: {step.StepName}");
                            tileDialogueDone = true;
                        });
                        yield return new WaitUntil(() => tileDialogueDone);
                        _handUI.Hide();
                        _uiBlocker.HideBlocker();
                        break;

                    case TutorialStepType.WaitForAction:
                        Debug.Log($"[TutorialManager] Waiting for action: {step.ActionKey}");
                        _gameManager.SetSpeed(0); // Pause for placement
                        _waitingForAction = true;
                        _waitingActionKey = step.ActionKey;
                        yield return new WaitUntil(() => !_waitingForAction);
                        _gameManager.SetSpeed(1); // Resume after placement
                        Debug.Log($"[TutorialManager] Action {step.ActionKey} received.");
                        break;

                    case TutorialStepType.WaitTime:
                        Debug.Log($"[TutorialManager] Waiting for duration: {step.Duration}s");
                        yield return new WaitForSecondsRealtime(step.Duration);
                        break;
                }

                Debug.Log($"[TutorialManager] <<< Finished Step [{_currentStepIndex}]: {step.StepName}");
                _currentStepIndex++;
            }

            IsInTutorial = false;
            _activeTutorial = null;
            _gameManager.SetSpeed(1);
            Debug.Log("[TutorialManager] Tutorial Sequence Completed.");
        }

        private void HandleUIHighlight(TutorialStep step)
        {
            GameObject target = GameObject.Find(step.TargetUIName);
            if (target != null)
            {
                RectTransform rt = target.GetComponent<RectTransform>();
                if (rt != null)
                {
                    _uiBlocker.ShowBlockerWithTarget(rt);
                    
                    if (step.DragShowHand)
                    {
                        GameObject dragTarget = GameObject.Find(step.HandDragTargetUIName);
                        if (dragTarget != null)
                        {
                            _handUI.MoveHand(rt.position, dragTarget.transform.position);
                        }
                        else
                        {
                            Vector2 worldTarget = GetScreenPosForTile(step.HandDragTargetTile);
                            _handUI.MoveHand(rt.position, worldTarget);
                        }
                    }
                    else if (step.ShowHand)
                    {
                        _handUI.ShowAt(rt.position);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[TutorialManager] Could not find UI target: {step.TargetUIName}");
            }
        }

        private Vector2 GetScreenPosForTile(Vector2Int tile)
        {
            return Camera.main.WorldToScreenPoint(GetWorldPosForTile(tile));
        }

        private Vector3 GetWorldPosForTile(Vector2Int tile)
        {
            // Assuming 1 unit per tile for now
            return new Vector3(tile.x, 0, tile.y);
        }

        public void OnActionTriggered(string actionKey)
        {
            if (_waitingForAction && _waitingActionKey == actionKey)
            {
                _waitingForAction = false;
            }
        }
    }
}
