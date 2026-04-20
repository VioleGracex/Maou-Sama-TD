using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Data;
using MaouSamaTD.Units;
using Zenject;

namespace MaouSamaTD.UI.Gacha
{
    public class GachaResultItem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _unitAvatar;
        [SerializeField] private Image _rarityFrame;
        [SerializeField] private TextMeshProUGUI _unitName;
        [SerializeField] private GameObject _newBadge;
        [SerializeField] private GameObject _glowEffect;
        
        [Header("Animation")]
        [SerializeField] private Animator _animator;
        
        [Header("Colors")]
        [SerializeField] private Color _colorLegendary = new Color(1f, 0.85f, 0f);
        [SerializeField] private Color _colorMaster = new Color(0.7f, 0.3f, 1f);
        [SerializeField] private Color _colorElite = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color _colorCommon = new Color(0.7f, 0.7f, 0.7f);

        [Inject] private UnitDatabase _unitDatabase;

        public void Setup(UnitInventoryEntry entry, bool isNew = false)
        {
            UnitData data = _unitDatabase.GetUnitByID(entry.UnitID);
            if (data == null) return;

            if (_unitAvatar != null) _unitAvatar.sprite = data.BaseSkin.Chibi;
            if (_unitName != null) _unitName.text = data.UnitName;
            if (_newBadge != null) _newBadge.SetActive(isNew);
            
            ApplyRarityStylling(data.Rarity);
            
            if (_animator != null) _animator.SetTrigger("Reveal");
        }

        private void ApplyRarityStylling(UnitRarity rarity)
        {
            Color frameColor = _colorCommon;
            bool showGlow = false;

            switch (rarity)
            {
                case UnitRarity.Legendary:
                    frameColor = _colorLegendary;
                    showGlow = true;
                    break;
                case UnitRarity.Master:
                    frameColor = _colorMaster;
                    showGlow = true;
                    break;
                case UnitRarity.Elite:
                    frameColor = _colorElite;
                    break;
            }

            if (_rarityFrame != null) _rarityFrame.color = frameColor;
            if (_glowEffect != null) _glowEffect.SetActive(showGlow);
            
            // Set animator param if needed for premium effects
            if (_animator != null) _animator.SetInteger("Rarity", (int)rarity);
        }
    }
}
