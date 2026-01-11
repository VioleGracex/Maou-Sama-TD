using UnityEngine;
using TMPro;
using MaouSamaTD.Managers;

namespace MaouSamaTD.UI
{
    public class CurrencyUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _sealsText;

        private void Start()
        {
            // Auto-anchor to Top-Right
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.one;
                rect.anchorMax = Vector2.one;
                rect.pivot = Vector2.one;
                rect.anchoredPosition = new Vector2(-20, -20);
            }

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnSealsChanged += UpdateUI;
                UpdateUI(CurrencyManager.Instance.CurrentSeals);
            }
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnSealsChanged -= UpdateUI;
            }
        }

        private void UpdateUI(int amount)
        {
            if (_sealsText != null)
            {
                _sealsText.text = $"DP: {amount}";
            }
        }
    }
}