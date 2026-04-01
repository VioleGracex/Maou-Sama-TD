using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using MaouSamaTD.Data;
using MaouSamaTD.Managers;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


namespace MaouSamaTD.Mandates
{
    public class MandateManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string _mandateLabel = "Mandate";
        
        private List<MandateData> _allMandates = new List<MandateData>();
        private AsyncOperationHandle<IList<MandateData>> _loadHandle;

        [Header("Debug")]
        [SerializeField] private bool _debug = true;

        [Inject] private SaveManager _saveManager;
        
        public IEnumerable<MandateData> AllMandates => _allMandates;

        private void Awake()
        {
            LoadMandates();
        }

        private void OnDestroy()
        {
            if (_loadHandle.IsValid())
            {
                Addressables.Release(_loadHandle);
            }
        }

        private void LoadMandates()
        {
            _allMandates.Clear();
            _loadHandle = Addressables.LoadAssetsAsync<MandateData>((object)_mandateLabel, null);
            _loadHandle.Completed += handle => 
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _allMandates.AddRange(handle.Result);
                    if (_debug) Debug.Log($"[MandateManager] Successfully loaded {_allMandates.Count} mandates from Addressables.");
                }
                else
                {
                    Debug.LogError($"[MandateManager] Failed to load mandates from label '{_mandateLabel}'. Error: {handle.OperationException}");
                }
            };
        }

        public int GetProgress(string mandateID)
        {
            var data = _saveManager.CurrentData.MandateProgress.FirstOrDefault(p => p.MandateID == mandateID);
            return data.Progress;
        }

        public bool IsClaimed(string mandateID)
        {
            return _saveManager.CurrentData.ClaimedMandates.Contains(mandateID);
        }

        public bool IsCompleted(MandateData mandate)
        {
            return GetProgress(mandate.UniqueID) >= mandate.RequiredAmount;
        }

        public bool CanClaim(MandateData mandate)
        {
            return IsCompleted(mandate) && !IsClaimed(mandate.UniqueID);
        }

        public void AddProgress(string key, int amount)
        {
            foreach (var mandate in _allMandates)
            {
                if (mandate.RequirementKey == key && !IsClaimed(mandate.UniqueID))
                {
                    UpdateMandateProgress(mandate.UniqueID, amount);
                }
            }
            _saveManager.Save();
        }

        private void UpdateMandateProgress(string mandateID, int amount)
        {
            var progressList = _saveManager.CurrentData.MandateProgress;
            int index = progressList.FindIndex(p => p.MandateID == mandateID);
            
            if (index >= 0)
            {
                var p = progressList[index];
                p.Progress += amount;
                progressList[index] = p;
            }
            else
            {
                progressList.Add(new MandateProgressData(mandateID, amount));
            }
        }

        public bool ClaimReward(MandateData mandate)
        {
            if (!CanClaim(mandate)) return false;

            foreach (var reward in mandate.Rewards)
            {
                GrantReward(reward);
            }

            _saveManager.CurrentData.ClaimedMandates.Add(mandate.UniqueID);
            _saveManager.Save();
            return true;
        }

        private void GrantReward(RewardData reward)
        {
            switch (reward.Type)
            {
                case RewardType.GoldCoins:
                    _saveManager.CurrentData.Gold += reward.Amount;
                    break;
                case RewardType.BloodCrests:
                    _saveManager.CurrentData.BloodCrest += reward.Amount;
                    break;
            }
        }
    }
}
