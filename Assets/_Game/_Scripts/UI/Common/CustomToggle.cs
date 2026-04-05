using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;
using NaughtyAttributes;

namespace MaouSamaTD.UI.Common
{
    /// <summary>
    /// Premium animated toggle component for Maou-Sama TD.
    /// Hand-crafted for the "Aris" persona.
    /// Uses GameObject references for better tool compatibility and internal caching.
    /// </summary>
    public class CustomToggle : MonoBehaviour, IPointerClickHandler
    {
        [Header("References (GameObjects)")]
        [SerializeField, Required] private GameObject _handleObject;
        [SerializeField] private GameObject _textONObject;
        [SerializeField] private GameObject _textOFFObject;
        [SerializeField] private GameObject _backgroundObject;

        [Header("Animation Settings")]
        [SerializeField, Range(0.01f, 1f)] private float _animationDuration = 0.25f;
        [SerializeField] private Ease _easeType = Ease.OutQuad;
        
        [Header("Handle Positions")]
        [SerializeField] private float _posON = 50f;
        [SerializeField] private float _posOFF = -50f;

        [Header("Colors (Optional)")]
        [SerializeField] private bool _useColorTransition = true;
        [SerializeField] private Color _colorChosen = Color.white;
        [SerializeField] private Color _colorNotChosen = new Color(1, 1, 1, 0.2f);

        [Header("State")]
        [SerializeField] private bool _isOn;
        public bool IsOn => _isOn;

        public UnityEvent<bool> OnValueChanged;

        // Cached components
        private RectTransform _handle;
        private TextMeshProUGUI _textON;
        private TextMeshProUGUI _textOFF;
        private Image _bgImage;

        [Button("Capture Current as ON")]
        private void CaptureON()
        {
            if (_handleObject == null) { Debug.LogWarning("Handle Object missing!"); return; }
            _posON = _handleObject.GetComponent<RectTransform>().anchoredPosition.x;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [Button("Capture Current as OFF")]
        private void CaptureOFF()
        {
            if (_handleObject == null) { Debug.LogWarning("Handle Object missing!"); return; }
            _posOFF = _handleObject.GetComponent<RectTransform>().anchoredPosition.x;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [Button("Preview Toggle")]
        private void PreviewToggle()
        {
            SetIsOn(!_isOn, Application.isPlaying);
        }

        private void Reset()
        {
            // Auto-assign references by name or hierarchy
            _handleObject = transform.Find("Handle")?.gameObject;
            _textONObject = transform.Find("Label_ON")?.gameObject;
            _textOFFObject = transform.Find("Label_OFF")?.gameObject;
            _backgroundObject = gameObject;
            
            CacheComponents();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                CacheComponents();
                UpdateVisuals(false);
            }
        }

        private void Awake()
        {
            CacheComponents();
            UpdateVisuals(false);
        }

        private void CacheComponents()
        {
            if (_handleObject != null) _handle = _handleObject.GetComponent<RectTransform>();
            if (_textONObject != null) _textON = _textONObject.GetComponent<TextMeshProUGUI>();
            if (_textOFFObject != null) _textOFF = _textOFFObject.GetComponent<TextMeshProUGUI>();
            if (_backgroundObject != null) _bgImage = _backgroundObject.GetComponent<Image>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Toggle();
        }

        public void Toggle()
        {
            SetIsOn(!_isOn);
        }

        public void SetIsOn(bool value, bool animate = true)
        {
            if (_isOn == value && Application.isPlaying) return;
            
            _isOn = value;
            UpdateVisuals(animate);
            OnValueChanged?.Invoke(_isOn);
        }

        public void SetIsOnWithoutNotify(bool value)
        {
            _isOn = value;
            UpdateVisuals(false);
        }

        private void UpdateVisuals(bool animate)
        {
            if (_handle == null) CacheComponents();
            if (_handle == null) return;

            float targetX = _isOn ? _posON : _posOFF;
            
            if (animate && Application.isPlaying)
            {
                _handle.DOKill();
                _handle.DOAnchorPosX(targetX, _animationDuration).SetEase(_easeType);

                if (_useColorTransition)
                {
                    if (_textON != null) _textON.DOColor(_isOn ? _colorChosen : _colorNotChosen, _animationDuration);
                    if (_textOFF != null) _textOFF.DOColor(_isOn ? _colorNotChosen : _colorChosen, _animationDuration);
                }
            }
            else
            {
                _handle.anchoredPosition = new Vector2(targetX, _handle.anchoredPosition.y);
                
                if (_useColorTransition)
                {
                    if (_textON != null) _textON.color = _isOn ? _colorChosen : _colorNotChosen;
                    if (_textOFF != null) _textOFF.color = _isOn ? _colorNotChosen : _colorChosen;
                }
            }
        }
    }
}
