using UnityEngine;
using Zenject;
using MaouSamaTD.Managers;

namespace MaouSamaTD.Installers
{
    public class ProjectGlobalInstaller : MonoInstaller
    {
        [SerializeField] private SaveManager _saveManagerPrefab;

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

            // Bind GameSelectionState as a pure C# class singleton
            Container.Bind<GameSelectionState>()
                .AsSingle()
                .NonLazy();
                
            Debug.Log("[ProjectGlobalInstaller] Global Services Bound.");
        }
    }
}
