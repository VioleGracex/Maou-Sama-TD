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
        [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.7f);
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
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        public void ShowBlockerWithTarget(RectTransform target)
        {
            targetElements.Clear();
            if (target != null) targetElements.Add(target);
            isWorldHighlight = false;
            Show();
        }

        public void ShowBlockerWithWorldHighlight(Vector3 worldPos, float radius = 1.0f)
        {
            targetElements.Clear();
            isWorldHighlight = true;
            worldHighlightPos = worldPos;
            worldHighlightRadius = radius;
            Show();
        }

        public void HideBlocker()
        {
            if (!isActive) return;
            canvasGroup.DOFade(0, transitionDuration).OnComplete(() =>
            {
                gameObject.SetActive(false);
                isActive = false;
            });
        }

        private void Show()
        {
            if (overlayGO == null) CreateOverlay();
            
            gameObject.SetActive(true);
            isActive = true;
            
            UpdateOverlayMask();
            overlayRaycaster.SetTargets(targetElements);
            
            canvasGroup.DOKill();
            canvasGroup.DOFade(1, transitionDuration);
        }

        private void CreateOverlay()
        {
            Canvas parentCanvas = GetComponent<Canvas>();
            overlayGO = new GameObject("Overlay_Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HoleRaycaster));
            overlayGO.transform.SetParent(parentCanvas.transform, false);
            
            var rect = overlayGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            overlayImage = overlayGO.GetComponent<Image>();
            overlayImage.material = new Material(overlayMaterial);
            overlayImage.color = overlayColor;
            
            overlayRaycaster = overlayGO.GetComponent<HoleRaycaster>();
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
            else
            {
                foreach (var rt in targetElements)
                {
                    if (rt == null) continue;
                    DrawUIHole(rt, pixels, overlayRect);
                }
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

            int pxMinX = Mathf.RoundToInt(minX * maskSize);
            int pxMinY = Mathf.RoundToInt(minY * maskSize);
            int pxMaxX = Mathf.RoundToInt(maxX * maskSize);
            int pxMaxY = Mathf.RoundToInt(maxY * maskSize);

            for (int y = pxMinY; y < pxMaxY; y++)
            for (int x = pxMinX; x < pxMaxX; x++)
            {
                int idx = y * maskSize + x;
                if (idx >= 0 && idx < pixels.Length) pixels[idx] = new Color32(255, 255, 255, 0);
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
            int pixelRadius = Mathf.RoundToInt(uvRadius * maskSize);

            for (int y = centerY - pixelRadius; y <= centerY + pixelRadius; y++)
            for (int x = centerX - pixelRadius; x <= centerX + pixelRadius; x++)
            {
                if (x < 0 || x >= maskSize || y < 0 || y >= maskSize) continue;
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                if (dist <= pixelRadius)
                {
                    int idx = y * maskSize + x;
                    pixels[idx] = new Color32(255, 255, 255, 0);
                }
            }
        }

        public class HoleRaycaster : Image
        {
            private List<RectTransform> targets = new List<RectTransform>();
            public void SetTargets(List<RectTransform> rects)
            {
                targets.Clear();
                if (rects != null) targets.AddRange(rects);
            }
            public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
            {
                foreach (var target in targets)
                {
                    if (target != null && RectTransformUtility.RectangleContainsScreenPoint(target, screenPoint, eventCamera))
                        return false;
                }
                return true;
            }
        }
    }
}