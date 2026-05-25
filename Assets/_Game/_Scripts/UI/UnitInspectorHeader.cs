using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Handles name, level, rarity, stars, and portrait visuals for the unit inspector.
    /// </summary>
    public class UnitInspectorHeader : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _levelMaxText;
        [SerializeField] private TextMeshProUGUI _rarityText;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Transform _starsRoot;
        [SerializeField] private TextMeshProUGUI _expText;
        [SerializeField] private Image _levelFillImage;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _costText;

        private void Awake()
        {
            AutoBind();
        }

        private void AutoBind()
        {
            var rootUI = GetComponentInParent<UnitInspectorFullScreenUI>();
            Transform searchRoot = rootUI != null ? rootUI.transform : this.transform.root;

            var texts = searchRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                string n = t.name.ToLower();
                if (_nameText == null && n.Contains("name")) _nameText = t;
                else if (_levelMaxText == null && n.Contains("max")) _levelMaxText = t;
                else if (_levelText == null && n.Contains("level") && !n.Contains("amity")) _levelText = t;
                else if (_rarityText == null && n.Contains("rarity")) _rarityText = t;
                else if (_expText == null && n.Contains("exp")) _expText = t;
                else if (_hpText == null && n.Contains("hp")) _hpText = t;
                else if (_costText == null && n.Contains("cost")) _costText = t;
            }

            var images = searchRoot.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                string n = img.name.ToLower();
                if (_portraitImage == null && n.Contains("portrait")) _portraitImage = img;
                else if (_levelFillImage == null && (n.Contains("fill") || n.Contains("bar"))) _levelFillImage = img;
            }

            if (_starsRoot == null)
            {
                foreach (Transform t in searchRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.ToLower().Contains("star") && t.childCount > 0)
                    {
                        _starsRoot = t;
                        break;
                    }
                }
            }
        }

        public void Refresh(UnitData u)
        {
            if (u == null) return;

            if (_nameText) _nameText.text = u.UnitName.ToUpper();
            if (_rarityText) _rarityText.text = u.Rarity.GetShortName();
            
            // Full body portrait in main area
            if (_portraitImage) _portraitImage.sprite = u.GetSprite(UnitData.UnitImageType.FullSprite);
            
            if (_levelText) _levelText.text = $"{u.Level}";
            if (_levelMaxText) _levelMaxText.text = $"/ {u.MaxLevel}";
            
            // Experience / Progress
            int req = MaouSamaTD.Progression.ProgressionLogic.GetRequiredXP(u.Level);
            float progress = (float)u.Experience / req;
            
            if (_levelFillImage) _levelFillImage.fillAmount = progress;
            if (_expText) _expText.text = $"{u.Experience} / {req}";

            // Star Rating
            if (_starsRoot != null)
            {
                for (int i = 0; i < _starsRoot.childCount; i++)
                {
                    Transform star = _starsRoot.GetChild(i);
                    Image img = star.GetComponent<Image>();
                    if (img == null) img = star.GetComponentInChildren<Image>();
                    if (img) img.color = i < u.StarRating ? new Color(1f, 0.8f, 0f) : new Color(0.2f, 0.2f, 0.2f);
                }
            }

            // HP & Cost
            if (_hpText)
            {
                float displayHp = u.CalculatedStats.MaxHp > 0 ? u.CalculatedStats.MaxHp : u.MaxHp * 2f;
                _hpText.text = $"HP {displayHp:F0}";
            }
            if (_costText)
            {
                _costText.text = $"{u.DeploymentCost} SP";
            }
        }
    }
}
