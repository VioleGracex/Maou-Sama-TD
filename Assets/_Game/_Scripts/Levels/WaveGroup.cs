using System;
using UnityEngine;
using MaouSamaTD.Units;

namespace MaouSamaTD.Levels
{
    [Serializable]
    public class WaveGroup
    {
        [Tooltip("The type of enemy to spawn")]
        public EnemyData EnemyType;
        
        [Tooltip("Number of enemies in this group")]
        public int Count = 1;
        
        [Tooltip("Time between each enemy spawn in this group")]
        public float SpawnInterval = 1f; // e.g. 0.5s between each goblin
        
        [Tooltip("Delay before this group starts spawning (relative to wave start)")]
        public float InitialDelay = 0f;
        
        [Tooltip("Which spawn point index to use from LevelData")]
        public int SpawnPointIndex = 0;
    }
}
