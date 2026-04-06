using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MaouSamaTD.Progression;
using MaouSamaTD.Units;
using MaouSamaTD.Data;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Handles Level Up, Duplicate consumption, and XP meter logic.
    /// </summary>
    public class UnitInspectorXPPanel : MonoBehaviour
    {
        [Header("XP References")]
        [SerializeField] private ScrollRect _duplicatesScrollRect;
        [SerializeField] private GameObject _duplicateItemPrefab;
        [SerializeField] private TextMeshProUGUI _xpMeterValueText;
        [SerializeField] private Button _btnConfirmLevelUp;

        private UnitData _currentUnit;
        private List<UnitInventoryEntry> _selectedDuplicates = new List<UnitInventoryEntry>();
        private MaouSamaTD.Managers.SaveManager _saveManager;

        public void Initialize(MaouSamaTD.Managers.SaveManager saveManager)
        {
            _saveManager = saveManager;
            if (_btnConfirmLevelUp) _btnConfirmLevelUp.onClick.AddListener(PerformLevelUp);
        }

        public void Refresh(UnitData u)
        {
            _currentUnit = u;
            if (u == null || _saveManager == null || _saveManager.CurrentData == null) return;
            
            _selectedDuplicates.Clear();
            if (_btnConfirmLevelUp) _btnConfirmLevelUp.interactable = false;

            // Clear duplicates list
            if (_duplicatesScrollRect != null && _duplicatesScrollRect.content != null)
            {
                foreach (Transform child in _duplicatesScrollRect.content) Destroy(child.gameObject);
                
                var inventory = _saveManager.CurrentData.UnitInventory;
                var duplicates = inventory.FindAll(entry => entry.UnitID == _currentUnit.name);

                foreach (var entry in duplicates)
                {
                    if (_duplicateItemPrefab == null) continue;
                    GameObject go = Instantiate(_duplicateItemPrefab, _duplicatesScrollRect.content);
                    var item = go.GetComponent<VassalDuplicateItemUI>();
                    if (item != null)
                    {
                        item.Setup(entry, _currentUnit.GetSprite(UnitData.UnitImageType.Avatar), (e) => OnDuplicateSelected(e, item));
                    }
                }
            }

            UpdateXPMeter();
        }

        private void UpdateXPMeter()
        {
            if (_currentUnit == null) return;
            int req = ProgressionLogic.GetRequiredXP(_currentUnit.Level);
            if (_xpMeterValueText) _xpMeterValueText.text = $"{_currentUnit.Experience} / {req}";
        }

        private void OnDuplicateSelected(UnitInventoryEntry entry, VassalDuplicateItemUI item)
        {
            if (_selectedDuplicates.Contains(entry))
            {
                _selectedDuplicates.Remove(entry);
                item.SetSelected(false);
            }
            else
            {
                _selectedDuplicates.Add(entry);
                item.SetSelected(true);
            }

            if (_btnConfirmLevelUp) _btnConfirmLevelUp.interactable = _selectedDuplicates.Count > 0;
        }

        private void PerformLevelUp()
        {
            if (_currentUnit == null || _selectedDuplicates.Count == 0) return;

            int totalXPGain = _selectedDuplicates.Count * 500; // Consumption logic
            ProgressionLogic.AddXP(_currentUnit, totalXPGain);
            
            foreach (var entry in _selectedDuplicates)
            {
                _saveManager.CurrentData.UnitInventory.Remove(entry);
            }

            _saveManager.Save();
            Refresh(_currentUnit);
        }
    }
}
