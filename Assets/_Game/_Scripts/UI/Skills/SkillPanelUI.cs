using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Skills;
using DG.Tweening;
using Zenject;
using MaouSamaTD.Managers;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI.Skills
{
    public class SkillPanelUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private Transform _buttonContainer;
        [SerializeField] private SkillButtonUI _buttonPrefab;
        private List<SovereignRiteData> _skillsToDisplay = new List<SovereignRiteData>();

        [Header("Animation")]
        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private UnityEngine.UI.Button _toggleButton;
        [SerializeField] private float _hideOffset = 300f; // Distance to move right
        
        [Inject] private SkillManager _skillManager;
        [Inject] private InteractionManager _interactionManager;
        [Inject] private BattleCurrencyManager _currencyManager;
        [Inject] private MaouSamaTD.Managers.GameSelectionState _gameSelectionState;
        [Inject(Optional = true)] private TutorialManager _tutorialManager;
        
        private List<SkillButtonUI> _spawnedButtons = new List<SkillButtonUI>();
        private bool _isVisible = false; // Default: Docked/Hidden
        public bool IsVisible => _isVisible;
        private Vector2 _visiblePos;

        // Container references for the swipe animation
        private RectTransform _buttonsContainerRect;
        private RectTransform _descriptionContainerRect;
        
        // References to description UI elements
        private TMPro.TextMeshProUGUI _skillNameTxt;
        private TMPro.TextMeshProUGUI _skillCostTxt;
        private UnityEngine.UI.Image _skillIconImg;
        private TMPro.TextMeshProUGUI _skillInfoTxt;   // lore / flavour text
        private TMPro.TextMeshProUGUI _skillStatsTxt;  // colored stats block
        private RangePatternUI _rangePatternUI;
        private TMPro.TextMeshProUGUI _rangeStatsTxt;

        private void OnEnable()
        {
            if (_toggleButton != null)
                _toggleButton.onClick.AddListener(ToggleVisibility);

            if (_interactionManager != null)
            {
                _interactionManager.OnSkillSelectedChanged += HandleSkillSelectedChanged;
            }
        }

        private void OnDisable()
        {
            if (_toggleButton != null)
                _toggleButton.onClick.RemoveListener(ToggleVisibility);

            if (_interactionManager != null)
            {
                _interactionManager.OnSkillSelectedChanged -= HandleSkillSelectedChanged;
            }
        }

        private void Start()
        {
            CheckTutorialDock();

            if (_panelRect != null) 
            {
                _visiblePos = _panelRect.anchoredPosition;
                // Force initial position to Hidden (Docked)
                _panelRect.anchoredPosition = _visiblePos + new Vector2(_hideOffset, 0); 
                
                // Only set inactive if Level 1, to prevent it from hiding the toggle entirely if it's part of the same hierarchy or breaking the softlock
                bool isLevel1 = _gameSelectionState != null && _gameSelectionState.SelectedLevel != null && 
                                (_gameSelectionState.SelectedLevel.LevelIndex == 1 || _gameSelectionState.SelectedLevel.LevelID == "1-1");
                
                // Ensure it is explicitly activated in Level 2+
                _panelRect.gameObject.SetActive(!isLevel1);

                // Dynamically find and setup containers and sub-elements
                SetupUiReferences();
            }

            if (_toggleButton != null)
            {
                 _toggleButton.gameObject.name = "SovereignRiteToggle";
                 var txt = _toggleButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                 if (txt != null) txt.text = "Show"; // Initial state is Hidden, so button says "Show"
            }
        }

        private void SetupUiReferences()
        {
            if (_panelRect == null) return;

            Transform middleArea = _panelRect.transform.Find("MiddleArea");
            if (middleArea == null) return;

            // 1. Get container references
            Transform sbc = middleArea.Find("SkillButtons_Container");
            if (sbc != null) _buttonsContainerRect = sbc.GetComponent<RectTransform>();

            Transform sdc = middleArea.Find("SkillDescription_Container");
            if (sdc != null)
            {
                _descriptionContainerRect = sdc.GetComponent<RectTransform>();

                // Set up click/untoggle handler on description container
                var clickImg = sdc.GetComponent<UnityEngine.UI.Image>();
                if (clickImg == null)
                {
                    clickImg = sdc.gameObject.AddComponent<UnityEngine.UI.Image>();
                    clickImg.color = new Color(0, 0, 0, 0.01f); // Transparent but fully raycastable
                }
                var clickBtn = sdc.GetComponent<UnityEngine.UI.Button>();
                if (clickBtn == null)
                {
                    clickBtn = sdc.gameObject.AddComponent<UnityEngine.UI.Button>();
                }
                clickBtn.onClick.RemoveAllListeners();
                clickBtn.onClick.AddListener(() => {
                    _interactionManager?.DeselectSkill();
                });

                // Find close button
                Transform closeBtnTrans = sdc.Find("CloseButton");
                if (closeBtnTrans != null)
                {
                    var closeBtn = closeBtnTrans.GetComponent<UnityEngine.UI.Button>();
                    if (closeBtn != null)
                    {
                        closeBtn.onClick.RemoveAllListeners();
                        closeBtn.onClick.AddListener(() => {
                            _interactionManager?.DeselectSkill();
                        });
                    }
                }

                // 2. Get Title components
                Transform skillTitle = sdc.Find("Skill_Title");
                if (skillTitle != null)
                {
                    Transform iconTrans = skillTitle.Find("Skill_Icon");
                    if (iconTrans != null) _skillIconImg = iconTrans.GetComponent<UnityEngine.UI.Image>();

                    Transform nameTrans = skillTitle.Find("SkillName_Txt");
                    if (nameTrans != null) _skillNameTxt = nameTrans.GetComponent<TMPro.TextMeshProUGUI>();

                    Transform costTrans = skillTitle.Find("SkillCost_Txt");
                    if (costTrans != null) _skillCostTxt = costTrans.GetComponent<TMPro.TextMeshProUGUI>();
                }

                // 3a. Lore description text
                Transform descTrans = sdc.Find("MiddleSplit/Description_BG/Skill_Info_Txt")
                                   ?? sdc.Find("MiddleSplit/Skill_Info_Txt")
                                   ?? sdc.Find("Skill_Info_Txt");
                if (descTrans != null)
                {
                    _skillInfoTxt = descTrans.GetComponent<TMPro.TextMeshProUGUI>();
                    if (_skillInfoTxt != null)
                    {
                        _skillInfoTxt.enableAutoSizing = true;
                        _skillInfoTxt.fontSizeMin = 8f;
                        _skillInfoTxt.fontSizeMax = 13f;
                        _skillInfoTxt.enableWordWrapping = true;
                        _skillInfoTxt.fontStyle = TMPro.FontStyles.Italic;
                        _skillInfoTxt.color = new Color(0.88f, 0.88f, 0.95f, 1f);
                    }
                }

                // 3b. Stats block text (new, lives beside lore inside Description_BG)
                Transform statsTrans2 = sdc.Find("MiddleSplit/Description_BG/Skill_Stats_Txt");
                if (statsTrans2 != null)
                {
                    _skillStatsTxt = statsTrans2.GetComponent<TMPro.TextMeshProUGUI>();
                    if (_skillStatsTxt != null)
                    {
                        _skillStatsTxt.enableAutoSizing = true;
                        _skillStatsTxt.fontSizeMin = 7f;
                        _skillStatsTxt.fontSizeMax = 12f;
                        _skillStatsTxt.enableWordWrapping = true;
                        _skillStatsTxt.fontStyle = TMPro.FontStyles.Normal;
                    }
                }

                // 4. Get RangeGrid
                Transform gridTrans = sdc.Find("MiddleSplit/Range_Container/RangeGrid") ?? sdc.Find("RangeGrid");
                if (gridTrans != null)
                {
                    _rangePatternUI = gridTrans.GetComponent<RangePatternUI>();

                    // RangeGrid_StatsTxt lives directly under Range_Container (not under RangeGrid)
                    Transform rangeContainer = gridTrans.parent;
                    Transform statsTrans = rangeContainer?.Find("RangeGrid_StatsTxt")
                                       ?? sdc.Find("RangeGrid_StatsTxt");
                    if (statsTrans == null)
                    {
                        var statsGo = new GameObject("RangeGrid_StatsTxt", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                        statsGo.transform.SetParent(rangeContainer != null ? rangeContainer : sdc, false);
                        statsTrans = statsGo.transform;
                        var srt = statsTrans.GetComponent<RectTransform>();
                        srt.anchorMin = new Vector2(0f, 0f);
                        srt.anchorMax = new Vector2(1f, 0f);
                        srt.pivot     = new Vector2(0.5f, 0f);
                        srt.anchoredPosition = new Vector2(0f, 4f);
                        srt.sizeDelta = new Vector2(0f, 28f);
                    }
                    _rangeStatsTxt = statsTrans.GetComponent<TMPro.TextMeshProUGUI>();
                    if (_rangeStatsTxt != null)
                    {
                        _rangeStatsTxt.fontSize        = 10f;
                        _rangeStatsTxt.alignment       = TMPro.TextAlignmentOptions.Center;
                        _rangeStatsTxt.fontStyle       = TMPro.FontStyles.Bold;
                        _rangeStatsTxt.enableWordWrapping = true;
                        _rangeStatsTxt.color           = new Color(0.85f, 0.75f, 0.50f, 0.9f);
                    }
                }
            }

            // Lock initial states of both containers
            if (_buttonsContainerRect != null)
            {
                _buttonsContainerRect.anchoredPosition = new Vector2(0f, _buttonsContainerRect.anchoredPosition.y);
            }
            if (_descriptionContainerRect != null)
            {
                _descriptionContainerRect.anchoredPosition = new Vector2(360f, _descriptionContainerRect.anchoredPosition.y);
                _descriptionContainerRect.gameObject.SetActive(false);
            }
        }

        private void CheckTutorialDock()
        {
            if (_gameSelectionState != null && _gameSelectionState.SelectedLevel != null)
            {
                // Level 1: No Sovereign Rites allowed/visible (Tutorial)
                if (_gameSelectionState.SelectedLevel.LevelIndex == 1 || _gameSelectionState.SelectedLevel.LevelID == "1-1")
                {
                    if (_toggleButton != null) _toggleButton.gameObject.SetActive(false);
                    gameObject.SetActive(false);
                }
            }
        }

        public void Init(List<SovereignRiteData> skills)
        {
            CheckTutorialDock();

            bool isLevel1 = _gameSelectionState != null && _gameSelectionState.SelectedLevel != null && 
                            (_gameSelectionState.SelectedLevel.LevelIndex == 1 || _gameSelectionState.SelectedLevel.LevelID == "1-1");

            if (isLevel1 || skills == null || skills.Count == 0)
            {
                if (_toggleButton != null) _toggleButton.gameObject.SetActive(false);
                gameObject.SetActive(false);
                return;
            }
            else
            {
                gameObject.SetActive(true);
            }

            if (!gameObject.activeSelf) return;

            _skillsToDisplay.Clear();
            if (skills != null)
            {
                _skillsToDisplay.AddRange(skills);
            }
            Refresh();
        }

        public void Refresh()
        {
            // Clear old
            foreach (var btn in _spawnedButtons) Destroy(btn.gameObject);
            _spawnedButtons.Clear();

            // Spawn new
            foreach (var skill in _skillsToDisplay)
            {
                if (skill == null) continue;
                
                var btn = Instantiate(_buttonPrefab, _buttonContainer);
                btn.Initialize(skill, _skillManager, _interactionManager, _currencyManager);
                
                // Name the button based on skill asset name for Tutorial Targeting
                string btnName = "SkillButton_" + skill.name.Replace(" ", "");
                btn.gameObject.name = btnName;

                _spawnedButtons.Add(btn);
            }
        }

        public void ToggleVisibility()
        {
            if (_panelRect == null) return;
            
            _isVisible = !_isVisible;
            
            if (_isVisible && !_panelRect.gameObject.activeSelf)
            {
                _panelRect.gameObject.SetActive(true);
            }
            
            // Move Right on Hide
            Vector2 targetPos = _isVisible ? _visiblePos : _visiblePos + new Vector2(_hideOffset, 0);
            
            _panelRect.DOAnchorPos(targetPos, 0.3f).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(() => {
                if (_isVisible)
                {
                    _tutorialManager?.OnActionTriggered("RiteMenuOpened");
                }
            });
            
            // Fix: Update Text
            if (_toggleButton != null)
            {
                var txt = _toggleButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = _isVisible ? "Hide" : "Show"; 
                }
            }

            // Always hide the tutorial hand when the panel state changes —
            // the TutorialManager will re-show it on the next highlight refresh.
            _tutorialManager?.HideHand();
        }

        private void HandleSkillSelectedChanged(SovereignRiteData skill)
        {
            if (skill != null)
            {
                // Update description UI fields first
                UpdateSkillDescriptionUI(skill);

                // Swapped/selected: Slide buttons out to Left (-360), slide description in from Right (0)
                if (_buttonsContainerRect != null)
                {
                    _buttonsContainerRect.DOKill();
                    _buttonsContainerRect.DOAnchorPosX(-360f, 0.25f).SetEase(Ease.OutQuad).SetUpdate(true);
                }

                if (_descriptionContainerRect != null)
                {
                    _descriptionContainerRect.DOKill();
                    _descriptionContainerRect.gameObject.SetActive(true);
                    _descriptionContainerRect.DOAnchorPosX(0f, 0.25f).SetEase(Ease.OutQuad).SetUpdate(true);
                }
            }
            else
            {
                // Idle state: Slide buttons back to center (0), slide description out to Right (360)
                if (_buttonsContainerRect != null)
                {
                    _buttonsContainerRect.DOKill();
                    _buttonsContainerRect.DOAnchorPosX(0f, 0.25f).SetEase(Ease.OutQuad).SetUpdate(true);
                }

                if (_descriptionContainerRect != null)
                {
                    _descriptionContainerRect.DOKill();
                    _descriptionContainerRect.DOAnchorPosX(360f, 0.25f).SetEase(Ease.OutQuad).SetUpdate(true).OnComplete(() => {
                        // Keep container disabled when fully hidden to optimize UI performance
                        if (_interactionManager == null || _interactionManager.SelectedSkill == null)
                        {
                            if (_descriptionContainerRect != null) _descriptionContainerRect.gameObject.SetActive(false);
                        }
                    });
                }
            }
        }

        private void UpdateSkillDescriptionUI(SovereignRiteData skill)
        {
            if (skill == null) return;

            // 1. Skill name — no wrap, ellipsis
            if (_skillNameTxt != null)
            {
                _skillNameTxt.text = skill.SkillName;
                _skillNameTxt.enableWordWrapping = false;
                _skillNameTxt.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            }

            // 2. SP cost — purple, no wrap
            if (_skillCostTxt != null)
            {
                _skillCostTxt.text = $"<color=#CC88FF><b>{skill.SealCost} SP</b></color>";
                _skillCostTxt.enableWordWrapping = false;
                _skillCostTxt.overflowMode = TMPro.TextOverflowModes.Overflow;
            }

            // 3. Icon
            if (_skillIconImg != null)
            {
                _skillIconImg.sprite = skill.Icon;
                _skillIconImg.gameObject.SetActive(skill.Icon != null);
            }

            // 4. LORE — plain narrative text from SO (italic, soft blue-white, auto-size)
            if (_skillInfoTxt != null)
                _skillInfoTxt.text = skill.Description;

            // 5. STATS — generated from SO data with unified color tokens (auto-size)
            if (_skillStatsTxt != null)
            {
                var sb = new System.Text.StringBuilder();

                // Target
                string targetLabel = skill.TargetType == SkillTargetType.Tile ? "Tile" : "Unit";
                sb.AppendLine($"<color=#AAAAAA>Target</color>  <color=#44CCFF><b>{targetLabel}</b></color>");

                // Damage
                if (skill.EffectType == SkillEffectType.Damage)
                    sb.AppendLine($"<color=#AAAAAA>Damage</color>  <color=#FF4444><b>{skill.Value:N0} Magic DMG</b></color>");

                // Buff modifiers
                if (skill.EffectType == SkillEffectType.Buff && skill.Modifiers != null)
                {
                    foreach (var mod in skill.Modifiers)
                        sb.AppendLine($"<color=#AAAAAA>{mod.Stat}</color>  <color=#44FF88><b>+{mod.Value}%</b></color>");
                }

                // Duration
                if (skill.Duration > 0)
                    sb.AppendLine($"<color=#AAAAAA>Duration</color>  <color=#44CCFF><b>{skill.Duration:F0}s</b></color>");

                // Area
                if (skill.Radius > 0)
                    sb.AppendLine($"<color=#AAAAAA>Area</color>  <color=#FFDD44><b>{skill.AoeShape} r{skill.Radius:F0}</b></color>");
                else
                    sb.AppendLine($"<color=#AAAAAA>Area</color>  <color=#FFDD44><b>Single Point</b></color>");

                _skillStatsTxt.text = sb.ToString().TrimEnd();
            }

            // 6. Range grid pattern
            if (_rangePatternUI != null)
            {
                AttackPattern pattern = AttackPattern.All;
                if (skill.AoeShape == AoeShape.Cross)      pattern = AttackPattern.Cross;
                else if (skill.AoeShape == AoeShape.DiagonalX) pattern = AttackPattern.Diagonal;

                int range = Mathf.RoundToInt(skill.Radius);
                _rangePatternUI.SetPattern(pattern, range);
            }

            // 7. Range stats label below grid
            if (_rangeStatsTxt != null)
            {
                string shape    = skill.Radius > 0 ? skill.AoeShape.ToString() : "Point";
                string sizeDesc = skill.Radius > 0 ? $"{skill.Radius:F0}x{skill.Radius:F0}" : "1 Tile";
                _rangeStatsTxt.text =
                    $"<b><color=#FFDD44>{shape}</color>  <color=#44CCFF>{sizeDesc}</color></b>";
                _rangeStatsTxt.enableAutoSizing = true;
                _rangeStatsTxt.fontSizeMin = 7f;
                _rangeStatsTxt.fontSizeMax = 11f;
            }
        }
    }
}
