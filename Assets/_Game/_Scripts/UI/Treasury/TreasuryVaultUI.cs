using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MaouSamaTD.Data;
using MaouSamaTD.Managers;
using Zenject;
using DG.Tweening;

namespace MaouSamaTD.UI.Treasury
{
    public class TreasuryVaultUI : MonoBehaviour, IUIController
    {
        [Header("IUIController Architecture")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private bool _addsToHistory = true;
        [SerializeField] private NavigationFeatures _navFeatures = NavigationFeatures.BackButton | NavigationFeatures.CitadelButton;

        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => _addsToHistory;
        public NavigationFeatures ConfiguredNavFeatures => _navFeatures;

        [Header("UI References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [Header("Grids")]
        [SerializeField] private RectTransform _offeringGrid;
        [SerializeField] private RectTransform _skinsGrid;
        [SerializeField] private RectTransform _giftsGrid;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject _offeringPackPrefab;
        [SerializeField] private GameObject _skinPackPrefab;
        [SerializeField] private GameObject _giftPackPrefab;

        [Header("Navigation Tabs")]
        [SerializeField] private Button _offeringsTabBtn;
        [SerializeField] private Button _skinsTabBtn;
        [SerializeField] private Button _giftsTabBtn;

        [Header("Windows")]
        [SerializeField] private GameObject _offeringsWindow;
        [SerializeField] private GameObject _skinsWindow;
        [SerializeField] private GameObject _giftsWindow;

        [Header("Tab Styling")]
        [SerializeField] private Color _activeTabColor = new Color(0f, 0.95f, 1f, 1f); // Cyan
        [SerializeField] private Color _hoverTabColor = new Color(0.5f, 0.97f, 1f, 1f); // Brighter Cyan
        [SerializeField] private Color _inactiveTabColor = Color.white;

        [Header("Data")]
        [SerializeField] private List<StoreItemSO> _offerings;
        [SerializeField] private List<StoreItemSO> _skins;
        [SerializeField] private List<StoreItemSO> _gifts;

        private EconomyManager _economyManager;
        private List<Button> _tabButtons;
        private List<GameObject> _windows;

        [Inject]
        public void Construct(EconomyManager economyManager)
        {
            _economyManager = economyManager;
        }

        private void Start()
        {
            if (_offeringsTabBtn == null) return; // Basic guard

            _tabButtons = new List<Button> { _offeringsTabBtn, _skinsTabBtn, _giftsTabBtn };
            _windows = new List<GameObject> { _offeringsWindow, _skinsWindow, _giftsWindow };

            SetupTabListeners();
            SwitchTab(0); // Default to Offerings
            SetupAllGrids();

        }

        private void SetupTabListeners()
        {
            if (_tabButtons == null) return;
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                if (_tabButtons[i] == null) continue;
                int index = i;
                _tabButtons[i].onClick.AddListener(() => SwitchTab(index));
            }
        }

        public void SwitchTab(int index)
        {
            if (_tabButtons == null || _windows == null) return;

            for (int i = 0; i < _windows.Count; i++)
            {
                bool isActive = (i == index);
                if (_windows[i] != null) _windows[i].SetActive(isActive);
                
                // Styling
                if (i < _tabButtons.Count && _tabButtons[i] != null)
                {
                    var colors = _tabButtons[i].colors;
                    colors.normalColor = isActive ? _activeTabColor : _inactiveTabColor;
                    colors.selectedColor = isActive ? _activeTabColor : _inactiveTabColor;
                    colors.highlightedColor = _hoverTabColor;
                    _tabButtons[i].colors = colors;
                    
                    _tabButtons[i].transform.DOScale(isActive ? 1.05f : 1f, 0.2f).SetUpdate(true);
                }
            }
        }

        private void SetupAllGrids()
        {
            SetupGrid(_offeringGrid, _offerings, _offeringPackPrefab);
            SetupGrid(_skinsGrid, _skins, _skinPackPrefab);
            SetupGrid(_giftsGrid, _gifts, _giftPackPrefab);
        }

        private void SetupGrid(RectTransform grid, List<StoreItemSO> items, GameObject prefab)
        {
            if (grid == null || prefab == null || items == null) return;

            // Clear existing
            foreach (Transform child in grid) Destroy(child.gameObject);

            // Populate
            foreach (var item in items)
            {
                var go = Instantiate(prefab, grid);
                var itemUI = go.GetComponent<TreasuryOfferingItemUI>();
                if (itemUI != null)
                {
                    itemUI.Setup(item);
                    itemUI.OnPurchaseRequested += HandlePurchase;
                }
            }
        }

        private void HandlePurchase(StoreItemSO data)
        {
            if (_economyManager == null) return;

            Debug.Log($"[Treasury] Purchase requested for: {data.ItemName} (Type: {data.Type})");
            
            if (data.Type == StoreItemType.Currency)
            {
                _economyManager.AddBloodCrest(data.CurrencyAmount);
            }
            else if (data.Type == StoreItemType.Skin)
            {
                 // Handle skin unlocking
                 Debug.Log($"[Treasury] Skin Unlock: {data.SkinID}");
            }
        }

        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0;
                _canvasGroup.DOFade(1, 0.3f).SetUpdate(true);
            }
        }

        public void Close()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.DOFade(0, 0.3f).SetUpdate(true).OnComplete(() => 
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

        public bool RequestClose() => true;
    }
}
