using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Levels;
using MaouSamaTD.Managers;
using System;
using DG.Tweening;
using System.Collections.Generic;
using Zenject;
using NaughtyAttributes;

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
        [SerializeField] private RectTransform _scrollContent;
        [SerializeField] private TextMeshProUGUI _scrollDescriptionText;

        [Header("Mission Conditions Section")]
        [SerializeField] private GameObject _conditionsHeader;
        [SerializeField] private RectTransform _conditionsContainer;
        [SerializeField] private TextMeshProUGUI _winConditionText;
        [SerializeField] private TextMeshProUGUI _loseConditionText;

        [Header("Spawning Enemies Section")]
        [SerializeField] private GameObject _enemiesHeader;
        [SerializeField] private RectTransform _enemiesContainer;

        [Header("1-Time Rewards Section")]
        [SerializeField] private GameObject _oneTimeHeader;
        [SerializeField] private RectTransform _oneTimeContainer;

        [Header("Replay Victory Rewards Section")]
        [SerializeField] private GameObject _replayHeader;
        [SerializeField] private RectTransform _replayContainer;

        [Header("Stage Drops Section")]
        [SerializeField] private GameObject _dropsHeader;
        [SerializeField] private RectTransform _dropsContainer;

        [Header("Navigation Buttons")]
        [SerializeField] private Button _prevLevelBtn;
        [SerializeField] private Button _nextLevelBtn;
        [SerializeField] private Button _closeButton;

        [Header("Prefabs")]
        [SerializeField] private RewardItemUI _rewardPrefab;
        [SerializeField] private MonsterCardUI _monsterCardPrefab;
        [SerializeField] private GameObject _separatorPrefab;
        
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
                _titleText.enableAutoSizing = false;
                _titleText.fontSize = 24f;
                _titleText.characterSpacing = 0.5f;
                _titleText.color = new Color(1f, 0.75f, 0.15f, 1f); // #FFBF26 Premium Gold
                _titleText.fontStyle = FontStyles.Bold;
                _titleText.alignment = TextAlignmentOptions.TopLeft;
                _titleText.overflowMode = TextOverflowModes.Overflow;
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
                                    if (iconImg != null)
                                    {
                                        if (catIcon != null)
                                        {
                                            iconImg.sprite = catIcon;
                                            iconImg.enabled = true;
                                        }
                                        else
                                        {
                                            iconImg.enabled = false;
                                        }
                                        
                                        if (ColorUtility.TryParseHtmlString(hexColor, out var parsedColor))
                                        {
                                            iconImg.color = parsedColor; // Dynamically color category icon to match cyan/gold/purple/red
                                        }
                                    }
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
                _scrollDescriptionText.fontSize = 15f;
                _scrollDescriptionText.characterSpacing = 0.4f;
                _scrollDescriptionText.lineSpacing = 12f;
                _scrollDescriptionText.color = new Color(0.85f, 0.85f, 0.85f, 0.95f);
                _scrollDescriptionText.alignment = TextAlignmentOptions.TopLeft;
                _scrollDescriptionText.overflowMode = TextOverflowModes.Overflow;
            }

            // 2. Replay & Loot Rewards section
            bool hasReplayRewards = false;
            if (_replayContainer != null)
            {
                ClearContainer(_replayContainer);
                List<MaouSamaTD.Data.RewardData> winRewardsList = level.WinRewards;
                if ((winRewardsList == null || winRewardsList.Count == 0) && (level.LevelID == "1-1" || level.LevelID == "0-1"))
                {
                    winRewardsList = new List<MaouSamaTD.Data.RewardData>();
                    winRewardsList.Add(new MaouSamaTD.Data.RewardData { Type = MaouSamaTD.Data.RewardType.GoldCoins, Amount = 150 });
                    winRewardsList.Add(new MaouSamaTD.Data.RewardData { Type = MaouSamaTD.Data.RewardType.BloodCrests, Amount = 30 });
                    winRewardsList.Add(new MaouSamaTD.Data.RewardData { Type = MaouSamaTD.Data.RewardType.Gems, Amount = 5 });
                }

                if (winRewardsList != null)
                {
                    foreach (var reward in winRewardsList)
                    {
                        if (reward.Amount <= 0) continue;
                        Sprite icon = GetRewardSprite(reward.Type.ToString());
                        string displayName = FormatRewardName(reward.Type.ToString());
                        CreateRewardItem(_replayContainer, icon, $"+{reward.Amount} {displayName}", Color.white, 105f);
                        hasReplayRewards = true;
                    }
                }

                if (level.Category == LevelCategory.RiteDungeon)
                {
                    var rites = new List<MaouSamaTD.Skills.SovereignRiteData>();
                    if (level.MaleSovereignRites != null) rites.AddRange(level.MaleSovereignRites);
                    if (level.FemaleSovereignRites != null) rites.AddRange(level.FemaleSovereignRites);

                    foreach (var rite in rites)
                    {
                        if (rite == null) continue;
                        CreateRewardItem(_replayContainer, rite.Icon, $"Rite: {rite.SkillName}", new Color(0.85f, 0.35f, 1f), 130f);
                        hasReplayRewards = true;
                    }
                }

                if (_replayHeader != null) _replayHeader.SetActive(hasReplayRewards);
                _replayContainer.gameObject.SetActive(hasReplayRewards);
                
                if (hasReplayRewards && _separatorPrefab != null)
                {
                    Instantiate(_separatorPrefab, _replayContainer);
                }
            }

            bool hasDrops = false;
            if (_dropsContainer != null)
            {
                ClearContainer(_dropsContainer);
                var lootConfig = level.StageLootConfig;
                if ((lootConfig == null || lootConfig.Count == 0) && (level.LevelID == "1-1" || level.LevelID == "0-1"))
                {
                    lootConfig = new List<LevelData.LevelLootItem>();
                    lootConfig.Add(new LevelData.LevelLootItem { ItemID = "Demonite Shard", DropChance = 0.75f, MinQuantity = 1, MaxQuantity = 2 });
                    lootConfig.Add(new LevelData.LevelLootItem { ItemID = "Soul Core", DropChance = 0.25f, MinQuantity = 1, MaxQuantity = 1 });
                }

                if (lootConfig != null)
                {
                    foreach (var loot in lootConfig)
                    {
                        Sprite icon = GetRewardSprite(loot.ItemID);
                        CreateRewardItem(_dropsContainer, icon, $"{loot.ItemID} ({(loot.DropChance * 100f):0}%)", new Color(0.97f, 0.79f, 0.14f), 115f);
                        hasDrops = true;
                    }
                }

                if (_dropsHeader != null) _dropsHeader.SetActive(hasDrops);
                _dropsContainer.gameObject.SetActive(hasDrops);
                
                if (hasDrops && _separatorPrefab != null)
                {
                    Instantiate(_separatorPrefab, _dropsContainer);
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
                            
                            // Rich text alignment tag to push reward to the right in a full-width card
                            string displayString = $"⭐ {cond.Description} <align=right>🎁 +{reward.Amount} {FormatRewardName(reward.Type.ToString())}{status}</align>";
                            oneTimeList.Add((rSprite, displayString));
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
                string displayString = $"⭐ Rite Completion <align=right>New Rite{status}</align>";
                oneTimeList.Add((riteSprite, displayString));
                hasOneTime = true;
            }
            else if (level.Category == LevelCategory.VassalDungeon)
            {
                Sprite vassalSprite = null;
#if UNITY_EDITOR
                vassalSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI_Pages/Home/cmd_node_chambers.png");
#endif
                string status = isCleared ? " <color=#778899>[Recruited]</color>" : " <color=#ffd700>[Recruit on Clear]</color>";
                string displayString = $"⭐ Vassal Recruitment <align=right>New Vassal{status}</align>";
                oneTimeList.Add((vassalSprite, displayString));
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
                        headerTxt.text = "STAR OBJECTIVES & BONUS";
                    }
                }
                _oneTimeContainer.gameObject.SetActive(showOneTime);

                if (showOneTime)
                {
                    foreach (var itemData in oneTimeList)
                    {
                        // Hiding the left icon for full-width star objective cards to match HTML reference
                        CreateRewardItem(_oneTimeContainer, null, itemData.qty, Color.white, 90f);
                    }
                    if (_separatorPrefab != null)
                    {
                        Instantiate(_separatorPrefab, _oneTimeContainer);
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
                    vlg.spacing = 16f; // Spacious spacing
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = false;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = false;
                    vlg.padding = new RectOffset(12, 12, 12, 12); // Spacious padding
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
                        headerTxt.text = "EXPECTED HOSTILE FORCES";
                    }
                }
                _enemiesContainer.gameObject.SetActive(hasEnemies);

                if (hasEnemies)
                {
                    foreach (var enemy in uniqueEnemies)
                    {
                        if (_monsterCardPrefab != null)
                        {
                            MonsterCardUI card = Instantiate(_monsterCardPrefab, _enemiesContainer);
                            card.Setup(enemy);
                            var hover = card.GetComponent<UIHoverEffect>();
                            if (hover == null) hover = card.gameObject.AddComponent<UIHoverEffect>();
                            if (hover != null)
                            {
                                hover.Configure(
                                    new Color(0.08f, 0.08f, 0.1f, 0.8f),      // Normal BG: rgba(20, 20, 26, 0.8)
                                    new Color(0.09f, 0.09f, 0.13f, 0.95f),    // Hover BG: rgba(24, 24, 32, 0.95)
                                    new Color(1f, 0.75f, 0.15f, 0.12f),       // Normal Outline: rgba(255, 191, 38, 0.12)
                                    new Color(1f, 0.75f, 0.15f, 0.3f),        // Hover Outline: rgba(255, 191, 38, 0.3)
                                    1.02f
                                );
                            }
                        }
                    }

                    if (_separatorPrefab != null)
                    {
                        Instantiate(_separatorPrefab, _enemiesContainer);
                    }
                }
            }

            // 5. Deactivate old reward lists (Removed because they are now populated above)

            // 6. Mission Conditions Section
            bool hasConditions = true;
            if (_conditionsContainer != null)
            {
                _conditionsContainer.gameObject.SetActive(hasConditions);
                if (_conditionsHeader != null) _conditionsHeader.SetActive(hasConditions);

                if (_winConditionText != null)
                {
                    string winDesc = "";
                    if (level.WinConditions != null && level.WinConditions.Count > 0)
                    {
                        var descriptions = new List<string>();
                        foreach (var cond in level.WinConditions)
                        {
                            if (!string.IsNullOrEmpty(cond.Description)) descriptions.Add(cond.Description);
                        }
                        winDesc = string.Join(", ", descriptions);
                    }
                    if (string.IsNullOrEmpty(winDesc))
                    {
                        winDesc = "Defeat all enemy waves";
                    }
                    _winConditionText.text = $"<color=#10B981>● <b>WIN:</b></color> {winDesc}";
                }

                if (_loseConditionText != null)
                {
                    string loseDesc = "";
                    if (level.LoseConditions != null && level.LoseConditions.Count > 0)
                    {
                        var descriptions = new List<string>();
                        foreach (var cond in level.LoseConditions)
                        {
                            if (!string.IsNullOrEmpty(cond.Description)) descriptions.Add(cond.Description);
                        }
                        loseDesc = string.Join(", ", descriptions);
                    }
                    if (string.IsNullOrEmpty(loseDesc))
                    {
                        loseDesc = "Your core health reaches 0";
                    }
                    _loseConditionText.text = $"<color=#EF4444>● <b>LOSE:</b></color> {loseDesc}";
                }
            }

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
                if (container == _oneTimeContainer)
                {
                    // Let the layout group control the width, enforce a clean height
                    itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, 38f);
                }
                else
                {
                    itemRect.sizeDelta = new Vector2(width, 48f);
                }
            }
            
            var itemText = item.GetComponentInChildren<TextMeshProUGUI>();
            if (itemText != null)
            {
                itemText.fontSize = 14f;
                itemText.fontStyle = FontStyles.Bold;
                itemText.color = textColor ?? Color.white;
            }

            var bgImg = item.GetComponent<UnityEngine.UI.Image>();
            var outline = item.GetComponent<Outline>();
            if (outline == null) outline = item.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, 1f);

            var hover = item.GetComponent<UIHoverEffect>();
            if (hover == null) hover = item.gameObject.AddComponent<UIHoverEffect>();
            if (hover != null)
            {
                if (container == _oneTimeContainer)
                {
                    // Star Objective: full-width card
                    hover.Configure(
                        new Color(0.06f, 0.08f, 0.1f, 0.65f),      // Normal BG: rgba(16, 20, 25, 0.65)
                        new Color(0.08f, 0.1f, 0.12f, 0.85f),      // Hover BG: rgba(20, 24, 30, 0.85)
                        new Color(1f, 1f, 1f, 0.05f),              // Normal Outline: rgba(255, 255, 255, 0.05)
                        new Color(1f, 0.75f, 0.15f, 0.35f),        // Hover Outline: rgba(255, 191, 38, 0.35)
                        1.01f
                    );
                }
                else
                {
                    // Standard capsule/card style (Replay / Drops)
                    hover.Configure(
                        new Color(0.12f, 0.12f, 0.15f, 0.85f),     // Normal BG: rgba(30, 30, 38, 0.85)
                        new Color(0.14f, 0.14f, 0.19f, 0.95f),     // Hover BG: rgba(36, 36, 48, 0.95)
                        new Color(1f, 0.75f, 0.15f, 0.2f),         // Normal Outline: rgba(255, 191, 38, 0.2)
                        new Color(1f, 0.75f, 0.15f, 0.6f),         // Hover Outline: rgba(255, 191, 38, 0.6)
                        1.03f
                    );
                }
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

            // Buttons will remain children of BottomButtonsGroup and be automatically laid out by HorizontalLayoutGroup.
            // No runtime repositioning or SetParent needed here!

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

#if UNITY_EDITOR
        #region Editor Methods
        [Button("Populate Placeholders (Editor Only)")]
        private void PopulatePlaceholders()
        {
            if (!Application.isPlaying)
            {
                ClearContainerEditor(_enemiesContainer);
                ClearContainerEditor(_oneTimeContainer);
                
                if (_monsterCardPrefab != null)
                {
                    UnityEditor.PrefabUtility.InstantiatePrefab(_monsterCardPrefab, _enemiesContainer);
                    UnityEditor.PrefabUtility.InstantiatePrefab(_monsterCardPrefab, _enemiesContainer);
                }
                
                if (_separatorPrefab != null && _enemiesContainer != null)
                {
                    UnityEditor.PrefabUtility.InstantiatePrefab(_separatorPrefab, _enemiesContainer);
                }

                if (_rewardPrefab != null)
                {
                    UnityEditor.PrefabUtility.InstantiatePrefab(_rewardPrefab, _oneTimeContainer);
                    UnityEditor.PrefabUtility.InstantiatePrefab(_rewardPrefab, _oneTimeContainer);
                    UnityEditor.PrefabUtility.InstantiatePrefab(_rewardPrefab, _oneTimeContainer);
                }
                
                if (_separatorPrefab != null && _oneTimeContainer != null)
                {
                    UnityEditor.PrefabUtility.InstantiatePrefab(_separatorPrefab, _oneTimeContainer);
                }

                if (_winConditionText != null) _winConditionText.text = "<color=#10B981>● <b>WIN:</b></color> Defeat 3 Waves of Enemies";
                if (_loseConditionText != null) _loseConditionText.text = "<color=#EF4444>● <b>LOSE:</b></color> Your core health reaches 0";

                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        private void ClearContainerEditor(Transform container)
        {
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(container.GetChild(i).gameObject);
            }
        }
        #endregion
#endif
    }
}
