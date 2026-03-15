using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MaouSamaTD.Data;

namespace MaouSamaTD.UI.Gacha
{
    public class GachaDetailsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _detailedDescText;
        [SerializeField] private TextMeshProUGUI _probabilityText;
        [SerializeField] private Button _btnClose;

        public void Initialize(GachaBannerSO banner)
        {
            if (_titleText != null) _titleText.text = banner.BannerName + " Details";
            if (_detailedDescText != null) _detailedDescText.text = banner.DetailedDescription;
            if (_probabilityText != null) _probabilityText.text = banner.ProbabilityDetails;
            
            if (_btnClose != null)
            {
                _btnClose.onClick.RemoveAllListeners();
                _btnClose.onClick.AddListener(Close);
            }
        }

        public void Open(GachaBannerSO banner)
        {
            Initialize(banner);
            if (_visualRoot != null) _visualRoot.SetActive(true);
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }
    }
}
