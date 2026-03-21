using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Assets.SimpleLocalization.Scripts;
using MaouSamaTD.UI.MainMenu;
using MaouSamaTD.UI.Gacha;
using MaouSamaTD.UI.Vassals;
using TMPro;

namespace MaouSamaTD.UI
{
    public class UINavigationOverlay : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] public RectTransform _menuPanel;
        [SerializeField] public float _hiddenY = 200f;
        [SerializeField] public float _shownY = 0f;
        [SerializeField] public float _duration = 0.3f;
        [SerializeField] public Ease _ease = Ease.OutBack;

        [Header("Nav Buttons")]
        [SerializeField] public Button _btnHome;
        [SerializeField] public Button _btnCampaign;
        [SerializeField] private Button _btnShop;
        [SerializeField] public Button _btnVassals;
        [SerializeField] public Button _btnCohorts;
        [SerializeField] public Button _btnManifest;

        [Header("Indicators")]
        [SerializeField] public GameObject[] _indicators;

        private bool _isOpen = false;

        private void Start()
        {
            if (_menuPanel != null) 
                _menuPanel.anchoredPosition = new Vector2(_menuPanel.anchoredPosition.x, _hiddenY);
            
            gameObject.SetActive(false);

            if (_btnHome) _btnHome.onClick.AddListener(() => NavigateToHome());
            if (_btnCampaign) _btnCampaign.onClick.AddListener(() => NavigateTo<CampaignPage>());
            if (_btnVassals) _btnVassals.onClick.AddListener(() => NavigateTo<VassalsBarracksPanel>());
            if (_btnCohorts) _btnCohorts.onClick.AddListener(() => NavigateTo<CohortManagerPanel>());
            if (_btnManifest) _btnManifest.onClick.AddListener(() => NavigateTo<GachaPanel>());
            
            LocalizeUI();
        }

        private void LocalizeUI()
        {
            if (_btnCampaign) _btnCampaign.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Home.Navigation.Campaign");
            if (_btnShop) _btnShop.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Home.Navigation.Shop");
            if (_btnVassals) _btnVassals.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Home.Navigation.Vassals");
            if (_btnCohorts) _btnCohorts.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Home.Navigation.Cohorts");
            if (_btnManifest) _btnManifest.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Home.Navigation.Manifest");
        }

        public void Toggle()
        {
            if (_isOpen) Hide();
            else Show();
        }

        public void Show()
        {
            if (_isOpen) return;
            _isOpen = true;
            gameObject.SetActive(true);
            _menuPanel.DOKill();
            _menuPanel.DOAnchorPosY(_shownY, _duration).SetEase(_ease).SetUpdate(true);
        }

        public void Hide()
        {
            if (!_isOpen) return;
            _isOpen = false;
            _menuPanel.DOKill();
            _menuPanel.DOAnchorPosY(_hiddenY, _duration).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));
        }

        public void UpdateHighlight(System.Type pageType)
        {
            if (_indicators == null || _indicators.Length < 5) return;
            _indicators[0].SetActive(pageType == typeof(CampaignPage));
            _indicators[1].SetActive(false); // Shop
            _indicators[2].SetActive(pageType == typeof(VassalsBarracksPanel));
            _indicators[3].SetActive(pageType == typeof(CohortManagerPanel));
            _indicators[4].SetActive(pageType == typeof(GachaPanel));
        }

        private void NavigateTo<T>() where T : MonoBehaviour, IUIController
        {
            Hide();
            var panel = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (panel != null) UIFlowManager.Instance.OpenPanel(panel);
        }

        private void NavigateToHome()
        {
            Hide();
            UIFlowManager.Instance.ClearHistory(true);
            var home = Object.FindAnyObjectByType<HomeUIManager>(FindObjectsInactive.Include);
            if (home != null) home.Open();
        }
    }
}
