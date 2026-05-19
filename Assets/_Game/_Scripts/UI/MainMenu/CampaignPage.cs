using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Levels;
using MaouSamaTD.Managers;
using MaouSamaTD.UI.Common;
using Zenject;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
        
        [Header("Tabs")]
        [SerializeField] private UnityEngine.UI.Button _mainStoryTabButton;
        [SerializeField] private UnityEngine.UI.Button _resourceDungeonsTabButton;
        [SerializeField] private UnityEngine.UI.Button _specialDungeonsTabButton;
        private LevelCategory _currentTab = LevelCategory.MainStory;

        [SerializeField] private MaouSamaTD.UI.Cohorts.CohortSquadUI _cohortSquadUI;
        
        [Inject] private SaveManager _saveManager;

        private GenericListView<LevelDisplayData, LevelButton> _listView;

        private List<LevelButton> _spawnedButtons = new List<LevelButton>();
        private Canvas _canvas;

        // Left sidebar procedurally-created fields
        private GameObject _sidebarRoot;
        private Transform _sidebarContentContainer;

        // Dynamic Zoom buttons
        private UnityEngine.UI.Button _zoomInButton;
        private UnityEngine.UI.Button _zoomOutButton;

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
            
            if (_mainStoryTabButton != null) _mainStoryTabButton.onClick.AddListener(() => SelectTab(LevelCategory.MainStory));
            if (_resourceDungeonsTabButton != null) _resourceDungeonsTabButton.onClick.AddListener(() => SelectTab(LevelCategory.ResourceDungeon));
            
            // For now, mapping both Rite and Vassal dungeons to the "Special Dungeons" tab if clicked
            if (_specialDungeonsTabButton != null) _specialDungeonsTabButton.onClick.AddListener(() => SelectTab(LevelCategory.RiteDungeon));

            UpdateTabVisuals();
            EnsureZoomButtonsExist();
            EnsureLeftSidebarExists();

            if (_zoomInButton != null) _zoomInButton.onClick.AddListener(OnZoomInClicked);
            if (_zoomOutButton != null) _zoomOutButton.onClick.AddListener(OnZoomOutClicked);
        }

        private void Start()
        {
            Debug.Log("[CampaignPage]start");
            SelectTab(LevelCategory.MainStory);
        }

        public void SelectTab(LevelCategory category)
        {
            _currentTab = category;
            UpdateTabVisuals();
            Refresh();
        }

        public void Open()
        {
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

        private void DoRefresh()
        {
            if (_levelContainer == null || _levelButtonPrefab == null || _allLevels == null || _allLevels.Count == 0)
            {
                Debug.LogWarning("[CampaignPage] Missing references or levels! Cannot spawn level buttons.");
                return;
            }

            // Dynamically assign the map sprite to the LevelContainer's Image
            var containerImage = _levelContainer.GetComponent<UnityEngine.UI.Image>();
            if (containerImage != null)
            {
                if (_mapSprite != null)
                {
                    containerImage.sprite = _mapSprite;
                }
                else
                {
#if UNITY_EDITOR
                    // Fallback to loading the default Gehenna map sprite in Editor if not assigned
                    _mapSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Gehenna.png");
                    if (_mapSprite != null)
                    {
                        containerImage.sprite = _mapSprite;
                    }
#endif
                }
                containerImage.color = Color.white;
            }

            Debug.Log($"[CampaignPage] Starting Refresh. Total levels: {_allLevels.Count}");

            List<LevelDisplayData> displayDataList = new List<LevelDisplayData>();
            
            List<LevelData> targetList = null;
            if (_currentTab == LevelCategory.MainStory) targetList = _mainStoryLevels;
            else if (_currentTab == LevelCategory.ResourceDungeon) targetList = _resourceDungeons;
            else if (_currentTab == LevelCategory.RiteDungeon)
            {
                // Rite & Vassal dungeons combined under special dungeons tab
                targetList = new List<LevelData>(_riteDungeons);
                targetList.AddRange(_vassalDungeons);
            }

            if (targetList != null)
            {
                for (int i = 0; i < targetList.Count; i++)
                {
                    LevelData level = targetList[i];
                    if (level == null) continue;

                    displayDataList.Add(new LevelDisplayData
                    {
                        Level = level,
                        Index = i,
                        IsLocked = !IsLevelUnlocked(level, i, targetList),
                        StarCount = GetLevelStars(level)
                    });
                }
            }

            // Manual instantiation for Node Map layout
            for (int k = _levelContainer.childCount - 1; k >= 0; k--)
            {
                if (Application.isPlaying)
                {
                    Destroy(_levelContainer.GetChild(k).gameObject);
                }
                else
                {
                    DestroyImmediate(_levelContainer.GetChild(k).gameObject);
                }
            }

            _spawnedButtons.Clear();

            for (int i = 0; i < displayDataList.Count; i++)
            {
                var data = displayDataList[i];
                var btn = Instantiate(_levelButtonPrefab, _levelContainer);
                
                var rect = btn.GetComponent<RectTransform>();
                if (rect != null)
                {
                    // Place at coordinate absolute on LevelContainer 2D Map
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.zero;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = data.Level.CampaignMapPosition;
                }
                
                btn.Setup(data, (o) => OnLevelClicked(data.Level));

                _spawnedButtons.Add(btn);
            }

            // Draw all splines and connections
            DrawAllSplines();

            // Center ScrollRect on active progression level
            if (displayDataList.Count > 0)
            {
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

            DrawAllSplines();
        }

        private void DrawAllSplines()
        {
            var positions = new Dictionary<LevelData, Vector2>();
            foreach (var btn in _spawnedButtons)
            {
                if (btn != null && btn.LevelDataForCallback != null)
                {
                    var rect = btn.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        positions[btn.LevelDataForCallback] = rect.anchoredPosition;
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
                            DrawConnectionLine(sourcePos, targetPos, GetCategoryColor(sourceLvl.Category));
                            drawnConnections.Add(key);
                        }
                    }
                }
            }
            else
            {
                // Fallback to sequential main story drawing
                if (_currentTab == LevelCategory.MainStory && _spawnedButtons.Count > 1)
                {
                    for (int i = 1; i < _spawnedButtons.Count; i++)
                    {
                        var prevBtn = _spawnedButtons[i - 1];
                        var currBtn = _spawnedButtons[i];
                        if (prevBtn != null && currBtn != null &&
                            positions.TryGetValue(prevBtn.LevelDataForCallback, out var prevPos) &&
                            positions.TryGetValue(currBtn.LevelDataForCallback, out var currPos))
                        {
                            DrawConnectionLine(prevPos, currPos, GetCategoryColor(prevBtn.LevelDataForCallback.Category));
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

        private void DrawConnectionLine(Vector2 start, Vector2 end, Color color)
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
            Sprite circleSprite = null;
#if UNITY_EDITOR
            circleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icons/Circle.png");
#endif

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
            }
        }



        private void UpdateTabVisuals()
        {
            SetTabActiveVisuals(_mainStoryTabButton, _currentTab == LevelCategory.MainStory);
            SetTabActiveVisuals(_resourceDungeonsTabButton, _currentTab == LevelCategory.ResourceDungeon);
            SetTabActiveVisuals(_specialDungeonsTabButton, _currentTab == LevelCategory.RiteDungeon || _currentTab == LevelCategory.VassalDungeon);
        }

        private void SetTabActiveVisuals(UnityEngine.UI.Button button, bool isActive)
        {
            if (button == null) return;
            
            var img = button.GetComponent<UnityEngine.UI.Image>();
            var tmp = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            
            if (isActive)
            {
                // Active premium style: glowing warm gold/ember border/background, bold white text
                if (img != null) img.color = new Color(0.9f, 0.45f, 0.1f, 1f); 
                if (tmp != null)
                {
                    tmp.color = Color.white;
                    tmp.fontStyle = TMPro.FontStyles.Bold;
                }
            }
            else
            {
                // Inactive dim style: dark-glass gray background, semi-transparent gray text
                if (img != null) img.color = new Color(0.15f, 0.15f, 0.17f, 0.8f);
                if (tmp != null)
                {
                    tmp.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
                    tmp.fontStyle = TMPro.FontStyles.Normal;
                }
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

        private bool IsLevelUnlocked(LevelData level, int index, List<LevelData> list)
        {
            if (index == 0) return true; // First level always unlocked
            
            if (list == null || index < 0 || index >= list.Count) return false;
            var prevLevel = list[index - 1];
            if (prevLevel == null) return false;
            
            if (_saveManager == null) return false; // Fallback if SaveManager is missing
            
            return _saveManager.IsLevelCompleted(prevLevel.LevelID);
        }
        
        private int GetLevelStars(LevelData level)
        {
             if (level == null || _saveManager == null || _saveManager.CurrentData == null) return 0;

             var entry = _saveManager.CurrentData.LevelStars.Find(x => x.LevelID == level.LevelID);
             // Verify if we actually found it (default struct check)
             if (entry.LevelID == level.LevelID) return entry.Stars;
             return 0;
        }

        private void OnLevelClicked(LevelData level)
        {
            // Open Briefing as a popup window
            if (_briefingPanel != null)
            {
                MaouSamaTD.UI.UIFlowManager.Instance.OpenPanel(_briefingPanel);
                _briefingPanel.Setup(level, OnBriefingEngage);
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

        private void EnsureZoomButtonsExist()
        {
            if (_zoomInButton != null && _zoomOutButton != null) return;

            // Try to find them first
            _zoomInButton = transform.Find("ZoomInButton")?.GetComponent<UnityEngine.UI.Button>();
            _zoomOutButton = transform.Find("ZoomOutButton")?.GetComponent<UnityEngine.UI.Button>();

            if (_zoomInButton != null && _zoomOutButton != null) return;

            // Create Zoom Container
            GameObject zoomContainer = new GameObject("ZoomContainer", typeof(RectTransform));
            zoomContainer.transform.SetParent(transform, false);

            var containerRect = zoomContainer.GetComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(60f, 130f);
            containerRect.anchorMin = new Vector2(1f, 0f);
            containerRect.anchorMax = new Vector2(1f, 0f);
            containerRect.pivot = new Vector2(1f, 0f);
            containerRect.anchoredPosition = new Vector2(-20f, 20f); // Bottom-right corner

            // Create Zoom In
            if (_zoomInButton == null)
            {
                GameObject zoomInGo = new GameObject("ZoomInButton", typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                zoomInGo.transform.SetParent(zoomContainer.transform, false);
                _zoomInButton = zoomInGo.GetComponent<UnityEngine.UI.Button>();

                GameObject txtGo = new GameObject("Text", typeof(TMPro.TextMeshProUGUI));
                txtGo.transform.SetParent(zoomInGo.transform, false);
                var tmp = txtGo.GetComponent<TMPro.TextMeshProUGUI>();
                tmp.text = "+";
                tmp.fontSize = 28;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.color = Color.white;

                var rect = zoomInGo.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(50f, 50f);
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, 0f);

                var txtRect = txtGo.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.sizeDelta = Vector2.zero;

                var img = zoomInGo.GetComponent<UnityEngine.UI.Image>();
                img.color = new Color(0.15f, 0.15f, 0.17f, 0.8f);
            }

            // Create Zoom Out
            if (_zoomOutButton == null)
            {
                GameObject zoomOutGo = new GameObject("ZoomOutButton", typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                zoomOutGo.transform.SetParent(zoomContainer.transform, false);
                _zoomOutButton = zoomOutGo.GetComponent<UnityEngine.UI.Button>();

                GameObject txtGo = new GameObject("Text", typeof(TMPro.TextMeshProUGUI));
                txtGo.transform.SetParent(zoomOutGo.transform, false);
                var tmp = txtGo.GetComponent<TMPro.TextMeshProUGUI>();
                tmp.text = "-";
                tmp.fontSize = 28;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.color = Color.white;

                var rect = zoomOutGo.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(50f, 50f);
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 0f);

                var txtRect = txtGo.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.sizeDelta = Vector2.zero;

                var img = zoomOutGo.GetComponent<UnityEngine.UI.Image>();
                img.color = new Color(0.15f, 0.15f, 0.17f, 0.8f);
            }
        }

        private void EnsureLeftSidebarExists()
        {
            if (_sidebarRoot != null) return;

            // Check if there is already a LeftSidebar
            var existing = transform.Find("LeftSidebar");
            if (existing != null)
            {
                _sidebarRoot = existing.gameObject;
                _sidebarContentContainer = _sidebarRoot.transform.Find("ScrollView/Viewport/Content");
                return;
            }

            // Create LeftSidebar panel
            _sidebarRoot = new GameObject("LeftSidebar", typeof(UnityEngine.UI.Image));
            _sidebarRoot.transform.SetParent(transform, false);
            _sidebarRoot.transform.SetAsLastSibling(); // Draw in front

            var sidebarRect = _sidebarRoot.GetComponent<RectTransform>();
            sidebarRect.sizeDelta = new Vector2(340f, 0f);
            sidebarRect.anchorMin = new Vector2(0f, 0f);
            sidebarRect.anchorMax = new Vector2(0f, 1f);
            sidebarRect.pivot = new Vector2(0f, 0.5f);
            sidebarRect.anchoredPosition = new Vector2(0f, 0f);

            var sidebarImg = _sidebarRoot.GetComponent<UnityEngine.UI.Image>();
            sidebarImg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f); // Rich dark glassmorphism

            // Add thin elegant border on the right
            GameObject borderGo = new GameObject("RightBorder", typeof(UnityEngine.UI.Image));
            borderGo.transform.SetParent(_sidebarRoot.transform, false);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.sizeDelta = new Vector2(2f, 0f);
            borderRect.anchorMin = new Vector2(1f, 0f);
            borderRect.anchorMax = new Vector2(1f, 1f);
            borderRect.pivot = new Vector2(1f, 0.5f);
            borderRect.anchoredPosition = Vector2.zero;
            borderGo.GetComponent<UnityEngine.UI.Image>().color = new Color(0.9f, 0.45f, 0.1f, 0.4f); // Warm glowing gold line

            // Add Header Title
            GameObject titleGo = new GameObject("SidebarTitle", typeof(TMPro.TextMeshProUGUI));
            titleGo.transform.SetParent(_sidebarRoot.transform, false);
            var titleTmp = titleGo.GetComponent<TMPro.TextMeshProUGUI>();
            titleTmp.text = "DEMONIC CAMPAIGNS";
            titleTmp.fontSize = 20;
            titleTmp.alignment = TMPro.TextAlignmentOptions.Center;
            titleTmp.fontStyle = TMPro.FontStyles.Bold;
            titleTmp.color = new Color(0.9f, 0.45f, 0.1f, 1f); // Warm gold

            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(320f, 40f);
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -20f);

            // Create Scroll View for items
            GameObject scrollViewGo = new GameObject("ScrollView", typeof(UnityEngine.UI.ScrollRect));
            scrollViewGo.transform.SetParent(_sidebarRoot.transform, false);
            var scrollRect = scrollViewGo.GetComponent<UnityEngine.UI.ScrollRect>();

            var scrollRectTransform = scrollViewGo.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.offsetMin = new Vector2(10f, 10f);
            scrollRectTransform.offsetMax = new Vector2(-10f, -70f); // Leave room for title at top

            // Viewport
            GameObject viewportGo = new GameObject("Viewport", typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Mask));
            viewportGo.transform.SetParent(scrollViewGo.transform, false);
            viewportGo.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            viewportGo.GetComponent<UnityEngine.UI.Image>().color = Color.clear;

            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;

            // Content
            GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup), typeof(UnityEngine.UI.ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            _sidebarContentContainer = contentGo.transform;

            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);

            var layout = contentGo.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(5, 5, 5, 5);

            var fitter = contentGo.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
        }

        private void RefreshLeftSidebar()
        {
            EnsureLeftSidebarExists();

            // Clear old sidebar items
            foreach (Transform child in _sidebarContentContainer)
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

            if (_allLevels == null) return;

            // Populate all levels in the current category
            List<LevelData> targetList = null;
            if (_currentTab == LevelCategory.MainStory) targetList = _mainStoryLevels;
            else if (_currentTab == LevelCategory.ResourceDungeon) targetList = _resourceDungeons;
            else if (_currentTab == LevelCategory.RiteDungeon)
            {
                targetList = new List<LevelData>(_riteDungeons);
                targetList.AddRange(_vassalDungeons);
            }

            if (targetList == null) return;

            for (int i = 0; i < targetList.Count; i++)
            {
                var level = targetList[i];
                if (level == null) continue;

                // Check placement
                bool isPlaced = level.CampaignMapPosition != Vector2.zero && level.CampaignMapPosition != new Vector2(1024f, 571f);
                bool isUnlocked = IsLevelUnlocked(level, i, targetList);
                bool isCompleted = _saveManager != null && _saveManager.IsLevelCompleted(level.LevelID);

                // Create Sidebar Item
                GameObject itemGo = new GameObject($"SidebarItem_{level.LevelID}", typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                itemGo.transform.SetParent(_sidebarContentContainer, false);

                var itemRect = itemGo.GetComponent<RectTransform>();
                itemRect.sizeDelta = new Vector2(0f, 55f);

                var itemImg = itemGo.GetComponent<UnityEngine.UI.Image>();
                itemImg.color = new Color(0.15f, 0.15f, 0.18f, 0.85f); // Beautiful dark glass list item

                // Left accent bar showing color based on placement status
                GameObject accentGo = new GameObject("Accent", typeof(UnityEngine.UI.Image));
                accentGo.transform.SetParent(itemGo.transform, false);
                var accentRect = accentGo.GetComponent<RectTransform>();
                accentRect.sizeDelta = new Vector2(4f, 0f);
                accentRect.anchorMin = new Vector2(0f, 0f);
                accentRect.anchorMax = new Vector2(0f, 1f);
                accentRect.pivot = new Vector2(0f, 0.5f);
                accentRect.anchoredPosition = Vector2.zero;
                accentGo.GetComponent<UnityEngine.UI.Image>().color = isPlaced ? new Color(0.1f, 0.8f, 1.0f, 0.8f) : new Color(0.5f, 0.5f, 0.5f, 0.5f);

                // Name Text
                GameObject nameGo = new GameObject("Text", typeof(TMPro.TextMeshProUGUI));
                nameGo.transform.SetParent(itemGo.transform, false);
                var nameTmp = nameGo.GetComponent<TMPro.TextMeshProUGUI>();
                nameTmp.text = $"{level.LevelID} {level.LevelName}";
                nameTmp.fontSize = 14;
                nameTmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                nameTmp.color = isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.7f);

                var nameRect = nameGo.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0f, 0f);
                nameRect.anchorMax = new Vector2(1f, 1f);
                nameRect.pivot = new Vector2(0.5f, 0.5f);
                nameRect.offsetMin = new Vector2(15f, 0f);
                nameRect.offsetMax = new Vector2(-40f, 0f);

                // Icon for Placed & Completed
                GameObject statusGo = new GameObject("StatusIcon", typeof(TMPro.TextMeshProUGUI));
                statusGo.transform.SetParent(itemGo.transform, false);
                var statusTmp = statusGo.GetComponent<TMPro.TextMeshProUGUI>();
                
                string statusText = "";
                if (isCompleted)
                {
                    statusText = "<color=#E6B800>✔</color>"; // Gold check
                }
                else if (!isUnlocked)
                {
                    statusText = "<color=#777777>🔒</color>"; // Lock
                }
                else
                {
                    statusText = isPlaced ? "<color=#00CCFF>📍</color>" : "<color=#888888>◌</color>"; // Placed (Cyan Pin) vs Unplaced (Dot)
                }

                statusTmp.text = statusText;
                statusTmp.fontSize = 16;
                statusTmp.alignment = TMPro.TextAlignmentOptions.Center;

                var statusRect = statusGo.GetComponent<RectTransform>();
                statusRect.sizeDelta = new Vector2(30f, 30f);
                statusRect.anchorMin = new Vector2(1f, 0.5f);
                statusRect.anchorMax = new Vector2(1f, 0.5f);
                statusRect.pivot = new Vector2(1f, 0.5f);
                statusRect.anchoredPosition = new Vector2(-5f, 0f);

                // Button setup
                var btn = itemGo.GetComponent<UnityEngine.UI.Button>();
                
                float lastClickTime = 0f;
                btn.onClick.AddListener(() => {
                    float currentTime = Time.unscaledTime;
                    if (currentTime - lastClickTime < 0.3f)
                    {
                        if (isPlaced)
                        {
                            CenterScrollOnPosition(level.CampaignMapPosition);
                            var mapBtn = _spawnedButtons.Find(b => b != null && b.LevelDataForCallback == level);
                            if (mapBtn != null)
                            {
                                OnLevelClicked(level);
                            }
                        }
                    }
                    else
                    {
                        if (isPlaced)
                        {
                            CenterScrollOnPosition(level.CampaignMapPosition);
                        }
                    }
                    lastClickTime = currentTime;
                });
            }
        }
    }
}

