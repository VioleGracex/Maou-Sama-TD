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
    public class VaultInventoryUI : MonoBehaviour, IUIController
    {
        public enum ItemFilter
        {
            All,
            Cores,
            Materials
        }

        [Header("IUIController Architecture")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private bool _addsToHistory = true;
        [SerializeField] private NavigationFeatures _navFeatures = NavigationFeatures.BackButton | NavigationFeatures.CitadelButton;

        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => _addsToHistory;
        public NavigationFeatures ConfiguredNavFeatures => _navFeatures;

        [Header("UI Scroll & Grid")]
        [SerializeField] private ScrollRect _itemsScrollRect;
        [SerializeField] private GameObject _itemCardPrefab;
        [SerializeField] private TextMeshProUGUI _txtEmptyInventoryNotice;

        [Header("Navigation Tabs")]
        [SerializeField] private Button _btnAllTab;
        [SerializeField] private Button _btnCoresTab;
        [SerializeField] private Button _btnMatsTab;

        [Header("Tab Colors")]
        [SerializeField] private Color _activeTabColor = new Color(0f, 0.95f, 1f, 1f); // Cyan
        [SerializeField] private Color _hoverTabColor = new Color(0.5f, 0.97f, 1f, 1f); // Light Cyan
        [SerializeField] private Color _inactiveTabColor = Color.white;

        [Header("Details Panel/Window")]
        [SerializeField] private GameObject _detailsPanel;
        [SerializeField] private CanvasGroup _detailsCanvasGroup;
        [SerializeField] private RectTransform _detailsWindowRect;
        [SerializeField] private TextMeshProUGUI _txtDetailsName;
        [SerializeField] private Image _imgDetailsIcon;
        [SerializeField] private TextMeshProUGUI _txtDetailsDesc;
        [SerializeField] private TextMeshProUGUI _txtDetailsQuantity;
        [SerializeField] private TextMeshProUGUI _txtDetailsSource;
        [SerializeField] private Button _btnDetailsClose;

        [Header("Registry / Item Config Assets")]
        [SerializeField] private List<ItemConfigSO> _itemConfigs = new List<ItemConfigSO>();

        private SaveManager _saveManager;
        private ItemFilter _currentFilter = ItemFilter.All;
        private List<Button> _tabButtons;
        private CanvasGroup _canvasGroup;

        [Inject]
        public void Construct(SaveManager saveManager)
        {
            _saveManager = saveManager;
        }

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            _tabButtons = new List<Button> { _btnAllTab, _btnCoresTab, _btnMatsTab };

            SetupTabListeners();
            SetupDetailsPanelListener();

            if (_detailsPanel != null) _detailsPanel.SetActive(false);
            
            SwitchFilter(ItemFilter.All);
        }

        private void SetupTabListeners()
        {
            if (_btnAllTab != null) _btnAllTab.onClick.AddListener(() => SwitchFilter(ItemFilter.All));
            if (_btnCoresTab != null) _btnCoresTab.onClick.AddListener(() => SwitchFilter(ItemFilter.Cores));
            if (_btnMatsTab != null) _btnMatsTab.onClick.AddListener(() => SwitchFilter(ItemFilter.Materials));
        }

        private void SetupDetailsPanelListener()
        {
            if (_btnDetailsClose != null)
            {
                _btnDetailsClose.onClick.AddListener(HideDetailsPanel);
            }
        }

        public void SwitchFilter(ItemFilter filter)
        {
            _currentFilter = filter;
            UpdateTabStyling();
            RebuildGrid();
        }

        private void UpdateTabStyling()
        {
            if (_tabButtons == null) return;

            for (int i = 0; i < _tabButtons.Count; i++)
            {
                if (_tabButtons[i] == null) continue;

                bool isActive = (i == (int)_currentFilter);
                var colors = _tabButtons[i].colors;
                colors.normalColor = isActive ? _activeTabColor : _inactiveTabColor;
                colors.selectedColor = isActive ? _activeTabColor : _inactiveTabColor;
                colors.highlightedColor = _hoverTabColor;
                _tabButtons[i].colors = colors;

                _tabButtons[i].transform.DOScale(isActive ? 1.05f : 1f, 0.2f).SetUpdate(true);
            }
        }

        public void RebuildGrid()
        {
            if (_itemsScrollRect == null || _itemsScrollRect.content == null) return;

            // Clear previous children
            foreach (Transform child in _itemsScrollRect.content)
            {
                Destroy(child.gameObject);
            }

            int visibleCardCount = 0;

            foreach (var config in _itemConfigs)
            {
                if (config == null) continue;

                // Apply filter criteria
                bool pass = _currentFilter switch
                {
                    ItemFilter.All => true,
                    ItemFilter.Cores => config.ItemID.StartsWith("xp_core_"),
                    ItemFilter.Materials => config.ItemID.StartsWith("mat_"),
                    _ => true
                };

                if (!pass) continue;

                int qty = _saveManager != null ? _saveManager.GetItemCount(config.ItemID) : 0;

                // Instantiate item card
                if (_itemCardPrefab != null)
                {
                    var cardObj = Instantiate(_itemCardPrefab, _itemsScrollRect.content);
                    var cardUI = cardObj.GetComponent<VaultItemCardUI>();
                    if (cardUI != null)
                    {
                        cardUI.Setup(
                            config.ItemName,
                            config.ItemIcon,
                            qty,
                            config.BackgroundColor,
                            () => ShowDetailsPanel(config, qty)
                        );
                        visibleCardCount++;
                    }
                }
            }

            if (_txtEmptyInventoryNotice != null)
            {
                _txtEmptyInventoryNotice.gameObject.SetActive(visibleCardCount == 0);
            }
        }

        private void ShowDetailsPanel(ItemConfigSO config, int quantity)
        {
            if (_detailsPanel == null) return;

            if (_txtDetailsName != null) _txtDetailsName.text = config.ItemName.ToUpper();
            if (_imgDetailsIcon != null) _imgDetailsIcon.sprite = config.ItemIcon;
            if (_txtDetailsDesc != null) _txtDetailsDesc.text = config.Description;
            if (_txtDetailsQuantity != null) _txtDetailsQuantity.text = $"OWNED: {quantity}";
            if (_txtDetailsSource != null) _txtDetailsSource.text = GetItemSourceText(config.ItemID);

            _detailsPanel.SetActive(true);

            // Pop-up tween animation
            if (_detailsCanvasGroup != null)
            {
                _detailsCanvasGroup.alpha = 0f;
                _detailsCanvasGroup.DOFade(1f, 0.25f).SetUpdate(true);
            }
            if (_detailsWindowRect != null)
            {
                _detailsWindowRect.localScale = Vector3.one * 0.8f;
                _detailsWindowRect.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        private void HideDetailsPanel()
        {
            if (_detailsPanel == null) return;

            if (_detailsCanvasGroup != null && _detailsWindowRect != null)
            {
                _detailsWindowRect.DOScale(0.8f, 0.2f).SetEase(Ease.InBack).SetUpdate(true);
                _detailsCanvasGroup.DOFade(0f, 0.2f).SetUpdate(true).OnComplete(() =>
                {
                    _detailsPanel.SetActive(false);
                });
            }
            else
            {
                _detailsPanel.SetActive(false);
            }
        }

        private string GetItemSourceText(string itemId)
        {
            return itemId switch
            {
                "mat_shadow_essence" => "Drops from: Shadow, Undead, or Demon enemies in conquests.",
                "mat_bandit_insignia" => "Drops from: Bandit enemies in conquests.",
                "mat_animal_fang" => "Drops from: Animal enemies in conquests.",
                "mat_golem_core" => "Drops from: Golem enemies in conquests.",
                "xp_core_common" or "xp_core_rare" or "xp_core_epic" or "xp_core_legendary" => "Universal drop from all enemy categories in conquests.",
                _ => "Obtainable through achievements and special missions."
            };
        }

        // ── IUIController Interface ─────────────────────────────────────────
        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);

            // Populate the grid with fresh quantity checks
            RebuildGrid();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.DOFade(1f, 0.3f).SetUpdate(true);
            }

            if (_detailsPanel != null) _detailsPanel.SetActive(false);
        }

        public void Close()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.DOFade(0f, 0.25f).SetUpdate(true).OnComplete(() =>
                {
                    if (_visualRoot != null) _visualRoot.SetActive(false);
                });
            }
            else
            {
                if (_visualRoot != null) _visualRoot.SetActive(false);
            }
        }

        public bool RequestClose()
        {
            // Close details panel first if active, otherwise let flow close this screen
            if (_detailsPanel != null && _detailsPanel.activeSelf)
            {
                HideDetailsPanel();
                return false;
            }
            return true;
        }

        public void ResetState()
        {
            SwitchFilter(ItemFilter.All);
        }

        // Editor support to populate ItemConfigs automatically
#if UNITY_EDITOR
        [ContextMenu("Find All ItemConfigs")]
        public void FindAllItemConfigs()
        {
            _itemConfigs.Clear();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemConfigSO", new[] { "Assets/_Game/Data/Items" });
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemConfigSO>(path);
                if (item != null && !_itemConfigs.Contains(item))
                {
                    _itemConfigs.Add(item);
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[VaultInventoryUI] Found and assigned {_itemConfigs.Count} ItemConfigSO assets.");
        }
#endif
    }
}
