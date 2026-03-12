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
        [Inject] private UnitInspectorUI _unitInspectorUI;
        [Inject] private DeploymentUI _deploymentUI;
        #endregion

        #region Serialized Settings
        [Header("Tutorial Visual Config")]
        [SerializeField] private Vector3 _tileHighlightOffset = new Vector3(0, -0.4f, 0);
        
        [Header("World Hole Settings")]
        [SerializeField] private Vector2 _unitWorldHoleSizeDefault = Vector2.one;
        [SerializeField] private float _unitWorldHoleYOffset = 1.0f;

        [Header("Debug")]
        [SerializeField] private bool _showDebugLogs = true;
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
            if (_showDebugLogs) Debug.Log($"[tutorial] StartTutorial called for: {data?.name}");
            if (IsInTutorial)
            {
                if (_showDebugLogs) Debug.LogWarning("[tutorial] Tutorial already in progress!");
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
            
            if (_showDebugLogs) Debug.Log($"[tutorial] Starting Tutorial Routine with {data.Steps.Count} steps.");
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
                    if (_showDebugLogs) Debug.Log($"[tutorial] Delaying for {step.DelayBefore}s before step {step.StepName}");
                    yield return new WaitForSecondsRealtime(step.DelayBefore);
                }

                if (_showDebugLogs) Debug.Log($"[tutorial] >>> Executing Step [{_currentStepIndex}]: {step.StepName} ({step.Type})");
                
                // Clear any previous tile highlights
                ClearAllTileHighlights();

                switch (step.Type)
                {
                    case TutorialStepType.DialogueOnly:
                        if (step.StopTime) _gameManager.SetSpeed(0);
                        HandleUIHighlight(step);
                        bool dialogueDone = false;
                        _dialogueManager.StartDialogue(step.Dialogue, () => 
                        {
                            if (_showDebugLogs) Debug.Log($"[tutorial] Dialogue completed for step: {step.StepName}");
                            dialogueDone = true;
                        });
                        yield return new WaitUntil(() => dialogueDone);
                        _uiBlocker.HideBlocker(); 
                        break;

                    case TutorialStepType.HighlightUI:
                        if (step.StopTime) _gameManager.SetSpeed(0);
                        HandleUIHighlight(step);
                        bool uiDialogueDone = false;
                        if (step.Dialogue != null && step.Dialogue.Lines != null && step.Dialogue.Lines.Count > 0)
                        {
                            _dialogueManager.StartDialogue(step.Dialogue, () => 
                            {
                                if (_showDebugLogs) Debug.Log($"[tutorial] UI Highlight Dialogue completed for step: {step.StepName}");
                                uiDialogueDone = true;
                            });
                            yield return new WaitUntil(() => uiDialogueDone);
                        }
                        else
                        {
                            if (_showDebugLogs) Debug.Log($"[tutorial] No dialogue for HighlightUI step: {step.StepName}, moving on.");
                        }
                        _handUI.Hide(); 
                        _uiBlocker.HideBlocker();
                        break;

                    case TutorialStepType.HighlightTile:
                        if (step.StopTime) _gameManager.SetSpeed(0);
                        HandleUIHighlight(step);
                        if (step.TargetTiles != null)
                        {
                            foreach (var wt in step.TargetTiles) HighlightTile(wt.Coordinate);
                        }
                        
                        bool tileDialogueDone = false;
                        _dialogueManager.StartDialogue(step.Dialogue, () => 
                        {
                            if (_showDebugLogs) Debug.Log($"[tutorial] Tile Highlight Dialogue completed for step: {step.StepName}");
                            tileDialogueDone = true;
                        });
                        yield return new WaitUntil(() => tileDialogueDone);
                        _handUI.Hide();
                        _uiBlocker.HideBlocker();
                        ClearAllTileHighlights();
                        break;

                    case TutorialStepType.WaitForAction:
                        if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for action: {step.ActionKey}");
                        if (step.StopTime) _gameManager.SetSpeed(0); 
                        
                        HandleUIHighlight(step);
                        
                        // Set waiting state BEFORE dialogue so InteractionManager can unlock selection if needed
                        _waitingForAction = true;
                        _waitingActionKey = step.ActionKey;

                        if (step.ActionKey == "SkillUsed" && _unitInspectorUI != null)
                        {
                            _unitInspectorUI.IsLocked = true;
                        }

                        if (step.Dialogue != null && step.Dialogue.Lines != null && step.Dialogue.Lines.Count > 0)
                        {
                            bool actionDialogueDone = false;
                            _dialogueManager.StartDialogue(step.Dialogue, () => actionDialogueDone = true);
                            yield return new WaitUntil(() => actionDialogueDone);
                        }

                        // Check buffer first (for fast sequential actions)
                        if (_triggeredActionsBuffer.Contains(step.ActionKey))
                        {
                            if (_showDebugLogs) Debug.Log($"[tutorial] Action {step.ActionKey} found in buffer, proceeding.");
                            _waitingForAction = false; // Received during dialogue
                            _triggeredActionsBuffer.Remove(step.ActionKey);
                        }
                        else
                        {
                            // Already set to true above, just wait
                            yield return new WaitUntil(() => !_waitingForAction);
                            _triggeredActionsBuffer.Remove(step.ActionKey); // Clean up
                        }
                        
                        if (_unitInspectorUI != null) _unitInspectorUI.IsLocked = false;
                        _handUI.Hide(); 
                        _uiBlocker.HideBlocker();
                        ClearAllTileHighlights();
                        
                        if (step.ResumeTime) _gameManager.SetSpeed(1); 
                        if (_showDebugLogs) Debug.Log($"[tutorial] Action {step.ActionKey} received.");
                        break;

                    case TutorialStepType.WaitTime:
                        if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for duration: {step.Duration}s");
                        yield return new WaitForSecondsRealtime(step.Duration);
                        break;

                    case TutorialStepType.StartWave:
                        if (_showDebugLogs) Debug.Log($"[tutorial] Starting Wave Index: {step.WaveIndex}");
                        if (_enemyManager != null)
                        {
                            _enemyManager.StartSpecificWave(step.WaveIndex);
                        }
                        break;

                    case TutorialStepType.WaitForWave:
                        if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for Wave completion (Index: {step.WaveIndex})");
                        _gameManager.SetSpeed(1); 
                        yield return new WaitUntil(() => _enemyManager != null && _enemyManager.ActiveEnemyCount == 0 && !_enemyManager.IsSpawning);
                        if (_showDebugLogs) Debug.Log("[tutorial] Wave cleared.");
                        break;

                    case TutorialStepType.WaitForCondition:
                        if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for condition: {step.ActionKey} (Value: {step.RequiredCount})");
                        _gameManager.SetSpeed(1); // Usually we want time moving for kills/enemies
                        yield return new WaitUntil(() => CheckCondition(step));
                        _gameManager.SetSpeed(0);
                        break;

                    case TutorialStepType.CustomCommand:
                        {
                            string targetName = (step.TargetUI != null ? step.TargetUI.Name : "");
                            if (_showDebugLogs) Debug.Log($"[tutorial] Executing Custom Command: {step.ActionKey} for {targetName}");
                            
                            if (step.ActionKey == "ChargeUnitUlt")
                            {
                                var unit = PlayerUnit.ActiveUnits.Find(u => u.Data != null && u.Data.UnitName == targetName);
                                if (unit == null) unit = PlayerUnit.ActiveUnits.Find(u => u.gameObject.name.Contains(targetName));
                                
                                if (unit != null)
                                {
                                    unit.ForceChargeUltimate();
                                }
                                else
                                {
                                    if (_showDebugLogs) Debug.LogWarning($"[tutorial] CustomCommand ChargeUnitUlt: Could not find unit '{targetName}'");
                                }
                            }
                            else if (step.ActionKey == "UnlockSelection")
                            {
                                if (_interactionManager != null)
                                {
                                    _interactionManager.IsSelectionLocked = false;
                                    if (_showDebugLogs) Debug.Log("[tutorial] CustomCommand: Unit Selection UNLOCKED.");
                                }
                            }
                            else if (step.ActionKey == "GrantMaxSeals")
                            {
                                if (MaouSamaTD.Managers.CurrencyManager.Instance != null)
                                {
                                    MaouSamaTD.Managers.CurrencyManager.Instance.GiveSeals(MaouSamaTD.Managers.CurrencyManager.Instance.MaxSeals);
                                    if (_showDebugLogs) Debug.Log("[tutorial] CustomCommand: GrantMaxSeals executed.");
                                }
                                else
                                {
                                    if (_showDebugLogs) Debug.LogWarning("[tutorial] CustomCommand GrantMaxSeals: CurrencyManager.Instance is NULL!");
                                }
                            }
                            else if (step.ActionKey == "SetUnitButtonActive")
                            {
                                if (_deploymentUI != null)
                                {
                                    bool active = (step.RequiredCount > 0);
                                    _deploymentUI.SetUnitButtonVisibility(targetName, active);
                                    if (_showDebugLogs) Debug.Log($"[tutorial] CustomCommand: SetUnitButtonActive for {targetName} to {active}");
                                }
                                else
                                {
                                    if (_showDebugLogs) Debug.LogWarning("[tutorial] CustomCommand SetUnitButtonActive: DeploymentUI is NULL!");
                                }
                            }
                            break;
                        }
                }

                if (_showDebugLogs) Debug.Log($"[tutorial] <<< Finished Step [{_currentStepIndex}]: {step.StepName}");
                _currentStepIndex++;
            }

            IsInTutorial = false;
            _activeTutorial = null;
            _gameManager.SetSpeed(1);
            if (_interactionManager != null) _interactionManager.IsSelectionLocked = false;
            
            if (_showDebugLogs) Debug.Log("[tutorial] Tutorial Sequence Completed.");
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

            // 2.5 Full Blocker - No Targeting
            if (step.FullBlocker)
            {
                _uiBlocker.ShowBlockerWithDetailedTargets(null, null);
                // Still handle hand logic if needed, but usually full blockers don't show hands pointing to nothing
                if (!step.ShowHand && !step.DragShowHand) _handUI.Hide();
                return;
            }

            List<UIPopupBlocker.UIHighlightData> uiHits = new List<UIPopupBlocker.UIHighlightData>();
            List<UIPopupBlocker.WorldHighlightData> worldHighlights = new List<UIPopupBlocker.WorldHighlightData>();

            // 3. Collect UI Targets
            List<UITarget> uiTargets = new List<UITarget>();
            if (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) uiTargets.Add(step.TargetUI);
            if (step.AdditionalTargetUI != null) uiTargets.AddRange(step.AdditionalTargetUI);

            foreach (var ut in uiTargets)
            {
                RectTransform rt = FindTargetRect(ut.Name);
                if (rt != null) 
                {
                    uiHits.Add(new UIPopupBlocker.UIHighlightData 
                    { 
                         Target = rt, 
                         Size = (ut.Size != Vector2.zero) ? ut.Size : Vector2.one,
                         Offset = ut.SizeOffset
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

            // 5. Apply to Blocker
            _uiBlocker.ShowBlockerWithDetailedTargets(uiHits, worldHighlights);

            // 6. Hand UI Logic
            if (step.DragShowHand && (uiHits.Count > 0 || worldHighlights.Count > 0))
            {
                Vector2 startPos = Vector2.zero;
                if (uiHits.Count > 0) startPos = (Vector2)uiHits[0].Target.position + uiHits[0].Offset;
                else if (worldHighlights.Count > 0) startPos = Camera.main.WorldToScreenPoint(worldHighlights[0].Position);

                // Find drag target
                if (step.HandTargetUIOverride != null && !string.IsNullOrEmpty(step.HandTargetUIOverride.Name))
                {
                    RectTransform drt = FindTargetRect(step.HandTargetUIOverride.Name);
                    if (drt != null) 
                    {
                        Vector3[] corners = new Vector3[4];
                        drt.GetWorldCorners(corners);
                        Vector3 center = (corners[0] + corners[2]) * 0.5f;
                        Vector3 size = corners[2] - corners[0];
                        Vector2 targetPos = (Vector2)center + new Vector2(size.x * step.HandTargetUIOverride.SizeOffset.x, size.y * step.HandTargetUIOverride.SizeOffset.y);
                        float finalScale = step.HandScale * step.HandTargetUIOverride.Size.x;
                        if (_showDebugLogs) Debug.Log($"[tutorial] DRAG Hand: Override Scale {step.HandScale} * {step.HandTargetUIOverride.Size.x} = {finalScale}");
                        _handUI.MoveHand(startPos, targetPos, finalScale);
                    }
                    else _handUI.MoveHand(startPos, Vector2.zero, step.HandScale);
                }
                else
                {
                    Vector3 worldTarget = GetWorldPosForTile(step.HandTargetTileOverride) + step.HandTargetTileOffsetOverride;
                    Vector2 screenTarget = Camera.main.WorldToScreenPoint(worldTarget);
                    if (_showDebugLogs) Debug.Log($"[tutorial] DRAG Hand: Tile Scale {step.HandScale}");
                    _handUI.MoveHand(startPos, screenTarget, step.HandScale);
                }
            }
            else if (step.ShowHand)
            {
                Vector2 handPos = Vector2.zero;
                float handScale = step.HandScale;

                // 1. Check for manual Hand Override (UITarget or Tile)
                if (step.HandTargetUIOverride != null && !string.IsNullOrEmpty(step.HandTargetUIOverride.Name))
                {
                    RectTransform drt = FindTargetRect(step.HandTargetUIOverride.Name);
                    if (drt != null) 
                    {
                        Vector3[] corners = new Vector3[4];
                        drt.GetWorldCorners(corners);
                        Vector3 center = (corners[0] + corners[2]) * 0.5f;
                        Vector3 size = corners[2] - corners[0];
                        handPos = (Vector2)center + new Vector2(size.x * step.HandTargetUIOverride.SizeOffset.x, size.y * step.HandTargetUIOverride.SizeOffset.y);
                        handScale *= step.HandTargetUIOverride.Size.x;
                    }
                }
                else if (step.HandTargetTileOverride != Vector2Int.zero)
                {
                    Vector3 worldTarget = GetWorldPosForTile(step.HandTargetTileOverride) + step.HandTargetTileOffsetOverride;
                    handPos = Camera.main.WorldToScreenPoint(worldTarget);
                }
                // 2. Fallback to Primary Target
                else if (worldHighlights.Count > 0) 
                {
                    handPos = Camera.main.WorldToScreenPoint(worldHighlights[0].Position);
                }
                else if (uiHits.Count > 0) 
                {
                    Vector3[] corners = new Vector3[4];
                    uiHits[0].Target.GetWorldCorners(corners);
                    Vector3 center = (corners[0] + corners[2]) * 0.5f;
                    Vector3 size = corners[2] - corners[0];
                    handPos = (Vector2)center + new Vector2(size.x * uiHits[0].Offset.x, size.y * uiHits[0].Offset.y);
                    handScale *= uiHits[0].Size.x;
                }

                if (handPos != Vector2.zero) 
                {
                    if (_showDebugLogs) Debug.Log($"[tutorial] STATIC Hand: Calculated Final Scale {handScale} (Base: {step.HandScale})");
                    _handUI.ShowAt(handPos, handScale);
                }
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
                if (_showDebugLogs) Debug.Log($"[tutorial] HIGHLIGHTING TILE at {coord}");
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

        public List<Vector2Int> GetRequiredPlacementTiles()
        {
            List<Vector2Int> allowed = new List<Vector2Int>();
            if (currentStep == null) return allowed;

            // If we are specifically waiting for UnitPlaced, honor the overrides
            if (currentStep.ActionKey == "UnitPlaced")
            {
                if (currentStep.HandTargetTileOverride != Vector2Int.zero && currentStep.HandTargetTileOverride != new Vector2Int(-1, -1))
                {
                    allowed.Add(currentStep.HandTargetTileOverride);
                }
            }

            // Always allow any TargetTiles defined in the step
            if (currentStep.TargetTiles != null)
            {
                foreach (var wt in currentStep.TargetTiles)
                {
                    if (!allowed.Contains(wt.Coordinate))
                        allowed.Add(wt.Coordinate);
                }
            }
            
            return allowed;
        }

        private bool CheckCondition(TutorialStep step)
        {
            switch (step.ActionKey)
            {
                case "UnitKills":
                {
                    PlayerUnit targetUnit = null;
                    string targetName = (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) ? step.TargetUI.Name : "";
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
                        if (met && _showDebugLogs) Debug.Log($"[tutorial] Condition MET: {step.ActionKey} ({targetUnit.KillCount}/{step.RequiredCount})");
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
                    else
                    {
                        string targetName = (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) ? step.TargetUI.Name : "";
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

                        foreach(var enemy in EnemyUnit.ActiveEnemies)
                        {
                            if (enemy == null) continue;
                            float dist = Vector3.Distance(centerPos, enemy.transform.position);
                            if (dist <= threshold) count++;
                        }
                        bool met = count >= step.RequiredCount && step.RequiredCount > 0;
                        if (met && _showDebugLogs) Debug.Log($"[tutorial] Condition MET: {step.ActionKey} ({count}/{step.RequiredCount})");
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
                        if (met && _showDebugLogs) Debug.Log($"[tutorial] Condition MET: WaveFinishedSpawning");
                        return met;
                    }
                    return false;
                
                case "UnitReach":
                {
                    string reachName = (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) ? step.TargetUI.Name : "";
                    string reachTarget = reachName.Contains("_") ? reachName.Substring(reachName.IndexOf('_') + 1) : reachName;
                    foreach(var u in PlayerUnit.ActiveUnits)
                    {
                        if (u != null && (u.gameObject.name == reachName || (u.Data != null && u.Data.UnitName == reachTarget)))
                        {
                            bool met = u.ReachCount >= step.RequiredCount && step.RequiredCount > 0;
                            if (met && _showDebugLogs) Debug.Log($"[tutorial] Condition MET: UnitReach ({u.ReachCount}/{step.RequiredCount})");
                            return met;
                        }
                    }
                    return false;
                }

                case "BossHealth":
                {
                    string bossName = (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) ? step.TargetUI.Name : "Abyssal Shade";
                    var boss = EnemyUnit.ActiveEnemies.FirstOrDefault(e => e.EnemyData != null && e.EnemyData.EnemyName == bossName);
                    if (boss != null)
                    {
                        float hpPercent = (boss.CurrentHp / boss.MaxHp) * 100f;
                        bool met = hpPercent <= step.RequiredCount;
                        if (met && _showDebugLogs) Debug.Log($"[tutorial] Condition MET: BossHealth ({hpPercent}% <= {step.RequiredCount}%)");
                        return met;
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
