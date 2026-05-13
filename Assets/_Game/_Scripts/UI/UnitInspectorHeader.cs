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
