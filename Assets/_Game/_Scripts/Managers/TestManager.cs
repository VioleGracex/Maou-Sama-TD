using UnityEngine;
using MaouSamaTD.Levels;
using MaouSamaTD.Units;
using MaouSamaTD.Skills;
using MaouSamaTD.UI.Skills;
using System.Collections.Generic;
using NaughtyAttributes;
using Zenject;

namespace MaouSamaTD.Managers
{
    public class TestManager : MonoBehaviour
    {
        #region Fields
        [Header("Test Configuration")]
        [SerializeField] private LevelData _levelData;
        [SerializeField] private List<MaouSamaTD.Tutorial.TutorialDataSO> _testTutorials = new List<MaouSamaTD.Tutorial.TutorialDataSO>();
        [SerializeField] private int _tutorialIndex = 0;
        
        [Inject] private GameManager _gameManager;
        [Inject] private SkillManager _skillManager;
        [Inject] private CurrencyManager _currencyManager;
        [InjectOptional] private SkillPanelUI _skillPanelUI;
        #endregion

        #region Public API
        [Button("Ready All Skills")]
        public void ReadyAllSkills()
        {
            if (_skillManager != null) _skillManager.ResetAllCooldowns();
        }

        [Button("Maximize Seals (99)")]
        public void MaximizeSeals()
        {
            if (_currencyManager != null) _currencyManager.SetSeals(99);
        }

        [Button("Start Test Level")]
        public void StartTestLevel()
        {
            if (_gameManager == null)
            {
                Debug.LogError("[TestManager] GameManager not injected!");
                return;
            }

            if (_levelData == null)
            {
                Debug.LogWarning("[TestManager] No LevelData assigned for re-init!");
                return;
            }

            _gameManager.LoadLevelData(_levelData);
            Debug.Log($"[TestManager] Re-initialized Level: {_levelData.LevelName}");
        }

        [Button("Spawn Male Rites")]
        public void SpawnMaleRites()
        {
            if (_levelData == null) return;
            UpdateRites(_levelData.MaleSovereignRites);
            Debug.Log("[TestManager] Spawned Male Rites.");
        }

        [Button("Spawn Female Rites")]
        public void SpawnFemaleRites()
        {
            if (_levelData == null) return;
            UpdateRites(_levelData.FemaleSovereignRites);
            Debug.Log("[TestManager] Spawned Female Rites.");
        }

        [Button("Spawn Both Rites")]
        public void SpawnBothRites()
        {
            if (_levelData == null) return;
            
            List<SovereignRiteData> allRites = new List<SovereignRiteData>();
            if (_levelData.MaleSovereignRites != null) allRites.AddRange(_levelData.MaleSovereignRites);
            if (_levelData.FemaleSovereignRites != null) allRites.AddRange(_levelData.FemaleSovereignRites);
            
            UpdateRites(allRites);
            Debug.Log("[TestManager] Spawned Both Rites.");
        }

        [Button("Trigger Tutorial From Level")]
        public void TriggerTutorial()
        {
            if (_levelData == null || !_levelData.HasTutorial)
            {
                Debug.LogWarning("[TestManager] Current LevelData has no Tutorial.");
                return;
            }

            var tutorialManager = FindObjectOfType<TutorialManager>();
            if (tutorialManager != null)
            {
                tutorialManager.StartTutorial(_levelData.TutorialData);
                Debug.Log($"[TestManager] Triggered Tutorial for: {_levelData.LevelName}");
            }
        }

        [Button("Run Tutorial By Index")]
        public void RunTutorialByIndex()
        {
            if (_testTutorials == null || _tutorialIndex < 0 || _tutorialIndex >= _testTutorials.Count)
            {
                Debug.LogWarning("[TestManager] Invalid tutorial index or list is empty.");
                return;
            }

            var tutorialManager = FindObjectOfType<TutorialManager>();
            if (tutorialManager != null)
            {
                tutorialManager.StartTutorial(_testTutorials[_tutorialIndex]);
                Debug.Log($"[TestManager] Triggered Tutorial at Index: {_tutorialIndex}");
            }
        }
        #endregion

        #region Internal Logic
        private void UpdateRites(List<SovereignRiteData> rites)
        {
            if (_skillManager != null) _skillManager.Init(rites);
            if (_skillPanelUI != null) _skillPanelUI.Init(rites);
        }
        #endregion
    }
}
