using UnityEngine;
using Zenject;

namespace MaouSamaTD.Managers
{
    /// <summary>
    /// Handles application-wide settings (Audio, Quality).
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        [Inject] private SaveManager _saveManager;

        public int QualityLevel => _saveManager?.CurrentData?.Settings?.QualityLevel ?? 2;
        public float UIAdaptation => _saveManager?.CurrentData?.Settings?.UIAdaptation ?? 90f;
        public string Language => _saveManager?.CurrentData?.Settings?.Language ?? "English";

        public bool Maintain2x { get; private set; } = true; // Still standalone? Let's keep it for now if needed else it's redundant
        public bool PerformanceOptimization => _saveManager?.CurrentData?.Settings?.PerformanceOptimization ?? true;
        public bool BaseFloodlights { get; private set; } = true;

        public int TargetFPS => _saveManager?.CurrentData?.Settings?.TargetFPS ?? 30;
        public bool BatterySaveMode => _saveManager?.CurrentData?.Settings?.BatterySaveMode ?? false;
        public bool AntiAliasing => _saveManager?.CurrentData?.Settings?.AntiAliasing ?? true;


        private void Start()
        {
            InitializeSettings();
            ApplySettings();
        }

        private void InitializeSettings()
        {
            if (_saveManager?.CurrentData == null) return;

            // Only detect language on the very first launch (if data was just created)
            // We assume if it's "English" and TargetFPS is 30 (default), it might be first launch or user reset
            // But better to check a flag or just do it once if not set.
            // Simplified: If CurrentData was just created and we are in Start, we can detect.
            // For now, let's just implement the detection.
            if (string.IsNullOrEmpty(_saveManager.CurrentData.Settings.Language) || _saveManager.CurrentData.Settings.Language == "English")
            {
                DetectDeviceLanguage();
            }
        }

        private void DetectDeviceLanguage()
        {
            string detected = Application.systemLanguage switch
            {
                SystemLanguage.Japanese => "Japanese",
                SystemLanguage.Russian => "Russian",
                SystemLanguage.English => "English",
                _ => "English" // Fallback
            };
            
            if (_saveManager?.CurrentData?.Settings != null)
                _saveManager.CurrentData.Settings.Language = detected;
        }

        public void ApplySettings()
        {
            ApplyLanguage();
            Application.targetFrameRate = TargetFPS;
            QualitySettings.antiAliasing = AntiAliasing ? 2 : 0;
            QualitySettings.SetQualityLevel(QualityLevel);
        }

        public void ApplyLanguage()
        {
            Assets.SimpleLocalization.Scripts.LocalizationManager.Language = Language;
        }

        public void SetLanguage(string language)
        {
            if (_saveManager?.CurrentData?.Settings != null)
            {
                _saveManager.CurrentData.Settings.Language = language;
                ApplyLanguage();
                SaveSettings();
            }
        }

        public void WipeData()
        {
            if (_saveManager != null)
            {
                _saveManager.DeleteSaveData();
                SaveSettings(); // Recreation happens in SaveManager if null, or we just force it here if needed
            }
        }

        public void SetQuality(int level)
        {
            if (_saveManager?.CurrentData?.Settings != null)
            {
                _saveManager.CurrentData.Settings.QualityLevel = level;
                QualitySettings.SetQualityLevel(level);
                SaveSettings();
            }
        }

        public void SetUIAdaptation(float value)
        {
            if (_saveManager?.CurrentData?.Settings != null)
            {
                _saveManager.CurrentData.Settings.UIAdaptation = value;
                SaveSettings();
            }
        }

        public void SetPerformanceOptimization(bool enabled) 
        { 
            if (_saveManager?.CurrentData?.Settings != null)
            {
                _saveManager.CurrentData.Settings.PerformanceOptimization = enabled; 
                SaveSettings(); 
            }
        }

        public void SetTargetFPS(int fps)
        {
            if (_saveManager?.CurrentData?.Settings != null)
            {
                _saveManager.CurrentData.Settings.TargetFPS = fps;
                Application.targetFrameRate = fps;
                SaveSettings();
            }
        }

        public void SetBatterySaveMode(bool enabled) 
        { 
            if (_saveManager?.CurrentData?.Settings != null)
            {
                _saveManager.CurrentData.Settings.BatterySaveMode = enabled; 
                SaveSettings(); 
            }
        }

        public void SetAntiAliasing(bool enabled)
        {
            if (_saveManager?.CurrentData?.Settings != null)
            {
                _saveManager.CurrentData.Settings.AntiAliasing = enabled;
                QualitySettings.antiAliasing = enabled ? 2 : 0;
                SaveSettings();
            }
        }

        private void LoadSettings()
        {
            // Handled via CurrentData properties
        }

        private void SaveSettings()
        {
            if (_saveManager != null)
            {
                _saveManager.Save();
            }
        }
    }
}
