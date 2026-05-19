using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Levels;
using System;
using DG.Tweening;
using System.Collections.Generic;

namespace MaouSamaTD.UI.MainMenu
{
    public class BriefingPanel : MonoBehaviour, IUIController
    {
        #region Variables
        [Header("UI Components")]
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => false;
        [SerializeField] private NavigationFeatures _navFeatures = NavigationFeatures.BackButton;
        public NavigationFeatures ConfiguredNavFeatures => _navFeatures;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _rewardValueText;
        [SerializeField] private Button _engageButton;
        
        [Header("Rewards Grid")]
        [SerializeField] private Transform _rewardsContainer;
        [SerializeField] private RewardItemUI _rewardPrefab;
        
        [Header("Animation")]
        [SerializeField] private float _animDuration = 0.3f;
        [SerializeField] private Ease _animEase = Ease.OutBack;

        private LevelData _currentLevel;
        private Action<LevelData> _onEngageClicked;

        // Dynamic overlapping level cycling buttons
        private Button _prevLevelBtn;
        private Button _nextLevelBtn;

        [InjectOptional] private SaveManager _saveManager;
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (_engageButton != null)
            {
                _engageButton.onClick.AddListener(OnEngage);
            }
            if (_saveManager == null)
            {
                _saveManager = FindObjectOfType<SaveManager>();
            }
        }

        private void Update()
        {
            if (_visualRoot != null && _visualRoot.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                {
                    _prevLevelBtn?.onClick.Invoke();
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                {
                    _nextLevelBtn?.onClick.Invoke();
                }
            }
        }
        #endregion

        #region Public Methods
        public void Setup(LevelData level, Action<LevelData> onEngageCallback)
        {
            Debug.Log($"[BriefingPanel] Setup called for level: {(level != null ? level.LevelName : "NULL")}");
            _currentLevel = level;
            _onEngageClicked = onEngageCallback;

            if (_titleText != null) _titleText.text = level.LevelName;
            if (_descriptionText != null) _descriptionText.text = level.Description;
            
            // Populate Rewards dynamically
            if (_rewardsContainer != null && _rewardPrefab != null)
            {
                // Clear old rewards
                foreach (Transform child in _rewardsContainer)
                {
                    Destroy(child.gameObject);
                }

                // Add Guaranteed Rewards
                if (level.WinRewards != null)
                {
                    bool isCleared = false;
                    if (_saveManager != null && _saveManager.CurrentData != null)
                    {
                        isCleared = _saveManager.CurrentData.CompletedLevels.Contains(level.LevelID);
                    }

                    foreach (var reward in level.WinRewards)
                    {
                        RewardItemUI item = Instantiate(_rewardPrefab, _rewardsContainer);
                        string suffix = "";
                        if (reward.Type == MaouSamaTD.Data.RewardType.BloodCrests || reward.Type == MaouSamaTD.Data.RewardType.Gems)
                        {
                            suffix = isCleared ? " [Claimed]" : " [First Clear]";
                        }
                        item.Setup(null, $"{reward.Amount} {reward.Type}{suffix}");
                    }
                }

                // Add Potential Loot Drops
                if (level.StageLootConfig != null)
                {
                    foreach (var loot in level.StageLootConfig)
                    {
                        RewardItemUI item = Instantiate(_rewardPrefab, _rewardsContainer);
                        string qty = loot.MinQuantity == loot.MaxQuantity 
                            ? loot.MinQuantity.ToString() 
                            : $"{loot.MinQuantity}-{loot.MaxQuantity}";
                        item.Setup(null, $"{qty} {loot.ItemID} ({(loot.DropChance * 100f):0}%)");
                    }
                }
            }

            // Detect and setup overlapping/close levels navigation cycle buttons
            UpdateOverlappingNavigation();
        }

        public void Open()
        {
            Debug.Log($"[BriefingPanel] Open called.");
            if (_visualRoot == null)
            {
                Debug.LogError($"[UIFlow] {gameObject.name} (BriefingPanel) cannot open! _visualRoot is not assigned in the Inspector.");
                return;
            }
            _visualRoot.SetActive(true);
            
            _visualRoot.transform.localScale = Vector3.zero;
            _visualRoot.transform.DOScale(Vector3.one, _animDuration).SetEase(_animEase).SetUpdate(true);

            // Hide global buttons to prevent them from rendering on top of briefing
            if (UIFlowManager.Instance != null)
            {
                UIFlowManager.Instance.UpdateNavigationFeatures(NavigationFeatures.None);
            }
        }

        public void Close()
        {
            Debug.Log($"[BriefingPanel] Close called. activeSelf: {(_visualRoot != null && _visualRoot.activeSelf)}");
            if (_visualRoot == null || !_visualRoot.activeSelf) return;

            _visualRoot.transform.DOScale(Vector3.zero, _animDuration / 2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => {
                _visualRoot.SetActive(false);

                // Restore global buttons
                if (UIFlowManager.Instance != null)
                {
                    UIFlowManager.Instance.UpdateNavigationFeatures(NavigationFeatures.BackButton | NavigationFeatures.CitadelButton);
                }
            });
        }

        public bool RequestClose() => true;

        public void ResetState()
        {
            _currentLevel = null;
            _onEngageClicked = null;
        }
        #endregion

        #region Private Methods
        private void OnEngage()
        {
            Debug.Log($"[BriefingPanel] OnEngage clicked! Launching level: {(_currentLevel != null ? _currentLevel.LevelName : "NULL")}");
            Close();
            _onEngageClicked?.Invoke(_currentLevel);
        }

        private void UpdateOverlappingNavigation()
        {
            if (_currentLevel == null) return;

            var campaignPage = FindObjectOfType<CampaignPage>();
            List<LevelData> activeLevels = new List<LevelData>();
            if (campaignPage != null && campaignPage.SpawnedButtons != null)
            {
                foreach (var btn in campaignPage.SpawnedButtons)
                {
                    if (btn != null && btn.LevelDataForCallback != null)
                    {
                        activeLevels.Add(btn.LevelDataForCallback);
                    }
                }
            }

            // Find close levels within 60 units (overlapping nodes)
            List<LevelData> closeLevels = new List<LevelData>();
            foreach (var lvl in activeLevels)
            {
                if (lvl == null || lvl == _currentLevel || lvl.Category != _currentLevel.Category) continue;
                
                float dist = Vector2.Distance(lvl.CampaignMapPosition, _currentLevel.CampaignMapPosition);
                if (dist < 60f)
                {
                    closeLevels.Add(lvl);
                }
            }

            closeLevels = System.Linq.Enumerable.ToList(System.Linq.Enumerable.OrderBy(closeLevels, l => l.LevelID));
            bool showNav = closeLevels.Count > 0;

            EnsureNavigationButtonsExist();

            if (_prevLevelBtn != null)
            {
                _prevLevelBtn.gameObject.SetActive(showNav);
                _prevLevelBtn.onClick.RemoveAllListeners();
                if (showNav)
                {
                    _prevLevelBtn.onClick.AddListener(() => {
                        var cycleList = new List<LevelData> { _currentLevel };
                        cycleList.AddRange(closeLevels);
                        int curIdx = cycleList.IndexOf(_currentLevel);
                        int nextIdx = (curIdx - 1 + cycleList.Count) % cycleList.Count;
                        var nextLevel = cycleList[nextIdx];
                        Setup(nextLevel, _onEngageClicked);
                        
                        // Center camera/scroll on the new node
                        if (campaignPage != null)
                        {
                            campaignPage.CenterScrollOnPosition(nextLevel.CampaignMapPosition);
                        }
                    });
                }
            }

            if (_nextLevelBtn != null)
            {
                _nextLevelBtn.gameObject.SetActive(showNav);
                _nextLevelBtn.onClick.RemoveAllListeners();
                if (showNav)
                {
                    _nextLevelBtn.onClick.AddListener(() => {
                        var cycleList = new List<LevelData> { _currentLevel };
                        cycleList.AddRange(closeLevels);
                        int curIdx = cycleList.IndexOf(_currentLevel);
                        int nextIdx = (curIdx + 1) % cycleList.Count;
                        var nextLevel = cycleList[nextIdx];
                        Setup(nextLevel, _onEngageClicked);
                        
                        // Center camera/scroll on the new node
                        if (campaignPage != null)
                        {
                            campaignPage.CenterScrollOnPosition(nextLevel.CampaignMapPosition);
                        }
                    });
                }
            }
        }

        private void EnsureNavigationButtonsExist()
        {
            if (_prevLevelBtn != null && _nextLevelBtn != null) return;

            // Try to find them first
            _prevLevelBtn = transform.Find("PrevLevelButton")?.GetComponent<Button>();
            _nextLevelBtn = transform.Find("NextLevelButton")?.GetComponent<Button>();

            if (_prevLevelBtn != null && _nextLevelBtn != null) return;

            // If not found, let's create them next to _titleText
            Transform container = _titleText != null ? _titleText.transform.parent : transform;
            if (container == null) container = transform;

            if (_prevLevelBtn == null)
            {
                GameObject prevGo = new GameObject("PrevLevelButton", typeof(Image), typeof(Button));
                prevGo.transform.SetParent(container, false);
                _prevLevelBtn = prevGo.GetComponent<Button>();

                GameObject txtGo = new GameObject("Text", typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(prevGo.transform, false);
                var tmp = txtGo.GetComponent<TextMeshProUGUI>();
                tmp.text = "<";
                tmp.fontSize = 24;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;

                var rect = prevGo.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(40f, 40f);
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(10f, 0f);

                var txtRect = txtGo.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.sizeDelta = Vector2.zero;

                var img = prevGo.GetComponent<Image>();
                img.color = new Color(0.15f, 0.15f, 0.17f, 0.8f);
            }

            if (_nextLevelBtn == null)
            {
                GameObject nextGo = new GameObject("NextLevelButton", typeof(Image), typeof(Button));
                nextGo.transform.SetParent(container, false);
                _nextLevelBtn = nextGo.GetComponent<Button>();

                GameObject txtGo = new GameObject("Text", typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(nextGo.transform, false);
                var tmp = txtGo.GetComponent<TextMeshProUGUI>();
                tmp.text = ">";
                tmp.fontSize = 24;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;

                var rect = nextGo.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(40f, 40f);
                rect.anchorMin = new Vector2(1f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.anchoredPosition = new Vector2(-10f, 0f);

                var txtRect = txtGo.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.sizeDelta = Vector2.zero;

                var img = nextGo.GetComponent<Image>();
                img.color = new Color(0.15f, 0.15f, 0.17f, 0.8f);
            }
        }
        #endregion
    }
}
