using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using MaouSamaTD.Data;
using MaouSamaTD.Managers;
using Zenject;
using System.Collections.Generic;
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
        [Inject] private GameSelectionState _gameSelectionState;
        
        // This is a static reference we can use universally since it will be loaded from Addressables
        private static UnitDatabase _loadedUnitDatabase;
        private static MaouSamaTD.Data.LevelDatabase _loadedLevelDatabase;
        private static MaouSamaTD.Units.ClassScalingData _loadedScalingData;

        public static UnitDatabase LoadedUnitDatabase 
        {
            get
            {
                if (_loadedUnitDatabase == null)
                {
                    Debug.Log("[AppEntryPoint] LoadedUnitDatabase accessed while null. Attempting sync load...");
                    var handle = Addressables.LoadAssetAsync<UnitDatabase>("UnitDatabase");
                    _loadedUnitDatabase = handle.WaitForCompletion();
                }
                return _loadedUnitDatabase;
            }
            private set => _loadedUnitDatabase = value;
        }

        public static MaouSamaTD.Data.LevelDatabase LoadedLevelDatabase 
        {
            get
            {
                if (_loadedLevelDatabase == null)
                {
                    Debug.Log("[AppEntryPoint] LoadedLevelDatabase accessed while null. Attempting sync load...");
                    try {
                        var handle = Addressables.LoadAssetAsync<MaouSamaTD.Data.LevelDatabase>("LevelDatabase");
                        _loadedLevelDatabase = handle.WaitForCompletion();
                    } catch (System.Exception e) {
                        Debug.LogWarning($"[AppEntryPoint] Addressables failed to load LevelDatabase: {e.Message}. Trying fallback path...");
                    }

                    if (_loadedLevelDatabase == null)
                    {
                        // Fallback to direct path for editor/dev stability
                        #if UNITY_EDITOR
                        _loadedLevelDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<MaouSamaTD.Data.LevelDatabase>("Assets/_Game/Data/Levels/LevelDatabase.asset");
                        #endif
                    }
                }
                return _loadedLevelDatabase;
            }
            private set => _loadedLevelDatabase = value;
        }

        public static MaouSamaTD.Units.ClassScalingData LoadedScalingData 
        {
            get
            {
                if (_loadedScalingData == null)
                {
                    Debug.Log("[AppEntryPoint] LoadedScalingData accessed while null. Attempting sync load...");
                    var handle = Addressables.LoadAssetAsync<MaouSamaTD.Units.ClassScalingData>("ClassScaleData");
                    _loadedScalingData = handle.WaitForCompletion();
                }
                return _loadedScalingData;
            }
            private set => _loadedScalingData = value;
        }

        [Header("Debug")]
        [SerializeField] private bool _grantDebugResources;

        private void OnValidate()
        {
            // Debug resources now handled in boot sequence to avoid being overridden by Load()
        }

        public void StartBootSequence(Action<float> onProgress, Action onComplete)
        {
            StartCoroutine(InitializeGameDataCoroutine(onProgress, onComplete));
        }

        private IEnumerator InitializeGameDataCoroutine(Action<float> onProgress, Action onComplete)
        {
            // If already initialized, we can skip the slow and potentially crash-prone Addressables loading entirely!
            if (LoadedUnitDatabase != null && LoadedLevelDatabase != null && LoadedScalingData != null)
            {
                Debug.Log("[AppEntryPoint] Game data already initialized. Skipping Addressables reload.");
                onProgress?.Invoke(1.0f);
                
                // Still load save data to make sure any updates or level clears are reflected
                if (_saveManager != null)
                {
                    _saveManager.Load();
                }
                
                onComplete?.Invoke();
                yield break;
            }

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

            Debug.Log("[AppEntryPoint] Loading all Levels by label 'LevelData'...");
            var levelHandle = Addressables.LoadAssetsAsync<MaouSamaTD.Levels.LevelData>("LevelData", null);
            while (!levelHandle.IsDone)
            {
                onProgress?.Invoke(0.5f + levelHandle.PercentComplete * 0.1f);
                yield return null;
            }

            if (levelHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var levels = new List<MaouSamaTD.Levels.LevelData>(levelHandle.Result);
                levels.Sort((a, b) => a.LevelIndex.CompareTo(b.LevelIndex));
                
                if (LoadedLevelDatabase == null)
                {
                    LoadedLevelDatabase = ScriptableObject.CreateInstance<MaouSamaTD.Data.LevelDatabase>();
                }
                LoadedLevelDatabase.AllLevels = levels;
                
                Debug.Log($"[AppEntryPoint] Successfully loaded {levels.Count} levels via Addressables label.");
            }
            else
            {
                Debug.LogError("[AppEntryPoint] Failed to load levels by label 'LevelData'. Make sure levels are labeled in Addressables!");
            }

            Debug.Log("[AppEntryPoint] Loading ClassScalingData from Addressables...");
            // As requested, use the label 'ClassScaleData' instead of the full path
            var scalingHandle = Addressables.LoadAssetAsync<MaouSamaTD.Units.ClassScalingData>("ClassScaleData");
            while (!scalingHandle.IsDone)
            {
            onProgress?.Invoke(0.6f + scalingHandle.PercentComplete * 0.3f);
                yield return null;
            }

            if (scalingHandle.Status == AsyncOperationStatus.Succeeded)
            {
                LoadedScalingData = scalingHandle.Result;
                Debug.Log($"[AppEntryPoint] Successfully loaded ClassScalingData.");

                // Trigger an initial refresh of all loaded unit data properties
                if (LoadedUnitDatabase != null && LoadedUnitDatabase.AllUnits != null)
                {
                    foreach (var unit in LoadedUnitDatabase.AllUnits)
                    {
                        if (unit != null)
                            unit.RefreshStats(LoadedScalingData);
                    }
                }
            }

            Debug.Log("[AppEntryPoint] Initializing Save Data...");
            if (_saveManager != null)
            {
                _saveManager.Load();
                
                if (_grantDebugResources)
                {
                    _saveManager.AddGold(10000);
                    _saveManager.AddBloodCrest(10000);
                    Debug.Log("<color=green>[DEBUG]</color> Granted 10,000 Gold and 10,000 Bloodcrest after Load.");
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

            if (_saveManager == null || _saveManager.CurrentData == null)
            {
                Debug.LogError("[AppEntryPoint] SaveManager or SaveData is null in ProceedToGame!");
                return;
            }

            bool isNewPlayer = _saveManager.CurrentData.PlayerName == "Mephisto";
            bool level1Done  = _saveManager.IsLevelCompleted("1-1");

            // ── Fresh account: show Ascension screen (Arise button will load BattleScene)
            if (isNewPlayer && _ascensionPanel != null)
            {
                Debug.Log("[AppEntryPoint] Fresh save – opening Ascension.");
                _ascensionPanel.Open();
                if (_homeUIManager != null) _homeUIManager.Close();
                return;
            }

            // ── Ascension done but Level 1 not yet cleared: go straight to Battle
            if (!level1Done)
            {
                Debug.Log("[AppEntryPoint] Level 1-1 not completed – routing to BattleScene.");
                
                if (_gameSelectionState != null && LoadedLevelDatabase != null)
                {
                    var lvl1 = LoadedLevelDatabase.GetLevelByID("1-1");
                    if (lvl1 != null)
                    {
                        _gameSelectionState.SetLevel(lvl1);
                        Debug.Log("[AppEntryPoint] Automatically selected Level 1-1 in GameSelectionState.");
                    }
                    else
                    {
                        Debug.LogError("[AppEntryPoint] Could not find Level 1-1 in LoadedLevelDatabase!");
                    }
                }

                var loader = UnityEngine.Object.FindFirstObjectByType<MaouSamaTD.UI.MainMenu.LoadingScreenPanel>(FindObjectsInactive.Include);
                if (loader != null)
                {
                    loader.LoadSceneTransition("BattleScene");
                }
                else
                {
                    Debug.LogWarning("[AppEntryPoint] LoadingScreenPanel missing – direct scene load.");
                    UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
                }
                return;
            }

            // ── Normal returning player: open Home hub
            if (_homeUIManager != null) _homeUIManager.Open();
        }
    }
}

