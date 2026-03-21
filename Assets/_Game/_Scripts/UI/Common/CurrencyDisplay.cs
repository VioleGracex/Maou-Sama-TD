using UnityEngine;
using TMPro;
using MaouSamaTD.Managers;
using Zenject;

namespace MaouSamaTD.UI.Common
{
    public enum CurrencyType { Gold, BloodCrest }

    public class CurrencyDisplay : MonoBehaviour
    {
        [SerializeField] private CurrencyType _currencyType;
        [SerializeField] private TextMeshProUGUI _valueText;

        [Inject] private EconomyManager _economyManager;

        private void Start()
        {
            if (_valueText == null) _valueText = GetComponent<TextMeshProUGUI>();
            if (_valueText == null) _valueText = GetComponentInChildren<TextMeshProUGUI>();

            if (_economyManager != null)
            {
                _economyManager.OnGoldChanged += UpdateDisplay;
                _economyManager.OnBloodCrestChanged += UpdateDisplay;
                UpdateDisplay(0); // Initial update
            }
        }

        private void OnDestroy()
        {
            if (_economyManager != null)
            {
                _economyManager.OnGoldChanged -= UpdateDisplay;
                _economyManager.OnBloodCrestChanged -= UpdateDisplay;
            }
        }

        private void OnEnable()
        {
            UpdateDisplay(0);
        }

        private void UpdateDisplay(int _ = 0)
        {
            if (_valueText == null || _economyManager == null) return;

            int value = _currencyType == CurrencyType.Gold ? _economyManager.Gold : _economyManager.BloodCrest;
            _valueText.text = value.ToString("N0");
        }
    }
}
