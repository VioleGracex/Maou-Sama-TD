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
        }

        [Header("Overlay Settings")]
        [SerializeField] private Material overlayMaterial;
        [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.85f);
        [SerializeField] private int maskSize = 512;
        [SerializeField] private float transitionDuration = 0.2f;

        private List<UIHighlightData> uiHighlights = new List<UIHighlightData>();
        private List<WorldHighlightData> worldHighlights = new List<WorldHighlightData>();
        private bool isWorldHighlight = false;
        private bool _isDirty = true;
        
        private GameObject overlayGO;
        private Image overlayImage;
        private HoleRaycaster overlayRaycaster;
        private bool isActive = false;
        private Texture2D maskTex;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999; 
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
        }

        public void ShowBlockerWithDetailedTargets(List<UIHighlightData> uiHits, List<WorldHighlightData> worldHits)
        {
            uiHighlights.Clear();
            if (uiHits != null) uiHighlights.AddRange(uiHits);

            isWorldHighlight = worldHits != null && worldHits.Count > 0;
            worldHighlights.Clear();
            if (worldHits != null) worldHighlights.AddRange(worldHits);
            _isDirty = true;
            Show();
        }

        public void ShowBlockerWithWorldHighlightData(List<RectTransform> targets, List<WorldHighlightData> highlights)
        {
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

        public void ShowBlockerWithTarget(RectTransform target)
        {
            if (target == null) return;
            // Add if not exists
            if (!uiHighlights.Exists(h => h.Target == target))
            {
                uiHighlights.Add(new UIHighlightData { Target = target, Size = Vector2.one });
                _isDirty = true;
            }
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

                Vector2 sMin = RectTransformUtility.WorldToScreenPoint(targetCam, center - size * 0.5f);
                Vector2 sMax = RectTransformUtility.WorldToScreenPoint(targetCam, center + size * 0.5f);

                if (screenPoint.x >= Mathf.Min(sMin.x, sMax.x) && screenPoint.x <= Mathf.Max(sMin.x, sMax.x) && 
                    screenPoint.y >= Mathf.Min(sMin.y, sMax.y) && screenPoint.y <= Mathf.Max(sMin.y, sMax.y))
                {
                    return true;
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
                            return true;
                    }
                }
            }

            return false;
        }

        public void HideBlocker()
        {
            if (!isActive) return;
            canvasGroup.DOKill();
            canvasGroup.DOFade(0, transitionDuration).SetUpdate(true).OnComplete(() =>
            {
                canvasGroup.blocksRaycasts = false;
                isActive = false;
                uiHighlights.Clear();
                worldHighlights.Clear();
                isWorldHighlight = false;
                _isDirty = true;
            });
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
            isActive = true;
            _isDirty = true;
            
            UpdateOverlayMask();
            overlayRaycaster.SetUITargets(uiHighlights);
            overlayRaycaster.SetWorldHighlights(isWorldHighlight, worldHighlights);
            
            canvasGroup.DOKill();
            canvasGroup.alpha = 1; 
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
            
            // Only update if dirty or moving (though for now we check highlights positions every frame if we want animating holes)
            // But if everything is static, we can save a lot.
            // For simplicity, we'll keep updating if any world highlight exists (as they move with camera/units)
            // But UI highlights are often static.
            
            if (!_isDirty && !isWorldHighlight && uiHighlights.Count > 0) return;

            if (maskTex == null || maskTex.width != maskSize)
            {
                maskTex = new Texture2D(maskSize, maskSize, TextureFormat.Alpha8, false);
                maskTex.wrapMode = TextureWrapMode.Clamp;
            }

            Color32[] pixels = maskTex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);

            var overlayRect = overlayImage.rectTransform;

            if (isWorldHighlight)
            {
                DrawWorldHole(pixels, overlayRect);
            }
            
            foreach (var h in uiHighlights)
            {
                if (h.Target == null) continue;
                DrawUIHole(h, pixels, overlayRect);
            }

            maskTex.SetPixels32(pixels);
            maskTex.Apply(false);
            overlayImage.material.SetTexture("_MaskTex", maskTex);
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

            Vector2 localBL, localTR;
            Camera targetCam = GetTargetCamera(rt);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, RectTransformUtility.WorldToScreenPoint(targetCam, scaledCorners[0]), null, out localBL);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, RectTransformUtility.WorldToScreenPoint(targetCam, scaledCorners[2]), null, out localTR);

            Rect overlayPixelRect = overlayRect.rect;
            float minX = Mathf.InverseLerp(overlayPixelRect.xMin, overlayPixelRect.xMax, localBL.x);
            float minY = Mathf.InverseLerp(overlayPixelRect.yMin, overlayPixelRect.yMax, localBL.y);
            float maxX = Mathf.InverseLerp(overlayPixelRect.xMin, overlayPixelRect.xMax, localTR.x);
            float maxY = Mathf.InverseLerp(overlayPixelRect.yMin, overlayPixelRect.yMax, localTR.y);

            int pxMinX = Mathf.Clamp(Mathf.RoundToInt(minX * maskSize), 0, maskSize);
            int pxMinY = Mathf.Clamp(Mathf.RoundToInt(minY * maskSize), 0, maskSize);
            int pxMaxX = Mathf.Clamp(Mathf.RoundToInt(maxX * maskSize), 0, maskSize);
            int pxMaxY = Mathf.Clamp(Mathf.RoundToInt(maxY * maskSize), 0, maskSize);

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

        public class HoleRaycaster : Image
        {
            private List<UIHighlightData> uiHighlights = new List<UIHighlightData>();
            private bool isWorldHighlight;
            private List<WorldHighlightData> worldHighlights = new List<WorldHighlightData>();

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

            public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
            {
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

                    Vector2 sMin = RectTransformUtility.WorldToScreenPoint(targetCam, center - size * 0.5f);
                    Vector2 sMax = RectTransformUtility.WorldToScreenPoint(targetCam, center + size * 0.5f);
                    
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