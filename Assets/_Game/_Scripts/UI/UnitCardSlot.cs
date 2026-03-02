using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace MaouSamaTD.UI
{
    public class UnitCardSlot : MonoBehaviour
    {
        [SerializeField] private GameObject _emptyVisual; // Visual graphic representing empty state ("+")
        [SerializeField] private MaouSamaTD.UI.MainMenu.UnitCardUI _unitCardUI; // The pre-placed unit card child object
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
        }

        public void SetUnit(MaouSamaTD.Units.UnitData unitData)
        {
            if (_emptyVisual != null) _emptyVisual.SetActive(false);

            if (_unitCardUI != null)
            {
                Debug.Log($"[UnitCardSlot] Slot {Index} setting unit: {(unitData != null ? unitData.UnitName : "NULL")}");
                _unitCardUI.Setup(unitData);
            }
            else
            {
                Debug.LogError($"[UnitCardSlot] Slot {Index} missing _unitCardUI reference!");
            }
        }

        public void SetEmpty()
        {
            if (_emptyVisual != null) _emptyVisual.SetActive(true);

            if (_unitCardUI != null)
            {
                Debug.Log($"[UnitCardSlot] Slot {Index} setting empty.");
                _unitCardUI.Setup(null);
            }
        }

        private void HandleClick()
        {
            OnClick?.Invoke(Index);
        }
    }
}