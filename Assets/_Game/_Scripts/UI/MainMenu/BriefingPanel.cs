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
        [SerializeField] private TextMeshProUGUI _recommendedLevelText;
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
        private bool _initialized = false;

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

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

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
        public void Setup(LevelData level, bool isUnlocked, Action<LevelData> onEngageCallback)
        {
            if (level == null)
            {
                Debug.LogError("[BriefingPanel] Setup called with null LevelData!");
                return;
            }
            if (_visualRoot == null)
            {
                Debug.LogError("[BriefingPanel] Setup called but _visualRoot is null!");
                return;
            }

            Debug.Log($"[BriefingPanel] Setup called for level: {level.LevelName}");
            _currentLevel = level;
            _onEngageClicked = onEngageCallback;

            // =======================================================================
            // DATA POPULATION ONLY — Layout is baked into the scene; do not set
            // RectTransform anchors/offsets here to keep the Scene view valid.
            // =======================================================================

            // -- Title: Level name only (no inline recommended level — that's a separate TMP) --
            if (_titleText != null)
            {
                _titleText.text = $"{LevelButton.FormatLevelID(level.LevelID)} {level.LevelName}";
                _titleText.fontSize = 15f;
                _titleText.characterSpacing = 0.3f;
                _titleText.color = new Color(0.97f, 0.79f, 0.14f, 1f);
                _titleText.fontStyle = FontStyles.Bold;
                _titleText.alignment = TextAlignmentOptions.TopLeft;
                _titleText.overflowMode = TextOverflowModes.Truncate;
                _titleText.enableWordWrapping = true;

                // -- Category badge (colour-coded by type) --
                var titleHolder = _titleText.transform.parent;
                if (titleHolder != null)
                {
                    var categoryHeaderGO = titleHolder.Find("CategoryHeaderGroup");
                    if (categoryHeaderGO != null)
                    {
                        var categoryTextTrans = categoryHeaderGO.Find("Sov_Link_Text");
                        if (categoryTextTrans == null) categoryTextTrans = titleHolder.Find("Sov_Link_Text");

                        if (categoryTextTrans != null)
                        {
                            var categoryTmp = categoryTextTrans.GetComponent<TextMeshProUGUI>();
                            if (categoryTmp != null)
                            {
                                string hexColor;
                                string catName;
                                Sprite catIcon = null;

                                switch (level.Category)
                                {
                                    case LevelCategory.MainStory:
                                        hexColor = "#19CCFF"; catName = "MAIN STORY";
#if UNITY_EDITOR
                                        catIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI_Pages/Home/cmd_node_citadel.png");
#endif
                                        break;
                                    case LevelCategory.ResourceDungeon:
                                        hexColor = "#FFBF26"; catName = "RESOURCE DUNGEON";
#if UNITY_EDITOR
                                        catIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI_Pages/Home/cmd_node_treasury.png");
#endif
                                        break;
                                    case LevelCategory.RiteDungeon:
                                        hexColor = "#D959FF"; catName = "RITE DUNGEON";
#if UNITY_EDITOR
                                        catIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI_Pages/Home/cmd_node_ritual.png");
#endif
                                        break;
                                    case LevelCategory.VassalDungeon:
                                        hexColor = "#FF4C4C"; catName = "SOVEREIGN LINK";
#if UNITY_EDITOR
                                        catIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI_Pages/Home/cmd_node_chambers.png");
#endif
                                        break;
                                    default:
                                        hexColor = "#FFFFFF"; catName = "UNKNOWN";
                                        break;
                                }

                                categoryTmp.text = $"<color={hexColor}><b>{catName}</b></color>";
                                categoryTmp.alignment = TextAlignmentOptions.Left;

                                // Update category icon
                                var iconTrans = categoryHeaderGO.Find("Icon");
                                if (iconTrans != null)
                                {
                                    var iconImg = iconTrans.GetComponent<Image>();
                                    if (iconImg != null && catIcon != null)
                                        iconImg.sprite = catIcon;
                                }
                            }
                        }
                    }
                }
            }

            // -- Recommended Level: dedicated text object beneath title --
            if (_recommendedLevelText == null && _titleText != null)
            {
                // Auto-discover: child of same parent named RecommendedLevelText
                var recT = _titleText.transform.parent?.Find("RecommendedLevelText");
                if (recT != null) _recommendedLevelText = recT.GetComponent<TextMeshProUGUI>();
            }
            if (_recommendedLevelText != null)
            {
                _recommendedLevelText.text = $"Recommended Lv. {level.MinMonsterLevel} – {level.MaxMonsterLevel}";
                _recommendedLevelText.gameObject.SetActive(true);
            }

            bool isCleared = false;
            if (_saveManager != null && _saveManager.CurrentData != null && _saveManager.CurrentData.CompletedLevels != null)
            {
                isCleared = _saveManager.CurrentData.CompletedLevels.Contains(level.LevelID);
            }

            if (_engageButton != null)
            {
                _engageButton.interactable = isUnlocked;
                var engageText = _engageButton.GetComponentInChildren<TextMeshProUGUI>();
                if (engageText != null)
                {
                    if (!isUnlocked)
                    {
                        if (level.RequiredUnitLevel > 1 && (_saveManager == null || _saveManager.GetHighestUnitLevel() < level.RequiredUnitLevel))
                        {
                            engageText.text = $"REQUIRES LV {level.RequiredUnitLevel}";
                        }
                        else if (level.RequiredPreviousLevel != null && (_saveManager == null || !_saveManager.IsLevelCompleted(level.RequiredPreviousLevel.LevelID)))
                        {
                            engageText.text = $"CLEAR {level.RequiredPreviousLevel.LevelID} FIRST";
                        }
                        else
                        {
                            engageText.text = "LOCKED";
                        }
                    }
                    else
                    {
                        engageText.text = isCleared ? "REPLAY" : "ENGAGE";
                    }
                }
            }

            // --- Populate Scroll Content (data only, layout baked in scene) ---

            // 1. Description Text — always visible, placeholder if no description assigned
            if (_scrollDescriptionText != null)
            {
                _scrollDescriptionText.gameObject.SetActive(true);
                _scrollDescriptionText.text = string.IsNullOrEmpty(level.Description)
                    ? "<color=#555555><i>No briefing data available for this sector.</i></color>"
                    : level.Description;
                _scrollDescriptionText.fontSize = 12.5f;
                _scrollDescriptionText.characterSpacing = 0.4f;
                _scrollDescriptionText.lineSpacing = 12f;
                _scrollDescriptionText.color = new Color(0.85f, 0.85f, 0.85f, 0.95f);
                _scrollDescriptionText.alignment = TextAlignmentOptions.TopLeft;
                _scrollDescriptionText.overflowMode = TextOverflowModes.Overflow;
            }

            // 2. Rewards section — data population only, layout baked in scene
            Transform rewardTrans = _visualRoot.transform.Find("Reward");
            if (rewardTrans != null)
            {
                // Hide the static legacy value text
                Transform valueTrans = rewardTrans.Find("Briefing_Reward_Value");
                if (valueTrans != null) valueTrans.gameObject.SetActive(false);

                // Title label sizing & spacing
                Transform labelTrans = rewardTrans.Find("Briefing_Reward_Label");
                if (labelTrans != null)
                {
                    var labelTmp = labelTrans.GetComponent<TextMeshProUGUI>();
                    if (labelTmp != null)
                    {
                        labelTmp.text = "REWARDS";
                        labelTmp.fontSize = 13f;
                        labelTmp.fontStyle = FontStyles.Bold;
                        labelTmp.characterSpacing = 1f;
                        labelTmp.color = new Color(0.97f, 0.79f, 0.14f, 0.9f); // Subtle Maou Gold
                    }
                    var labelRect = labelTrans.GetComponent<RectTransform>();
                    if (labelRect != null)
                    {
                        labelRect.anchorMin = new Vector2(0f, 0.5f);
                        labelRect.anchorMax = new Vector2(0f, 0.5f);
                        labelRect.pivot = new Vector2(0f, 0.5f);
                        labelRect.anchoredPosition = new Vector2(12f, 0f);
                        labelRect.sizeDelta = new Vector2(80f, 30f);
                    }
                }

                // Find or build the ScrollView programmatically under Reward container
                Transform scrollTrans = rewardTrans.Find("DynamicRewardsScrollView");
                GameObject scrollGo;
                if (scrollTrans == null)
                {
                    scrollGo = new GameObject("DynamicRewardsScrollView", typeof(RectTransform), typeof(ScrollRect));
                    scrollGo.transform.SetParent(rewardTrans, false);
                    
                    var scrollRect = scrollGo.GetComponent<ScrollRect>();
                    scrollRect.horizontal = true;
                    scrollRect.vertical = false;
                    scrollRect.horizontalScrollbar = null;
                    scrollRect.verticalScrollbar = null;

                    var sRect = scrollGo.GetComponent<RectTransform>();
                    sRect.anchorMin = new Vector2(0f, 0f);
                    sRect.anchorMax = new Vector2(1f, 1f);
                    sRect.pivot = new Vector2(0.5f, 0.5f);
                    sRect.offsetMin = new Vector2(95f, 5f);  // Pushed right to clear the label
                    sRect.offsetMax = new Vector2(-10f, -5f);

                    // Viewport
                    GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                    viewportGo.transform.SetParent(scrollGo.transform, false);
                    var vRect = viewportGo.GetComponent<RectTransform>();
                    vRect.anchorMin = Vector2.zero;
                    vRect.anchorMax = Vector2.one;
                    vRect.sizeDelta = Vector2.zero;
                    viewportGo.GetComponent<Image>().color = Color.clear;

                    // Content Container
                    GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
                    contentGo.transform.SetParent(viewportGo.transform, false);
                    var cRect = contentGo.GetComponent<RectTransform>();
                    cRect.anchorMin = new Vector2(0f, 0.5f);
                    cRect.anchorMax = new Vector2(0f, 0.5f);
                    cRect.pivot = new Vector2(0f, 0.5f);
                    cRect.sizeDelta = new Vector2(0f, 60f);

                    var hLayout = contentGo.GetComponent<HorizontalLayoutGroup>();
                    hLayout.spacing = 8f;
                    hLayout.childAlignment = TextAnchor.MiddleLeft;
                    hLayout.childControlWidth = false;
                    hLayout.childControlHeight = false;
                    hLayout.childForceExpandWidth = false;
                    hLayout.childForceExpandHeight = false;
                    hLayout.padding = new RectOffset(4, 4, 4, 4);

                    var csFitter = contentGo.GetComponent<ContentSizeFitter>();
                    csFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    csFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

                    scrollRect.viewport = vRect;
                    scrollRect.content = cRect;
                }
                else
                {
                    scrollGo = scrollTrans.gameObject;
                }

                ScrollRect rewardsScroll = scrollGo.GetComponent<ScrollRect>();
                Transform contentTrans = rewardsScroll.content;

                // Clear old reward cells
                ClearContainer(contentTrans);

                bool hasRewards = false;

                // Load real/placeholder WinRewards
                List<MaouSamaTD.Data.RewardData> winRewardsList = level.WinRewards;
                if ((winRewardsList == null || winRewardsList.Count == 0) && (level.LevelID == "1-1" || level.LevelID == "0-1"))
                {
                    winRewardsList = new List<MaouSamaTD.Data.RewardData>();
                    winRewardsList.Add(new MaouSamaTD.Data.RewardData { Type = MaouSamaTD.Data.RewardType.GoldCoins, Amount = 150 });
                    winRewardsList.Add(new MaouSamaTD.Data.RewardData { Type = MaouSamaTD.Data.RewardType.BloodCrests, Amount = 30 });
                    winRewardsList.Add(new MaouSamaTD.Data.RewardData { Type = MaouSamaTD.Data.RewardType.Gems, Amount = 5 });
                }

                // Spawning WinRewards (repeatable Gold, Blood Crests, Gems, etc.)
                if (winRewardsList != null)
                {
                    foreach (var reward in winRewardsList)
                    {
                        if (reward.Amount <= 0) continue;
                        
                        Sprite icon = GetRewardSprite(reward.Type.ToString());
                        string displayName = FormatRewardName(reward.Type.ToString());
                        CreateRewardItem(contentTrans, icon, $"+{reward.Amount} {displayName}", Color.white, 105f);
                        hasRewards = true;
                    }
                }

                // Spawning Sovereign Rites Unlocks
                if (level.Category == LevelCategory.RiteDungeon)
                {
                    var rites = new List<MaouSamaTD.Skills.SovereignRiteData>();
                    if (level.MaleSovereignRites != null) rites.AddRange(level.MaleSovereignRites);
                    if (level.FemaleSovereignRites != null) rites.AddRange(level.FemaleSovereignRites);

                    foreach (var rite in rites)
                    {
                        if (rite == null) continue;
                        CreateRewardItem(contentTrans, rite.Icon, $"Rite: {rite.SkillName}", new Color(0.85f, 0.35f, 1f), 130f);
                        hasRewards = true;
                    }
                }

                // Load real/placeholder Loot Item drops
                var lootConfig = level.StageLootConfig;
                if ((lootConfig == null || lootConfig.Count == 0) && (level.LevelID == "1-1" || level.LevelID == "0-1"))
                {
                    lootConfig = new List<LevelData.LevelLootItem>();
                    lootConfig.Add(new LevelData.LevelLootItem { ItemID = "Demonite Shard", DropChance = 0.75f, MinQuantity = 1, MaxQuantity = 2 });
                    lootConfig.Add(new LevelData.LevelLootItem { ItemID = "Soul Core", DropChance = 0.25f, MinQuantity = 1, MaxQuantity = 1 });
                }

                // Spawning Stage Drops / Loot Items
                if (lootConfig != null)
                {
                    foreach (var loot in lootConfig)
                    {
                        Sprite icon = GetRewardSprite(loot.ItemID);
                        CreateRewardItem(contentTrans, icon, $"{loot.ItemID} ({(loot.DropChance * 100f):0}%)", new Color(0.97f, 0.79f, 0.14f), 115f);
                        hasRewards = true;
                    }
                }

                if (!hasRewards)
                {
                    CreateRewardItem(contentTrans, null, "No loot drops", Color.gray, 100f);
                }
            }

            // 3. 1-Time Rewards & Star Objectives Section
            bool hasOneTime = false;
            List<(Sprite icon, string qty)> oneTimeList = new List<(Sprite icon, string qty)>();

            int currentStars = 0;
            if (_saveManager != null && _saveManager.CurrentData != null && _saveManager.CurrentData.LevelStars != null)
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

            // Load real/placeholder Star Conditions
            List<StarCondition> starConditionsList = level.StarConditions;
            if ((starConditionsList == null || starConditionsList.Count == 0) && (level.LevelID == "1-1" || level.LevelID == "0-1"))
            {
                starConditionsList = new List<StarCondition>();
                
                var cond1 = new StarCondition();
                cond1.Description = "Complete Level";
                cond1.Type = StarCondition.ConditionType.CompleteLevel;
                cond1.BonusRewards = new List<MaouSamaTD.Data.RewardData> {
                    new MaouSamaTD.Data.RewardData { Type = MaouSamaTD.Data.RewardType.GoldCoins, Amount = 100 }
                };
                starConditionsList.Add(cond1);

                var cond2 = new StarCondition();
                cond2.Description = "Sovereign HP above 50%";
                cond2.Type = StarCondition.ConditionType.BaseHealth;
                cond2.TargetValue = 50f;
                cond2.BonusRewards = new List<MaouSamaTD.Data.RewardData> {
                    new MaouSamaTD.Data.RewardData { Type = MaouSamaTD.Data.RewardType.BloodCrests, Amount = 15 }
                };
                starConditionsList.Add(cond2);

                var cond3 = new StarCondition();
                cond3.Description = "Finish within 180s";
                cond3.Type = StarCondition.ConditionType.TimeLimit;
                cond3.TargetValue = 180f;
                cond3.BonusRewards = new List<MaouSamaTD.Data.RewardData> {
                    new MaouSamaTD.Data.RewardData { Type = MaouSamaTD.Data.RewardType.Gems, Amount = 5 }
                };
                starConditionsList.Add(cond3);
            }

            if (starConditionsList != null)
            {
                for (int sIdx = 0; sIdx < starConditionsList.Count; sIdx++)
                {
                    var cond = starConditionsList[sIdx];
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
                if (_oneTimeHeader != null)
                {
                    _oneTimeHeader.SetActive(showOneTime);
                    var headerTxt = _oneTimeHeader.GetComponentInChildren<TextMeshProUGUI>();
                    if (headerTxt != null)
                    {
                        headerTxt.text = "[ STAR OBJECTIVES & BONUS ]";
                        headerTxt.fontSize = 12f;
                        headerTxt.fontStyle = FontStyles.Bold;
                        headerTxt.characterSpacing = 1.5f;
                        headerTxt.color = new Color(0.97f, 0.79f, 0.14f);
                    }
                }
                _oneTimeContainer.gameObject.SetActive(showOneTime);

                if (showOneTime)
                {
                    foreach (var itemData in oneTimeList)
                    {
                        CreateRewardItem(_oneTimeContainer, itemData.icon, itemData.qty, Color.white, 90f);
                    }
                }
            }

            // 4. Spawning Enemies Forces Section (Chibis, names, custom detailed movement/rank badges, stats)
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

            // Load real/placeholder Expected Hostile Forces for Level 1-1
            if (uniqueEnemies.Count == 0 && (level.LevelID == "1-1" || level.LevelID == "0-1"))
            {
#if UNITY_EDITOR
                // 1. Search AssetDatabase for any EnemyData assets
                var guids = UnityEditor.AssetDatabase.FindAssets("t:EnemyData");
                foreach (var guid in guids)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var enemy = UnityEditor.AssetDatabase.LoadAssetAtPath<MaouSamaTD.Units.EnemyData>(path);
                    if (enemy != null && !uniqueEnemies.Contains(enemy))
                    {
                        uniqueEnemies.Add(enemy);
                        if (uniqueEnemies.Count >= 2) break; // Load up to 2 actual enemies
                    }
                }
#endif
                // 2. Fallback to high-fidelity mock enemy data if database search had no matches
                if (uniqueEnemies.Count == 0)
                {
                    var mockEnemy1 = ScriptableObject.CreateInstance<MaouSamaTD.Units.EnemyData>();
                    mockEnemy1.EnemyName = "Gehenna Scout";
                    mockEnemy1.MaxHp = 45f;
                    mockEnemy1.MoveSpeed = 2.2f;
                    mockEnemy1.AttackPower = 6f;
                    mockEnemy1.Rank = MaouSamaTD.Units.EnemyRank.Normal;
                    mockEnemy1.MovementType = MaouSamaTD.Units.EnemyMovementType.Ground;
                    uniqueEnemies.Add(mockEnemy1);

                    var mockEnemy2 = ScriptableObject.CreateInstance<MaouSamaTD.Units.EnemyData>();
                    mockEnemy2.EnemyName = "Shadow Harpy";
                    mockEnemy2.MaxHp = 75f;
                    mockEnemy2.MoveSpeed = 1.8f;
                    mockEnemy2.AttackPower = 10f;
                    mockEnemy2.Rank = MaouSamaTD.Units.EnemyRank.Elite;
                    mockEnemy2.MovementType = MaouSamaTD.Units.EnemyMovementType.Flying;
                    uniqueEnemies.Add(mockEnemy2);
                }
            }

            if (_enemiesContainer != null)
            {
                ClearContainer(_enemiesContainer);
                
                var vlg = _enemiesContainer.GetComponent<VerticalLayoutGroup>();
                if (vlg == null)
                {
                    var oldGroup = _enemiesContainer.GetComponent<LayoutGroup>();
                    if (oldGroup != null)
                    {
                        if (Application.isPlaying) Destroy(oldGroup);
                        else DestroyImmediate(oldGroup);
                    }
                    vlg = _enemiesContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                }
                if (vlg != null)
                {
                    vlg.spacing = 8f;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = false;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = false;
                    vlg.padding = new RectOffset(6, 6, 6, 6);
                }

                var csf = _enemiesContainer.GetComponent<ContentSizeFitter>();
                if (csf == null)
                {
                    csf = _enemiesContainer.gameObject.AddComponent<ContentSizeFitter>();
                }
                if (csf != null)
                {
                    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }

                bool hasEnemies = uniqueEnemies.Count > 0 && _rewardPrefab != null;
                if (_enemiesHeader != null)
                {
                    _enemiesHeader.SetActive(hasEnemies);
                    var headerTxt = _enemiesHeader.GetComponentInChildren<TextMeshProUGUI>();
                    if (headerTxt != null)
                    {
                        headerTxt.text = "[ EXPECTED HOSTILE FORCES ]";
                        headerTxt.fontSize = 12f;
                        headerTxt.fontStyle = FontStyles.Bold;
                        headerTxt.characterSpacing = 1.5f;
                        headerTxt.color = new Color(0.97f, 0.79f, 0.14f);
                    }
                }
                _enemiesContainer.gameObject.SetActive(hasEnemies);

                if (hasEnemies)
                {
                    foreach (var enemy in uniqueEnemies)
                    {
                        // Create a premium tactical MonsterCard card GameObject
                        GameObject cardGo = new GameObject("MonsterCard", typeof(RectTransform), typeof(Image));
                        cardGo.transform.SetParent(_enemiesContainer, false);
                        
                        var cardRect = cardGo.GetComponent<RectTransform>();
                        cardRect.sizeDelta = new Vector2(0f, 75f); // Reduced height to fit descriptions cleanly
                        
                        var cardImg = cardGo.GetComponent<Image>();
                        cardImg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f); // Deep circular dark glassmorphism
                        
                        var cardOutline = cardGo.AddComponent<Outline>();
                        cardOutline.effectColor = new Color(0.92f, 0.3f, 0.29f, 0.35f); // Crimson border accent
                        cardOutline.effectDistance = new Vector2(1f, 1f);
                        
                        var cardLayout = cardGo.AddComponent<HorizontalLayoutGroup>();
                        cardLayout.spacing = 12f;
                        cardLayout.padding = new RectOffset(10, 10, 8, 8);
                        cardLayout.childAlignment = TextAnchor.MiddleLeft;
                        cardLayout.childControlWidth = false;
                        cardLayout.childControlHeight = false;
                        cardLayout.childForceExpandWidth = false;
                        cardLayout.childForceExpandHeight = false;

                        // Circular Chibi Image Frame
                        GameObject chibiFrameGo = new GameObject("ChibiFrame", typeof(RectTransform), typeof(Image));
                        chibiFrameGo.transform.SetParent(cardGo.transform, false);
                        var fRect = chibiFrameGo.GetComponent<RectTransform>();
                        fRect.sizeDelta = new Vector2(50f, 50f);
                        var fImg = chibiFrameGo.GetComponent<Image>();
                        fImg.color = new Color(0.08f, 0.08f, 0.1f, 1f); // Dark background
                        var fOutline = chibiFrameGo.AddComponent<Outline>();
                        fOutline.effectColor = new Color(0.97f, 0.79f, 0.14f, 0.4f); // Golden frame
                        fOutline.effectDistance = new Vector2(1f, 1f);

                        GameObject chibiGo = new GameObject("Chibi", typeof(RectTransform), typeof(Image));
                        chibiGo.transform.SetParent(chibiFrameGo.transform, false);
                        var chibiRect = chibiGo.GetComponent<RectTransform>();
                        chibiRect.anchorMin = Vector2.zero;
                        chibiRect.anchorMax = Vector2.one;
                        chibiRect.sizeDelta = Vector2.zero;
                        var chibiImg = chibiGo.GetComponent<Image>();
                        chibiImg.sprite = enemy.EnemySprite ?? enemy.FullBodyArt;
                        chibiImg.preserveAspect = true;
                        
                        // Text & Tactical Info Container
                        GameObject infoGo = new GameObject("InfoContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
                        infoGo.transform.SetParent(cardGo.transform, false);
                        var infoRect = infoGo.GetComponent<RectTransform>();
                        infoRect.sizeDelta = new Vector2(240f, 55f);
                        
                        var infoLayout = infoGo.GetComponent<VerticalLayoutGroup>();
                        infoLayout.spacing = 2f;
                        infoLayout.childAlignment = TextAnchor.MiddleLeft;
                        infoLayout.childControlWidth = true;
                        infoLayout.childControlHeight = false;
                        infoLayout.childForceExpandWidth = true;
                        infoLayout.childForceExpandHeight = false;

                        // Title Line: Name + Badges
                        GameObject titleLineGo = new GameObject("TitleLine", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                        titleLineGo.transform.SetParent(infoGo.transform, false);
                        var tlRect = titleLineGo.GetComponent<RectTransform>();
                        tlRect.sizeDelta = new Vector2(0f, 18f);

                        var tlLayout = titleLineGo.GetComponent<HorizontalLayoutGroup>();
                        tlLayout.spacing = 6f;
                        tlLayout.childAlignment = TextAnchor.MiddleLeft;
                        tlLayout.childControlWidth = false;
                        tlLayout.childControlHeight = false;
                        tlLayout.childForceExpandWidth = false;
                        tlLayout.childForceExpandHeight = false;

                        // Enemy Name
                        GameObject nameGo = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
                        nameGo.transform.SetParent(titleLineGo.transform, false);
                        var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
                        nameTmp.text = enemy.EnemyName;
                        nameTmp.fontSize = 12.5f;
                        nameTmp.fontStyle = FontStyles.Bold;
                        nameTmp.color = Color.white;

                        // Rank Badge (BOSS / ELITE / NORMAL)
                        GameObject rankBadgeGo = new GameObject("RankBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
                        rankBadgeGo.transform.SetParent(titleLineGo.transform, false);
                        var rankTmp = rankBadgeGo.GetComponent<TextMeshProUGUI>();
                        if (enemy.IsBoss || enemy.Rank == MaouSamaTD.Units.EnemyRank.Boss)
                        {
                            rankTmp.text = "<color=#FF3333><size=9.5><b>[BOSS]</b></size></color>";
                        }
                        else if (enemy.Rank == MaouSamaTD.Units.EnemyRank.Elite)
                        {
                            rankTmp.text = "<color=#FF9900><size=9.5><b>[ELITE]</b></size></color>";
                        }
                        else
                        {
                            rankTmp.text = "<color=#BBBBBB><size=9.5><b>[NORMAL]</b></size></color>";
                        }
                        rankTmp.fontSize = 10f;

                        // Movement badge
                        GameObject moveBadgeGo = new GameObject("MoveBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
                        moveBadgeGo.transform.SetParent(titleLineGo.transform, false);
                        var moveTmp = moveBadgeGo.GetComponent<TextMeshProUGUI>();
                        if (enemy.MovementType == MaouSamaTD.Units.EnemyMovementType.Flying)
                        {
                            moveTmp.text = "<color=#FF9933><size=9><b>[AERIAL]</b></size></color>";
                        }
                        else if (enemy.CollisionType == MaouSamaTD.Units.EnemyCollisionType.IgnoreUnits || enemy.EvasionType == MaouSamaTD.Units.EnemyEvasionType.BypassBlockers)
                        {
                            moveTmp.text = "<color=#FF3399><size=9><b>[PHASING]</b></size></color>";
                        }
                        else
                        {
                            moveTmp.text = "<color=#00FFCC><size=9><b>[GROUND]</b></size></color>";
                        }
                        moveTmp.fontSize = 10f;

                        // Stats Line
                        GameObject statsGo = new GameObject("StatsText", typeof(RectTransform), typeof(TextMeshProUGUI));
                        statsGo.transform.SetParent(infoGo.transform, false);
                        var statsTmp = statsGo.GetComponent<TextMeshProUGUI>();
                        statsTmp.text = $"<color=#888888>HP: <color=white>{enemy.MaxHp}</color>  |  Speed: <color=white>{enemy.MoveSpeed:F1}</color>  |  Power: <color=white>{enemy.AttackPower:F1}</color></color>";
                        statsTmp.fontSize = 11f;
                    }
                }
            }

            // 5. Deactivate old reward lists
            if (_replayHeader != null) _replayHeader.SetActive(false);
            if (_replayContainer != null) _replayContainer.gameObject.SetActive(false);
            if (_dropsHeader != null) _dropsHeader.SetActive(false);
            if (_dropsContainer != null) _dropsContainer.gameObject.SetActive(false);

            // Update overlapping navigation buttons
            UpdateOverlappingNavigation();
        }

        private RewardItemUI CreateRewardItem(Transform container, Sprite icon, string quantity, Color? textColor = null, float width = 110f)
        {
            RewardItemUI item = Instantiate(_rewardPrefab, container);
            item.Setup(icon, quantity);
            
            var itemRect = item.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                itemRect.sizeDelta = new Vector2(width, 48f);
            }
            
            var itemText = item.GetComponentInChildren<TextMeshProUGUI>();
            if (itemText != null)
            {
                itemText.fontSize = 10.5f;
                itemText.fontStyle = FontStyles.Bold;
                itemText.color = textColor ?? Color.white;
            }

            var bgImg = item.GetComponent<UnityEngine.UI.Image>();
            if (bgImg != null)
            {
                bgImg.color = new Color(0.12f, 0.12f, 0.15f, 0.85f); // Deep dark semi-transparent glass
                var outline = item.GetComponent<Outline>();
                if (outline == null) outline = item.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.97f, 0.79f, 0.14f, 0.35f); // Gold outline
                outline.effectDistance = new Vector2(1f, 1f);
            }

            return item;
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
            // Layout is baked in the scene — no runtime anchor/offset overrides needed here.

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
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in container)
            {
                children.Add(child.gameObject);
            }
            foreach (var child in children)
            {
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
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
                        bool isUnlocked = !campaignPage.IsLevelLockedInUI(prevLevel);
                        Setup(prevLevel, isUnlocked, _onEngageClicked);
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
                        bool isUnlocked = !campaignPage.IsLevelLockedInUI(nextLevel);
                        Setup(nextLevel, isUnlocked, _onEngageClicked);
                        campaignPage.CenterScrollOnPosition(nextLevel.CampaignMapPosition);
                    });
                }
            }
        }

        private Sprite GetRewardSprite(string rewardName)
        {
            if (string.IsNullOrEmpty(rewardName)) return null;
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
