using UnityEngine;
using Zenject;
using MaouSamaTD.Managers;

namespace MaouSamaTD.Installers
{
    public class ProjectGlobalInstaller : MonoInstaller
    {
        [SerializeField] private SaveManager _saveManagerPrefab;
        [SerializeField] private SettingsManager _settingsManagerPrefab;
        [SerializeField] private EconomyManager _economyManagerPrefab;

        public override void InstallBindings()
        {
            if (_saveManagerPrefab == null)
            {
                Debug.LogError("[ProjectGlobalInstaller] SaveManager Prefab is NOT assigned in the Inspector!");
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
                 Container.Bind<EconomyManager>()
                    .FromNewComponentOnNewGameObject()
                    .AsSingle()
                    .NonLazy();
            }

            // Bind AudioSettingsManager from Hierarchy as per user request
            Container.Bind<AudioSettingsManager>()
                .FromComponentInHierarchy()
                .AsSingle()
                .NonLazy();

            // Bind GameSelectionState as a pure C# class singleton
            Container.Bind<GameSelectionState>()
                .AsSingle()
                .NonLazy();
                
            Debug.Log("[ProjectGlobalInstaller] Global Services Bound.");
        }
    }
}
