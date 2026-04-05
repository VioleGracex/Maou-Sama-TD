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
        #region Fields
        private const string SaveFileName = "player_save.json";
        private const string HashKey = "MaouSamaTD_Sylvan_Secret"; 

        public PlayerData CurrentData { get; private set; }
        
        private string SaveFolder => Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "Maou-Sama-TD");
        private string SavePath => Path.Combine(SaveFolder, SaveFileName);
        
        private string LegacySavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        #endregion

        #region Lifecycle
        [Inject]
        public void Construct()
        {
            CheckAndMigrateSave();
            Load();
        }

        private void CheckAndMigrateSave()
        {
            if (File.Exists(LegacySavePath) && !File.Exists(SavePath))
            {
                try 
                {
                    if (!Directory.Exists(SaveFolder)) Directory.CreateDirectory(SaveFolder);
                    File.Move(LegacySavePath, SavePath);
                    Debug.Log($"[SaveManager] Migrated save data from {LegacySavePath} to {SavePath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SaveManager] Failed to migrate save data: {e.Message}");
                }
            }
            else if (!Directory.Exists(SaveFolder))
            {
                Directory.CreateDirectory(SaveFolder);
            }
        }
        #endregion

        #region Public API
        public void Save()
        {
            if (CurrentData == null) CurrentData = new PlayerData();
            
            string json = JsonUtility.ToJson(CurrentData, true);
            string hash = GenerateHash(json);
            
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

        public void DeleteSaveData()
        {
            if (File.Exists(SavePath))
            {
                try
                {
                    File.Delete(SavePath);
                    Debug.Log("[SaveManager] Save Data Deleted successfully.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SaveManager] Failed to delete save data: {e.Message}");
                }
            }
            CurrentData = null;
            CreateNewSave();
        }

        public void AddGold(int amount)
        {
            if (CurrentData == null) return;
            CurrentData.Gold += amount;
            Save();
        }

        public bool SpendGold(int amount)
        {
            if (CurrentData == null || CurrentData.Gold < amount) return false;
            CurrentData.Gold -= amount;
            Save();
            return true;
        }

        public void AddBloodCrest(int amount)
        {
            if (CurrentData == null) return;
            CurrentData.BloodCrest += amount;
            Save();
        }

        public bool SpendBloodCrest(int amount)
        {
            if (CurrentData == null || CurrentData.BloodCrest < amount) return false;
            CurrentData.BloodCrest -= amount;
            Save();
            return true;
        }

        public void AddCurrency(int amount) => AddGold(amount);
        public bool SpendCurrency(int amount) => SpendGold(amount);

        public void LevelComplete(string levelID, int stars)
        {
            if (CurrentData == null) return;

            if (!CurrentData.CompletedLevels.Contains(levelID))
            {
                CurrentData.CompletedLevels.Add(levelID);
            }

            var starEntry = CurrentData.LevelStars.FindIndex(x => x.LevelID == levelID);
            if (starEntry != -1)
            {
                if (CurrentData.LevelStars[starEntry].Stars < stars)
                {
                    var entry = CurrentData.LevelStars[starEntry];
                    entry.Stars = stars;
                    CurrentData.LevelStars[starEntry] = entry; 
                }
            }
            else
            {
                CurrentData.LevelStars.Add(new LevelStarData(levelID, stars));
            }
            
            AddActivity($"Complete_{levelID}"); 
            Save();
        }

        public void AwakenLilith()
        {
            if (CurrentData == null) return;
            if (CurrentData.IsLilithAwakened) return;

            CurrentData.IsLilithAwakened = true;
            
            if (!CurrentData.UnlockedUnits.Contains("Lilith"))
            {
                CurrentData.UnlockedUnits.Add("Lilith");
            }
            
            BattleCurrencyManager currencyMgr = FindFirstObjectByType<BattleCurrencyManager>();
            if (currencyMgr != null)
            {
                currencyMgr.SetMaxSeals(150);
            }

            Debug.Log("[SaveManager] Lilith has been awakened! Sovereign power regained.");
            UnlockUnit("Lilith");
            Save();
        }

        public void UnlockUnit(string unitID)
        {
            if (CurrentData == null) return;
            
            if (!CurrentData.UnlockedUnits.Contains(unitID))
            {
                CurrentData.UnlockedUnits.Add(unitID);
            }
            
            // Add to instance inventory if not already there (for unique starter/gift units)
            // Note: Gacha handles its own inventory addition, this is for guaranteed/story unlocks
            if (!CurrentData.UnitInventory.Exists(x => x.UnitID == unitID))
            {
                CurrentData.UnitInventory.Add(new UnitInventoryEntry(unitID));
            }
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
            
            if (CurrentData.Cohorts == null) CurrentData.Cohorts = new List<CohortData>();
            
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
             GetCohort(index); 
             
             CurrentData.Cohorts[index] = new CohortData
             {
                 CohortName = CurrentData.Cohorts[index].CohortName,
                 UnitIDs = unitIDs
             };
             Save();
        }
        #endregion

        #region Internal Logic
        private void CreateNewSave()
        {
            CurrentData = new PlayerData();
            CurrentData.Gold = 0;
            CurrentData.BloodCrest = 0;
            CurrentData.UnlockedUnits = new List<string>() { "Ignis" }; 
            CurrentData.UnitInventory = new List<UnitInventoryEntry>() { new UnitInventoryEntry("Ignis") };
            
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
        #endregion
    }
}
