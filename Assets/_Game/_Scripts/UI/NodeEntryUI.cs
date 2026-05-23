using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MaouSamaTD.UI
{
    /// <summary>Prefab component for a single Resonance Node row in Honkai Star Rail style.</summary>
    public class NodeEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _txtTierLabel; // e.g. "NODE TIER 01" or "RESONANCE I"
        [SerializeField] private TextMeshProUGUI _txtNodeName; // e.g. "SOVEREIGN HEART"
        [SerializeField] private TextMeshProUGUI _txtDescription; // e.g. "Increases HP by 5%"
        [SerializeField] private TextMeshProUGUI _txtStatus; // e.g. "✦ ACTIVE" or "◌ LOCKED"
        [SerializeField] private Image _nodeIcon;
        [SerializeField] private Button _btnUnlock;
        
        [SerializeField] private Color _colorLocked   = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private Color _colorUnlocked = new Color(0.9f, 0.7f, 0.2f, 1f);

        // Compatibility method to prevent compilation errors if old code calls Setup
        public void Setup(string label, bool unlocked, bool canUnlock, System.Action onUnlock)
        {
            SetupRich("RESONANCE", label, "", unlocked, canUnlock, onUnlock, null);
        }

        public void SetupRich(string tierLabel, string nodeName, string desc, bool unlocked, bool canUnlock, System.Action onUnlock, Sprite icon)
        {
            if (_txtTierLabel) _txtTierLabel.text = string.IsNullOrEmpty(tierLabel) ? "RESONANCE" : tierLabel.ToUpper();
            if (_txtNodeName) _txtNodeName.text = string.IsNullOrEmpty(nodeName) ? "SOVEREIGN BOND" : nodeName.ToUpper();
            
            if (_txtDescription) 
            {
                _txtDescription.text = desc;
                _txtDescription.color = unlocked ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
            }

            if (_txtStatus)
            {
                _txtStatus.text = unlocked ? "✦ ACTIVE" : "◌ LOCKED";
                _txtStatus.color = unlocked ? _colorUnlocked : _colorLocked;
            }

            if (_nodeIcon)
            {
                if (icon != null)
                {
                    _nodeIcon.sprite = icon;
                    _nodeIcon.gameObject.SetActive(true);
                }
                _nodeIcon.color = unlocked ? _colorUnlocked : _colorLocked;
            }

            if (_btnUnlock)
            {
                _btnUnlock.gameObject.SetActive(!unlocked);
                _btnUnlock.interactable = canUnlock;
                _btnUnlock.onClick.RemoveAllListeners();
                _btnUnlock.onClick.AddListener(() => onUnlock());
            }
        }
    }
}
