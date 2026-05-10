using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaouSamaTD.Levels
{
    [Serializable]
    public class WaveData
    {
        [Tooltip("Groups of enemies in this wave (can run in parallel with different delays)")]
        public List<WaveGroup> Groups = new List<WaveGroup>();
        
        [Tooltip("Time to wait after this wave finishes (all enemies dead?) or fixed duration? usually 'Time before NEXT wave'")]
        public float DelayBeforeNextWave = 5f; 
        
        [Tooltip("Message to display when wave starts")]
        public string WaveMessage;

        [Header("Dialogue")]
        [Tooltip("Story to play BEFORE the wave starts spawning")]
        public MaouSamaTD.Story.StoryDataSO PreWaveStory;
        
        [Tooltip("Story to play AFTER the wave is cleared (all enemies dead)")]
        public MaouSamaTD.Story.StoryDataSO PostWaveStory;
    }
}
