using UnityEngine;
using Zenject;
using MaouSamaTD.Managers;
using MaouSamaTD.Data;
using MaouSamaTD.Mandates;


namespace MaouSamaTD.Installers
{
    public class ProjectGlobalInstaller : MonoInstaller
    {
        [SerializeField] private SaveManager _saveManagerPrefab;
        [SerializeField] private SettingsManager _settingsManagerPrefab;
        [SerializeField] private EconomyManager _economyManagerPrefab;
        [SerializeField] private AudioSettingsManager _audioSettingsManagerPrefab;
        [SerializeField] private MandateManager _mandateManagerPrefab;
        [SerializeField] private UnitDatabase _unitDatabase;


        public override void InstallBindings()
        {
            if (_saveManagerPrefab == null)
            {
                Debug.LogError("[ProjectGlobalInstaller] SaveManager Prefab is NOT assigned in the Inspector!");
            }

            // Bind UnitDatabase if assigned
            if (_unitDatabase != null)
            {
                Container.Bind<UnitDatabase>()
                    .FromInstance(_unitDatabase)
                    .AsSingle();
            }
            else
            {
                Debug.LogError("[ProjectGlobalInstaller] UnitDatabase is NOT assigned in the Inspector! GachaManager will fail.");
            }

            // Bind SaveManager from the assigned prefab
            Container.Bind<SaveManager>()
                .FromComponentInNewPrefab(_saveManagerPrefab)
                .AsSingle()
                .NonLazy();

            // Bind SettingsManager
            if (_settingsManagerPrefab != null)
            {
                Container.Bind<SettingsManager>()
                    .FromComponentInNewPrefab(_settingsManagerPrefab)
                    .AsSingle()
                    .NonLazy();
            }
            else
            {
                Container.Bind<SettingsManager>()
                    .FromNewComponentOnNewGameObject()
                    .AsSingle()
                    .NonLazy();
            }

            // Bind EconomyManager
            if (_economyManagerPrefab != null)
            {
                Container.Bind<EconomyManager>()
                    .FromComponentInNewPrefab(_economyManagerPrefab)
                    .AsSingle()
                    .NonLazy();
            }
            else
            {
                Debug.LogWarning("[ProjectGlobalInstaller] EconomyManager Prefab missing! Creating empty instance to prevent crash.");
                 Container.Bind<EconomyManager>()
                    .FromNewComponentOnNewGameObject()
                    .AsSingle()
                    .NonLazy();
            }

            // Bind AudioSettingsManager
            if (_audioSettingsManagerPrefab != null)
            {
                Container.Bind<AudioSettingsManager>()
                    .FromComponentInNewPrefab(_audioSettingsManagerPrefab)
                    .AsSingle()
                    .NonLazy();
            }
            else
            {
                Debug.LogWarning("[ProjectGlobalInstaller] AudioSettingsManager Prefab missing! Creating empty instance to prevent crash.");
                Container.Bind<AudioSettingsManager>()
                    .FromNewComponentOnNewGameObject()
                    .AsSingle()
                    .NonLazy();
            }

            // Bind GameSelectionState as a pure C# class singleton
            Container.Bind<GameSelectionState>()
                .AsSingle()
                .NonLazy();

            // Bind MandateManager
            if (_mandateManagerPrefab != null)
            {
                Container.Bind<MandateManager>()
                    .FromComponentInNewPrefab(_mandateManagerPrefab)
                    .AsSingle()
                    .NonLazy();
            }
            else
            {
                Debug.LogWarning("[ProjectGlobalInstaller] MandateManager Prefab missing! Creating empty instance to prevent crash.");
                Container.Bind<MandateManager>()
                    .FromNewComponentOnNewGameObject()
                    .AsSingle()
                    .NonLazy();
            }


                
            Debug.Log("[ProjectGlobalInstaller] Global Services Bound.");
        }
    }
}
