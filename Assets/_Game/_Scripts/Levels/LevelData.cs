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
        public int MaxAuthoritySeals = 99;
        public float AuthoritySealsPerSecond = 1f;

        [Header("Waves")]
        public List<WaveData> Waves = new List<WaveData>();

        [Header("Tutorial")]
        public bool HasTutorial;
        [ShowIf("HasTutorial")]
        public MaouSamaTD.Tutorial.TutorialDataSO TutorialData;

        [Header("Story")]
        public bool HasStory;
        [ShowIf("HasStory")]
        public MaouSamaTD.Story.StoryDataSO IntroStory;
        [ShowIf("HasStory")]
        public MaouSamaTD.Story.StoryDataSO OutroStory;

        [Header("Clear Conditions (Stars)")]
        [Tooltip("The conditions to earn stars on this level. Tutorial levels usually grant all stars automatically.")]
        public List<StarCondition> StarConditions = new List<StarCondition>();

        [Header("Win/Lose Conditions")]
        [Tooltip("Conditions that trigger level victory. If empty, default is AllWavesCleared.")]
        public List<LevelCondition> WinConditions = new List<LevelCondition>();
        
        [Tooltip("Conditions that trigger level failure. If empty, default is BaseHPZero.")]
        public List<LevelCondition> LoseConditions = new List<LevelCondition>();
    }

    public enum LevelConditionType
    {
        AllWavesCleared,      // Default Win
        BaseHPZero,           // Default Lose
        EnemiesPassedLimit,   // Lose if X enemies pass
        SurvivalTime,         // Win after X seconds
        ProtectSpecificUnit   // Lose if specific unit dies
    }

    [System.Serializable]
    public class LevelCondition
    {
        public string Description;
        public LevelConditionType Type;
        public float Value; // Threshold for the condition
    }

    [System.Serializable]
    public class StarCondition
    {

        public enum ConditionType
        {
            CompleteLevel,        // Just finishing
            TimeLimit,           // Finish within X seconds
            BaseHealth,          // Base health remains above X%
            UnitLossLimit,       // Lost fewer than X units
            SpecificUnitSurvived // A mandatory unit did not retreat
        }

        public string Description;
        public ConditionType Type;
        public float TargetValue; // Time, HP %, or Unit Count
        public List<MaouSamaTD.Data.RewardData> BonusRewards = new List<MaouSamaTD.Data.RewardData>();
        
        [Tooltip("If true, this star is automatically granted if the level is marked as Tutorial.")]
        public bool AutoGrantInTutorial = true;
    }
}
