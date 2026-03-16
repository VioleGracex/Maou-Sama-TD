using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;
using MaouSamaTD.UI.Common;

namespace MaouSamaTD.UI.MainMenu
{
    /// <summary>
    /// Game Settings page. Allows adjusting Audio and Graphics.
    /// </summary>
    public class SettingsPanel : MonoBehaviour, IUIController
    {
        #region Serialized Fields

        #region UI Controller Architecture
        [Header("UI Controller Architecture")]
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;
        #endregion

        #region Tabs
        [Header("Tabs")]
        [SerializeField] private Button _btnGameTab;
        [SerializeField] private Button _btnAudioTab;
        [SerializeField] private Button _btnAccountTab;
        [SerializeField] private Button _btnOthersTab;
        #endregion

        #region Pages
        [Header("Pages")]
        [SerializeField] private GameObject _gamePage;
        [SerializeField] private GameObject _audioPage;
        [SerializeField] private GameObject _accountPage;
        [SerializeField] private GameObject _othersPage;
        #endregion

        #region Game Settings
        [Header("Game Settings")]
        [SerializeField] private CustomSlider _sliderUIAdaptation;
        [SerializeField] private TMP_Dropdown _dropdownQuality;
        [SerializeField] private TMP_Dropdown _dropdownLanguage;
        [SerializeField] private CustomToggle _togglePerformance;
        [SerializeField] private CustomToggle _toggleFPS; 
        [SerializeField] private CustomToggle _toggleAntiAliasing;
        [SerializeField] private CustomToggle _toggleBatterySave;
        #endregion

        #region Audio Settings
        [Header("Audio Sliders")]
        [SerializeField] private CustomSlider _sliderMusic;
        [SerializeField] private CustomSlider _sliderSFX;
        [SerializeField] private CustomSlider _sliderVoice;

        [Header("Audio Toggles")]
        [SerializeField] private CustomToggle _toggleMusic;
        [SerializeField] private CustomToggle _toggleSFX;
        [SerializeField] private CustomToggle _toggleVoice;
        #endregion

        
        #region Account Settings
        [Header("Account Settings")]
        [SerializeField] private Button _btnWipeData;
        [SerializeField] private GameObject _wipeConfirmationPopup;
        [SerializeField] private Button _btnConfirmWipe;
        [SerializeField] private Button _btnCancelWipe;
        #endregion

        #region Navigation
        [Header("Navigation")]
        [SerializeField] private Button _btnBack;
        #endregion

        #region Tab Visuals
        [Header("Tab Visuals")]
        [SerializeField] private Sprite _spriteTabActive;
        [SerializeField] private Sprite _spriteTabInactive;
        [SerializeField] private Color _colorTabActive = Color.white;
        [SerializeField] private Color _colorTabInactive = new Color(0.0235f, 0.0235f, 0.0353f, 1f);
        #endregion

        #endregion

        #region Injected Dependencies
        [Inject] private MaouSamaTD.Managers.SettingsManager _settingsManager;
        [Inject] private MaouSamaTD.Managers.AudioSettingsManager _audioManager;
        #endregion

        private void Start()
        {
            PopulateDropdowns();

            if (_sliderMusic != null) _sliderMusic.OnValueChanged.AddListener(OnMusicChanged);
            if (_sliderSFX != null) _sliderSFX.OnValueChanged.AddListener(OnSFXChanged);
            if (_sliderVoice != null) _sliderVoice.OnValueChanged.AddListener(OnVoiceChanged);
            if (_sliderUIAdaptation != null) _sliderUIAdaptation.OnValueChanged.AddListener(OnUIAdaptationChanged);
            
            if (_toggleMusic != null) _toggleMusic.OnValueChanged.AddListener(OnMusicToggle);
            if (_toggleSFX != null) _toggleSFX.OnValueChanged.AddListener(OnSFXToggle);
            if (_toggleVoice != null) _toggleVoice.OnValueChanged.AddListener(OnVoiceToggle);

            if (_togglePerformance != null) _togglePerformance.OnValueChanged.AddListener(OnPerformanceToggle);
            if (_toggleFPS != null) _toggleFPS.OnValueChanged.AddListener(OnFPSToggle);
            if (_toggleAntiAliasing != null) _toggleAntiAliasing.OnValueChanged.AddListener(OnAntiAliasingToggle);
            if (_toggleBatterySave != null) _toggleBatterySave.OnValueChanged.AddListener(OnBatterySaveToggle);

            if (_btnBack != null) _btnBack.onClick.AddListener(OnBackClicked);

            if (_btnGameTab != null) _btnGameTab.onClick.AddListener(() => SwitchTab(SettingsTab.Game));
            if (_btnAudioTab != null) _btnAudioTab.onClick.AddListener(() => SwitchTab(SettingsTab.Audio));
            if (_btnAccountTab != null) _btnAccountTab.onClick.AddListener(() => SwitchTab(SettingsTab.Account));
            if (_btnOthersTab != null) _btnOthersTab.onClick.AddListener(() => SwitchTab(SettingsTab.Others));

            if (_dropdownQuality != null) _dropdownQuality.onValueChanged.AddListener(OnQualityDropdownChanged);
            if (_dropdownLanguage != null) _dropdownLanguage.onValueChanged.AddListener(OnLanguageChanged);
            if (_btnWipeData != null) _btnWipeData.onClick.AddListener(OnWipeDataClicked);

            if (_btnConfirmWipe != null) _btnConfirmWipe.onClick.AddListener(OnConfirmWipeClicked);
            if (_btnCancelWipe != null) _btnCancelWipe.onClick.AddListener(OnCancelWipeClicked);
        }

        private void PopulateDropdowns()
        {
            if (_dropdownQuality != null)
            {
                _dropdownQuality.ClearOptions();
                var options = new System.Collections.Generic.List<string>() { "Low", "Medium", "High", "Ultra" }; // Or fetch from QualitySettings
                _dropdownQuality.AddOptions(options);
            }

            if (_dropdownLanguage != null)
            {
                _dropdownLanguage.ClearOptions();
                var options = new System.Collections.Generic.List<string>() { "English", "Japanese", "Russian" };
                _dropdownLanguage.AddOptions(options);
            }
        }

        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            SwitchTab(SettingsTab.Game); // Default to Game (renamed from Graphics)
            RefreshUI();
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            
            // Ensure any open popups are also closed
            if (_wipeConfirmationPopup != null) _wipeConfirmationPopup.SetActive(false);
        }

        public bool RequestClose() => true;

        public void ResetState()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_settingsManager == null) return;
            
            // Graphics
            if (_sliderUIAdaptation != null) _sliderUIAdaptation.SetValueWithoutNotify(_settingsManager.UIAdaptation);

            if (_audioManager != null)
            {
                if (_sliderMusic != null) _sliderMusic.SetValueWithoutNotify(_audioManager.MusicVolume * 100f);
                if (_sliderSFX != null) _sliderSFX.SetValueWithoutNotify(_audioManager.SFXVolume * 100f);
                if (_sliderVoice != null) _sliderVoice.SetValueWithoutNotify(_audioManager.VoiceVolume * 100f);

                if (_toggleMusic != null) _toggleMusic.SetIsOnWithoutNotify(_audioManager.MusicEnabled);
                if (_toggleSFX != null) _toggleSFX.SetIsOnWithoutNotify(_audioManager.SFXEnabled);
                if (_toggleVoice != null) _toggleVoice.SetIsOnWithoutNotify(_audioManager.VoiceEnabled);

                // Initialize Interactivity
                if (_sliderMusic != null) _sliderMusic.Interactable = _audioManager.MusicEnabled;
                if (_sliderSFX != null) _sliderSFX.Interactable = _audioManager.SFXEnabled;
                if (_sliderVoice != null) _sliderVoice.Interactable = _audioManager.VoiceEnabled;
            }


            if (_dropdownQuality != null) _dropdownQuality.SetValueWithoutNotify(_settingsManager.QualityLevel);
            if (_togglePerformance != null) _togglePerformance.SetIsOnWithoutNotify(_settingsManager.PerformanceOptimization);
            if (_toggleFPS != null) _toggleFPS.SetIsOnWithoutNotify(_settingsManager.TargetFPS == 60); // ON means 60, OFF means 30
            if (_toggleAntiAliasing != null) _toggleAntiAliasing.SetIsOnWithoutNotify(_settingsManager.AntiAliasing);
            if (_toggleBatterySave != null) _toggleBatterySave.SetIsOnWithoutNotify(_settingsManager.BatterySaveMode);

            if (_dropdownLanguage != null)
            {
                int langIndex = _settingsManager.Language switch
                {
                    "English" => 0,
                    "Japanese" => 1,
                    "Russian" => 2,
                    _ => 0
                };
                _dropdownLanguage.SetValueWithoutNotify(langIndex);
            }
        }



        private void SwitchTab(SettingsTab tab)
        {
            if (_gamePage != null) _gamePage.SetActive(tab == SettingsTab.Game);
            if (_audioPage != null) _audioPage.SetActive(tab == SettingsTab.Audio);
            if (_accountPage != null) _accountPage.SetActive(tab == SettingsTab.Account);
            if (_othersPage != null) _othersPage.SetActive(tab == SettingsTab.Others);
            
            // Update button visual states
            UpdateTabButton(_btnGameTab, tab == SettingsTab.Game);
            UpdateTabButton(_btnAudioTab, tab == SettingsTab.Audio);
            UpdateTabButton(_btnAccountTab, tab == SettingsTab.Account);
            UpdateTabButton(_btnOthersTab, tab == SettingsTab.Others);
        }

        private void UpdateTabButton(Button btn, bool active)
        {
            if (btn == null) return;
            
            // Swap sprite based on active state
            if (active && _spriteTabActive != null)
            {
                btn.image.sprite = _spriteTabActive;
                btn.image.color = _colorTabActive;
            }
            else if (!active && _spriteTabInactive != null)
            {
                btn.image.sprite = _spriteTabInactive;
                btn.image.color = _colorTabInactive;
            }
        }

        private void OnMusicChanged(float val)
        {
            if (_audioManager != null) _audioManager.SetMusicVolume(val / 100f);
        }

        private void OnSFXChanged(float val)
        {
            if (_audioManager != null) _audioManager.SetSFXVolume(val / 100f);
        }

        private void OnVoiceChanged(float val)
        {
            if (_audioManager != null) _audioManager.SetVoiceVolume(val / 100f);
        }

        private void OnUIAdaptationChanged(float val)
        {
            if (_settingsManager != null) _settingsManager.SetUIAdaptation(val);
        }
        
        private void OnMusicToggle(bool val) 
        { 
            if (_audioManager != null) _audioManager.SetMusicEnabled(val); 
            if (_sliderMusic != null) _sliderMusic.Interactable = val;
        }

        private void OnSFXToggle(bool val) 
        { 
            if (_audioManager != null) _audioManager.SetSFXEnabled(val); 
            if (_sliderSFX != null) _sliderSFX.Interactable = val;
        }

        private void OnVoiceToggle(bool val) 
        { 
            if (_audioManager != null) _audioManager.SetVoiceEnabled(val); 
            if (_sliderVoice != null) _sliderVoice.Interactable = val;
        }

        private void OnQualityDropdownChanged(int val) => _settingsManager.SetQuality(val);

        private void OnLanguageChanged(int index)
        {
            string lang = index switch
            {
                0 => "English",
                1 => "Japanese",
                2 => "Russian",
                _ => "English"
            };
            _settingsManager.SetLanguage(lang);
        }

        private void OnWipeDataClicked()
        {
            if (_wipeConfirmationPopup != null) _wipeConfirmationPopup.SetActive(true);
            else OnConfirmWipeClicked(); // Fallback if no popup exists
        }

        private void OnConfirmWipeClicked()
        {
            _settingsManager.WipeData();
            if (_wipeConfirmationPopup != null) _wipeConfirmationPopup.SetActive(false);
            RefreshUI();
        }

        private void OnCancelWipeClicked()
        {
            if (_wipeConfirmationPopup != null) _wipeConfirmationPopup.SetActive(false);
        }


        private void OnPerformanceToggle(bool val) => _settingsManager.SetPerformanceOptimization(val);
        private void OnFPSToggle(bool val) => _settingsManager.SetTargetFPS(val ? 60 : 30);
        private void OnAntiAliasingToggle(bool val) => _settingsManager.SetAntiAliasing(val);
        private void OnBatterySaveToggle(bool val) => _settingsManager.SetBatterySaveMode(val);


        private void OnBackClicked()
        {
            // Assuming UIFlowManager is available as a singleton per page-flow-manifest.md logic
            // or simply close the panel if that's how it's managed
            Close();
        }

        private enum SettingsTab
        {
            Game,
            Audio,
            Account,
            Others
        }
    }
}
