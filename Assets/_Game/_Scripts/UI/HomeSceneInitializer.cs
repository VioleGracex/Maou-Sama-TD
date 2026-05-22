using UnityEngine;
using Zenject;
using MaouSamaTD.UI;
using MaouSamaTD.UI.MainMenu;
using MaouSamaTD.UI.Cohorts;
using MaouSamaTD.UI.Vassals;
using MaouSamaTD.UI.Mandates;
using MaouSamaTD.UI.Treasury;

namespace MaouSamaTD.UI
{
    public class HomeSceneInitializer : MonoBehaviour
    {
        [Header("Required UI Components")]
        [SerializeField] private UINavigationOverlay _navigationOverlay;
        [SerializeField] private AscensionPanel _ascensionPanel;
        [SerializeField] private CohortSquadUI _cohortSquadUI;
        [SerializeField] private VassalManagerUI _vassalManager;
        [SerializeField] private MandatesPanel _mandatesPanel;
        [SerializeField] private TreasuryVaultUI _treasuryVault;
        [SerializeField] private VaultInventoryUI _vaultInventory;
        [SerializeField] private ChambersPageUI _chambersPage;
        [SerializeField] private SettingsPanel _settingsPanel;
        [SerializeField] private CampaignPage _campaignPage;
        [SerializeField] private HomeUIManager _homeUIManager;
        [SerializeField] private HomeUIController_UGUI _homeUIController;
        [SerializeField] private BriefingPanel _briefingPanel;

        private void Awake()
        {
            // Auto-locate components if not manually assigned in inspector
            if (_navigationOverlay == null) _navigationOverlay = FindObjectOfType<UINavigationOverlay>(true);
            if (_ascensionPanel == null) _ascensionPanel = FindObjectOfType<AscensionPanel>(true);
            if (_cohortSquadUI == null) _cohortSquadUI = FindObjectOfType<CohortSquadUI>(true);
            if (_vassalManager == null) _vassalManager = FindObjectOfType<VassalManagerUI>(true);
            if (_mandatesPanel == null) _mandatesPanel = FindObjectOfType<MandatesPanel>(true);
            if (_treasuryVault == null) _treasuryVault = FindObjectOfType<TreasuryVaultUI>(true);
            if (_vaultInventory == null) _vaultInventory = FindObjectOfType<VaultInventoryUI>(true);
            if (_chambersPage == null) _chambersPage = FindObjectOfType<ChambersPageUI>(true);
            if (_settingsPanel == null) _settingsPanel = FindObjectOfType<SettingsPanel>(true);
            if (_campaignPage == null) _campaignPage = FindObjectOfType<CampaignPage>(true);
            if (_homeUIManager == null) _homeUIManager = FindObjectOfType<HomeUIManager>(true);
            if (_homeUIController == null) _homeUIController = FindObjectOfType<HomeUIController_UGUI>(true);
            if (_briefingPanel == null) _briefingPanel = FindObjectOfType<BriefingPanel>(true);
        }

        private void Start()
        {
            Debug.Log("[HomeSceneInitializer] Starting coordinated UI Boot Sequence...");

            // 1. Initialize core layouts & overlays
            if (_navigationOverlay != null)
            {
                Debug.Log("[HomeSceneInitializer] Initializing UINavigationOverlay...");
                _navigationOverlay.Initialize();
            }

            // 2. Initialize secondary overlays and panels
            if (_briefingPanel != null)
            {
                Debug.Log("[HomeSceneInitializer] Initializing BriefingPanel...");
                _briefingPanel.Initialize();
            }

            // 3. Initialize all sub-pages
            if (_ascensionPanel != null)
            {
                Debug.Log("[HomeSceneInitializer] Initializing AscensionPanel...");
                _ascensionPanel.Initialize();
            }
            if (_cohortSquadUI != null)
            {
                Debug.Log("[HomeSceneInitializer] Initializing CohortSquadUI...");
                _cohortSquadUI.Initialize();
            }
            if (_treasuryVault != null)
            {
                Debug.Log("[HomeSceneInitializer] Initializing TreasuryVaultUI...");
                _treasuryVault.Initialize();
            }
            if (_vaultInventory != null)
            {
                Debug.Log("[HomeSceneInitializer] Initializing VaultInventoryUI...");
                _vaultInventory.Initialize();
            }
            if (_settingsPanel != null)
            {
                Debug.Log("[HomeSceneInitializer] Initializing SettingsPanel...");
                _settingsPanel.Initialize();
            }

            // 4. Initialize CampaignPage (loads map buttons, splines, left sidebar)
            if (_campaignPage != null)
            {
                Debug.Log("[HomeSceneInitializer] Initializing CampaignPage...");
                _campaignPage.Initialize();
            }

            // 5. Initialize the central HomeUIManager (routes gacha tutorial checks and starts notifications)
            if (_homeUIManager != null)
            {
                Debug.Log("[HomeSceneInitializer] Initializing HomeUIManager...");
                _homeUIManager.Initialize();
            }

            // 6. Initialize HomeUIController_UGUI (loads active character settings)
            if (_homeUIController != null)
            {
                Debug.Log("[HomeSceneInitializer] Initializing HomeUIController_UGUI...");
                _homeUIController.Initialize();
            }

            Debug.Log("[HomeSceneInitializer] UI Boot Sequence Complete!");
        }
    }
}
