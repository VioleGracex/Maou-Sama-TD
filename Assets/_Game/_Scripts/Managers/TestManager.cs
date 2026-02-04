using UnityEngine;
using MaouSamaTD.Levels;
using MaouSamaTD.Units;
using System.Collections.Generic;
using NaughtyAttributes;
// using Zenject; // If needed later

namespace MaouSamaTD.Managers
{
    public class TestManager : MonoBehaviour
    {
        [Header("Test Configuration")]
        public EnemyData TestEnemyData;

        [Button("Start Test Level")]
        public void StartTestLevel()
        {
            var gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("TestManager: No GameManager found!");
                return;
            }

            if (TestEnemyData == null)
            {
                Debug.LogWarning("TestManager: No TestEnemyData assigned!");
                return;
            }

            // Create dummy MapData
            MapData testMap = ScriptableObject.CreateInstance<MapData>();
            testMap.Width = 10;
            testMap.Height = 10;
            testMap.MapSeed = 12345;
            testMap.SpawnPoints = new List<Vector2Int>{ new Vector2Int(0, 5) };
            testMap.ExitPoints = new List<Vector2Int>{ new Vector2Int(9, 5) };

            // Create dummy LevelData
            LevelData testLevel = ScriptableObject.CreateInstance<LevelData>();
            testLevel.LevelName = "Test Level";
            testLevel.GracePeriod = 2f; 
            testLevel.MapData = testMap; // Assign MapData
            testLevel.Waves = new List<WaveData>();

            WaveData wave = new WaveData();
            wave.WaveMessage = "Test Wave";
            wave.Groups = new List<WaveGroup>();

            WaveGroup group = new WaveGroup();
            group.EnemyType = TestEnemyData;
            group.Count = 5;
            group.SpawnInterval = 1f;

            wave.Groups.Add(group);
            testLevel.Waves.Add(wave);

            gameManager.LoadLevelData(testLevel);
            Debug.Log("TestManager: Started Test Level via GameManager.");
        }
    }
}
