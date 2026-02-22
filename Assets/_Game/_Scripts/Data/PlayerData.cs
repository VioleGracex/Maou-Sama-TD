using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaouSamaTD.Data
{
    [Serializable]
    public class PlayerData
    {
        public List<string> CompletedLevels = new List<string>();
        // Using List of structs for JsonUtility compatibility instead of Dictionary
        public List<LevelStarData> LevelStars = new List<LevelStarData>();
        public List<string> UnlockedUnits = new List<string>();
        public int Currency;

        [Header("Ascension Identity")]
        public string PlayerName = "Mephisto"; // Custom input
        public string TrueName = "Tyrant"; // e.g. "Tyrant" or "Sovereign"
        public MaouGender Gender = MaouGender.Male; // e.g. Male(Force) / Female(Guile)
        
        // Anti-cheat activity log
        public List<ActivityEntry> Activities = new List<ActivityEntry>();

        // Cohort / Squad Data
        public List<CohortData> Cohorts = new List<CohortData>();
        public int CurrentCohortIndex = 0;
    }

    [Serializable]
    public struct CohortData
    {
        public string CohortName;
        // 12 slots for unit IDs (matching UnitData.UnitID or name)
        public List<string> UnitIDs; 

        public CohortData(string name)
        {
            CohortName = name;
            UnitIDs = new List<string>(new string[12]); // Initialize with 12 empty slots
        }
    }

    [Serializable]
    public struct LevelStarData
    {
        public string LevelID;
        public int Stars;
        
        public LevelStarData(string levelID, int stars)
        {
            LevelID = levelID;
            Stars = stars;
        }
    }

    [Serializable]
    public struct ActivityEntry
    {
        public string ActivityName;
        public int Count;
        
        public ActivityEntry(string activityName, int count)
        {
            ActivityName = activityName;
            Count = count;
        }
    }

    [Serializable]
    public enum MaouGender
    {
        Male = 0,   // Force / Sovereign
        Female = 1  // Guile / Sovereign
    }
}
