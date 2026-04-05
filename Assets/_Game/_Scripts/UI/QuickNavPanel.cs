using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace MaouSamaTD.UI.MainMenu
{
    /// <summary>
    /// A quick navigation overlay that allows players to jump between major pages 
    /// (e.g., Conquest, Cohorts, Vassals) instantly, similar to Arknights.
    /// </summary>
    public class QuickNavPanel : MonoBehaviour, IUIController
    {
        [Header("UI Controller Architecture")]
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => false; // Overlay doesn't hide back stack
        [SerializeField] private NavigationFeatures _navFeatures = NavigationFeatures.None;
        public NavigationFeatures ConfiguredNavFeatures => _navFeatures;

        [Header("Navigation Buttons")]
        [SerializeField] private Button _btnHome;
        [SerializeField] private Button _btnConquest;
        [SerializeField] private Button _btnCohorts;
        [SerializeField] private Button _btnVassals;
        [SerializeField] private Button _btnManifest;
        [SerializeField] private Button _btnTreasury;

        [Header("Target Pages")]
        [SerializeField] private HomeUIManager _homeUI;
        [SerializeField] private CampaignPage _campaignPage;
        [SerializeField] private MaouSamaTD.UI.Cohorts.CohortSquadUI _cohortSquadPanel;
        [SerializeField] private MaouSamaTD.UI.Vassals.VassalManagerUI _vassalInventoryPanel;

        private void Start()
        {
            if (_btnHome != null) _btnHome.onClick.AddListener(() => NavigateTo(HomeTab.Home));
            if (_btnConquest != null) _btnConquest.onClick.AddListener(() => NavigateTo(HomeTab.Conquest));
            if (_btnCohorts != null) _btnCohorts.onClick.AddListener(() => NavigateTo(HomeTab.Cohorts));
            if (_btnVassals != null) _btnVassals.onClick.AddListener(() => NavigateTo(HomeTab.Vassals));
            
            // Note: Manifest and Treasury would need their respective pages assigned if they exist
        }

        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        public bool RequestClose() => true;

        public void ResetState()
        {
            // No complex state to reset for a quick nav menu
        }

        private void NavigateTo(HomeTab tab)
        {
            // Close the quick nav menu first
            Close();

            // Clear history and jump to the target
            UIFlowManager.Instance.ClearHistory(true);

            switch (tab)
            {
                case HomeTab.Home:
                    if (_homeUI != null) _homeUI.Open();
                    break;
                case HomeTab.Conquest:
                    if (_campaignPage != null) UIFlowManager.Instance.OpenPanel(_campaignPage);
                    break;
                case HomeTab.Cohorts:
                    if (_cohortSquadPanel != null) UIFlowManager.Instance.OpenPanel(_cohortSquadPanel);
                    break;
                case HomeTab.Vassals:
                    if (_vassalInventoryPanel != null) UIFlowManager.Instance.OpenPanel(_vassalInventoryPanel);
                    break;
            }
        }

        private enum HomeTab
        {
            Home,
            Conquest,
            Cohorts,
            Vassals
        }
    }
}
