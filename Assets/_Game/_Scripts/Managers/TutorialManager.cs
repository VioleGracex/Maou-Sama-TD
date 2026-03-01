using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zenject;
using MaouSamaTD.Levels;
using MaouSamaTD.UI;
using MaouSamaTD.UI.Tutorial;
using MaouSamaTD.Tutorial;
using MaouSamaTD.Units;

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
        [Inject] private EnemyManager _enemyManager;

        public bool IsInTutorial { get; private set; }
        private TutorialDataSO _activeTutorial;
        private int _currentStepIndex = -1;
        private bool _waitingForAction = false;
        private string _waitingActionKey;

        public void StartTutorial(TutorialDataSO data)
        {
            Debug.Log($"[tutorial] StartTutorial called for: {data?.name}");
            if (IsInTutorial)
            {
                Debug.LogWarning("[tutorial] Tutorial already in progress!");
                return;
            }
            if (data == null)
            {
                Debug.LogError("[tutorial] TutorialDataSO is NULL!");
                return;
            }

            _activeTutorial = data;
            IsInTutorial = true;
            _currentStepIndex = 0;
            
            EnsureUIComponentsActive();
            
            Debug.Log($"[tutorial] Starting Tutorial Routine with {data.Steps.Count} steps.");
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
                    Debug.Log($"[tutorial] Delaying for {step.DelayBefore}s before step {step.StepName}");
                    yield return new WaitForSecondsRealtime(step.DelayBefore);
                }

                Debug.Log($"[tutorial] >>> Executing Step [{_currentStepIndex}]: {step.StepName} ({step.Type})");
                
                // Clear any previous tile highlights
                ClearAllTileHighlights();

                switch (step.Type)
                {
                    case TutorialStepType.DialogueOnly:
                        _gameManager.SetSpeed(0);
                        bool dialogueDone = false;
                        _dialogueManager.StartDialogue(step.Dialogue, () => 
                        {
                            Debug.Log($"[tutorial] Dialogue completed for step: {step.StepName}");
                            dialogueDone = true;
                        });
                        yield return new WaitUntil(() => dialogueDone);
                        _uiBlocker.HideBlocker(); 
                        break;

                    case TutorialStepType.HighlightUI:
                        _gameManager.SetSpeed(0);
                        HandleUIHighlight(step);
                        bool uiDialogueDone = false;
                        if (step.Dialogue != null && step.Dialogue.Lines != null && step.Dialogue.Lines.Count > 0)
                        {
                            _dialogueManager.StartDialogue(step.Dialogue, () => 
                            {
                                Debug.Log($"[tutorial] UI Highlight Dialogue completed for step: {step.StepName}");
                                uiDialogueDone = true;
                            });
                            yield return new WaitUntil(() => uiDialogueDone);
                        }
                        else
                        {
                            Debug.Log($"[tutorial] No dialogue for HighlightUI step: {step.StepName}, moving on.");
                        }
                        _handUI.Hide(); 
                        _uiBlocker.HideBlocker();
                        break;

                    case TutorialStepType.HighlightTile:
                        _gameManager.SetSpeed(0);
                        _uiBlocker.ShowBlockerWithWorldHighlight(GetWorldPosForTile(step.TargetTile), 1.0f);
                        _handUI.ShowAt(GetScreenPosForTile(step.TargetTile));
                        HighlightTile(step.TargetTile);
                        
                        bool tileDialogueDone = false;
                        _dialogueManager.StartDialogue(step.Dialogue, () => 
                        {
                            Debug.Log($"[tutorial] Tile Highlight Dialogue completed for step: {step.StepName}");
                            tileDialogueDone = true;
                        });
                        yield return new WaitUntil(() => tileDialogueDone);
                        _handUI.Hide();
                        _uiBlocker.HideBlocker();
                        ClearAllTileHighlights();
                        break;

                    case TutorialStepType.WaitForAction:
                        Debug.Log($"[tutorial] Waiting for action: {step.ActionKey}");
                        _gameManager.SetSpeed(0); 
                        
                        HandleUIHighlight(step);
                        
                        if (step.ActionKey == "UnitPlaced")
                        {
                             HighlightTile(step.HandDragTargetTile);
                        }

                        if (step.Dialogue != null && step.Dialogue.Lines != null && step.Dialogue.Lines.Count > 0)
                        {
                            bool actionDialogueDone = false;
                            _dialogueManager.StartDialogue(step.Dialogue, () => actionDialogueDone = true);
                            yield return new WaitUntil(() => actionDialogueDone);
                        }

                        _waitingForAction = true;
                        _waitingActionKey = step.ActionKey;
                        yield return new WaitUntil(() => !_waitingForAction);
                        
                        _handUI.Hide(); 
                        _uiBlocker.HideBlocker();
                        ClearAllTileHighlights();
                        _gameManager.SetSpeed(1); 
                        Debug.Log($"[tutorial] Action {step.ActionKey} received.");
                        break;

                    case TutorialStepType.WaitTime:
                        Debug.Log($"[tutorial] Waiting for duration: {step.Duration}s");
                        yield return new WaitForSecondsRealtime(step.Duration);
                        break;

                    case TutorialStepType.StartWave:
                        Debug.Log($"[tutorial] Starting Wave Index: {step.WaveIndex}");
                        if (_enemyManager != null)
                        {
                            _enemyManager.StartSpecificWave(step.WaveIndex);
                        }
                        break;

                    case TutorialStepType.WaitForWave:
                        Debug.Log($"[tutorial] Waiting for Wave completion (Index: {step.WaveIndex})");
                        _gameManager.SetSpeed(1); 
                        yield return new WaitUntil(() => _enemyManager != null && _enemyManager.ActiveEnemyCount == 0 && !_enemyManager.IsSpawning);
                        Debug.Log("[tutorial] Wave cleared.");
                        break;

                    case TutorialStepType.WaitForCondition:
                        Debug.Log($"[tutorial] Waiting for condition: {step.ActionKey} (Value: {step.RequiredCount})");
                        _gameManager.SetSpeed(1); // Usually we want time moving for kills/enemies
                        yield return new WaitUntil(() => CheckCondition(step));
                        _gameManager.SetSpeed(0);
                        break;
                }

                Debug.Log($"[tutorial] <<< Finished Step [{_currentStepIndex}]: {step.StepName}");
                _currentStepIndex++;
            }

            IsInTutorial = false;
            _activeTutorial = null;
            _gameManager.SetSpeed(1);
            Debug.Log("[tutorial] Tutorial Sequence Completed.");
        }

        private void HandleUIHighlight(TutorialStep step)
        {
            List<RectTransform> targets = new List<RectTransform>();

            // Main Target
            if (!string.IsNullOrEmpty(step.TargetUIName))
            {
                Debug.Log($"[tutorial] HandleUIHighlight searching for main target: {step.TargetUIName}");
                GameObject mainTarget = GameObject.Find(step.TargetUIName);
                if (mainTarget != null)
                {
                    Debug.Log($"[tutorial] Found main target: {mainTarget.name}");
                    RectTransform rt = mainTarget.GetComponent<RectTransform>();
                    if (rt != null) targets.Add(rt);
                }
                else
                {
                    Debug.LogWarning($"[tutorial] Could not find main UI target: {step.TargetUIName}");
                }
            }

            // Additional Targets
            if (step.AdditionalTargetUINames != null)
            {
                foreach (var name in step.AdditionalTargetUINames)
                {
                    if (string.IsNullOrEmpty(name)) continue;
                    GameObject extraTarget = GameObject.Find(name);
                    if (extraTarget != null)
                    {
                        RectTransform rt = extraTarget.GetComponent<RectTransform>();
                        if (rt != null && !targets.Contains(rt)) targets.Add(rt);
                    }
                }
            }

            if (targets.Count > 0)
            {
                if (step.DragShowHand)
                {
                    RectTransform mainRT = targets[0];
                    Vector3 worldTarget = GetWorldPosForTile(step.HandDragTargetTile);
                    _uiBlocker.ShowBlockerWithTargets(targets, worldTarget, 1.0f);
                    
                    GameObject dragTargetGO = GameObject.Find(step.HandDragTargetUIName);
                    if (dragTargetGO != null)
                    {
                        Vector2 targetPos = dragTargetGO.transform.position;
                        if (dragTargetGO.GetComponent<RectTransform>() != null) {
                            targetPos = dragTargetGO.GetComponent<RectTransform>().position;
                        }
                        _handUI.MoveHand(mainRT.position, targetPos);
                    }
                    else
                    {
                        Vector2 screenTarget = GetScreenPosForTile(step.HandDragTargetTile);
                        _handUI.MoveHand(mainRT.position, screenTarget);
                    }
                }
                else
                {
                    _uiBlocker.ShowBlockerWithTargets(targets);
                    if (step.ShowHand)
                    {
                        _handUI.ShowAt(targets[0].position);
                    }
                }
            }
        }

        private List<Vector2Int> _highlightedTiles = new List<Vector2Int>();
        private void HighlightTile(Vector2Int coord)
        {
            var tile = _gridManager.GetTileAt(coord);
            if (tile != null)
            {
                Debug.Log($"[tutorial] HIGHLIGHTING TILE at {coord}");
                tile.SetHighlight(true, Color.yellow, true);
                _highlightedTiles.Add(coord);
            }
        }

        private void ClearAllTileHighlights()
        {
            foreach (var coord in _highlightedTiles)
            {
                var tile = _gridManager.GetTileAt(coord);
                if (tile != null) tile.SetHighlight(false, Color.black);
            }
            _highlightedTiles.Clear();
        }

        private Vector2 GetScreenPosForTile(Vector2Int tile)
        {
            return Camera.main.WorldToScreenPoint(GetWorldPosForTile(tile));
        }

        private Vector3 GetWorldPosForTile(Vector2Int tile)
        {
            if (_gridManager != null)
            {
                return _gridManager.GridToWorldPosition(tile);
            }
            return new Vector3(tile.x, 0, tile.y);
        }

        public void OnActionTriggered(string actionKey)
        {
            if (_waitingForAction && _waitingActionKey == actionKey)
            {
                _waitingForAction = false;
            }
        }

        private bool CheckCondition(TutorialStep step)
        {
            switch (step.ActionKey)
            {
                case "UnitKills":
                    GameObject unitGO = GameObject.Find(step.TargetUIName); // Reuse target name for unit name
                    if (unitGO != null)
                    {
                        PlayerUnit unit = unitGO.GetComponent<PlayerUnit>();
                        if (unit != null) return unit.KillCount >= step.RequiredCount;
                    }
                    return false;

                case "EnemiesInRange":
                    GameObject centerGO = GameObject.Find(step.TargetUIName);
                    if (centerGO != null)
                    {
                        int count = 0;
                        float worldRadius = step.HandDragTargetRadius > 0 ? step.HandDragTargetRadius : 2.5f; // Approx
                        foreach(var enemy in EnemyUnit.ActiveEnemies)
                        {
                            if (Vector3.Distance(centerGO.transform.position, enemy.transform.position) <= worldRadius * 2.0f) // Isometric 2 tiles is approx 4 units
                            {
                                count++;
                            }
                        }
                        return count >= step.RequiredCount;
                    }
                    return false;

                default:
                    return false;
            }
        }
    }
}
