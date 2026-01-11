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

        private void UpdateSealsUI(int amount)
        {
            if (_authoritySealsText != null)
                _authoritySealsText.text = $"AUTHORITY SEALS\n<size=40>{amount}</size>";
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

            foreach (var unit in _availableUnits)
            {
                GameObject btnObj = Instantiate(_buttonPrefab, _barContainer);
                Button btn = btnObj.GetComponent<Button>();
                TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                
                // Better Text Format
                if (btnText != null) 
                {
                    btnText.text = $"<b>{unit.UnitName}</b>\n<color=yellow>{unit.DeploymentCost}</color>";
                    btnText.alignment = TextAlignmentOptions.Center;
                }
                
                // Colorize button based on class?
                Image img = btnObj.GetComponent<Image>();
                if (img != null)
                {
                    if (unit.Class == UnitClass.Melee) img.color = new Color(0.8f, 0.4f, 0.4f); // Reddish
                    else if (unit.Class == UnitClass.Ranged) img.color = new Color(0.4f, 0.4f, 0.8f); // Bluish
                    else if (unit.Class == UnitClass.Healer) img.color = new Color(0.4f, 0.8f, 0.4f); // Greenish
                }
                
                // Add Drag Handler logic
                UnitDragHandler dragHandler = btnObj.AddComponent<UnitDragHandler>();
                dragHandler.Initialize(unit);
            }
        }

        public void SpawnUnit(Tile tile, UnitData unitData)
        {
            CurrencyManager.Instance.TrySpendSeals(unitData.DeploymentCost);
            PlayerUnit newUnit = Instantiate(_unitPrefab, tile.transform.position, Quaternion.identity);
            newUnit.Initialize(unitData);
            
            tile.SetOccupied(true);
            
            Debug.Log($"Deployed {unitData.UnitName}!");
        }
    }
}
