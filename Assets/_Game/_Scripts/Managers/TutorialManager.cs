using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        #region Dependencies
        [Inject] private DialogueManager _dialogueManager;
        [Inject] private GameManager _gameManager;
        [Inject] private Grid.GridManager _gridManager;
        [Inject] private InteractionManager _interactionManager;
        [Inject] private TutorialHandUI _handUI;
        [Inject] private UIPopupBlocker _uiBlocker;
        [Inject] private EnemyManager _enemyManager;
        #endregion

        #region Serialized Settings
        [Header("Tutorial Visual Config")]
        [SerializeField] private Vector3 _tileHighlightOffset = new Vector3(0, -0.4f, 0);
        
        [Header("World Hole Settings")]
        [SerializeField] private Vector2 _unitWorldHoleSizeDefault = Vector2.one;
        [SerializeField] private float _unitWorldHoleYOffset = 1.0f;
        #endregion
        
        #region State
        public bool IsInTutorial { get; private set; }
        private TutorialDataSO _activeTutorial;
        private int _currentStepIndex = -1;
        private bool _waitingForAction = false;
        private string _waitingActionKey;
        private HashSet<string> _triggeredActionsBuffer = new HashSet<string>();
        private TutorialStep currentStep => (_activeTutorial != null && _currentStepIndex >= 0 && _currentStepIndex < _activeTutorial.Steps.Count) ? _activeTutorial.Steps[_currentStepIndex] : null;
        #endregion

        #region Public API
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
        #endregion

        #region Lifecycle
        private void EnsureUIComponentsActive()
        {
            if (_dialogueManager != null) _dialogueManager.gameObject.SetActive(true);
            if (_handUI != null) _handUI.gameObject.SetActive(true);
            if (_uiBlocker != null) _uiBlocker.gameObject.SetActive(true);
        }
        #endregion

        #region Core Tutorial Loop
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
                        HandleUIHighlight(step);
                        HighlightTile(step.TargetTile);
                        if (step.AdditionalTargetTiles != null)
                        {
                            foreach (var tile in step.AdditionalTargetTiles) HighlightTile(tile);
                        }
                        
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
                        
                        // Set waiting state BEFORE dialogue so InteractionManager can unlock selection if needed
                        _waitingForAction = true;
                        _waitingActionKey = step.ActionKey;

                        if (step.Dialogue != null && step.Dialogue.Lines != null && step.Dialogue.Lines.Count > 0)
                        {
                            bool actionDialogueDone = false;
                            _dialogueManager.StartDialogue(step.Dialogue, () => actionDialogueDone = true);
                            yield return new WaitUntil(() => actionDialogueDone);
                        }

                        // Check buffer first (for fast sequential actions)
                        if (_triggeredActionsBuffer.Contains(step.ActionKey))
                        {
                            Debug.Log($"[tutorial] Action {step.ActionKey} found in buffer, proceeding.");
                            _waitingForAction = false; // Received during dialogue
                            _triggeredActionsBuffer.Remove(step.ActionKey);
                        }
                        else
                        {
                            // Already set to true above, just wait
                            yield return new WaitUntil(() => !_waitingForAction);
                            _triggeredActionsBuffer.Remove(step.ActionKey); // Clean up
                        }
                        
                        _handUI.Hide(); 
                        _uiBlocker.HideBlocker();
                        ClearAllTileHighlights();
                        
                        if (step.ResumeTime) _gameManager.SetSpeed(1); 
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

                    case TutorialStepType.CustomCommand:
                        Debug.Log($"[tutorial] Executing Custom Command: {step.ActionKey} for {step.TargetUIName}");
                        if (step.ActionKey == "ChargeUnitUlt")
                        {
                            var unit = PlayerUnit.ActiveUnits.Find(u => u.Data != null && u.Data.UnitName == step.TargetUIName);
                            if (unit == null) unit = PlayerUnit.ActiveUnits.Find(u => u.gameObject.name.Contains(step.TargetUIName));
                            
                            if (unit != null)
                            {
                                unit.ForceChargeUltimate();
                            }
                            else
                            {
                                Debug.LogWarning($"[tutorial] CustomCommand ChargeUnitUlt: Could not find unit {step.TargetUIName}");
                            }
                        }
                        else if (step.ActionKey == "UnlockSelection")
                        {
                            if (_interactionManager != null)
                            {
                                _interactionManager.IsSelectionLocked = false;
                                Debug.Log("[tutorial] CustomCommand: Unit Selection UNLOCKED.");
                            }
                        }
                        break;
                }

                Debug.Log($"[tutorial] <<< Finished Step [{_currentStepIndex}]: {step.StepName}");
                _currentStepIndex++;
            }

            IsInTutorial = false;
            _activeTutorial = null;
            _gameManager.SetSpeed(1);
            Debug.Log("[tutorial] Tutorial Sequence Completed.");
            _uiBlocker.HideBlocker();
            _handUI.Hide();
        }
        #endregion

        #region Visuals & Highlighting
        private void HandleUIHighlight(TutorialStep step)
        {
            if (step == null) return;

            // 1. Reset Logic
            if (step.ResetBlocker)
            {
                _uiBlocker.ClearTargets();
                _handUI.Hide();
            }

            // 2. Use Blocker Toggle
            if (!step.UseBlocker)
            {
                _uiBlocker.HideBlocker();
                if (!step.ShowHand && !step.DragShowHand) _handUI.Hide();
                return;
            }

            List<UIPopupBlocker.UIHighlightData> uiHits = new List<UIPopupBlocker.UIHighlightData>();
            List<UIPopupBlocker.WorldHighlightData> worldHighlights = new List<UIPopupBlocker.WorldHighlightData>();

            // 3. Collect UI Targets
            List<UITarget> uiTargets = new List<UITarget>();
            if (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) uiTargets.Add(step.TargetUI);
            if (step.AdditionalTargetUI != null) uiTargets.AddRange(step.AdditionalTargetUI);

            // Legacy UI Fallback
            if (uiTargets.Count == 0 && !string.IsNullOrEmpty(step.TargetUIName))
            {
                uiTargets.Add(new UITarget { Name = step.TargetUIName, Size = step.HoleSize });
            }
            if (step.AdditionalTargetUINames != null)
            {
                foreach (var name in step.AdditionalTargetUINames)
                {
                    if (!string.IsNullOrEmpty(name)) uiTargets.Add(new UITarget { Name = name, Size = step.HoleSize });
                }
            }

            foreach (var ut in uiTargets)
            {
                RectTransform rt = FindTargetRect(ut.Name);
                if (rt != null) 
                {
                    uiHits.Add(new UIPopupBlocker.UIHighlightData 
                    { 
                         Target = rt, 
                         Size = (ut.Size != Vector2.zero) ? ut.Size : Vector2.one 
                    });
                }
            }

            // 4. Collect World Targets
            if (step.TargetTiles != null && step.TargetTiles.Count > 0)
            {
                foreach (var wt in step.TargetTiles)
                {
                    worldHighlights.Add(new UIPopupBlocker.WorldHighlightData 
                    {
                        Position = GetWorldPosForTile(wt.Coordinate) + wt.Offset,
                        Size = wt.Size,
                        Height = wt.Height
                    });
                }
            }
            else
            {
                // Legacy Tile Fallback
                if (step.TargetTile != Vector2Int.zero)
                {
                    worldHighlights.Add(new UIPopupBlocker.WorldHighlightData 
                    {
                        Position = GetWorldPosForTile(step.TargetTile),
                        Size = step.HoleSize,
                        Height = step.HoleHeight
                    });
                }
                if (step.AdditionalTargetTiles != null)
                {
                    foreach (var tile in step.AdditionalTargetTiles)
                    {
                        worldHighlights.Add(new UIPopupBlocker.WorldHighlightData 
                        {
                            Position = GetWorldPosForTile(tile),
                            Size = step.HoleSize,
                            Height = step.HoleHeight
                        });
                    }
                }
            }

            // 5. Apply to Blocker
            _uiBlocker.ShowBlockerWithDetailedTargets(uiHits, worldHighlights);

            // 6. Hand UI Logic
            if (step.DragShowHand && (uiHits.Count > 0 || worldHighlights.Count > 0))
            {
                Vector2 startPos = Vector2.zero;
                if (uiHits.Count > 0) startPos = uiHits[0].Target.position;
                else if (worldHighlights.Count > 0) startPos = Camera.main.WorldToScreenPoint(worldHighlights[0].Position);

                // Find drag target
                if (step.HandDragTargetUI != null && !string.IsNullOrEmpty(step.HandDragTargetUI.Name))
                {
                    RectTransform drt = FindTargetRect(step.HandDragTargetUI.Name);
                    if (drt != null) _handUI.MoveHand(startPos, drt.position);
                    else _handUI.MoveHand(startPos, Vector2.zero); // Or hide?
                }
                else
                {
                    Vector3 worldTarget = GetWorldPosForTile(step.HandDragTargetTile) + step.HandDragTargetTileOffset;
                    Vector2 screenTarget = Camera.main.WorldToScreenPoint(worldTarget);
                    _handUI.MoveHand(startPos, screenTarget);
                }
            }
            else if (step.ShowHand)
            {
                Vector2 handPos = Vector2.zero;
                if (worldHighlights.Count > 0) 
                    handPos = Camera.main.WorldToScreenPoint(worldHighlights[0].Position);
                else if (uiHits.Count > 0) 
                    handPos = uiHits[0].Target.position;

                if (handPos != Vector2.zero) _handUI.ShowAt(handPos);
            }
        }

        private RectTransform FindTargetRect(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            GameObject go = GameObject.Find(name);
            if (go != null)
            {
                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt != null) return rt;
                
                // If found but no RT, it's a world object, check for unit canvas
                Canvas canvas = go.GetComponentInChildren<Canvas>(true);
                if (canvas != null) return canvas.GetComponent<RectTransform>() ?? canvas.transform as RectTransform;
            }

            // Fallback for units by name
            string unitName = name;
            if (unitName.StartsWith("Enemy_")) unitName = unitName.Replace("Enemy_", "");
            else if (unitName.StartsWith("Unit_")) unitName = unitName.Replace("Unit_", "");

            PlayerUnit pu = PlayerUnit.ActiveUnits.FirstOrDefault(u => u.name == unitName || u.name.Contains(unitName));
            if (pu != null)
            {
                Canvas canvas = pu.GetComponentInChildren<Canvas>(true);
                if (canvas != null) return canvas.GetComponent<RectTransform>() ?? canvas.transform as RectTransform;
            }

            EnemyUnit eu = EnemyUnit.ActiveEnemies.FirstOrDefault(u => u.name == name || u.name.Contains(unitName));
            if (eu != null)
            {
                Canvas canvas = eu.GetComponentInChildren<Canvas>(true);
                if (canvas != null) return canvas.GetComponent<RectTransform>() ?? canvas.transform as RectTransform;
            }

            return null;
        }
        #endregion

        #region Tile Helpers
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

        private Vector3 GetWorldPosForTile(Vector2Int tileCoord)
        {
            if (_gridManager != null)
            {
                var tile = _gridManager.GetTileAt(tileCoord);
                Vector3 pos;
                
                if (tile != null)
                {
                    // Use actual transform position which includes HighGround Y offset
                    pos = tile.transform.position;
                }
                else
                {
                    pos = _gridManager.GridToWorldPosition(tileCoord);
                }

                // Apply a serialized offset to align with the tile footprint visually
                // in isometric perspective
                return pos + _tileHighlightOffset;
            }
            return new Vector3(tileCoord.x, -0.2f, tileCoord.y) + _tileHighlightOffset;
        }
        #endregion

        #region Actions & Conditions
        public void OnActionTriggered(string actionKey)
        {
            _triggeredActionsBuffer.Add(actionKey);

            if (_waitingForAction && _waitingActionKey == actionKey)
            {
                _waitingForAction = false;
                if (currentStep != null && currentStep.ResumeTime)
                {
                    _gameManager.SetSpeed(1); // Resume time only if step allows
                }
            }
        }

        public bool IsWaitingForAction(string actionKey)
        {
            return _waitingForAction && _waitingActionKey == actionKey;
        }

        public Vector2Int GetRequiredPlacementTile()
        {
            if (currentStep == null) return new Vector2Int(-1, -1);

            if (currentStep.ActionKey == "UnitPlaced")
                return currentStep.HandDragTargetTile;

            if (currentStep.TargetTiles != null && currentStep.TargetTiles.Count > 0)
                return currentStep.TargetTiles[0].Coordinate;
            
            // Legacy fallback
            if (currentStep.TargetTile != Vector2Int.zero)
                return currentStep.TargetTile;

            return new Vector2Int(-1, -1);
        }

        private bool CheckCondition(TutorialStep step)
        {
            switch (step.ActionKey)
            {
                case "UnitKills":
                {
                    PlayerUnit targetUnit = null;
                    string targetName = (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) ? step.TargetUI.Name : step.TargetUIName;
                    string killsTarget = targetName.Contains("_") ? targetName.Substring(targetName.IndexOf('_') + 1) : targetName;
                    
                    foreach(var u in PlayerUnit.ActiveUnits)
                    {
                        if (u != null && u.Data != null && (u.Data.UnitName == killsTarget || u.gameObject.name == targetName))
                        {
                            targetUnit = u;
                            break;
                        }
                    }

                    if (targetUnit != null)
                    {
                        bool met = targetUnit.KillCount >= step.RequiredCount && step.RequiredCount > 0;
                        if (met) Debug.Log($"[tutorial] Condition MET: {step.ActionKey} ({targetUnit.KillCount}/{step.RequiredCount})");
                        return met;
                    }
                    return false;
                }

                case "EnemiesInRange":
                {
                    Vector3 centerPos = Vector3.zero;
                    bool foundCenter = false;
                    
                    if (step.TargetTiles != null && step.TargetTiles.Count > 0)
                    {
                        centerPos = GetWorldPosForTile(step.TargetTiles[0].Coordinate);
                        foundCenter = true;
                    }
                    else if (step.TargetTile != Vector2Int.zero)
                    {
                        centerPos = GetWorldPosForTile(step.TargetTile);
                        foundCenter = true;
                    }
                    else
                    {
                        string targetName = (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) ? step.TargetUI.Name : step.TargetUIName;
                        string rangeTarget = targetName.Contains("_") ? targetName.Substring(targetName.IndexOf('_') + 1) : targetName;
                        foreach(var u in PlayerUnit.ActiveUnits)
                        {
                            if (u != null && u.Data != null && (u.Data.UnitName == rangeTarget || u.gameObject.name == targetName))
                            {
                                centerPos = u.transform.position;
                                foundCenter = true;
                                break;
                            }
                        }
                    }

                    if (foundCenter)
                    {
                        int count = 0;
                        float threshold = 2.0f;
                        if (step.TargetTiles != null && step.TargetTiles.Count > 0) threshold = step.TargetTiles[0].Size.x;
                        else if (step.HoleSize.x > 0) threshold = step.HoleSize.x;

                        foreach(var enemy in EnemyUnit.ActiveEnemies)
                        {
                            if (enemy == null) continue;
                            float dist = Vector3.Distance(centerPos, enemy.transform.position);
                            if (dist <= threshold) count++;
                        }
                        bool met = count >= step.RequiredCount && step.RequiredCount > 0;
                        if (met) Debug.Log($"[tutorial] Condition MET: {step.ActionKey} ({count}/{step.RequiredCount})");
                        return met;
                    }
                    return false;
                }

                case "WaveFinishedSpawning":
                    // If enemy manager is null or not spawning, and we are at or past the wave index?
                    // Actually, StartWave sets _isSpawning. We wait for it to become false after it was true.
                    // But simpler: just check if enemy manager reports it is no longer spawning.
                    if (_enemyManager != null)
                    {
                        bool met = !_enemyManager.IsSpawning;
                        if (met) Debug.Log($"[tutorial] Condition MET: WaveFinishedSpawning");
                        return met;
                    }
                    return false;
                
                case "UnitReach":
                {
                    string reachName = (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) ? step.TargetUI.Name : step.TargetUIName;
                    string reachTarget = reachName.Contains("_") ? reachName.Substring(reachName.IndexOf('_') + 1) : reachName;
                    foreach(var u in PlayerUnit.ActiveUnits)
                    {
                        if (u != null && (u.gameObject.name == reachName || (u.Data != null && u.Data.UnitName == reachTarget)))
                        {
                            bool met = u.ReachCount >= step.RequiredCount && step.RequiredCount > 0;
                            if (met) Debug.Log($"[tutorial] Condition MET: UnitReach ({u.ReachCount}/{step.RequiredCount})");
                            return met;
                        }
                    }
                    return false;
                }

                default:
                    return false;
            }
        }
        #endregion
    }
}
