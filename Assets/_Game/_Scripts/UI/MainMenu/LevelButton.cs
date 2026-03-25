using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Levels;
using System;

namespace MaouSamaTD.UI.MainMenu
{
    [Serializable]
    public struct LevelDisplayData
    {
        public LevelData Level;
        public int Index;
        public bool IsLocked;
        public int StarCount;

        public string LevelID => Level != null ? Level.LevelID : string.Empty;
        public int Version => (IsLocked ? 1 : 0) ^ StarCount;
    }

    public class LevelButton : MonoBehaviour, MaouSamaTD.UI.Common.IListItem<LevelDisplayData>
    {
        [SerializeField] private TextMeshProUGUI _levelNameText;
        [SerializeField] private TextMeshProUGUI _levelNumberText; // e.g. "01"
        [SerializeField] private GameObject _lockedOverlay;
        [SerializeField] private GameObject[] _stars; // Array of star objects (e.g., 3 stars)
        [SerializeField] private Button _button;
        
        private LevelDisplayData _displayData;
        private Action<LevelData> _onClick;

        public LevelData LevelDataForCallback => _displayData.Level;

        // IListItem implementation
        public string GetContentID() => _displayData.LevelID;
        public int GetContentVersion() => _displayData.Version;

        public void Setup(LevelDisplayData data, Action<UnityEngine.Component> onClick = null)
        {
            if (onClick != null) _onClick = (comp) => (onClick as Action<LevelButton>)?.Invoke(this); // This is a bit messy, let's fix it
            
            _displayData = data;
            var level = data.Level;
            
            if (_levelNameText != null) 
                _levelNameText.text = level.LevelName.ToUpper();
            
            if (_levelNumberText != null)
                _levelNumberText.text = (data.Index + 1).ToString("D2"); // "01", "02"
            
            if (_lockedOverlay != null) 
                _lockedOverlay.SetActive(data.IsLocked);
            
            if (_button != null)
            {
                _button.interactable = !data.IsLocked;
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClicked);
            }

            if (_stars != null)
            {
                for (int i = 0; i < _stars.Length; i++)
                {
                    if (_stars[i] != null) 
                        _stars[i].SetActive(i < data.StarCount);
                }
            }
        }

        // Legacy Setup for compatibility if needed, but we'll try to refactor everything
        public void Setup(LevelData data, int index, bool isLocked, int starCount, Action<LevelData> onClick)
        {
            _onClick = onClick;
            Setup(new LevelDisplayData { Level = data, Index = index, IsLocked = isLocked, StarCount = starCount });
        }

        private void OnClicked()
        {
            _onClick?.Invoke(_displayData.Level);
        }
    }
}
