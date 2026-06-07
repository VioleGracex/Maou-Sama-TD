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
using MaouSamaTD.Skills;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MaouSamaTD.Managers
{
    public class TutorialManager : MonoBehaviour
    {
        #region Dependencies
        [Inject(Optional = true)] private DialogueManager _dialogueManager;
        [Inject(Optional = true)] private GameManager _gameManager;
        [Inject(Optional = true)] private Grid.GridManager _gridManager;
        [Inject(Optional = true)] private InteractionManager _interactionManager;
        [Inject(Optional = true)] private TutorialHandUI _handUI;
        [Inject(Optional = true)] private UIPopupBlocker _uiBlocker;
        [Inject(Optional = true)] private EnemyManager _enemyManager;
        [Inject(Optional = true)] private UnitInspectorUI _unitInspectorUI;
        [Inject(Optional = true)] private DeploymentUI _deploymentUI;
        [Inject(Optional = true)] private BattleCurrencyManager _currencyManager;
        [Inject(Optional = true)] private MaouSamaTD.Managers.SaveManager _saveManager;
        [Inject(Optional = true)] private MaouSamaTD.Skills.SkillManager _skillManager;
        #endregion

        #region Serialized Settings
        [Header("Tutorial Visual Config")]
        [SerializeField] private Vector3 _tileHighlightOffset = new Vector3(0, -0.4f, 0);
        
        [Header("World Hole Settings")]
        [SerializeField] private Vector2 _unitWorldHoleSizeDefault = Vector2.one;
        [SerializeField] private float _unitWorldHoleYOffset = 1.0f;

        [Header("Debug")]
        [SerializeField] private bool _showDebugLogs = true;

        [Header("Level 2 Lilith Sealed Settings")]
        [SerializeField] private Sprite _lilithSealedSprite;
        [SerializeField] private Vector2Int _lilithSealedCoordinate = new Vector2Int(9, 11);
        #endregion
        
        #region State
        public bool IsInTutorial { get; private set; }
        public TutorialDataSO ActiveTutorial => _activeTutorial;
        private TutorialDataSO _activeTutorial;
        private int _currentStepIndex = -1;
        private TutorialStep _currentStep;
        private bool _waitingForAction = false;
        private string _waitingActionKey;
        private HashSet<string> _triggeredActionsBuffer = new HashSet<string>();
        private TutorialStep currentStep => _currentStep;
        private bool _isWaitingForDialogueCondition = false;
        private bool _bossPhasedTriggered = false;
        private int _currentStepMissCount = 0;
        private int _nextStepIndexOverride = -1;
        private Dictionary<string, RectTransform> _uiTargetCache = new Dictionary<string, RectTransform>();
        private GameObject _lilithSealedInstance;

        public int CurrentLevelIndex
        {
            get
            {
                if (_gameManager != null && _gameManager.CurrentLevelData != null)
                {
                    return _gameManager.CurrentLevelData.LevelIndex;
                }
                // Fallback to name-based detection if GameManager/CurrentLevelData is not available
                if (_activeTutorial != null)
                {
                    if (_activeTutorial.name.Contains("Level2")) return 2;
                    if (_activeTutorial.name.Contains("Level1")) return 1;
                }
                return -1;
            }
        }
        #endregion

        #region Public API
        private int FindStepIndexByName(string stepName)
        {
            if (_activeTutorial == null || string.IsNullOrEmpty(stepName)) return -1;
            for (int i = 0; i < _activeTutorial.Steps.Count; i++)
            {
                if (_activeTutorial.Steps[i] != null && _activeTutorial.Steps[i].StepName == stepName)
                {
                    return i;
                }
            }
            return -1;
        }

        public void OnRiteUsed(SovereignRiteData skill, Vector3 targetPosition, UnitBase targetUnit)
        {
            if (_currentStep == null || !_currentStep.EnableMissInterception || skill == null) return;

            // Check if the boss is still alive/unhurt
            string bossName = _currentStep.MissTargetBossName;
            var boss = EnemyUnit.ActiveEnemies.FirstOrDefault(e => e.EnemyData != null && e.EnemyData.EnemyName == bossName);
            if (boss != null && !boss.IsDead)
            {
                if (_showDebugLogs) Debug.Log($"[tutorial] Rite missed boss '{bossName}'. Triggering retry/miss interception.");

                // Refund seals
                if (_currencyManager != null)
                {
                    _currencyManager.GiveSeals(skill.SealCost);
                }

                // Reset cooldown
                _skillManager?.ForceSetReady(skill);

                // Handle Failure Branch Jumping
                if (!string.IsNullOrEmpty(_currentStep.OnFailJumpToStepName))
                {
                    int targetIndex = FindStepIndexByName(_currentStep.OnFailJumpToStepName);
                    if (targetIndex >= 0)
                    {
                        if (_showDebugLogs) Debug.Log($"[tutorial] Rite missed. Jumping to fail branch step: '{_currentStep.OnFailJumpToStepName}' (Index {targetIndex})");
                        _nextStepIndexOverride = targetIndex;
                        _waitingForAction = false; // Stop waiting so the routine advances immediately
                        _currentStepMissCount++;
                        return;
                    }
                }

                // Fallback to standard ConsecutiveMissDialogues list if OnFailJumpToStepName is not specified
                if (_currentStep.ConsecutiveMissDialogues != null && _currentStep.ConsecutiveMissDialogues.Count > 0)
                {
                    int index = Mathf.Min(_currentStepMissCount, _currentStep.ConsecutiveMissDialogues.Count - 1);
                    DialogueData dialogue = _currentStep.ConsecutiveMissDialogues[index];
                    bool isThirdOrGreaterMiss = (_currentStepMissCount >= 2); // 3rd miss is index 2 (starting at 0)

                    if (dialogue != null && _dialogueManager != null)
                    {
                        _gameManager?.SetSpeed(0, true);
                        _dialogueManager.StartDialogue(dialogue, () =>
                        {
                            if (_showDebugLogs) Debug.Log("[tutorial] Custom Miss/Retry Dialogue Finished.");
                            
                            if (isThirdOrGreaterMiss)
                            {
                                if (_showDebugLogs) Debug.Log("[tutorial] Third miss reached! Lilith finishes the boss.");
                                
                                // Kill the boss
                                var targetBoss = EnemyUnit.ActiveEnemies.FirstOrDefault(e => e.EnemyData != null && e.EnemyData.EnemyName == bossName);
                                if (targetBoss != null)
                                {
                                    targetBoss.PreventDeathForTutorial = false; // Lift immortality
                                    targetBoss.Die(); // Kill the boss
                                    
                                    // User requested: resume time and hide blocker then wait 2 seconds then show next step
                                    _gameManager?.SetSpeed(1, true);
                                    _uiBlocker.HideBlocker();
                                    _handUI.Hide();
                                    StartCoroutine(DelayedCompleteWaitForAction(2.0f));
                                }
                                else
                                {
                                    // Fallback if boss is already gone
                                    _waitingForAction = false;
                                }
                            }
                        });
                    }
                }

                _currentStepMissCount++;
            }
        }

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
            _bossPhasedTriggered = false;
            _nextStepIndexOverride = -1;

            // Level 2 Start Logic: Set initial seals to 50
            if (CurrentLevelIndex == 2)
            {
                if (_currencyManager != null)
                {
                    _currencyManager.SetMaxSeals(50);
                    _currencyManager.SetSeals(50);
                    if (_showDebugLogs) Debug.Log("[tutorial] Level 2 Initialized: Seals set to 50.");
                }
                SpawnLilithSealedVisual();
            }

            // Level 1 Start Logic: Ensure Sovereign Rite Panel is strictly hidden
            if (CurrentLevelIndex == 1)
            {
                var skillPanel = FindAnyObjectByType<MaouSamaTD.UI.Skills.SkillPanelUI>();
                if (skillPanel != null)
                {
                    skillPanel.gameObject.SetActive(false);
                    skillPanel.HideToggle();
                    if (_showDebugLogs) Debug.Log("[tutorial] Level 1 Initialized: Forcing SkillPanelUI to SetActive(false) and hiding toggle.");
                }
            }
            
            EnsureUIComponentsActive();
            
            if (_showDebugLogs) Debug.Log($"[tutorial] Starting Tutorial Routine with {data.Steps.Count} steps.");
            StartCoroutine(TutorialRoutine());
        }

        /// <summary>Externally hides the tutorial hand (e.g. when a panel is toggled).</summary>
        public void HideHand()
        {
            if (_handUI != null) _handUI.Hide();
        }

        /// <summary>Purges all tutorial systems for levels without tutorials.</summary>
        public void Purge()
        {
            if (_showDebugLogs) Debug.Log("[tutorial] Purge called. Disabling TutorialManager and hiding visuals.");
            
            IsInTutorial = false;
            _gameManager?.SetSpeed(1);
            StopAllCoroutines();
            
            if (_dialogueManager != null) _dialogueManager.HideDialogue();
            if (_uiBlocker != null) _uiBlocker.HideBlocker(true);
            if (_handUI != null)
            {
                _handUI.Hide();
                _handUI.gameObject.SetActive(false);
            }
            if (_interactionManager != null) _interactionManager.IsSelectionLocked = false;

            this.enabled = false;
        }
        #endregion

        #region Lifecycle
        private void OnEnable()
        {
            MaouSamaTD.Units.BossPhaseAbility.OnPhaseTriggered += HandleBossPhaseTriggered;
        }

        private void OnDisable()
        {
            MaouSamaTD.Units.BossPhaseAbility.OnPhaseTriggered -= HandleBossPhaseTriggered;
        }

        private void HandleBossPhaseTriggered(MaouSamaTD.Units.EnemyUnit boss)
        {
            _bossPhasedTriggered = true;
            if (_showDebugLogs) Debug.Log("[tutorial] Boss phase triggered event received by TutorialManager!");
        }

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
            _currentStep = null;
            
            if (_dialogueManager != null) _dialogueManager.HideDialogue();
            if (_uiBlocker != null) _uiBlocker.HideBlocker(true);
            if (_handUI != null) _handUI.Hide();
            if (_interactionManager != null) _interactionManager.IsSelectionLocked = false;
            
            // Cleanup invincibility
            if (_gameManager != null) _gameManager.PreventDeathForTutorial = false;
        }

        private bool _isSkillTargetingLastFrame = false;
        private bool _isDraggingLastFrame = false;
        private bool _isPlacementModeLastFrame = false;
        private bool _isSkillPanelVisibleLastFrame = false;
        private bool _wasTargetActiveLastFrame = false;
        private float _nextHighlightRefreshTime = 0f;

        private void Update()
        {
            if (!IsInTutorial || _activeTutorial == null || _currentStepIndex >= _activeTutorial.Steps.Count) return;

            // Prevent UI highlights or blockers from activating prematurely during wait states
            if (_isWaitingForDialogueCondition) return;

            var step = _activeTutorial.Steps[_currentStepIndex];
            
            // Periodically check if targets are still valid/active (e.g. if user closes a menu)
            if (Time.unscaledTime > _nextHighlightRefreshTime)
            {
                _nextHighlightRefreshTime = Time.unscaledTime + 0.1f; // Much faster refresh for responsive UI
                if (step.UseBlocker && step.TargetUI != null)
                {
                    var rt = FindTargetRect(step.TargetUI.Name);
                    bool isActive = rt != null && rt.gameObject.activeInHierarchy;
                    if (isActive != _wasTargetActiveLastFrame)
                    {
                        _wasTargetActiveLastFrame = isActive;
                        if (_showDebugLogs) Debug.Log($"[tutorial] Target active state changed to {isActive}. Refreshing highlights.");
                        HandleUIHighlight(step);
                    }
                    else if (!isActive)
                    {
                        HandleUIHighlight(step);
                    }
                }
            }

            // Every frame, check if there is a world unit highlight target and refresh UI highlight so the cutout dynamically follows it
            if (step.UseBlocker)
            {
                bool hasWorldUnit = false;
                if (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name))
                {
                    string targetName = step.TargetUI.Name;
                    if (targetName.StartsWith("Enemy_")) targetName = targetName.Replace("Enemy_", "");
                    else if (targetName.StartsWith("Unit_")) targetName = targetName.Replace("Unit_", "");

                    hasWorldUnit = PlayerUnit.ActiveUnits.Any(u => u.name == targetName || u.name.Contains(targetName) || u.gameObject.name == step.TargetUI.Name) ||
                                        EnemyUnit.ActiveEnemies.Any(u => u.name == step.TargetUI.Name || u.name.Contains(targetName));
                }

                if (hasWorldUnit || step.StepName == "One-Shot Rite" || step.StepName == "Boss Bypasses!" || step.StepName == "Boss Bypasses ignis")
                {
                    HandleUIHighlight(step);
                }
            }

            // Dynamic Skill Targeting & Panel Visibility logic: Update hand and blocker when switching between skill selection, unit targeting, or menu toggle.
            if (step.ActionKey == "SkillUsed" || step.ActionKey == "RiteMenuOpened")
            {
                bool isTargeting = _interactionManager != null && _interactionManager.IsSkillTargeting;
                var skillPanel = FindObjectOfType<MaouSamaTD.UI.Skills.SkillPanelUI>();
                bool isPanelVisible = skillPanel != null && skillPanel.IsVisible;

                if (isTargeting != _isSkillTargetingLastFrame || isPanelVisible != _isSkillPanelVisibleLastFrame)
                {
                    _isSkillTargetingLastFrame = isTargeting;
                    _isSkillPanelVisibleLastFrame = isPanelVisible;
                    if (_showDebugLogs) Debug.Log($"[tutorial] Skill state changed (Targeting: {isTargeting}, PanelVisible: {isPanelVisible}). Refreshing highlights.");
                    HandleUIHighlight(step);
                }
            }

            // Dynamic Unit Placement logic: Refresh highlights every frame while dragging or when placement mode is toggled (double-click).
            if (step.ActionKey == "UnitPlaced")
            {
                bool isDragging = _interactionManager != null && _interactionManager.IsDragging;
                bool isSelected = _interactionManager != null && _interactionManager.SelectedUnitData != null;
                bool isPlacementMode = isDragging || isSelected;

                if (isPlacementMode != _isPlacementModeLastFrame || isDragging != _isDraggingLastFrame)
                {
                    _isDraggingLastFrame = isDragging;
                    _isPlacementModeLastFrame = isPlacementMode;
                    if (_showDebugLogs) Debug.Log($"[tutorial] Placement mode changed (Dragging: {isDragging}, Selected: {isSelected}). Refreshing highlights.");
                    HandleUIHighlight(step);
                }
                
                // Keep refreshing while dragging for smooth movement (e.g. if we add cursor follow later)
                // but only if isDragging is true.
                if (isDragging)
                {
                    HandleUIHighlight(step);
                }

                // Ensure player continuously has enough seals to deploy during this placement step
                if (_currencyManager != null && step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name))
                {
                    int requiredCost = GetUnitDeploymentCost(step.TargetUI.Name);
                    if (requiredCost > 0 && _currencyManager.CurrentSeals < requiredCost)
                    {
                        _currencyManager.SetSeals(requiredCost);
                        if (_showDebugLogs) Debug.Log($"[tutorial] Update: Set seals to {requiredCost} for step '{step.StepName}' (unit: {step.TargetUI.Name})");
                    }
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
            bool isLevel1 = CurrentLevelIndex == 1;
            while (_currentStepIndex < _activeTutorial.Steps.Count)
            {
                TutorialStep step = _activeTutorial.Steps[_currentStepIndex];
                
                if (step.DelayBefore > 0 && !isLevel1)
                {
                    if (_showDebugLogs) Debug.Log($"[tutorial] Delaying for {step.DelayBefore}s before step {step.StepName}");
                    yield return new WaitForSecondsRealtime(step.DelayBefore);
                }

                if (_showDebugLogs) Debug.Log($"[tutorial] >>> Executing Step [{_currentStepIndex}]: {step.StepName} ({step.Type})");
                
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
                if (step.ActionKey == "UnitPlaced")
                {
                    var tiles = GetRequiredPlacementTiles();
                    if (tiles.Count > 0)
                    {
                        Debug.Log($"[Salavan] AllowedPlacementTile: {tiles[0].x},{tiles[0].y}");
                    }
                }
#endif

                ClearAllTileHighlights();

                // Hardening: Clear triggered action buffer for WaitForAction steps to prevent stale triggers from previous steps skipping them
                if (step.Type == TutorialStepType.WaitForAction && !string.IsNullOrEmpty(step.ActionKey))
                {
                    if (_showDebugLogs) Debug.Log($"[tutorial] Entering WaitForAction step '{step.StepName}'. Clearing action buffer to prevent stale skip.");
                    _triggeredActionsBuffer.Remove(step.ActionKey);
                    // Also remove generic SkillUsed if we are waiting for SkillUsed
                    if (step.ActionKey == "SkillUsed") _triggeredActionsBuffer.Remove("RiteUsed");
                    if (step.ActionKey == "RiteUsed") _triggeredActionsBuffer.Remove("SkillUsed");
                }

                // Skip step if already completed (e.g., unit already placed)
                if (CheckStepAlreadyCompleted(step))
                {
                    if (_showDebugLogs) Debug.Log($"[tutorial] Skipping Step [{_currentStepIndex}] {step.StepName} because it's already completed.");
                    _currentStepIndex++;
                    continue;
                }

                // Skip steps if the target rite button doesn't exist in the scene
                // (handles male/female rite mismatch or loadout differences gracefully)
                if (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name) &&
                    step.TargetUI.Name.StartsWith("SkillButton_"))
                {
                    bool mainRiteExists = IsRiteButtonAvailable(step.TargetUI.Name);
                    bool anyAdditionalRiteExists = false;
                    
                    if (step.AdditionalTargetUI != null)
                    {
                        foreach (var target in step.AdditionalTargetUI)
                        {
                            if (!string.IsNullOrEmpty(target.Name) && target.Name.StartsWith("SkillButton_"))
                            {
                                if (IsRiteButtonAvailable(target.Name))
                                {
                                    anyAdditionalRiteExists = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!mainRiteExists && !anyAdditionalRiteExists)
                    {
                        if (_showDebugLogs) Debug.Log($"[tutorial] Skipping Step [{_currentStepIndex}] {step.StepName}: no target rite buttons found in player loadout.");
                        _currentStepIndex++;
                        continue;
                    }
                }
                
                _currentStep = step;
                _currentStepMissCount = 0;
                _uiTargetCache.Clear(); // Clear cache for new step
                _wasTargetActiveLastFrame = false;

                // Reset frame-state tracking variables for the new step to avoid stale transitions
                _isSkillTargetingLastFrame = _interactionManager != null && _interactionManager.IsSkillTargeting;
                var currentSkillPanel = FindObjectOfType<MaouSamaTD.UI.Skills.SkillPanelUI>();
                _isSkillPanelVisibleLastFrame = currentSkillPanel != null && currentSkillPanel.IsVisible;
                // Special check for Level 2: Ensure player has enough seals for AOE/Boss phase
                if (CurrentLevelIndex == 2 && 
                    (step.StepName == "Mobs Swarming!" || step.StepName == "Teach AOE Rite" || step.StepName == "Cast AOE Rite" || step.StepName == "Boss Appears" || step.StepName.Contains("Rite")))
                {
                    if (_currencyManager != null)
                    {
                        // Give 99 seals for the boss phase as requested
                        int targetSeals = step.StepName.Contains("Rite") ? 99 : 25;
                        if (_currencyManager.CurrentSeals < targetSeals)
                        {
                            _currencyManager.SetSeals(targetSeals);
                            if (_showDebugLogs) Debug.Log($"[tutorial] Ensuring player has enough seals for step: {step.StepName}. Set seals to {targetSeals}.");
                        }
                    }
                }

                // Hardening: Automatically reset skill/rite cooldowns when entering a step that requires using them.
                // This prevents soft-locks if the player accidentally used a skill earlier and it's on cooldown.
                if (step.ActionKey == "SkillUsed" || step.ActionKey == "RiteUsed" || step.StepName.Contains("Rite") || step.StepName.Contains("Skill"))
                {
                    _skillManager?.ResetAllCooldowns();
                }
                
                // Reset visuals once at the start of the step if requested
                if (step.ResetBlocker)
                {
                    _uiBlocker?.ClearTargets();
                    _handUI?.Hide();
                }

                // 1. Handle StopTime for all step types at the start
                bool shouldStop = step.StopTime;
                if (step.ActionKey == "BossPassedUnit" || step.ActionKey == "BossReachedIgnis" || step.ActionKey == "BossBypass")
                {
                    shouldStop = false;
                }

                if (shouldStop)
                {
                    float delay = step.DelayBeforeStopTime;
                    if (step.StepName == "Boss Bypasses!" || step.StepName == "Boss Bypasses ignis" || step.StepName == "Boss is Bypassing Defenses!")
                    {
                        delay = 0.5f;
                    }

                    if (delay > 0f && !isLevel1)
                    {
                        if (_showDebugLogs) Debug.Log($"[tutorial] Step {step.StepName} requested StopTime with delay of {delay}s.");
                        StartCoroutine(DelayedTimeStop(delay, step.StepName));
                    }
                    else
                    {
                        if (_showDebugLogs) Debug.Log($"[tutorial] Step {step.StepName} requested StopTime. Pausing game.");
                        _gameManager.SetSpeed(0, true);
                    }
                }

                switch (step.Type)
                {
                    case TutorialStepType.DialogueOnly:
                        // TRIGGER: Shows a dialogue box. If an ActionKey is provided, it first waits for that condition to be met.
                        // NOTE: If dialogue is missing, it will proceed immediately or after the ActionKey condition.
                        {
                            // If an ActionKey is provided for a dialogue step, wait for that condition before showing it
                            if (!string.IsNullOrEmpty(step.ActionKey))
                            {
                                if (_showDebugLogs) Debug.Log($"[tutorial] DialogueOnly step {step.StepName} waiting for condition: {step.ActionKey}");
                                
                                // Ensure time flows if we are waiting for a dynamic condition
                                if (_gameManager.CurrentSpeed < 0.1f)
                                {
                                    _gameManager.SetSpeed(1);
                                }

                                _isWaitingForDialogueCondition = true;
                                _uiBlocker?.HideBlocker(true); // Hide blocker during wait/boss walk-up
                                _handUI?.Hide();               // Hide hand during wait
                                yield return new WaitUntil(() => CheckCondition(step));
                                yield return StartCoroutine(HandlePostActionDelay(step));
                                _isWaitingForDialogueCondition = false;
                            }

                            // Apply StopTime AFTER the condition wait, so the game pauses for the actual dialogue
                            if (step.StopTime)
                            {
                                float delay = step.DelayBeforeStopTime;
                                if (step.StepName == "Boss Bypasses!" || step.StepName == "Boss Bypasses ignis" || step.StepName == "Boss is Bypassing Defenses!")
                                {
                                    delay = 0.5f;
                                }

                                if (delay > 0f && !isLevel1)
                                {
                                    if (_showDebugLogs) Debug.Log($"[tutorial] Step {step.StepName} requested StopTime with delay of {delay}s (after condition).");
                                    StartCoroutine(DelayedTimeStop(delay, step.StepName));
                                }
                                else
                                {
                                    if (_showDebugLogs) Debug.Log($"[tutorial] Step {step.StepName} requested StopTime. Pausing game.");
                                    _gameManager.SetSpeed(0, true);
                                }
                            }

                            // Special Logic for Level 2 Boss Bypass: Lilith Refills Seals
                            if (CurrentLevelIndex == 2 && step.StepName == "Boss Bypasses!")
                            {
                                if (_currencyManager != null)
                                {
                                    _currencyManager.SetMaxSeals(99);
                                    _currencyManager.SetSeals(99);
                                    if (_showDebugLogs) Debug.Log("[tutorial] Lilith Bonus Applied: Seals set to 99.");
                                }
                                if (_gameManager != null)
                                {
                                    _gameManager.PreventDeathForTutorial = true;
                                    if (_showDebugLogs) Debug.Log("[tutorial] Player invincibility enabled for boss encounter.");
                                }
                            }

                            // USER REQUEST: Wait for dialogue to finish before showing highlights/blockers
                            bool dialogueDone = false;
                            if (step.Dialogue != null)
                            {
                                _dialogueManager.StartDialogue(step.Dialogue, () => 
                                {
                                    if (_showDebugLogs) Debug.Log($"[tutorial] Dialogue completed for step: {step.StepName}");
                                    dialogueDone = true;
                                });
                                yield return new WaitUntil(() => dialogueDone);
                            }
                            else
                            {
                                if (_showDebugLogs) Debug.LogWarning($"[tutorial] DialogueOnly step '{step.StepName}' has no Dialogue data. Skipping dialogue.");
                            }

                            if (step.UseBlocker)
                            {
                                HandleUIHighlight(step);
                            }
                            
                            _handUI.Hide();
                        }
                        break;

                    case TutorialStepType.HighlightUI:
                        // TRIGGER: Highlights a specific UI element and optionally shows dialogue.
                        {
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

                            // Highlight AFTER dialogue
                            HandleUIHighlight(step);
                            
                            // If this is just a highlight step without a wait, we might need a small delay or just proceed
                            if (string.IsNullOrEmpty(step.ActionKey))
                            {
                                if (!isLevel1)
                                    yield return new WaitForSecondsRealtime(0.5f);
                            }
                            else
                            {
                                _waitingForAction = true;
                                _waitingActionKey = step.ActionKey;
                                if (CheckStepAlreadyCompleted(step))
                                {
                                    if (_showDebugLogs) Debug.Log($"[tutorial] Action {step.ActionKey} is already satisfied upon entering wait, bypassing.");
                                    _waitingForAction = false;
                                }
                                else
                                {
                                    yield return new WaitUntil(() => !_waitingForAction);
                                }
                                yield return StartCoroutine(HandlePostActionDelay(step));
                            }

                            _handUI.Hide();
                        }
                        break;

                    case TutorialStepType.HighlightTile:
                        // TRIGGER: Highlights one or more world tiles and optionally shows dialogue.
                        {
                            bool tileDialogueDone = false;
                            if (step.Dialogue != null)
                            {
                                _dialogueManager.StartDialogue(step.Dialogue, () => 
                                {
                                    if (_showDebugLogs) Debug.Log($"[tutorial] Tile Highlight Dialogue completed for step: {step.StepName}");
                                    tileDialogueDone = true;
                                });
                                yield return new WaitUntil(() => tileDialogueDone);
                            }

                            HandleUIHighlight(step);
                            if (step.TargetTiles != null)
                            {
                                foreach (var wt in step.TargetTiles) HighlightTile(wt.Coordinate);
                            }
                            
                            if (string.IsNullOrEmpty(step.ActionKey))
                            {
                                if (!isLevel1)
                                    yield return new WaitForSecondsRealtime(0.5f);
                            }
                            else
                            {
                                _waitingForAction = true;
                                _waitingActionKey = step.ActionKey;
                                if (CheckStepAlreadyCompleted(step))
                                {
                                    if (_showDebugLogs) Debug.Log($"[tutorial] Action {step.ActionKey} is already satisfied upon entering wait, bypassing.");
                                    _waitingForAction = false;
                                }
                                else
                                {
                                    yield return new WaitUntil(() => !_waitingForAction);
                                }
                                
                                // USER REQUEST: Close UI blocker right away when action is triggered
                                // to avoid it 'floating' over the game while time is briefly resumed.
                                _uiBlocker.HideBlocker(isLevel1);
                                
                                yield return StartCoroutine(HandlePostActionDelay(step));
                            }

                            _handUI.Hide();
                            ClearAllTileHighlights();
                        }
                        break;

                    case TutorialStepType.WaitForAction:
                        // TRIGGER: Waits for a specific ActionKey (e.g., 'UnitPlaced', 'SkillUsed') to be triggered by the game.
                        {
                            if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for action: {step.ActionKey}");

                            if (step.Dialogue != null && step.Dialogue.Lines != null && step.Dialogue.Lines.Count > 0)
                            {
                                bool actionDialogueDone = false;
                                _dialogueManager.StartDialogue(step.Dialogue, () => actionDialogueDone = true);
                                yield return new WaitUntil(() => actionDialogueDone);
                            }

                            // Show highlight and hand AFTER dialogue
                            if (step.UseBlocker)
                            {
                                HandleUIHighlight(step);
                            }

                            // Auto-start wave if it hasn't started yet to prevent soft-locks
                            if (step.WaveIndex >= 0 && _enemyManager != null && !_enemyManager.HasWaveStarted(step.WaveIndex))
                            {
                                if (_showDebugLogs) Debug.Log($"[tutorial] Auto-starting wave {step.WaveIndex} for action step {step.StepName}");
                                _enemyManager.StartSpecificWave(step.WaveIndex);
                            }

                            if (step.ActionKey == "SkillUsed" && _unitInspectorUI != null)
                            {
                                _unitInspectorUI.IsLocked = true;
                            }

                            // Ensure the player has exactly enough seals to cast the skill in this step.
                            // We look up the actual SealCost from the loaded rite instead of using
                            // hardcoded values, so this works for both male and female rites.
                            if (_currencyManager != null && step.ActionKey == "SkillUsed" &&
                                step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name))
                            {
                                if (_skillManager != null) _skillManager.ResetAllCooldowns();
                                int requiredCost = 0;
                                if (step.TargetUI.Name != "Ult_Btn")
                                {
                                    requiredCost = GetRiteSealCostFromButtonName(step.TargetUI.Name);
                                }
                                if (requiredCost > 0 && _currencyManager.CurrentSeals < requiredCost)
                                {
                                    _currencyManager.SetSeals(requiredCost);
                                    if (_showDebugLogs) Debug.Log($"[tutorial] Set seals to {requiredCost} for step '{step.StepName}' (rite: {step.TargetUI.Name})");
                                }
                            }
                            
                            // Ensure the player has enough seals to place the unit in this step.
                            if (_currencyManager != null && step.ActionKey == "UnitPlaced" &&
                                step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name))
                            {
                                int requiredCost = GetUnitDeploymentCost(step.TargetUI.Name);
                                if (requiredCost > 0 && _currencyManager.CurrentSeals < requiredCost)
                                {
                                    _currencyManager.SetSeals(requiredCost);
                                    if (_showDebugLogs) Debug.Log($"[tutorial] Set seals to {requiredCost} for step '{step.StepName}' (unit placement: {step.TargetUI.Name})");
                                }
                            }
                            
                            _waitingForAction = true;
                            // Auto-skip "Open Rite Menu" if it's already open
                            if (step.ActionKey == "RiteMenuOpened")
                            {
                                var skillPanel = FindAnyObjectByType<MaouSamaTD.UI.Skills.SkillPanelUI>();
                                if (skillPanel != null && skillPanel.IsVisible)
                                {
                                    if (_showDebugLogs) Debug.Log("[tutorial] Rite Menu already open, auto-completing step.");
                                    _triggeredActionsBuffer.Add("RiteMenuOpened");
                                }
                            }

                            _waitingActionKey = step.ActionKey;
                            
                            if (CheckStepAlreadyCompleted(step))
                            {
                                if (_showDebugLogs) Debug.Log($"[tutorial] Action {step.ActionKey} is already satisfied upon entering wait, bypassing.");
                                _waitingForAction = false;
                            }
                            else if (_triggeredActionsBuffer.Contains(step.ActionKey))
                            {
                                if (_showDebugLogs) Debug.Log($"[tutorial] Action {step.ActionKey} found in buffer early, proceeding.");
                                _waitingForAction = false; 
                                _triggeredActionsBuffer.Remove(step.ActionKey);
                            }
                            else
                            {
                                yield return new WaitUntil(() => !_waitingForAction);
                            }
                            
                            yield return StartCoroutine(HandlePostActionDelay(step));
                            _handUI.Hide();

                            // If executing the ultimate on boss, remove death prevention
                            if (step.StepName == "Execute the Ultimate")
                            {
                                foreach (var boss in EnemyUnit.ActiveEnemies)
                                {
                                    if (boss != null && boss.PreventDeathForTutorial) boss.PreventDeathForTutorial = false;
                                }
                            }

                            if (step.ActionKey == "BossUsedPassive")
                            {
                                bool bossPhased = false;
                                System.Action<MaouSamaTD.Units.EnemyUnit> handler = (enemy) => { bossPhased = true; };
                                MaouSamaTD.Units.BossPhaseAbility.OnPhaseTriggered += handler;

                                // Prevent race condition: if boss HP is already <= 70%, it phased just before this step began
                                var boss = MaouSamaTD.Units.EnemyUnit.ActiveEnemies.FirstOrDefault(e => e.EnemyData != null && e.EnemyData.EnemyName == "Abyssal Shade");
                                if (boss != null && (boss.CurrentHp / boss.MaxHp) <= 0.701f)
                                {
                                    bossPhased = true;
                                }

                                if (!bossPhased)
                                {
                                    float bossPassiveTimeout = 30f;
                                    float bossPassiveTimer = 0f;
                                    yield return new WaitUntil(() =>
                                    {
                                        bossPassiveTimer += Time.deltaTime;
                                        // Safety: boss missing or dead means phase event will never fire
                                        var b = MaouSamaTD.Units.EnemyUnit.ActiveEnemies.FirstOrDefault(e => e.EnemyData?.EnemyName == "Abyssal Shade");
                                        if (b == null || b.IsDead)
                                        {
                                            if (_showDebugLogs) Debug.LogWarning("[tutorial] BossUsedPassive: Boss missing/dead — auto-resolving step.");
                                            return true;
                                        }
                                        if (bossPassiveTimer >= bossPassiveTimeout)
                                        {
                                            if (_showDebugLogs) Debug.LogWarning("[tutorial] BossUsedPassive: Timeout reached — auto-resolving step.");
                                            return true;
                                        }
                                        return bossPhased || !_waitingForAction;
                                    });
                                }
                                
                                MaouSamaTD.Units.BossPhaseAbility.OnPhaseTriggered -= handler;
                                _waitingForAction = false;
                                _triggeredActionsBuffer.Remove(step.ActionKey);
                            }

                            else if (_triggeredActionsBuffer.Contains(step.ActionKey))
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
                            
                            if (_showDebugLogs) Debug.Log($"[tutorial] Action {step.ActionKey} received.");
                        }
                        break;

                    case TutorialStepType.WaitTime:
                        // TRIGGER: A simple time delay in realtime seconds.
                        {
                            HandleUIHighlight(step);
                            if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for duration: {step.Duration}s");
                            // Ensure time flows for waiting, otherwise we hang
                            _gameManager.SetSpeed(1);
                            yield return new WaitForSeconds(step.Duration);
                        }
                        break;

                    case TutorialStepType.StartWave:
                        // Step is used to log/highlight, but EnemyManager handles actual spawning naturally.
                        {
                            HandleUIHighlight(step);
                            if (_showDebugLogs) Debug.Log($"[tutorial] Acknowledged Start Wave Index: {step.WaveIndex} (EnemyManager handles this natively)");
                        }
                        break;

                    case TutorialStepType.WaitForWave:
                        // TRIGGER: Waits until all enemies in the current wave are defeated and spawning is finished.
                        {
                            HandleUIHighlight(step);
                            if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for Wave completion (Index: {step.WaveIndex})");
                            if (step.ResumeTime) _gameManager.SetSpeed(1); 
                            else _gameManager.SetSpeed(0, true); 

                            yield return new WaitUntil(() => _enemyManager != null && _enemyManager.IsWaveCleared(step.WaveIndex));
                            if (_showDebugLogs) Debug.Log("[tutorial] Wave cleared.");
                        }
                        break;

                    case TutorialStepType.WaitForCondition:
                        // TRIGGER: Waits for a dynamic game state (e.g., 'BossHealth', 'EnemiesInRange') via CheckCondition().
                        {
                            HandleUIHighlight(step);
                            if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for condition: {step.ActionKey}");
                            
                            // Ensure time flows if we are waiting for a dynamic condition, UNLESS StopTime is requested!
                            bool shouldStopTime = step.StopTime || step.StepName == "One-Shot Rite";
                            
                            // Safety Override: Don't stop time for conditions that require enemy movement to progress
                            if (step.ActionKey == "BossPassedUnit" || step.ActionKey == "BossReachedIgnis" || step.ActionKey == "BossBypass")
                            {
                                shouldStopTime = false;
                                if (_showDebugLogs && _gameManager.CurrentSpeed < 0.1f) 
                                    Debug.Log("[tutorial] Safety Override: Resuming time for boss movement condition.");
                            }

                            if (shouldStopTime)
                            {
                                if (_gameManager.CurrentSpeed > 0.1f)
                                {
                                    _gameManager.SetSpeed(0, true);
                                }
                            }
                            else if (_gameManager.CurrentSpeed < 0.1f)
                            {
                                _gameManager.SetSpeed(1);
                            }

                            yield return new WaitUntil(() => CheckCondition(step));

                            // Post-condition delay for boss bypass so the player can see him teleport
                            if (step.ActionKey == "BossPassedUnit")
                            {
                                if (_showDebugLogs) Debug.Log("[tutorial] BossPassedUnit condition met. Delaying 0.75s so player can see the boss on the next tile.");
                                yield return new WaitForSecondsRealtime(0.75f);
                            }
                        }
                        break;

                    case TutorialStepType.CustomCommand:
                        // TRIGGER: Executes specific coded actions based on ActionKey (e.g., 'SetMaxAuthoritySeals', 'ChargeUnitUlt').
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
                                    if (_showDebugLogs) Debug.Log($"[tutorial] CustomCommand: {step.ActionKey} executed.");
                                }
                                else
                                {
                                    if (_showDebugLogs) Debug.LogWarning($"[tutorial] CustomCommand {step.ActionKey}: BattleCurrencyManager dependency is missing!");
                                }
                            }
                            else if (step.ActionKey == "SetMaxAuthoritySeals")
                            {
                                if (_currencyManager != null)
                                {
                                    _currencyManager.SetMaxSeals(step.RequiredCount);
                                    if (_showDebugLogs) Debug.Log($"[tutorial] CustomCommand: Max Seals set to {step.RequiredCount}.");
                                }
                            }
                            else if (step.ActionKey == "SetAuthoritySeals")
                            {
                                if (_currencyManager != null)
                                {
                                    if (step.RequiredCount > _currencyManager.MaxSeals)
                                    {
                                        _currencyManager.SetMaxSeals(step.RequiredCount);
                                        if (_showDebugLogs) Debug.Log($"[tutorial] SetAuthoritySeals: RequiredCount {step.RequiredCount} exceeds MaxSeals. Automatically increased MaxSeals first.");
                                    }
                                    _currencyManager.SetSeals(step.RequiredCount);
                                    if (_showDebugLogs) Debug.Log($"[tutorial] CustomCommand: Current Seals set to {step.RequiredCount}.");
                                }
                            }
                            else if (step.ActionKey == "AwakenLilith")
                            {
                                if (_lilithSealedInstance != null)
                                {
                                    _lilithSealedInstance.SetActive(false);
                                    if (_showDebugLogs) Debug.Log("[tutorial] Lilith unsealed: Hiding sealed visual.");
                                }
                                if (_saveManager != null)
                                {
                                    _saveManager.AwakenLilith();
                                    
                                    // Start async load from Addressables and WAIT for it to complete
                                    // before moving to the next tutorial step
                                    yield return StartCoroutine(LoadAndAwakenLilith());
                                    
                                    if (_showDebugLogs) Debug.Log("[tutorial] CustomCommand: AwakenLilith started (Addressables).");
                                }
                            }
                            else if (step.ActionKey == "ShowLilith")
                            {
                                if (_deploymentUI != null)
                                {
                                    _deploymentUI.SetUnitButtonVisibility("Lilith", true);
                                    if (_showDebugLogs) Debug.Log("[tutorial] CustomCommand: ShowLilith executed.");
                                }
                            }
                            else if (step.ActionKey == "SetPhasingAndImmunity")
                            {
                                string bossName = string.IsNullOrEmpty(targetName) ? "Abyssal Shade" : targetName;
                                var boss = EnemyUnit.ActiveEnemies.FirstOrDefault(e => e.EnemyData != null && e.EnemyData.EnemyName == bossName);
                                if (boss != null)
                                {
                                    // Set Phasing Charges
                                    var phasingField = typeof(EnemyUnit).GetField("_currentPhasingCharges", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                    if (phasingField != null)
                                    {
                                        phasingField.SetValue(boss, (int)step.RequiredCount);
                                        if (_showDebugLogs) Debug.Log($"[tutorial] CustomCommand: SetPhasingCharges to {step.RequiredCount} for {bossName}");
                                    }

                                    // Add Melee Immunity
                                    if (!boss.Immunities.Contains(DamageType.Melee))
                                    {
                                        boss.Immunities.Add(DamageType.Melee);
                                        if (_showDebugLogs) Debug.Log($"[tutorial] CustomCommand: Added MELEE IMMUNITY to {bossName}");
                                    }
                                }
                                else
                                {
                                    if (_showDebugLogs) Debug.LogWarning($"[tutorial] CustomCommand SetPhasingAndImmunity: Could not find boss '{bossName}'");
                                }
                            }
                            else if (step.ActionKey == "SetUnitButtonActive")
                            {
                                if (_deploymentUI != null)
                                {
                                     if (!string.IsNullOrEmpty(targetName))
                                     {
                                         bool active = (step.RequiredCount > 0);
                                         _deploymentUI.SetUnitButtonVisibility(targetName, active);
                                     }
                                     else if (_showDebugLogs)
                                     {
                                         Debug.LogWarning($"[tutorial] SetUnitButtonActive: TargetUI.Name is empty for step '{step.StepName}'");
                                     }
                                    if (_showDebugLogs) Debug.Log($"[tutorial] CustomCommand: SetUnitButtonActive for {targetName} to {step.RequiredCount > 0}");
                                }
                                else
                                {
                                    if (_showDebugLogs) Debug.LogWarning("[tutorial] CustomCommand SetUnitButtonActive: DeploymentUI is NULL!");
                                }
                            }
                            else if (step.ActionKey == "SetSpawnMapping")
                            {
                                if (_gridManager != null)
                                {
                                    Vector2Int spawnCoord = step.HandTargetTileOverride;
                                    int exitIndex = step.RequiredCount;
                                    _gridManager.SetSpawnMapping(spawnCoord, exitIndex);
                                    if (_showDebugLogs) Debug.Log($"[tutorial] CustomCommand SetSpawnMapping: Spawn {spawnCoord} now maps to Exit index {exitIndex}");
                                }
                                else
                                {
                                    if (_showDebugLogs) Debug.LogWarning("[tutorial] CustomCommand SetSpawnMapping: GridManager is NULL!");
                                }
                            }
                            else if (step.ActionKey == "SpawnEnemyClump")
                            {
                                if (_enemyManager != null)
                                {
                                    System.Action<EnemyData> spawnClump = (data) => 
                                    {
                                        if (data == null) return;
                                        for (int i = 0; i < 15; i++)
                                        {
                                            _enemyManager.SpawnEnemy(data, 0, i, 0);
                                        }
                                        if (_showDebugLogs) Debug.Log("[tutorial] CustomCommand: SpawnEnemyClump executed (15 enemies).");
                                    };

                                    // Try loading via Addressables with full path first
                                    string fullPath = "Assets/_Game/Data/Units/Enemies/Shadow/Regular/EnemySO_Lesser-Shadow.asset";
                                    Addressables.LoadAssetAsync<EnemyData>(fullPath).Completed += (handle) =>
                                    {
                                        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                                        {
                                            spawnClump(handle.Result);
                                        }
                                        else
                                        {
                                            // Fallback to short name
                                            Addressables.LoadAssetAsync<EnemyData>("EnemySO_Lesser-Shadow").Completed += (handle2) => 
                                            {
                                                if (handle2.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                                                {
                                                    spawnClump(handle2.Result);
                                                }
                                                else
                                                {
                                                    #if UNITY_EDITOR
                                                    // Final editor-only fallback
                                                    EnemyData editorData = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>(fullPath);
                                                    if (editorData != null)
                                                    {
                                                        spawnClump(editorData);
                                                    }
                                                    else
                                                    #endif
                                                    {
                                                        Debug.LogError("[tutorial] CustomCommand SpawnEnemyClump: Failed to load EnemyData asset via Addressables or Path!");
                                                    }
                                                }
                                            };
                                        }
                                    };
                                }
                            }

                            // After command execution, check for dialogue (fixes soft-lock where dialogue was skipped)
                            if (step.Dialogue != null && step.Dialogue.Lines != null && step.Dialogue.Lines.Count > 0)
                            {
                                bool cmdDialogueDone = false;
                                if (_showDebugLogs) Debug.Log($"[tutorial] Starting dialogue for CustomCommand step: {step.StepName}");
                                _dialogueManager.StartDialogue(step.Dialogue, () => cmdDialogueDone = true);
                                
                                if (step.UseBlocker) HandleUIHighlight(step);
                                
                                yield return new WaitUntil(() => cmdDialogueDone);
                            }
                        }
                        break;
                }
                
                if (step.ResumeTime) _gameManager.SetSpeed(1);

                if (_showDebugLogs) Debug.Log($"[tutorial] <<< Finished Step [{_currentStepIndex}]: {step.StepName}");
                
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                MaouSamaTD.Testing.GameStateExporter.PushEvent($"TutorialStepPassed:{step.StepName}");
#endif
                
                // Redirection Engine
                if (_nextStepIndexOverride >= 0)
                {
                    _currentStepIndex = _nextStepIndexOverride;
                    _nextStepIndexOverride = -1;
                }
                else if (!string.IsNullOrEmpty(step.OnCompleteJumpToStepName))
                {
                    int targetIndex = FindStepIndexByName(step.OnCompleteJumpToStepName);
                    if (targetIndex >= 0)
                    {
                        if (_showDebugLogs) Debug.Log($"[tutorial] Step {step.StepName} finished. Branching redirect to: '{step.OnCompleteJumpToStepName}' (Index {targetIndex})");
                        _currentStepIndex = targetIndex;
                    }
                    else
                    {
                        Debug.LogError($"[tutorial] Could not find redirect step name: '{step.OnCompleteJumpToStepName}' in tutorial steps!");
                        _currentStepIndex++;
                    }
                }
                else
                {
                    _currentStepIndex++;
                }
            }

            IsInTutorial = false;
            _activeTutorial = null;
            _currentStep = null;
            _gameManager.SetSpeed(1);
            if (_interactionManager != null) _interactionManager.IsSelectionLocked = false;
            
            if (_showDebugLogs) Debug.Log("[tutorial] Tutorial Sequence Completed.");
            _uiBlocker.HideBlocker(isLevel1);
            _handUI.Hide();
        }
        #endregion

        #region Visuals & Highlighting
        private void HandleUIHighlight(TutorialStep step)
        {
            if (step == null) return;

            bool isLevel1 = CurrentLevelIndex == 1;

            // Do not return early if dialogue is showing; we still want to process highlights
            // even if there is a dim, to allow 'holes' through the dimming layer.
            bool isDialogueShowing = _dialogueManager != null && _dialogueManager.DialogueUI != null && 
                                   _dialogueManager.DialogueUI.IsShowingDialogue;

            bool hasDialogue = _dialogueManager != null && _dialogueManager.IsDialogueActive;
            if (!step.UseBlocker && !hasDialogue)
            {
                _uiBlocker.HideBlocker(isLevel1);
                if (!step.ShowHand && !step.DragShowHand) _handUI.Hide();
                return;
            }

            if (step.FullBlocker)
            {
                List<UIPopupBlocker.UIHighlightData> fullHits = new List<UIPopupBlocker.UIHighlightData>();
                // Even with a full blocker, we MUST allow clicking the dialogue box
                if (_dialogueManager != null && _dialogueManager.DialogueUI != null && _dialogueManager.DialogueUI.IsShowingDialogue)
                {
                    RectTransform dialogueRT = _dialogueManager.DialogueUI.GetActivePanelRect();
                    if (dialogueRT != null)
                    {
                        fullHits.Add(new UIPopupBlocker.UIHighlightData { Target = dialogueRT, Size = Vector2.one });
                    }
                }
                _uiBlocker.ShowBlockerWithDetailedTargets(fullHits, null);
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
            bool isPlacementMode = _interactionManager != null && (isDragging || _interactionManager.SelectedUnitData != null);

            bool isDialogueActive = _dialogueManager != null && _dialogueManager.IsDialogueActive;

            if (isSkillStep && !isDialogueActive)
            {
                // Always show the skill button so its glow/state is visible even when targeting tiles
                if (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) uiTargets.Add(step.TargetUI);

                if (isSkillTargeting)
                {
                    // While targeting, also show additional targets (e.g. Ignis or other units)
                    if (step.AdditionalTargetUI != null) uiTargets.AddRange(step.AdditionalTargetUI);
                }
            }
            else if (isPlacementStep && !isDialogueActive)
            {
                if (isPlacementMode)
                {
                    // While in placement mode (dragging or selected), we focus on the tiles
                }
                else
                {
                    // Before selecting/dragging, highlight the unit button
                    if (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) uiTargets.Add(step.TargetUI);
                }
            }
            else if (!isDialogueActive)
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
                    float yOffset = _unitWorldHoleYOffset + ut.SizeOffset.y;
                    if (eu != null && eu.EnemyData != null && eu.EnemyData.EnemyName == "Abyssal Shade")
                    {
                        yOffset += 1.0f;
                    }
                    worldHighlights.Add(new UIPopupBlocker.WorldHighlightData
                    {
                        Position = t.position + new Vector3(0, yOffset, 0),
                        Size = size,
                        Height = 0f
                    });
                }
                else
                {
                    RectTransform rt = FindTargetRect(ut.Name);

                    // Special case: Ult_Btn is dynamically shown/hidden per-frame based on charge state.
                    // If FindTargetRect didn't find it active, search in the UnitInspector panel's children
                    // (including inactive) so we can still create the cutout while the panel is visible.
                    if (rt == null && ut.Name == "Ult_Btn" && _unitInspectorUI != null && _unitInspectorUI.PanelRect != null)
                    {
                        rt = _unitInspectorUI.PanelRect
                            .GetComponentsInChildren<RectTransform>(true)
                            .FirstOrDefault(r => r.name == "Ult_Btn");
                    }

                    if (rt != null) 
                    {
                        bool isSkillButton = ut.Name.Contains("SovereignRite") || ut.Name.Contains("SkillButton");
                        bool isMenuVisible = true;
                        
                        // If it's a skill button, check if the panel is actually visible (not slid off-screen)
                        if (isSkillButton)
                        {
                            var skillPanel = FindAnyObjectByType<MaouSamaTD.UI.Skills.SkillPanelUI>();
                            if (skillPanel != null && !skillPanel.IsVisible)
                            {
                                isMenuVisible = false;
                            }
                        }

                        // For Ult_Btn: treat as visible as long as the inspector panel itself is active,
                        // since the button's own active state is toggled per-frame by UpdateChargeVisuals().
                        bool isUltBtnTarget = ut.Name == "Ult_Btn";
                        bool isEffectivelyVisible = isUltBtnTarget
                            ? (_unitInspectorUI != null && _unitInspectorUI.IsPanelActive)
                            : (rt.gameObject.activeInHierarchy && isMenuVisible);

                        if (isEffectivelyVisible)
                        {
                            uiHits.Add(new UIPopupBlocker.UIHighlightData 
                            { 
                                 Target = rt, 
                                 Size = (ut.Size != Vector2.zero) ? ut.Size : Vector2.one,
                                 Offset = ut.SizeOffset
                            });
                        }
                        else if (isSkillButton)
                        {
                            // If a Rite button is inactive or the menu is off-screen,
                            // Highlight the toggle button so the user can reopen it.
                            RectTransform toggleRt = FindTargetRect("SovereignRiteToggle");
                            // Use activeSelf instead of activeInHierarchy because if the whole skills panel is inactive, 
                            // we still want to find and highlight the toggle if it's the target.
                            if (toggleRt != null)
                            {
                                uiHits.Add(new UIPopupBlocker.UIHighlightData 
                                { 
                                     Target = toggleRt, 
                                     Size = Vector2.one * 1.2f,
                                     Offset = Vector2.zero
                                });
                            }
                        }
                    }
                }
            }

            if (step.TargetTiles != null && step.TargetTiles.Count > 0 && !isDialogueActive)
            {
                // Tiles are ONLY cut if:
                // 1. It's NOT a placement step (highlight-only steps)
                // 2. It IS a placement step AND the player is currently in placement mode
                bool shouldShowTiles = !isPlacementStep || isPlacementMode;

                if (shouldShowTiles)
                {
                    foreach (var wt in step.TargetTiles)
                    {
                        Vector3 position = GetWorldPosForTile(wt.Coordinate) + wt.Offset;
                        if (step.StepName == "One-Shot Rite")
                        {
                            var boss = EnemyUnit.ActiveEnemies.FirstOrDefault(e => e.EnemyData != null && e.EnemyData.EnemyName == "Abyssal Shade");
                            if (boss != null)
                            {
                                position = boss.transform.position + new Vector3(0, _unitWorldHoleYOffset + 1.0f, 0);
                            }
                        }

                        worldHighlights.Add(new UIPopupBlocker.WorldHighlightData 
                        {
                            Position = position,
                            Size = wt.Size,
                            Height = wt.Height
                        });
                    }
                }
            }

            // DYNAMIC: Highlight tile boss stands on during "Boss Bypasses!" step
            if (!isDialogueActive && (step.StepName == "Boss Bypasses!" || step.StepName == "Boss Bypasses ignis"))
            {
                var boss = EnemyUnit.ActiveEnemies.FirstOrDefault(e => e.EnemyData != null && e.EnemyData.EnemyName == "Abyssal Shade");
                if (boss != null)
                {
                    Vector3 position = boss.transform.position + new Vector3(0, _unitWorldHoleYOffset + 1.0f, 0);
                    worldHighlights.Add(new UIPopupBlocker.WorldHighlightData 
                    {
                        Position = position,
                        Size = new Vector2(1.2f, 1.2f),
                        Height = 1.0f
                    });
                }
            }
            
            // Always allow clicking the dialogue box if it's active
            if (_dialogueManager != null && _dialogueManager.DialogueUI != null && _dialogueManager.DialogueUI.IsShowingDialogue)
            {
                RectTransform dialogueRT = _dialogueManager.DialogueUI.GetActivePanelRect();
                if (dialogueRT != null)
                {
                    // Add as a highlight target so it's not blocked
                    uiHits.Add(new UIPopupBlocker.UIHighlightData { Target = dialogueRT, Size = Vector2.one });
                }
            }

            // Always allow clicking the unit stats/inspector window if it's active
            if (_unitInspectorUI != null && _unitInspectorUI.IsPanelActive && _unitInspectorUI.PanelRect != null)
            {
                uiHits.Add(new UIPopupBlocker.UIHighlightData { Target = _unitInspectorUI.PanelRect, Size = Vector2.one });
            }

            if (isDialogueActive) _uiBlocker.SetSortingOrder(2999);
            else _uiBlocker.SetSortingOrder(50);

            // Hide the blocker when the unit inspector panel is open on Level 1,
            // UNLESS the step is specifically targeting the Ult_Btn ("Activate Ignis Skill" flow).
            bool isTargetingUltBtn = step.TargetUI != null && step.TargetUI.Name == "Ult_Btn";
            if (_unitInspectorUI != null && _unitInspectorUI.IsPanelActive && isLevel1 && !isTargetingUltBtn)
            {
                _uiBlocker.HideBlocker(isLevel1);
            }
            else
            {
                _uiBlocker.ShowBlockerWithDetailedTargets(uiHits, worldHighlights);
            }


            bool ignoreOverride = isSkillStep && isSkillTargeting;

            if (step.DragShowHand && (uiHits.Count > 0 || worldHighlights.Count > 0) && !_dialogueManager.IsDialogueActive)
            {
                if (isDragging || isSkillTargeting || isPlacementMode)
                {
                    _handUI.Hide();
                }
                else
                {
                    Vector2 startPos = Vector2.zero;
                    if (uiHits.Count > 0) startPos = (Vector2)uiHits[0].Target.position + uiHits[0].Offset;
                    else if (worldHighlights.Count > 0) startPos = Camera.main.WorldToScreenPoint(worldHighlights[0].Position);

                    if (!ignoreOverride && step.HandTargetUIOverride != null && !string.IsNullOrEmpty(step.HandTargetUIOverride.Name))
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
            }
            else if (step.ShowHand && !isDialogueActive)
            {
                if (isDragging || isSkillTargeting || isPlacementMode)
                {
                    _handUI.Hide();
                }
                else
                {
                    Vector2 handPos = Vector2.zero;
                    float handScale = step.HandScale;

                    if (!ignoreOverride && step.HandTargetUIOverride != null && !string.IsNullOrEmpty(step.HandTargetUIOverride.Name))
                    {
                        if (GetTargetScreenPositionAndScale(step.HandTargetUIOverride, out Vector2 targetPos, out float scaleMult))
                        {
                            handPos = targetPos;
                            handScale *= scaleMult;
                        }
                    }

                    if (handPos == Vector2.zero && uiHits.Count > 0) 
                    {
                        Vector3[] corners = new Vector3[4];
                        uiHits[0].Target.GetWorldCorners(corners);
                        Vector3 center = (corners[0] + corners[2]) * 0.5f;
                        handPos = (Vector2)center + uiHits[0].Offset;
                        handScale *= uiHits[0].Size.x;
                    }
                    else if (handPos == Vector2.zero && step.HandTargetTileOverride != Vector2Int.zero && (isPlacementMode || step.ActionKey != "UnitPlaced"))
                    {
                        Vector3 worldTarget = GetWorldPosForTile(step.HandTargetTileOverride) + step.HandTargetTileOffsetOverride;
                        handPos = Camera.main.WorldToScreenPoint(worldTarget);
                    }
                    else if (handPos == Vector2.zero && worldHighlights.Count > 0) 
                    {
                        handPos = Camera.main.WorldToScreenPoint(worldHighlights[0].Position);
                    }

                    if (handPos != Vector2.zero) 
                    {
                        _handUI.ShowAt(handPos, handScale);
                    }
                    else
                    {
                        _handUI.Hide();
                    }
                }
            }
            else
            {
                _handUI.Hide();
            }
        }

private RectTransform FindTargetRect(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            // Gender-aware resolution: if the name contains a gender suffix (_Female/_Male),
            // first try swapping to the player's actual gender, then fall back to the stored name.
            string resolvedName = ResolveGenderSuffix(name);

            // Try cache first
            if (_uiTargetCache.TryGetValue(resolvedName, out RectTransform cached) && cached != null) return cached;

            // Try active first (fastest)
            GameObject go = GameObject.Find(resolvedName);

            // If not found with resolved name, try original name
            if (go == null && resolvedName != name)
                go = GameObject.Find(name);

            // If still not found, search children of main Canvas
            if (go == null)
            {
                // Targeted search is much faster than Resources.FindObjectsOfTypeAll
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    var all = canvas.GetComponentsInChildren<RectTransform>(true);
                    var found = System.Array.Find(all, r => r.name == resolvedName || r.name == name);
                    if (found != null) go = found.gameObject;
                }
            }

            bool isGoActive = go != null && go.activeInHierarchy;
            if (!isGoActive && (name.StartsWith("SkillButton_") || resolvedName.StartsWith("SkillButton_")))
            {
                if (_currentStep != null)
                {
                    if (_currentStep.TargetUI != null && !string.IsNullOrEmpty(_currentStep.TargetUI.Name))
                    {
                        var rect = FindActiveRectInHierarchy(_currentStep.TargetUI.Name);
                        if (rect != null) return rect;
                    }
                    if (_currentStep.AdditionalTargetUI != null)
                    {
                        foreach (var target in _currentStep.AdditionalTargetUI)
                        {
                            if (target != null && !string.IsNullOrEmpty(target.Name))
                            {
                                var rect = FindActiveRectInHierarchy(target.Name);
                                if (rect != null) return rect;
                            }
                        }
                    }
                }
            }

            if (go != null)
            {
                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt != null) 
                {
                    _uiTargetCache[resolvedName] = rt;
                    return rt;
                }

                Canvas canvas = go.GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                {
                    var crt = canvas.GetComponent<RectTransform>() ?? canvas.transform as RectTransform;
                    if (crt != null) _uiTargetCache[resolvedName] = crt;
                    return crt;
                }
            }

            return null;
        }

        private RectTransform FindActiveRectInHierarchy(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string resolvedName = ResolveGenderSuffix(name);

            // Try active first (fastest)
            GameObject go = GameObject.Find(resolvedName);
            if (go != null && go.activeInHierarchy)
            {
                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt != null) 
                {
                    _uiTargetCache[resolvedName] = rt;
                    return rt;
                }
            }

            // Fallback to Canvas search (still faster than Resources.FindObjectsOfTypeAll)
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                var all = canvas.GetComponentsInChildren<RectTransform>(true);
                var found = System.Array.Find(all, r => (r.name == resolvedName || r.name == name) && r.gameObject.activeInHierarchy);
                if (found != null)
                {
                    _uiTargetCache[resolvedName] = found;
                    return found;
                }
            }

            return null;
        }

/// <summary>
        /// Replaces _Female or _Male suffix in a UI target name with the player's actual gender suffix.
        /// Falls back to the original name if no gender-specific match exists.
        /// </summary>
        private string ResolveGenderSuffix(string name)
        {
            // Male is fallback in case of error/uninitialized save data
            bool isMale = true;
            if (_saveManager != null && _saveManager.CurrentData != null)
            {
                isMale = _saveManager.CurrentData.Gender == MaouSamaTD.Data.MaouGender.Male;
            }

            // If it is a generic skill button alias (starts with SkillButton_)
            if (name.StartsWith("SkillButton_") && _skillManager != null && _skillManager.AvailableSkills != null)
            {
                string tag = name.Substring("SkillButton_".Length);
                
                // Try to find an available skill with this Tag
                foreach (var rite in _skillManager.AvailableSkills)
                {
                    if (rite != null && rite.Tag == tag)
                    {
                        return "SkillButton_" + rite.name.Replace(" ", "");
                    }
                }
            }

            string targetSuffix = isMale ? "_Male" : "_Female";
            string oppositeSuffix = isMale ? "_Female" : "_Male";

            if (name.EndsWith(oppositeSuffix))
                return name.Substring(0, name.Length - oppositeSuffix.Length) + targetSuffix;

            return name;
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
                bool isFallback = false;
                // Dynamic Fallback: If the target is a SkillButton or SovereignRite, but it's inactive in hierarchy, 
                // the skill menu is closed. In this case, redirect the Hand UI position and scale to the "SovereignRiteToggle" button.
                if (!drt.gameObject.activeInHierarchy && (ut.Name.Contains("SkillButton") || ut.Name.Contains("SovereignRite")))
                {
                    RectTransform toggleRt = FindTargetRect("SovereignRiteToggle");
                    if (toggleRt != null)
                    {
                        drt = toggleRt;
                        scaleMultiplier = 1.2f;
                        isFallback = true;
                    }
                }

                Vector3[] corners = new Vector3[4];
                drt.GetWorldCorners(corners);
                Vector3 center = (corners[0] + corners[2]) * 0.5f;

                if (isFallback)
                {
                    // For the toggle fallback, ignore target offset since it belongs to the skill button
                    screenPos = (Vector2)center;
                }
                else
                {
                    // Apply offset. If ut.SizeOffset was intended as pixels, we should scale it by the canvas ratio.
                    // However, adding it directly to screen space center is generally what's expected for simple offsets.
                    // For dragging distance stability, we use the corners center + offset.
                    screenPos = (Vector2)center + ut.SizeOffset;
                }

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
        private IEnumerator DelayedCompleteWaitForAction(float delay)
        {
            yield return new WaitForSeconds(delay);
            _waitingForAction = false;
            if (_showDebugLogs) Debug.Log($"[tutorial] Delay finished. Proceeding from boss death.");
        }

        public void OnActionTriggered(string key)
        {
            // Handle Ignis Death in Tutorial Level 2
            if (key == "UnitDied_Ignis" && CurrentLevelIndex == 2)
            {
                if (_showDebugLogs) Debug.Log("[tutorial] Ignis died. Giving 99 seals and advancing step as requested.");
                _currencyManager?.SetSeals(99);
                _waitingForAction = false; // Stop waiting to advance
                return; // Advance immediately
            }

            // Handle Boss Reaching Exit in Tutorial Level 2 (counts as bypass/fail but proceed)
            if (key == "EnemyReachedExit_Abyssal Shade" && CurrentLevelIndex == 2)
            {
                if (_showDebugLogs) Debug.Log("[tutorial] Boss reached exit. Advancing tutorial step.");
                _waitingForAction = false;
                return;
            }

            if (_waitingForAction && _waitingActionKey == key)
            {
                _waitingForAction = false;
                if (_currentStep != null && _currentStep.ResumeTime)
                {
                    _gameManager.SetSpeed(1); 
                }
            }
            else
            {
                _triggeredActionsBuffer.Add(key);
            }
        }

        private IEnumerator HandlePostActionDelay(TutorialStep step)
        {
            if (string.IsNullOrEmpty(step.ActionKey)) yield break;
            
            // Special Case: Level 2 Rite Usage (e.g. AOE or Ultimate)
            // We want time to resume briefly so the player sees the result (damage, death, floating text)
            bool isLevel2RiteUsage = CurrentLevelIndex == 2 && 
                                     (step.ActionKey == "RiteUsed" || step.ActionKey == "SkillUsed" || step.StepName.Contains("Rite"));

            if (isLevel2RiteUsage)
            {
                if (_showDebugLogs) Debug.Log($"[tutorial] Post-Action delay (2s resume) for step: {step.StepName}");
                
                // Briefly resume time
                _gameManager.SetSpeed(1);
                
                // USER REQUEST: Eliminate artificial delays. 
                // Reducing from 2.0s to 0.5s so player sees the immediate impact but doesn't wait.
                float delay = (step.ActionKey == "SkillUsed" && step.StepName.Contains("Empower")) ? 0.2f : 0.5f;
                yield return new WaitForSeconds(delay);
                
                // Re-pause if this step requested StopTime, to maintain the state until transition
                // EXCEPT for Level 2 Boss death steps, where we want the EnemyManager's cinematic to take over
                bool isBossDeathStep = step.StepName.Contains("One-Shot") || step.StepName.Contains("Boss");
                if (step.StopTime && !isBossDeathStep)
                {
                    _gameManager.SetSpeed(0, true);
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

        public string GetCurrentStepName()
        {
            if (!IsInTutorial || _activeTutorial == null || _currentStepIndex < 0 || _currentStepIndex >= _activeTutorial.Steps.Count)
                return "None";
            return _activeTutorial.Steps[_currentStepIndex].StepName;
        }

        public List<Vector2Int> GetRequiredPlacementTiles()
        {
            List<Vector2Int> allowed = new List<Vector2Int>();
            if (_currentStep == null) return allowed;

            if (_currentStep.ActionKey == "UnitPlaced")
            {
                if (_currentStep.HandTargetTileOverride != Vector2Int.zero && _currentStep.HandTargetTileOverride != new Vector2Int(-1, -1))
                {
                    allowed.Add(_currentStep.HandTargetTileOverride);
                }
            }

            if (_currentStep.TargetTiles != null)
            {
                foreach (var wt in _currentStep.TargetTiles)
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
                        if (met) return true;
                    }

                    // Unstuckable Guard: If the player killed the enemies by other means (e.g. rites, environment, etc.)
                    // and there are no active enemies left or the wave is cleared, complete the step to prevent soft-locks.
                    if (EnemyUnit.ActiveEnemies.Count == 0)
                    {
                        if (_enemyManager != null && !_enemyManager.IsSpawning)
                        {
                            Debug.Log($"[tutorial] Unstuckable Guard: No active enemies left. Skipping UnitKills check.");
                            return true;
                        }
                    }
                    if (step.WaveIndex >= 0 && _enemyManager != null && _enemyManager.IsWaveCleared(step.WaveIndex))
                    {
                        Debug.Log($"[tutorial] Unstuckable Guard: Wave {step.WaveIndex} cleared. Skipping UnitKills check.");
                        return true;
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
                        float threshold = 1.5f;
                        if (step.TargetTiles != null && step.TargetTiles.Count > 0)
                        {
                            threshold = step.TargetTiles[0].Size.x;
                        }
                        else if (step.StepName == "Wait for Cluster Proximity")
                        {
                            threshold = 2.8f; // Allow 1 or 2 tiles in front of Ignis
                        }

                        // Guard: require the wave to have actually started before evaluating proximity.
                        // Without this, the check fires immediately when ActiveEnemies is empty (pre-spawn).
                        if (step.WaveIndex >= 0 && _enemyManager != null)
                        {
                            bool waveHasStarted = _enemyManager.HasWaveStarted(step.WaveIndex);
                            bool waveCleared    = _enemyManager.IsWaveCleared(step.WaveIndex);

                            // Wave hasn't started at all yet — keep waiting, don't evaluate proximity
                            if (!waveHasStarted && !waveCleared)
                            {
                                return false;
                            }
                        }

                        bool exitIsLeft = (_gridManager != null && _gridManager.exitIsLeft);
                        foreach(var enemy in EnemyUnit.ActiveEnemies)
                        {
                            if (enemy == null) continue;
                            float dist = Vector3.Distance(centerPos, enemy.transform.position);
                            
                            bool inRange = false;
                            if (step.StepName == "Wait for Cluster Proximity")
                            {
                                // Check if enemy is in front of Ignis (within threshold) or has passed Ignis
                                bool inFront = exitIsLeft ? (enemy.transform.position.x >= centerPos.x - 0.5f) : (enemy.transform.position.x <= centerPos.x + 0.5f);
                                bool passed = exitIsLeft ? (enemy.transform.position.x < centerPos.x) : (enemy.transform.position.x > centerPos.x);
                                inRange = (dist <= threshold && inFront) || passed;
                            }
                            else
                            {
                                inRange = dist <= threshold;
                            }

                            if (inRange) count++;
                        }

                        int requiredCount = step.RequiredCount;
                        if (step.StepName == "Wait for Cluster Proximity")
                        {
                            // Cap required count to number of remaining alive enemies
                            requiredCount = Mathf.Min(step.RequiredCount, EnemyUnit.ActiveEnemies.Count);
                            if (EnemyUnit.ActiveEnemies.Count == 0) return true;
                        }

                        bool met = count >= requiredCount && requiredCount > 0;
                        
                        // Soft-lock guard: if wave cleared before enemies could swarm, advance anyway
                        if (!met && step.WaveIndex >= 0 && _enemyManager != null && _enemyManager.IsWaveCleared(step.WaveIndex))
                        {
                            return true;
                        }
                        // Soft-lock guard: all enemies dead and wave WAS started (not pre-spawn empty state)
                        else if (!met && EnemyUnit.ActiveEnemies.Count == 0 
                                 && (step.WaveIndex < 0 || (step.WaveIndex >= 0 && _enemyManager != null && _enemyManager.HasWaveStarted(step.WaveIndex))))
                        {
                            return true;
                        }

                        return met;
                    }
                    return false;
                }

                case "WaveFinishedSpawning":
                    if (_enemyManager != null)
                    {
                        // Robust check: Wave has started AND spawning is currently finished AND wave index matches
                        bool hasStarted = step.WaveIndex < 0 || _enemyManager.HasWaveStarted(step.WaveIndex);
                        bool correctWave = step.WaveIndex < 0 || _enemyManager.CurrentWaveIndex >= step.WaveIndex;
                        bool hasEnemies = EnemyUnit.ActiveEnemies.Count > 0;
                        bool met = hasStarted && !_enemyManager.IsSpawning && correctWave && hasEnemies;
                        return met;
                    }
                    return false;
                
                case "EnemiesSpawned":
                    if (_enemyManager != null && _enemyManager.HasWaveStarted(step.WaveIndex))
                    {
                        return _enemyManager.GetTotalSpawnedInWave(step.WaveIndex) >= step.RequiredCount;
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
                    string bossName = step.ActionKey.Contains("|") ? step.ActionKey.Split('|')[1] : "Abyssal Shade";
                    var boss = EnemyUnit.ActiveEnemies.FirstOrDefault(e => e.EnemyData != null && e.EnemyData.EnemyName == bossName);
                    
                    if (boss != null)
                    {
                        // RequiredCount == 0 means "kill the boss" — allow death and wait for it
                        if (step.RequiredCount == 0)
                        {
                            boss.PreventDeathForTutorial = false; // Lift immortality so the skill can kill it
                            boss.Immunities.Clear(); // Clear immunities so it can take damage from the one-shot rite
                            return boss.IsDead;
                        }

                        // RequiredCount > 0 is an HP-gate threshold — keep boss immortal until it fires
                        boss.PreventDeathForTutorial = true;
                        float healthPct = (boss.CurrentHp / boss.MaxHp) * 100f;
                        bool met = healthPct <= step.RequiredCount;
                        
                        if (_showDebugLogs && Time.frameCount % 60 == 0) // Log occasionally
                            Debug.Log($"[tutorial] Boss Health Check ({bossName}): {healthPct:F1}% <= {step.RequiredCount}% ? {met}");
                            
                        return met;
                    }
                    else
                    {
                        // RequiredCount == 0 and boss is gone = success
                        if (step.RequiredCount == 0) return true;

                        if (_showDebugLogs && Time.frameCount % 120 == 0)
                            Debug.LogWarning($"[tutorial] BossHealth condition failed: Boss '{bossName}' not found in ActiveEnemies ({EnemyUnit.ActiveEnemies.Count} active)");
                    }
                    return false;
                }

                case "BossReachedIgnis":
                {
                    var ignis = PlayerUnit.ActiveUnits.FirstOrDefault(u => u != null && u.name.Contains("Ignis"));
                    if (ignis == null) return true; // Fallback to avoid soft-lock if Ignis is missing
                    
                    // Check if any enemy (ideally the boss) is within 5 units of Ignis
                    return EnemyUnit.ActiveEnemies.Any(e => e != null && Vector3.Distance(e.transform.position, ignis.transform.position) < 5f);
                }

                case "EnemiesNearUnit":
                {
                    string targetName = (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) ? step.TargetUI.Name : "Ignis";
                    var targetUnit = PlayerUnit.ActiveUnits.FirstOrDefault(u => u != null && (u.name.Contains(targetName) || (u.Data != null && u.Data.UnitName == targetName)));
                    
                    if (targetUnit == null) return true; // Fallback to avoid soft-lock
                    
                    float threshold = step.RequiredCount > 0 ? step.RequiredCount : 2.5f; // Default to ~1-1.5 tiles
                    return EnemyUnit.ActiveEnemies.Any(e => e != null && Vector3.Distance(e.transform.position, targetUnit.transform.position) <= threshold);
                }

                case "BossPassedUnit":
                {
                    string targetName = (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) ? step.TargetUI.Name : "Ignis";
                    string bossName = step.ActionKey.Contains("|") ? step.ActionKey.Split('|')[1] : "Abyssal Shade";
                    
                    var targetUnit = PlayerUnit.ActiveUnits.FirstOrDefault(u => u != null && (u.name.Contains(targetName) || (u.Data != null && u.Data.UnitName == targetName)));
                    var boss = EnemyUnit.ActiveEnemies.FirstOrDefault(e => e.EnemyData != null && e.EnemyData.EnemyName == bossName);
                    
                    if (boss != null)
                    {
                        // Keep boss immortal while we wait for it to bypass — so Ignis can't kill it
                        boss.PreventDeathForTutorial = true;

                        if (targetUnit != null)
                        {
                            bool exitIsLeft = (_gridManager != null && _gridManager.exitIsLeft);
                            bool passed = false;

                            if (_gridManager != null)
                            {
                                Vector2Int bossCoord = _gridManager.WorldToGridCoordinates(boss.transform.position);
                                Vector2Int targetCoord = _gridManager.WorldToGridCoordinates(targetUnit.transform.position);

                                var pathFromBoss = _gridManager.GetPath(bossCoord, _gridManager.ExitPoint, boss.EnemyData.MovementType, true);
                                var pathFromTarget = _gridManager.GetPath(targetCoord, _gridManager.ExitPoint, boss.EnemyData.MovementType, true);

                                if (pathFromBoss != null && pathFromTarget != null)
                                {
                                    // Boss is passed if it is closer to the exit along the path (fewer remaining path tiles)
                                    passed = pathFromBoss.Count < pathFromTarget.Count;
                                }
                                else
                                {
                                    // Fallback: check X axis
                                    passed = exitIsLeft ? (boss.transform.position.x < (targetUnit.transform.position.x - 0.25f)) : (boss.transform.position.x > (targetUnit.transform.position.x + 0.25f));
                                }
                            }
                            else
                            {
                                passed = exitIsLeft ? (boss.transform.position.x < (targetUnit.transform.position.x - 0.25f)) : (boss.transform.position.x > (targetUnit.transform.position.x + 0.25f));
                            }

                            // Throttled position log so we can see the boss moving
                            if (_showDebugLogs && Time.frameCount % 60 == 0)
                                Debug.Log($"[tutorial] BossPassedUnit check: Boss={boss.transform.position}, Ignis={targetUnit.transform.position}, passed={passed}");

                            if (passed)
                            {
                                if (_showDebugLogs) Debug.Log($"[tutorial] Boss {bossName} passed {targetName}!");
                                return true;
                            }
                        }
                        else
                        {
                            // Fallback if Ignis is not found
                            bool exitIsLeft = (_gridManager != null && _gridManager.exitIsLeft);
                            bool passed = exitIsLeft ? (boss.transform.position.x < -2.25f) : (boss.transform.position.x > 2.25f);
                            if (passed)
                            {
                                if (_showDebugLogs) Debug.Log($"[tutorial] Boss {bossName} passed threshold (fallback)! (ExitIsLeft: {exitIsLeft})");
                                return true;
                            }
                        }
                    }
                    
                    if (_showDebugLogs && Time.frameCount % 120 == 0)
                    {
                        if (targetUnit == null) Debug.LogWarning($"[tutorial] BossPassedUnit: Target unit '{targetName}' not found!");
                        if (boss == null) Debug.LogWarning($"[tutorial] BossPassedUnit: Boss '{bossName}' not found!");
                    }
                    
                    return false;
                }

                case "WaveStarted":
                {
                    if (_enemyManager != null && step.WaveIndex >= 0)
                    {
                        bool started = _enemyManager.HasWaveStarted(step.WaveIndex);
                        // Overflow guard: if game has already advanced past this wave, treat as started
                        if (!started && _enemyManager.CurrentWaveIndex > step.WaveIndex)
                            started = true;
                        return started;
                    }
                    return false;
                }

                case "WaveCleared":
                {
                    if (_enemyManager != null && step.WaveIndex >= 0)
                    {
                        bool cleared = _enemyManager.IsWaveCleared(step.WaveIndex);
                        // Overflow guard: if game has advanced past this wave index, it must have cleared
                        if (!cleared && _enemyManager.CurrentWaveIndex > step.WaveIndex)
                            cleared = true;
                        return cleared;
                    }
                    return false;
                }

                case "BossBypass":
                {
                    if (_bossPhasedTriggered) return true;

                    // Check if the Abyssal Shade has started phasing or completed its bypass
                    var boss = MaouSamaTD.Units.EnemyUnit.ActiveEnemies.FirstOrDefault(e => e.EnemyData != null && e.EnemyData.EnemyName == "Abyssal Shade");
                    if (boss == null) return false;
                    
                    // 1. If boss has HP <= 0.701f (or 70.1%), it has phased/bypassed
                    if (boss.CurrentHp / boss.MaxHp <= 0.701f) return true;

                    // 2. If boss has phasing charges (which are granted on phase)
                    if (boss.CurrentPhasingCharges > 0) return true;

                    // 3. Fallback: check if the boss has physically passed Ignis
                    var ignis = PlayerUnit.ActiveUnits.FirstOrDefault(u => u != null && u.name.Contains("Ignis"));
                    if (ignis != null)
                    {
                        if (_gridManager == null) _gridManager = FindAnyObjectByType<Grid.GridManager>();
                        if (_gridManager != null)
                        {
                            bool exitIsLeft = _gridManager.exitIsLeft;
                            if (exitIsLeft && boss.transform.position.x < (ignis.transform.position.x - 0.25f)) return true;
                            if (!exitIsLeft && boss.transform.position.x > (ignis.transform.position.x + 0.25f)) return true;
                        }
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

                // Special case for RiteMenuOpened: if skill panel is already open, skip step entirely
                if (step.ActionKey == "RiteMenuOpened")
                {
                    var skillPanel = FindAnyObjectByType<MaouSamaTD.UI.Skills.SkillPanelUI>();
                    if (skillPanel != null && skillPanel.IsVisible)
                    {
                        return true;
                    }
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
                
                // Special case for UnitSelected / UnitStatsOpened
                if (step.ActionKey == "UnitSelected" || step.ActionKey == "UnitStatsOpened")
                {
                    bool isPanelActive = _unitInspectorUI != null && _unitInspectorUI.IsPanelActive;
                    bool hasInspectedUnit = _interactionManager != null && _interactionManager.InspectedUnit != null;

                    if (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name) &&
                       (step.TargetUI.Name.StartsWith("Unit_") || step.TargetUI.Name.StartsWith("Enemy_")))
                    {
                        string unitName = step.TargetUI.Name.Replace("Unit_", "").Replace("Enemy_", "");
                        
                        if (hasInspectedUnit)
                        {
                            var selected = _interactionManager.InspectedUnit;
                            if (selected.Data != null && 
                                (selected.Data.UnitName.Equals(unitName, System.StringComparison.OrdinalIgnoreCase) || 
                                 selected.name.Contains(unitName)))
                            {
                                return true;
                            }
                        }
                        
                        if (isPanelActive && hasInspectedUnit)
                        {
                            var selected = _interactionManager.InspectedUnit;
                            if (selected.Data != null && 
                                (selected.Data.UnitName.Equals(unitName, System.StringComparison.OrdinalIgnoreCase) || 
                                 selected.name.Contains(unitName)))
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        if (step.ActionKey == "UnitStatsOpened" && isPanelActive) return true;
                        if (step.ActionKey == "UnitSelected" && hasInspectedUnit) return true;
                        if (isPanelActive || hasInspectedUnit) return true;
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

        private IEnumerator DelayedTimeStop(float delay, string stepName)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (IsInTutorial && _currentStep != null && _currentStep.StepName == stepName && _currentStep.StopTime)
            {
                if (_showDebugLogs) Debug.Log($"[tutorial] Delayed time pause executed for step: {stepName}");
                _gameManager.SetSpeed(0, true);
            }
        }
        #endregion

        public int GetCurrentStepIndex() => _currentStepIndex;
        public MaouSamaTD.Tutorial.TutorialStep GetCurrentStep() => _activeTutorial != null && _currentStepIndex >= 0 && _currentStepIndex < _activeTutorial.Steps.Count ? _activeTutorial.Steps[_currentStepIndex] : null;

        /// <summary>
        /// Returns true if a SkillButton_ UI element with this name exists and is active in the scene.
        /// Used to skip rite tutorial steps that don't apply to the current gender/loadout.
        /// </summary>
        private bool IsRiteButtonAvailable(string buttonName)
        {
            if (string.IsNullOrEmpty(buttonName)) return false;

            string resolvedName = ResolveGenderSuffix(buttonName);

            // Try direct name search (works if button is active)
            GameObject go = GameObject.Find(resolvedName);
            if (go != null) return true;

            // Also check all inactive objects (button might be in docked panel)
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            return System.Array.Exists(allObjects, o => o.name == resolvedName || o.name == buttonName);
        }

        /// <summary>
        /// Finds the SovereignRiteData whose button name matches, then returns its SealCost.
        /// Button names are "SkillButton_" + asset name without spaces (set by SkillPanelUI.Refresh).
        /// Returns 0 if not found.
        /// </summary>
        private int GetRiteSealCostFromButtonName(string buttonName)
        {
            if (_skillManager == null || string.IsNullOrEmpty(buttonName)) return 0;

            string resolvedName = ResolveGenderSuffix(buttonName);

            // Button name pattern: "SkillButton_" + skill.name (no spaces)
            string skillKey = resolvedName.Replace("SkillButton_", "").ToLower();

            foreach (var rite in _skillManager.AvailableSkills)
            {
                if (rite == null) continue;
                string riteBtnName = rite.name.Replace(" ", "").ToLower();
                if (riteBtnName == skillKey)
                {
                    // Ensure MaxSeals is high enough, then set exact amount
                    if (_currencyManager != null && rite.SealCost > _currencyManager.MaxSeals)
                    {
                        _currencyManager.SetMaxSeals(rite.SealCost);
                    }
                    return rite.SealCost;
                }
            }

            if (resolvedName == "Ult_Btn") return 0;

            if (_showDebugLogs) Debug.LogWarning($"[tutorial] GetRiteSealCostFromButtonName: Could not find rite for button '{buttonName}' (resolved: '{resolvedName}')");
            return 0;
        }

        private int GetUnitDeploymentCost(string unitName)
        {
            if (string.IsNullOrEmpty(unitName)) return 0;

            string cleanedName = unitName;
            if (cleanedName.StartsWith("Unit_")) cleanedName = cleanedName.Replace("Unit_", "");
            if (cleanedName.StartsWith("Enemy_")) cleanedName = cleanedName.Replace("Enemy_", "");

            if (_deploymentUI != null)
            {
                var units = _deploymentUI.AvailableUnits;
                if (units != null)
                {
                    foreach (var u in units)
                    {
                        if (u != null && (u.UnitName.Equals(cleanedName, System.StringComparison.OrdinalIgnoreCase) || 
                                          u.name.Equals(cleanedName, System.StringComparison.OrdinalIgnoreCase) ||
                                          u.name.Contains(cleanedName) ||
                                          cleanedName.Contains(u.UnitName)))
                        {
                            return u.DeploymentCost;
                        }
                    }
                }
            }

            // Fallback hardcoded values for standard units just in case
            if (cleanedName.Equals("Ignis", System.StringComparison.OrdinalIgnoreCase)) return 20;
            if (cleanedName.Equals("Lilith", System.StringComparison.OrdinalIgnoreCase)) return 10;

            return 0;
        }

        private void SpawnLilithSealedVisual()
        {
            if (_lilithSealedSprite == null)
            {
                if (_showDebugLogs) Debug.LogWarning("[tutorial] _lilithSealedSprite is null! Lilith sealed visual will not be spawned.");
                return;
            }

            if (_gridManager == null)
            {
                Debug.LogError("[tutorial] Cannot spawn Lilith sealed visual because GridManager is missing.");
                return;
            }

            MaouSamaTD.Grid.Tile tile = _gridManager.GetTileAt(_lilithSealedCoordinate);
            if (tile == null)
            {
                Debug.LogError($"[tutorial] Cannot spawn Lilith sealed visual: Tile at coordinate {_lilithSealedCoordinate} is null.");
                return;
            }

            if (_showDebugLogs) Debug.Log($"[tutorial] Spawning Lilith sealed visual at {_lilithSealedCoordinate}");

            _lilithSealedInstance = new GameObject("Lilith_Sealed_Visual");
            _lilithSealedInstance.transform.position = tile.transform.position + new Vector3(0, 0.5f, 0);

            SpriteRenderer sr = _lilithSealedInstance.AddComponent<SpriteRenderer>();
            sr.sprite = _lilithSealedSprite;
            sr.sortingOrder = 100;

            _lilithSealedInstance.AddComponent<MaouSamaTD.Utils.Billboard>();
        }
    }
}
