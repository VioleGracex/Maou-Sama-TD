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
        [Inject] private BattleCurrencyManager _currencyManager;
        [Inject] private DiContainer _container;
        [Inject(Optional = true)] private TutorialManager _tutorialManager;

        [Header("Config")]
        [SerializeField] private GameObject _buttonPrefab;
        [SerializeField] private Transform _barContainer;
        [SerializeField] private PlayerUnit _unitPrefab; 

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _authoritySealsText; 

        // Dynamic State
        private List<UnitData> _availableUnits = new List<UnitData>();
        public List<UnitData> AvailableUnits => _availableUnits;
        
        [Header("Animation")]
        [SerializeField] private RectTransform _panelRect; 
        [SerializeField] private Button _toggleButton;
        [SerializeField] private float _hideOffset = 400f; 
        [SerializeField] private bool _enableButtonEntranceAnimation = true;
        [SerializeField] private float _staggerDelay = 0.08f;
        [SerializeField] private float _entranceDuration = 0.35f;
        private bool _isVisible = true;
        private Vector2 _visiblePos;

        private HashSet<UnitData> _deployedUnits = new HashSet<UnitData>();
        public IEnumerable<UnitData> DeployedUnits => _deployedUnits;
        private Dictionary<UnitData, float> _cooldownTimers = new Dictionary<UnitData, float>();
        private List<UnitButtonUI> _unitButtons = new List<UnitButtonUI>();
        private Dictionary<UnitData, PlayerUnit> _activeInstances = new Dictionary<UnitData, PlayerUnit>();
        private Dictionary<UnitData, float> _vassalHpRatios = new Dictionary<UnitData, float>();
        private Dictionary<UnitData, bool> _isManuallyRetreated = new Dictionary<UnitData, bool>();

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
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.hKey.wasPressedThisFrame)
            {
                ToggleVisibility();
            }

            // 0. Update live HP ratios for all deployed active instances
            foreach (var kvp in _activeInstances)
            {
                if (kvp.Value != null)
                {
                    _vassalHpRatios[kvp.Key] = kvp.Value.CurrentHp / kvp.Value.MaxHp;
                }
            }

            // 1. Recover HP for non-deployed units in real-time
            List<UnitData> unitsToRecover = new List<UnitData>(_availableUnits);
            foreach (var unit in _deployedUnits)
            {
                unitsToRecover.Remove(unit);
            }

            foreach (var unit in unitsToRecover)
            {
                if (unit != null && _vassalHpRatios.ContainsKey(unit))
                {
                    float ratio = _vassalHpRatios[unit];
                    if (ratio < 1.0f)
                    {
                        // Healing Speed: Manual retreat = 10%/sec, Defeated/KO = 2%/sec
                        bool isManual = _isManuallyRetreated.ContainsKey(unit) && _isManuallyRetreated[unit];
                        float healRate = isManual ? 0.10f : 0.02f;
                        
                        ratio += healRate * Time.deltaTime;
                        if (ratio > 1.0f) ratio = 1.0f;
                        
                        _vassalHpRatios[unit] = ratio;
                    }
                }
            }

            // 2. Clear KO status when fully healed
            foreach (var unit in _availableUnits)
            {
                if (unit != null && _vassalHpRatios.ContainsKey(unit) && _vassalHpRatios[unit] >= 1.0f)
                {
                    if (_isManuallyRetreated.ContainsKey(unit) && !_isManuallyRetreated[unit])
                    {
                        _isManuallyRetreated[unit] = true; // No longer KO, fully recovered
                        RefreshButtonsState();
                    }
                }
            }

            // 3. Update Cooldown timers in real-time
            if (_cooldownTimers.Count > 0)
            {
                List<UnitData> activeCooldowns = new List<UnitData>(_cooldownTimers.Keys);
                List<UnitData> finishedCooldowns = new List<UnitData>();

                foreach (var unit in activeCooldowns)
                {
                    _cooldownTimers[unit] -= Time.deltaTime;
                    UpdateButtonCooldownVisual(unit); 

                    if (_cooldownTimers[unit] <= 0)
                    {
                        finishedCooldowns.Add(unit);
                    }
                }

                foreach (var unit in finishedCooldowns)
                {
                    _cooldownTimers[unit].Equals(0); // Dummy/clean
                    _cooldownTimers.Remove(unit);
                    UpdateButtonCooldownVisual(unit); 
                    RefreshButtonsState(); 
                }
            }

            // 4. Update HP Slider visuals on all buttons in real-time
            foreach (var btn in _unitButtons)
            {
                if (btn != null && btn.Data != null)
                {
                    float hpRatio = _vassalHpRatios.ContainsKey(btn.Data) ? _vassalHpRatios[btn.Data] : 1.0f;
                    btn.UpdateHpSlider(hpRatio);
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
                _authoritySealsText.text = $"{amount} / {_currencyManager.MaxSeals}";
            
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

            _vassalHpRatios.Clear();
            _isManuallyRetreated.Clear();
            foreach (var unit in _availableUnits)
            {
                if (unit != null)
                {
                    _vassalHpRatios[unit] = 1.0f;
                    _isManuallyRetreated[unit] = true;
                }
            }
            
            GenerateButtons();
            
            if (_currencyManager != null)
                UpdateSealsUI(_currencyManager.CurrentSeals);
        }

        public void AddUnit(UnitData unit)
        {
            if (unit == null) return;
            
            if (!_availableUnits.Contains(unit))
                _availableUnits.Add(unit);

            if (!_vassalHpRatios.ContainsKey(unit))
            {
                _vassalHpRatios[unit] = 1.0f;
                _isManuallyRetreated[unit] = true;
            }
            
            // Ensure button exists even if unit was already in the list (e.g. for dynamic tutorial additions)
            if (_unitButtons.Exists(b => b.Data == unit))
            {
                Debug.Log($"[DeploymentUI] Button for {unit.UnitName} already exists.");
                return;
            }
            
            // Instantiate button
            GameObject btnObj = _container.InstantiatePrefab(_buttonPrefab, _barContainer);
            UnitButtonUI btnUI = btnObj.GetComponent<UnitButtonUI>();
            if (btnUI == null) btnUI = btnObj.AddComponent<UnitButtonUI>();

            btnUI.Initialize(unit);
            _unitButtons.Add(btnUI);

            if (_enableButtonEntranceAnimation)
            {
                btnObj.transform.localScale = Vector3.zero;
                btnObj.transform.DOScale(Vector3.one, _entranceDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }
            
            RefreshButtonsState();
        }

        private void GenerateButtons()
        {
            if (_barContainer == null) return;
            foreach(Transform child in _barContainer) Destroy(child.gameObject);
            _unitButtons.Clear();
            _deployedUnits.Clear();
            _cooldownTimers.Clear();

            Debug.Log($"[DeploymentUI] Starting GenerateButtons for {_availableUnits.Count} units.");
            int index = 0;
            foreach (var unit in _availableUnits)
            {
                if (unit == null)
                {
                    Debug.LogWarning("[DeploymentUI] Found NULL unit in available units list during button generation!");
                    continue;
                }

                GameObject btnObj = _container.InstantiatePrefab(_buttonPrefab, _barContainer);
                
                UnitButtonUI btnUI = btnObj.GetComponent<UnitButtonUI>();
                if (btnUI == null) btnUI = btnObj.AddComponent<UnitButtonUI>();

                btnUI.Initialize(unit);
                _unitButtons.Add(btnUI);
                Debug.Log($"[DeploymentUI] Generated button for unit: {unit.UnitName} (Cost: {unit.DeploymentCost})");

                if (_enableButtonEntranceAnimation)
                {
                    btnObj.transform.localScale = Vector3.zero;
                    float delay = index * _staggerDelay;
                    btnObj.transform.DOScale(Vector3.one, _entranceDuration)
                        .SetEase(Ease.OutBack)
                        .SetDelay(delay)
                        .SetUpdate(true);
                }
                index++;
            }
            Debug.Log($"[DeploymentUI] Finished GenerateButtons. Total buttons in bar: {_unitButtons.Count}");
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

            if (_currencyManager != null)
                _currencyManager.TrySpendSeals(unitData.DeploymentCost);

            PlayerUnit newUnit = Instantiate(_unitPrefab, tile.transform.position, Quaternion.identity);
            
            // Facing Logic
            Grid.GridManager gm = FindFirstObjectByType<Grid.GridManager>();
            if (gm != null && gm.SpawnPoints != null && gm.SpawnPoints.Count > 0)
            {
                // Find closest spawn point
                Vector2Int closestSpawn = gm.SpawnPoints[0].Coordinate;
                float minDist = Vector2.Distance(tile.Coordinate, closestSpawn);
                
                for (int i = 1; i < gm.SpawnPoints.Count; i++)
                {
                    float dist = Vector2.Distance(tile.Coordinate, gm.SpawnPoints[i].Coordinate);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestSpawn = gm.SpawnPoints[i].Coordinate;
                    }
                }

                // If spawn is to the left (-Z), flip. Default sprite faces Right (+Z/Right).
                // Grid Y is World Z.
                var sr = newUnit.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    sr.flipX = closestSpawn.y < tile.Coordinate.y;
                }
            }
            
            newUnit.Initialize(unitData);
            float initialRatio = _vassalHpRatios.ContainsKey(unitData) ? _vassalHpRatios[unitData] : 1.0f;
            newUnit.SetHpRatio(initialRatio);
            
            newUnit.CurrentTile = tile;
            tile.SetOccupant(newUnit);

            newUnit.OnRetreat += (u) => OnUnitRetreated(u.Data);
            
            _deployedUnits.Add(unitData);
            _activeInstances[unitData] = newUnit;
            
            RefreshButtonsState();
            
            if (_tutorialManager != null) _tutorialManager.OnActionTriggered("UnitPlaced");
            Debug.Log($"Deployed {unitData.UnitName}!");
        }

        public void OnUnitRetreated(UnitData unitData)
        {
            if (_deployedUnits.Contains(unitData))
            {
                _deployedUnits.Remove(unitData);
                
                if (_activeInstances.TryGetValue(unitData, out var instance))
                {
                    if (instance == null || instance.IsDead || instance.CurrentHp <= 0)
                    {
                        _vassalHpRatios[unitData] = 0f;
                        _isManuallyRetreated[unitData] = false; // Defeated/KO -> Slow healing (2%/s)
                    }
                    else
                    {
                        _vassalHpRatios[unitData] = instance.CurrentHp / instance.MaxHp;
                    }
                }
                
                if (_activeInstances.ContainsKey(unitData)) _activeInstances.Remove(unitData);
                
                // Start Cooldown
                _cooldownTimers[unitData] = unitData.RespawnTime;
                
                RefreshButtonsState();
                Debug.Log($"Unit {unitData.UnitName} retreated/defeated. Cooldown started: {unitData.RespawnTime}s");
            }
        }
        
        public void RetreatUnitInstance(PlayerUnit unit)
        {
            if (unit == null) return;
            
            if (unit.Data != null)
            {
                float ratio = unit.CurrentHp / unit.MaxHp;
                _vassalHpRatios[unit.Data] = ratio;
                _isManuallyRetreated[unit.Data] = true; // Manual Retreat -> Fast healing (10%/s)
                if (_currencyManager != null)
                {
                    int refund = Mathf.FloorToInt(unit.Data.DeploymentCost * 0.5f);
                    _currencyManager.AddSeals(refund);
                }
            }
            
            unit.Retreat();
        }

        public void RetreatUnitByData(UnitData unitData)
        {
            if (unitData == null) return;
            if (_activeInstances.TryGetValue(unitData, out var instance))
            {
                float ratio = instance.CurrentHp / instance.MaxHp;
                _vassalHpRatios[unitData] = ratio;
                _isManuallyRetreated[unitData] = true; // Manual Retreat -> Fast healing (10%/s)
                if (_currencyManager != null)
                {
                    int refund = Mathf.FloorToInt(unitData.DeploymentCost * 0.5f);
                    _currencyManager.AddSeals(refund);
                }

                instance.Retreat();
            }
        }

        private void ToggleVisibility()
        {
            if (_panelRect == null) return;
            
            _isVisible = !_isVisible;
            // Assuming docked on the left, so hide offset should move it left (negative X)
            float targetX = _isVisible ? _visiblePos.x : _visiblePos.x - _hideOffset;
            
            _panelRect.DOAnchorPosX(targetX, _entranceDuration).SetEase(Ease.OutQuint);
            
            // Optional: Flip an arrow icon on the button (Horizontal flip)
            if (_toggleButton != null)
            {
                var rect = _toggleButton.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.DOScaleX(_isVisible ? 1f : -1f, _entranceDuration);
                }
            }
        }

        public void SetUnitButtonVisibility(string unitName, bool visible)
        {
            // Support both internal name "Lilith" and UI name "UnitButton_Lilith"
            string targetName = unitName.StartsWith("UnitButton_") ? unitName.Substring("UnitButton_".Length) : unitName;

            UnitButtonUI btn = _unitButtons.Find(b => b.Data != null && b.Data.UnitName == targetName);
            if (btn != null)
            {
                bool wasActive = btn.gameObject.activeSelf;
                btn.gameObject.SetActive(visible);
                Debug.Log($"[DeploymentUI] Set visibility for {targetName} to {visible}");

                // Play beautiful pop-in entrance animation if newly made visible
                if (visible && !wasActive)
                {
                    btn.transform.localScale = Vector3.zero;
                    btn.transform.DOScale(Vector3.one, 0.4f)
                        .SetEase(Ease.OutBack)
                        .SetUpdate(true);
                }
            }
            else
            {
                Debug.LogWarning($"[DeploymentUI] Could not find button for unit {targetName} to set visibility (Original: {unitName}).");
            }
        }
    }
}
