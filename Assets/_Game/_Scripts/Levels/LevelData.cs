using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes; // For better list display if available, or just standard inspector

namespace MaouSamaTD.Levels
{
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "MaouSamaTD/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Level Info")]
        public string LevelName = "Level 1";
        [TextArea] public string Description = "The first battle.";
        
        [Tooltip("Time in seconds before the first wave starts")]
        public float GracePeriod = 5f;

        [Header("Campaign Settings")]
        public string LevelID = "1-1";
        public int RewardCurrency = 100;
        
        [Tooltip("If populated, forces the player to use this specific cohort for the first 10 slots. Existing cohort selection is ignored.")]
        public List<MaouSamaTD.Units.UnitData> PremadeCohort;
        [Tooltip("If true, the player cannot change the premade cohort slots.")]
        public bool IsCohortLocked = true;

        [Space]
        [Tooltip("The 11th specific assistant/friend unit provided for this level.")]
        public MaouSamaTD.Units.UnitData SupportAssistant;
        [Tooltip("If true, the player cannot swap out the support assistant.")]
        public bool IsAssistantLocked = true;


        [Header("Map Settings")]
        public MapData MapData;

        [Header("Economy")]
        public int StartingAuthoritySeals = 10;
        public float AuthoritySealsPerSecond = 1f;

        [Header("Waves")]
        public List<WaveData> Waves = new List<WaveData>();
    }
}
