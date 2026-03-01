using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using NaughtyAttributes;
using DG.Tweening;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Black transparent overlay that blocks raycasts everywhere EXCEPT for the area(s) of the target UI element(s).
    /// Now supports world-space highlights (tiles) by projecting them to UI space.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class UIPopupBlocker : MonoBehaviour
    {
        [Header("Overlay Settings")]
        [SerializeField] private Material overlayMaterial;
        [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.85f);
        [SerializeField] private int maskSize = 1024;
        [SerializeField] private float transitionDuration = 0.3f;

        private List<RectTransform> targetElements = new List<RectTransform>();
        private GameObject overlayGO;
        private Image overlayImage;
        private HoleRaycaster overlayRaycaster;
        private bool isActive = false;
        private Texture2D maskTex;
        private CanvasGroup canvasGroup;

        // For World Highlights
        private bool isWorldHighlight = false;
        private Vector3 worldHighlightPos;
        private float worldHighlightRadius;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Ensure Canvas is set up correctly for full-screen overlay
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999; 
            }

            gameObject.SetActive(true);
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
            isActive = false;
        }

        public void ShowBlockerWithTargets(List<RectTransform> targets, Vector3? worldPos = null, float worldRadius = 1.0f)
        {
            if (!isActive)
            {
                targetElements.Clear();
            }
            
            if (targets != null) 
            {
                foreach(var t in targets)
                {
                    if (!targetElements.Contains(t)) targetElements.Add(t);
                }
            }
            
            if (worldPos.HasValue)
            {
                isWorldHighlight = true;
                worldHighlightPos = worldPos.Value;
                worldHighlightRadius = worldRadius;
            }
            // Don't reset isWorldHighlight if we are just adding UI targets
            
            Show();
        }

        public void RemoveTarget(RectTransform target)
        {
            if (target == null) return;
            if (targetElements.Contains(target))
            {
                targetElements.Remove(target);
                if (isActive)
                {
                    UpdateOverlayMask();
                    overlayRaycaster.SetTargets(targetElements);
                }
            }
        }

        public void ClearTargets()
        {
            targetElements.Clear();
            isWorldHighlight = false;
            if (isActive)
            {
                UpdateOverlayMask();
                overlayRaycaster.SetTargets(targetElements);
                overlayRaycaster.SetWorldHighlight(false, Vector3.zero, 0);
            }
        }

        public void ShowBlockerWithTarget(RectTransform target)
        {
            List<RectTransform> list = new List<RectTransform>();
            if (target != null) list.Add(target);
            ShowBlockerWithTargets(list);
        }

        public void ShowBlockerWithWorldHighlight(Vector3 worldPos, float radius = 1.0f)
        {
            ShowBlockerWithTargets(null, worldPos, radius);
        }

        public void HideBlocker()
        {
            if (!isActive) return;
            canvasGroup.DOKill();
            canvasGroup.DOFade(0, transitionDuration).SetUpdate(true).OnComplete(() =>
            {
                canvasGroup.blocksRaycasts = false;
                isActive = false;
                targetElements.Clear(); // Cleanup targets on hide
                isWorldHighlight = false;
            });
        }

        private void Show()
        {
            if (overlayGO == null) CreateOverlay();
            
            // Sync current inspector color to material
            if (overlayImage != null)
            {
                overlayImage.color = Color.white; // Identity for multiplication
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

            Debug.Log($"[tutorial] Showing blocker. Color: {overlayColor}, UI Targets: {targetElements.Count}, World Highlight: {isWorldHighlight}");
            
            gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true;
            isActive = true;
            
            UpdateOverlayMask();
            overlayRaycaster.SetTargets(targetElements);
            overlayRaycaster.SetWorldHighlight(isWorldHighlight, worldHighlightPos, worldHighlightRadius);
            
            canvasGroup.DOKill();
            canvasGroup.alpha = 1; // Force opaque immediately as per user request
            // canvasGroup.DOFade(1, transitionDuration).SetUpdate(true); // Disable fade for now to guarantee visibility
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

            // HoleRaycaster inherits from Image, so adding it adds the Image component automatically
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
            
            foreach (var rt in targetElements)
            {
                if (rt == null) continue;
                DrawUIHole(rt, pixels, overlayRect);
            }

            maskTex.SetPixels32(pixels);
            maskTex.Apply(false);
            overlayImage.material.SetTexture("_MaskTex", maskTex);
        }

        private void DrawUIHole(RectTransform rt, Color32[] pixels, RectTransform overlayRect)
        {
            Vector3[] worldCorners = new Vector3[4];
            rt.GetWorldCorners(worldCorners);

            Vector2 localBL, localTR;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, RectTransformUtility.WorldToScreenPoint(null, worldCorners[0]), null, out localBL);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, RectTransformUtility.WorldToScreenPoint(null, worldCorners[2]), null, out localTR);

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
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldHighlightPos);
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, screenPos, null, out localPos);

            Rect overlayPixelRect = overlayRect.rect;
            float uvX = Mathf.InverseLerp(overlayPixelRect.xMin, overlayPixelRect.xMax, localPos.x);
            float uvY = Mathf.InverseLerp(overlayPixelRect.yMin, overlayPixelRect.yMax, localPos.y);

            int centerX = Mathf.RoundToInt(uvX * maskSize);
            int centerY = Mathf.RoundToInt(uvY * maskSize);
            
            float screenRadius = (Camera.main.WorldToScreenPoint(worldHighlightPos + Vector3.right * worldHighlightRadius) - (Vector3)screenPos).magnitude;
            float uvRadius = screenRadius / Screen.width; 
            int pixelRadiusX = Mathf.RoundToInt(uvRadius * maskSize);
            // Apply isometric squish factor (approx 0.6 is common for 60deg/isometric tilts)
            int pixelRadiusY = Mathf.RoundToInt(pixelRadiusX * 0.6f); 

            for (int y = centerY - pixelRadiusY; y <= centerY + pixelRadiusY; y++)
            for (int x = centerX - pixelRadiusX; x <= centerX + pixelRadiusX; x++)
            {
                if (x < 0 || x >= maskSize || y < 0 || y >= maskSize) continue;
                
                // Ellipse distance formula: (x^2 / a^2) + (y^2 / b^2) <= 1
                float dx = (x - centerX) / (float)pixelRadiusX;
                float dy = (y - centerY) / (float)pixelRadiusY;
                
                if (dx * dx + dy * dy <= 1.0f)
                {
                    int idx = y * maskSize + x;
                    pixels[idx] = new Color32(255, 255, 255, 0);
                }
            }
        }

        public class HoleRaycaster : Image
        {
            private List<RectTransform> targets = new List<RectTransform>();
            private bool isWorldHighlight;
            private Vector3 worldPos;
            private float worldRadius;

            public void SetTargets(List<RectTransform> rects)
            {
                targets.Clear();
                if (rects != null) targets.AddRange(rects);
            }

            public void SetWorldHighlight(bool active, Vector3 pos, float radius)
            {
                isWorldHighlight = active;
                worldPos = pos;
                worldRadius = radius;
            }

            public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
            {
                // 1. Check UI Targets
                foreach (var target in targets)
                {
                    if (target != null && RectTransformUtility.RectangleContainsScreenPoint(target, screenPoint, eventCamera))
                    {
                        // Debug.Log($"[UIPopupBlocker] Raycast VALID (UI Target: {target.name})");
                        return false; 
                    }
                }

                // 2. Check World Highlight (Projected to screen)
                if (isWorldHighlight)
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        Vector2 screenHolePos = mainCam.WorldToScreenPoint(worldPos);
                        float screenRadiusX = (mainCam.WorldToScreenPoint(worldPos + Vector3.right * worldRadius) - (Vector3)screenHolePos).magnitude;
                        float screenRadiusY = screenRadiusX * 0.6f; // Isometric squish
                        
                        Vector2 diff = screenPoint - screenHolePos;
                        float dx = diff.x / screenRadiusX;
                        float dy = diff.y / screenRadiusY;
                        
                        if (dx * dx + dy * dy <= 1.0f)
                        {
                            return false; 
                        }
                    }
                }

                // 3. Block
                return true; 
            }
        }
    }
}