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
        [InjectOptional] private MaouSamaTD.Skills.SkillManager _skillManager;
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

            // Level 2 Start Logic: Set initial seals to 50
            if (_activeTutorial != null && _activeTutorial.name.Contains("Level2"))
            {
                if (_currencyManager != null)
                {
                    _currencyManager.SetMaxSeals(50);
                    _currencyManager.SetSeals(50);
                    if (_showDebugLogs) Debug.Log("[tutorial] Level 2 Initialized: Seals set to 50.");
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
            
            // Cleanup invincibility
            if (_gameManager != null) _gameManager.PreventDeathForTutorial = false;
        }

        private bool _isSkillTargetingLastFrame = false;
        private bool _isDraggingLastFrame = false;
        private float _nextHighlightRefreshTime = 0f;

        private void Update()
        {
            if (!IsInTutorial || _activeTutorial == null || _currentStepIndex >= _activeTutorial.Steps.Count) return;

            var step = _activeTutorial.Steps[_currentStepIndex];
            
            // Periodically check if targets are still valid/active (e.g. if user closes a menu)
            if (Time.unscaledTime > _nextHighlightRefreshTime)
            {
                _nextHighlightRefreshTime = Time.unscaledTime + 0.5f;
                if (step.UseBlocker && step.TargetUI != null)
                {
                    var rt = FindTargetRect(step.TargetUI.Name);
                    if (rt == null || !rt.gameObject.activeInHierarchy)
                    {
                        HandleUIHighlight(step);
                    }
                }
            }

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

            // Dynamic Unit Placement logic: Refresh highlights every frame while dragging
            // so the tile cut-out appears immediately (not just on state transition).
            if (step.ActionKey == "UnitPlaced")
            {
                bool isDragging = _interactionManager != null && _interactionManager.IsDragging;
                if (isDragging != _isDraggingLastFrame)
                {
                    _isDraggingLastFrame = isDragging;
                    if (_showDebugLogs) Debug.Log($"[tutorial] Dragging state changed to: {isDragging}. Refreshing highlights.");
                    HandleUIHighlight(step);
                }
                
                // Keep refreshing while dragging for smooth movement (e.g. if we add cursor follow later)
                // but only if isDragging is true.
                if (isDragging)
                {
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

                switch (step.Type)
                {
                    case TutorialStepType.DialogueOnly:
                        // TRIGGER: Shows a dialogue box. If an ActionKey is provided, it first waits for that condition to be met.
                        // NOTE: If dialogue is missing, it will proceed immediately or after the ActionKey condition.
                        if (step.StopTime) _gameManager.SetSpeed(0);
                        
                        // Special Logic for Level 2 Boss Bypass: Lilith Refills Seals
                        if (_activeTutorial != null && _activeTutorial.name.Contains("Level2") && step.StepName == "Boss Bypasses!")
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
                        
                        // If an ActionKey is provided for a dialogue step, wait for that condition before showing it
                        if (!string.IsNullOrEmpty(step.ActionKey))
                        {
                            if (_showDebugLogs) Debug.Log($"[tutorial] DialogueOnly step {step.StepName} waiting for condition: {step.ActionKey}");
                            yield return new WaitUntil(() => CheckCondition(step));
                        }

                        HandleUIHighlight(step);
                        bool dialogueDone = false;

                        if (step.Dialogue != null)
                        {
                            _dialogueManager.StartDialogue(step.Dialogue, () => 
                            {
                                if (_showDebugLogs) Debug.Log($"[tutorial] Dialogue completed for step: {step.StepName}");
                                dialogueDone = true;
                            });
                        }
                        else
                        {
                            if (_showDebugLogs) Debug.LogWarning($"[tutorial] DialogueOnly step '{step.StepName}' has no Dialogue data. Skipping dialogue.");
                            dialogueDone = true;
                        }

                        yield return new WaitUntil(() => dialogueDone);
                        
                        // Hide the hand when dialogue is done so it doesn't linger
                        _handUI.Hide();
                        break;

                    case TutorialStepType.HighlightUI:
                        // TRIGGER: Highlights a specific UI element and optionally shows dialogue.
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
                        // TRIGGER: Highlights one or more world tiles and optionally shows dialogue.
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
                        // TRIGGER: Waits for a specific ActionKey (e.g., 'UnitPlaced', 'SkillUsed') to be triggered by the game.
                        if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for action: {step.ActionKey}");
                        if (step.StopTime) _gameManager.SetSpeed(0); 

                        if (step.Dialogue != null && step.Dialogue.Lines != null && step.Dialogue.Lines.Count > 0)
                        {
                            if (step.UseBlocker)
                            {
                                _uiBlocker.ShowBlockerWithDetailedTargets(null, null);
                                _handUI.Hide();
                            }
                            
                            bool actionDialogueDone = false;
                            _dialogueManager.StartDialogue(step.Dialogue, () => actionDialogueDone = true);
                            yield return new WaitUntil(() => actionDialogueDone);
                        }

                        HandleUIHighlight(step);
                        
                        _waitingForAction = true;
                        _waitingActionKey = step.ActionKey;

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
                            int requiredCost = GetRiteSealCostFromButtonName(step.TargetUI.Name);
                            if (requiredCost > 0 && _currencyManager.CurrentSeals < requiredCost)
                            {
                                _currencyManager.SetSeals(requiredCost);
                                if (_showDebugLogs) Debug.Log($"[tutorial] Set seals to {requiredCost} for step '{step.StepName}' (rite: {step.TargetUI.Name})");
                            }
                        }

                        // Auto-skip "Open Rite Menu" if it's already open
                        if (step.ActionKey == "RiteMenuOpened")
                        {
                            var skillPanel = FindObjectOfType<MaouSamaTD.UI.Skills.SkillPanelUI>();
                            if (skillPanel != null && skillPanel.IsVisible)
                            {
                                if (_showDebugLogs) Debug.Log("[tutorial] Rite Menu already open, auto-completing step.");
                                _triggeredActionsBuffer.Add("RiteMenuOpened");
                            }
                        }

                        // If executing the ultimate on boss, remove death prevention
                        if (step.StepName == "Execute the Ultimate")
                        {
                            foreach (var boss in EnemyUnit.ActiveEnemies)
                            {
                                if (boss != null && boss.PreventDeathForTutorial) boss.PreventDeathForTutorial = false;
                            }
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
                        // TRIGGER: A simple time delay in realtime seconds.
                        HandleUIHighlight(step);
                        if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for duration: {step.Duration}s");
                        yield return new WaitForSecondsRealtime(step.Duration);
                        break;

                    case TutorialStepType.StartWave:
                        // TRIGGER: Manually starts a specific wave index via EnemyManager.
                        HandleUIHighlight(step);
                        if (_showDebugLogs) Debug.Log($"[tutorial] Starting Wave Index: {step.WaveIndex}");
                        if (_enemyManager != null)
                        {
                            _enemyManager.StartSpecificWave(step.WaveIndex);
                        }
                        break;

                    case TutorialStepType.WaitForWave:
                        // TRIGGER: Waits until all enemies in the current wave are defeated and spawning is finished.
                        HandleUIHighlight(step);
                        if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for Wave completion (Index: {step.WaveIndex})");
                        if (step.ResumeTime) _gameManager.SetSpeed(1); 
                        else _gameManager.SetSpeed(0); 
                        yield return new WaitUntil(() => _enemyManager != null && _enemyManager.IsWaveCleared(step.WaveIndex));
                        if (_showDebugLogs) Debug.Log("[tutorial] Wave cleared.");
                        break;

                    case TutorialStepType.WaitForCondition:
                        // TRIGGER: Waits for a dynamic game state (e.g., 'BossHealth', 'EnemiesInRange') via CheckCondition().
                        HandleUIHighlight(step);
                        if (_showDebugLogs) Debug.Log($"[tutorial] Waiting for condition: {step.ActionKey} (Value: {step.RequiredCount})");
                        if (step.ResumeTime) _gameManager.SetSpeed(1); 
                        else _gameManager.SetSpeed(0); 
                        yield return new WaitUntil(() => CheckCondition(step));
                        // REMOVED: _gameManager.SetSpeed(0); - Next step should decide if it wants to pause.
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
                                    _currencyManager.SetSeals(step.RequiredCount);
                                    if (_showDebugLogs) Debug.Log($"[tutorial] CustomCommand: Current Seals set to {step.RequiredCount}.");
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
                                    bool active = (step.RequiredCount > 0);
                                    _deploymentUI.SetUnitButtonVisibility(targetName, active);
                                    if (_showDebugLogs) Debug.Log($"[tutorial] CustomCommand: SetUnitButtonActive for {targetName} to {active}");
                                }
                                else
                                {
                                    if (_showDebugLogs) Debug.LogWarning("[tutorial] CustomCommand SetUnitButtonActive: DeploymentUI is NULL!");
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
                            else if (step.ActionKey == "SetMaxAuthoritySeals")
                            {
                                if (_currencyManager != null)
                                {
                                    int newMax = step.RequiredCount;
                                    _currencyManager.SetMaxSeals(newMax);
                                    if (_showDebugLogs) Debug.Log($"[tutorial] CustomCommand: SetMaxAuthoritySeals → {newMax}");
                                }
                                else
                                {
                                    if (_showDebugLogs) Debug.LogWarning("[tutorial] CustomCommand SetMaxAuthoritySeals: BattleCurrencyManager is NULL!");
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
                // Always show the skill button so its glow/state is visible even when targeting tiles
                if (step.TargetUI != null && !string.IsNullOrEmpty(step.TargetUI.Name)) uiTargets.Add(step.TargetUI);

                if (isSkillTargeting)
                {
                    // While targeting, also show additional targets (e.g. Ignis or other units)
                    if (step.AdditionalTargetUI != null) uiTargets.AddRange(step.AdditionalTargetUI);
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
                        if (rt.gameObject.activeInHierarchy)
                        {
                            uiHits.Add(new UIPopupBlocker.UIHighlightData 
                            { 
                                 Target = rt, 
                                 Size = (ut.Size != Vector2.zero) ? ut.Size : Vector2.one,
                                 Offset = ut.SizeOffset
                            });
                        }
                        else if (ut.Name.Contains("SovereignRite") || ut.Name.Contains("SkillButton"))
                        {
                            // If a Rite button is inactive, the menu is likely closed.
                            // Highlight the toggle button so the user can reopen it.
                            RectTransform toggleRt = FindTargetRect("SovereignRiteToggle");
                            if (toggleRt != null && toggleRt.gameObject.activeInHierarchy)
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

            bool ignoreOverride = isSkillStep && isSkillTargeting;

            if (step.DragShowHand && (uiHits.Count > 0 || worldHighlights.Count > 0) && !_dialogueManager.IsDialogueActive)
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
            else if (step.ShowHand && !_dialogueManager.IsDialogueActive)
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
                else if (step.HandTargetTileOverride != Vector2Int.zero && (isDragging || step.ActionKey != "UnitPlaced"))
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
                else
                {
                    _handUI.Hide();
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

            // Try active first (fastest)
            GameObject go = GameObject.Find(resolvedName);

            // If not found with resolved name, try original name
            if (go == null && resolvedName != name)
                go = GameObject.Find(name);

            // If still not found, search all (including inactive)
            if (go == null)
            {
                var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                go = allObjects.FirstOrDefault(o => o.name == resolvedName);
                if (go == null && resolvedName != name)
                    go = allObjects.FirstOrDefault(o => o.name == name);
            }

            if (go != null)
            {
                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt != null) return rt;

                Canvas canvas = go.GetComponentInChildren<Canvas>(true);
                if (canvas != null) return canvas.GetComponent<RectTransform>() ?? canvas.transform as RectTransform;
            }

            return null;
        }

/// <summary>
        /// Replaces _Female or _Male suffix in a UI target name with the player's actual gender suffix.
        /// Falls back to the original name if no gender-specific match exists.
        /// </summary>
        private string ResolveGenderSuffix(string name)
        {
            if (_saveManager == null || _saveManager.CurrentData == null) return name;

            bool isMale = _saveManager.CurrentData.Gender == MaouSamaTD.Data.MaouGender.Male;
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

        public string GetCurrentStepName()
        {
            if (!IsInTutorial || _activeTutorial == null || _currentStepIndex < 0 || _currentStepIndex >= _activeTutorial.Steps.Count)
                return "None";
            return _activeTutorial.Steps[_currentStepIndex].StepName;
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
                        // Robust check: Wave has started AND spawning is currently finished AND wave index matches
                        bool hasStarted = step.WaveIndex < 0 || _enemyManager.HasWaveStarted(step.WaveIndex);
                        bool correctWave = step.WaveIndex < 0 || _enemyManager.CurrentWaveIndex >= step.WaveIndex;
                        bool met = hasStarted && !_enemyManager.IsSpawning && correctWave;
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

                    // RequiredCount == 0 means "kill the boss" — allow death and wait for it
                    if (step.RequiredCount == 0)
                    {
                        if (boss == null) return true; // Boss already dead/destroyed
                        boss.PreventDeathForTutorial = false; // Lift immortality so the skill can kill it
                        return boss.IsDead;
                    }

                    // RequiredCount > 0 is an HP-gate threshold — keep boss immortal until it fires
                    if (boss != null)
                    {
                        boss.PreventDeathForTutorial = true;
                        float hpPercent = (boss.CurrentHp / boss.MaxHp) * 100f;
                        return hpPercent <= step.RequiredCount;
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
                    var targetUnit = PlayerUnit.ActiveUnits.FirstOrDefault(u => u != null && (u.name.Contains(targetName) || (u.Data != null && u.Data.UnitName == targetName)));
                    
                    if (targetUnit == null) return true; // Fallback

                    string bossName = step.ActionKey.Contains("|") ? step.ActionKey.Split('|')[1] : "Abyssal Shade";
                    var boss = EnemyUnit.ActiveEnemies.FirstOrDefault(e => e.EnemyData != null && e.EnemyData.EnemyName == bossName);
                    
                    if (boss == null) return false;

                    // If exit is at a smaller X than target, boss has passed if boss.x < target.x
                    // For Level 2, we'll assume the standard direction based on Spawn vs Exit
                    if (_gridManager != null)
                    {
                        bool exitIsLeft = _gridManager.ExitPoint.x < _gridManager.SpawnPoint.x;
                        if (exitIsLeft)
                        {
                            return boss.transform.position.x < (targetUnit.transform.position.x - 0.5f);
                        }
                        else
                        {
                            return boss.transform.position.x > (targetUnit.transform.position.x + 0.5f);
                        }
                    }
                    
                    return Vector3.Distance(boss.transform.position, targetUnit.transform.position) < 2f; // Fallback
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

        public int GetCurrentStepIndex() => _currentStepIndex;
        public MaouSamaTD.Tutorial.TutorialStep GetCurrentStep() => _activeTutorial != null && _currentStepIndex >= 0 && _currentStepIndex < _activeTutorial.Steps.Count ? _activeTutorial.Steps[_currentStepIndex] : null;

        /// <summary>
        /// Returns true if a SkillButton_ UI element with this name exists and is active in the scene.
        /// Used to skip rite tutorial steps that don't apply to the current gender/loadout.
        /// </summary>
        private bool IsRiteButtonAvailable(string buttonName)
        {
            if (string.IsNullOrEmpty(buttonName)) return false;

            // Try direct name search (works if button is active)
            GameObject go = GameObject.Find(buttonName);
            if (go != null) return true;

            // Also check all inactive objects (button might be in docked panel)
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            return System.Array.Exists(allObjects, o => o.name == buttonName);
        }

        /// <summary>
        /// Finds the SovereignRiteData whose button name matches, then returns its SealCost.
        /// Button names are "SkillButton_" + asset name without spaces (set by SkillPanelUI.Refresh).
        /// Returns 0 if not found.
        /// </summary>
        private int GetRiteSealCostFromButtonName(string buttonName)
        {
            if (_skillManager == null || string.IsNullOrEmpty(buttonName)) return 0;

            // Button name pattern: "SkillButton_" + skill.name (no spaces)
            string skillKey = buttonName.Replace("SkillButton_", "").ToLower();

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

            if (_showDebugLogs) Debug.LogWarning($"[tutorial] GetRiteSealCostFromButtonName: Could not find rite for button '{buttonName}'");
            return 0;
        }
    }
}
