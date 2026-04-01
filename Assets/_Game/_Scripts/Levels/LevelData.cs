using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes; // For better list display if available, or just standard inspector

namespace MaouSamaTD.Levels
{
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "MaouSamaTD/Level Data")]
    public class LevelData : MaouSamaTD.Core.GameDataSO
    {
        [Header("Identity")]
        [Tooltip("Unique integer ID for Addressables and easier logic (e.g., 1, 2, 3...)")]
        public int LevelIndex;

        [Header("Level Info")]
        public string LevelName = "Level 1";
        [TextArea] public string Description = "The first battle.";
        
        [Tooltip("Time in seconds before the first wave starts")]
        public float GracePeriod = 5f;

        [Header("Campaign Settings")]
        public string LevelID = "1-1";

        [Tooltip("List of rewards granted upon clearing the level for the first time or repeatedly (depending on logic).")]
        public List<MaouSamaTD.Data.RewardData> WinRewards = new List<MaouSamaTD.Data.RewardData>();
        
        [Tooltip("If populated, forces the player to use this specific cohort for the first 11 slots. Existing cohort selection is ignored.")]
        public List<MaouSamaTD.Units.UnitData> PremadeCohort;
        [Tooltip("If true, the player cannot change the premade cohort slots.")]
        public bool IsCohortLocked = true;

        [Space]
        [Tooltip("The 12th specific assistant/friend unit provided for this level.")]
        public MaouSamaTD.Units.UnitData SupportAssistant;
        [Tooltip("If true, the player cannot swap out the support assistant.")]
        public bool IsAssistantLocked = true;

        [Header("Sovereign Rites")]
        [Tooltip("The rites available for the Male Maou in this level.")]
        public List<MaouSamaTD.Skills.SovereignRiteData> MaleSovereignRites = new List<MaouSamaTD.Skills.SovereignRiteData>();
        [Tooltip("The rites available for the Female Maou in this level.")]
        public List<MaouSamaTD.Skills.SovereignRiteData> FemaleSovereignRites = new List<MaouSamaTD.Skills.SovereignRiteData>();
        [Tooltip("If true, the player cannot change these rites.")]
        public bool IsRitesLocked = true;


        [Header("Map Settings")]
        public MapData MapData;

        [Header("Progression Rewards")]
        [Tooltip("Base XP rewarded to every deployed unit upon winning the level.")]
        public int MissionXP = 100;

        [Header("Economy")]
        public int StartingAuthoritySeals = 10;
        public int MaxAuthoritySeals = 30;
        public float AuthoritySealsPerSecond = 1f;

        [Header("Waves")]
        public List<WaveData> Waves = new List<WaveData>();

        [Header("Tutorial")]
        public bool HasTutorial;
        [ShowIf("HasTutorial")]
        public MaouSamaTD.Tutorial.TutorialDataSO TutorialData;
    }
}
