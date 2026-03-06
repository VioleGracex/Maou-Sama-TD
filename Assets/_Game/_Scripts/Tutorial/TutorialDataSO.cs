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
        
        [Header("Visual Customization")]
        [Tooltip("Enable dark overlay blocker for this step")]
        public bool UseBlocker = true;

        [ShowIf("UseBlocker")]
        [Tooltip("Reset previous holes when this step starts")]
        public bool ResetBlocker = true;

        [Tooltip("Automatically resume game time (scale 1) after this step completes")]
        public bool ResumeTime = true;


        #region NaughtyAttributes Helpers
        private bool HasUITarget => Type == TutorialStepType.HighlightUI || Type == TutorialStepType.WaitForAction || Type == TutorialStepType.WaitForCondition;
        private bool HasTileTarget => Type == TutorialStepType.HighlightTile;
        private bool HasDuration => Type == TutorialStepType.WaitTime;
        private bool HasAction => Type == TutorialStepType.WaitForAction || Type == TutorialStepType.WaitForCondition;
        private bool CanShowHand => Type != TutorialStepType.DialogueOnly && Type != TutorialStepType.WaitTime && Type != TutorialStepType.WaitForWave;
        private bool IsWaitAction => Type == TutorialStepType.WaitForAction;
        private bool IsWaveStep => Type == TutorialStepType.StartWave || Type == TutorialStepType.WaitForWave;
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
