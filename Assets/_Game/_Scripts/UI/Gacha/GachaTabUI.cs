using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MaouSamaTD.UI.Gacha
{
    public class GachaTabUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _selectedScale = 1.2f;
        [SerializeField] private float _unselectedScale = 1.0f;
        [SerializeField] private float _unselectedAlpha = 0.5f;
        [SerializeField] private float _transitionDuration = 0.2f;

        [Header("References")]
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _tabText;

        private Vector3 _targetScale;
        private float _targetAlpha;

        private void Awake()
        {
            if (_contentRoot == null) _contentRoot = GetComponent<RectTransform>();
        }

        public void SetState(bool selected, bool immediate = false)
        {
            _targetScale = Vector3.one * (selected ? _selectedScale : _unselectedScale);
            _targetAlpha = selected ? 1.0f : _unselectedAlpha;

            if (immediate)
            {
                if (_contentRoot != null) _contentRoot.localScale = _targetScale;
                SetAlpha(_targetAlpha);
            }
        }

        private void Update()
        {
            if (_contentRoot != null)
            {
                _contentRoot.localScale = Vector3.Lerp(_contentRoot.localScale, _targetScale, Time.deltaTime / _transitionDuration);
            }

            float currentAlpha = _background != null ? _background.color.a : (_tabText != null ? _tabText.color.a : 1.0f);
            float newAlpha = Mathf.Lerp(currentAlpha, _targetAlpha, Time.deltaTime / _transitionDuration);
            SetAlpha(newAlpha);
        }

        private void SetAlpha(float alpha)
        {
            if (_background != null)
            {
                Color c = _background.color;
                c.a = alpha;
                _background.color = c;
            }
            if (_tabText != null)
            {
                Color c = _tabText.color;
                c.a = alpha;
                _tabText.color = c;
            }
        }
    }
}
