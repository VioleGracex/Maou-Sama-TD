using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using MaouSamaTD.Units;
using Assets.SimpleLocalization.Scripts;
using Zenject;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Full-screen unit inspector for the Vassals page (when opened from Home).
    /// </summary>
    public class UnitInspectorFullScreenUI : MonoBehaviour, IUIController
    {
        [Header("UI Controller Architecture")]
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;

        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.3f;

        [Header("Header Info")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _rarityText;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Transform _starsRoot;

        [Header("Tab Buttons")]
        [SerializeField] private Button _tabStats;
        [SerializeField] private Button _tabSkills;
        [SerializeField] private Button _tabResonance;
        [SerializeField] private Button _tabSkins;

        [Header("Tab Content Roots")]
        [SerializeField] private GameObject _contentStats;
        [SerializeField] private GameObject _contentSkills;
        [SerializeField] private GameObject _contentResonance;
        [SerializeField] private GameObject _contentSkins;

        [Header("Buttons")]
        [SerializeField] private Button _btnClose;
        [SerializeField] private Button _btnUpgradeSkill;
        [SerializeField] private Button _btnSkins;
        [SerializeField] private Button _btnPromote;

        private UnitData _currentUnit;

        private void Start()
        {
            if (_btnClose) _btnClose.onClick.AddListener(Close);
            if (_tabStats) _tabStats.onClick.AddListener(() => SwitchTab(0));
            if (_tabSkills) _tabSkills.onClick.AddListener(() => SwitchTab(1));
            if (_tabResonance) _tabResonance.onClick.AddListener(() => SwitchTab(2));
            if (_tabSkins) _tabSkins.onClick.AddListener(() => SwitchTab(3));

            if (_btnSkins) _btnSkins.onClick.AddListener(() => SwitchTab(3));
            
            // Initial Localization
            LocalizeUI();
        }

        private void LocalizeUI()
        {
            if (_tabStats) _tabStats.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Vassals.Inspector.Stats");
            if (_tabSkills) _tabSkills.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Vassals.Inspector.Skills");
            if (_tabResonance) _tabResonance.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Vassals.Inspector.Resonance");
            if (_tabSkins) _tabSkins.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Vassals.Inspector.Skins");

            if (_btnUpgradeSkill) _btnUpgradeSkill.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Vassals.Inspector.UpgradeSkills");
            if (_btnSkins) _btnSkins.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Vassals.Inspector.Skins");
            if (_btnPromote) _btnPromote.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Localize("Vassals.Inspector.Promote");
        }

        public void Open(UnitData unit)
        {
            _currentUnit = unit;
            PopulateHeader(unit);
            SwitchTab(0);
            Open();
        }

        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0;
                _canvasGroup.DOFade(1, _fadeDuration).SetUpdate(true);
            }
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

        public void ResetState()
        {
            // Reset to defaults
        }

        public bool RequestClose() => true;

        private void SwitchTab(int index)
        {
            if (_contentStats) _contentStats.SetActive(index == 0);
            if (_contentSkills) _contentSkills.SetActive(index == 1);
            if (_contentResonance) _contentResonance.SetActive(index == 2);
            if (_contentSkins) _contentSkins.SetActive(index == 3);
            
            // Populate content logic similar to VassalDetailPanel
        }

        private void PopulateHeader(UnitData u)
        {
            if (_nameText) _nameText.text = u.UnitName.ToUpper();
            if (_rarityText) _rarityText.text = u.Rarity.ToString().ToUpper();
            if (_portraitImage) _portraitImage.sprite = u.UnitSprite;
        }
    }
}
