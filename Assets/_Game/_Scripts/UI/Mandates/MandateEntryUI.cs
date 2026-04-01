using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Data;
using MaouSamaTD.Mandates;

namespace MaouSamaTD.UI.Mandates
{
    public class MandateEntryUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _txtTitle;
        [SerializeField] private TextMeshProUGUI _txtDescription;
        [SerializeField] private TextMeshProUGUI _txtProgress;
        [SerializeField] private Image _imgProgress;
        [SerializeField] private Button _btnClaim;
        [SerializeField] private GameObject _objClaimed;
        
        [Header("Rewards")]
        [SerializeField] private Transform _rewardContainer;
        
        private MandateData _mandate;
        private MandateManager _manager;
        private MandatesPanel _parent;

        public void Setup(MandateData mandate, MandateManager manager, MandatesPanel parent)
        {
            _mandate = mandate;
            _manager = manager;
            _parent = parent;

            if (_txtTitle != null) _txtTitle.text = mandate.Title;
            if (_txtDescription != null) _txtDescription.text = mandate.Description;
            
            Refresh();

            if (_btnClaim != null)
            {
                _btnClaim.onClick.RemoveAllListeners();
                _btnClaim.onClick.AddListener(OnClaimClicked);
            }
        }

        public void Refresh()
        {
            int progress = _manager.GetProgress(_mandate.UniqueID);
            bool completed = progress >= _mandate.RequiredAmount;
            bool claimed = _manager.IsClaimed(_mandate.UniqueID);

            if (_txtProgress != null) _txtProgress.text = $"{progress} / {_mandate.RequiredAmount}";
            if (_imgProgress != null) _imgProgress.fillAmount = (float)progress / _mandate.RequiredAmount;

            if (_btnClaim != null) _btnClaim.gameObject.SetActive(completed && !claimed);
            if (_objClaimed != null) _objClaimed.SetActive(claimed);
            
            if (_btnClaim != null) _btnClaim.interactable = completed && !claimed;
        }

        private void OnClaimClicked()
        {
            if (_manager.ClaimReward(_mandate))
            {
                _parent.RefreshList();
            }
        }
    }
}
