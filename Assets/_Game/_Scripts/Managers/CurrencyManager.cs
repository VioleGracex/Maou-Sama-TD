using UnityEngine;
using System;

namespace MaouSamaTD.Managers
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject); // Ensure only one instance exists
            }
        }

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

        public void SetMaxSeals(int newMax)
        {
            _maxSeals = newMax;
            OnSealsChanged?.Invoke(CurrentSeals);
        }

        private float _regenTimer;
        #endregion

        #region Lifecycle
        public void Init(MaouSamaTD.Levels.LevelData levelData = null)
        {
            CurrentSeals = _startingSeals;
            
            if (levelData != null)
            {
                _maxSeals = levelData.MaxAuthoritySeals;
                _startingSeals = levelData.StartingAuthoritySeals;
                CurrentSeals = _startingSeals;
            }
            else
            {
                _maxSeals = 30; // Default fallback
            }

            // Tiered Logic: Level Base + Lilith Bonus (30) capped at 99
            SaveManager save = FindFirstObjectByType<SaveManager>();
            if (save != null && save.CurrentData != null)
            {
                int bonus = save.CurrentData.IsLilithAwakened ? 30 : 0;
                
                // If we have a specific persistent MaxSeals override (e.g. from shop/upgrades), use it
                // Otherwise calculate based on level + Lilith
                if (save.CurrentData.MaxSeals > 0)
                {
                    _maxSeals = save.CurrentData.MaxSeals;
                }
                else
                {
                    _maxSeals = Mathf.Min(_maxSeals + bonus, 99);
                }
            }
            
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

        public void GiveSeals(int amount)
        {
            AddSeals(amount);
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