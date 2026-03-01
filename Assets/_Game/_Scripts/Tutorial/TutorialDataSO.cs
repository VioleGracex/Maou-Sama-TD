using System.Collections.Generic;
using UnityEngine;
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
    public class TutorialStep
    {
        public string StepName;
        public TutorialStepType Type;
        
        [Header("Dialogue")]
        public DialogueData Dialogue;
        
        [Header("Targeting")]
        [Tooltip("Name of the UI object or path to highlight")]
        public string TargetUIName;
        public List<string> AdditionalTargetUINames = new List<string>();
        public Vector2Int TargetTile;
        
        [Header("Parameters")]
        public float DelayBefore = 0f;
        public float Duration = 2f;
        [Tooltip("Action string to wait for (e.g., 'UnitPlaced', 'WaveStarted')")]
        public string ActionKey;
        
        [Header("Visuals")]
        public bool ShowHand = true;
        public bool DragShowHand = false;
        public string HandDragTargetUIName;
        public Vector2Int HandDragTargetTile;

        [Header("Wave Interaction")]
        [Tooltip("The index of the wave to start or wait for")]
        public int WaveIndex = -1;

        [Header("Conditions")]
        public int RequiredCount;
        public float HandDragTargetRadius = 1.0f;
    }

    [CreateAssetMenu(fileName = "NewTutorialData", menuName = "MaouSamaTD/Tutorial Data")]
    public class TutorialDataSO : ScriptableObject
    {
        public List<TutorialStep> Steps = new List<TutorialStep>();
    }
}
