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
        [Inject(Optional = true)] private TutorialManager _tutorialManager;
        [Inject] private StoryManager _storyManager;
        [Inject] private CameraManager _cameraManager;

        [SerializeField] private float _outroDelay = 2.0f;
        [Header("Debug")]
        [SerializeField] private bool _showDebugLogs = true;
        
        [Header("Containers")]
        [SerializeField] private Transform _enemyContainer;
        
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
        
        public int CurrentWaveRemainingEnemies => _waveEnemyCounts.ContainsKey(_currentWaveIndex) ? _waveEnemyCounts[_currentWaveIndex] : 0;
        public int CurrentWaveTotalEnemies => GetTotalEnemiesInWave(_currentWaveIndex);
        
        public event System.Action<string, int> OnWaveStarted;

        private Dictionary<int, int> _waveEnemyCounts = new Dictionary<int, int>();
        private Dictionary<int, int> _waveSpawnedCounts = new Dictionary<int, int>();
        private HashSet<int> _wavesThatStartedSpawning = new HashSet<int>();
        private HashSet<int> _wavesFinishedSpawning = new HashSet<int>();
        
        public bool IsWaveCleared(int waveIndex) => waveIndex < 0 || (_wavesFinishedSpawning.Contains(waveIndex) && (!_waveEnemyCounts.ContainsKey(waveIndex) || _waveEnemyCounts[waveIndex] <= 0));
        public bool HasWaveStarted(int waveIndex) => _wavesThatStartedSpawning.Contains(waveIndex);
        
        public int GetTotalEnemiesInWave(int waveIndex)
        {
            if (_waves == null || waveIndex < 0 || waveIndex >= _waves.Count) return 0;
            int total = 0;
            foreach (var group in _waves[waveIndex].Groups)
            {
                total += group.Count;
            }
            return total;
        }

        public int GetTotalSpawnedInWave(int waveIndex)
        {
            if (_waveSpawnedCounts.ContainsKey(waveIndex)) return _waveSpawnedCounts[waveIndex];
            return 0;
        }
        #endregion

        #region Lifecycle
        private void Awake()
        {
            // Container is now passed via Initialize or set in Inspector
        }

        private void Start()
        {
            EnemyUnit.OnAnyEnemyRemoved -= HandleEnemyRemoved;
            EnemyUnit.OnAnyEnemyRemoved += HandleEnemyRemoved;
        }

        private void Update()
        {
            if (!_isInitialized) return;

            if (!_victoryTriggered && _allWavesFinished && !_isSpawning)
            {
                bool allDead = true;
                bool hasAny = false;
                foreach (var enemy in EnemyUnit.ActiveEnemies)
                {
                    if (enemy != null)
                    {
                        hasAny = true;
                        if (!enemy.IsDead)
                        {
                            allDead = false;
                            break;
                        }
                    }
                }

                if ((hasAny && allDead) || (!hasAny && EnemyUnit.ActiveEnemies.Count == 0))
                {
                    StartCoroutine(TriggerLevelClearSequence());
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

            // Detect last enemy death for level clear polish
            if (_allWavesFinished && !_isSpawning && EnemyUnit.ActiveEnemies.Count == 0)
            {
                StartCoroutine(TriggerLevelClearSequence());
            }
        }

        private IEnumerator TriggerLevelClearSequence()
        {
            if (_victoryTriggered) yield break;
            _victoryTriggered = true;
            
            if (_showDebugLogs) Debug.Log("[EnemyManager] Last enemy defeated! Starting level clear sequence.");

            // 1. Initial Impact: 0.5x Slow Motion
            if (_gameManager != null)
            {
                _gameManager.SetSpeed(0.5f);
            }

            // 2. Camera Shake
            if (_cameraManager != null)
            {
                _cameraManager.Shake(1.2f, 0.4f);
            }
            // 3. Brief slow-mo duration
            yield return new WaitForSecondsRealtime(3.0f);

            // Wait for all active enemies to perish (complete death animations and get destroyed)
            yield return new WaitUntil(() => EnemyUnit.ActiveEnemies.Count == 0);

            // 4. Resume Time (as requested: "after killing boss resume time")
            if (_gameManager != null)
            {
                if (_showDebugLogs) Debug.Log("[EnemyManager] Resuming time to 1.0x for outro delay.");
                _gameManager.SetSpeed(1.0f);
            }

            // 5. Optional Outro Delay (default 2s)
            if (_outroDelay > 0)
            {
                if (_showDebugLogs) Debug.Log($"[EnemyManager] Waiting for outro delay: {_outroDelay}s");
                yield return new WaitForSeconds(_outroDelay);
            }

            // 6. Trigger Victory UI (banner, panel, etc)
            if (_gameManager != null)
            {
                _gameManager.Victory();
            }
        }
        #endregion

        #region Public API
        public void Initialize(List<WaveData> waves, Transform enemyContainer, float gracePeriod = 0f, bool startImmediately = true)
        {
            _waves = waves;
            _enemyContainer = enemyContainer;
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
                    yield return StartCoroutine(WaitScaled(group.InitialDelay));

                for (int i = 0; i < group.Count; i++)
                {
                    SpawnEnemy(group.EnemyType, waveIndex, enemyCounter, group.SpawnPointIndex);
                    _waveEnemyCounts[waveIndex]++;
                    enemyCounter++;
                    
                    if (group.SpawnInterval > 0)
                        yield return StartCoroutine(WaitScaled(group.SpawnInterval));
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
            Vector2Int exitPoint = _gridManager.GetTargetExitForSpawn(spawnPoint, data.MovementType);

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
            bool isFirstEnemyOfWave = _waveSpawnedCounts[waveIndex] == 0;
            _waveSpawnedCounts[waveIndex]++;

            if (isFirstEnemyOfWave && _pathVisualizer != null)
            {
                _pathVisualizer.HideWithMinimumDuration(2f);
            }
            
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
                yield return StartCoroutine(WaitScaled(initialDelay));
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

                // Pre-Wave Story
                if (wave.PreWaveStory != null && _storyManager != null)
                {
                    bool storyFinished = false;
                    _storyManager.PlayStory(wave.PreWaveStory, () => storyFinished = true);
                    yield return new WaitUntil(() => storyFinished);
                }

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

                // Post-Wave Story
                if (wave.PostWaveStory != null && _storyManager != null)
                {
                    bool storyFinished = false;
                    _storyManager.PlayStory(wave.PostWaveStory, () => storyFinished = true);
                    yield return new WaitUntil(() => storyFinished);
                }

                if (wave.DelayBeforeNextWave > 0)
                {
                    _isSpawning = false; // Not spawning during the delay
                    yield return StartCoroutine(WaitScaled(wave.DelayBeforeNextWave));
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
                yield return StartCoroutine(WaitScaled(group.InitialDelay));

            for (int i = 0; i < group.Count; i++)
            {
                if (!_isSpawning) yield break;
                
                SpawnEnemy(group.EnemyType, waveCounter, i, group.SpawnPointIndex);
                _waveEnemyCounts[waveCounter]++;
                
                if (group.SpawnInterval > 0)
                    yield return StartCoroutine(WaitScaled(group.SpawnInterval));
            }
        }

        private IEnumerator WaitScaled(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float speed = 1f;
                if (_gameManager != null)
                {
                    if (_gameManager.IsPaused || _gameManager.IsTutorialTimeStop || _gameManager.CurrentSpeed == 0f)
                    {
                        speed = 0f;
                    }
                    else
                    {
                        speed = _gameManager.CurrentSpeed;
                    }
                }
                elapsed += Time.unscaledDeltaTime * speed;
                yield return null;
            }
        }
        #endregion
    }
}
