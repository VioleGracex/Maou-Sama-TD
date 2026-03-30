using UnityEngine;
using System;
using MaouSamaTD.Managers;
using Zenject;

namespace MaouSamaTD.Managers
{
    /// <summary>
    /// Global manager for persistent currencies (Gold and Blood Crest).
    /// Lives across scenes as a singleton.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        [Inject] private SaveManager _saveManager;

        public event Action<int> OnGoldChanged;
        public event Action<int> OnBloodCrestChanged;

        public int Gold => _saveManager?.CurrentData != null ? _saveManager.CurrentData.Gold : 0;
        public int BloodCrest => _saveManager?.CurrentData != null ? _saveManager.CurrentData.BloodCrest : 0;

        public void AddGold(int amount)
        {
            if (_saveManager == null) return;
            _saveManager.AddGold(amount);
            OnGoldChanged?.Invoke(Gold);
        }

        public bool TrySpendGold(int cost)
        {
            if (_saveManager == null) return false;
            if (Gold >= cost)
            {
                _saveManager.SpendGold(cost);
                OnGoldChanged?.Invoke(Gold);
                return true;
            }
            return false;
        }

        public void AddBloodCrest(int amount)
        {
            if (_saveManager == null) return;
            _saveManager.AddBloodCrest(amount);
            OnBloodCrestChanged?.Invoke(BloodCrest);
        }

        public bool TrySpendBloodCrest(int cost)
        {
            if (_saveManager == null) return false;
            if (BloodCrest >= cost)
            {
                _saveManager.SpendBloodCrest(cost);
                OnBloodCrestChanged?.Invoke(BloodCrest);
                return true;
            }
            return false;
        }
    }
}
