using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using MaouSamaTD.Data;
using MaouSamaTD.Managers;
using Zenject;
using System.Collections;
using System;

namespace MaouSamaTD.Core
{
    public class AppEntryPoint : MonoBehaviour
    {
        [Header("UI Routing")]
        [SerializeField] private MaouSamaTD.UI.MainMenu.HomeUIManager _homeUIManager;
        [SerializeField] private MaouSamaTD.UI.MainMenu.AscensionPanel _ascensionPanel;

        [Inject] private SaveManager _saveManager;
        
        // This is a static reference we can use universally since it will be loaded from Addressables
        public static UnitDatabase LoadedUnitDatabase { get; private set; }

        public void StartBootSequence(Action<float> onProgress, Action onComplete)
        {
            StartCoroutine(InitializeGameDataCoroutine(onProgress, onComplete));
        }

        private IEnumerator InitializeGameDataCoroutine(Action<float> onProgress, Action onComplete)
        {
            Debug.Log("[AppEntryPoint] Bootstrapping Addressables...");
            var initHandle = Addressables.InitializeAsync();
            while (!initHandle.IsDone)
            {
                onProgress?.Invoke(initHandle.PercentComplete * 0.2f);
                yield return null;
            }

            Debug.Log("[AppEntryPoint] Loading UnitDatabase from Addressables...");
            var dbHandle = Addressables.LoadAssetAsync<UnitDatabase>("UnitDatabase");
            while (!dbHandle.IsDone)
            {
                onProgress?.Invoke(0.2f + dbHandle.PercentComplete * 0.7f);
                yield return null;
            }

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

            onProgress?.Invoke(1.0f);
            onComplete?.Invoke();
        }

        public void ProceedToGame()
        {
            Debug.Log("[AppEntryPoint] App Initialization Complete. Proceeding to destination...");
            
            // Check if this is a fresh new save
            if (_saveManager.CurrentData != null && _saveManager.CurrentData.PlayerName == "Mephisto" && _ascensionPanel != null)
            {
                Debug.Log("[AppEntryPoint] Fresh save detected. Triggering Ascension Sequence.");
                _ascensionPanel.Open();
                if (_homeUIManager != null) _homeUIManager.Close();
            }
            else
            {
                if (_homeUIManager != null) _homeUIManager.Open();
            }
        }
    }
}

