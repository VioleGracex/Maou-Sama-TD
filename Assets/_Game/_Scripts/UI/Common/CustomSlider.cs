using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using UnityEngine.Events;

namespace MaouSamaTD.UI.Common
{
    /// <summary>
    /// Custom Premium Slider for Maou-Sama TD.
    /// Follows "Aris" Persona: Futuristic, Dark Theme, tech-cyan accents.
    /// Handles manual dragging, value mapping, and DOTween animations.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CustomSlider : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform _fillTransform;
        [SerializeField] private RectTransform _handleTransform;
        [SerializeField] private TextMeshProUGUI _tmpValue;

        [Header("Settings")]
        [SerializeField] private float _minValue = 0f;
        [SerializeField] private float _maxValue = 100f;
        [SerializeField] private bool _wholeNumbers = true;
        [SerializeField] private float _animationDuration = 0.1f;
        [SerializeField] private string _valueFormat = "{0}"; 

        [Header("Interactivity")]
        [SerializeField] private bool _interactable = true;
        [SerializeField] private Color _activeColor = new Color(0.12f, 0.94f, 1f, 1f); // Tech Cyan
        [SerializeField] private Color _disabledColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Grey

        [Header("Events")]
        public UnityEvent<float> OnValueChanged;

        private RectTransform _rectTransform;
        private float _currentValue;

        public float Value
        {
            get => _currentValue;
            set => SetValue(value);
        }

        public bool Interactable
        {
            get => _interactable;
            set
            {
                if (_interactable == value) return;
                _interactable = value;
                UpdateVisuals();
            }
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            UpdateVisuals(true); 
        }

        private void Start()
        {
            // Force a frame delay to ensure layout groups have calculated dimensions
            StartCoroutine(InitPositionRoutine());
        }

        private System.Collections.IEnumerator InitPositionRoutine()
        {
            yield return null; // Wait for end of frame or next frame
            UpdateVisuals(true);
        }

        public void SetValue(float newValue, bool notify = true)
        {
            float clampedValue = Mathf.Clamp(newValue, _minValue, _maxValue);
            if (_wholeNumbers) clampedValue = Mathf.Round(clampedValue);

            if (Mathf.Approximately(_currentValue, clampedValue)) return;

            _currentValue = clampedValue;
            UpdateVisuals();

            if (notify)
            {
                OnValueChanged?.Invoke(_currentValue);
            }
        }

        public void SetValueWithoutNotify(float newValue)
        {
            float clampedValue = Mathf.Clamp(newValue, _minValue, _maxValue);
            if (_wholeNumbers) clampedValue = Mathf.Round(clampedValue);
            
            _currentValue = clampedValue;
            UpdateVisuals(true);
        }

        private void UpdateVisuals(bool immediate = false)
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();

            float percentage = Mathf.InverseLerp(_minValue, _maxValue, _currentValue);
            float width = _rectTransform.rect.width;

            float duration = immediate ? 0f : _animationDuration;

            // Animate Fill
            if (_fillTransform != null)
            {
                _fillTransform.DOAnchorMax(new Vector2(percentage, _fillTransform.anchorMax.y), duration).SetEase(Ease.OutQuint);
            }

            // Animate Handle
            if (_handleTransform != null)
            {
                float handleX = Mathf.Lerp(0, width, percentage);
                _handleTransform.DOAnchorPosX(handleX, duration).SetEase(Ease.OutQuint);
            }

            // Update Text
            if (_tmpValue != null)
            {
                string valStr = _wholeNumbers ? Mathf.RoundToInt(_currentValue).ToString() : _currentValue.ToString("F1");
                _tmpValue.text = string.Format(_valueFormat, valStr);
                _tmpValue.color = _interactable ? Color.white : _disabledColor;
            }

            // Update Interactable Colors
            Color targetColor = _interactable ? _activeColor : _disabledColor;
            if (_fillTransform != null)
            {
                var img = _fillTransform.GetComponent<Image>();
                if (img != null) img.DOColor(targetColor, immediate ? 0f : _animationDuration);
            }
            if (_handleTransform != null)
            {
                var img = _handleTransform.GetComponent<Image>();
                if (img != null) img.DOColor(targetColor, immediate ? 0f : _animationDuration);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateFromPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateFromPointer(eventData);
        }

        private void UpdateFromPointer(PointerEventData eventData)
        {
            if (!_interactable) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
                return;

            float width = _rectTransform.rect.width;
            float normalizedX = Mathf.Clamp01((localPoint.x + _rectTransform.pivot.x * width) / width);
            float newValue = Mathf.Lerp(_minValue, _maxValue, normalizedX);
            
            SetValue(newValue);
        }

        // Context menu to auto-assign components
        [ContextMenu("Reset References")]
        private void Reset()
        {
            _fillTransform = FindInChild<RectTransform>("Fill");
            _handleTransform = FindInChild<RectTransform>("Handle");
            _tmpValue = FindInChild<TextMeshProUGUI>("Text");
            
            if (_tmpValue == null) _tmpValue = FindInChild<TextMeshProUGUI>("Value");
        }

        private T FindInChild<T>(string namePart) where T : Component
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Contains(namePart, System.StringComparison.OrdinalIgnoreCase))
                {
                    T comp = child.GetComponent<T>();
                    if (comp != null) return comp;
                }
            }
            return null;
        }
    }
}
