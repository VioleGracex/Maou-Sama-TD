using UnityEngine;
using Zenject;

namespace MaouSamaTD.Managers
{
    /// <summary>
    /// Specialized manager for Audio settings.
    /// Handles Master, Music, SFX, and Voice volumes.
    /// </summary>
    public class AudioSettingsManager : MonoBehaviour
    {
        [Inject] private SaveManager _saveManager;

        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 0.8f;
        public float SFXVolume { get; private set; } = 0.8f;
        public float VoiceVolume { get; private set; } = 0.8f;

        public bool MusicEnabled { get; private set; } = true;
        public bool SFXEnabled { get; private set; } = true;
        public bool VoiceEnabled { get; private set; } = true;

        private void Start()
        {
            LoadAudioSettings();
        }

        public void SetMusicVolume(float volume)
        {
            float clampedVolume = Mathf.Clamp01(volume);
            if (Mathf.Approximately(MusicVolume, clampedVolume)) return;
            MusicVolume = clampedVolume;
            SaveAudioSettings();
        }

        public void SetMusicEnabled(bool enabled)
        {
            if (MusicEnabled == enabled) return;
            MusicEnabled = enabled;
            SaveAudioSettings();
        }

        public void SetSFXVolume(float volume)
        {
            float clampedVolume = Mathf.Clamp01(volume);
            if (Mathf.Approximately(SFXVolume, clampedVolume)) return;
            SFXVolume = clampedVolume;
            SaveAudioSettings();
        }

        public void SetSFXEnabled(bool enabled)
        {
            if (SFXEnabled == enabled) return;
            SFXEnabled = enabled;
            SaveAudioSettings();
        }

        public void SetVoiceVolume(float volume)
        {
            float clampedVolume = Mathf.Clamp01(volume);
            if (Mathf.Approximately(VoiceVolume, clampedVolume)) return;
            VoiceVolume = clampedVolume;
            SaveAudioSettings();
        }

        public void SetVoiceEnabled(bool enabled)
        {
            if (VoiceEnabled == enabled) return;
            VoiceEnabled = enabled;
            SaveAudioSettings();
        }

        public void SetMasterVolume(float volume)
        {
            float clampedVolume = Mathf.Clamp01(volume);
            if (Mathf.Approximately(MasterVolume, clampedVolume)) return;
            MasterVolume = clampedVolume;
            SaveAudioSettings();
        }

        private void LoadAudioSettings()
        {
            if (_saveManager != null && _saveManager.CurrentData != null)
            {
                // Placeholder for loading logic from SaveData
            }
        }

        private void SaveAudioSettings()
        {
            if (_saveManager != null)
            {
                _saveManager.Save();
            }
        }
    }
}
