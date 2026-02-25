using UnityEngine;
using System;

namespace MaouSamaTD.Managers
{
    public class CurrencyManager : MonoBehaviour
    {
        #region Events
        public event Action<int> OnSealsChanged;
        #endregion

        #region Fields
        [Header("Settings")]
        [SerializeField] private int _startingSeals = 10;
        [SerializeField] private int _maxSeals = 99;
        [SerializeField] private float _regenInterval = 1f; // Seconds per seal
        [SerializeField] private int _regenAmount = 1;

        public int CurrentSeals { get; private set; }
        public int MaxSeals => _maxSeals;

        private float _regenTimer;
        #endregion

        #region Lifecycle
        public void Init()
        {
            CurrentSeals = _startingSeals;
            OnSealsChanged?.Invoke(CurrentSeals);
        }

        private void Update()
        {
            HandleRegen();
        }
        #endregion

        #region Public API
        public void AddSeals(int amount)
        {
            CurrentSeals = Mathf.Min(CurrentSeals + amount, _maxSeals);
            OnSealsChanged?.Invoke(CurrentSeals);
        }

        public void SetSeals(int amount)
        {
            CurrentSeals = Mathf.Clamp(amount, 0, _maxSeals);
            OnSealsChanged?.Invoke(CurrentSeals);
            Debug.Log($"[CurrencyManager] Seals set to {CurrentSeals}.");
        }

        public bool CanAfford(int cost)
        {
            return CurrentSeals >= cost;
        }

        public bool TrySpendSeals(int cost)
        {
            if (CanAfford(cost))
            {
                CurrentSeals -= cost;
                OnSealsChanged?.Invoke(CurrentSeals);
                return true;
            }
            return false;
        }
        #endregion

        #region Internal Logic
        private void HandleRegen()
        {
            if (CurrentSeals >= _maxSeals) return;

            _regenTimer += Time.deltaTime;
            if (_regenTimer >= _regenInterval)
            {
                _regenTimer = 0f;
                AddSeals(_regenAmount);
            }
        }
        #endregion
    }
}