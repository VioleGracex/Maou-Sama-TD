using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;
using MaouSamaTD.Data;
using MaouSamaTD.UI;
using MaouSamaTD.Mandates;


namespace MaouSamaTD.UI.Mandates
{
    public class MandatesPanel : MonoBehaviour, IUIController
    {
        [Header("UI References")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Transform _entryContainer;
        [SerializeField] private MandateEntryUI _entryPrefab;
        
        [Header("Tabs")]
        [SerializeField] private Button _tabAll;
        [SerializeField] private Button _tabDaily;
        [SerializeField] private Button _tabWeekly;
        [SerializeField] private Button _tabStory;
        
        [Header("Actions")]
        [SerializeField] private Button _btnSeizeAll;
        
        [Header("Filters")]
        [SerializeField] private MaouSamaTD.UI.Common.CustomToggle _toggleShowFinished;

        [Inject] private MandateManager _mandateManager;
        [InjectOptional] private UIFlowManager _uiFlow;

        private UIFlowManager UIControl => _uiFlow ?? UIFlowManager.Instance;

        [Header("Tab Visuals")]
        [SerializeField] private Color _activeColor = Color.white;
        [SerializeField] private Color _inactiveColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private TextMeshProUGUI[] _tabLabels; // Order: All, Daily, Weekly, Story
        [SerializeField] private GameObject[] _underlineIndicators;

        private MandateType? _currentTab = null; // null = All Mandates
        private List<MandateEntryUI> _activeEntries = new List<MandateEntryUI>();

        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;
        [SerializeField] private NavigationFeatures _navFeatures = NavigationFeatures.BackButton | NavigationFeatures.CitadelButton;
        public NavigationFeatures ConfiguredNavFeatures => _navFeatures;

        private void Awake()
        {
            if (_tabAll != null) _tabAll.onClick.AddListener(() => SwitchTab(null, 0));
            if (_tabDaily != null) _tabDaily.onClick.AddListener(() => SwitchTab(MandateType.Daily, 1));
            if (_tabWeekly != null) _tabWeekly.onClick.AddListener(() => SwitchTab(MandateType.Weekly, 2));
            if (_tabStory != null) _tabStory.onClick.AddListener(() => SwitchTab(MandateType.StoryAndLegacy, 3));
            
            if (_btnSeizeAll != null) _btnSeizeAll.onClick.AddListener(OnSeizeAllClicked);
            
            if (_toggleShowFinished != null) _toggleShowFinished.OnValueChanged.AddListener(_ => RefreshList());

            UpdateTabVisuals(0); // Default to "All"
        }

        public void Open()
        {
            _visualRoot.SetActive(true);
            RefreshList();
        }

        public void Close()
        {
            _visualRoot.SetActive(false);
        }

        public bool RequestClose() => true;

        public void Preheat()
        {
            // Optional: Initialize data if needed
        }

        private void RequestCloseAndNavigate()
        {
            if (RequestClose()) 
            {
                if (UIControl != null) UIControl.GoBack();
                else Close(); // Fallback if no flow manager
            }
        }

        public void ResetState()
        {
            _currentTab = null;
            if (_toggleShowFinished != null) _toggleShowFinished.SetIsOn(true, false);
            UpdateTabVisuals(0);
        }

        private void SwitchTab(MandateType? type, int tabIndex)
        {
            _currentTab = type;
            UpdateTabVisuals(tabIndex);
            RefreshList();
        }

        private void UpdateTabVisuals(int activeIndex)
        {
            for (int i = 0; i < _tabLabels.Length; i++)
            {
                if (_tabLabels[i] != null)
                    _tabLabels[i].color = (i == activeIndex) ? _activeColor : _inactiveColor;
                
                if (_underlineIndicators != null && i < _underlineIndicators.Length && _underlineIndicators[i] != null)
                    _underlineIndicators[i].SetActive(i == activeIndex);
            }
        }

        private void OnSeizeAllClicked()
        {
            int claimedCount = _mandateManager.ClaimAll(_currentTab);
            if (claimedCount > 0)
            {
                RefreshList();
                Debug.Log($"[MandatesPanel] Seize All claimed {claimedCount} mandates.");
            }
        }

        public void RefreshList()
        {
            // Clear existing logic-tracked entries
            foreach (var entry in _activeEntries)
            {
                if (entry != null) Destroy(entry.gameObject);
            }
            _activeEntries.Clear();

            // Also clear any hard-coded children in the container (ghost leftovers from editor)
            if (_entryContainer != null)
            {
                foreach (Transform child in _entryContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            // Filter
            if (_mandateManager == null) return;
            var mandates = _mandateManager.AllMandates.AsEnumerable();
            
            if (_currentTab.HasValue)
            {
                mandates = mandates.Where(m => m.Type == _currentTab.Value);
            }

            if (_toggleShowFinished != null && !_toggleShowFinished.IsOn)
            {
                mandates = mandates.Where(m => !_mandateManager.IsClaimed(m.UniqueID));
            }

            // Sort: Claimable > In Progress > Claimed
            var sorted = mandates
                .OrderByDescending(m => _mandateManager.CanClaim(m)) // True first
                .ThenBy(m => _mandateManager.IsClaimed(m.UniqueID)) // False first
                .ThenBy(m => m.Title)
                .ToList();

            if (_entryContainer != null)
            {
                foreach (var mandate in sorted)
                {
                    var entry = Instantiate(_entryPrefab, _entryContainer);
                    entry.Setup(mandate, _mandateManager, this);
                    _activeEntries.Add(entry);
                }
            }
        }
    }
}
