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
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
        [Inject] private BattleCurrencyManager _currencyManager;
        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;
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
        private void Start()
        {
            if (_gameManager != null)
            {
                _gameManager.OnGameFinished += StopTutorial;
            }
        }

        private void OnDestroy()
        {
            if (_gameManager != null)
            {
                _gameManager.OnGameFinished -= StopTutorial;
            }
        }

        private void StopTutorial()
        {
            if (!IsInTutorial) return;
            
            if (_showDebugLogs) Debug.Log("[tutorial] StopTutorial called due to Game End.");
            StopAllCoroutines();
            
            IsInTutorial = false;
            _activeTutorial = null;
            
            if (_dialogueManager != null) _dialogueManager.HideDialogue();
            if (_uiBlocker != null) _uiBlocker.HideBlocker(true);
            if (_handUI != null) _handUI.Hide();
            if (_interactionManager != null) _interactionManager.IsSelectionLocked = false;
        }

        private bool _isSkillTargetingLastFrame = false;
        private bool _isDraggingLastFrame = false;
        private void Update()
        {
            if (!IsInTutorial || _activeTutorial == null || _currentStepIndex >= _activeTutorial.Steps.Count) return;

            var step = _activeTutorial.Steps[_currentStepIndex];
            
            // Dynamic Skill Targeting logic: Update hand and blocker when switching between skill selection and unit targeting
            if (step.ActionKey == "SkillUsed" || step.ActionKey == "RiteMenuOpened")
            {
                bool isTargeting = _interactionManager != null && _interactionManager.IsSkillTargeting;
                if (isTargeting != _isSkillTargetingLastFrame)
                {
                    _isSkillTargetingLastFrame = isTargeting;
                    if (_showDebugLogs) Debug.Log($"[tutorial] Skill targeting state changed to: {isTargeting}. Refreshing highlights.");
                    HandleUIHighlight(step);
                }
            }

            // Dynamic Unit Placement logic: Update hand and blocker when starting/stopping drag
            if (step.ActionKey == "UnitPlaced")
            {
                bool isDragging = _interactionManager != null && _interactionManager.IsDragging;
                if (isDragging != _isDraggingLastFrame)
                {
                    _isDraggingLastFrame = isDragging;
                    if (_showDebugLogs) Debug.Log($"[tutorial] Dragging state changed to: {isDragging}. Refreshing highlights.");
                    HandleUIHighlight(step);
                }
            }
        }

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
                
                ClearAllTileHighlights();

                // Skip step if already completed (e.g., unit already placed)
                if (CheckStepAlreadyCompleted(step))
                {
                    if (_showDebugLogs) Debug.Log($"[tutorial] Skipping Step [{_currentStepIndex}] {step.StepName} because it's already completed.");
                    _currentStepIndex++;
                    continue;
                }

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
                        
                        // Hide the hand when dialogue is done so it doesn't linger
                        _handUI.Hide();
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
                        ClearAllTileHighlights();
                        break;

                    case TutorialStepType.WaitForAction:
                        if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for action: {step.ActionKey}");
                        if (step.StopTime) _gameManager.SetSpeed(0); 
                        
                        HandleUIHighlight(step);

                        if (step.Dialogue != null && step.Dialogue.Lines != null && step.Dialogue.Lines.Count > 0)
                        {
                            bool actionDialogueDone = false;
                            _dialogueManager.StartDialogue(step.Dialogue, () => actionDialogueDone = true);
                            yield return new WaitUntil(() => actionDialogueDone);
                        }
                        
                        _waitingForAction = true;
                        _waitingActionKey = step.ActionKey;

                        if (step.ActionKey == "SkillUsed" && _unitInspectorUI != null)
                        {
                            _unitInspectorUI.IsLocked = true;
                        }

                        if (_triggeredActionsBuffer.Contains(step.ActionKey))
                        {
                            if (_showDebugLogs) Debug.Log($"[tutorial] Action {step.ActionKey} found in buffer, proceeding.");
                            _waitingForAction = false; 
                            _triggeredActionsBuffer.Remove(step.ActionKey);
                        }
                        else
                        {
                            yield return new WaitUntil(() => !_waitingForAction);
                            _triggeredActionsBuffer.Remove(step.ActionKey); 
                        }
                        
                        if (_unitInspectorUI != null) _unitInspectorUI.IsLocked = false;
                        _handUI.Hide(); 
                        ClearAllTileHighlights();
                        
                        if (step.ResumeTime) _gameManager.SetSpeed(1); 
                        if (_showDebugLogs) Debug.Log($"[tutorial] Action {step.ActionKey} received.");
                        break;

                    case TutorialStepType.WaitTime:
                        HandleUIHighlight(step);
                        if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for duration: {step.Duration}s");
                        yield return new WaitForSecondsRealtime(step.Duration);
                        break;

                    case TutorialStepType.StartWave:
                        HandleUIHighlight(step);
                        if (_showDebugLogs) Debug.Log($"[tutorial] Starting Wave Index: {step.WaveIndex}");
                        if (_enemyManager != null)
                        {
                            _enemyManager.StartSpecificWave(step.WaveIndex);
                        }
                        break;

                    case TutorialStepType.WaitForWave:
                        HandleUIHighlight(step);
                        if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for Wave completion (Index: {step.WaveIndex})");
                        _gameManager.SetSpeed(1); 
                        yield return new WaitUntil(() => _enemyManager != null && _enemyManager.ActiveEnemyCount == 0 && !_enemyManager.IsSpawning);
                        if (_showDebugLogs) Debug.Log("[tutorial] Wave cleared.");
                        break;

                    case TutorialStepType.WaitForCondition:
                        HandleUIHighlight(step);
                        if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for condition: {step.ActionKey} (Value: {step.RequiredCount})");
                        _gameManager.SetSpeed(1); 
                        yield return new WaitUntil(() => CheckCondition(step));
                        _gameManager.SetSpeed(0);
                        break;

                    case TutorialStepType.CustomCommand:
                        {
                            HandleUIHighlight(step);
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
                                if (_currencyManager != null)
                                {
                                    _currencyManager.GiveSeals(_currencyManager.MaxSeals);
                                    if (_showDebugLogs) Debug.Log("[tutorial] CustomCommand: GrantMaxSeals executed.");
                                }
                                else
                                {
                                    if (_showDebugLogs) Debug.LogWarning("[tutorial] CustomCommand GrantMaxSeals: BattleCurrencyManager dependency is missing!");
                                }
                            }
                            else if (step.ActionKey == "AwakenLilith")
                            {
                                if (_saveManager != null)
                                {
                                    _saveManager.AwakenLilith();
                                    
                                    // Start async load from Addressables and WAIT for it to complete
                                    // before moving to the next tutorial step
                                    yield return StartCoroutine(LoadAndAwakenLilith());
                                    
                                    if (_showDebugLogs) Debug.Log("[tutorial] CustomCommand: AwakenLilith started (Addressables).");
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

            // Force victory at the end of tutorial levels if it hasn't been triggered yet
            if (_gameManager != null && !_gameManager.IsGameEnded)
            {
                Debug.Log("[tutorial] Tutorial ended. Triggering Level Victory.");
                _gameManager.Victory();
            }
        }
        #endregion

        #region Visuals & Highlighting
        private void HandleUIHighlight(TutorialStep step)
        {
            if (step == null) return;

            if (step.ResetBlocker)
            {
                _uiBlocker.ClearTargets();
                _handUI.Hide();
            }

            if (!step.UseBlocker)
            {
                _uiBlocker.HideBlocker();
                if (!step.ShowHand && !step.DragShowHand) _handUI.Hide();
                return;
            }

            if (step.FullBlocker)
            {
                _uiBlocker.ShowBlockerWithDetailedTargets(null, null);
                if (!step.ShowHand && !step.DragShowHand) _handUI.Hide();
                return;
            }

            List<UIPopupBlocker.UIHighlightData> uiHits = new List<UIPopupBlocker.UIHighlightData>();
            List<UIPopupBlocker.WorldHighlightData> worldHighlights = new List<UIPopupBlocker.WorldHighlightData>();

            List<UITarget> uiTargets = new List<UITarget>();
            
            bool isSkillTargeting = _interactionManager != null && _interactionManager.IsSkillTargeting;
            bool isSkillStep = step.ActionKey == "SkillUsed";
            bool isPlacementStep = step.ActionKey == "UnitPlaced";
            bool isDragging = _interactionManager != null && _interactionManager.IsDragging;

            if (isSkillStep)
            {
                if (isSkillTargeting)
                {
                    // If we are targeting, ONLY show the additional targets (the units on the field)
                    if (step.AdditionalTargetUI != null) uiTargets.AddRange(step.AdditionalTargetUI);
                }
                else
                {
                    // If we are not targeting yet, ONLY show the primary target (the skill button)
                    if (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) uiTargets.Add(step.TargetUI);
                }
            }
            else if (isPlacementStep)
            {
                if (isDragging)
                {
                    // While dragging, we focus on the tiles (handled in the Tiles section below)
                    // We might still want to highlight some UI if needed, but usually we don't
                }
                else
                {
                    // Before dragging, highlight the unit button
                    if (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) uiTargets.Add(step.TargetUI);
                }
            }
            else
            {
                if (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) uiTargets.Add(step.TargetUI);
                if (step.AdditionalTargetUI != null) uiTargets.AddRange(step.AdditionalTargetUI);
            }

            foreach (var ut in uiTargets)
            {
                string unitName = ut.Name;
                if (unitName.StartsWith("Enemy_")) unitName = unitName.Replace("Enemy_", "");
                else if (unitName.StartsWith("Unit_")) unitName = unitName.Replace("Unit_", "");

                PlayerUnit pu = PlayerUnit.ActiveUnits.FirstOrDefault(u => u.name == unitName || u.name.Contains(unitName) || u.gameObject.name == ut.Name);
                EnemyUnit eu = pu == null ? EnemyUnit.ActiveEnemies.FirstOrDefault(u => u.name == ut.Name || u.name.Contains(unitName)) : null;

                if (pu != null || eu != null)
                {
                    Transform t = pu != null ? pu.transform : eu.transform;
                    Vector2 size = (ut.Size != Vector2.zero) ? ut.Size : _unitWorldHoleSizeDefault;
                    // Multiply size by specific offset if needed, or just use default unit settings.
                    worldHighlights.Add(new UIPopupBlocker.WorldHighlightData
                    {
                        Position = t.position + new Vector3(0, _unitWorldHoleYOffset, 0),
                        Size = size,
                        Height = 0f
                    });
                }
                else
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
            }

            if (step.TargetTiles != null && step.TargetTiles.Count > 0)
            {
                // Tiles are ONLY cut if:
                // 1. It's NOT a placement step (highlight-only steps)
                // 2. It IS a placement step AND the player is currently dragging
                bool shouldShowTiles = !isPlacementStep || isDragging;

                if (shouldShowTiles)
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
            }

            _uiBlocker.ShowBlockerWithDetailedTargets(uiHits, worldHighlights);

            if (step.DragShowHand && (uiHits.Count > 0 || worldHighlights.Count > 0))
            {
                Vector2 startPos = Vector2.zero;
                if (uiHits.Count > 0) startPos = (Vector2)uiHits[0].Target.position + uiHits[0].Offset;
                else if (worldHighlights.Count > 0) startPos = Camera.main.WorldToScreenPoint(worldHighlights[0].Position);

                if (step.HandTargetUIOverride != null && !string.IsNullOrEmpty(step.HandTargetUIOverride.Name))
                {
                    if (GetTargetScreenPositionAndScale(step.HandTargetUIOverride, out Vector2 targetPos, out float scaleMult))
                    {
                        float finalScale = step.HandScale * scaleMult;
                        _handUI.MoveHand(startPos, targetPos, finalScale);
                    }
                    else _handUI.MoveHand(startPos, Vector2.zero, step.HandScale);
                }
                else
                {
                    Vector3 worldTarget = GetWorldPosForTile(step.HandTargetTileOverride) + step.HandTargetTileOffsetOverride;
                    Vector2 screenTarget = Camera.main.WorldToScreenPoint(worldTarget);
                    _handUI.MoveHand(startPos, screenTarget, step.HandScale);
                }
            }
            else if (step.ShowHand)
            {
                Vector2 handPos = Vector2.zero;
                float handScale = step.HandScale;

                if (step.HandTargetUIOverride != null && !string.IsNullOrEmpty(step.HandTargetUIOverride.Name))
                {
                    if (GetTargetScreenPositionAndScale(step.HandTargetUIOverride, out Vector2 targetPos, out float scaleMult))
                    {
                        handPos = targetPos;
                        handScale *= scaleMult;
                    }
                }
                else if (step.HandTargetTileOverride != Vector2Int.zero)
                {
                    Vector3 worldTarget = GetWorldPosForTile(step.HandTargetTileOverride) + step.HandTargetTileOffsetOverride;
                    handPos = Camera.main.WorldToScreenPoint(worldTarget);
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
                else if (worldHighlights.Count > 0) 
                {
                    handPos = Camera.main.WorldToScreenPoint(worldHighlights[0].Position);
                }

                if (handPos != Vector2.zero) 
                {
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
                
                Canvas canvas = go.GetComponentInChildren<Canvas>(true);
                if (canvas != null) return canvas.GetComponent<RectTransform>() ?? canvas.transform as RectTransform;
            }

            return null;
        }

        private bool GetTargetScreenPositionAndScale(UITarget ut, out Vector2 screenPos, out float scaleMultiplier)
        {
            screenPos = Vector2.zero;
            scaleMultiplier = 1f;

            string unitName = ut.Name;
            if (unitName.StartsWith("Enemy_")) unitName = unitName.Replace("Enemy_", "");
            else if (unitName.StartsWith("Unit_")) unitName = unitName.Replace("Unit_", "");

            PlayerUnit pu = PlayerUnit.ActiveUnits.FirstOrDefault(u => u.name == unitName || u.name.Contains(unitName) || u.gameObject.name == ut.Name);
            EnemyUnit eu = pu == null ? EnemyUnit.ActiveEnemies.FirstOrDefault(u => u.name == ut.Name || u.name.Contains(unitName)) : null;

            if (pu != null || eu != null)
            {
                Transform t = pu != null ? pu.transform : eu.transform;
                Vector3 worldPos = t.position + new Vector3(0, _unitWorldHoleYOffset, 0);
                screenPos = Camera.main.WorldToScreenPoint(worldPos);
                scaleMultiplier = ut.Size.x != 0 ? ut.Size.x : 1f;
                return true;
            }

            RectTransform drt = FindTargetRect(ut.Name);
            if (drt != null)
            {
                Vector3[] corners = new Vector3[4];
                drt.GetWorldCorners(corners);
                Vector3 center = (corners[0] + corners[2]) * 0.5f;
                Vector3 size = corners[2] - corners[0];
                screenPos = (Vector2)center + new Vector2(size.x * ut.SizeOffset.x, size.y * ut.SizeOffset.y);
                scaleMultiplier = ut.Size.x != 0 ? ut.Size.x : 1f;
                return true;
            }

            return false;
        }
        #endregion

        #region Tile Helpers
        private List<Vector2Int> _highlightedTiles = new List<Vector2Int>();
        private void HighlightTile(Vector2Int coord)
        {
            var tile = _gridManager.GetTileAt(coord);
            if (tile != null)
            {
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
                    pos = tile.transform.position;
                }
                else
                {
                    pos = _gridManager.GridToWorldPosition(tileCoord);
                }

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
                    _gameManager.SetSpeed(1); 
                }
            }
        }

        public bool IsWaitingForAction(string actionKey)
        {
            return _waitingForAction && _waitingActionKey == actionKey;
        }

        public string GetCurrentStepActionKey()
        {
            if (!IsInTutorial || _activeTutorial == null || _currentStepIndex < 0 || _currentStepIndex >= _activeTutorial.Steps.Count)
                return string.Empty;
            return _activeTutorial.Steps[_currentStepIndex].ActionKey;
        }

        public List<Vector2Int> GetRequiredPlacementTiles()
        {
            List<Vector2Int> allowed = new List<Vector2Int>();
            if (currentStep == null) return allowed;

            if (currentStep.ActionKey == "UnitPlaced")
            {
                if (currentStep.HandTargetTileOverride != Vector2Int.zero && currentStep.HandTargetTileOverride != new Vector2Int(-1, -1))
                {
                    allowed.Add(currentStep.HandTargetTileOverride);
                }
            }

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
                        return met;
                    }
                    return false;
                }

                case "WaveFinishedSpawning":
                    if (_enemyManager != null)
                    {
                        bool met = !_enemyManager.IsSpawning;
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
                        return met;
                    }
                    return false;
                }

                default:
                    return false;
            }
        }

        private bool CheckStepAlreadyCompleted(TutorialStep step)
        {
            if (step == null) return false;

            // If it's a condition step, reuse the existing logic
            if (step.Type == TutorialStepType.WaitForCondition)
            {
                return CheckCondition(step);
            }

            // If it's an action step, check the buffer or specific conditions
            if (step.Type == TutorialStepType.WaitForAction)
            {
                if (_triggeredActionsBuffer.Contains(step.ActionKey))
                {
                    return true;
                }

                // Special case for UnitPlaced
                if (step.ActionKey == "UnitPlaced")
                {
                    if (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name))
                    {
                        string unitName = step.TargetUI.Name;
                        if (unitName.StartsWith("Unit_")) unitName = unitName.Replace("Unit_", "");
                        if (unitName.StartsWith("Enemy_")) unitName = unitName.Replace("Enemy_", "");

                        // Check if a unit with this name exists in the active units
                        bool alreadyPlaced = PlayerUnit.ActiveUnits.Any(u => u != null && u.Data != null && 
                            (u.Data.UnitName == unitName || u.name == unitName || u.name.Contains(unitName)));
                        
                        if (alreadyPlaced) return true;
                    }
                }
                
                // Special case for UnitSelected
                if (step.ActionKey == "UnitSelected")
                {
                    if (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name))
                    {
                        string unitName = step.TargetUI.Name;
                        if (unitName.StartsWith("Unit_")) unitName = unitName.Replace("Unit_", "");
                        
                        if (_interactionManager != null && _interactionManager.InspectedUnit != null)
                        {
                            var selected = _interactionManager.InspectedUnit;
                            return selected.Data != null && (selected.Data.UnitName == unitName || selected.name.Contains(unitName));
                        }
                    }
                }
            }

            return false;
        }
        private IEnumerator LoadAndAwakenLilith()
        {
            if (_showDebugLogs) Debug.Log("[tutorial] Loading Lilith from Addressables (Char_Lilith_UnitData)...");
            
            var handle = Addressables.LoadAssetAsync<UnitData>("Char_Lilith_UnitData");
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                UnitData lilithData = handle.Result;
                if (_deploymentUI != null)
                {
                    _deploymentUI.AddUnit(lilithData);
                    _deploymentUI.SetUnitButtonVisibility("Lilith", true);
                    if (_showDebugLogs) Debug.Log($"[tutorial] Lilith '{lilithData.UnitName}' successfully added to DeploymentUI.");
                }
                else
                {
                    Debug.LogWarning("[tutorial] DeploymentUI is missing, cannot add Lilith!");
                }
            }
            else
            {
                Debug.LogError("[tutorial] Failed to load Lilith from Addressables! Check if 'Char_Lilith_UnitData' address is correct.");
            }
        }
        #endregion
    }
}
