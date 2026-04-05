using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Data;
using System;

namespace MaouSamaTD.UI.Treasury
{
    /// <summary>
    /// Component for an individual Blood Crest offering card in the shop.
    /// </summary>
    public class TreasuryOfferingItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _packageNameTxt;
        [SerializeField] private TextMeshProUGUI _descriptionTxt;
        [SerializeField] private TextMeshProUGUI _amountTxt;
        [SerializeField] private TextMeshProUGUI _priceTxt;
        [SerializeField] private Image _iconImg;
        [SerializeField] private GameObject _officialOfferingLabel;
        [SerializeField] private Button _buyBtn;

        private StoreItemSO _data;
        public Action<StoreItemSO> OnPurchaseRequested;

        public void Setup(StoreItemSO data)
        {
            _data = data;
            if (_data == null) return;

            if (_packageNameTxt) _packageNameTxt.text = _data.ItemName.ToUpper();
            if (_descriptionTxt) _descriptionTxt.text = _data.Description.ToUpper(); 
            
            if (_amountTxt) 
            {
                if (_data.Type == StoreItemType.Currency)
                    _amountTxt.text = $"{_data.CurrencyAmount:N0} <color=#00F2FF><i>GEMS</i></color>";
                else if (_data.Type == StoreItemType.Skin)
                    _amountTxt.text = "LEVEL 1 <color=#FFE400>SKIN</color>";
                else
                    _amountTxt.text = "OFFERING GIFT";
            }

            if (_priceTxt) 
            {
                if (_data.USDPrice > 0)
                    _priceTxt.text = $"${_data.USDPrice:F2} USD";
                else if (_data.GemPrice > 0)
                    _priceTxt.text = $"{_data.GemPrice:N0} <color=#00F2FF>GEMS</color>";
            }

            if (_iconImg) _iconImg.sprite = _data.Icon;
            if (_officialOfferingLabel) _officialOfferingLabel.SetActive(_data.IsOfficialOffering);

            if (_buyBtn)
            {
                _buyBtn.onClick.RemoveAllListeners();
                _buyBtn.onClick.AddListener(() => OnPurchaseRequested?.Invoke(_data));
            }
        }
    }
}
