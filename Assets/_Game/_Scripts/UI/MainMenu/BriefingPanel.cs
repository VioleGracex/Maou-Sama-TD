using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Levels;
using MaouSamaTD.Managers;
using System;
using DG.Tweening;
using System.Collections.Generic;
using Zenject;

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
        [SerializeField] private Button _engageButton;

        [Header("Scroll System")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Transform _scrollContent;
        [SerializeField] private TextMeshProUGUI _scrollDescriptionText;

        [Header("Spawning Enemies Section")]
        [SerializeField] private GameObject _enemiesHeader;
        [SerializeField] private Transform _enemiesContainer;

        [Header("1-Time Rewards Section")]
        [SerializeField] private GameObject _oneTimeHeader;
        [SerializeField] private Transform _oneTimeContainer;

        [Header("Replay Victory Rewards Section")]
        [SerializeField] private GameObject _replayHeader;
        [SerializeField] private Transform _replayContainer;

        [Header("Stage Drops Section")]
        [SerializeField] private GameObject _dropsHeader;
        [SerializeField] private Transform _dropsContainer;

        [Header("Navigation Buttons")]
        [SerializeField] private Button _prevLevelBtn;
        [SerializeField] private Button _nextLevelBtn;
        [SerializeField] private Button _closeButton;

        [Header("Rewards Grid Prefab")]
        [SerializeField] private RewardItemUI _rewardPrefab;
        
        [Header("Animation")]
        [SerializeField] private float _animDuration = 0.3f;
        [SerializeField] private Ease _animEase = Ease.OutBack;

        private LevelData _currentLevel;
        private Action<LevelData> _onEngageClicked;

        [InjectOptional] private SaveManager _saveManager;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (_visualRoot != null)
            {
                _visualRoot.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            if (_engageButton != null)
            {
                _engageButton.onClick.AddListener(OnEngage);
            }
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
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
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
                    {
                        if (_prevLevelBtn != null && _prevLevelBtn.gameObject.activeSelf)
                        {
                            _prevLevelBtn.onClick.Invoke();
                        }
                    }
                    else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
                    {
                        if (_nextLevelBtn != null && _nextLevelBtn.gameObject.activeSelf)
                        {
                            _nextLevelBtn.onClick.Invoke();
                        }
                    }
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

            // Spacing: set beautiful floating top/bottom margin card anchors
            if (_visualRoot != null)
            {
                var panelImg = _visualRoot.GetComponent<Image>();
                if (panelImg != null)
                {
                    panelImg.color = new Color(0.06f, 0.06f, 0.08f, 0.98f); // Deep dark luxurious Gehenna palette
                    var outline = _visualRoot.GetComponent<Outline>();
                    if (outline == null) outline = _visualRoot.AddComponent<Outline>();
                    outline.effectColor = new Color(0.92f, 0.3f, 0.29f, 0.6f); // Crimson Accent
                    outline.effectDistance = new Vector2(2f, 2f);
                }

                // Adjust anchors for floating side panel with top/bottom spacing
                var rect = _visualRoot.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.67f, 0f);
                    rect.anchorMax = new Vector2(0.97f, 1f);
                    rect.offsetMin = new Vector2(0f, 115f);
                    rect.offsetMax = new Vector2(0f, -115f);
                }

                // Style Engage button background
                if (_engageButton != null)
                {
                    var btnImg = _engageButton.GetComponent<Image>();
                    if (btnImg != null)
                    {
                        btnImg.color = new Color(0.92f, 0.3f, 0.29f, 1f); // Crimson accent
                    }
                    var outline = _engageButton.GetComponent<Outline>();
                    if (outline == null) outline = _engageButton.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.97f, 0.79f, 0.14f, 0.7f); // Maou Gold glow
                    outline.effectDistance = new Vector2(1.5f, 1.5f);
                }
            }

            if (_titleText != null)
            {
                _titleText.text = $"{LevelButton.FormatLevelID(level.LevelID)} {level.LevelName}\n<size=14><color=#ffd700>Recommended Lv. {level.MinMonsterLevel} - {level.MaxMonsterLevel}</color></size>";
                _titleText.fontSize = 20f;
                _titleText.color = new Color(0.97f, 0.79f, 0.14f, 1f); // Maou Gold
                _titleText.fontStyle = FontStyles.Bold;
            }

            bool isCleared = false;
            if (_saveManager != null && _saveManager.CurrentData != null)
            {
                isCleared = _saveManager.CurrentData.CompletedLevels.Contains(level.LevelID);
            }

            if (_engageButton != null)
            {
                _engageButton.interactable = true;
                var engageText = _engageButton.GetComponentInChildren<TextMeshProUGUI>();
                if (engageText != null)
                {
                    engageText.text = isCleared ? "REPLAY" : "ENGAGE";
                }
            }

            // --- Configure Pre-Placed Scroll View Elements ---

            // 1. Description
            if (_scrollDescriptionText != null)
            {
                _scrollDescriptionText.text = level.Description;
                _scrollDescriptionText.gameObject.SetActive(!string.IsNullOrEmpty(level.Description));
            }

            // 2. 1-Time Rewards & Unlocks Section (If Exists List)
            bool hasOneTime = false;
            List<(Sprite icon, string qty)> oneTimeList = new List<(Sprite icon, string qty)>();

            int currentStars = 0;
            if (_saveManager != null && _saveManager.CurrentData != null)
            {
                var starData = _saveManager.CurrentData.LevelStars.Find(s => s.LevelID == level.LevelID);
                if (starData.LevelID != null)
                {
                    currentStars = starData.Stars;
                }
                else if (isCleared)
                {
                    currentStars = 3;
                }
            }

            if (level.StarConditions != null)
            {
                for (int sIdx = 0; sIdx < level.StarConditions.Count; sIdx++)
                {
                    var cond = level.StarConditions[sIdx];
                    bool claimed = sIdx < currentStars;

                    if (cond.BonusRewards != null)
                    {
                        foreach (var reward in cond.BonusRewards)
                        {
                            Sprite rSprite = GetRewardSprite(reward.Type.ToString());
                            string status = claimed ? " <color=#778899>[Claimed]</color>" : " <color=#ffd700>[Available]</color>";
                            oneTimeList.Add((rSprite, $"{reward.Amount} {FormatRewardName(reward.Type.ToString())}{status}"));
                            hasOneTime = true;
                        }
                    }
                }
            }

            if (level.Category == LevelCategory.RiteDungeon)
            {
                Sprite riteSprite = null;
#if UNITY_EDITOR
                riteSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI_Pages/Home/cmd_node_citadel.png");
#endif
                string status = isCleared ? " <color=#778899>[Unlocked]</color>" : " <color=#ffd700>[Unlock on Clear]</color>";
                oneTimeList.Add((riteSprite, $"New Rite Unlock{status}"));
                hasOneTime = true;
            }
            else if (level.Category == LevelCategory.VassalDungeon)
            {
                Sprite vassalSprite = null;
#if UNITY_EDITOR
                vassalSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI_Pages/Home/cmd_node_chambers.png");
#endif
                string status = isCleared ? " <color=#778899>[Recruited]</color>" : " <color=#ffd700>[Recruit on Clear]</color>";
                oneTimeList.Add((vassalSprite, $"New Vassal Unlock{status}"));
                hasOneTime = true;
            }

            if (_oneTimeContainer != null)
            {
                ClearContainer(_oneTimeContainer);
                bool showOneTime = hasOneTime && _rewardPrefab != null;
                if (_oneTimeHeader != null) _oneTimeHeader.SetActive(showOneTime);
                _oneTimeContainer.gameObject.SetActive(showOneTime);

                if (showOneTime)
                {
                    foreach (var itemData in oneTimeList)
                    {
                        RewardItemUI item = Instantiate(_rewardPrefab, _oneTimeContainer);
                        item.Setup(itemData.icon, itemData.qty);
                        
                        var itemRect = item.GetComponent<RectTransform>();
                        if (itemRect != null) itemRect.sizeDelta = new Vector2(85f, 85f);
                    }
                }
            }

            // 3. Repeatable Monster Forces Section (Chibis, names, level ranges, and repeatable drops)
            var uniqueEnemies = new List<MaouSamaTD.Units.EnemyData>();
            if (level.Waves != null)
            {
                foreach (var wave in level.Waves)
                {
                    if (wave.Groups != null)
                    {
                        foreach (var gp in wave.Groups)
                        {
                            if (gp != null && gp.EnemyType != null && !uniqueEnemies.Contains(gp.EnemyType))
                            {
                                uniqueEnemies.Add(gp.EnemyType);
                            }
                        }
                    }
                }
            }

            if (_enemiesContainer != null)
            {
                ClearContainer(_enemiesContainer);
                
                // Adjust EnemiesContainer Layout programmatically if needed to support vertical list of cards nicely
                var vlg = _enemiesContainer.GetComponent<VerticalLayoutGroup>();
                if (vlg == null)
                {
                    vlg = _enemiesContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                }
                vlg.spacing = 10f;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.padding = new RectOffset(6, 6, 6, 6);

                var csf = _enemiesContainer.GetComponent<ContentSizeFitter>();
                if (csf == null)
                {
                    csf = _enemiesContainer.gameObject.AddComponent<ContentSizeFitter>();
                }
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                bool hasEnemies = uniqueEnemies.Count > 0 && _rewardPrefab != null;
                if (_enemiesHeader != null)
                {
                    _enemiesHeader.SetActive(hasEnemies);
                    var headerTxt = _enemiesHeader.GetComponentInChildren<TextMeshProUGUI>();
                    if (headerTxt != null) headerTxt.text = "Repeatable Spawning Forces";
                }
                _enemiesContainer.gameObject.SetActive(hasEnemies);

                if (hasEnemies)
                {
                    foreach (var enemy in uniqueEnemies)
                    {
                        // Create a premium MonsterCard card GameObject
                        GameObject cardGo = new GameObject("MonsterCard", typeof(RectTransform), typeof(Image));
                        cardGo.transform.SetParent(_enemiesContainer, false);
                        
                        var cardRect = cardGo.GetComponent<RectTransform>();
                        cardRect.sizeDelta = new Vector2(0f, 95f);
                        
                        var cardImg = cardGo.GetComponent<Image>();
                        cardImg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f); // Deep circular dark glassmorphism
                        
                        var cardOutline = cardGo.AddComponent<Outline>();
                        cardOutline.effectColor = new Color(0.92f, 0.3f, 0.29f, 0.35f); // Beautiful Crimson outline
                        cardOutline.effectDistance = new Vector2(1f, 1f);
                        
                        var cardLayout = cardGo.AddComponent<HorizontalLayoutGroup>();
                        cardLayout.spacing = 12f;
                        cardLayout.padding = new RectOffset(8, 8, 8, 8);
                        cardLayout.childAlignment = TextAnchor.MiddleLeft;
                        cardLayout.childControlWidth = false;
                        cardLayout.childControlHeight = false;
                        cardLayout.childForceExpandWidth = false;
                        cardLayout.childForceExpandHeight = false;

                        var cardFitter = cardGo.AddComponent<ContentSizeFitter>();
                        cardFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                        cardFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

                        // Chibi Image
                        GameObject chibiGo = new GameObject("Chibi", typeof(RectTransform), typeof(Image));
                        chibiGo.transform.SetParent(cardGo.transform, false);
                        var chibiRect = chibiGo.GetComponent<RectTransform>();
                        chibiRect.sizeDelta = new Vector2(65f, 65f);
                        var chibiImg = chibiGo.GetComponent<Image>();
                        chibiImg.sprite = enemy.EnemySprite ?? enemy.FullBodyArt;
                        chibiImg.preserveAspect = true;
                        
                        // Info Container
                        GameObject infoGo = new GameObject("InfoContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
                        infoGo.transform.SetParent(cardGo.transform, false);
                        var infoRect = infoGo.GetComponent<RectTransform>();
                        infoRect.sizeDelta = new Vector2(250f, 75f);
                        
                        var infoLayout = infoGo.GetComponent<VerticalLayoutGroup>();
                        infoLayout.spacing = 4f;
                        infoLayout.childAlignment = TextAnchor.MiddleLeft;
                        infoLayout.childControlWidth = true;
                        infoLayout.childControlHeight = false;
                        infoLayout.childForceExpandWidth = true;
                        infoLayout.childForceExpandHeight = false;

                        // Monster Name + Level Range
                        GameObject nameGo = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
                        nameGo.transform.SetParent(infoGo.transform, false);
                        var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
                        nameTmp.text = $"{enemy.EnemyName} <color=#ffd700>Lv.{level.MinMonsterLevel}-{level.MaxMonsterLevel}</color>";
                        nameTmp.fontSize = 13f;
                        nameTmp.fontStyle = FontStyles.Bold;
                        nameTmp.color = Color.white;

                        // Drops Horizontal Container
                        GameObject dropsGo = new GameObject("DropsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                        dropsGo.transform.SetParent(infoGo.transform, false);
                        var dropsRect = dropsGo.GetComponent<RectTransform>();
                        dropsRect.sizeDelta = new Vector2(0f, 32f);
                        
                        var dropsLayout = dropsGo.GetComponent<HorizontalLayoutGroup>();
                        dropsLayout.spacing = 6f;
                        dropsLayout.childAlignment = TextAnchor.MiddleLeft;
                        dropsLayout.childControlWidth = false;
                        dropsLayout.childControlHeight = false;
                        dropsLayout.childForceExpandWidth = false;
                        dropsLayout.childForceExpandHeight = false;

                        // Populate Repeatable Drops
                        bool hasAnyDrops = false;
                        if (level.StageLootConfig != null)
                        {
                            foreach (var loot in level.StageLootConfig)
                            {
                                RewardItemUI dropItem = Instantiate(_rewardPrefab, dropsGo.transform);
                                Sprite icon = GetRewardSprite(loot.ItemID);
                                dropItem.Setup(icon, $"{(loot.DropChance * 100f):0}%");
                                
                                var dropRect = dropItem.GetComponent<RectTransform>();
                                if (dropRect != null) dropRect.sizeDelta = new Vector2(32f, 32f);
                                
                                var itemTxt = dropItem.GetComponentInChildren<TextMeshProUGUI>();
                                if (itemTxt != null)
                                {
                                    itemTxt.fontSize = 9f;
                                    itemTxt.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
                                }
                                hasAnyDrops = true;
                            }
                        }

                        if (level.WinRewards != null)
                        {
                            foreach (var reward in level.WinRewards)
                            {
                                RewardItemUI dropItem = Instantiate(_rewardPrefab, dropsGo.transform);
                                Sprite icon = GetRewardSprite(reward.Type.ToString());
                                dropItem.Setup(icon, "100%");
                                
                                var dropRect = dropItem.GetComponent<RectTransform>();
                                if (dropRect != null) dropRect.sizeDelta = new Vector2(32f, 32f);
                                
                                var itemTxt = dropItem.GetComponentInChildren<TextMeshProUGUI>();
                                if (itemTxt != null)
                                {
                                    itemTxt.fontSize = 9f;
                                    itemTxt.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
                                }
                                hasAnyDrops = true;
                            }
                        }

                        if (!hasAnyDrops)
                        {
                            GameObject noDropGo = new GameObject("NoDropText", typeof(RectTransform), typeof(TextMeshProUGUI));
                            noDropGo.transform.SetParent(dropsGo.transform, false);
                            var noDropTmp = noDropGo.GetComponent<TextMeshProUGUI>();
                            noDropTmp.text = "<color=#888888>No repeatable drops</color>";
                            noDropTmp.fontSize = 11f;
                        }
                    }
                }
            }

            // 4. Deactivate/Streamline separate old reward sections
            if (_replayHeader != null) _replayHeader.SetActive(false);
            if (_replayContainer != null) _replayContainer.gameObject.SetActive(false);
            if (_dropsHeader != null) _dropsHeader.SetActive(false);
            if (_dropsContainer != null) _dropsContainer.gameObject.SetActive(false);

            // Update overlapping navigation buttons
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

            // Spacing top area and bottom area (dont make them full height) - Premium floating card style
            var rect = _visualRoot.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.67f, 0f);
                rect.anchorMax = new Vector2(0.97f, 1f);
                rect.offsetMin = new Vector2(0f, 115f);
                rect.offsetMax = new Vector2(0f, -115f);
            }

            // Apply canvas sorting override to stay on top of the Navigation Bar
            var canvas = _visualRoot.GetComponent<Canvas>();
            if (canvas == null) canvas = _visualRoot.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50;

            var raycaster = _visualRoot.GetComponent<GraphicRaycaster>();
            if (raycaster == null) raycaster = _visualRoot.AddComponent<GraphicRaycaster>();
            
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
        private void ClearContainer(Transform container)
        {
            if (container == null) return;
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

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
            if (campaignPage == null) return;

            LevelData prevLevel = null;
            LevelData nextLevel = null;

            if (_currentLevel.Category == LevelCategory.MainStory)
            {
                // Sequential story cycling
                var storyLevels = new List<LevelData>();
                foreach (var lvl in campaignPage.AllLevels)
                {
                    if (lvl != null && lvl.Category == LevelCategory.MainStory)
                    {
                        storyLevels.Add(lvl);
                    }
                }
                storyLevels.Sort((a, b) => a.LevelIndex.CompareTo(b.LevelIndex));

                if (storyLevels.Count > 1)
                {
                    int curIdx = storyLevels.IndexOf(_currentLevel);
                    if (curIdx >= 0)
                    {
                        prevLevel = storyLevels[(curIdx - 1 + storyLevels.Count) % storyLevels.Count];
                        nextLevel = storyLevels[(curIdx + 1) % storyLevels.Count];
                    }
                }
            }
            else
            {
                // Spatial/distance-based dungeon cycling
                var activeSiblings = new List<LevelData>();
                foreach (var btn in campaignPage.SpawnedButtons)
                {
                    if (btn != null && btn.LevelDataForCallback != null && btn.LevelDataForCallback.Category == _currentLevel.Category && btn.LevelDataForCallback != _currentLevel)
                    {
                        activeSiblings.Add(btn.LevelDataForCallback);
                    }
                }

                if (activeSiblings.Count > 0)
                {
                    // Find Previous (Left Arrow): closest node situating to the left (X < current.X)
                    var leftNodes = new List<LevelData>();
                    foreach (var lvl in activeSiblings)
                    {
                        if (lvl.CampaignMapPosition.x < _currentLevel.CampaignMapPosition.x)
                            leftNodes.Add(lvl);
                    }

                    if (leftNodes.Count > 0)
                    {
                        leftNodes.Sort((a, b) => Mathf.Abs(a.CampaignMapPosition.x - _currentLevel.CampaignMapPosition.x)
                            .CompareTo(Mathf.Abs(b.CampaignMapPosition.x - _currentLevel.CampaignMapPosition.x)));
                        prevLevel = leftNodes[0];
                    }
                    else
                    {
                        // Wrap around to rightmost node
                        activeSiblings.Sort((a, b) => b.CampaignMapPosition.x.CompareTo(a.CampaignMapPosition.x));
                        prevLevel = activeSiblings[0];
                    }

                    // Find Next (Right Arrow): closest node situating to the right (X > current.X)
                    var rightNodes = new List<LevelData>();
                    foreach (var lvl in activeSiblings)
                    {
                        if (lvl.CampaignMapPosition.x > _currentLevel.CampaignMapPosition.x)
                            rightNodes.Add(lvl);
                    }

                    if (rightNodes.Count > 0)
                    {
                        rightNodes.Sort((a, b) => Mathf.Abs(a.CampaignMapPosition.x - _currentLevel.CampaignMapPosition.x)
                            .CompareTo(Mathf.Abs(b.CampaignMapPosition.x - _currentLevel.CampaignMapPosition.x)));
                        nextLevel = rightNodes[0];
                    }
                    else
                    {
                        // Wrap around to leftmost node
                        activeSiblings.Sort((a, b) => a.CampaignMapPosition.x.CompareTo(b.CampaignMapPosition.x));
                        nextLevel = activeSiblings[0];
                    }
                }
            }

            bool showPrev = prevLevel != null;
            bool showNext = nextLevel != null;

            if (_visualRoot != null)
            {
                var rootRect = _visualRoot.GetComponent<RectTransform>();
                
                // Keep the engage button centered on the same row at the bottom of the briefing card
                if (_engageButton != null)
                {
                    var engageRect = _engageButton.GetComponent<RectTransform>();
                    if (engageRect != null)
                    {
                        engageRect.SetParent(rootRect, false);
                        engageRect.anchorMin = new Vector2(0.5f, 0f);
                        engageRect.anchorMax = new Vector2(0.5f, 0f);
                        engageRect.pivot = new Vector2(0.5f, 0.5f);
                        engageRect.anchoredPosition = new Vector2(0f, 40f); // Centered 40px above bottom
                        engageRect.sizeDelta = new Vector2(180f, 44f);
                    }
                }

                if (_prevLevelBtn != null)
                {
                    var prevRect = _prevLevelBtn.GetComponent<RectTransform>();
                    prevRect.SetParent(rootRect, false);
                    prevRect.anchorMin = new Vector2(0.5f, 0f);
                    prevRect.anchorMax = new Vector2(0.5f, 0f);
                    prevRect.pivot = new Vector2(0.5f, 0.5f);
                    prevRect.anchoredPosition = new Vector2(-120f, 40f); // Left of engage button, same row
                    prevRect.sizeDelta = new Vector2(40f, 40f);
                }

                if (_nextLevelBtn != null)
                {
                    var nextRect = _nextLevelBtn.GetComponent<RectTransform>();
                    nextRect.SetParent(rootRect, false);
                    nextRect.anchorMin = new Vector2(0.5f, 0f);
                    nextRect.anchorMax = new Vector2(0.5f, 0f);
                    nextRect.pivot = new Vector2(0.5f, 0.5f);
                    nextRect.anchoredPosition = new Vector2(120f, 40f); // Right of engage button, same row
                    nextRect.sizeDelta = new Vector2(40f, 40f);
                }
            }

            if (_prevLevelBtn != null)
            {
                _prevLevelBtn.gameObject.SetActive(showPrev);
                _prevLevelBtn.onClick.RemoveAllListeners();
                if (showPrev)
                {
                    _prevLevelBtn.onClick.AddListener(() => {
                        Setup(prevLevel, _onEngageClicked);
                        campaignPage.CenterScrollOnPosition(prevLevel.CampaignMapPosition);
                    });
                }
            }

            if (_nextLevelBtn != null)
            {
                _nextLevelBtn.gameObject.SetActive(showNext);
                _nextLevelBtn.onClick.RemoveAllListeners();
                if (showNext)
                {
                    _nextLevelBtn.onClick.AddListener(() => {
                        Setup(nextLevel, _onEngageClicked);
                        campaignPage.CenterScrollOnPosition(nextLevel.CampaignMapPosition);
                    });
                }
            }
        }

        private Sprite GetRewardSprite(string rewardName)
        {
            Sprite sprite = null;
            string path = "";
            string lower = rewardName.ToLower();

            if (lower.Contains("gold"))
            {
                path = "Assets/_Game/Art/UI/Icons/Gacha/icon_gold_pile.png";
            }
            else if (lower.Contains("blood") || lower.Contains("crest"))
            {
                path = "Assets/_Game/Art/UI/Icons/Gacha/icon_blood_crest.png";
            }
            else if (lower.Contains("gem") || lower.Contains("soul"))
            {
                path = "Assets/_Game/Art/UI_Pages/Home/cmd_node_manifest_soul.png";
            }
            else if (lower.Contains("xp") || lower.Contains("core"))
            {
                path = "Assets/_Game/Art/Items/xp_core_common.png";
            }

            if (!string.IsNullOrEmpty(path))
            {
#if UNITY_EDITOR
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#endif
            }
            return sprite;
        }

        private string FormatRewardName(string rawName)
        {
            if (rawName == "GoldCoins") return "Gold Coins";
            if (rawName == "BloodCrests") return "Blood Crests";
            if (rawName == "PlayerXP") return "Player XP";
            if (rawName == "UnitXP") return "Unit XP";
            if (rawName == "Gems") return "Gems";
            return rawName;
        }
        #endregion
    }
}
