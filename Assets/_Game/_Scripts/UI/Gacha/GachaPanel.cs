using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zenject;
using MaouSamaTD.Data;
using MaouSamaTD.UI;
using MaouSamaTD.Managers;

namespace MaouSamaTD.UI.Gacha
{
    public class GachaPanel : global::UnityEngine.MonoBehaviour, IUIController
    {
        [Header("UI Controller Architecture")]
        [SerializeField] private global::UnityEngine.GameObject _visualRoot;
        public global::UnityEngine.GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;
        [SerializeField] private NavigationFeatures _navFeatures = NavigationFeatures.BackButton | NavigationFeatures.CitadelButton;
        public NavigationFeatures ConfiguredNavFeatures => _navFeatures;

        [Header("Tabs")]
        [SerializeField] private List<GachaTabButton> _tabs;
        
        [Header("Banner Content")]
        [SerializeField] private Image _bannerArt;
        [SerializeField] private TMPro.TextMeshProUGUI _bannerName;
        [SerializeField] private TMPro.TextMeshProUGUI _bannerDescription;
        [SerializeField] private TMPro.TextMeshProUGUI _costSingleTxt;
        [SerializeField] private TMPro.TextMeshProUGUI _costMultiTxt;
        [SerializeField] private TMPro.TextMeshProUGUI _countSingleTxt;
        [SerializeField] private TMPro.TextMeshProUGUI _countMultiTxt;
        [SerializeField] private Image _imgSingleCurrency;
        [SerializeField] private Image _imgMultiCurrency;
        
        [Header("Currency Sprites")]
        [SerializeField] private Sprite _spriteGold;
        [SerializeField] private Sprite _spriteBloodCrest;
        
        [Header("Buttons")]
        [SerializeField] private Button _btnSingle;
        [SerializeField] private Button _btnMulti;
        [SerializeField] private Button _btnIntent; // "Choose Guaranteed One"
        [SerializeField] private Button _btnDetails; // "View Details"
        
        [Header("Sub-Panels")]
        [SerializeField] private GachaAnimationController _animController;
        [SerializeField] private GachaDetailsPanel _detailsPanel;
        // SoulIntentPanel _intentPanel; // Placeholder if needed later
        
        private GachaBannerSO _currentBanner;
        private bool _isTutorialMode = false;
        private System.Action _onTutorialConfirmed;

        [Header("Injection Fallbacks")]
        [SerializeField] private MaouSamaTD.Managers.GachaManager _gachaManager;

        [Inject(Optional = true)] private SaveManager _saveManager;

        public void Open()
        {
            _isTutorialMode = false;
            _onTutorialConfirmed = null;
            OpenInternal();
        }

        /// <summary>
        /// Switches the (already-open) Gacha panel into tutorial mode.
        /// Call this AFTER UIFlowManager.OpenPanel() so the panel is already
        /// initialised. Hides the single-pull button and locks tab-switching
        /// until the player confirms their first 10-pull result.
        /// </summary>
        public void OpenInTutorialMode(UIPopupBlocker blocker, System.Action onConfirmed)
        {
            _isTutorialMode = true;
            _onTutorialConfirmed = onConfirmed;

            // Disable distractions during tutorial
            if (_btnSingle != null) _btnSingle.gameObject.SetActive(false);
            if (_btnIntent != null) _btnIntent.interactable = false;
            if (_btnDetails != null) _btnDetails.interactable = false;

            // Lock all tabs so the player can't switch banners
            if (_tabs != null)
            {
                foreach (var tab in _tabs) 
                {
                    if (tab.Button != null) tab.Button.interactable = false;
                }
            }

            if (blocker != null && _btnMulti != null)
            {
                StartCoroutine(ShowTutorialBlockerRoutine(blocker, _btnMulti.GetComponent<RectTransform>()));
            }
        }

        private System.Collections.IEnumerator ShowTutorialBlockerRoutine(UIPopupBlocker blocker, RectTransform target)
        {
            yield return new WaitForEndOfFrame();
            blocker.ShowBlockerWithTarget(target);
        }

        private void OpenInternal()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            
            if (_gachaManager != null)
            {
                _gachaManager.OnSummonCompleted += OnSummonResults;
                _gachaManager.OnPoolsReady += OnPoolsReady;
                SetSummonButtonsInteractable(_gachaManager.IsPoolReady);
                
                if (_currentBanner != null && _currentBanner.Pool != null)
                    _gachaManager.InitializePools(_currentBanner.Pool);
            }
            
            if (_btnSingle != null) _btnSingle.onClick.AddListener(() => Summon(false));
            if (_btnMulti != null) _btnMulti.onClick.AddListener(() => Summon(true));
            if (_btnIntent != null) _btnIntent.onClick.AddListener(() => OnIntentClicked());
            if (_btnDetails != null) _btnDetails.onClick.AddListener(() => OnDetailsClicked());

            if (_tabs != null)
            {
                foreach (var tab in _tabs)
                {
                    if (tab.Button != null)
                    {
                        var t = tab;
                        tab.Button.onClick.AddListener(() => OnTabSelected(t));
                    }
                }
            }

            ResetState();
        }

        private void OnDetailsClicked()
        {
            if (_detailsPanel != null && _currentBanner != null)
            {
                _detailsPanel.Open(_currentBanner);
            }
        }

        private void OnIntentClicked()
        {
            // Logic for soul intent selection window
            Debug.Log("[GachaPanel] Intent selection clicked.");
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            
            if (_gachaManager != null)
            {
                _gachaManager.OnSummonCompleted -= OnSummonResults;
                _gachaManager.OnPoolsReady -= OnPoolsReady;
            }
            
            if (_btnSingle != null) _btnSingle.onClick.RemoveAllListeners();
            if (_btnMulti != null) _btnMulti.onClick.RemoveAllListeners();
            if (_btnIntent != null) _btnIntent.onClick.RemoveAllListeners();
            if (_btnDetails != null) _btnDetails.onClick.RemoveAllListeners();

            if (_tabs != null)
            {
                foreach (var tab in _tabs)
                {
                    if (tab.Button != null) tab.Button.onClick.RemoveAllListeners();
                }
            }
        }

        public bool RequestClose() => true;

        private void OnPoolsReady()
        {
            SetSummonButtonsInteractable(true);
        }

        private void SetSummonButtonsInteractable(bool interactable)
        {
            if (_btnSingle != null) _btnSingle.interactable = interactable;
            if (_btnMulti != null) _btnMulti.interactable = interactable;
        }

        private void Summon(bool isMulti)
        {
            if (_gachaManager != null && _gachaManager.CanSummon(_currentBanner, isMulti))
            {
                if (_isTutorialMode)
                {
                    var blocker = Object.FindFirstObjectByType<UIPopupBlocker>(FindObjectsInactive.Include);
                    if (blocker != null) blocker.HideBlocker();

                    if (_saveManager != null && _saveManager.CurrentData != null)
                    {
                        _saveManager.CurrentData.GachaTutorialShown = true;
                        _saveManager.Save();
                        Debug.Log("[GachaPanel] Gacha tutorial marked as complete immediately upon summon.");
                    }
                }

                _gachaManager.Summon(_currentBanner, isMulti);
            }
        }

        private void OnSummonResults(List<UnitInventoryEntry> results)
        {
            // Lock Navigation UI during ritual
            if (UIFlowManager.Instance != null)
            {
                UIFlowManager.Instance.UpdateNavigationFeatures(NavigationFeatures.None);
            }

            if (_animController != null)
            {
                _animController.PlayRitual(results);
            }
        }

        public void RestoreNavigation()
        {
            // Restore Navigation UI after result confirmation
            if (UIFlowManager.Instance != null)
            {
                UIFlowManager.Instance.UpdateNavigationFeatures(ConfiguredNavFeatures);
            }

            // If this was the tutorial, fire the callback
            if (_isTutorialMode)
            {
                _isTutorialMode = false;

                // Re-show the single pull button for future normal session use
                if (_btnSingle != null) _btnSingle.gameObject.SetActive(true);

                _onTutorialConfirmed?.Invoke();
                _onTutorialConfirmed = null;
            }
        }

        public void ResetState()
        {
            if (_tabs != null && _tabs.Count > 0) 
            {
                OnTabSelected(_tabs[0]);
                // Ensure other tabs are visually reset
                for (int i = 1; i < _tabs.Count; i++)
                {
                    if (_tabs[i].TabUI != null) _tabs[i].TabUI.SetState(false, true);
                }
            }
        }

        public void OnTabSelected(GachaTabButton selectedTab)
        {
            _currentBanner = selectedTab.Banner;
            
            // Update Tab Visuals
            foreach (var tab in _tabs)
            {
                if (tab.TabUI != null)
                {
                    tab.TabUI.SetState(tab == selectedTab);
                }
            }

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_currentBanner == null) return;
            
            if (_bannerName != null) _bannerName.text = _currentBanner.BannerName;
            
            // FGO-style dynamic description using data fields
            if (_bannerDescription != null) 
            {
                string desc = _currentBanner.Description;
                if (!string.IsNullOrEmpty(_currentBanner.FeaturedUnitName))
                {
                    desc = $"Rate-up for {MaouSamaTD.Units.UnitRarity.Legendary}-Star:\n<color=#FFD700>{_currentBanner.FeaturedUnitName}</color>\n" + 
                           "Every 10 summons guarantees a 4-Star soul or higher.";
                }
                _bannerDescription.text = desc;
            }

            if (_bannerArt != null) _bannerArt.sprite = _currentBanner.BannerArt;
            
            // Reverted from TMP Sprites to Image-based Icons
            Sprite currencySprite = _currentBanner.Currency == Data.GachaCurrencyType.Gold ? _spriteGold : _spriteBloodCrest;
            
            if (_imgSingleCurrency != null) _imgSingleCurrency.sprite = currencySprite;
            if (_imgMultiCurrency != null) _imgMultiCurrency.sprite = currencySprite;

            if (_costSingleTxt != null) _costSingleTxt.text = _currentBanner.SingleCost.ToString();
            if (_costMultiTxt != null) _costMultiTxt.text = _currentBanner.MultiCost.ToString();
            
            if (_countSingleTxt != null) _countSingleTxt.text = "x 1";
            if (_countMultiTxt != null) _countMultiTxt.text = "x 10";
        }
    }

    [System.Serializable]
    public class GachaTabButton
    {
        public Button Button;
        public GachaBannerSO Banner;
        public GachaTabUI TabUI;
    }
}
