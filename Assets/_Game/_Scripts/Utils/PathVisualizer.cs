using UnityEngine;
using Zenject;
using System.Collections.Generic;
using MaouSamaTD.Grid;
using MaouSamaTD.Managers;
using MaouSamaTD.Units;
using MaouSamaTD.Levels;

namespace MaouSamaTD.Utils
{
    [RequireComponent(typeof(LineRenderer))]
    public class PathVisualizer : MonoBehaviour
    {
        [Inject] private GridManager _gridManager;

        [SerializeField] private Material _sourceMaterial;
        [SerializeField] private LineRenderer _lineRendererPrefab;

        private List<LineRenderer> _activeLines = new List<LineRenderer>();
        private List<LineRenderer> _linePool = new List<LineRenderer>();
        private Material _materialInstance;
        private Coroutine _fadeRoutine;
        private float _currentAlpha = 0f;

        public void Init(Material overrideMaterial = null)
        {
            if (overrideMaterial != null) _sourceMaterial = overrideMaterial;
            ConfigureMaterial(); 
            SetAlpha(0f);
        }

        private void ConfigureMaterial()
        {
            if (_sourceMaterial != null)
            {
                _materialInstance = new Material(_sourceMaterial);
            }
            else
            {
                Shader shader = Shader.Find("Mobile/Particles/Additive");
                if (shader == null) shader = Shader.Find("Particles/Additive");
                if (shader == null) shader = Shader.Find("Sprites/Default"); 
                _materialInstance = new Material(shader);
            }

            _materialInstance.mainTexture = GenerateArrowTexture();
        }

        private Texture2D GenerateArrowTexture()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Color clear = new Color(0, 0, 0, 0); 
            Color white = new Color(1, 1, 1, 1);

            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float distFromCenter = Mathf.Abs(v - 0.5f);
                    float arrowEdgeX = 55 - (distFromCenter * 60); 
                    
                    if (x < arrowEdgeX && x > arrowEdgeX - 15)
                    {
                        pixels[y * size + x] = white;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        public void ShowPathsForWave(WaveData wave)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            foreach (var lr in _activeLines)
            {
                lr.enabled = false;
                _linePool.Add(lr);
            }
            _activeLines.Clear();
            _currentAlpha = 0f;
            SetAlpha(0f);
            
            if (wave == null || wave.Groups == null) return;

            HashSet<(int index, EnemyMovementType moveType)> pathRequests = new HashSet<(int, EnemyMovementType)>();
            foreach (var group in wave.Groups)
            {
                if (group.EnemyType != null)
                {
                    pathRequests.Add((group.SpawnPointIndex, group.EnemyType.MovementType));
                }
                else
                {
                    pathRequests.Add((group.SpawnPointIndex, EnemyMovementType.Ground));
                }
            }

            foreach (var req in pathRequests)
            {
                if (req.index < 0 || req.index >= _gridManager.SpawnPoints.Count) continue;
                Vector2Int spawn = _gridManager.SpawnPoints[req.index].Coordinate;
                Vector2Int exit = _gridManager.GetTargetExitForSpawn(spawn, req.moveType);
                
                Queue<Tile> path = _gridManager.GetPath(spawn, exit, req.moveType);
                if (path != null && path.Count > 0)
                {
                    CreatePathLine(spawn, path, req.moveType);
                }
            }
            Show();
        }

        private void CreatePathLine(Vector2Int start, Queue<Tile> path, EnemyMovementType moveType)
        {
            LineRenderer lr = GetLineRenderer();
            
            float visualHeight = 0.7f;
            List<Vector3> points = new List<Vector3>();
            points.Add(_gridManager.GridToWorldPosition(start) + Vector3.up * visualHeight);

            foreach (var tile in path)
            {
                points.Add(tile.transform.position + Vector3.up * visualHeight);
            }

            lr.positionCount = points.Count;
            lr.SetPositions(points.ToArray());
            
            // Distinguish colors if multiple types exist
            if (moveType == EnemyMovementType.Flying)
            {
                lr.startColor = new Color(0.5f, 0f, 1f, _currentAlpha); // Purple for flyers
                lr.endColor = new Color(1f, 0f, 0.5f, _currentAlpha);
            }
            else
            {
                lr.startColor = new Color(1f, 0.5f, 0f, _currentAlpha); // Orange for ground
                lr.endColor = new Color(0f, 1f, 0.5f, _currentAlpha);
            }

            lr.enabled = true;
            _activeLines.Add(lr);
        }

        private LineRenderer GetLineRenderer()
        {
            LineRenderer lr;
            if (_linePool.Count > 0)
            {
                lr = _linePool[0];
                _linePool.RemoveAt(0);
            }
            else
            {
                if (_lineRendererPrefab != null)
                {
                    lr = Instantiate(_lineRendererPrefab, transform);
                }
                else
                {
                    GameObject go = new GameObject("PathLine");
                    go.transform.SetParent(transform);
                    lr = go.AddComponent<LineRenderer>();
                    lr.startWidth = 0.5f;
                    lr.endWidth = 0.5f;
                    lr.useWorldSpace = true;
                    lr.textureMode = LineTextureMode.RepeatPerSegment;
                }
            }
            lr.material = _materialInstance;
            return lr;
        }

        private float _shownTime;

        public void Show()
        {
            _shownTime = Time.time;
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeRoutine(1f, 1.0f));
            
            CancelInvoke("Hide");
            Invoke("Hide", 6f);
        }

        public void HideWithMinimumDuration(float minDuration = 2f)
        {
            float elapsed = Time.time - _shownTime;
            if (elapsed < minDuration)
            {
                float remaining = minDuration - elapsed;
                CancelInvoke("Hide");
                Invoke("Hide", remaining);
            }
            else
            {
                Hide();
            }
        }

        public void Hide()
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeRoutine(0f, 0.5f));
        }

        private System.Collections.IEnumerator FadeRoutine(float targetAlpha, float duration)
        {
            float startAlpha = _currentAlpha;
            float time = 0;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;
                _currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                SetAlpha(_currentAlpha);
                yield return null;
            }
            
            _currentAlpha = targetAlpha;
            SetAlpha(_currentAlpha);

            if (_currentAlpha <= 0.01f)
            {
                foreach (var lr in _activeLines)
                {
                    lr.enabled = false;
                    _linePool.Add(lr);
                }
                _activeLines.Clear();
            }
        }

        private void SetAlpha(float a)
        {
            foreach (var lr in _activeLines)
            {
                Color sc = lr.startColor;
                Color ec = lr.endColor;
                sc.a = a;
                ec.a = a;
                lr.startColor = sc;
                lr.endColor = ec;
            }
        }

        private void Update()
        {
            if (_activeLines.Count > 0 && _currentAlpha > 0 && _materialInstance != null)
            {
                float offset = Time.time * -2.0f; 
                _materialInstance.mainTextureOffset = new Vector2(offset, 0);
            }
        }
    }
}
