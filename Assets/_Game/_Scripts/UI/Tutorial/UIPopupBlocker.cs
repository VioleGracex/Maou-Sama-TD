using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Black transparent overlay that blocks raycasts everywhere EXCEPT for the area(s) of the target UI element(s).
    /// Supports both UI RectTransforms and world-space highlights (tiles).
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class UIPopupBlocker : MonoBehaviour
    {
        public struct WorldHighlightData
        {
            public Vector3 Position;
            public Vector2 Size;
            public float Height;
        }

        [System.Serializable]
        public struct UIHighlightData
        {
            public RectTransform Target;
            public Vector2 Size; // Multiplier, 1 = original size
            public Vector2 Offset;
        }

        [Header("Overlay Settings")]
        [SerializeField] private Material overlayMaterial;
        [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.85f);
        [SerializeField] private int maskSize = 256;
        [SerializeField] private float transitionDuration = 0.1f;

        private List<UIHighlightData> uiHighlights = new List<UIHighlightData>();
        private List<WorldHighlightData> worldHighlights = new List<WorldHighlightData>();
        private bool isWorldHighlight = false;
        private bool _isDirty = true;
        
        private GameObject overlayGO;
        private Image overlayImage;
        private HoleRaycaster overlayRaycaster;
        private bool isActive = false;
        public bool IsActive => isActive;

        /// <summary>
        /// When true, ALL clicks are blocked — including normally whitelisted buttons (SpeedButton, PauseButton).
        /// Use this for full modal overlays like post-battle screens or loading transitions.
        /// </summary>
        public bool BlockAllInput = false;
        private Texture2D maskTex;
        private Color32[] _cachedPixels;
        private CanvasGroup canvasGroup;
        private int _lastHash = 0;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50; // Below game UI buttons (typically ~100) but above game world
            }

            gameObject.SetActive(true);
            
            RectTransform tr = GetComponent<RectTransform>();
            if (tr != null)
            {
                tr.anchorMin = Vector2.zero;
                tr.anchorMax = Vector2.one;
                tr.offsetMin = Vector2.zero;
                tr.offsetMax = Vector2.zero;
                tr.pivot = new Vector2(0.5f, 0.5f);
            }

            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
            isActive = false;

            // Pre-allocate pixels
            _cachedPixels = new Color32[maskSize * maskSize];

            // Dynamically parent to MainCanvas if found
            GameObject mainCanvasGO = GameObject.FindWithTag("MainCanvas");
            if (mainCanvasGO != null && transform.parent != mainCanvasGO.transform)
            {
                transform.SetParent(mainCanvasGO.transform, false);
                transform.SetAsLastSibling();
            }
        }

        public void SetSortingOrder(int order)
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.sortingOrder = order;
        }

        public void ShowBlockerWithDetailedTargets(List<UIHighlightData> uiHits, List<WorldHighlightData> worldHits, bool blockAll = false)
        {
            BlockAllInput = blockAll;
            uiHighlights.Clear();
            if (uiHits != null) uiHighlights.AddRange(uiHits);

            isWorldHighlight = worldHits != null && worldHits.Count > 0;
            worldHighlights.Clear();
            if (worldHits != null) worldHighlights.AddRange(worldHits);
            _isDirty = true;
            Show();
        }

        public void ShowBlockerWithWorldHighlightData(List<RectTransform> targets, List<WorldHighlightData> highlights, bool blockAll = false)
        {
            BlockAllInput = blockAll;
            uiHighlights.Clear();
            if (targets != null)
            {
                foreach (var t in targets) uiHighlights.Add(new UIHighlightData { Target = t, Size = Vector2.one });
            }
            
            isWorldHighlight = highlights != null && highlights.Count > 0;
            worldHighlights.Clear();
            if (highlights != null) worldHighlights.AddRange(highlights);
            _isDirty = true;
            Show();
        }

        public void ShowBlockerWithTarget(RectTransform target, bool blockAll = false)
        {
            if (target == null) return;
            BlockAllInput = blockAll;
            // Add if not exists
            if (!uiHighlights.Exists(h => h.Target == target))
            {
                uiHighlights.Add(new UIHighlightData { Target = target, Size = Vector2.one });
                _isDirty = true;
            }
            Show();
        }

        /// <summary>
        /// Show the blocker with NO holes — blocks ALL clicks on the entire screen.
        /// Use for loading screens and full modal transitions.
        /// </summary>
        public void ShowFullBlocker()
        {
            BlockAllInput = true;
            uiHighlights.Clear();
            worldHighlights.Clear();
            isWorldHighlight = false;
            _isDirty = true;
            Show();
        }

        public void RemoveTarget(RectTransform target)
        {
            if (target == null) return;
            uiHighlights.RemoveAll(h => h.Target == target);
            _isDirty = true;
            if (isActive) UpdateOverlayMask();
        }

        public void ClearTargets()
        {
            uiHighlights.Clear();
            worldHighlights.Clear();
            isWorldHighlight = false;
            _isDirty = true;
            
            if (overlayRaycaster != null)
            {
                UpdateOverlayMask();
                overlayRaycaster.SetUITargets(uiHighlights);
                overlayRaycaster.SetWorldHighlights(false, worldHighlights);
            }
        }

        public bool IsPointerInHole(Vector2 screenPoint)
        {
            return IsPointerInUIHole(screenPoint) || IsPointerInWorldHole(screenPoint);
        }

        public bool IsPointerInUIHole(Vector2 screenPoint)
        {
            if (!this.gameObject.activeInHierarchy || !isActive) return false;

            foreach (var h in uiHighlights)
            {
                if (h.Target == null) continue;

                Camera targetCam = GetTargetCamera(h.Target);
                Vector3[] corners = new Vector3[4];
                h.Target.GetWorldCorners(corners);
                Vector3 center = (corners[0] + corners[2]) * 0.5f;
                Vector3 size = corners[2] - corners[0];
                size.x *= h.Size.x;
                size.y *= h.Size.y;

                Vector2 sMin = RectTransformUtility.WorldToScreenPoint(targetCam, center + new Vector3(size.x * h.Offset.x, size.y * h.Offset.y, 0) - size * 0.5f);
                Vector2 sMax = RectTransformUtility.WorldToScreenPoint(targetCam, center + new Vector3(size.x * h.Offset.x, size.y * h.Offset.y, 0) + size * 0.5f);

                if (screenPoint.x >= Mathf.Min(sMin.x, sMax.x) && screenPoint.x <= Mathf.Max(sMin.x, sMax.x) &&
                    screenPoint.y >= Mathf.Min(sMin.y, sMax.y) && screenPoint.y <= Mathf.Max(sMin.y, sMax.y))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsPointerInWorldHole(Vector2 screenPoint)
        {
            if (!this.gameObject.activeInHierarchy || !isActive || !isWorldHighlight) return false;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                foreach (var h in worldHighlights)
                {
                    float hsX = h.Size.x * 0.5f;
                    float hsZ = h.Size.y * 0.5f;
                    float vhs = h.Height * 0.5f;

                    Vector3[] corners = new Vector3[]
                    {
                        h.Position + new Vector3(-hsX, -vhs, -hsZ),
                        h.Position + new Vector3(hsX, -vhs, -hsZ),
                        h.Position + new Vector3(hsX, -vhs, hsZ),
                        h.Position + new Vector3(-hsX, -vhs, hsZ),
                        h.Position + new Vector3(-hsX, vhs, -hsZ),
                        h.Position + new Vector3(hsX, vhs, -hsZ),
                        h.Position + new Vector3(hsX, vhs, hsZ),
                        h.Position + new Vector3(-hsX, vhs, hsZ)
                    };

                    float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                    foreach (var corner in corners)
                    {
                        Vector2 sPos = mainCam.WorldToScreenPoint(corner);
                        minX = Mathf.Min(minX, sPos.x);
                        minY = Mathf.Min(minY, sPos.y);
                        maxX = Mathf.Max(maxX, sPos.x);
                        maxY = Mathf.Max(maxY, sPos.y);
                    }

                    if (screenPoint.x >= minX && screenPoint.x <= maxX && screenPoint.y >= minY && screenPoint.y <= maxY)
                        return true;
                }
            }

            return false;
        }

        public void HideBlocker(bool immediate = false)
        {
            if (!isActive) return;
            canvasGroup.DOKill();
            BlockAllInput = false;
            
            if (immediate)
            {
                canvasGroup.alpha = 0;
                canvasGroup.blocksRaycasts = false;
                isActive = false;
                uiHighlights.Clear();
                worldHighlights.Clear();
                isWorldHighlight = false;
                _isDirty = true;
            }
            else
            {
                canvasGroup.DOFade(0, transitionDuration).SetUpdate(true).OnComplete(() =>
                {
                    canvasGroup.blocksRaycasts = false;
                    isActive = false;
                    BlockAllInput = false;
                    uiHighlights.Clear();
                    worldHighlights.Clear();
                    isWorldHighlight = false;
                    _isDirty = true;
                });
            }
        }

        private void Show()
        {
            if (overlayGO == null) CreateOverlay();
            
            if (overlayImage != null)
            {
                overlayImage.color = Color.white;
                if (overlayImage.material != null)
                {
                    overlayImage.material.SetColor("_Color", overlayColor);
                }
            }

            if (overlayGO != null)
            {
                var rect = overlayGO.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true;
            if (!isActive) _isDirty = true;
            isActive = true;
            
            UpdateOverlayMask();
            overlayRaycaster.SetUITargets(uiHighlights);
            overlayRaycaster.SetWorldHighlights(isWorldHighlight, worldHighlights);
            
            canvasGroup.DOKill();
            canvasGroup.alpha = 1f; // Force immediate opaque to prevent 'weird movement' or fading artifacts
        }

        private void CreateOverlay()
        {
            Canvas parentCanvas = GetComponent<Canvas>();
            overlayGO = new GameObject("Overlay_Image", typeof(RectTransform), typeof(CanvasRenderer));
            overlayGO.transform.SetParent(parentCanvas.transform, false);
            
            var rect = overlayGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            overlayRaycaster = overlayGO.AddComponent<HoleRaycaster>();
            overlayImage = overlayRaycaster;

            if (overlayMaterial != null)
            {
                overlayImage.material = new Material(overlayMaterial);
            }
            overlayImage.color = Color.white;
            overlayImage.raycastTarget = true;
        }

        private void LateUpdate()
        {
            if (isActive)
            {
                UpdateOverlayMask();
            }
        }

        private void UpdateOverlayMask()
        {
            if (overlayImage == null || overlayImage.material == null) return;
            
            int currentHash = CalculateUIHash();
            if (!_isDirty && currentHash == _lastHash) return;
            _lastHash = currentHash;
            _isDirty = false;

            if (maskTex == null || maskTex.width != maskSize)
            {
                maskTex = new Texture2D(maskSize, maskSize, TextureFormat.Alpha8, false);
                maskTex.wrapMode = TextureWrapMode.Clamp;
                _cachedPixels = new Color32[maskSize * maskSize];
            }

            // Fill with solid white (opaque in mask)
            for (int i = 0; i < _cachedPixels.Length; i++) _cachedPixels[i] = new Color32(255, 255, 255, 255);

            var overlayRect = overlayImage.rectTransform;

            if (isWorldHighlight)
            {
                DrawWorldHole(_cachedPixels, overlayRect);
            }
            
            foreach (var h in uiHighlights)
            {
                if (h.Target == null) continue;
                DrawUIHole(h, _cachedPixels, overlayRect);
            }

            maskTex.SetPixels32(_cachedPixels);
            maskTex.Apply(false);
            overlayImage.material.SetTexture("_MaskTex", maskTex);
            
            _isDirty = false; // Reset dirty flag
        }

        private void DrawUIHole(UIHighlightData data, Color32[] pixels, RectTransform overlayRect)
        {
            RectTransform rt = data.Target;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Vector3 center = (corners[0] + corners[2]) * 0.5f;
            Vector3 size = corners[2] - corners[0];
            Vector3 scaledSize = new Vector3(size.x * data.Size.x, size.y * data.Size.y, size.z);
            
            Vector3[] scaledCorners = new Vector3[4];
            scaledCorners[0] = center + new Vector3(-scaledSize.x * 0.5f, -scaledSize.y * 0.5f, 0);
            scaledCorners[2] = center + new Vector3(scaledSize.x * 0.5f, scaledSize.y * 0.5f, 0);

            Camera targetCam = GetTargetCamera(rt);
            Vector3 relativeOffset = new Vector3(size.x * data.Offset.x, size.y * data.Offset.y, 0);

            // Map to screen pixel coordinates
            Vector2 screenBL = RectTransformUtility.WorldToScreenPoint(targetCam, scaledCorners[0] + relativeOffset);
            Vector2 screenTR = RectTransformUtility.WorldToScreenPoint(targetCam, scaledCorners[2] + relativeOffset);

            // Map screen pixels → mask pixels directly (correct aspect ratio)
            float sw = Screen.width;
            float sh = Screen.height;
            int pxMinX = Mathf.Clamp(Mathf.RoundToInt((screenBL.x / sw) * maskSize), 0, maskSize);
            int pxMinY = Mathf.Clamp(Mathf.RoundToInt((screenBL.y / sh) * maskSize), 0, maskSize);
            int pxMaxX = Mathf.Clamp(Mathf.RoundToInt((screenTR.x / sw) * maskSize), 0, maskSize);
            int pxMaxY = Mathf.Clamp(Mathf.RoundToInt((screenTR.y / sh) * maskSize), 0, maskSize);

            for (int y = pxMinY; y < pxMaxY; y++)
            {
                for (int x = pxMinX; x < pxMaxX; x++)
                {
                    int idx = y * maskSize + x;
                    if (idx >= 0 && idx < pixels.Length) pixels[idx] = new Color32(255, 255, 255, 0);
                }
            }
        }

        private void DrawWorldHole(Color32[] pixels, RectTransform overlayRect)
        {
            if (Camera.main == null) return;

            foreach (var h in worldHighlights)
            {
                float hsX = h.Size.x * 0.5f;
                float hsZ = h.Size.y * 0.5f;
                float vhs = h.Height * 0.5f;

                Vector3[] corners = new Vector3[]
                {
                    h.Position + new Vector3(-hsX, -vhs, -hsZ),
                    h.Position + new Vector3(hsX, -vhs, -hsZ),
                    h.Position + new Vector3(hsX, -vhs, hsZ),
                    h.Position + new Vector3(-hsX, -vhs, hsZ),
                    h.Position + new Vector3(-hsX, vhs, -hsZ),
                    h.Position + new Vector3(hsX, vhs, -hsZ),
                    h.Position + new Vector3(hsX, vhs, hsZ),
                    h.Position + new Vector3(-hsX, vhs, hsZ)
                };

                Vector2 minP = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 maxP = new Vector2(float.MinValue, float.MinValue);

                foreach (var worldCorner in corners)
                {
                    Vector2 screenPos = Camera.main.WorldToScreenPoint(worldCorner);
                    Vector2 localPos;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, screenPos, null, out localPos);
                    
                    minP = Vector2.Min(minP, localPos);
                    maxP = Vector2.Max(maxP, localPos);
                }

                Rect overlayPixelRect = overlayRect.rect;
                float minXIdx = Mathf.InverseLerp(overlayPixelRect.xMin, overlayPixelRect.xMax, minP.x);
                float minYIdx = Mathf.InverseLerp(overlayPixelRect.yMin, overlayPixelRect.yMax, minP.y);
                float maxXIdx = Mathf.InverseLerp(overlayPixelRect.xMin, overlayPixelRect.xMax, maxP.x);
                float maxYIdx = Mathf.InverseLerp(overlayPixelRect.yMin, overlayPixelRect.yMax, maxP.y);

                int pxMinX = Mathf.Clamp(Mathf.RoundToInt(minXIdx * maskSize), 0, maskSize);
                int pxMinY = Mathf.Clamp(Mathf.RoundToInt(minYIdx * maskSize), 0, maskSize);
                int pxMaxX = Mathf.Clamp(Mathf.RoundToInt(maxXIdx * maskSize), 0, maskSize);
                int pxMaxY = Mathf.Clamp(Mathf.RoundToInt(maxYIdx * maskSize), 0, maskSize);

                for (int y = pxMinY; y < pxMaxY; y++)
                {
                    for (int x = pxMinX; x < pxMaxX; x++)
                    {
                        int idx = y * maskSize + x;
                        if (idx >= 0 && idx < pixels.Length) pixels[idx] = new Color32(255, 255, 255, 0);
                    }
                }
            }
        }

        private Camera GetTargetCamera(RectTransform rt)
        {
            if (rt == null) return null;
            Canvas canvas = rt.GetComponentInParent<Canvas>();
            if (canvas == null) return null;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main;
        }

        private int CalculateUIHash()
        {
            unchecked 
            {
                int hash = 17;
                foreach (var h in uiHighlights)
                {
                    if (h.Target == null) continue;
                    hash = hash * 31 + h.Target.position.GetHashCode();
                    hash = hash * 31 + h.Target.rect.size.GetHashCode();
                    hash = hash * 31 + h.Size.GetHashCode();
                    hash = hash * 31 + h.Offset.GetHashCode();
                }
                if (isWorldHighlight)
                {
                    foreach (var w in worldHighlights)
                    {
                        hash = hash * 31 + w.Position.GetHashCode();
                        hash = hash * 31 + w.Size.GetHashCode();
                    }
                }
                return hash;
            }
        }

        public class HoleRaycaster : Image
        {
            private List<UIHighlightData> uiHighlights = new List<UIHighlightData>();
            private bool isWorldHighlight;
            private List<WorldHighlightData> worldHighlights = new List<WorldHighlightData>();

            private MaouSamaTD.Managers.GameManager _gameManager;
            private GameControlUI _gameControlUI;

            private bool IsRectTransformHit(RectTransform rt, Vector2 screenPoint)
            {
                if (rt == null || !rt.gameObject.activeInHierarchy) return false;
                
                Camera targetCam = null;
                Canvas canvas = rt.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) targetCam = Camera.main;

                Vector3[] corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                Vector3 center = (corners[0] + corners[2]) * 0.5f;
                Vector3 size = (corners[2] - corners[0]);

                Vector2 sMin = RectTransformUtility.WorldToScreenPoint(targetCam, center - size * 0.5f);
                Vector2 sMax = RectTransformUtility.WorldToScreenPoint(targetCam, center + size * 0.5f);
                
                return screenPoint.x >= Mathf.Min(sMin.x, sMax.x) && screenPoint.x <= Mathf.Max(sMin.x, sMax.x) && 
                       screenPoint.y >= Mathf.Min(sMin.y, sMax.y) && screenPoint.y <= Mathf.Max(sMin.y, sMax.y);
            }

            public void SetUITargets(List<UIHighlightData> rects)
            {
                uiHighlights.Clear();
                if (rects != null) uiHighlights.AddRange(rects);
            }

            public void SetWorldHighlights(bool active, List<WorldHighlightData> highlights)
            {
                isWorldHighlight = active;
                worldHighlights.Clear();
                if (highlights != null) worldHighlights.AddRange(highlights);
            }

            private float _lastLookupTime = -1f;

            public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
            {
                if (_gameManager == null || _gameControlUI == null)
                {
                    if (Time.unscaledTime - _lastLookupTime > 1.0f)
                    {
                        _lastLookupTime = Time.unscaledTime;
                        if (_gameManager == null)
                        {
                            _gameManager = UnityEngine.Object.FindObjectOfType<MaouSamaTD.Managers.GameManager>();
                        }
                        if (_gameControlUI == null)
                        {
                            _gameControlUI = UnityEngine.Object.FindObjectOfType<GameControlUI>();
                        }
                    }
                }

                // Get the owning UIPopupBlocker to check BlockAllInput flag
                var blocker = GetComponentInParent<UIPopupBlocker>();
                bool blockAll = blocker != null && blocker.BlockAllInput;

                // 1. If game is paused, do not block any clicks so the player can interact with Pause menu options (Resume, Restart, Retreat)
                // But only if we're NOT in full-block mode (e.g. loading screen).
                if (!blockAll && _gameManager != null && _gameManager.IsPaused)
                {
                    return false;
                }

                // 2. We no longer whitelist the Pause/Speed buttons here to prevent player interaction during active tutorial steps/dialogues.

                foreach (var h in uiHighlights)
                {
                    if (h.Target == null) continue;
                    
                    Camera targetCam = null;
                    Canvas canvas = h.Target.GetComponentInParent<Canvas>();
                    if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) targetCam = Camera.main;

                    Vector3[] corners = new Vector3[4];
                    h.Target.GetWorldCorners(corners);
                    Vector3 center = (corners[0] + corners[2]) * 0.5f;
                    Vector3 size = (corners[2] - corners[0]);
                    size.x *= h.Size.x;
                    size.y *= h.Size.y;

                    Vector2 sMin = RectTransformUtility.WorldToScreenPoint(targetCam, center + new Vector3(size.x * h.Offset.x, size.y * h.Offset.y, 0) - size * 0.5f);
                    Vector2 sMax = RectTransformUtility.WorldToScreenPoint(targetCam, center + new Vector3(size.x * h.Offset.x, size.y * h.Offset.y, 0) + size * 0.5f);
                    
                    if (screenPoint.x >= Mathf.Min(sMin.x, sMax.x) && screenPoint.x <= Mathf.Max(sMin.x, sMax.x) && 
                        screenPoint.y >= Mathf.Min(sMin.y, sMax.y) && screenPoint.y <= Mathf.Max(sMin.y, sMax.y))
                    {
                        return false;
                    }
                }

                if (isWorldHighlight)
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        foreach (var h in worldHighlights)
                        {
                            float hsX = h.Size.x * 0.5f;
                            float hsZ = h.Size.y * 0.5f;
                            float vhs = h.Height * 0.5f;

                            Vector3[] corners = new Vector3[]
                            {
                                h.Position + new Vector3(-hsX, -vhs, -hsZ),
                                h.Position + new Vector3(hsX, -vhs, -hsZ),
                                h.Position + new Vector3(hsX, -vhs, hsZ),
                                h.Position + new Vector3(-hsX, -vhs, hsZ),
                                h.Position + new Vector3(-hsX, vhs, -hsZ),
                                h.Position + new Vector3(hsX, vhs, -hsZ),
                                h.Position + new Vector3(hsX, vhs, hsZ),
                                h.Position + new Vector3(-hsX, vhs, hsZ)
                            };

                            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                            foreach (var corner in corners)
                            {
                                Vector2 sPos = mainCam.WorldToScreenPoint(corner);
                                minX = Mathf.Min(minX, sPos.x);
                                minY = Mathf.Min(minY, sPos.y);
                                maxX = Mathf.Max(maxX, sPos.x);
                                maxY = Mathf.Max(maxY, sPos.y);
                            }

                            if (screenPoint.x >= minX && screenPoint.x <= maxX && screenPoint.y >= minY && screenPoint.y <= maxY)
                                return false;
                        }
                    }
                }

                return true; 
            }
        }
    }
}