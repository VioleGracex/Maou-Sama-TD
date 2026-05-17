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
            Debug.Log($"[UnitInspector] Chamber clicked for unit: {_currentUnit.UnitName}");
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
                _canvasGroup.alpha = 0;
                _canvasGroup.DOFade(1, _fadeDuration).SetUpdate(true);
            }

            if (_debug) Debug.Log($"[UnitInspector] Opening for: {_currentUnit.UnitName}");
        }

        public void Close()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.DOFade(0, _fadeDuration).SetUpdate(true).OnComplete(() => 
                {
                    if (_visualRoot != null) _visualRoot.SetActive(false);
                });
            }
            else
            {
                if (_visualRoot != null) _visualRoot.SetActive(false);
            }
        }

        public void ResetState() { }

        public bool RequestClose()
        {
            if (_debug) Debug.Log($"[UnitInspector] RequestClose called. Current tab: {_currentTabIndex}");
            if (_currentTabIndex != 0)
            {
                if (_debug) Debug.Log("[UnitInspector] Tab is not 0. Switching to tab 0 and blocking close.");
                SwitchTab(0); 
                return false; 
            }
            if (_debug) Debug.Log("[UnitInspector] Tab is 0. Allowing close.");
            return true; 
        }

        private void SwitchTab(int index)
        {
            if (_debug) Debug.Log($"[UnitInspector] SwitchTab to: {index}");
            _currentTabIndex = index;
            if (_contentStats) _contentStats.SetActive(index == 0);
            if (_contentSkills) _contentSkills.SetActive(index == 1);
            if (_contentResonance) _contentResonance.SetActive(index == 2);
            if (_contentSkins) _contentSkins.SetActive(index == 3);
            if (_contentXP) _contentXP.SetActive(index == 4);

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
                case 2: _resonancePanel?.Refresh(_currentUnit); break;
                case 3: _skinsPanel?.Refresh(_currentUnit); break;
                case 4: _xpPanel?.Refresh(_currentUnit); break;
            }
            // Always refresh header as stats/levels might change
            _header?.Refresh(_currentUnit);
        }
    }
}

