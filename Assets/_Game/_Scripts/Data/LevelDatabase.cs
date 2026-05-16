using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Levels;

namespace MaouSamaTD.Data
{
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "MaouSamaTD/LevelDatabase")]
    public class LevelDatabase : ScriptableObject
    {
        public List<LevelData> AllLevels;

        public LevelData GetLevelByID(string id)
        {
            if (string.IsNullOrEmpty(id) || AllLevels == null) return null;
            return AllLevels.Find(l => l != null && l.LevelID == id);
        }

        public LevelData GetNextLevel(LevelData current)
        {
            if (current == null || AllLevels == null) return null;
            
            int currentIndex = AllLevels.IndexOf(current);
            if (currentIndex != -1 && currentIndex + 1 < AllLevels.Count)
            {
                return AllLevels[currentIndex + 1];
            }
            return null;
        }
    }
}
