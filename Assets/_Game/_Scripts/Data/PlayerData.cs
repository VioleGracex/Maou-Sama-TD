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
        public List<string> UnlockedUnits = new List<string>(); // Legacy / Discovery list
        public List<UnitInventoryEntry> UnitInventory = new List<UnitInventoryEntry>();
        
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

        [Header("Home Page Customization")]
        public List<HomeCharacterSettings> HomePresets = new List<HomeCharacterSettings>();
        public int ActivePresetIndex = 0;

        public HomeCharacterSettings CurrentHomeSettings
        {
            get
            {
                if (HomePresets == null || HomePresets.Count == 0)
                {
                    HomePresets = new List<HomeCharacterSettings> { new HomeCharacterSettings() };
                }
                return HomePresets[Mathf.Clamp(ActivePresetIndex, 0, HomePresets.Count - 1)];
            }
        }
    }

    [Serializable]
    public class HomeCharacterSettings
    {
        public string PresetName = "Default";
        public string SelectedUnitID = "Ignis"; // Default unit
        public Vector2 Position = new Vector2(0, 0); // Position relative to center/screen
        public float Scale = 1.0f;
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

        // Notifications (Removed)
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
    public class UnitInventoryEntry
    {
        public string InstanceID; // Unique GUID for this specific instance
        public string UnitID;     // ID/Name of the UnitData asset
        public int Level = 1;
        public int Potential = 0;  // Arknights-style potential (from duplicates)
        public int Experience = 0;
        public long AcquisitionDate;

        public UnitInventoryEntry(string unitID)
        {
            InstanceID = Guid.NewGuid().ToString();
            UnitID = unitID;
            AcquisitionDate = DateTime.UtcNow.Ticks;
        }
    }

    [Serializable]
    public enum MaouGender
    {
        Male = 0,   // Force / Sovereign
        Female = 1  // Guile / Sovereign
    }
}
