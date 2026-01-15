using MaouSamaTD.Managers;
using UnityEngine;
using UnityEngine.UI;
using MaouSamaTD.Units;
using MaouSamaTD.Grid;
using System.Collections.Generic;
using TMPro;

namespace MaouSamaTD.UI
{
    public class DeploymentUI : MonoBehaviour
    {
        public static DeploymentUI Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private List<UnitData> _availableUnits;
        [SerializeField] private UnitData _ignisData; // Special ref for testing
        [SerializeField] private GameObject _buttonPrefab;
        [SerializeField] private Transform _barContainer;
        [SerializeField] private PlayerUnit _unitPrefab; // The actual prefab to spawn

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _authoritySealsText; 

        [SerializeField] private int _maxCohortSize = 13; // 12 + 1 Friend/Support
        
        private void Awake()
        {
             if (Instance != null && Instance != this) Destroy(gameObject);
             else Instance = this;
        }

        private HashSet<UnitData> _deployedUnits = new HashSet<UnitData>();
        private Dictionary<UnitData, float> _cooldownTimers = new Dictionary<UnitData, float>();
        private List<UnitButtonUI> _unitButtons = new List<UnitButtonUI>();

        private void OnEnable()
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnSealsChanged += UpdateSealsUI;
        }

        private void OnDisable()
        {
             if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnSealsChanged -= UpdateSealsUI;
        }

        private void Update()
        {
            if (_cooldownTimers.Count > 0)
            {
                List<UnitData> finishedCooldowns = new List<UnitData>();
                
                // create a copy of keys to iterate safely if simple loop
                // But we need to update dictionary values.
                
                // To avoid "collection modified" errors, we can use a separate list of keys or just iterate keys
                List<UnitData> keys = new List<UnitData>(_cooldownTimers.Keys);

                foreach (var unit in keys)
                {
                    _cooldownTimers[unit] -= Time.deltaTime;
                    
                    // Update UI immediately for smooth visual
                    UpdateButtonCooldownVisual(unit);

                    if (_cooldownTimers[unit] <= 0)
                    {
                        finishedCooldowns.Add(unit);
                    }
                }

                foreach (var unit in finishedCooldowns)
                {
                    _cooldownTimers.Remove(unit);
                    UpdateButtonCooldownVisual(unit); // Final clear
                    RefreshButtonsState(); // Check if can afford etc
                }
            }
        }

        private void UpdateButtonCooldownVisual(UnitData unit)
        {
            UnitButtonUI btn = _unitButtons.Find(b => b.Data == unit);
            if (btn != null)
            {
                float currentCooldown = _cooldownTimers.ContainsKey(unit) ? _cooldownTimers[unit] : 0;
                float totalCooldown = unit.RespawnTime;
                
                // Avoid div by zero
                float progress = (totalCooldown > 0) ? (currentCooldown / totalCooldown) : 0;
                
                btn.UpdateCooldown(progress);
            }
        }

        private void UpdateSealsUI(int amount)
        {
            if (_authoritySealsText != null)
                _authoritySealsText.text = $"AUTHORITY SEALS\n<size=40>{amount}</size>";
            
            RefreshButtonsState();
        }

        public void Initialize()
        {
            // Setup Ignis if not in list
            if (_ignisData != null && !_availableUnits.Contains(_ignisData))
            {
                _availableUnits.Insert(0, _ignisData);
            }
            
            // Limit Cohort Size
            if (_availableUnits.Count > _maxCohortSize)
            {
                Debug.LogWarning($"Cohort size exceeded limit of {_maxCohortSize}. Truncating.");
                _availableUnits = _availableUnits.GetRange(0, _maxCohortSize);
            }
            
            GenerateButtons();
            
            // Initial UI Update
            if (CurrencyManager.Instance != null)
                UpdateSealsUI(CurrencyManager.Instance.CurrentSeals);
        }

        private void GenerateButtons()
        {
            // Clear existing
            foreach(Transform child in _barContainer) Destroy(child.gameObject);
            _unitButtons.Clear();
            _deployedUnits.Clear();
            _cooldownTimers.Clear();

            foreach (var unit in _availableUnits)
            {
                GameObject btnObj = Instantiate(_buttonPrefab, _barContainer);
                
                // Add UnitButtonUI if missing (for legacy prefabs)
                UnitButtonUI btnUI = btnObj.GetComponent<UnitButtonUI>();
                if (btnUI == null) btnUI = btnObj.AddComponent<UnitButtonUI>();

                btnUI.Initialize(unit);
                _unitButtons.Add(btnUI);
            }
        }

        private void RefreshButtonsState()
        {
            if (CurrencyManager.Instance == null) return;
            int currentSeals = CurrencyManager.Instance.CurrentSeals;

            foreach (var btnUI in _unitButtons)
            {
                if (btnUI == null) continue;
                
                UnitData unit = btnUI.Data;
                if (unit == null) continue;
                
                bool isDeployed = _deployedUnits.Contains(unit);
                bool canAfford = currentSeals >= unit.DeploymentCost;
                bool isCoolingDown = _cooldownTimers.ContainsKey(unit);
                
                btnUI.UpdateState(canAfford, isDeployed, isCoolingDown);
            }
        }

        public void SpawnUnit(Tile tile, UnitData unitData)
        {
            if (_deployedUnits.Contains(unitData))
            {
                Debug.LogWarning($"Unit {unitData.UnitName} already deployed!");
                return;
            }
            if (_cooldownTimers.ContainsKey(unitData))
            {
                Debug.LogWarning($"Unit {unitData.UnitName} is on cooling down!");
                return;
            }

            CurrencyManager.Instance.TrySpendSeals(unitData.DeploymentCost);
            PlayerUnit newUnit = Instantiate(_unitPrefab, tile.transform.position, Quaternion.identity);
            newUnit.Initialize(unitData);
            
            _deployedUnits.Add(unitData);
            tile.SetOccupied(true);
            
            RefreshButtonsState();
            
            Debug.Log($"Deployed {unitData.UnitName}!");
        }

        public void OnUnitRetreated(UnitData unitData)
        {
            if (_deployedUnits.Contains(unitData))
            {
                _deployedUnits.Remove(unitData);
                
                // Start Cooldown
                _cooldownTimers[unitData] = unitData.RespawnTime;
                
                RefreshButtonsState();
                Debug.Log($"Unit {unitData.UnitName} retreated/defeated. Cooldown started: {unitData.RespawnTime}s");
            }
        }
    }
}
