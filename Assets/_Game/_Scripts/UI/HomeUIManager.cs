using UnityEngine;
using UnityEngine.UI;

namespace MaouSamaTD.UI.MainMenu
{
    /// <summary>
    /// Central hub for the Main Menu. Listens to all main buttons (Conquest, Cohorts, etc.)
    /// and routes them to the correct UI pages via the UIFlowManager.
    /// </summary>
    public class HomeUIManager : MonoBehaviour
    {
        [Header("Roots")]
        [Tooltip("The actual UI Canvas or Panel object that represents the Home Page graphics.")]
        [SerializeField] private GameObject _visualRoot;

        [Header("Main Menu Nav Buttons")]
        [SerializeField] private Button _btnConquest;
        [SerializeField] private Button _btnCohorts;
        [SerializeField] private Button _btnVassals;
        [SerializeField] private Button _btnMandates;
        [SerializeField] private Button _btnThrone;
        [SerializeField] private Button _btnVault;
        [SerializeField] private Button _btnRanks;
        [SerializeField] private Button _btnDaily;
        [SerializeField] private Button _btnGrimoire;

        [Header("Target UI Pages")]
        [Tooltip("Assign the Campaign Page script here.")]
        [SerializeField] private CampaignPage _campaignPage;
        
        [Tooltip("Assign the Barracks/Unit Selection panel here if it acts as the Vassals/Cohorts page.")]
        [SerializeField] private UnitSelectionPanel _unitSelectionPanel;

        private void Start()
        {
            // Hook up all navigation buttons
            if (_btnConquest != null) _btnConquest.onClick.AddListener(OnConquestClicked);
            if (_btnCohorts != null) _btnCohorts.onClick.AddListener(OnCohortsClicked);
            if (_btnVassals != null) _btnVassals.onClick.AddListener(OnVassalsClicked);
            if (_btnMandates != null) _btnMandates.onClick.AddListener(OnMandatesClicked);
            if (_btnThrone != null) _btnThrone.onClick.AddListener(OnThroneClicked);
            if (_btnVault != null) _btnVault.onClick.AddListener(OnVaultClicked);
            if (_btnRanks != null) _btnRanks.onClick.AddListener(OnRanksClicked);
            if (_btnDaily != null) _btnDaily.onClick.AddListener(OnDailyClicked);
            if (_btnGrimoire != null) _btnGrimoire.onClick.AddListener(OnGrimoireClicked);
        }

        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        private void OnConquestClicked()
        {
            if (_campaignPage != null)
            {
                // This automatically triggers campaignPage.ResetState() inside UIFlowManager
                UIFlowManager.Instance.OpenPanel(_campaignPage);
            }
            else
            {
                Debug.LogWarning("[HomeUIManager] Conquest clicked, but CampaignPage is not assigned!");
            }
        }

        private void OnCohortsClicked()
        {
            // If UnitSelectionPanel acts as Cohorts or Vassals
            if (_unitSelectionPanel != null)
            {
                UIFlowManager.Instance.OpenPanel(_unitSelectionPanel);
            }
            else
            {
                Debug.LogWarning("[HomeUIManager] Cohorts clicked, but UnitSelectionPanel is not assigned!");
            }
        }

        private void OnVassalsClicked()
        {
            // If UnitSelectionPanel acts as Cohorts or Vassals
            if (_unitSelectionPanel != null)
            {
                UIFlowManager.Instance.OpenPanel(_unitSelectionPanel);
            }
            else
            {
                Debug.LogWarning("[HomeUIManager] Vassals clicked, but UnitSelectionPanel is not assigned!");
            }
        }

        private void OnMandatesClicked() { Debug.Log("[HomeUIManager] Mandates clicked (Not Implemented Yet)"); }
        private void OnThroneClicked() { Debug.Log("[HomeUIManager] Throne clicked (Not Implemented Yet)"); }
        private void OnVaultClicked() { Debug.Log("[HomeUIManager] Vault clicked (Not Implemented Yet)"); }
        private void OnRanksClicked() { Debug.Log("[HomeUIManager] Ranks clicked (Not Implemented Yet)"); }
        private void OnDailyClicked() { Debug.Log("[HomeUIManager] Daily clicked (Not Implemented Yet)"); }
        private void OnGrimoireClicked() { Debug.Log("[HomeUIManager] Grimoire clicked (Not Implemented Yet)"); }
    }
}
