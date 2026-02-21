using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Levels;
using System;

namespace MaouSamaTD.UI.MainMenu
{
    public class LevelButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _levelNameText;
        [SerializeField] private TextMeshProUGUI _levelNumberText; // e.g. "01"
        [SerializeField] private GameObject _lockedOverlay;
        [SerializeField] private GameObject[] _stars; // Array of star objects (e.g., 3 stars)
        [SerializeField] private Button _button;
        
        private LevelData _data;
        private Action<LevelData> _onClick;

        public void Setup(LevelData data, int index, bool isLocked, int starCount, Action<LevelData> onClick)
        {
            gameObject.SetActive(true);
            _data = data;
            _onClick = onClick;
            
            if (_levelNameText != null) 
                _levelNameText.text = data.LevelName.ToUpper();
            
            if (_levelNumberText != null)
                _levelNumberText.text = (index + 1).ToString("D2"); // "01", "02"
            
            if (_lockedOverlay != null) 
                _lockedOverlay.SetActive(isLocked);
            
            if (_button != null)
            {
                _button.interactable = !isLocked;
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClicked);
            }

            if (_stars != null)
            {
                for (int i = 0; i < _stars.Length; i++)
                {
                    if (_stars[i] != null) 
                        _stars[i].SetActive(i < starCount);
                }
            }
        }

        private void OnClicked()
        {
            _onClick?.Invoke(_data);
        }
    }
}
