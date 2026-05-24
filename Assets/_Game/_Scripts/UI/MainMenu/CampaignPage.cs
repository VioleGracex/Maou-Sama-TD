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
        private bool _initialized = false;
        
        [Header("Toggles")]
        [SerializeField] private UnityEngine.UI.Button _mainStoryTabButton;
        [SerializeField] private UnityEngine.UI.Button _resourceDungeonsTabButton;
        [SerializeField] private UnityEngine.UI.Button _specialDungeonsTabButton;
        
        [SerializeField] private bool _showMainStory = true;
        [SerializeField] private bool _showResourceDungeons = false;
        [SerializeField] private bool _showSpecialDungeons = false;
        [SerializeField] private float _maxZoomDistance = 4.0f;

        public bool ShowMainStory => _tabController != null ? _tabController.ShowMainStory : _showMainStory;
        public bool ShowResourceDungeons => _tabController != null ? _tabController.ShowResourceDungeons : _showResourceDungeons;
        public bool ShowSpecialDungeons => _tabController != null ? _tabController.ShowSpecialDungeons : _showSpecialDungeons;

        [SerializeField] private MaouSamaTD.UI.Cohorts.CohortSquadUI _cohortSquadUI;
        
        [Inject] private SaveManager _saveManager;

        [Header("Sidebar & Navigation UI")]
        [SerializeField] private GameObject _sidebarRoot;
        [SerializeField] private Transform _sidebarContentContainer;
        [SerializeField] private UnityEngine.UI.Button _zoomInButton;
        [SerializeField] private UnityEngine.UI.Button _zoomOutButton;
        [SerializeField] private SidebarLevelItem _sidebarItemPrefab;
        [SerializeField] private Sprite _arrowLeftSprite;
        [SerializeField] private Sprite _arrowRightSprite;
        [SerializeField] private UnityEngine.UI.Slider _zoomSlider;

        [Header("Modular Controllers")]
        [SerializeField] private CampaignMapVisuals _mapVisuals = new CampaignMapVisuals();
        [SerializeField] private CampaignSidebarController _sidebarController = new CampaignSidebarController();
        [SerializeField] private CampaignTabController _tabController = new CampaignTabController();

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
        public List<LevelButton> SpawnedButtons => _mapVisuals.SpawnedButtons;

        private void OnEnable()
        {
            if (_initialized)
            {
                _tabController.UpdateTabVisuals();
                Refresh();
            }
        }

        private void Awake()
        {
            // Dynamic fallback search for _briefingPanel if reference is lost
            if (_briefingPanel == null)
            {
                _briefingPanel = FindObjectOfType<BriefingPanel>(true);
                if (_briefingPanel == null)
                {
                    Debug.LogWarning("[CampaignPage] BriefingPanel was not found in the scene! Click interactions will not show details.");
                }
            }
        }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // Initialize modular subcomponents with top-level fields
            _tabController.Initialize(this, _mainStoryTabButton, _resourceDungeonsTabButton, _specialDungeonsTabButton, _showMainStory, _showResourceDungeons, _showSpecialDungeons);
            _sidebarController.Initialize(this, _sidebarRoot, _sidebarContentContainer, _sidebarItemPrefab, _arrowLeftSprite, _arrowRightSprite);
            _mapVisuals.Initialize(this, _levelContainer, _levelButtonPrefab, _mapSprite, _maxZoomDistance, _zoomInButton, _zoomOutButton, _zoomSlider);

            Preheat();
            Refresh();
        }

        public void ToggleCategory(LevelCategory category)
        {
            if (_tabController != null)
            {
                _tabController.ToggleCategory(category);
            }
        }

        public void Open()
        {
            Initialize(); // Ensure initialized when opened
            _mapVisuals.HasInitializedMapPosition = false; // Reset map initialization so it centers on active node on open
            if (_visualRoot != null) _visualRoot.SetActive(true);
            _tabController.UpdateTabVisuals();
            Refresh();
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            if (_briefingPanel != null) _briefingPanel.Close();
            if (_cohortSquadUI != null) _cohortSquadUI.Close();
        }

        public bool RequestClose() => true;

        public void Preheat()
        {
            EnsureLevelsLoaded(() => {
                Debug.Log($"[CampaignPage] Preheated: {(_allLevels?.Count ?? 0)} levels loaded.");
            });
            
            if (_saveManager != null)
            {
                var data = _saveManager.CurrentData;
                Debug.Log($"[CampaignPage] Preheating: Save data loaded. Player: {data?.PlayerName}");
            }
        }

        public void ResetState()
        {
        }

        private void Update()
        {
            if (_initialized && _mapVisuals != null)
            {
                _mapVisuals.UpdateZoomSlider();
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

            Addressables.LoadAssetsAsync<LevelData>((object)"LevelData", null).Completed += handle =>
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
            if (_mapVisuals != null)
            {
                _mapVisuals.ClearSpawnedNodesAndSplinesEditorTime();
            }
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

            _tabController.EnsureTabsOnTop(_visualRoot, _levelContainer);

            Debug.Log($"[CampaignPage] Starting Refresh (Toggles Mode), Total levels: {_allLevels.Count}");

            List<LevelDisplayData> displayDataList = new List<LevelDisplayData>();

            bool showMainStory = ShowMainStory;
            bool showResourceDungeons = ShowResourceDungeons;
            bool showSpecialDungeons = ShowSpecialDungeons;

            if (showMainStory)
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
            if (showResourceDungeons)
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
            if (showSpecialDungeons)
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

            int activeCategoriesHash = (showMainStory ? 1 : 0) | (showResourceDungeons ? 2 : 0) | (showSpecialDungeons ? 4 : 0);
            bool isTabSwap = _mapVisuals.LastLoadedHash != -1 && _mapVisuals.LastLoadedHash != activeCategoriesHash;
            _mapVisuals.LastLoadedHash = activeCategoriesHash;

            _mapVisuals.RefreshMap(displayDataList, isTabSwap);

            _sidebarController.RefreshLeftSidebar(
                _allLevels,
                _mainStoryLevels,
                _resourceDungeons,
                _riteDungeons,
                _vassalDungeons,
                showMainStory,
                showResourceDungeons,
                showSpecialDungeons,
                _saveManager
            );
        }

        public void RedrawSplinesOnly()
        {
            if (_mapVisuals != null)
            {
                _mapVisuals.RedrawSplinesOnly();
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
                viewportSize = new Vector2(1920f, 1080f);
            }

            Vector2 targetContentPos = -position + (viewportSize / 2f);

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
            if (_saveManager == null) return false;

            if (level != null)
            {
                if (level.RequiredUnitLevel > 1 && _saveManager.GetHighestUnitLevel() < level.RequiredUnitLevel)
                    return false;
                
                if (level.RequiredPreviousLevel != null && !_saveManager.IsLevelCompleted(level.RequiredPreviousLevel.LevelID))
                    return false;
            }

            if (index == 0) return true;
            
            if (list == null || index < 0 || index >= list.Count) return false;
            var prevLevel = list[index - 1];
            if (prevLevel == null) return false;
            
            return _saveManager.IsLevelCompleted(prevLevel.LevelID);
        }

        public bool IsLevelLockedInUI(LevelData level)
        {
            if (_mapVisuals == null || _mapVisuals.SpawnedButtons == null) return false;
            foreach (var btn in _mapVisuals.SpawnedButtons)
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
             if (entry.LevelID == level.LevelID) return entry.Stars;
             return 0;
        }

        private void OnLevelClicked(LevelData level, bool isUnlocked)
        {
            if (_briefingPanel != null)
            {
                UIFlowManager.Instance.OpenPanel(_briefingPanel);
                _briefingPanel.Setup(level, isUnlocked, OnBriefingEngage);
            }
            else
            {
                Debug.LogWarning("[CampaignPage] Briefing Panel is null! Using fallback.");
                OnBriefingEngage(level);
            }
        }

        private void OnBriefingEngage(LevelData level)
        {
            if (_cohortSquadUI != null)
            {
                UIFlowManager.Instance.OpenPanel(_cohortSquadUI);

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

                _cohortSquadUI.OpenReadiness(level);
            }
            else
            {
                Debug.LogError("[CampaignPage] Cohort Manager UI is not assigned in CampaignPage!");
            }
        }

        private LevelButton _selectedLevelButton;

        public void DeselectCurrentNode()
        {
            if (_selectedLevelButton != null)
            {
                _selectedLevelButton.SetSelected(false);
                _selectedLevelButton = null;
            }
        }

        public void OnLevelClickedPublic(LevelButton btn, LevelData level, bool isUnlocked)
        {
            if (_selectedLevelButton != null)
            {
                _selectedLevelButton.SetSelected(false);
            }
            _selectedLevelButton = btn;
            if (_selectedLevelButton != null)
            {
                _selectedLevelButton.SetSelected(true);
            }

            OnLevelClicked(level, isUnlocked);
        }

        public Color GetCategoryColorPublic(LevelCategory category)
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
    }
}
