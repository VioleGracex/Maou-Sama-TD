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
        WaitForCondition // New
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
        
        [Header("Targeting (Legacy)")]
        [ShowIf("ShouldShowLegacyFields")]
        public string TargetUIName;
        [ShowIf("ShouldShowLegacyFields")]
        public List<string> AdditionalTargetUINames = new List<string>();
        [ShowIf("ShouldShowLegacyFields")]
        public Vector2Int TargetTile;
        [ShowIf("ShouldShowLegacyFields")]
        public List<Vector2Int> AdditionalTargetTiles = new List<Vector2Int>();

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
        
        [Header("Visuals")]
        [ShowIf("CanShowHand")]
        public bool ShowHand = true;
        
        [ShowIf("IsWaitAction")]
        public bool DragShowHand = false;
        
        [ShowIf("DragShowHand")]
        public UITarget HandDragTargetUI;
        
        [ShowIf("DragShowHand")]
        public Vector2Int HandDragTargetTile;
        
        [ShowIf("DragShowHand")]
        public Vector3 HandDragTargetTileOffset = Vector3.zero;

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

        [ShowIf("ShouldShowLegacyFields")]
        [Tooltip("Base size of the hole (X, Z) on the floor. Used for UI targets as well.")]
        public Vector2 HoleSize = Vector2.one;

        [ShowIf("ShouldShowLegacyFields")]
        [Tooltip("Base vertical height of the world hole column.")]
        public float HoleHeight = 2.0f;

        #region NaughtyAttributes Helpers
        private bool HasUITarget => Type == TutorialStepType.HighlightUI || Type == TutorialStepType.WaitForAction || Type == TutorialStepType.WaitForCondition;
        private bool HasTileTarget => Type == TutorialStepType.HighlightTile;
        private bool HasDuration => Type == TutorialStepType.WaitTime;
        private bool HasAction => Type == TutorialStepType.WaitForAction || Type == TutorialStepType.WaitForCondition;
        private bool CanShowHand => Type != TutorialStepType.DialogueOnly && Type != TutorialStepType.WaitTime && Type != TutorialStepType.WaitForWave;
        private bool IsWaitAction => Type == TutorialStepType.WaitForAction;
        private bool IsWaveStep => Type == TutorialStepType.StartWave || Type == TutorialStepType.WaitForWave;
        private bool HasCondition => Type == TutorialStepType.WaitForCondition;
        private bool ShouldShowLegacyFields => UseBlocker && (Type == TutorialStepType.HighlightUI || Type == TutorialStepType.WaitForAction) && (TargetUI == null || string.IsNullOrEmpty(TargetUI.Name));
        #endregion
    }

    [CreateAssetMenu(fileName = "NewTutorialData", menuName = "MaouSamaTD/Tutorial Data")]
    public class TutorialDataSO : ScriptableObject
    {
        public List<TutorialStep> Steps = new List<TutorialStep>();
    }
}
