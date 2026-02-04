using UnityEngine;
using Zenject;
using MaouSamaTD.Managers;
using MaouSamaTD.UI;
using MaouSamaTD.Grid;
using MaouSamaTD.Skills;

namespace MaouSamaTD.Installers
{
    public class GameInstaller : MonoInstaller
    {
        [Header("Scene References")]
        [SerializeField] private UnitInspectorUI _unitInspectorUI;
        [SerializeField] private DeploymentUI _deploymentUI;
        [SerializeField] private CameraControlUI _cameraControlUI;
        [SerializeField] private InteractionManager _interactionManager;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private CameraManager _cameraManager;
        [SerializeField] private SkillManager _skillManager;
        [SerializeField] private EnemyManager _enemyManager;

        public override void InstallBindings()
        {
            // Bind Instances from Scene
            if (_unitInspectorUI) Container.Bind<UnitInspectorUI>().FromInstance(_unitInspectorUI).AsSingle();
            if (_deploymentUI) Container.Bind<DeploymentUI>().FromInstance(_deploymentUI).AsSingle();
            if (_interactionManager) Container.Bind<InteractionManager>().FromInstance(_interactionManager).AsSingle();
            if (_currencyManager) Container.Bind<CurrencyManager>().FromInstance(_currencyManager).AsSingle();
            if (_gridManager) Container.Bind<GridManager>().FromInstance(_gridManager).AsSingle();
            if (_skillManager) Container.Bind<SkillManager>().FromInstance(_skillManager).AsSingle();
            
            if (_gameManager) 
            {
                Container.Bind<GameManager>().FromInstance(_gameManager).AsSingle();
                Container.QueueForInject(_gameManager); // Ensure it gets injected
            }
            
            
            if (_cameraManager) Container.Bind<CameraManager>().FromInstance(_cameraManager).AsSingle();
            if (_enemyManager) Container.Bind<EnemyManager>().FromInstance(_enemyManager).AsSingle();
            if (_cameraControlUI) Container.Bind<CameraControlUI>().FromInstance(_cameraControlUI).AsSingle();

            // Bind GridGenerator using hierarchy search since it's not explicitly referenced in various installers
            Container.Bind<GridGenerator>().FromComponentInHierarchy().AsSingle();
            
            // Bind PathVisualizer
            Container.Bind<MaouSamaTD.Utils.PathVisualizer>().FromNewComponentOnNewGameObject().AsSingle();
            
            // If they can be null and we want to find them automatically:
            // Container.Bind<UnitInspectorUI>().FromComponentInHierarchy().AsSingle();
            // But explicit references are usually safer/cleaner for MonoInstallers.
        }
    }
}
