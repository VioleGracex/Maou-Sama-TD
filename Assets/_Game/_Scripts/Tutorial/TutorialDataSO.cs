using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using MaouSamaTD.Tutorial;

namespace MaouSamaTD.Tutorial
{
    public enum TutorialStepType
    {
        DialogueOnly,
        HighlightUI,
        HighlightTile,
        WaitForAction,
        WaitTime,
        CustomCommand,
        StartWave,
        WaitForWave,
        WaitForCondition
    }

    [System.Serializable]
    public class WorldTarget
    {
        public Vector2Int Coordinate;
        public Vector2 Size = Vector2.one;
        public Vector3 Offset = Vector3.zero;
        public float Height = 2.0f;
    }

    [System.Serializable]
    public class UITarget
    {
        public string Name;
        public Vector2 Size = Vector2.one;
        public Vector2 SizeOffset = Vector2.zero;
    }

    [System.Serializable]
    public class TutorialStep
    {
        public string StepName;
        public TutorialStepType Type;
        
        [Header("Dialogue")]
        public DialogueData Dialogue;

        [Button("Debug: Log Step Details")]
        private void DebugLogStep()
        {
            Debug.Log($"[tutorial-debug] Step: {StepName}, Type: {Type}, HandScale: {HandScale}, TargetUI: {(TargetUI != null ? TargetUI.Name : "null")}");
        }

        [Header("Targeting (New)")]
        [ShowIf("HasUITarget")]
        [Tooltip("Primary target UI element")]
        public UITarget TargetUI;
        
        [ShowIf("HasUITarget")]
        public List<UITarget> AdditionalTargetUI = new List<UITarget>();
        
        [ShowIf("HasTileTarget")]
        [Tooltip("Target tiles with individual sizing")]
        public List<WorldTarget> TargetTiles = new List<WorldTarget>();
        
        [Header("Parameters")]
        public float DelayBefore = 0f;
        [ShowIf("HasDuration")]
        public float Duration = 2f;
        
        [ShowIf("HasAction")]
        [Tooltip("Action string to wait for (e.g., 'UnitPlaced', 'WaveStarted')")]
        public string ActionKey;
        
        [Header("Hand Visuals")]
        [ShowIf("CanShowHand")]
        public bool ShowHand = true;

        [ShowIf("ShowHand")]
        [Tooltip("Base scale for the hand visual")]
        public float HandScale = 1.0f;
        
        [ShowIf("ShowHand")]
        public bool DragShowHand = false;
        
        [ShowIf("ShowHand")]
        [Tooltip("Visual override for the hand position/scale/offset (if empty, uses primary target)")]
        public UITarget HandTargetUIOverride;
        
        [ShowIf("ShowHand")]
        public Vector2Int HandTargetTileOverride;
        
        [ShowIf("ShowHand")]
        public Vector3 HandTargetTileOffsetOverride = Vector3.zero;

        [Header("Wave Interaction")]
        [ShowIf("IsWaveStep")]
        [Tooltip("The index of the wave to start or wait for")]
        public int WaveIndex = -1;

        [Header("Conditions")]
        [ShowIf("HasCondition")]
        public int RequiredCount;
        
        [ShowIf("UseBlocker")]
        [Tooltip("Enable dark overlay blocker for this step")]
        public bool UseBlocker = true;

        [ShowIf("UseBlocker")]
        [Tooltip("If true, the blocker covers the entire screen without any holes/cutouts")]
        public bool FullBlocker = false;

        [ShowIf("UseBlocker")]
        [Tooltip("Reset previous holes when this step starts")]
        public bool ResetBlocker = true;

        [Label("Pause Time on Step Start")]
        [Tooltip("If true, game time will be paused (speed 0) at the start of this step")]
        public bool StopTime = true;

        [Label("Resume Time on Step Complete")]
        [Tooltip("Automatically resume game time (scale 1) after this step completes")]
        public bool ResumeTime = true;

        [Label("Delay Before Pausing Time")]
        [Tooltip("Optional delay (in seconds) after the step starts but before the game is paused. Useful for allowing action animations to play out.")]
        [ShowIf("StopTime")]
        public float DelayBeforeStopTime = 0f;

        [Header("Miss / Fail Branching")]
        [Tooltip("If true, casting a rite during this step checks if target is missed/alive and handles refunding.")]
        public bool EnableMissInterception;
        [ShowIf("EnableMissInterception")]
        [Tooltip("The name of the boss/enemy to check for miss interception.")]
        public string MissTargetBossName = "Abyssal Shade";
        [ShowIf("EnableMissInterception")]
        [Tooltip("If a miss/fail is detected, jump to this step name (e.g. '21-a') instead of the next step.")]
        public string OnFailJumpToStepName;
        [Tooltip("When this step finishes successfully, jump to this step name (e.g. '21') instead of the next sequential step.")]
        public string OnCompleteJumpToStepName;
        [ShowIf("EnableMissInterception")]
        [Tooltip("Optional sequential list of dialogues to display on consecutive misses (fallback if OnFailJumpToStepName is empty).")]
        public List<DialogueData> ConsecutiveMissDialogues = new List<DialogueData>();

        [Header("Developer Notes")]
        [TextArea(3, 10)]
        [Tooltip("Internal notes about this step's purpose, triggers, or logic.")]
        public string Comment;


        #region NaughtyAttributes Helpers
        private bool HasUITarget => Type == TutorialStepType.HighlightUI || Type == TutorialStepType.WaitForAction || Type == TutorialStepType.WaitForCondition;
        private bool HasTileTarget => Type == TutorialStepType.HighlightTile;
        private bool HasDuration => Type == TutorialStepType.WaitTime;
        private bool HasAction => Type == TutorialStepType.WaitForAction || Type == TutorialStepType.WaitForCondition;
        private bool CanShowHand => Type != TutorialStepType.DialogueOnly && Type != TutorialStepType.WaitTime && Type != TutorialStepType.WaitForWave;
        private bool IsWaitAction => Type == TutorialStepType.WaitForAction;
        private bool IsWaveStep => Type == TutorialStepType.StartWave || Type == TutorialStepType.WaitForWave || Type == TutorialStepType.WaitForCondition;
        private bool HasCondition => Type == TutorialStepType.WaitForCondition;
        #endregion
    }

    [CreateAssetMenu(fileName = "NewTutorialData", menuName = "MaouSamaTD/Tutorial Data")]
    public class TutorialDataSO : ScriptableObject
    {
        [Header("Editor Settings")]
        public bool ShowCustomEditor = true;

        public List<TutorialStep> Steps = new List<TutorialStep>();
    }
}
