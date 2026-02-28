using UnityEngine;
using Zenject;
using MaouSamaTD.Managers;
using MaouSamaTD.UI;
using MaouSamaTD.Grid;
using MaouSamaTD.Skills;
using MaouSamaTD.UI.Tutorial;

namespace MaouSamaTD.Installers
{
    public class GameInstaller : MonoInstaller
    {
        [Header("Scene References")]
        [SerializeField] private UnitInspectorUI _unitInspectorUI;
        [SerializeField] private DeploymentUI _deploymentUI;
        [SerializeField] private CameraControlUI _cameraControlUI;
        [SerializeField] private MaouSamaTD.UI.Skills.SkillPanelUI _skillPanelUI;
        [SerializeField] private InteractionManager _interactionManager;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private CameraManager _cameraManager;
        [SerializeField] private SkillManager _skillManager;
        [SerializeField] private EnemyManager _enemyManager;

        [Header("Tutorial & Dialogue")]
        [SerializeField] private DialogueUI _dialogueUI;
        [SerializeField] private MaouSamaTD.UI.Tutorial.TutorialHandUI _tutorialHandUI;
        [SerializeField] private TutorialManager _tutorialManager;
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private UIPopupBlocker _uiPopupBlocker;

        public override void InstallBindings()
        {
            // ... existing bindings ...
            
            if (_unitInspectorUI) Container.Bind<UnitInspectorUI>().FromInstance(_unitInspectorUI).AsSingle();
            if (_deploymentUI) Container.Bind<DeploymentUI>().FromInstance(_deploymentUI).AsSingle();
            if (_skillPanelUI) Container.Bind<MaouSamaTD.UI.Skills.SkillPanelUI>().FromInstance(_skillPanelUI).AsSingle();
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

            // Tutorial & Dialogue
            if (_uiPopupBlocker) Container.Bind<UIPopupBlocker>().FromInstance(_uiPopupBlocker).AsSingle();
            else Container.Bind<UIPopupBlocker>().FromComponentInHierarchy().AsSingle();

            if (_dialogueUI) Container.Bind<DialogueUI>().FromInstance(_dialogueUI).AsSingle();
            else Container.Bind<DialogueUI>().FromComponentInHierarchy().AsSingle();

            if (_tutorialHandUI) Container.Bind<MaouSamaTD.UI.Tutorial.TutorialHandUI>().FromInstance(_tutorialHandUI).AsSingle();
            
            if (_dialogueManager) Container.Bind<DialogueManager>().FromInstance(_dialogueManager).AsSingle();
            else Container.Bind<DialogueManager>().FromComponentInHierarchy().AsSingle();

            if (_tutorialManager) Container.Bind<TutorialManager>().FromInstance(_tutorialManager).AsSingle();
            else Container.Bind<TutorialManager>().FromComponentInHierarchy().AsSingle();

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
