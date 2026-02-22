using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using MaouSamaTD.Data;
using MaouSamaTD.Managers;
using Zenject;
using System.Collections;

namespace MaouSamaTD.Core
{
    public class AppEntryPoint : MonoBehaviour
    {
        [Header("UI Blocking")]
        [SerializeField] private GameObject _loadingScreenRoot;
        [SerializeField] private GameObject _homeScreenRoot;
        [SerializeField] private MaouSamaTD.UI.MainMenu.AscensionPanel _ascensionPanel;

        [Inject] private SaveManager _saveManager;
        
        // This is a static reference we can use universally since it will be loaded from Addressables
        public static UnitDatabase LoadedUnitDatabase { get; private set; }

        private void Awake()
        {
            // Activate the loading screen instantly on frame 1
            if (_loadingScreenRoot != null) _loadingScreenRoot.SetActive(true);
        }

        private void Start()
        {
            // Wait until Start() to run the coroutine, because Zenject processes [Inject] AFTER Awake()
            StartCoroutine(InitializeGameDataCoroutine());
        }

        private IEnumerator InitializeGameDataCoroutine()
        {
            Debug.Log("[AppEntryPoint] Bootstrapping Addressables...");
            var initHandle = Addressables.InitializeAsync();
            yield return initHandle;

            Debug.Log("[AppEntryPoint] Loading UnitDatabase from Addressables...");
            // Notice we load using the exact address we assigned via MCP ("UnitDatabase")
            var dbHandle = Addressables.LoadAssetAsync<UnitDatabase>("UnitDatabase");
            yield return dbHandle;

            if (dbHandle.Status == AsyncOperationStatus.Succeeded)
            {
                LoadedUnitDatabase = dbHandle.Result;
                Debug.Log($"[AppEntryPoint] Successfully loaded UnitDatabase. Units found: {LoadedUnitDatabase.AllUnits.Count}");
            }
            else
            {
                Debug.LogError("[AppEntryPoint] Failed to load UnitDatabase from Addressables!");
            }

            Debug.Log("[AppEntryPoint] Initializing Save Data...");
            // SaveManager automatically loaded inside its constructor/Zenject Init, but we can double check here.
            if (_saveManager.CurrentData == null)
            {
                 _saveManager.Load();
            }

            Debug.Log("[AppEntryPoint] App Initialization Complete. Enabling UI...");
            if (_loadingScreenRoot != null) _loadingScreenRoot.SetActive(false);
            
            // Check if this is a fresh new save
            if (_saveManager.CurrentData.PlayerName == "Mephisto" && _ascensionPanel != null)
            {
                Debug.Log("[AppEntryPoint] Fresh save detected. Triggering Ascension Sequence.");
                _ascensionPanel.Open();
                if (_homeScreenRoot != null) _homeScreenRoot.SetActive(false);
            }
            else
            {
                if (_homeScreenRoot != null) _homeScreenRoot.SetActive(true);
            }
            
            // You can optionally fire a global event here if other scripts are waiting for init
        }
    }
}
