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

        [Header("Visual Feedback (Optional)")]
        [SerializeField] private float _inactiveDimMultiplier = 0.5f;

        private Image _vassalsTabImage;
        private Image _ritesTabImage;
        private Color _vassalsOriginalColor;
        private Color _ritesOriginalColor;

        private void Awake()
        {
            if (_vassalsTabBtn != null)
            {
                _vassalsTabImage = _vassalsTabBtn.GetComponent<Image>();
                if (_vassalsTabImage != null) _vassalsOriginalColor = _vassalsTabImage.color;
            }
                
            if (_ritesTabBtn != null)
            {
                _ritesTabImage = _ritesTabBtn.GetComponent<Image>();
                if (_ritesTabImage != null) _ritesOriginalColor = _ritesTabImage.color;
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
                    Color c = _vassalsOriginalColor;
                    if (!isVassalsActive) c = new Color(c.r * _inactiveDimMultiplier, c.g * _inactiveDimMultiplier, c.b * _inactiveDimMultiplier, c.a);
                    _vassalsTabImage.color = c;
                }
                _vassalsTabBtn.interactable = !isVassalsActive;
            }

            if (_ritesTabBtn != null)
            {
                if (_ritesTabImage != null)
                {
                    Color c = _ritesOriginalColor;
                    if (isVassalsActive) c = new Color(c.r * _inactiveDimMultiplier, c.g * _inactiveDimMultiplier, c.b * _inactiveDimMultiplier, c.a);
                    _ritesTabImage.color = c;
                }
                _ritesTabBtn.interactable = isVassalsActive;
            }
        }
    }
}
