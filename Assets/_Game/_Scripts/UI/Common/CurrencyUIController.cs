using UnityEngine;
using TMPro;
using MaouSamaTD.Managers;
using Zenject;

namespace MaouSamaTD.UI.Common
{
    /// <summary>
    /// Controller for the Header Currency UI.
    /// Listens to EconomyManager events and updates Gold and Blood Crest text elements.
    /// </summary>
    public class CurrencyUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _goldTxt;
        [SerializeField] private TextMeshProUGUI _bloodCrestTxt;

        private EconomyManager _economyManager;

        [Inject]
        public void Construct(EconomyManager economyManager)
        {
            _economyManager = economyManager;
        }

        private void Start()
        {
            UpdateUI();

            // Subscribe to events
            if (_economyManager != null)
            {
                _economyManager.OnGoldChanged += HandleGoldChanged;
                _economyManager.OnBloodCrestChanged += HandleBloodCrestChanged;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe to avoid memory leaks
            if (_economyManager != null)
            {
                _economyManager.OnGoldChanged -= HandleGoldChanged;
                _economyManager.OnBloodCrestChanged -= HandleBloodCrestChanged;
            }
        }

        private void HandleGoldChanged(int val)
        {
            if (_goldTxt != null)
            {
                _goldTxt.text = val.ToString();
            }
        }

        private void HandleBloodCrestChanged(int val)
        {
            if (_bloodCrestTxt != null)
            {
                _bloodCrestTxt.text = val.ToString();
            }
        }

        public void UpdateUI()
        {
            if (_economyManager != null)
            {
                HandleGoldChanged(_economyManager.Gold);
                HandleBloodCrestChanged(_economyManager.BloodCrest);
            }
        }
    }
}
