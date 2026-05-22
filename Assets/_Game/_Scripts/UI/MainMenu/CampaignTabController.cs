using UnityEngine;
using UnityEngine.UI;
using MaouSamaTD.Levels;


namespace MaouSamaTD.UI.MainMenu
{
    [System.Serializable]
    public class CampaignTabController
    {
        [SerializeField] private Button _mainStoryTabButton;
        [SerializeField] private Button _resourceDungeonsTabButton;
        [SerializeField] private Button _specialDungeonsTabButton;

        [SerializeField] private bool _showMainStory = true;
        [SerializeField] private bool _showResourceDungeons = false;
        [SerializeField] private bool _showSpecialDungeons = false;

        public bool ShowMainStory => _showMainStory;
        public bool ShowResourceDungeons => _showResourceDungeons;
        public bool ShowSpecialDungeons => _showSpecialDungeons;

        private CampaignPage _page;

        public void Initialize(
            CampaignPage page,
            Button mainStoryTabButton,
            Button resourceDungeonsTabButton,
            Button specialDungeonsTabButton,
            bool showMainStory,
            bool showResourceDungeons,
            bool showSpecialDungeons)
        {
            _page = page;
            _mainStoryTabButton = mainStoryTabButton;
            _resourceDungeonsTabButton = resourceDungeonsTabButton;
            _specialDungeonsTabButton = specialDungeonsTabButton;
            _showMainStory = showMainStory;
            _showResourceDungeons = showResourceDungeons;
            _showSpecialDungeons = showSpecialDungeons;

            if (_mainStoryTabButton != null)
            {
                _mainStoryTabButton.onClick.RemoveAllListeners();
                _mainStoryTabButton.onClick.AddListener(() => ToggleCategory(LevelCategory.MainStory));
            }
            if (_resourceDungeonsTabButton != null)
            {
                _resourceDungeonsTabButton.onClick.RemoveAllListeners();
                _resourceDungeonsTabButton.onClick.AddListener(() => ToggleCategory(LevelCategory.ResourceDungeon));
            }
            if (_specialDungeonsTabButton != null)
            {
                _specialDungeonsTabButton.onClick.RemoveAllListeners();
                _specialDungeonsTabButton.onClick.AddListener(() => ToggleCategory(LevelCategory.RiteDungeon));
            }

            UpdateTabVisuals();
        }

        public void SetToggles(bool mainStory, bool resourceDungeons, bool specialDungeons)
        {
            _showMainStory = mainStory;
            _showResourceDungeons = resourceDungeons;
            _showSpecialDungeons = specialDungeons;
            UpdateTabVisuals();
        }

        public void ToggleCategory(LevelCategory category)
        {
            switch (category)
            {
                case LevelCategory.MainStory:
                    _showMainStory = !_showMainStory;
                    break;
                case LevelCategory.ResourceDungeon:
                    _showResourceDungeons = !_showResourceDungeons;
                    break;
                case LevelCategory.RiteDungeon:
                case LevelCategory.VassalDungeon:
                    _showSpecialDungeons = !_showSpecialDungeons;
                    break;
            }

            // Fallback comfort: keep at least one category visible
            if (!_showMainStory && !_showResourceDungeons && !_showSpecialDungeons)
            {
                switch (category)
                {
                    case LevelCategory.MainStory:
                        _showMainStory = true;
                        break;
                    case LevelCategory.ResourceDungeon:
                        _showResourceDungeons = true;
                        break;
                    default:
                        _showSpecialDungeons = true;
                        break;
                }
            }

            UpdateTabVisuals();
            _page.Refresh();
        }

        public void UpdateTabVisuals()
        {
            ApplyTabActiveAlpha(_mainStoryTabButton, _showMainStory);
            ApplyTabActiveAlpha(_resourceDungeonsTabButton, _showResourceDungeons);
            ApplyTabActiveAlpha(_specialDungeonsTabButton, _showSpecialDungeons);
        }

        private void ApplyTabActiveAlpha(Button button, bool isActive)
        {
            if (button == null) return;
            var img = button.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, isActive ? 1f : 0.80f);
            }
        }

        public void EnsureTabsOnTop(GameObject visualRoot, Transform levelContainer)
        {
            if (_mainStoryTabButton != null)
                _mainStoryTabButton.transform.SetAsLastSibling();
            if (_resourceDungeonsTabButton != null)
                _resourceDungeonsTabButton.transform.SetAsLastSibling();
            if (_specialDungeonsTabButton != null)
                _specialDungeonsTabButton.transform.SetAsLastSibling();

            var tabParent = _mainStoryTabButton?.transform.parent;
            if (tabParent != null && tabParent != levelContainer)
            {
                tabParent.SetAsLastSibling();
                
                Transform current = tabParent;
                Transform root = visualRoot != null ? visualRoot.transform : _page.transform;
                while (current != null && current.parent != null && current.parent != root && current.parent != current)
                {
                    current.parent.SetAsLastSibling();
                    current = current.parent;
                }
            }
        }
    }
}
