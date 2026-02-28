using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace MaouSamaTD.UI.Tutorial
{
    public class TutorialMaskUI : MonoBehaviour
    {
        [SerializeField] private Image _maskImage; // A large black image with some transparency
        [SerializeField] private CanvasGroup _canvasGroup;

        public void Init()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_canvasGroup != null) _canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }
        
        // This could be expanded to use a shader for "cutouts" 
        // For now, let's keep it simple: just a full screen dim/block
        
        [SerializeField] private GameObject _panel;

        public void Show(float alpha = 0.5f)
        {
            gameObject.SetActive(true);
            _panel.SetActive(true);
            _canvasGroup.DOFade(alpha, 0.3f).SetUpdate(true); // SetUpdate(true) allows it to run while time is paused
        }

        public void Hide()
        {
            _canvasGroup.DOFade(0, 0.2f).SetUpdate(true).OnComplete(() => 
            {
                _panel.SetActive(false);
                gameObject.SetActive(false);
            });
        }
    }
}
