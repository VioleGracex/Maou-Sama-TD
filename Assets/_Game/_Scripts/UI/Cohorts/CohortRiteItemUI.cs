using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using MaouSamaTD.Skills;

namespace MaouSamaTD.UI.Cohorts
{
    public class CohortRiteItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _costText;

        [Header("Accordion Details")]
        [SerializeField] private GameObject _detailsPanel;
        [SerializeField] private TextMeshProUGUI _statsText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _expandArrowText;

        [Header("Tags & Range")]
        [SerializeField] private RangePatternUI _rangeGrid;
        [SerializeField] private RectTransform _tagsContainer;
        [SerializeField] private GameObject _tagPrefab;
        [SerializeField] private Outline _cardOutline;

        public SovereignRiteData RiteData { get; private set; }
        private CanvasGroup _canvasGroup;
        private Canvas _parentCanvas;
        private GameObject _draggedVisual;
        private bool _isLocked = false;
        private bool _isExpanded = false;
        private bool _isAccordion = false;
        private Coroutine _animateCoroutine = null;

        public void Setup(SovereignRiteData data, bool isLocked)
        {
            RiteData = data;
            _isLocked = isLocked;

            if (_iconImage != null && data != null && data.Icon != null)
                _iconImage.sprite = data.Icon;
            if (_nameText != null && data != null)
                _nameText.text = data.SkillName;
            if (_costText != null && data != null)
                _costText.text = $"{data.SealCost} Seals";

            if (_descriptionText != null && data != null)
                _descriptionText.text = data.GetFormattedDescription();

            if (_statsText != null && data != null)
            {
                string stats = "";
                if (data.EffectType == SkillEffectType.Damage)
                {
                    stats += $"Damage: {data.Value}";
                }
                else if (data.EffectType == SkillEffectType.Buff || data.EffectType == SkillEffectType.Debuff)
                {
                    stats += $"Effect: {data.EffectType} ({data.Value})";
                    if (data.Duration > 0) stats += $" | Duration: {data.Duration}s";
                }
                else
                {
                    stats += $"Effect: {data.EffectType}";
                }

                stats += $" | Cooldown: {data.Cooldown:F0}s";
                _statsText.text = stats;
            }

            if (_rangeGrid != null && data != null)
            {
                _rangeGrid.SetAoePattern(data.AoeShape, data.Radius);
            }

            Color typeColor = GetColorForEffectType(data != null ? data.EffectType : SkillEffectType.Damage);

            if (_cardOutline != null)
            {
                _cardOutline.effectColor = typeColor;
            }

            if (_tagsContainer != null && data != null)
            {
                Transform tagsLine = _tagsContainer.Find("TagsLine");
                Transform searchRoot = tagsLine != null ? tagsLine : _tagsContainer;

                Transform spellTag = searchRoot.Find("Tag_Spell_Type");
                if (spellTag != null)
                {
                    spellTag.gameObject.SetActive(true);
                    var txt = spellTag.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = data.Persistence.ToString().ToUpper();
                }

                Transform effectTag = searchRoot.Find("Tag_Effect_Type");
                if (effectTag != null)
                {
                    effectTag.gameObject.SetActive(true);
                    var txt = effectTag.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = data.EffectType.ToString().ToUpper();
                    
                    var img = effectTag.GetComponent<Image>();
                    // Give effect tag the color of the effect
                    if (img != null) img.color = new Color(typeColor.r, typeColor.g, typeColor.b, 0.8f);
                    
                    var outline = effectTag.GetComponent<Outline>();
                    if (outline != null) outline.effectColor = typeColor;
                }
                
                // Cleanup any old dynamic clones just in case
                foreach (Transform child in searchRoot)
                {
                    if (child.name.Contains("(Clone)")) Destroy(child.gameObject);
                }
            }

            // Accordion logic: only if the description is too long (> 70 chars) do we make it an accordion
            bool isLong = data != null && !string.IsNullOrEmpty(data.GetFormattedDescription()) && data.GetFormattedDescription().Length > 70;
            _isAccordion = isLong;

            if (_expandArrowText != null)
            {
                _expandArrowText.gameObject.SetActive(isLong);
            }

            if (isLong)
            {
                _isExpanded = false;
                if (_expandArrowText != null) _expandArrowText.text = "▼";
                if (_descriptionText != null) _descriptionText.text = GetCollapsedText(data.GetFormattedDescription());
                if (_statsText != null) _statsText.gameObject.SetActive(false);
                if (_detailsPanel != null) _detailsPanel.SetActive(true);
                SetHeightImmediate(140f);
            }
            else
            {
                _isExpanded = true;
                if (_expandArrowText != null) _expandArrowText.text = "";
                if (_descriptionText != null) _descriptionText.text = data != null ? data.GetFormattedDescription() : "";
                if (_statsText != null) _statsText.gameObject.SetActive(true);
                if (_detailsPanel != null) _detailsPanel.SetActive(true);
                SetHeightImmediate(140f);
            }

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _parentCanvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isLocked || RiteData == null) return;

            // Instantiate visual helper for dragging
            _draggedVisual = Instantiate(gameObject, _parentCanvas.transform);
            
            // Remove scripts on the copy to avoid double drags/drops
            var duplicateComponent = _draggedVisual.GetComponent<CohortRiteItemUI>();
            if (duplicateComponent != null) Destroy(duplicateComponent);
            
            // Set opacity of visual copy
            var cg = _draggedVisual.GetComponent<CanvasGroup>();
            if (cg == null) cg = _draggedVisual.AddComponent<CanvasGroup>();
            cg.alpha = 0.6f;
            cg.blocksRaycasts = false;

            // Dim original
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0.4f;

            UpdateDragPosition(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isLocked || _draggedVisual == null) return;
            UpdateDragPosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_draggedVisual != null)
            {
                Destroy(_draggedVisual);
                _draggedVisual = null;
            }

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isLocked || RiteData == null) return;

            // Ignore clicks if dragging was active
            if (eventData.dragging) return;

            if (CohortRiteSlot.SelectedSlot != null)
            {
                CohortRiteSlot.SelectedSlot.SelectRite(RiteData);
                CohortRiteSlot.SelectedSlot.Deselect();
            }
            else if (_isAccordion)
            {
                ToggleExpand();
            }
        }

        public void ToggleExpand()
        {
            if (!_isAccordion) return;

            _isExpanded = !_isExpanded;
            if (_expandArrowText != null) _expandArrowText.text = _isExpanded ? "▲" : "▼";

            if (_isExpanded)
            {
                if (_descriptionText != null && RiteData != null)
                    _descriptionText.text = RiteData.GetFormattedDescription();
                if (_statsText != null) _statsText.gameObject.SetActive(true);
                StartAnimateHeight(260f);
            }
            else
            {
                if (_descriptionText != null && RiteData != null)
                    _descriptionText.text = GetCollapsedText(RiteData.GetFormattedDescription());
                if (_statsText != null) _statsText.gameObject.SetActive(false);
                StartAnimateHeight(140f);
            }
        }

        private void SetHeightImmediate(float targetHeight)
        {
            if (_animateCoroutine != null)
            {
                StopCoroutine(_animateCoroutine);
                _animateCoroutine = null;
            }
            var le = GetComponent<LayoutElement>();
            if (le == null) le = gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = targetHeight;
            le.minHeight = targetHeight;
            if (transform.parent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
            }
        }

        private void StartAnimateHeight(float targetHeight)
        {
            if (_animateCoroutine != null) StopCoroutine(_animateCoroutine);
            _animateCoroutine = StartCoroutine(AnimateHeightCoroutine(targetHeight));
        }

        private System.Collections.IEnumerator AnimateHeightCoroutine(float targetHeight)
        {
            var le = GetComponent<LayoutElement>();
            if (le == null) le = gameObject.AddComponent<LayoutElement>();

            float startHeight = le.preferredHeight;
            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t); // Smoothstep

                float currentHeight = Mathf.Lerp(startHeight, targetHeight, t);
                le.preferredHeight = currentHeight;
                le.minHeight = currentHeight;

                if (transform.parent != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
                }
                yield return null;
            }

            le.preferredHeight = targetHeight;
            le.minHeight = targetHeight;
            if (transform.parent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
            }
            _animateCoroutine = null;
        }

        private string GetCollapsedText(string fullText)
        {
            if (string.IsNullOrEmpty(fullText)) return "";
            if (fullText.Length <= 70) return fullText;
            return fullText.Substring(0, 67) + "...";
        }

        private void UpdateDragPosition(PointerEventData eventData)
        {
            if (_draggedVisual == null || _parentCanvas == null) return;

            RectTransform rect = _draggedVisual.GetComponent<RectTransform>();
            Vector3 globalMousePos;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                _parentCanvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out globalMousePos))
            {
                rect.position = globalMousePos;
            }
        }

        private Color GetColorForEffectType(SkillEffectType type)
        {
            switch (type)
            {
                case SkillEffectType.Buff: return new Color(0.2f, 0.8f, 0.2f, 1f); // Green
                case SkillEffectType.Debuff: return new Color(0.8f, 0.2f, 0.8f, 1f); // Purple
                case SkillEffectType.Damage: return new Color(0.9f, 0.3f, 0.1f, 1f); // Red-Orange
                default: return new Color(0.4f, 0.6f, 0.8f, 1f); // Blue
            }
        }
    }
}
