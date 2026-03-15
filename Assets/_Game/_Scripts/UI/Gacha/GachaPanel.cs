using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zenject;
using MaouSamaTD.Data;
using MaouSamaTD.UI;

namespace MaouSamaTD.UI.Gacha
{
    public class GachaPanel : global::UnityEngine.MonoBehaviour, IUIController
    {
        [Header("UI Controller Architecture")]
        [SerializeField] private global::UnityEngine.GameObject _visualRoot;
        public global::UnityEngine.GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;

        [Header("Tabs")]
        [SerializeField] private List<GachaTabButton> _tabs;
        
        [Header("Banner Content")]
        [SerializeField] private Image _bannerArt;
        [SerializeField] private TMPro.TextMeshProUGUI _bannerName;
        [SerializeField] private TMPro.TextMeshProUGUI _bannerDescription;
        [SerializeField] private TMPro.TextMeshProUGUI _costSingleTxt;
        [SerializeField] private TMPro.TextMeshProUGUI _costMultiTxt;
        
        [Header("Buttons")]
        [SerializeField] private Button _btnSingle;
        [SerializeField] private Button _btnMulti;
        [SerializeField] private Button _btnIntent; // "Choose Guaranteed One"
        
        [Header("Sub-Panels")]
        [SerializeField] private GachaAnimationController _animController;
        // SoulIntentPanel _intentPanel; // Placeholder if needed later
        
        private GachaBannerSO _currentBanner;

        [Header("Injection Fallbacks")]
        [SerializeField] private MaouSamaTD.Managers.GachaManager _gachaManager;

        public void Open()
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

            RefreshUI();
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
        }

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
                _gachaManager.Summon(_currentBanner, isMulti);
            }
        }

        private void OnSummonResults(List<UnitInventoryEntry> results)
        {
            if (_animController != null)
            {
                _animController.PlayRitual(results);
            }
        }

        public void ResetState()
        {
            if (_tabs != null && _tabs.Count > 0) OnTabSelected(_tabs[0]);
        }

        public void OnTabSelected(GachaTabButton tab)
        {
            _currentBanner = tab.Banner;
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_currentBanner == null) return;
            
            if (_bannerName != null) _bannerName.text = _currentBanner.BannerName;
            if (_bannerDescription != null) _bannerDescription.text = _currentBanner.Description;
            if (_bannerArt != null) _bannerArt.sprite = _currentBanner.BannerArt;
            
            if (_costSingleTxt != null) _costSingleTxt.text = _currentBanner.SingleCost.ToString();
            if (_costMultiTxt != null) _costMultiTxt.text = _currentBanner.MultiCost.ToString();
        }
    }

    [System.Serializable]
    public class GachaTabButton
    {
        public Button Button;
        public GachaBannerSO Banner;
    }
}
