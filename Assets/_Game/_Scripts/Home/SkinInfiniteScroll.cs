using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections.Generic;
using System;
using NaughtyAttributes;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// A high-fidelity, infinite horizontal scroll for skin selection.
    /// Features smooth DOTween animations, depth scaling, and center enlargement.
    /// </summary>
    public class SkinInfiniteScroll : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IScrollHandler
    {
        [Header("Layout Settings")]
        [SerializeField] private float _itemSpacing = 200f;
        [SerializeField] private float _centerScale = 1.3f;
        [SerializeField] private float _sideScale = 0.7f;
        [SerializeField] private float _sideAlpha = 0.5f;
        [SerializeField] private float _depthEffectStrength = 1.0f;
        [SerializeField] private int _visibleItemsCount = 5;

        [Header("Animation Settings")]
        [SerializeField] private float _snapDuration = 0.3f;
        [SerializeField] private Ease _snapEase = Ease.OutBack;
        [SerializeField] private float _dragSensitivity = 1.0f;
        [SerializeField] private float _scrollWheelSensitivity = 0.5f;

        [Header("References")]
        [SerializeField] private RectTransform _content;
        [SerializeField] private Button _btnLeft;
        [SerializeField] private Button _btnRight;

        public RectTransform Content => _content;
        public event Action<int> OnSelectionChanged;

        private List<RectTransform> _items = new List<RectTransform>();
        private List<RectTransform> _sortCache = new List<RectTransform>();
        private float _currentScroll = 0f;
        private float _targetScroll = 0f;
        private Tween _snapTween;
        private bool _isDragging = false;
        private int _selectedIndex = -1;
        private bool _needsLayout = false;

        private void OnEnable()
        {
            if (_btnLeft) _btnLeft.onClick.AddListener(MovePrev);
            if (_btnRight) _btnRight.onClick.AddListener(MoveNext);
        }

        private void OnDisable()
        {
            if (_btnLeft) _btnLeft.onClick.RemoveListener(MovePrev);
            if (_btnRight) _btnRight.onClick.RemoveListener(MoveNext);
            _snapTween?.Kill();
        }

        public void Initialize(List<GameObject> itemObjects)
        {
            _items.Clear();
            foreach (var obj in itemObjects)
            {
                if (obj != null)
                {
                    _items.Add(obj.GetComponent<RectTransform>());
                }
            }
            
            _currentScroll = 0;
            _targetScroll = 0;
            ApplyLayout();
            NotifySelection(0);
        }

        private void Update()
        {
            if (!_isDragging && (_snapTween == null || !_snapTween.IsActive()))
            {
                if (Mathf.Abs(_currentScroll - _targetScroll) > 0.001f)
                {
                    _currentScroll = Mathf.Lerp(_currentScroll, _targetScroll, Time.deltaTime * 15f);
                    ApplyLayout();
                }
            }
            
            if (_needsLayout)
            {
                _needsLayout = false;
                ApplyLayout();
            }
        }

        [Button("Fix Layout (Manual)")]
        public void EditorFixLayout()
        {
            if (_content != null)
            {
                _items.Clear();
                foreach (RectTransform child in _content) _items.Add(child);
            }
            ApplyLayout();
        }

        public void ApplyLayout()
        {
            int count = _items.Count;
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                var item = _items[i];
                if (item == null) continue;

                // Enforce layout: Center anchors and pivot for reliable math
                item.anchorMin = item.anchorMax = item.pivot = new Vector2(0.5f, 0.5f);

                // Use modulo to get exact index in infinite loop
                float offset = i - _currentScroll;
                
                // Infinite wrap logic
                while (offset > count / 2f) offset -= count;
                while (offset < -count / 2f) offset += count;

                item.anchoredPosition = new Vector2(offset * _itemSpacing, 0);

                float distance = Mathf.Abs(offset);
                float t = Mathf.Clamp01(distance * _depthEffectStrength);
                
                float scale = Mathf.Lerp(_centerScale, _sideScale, t);
                item.localScale = new Vector3(scale, scale, 1);

                var cg = item.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = Mathf.Lerp(1f, _sideAlpha, t);
                
                item.gameObject.SetActive(distance < _visibleItemsCount / 2f + 1);
            }

            // Depth sorting WITHOUT modifying the primary _items list order
            _sortCache.Clear();
            _sortCache.AddRange(_items);
            _sortCache.Sort((a, b) => a.localScale.x.CompareTo(b.localScale.x));
            for (int i = 0; i < _sortCache.Count; i++)
            {
                _sortCache[i].SetAsLastSibling();
            }
        }

        public void MoveNext()
        {
            _targetScroll = Mathf.Round(_targetScroll + 1);
            PerformSnap();
        }

        public void MovePrev()
        {
            _targetScroll = Mathf.Round(_targetScroll - 1);
            PerformSnap();
        }

        private void PerformSnap()
        {
            int count = _items.Count;
            if (count == 0) return;

            _snapTween?.Kill();
            _snapTween = DOTween.To(() => _currentScroll, x => {
                _currentScroll = x;
                ApplyLayout();
            }, _targetScroll, _snapDuration)
            .SetEase(_snapEase)
            .OnComplete(() => {
                _targetScroll = Mathf.Repeat(_targetScroll, count);
                _currentScroll = _targetScroll;
                CheckSelectionUpdate();
            });
        }

        private void CheckSelectionUpdate()
        {
            if (_items.Count == 0) return;
            int index = Mathf.RoundToInt(Mathf.Repeat(_currentScroll, _items.Count));
            if (index != _selectedIndex)
            {
                NotifySelection(index);
            }
        }

        private void NotifySelection(int index)
        {
            _selectedIndex = index;
            OnSelectionChanged?.Invoke(index);
        }

        #region Input Handlers
        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _snapTween?.Kill();
        }

        public void OnDrag(PointerEventData eventData)
        {
            float delta = eventData.delta.x / _itemSpacing * _dragSensitivity;
            _currentScroll -= delta;
            _targetScroll = _currentScroll;
            ApplyLayout();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            _targetScroll = Mathf.Round(_currentScroll);
            PerformSnap();
        }

        public void OnScroll(PointerEventData eventData)
        {
            _targetScroll -= eventData.scrollDelta.y * _scrollWheelSensitivity;
            _targetScroll = Mathf.Round(_targetScroll);
            PerformSnap();
        }
        #endregion
    }
}
