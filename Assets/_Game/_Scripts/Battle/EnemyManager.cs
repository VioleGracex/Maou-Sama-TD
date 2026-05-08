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
        [Inject] private TutorialManager _tutorialManager;
        
        private Transform _enemyContainer;
        private bool _isSpawning = false;
        private bool _allWavesFinished = false;
        private bool _victoryTriggered = false;
        private bool _isInitialized = false;
        
        private List<WaveData> _waves;

        public bool IsSpawning => _isSpawning;
        public bool AllWavesFinished => _allWavesFinished;
        public int ActiveEnemyCount => EnemyUnit.ActiveEnemies.Count;
        
        private int _currentWaveIndex = 0;
        public int CurrentWaveIndex => _currentWaveIndex;
        public int TotalWaves => _waves != null ? _waves.Count : 0;
        
        public event System.Action<string, int> OnWaveStarted;

        private Dictionary<int, int> _waveEnemyCounts = new Dictionary<int, int>();
        private Dictionary<int, int> _waveSpawnedCounts = new Dictionary<int, int>();
        private HashSet<int> _wavesThatStartedSpawning = new HashSet<int>();
        private HashSet<int> _wavesFinishedSpawning = new HashSet<int>();
        
        public bool IsWaveCleared(int waveIndex) => waveIndex < 0 || (_wavesFinishedSpawning.Contains(waveIndex) && (!_waveEnemyCounts.ContainsKey(waveIndex) || _waveEnemyCounts[waveIndex] <= 0));
        public bool HasWaveStarted(int waveIndex) => _wavesThatStartedSpawning.Contains(waveIndex);
        
        public int GetTotalSpawnedInWave(int waveIndex)
        {
            if (_waveSpawnedCounts.ContainsKey(waveIndex)) return _waveSpawnedCounts[waveIndex];
            return 0;
        }
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
            EnemyUnit.OnAnyEnemyRemoved += HandleEnemyRemoved;
        }

        private void Update()
        {
            if (!_isInitialized) return;

            if (!_victoryTriggered && _allWavesFinished && !_isSpawning)
            {
                if (EnemyUnit.ActiveEnemies.Count == 0)
                {
                    // Check if tutorial is still in progress
                    if (_tutorialManager != null && _tutorialManager.IsInTutorial)
                    {
                        return;
                    }

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
            EnemyUnit.OnAnyEnemyRemoved -= HandleEnemyRemoved;
        }

        private void HandleEnemyRemoved(EnemyUnit enemy)
        {
            if (enemy == null) return;
            int waveIdx = enemy.WaveIndex;
            if (_waveEnemyCounts.ContainsKey(waveIdx))
            {
                _waveEnemyCounts[waveIdx]--;
                if (_waveEnemyCounts[waveIdx] < 0) _waveEnemyCounts[waveIdx] = 0;
            }
        }
        #endregion

        #region Public API
        public void Initialize(List<WaveData> waves, float gracePeriod = 0f, bool startImmediately = true)
        {
            _waves = waves;
            _allWavesFinished = false;
            _victoryTriggered = false;
            _isInitialized = true;

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
            
            _isSpawning = true; // Set immediately to avoid tutorial race conditions
            StopAllCoroutines();
            StartCoroutine(SpawnSingleWaveRoutine(_waves[waveIndex], waveIndex));
        }

        private IEnumerator SpawnSingleWaveRoutine(WaveData wave, int waveIndex)
        {
            _currentWaveIndex = waveIndex;
            _isSpawning = true;
            _wavesThatStartedSpawning.Add(waveIndex);
            if (!_waveEnemyCounts.ContainsKey(waveIndex)) _waveEnemyCounts[waveIndex] = 0;
            if (!_waveSpawnedCounts.ContainsKey(waveIndex)) _waveSpawnedCounts[waveIndex] = 0;
            OnWaveStarted?.Invoke(wave.WaveMessage, waveIndex);

            MaouSamaTD.Battle.BattleLogManager.Instance.LogEvent(MaouSamaTD.Battle.BattleLogType.WaveStart, "Director", "", $"Wave {waveIndex + 1} Started", 0);
            
            if (!string.IsNullOrEmpty(wave.WaveMessage))
            {
                Debug.Log($"[EnemyManager] Tutorial Wave: {wave.WaveMessage}");
            }
            
            int enemyCounter = 0;
            foreach (var group in wave.Groups)
            {
                if (group.InitialDelay > 0)
                    yield return new WaitForSeconds(group.InitialDelay);

                for (int i = 0; i < group.Count; i++)
                {
                    if (enemyCounter == 0 && _pathVisualizer != null) _pathVisualizer.Hide();
                    SpawnEnemy(group.EnemyType, waveIndex, enemyCounter, group.SpawnPointIndex);
                    _waveEnemyCounts[waveIndex]++;
                    enemyCounter++;
                    
                    if (group.SpawnInterval > 0)
                        yield return new WaitForSeconds(group.SpawnInterval);
                }
            }
            _isSpawning = false;
            _wavesFinishedSpawning.Add(waveIndex);

            // Tutorial Trigger: This wave has finished its spawning sequence
            if (_tutorialManager != null)
            {
                _tutorialManager.OnActionTriggered("WaveFinishedSpawning");
            }
        }


        public void SpawnEnemy(EnemyData data, int waveIndex, int enemyIndex, int spawnPointIndex = 0)
        {
            if (_gridManager == null || _enemyPrefab == null || data == null) return;
            
            if (_gridManager.SpawnPoints == null || _gridManager.SpawnPoints.Count == 0)
            {
                Debug.LogError("[EnemyManager] No spawn points registered in GridManager!");
                return;
            }
            
            if (spawnPointIndex < 0 || spawnPointIndex >= _gridManager.SpawnPoints.Count)
            {
                Debug.LogWarning($"[EnemyManager] SpawnPointIndex {spawnPointIndex} is out of bounds (Count: {_gridManager.SpawnPoints.Count}). Defaulting to 0.");
                spawnPointIndex = 0;
            }

            Vector2Int spawnPoint = _gridManager.SpawnPoints[spawnPointIndex].Coordinate;
            
            // 1. Validate Spawning Constraints & Get Correct Exit
            Tile spawnTile = _gridManager.GetTileAt(spawnPoint);
            Vector2Int exitPoint = _gridManager.GetTargetExitForSpawn(spawnPoint);

            if (spawnTile != null)
            {
                // Ground units must spawn on Ground tiles (Walkable, SpawnPoint, etc.)
                bool isHighGroundTile = spawnTile.Type == TileType.HighGround || 
                                       spawnTile.Type == TileType.DecoHighGround || 
                                       spawnTile.Type == TileType.SpawnPointHigh || 
                                       spawnTile.Type == TileType.ExitPointHigh;

                if (data.MovementType == MaouSamaTD.Units.EnemyMovementType.Ground)
                {
                    if (isHighGroundTile)
                    {
                        Debug.LogWarning($"[EnemyManager] Cannot spawn Ground enemy '{data.EnemyName}' at {spawnPoint} (High Ground Tile: {spawnTile.Type}). Skipping.");
                        return;
                    }
                }
            }

            // High Ground Logic for Flying/Mixed units: If unit is flying, force it to target the CLOSEST high ground exit if one exists
            if (data.MovementType == MaouSamaTD.Units.EnemyMovementType.Flying || data.MovementType == MaouSamaTD.Units.EnemyMovementType.Mixed)
            {
                 float minDistance = float.MaxValue;
                 Vector2Int closestHighExit = exitPoint;
                 bool foundHighExit = false;

                 foreach(var ep in _gridManager.ExitPoints)
                 {
                     var et = _gridManager.GetTileAt(ep);
                     if (et != null && (et.Type == TileType.ExitPointHigh || et.IsHighGround))
                     {
                         float dist = Vector2.Distance(new Vector2(spawnPoint.x, spawnPoint.y), new Vector2(ep.x, ep.y));
                         if (dist < minDistance)
                         {
                             minDistance = dist;
                             closestHighExit = ep;
                             foundHighExit = true;
                         }
                     }
                 }
                 if (foundHighExit) exitPoint = closestHighExit;
            }

            // 2. Get Path
            Queue<Tile> path = _gridManager.GetPath(spawnPoint, exitPoint, data.MovementType, false);
            
            // Fallback: If blocked, path ignoring occupants (so they spawn and fight)
            if (path == null || path.Count == 0)
            {
                Debug.Log($"[EnemyManager] Spawn Path Blocked for {data.EnemyName} from {spawnPoint} to {exitPoint}! Attempting fallback...");
                path = _gridManager.GetPath(spawnPoint, exitPoint, data.MovementType, true);
            }

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning($"[EnemyManager] No path found for {data.EnemyName} even ignoring occupants! Skipping spawn.");
                return;
            }

            // 3. Instantiate
            Vector3 startPos = _gridManager.GridToWorldPosition(spawnPoint);
            MaouSamaTD.Units.EnemyUnit enemy = Instantiate(_enemyPrefab, startPos, Quaternion.identity, _enemyContainer);
            
            if (!_waveSpawnedCounts.ContainsKey(waveIndex)) _waveSpawnedCounts[waveIndex] = 0;
            _waveSpawnedCounts[waveIndex]++;
            
            // 4. Initialize
            enemy.gameObject.SetActive(true);
            enemy.Initialize(data, waveIndex, enemyIndex);
            enemy.GoalCoord = exitPoint;
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

            int waveCounter = 0;
            foreach (var wave in _waves)
            {
                if (!_isSpawning) yield break;
                
                _currentWaveIndex = waveCounter;

                if (!string.IsNullOrEmpty(wave.WaveMessage))
                {
                    Debug.Log($"[EnemyManager] Starting Wave: {wave.WaveMessage}");
                }
                
                // Show Paths for this wave
                _pathVisualizer?.ShowPathsForWave(wave);

                List<Coroutine> groupRoutines = new List<Coroutine>();
                _wavesThatStartedSpawning.Add(waveCounter);
                if (!_waveEnemyCounts.ContainsKey(waveCounter)) _waveEnemyCounts[waveCounter] = 0;
                if (!_waveSpawnedCounts.ContainsKey(waveCounter)) _waveSpawnedCounts[waveCounter] = 0;
                
                OnWaveStarted?.Invoke(wave.WaveMessage, waveCounter);
                _tutorialManager?.OnActionTriggered("WaveStarted");

                foreach (var group in wave.Groups)
                {
                    if (!_isSpawning) yield break;
                    groupRoutines.Add(StartCoroutine(SpawnGroupRoutine(group, waveCounter)));
                }

                // Wait for all groups in this wave to finish their spawning sequence
                foreach (var routine in groupRoutines)
                {
                    yield return routine;
                }

                // Tutorial Trigger: This specific wave (waveCounter) has finished its spawning sequence
                _wavesFinishedSpawning.Add(waveCounter);
                if (_tutorialManager != null)
                {
                    _tutorialManager.OnActionTriggered("WaveFinishedSpawning");
                }

                // WAIT: Wait for the entire wave to be cleared (all enemies defeated/escaped) before proceeding to next wave delay
                yield return new WaitUntil(() => IsWaveCleared(waveCounter));

                if (wave.DelayBeforeNextWave > 0)
                {
                    _isSpawning = false; // Not spawning during the delay
                    yield return new WaitForSeconds(wave.DelayBeforeNextWave);
                    _isSpawning = true;
                }
                
                waveCounter++;
            }

            _isSpawning = false;
            _allWavesFinished = true;
            Debug.Log("[EnemyManager] All waves finished.");
        }

        private IEnumerator SpawnGroupRoutine(WaveGroup group, int waveCounter)
        {
            if (group.InitialDelay > 0)
                yield return new WaitForSeconds(group.InitialDelay);

            for (int i = 0; i < group.Count; i++)
            {
                if (!_isSpawning) yield break;

                // On the very first enemy of the level, hide the visualizer if it was still showing
                if (waveCounter == 0 && i == 0 && _pathVisualizer != null) _pathVisualizer.Hide();
                
                SpawnEnemy(group.EnemyType, waveCounter, i, group.SpawnPointIndex);
                _waveEnemyCounts[waveCounter]++;
                
                if (group.SpawnInterval > 0)
                    yield return new WaitForSeconds(group.SpawnInterval);
            }
        }
        #endregion
    }
}
