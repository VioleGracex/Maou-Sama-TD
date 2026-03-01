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
        #region Fields
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

        public bool IsSpawning => _isSpawning;
        public bool AllWavesFinished => _allWavesFinished;
        public int ActiveEnemyCount => EnemyUnit.ActiveEnemies.Count;
        #endregion

        #region Lifecycle
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
                    Debug.Log("[EnemyManager] All enemies defeated. Victory!");
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
        #endregion

        #region Public API
        public void Initialize(List<WaveData> waves, float gracePeriod = 0f, bool startImmediately = true)
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
                if (startImmediately)
                    SetSpawnState(true, gracePeriod);
            }
            else
            {
                 Debug.LogWarning("[EnemyManager] EnemySpawner initialized with empty waves.");
                 _allWavesFinished = true;
            }
        }

        public void StartSpecificWave(int waveIndex)
        {
            if (_waves == null || waveIndex < 0 || waveIndex >= _waves.Count) return;
            
            StopAllCoroutines();
            StartCoroutine(SpawnSingleWaveRoutine(_waves[waveIndex]));
        }

        private IEnumerator SpawnSingleWaveRoutine(WaveData wave)
        {
            _isSpawning = true;
            if (!string.IsNullOrEmpty(wave.WaveMessage))
            {
                Debug.Log($"[EnemyManager] Tutorial Wave: {wave.WaveMessage}");
            }
            
            foreach (var group in wave.Groups)
            {
                if (group.InitialDelay > 0)
                    yield return new WaitForSeconds(group.InitialDelay);

                for (int i = 0; i < group.Count; i++)
                {
                    SpawnEnemy(group.EnemyType, group.SpawnPointIndex);
                    
                    if (group.SpawnInterval > 0)
                        yield return new WaitForSeconds(group.SpawnInterval);
                }
            }
            _isSpawning = false;
        }


        public void SpawnEnemy(EnemyData data, int spawnPointIndex = 0)
        {
            if (_gridManager == null || _enemyPrefab == null || data == null) return;

            // 1. Get Path (Normal)
            Queue<Tile> path = _gridManager.GetPath(_gridManager.SpawnPoint, _gridManager.ExitPoint, data.MovementType, false);
            
            // Fallback: If blocked, path ignoring occupants (so they spawn and fight)
            if (path == null || path.Count == 0)
            {
                Debug.Log("[EnemyManager] Spawn Path Blocked! Attempting fallback (Ignore Occupants)...");
                path = _gridManager.GetPath(_gridManager.SpawnPoint, _gridManager.ExitPoint, data.MovementType, true);
            }

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning("[EnemyManager] No path found even ignoring occupants!");
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
        #endregion

        #region Internal Logic
        private void OnGridChanged()
        {
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
                Debug.Log($"[EnemyManager] Waiting for Grace Period: {initialDelay}s");
                if (_pathVisualizer != null) _pathVisualizer.Show(); 
                yield return new WaitForSeconds(initialDelay);
            }

            if (_pathVisualizer != null) _pathVisualizer.Hide(); 
            _isSpawning = true;
            
            if (_waves == null) yield break;

            foreach (var wave in _waves)
            {
                if (!_isSpawning) yield break;

                if (!string.IsNullOrEmpty(wave.WaveMessage))
                {
                    Debug.Log($"[EnemyManager] Starting Wave: {wave.WaveMessage}");
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
            Debug.Log("[EnemyManager] All waves finished.");
        }
        #endregion
    }
}
