using UnityEngine;
using UnityEngine.UI;
using Assets.SimpleLocalization.Scripts;
using Zenject;
using MaouSamaTD.UI;
using MaouSamaTD.UI.Common;
using MaouSamaTD.UI.MainMenu;
using MaouSamaTD.UI.Cohorts;
using MaouSamaTD.UI.Vassals;
using MaouSamaTD.UI.Mandates;


namespace MaouSamaTD.UI.MainMenu
{
    /// <summary>
    /// Central hub for the Main Menu. Listens to all main buttons (Conquest, Cohorts, etc.)
    /// and routes them to the correct UI pages via the UIFlowManager.
    /// </summary>
    public class HomeUIManager : MonoBehaviour
    {
        [Header("System Panels")]
        [SerializeField] private NavigationFeatures _navFeatures = NavigationFeatures.None;
        public NavigationFeatures ConfiguredNavFeatures => _navFeatures;
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
        [SerializeField] private Button _btnManifest;

        [Header("Global Header Buttons")]
        [SerializeField] private Button _btnSettings;
        public Button _btnCitadel; // Renamed from _btnHome or similar
        
        [Header("Account & Currency Info")]
        [SerializeField] private TMPro.TextMeshProUGUI _accountNameText;
        public CurrencyDisplay _goldDisplay;
        public CurrencyDisplay _bloodCrestDisplay;

        [Header("Nav Overlay")]
        public UINavigationOverlay _navOverlay;

        [Header("Debug")]
        [SerializeField] private bool _debug = true;

        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;

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
            if (_btnManifest != null) _btnManifest.onClick.AddListener(OnManifestClicked);

            if (_btnSettings != null) _btnSettings.onClick.AddListener(OnSettingsClicked);


            UpdateAccountInfo();
            PreheatData();
        }

        private void PreheatData()
        {
            Debug.Log("[HomeUIManager] Starting UI Data Preheating...");
            
            // Find all core pages (even inactive ones) and preheat their data
            var campaign = Object.FindAnyObjectByType<CampaignPage>(FindObjectsInactive.Include);
            if (campaign != null) campaign.Preheat();

            var vassalInventory = Object.FindAnyObjectByType<VassalManagerUI>(FindObjectsInactive.Include);
            if (vassalInventory != null) vassalInventory.Preheat();

            var cohortSquad = Object.FindAnyObjectByType<CohortSquadUI>(FindObjectsInactive.Include);
            if (cohortSquad != null) cohortSquad.Preheat();

            var mandates = Object.FindAnyObjectByType<MandatesPanel>(FindObjectsInactive.Include);
            if (mandates != null) mandates.Preheat();


            Debug.Log("[HomeUIManager] UI Data Preheating Complete.");
        }

        private void UpdateAccountInfo()
        {
            if (_accountNameText != null && _saveManager != null && _saveManager.CurrentData != null)
            {
                string label = LocalizationManager.Localize("Home.Account.Label");
                string playerName = _saveManager.CurrentData.PlayerName.ToUpper();
                _accountNameText.text = $"{label}: {playerName}";
            }
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
            var panel = Object.FindFirstObjectByType<CampaignPage>(FindObjectsInactive.Include);
            if (panel != null)
            {
                UIFlowManager.Instance.OpenPanel(panel);
            }
            else
            {
                Debug.LogWarning("[HomeUIManager] Conquest clicked, but CampaignPage could not be found!");
            }
        }

        private void OnCohortsClicked()
        {
            var cohortPanel = Object.FindAnyObjectByType<CohortSquadUI>(FindObjectsInactive.Include);
            if (cohortPanel != null)
            {
                UIFlowManager.Instance.OpenPanel(cohortPanel);
            }
            else
            {
                Debug.LogWarning("[HomeUIManager] CohortSquadUI not found in scene!");
            }
        }
    
        private void OnVassalsClicked()
        {
            var panel = Object.FindAnyObjectByType<VassalManagerUI>(FindObjectsInactive.Include);
            if (panel != null)
            {
                UIFlowManager.Instance.OpenPanel(panel);
            }
            else
            {
                Debug.LogWarning("[HomeUIManager] CohortManagerUI could not be found!");
            }
        }

        private void OnMandatesClicked()
        {
            var panel = Object.FindAnyObjectByType<MandatesPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                UIFlowManager.Instance.OpenPanel(panel);
            }
        }

        private void OnThroneClicked() { Debug.Log("[HomeUIManager] Throne clicked (Not Implemented Yet)"); }
        private void OnVaultClicked() { Debug.Log("[HomeUIManager] Vault clicked (Not Implemented Yet)"); }
        private void OnRanksClicked() { Debug.Log("[HomeUIManager] Ranks clicked (Not Implemented Yet)"); }
        private void OnDailyClicked()
        {
            var panel = Object.FindAnyObjectByType<MandatesPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                // Both buttons currently go to Mandates, which defaults to Daily.
                UIFlowManager.Instance.OpenPanel(panel);
            }
        }

        private void OnGrimoireClicked() { Debug.Log("[HomeUIManager] Grimoire clicked (Not Implemented Yet)"); }

        private void OnSettingsClicked()
        {
            var panel = Object.FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                UIFlowManager.Instance.OpenPanel(panel);
            }
        }

        private void OnManifestClicked()
        {
            var panel = Object.FindFirstObjectByType<MaouSamaTD.UI.Gacha.GachaPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                UIFlowManager.Instance.OpenPanel(panel);
            }
        }
    }
}
