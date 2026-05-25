using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Assets.SimpleLocalization.Scripts;
using Zenject;
using MaouSamaTD.UI;
using MaouSamaTD.UI.Common;
using MaouSamaTD.UI.MainMenu;
using MaouSamaTD.UI.Cohorts;
using MaouSamaTD.UI.Vassals;
using MaouSamaTD.UI.Mandates;
using MaouSamaTD.UI.Treasury;
using MaouSamaTD.Mandates;
using System.Linq;


namespace MaouSamaTD.UI.MainMenu
{
    /// <summary>
    /// Central hub for the Main Menu. Listens to all main buttons (Conquest, Cohorts, etc.)
    /// and routes them to the correct UI pages via the UIFlowManager.
    /// </summary>
    public class HomeUIManager : MonoBehaviour, IUIController
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
        [SerializeField] private Button _btnChambers;
        [SerializeField] private Button _btnMandates;
        [SerializeField] private Button _btnThrone;
        [SerializeField] private Button _btnTreasury;
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

        [Header("Tutorial")]
        [Tooltip("The blocker / tutorial hand overlay used for home-screen tutorials.")]
        [SerializeField] private UIPopupBlocker _tutorialBlocker;
        [Tooltip("The RectTransform of the Manifest Vassals button, used to show the tutorial pointer.")]
        [SerializeField] private RectTransform _manifestButtonRect;

        [Header("Debug")]
        [SerializeField] private bool _debug = true;

        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;

        // IUIController Implementation
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;
        public bool RequestClose() => false;
        public void ResetState() { }
        private bool _initialized = false;

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // Hook up all navigation buttons
            if (_btnConquest != null) _btnConquest.onClick.AddListener(OnConquestClicked);
            if (_btnCohorts != null) _btnCohorts.onClick.AddListener(OnCohortsClicked);
            if (_btnVassals != null) _btnVassals.onClick.AddListener(OnVassalsClicked);
            if (_btnChambers != null) _btnChambers.onClick.AddListener(OnChambersClicked);
            if (_btnMandates != null) _btnMandates.onClick.AddListener(OnMandatesClicked);
            if (_btnThrone != null) _btnThrone.onClick.AddListener(OnThroneClicked);
            if (_btnTreasury != null) _btnTreasury.onClick.AddListener(OnVaultClicked);
            if (_btnVault != null) _btnVault.onClick.AddListener(OnVaultInventoryClicked);
            if (_btnRanks != null) _btnRanks.onClick.AddListener(OnRanksClicked);
            if (_btnDaily != null) _btnDaily.onClick.AddListener(OnDailyClicked);
            if (_btnGrimoire != null) _btnGrimoire.onClick.AddListener(OnGrimoireClicked);
            if (_btnManifest != null) _btnManifest.onClick.AddListener(OnManifestClicked);

            if (_btnSettings != null) _btnSettings.onClick.AddListener(OnSettingsClicked);


            UpdateAccountInfo();
            PreheatData();
            UpdateNavButtons();

            // Register this home panel as the root panel in UIFlowManager
            if (UIFlowManager.Instance != null)
            {
                UIFlowManager.Instance.OpenPanel(this);
            }

            // Check if we should trigger the Gacha Tutorial (post Level 2)
            StartCoroutine(CheckGachaTutorial());
            StartCoroutine(NotificationUpdateRoutine());
        }

        /// <summary>
        /// Waits one frame to let all systems initialise, then checks whether
        /// the player has just completed Level 2 for the first time and hasn't
        /// yet seen the Gacha tutorial. If so, it starts the guided flow.
        /// </summary>
        private IEnumerator CheckGachaTutorial()
        {
            yield return null; // Wait one frame for all injects / Awake to finish

            if (_saveManager == null || _saveManager.CurrentData == null) yield break;
            if (_saveManager.CurrentData.GachaTutorialShown) yield break;
            if (!_saveManager.IsLevelCompleted("1-2")) yield break;

            Debug.Log("[HomeUIManager] Level 2 cleared – starting Gacha tutorial flow.");

            if (_manifestButtonRect != null && _tutorialBlocker != null)
            {
                // Step 1: Show blocker with a hole over the Manifest button
                _tutorialBlocker.ShowBlockerWithTarget(_manifestButtonRect);

                // Step 2: Wire a one-shot click on the Manifest button to open tutorial gacha
                if (_btnManifest != null)
                {
                    _btnManifest.onClick.RemoveAllListeners();
                    _btnManifest.onClick.AddListener(StartGachaTutorialPull);
                }
            }
            else
            {
                // No blocker configured – jump straight into tutorial mode
                StartGachaTutorialPull();
            }
        }

        private void StartGachaTutorialPull()
        {
            // Re-wire the Manifest button back to normal
            if (_btnManifest != null)
            {
                _btnManifest.onClick.RemoveAllListeners();
                _btnManifest.onClick.AddListener(OnManifestClicked);
            }

            var gachaPanel = Object.FindFirstObjectByType<MaouSamaTD.UI.Gacha.GachaPanel>(FindObjectsInactive.Include);
            if (gachaPanel == null)
            {
                Debug.LogWarning("[HomeUIManager] GachaPanel not found for tutorial!");
                return;
            }

            // Ensure the GameObject itself is active before opening
            gachaPanel.gameObject.SetActive(true);

            // Open through flow manager first so history is correct
            UIFlowManager.Instance.OpenPanel(gachaPanel);

            // Then switch it into tutorial mode (this re-uses the already-opened panel)
            gachaPanel.OpenInTutorialMode(_tutorialBlocker, () =>
            {
                Debug.Log("[HomeUIManager] Gacha tutorial complete – welcome to Manifest Vassals!");
            });
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
                Debug.LogWarning("[HomeUIManager] VassalManagerUI could not be found!");
            }
        }

        private void OnChambersClicked()
        {
            var panel = Object.FindAnyObjectByType<ChambersPageUI>(FindObjectsInactive.Include);
            if (panel != null)
            {
                UIFlowManager.Instance.OpenPanel(panel);
            }
            else
            {
                Debug.LogWarning("[HomeUIManager] ChambersPageUI could not be found!");
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
        private void OnVaultClicked()
        {
            var panel = Object.FindAnyObjectByType<TreasuryVaultUI>(FindObjectsInactive.Include);
            if (panel != null)
            {
                UIFlowManager.Instance.OpenPanel(panel);
            }
        }
        private void OnVaultInventoryClicked()
        {
            var panel = Object.FindAnyObjectByType<VaultInventoryUI>(FindObjectsInactive.Include);
            if (panel != null)
            {
                UIFlowManager.Instance.OpenPanel(panel);
            }
            else
            {
                Debug.LogWarning("[HomeUIManager] VaultInventoryUI could not be found!");
            }
        }
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
            var panel = SettingsPanel.Instance;
            if (panel == null)
            {
                panel = Object.FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include);
            }

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

        private void UpdateNavButtons()
        {
            if (_saveManager == null || _saveManager.CurrentData == null) return;

            // Logic: All buttons except Conquest are disabled until Level 1 ("1-1") is completed.
            // Level 1-1 completed -> Progression to Level 2.
            bool level1Cleared = _saveManager.IsLevelCompleted("1-1");
            
            // If Level 1 is not cleared, we restrict access
            if (!level1Cleared)
            {
                Debug.Log("[HomeUIManager] Level 1-1 not completed. Locking non-essential buttons.");
                
                if (_btnCohorts != null) _btnCohorts.interactable = false;
                if (_btnVassals != null) _btnVassals.interactable = false;
                if (_btnChambers != null) _btnChambers.interactable = false;
                if (_btnMandates != null) _btnMandates.interactable = false;
                if (_btnThrone != null) _btnThrone.interactable = false;
                if (_btnTreasury != null) _btnTreasury.interactable = false;
                if (_btnVault != null) _btnVault.interactable = false;
                if (_btnRanks != null) _btnRanks.interactable = false;
                if (_btnDaily != null) _btnDaily.interactable = false;
                if (_btnGrimoire != null) _btnGrimoire.interactable = false;
                if (_btnManifest != null) _btnManifest.interactable = false;

                // Also disable the Navigation Overlay if it exists to prevent side-nav usage
                if (_navOverlay != null)
                {
                    _navOverlay.gameObject.SetActive(false);
                    Debug.Log("[HomeUIManager] Navigation Overlay disabled until Level 1-1 completion.");
                }
            }
            else
            {
                Debug.Log("[HomeUIManager] Level 1-1 completed. All systems active.");
                // Ensure they are interactable (default)
                if (_btnCohorts != null) _btnCohorts.interactable = true;
                // ... and so on, but usually they start interactable in prefab.
                
                if (_navOverlay != null) _navOverlay.gameObject.SetActive(true);
            }
        }

        private IEnumerator NotificationUpdateRoutine()
        {
            while (true)
            {
                UpdateNotifications();
                yield return new WaitForSeconds(2.0f);
            }
        }

        private void UpdateNotifications()
        {
            if (_saveManager == null || _saveManager.CurrentData == null) return;

            // 1. Mandates notification: Unclaimed completed mandates
            var mandateManager = Object.FindFirstObjectByType<MandatesPanel>(FindObjectsInactive.Include)?.MandateManager;
            if (mandateManager == null)
            {
                mandateManager = Object.FindFirstObjectByType<MandateManager>(FindObjectsInactive.Include);
            }

            bool hasMandateNotif = false;
            if (mandateManager != null && mandateManager.AllMandates != null)
            {
                hasMandateNotif = mandateManager.AllMandates.Any(m => mandateManager.CanClaim(m));
            }

            SetNotificationBadge(_btnMandates, hasMandateNotif);

            // 2. Chambers notification: Any owned unit has Vigor < 100
            bool hasChamberNotif = _saveManager.CurrentData.UnitInventory.Any(u => u.Vigor < 100);
            SetNotificationBadge(_btnChambers, hasChamberNotif);
        }

        private void SetNotificationBadge(Button button, bool show)
        {
            if (button == null) return;

            var badgeName = "NotificationBadge_RedCircle";
            var existingBadge = button.transform.Find(badgeName);

            if (show)
            {
                if (existingBadge == null)
                {
                    var badgeGo = new GameObject(badgeName, typeof(RectTransform), typeof(Image));
                    badgeGo.transform.SetParent(button.transform, false);

                    var rect = badgeGo.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(1f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(-10, -10);
                    rect.sizeDelta = new Vector2(18, 18);

                    var img = badgeGo.GetComponent<Image>();
                    img.color = new Color(1f, 0.2f, 0.2f, 1f);

                    var outline = badgeGo.AddComponent<Outline>();
                    outline.effectColor = Color.white;
                    outline.effectDistance = new Vector2(1, 1);
                }
                else
                {
                    existingBadge.gameObject.SetActive(true);
                }
            }
            else
            {
                if (existingBadge != null)
                {
                    existingBadge.gameObject.SetActive(false);
                }
            }
        }
    }
}
