using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MaouSamaTD.UI
{
    /// <summary>Prefab component for a single Lore/Memory chamber row.</summary>
    public class MemoryEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _txtTitle;
        [SerializeField] private TextMeshProUGUI _txtBody;
        [SerializeField] private GameObject _lockOverlay;
        [SerializeField] private Button _btnUnlock;

        public void Setup(string title, string body, bool unlocked, bool canUnlock, System.Action onUnlock)
        {
            if (_txtTitle) _txtTitle.text = title;
            if (_txtBody)  _txtBody.text  = unlocked ? body : "???  Unlock: 1 Duplicate";
            if (_lockOverlay) _lockOverlay.SetActive(!unlocked);
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
