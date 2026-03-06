using MaouSamaTD.Managers;
using UnityEngine;
using UnityEngine.UI;
using MaouSamaTD.Units;
using MaouSamaTD.Grid;
using System.Collections.Generic;
using TMPro;
using Zenject;
using DG.Tweening;

namespace MaouSamaTD.UI
{
    public class DeploymentUI : MonoBehaviour
    {
        
        [Inject] private CurrencyManager _currencyManager;
        [Inject] private DiContainer _container;
        [Inject] private TutorialManager _tutorialManager;

        [Header("Config")]
        [SerializeField] private GameObject _buttonPrefab;
        [SerializeField] private Transform _barContainer;
        [SerializeField] private PlayerUnit _unitPrefab; 

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _authoritySealsText; 

        // Dynamic State
        private List<UnitData> _availableUnits = new List<UnitData>();
        
        [Header("Animation")]
        [SerializeField] private RectTransform _panelRect; 
        [SerializeField] private Button _toggleButton;
        [SerializeField] private float _hideOffset = 200f; 
        private bool _isVisible = true;
        private Vector2 _visiblePos;

        private HashSet<UnitData> _deployedUnits = new HashSet<UnitData>();
        private Dictionary<UnitData, float> _cooldownTimers = new Dictionary<UnitData, float>();
        private List<UnitButtonUI> _unitButtons = new List<UnitButtonUI>();



        private void OnEnable()
        {
            if (_currencyManager != null)
                _currencyManager.OnSealsChanged += UpdateSealsUI;
            
            if (_toggleButton != null)
                _toggleButton.onClick.AddListener(ToggleVisibility);
        }



        private void OnDisable()
        {
             if (_currencyManager != null)
                _currencyManager.OnSealsChanged -= UpdateSealsUI;
             
             if (_toggleButton != null)
                _toggleButton.onClick.RemoveListener(ToggleVisibility);
        }

        private void Update()
        {
            if (_cooldownTimers.Count > 0)
            {
                List<UnitData> finishedCooldowns = new List<UnitData>();
                
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
                    UpdateButtonCooldownVisual(unit); 
                    RefreshButtonsState(); 
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
                
                float progress = (totalCooldown > 0) ? (currentCooldown / totalCooldown) : 0;
                
                btn.UpdateCooldown(progress);
            }
        }

        private void UpdateSealsUI(int amount)
        {
            if (_authoritySealsText != null)
                _authoritySealsText.text = $"{amount}";
            
            RefreshButtonsState();
        }

        public void Init(List<UnitData> cohort, UnitData supportAssistant)
        {
            if (_panelRect != null) _visiblePos = _panelRect.anchoredPosition;

            _availableUnits.Clear();
            if (cohort != null)
            {
                _availableUnits.AddRange(cohort);
            }

            if (supportAssistant != null && !_availableUnits.Contains(supportAssistant))
            {
                _availableUnits.Add(supportAssistant);
            }
            
            GenerateButtons();
            
            if (_currencyManager != null)
                UpdateSealsUI(_currencyManager.CurrentSeals);
        }

        private void GenerateButtons()
        {
            foreach(Transform child in _barContainer) Destroy(child.gameObject);
            _unitButtons.Clear();
            _deployedUnits.Clear();
            _cooldownTimers.Clear();

            foreach (var unit in _availableUnits)
            {
                GameObject btnObj = _container.InstantiatePrefab(_buttonPrefab, _barContainer);
                
                UnitButtonUI btnUI = btnObj.GetComponent<UnitButtonUI>();
                if (btnUI == null) btnUI = btnObj.AddComponent<UnitButtonUI>();

                btnUI.Initialize(unit);
                _unitButtons.Add(btnUI);
            }
        }

        private void RefreshButtonsState()
        {
            if (_currencyManager == null) return;
            int currentSeals = _currencyManager.CurrentSeals;
            
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
        
        public void UpdateSelectionHighlight(UnitData selectedUnit)
        {
            foreach (var btn in _unitButtons)
            {
                bool isSelected = (selectedUnit != null && btn.Data == selectedUnit);
                btn.SetSelected(isSelected);
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

            _currencyManager.TrySpendSeals(unitData.DeploymentCost);
            PlayerUnit newUnit = Instantiate(_unitPrefab, tile.transform.position, Quaternion.identity);
            
            // Facing Logic
            Grid.GridManager gm = FindFirstObjectByType<Grid.GridManager>();
            if (gm != null && gm.SpawnPoints != null && gm.SpawnPoints.Count > 0)
            {
                // Find closest spawn point
                Vector2Int closestSpawn = gm.SpawnPoints[0];
                float minDist = Vector2.Distance(tile.Coordinate, closestSpawn);
                
                for (int i = 1; i < gm.SpawnPoints.Count; i++)
                {
                    float dist = Vector2.Distance(tile.Coordinate, gm.SpawnPoints[i]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestSpawn = gm.SpawnPoints[i];
                    }
                }

                // If spawn is to the left, face left. If spawn is to the right, face right.
                // Default sprite faces right usually.
                var sr = newUnit.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    sr.flipX = closestSpawn.x < tile.Coordinate.x;
                }
            }
            
            newUnit.Initialize(unitData);
            
            newUnit.CurrentTile = tile;
            tile.SetOccupant(newUnit);

            newUnit.OnRetreat += (u) => OnUnitRetreated(u.Data);
            
            _deployedUnits.Add(unitData);
            
            RefreshButtonsState();
            
            if (_tutorialManager != null) _tutorialManager.OnActionTriggered("UnitPlaced");
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
        
        public void RetreatUnitInstance(PlayerUnit unit)
        {
            if (unit == null) return;
            
            if (unit.CurrentTile != null)
            {
                unit.CurrentTile.SetOccupant(null); // Clear tile
            }
            
            if (unit.Data != null)
            {
                OnUnitRetreated(unit.Data);
            }
            
            Destroy(unit.gameObject);
        }

        public void ToggleVisibility()
        {
            if (_panelRect == null) return;

            _isVisible = !_isVisible;
            
            // Move Down on Hide (Standard Bottom Dock)
            Vector2 targetPos = _isVisible ? _visiblePos : _visiblePos + new Vector2(0, -_hideOffset);
            
            _panelRect.DOAnchorPos(targetPos, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void SetUnitButtonVisibility(string unitName, bool visible)
        {
            // Support both internal name "Lilith" and UI name "UnitButton_Lilith"
            string targetName = unitName.StartsWith("UnitButton_") ? unitName.Substring("UnitButton_".Length) : unitName;

            UnitButtonUI btn = _unitButtons.Find(b => b.Data != null && b.Data.UnitName == targetName);
            if (btn != null)
            {
                btn.gameObject.SetActive(visible);
                Debug.Log($"[DeploymentUI] Set visibility for {targetName} to {visible}");
            }
            else
            {
                Debug.LogWarning($"[DeploymentUI] Could not find button for unit {targetName} to set visibility (Original: {unitName}).");
            }
        }
    }
}
