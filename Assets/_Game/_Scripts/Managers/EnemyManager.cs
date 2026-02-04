using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MaouSamaTD.Grid;
using Zenject;
using NaughtyAttributes;
using MaouSamaTD.Levels;
using MaouSamaTD.Units;

namespace MaouSamaTD.Managers
{
    public class EnemyManager : MonoBehaviour
    {
        [Header("Settings")]
        // [SerializeField] private EnemyData _enemyData; // Legacy single data
        
        [Header("References")]
        [SerializeField] private MaouSamaTD.Units.EnemyUnit _enemyPrefab;
        [Inject] private GameManager _gameManager; 
        [Inject] private Grid.GridManager _gridManager;
        [Inject] private MaouSamaTD.Utils.PathVisualizer _pathVisualizer;
        
        private Transform _enemyContainer;
        private bool _isSpawning = false;
        private bool _allWavesFinished = false;
        private bool _victoryTriggered = false;
        private List<WaveData> _waves;

        private void Start()
        {
            if (_enemyContainer == null)
            {
                var container = GameObject.Find("Enemies");
                if (container == null) container = new GameObject("Enemies");
                _enemyContainer = container.transform;
            }
        }

        private void Update()
        {
            if (!_victoryTriggered && _allWavesFinished)
            {
                if (EnemyUnit.ActiveEnemies.Count == 0)
                {
                    _victoryTriggered = true;
                    Debug.Log("EnemyManager: All enemies defeated. Victory!");
                    if (_gameManager != null)
                    {
                        _gameManager.Victory();
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_gridManager != null)
            {
                _gridManager.OnGridStateChanged -= OnGridChanged;
            }
        }

        public void Initialize(List<WaveData> waves, float gracePeriod = 0f)
        {
            _waves = waves;
            _allWavesFinished = false;
            _victoryTriggered = false;

            if (_gridManager != null)
            {
                _gridManager.OnGridStateChanged -= OnGridChanged; // Safety unsubscribe
                _gridManager.OnGridStateChanged += OnGridChanged;
            }
            
            if (_waves != null && _waves.Count > 0)
            {
                SetSpawnState(true, gracePeriod);
            }
            else
            {
                 Debug.LogWarning("EnemySpawner initialized with empty waves.");
                 // If no waves, is it instant victory? Let's assume yes or wait.
                 // For safety, if empty waves, maybe just trigger finish?
                 _allWavesFinished = true;
            }
        }
        
        private void OnGridChanged()
        {
            // Grid changed (Unit placed or died). Recalculate paths for active enemies.
            // Using Static list from EnemyUnit to access all.
            // Create a copy to handle modification safety.
            var enemies = new List<EnemyUnit>(EnemyUnit.ActiveEnemies);
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    enemy.RecalculatePath();
                }
            }
        }

        private IEnumerator SpawnRoutine(float initialDelay)
        {
            if (initialDelay > 0)
            {
                Debug.Log($"EnemySpawner: Waiting for Grace Period: {initialDelay}s");
                if (_pathVisualizer != null) _pathVisualizer.Show(); // Show path during wait
                yield return new WaitForSeconds(initialDelay);
            }

            if (_pathVisualizer != null) _pathVisualizer.Hide(); // Hide when action starts
            _isSpawning = true;
            
            if (_waves == null) yield break;

            foreach (var wave in _waves)
            {
                if (!_isSpawning) yield break;

                if (!string.IsNullOrEmpty(wave.WaveMessage))
                {
                    Debug.Log($"Starting Wave: {wave.WaveMessage}");
                }
                
                foreach (var group in wave.Groups)
                {
                    if (!_isSpawning) yield break;
                    
                    if (group.InitialDelay > 0)
                        yield return new WaitForSeconds(group.InitialDelay);

                    for (int i = 0; i < group.Count; i++)
                    {
                        if (!_isSpawning) yield break;

                        SpawnEnemy(group.EnemyType, group.SpawnPointIndex);
                        
                        if (group.SpawnInterval > 0)
                            yield return new WaitForSeconds(group.SpawnInterval);
                    }
                }

                if (wave.DelayBeforeNextWave > 0)
                    yield return new WaitForSeconds(wave.DelayBeforeNextWave);
            }
            
            _isSpawning = false;
            _allWavesFinished = true; 
            Debug.Log("EnemySpawner: All waves finished.");
        }

        public void SpawnEnemy(EnemyData data, int spawnPointIndex = 0)
        {
            if (_gridManager == null || _enemyPrefab == null || data == null) return;

            // 1. Get Path (Normal)
            Queue<Tile> path = _gridManager.GetPath(_gridManager.SpawnPoint, _gridManager.ExitPoint, data.MovementType, false);
            
            // Fallback: If blocked, path ignoring occupants (so they spawn and fight)
            if (path == null || path.Count == 0)
            {
                Debug.Log("Spawn Path Blocked! Attempting fallback (Ignore Occupants)...");
                path = _gridManager.GetPath(_gridManager.SpawnPoint, _gridManager.ExitPoint, data.MovementType, true);
            }

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning("EnemySpawner: No path found even ignoring occupants!");
                return;
            }

            // 2. Instantiate
            Vector3 startPos = _gridManager.GridToWorldPosition(_gridManager.SpawnPoint);
            
            MaouSamaTD.Units.EnemyUnit enemy = Instantiate(_enemyPrefab, startPos, Quaternion.identity, _enemyContainer);
            
            // 3. Initialize
            enemy.gameObject.SetActive(true); // Ensure active
            enemy.Initialize(data);
            enemy.SetPath(path);
        }

        public void SetSpawnState(bool active, float initialDelay = 0f)
        {
            _isSpawning = active;
            StopAllCoroutines();
            if (active) 
            {
                StartCoroutine(SpawnRoutine(initialDelay));
            }
        }
    }
}
