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
        public int Gold;
        public int BloodCrest;
        public bool IsLilithAwakened;
        public int MaxSeals;

        [Header("Ascension Identity")]
        public string PlayerName = "Mephisto"; // Custom input
        public string TrueName = "Tyrant"; // e.g. "Tyrant" or "Sovereign"
        public MaouGender Gender = MaouGender.Male; // e.g. Male(Force) / Female(Guile)
        
        // Anti-cheat activity log
        public List<ActivityEntry> Activities = new List<ActivityEntry>();

        // Cohort / Squad Data
        public List<CohortData> Cohorts = new List<CohortData>();
        public int CurrentCohortIndex = 0;

        [Header("Settings")]
        public SettingsData Settings = new SettingsData();
    }

    [Serializable]
    public class SettingsData
    {
        public int QualityLevel = 2;
        public float UIAdaptation = 90f;
        public string Language = "English";
        public bool PerformanceOptimization = true;
        public int TargetFPS = 30;
        public bool BatterySaveMode = false;
        public bool AntiAliasing = true;

        // Audio
        public float MusicVolume = 0.8f;
        public float SFXVolume = 0.8f;
        public float VoiceVolume = 0.8f;
        public bool MusicEnabled = true;
        public bool SFXEnabled = true;
        public bool VoiceEnabled = true;

        // Notifications
        public bool ConfirmExitBase = true;
        public bool ShowDeploymentTips = true;
        public bool ShowOutputStatistics = true;
        public bool AutoResupply = true;
        public bool ShowEarningStatistics = true;
        public bool ShowFatigueStatistics = true;
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
