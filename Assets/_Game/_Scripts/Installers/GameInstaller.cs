using UnityEngine;
using Zenject;
using MaouSamaTD.Managers;
using MaouSamaTD.UI;
using MaouSamaTD.Grid;

namespace MaouSamaTD.Installers
{
    public class GameInstaller : MonoInstaller
    {
        [Header("Scene References")]
        [SerializeField] private UnitInspectorUI _unitInspectorUI;
        [SerializeField] private DeploymentUI _deploymentUI;
        [SerializeField] private InteractionManager _interactionManager;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private GridManager _gridManager;

        public override void InstallBindings()
        {
            // Bind Instances from Scene
            if (_unitInspectorUI) Container.Bind<UnitInspectorUI>().FromInstance(_unitInspectorUI).AsSingle();
            if (_deploymentUI) Container.Bind<DeploymentUI>().FromInstance(_deploymentUI).AsSingle();
            if (_interactionManager) Container.Bind<InteractionManager>().FromInstance(_interactionManager).AsSingle();
            if (_currencyManager) Container.Bind<CurrencyManager>().FromInstance(_currencyManager).AsSingle();
            if (_gridManager) Container.Bind<GridManager>().FromInstance(_gridManager).AsSingle();
            
            // If they can be null and we want to find them automatically:
            // Container.Bind<UnitInspectorUI>().FromComponentInHierarchy().AsSingle();
            // But explicit references are usually safer/cleaner for MonoInstallers.
        }
    }
}
