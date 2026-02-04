using System;
using System.Collections.Generic;

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
        
        // Anti-cheat activity log
        public List<ActivityEntry> Activities = new List<ActivityEntry>();
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
}
