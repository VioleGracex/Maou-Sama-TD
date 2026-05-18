using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using MaouSamaTD.Units;
using MaouSamaTD.Data;
using System.Collections.Generic;
using MaouSamaTD.Progression;
using Zenject;
using MaouSamaTD.Managers;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Modular coordinator for the full-screen unit inspector.
    /// Delegates specific UI logic to specialized sub-managers.
    /// </summary>
    public class UnitInspectorFullScreenUI : MonoBehaviour, IUIController
    {
        [Header("UI Controller Architecture")]
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;
        [SerializeField] private NavigationFeatures _navFeatures = NavigationFeatures.BackButton | NavigationFeatures.CitadelButton;
        public NavigationFeatures ConfiguredNavFeatures => _navFeatures;

        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.3f;

        [Header("Sub-Managers")]
        [SerializeField] private UnitInspectorHeader _header;
        [SerializeField] private UnitInspectorStatsPanel _statsPanel;
        [SerializeField] private UnitInspectorSkillsPanel _skillsPanel;
        [SerializeField] private UnitInspectorXPPanel _xpPanel;
        [SerializeField] private UnitInspectorSkinsPanel _skinsPanel;
        [SerializeField] private UnitInspectorResonancePanel _resonancePanel;

        [Header("Tab Content Roots")]
        [SerializeField] private GameObject _contentStats;
        [SerializeField] private GameObject _contentSkills;
        [SerializeField] private GameObject _contentResonance;
        [SerializeField] private GameObject _contentSkins;
        [SerializeField] private GameObject _contentXP;

        [Header("Navigation Buttons")]
        [SerializeField] private Button _btnHome;
        [SerializeField] private Button _btnClose;
        [SerializeField] private Button _btnEXP;
        [SerializeField] private Button _btnUpgradeSkill;
        [SerializeField] private Button _btnSkins;
        [SerializeField] private Button _btnPromote;
        [SerializeField] private Button _btnLevelUp;
        [SerializeField] private Button _btnChamber;

        [Header("Debug")]
        [SerializeField] private bool _debug = true;

        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;
        [Inject] private MaouSamaTD.Managers.EconomyManager _economyManager;

        private UnitData _currentUnit;
        private int _currentTabIndex = 0;

        private void Start()
        {
            // Navigation handled by UIFlowManager via IUIController. 
            // Manual listeners removed to prevent "double-back" bugs.
            if (_btnHome) _btnHome.onClick.AddListener(() => UIFlowManager.Instance.ClearHistory(true, true));
            // FIXED: Removed redundant _btnClose listener which caused double-back calls.
            
            // Sub-Panel Navigation
            if (_btnEXP || _btnLevelUp) 
            {
                if (_btnEXP) _btnEXP.onClick.AddListener(() => SwitchTab(4));
                if (_btnLevelUp) _btnLevelUp.onClick.AddListener(() => SwitchTab(4));
            }
            if (_btnSkins) _btnSkins.onClick.AddListener(() => SwitchTab(3));
            if (_btnPromote) _btnPromote.onClick.AddListener(() => SwitchTab(2));
            if (_btnUpgradeSkill) _btnUpgradeSkill.onClick.AddListener(() => SwitchTab(1));
            if (_btnChamber) _btnChamber.onClick.AddListener(OnChamberClicked);

            // Initialize Sub-Managers
            if (_xpPanel)        _xpPanel.Initialize(_saveManager);
            if (_skinsPanel)     _skinsPanel.Initialize();
            if (_resonancePanel) _resonancePanel.Initialize(_saveManager, _economyManager);
        }

        private void OnChamberClicked()
        {
            if (_currentUnit == null) return;
            var unit = _currentUnit;
            // 1. Clear UIFlowManager history completely to close both this inspector and the underlying VassalManager list.
            UIFlowManager.Instance.ClearHistory(true, true);
            
            var chambersPage = Object.FindFirstObjectByType<MaouSamaTD.UI.Vassals.ChambersPageUI>(FindObjectsInactive.Include);
            if (chambersPage != null)
            {
                UIFlowManager.Instance.OpenPanel(chambersPage);
                chambersPage.SelectUnit(unit);
            }
            else
            {
                Debug.LogWarning("[UnitInspector] ChambersPageUI not found!");
            }
        }

        public void SetUnit(UnitData unit)
        {
            _currentUnit = unit;
        }

        public void Open(UnitData unit)
        {
            SetUnit(unit);
            Open();
        }

        public void Open()
        {
            if (_currentUnit == null) return;

            RefreshAllPanels();
            SwitchTab(0);

            if (_visualRoot != null) _visualRoot.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            if (_debug) Debug.Log($"[UnitInspector] Opening for: {_currentUnit.UnitName}");
        }

        public void Close()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        public void ResetState() { }

        public bool RequestClose()
        {
            if (_debug) Debug.Log($"[UnitInspector] RequestClose called. Current tab: {_currentTabIndex}");
            if (_currentTabIndex != 0)
            {
                if (_currentTabIndex == 5)
                {
                    // Allow immediate closing if we are in the Chambers page (tab 5)
                    return true;
                }
                if (_debug) Debug.Log("[UnitInspector] Tab is not 0. Switching to tab 0 and blocking close.");
                SwitchTab(0); 
                return false; 
            }
            if (_debug) Debug.Log("[UnitInspector] Tab is 0. Allowing close.");
            return true; 
        }

        public void SwitchTab(int index)
        {
            if (_debug) Debug.Log($"[UnitInspector] SwitchTab to: {index}");
            _currentTabIndex = index;

            // Get Main_Content children
            Transform mainLeft = null;
            Transform mainPortrait = null;
            Transform mainRight = null;
            Transform mainTopBtns = null;

            if (_visualRoot != null)
            {
                var mainContent = _visualRoot.transform.Find("Main_Content");
                if (mainContent != null)
                {
                    mainLeft = mainContent.Find("Details_LeftSide");
                    mainPortrait = mainContent.Find("Character_Panel");
                    mainRight = mainContent.Find("Details_RightSide");
                    mainTopBtns = mainContent.Find("TopMiddle_Btns");
                }
            }

            bool isMainPage = (index == 0);

            // Set main page elements active/inactive
            if (mainLeft) mainLeft.gameObject.SetActive(isMainPage);
            if (mainPortrait) mainPortrait.gameObject.SetActive(isMainPage);
            if (mainRight) mainRight.gameObject.SetActive(isMainPage);
            if (mainTopBtns) mainTopBtns.gameObject.SetActive(isMainPage);

            // Ensure stats and skills panels are active if on main page
            if (_contentStats) _contentStats.SetActive(isMainPage);
            if (_contentSkills) _contentSkills.SetActive(isMainPage);

            // Set sub-pages active/inactive
            if (_contentResonance) _contentResonance.SetActive(index == 2 || index == 5);
            if (_contentSkins) _contentSkins.SetActive(index == 3);
            if (_contentXP) _contentXP.SetActive(index == 4); // assigned to Unit_Leveling_Page

            if (_btnHome) _btnHome.gameObject.SetActive(index != 3);

            RefreshActivePanel();
        }

        private void RefreshAllPanels()
        {
            if (_header)          _header.Refresh(_currentUnit);
            if (_statsPanel)      _statsPanel.Refresh(_currentUnit);
            if (_skillsPanel)     _skillsPanel.Refresh(_currentUnit);
            if (_xpPanel)         _xpPanel.Refresh(_currentUnit);
            if (_skinsPanel)      _skinsPanel.Refresh(_currentUnit);
            if (_resonancePanel)  _resonancePanel.Refresh(_currentUnit);
        }

        private void RefreshActivePanel()
        {
            switch (_currentTabIndex)
            {
                case 0: _statsPanel?.Refresh(_currentUnit); break;
                case 2: _resonancePanel?.OpenAsResonance(_currentUnit); break;
                case 3: _skinsPanel?.Refresh(_currentUnit); break;
                case 4: _xpPanel?.Refresh(_currentUnit); break;
                case 5: _resonancePanel?.OpenAsChamber(_currentUnit); break;
            }
            // Always refresh header as stats/levels might change
            _header?.Refresh(_currentUnit);
        }
    }
}

