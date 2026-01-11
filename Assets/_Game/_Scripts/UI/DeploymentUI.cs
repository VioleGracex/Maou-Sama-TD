using MaouSamaTD.Managers;
using UnityEngine;
using UnityEngine.UI;
using MaouSamaTD.Units;
using MaouSamaTD.Grid;
using MaouSamaTD.Managers;
using System.Collections.Generic;

namespace MaouSamaTD.UI
{
    public class DeploymentUI : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private List<UnitData> _availableUnits;
        [SerializeField] private UnitData _ignisData; // Special ref for testing
        [SerializeField] private GameObject _buttonPrefab;
        [SerializeField] private Transform _barContainer;
        [SerializeField] private PlayerUnit _unitPrefab; // The actual prefab to spawn

        private UnitData _selectedUnit = null;
        private GameObject _ghostVisual;
        
        [SerializeField] private int _maxCohortSize = 13; // 12 + 1 Friend/Support
        
        private void Start()
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
        }

        private void GenerateButtons()
        {
            // Clear existing
            foreach(Transform child in _barContainer) Destroy(child.gameObject);

            // Simple layout settings if container has LayoutGroup
            // If not, we might want to ensure it does or just instantiate
            
            foreach (var unit in _availableUnits)
            {
                GameObject btnObj = Instantiate(_buttonPrefab, _barContainer);
                Button btn = btnObj.GetComponent<Button>();
                Text btnText = btnObj.GetComponentInChildren<Text>();
                
                // Better Text Format
                if (btnText != null) 
                {
                    btnText.text = $"<b>{unit.UnitName}</b>\n<color=yellow>{unit.DeploymentCost}</color>";
                    btnText.alignment = TextAnchor.MiddleCenter;
                }
                
                // Colorize button based on class?
                Image img = btnObj.GetComponent<Image>();
                if (img != null)
                {
                    if (unit.Class == UnitClass.Melee) img.color = new Color(0.8f, 0.4f, 0.4f); // Reddish
                    else if (unit.Class == UnitClass.Ranged) img.color = new Color(0.4f, 0.4f, 0.8f); // Bluish
                    else if (unit.Class == UnitClass.Healer) img.color = new Color(0.4f, 0.8f, 0.4f); // Greenish
                }
                
                btn.onClick.AddListener(() => SelectUnit(unit));
            }
        }

        private void SelectUnit(UnitData unit)
        {
            _selectedUnit = unit;
            if (_ghostVisual != null) Destroy(_ghostVisual);

            // Create simple ghost
            _ghostVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(_ghostVisual.GetComponent<Collider>());
            // Make semi transparent
            var rend = _ghostVisual.GetComponent<Renderer>();
            rend.material.color = new Color(0, 1, 0, 0.5f);
            
            // Or use the unit's sprite if available
            if (unit.UnitSprite != null)
            {
                // Better ghost later
            }
        }

        private void Update()
        {
            if (_selectedUnit != null && _ghostVisual != null)
            {
                // Follow Mouse
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                     Vector2Int coord = GridManager.Instance.WorldToGridCoordinates(hit.point);
                     Vector3 snapPos = GridManager.Instance.GridToWorldPosition(coord);
                     _ghostVisual.transform.position = snapPos;

                    if (Input.GetMouseButtonDown(0))
                    {
                        TryPlaceUnit(coord);
                    }
                     // Right click to cancel
                    if (Input.GetMouseButtonDown(1))
                    {
                        CancelPlacement();
                    }
                }
            }
        }

        private void TryPlaceUnit(Vector2Int coord)
        {
            Tile tile = GridManager.Instance.GetTileAt(coord);
            // Check Currency
            if (!CurrencyManager.Instance.CanAfford(_selectedUnit.DeploymentCost))
            {
                Debug.Log("Not enough Authority Seals!");
                return;
            }
            if (tile == null) return;
            if (tile.IsOccupied) 
            {
                Debug.Log("Tile Occupied!");
                return;
            }

            // Check Unit Type vs Tile Type
            // Melee -> Walkable (Ground)
            // Ranged -> HighGround
            bool valid = false;
            
            if (_selectedUnit.Class == UnitClass.Melee && tile.Type == TileType.Walkable) valid = true;
            if (_selectedUnit.Class == UnitClass.Ranged && tile.Type == TileType.HighGround) valid = true;

            if (!valid)
            {
                Debug.Log($"Invalid Placement! Unit: {_selectedUnit.Class}, Tile: {tile.Type}");
                return;
            }

            // Place
            SpawnUnit(tile);
            
            // Deduct Cost (TODO)
            CancelPlacement();
        }

        private void SpawnUnit(Tile tile)
        {
            CurrencyManager.Instance.TrySpendSeals(_selectedUnit.DeploymentCost);
            PlayerUnit newUnit = Instantiate(_unitPrefab, tile.transform.position, Quaternion.identity);
            newUnit.Initialize(_selectedUnit);
            
            tile.SetOccupied(true);
            // newUnit.SetTile(tile); // Link back if needed
            
            Debug.Log($"Deployed {_selectedUnit.UnitName}!");
        }

        private void CancelPlacement()
        {
            _selectedUnit = null;
            if (_ghostVisual != null) Destroy(_ghostVisual);
        }
    }
}
