using UnityEngine;
using UnityEngine.UI;

namespace MaouSamaTD.UI
{
    public class LowerBarTabsController : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private Button _vassalsTabBtn;
        [SerializeField] private Button _ritesTabBtn;

        [Header("Containers")]
        [SerializeField] private GameObject _vassalsContainer; // UnitBar
        [SerializeField] private GameObject _ritesContainer;   // SkillsBar

        [Header("Vassals Tab Visuals")]
        [SerializeField] private Color _vassalsActiveColor = Color.white;
        [SerializeField] private Color _vassalsInactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("Rites Tab Visuals")]
        [SerializeField] private Color _ritesActiveColor = Color.white;
        [SerializeField] private Color _ritesInactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private Image _vassalsTabImage;
        private Image _ritesTabImage;

        private void Awake()
        {
            if (_vassalsTabBtn != null)
            {
                _vassalsTabImage = _vassalsTabBtn.GetComponent<Image>();
            }
                
            if (_ritesTabBtn != null)
            {
                _ritesTabImage = _ritesTabBtn.GetComponent<Image>();
            }
        }

        private void OnEnable()
        {
            if (_vassalsTabBtn != null)
                _vassalsTabBtn.onClick.AddListener(ShowVassalsTab);
                
            if (_ritesTabBtn != null)
                _ritesTabBtn.onClick.AddListener(ShowRitesTab);
        }

        private void OnDisable()
        {
            if (_vassalsTabBtn != null)
                _vassalsTabBtn.onClick.RemoveListener(ShowVassalsTab);
                
            if (_ritesTabBtn != null)
                _ritesTabBtn.onClick.RemoveListener(ShowRitesTab);
        }

        private void Start()
        {
            // Default to Vassals tab
            ShowVassalsTab();
        }

        public void ShowVassalsTab()
        {
            if (_vassalsContainer != null) _vassalsContainer.SetActive(true);
            if (_ritesContainer != null) _ritesContainer.SetActive(false);

            UpdateTabVisuals(true);
        }

        public void ShowRitesTab()
        {
            if (_vassalsContainer != null) _vassalsContainer.SetActive(false);
            if (_ritesContainer != null) _ritesContainer.SetActive(true);

            UpdateTabVisuals(false);
        }

        private void UpdateTabVisuals(bool isVassalsActive)
        {
            if (_vassalsTabBtn != null)
            {
                if (_vassalsTabImage != null)
                {
                    _vassalsTabImage.color = isVassalsActive ? _vassalsActiveColor : _vassalsInactiveColor;
                }
            }

            if (_ritesTabBtn != null)
            {
                if (_ritesTabImage != null)
                {
                    _ritesTabImage.color = !isVassalsActive ? _ritesActiveColor : _ritesInactiveColor;
                }
            }
        }
    }
}
