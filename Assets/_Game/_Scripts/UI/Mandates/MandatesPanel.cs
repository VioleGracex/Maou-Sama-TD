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
        [SerializeField] private Button _tabDaily;
        [SerializeField] private Button _tabEvents;
        [SerializeField] private Button _tabPermanent;
        [SerializeField] private Button _tabOneTime;
        
        [Header("Filters")]
        [SerializeField] private Toggle _toggleShowFinished;
        
        [Header("Navigation")]
        [SerializeField] private Button _btnClose;

        [Inject] private MandateManager _mandateManager;
        [InjectOptional] private UIFlowManager _uiFlow;

        private UIFlowManager UIControl => _uiFlow ?? UIFlowManager.Instance;

        private MandateType _currentTab = MandateType.Daily;
        private List<MandateEntryUI> _activeEntries = new List<MandateEntryUI>();

        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;

        private void Awake()
        {
            if (_tabDaily != null) _tabDaily.onClick.AddListener(() => SwitchTab(MandateType.Daily));
            if (_tabEvents != null) _tabEvents.onClick.AddListener(() => SwitchTab(MandateType.Event));
            if (_tabPermanent != null) _tabPermanent.onClick.AddListener(() => SwitchTab(MandateType.Permanent));
            if (_tabOneTime != null) _tabOneTime.onClick.AddListener(() => SwitchTab(MandateType.OneTime));
            
            if (_toggleShowFinished != null) _toggleShowFinished.onValueChanged.AddListener(_ => RefreshList());
            if (_btnClose != null) _btnClose.onClick.AddListener(RequestCloseAndNavigate);
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
            _currentTab = MandateType.Daily;
            _toggleShowFinished.isOn = true;
        }

        private void SwitchTab(MandateType type)
        {
            _currentTab = type;
            RefreshList();
        }

        public void RefreshList()
        {
            // Clear existing
            foreach (var entry in _activeEntries)
            {
                Destroy(entry.gameObject);
            }
            _activeEntries.Clear();

            // Filter
            var mandates = _mandateManager.AllMandates
                .Where(m => m.Type == _currentTab)
                .ToList();

            if (!_toggleShowFinished.isOn)
            {
                mandates = mandates.Where(m => !_mandateManager.IsClaimed(m.UniqueID)).ToList();
            }

            // Sort: Claimable > In Progress > Claimed
            var sorted = mandates
                .OrderByDescending(m => _mandateManager.CanClaim(m)) // True first
                .ThenBy(m => _mandateManager.IsClaimed(m.UniqueID)) // False first
                .ThenBy(m => m.Title)
                .ToList();

            foreach (var mandate in sorted)
            {
                var entry = Instantiate(_entryPrefab, _entryContainer);
                entry.Setup(mandate, _mandateManager, this);
                _activeEntries.Add(entry);
            }
        }
    }
}
