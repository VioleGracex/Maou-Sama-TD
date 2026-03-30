using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Specialized container for a unit within a Cohort Roster.
    /// Responsible for handling "Empty" states, "Assign" prompts, and wrapping a UnitCardUI.
    /// </summary>
    public class CohortSlot : MonoBehaviour
    {
        [SerializeField] private GameObject _emptyVisual; // Visual graphic representing empty state ("+")
        [SerializeField] private TextMeshProUGUI _emptySlotText; // Text for empty state ("ASSIGN SLOT X")
        [SerializeField] private MaouSamaTD.UI.MainMenu.UnitCardUI _unitCardUI; // The visual card component
        [SerializeField] private Button _button;
        
        public event System.Action<int> OnClick;
        public int Index { get; private set; }

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null) _button.onClick.AddListener(HandleClick);
        }

        public void SetIndex(int index)
        {
            Index = index;
            UpdateEmptyText();
        }

        private void UpdateEmptyText()
        {
            if (_emptySlotText != null)
            {
                // Localization key: UI_COHORT_SLOT_ASSIGN
                string key = "UI_COHORT_SLOT_ASSIGN";
                var loc = Assets.SimpleLocalization.Scripts.LocalizationManager.Dictionary;
                if (Assets.SimpleLocalization.Scripts.LocalizationManager.HasKey(key))
                {
                    _emptySlotText.text = Assets.SimpleLocalization.Scripts.LocalizationManager.Localize(key, Index + 1);
                }
                else
                {
                    _emptySlotText.text = $"ASSIGN SLOT {Index + 1}";
                }
            }
        }

        /// <summary>
        /// Assigns a unit to this slot and hides empty visuals.
        /// </summary>
        public void SetUnit(MaouSamaTD.Units.UnitData unitData, System.Action<UnityEngine.Component> onClick = null)
        {
            if (_emptyVisual != null) _emptyVisual.SetActive(false);
            if (_emptySlotText != null) _emptySlotText.gameObject.SetActive(false); // Hide assignment prompt

            if (_unitCardUI != null)
            {
                Debug.Log($"[CohortSlot] {gameObject.name} [Index: {Index}] SET UNIT: {(unitData != null ? unitData.UnitName : "NULL")}");
                _unitCardUI.gameObject.SetActive(true); 
                
                var callback = onClick ?? ((comp) => HandleClick());
                _unitCardUI.Setup(unitData, callback);
                _unitCardUI.UpdateVisuals(unitData); // Redundantly ensure visuals are active
                _unitCardUI.SetInteractable(false);
            }
            else
            {
                Debug.LogWarning($"[CohortSlot] {gameObject.name} is missing the UnitCardUI reference!");
            }
        }

        /// <summary>
        /// Clears the unit from this slot and shows the empty assignment prompt.
        /// </summary>
        public void SetEmpty()
        {
            if (_emptyVisual != null) _emptyVisual.SetActive(true);
            if (_emptySlotText != null) 
            {
                _emptySlotText.gameObject.SetActive(true); // Show assignment prompt
                UpdateEmptyText();
            }

            if (_unitCardUI != null)
            {
                _unitCardUI.gameObject.SetActive(false); // Turn off the card visual when slot is empty
            }
        }

        public MaouSamaTD.Units.UnitData Data => _unitCardUI != null ? _unitCardUI.Data : null;

        public void SetSelectionState(int index)
        {
            if (_unitCardUI != null) _unitCardUI.SetSelectionState(index);
        }

        private void HandleClick()
        {
            OnClick?.Invoke(Index);
        }
    }
}