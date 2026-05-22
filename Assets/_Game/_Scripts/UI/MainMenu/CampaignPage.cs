using UnityEngine;
using System;
using System.Collections.Generic;
using MaouSamaTD.Levels;
using MaouSamaTD.Managers;
using MaouSamaTD.UI.Common;
using Zenject;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;
using DG.Tweening;

namespace MaouSamaTD.UI.MainMenu
{
    public class CampaignPage : MonoBehaviour, IUIController
    {
        [Header("UI Controller Architecture")]
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;
        [SerializeField] private NavigationFeatures _navFeatures = NavigationFeatures.BackButton | NavigationFeatures.CitadelButton;
        public NavigationFeatures ConfiguredNavFeatures => _navFeatures;

        [Header("References")]
        [SerializeField] private Transform _levelContainer;
        [SerializeField] private LevelButton _levelButtonPrefab;
        [System.NonSerialized] private List<LevelData> _allLevels = new List<LevelData>();
        [SerializeField] private BriefingPanel _briefingPanel;
        [SerializeField] private Sprite _mapSprite;

        // Categorized lists
        private List<LevelData> _mainStoryLevels = new List<LevelData>();
        private List<LevelData> _resourceDungeons = new List<LevelData>();
        private List<LevelData> _riteDungeons = new List<LevelData>();
        private List<LevelData> _vassalDungeons = new List<LevelData>();
        private bool _isLevelsLoaded = false;
        private bool _hasInitializedMapPosition = false;
        private int _lastLoadedHash = -1;
        
        [Header("Toggles")]
        [SerializeField] private UnityEngine.UI.Button _mainStoryTabButton;
        [SerializeField] private UnityEngine.UI.Button _resourceDungeonsTabButton;
        [SerializeField] private UnityEngine.UI.Button _specialDungeonsTabButton;
        
        [SerializeField] private bool _showMainStory = true;
        [SerializeField] private bool _showResourceDungeons = false;
        [SerializeField] private bool _showSpecialDungeons = false;
        [SerializeField] private float _maxZoomDistance = 4.0f;

        public bool ShowMainStory => _showMainStory;
        public bool ShowResourceDungeons => _showResourceDungeons;
        public bool ShowSpecialDungeons => _showSpecialDungeons;

        private string _sidebarFilter = "ALL";

        [SerializeField] private MaouSamaTD.UI.Cohorts.CohortSquadUI _cohortSquadUI;
        
        [Inject] private SaveManager _saveManager;

        private GenericListView<LevelDisplayData, LevelButton> _listView;

        private List<LevelButton> _spawnedButtons = new List<LevelButton>();
        private Dictionary<LevelData, (LevelButton btn, GameObject glow)> _spawnedLevelNodes = new Dictionary<LevelData, (LevelButton btn, GameObject glow)>();
        private Canvas _canvas;

        [Header("Sidebar & Navigation UI")]
        [SerializeField] private GameObject _sidebarRoot;
        [SerializeField] private Transform _sidebarContentContainer;
        [SerializeField] private UnityEngine.UI.Button _zoomInButton;
        [SerializeField] private UnityEngine.UI.Button _zoomOutButton;
        [SerializeField] private SidebarLevelItem _sidebarItemPrefab;
        [SerializeField] private Sprite _arrowLeftSprite;
        [SerializeField] private Sprite _arrowRightSprite;


        public Transform LevelContainer => _levelContainer;
        public List<LevelData> AllLevels
        {
            get
            {
                if (_allLevels == null || _allLevels.Count == 0)
                {
#if UNITY_EDITOR
                    // Fallback load in editor
                    var dbGuid = UnityEditor.AssetDatabase.FindAssets("t:LevelDatabase");
                    if (dbGuid.Length > 0)
                    {
                        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(dbGuid[0]);
                        var db = UnityEditor.AssetDatabase.LoadAssetAtPath<MaouSamaTD.Data.LevelDatabase>(path);
                        if (db != null)
                        {
                            _allLevels = new List<LevelData>(db.AllLevels);
                            SeparateLevelsByCategory();
                        }
                    }
#endif
                }
                return _allLevels;
            }
        }
        public List<LevelButton> SpawnedButtons => _spawnedButtons;

        private void OnEnable()
        {
            UpdateTabVisuals();
            Refresh();
        }

        private void Awake()
        {
            // Removed GenericListView to implement custom Node Map layout
            _canvas = GetComponentInParent<Canvas>();
            
            // Dynamic fallback search for _briefingPanel if reference is lost
            if (_briefingPanel == null)
            {
                _briefingPanel = FindObjectOfType<BriefingPanel>(true);
                if (_briefingPanel == null)
                {
                    Debug.LogWarning("[CampaignPage] BriefingPanel was not found in the scene! Click interactions will not show details.");
                }
            }

            if (_mainStoryTabButton != null) _mainStoryTabButton.onClick.AddListener(() => ToggleCategory(LevelCategory.MainStory));
            if (_resourceDungeonsTabButton != null) _resourceDungeonsTabButton.onClick.AddListener(() => ToggleCategory(LevelCategory.ResourceDungeon));
            if (_specialDungeonsTabButton != null) _specialDungeonsTabButton.onClick.AddListener(() => ToggleCategory(LevelCategory.RiteDungeon));

            UpdateTabVisuals();
            EnsureZoomButtonsExist();
            EnsureLeftSidebarExists();

            if (_sidebarContentContainer != null)
            {
                foreach (Transform child in _sidebarContentContainer)
                {
                    if (child != null)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            if (_zoomInButton != null) _zoomInButton.onClick.AddListener(OnZoomInClicked);
            if (_zoomOutButton != null) _zoomOutButton.onClick.AddListener(OnZoomOutClicked);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_sidebarItemPrefab == null)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("StageLevel_Prefab t:GameObject");
                if (guids.Length > 0)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null)
                    {
                        _sidebarItemPrefab = go.GetComponent<SidebarLevelItem>();
                    }
                }
            }
        }
#endif

        private void Start()
        {
            Debug.Log("[CampaignPage]start");
            _showMainStory = true;
            _showResourceDungeons = false;
            _showSpecialDungeons = false;

            // Set max zoom distance dynamically from inspector field
            if (_levelContainer != null)
            {
                var zp = _levelContainer.GetComponent<CampaignMapZoomPan>();
                if (zp != null)
                {
                    zp.MaxZoom = _maxZoomDistance;
                }
            }

            UpdateTabVisuals();
            Refresh();
        }

        public void ToggleCategory(LevelCategory category)
        {
            switch (category)
            {
                case LevelCategory.MainStory:
                    _showMainStory = !_showMainStory;
                    break;
                case LevelCategory.ResourceDungeon:
                    _showResourceDungeons = !_showResourceDungeons;
                    break;
                case LevelCategory.RiteDungeon:
                case LevelCategory.VassalDungeon:
                    _showSpecialDungeons = !_showSpecialDungeons;
                    break;
            }

            // Fallback comfort: keep at least one category visible
            if (!_showMainStory && !_showResourceDungeons && !_showSpecialDungeons)
            {
                switch (category)
                {
                    case LevelCategory.MainStory:
                        _showMainStory = true;
                        break;
                    case LevelCategory.ResourceDungeon:
                        _showResourceDungeons = true;
                        break;
                    default:
                        _showSpecialDungeons = true;
                        break;
                }
            }

            UpdateTabVisuals();
            Refresh();
        }

        public void Open()
        {
            _hasInitializedMapPosition = false; // Reset map initialization so it centers on active node on open
            if (_visualRoot != null) _visualRoot.SetActive(true);
            UpdateTabVisuals();
            Refresh();
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            // Explicitly close sub-panels/overlays when the main page closes
            if (_briefingPanel != null) _briefingPanel.Close();
            if (_cohortSquadUI != null) _cohortSquadUI.Close();
        }

        public bool RequestClose() => true;

        public void Preheat()
        {
            EnsureLevelsLoaded(() => {
                Debug.Log($"[CampaignPage] Preheated: {_allLevels?.Count ?? 0} levels loaded.");
            });
            
            // Validate save manager status
            if (_saveManager != null)
            {
                var data = _saveManager.CurrentData;
                Debug.Log($"[CampaignPage] Preheating: Save data loaded. Player: {data?.PlayerName}");
            }
        }

        public void ResetState()
        {
            if (_briefingPanel != null)
            {
                // Resetting scrollbars or similar visual states can go here in the future.
            }
        }

        private void Update()
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

        private void EnsureLevelsLoaded(System.Action onComplete)
        {
            if (_isLevelsLoaded)
            {
                onComplete?.Invoke();
                return;
            }

            if (MaouSamaTD.Core.AppEntryPoint.LoadedLevelDatabase != null &&
                MaouSamaTD.Core.AppEntryPoint.LoadedLevelDatabase.AllLevels != null &&
                MaouSamaTD.Core.AppEntryPoint.LoadedLevelDatabase.AllLevels.Count > 0)
            {
                _allLevels = new List<LevelData>(MaouSamaTD.Core.AppEntryPoint.LoadedLevelDatabase.AllLevels);
                SeparateLevelsByCategory();
                _isLevelsLoaded = true;
                onComplete?.Invoke();
                return;
            }

            Addressables.LoadAssetsAsync<LevelData>("LevelData", null).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _allLevels = new List<LevelData>(handle.Result);
                    _allLevels.Sort((a, b) => a.LevelIndex.CompareTo(b.LevelIndex));
                    SeparateLevelsByCategory();
                    _isLevelsLoaded = true;
                    onComplete?.Invoke();
                }
                else
                {
#if UNITY_EDITOR
                    // Fallback to Editor loading
                    var dbGuid = UnityEditor.AssetDatabase.FindAssets("t:LevelDatabase");
                    if (dbGuid.Length > 0)
                    {
                        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(dbGuid[0]);
                        var db = UnityEditor.AssetDatabase.LoadAssetAtPath<MaouSamaTD.Data.LevelDatabase>(path);
                        if (db != null)
                        {
                            _allLevels = new List<LevelData>(db.AllLevels);
                            SeparateLevelsByCategory();
                            _isLevelsLoaded = true;
                            onComplete?.Invoke();
                            return;
                        }
                    }
#endif
                    Debug.LogError("[CampaignPage] Failed to load levels from Addressables!");
                    onComplete?.Invoke();
                }
            };
        }

        private void SeparateLevelsByCategory()
        {
            _mainStoryLevels.Clear();
            _resourceDungeons.Clear();
            _riteDungeons.Clear();
            _vassalDungeons.Clear();

            if (_allLevels == null) return;

            var seenStoryIds = new HashSet<string>();
            foreach (var level in _allLevels)
            {
                if (level == null) continue;

                switch (level.Category)
                {
                    case LevelCategory.MainStory:
                        // "dont allow same level to be repeated twice for story type levels"
                        if (!seenStoryIds.Contains(level.LevelID))
                        {
                            seenStoryIds.Add(level.LevelID);
                            _mainStoryLevels.Add(level);
                        }
                        break;
                    case LevelCategory.ResourceDungeon:
                        _resourceDungeons.Add(level);
                        break;
                    case LevelCategory.RiteDungeon:
                        _riteDungeons.Add(level);
                        break;
                    case LevelCategory.VassalDungeon:
                        _vassalDungeons.Add(level);
                        break;
                }
            }
        }

        public void Refresh()
        {
            EnsureLevelsLoaded(() => {
                DoRefresh();
            });
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
                        Destroy(go);
                    }
                    else
                    {
                        DestroyImmediate(go);
                    }
                }
            }

            _spawnedButtons.Clear();
            _spawnedLevelNodes.Clear();
        }

        private void DoRefresh()
        {
            if (!Application.isPlaying)
            {
                ClearSpawnedNodesAndSplinesEditorTime();
                return;
            }

            if (_levelContainer == null || _levelButtonPrefab == null || _allLevels == null || _allLevels.Count == 0)
            {
                Debug.LogWarning("[CampaignPage] Missing references or levels! Cannot spawn level buttons.");
                return;
            }

            // Assign map sprite
            var containerImage = _levelContainer.GetComponent<UnityEngine.UI.Image>();
            if (containerImage != null)
            {
                if (_mapSprite != null) containerImage.sprite = _mapSprite;
                else
                {
#if UNITY_EDITOR
                    _mapSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Gehenna.png");
                    if (_mapSprite != null) containerImage.sprite = _mapSprite;
#endif
                }
                containerImage.color = Color.white;
            }

            // Ensure tabs render on top of map container
            EnsureTabsOnTop();

            Debug.Log($"[CampaignPage] Starting Refresh (Toggles Mode), Total levels: {_allLevels.Count}");

            List<LevelDisplayData> displayDataList = new List<LevelDisplayData>();

            // Aggregate levels from all active/toggled categories
            if (_showMainStory)
            {
                for (int i = 0; i < _mainStoryLevels.Count; i++)
                {
                    LevelData level = _mainStoryLevels[i];
                    if (level == null) continue;
                    displayDataList.Add(new LevelDisplayData
                    {
                        Level = level, Index = i,
                        IsLocked = !IsLevelUnlocked(level, i, _mainStoryLevels),
                        StarCount = GetLevelStars(level)
                    });
                }
            }
            if (_showResourceDungeons)
            {
                for (int i = 0; i < _resourceDungeons.Count; i++)
                {
                    LevelData level = _resourceDungeons[i];
                    if (level == null) continue;
                    displayDataList.Add(new LevelDisplayData
                    {
                        Level = level, Index = i,
                        IsLocked = !IsLevelUnlocked(level, i, _resourceDungeons),
                        StarCount = GetLevelStars(level)
                    });
                }
            }
            if (_showSpecialDungeons)
            {
                List<LevelData> specialList = new List<LevelData>(_riteDungeons);
                specialList.AddRange(_vassalDungeons);
                for (int i = 0; i < specialList.Count; i++)
                {
                    LevelData level = specialList[i];
                    if (level == null) continue;
                    displayDataList.Add(new LevelDisplayData
                    {
                        Level = level, Index = i,
                        IsLocked = !IsLevelUnlocked(level, i, specialList),
                        StarCount = GetLevelStars(level)
                    });
                }
            }

            int activeCategoriesHash = (_showMainStory ? 1 : 0) | (_showResourceDungeons ? 2 : 0) | (_showSpecialDungeons ? 4 : 0);
            bool isTabSwap = _lastLoadedHash != -1 && _lastLoadedHash != activeCategoriesHash;
            _lastLoadedHash = activeCategoriesHash;

            // SMART DIFF REFRESH LOGIC
            HashSet<LevelData> targetLevels = new HashSet<LevelData>();
            foreach (var data in displayDataList) targetLevels.Add(data.Level);

            // Identify what needs to be removed
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
                            DOVirtual.DelayedCall(0.2f, () => { if (btn != null) Destroy(btn.gameObject); }).SetUpdate(true);
                        }
                        else
                        {
                            if (Application.isPlaying) Destroy(btn.gameObject);
                            else DestroyImmediate(btn.gameObject);
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
                            DOVirtual.DelayedCall(0.2f, () => { if (glow != null) Destroy(glow); }).SetUpdate(true);
                        }
                        else
                        {
                            if (Application.isPlaying) Destroy(glow);
                            else DestroyImmediate(glow);
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
                    if (Application.isPlaying) Destroy(child);
                    else DestroyImmediate(child);
                }
            }

            // Spawn newly added nodes
            SpawnLevelNodes(displayDataList, isTabSwap && Application.isPlaying);
        }

        private void SpawnLevelNodes(List<LevelDisplayData> displayDataList, bool animateIn)
        {
            // Load glow circle sprite
            Sprite glowCircle = GetGlowCircleSprite();

            int newlyAddedIndex = 0;

            for (int i = 0; i < displayDataList.Count; i++)
            {
                var data = displayDataList[i];
                
                // If it's already spawned, skip recreation entirely!
                if (_spawnedLevelNodes.ContainsKey(data.Level))
                {
                    continue;
                }

                newlyAddedIndex++;

                // Add glow circle background under the node for visibility
                var circleGo = new GameObject("NodeGlow", typeof(UnityEngine.UI.Image));
                circleGo.transform.SetParent(_levelContainer, false);
                var circleImg = circleGo.GetComponent<UnityEngine.UI.Image>();
                if (glowCircle != null) circleImg.sprite = glowCircle;
                Color nodeColor = GetCategoryColor(data.Level.Category);
                circleImg.color = new Color(nodeColor.r, nodeColor.g, nodeColor.b, 0.75f);
                var circleRect = circleGo.GetComponent<RectTransform>();
                circleRect.anchorMin = Vector2.zero;
                circleRect.anchorMax = Vector2.zero;
                circleRect.pivot = new Vector2(0.5f, 0.5f);
                circleRect.anchoredPosition = data.Level.CampaignMapPosition;
                circleRect.sizeDelta = new Vector2(110f, 110f); // 110x110 gives a gorgeous glowing aura around the node!
                circleGo.transform.SetAsFirstSibling();

                var btn = Instantiate(_levelButtonPrefab, _levelContainer);
                var rect = btn.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.zero;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = data.Level.CampaignMapPosition;
                }

                btn.Setup(data, (o) => OnLevelClicked(data.Level, !data.IsLocked));
                _spawnedButtons.Add(btn);
                _spawnedLevelNodes[data.Level] = (btn, circleGo);

                if (animateIn && Application.isPlaying)
                {
                    btn.transform.localScale = Vector3.zero;
                    circleGo.transform.localScale = Vector3.zero;
                    float delay = newlyAddedIndex * 0.04f;
                    btn.transform.DOScale(Vector3.one, 0.25f).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true);
                    circleGo.transform.DOScale(Vector3.one, 0.25f).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(() => {
                        // Start pulsing the glow circle!
                        if (circleGo != null && circleImg != null)
                        {
                            circleGo.transform.DOScale(1.15f, 1.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
                            circleImg.DOFade(0.5f, 1.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
                        }
                    });
                }
                else if (Application.isPlaying)
                {
                    // Start pulsing immediately if not animating in
                    circleGo.transform.DOScale(1.15f, 1.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
                    circleImg.DOFade(0.5f, 1.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
                }
            }

            DrawAllSplines(animateIn);

            // Only auto-center on very first load, not tab swaps
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
                CenterScrollOnPosition(centerTarget);
            }

            RefreshLeftSidebar();
        }

        private void EnsureTabsOnTop()
        {
            if (_mainStoryTabButton != null)
                _mainStoryTabButton.transform.SetAsLastSibling();
            if (_resourceDungeonsTabButton != null)
                _resourceDungeonsTabButton.transform.SetAsLastSibling();
            if (_specialDungeonsTabButton != null)
                _specialDungeonsTabButton.transform.SetAsLastSibling();

            // Bring the tab row container and all its ancestors up to the direct child of _visualRoot to the front
            var tabParent = _mainStoryTabButton?.transform.parent;
            if (tabParent != null && tabParent != _levelContainer)
            {
                tabParent.SetAsLastSibling();
                
                // Recursively move up the hierarchy and call SetAsLastSibling on each ancestor until we reach _visualRoot
                Transform current = tabParent;
                Transform root = _visualRoot != null ? _visualRoot.transform : transform;
                while (current != null && current.parent != null && current.parent != root && current.parent != current)
                {
                    current.parent.SetAsLastSibling();
                    current = current.parent;
                }
            }
        }

        public void RedrawSplinesOnly()
        {
            // Clear only the SplineDot objects under _levelContainer to optimize drag Performance
            for (int k = _levelContainer.childCount - 1; k >= 0; k--)
            {
                var child = _levelContainer.GetChild(k);
                if (child.name == "SplineDot")
                {
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
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

            // Check if any active level has explicit connections configured
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
                        var key = string.Compare(idA, idB, System.StringComparison.Ordinal) < 0 ? (idA, idB) : (idB, idA);

                        if (!drawnConnections.Contains(key))
                        {
                            DrawConnectionLine(sourcePos, targetPos, GetCategoryColor(sourceLvl.Category), animateIn, sourceIdx * 0.04f);
                            drawnConnections.Add(key);
                        }
                    }
                }
            }
            else
            {
                // Fallback to sequential main story drawing exclusively between story stages
                if (_showMainStory)
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
                                DrawConnectionLine(prevPos, currPos, GetCategoryColor(prevBtn.LevelDataForCallback.Category), animateIn, (i - 1) * 0.04f);
                            }
                        }
                    }
                }
            }
        }

        private Color GetCategoryColor(LevelCategory category)
        {
            switch (category)
            {
                case LevelCategory.MainStory:
                    return new Color(0.1f, 0.8f, 1.0f, 0.9f); // Premium Glowing Cyan
                case LevelCategory.ResourceDungeon:
                    return new Color(1.0f, 0.75f, 0.15f, 0.9f); // Premium Glowing Amber
                case LevelCategory.RiteDungeon:
                    return new Color(0.85f, 0.35f, 1.0f, 0.9f); // Premium Glowing Purple
                case LevelCategory.VassalDungeon:
                    return new Color(1.0f, 0.3f, 0.3f, 0.9f); // Premium Glowing Red
                default:
                    return new Color(1.0f, 1.0f, 1.0f, 0.9f);
            }
        }

        private void DrawConnectionLine(Vector2 start, Vector2 end, Color color, bool animateIn = false, float baseDelay = 0f)
        {
            // Perpendicular vector to create a beautiful curved arc
            Vector2 dir = end - start;
            Vector2 perp = new Vector2(-dir.y, dir.x).normalized;
            
            // Curved offset (amplitude based on distance to make it look uniform)
            float dist = dir.magnitude;
            float arcFactor = dist * 0.12f; 
            Vector2 control = (start + end) * 0.5f + perp * arcFactor;

            // Generate spline segments using small round dots
            int numDots = Mathf.Max(5, Mathf.RoundToInt(dist / 22f));
            Sprite circleSprite = GetSolidCircleSprite();

            for (int i = 0; i <= numDots; i++)
            {
                float t = (float)i / numDots;
                
                // Quadratic Bezier Curve formula
                Vector2 pos = (1f - t) * (1f - t) * start + 2f * (1f - t) * t * control + t * t * end;

                var dotGo = new GameObject("SplineDot", typeof(UnityEngine.UI.Image));
                dotGo.transform.SetParent(_levelContainer, false);
                dotGo.transform.SetAsFirstSibling(); // Draw behind nodes

                var img = dotGo.GetComponent<UnityEngine.UI.Image>();
                if (circleSprite != null) img.sprite = circleSprite;
                
                // Set the premium category color
                img.color = color;

                var rect = dotGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = pos;
                rect.sizeDelta = new Vector2(8f, 8f); // Beautiful thick glowing dots!

                if (animateIn && Application.isPlaying)
                {
                    dotGo.transform.localScale = Vector3.zero;
                    float delay = baseDelay + t * 0.15f;
                    dotGo.transform.DOScale(Vector3.one, 0.22f).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true);
                }
            }
        }



        private void UpdateTabVisuals()
        {
            // Visual state is baked in the scene (editor-mode). At runtime we only nudge
            // the image alpha to signal which tab is "pressed / active" vs inactive.
            // No color, outline, or font overrides here — they break the Scene view.
            ApplyTabActiveAlpha(_mainStoryTabButton, _showMainStory);
            ApplyTabActiveAlpha(_resourceDungeonsTabButton, _showResourceDungeons);
            ApplyTabActiveAlpha(_specialDungeonsTabButton, _showSpecialDungeons);
        }

        /// <summary>
        /// Pure runtime feedback only: fade the image alpha to show active/inactive state.
        /// All base colors, outlines, and text styles are authored once in the Unity Editor
        /// via execute_code and saved into the scene — do NOT set them here.
        /// </summary>
        private void ApplyTabActiveAlpha(UnityEngine.UI.Button button, bool isActive)
        {
            if (button == null) return;
            var img = button.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                // Active = fully opaque baked Crimson; Inactive = semi-transparent dark glass
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, isActive ? 1f : 0.80f);
            }
        }

        public void CenterScrollOnPosition(Vector2 position)
        {
            if (_levelContainer != null)
            {
                var zoomPan = _levelContainer.GetComponent<CampaignMapZoomPan>();
                if (zoomPan != null)
                {
                    zoomPan.FocusOnPosition(position);
                    return;
                }
            }

            var scrollRect = GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            if (scrollRect == null || scrollRect.content == null) return;

            var viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
            if (viewport == null) return;

            Vector2 viewportSize = viewport.rect.size;
            if (viewportSize.x <= 0f || viewportSize.y <= 0f)
            {
                // Fallback for uninitialized UI size (use standard reference)
                viewportSize = new Vector2(1920f, 1080f);
            }

            Vector2 targetContentPos = -position + (viewportSize / 2f);

            // Clamp content position to bounds of the content RectTransform
            Vector2 contentSize = scrollRect.content.rect.size;
            if (contentSize.x <= 0f) contentSize.x = 2048f;
            if (contentSize.y <= 0f) contentSize.y = 1143f;

            float minX = viewportSize.x - contentSize.x;
            float maxX = 0f;
            float minY = viewportSize.y - contentSize.y;
            float maxY = 0f;

            if (contentSize.x < viewportSize.x) targetContentPos.x = -contentSize.x / 2f + viewportSize.x / 2f;
            else targetContentPos.x = Mathf.Clamp(targetContentPos.x, minX, maxX);

            if (contentSize.y < viewportSize.y) targetContentPos.y = -contentSize.y / 2f + viewportSize.y / 2f;
            else targetContentPos.y = Mathf.Clamp(targetContentPos.y, minY, maxY);

            scrollRect.content.anchoredPosition = targetContentPos;
        }

        public bool IsLevelUnlocked(LevelData level, int index, List<LevelData> list)
        {
            if (_saveManager == null) return false; // Fallback if SaveManager is missing

            if (level != null)
            {
                if (level.RequiredUnitLevel > 1 && _saveManager.GetHighestUnitLevel() < level.RequiredUnitLevel)
                    return false;
                
                if (level.RequiredPreviousLevel != null && !_saveManager.IsLevelCompleted(level.RequiredPreviousLevel.LevelID))
                    return false;
            }

            if (index == 0) return true; // First level always unlocked, assuming explicit reqs pass
            
            if (list == null || index < 0 || index >= list.Count) return false;
            var prevLevel = list[index - 1];
            if (prevLevel == null) return false;
            
            return _saveManager.IsLevelCompleted(prevLevel.LevelID);
        }

        public bool IsLevelLockedInUI(LevelData level)
        {
            if (_spawnedButtons == null) return false;
            foreach (var btn in _spawnedButtons)
            {
                if (btn != null && btn.LevelDataForCallback == level)
                {
                    return btn.IsLocked;
                }
            }
            return false;
        }
        
        private int GetLevelStars(LevelData level)
        {
             if (level == null || _saveManager == null || _saveManager.CurrentData == null) return 0;

             var entry = _saveManager.CurrentData.LevelStars.Find(x => x.LevelID == level.LevelID);
             // Verify if we actually found it (default struct check)
             if (entry.LevelID == level.LevelID) return entry.Stars;
             return 0;
        }

        private void OnLevelClicked(LevelData level, bool isUnlocked)
        {
            // Open Briefing as a popup window
            if (_briefingPanel != null)
            {
                MaouSamaTD.UI.UIFlowManager.Instance.OpenPanel(_briefingPanel);
                _briefingPanel.Setup(level, isUnlocked, OnBriefingEngage);
            }
            else
            {
                Debug.LogWarning("[CampaignPage] Briefing Panel is null! Using fallback.");
                // Fallback direct
                OnBriefingEngage(level);
            }
        }
        private void OnBriefingEngage(LevelData level)
        {
            if (_cohortSquadUI != null)
            {
                // Give cohortSquadUI history priority so it hides campaign
                MaouSamaTD.UI.UIFlowManager.Instance.OpenPanel(_cohortSquadUI);

                // Ensure the scripts (CohortSquadUI, etc.) aren't deactivated when CampaignPage closes.
                GameObject readinessManager = _cohortSquadUI.gameObject;
                if (readinessManager.transform.parent != null && readinessManager.transform.parent.gameObject == gameObject)
                {
                    readinessManager.transform.SetParent(transform.parent, true);
                }

                Transform parent = transform.parent;
                if (parent != null)
                {
                    foreach (Transform child in transform)
                    {
                        if (child.GetComponent<IUIController>() != null)
                        {
                            child.SetParent(parent, true);
                        }
                    }
                }

                // Call OpenReadiness to initialize pre-battle constraints
                _cohortSquadUI.OpenReadiness(level);
            }
            else
            {
                Debug.LogError("[CampaignPage] Cohort Manager UI is not assigned in CampaignPage!");
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

        [SerializeField] private UnityEngine.UI.Slider _zoomSlider;

        private Sprite _solidCircleSprite;
        private Sprite _glowCircleSprite;

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
                        
                        // 1. Soft radial falloff for the glow
                        float glowAlpha = Mathf.Clamp01(1f - normDist);
                        glowAlpha = Mathf.Pow(glowAlpha, 2.0f) * 0.7f;
                        
                        // 2. Crisp outer ring around normDist = 0.82 to 0.9
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
            Transform targetParent = _visualRoot != null ? _visualRoot.transform : transform;

            // Try to find in Hierarchy first
            var zoomContainer = targetParent.Find("ZoomContainer");
            if (zoomContainer == null) zoomContainer = targetParent.Find("VisualRoot/ZoomContainer");
            if (zoomContainer == null) zoomContainer = transform.Find("ZoomContainer");
            
            if (zoomContainer != null)
            {
                if (_zoomInButton == null) _zoomInButton = zoomContainer.Find("ZoomInButton")?.GetComponent<UnityEngine.UI.Button>();
                if (_zoomInButton == null) _zoomInButton = zoomContainer.GetComponentInChildren<UnityEngine.UI.Button>(); // Find first button
                
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
                // Layout is baked in the scene — do NOT override sizeDelta, offsets,
                // handle size, fill width, or track width here. Only wire up events.

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

                return; // successfully wired existing hierarchy zoom controls!
            }

            // Build dynamic ZoomContainer as a robust fallback in editor if not already present
            if (zoomContainer == null)
            {
                GameObject containerGo = new GameObject("ZoomContainer", typeof(RectTransform));
                containerGo.transform.SetParent(targetParent, false);
                var rect = containerGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.anchoredPosition = new Vector2(-30f, 0f);
                rect.sizeDelta = new Vector2(44f, 380f); // Make the zoom slider container much longer (380f instead of 220f)
                zoomContainer = containerGo.transform;
            }

            Color btnBg = new Color(0.08f, 0.1f, 0.14f, 0.95f); // Semi-transparent dark circular button
            Color btnGlow = new Color(0.97f, 0.79f, 0.14f, 0.6f); // Maou Gold outline

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
                tm.fontSize = 26f; tm.color = new Color(0.97f, 0.79f, 0.14f, 1f); // Maou Gold color tint
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

            // Vertical slider between buttons
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

                // Background
                GameObject bgGo = new GameObject("Background", typeof(UnityEngine.UI.Image));
                bgGo.transform.SetParent(sliderGo.transform, false);
                var bgR = bgGo.GetComponent<RectTransform>();
                bgR.anchorMin = new Vector2(0.5f, 0f);
                bgR.anchorMax = new Vector2(0.5f, 1f);
                bgR.sizeDelta = new Vector2(14f, 0f); // Thicker track
                bgGo.GetComponent<UnityEngine.UI.Image>().color = new Color(0.15f, 0.18f, 0.25f, 0.9f);
                slider.targetGraphic = bgGo.GetComponent<UnityEngine.UI.Graphic>();

                // Fill area
                GameObject fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
                fillAreaGo.transform.SetParent(sliderGo.transform, false);
                var faR = fillAreaGo.GetComponent<RectTransform>();
                faR.anchorMin = new Vector2(0.5f, 0f);
                faR.anchorMax = new Vector2(0.5f, 1f);
                faR.sizeDelta = new Vector2(14f, 0f); // Thicker fill area

                GameObject fillGo = new GameObject("Fill", typeof(UnityEngine.UI.Image));
                fillGo.transform.SetParent(fillAreaGo.transform, false);
                var fillR = fillGo.GetComponent<RectTransform>();
                fillR.anchorMin = Vector2.zero; fillR.anchorMax = Vector2.one; fillR.sizeDelta = Vector2.zero;
                fillGo.GetComponent<UnityEngine.UI.Image>().color = new Color(0.97f, 0.79f, 0.14f, 0.8f); // Maou Gold fill
                slider.fillRect = fillR;

                // Handle
                GameObject handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
                handleAreaGo.transform.SetParent(sliderGo.transform, false);
                var haR = handleAreaGo.GetComponent<RectTransform>();
                haR.anchorMin = Vector2.zero; haR.anchorMax = Vector2.one; haR.sizeDelta = Vector2.zero;

                GameObject handleGo = new GameObject("Handle", typeof(UnityEngine.UI.Image));
                handleGo.transform.SetParent(handleAreaGo.transform, false);
                var hR = handleGo.GetComponent<RectTransform>();
                hR.sizeDelta = new Vector2(32f, 32f); // Large handle size
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

        private void EnsureLeftSidebarExists()
        {
            Transform targetParent = _visualRoot != null ? _visualRoot.transform : transform;

            if (_sidebarRoot == null)
            {
                var existing = targetParent.Find("LeftSideber");
                if (existing == null) existing = targetParent.Find("LeftSidebar");
                if (existing == null) existing = transform.Find("LeftSideber");
                if (existing == null) existing = transform.Find("LeftSidebar");
                if (existing != null)
                {
                    _sidebarRoot = existing.gameObject;
                }
            }

            if (_sidebarRoot != null)
            {
                var sidebarRect = _sidebarRoot.GetComponent<RectTransform>();
                if (sidebarRect != null)
                {
                    sidebarRect.anchorMin = new Vector2(0f, 0f);
                    sidebarRect.anchorMax = new Vector2(0f, 1f);
                    sidebarRect.pivot = new Vector2(0f, 0.5f);
                    sidebarRect.offsetMin = new Vector2(0f, 115f);
                    sidebarRect.offsetMax = new Vector2(300f, -115f);
                }

                if (_sidebarContentContainer == null)
                {
                    _sidebarContentContainer = _sidebarRoot.transform.Find("ScrollView/Viewport/Content");
                    if (_sidebarContentContainer == null)
                        _sidebarContentContainer = _sidebarRoot.transform.Find("Viewport/Content");
                    if (_sidebarContentContainer == null)
                        _sidebarContentContainer = _sidebarRoot.GetComponentInChildren<UnityEngine.UI.VerticalLayoutGroup>()?.transform;
                    if (_sidebarContentContainer == null)
                        _sidebarContentContainer = _sidebarRoot.transform;
                }

                // Adjust position of FiltersContainer and ScrollView in pre-placed sidebar to avoid overlapping with top overlay bar
                var filtersTrans = _sidebarRoot.transform.Find("FiltersContainer");
                if (filtersTrans == null) filtersTrans = _sidebarRoot.transform.Find("TabsContainer");
                if (filtersTrans != null)
                {
                    var filtersRect = filtersTrans.GetComponent<RectTransform>();
                    if (filtersRect != null)
                    {
                        filtersRect.anchoredPosition = new Vector2(filtersRect.anchoredPosition.x, -90f);
                    }
                }
                var scrollTrans = _sidebarRoot.transform.Find("ScrollView");
                if (scrollTrans != null)
                {
                    var scrollRect = scrollTrans.GetComponent<RectTransform>();
                    if (scrollRect != null)
                    {
                        scrollRect.offsetMax = new Vector2(scrollRect.offsetMax.x, -140f);
                    }
                }

                // Check if filters are already set up on the pre-placed sidebar buttons
                var buttons = _sidebarRoot.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                foreach (var btn in buttons)
                {
                    string btnName = btn.gameObject.name.ToUpper();
                    if (btnName.Contains("ALL"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SetSidebarFilter("ALL"));
                    }
                    else if (btnName.Contains("UNLOCKED"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SetSidebarFilter("UNLOCKED"));
                    }
                    else if (btnName.Contains("CLEARED") || btnName.Contains("COMPLETE"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SetSidebarFilter("CLEARED"));
                    }
                }
            }
            else
            {
                // Build dynamic LeftSidebar as a fallback if not pre-placed
                GameObject sidebarGo = new GameObject("LeftSidebar", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                sidebarGo.transform.SetParent(targetParent, false);
                var rect = sidebarGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.offsetMin = new Vector2(0f, 115f);
                rect.offsetMax = new Vector2(300f, -115f);

                var img = sidebarGo.GetComponent<UnityEngine.UI.Image>();
                img.color = new Color(0.08f, 0.1f, 0.14f, 0.95f);

                var outline = sidebarGo.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = new Color(0.97f, 0.79f, 0.14f, 0.5f);
                outline.effectDistance = new Vector2(2f, 2f);

                _sidebarRoot = sidebarGo;

                GameObject tabsGo = new GameObject("FiltersContainer", typeof(RectTransform), typeof(UnityEngine.UI.HorizontalLayoutGroup));
                tabsGo.transform.SetParent(sidebarGo.transform, false);
                var tabsRect = tabsGo.GetComponent<RectTransform>();
                tabsRect.anchorMin = new Vector2(0f, 1f);
                tabsRect.anchorMax = new Vector2(1f, 1f);
                tabsRect.pivot = new Vector2(0.5f, 1f);
                tabsRect.anchoredPosition = new Vector2(0f, -90f); // Pushed down to clear top overlay
                tabsRect.sizeDelta = new Vector2(-20f, 35f);

                var tabsLayout = tabsGo.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                tabsLayout.spacing = 5f;
                tabsLayout.childControlWidth = true;
                tabsLayout.childControlHeight = true;
                tabsLayout.childForceExpandWidth = true;
                tabsLayout.childForceExpandHeight = true;

                CreateFilterButton(tabsGo.transform, "ALL");
                CreateFilterButton(tabsGo.transform, "UNLOCKED");
                CreateFilterButton(tabsGo.transform, "CLEARED");

                GameObject scrollViewGo = new GameObject("ScrollView", typeof(RectTransform), typeof(UnityEngine.UI.ScrollRect));
                scrollViewGo.transform.SetParent(sidebarGo.transform, false);
                var scrollRect = scrollViewGo.GetComponent<RectTransform>();
                scrollRect.anchorMin = new Vector2(0f, 0f);
                scrollRect.anchorMax = new Vector2(1f, 1f);
                scrollRect.pivot = new Vector2(0.5f, 0.5f);
                scrollRect.offsetMin = new Vector2(10f, 10f);
                scrollRect.offsetMax = new Vector2(-10f, -140f); // Pushed down to clear top overlay

                var sr = scrollViewGo.GetComponent<UnityEngine.UI.ScrollRect>();
                sr.horizontal = false;
                sr.vertical = true;

                GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Mask));
                viewportGo.transform.SetParent(scrollViewGo.transform, false);
                var viewRect = viewportGo.GetComponent<RectTransform>();
                viewRect.anchorMin = Vector2.zero;
                viewRect.anchorMax = Vector2.one;
                viewRect.sizeDelta = Vector2.zero;
                viewportGo.GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0.1f);
                viewportGo.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;

                sr.viewport = viewRect;

                GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup), typeof(UnityEngine.UI.ContentSizeFitter));
                contentGo.transform.SetParent(viewportGo.transform, false);
                var contentRect = contentGo.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(0f, 0f);

                var vlg = contentGo.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                vlg.spacing = 8f;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.padding = new RectOffset(5, 5, 5, 5);

                var csf = contentGo.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                csf.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

                sr.content = contentRect;
                _sidebarContentContainer = contentRect;
            }

            // Ensure HamburgerToggle toggle button exists on LeftSidebar
            if (_sidebarRoot != null)
            {
                var rect = _sidebarRoot.GetComponent<RectTransform>();
                var toggleTrans = _sidebarRoot.transform.Find("HamburgerToggle");
                UnityEngine.UI.Button toggleBtn = null;
                TextMeshProUGUI tText = null;
                UnityEngine.UI.Image toggleImg = null;

                if (toggleTrans == null)
                {
                    GameObject toggleGo = new GameObject("HamburgerToggle", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                    toggleGo.transform.SetParent(_sidebarRoot.transform, false);
                    var toggleRect = toggleGo.GetComponent<RectTransform>();
                    toggleRect.anchorMin = new Vector2(1f, 0.5f);
                    toggleRect.anchorMax = new Vector2(1f, 0.5f);
                    toggleRect.pivot = new Vector2(0f, 0.5f);
                    toggleRect.anchoredPosition = new Vector2(5f, 0f);
                    toggleRect.sizeDelta = new Vector2(40f, 40f);

                    toggleImg = toggleGo.GetComponent<UnityEngine.UI.Image>();
                    toggleImg.color = Color.white;
                    
                    var toggleOutline = toggleGo.AddComponent<UnityEngine.UI.Outline>();
                    toggleOutline.effectColor = new Color(0f, 0.8f, 1f, 0.6f);
                    toggleOutline.effectDistance = new Vector2(1f, 1f);

                    GameObject toggleTextGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    toggleTextGo.transform.SetParent(toggleGo.transform, false);
                    var tTextRect = toggleTextGo.GetComponent<RectTransform>();
                    tTextRect.anchorMin = Vector2.zero;
                    tTextRect.anchorMax = Vector2.one;
                    tTextRect.sizeDelta = Vector2.zero;
                    tText = toggleTextGo.GetComponent<TextMeshProUGUI>();
                    tText.text = "◀";
                    tText.alignment = TextAlignmentOptions.Center;
                    tText.fontSize = 20f;
                    tText.color = new Color(0f, 0.8f, 1f, 1f);

                    toggleBtn = toggleGo.GetComponent<UnityEngine.UI.Button>();
                }
                else
                {
                    toggleBtn = toggleTrans.GetComponent<UnityEngine.UI.Button>();
                    toggleImg = toggleTrans.GetComponent<UnityEngine.UI.Image>();
                    tText = toggleTrans.Find("Label")?.GetComponent<TextMeshProUGUI>();
                    if (tText == null) tText = toggleTrans.GetComponentInChildren<TextMeshProUGUI>();
                }

                if (_arrowLeftSprite == null)
                {
#if UNITY_EDITOR
                    _arrowLeftSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI_Pages/256x256/ic_arrow_left.png");
#endif
                }
                if (_arrowRightSprite == null)
                {
#if UNITY_EDITOR
                    _arrowRightSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI_Pages/256x256/ic_arrow_right.png");
#endif
                }

                if (toggleBtn != null)
                {
                    toggleBtn.onClick.RemoveAllListeners();
                    bool isExpanded = true;
                    // Reset to expanded state initially
                    rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
                    
                    if (tText != null) tText.gameObject.SetActive(false); // Hide legacy text label!
                    
                    if (toggleImg != null && _arrowLeftSprite != null)
                    {
                        toggleImg.sprite = _arrowLeftSprite;
                        toggleImg.color = Color.white;
                        toggleImg.transform.localScale = Vector3.one;
                    }

                    toggleBtn.onClick.AddListener(() => {
                        isExpanded = !isExpanded;
                        float targetX = isExpanded ? 0f : -300f;
                        
                        if (toggleImg != null)
                        {
                            toggleImg.transform.localScale = new Vector3(isExpanded ? 1f : -1f, 1f, 1f);
                        }
                        
                        rect.DOAnchorPosX(targetX, 0.35f).SetEase(Ease.OutQuad).SetUpdate(true);
                    });
                }
            }
        }

        private void CreateFilterButton(Transform parent, string filterType)
        {
            GameObject btnGo = new GameObject($"FilterBtn_{filterType}", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            btnGo.transform.SetParent(parent, false);

            var img = btnGo.GetComponent<UnityEngine.UI.Image>();
            img.color = _sidebarFilter == filterType ? new Color(0.92f, 0.3f, 0.29f, 0.9f) : new Color(0.08f, 0.1f, 0.14f, 0.85f);

            var outline = btnGo.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = _sidebarFilter == filterType ? new Color(0.97f, 0.79f, 0.14f, 0.8f) : new Color(0.97f, 0.79f, 0.14f, 0.15f);
            outline.effectDistance = new Vector2(1f, 1f);

            GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);
            var txtRect = txtGo.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            var tm = txtGo.GetComponent<TextMeshProUGUI>();
            tm.text = filterType;
            tm.fontSize = 11f;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = _sidebarFilter == filterType ? Color.white : new Color(0.7f, 0.8f, 0.9f, 0.9f);

            var btn = btnGo.GetComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(() => {
                SetSidebarFilter(filterType);
            });
        }

        private void SetSidebarFilter(string filterType)
        {
            _sidebarFilter = filterType;
            
            if (_sidebarRoot != null)
            {
                var buttons = _sidebarRoot.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                foreach (var btn in buttons)
                {
                    string btnName = btn.gameObject.name.ToUpper();
                    bool isMatched = false;
                    if (btnName.Contains("ALL") && filterType == "ALL") isMatched = true;
                    else if (btnName.Contains("UNLOCKED") && filterType == "UNLOCKED") isMatched = true;
                    else if ((btnName.Contains("CLEARED") || btnName.Contains("COMPLETE")) && filterType == "CLEARED") isMatched = true;

                    var img = btn.GetComponent<UnityEngine.UI.Image>();
                    var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (img != null)
                    {
                        img.color = isMatched ? new Color(0.92f, 0.3f, 0.29f, 0.95f) : new Color(0.08f, 0.1f, 0.14f, 0.9f);
                        var outline = btn.GetComponent<UnityEngine.UI.Outline>();
                        if (outline == null) outline = btn.gameObject.AddComponent<UnityEngine.UI.Outline>();
                        outline.effectColor = isMatched ? new Color(0.97f, 0.79f, 0.14f, 0.8f) : new Color(0.97f, 0.79f, 0.14f, 0.15f);
                        outline.effectDistance = new Vector2(1.5f, 1.5f);
                    }
                    if (txt != null)
                    {
                        txt.color = isMatched ? Color.white : new Color(0.7f, 0.8f, 0.9f, 0.7f);
                    }
                }
            }

            RefreshLeftSidebar();
        }

        private void RefreshLeftSidebar()
        {
            EnsureLeftSidebarExists();

            if (_sidebarContentContainer == null) return;

            // Clear old sidebar items
            System.Collections.Generic.List<Transform> childrenToDestroy = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in _sidebarContentContainer)
            {
                childrenToDestroy.Add(child);
            }
            foreach (var child in childrenToDestroy)
            {
                if (child != null)
                {
                    child.gameObject.SetActive(false);
                    child.SetParent(null);
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }

            if (!Application.isPlaying) return;

            if (_allLevels == null) return;

            // Populate all levels in the active toggled categories
            List<(LevelData level, int originalIndex, List<LevelData> originalList)> targetList = new List<(LevelData, int, List<LevelData>)>();
            
            if (_showMainStory)
            {
                for (int i = 0; i < _mainStoryLevels.Count; i++)
                {
                    targetList.Add((_mainStoryLevels[i], i, _mainStoryLevels));
                }
            }
            if (_showResourceDungeons)
            {
                for (int i = 0; i < _resourceDungeons.Count; i++)
                {
                    targetList.Add((_resourceDungeons[i], i, _resourceDungeons));
                }
            }
            if (_showSpecialDungeons)
            {
                List<LevelData> specialList = new List<LevelData>(_riteDungeons);
                specialList.AddRange(_vassalDungeons);
                for (int i = 0; i < specialList.Count; i++)
                {
                    targetList.Add((specialList[i], i, specialList));
                }
            }

            for (int i = 0; i < targetList.Count; i++)
            {
                var entry = targetList[i];
                var level = entry.level;
                if (level == null) continue;

                // Check placement
                bool isPlaced = level.CampaignMapPosition != Vector2.zero && level.CampaignMapPosition != new Vector2(1024f, 571f);
                bool isUnlocked = IsLevelUnlocked(level, entry.originalIndex, entry.originalList);
                bool isCompleted = _saveManager != null && _saveManager.IsLevelCompleted(level.LevelID);

                // Sidebar filtering check
                if (_sidebarFilter == "UNLOCKED" && !isUnlocked) continue;
                if (_sidebarFilter == "CLEARED" && !isCompleted) continue;

                // Single-click handling logic to center map and open briefing
                Action clickAction = () => {
                    if (isPlaced)
                    {
                        CenterScrollOnPosition(level.CampaignMapPosition);
                        var mapBtn = _spawnedButtons.Find(b => b != null && b.LevelDataForCallback == level);
                        if (mapBtn != null)
                        {
                            OnLevelClicked(level, isUnlocked);
                        }
                    }
                };

                // Use the designer's styled prefab if assigned in the Inspector
                if (_sidebarItemPrefab != null)
                {
                    SidebarLevelItem item = Instantiate(_sidebarItemPrefab, _sidebarContentContainer);
                    item.Setup(level, isUnlocked, isPlaced, isCompleted, clickAction);
                }
                else
                {
                    // Fallback to procedurally generated item if no prefab is assigned
                    GameObject itemGo = new GameObject($"SidebarItem_{level.LevelID}", typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                    itemGo.transform.SetParent(_sidebarContentContainer, false);

                    var itemRect = itemGo.GetComponent<RectTransform>();
                    itemRect.sizeDelta = new Vector2(0f, 65f); // Increased size to fit stars beautifully

                    var itemImg = itemGo.GetComponent<UnityEngine.UI.Image>();
                    itemImg.color = new Color(0.15f, 0.15f, 0.18f, 0.85f);

                    // Left accent bar
                    GameObject accentGo = new GameObject("Accent", typeof(UnityEngine.UI.Image));
                    accentGo.transform.SetParent(itemGo.transform, false);
                    var accentRect = accentGo.GetComponent<RectTransform>();
                    accentRect.sizeDelta = new Vector2(4f, 0f);
                    accentRect.anchorMin = new Vector2(0f, 0f);
                    accentRect.anchorMax = new Vector2(0f, 1f);
                    accentRect.pivot = new Vector2(0f, 0.5f);
                    accentRect.anchoredPosition = Vector2.zero;
                    accentGo.GetComponent<UnityEngine.UI.Image>().color = isPlaced ? GetCategoryColor(level.Category) : new Color(0.5f, 0.5f, 0.5f, 0.5f);

                    // Name Text
                    GameObject nameGo = new GameObject("Text", typeof(TextMeshProUGUI));
                    nameGo.transform.SetParent(itemGo.transform, false);
                    var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
                    nameTmp.text = $"{LevelButton.FormatLevelID(level.LevelID)} {level.LevelName}";
                    nameTmp.fontSize = 13;
                    nameTmp.alignment = TextAlignmentOptions.TopLeft;
                    nameTmp.color = isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.7f);

                    var nameRect = nameGo.GetComponent<RectTransform>();
                    nameRect.anchorMin = new Vector2(0f, 0f);
                    nameRect.anchorMax = new Vector2(1f, 1f);
                    nameRect.pivot = new Vector2(0.5f, 0.5f);
                    nameRect.offsetMin = new Vector2(15f, 20f);
                    nameRect.offsetMax = new Vector2(-40f, -5f);

                    // High-fidelity Star Ratings rendering in Sidebar fallback
                    GameObject starHolderGo = new GameObject("Sidebar_StarHolder", typeof(RectTransform), typeof(UnityEngine.UI.HorizontalLayoutGroup));
                    starHolderGo.transform.SetParent(itemGo.transform, false);
                    var starRect = starHolderGo.GetComponent<RectTransform>();
                    starRect.anchorMin = new Vector2(0f, 0f);
                    starRect.anchorMax = new Vector2(1f, 0f);
                    starRect.pivot = new Vector2(0.5f, 0f);
                    starRect.anchoredPosition = new Vector2(15f, 4f);
                    starRect.sizeDelta = new Vector2(-55f, 10f);

                    var hlg = starHolderGo.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                    hlg.spacing = 2f;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = false;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = false;

                    if (isUnlocked)
                    {
                        int starsCount = 0;
                        if (_saveManager != null && _saveManager.CurrentData != null)
                        {
                            if (_saveManager.CurrentData.LevelStars != null)
                            {
                                var starData = _saveManager.CurrentData.LevelStars.Find(s => s.LevelID == level.LevelID);
                                if (starData.LevelID != null)
                                {
                                    starsCount = starData.Stars;
                                }
                            }
                            
                            if (starsCount == 0 && _saveManager.CurrentData.CompletedLevels != null && _saveManager.CurrentData.CompletedLevels.Contains(level.LevelID))
                            {
                                starsCount = 3;
                            }
                        }

                        for (int sIndex = 0; sIndex < 3; sIndex++)
                        {
                            GameObject starGo = new GameObject($"Star_{sIndex}", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                            starGo.transform.SetParent(starHolderGo.transform, false);
                            var img = starGo.GetComponent<UnityEngine.UI.Image>();
                            var sRect = starGo.GetComponent<RectTransform>();
                            sRect.sizeDelta = new Vector2(10f, 10f);

#if UNITY_EDITOR
                            var fullSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icons/UI_Icon_Star_Full.png");
                            var emptySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icons/UI_Icon_Star_Empty.png");
                            img.sprite = (sIndex < starsCount) ? fullSprite : emptySprite;
#endif
                        }
                    }

                    // Status Icon (Lock / Completed Check / Pin Placement)
                    GameObject statusGo = new GameObject("StatusIcon", typeof(TextMeshProUGUI));
                    statusGo.transform.SetParent(itemGo.transform, false);
                    var statusTmp = statusGo.GetComponent<TextMeshProUGUI>();

                    string statusText = "";
                    if (isCompleted)
                    {
                        statusText = "<color=#E6B800>[OK]</color>";
                    }
                    else if (!isUnlocked)
                    {
                        statusText = "<color=#777777>[L]</color>";
                    }
                    else
                    {
                        statusText = isPlaced ? "<color=#00CCFF>></color>" : "";
                    }

                    statusTmp.text = statusText;
                    statusTmp.fontSize = 16;
                    statusTmp.alignment = TextAlignmentOptions.Center;

                    var statusRect = statusGo.GetComponent<RectTransform>();
                    statusRect.sizeDelta = new Vector2(30f, 30f);
                    statusRect.anchorMin = new Vector2(1f, 0.5f);
                    statusRect.anchorMax = new Vector2(1f, 0.5f);
                    statusRect.pivot = new Vector2(1f, 0.5f);
                    statusRect.anchoredPosition = new Vector2(-5f, 0f);

                    var btn = itemGo.GetComponent<UnityEngine.UI.Button>();
                    btn.onClick.AddListener(() => clickAction());
                }
            }
        }
    }
}

