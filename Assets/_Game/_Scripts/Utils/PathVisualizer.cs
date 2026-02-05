using UnityEngine;
using Zenject;
using System.Collections.Generic;
using MaouSamaTD.Grid;
using MaouSamaTD.Managers;
using MaouSamaTD.Units;

namespace MaouSamaTD.Utils
{
    [RequireComponent(typeof(LineRenderer))]
    public class PathVisualizer : MonoBehaviour
    {
        [Inject] private GridManager _gridManager;

        private LineRenderer _lineRenderer;

        [SerializeField] private Material _sourceMaterial;

        private Material _materialInstance;
        private Coroutine _fadeRoutine;

        public void Init(Material overrideMaterial = null)
        {
            if (overrideMaterial != null) _sourceMaterial = overrideMaterial;

            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer == null)
            {
                _lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            ConfigureLineRenderer();
            ConfigureMaterial(); 
            
            DrawPath();
            
            // Set initial alpha to 0 for fade-in later
            SetAlpha(0f);
            _lineRenderer.enabled = false;
        }

        private void ConfigureLineRenderer()
        {
            _lineRenderer.startWidth = 0.5f; // Wider for arrows
            _lineRenderer.endWidth = 0.5f;
            _lineRenderer.positionCount = 0;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.textureMode = LineTextureMode.Tile; 
            
            _lineRenderer.startColor = new Color(1f, 0.5f, 0f, 0f); // Alpha 0 init
            _lineRenderer.endColor = new Color(0f, 1f, 0.5f, 0f);   
        }

        private void ConfigureMaterial()
        {
            if (_sourceMaterial != null)
            {
                // Source material (from Assets) ensures shader is included in build
                _materialInstance = new Material(_sourceMaterial);
            }
            else
            {
                // Fallback for Editor (might fail in Build)
                Shader shader = Shader.Find("Mobile/Particles/Additive");
                if (shader == null) shader = Shader.Find("Particles/Additive");
                if (shader == null) shader = Shader.Find("Sprites/Default"); 
                _materialInstance = new Material(shader);
            }

            _lineRenderer.material = _materialInstance;
            
            // Generate and Assign Arrow Texture
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

            // Clear
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            // Draw Arrow ">"
            // Center is (32,32)
            // Tip at (48, 32)
            // Tails at (16, 16) and (16, 48)
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // Normalized coords 0..1
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // Arrow Logic: simple chevron
                    // Line 1: y = x (approx bottom wing)
                    // Line 2: y = 1-x (approx top wing)
                    
                    // Let's do simple pixel math
                    // Center line y=32
                    
                    // Distance from center line
                    float distY = Mathf.Abs(y - size/2f);
                    
                    // Arrow shape: x should be roughly size/2 + (something - distY)
                    // Tip is at right (high X)
                    
                    // Chevron condition: x > (size - distY * 1.5) ?
                    // Let's create a filled chevron mask
                    
                    bool mask = false;
                    // Chevron pointing Right
                    // Head at x=50. Tail at x=10. Width based on Y.
                    
                    float tailX = 10 + distY; // As we go further from center, tail moves right -> Arrow points LEFT? 
                    // Wait.
                    // Center Y=32. DistY=0 -> TailX=10.
                    // Y=0. DistY=32 -> TailX=42.
                    // shape <
                    
                    // We want >
                    // Tip at X=50.
                    // Wings go back.
                    // X < 50 - distY
                    
                    // Let's draw a thick line arrow
                    if (x < (55 - distY) && x > (45 - distY))
                    {
                        mask = true;
                    }

                    if (mask) pixels[y * size + x] = white;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        [ContextMenu("Redraw Path")]
        public void DrawPath()
        {
            if (_gridManager == null) return;

            Queue<Tile> pathQueue = _gridManager.GetPath(_gridManager.SpawnPoint, _gridManager.ExitPoint, EnemyMovementType.Ground);
            
            if (pathQueue == null || pathQueue.Count == 0)
            {
                _lineRenderer.positionCount = 0;
                return;
            }

            List<Vector3> points = new List<Vector3>();
            
            // Add Start - Lifted 0.7f to be above floor/HighGround (0.5f)
            float visualHeight = 0.7f;
            points.Add(_gridManager.GridToWorldPosition(_gridManager.SpawnPoint) + Vector3.up * visualHeight);

            foreach (var tile in pathQueue)
            {
                points.Add(tile.transform.position + Vector3.up * visualHeight);
            }
            
            _lineRenderer.positionCount = points.Count;
            _lineRenderer.SetPositions(points.ToArray());
        }

        public void Show()
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = true;
                if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
                _fadeRoutine = StartCoroutine(FadeRoutine(1f, 1.0f)); // Fade In over 1s
            }
        }

        public void Hide()
        {
            if (_lineRenderer != null)
            {
                if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
                _fadeRoutine = StartCoroutine(FadeRoutine(0f, 0.5f)); // Fade Out over 0.5s
            }
        }

        private System.Collections.IEnumerator FadeRoutine(float targetAlpha, float duration)
        {
            float startAlpha = GetCurrentAlpha();
            float time = 0;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                SetAlpha(newAlpha);
                yield return null;
            }
            
            SetAlpha(targetAlpha);

            if (targetAlpha <= 0.01f)
            {
                _lineRenderer.enabled = false;
            }
        }

        private float GetCurrentAlpha()
        {
            return _lineRenderer.startColor.a;
        }

        private void SetAlpha(float a)
        {
            // Keep original colors but update alpha
            Color start = new Color(1f, 0.5f, 0f, a); // Orange
            Color end = new Color(0f, 1f, 0.5f, a);   // Green
            
            _lineRenderer.startColor = start;
            _lineRenderer.endColor = end;
        }

        private void Update()
        {
            if (_lineRenderer != null && _lineRenderer.enabled && _materialInstance != null)
            {
                float offset = Time.time * -2.0f; 
                _materialInstance.mainTextureOffset = new Vector2(offset, 0);
            }
        }
    }
}
