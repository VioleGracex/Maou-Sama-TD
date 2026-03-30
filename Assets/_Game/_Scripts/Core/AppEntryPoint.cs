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
        public static MaouSamaTD.Units.ClassScalingData LoadedScalingData { get; private set; }

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
                onProgress?.Invoke(initHandle.PercentComplete * 0.1f);
                yield return null;
            }

            Debug.Log("[AppEntryPoint] Loading UnitDatabase from Addressables...");
            var dbHandle = Addressables.LoadAssetAsync<UnitDatabase>("UnitDatabase");
            while (!dbHandle.IsDone)
            {
                onProgress?.Invoke(0.1f + dbHandle.PercentComplete * 0.4f);
                yield return null;
            }

            if (dbHandle.Status == AsyncOperationStatus.Succeeded)
            {
                LoadedUnitDatabase = dbHandle.Result;
                Debug.Log($"[AppEntryPoint] Successfully loaded UnitDatabase. Units found: {LoadedUnitDatabase.AllUnits.Count}");
            }

            Debug.Log("[AppEntryPoint] Loading ClassScalingData from Addressables...");
            // Use the full path as seen in the Unity Editor screenshot to ensure the key matches
            var scalingHandle = Addressables.LoadAssetAsync<MaouSamaTD.Units.ClassScalingData>("Assets/_Game/Data/ClassScalingData.asset");
            while (!scalingHandle.IsDone)
            {
                onProgress?.Invoke(0.5f + scalingHandle.PercentComplete * 0.4f);
                yield return null;
            }

            if (scalingHandle.Status == AsyncOperationStatus.Succeeded)
            {
                LoadedScalingData = scalingHandle.Result;
                Debug.Log($"[AppEntryPoint] Successfully loaded ClassScalingData.");

                // Trigger an initial refresh of all loaded unit data properties
                if (LoadedUnitDatabase != null)
                {
                    foreach (var unit in LoadedUnitDatabase.AllUnits)
                        unit.RefreshStats(LoadedScalingData);
                }
            }

            Debug.Log("[AppEntryPoint] Initializing Save Data...");
            // SaveManager automatically loaded inside its constructor/Zenject Init, but we can double check here.
            if (_saveManager != null)
            {
                if (_saveManager.CurrentData == null)
                {
                    _saveManager.Load();
                }
            }
            else
            {
                Debug.LogWarning("[AppEntryPoint] SaveManager not injected! Check ProjectGlobalInstaller.");
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

