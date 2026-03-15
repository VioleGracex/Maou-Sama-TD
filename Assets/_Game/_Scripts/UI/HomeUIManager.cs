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

        [Header("Global Header Buttons")]
        [SerializeField] private Button _btnSettings;
        [SerializeField] private Button _btnQuickNav;
        [SerializeField] private Button _btnHome;
        [SerializeField] private Button _btnManifest;

        // Panels are now looked up dynamically to ensure one source of truth.

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

            if (_btnSettings != null) _btnSettings.onClick.AddListener(OnSettingsClicked);
            if (_btnQuickNav != null) _btnQuickNav.onClick.AddListener(OnQuickNavClicked);
            if (_btnHome != null) _btnHome.onClick.AddListener(OnHomeClicked);
            if (_btnManifest != null) _btnManifest.onClick.AddListener(OnManifestClicked);
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
            var panel = Object.FindFirstObjectByType<UnitSelectionPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                UIFlowManager.Instance.OpenPanel(panel);
            }
            else
            {
                Debug.LogWarning("[HomeUIManager] Cohorts clicked, but UnitSelectionPanel could not be found!");
            }
        }

        private void OnVassalsClicked()
        {
            var panel = Object.FindFirstObjectByType<UnitSelectionPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                UIFlowManager.Instance.OpenPanel(panel);
            }
            else
            {
                Debug.LogWarning("[HomeUIManager] Vassals clicked, but UnitSelectionPanel could not be found!");
            }
        }

        private void OnMandatesClicked() { Debug.Log("[HomeUIManager] Mandates clicked (Not Implemented Yet)"); }
        private void OnThroneClicked() { Debug.Log("[HomeUIManager] Throne clicked (Not Implemented Yet)"); }
        private void OnVaultClicked() { Debug.Log("[HomeUIManager] Vault clicked (Not Implemented Yet)"); }
        private void OnRanksClicked() { Debug.Log("[HomeUIManager] Ranks clicked (Not Implemented Yet)"); }
        private void OnDailyClicked() { Debug.Log("[HomeUIManager] Daily clicked (Not Implemented Yet)"); }
        private void OnGrimoireClicked() { Debug.Log("[HomeUIManager] Grimoire clicked (Not Implemented Yet)"); }

        private void OnSettingsClicked()
        {
            var panel = Object.FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                UIFlowManager.Instance.OpenPanel(panel);
            }
        }

        private void OnQuickNavClicked()
        {
            var panel = Object.FindFirstObjectByType<QuickNavPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                UIFlowManager.Instance.OpenPanel(panel);
            }
        }

        private void OnHomeClicked()
        {
            // Clear all panel history and return to Home root
            UIFlowManager.Instance.ClearHistory(true);
            Open();
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
