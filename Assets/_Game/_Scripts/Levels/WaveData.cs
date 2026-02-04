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
    }
}
