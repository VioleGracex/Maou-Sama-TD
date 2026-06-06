using UnityEngine;
using System;
using System.Collections.Generic;
using MaouSamaTD.Levels;
using DG.Tweening;
using TMPro;

namespace MaouSamaTD.UI.MainMenu
{
    [System.Serializable]
    public class CampaignMapVisuals
    {
        private CampaignPage _page;
        private Transform _levelContainer;
        private LevelButton _levelButtonPrefab;
        private Sprite _mapSprite;
        private float _maxZoomDistance;
        private UnityEngine.UI.Button _zoomInButton;
        private UnityEngine.UI.Button _zoomOutButton;
        private UnityEngine.UI.Slider _zoomSlider;

        private Sprite _solidCircleSprite;
        private Sprite _glowCircleSprite;

        private List<LevelButton> _spawnedButtons = new List<LevelButton>();
        private Dictionary<LevelData, (LevelButton btn, GameObject glow)> _spawnedLevelNodes = new Dictionary<LevelData, (LevelButton btn, GameObject glow)>();
        private bool _hasInitializedMapPosition = false;
        private int _lastLoadedHash = -1;

        public List<LevelButton> SpawnedButtons => _spawnedButtons;
        public Dictionary<LevelData, (LevelButton btn, GameObject glow)> SpawnedLevelNodes => _spawnedLevelNodes;
        public bool HasInitializedMapPosition 
        { 
            get => _hasInitializedMapPosition; 
            set => _hasInitializedMapPosition = value; 
        }
        public int LastLoadedHash
        {
            get => _lastLoadedHash;
            set => _lastLoadedHash = value;
        }

        public void Initialize(
            CampaignPage page,
            Transform levelContainer,
            LevelButton levelButtonPrefab,
            Sprite mapSprite,
            float maxZoomDistance,
            UnityEngine.UI.Button zoomInButton,
            UnityEngine.UI.Button zoomOutButton,
            UnityEngine.UI.Slider zoomSlider)
        {
            _page = page;
            _levelContainer = levelContainer;
            _levelButtonPrefab = levelButtonPrefab;
            _mapSprite = mapSprite;
            _maxZoomDistance = maxZoomDistance;
            _zoomInButton = zoomInButton;
            _zoomOutButton = zoomOutButton;
            _zoomSlider = zoomSlider;

            // Set max zoom distance dynamically
            if (_levelContainer != null)
            {
                var zp = _levelContainer.GetComponent<CampaignMapZoomPan>();
                if (zp != null)
                {
                    zp.MaxZoom = _maxZoomDistance;
                }
            }

            EnsureZoomButtonsExist();
        }

        public void UpdateZoomSlider()
        {
            if (_zoomSlider != null && _levelContainer != null)
            {
                var zp = _levelContainer.GetComponent<CampaignMapZoomPan>();
                if (zp != null)
                {
                    float currentNorm = zp.GetZoomNormalized();
                    if (Mathf.Abs(_zoomSlider.value - currentNorm) > 0.01f)
                    {
                        _zoomSlider.SetValueWithoutNotify(currentNorm);
                    }
                }
            }
        }

        public void ClearSpawnedNodesAndSplinesEditorTime()
        {
            if (_levelContainer == null) return;

            var children = new List<GameObject>();
            foreach (Transform child in _levelContainer)
            {
                if (child == null) continue;
                string name = child.name;
                if (name.Contains("StageLevel_Prefab") || name.Contains("NodeGlow") || name.Contains("SplineDot"))
                {
                    children.Add(child.gameObject);
                }
            }

            foreach (var go in children)
            {
                if (go != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(go);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(go);
                    }
                }
            }

            _spawnedButtons.Clear();
            _spawnedLevelNodes.Clear();
        }

        public void RefreshMap(List<LevelDisplayData> displayDataList, bool isTabSwap)
        {
            if (!Application.isPlaying)
            {
                ClearSpawnedNodesAndSplinesEditorTime();
                return;
            }

            if (_levelContainer == null || _levelButtonPrefab == null)
            {
                Debug.LogWarning("[CampaignMapVisuals] Missing references! Cannot spawn level buttons.");
                return;
            }

            // Assign map sprite
            var containerImage = _levelContainer.GetComponent<UnityEngine.UI.Image>();
            if (containerImage != null)
            {
                if (_mapSprite != null) containerImage.sprite = _mapSprite;
               
                containerImage.color = Color.white;
            }

            // Identify what needs to be removed
            HashSet<LevelData> targetLevels = new HashSet<LevelData>();
            foreach (var data in displayDataList) targetLevels.Add(data.Level);

            List<LevelData> toRemove = new List<LevelData>();
            foreach (var level in _spawnedLevelNodes.Keys)
            {
                if (!targetLevels.Contains(level)) toRemove.Add(level);
            }

            // Animate out and destroy removed nodes
            foreach (var level in toRemove)
            {
                if (_spawnedLevelNodes.TryGetValue(level, out var tuple))
                {
                    var btn = tuple.btn;
                    var glow = tuple.glow;
                    _spawnedButtons.Remove(btn);

                    if (btn != null)
                    {
                        if (isTabSwap && Application.isPlaying)
                        {
                            btn.transform.DOScale(Vector3.zero, 0.18f).SetEase(Ease.InBack).SetUpdate(true);
                            var cg = btn.GetComponent<CanvasGroup>();
                            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
                            cg.DOFade(0f, 0.15f).SetUpdate(true);
                            DOVirtual.DelayedCall(0.2f, () => { if (btn != null) UnityEngine.Object.Destroy(btn.gameObject); }).SetUpdate(true);
                        }
                        else
                        {
                            if (Application.isPlaying) UnityEngine.Object.Destroy(btn.gameObject);
                            else UnityEngine.Object.DestroyImmediate(btn.gameObject);
                        }
                    }
                    if (glow != null)
                    {
                        if (isTabSwap && Application.isPlaying)
                        {
                            glow.transform.DOScale(Vector3.zero, 0.18f).SetEase(Ease.InBack).SetUpdate(true);
                            var cg = glow.GetComponent<CanvasGroup>();
                            if (cg == null) cg = glow.gameObject.AddComponent<CanvasGroup>();
                            cg.DOFade(0f, 0.15f).SetUpdate(true);
                            DOVirtual.DelayedCall(0.2f, () => { if (glow != null) UnityEngine.Object.Destroy(glow); }).SetUpdate(true);
                        }
                        else
                        {
                            if (Application.isPlaying) UnityEngine.Object.Destroy(glow);
                            else UnityEngine.Object.DestroyImmediate(glow);
                        }
                    }
                }
                _spawnedLevelNodes.Remove(level);
            }

            // Destroy all old splines
            for (int k = _levelContainer.childCount - 1; k >= 0; k--)
            {
                var child = _levelContainer.GetChild(k).gameObject;
                if (child.name == "SplineDot")
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(child);
                    else UnityEngine.Object.DestroyImmediate(child);
                }
            }

            // Spawn newly added nodes
            SpawnLevelNodes(displayDataList, isTabSwap && Application.isPlaying);
        }

        private void SpawnLevelNodes(List<LevelDisplayData> displayDataList, bool animateIn)
        {
            Sprite glowCircle = GetGlowCircleSprite();
            int newlyAddedIndex = 0;

            for (int i = 0; i < displayDataList.Count; i++)
            {
                var data = displayDataList[i];
                if (_spawnedLevelNodes.ContainsKey(data.Level))
                {
                    continue;
                }

                newlyAddedIndex++;

                var btn = UnityEngine.Object.Instantiate(_levelButtonPrefab, _levelContainer);
                btn.gameObject.name = "node_level_" + data.Level.LevelIndex;
                var rect = btn.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.zero;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = data.Level.CampaignMapPosition;
                }

                var circleGo = new GameObject("NodeGlow", typeof(UnityEngine.UI.Image));
                circleGo.transform.SetParent(btn.transform, false);
                var circleImg = circleGo.GetComponent<UnityEngine.UI.Image>();
                if (glowCircle != null) circleImg.sprite = glowCircle;
                Color nodeColor = _page.GetCategoryColorPublic(data.Level.Category);
                circleImg.color = new Color(nodeColor.r, nodeColor.g, nodeColor.b, 0.75f);
                var circleRect = circleGo.GetComponent<RectTransform>();
                circleRect.anchorMin = new Vector2(0.5f, 0.5f);
                circleRect.anchorMax = new Vector2(0.5f, 0.5f);
                circleRect.pivot = new Vector2(0.5f, 0.5f);
                circleRect.anchoredPosition = Vector2.zero;
                circleRect.sizeDelta = new Vector2(110f, 110f);
                circleGo.transform.SetAsFirstSibling();

                btn.Setup(data, (o) => _page.OnLevelClickedPublic(btn, data.Level, !data.IsLocked));
                btn.SetGlow(circleImg, new Color(nodeColor.r, nodeColor.g, nodeColor.b, 0.75f));
                
                _spawnedButtons.Add(btn);
                _spawnedLevelNodes[data.Level] = (btn, circleGo);

                if (animateIn && Application.isPlaying)
                {
                    btn.transform.localScale = Vector3.zero;
                    circleGo.transform.localScale = Vector3.zero;
                    float delay = newlyAddedIndex * 0.04f;
                    btn.transform.DOScale(Vector3.one, 0.25f).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true);
                    circleGo.transform.DOScale(Vector3.one, 0.25f).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(() => {
                        if (circleGo != null && circleImg != null)
                        {
                            circleGo.transform.DOScale(1.15f, 1.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
                            circleImg.DOFade(0.5f, 1.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
                        }
                    });
                }
                else if (Application.isPlaying)
                {
                    circleGo.transform.DOScale(1.15f, 1.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
                    circleImg.DOFade(0.5f, 1.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
                }
            }

            DrawAllSplines(animateIn);

            if (!_hasInitializedMapPosition && displayDataList.Count > 0)
            {
                _hasInitializedMapPosition = true;
                Vector2 centerTarget = displayDataList[0].Level.CampaignMapPosition;
                for (int i = displayDataList.Count - 1; i >= 0; i--)
                {
                    if (!displayDataList[i].IsLocked)
                    {
                        centerTarget = displayDataList[i].Level.CampaignMapPosition;
                        break;
                    }
                }
                _page.CenterScrollOnPosition(centerTarget);
            }
        }

        public void RedrawSplinesOnly()
        {
            for (int k = _levelContainer.childCount - 1; k >= 0; k--)
            {
                var child = _levelContainer.GetChild(k);
                if (child.name == "SplineDot")
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                }
            }

            DrawAllSplines(false);
        }

        private void DrawAllSplines(bool animateIn = false)
        {
            var positions = new Dictionary<LevelData, Vector2>();
            var indexMap = new Dictionary<LevelData, int>();
            for (int idx = 0; idx < _spawnedButtons.Count; idx++)
            {
                var btn = _spawnedButtons[idx];
                if (btn != null && btn.LevelDataForCallback != null)
                {
                    var rect = btn.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        positions[btn.LevelDataForCallback] = rect.anchoredPosition;
                        indexMap[btn.LevelDataForCallback] = idx;
                    }
                }
            }

            bool hasExplicitConnections = false;
            foreach (var btn in _spawnedButtons)
            {
                if (btn != null && btn.LevelDataForCallback != null)
                {
                    var lvl = btn.LevelDataForCallback;
                    if (lvl.ConnectedLevels != null && lvl.ConnectedLevels.Count > 0)
                    {
                        hasExplicitConnections = true;
                        break;
                    }
                }
            }

            var drawnConnections = new HashSet<(string, string)>();

            if (hasExplicitConnections)
            {
                foreach (var btn in _spawnedButtons)
                {
                    if (btn == null || btn.LevelDataForCallback == null) continue;
                    var sourceLvl = btn.LevelDataForCallback;
                    if (!positions.TryGetValue(sourceLvl, out var sourcePos)) continue;
                    if (!indexMap.TryGetValue(sourceLvl, out int sourceIdx)) continue;

                    if (sourceLvl.ConnectedLevels == null) continue;

                    foreach (var targetLvl in sourceLvl.ConnectedLevels)
                    {
                        if (targetLvl == null) continue;
                        if (!positions.TryGetValue(targetLvl, out var targetPos)) continue;

                        string idA = sourceLvl.LevelID;
                        string idB = targetLvl.LevelID;
                        var key = string.Compare(idA, idB, StringComparison.Ordinal) < 0 ? (idA, idB) : (idB, idA);

                        if (!drawnConnections.Contains(key))
                        {
                            DrawConnectionLine(sourcePos, targetPos, _page.GetCategoryColorPublic(sourceLvl.Category), animateIn, sourceIdx * 0.04f);
                            drawnConnections.Add(key);
                        }
                    }
                }
            }
            else
            {
                if (_page.ShowMainStory)
                {
                    List<LevelButton> storyButtons = new List<LevelButton>();
                    foreach (var btn in _spawnedButtons)
                    {
                        if (btn != null && btn.LevelDataForCallback != null && btn.LevelDataForCallback.Category == LevelCategory.MainStory)
                        {
                            storyButtons.Add(btn);
                        }
                    }
                    storyButtons.Sort((a, b) => a.LevelDataForCallback.LevelIndex.CompareTo(b.LevelDataForCallback.LevelIndex));

                    if (storyButtons.Count > 1)
                    {
                        for (int i = 1; i < storyButtons.Count; i++)
                        {
                            var prevBtn = storyButtons[i - 1];
                            var currBtn = storyButtons[i];
                            if (prevBtn != null && currBtn != null &&
                                positions.TryGetValue(prevBtn.LevelDataForCallback, out var prevPos) &&
                                positions.TryGetValue(currBtn.LevelDataForCallback, out var currPos))
                            {
                                DrawConnectionLine(prevPos, currPos, _page.GetCategoryColorPublic(prevBtn.LevelDataForCallback.Category), animateIn, (i - 1) * 0.04f);
                            }
                        }
                    }
                }
            }
        }

        private void DrawConnectionLine(Vector2 start, Vector2 end, Color color, bool animateIn = false, float baseDelay = 0f)
        {
            Vector2 dir = end - start;
            Vector2 perp = new Vector2(-dir.y, dir.x).normalized;
            float dist = dir.magnitude;
            float arcFactor = dist * 0.12f; 
            Vector2 control = (start + end) * 0.5f + perp * arcFactor;

            int numDots = Mathf.Max(5, Mathf.RoundToInt(dist / 22f));
            Sprite circleSprite = GetSolidCircleSprite();

            for (int i = 0; i <= numDots; i++)
            {
                float t = (float)i / numDots;
                Vector2 pos = (1f - t) * (1f - t) * start + 2f * (1f - t) * t * control + t * t * end;

                var dotGo = new GameObject("SplineDot", typeof(UnityEngine.UI.Image));
                dotGo.transform.SetParent(_levelContainer, false);
                dotGo.transform.SetAsFirstSibling();

                var img = dotGo.GetComponent<UnityEngine.UI.Image>();
                img.raycastTarget = false; // Prevent blocking map drag/hover events
                if (circleSprite != null) img.sprite = circleSprite;
                img.color = color;

                var rect = dotGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = pos;
                rect.sizeDelta = new Vector2(8f, 8f);

                if (animateIn && Application.isPlaying)
                {
                    dotGo.transform.localScale = Vector3.zero;
                    float delay = baseDelay + t * 0.15f;
                    dotGo.transform.DOScale(Vector3.one, 0.22f).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true);
                }
            }
        }

        private Sprite GetSolidCircleSprite()
        {
            if (_solidCircleSprite != null) return _solidCircleSprite;
            
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float radius = size / 2f - 0.5f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float edgeWidth = 1.0f;
                        float diff = radius - dist;
                        float alpha = Mathf.Clamp01(diff / edgeWidth);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
            }
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            _solidCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _solidCircleSprite;
        }

        private Sprite GetGlowCircleSprite()
        {
            if (_glowCircleSprite != null) return _glowCircleSprite;
            
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float radius = size / 2f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float normDist = dist / radius;
                        float glowAlpha = Mathf.Clamp01(1f - normDist);
                        glowAlpha = Mathf.Pow(glowAlpha, 2.0f) * 0.7f;
                        
                        float ringAlpha = 0f;
                        if (normDist >= 0.82f && normDist <= 0.9f)
                        {
                            float xNorm = (normDist - 0.82f) / 0.08f;
                            ringAlpha = Mathf.Sin(xNorm * Mathf.PI) * 0.9f;
                        }
                        
                        float finalAlpha = Mathf.Max(glowAlpha, ringAlpha);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, finalAlpha));
                    }
                }
            }
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            _glowCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _glowCircleSprite;
        }

        private void EnsureZoomButtonsExist()
        {
            Transform targetParent = _page.VisualRoot != null ? _page.VisualRoot.transform : _page.transform;

            var zoomContainer = targetParent.Find("ZoomContainer");
            if (zoomContainer == null) zoomContainer = targetParent.Find("VisualRoot/ZoomContainer");
            if (zoomContainer == null) zoomContainer = _page.transform.Find("ZoomContainer");
            
            if (zoomContainer != null)
            {
                if (_zoomInButton == null) _zoomInButton = zoomContainer.Find("ZoomInButton")?.GetComponent<UnityEngine.UI.Button>();
                if (_zoomInButton == null) _zoomInButton = zoomContainer.GetComponentInChildren<UnityEngine.UI.Button>();
                
                if (_zoomOutButton == null) _zoomOutButton = zoomContainer.Find("ZoomOutButton")?.GetComponent<UnityEngine.UI.Button>();
                if (_zoomOutButton == null && _zoomInButton != null)
                {
                    var buttons = zoomContainer.GetComponentsInChildren<UnityEngine.UI.Button>();
                    foreach (var b in buttons)
                    {
                        if (b != _zoomInButton)
                        {
                            _zoomOutButton = b;
                            break;
                        }
                    }
                }

                if (_zoomSlider == null) _zoomSlider = zoomContainer.GetComponentInChildren<UnityEngine.UI.Slider>();
            }

            if (_zoomInButton != null && _zoomOutButton != null && _zoomSlider != null)
            {
                _zoomInButton.onClick.RemoveAllListeners();
                _zoomInButton.onClick.AddListener(OnZoomInClicked);

                _zoomOutButton.onClick.RemoveAllListeners();
                _zoomOutButton.onClick.AddListener(OnZoomOutClicked);

                _zoomSlider.onValueChanged.RemoveAllListeners();
                _zoomSlider.onValueChanged.AddListener((v) => {
                    if (_levelContainer != null)
                    {
                        var zp = _levelContainer.GetComponent<CampaignMapZoomPan>();
                        if (zp != null) zp.SetZoomNormalized(v);
                    }
                });

                return;
            }

            if (zoomContainer == null)
            {
                GameObject containerGo = new GameObject("ZoomContainer", typeof(RectTransform));
                containerGo.transform.SetParent(targetParent, false);
                var rect = containerGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.anchoredPosition = new Vector2(-30f, 0f);
                rect.sizeDelta = new Vector2(44f, 380f);
                zoomContainer = containerGo.transform;
            }

            Color btnBg = new Color(0.08f, 0.1f, 0.14f, 0.95f);
            Color btnGlow = new Color(0.97f, 0.79f, 0.14f, 0.6f);

            UnityEngine.UI.Button MakeBtn(string name, string label, Vector2 anchor, Vector2 pivot, Vector2 pos)
            {
                GameObject go = new GameObject(name, typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                go.transform.SetParent(zoomContainer, false);
                var r = go.GetComponent<RectTransform>();
                r.anchorMin = anchor; r.anchorMax = anchor;
                r.pivot = pivot;
                r.anchoredPosition = pos;
                r.sizeDelta = new Vector2(40f, 40f);
                go.GetComponent<UnityEngine.UI.Image>().color = btnBg;
                var ol = go.AddComponent<UnityEngine.UI.Outline>();
                ol.effectColor = btnGlow; ol.effectDistance = new Vector2(1.5f, 1.5f);
                var txtGo = new GameObject("Text", typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(go.transform, false);
                var tr = txtGo.GetComponent<RectTransform>();
                tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
                var tm = txtGo.GetComponent<TextMeshProUGUI>();
                tm.text = label; tm.alignment = TextAlignmentOptions.Center;
                tm.fontSize = 26f; tm.color = new Color(0.97f, 0.79f, 0.14f, 1f);
                return go.GetComponent<UnityEngine.UI.Button>();
            }

            if (_zoomInButton == null)
            {
                _zoomInButton = MakeBtn("ZoomInButton", "+",
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f));
                _zoomInButton.onClick.AddListener(OnZoomInClicked);
            }

            if (_zoomOutButton == null)
            {
                _zoomOutButton = MakeBtn("ZoomOutButton", "−",
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f));
                _zoomOutButton.onClick.AddListener(OnZoomOutClicked);
            }

            if (_zoomSlider == null)
            {
                GameObject sliderGo = new GameObject("ZoomSlider", typeof(RectTransform), typeof(UnityEngine.UI.Slider));
                sliderGo.transform.SetParent(zoomContainer, false);
                var sr = sliderGo.GetComponent<RectTransform>();
                sr.anchorMin = new Vector2(0.5f, 0f);
                sr.anchorMax = new Vector2(0.5f, 1f);
                sr.pivot = new Vector2(0.5f, 0.5f);
                sr.offsetMin = new Vector2(-10f, 44f);
                sr.offsetMax = new Vector2(10f, -44f);

                var slider = sliderGo.GetComponent<UnityEngine.UI.Slider>();
                slider.direction = UnityEngine.UI.Slider.Direction.BottomToTop;
                slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0.3f;

                GameObject bgGo = new GameObject("Background", typeof(UnityEngine.UI.Image));
                bgGo.transform.SetParent(sliderGo.transform, false);
                var bgR = bgGo.GetComponent<RectTransform>();
                bgR.anchorMin = new Vector2(0.5f, 0f);
                bgR.anchorMax = new Vector2(0.5f, 1f);
                bgR.sizeDelta = new Vector2(14f, 0f);
                bgGo.GetComponent<UnityEngine.UI.Image>().color = new Color(0.15f, 0.18f, 0.25f, 0.9f);
                slider.targetGraphic = bgGo.GetComponent<UnityEngine.UI.Graphic>();

                GameObject fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
                fillAreaGo.transform.SetParent(sliderGo.transform, false);
                var faR = fillAreaGo.GetComponent<RectTransform>();
                faR.anchorMin = new Vector2(0.5f, 0f);
                faR.anchorMax = new Vector2(0.5f, 1f);
                faR.sizeDelta = new Vector2(14f, 0f);

                GameObject fillGo = new GameObject("Fill", typeof(UnityEngine.UI.Image));
                fillGo.transform.SetParent(fillAreaGo.transform, false);
                var fillR = fillGo.GetComponent<RectTransform>();
                fillR.anchorMin = Vector2.zero; fillR.anchorMax = Vector2.one; fillR.sizeDelta = Vector2.zero;
                fillGo.GetComponent<UnityEngine.UI.Image>().color = new Color(0.97f, 0.79f, 0.14f, 0.8f);
                slider.fillRect = fillR;

                GameObject handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
                handleAreaGo.transform.SetParent(sliderGo.transform, false);
                var haR = handleAreaGo.GetComponent<RectTransform>();
                haR.anchorMin = Vector2.zero; haR.anchorMax = Vector2.one; haR.sizeDelta = Vector2.zero;

                GameObject handleGo = new GameObject("Handle", typeof(UnityEngine.UI.Image));
                handleGo.transform.SetParent(handleAreaGo.transform, false);
                var hR = handleGo.GetComponent<RectTransform>();
                hR.sizeDelta = new Vector2(32f, 32f);
                handleGo.GetComponent<UnityEngine.UI.Image>().color = new Color(0.97f, 0.79f, 0.14f, 1f);
                slider.handleRect = hR;

                _zoomSlider = slider;

                slider.onValueChanged.AddListener((v) => {
                    if (_levelContainer != null)
                    {
                        var zp = _levelContainer.GetComponent<CampaignMapZoomPan>();
                        if (zp != null) zp.SetZoomNormalized(v);
                    }
                });
            }
        }

        private void OnZoomInClicked()
        {
            if (_levelContainer != null)
            {
                var zoomPan = _levelContainer.GetComponent<CampaignMapZoomPan>();
                if (zoomPan != null) zoomPan.ZoomIn();
            }
        }

        private void OnZoomOutClicked()
        {
            if (_levelContainer != null)
            {
                var zoomPan = _levelContainer.GetComponent<CampaignMapZoomPan>();
                if (zoomPan != null) zoomPan.ZoomOut();
            }
        }
    }
}
