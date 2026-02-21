using UnityEngine;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using Zenject;
using MaouSamaTD.Data;

namespace MaouSamaTD.Managers
{
    public class SaveManager : MonoBehaviour
    {
        private const string SaveFileName = "player_save.json";
        private const string HashKey = "MaouSamaTD_Sylvan_Secret"; // Basic obfuscation key

        public PlayerData CurrentData { get; private set; }
        
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        [Inject]
        public void Construct()
        {
            Load();
        }

        public void Save()
        {
            if (CurrentData == null) CurrentData = new PlayerData();
            
            string json = JsonUtility.ToJson(CurrentData, true);
            string hash = GenerateHash(json);
            
            // Format: JSON content + valid separator + Hash
            string content = json + "\n|HASH|" + hash;
            
            try 
            {
                File.WriteAllText(SavePath, content);
                Debug.Log($"[SaveManager] Game saved to: {SavePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save: {e.Message}");
            }
        }

        public void Load()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("[SaveManager] No save found. Creating new.");
                CreateNewSave();
                return;
            }

            try
            {
                string content = File.ReadAllText(SavePath);
                string[] parts = content.Split(new string[] { "\n|HASH|" }, System.StringSplitOptions.None);
                
                if (parts.Length < 2)
                {
                    Debug.LogWarning("[SaveManager] Save file corrupted (integrity check missing). Resetting.");
                    CreateNewSave();
                    return;
                }

                string json = parts[0];
                string savedHash = parts[1];
                string calculatedHash = GenerateHash(json);

                if (savedHash != calculatedHash)
                {
                    Debug.LogError("[SaveManager] Anti-Cheat: Hash mismatch! Data integrity compromised.");
                    // In a stricter system, you might ban or flag. For now, we reset or allow with warning (user asked for anti-cheat checks).
                    // We will reset to be safe/punitive for now as per "dont allow user to cheat".
                    CreateNewSave(); 
                    return;
                }

                CurrentData = JsonUtility.FromJson<PlayerData>(json);
                Debug.Log("[SaveManager] Save loaded successfully.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load save: {e.Message}");
                CreateNewSave();
            }
        }

        private void CreateNewSave()
        {
            CurrentData = new PlayerData();
            // Initial New Game State
            CurrentData.Currency = 0;
            CurrentData.UnlockedUnits = new List<string>()
            {
                "Ignis", // Default Starter Unit
                
            }; 
            
            Debug.Log($"[SaveManager] Created New Save Data. Granted Default Units: {string.Join(", ", CurrentData.UnlockedUnits)}");
            Save();
        }

        private string GenerateHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input + HashKey);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                return System.Convert.ToBase64String(hashBytes);
            }
        }
        
        #region Public API
        
        public void AddCurrency(int amount)
        {
            if (CurrentData == null) return;
            CurrentData.Currency += amount;
            Save();
        }

        public bool SpendCurrency(int amount)
        {
            if (CurrentData == null || CurrentData.Currency < amount) return false;
            CurrentData.Currency -= amount;
            Save();
            return true;
        }

        public void LevelComplete(string levelID, int stars)
        {
            if (CurrentData == null) return;

            if (!CurrentData.CompletedLevels.Contains(levelID))
            {
                CurrentData.CompletedLevels.Add(levelID);
            }

            // Update stars if higher
            var starEntry = CurrentData.LevelStars.FindIndex(x => x.LevelID == levelID);
            if (starEntry != -1)
            {
                if (CurrentData.LevelStars[starEntry].Stars < stars)
                {
                    var entry = CurrentData.LevelStars[starEntry];
                    entry.Stars = stars;
                    CurrentData.LevelStars[starEntry] = entry; // Update struct in list
                }
            }
            else
            {
                CurrentData.LevelStars.Add(new LevelStarData(levelID, stars));
            }
            
            AddActivity($"Complete_{levelID}"); // Anti-cheat logging
            Save();
        }

        public bool IsLevelCompleted(string levelID)
        {
            return CurrentData != null && CurrentData.CompletedLevels.Contains(levelID);
        }

        public void AddActivity(string activityName)
        {
            if (CurrentData == null) return;
            
            int index = CurrentData.Activities.FindIndex(x => x.ActivityName == activityName);
            if (index != -1)
            {
                var entry = CurrentData.Activities[index];
                entry.Count++;
                CurrentData.Activities[index] = entry;
            }
            else
            {
                CurrentData.Activities.Add(new ActivityEntry(activityName, 1));
            }
        }
        
        public List<string> GetCohort(int index)
        {
            if (CurrentData == null) return new List<string>();
            
            // Ensure Cohorts list is initialized
            if (CurrentData.Cohorts == null) CurrentData.Cohorts = new List<CohortData>();
            
            // Ensure specific cohort exists
            if (index < 0) index = 0;
            while (CurrentData.Cohorts.Count <= index)
            {
                CurrentData.Cohorts.Add(new CohortData($"Cohort {CurrentData.Cohorts.Count + 1}"));
            }
            
            return CurrentData.Cohorts[index].UnitIDs;
        }

        public void SetCohort(int index, List<string> unitIDs)
        {
             if (CurrentData == null) return;
             // Ensure exists (reuse logic or simplify)
             GetCohort(index); 
             
             CurrentData.Cohorts[index] = new CohortData
             {
                 CohortName = CurrentData.Cohorts[index].CohortName,
                 UnitIDs = unitIDs
             };
             Save();
        }

        #endregion
    }
}
