using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Levels;
using MaouSamaTD.Managers;
using MaouSamaTD.UI.Common;
using Zenject;

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
        [SerializeField] private List<LevelData> _allLevels;
        [SerializeField] private BriefingPanel _briefingPanel;
        [SerializeField] private Sprite _mapSprite;
        
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

        public Transform LevelContainer => _levelContainer;
        public List<LevelData> AllLevels => _allLevels;
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
            // Ensure levels are loaded into memory
            if (MaouSamaTD.Core.AppEntryPoint.LoadedLevelDatabase != null)
            {
                _allLevels = MaouSamaTD.Core.AppEntryPoint.LoadedLevelDatabase.AllLevels;
                Debug.Log($"[CampaignPage] Preheating: Loaded {_allLevels.Count} levels from global database.");
            }
            else if (_allLevels == null || _allLevels.Count == 0)
            {
                Debug.LogWarning("[CampaignPage] Preheating: No levels assigned and LevelDatabase not found!");
            }
            
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

        public void Refresh()
        {
            if (MaouSamaTD.Core.AppEntryPoint.LoadedLevelDatabase != null)
            {
                _allLevels = MaouSamaTD.Core.AppEntryPoint.LoadedLevelDatabase.AllLevels;
            }
            else
            {
#if UNITY_EDITOR
                // In Edit Mode, load from AssetDatabase to support full visual previewing and coordinate updates
                var dbGuid = UnityEditor.AssetDatabase.FindAssets("t:LevelDatabase");
                if (dbGuid.Length > 0)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(dbGuid[0]);
                    var db = UnityEditor.AssetDatabase.LoadAssetAtPath<MaouSamaTD.Data.LevelDatabase>(path);
                    if (db != null) _allLevels = db.AllLevels;
                }
#endif
            }

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
            for (int i = 0; i < _allLevels.Count; i++)
            {
                LevelData level = _allLevels[i];
                if (level == null) continue;

                // Tab Filter
                bool shouldDisplay = false;
                if (_currentTab == LevelCategory.MainStory && level.Category == LevelCategory.MainStory) shouldDisplay = true;
                else if (_currentTab == LevelCategory.ResourceDungeon && level.Category == LevelCategory.ResourceDungeon) shouldDisplay = true;
                else if (_currentTab == LevelCategory.RiteDungeon && (level.Category == LevelCategory.RiteDungeon || level.Category == LevelCategory.VassalDungeon)) shouldDisplay = true;

                if (!shouldDisplay) continue;

                displayDataList.Add(new LevelDisplayData
                {
                    Level = level,
                    Index = i,
                    IsLocked = !IsLevelUnlocked(level, i),
                    StarCount = GetLevelStars(level)
                });
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

        private void CenterScrollOnPosition(Vector2 position)
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

        private bool IsLevelUnlocked(LevelData level, int index)
        {
            if (index == 0) return true; // First level always unlocked
            
            // Check if previous level is completed
            var prevLevel = _allLevels[index - 1];
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
    }
}

