using UnityEngine;
using UnityEngine.UI;

namespace MaouSamaTD.UI
{
    public class UnitCardView : MonoBehaviour
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private Image _classIcon;
        [SerializeField] private RectTransform _starsContainer;
        [SerializeField] private Image _overlay;

        public void SetData(MaouSamaTD.Units.UnitData data)
        {
            if (data == null) 
            {
                SetEmpty();
                return;
            }
            
            if (_portrait != null)
            {
                 // Prefer UnitSprite (Portrait) if available, otherwise Icon?
                 _portrait.sprite = data.UnitSprite;
                 if (_portrait.sprite == null) _portrait.sprite = data.UnitIcon;
                 _portrait.color = Color.white;
            }

            if (_classIcon != null)
            {
                // We'd need a Helper to map UnitClass enum to sprite
                // _classIcon.sprite = ClassIconHelper.GetIcon(data.Class);
                // For now, disabling or ignoring
                _classIcon.enabled = false; 
            }
            
            // Re-enable Overlay if needed? Defaults on/off?
            // Assuming default state is OK.
        }

        public void SetEmpty()
        {
             // Clear visuals
             if (_portrait) _portrait.sprite = null;
        }
    }
}