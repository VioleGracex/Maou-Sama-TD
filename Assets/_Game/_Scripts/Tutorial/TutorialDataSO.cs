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
        CustomCommand
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
    }

    [CreateAssetMenu(fileName = "NewTutorialData", menuName = "MaouSamaTD/Tutorial Data")]
    public class TutorialDataSO : ScriptableObject
    {
        public List<TutorialStep> Steps = new List<TutorialStep>();
    }
}
