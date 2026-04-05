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
        [SerializeField] private TextMeshProUGUI _txtCategoryTag;
        [SerializeField] private Image _imgProgress;
        [SerializeField] private Button _btnClaim;
        
        [Header("Rewards")]
        [SerializeField] private Transform _rewardContainer;
        [SerializeField] private TextMeshProUGUI _txtRewardAmount;
        
        [Header("UI Visuals")]
        [SerializeField] private Image _imgCategoryBG;
        [SerializeField] private Image _imgRewardIcon;
        [SerializeField] private Color _colorDaily = new Color(0.2f, 0.4f, 0.8f);
        [SerializeField] private Color _colorWeekly = new Color(0.5f, 0.2f, 0.7f);
        [SerializeField] private Color _colorStory = new Color(0.7f, 0.2f, 0.6f);
        [SerializeField] private Color _colorEvent = new Color(0.8f, 0.5f, 0.1f);
        
        [Header("Reward Icons")]
        [SerializeField] private Sprite _spriteGold;
        [SerializeField] private Sprite _spriteBloodCrest;

        [Header("Background Status")]
        [SerializeField] private Image _imgBackground;
        [SerializeField] private Color _colorNormal = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        [SerializeField] private Color _colorReady = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        [SerializeField] private Color _colorClaimed = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color _colorReadyBtn = new Color(0.8f, 0.2f, 0.2f, 1f); // Reddish
        [SerializeField] private Color _colorNormalBtn = new Color(0.3f, 0.3f, 0.3f, 1f); // Grayish

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
            if (_txtCategoryTag != null) _txtCategoryTag.text = mandate.CategoryTag;
            
            // Category Styling
            if (_imgCategoryBG != null)
            {
                switch (mandate.Type)
                {
                    case MandateType.Daily: _imgCategoryBG.color = _colorDaily; break;
                    case MandateType.Weekly: _imgCategoryBG.color = _colorWeekly; break;
                    case MandateType.StoryAndLegacy: _imgCategoryBG.color = _colorStory; break;
                    case MandateType.Event: _imgCategoryBG.color = _colorEvent; break;
                }
            }

            // Simple Reward Display (First Reward)
            if (mandate.Rewards != null && mandate.Rewards.Count > 0)
            {
                var firstReward = mandate.Rewards[0];
                if (_imgRewardIcon != null)
                {
                    _imgRewardIcon.sprite = (firstReward.Type == RewardType.GoldCoins) ? _spriteGold : _spriteBloodCrest;
                }

                if (_txtRewardAmount != null)
                {
                    _txtRewardAmount.text = firstReward.Amount.ToString("N0");
                }
            }

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

            if (_btnClaim != null)
            {
                _btnClaim.gameObject.SetActive(true);
                _btnClaim.interactable = completed && !claimed;
                
                var btnText = _btnClaim.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = claimed ? 
                        Assets.SimpleLocalization.Scripts.LocalizationManager.Localize("MANDATES_CLAIMED") : 
                        Assets.SimpleLocalization.Scripts.LocalizationManager.Localize("MANDATES_SEIZE");
                }

                var btnImage = _btnClaim.GetComponent<Image>();
                if (btnImage != null)
                {
                    if (claimed) btnImage.color = _colorClaimed;
                    else btnImage.color = completed ? _colorReadyBtn : _colorNormalBtn;
                }
            }

            if (_imgBackground != null)
            {
                _imgBackground.color = (completed && !claimed) ? _colorReady : _colorNormal;
            }
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
