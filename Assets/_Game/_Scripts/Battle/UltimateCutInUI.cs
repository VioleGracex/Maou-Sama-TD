using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using NaughtyAttributes;

using Zenject;

namespace MaouSamaTD.UI
{
    public class UltimateCutInUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private RectTransform _redBanner;
        [SerializeField] private CanvasGroup _identityContainer; // Parent of Name/Title/Skill
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _skillNameText;
        [SerializeField] private TextMeshProUGUI _ultimateText;
        [SerializeField] private Image _skillNameBgImage;
        [SerializeField] private Image _titleBgImage;
        [SerializeField] private Image _portraitImage;
        private GameObject _backgroundDim;

        [Header("Animation Settings")]
        [SerializeField] private float _bannerAngle = 6.38f;
        [SerializeField] private float _bannerSlideDistance = 1500f;
        [SerializeField] private float _identityStartScale = 1.5f;
        [SerializeField] private float _ultimateTextOffset = 800f;
        [SerializeField] private float _animationDuration = 0.7f;

        private Coroutine _activeCoroutine;

        public static UltimateCutInUI Instance { get; private set; }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private Canvas _canvas;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 3500; // Above Dialogue (3000)

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0;
                _canvasGroup.blocksRaycasts = false;
            }
            
            Transform dimChild = transform.Find("Background_Dim");
            if (dimChild != null) _backgroundDim = dimChild.gameObject;

            if (_identityContainer != null) _identityContainer.alpha = 0;
        }

        public Coroutine Play(string unitName, string unitTitle, string skillName, 
            Color bannerColor, Color titleBgColor, Color titleTextColor, 
            Color skillBgColor, Color skillTextColor, Color nameTextColor, Sprite portrait)
        {
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                CleanupAnimationState();
            }
            
            _activeCoroutine = StartCoroutine(PlayAnimation(unitName, unitTitle, skillName, bannerColor, titleBgColor, titleTextColor, skillBgColor, skillTextColor, nameTextColor, portrait));
            return _activeCoroutine;
        }

        private void CleanupAnimationState()
        {
            // Reset UI state if interrupted
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        public IEnumerator PlayAnimation(string unitName, string unitTitle, string skillName, 
            Color bannerColor, Color titleBgColor, Color titleTextColor, 
            Color skillBgColor, Color skillTextColor, Color nameTextColor, Sprite portrait)
        {
            // Initial State for this animation
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1;
                _visualRoot.SetActive(true);
                _canvasGroup.blocksRaycasts = true;
            }

            // Calculate movement vector based on angle
            float angleRad = _bannerAngle * Mathf.Deg2Rad;
            Vector2 slideDir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            if (_redBanner != null)
            {
                _redBanner.anchoredPosition = -slideDir * _bannerSlideDistance;
                if (_redBanner.GetComponent<Image>() != null) _redBanner.GetComponent<Image>().color = bannerColor;
            }

            if (_titleBgImage != null) _titleBgImage.color = titleBgColor.a == 0 ? Color.white : titleBgColor;
            if (_titleText != null) _titleText.color = titleTextColor.a == 0 ? Color.black : titleTextColor;
            if (_skillNameBgImage != null) _skillNameBgImage.color = skillBgColor.a == 0 ? Color.black : skillBgColor;
            if (_skillNameText != null) _skillNameText.color = skillTextColor.a == 0 ? Color.white : skillTextColor;
            if (_nameText != null) _nameText.color = nameTextColor.a == 0 ? Color.white : nameTextColor;

            if (_identityContainer != null)
            {
                _identityContainer.alpha = 0;
                _identityContainer.transform.localScale = Vector3.one * _identityStartScale;
            }

            _ultimateText.alpha = 0;
            Vector2 finalUltPos = _ultimateText.rectTransform.anchoredPosition;
            _ultimateText.rectTransform.anchoredPosition = finalUltPos - slideDir * _ultimateTextOffset;

            Debug.Log($"[Ultimate] STARTING sequence for {unitName}.");

            // Verify UltimateCutInUI
            if (MaouSamaTD.UI.UltimateCutInUI.Instance == null)
            {
                Debug.LogError("[Ultimate] UltimateCutInUI.Instance is NULL! Is it in the scene?");
            }

            if (_nameText != null) _nameText.text = unitName.ToUpper();
            if (_titleText != null) _titleText.text = unitTitle.ToUpper();
            if (_skillNameText != null) _skillNameText.text = skillName.ToUpper();

            if (_portraitImage != null)
            {
                _portraitImage.sprite = portrait;
                _portraitImage.color = new Color(1, 1, 1, 0);
                _portraitImage.rectTransform.anchoredPosition = new Vector2(-500, 0);
                _portraitImage.DOFade(1, _animationDuration).SetUpdate(true);
                _portraitImage.rectTransform.DOAnchorPos(Vector2.zero, _animationDuration).SetEase(Ease.OutCubic).SetUpdate(true);
            }

            // 1. Simultaneous: Identity Scales Down & Fades In + Banner Slides In diagonally
            if (_identityContainer != null)
            {
                _identityContainer.DOFade(1, _animationDuration * 0.6f).SetUpdate(true);
                _identityContainer.transform.DOScale(1.0f, _animationDuration).SetEase(Ease.OutBack).SetUpdate(true);
            }

            Tween bannerTween = null;
            if (_redBanner != null)
            {
                bannerTween = _redBanner.DOAnchorPos(Vector2.zero, _animationDuration).SetEase(Ease.OutQuart).SetUpdate(true);
            }

            // Wait for banner to fully arrive before moving the "ULTIMATE" word
            if (bannerTween != null) yield return bannerTween.WaitForCompletion();
            else yield return new WaitForSecondsRealtime(_animationDuration);

            // 2. Ultimate text reveal - Starts ONLY AFTER banner is in position
            _ultimateText.DOFade(1, 0.2f).SetUpdate(true);
            yield return _ultimateText.rectTransform.DOAnchorPos(finalUltPos, 0.5f).SetEase(Ease.OutBack).SetUpdate(true).WaitForCompletion();

            // Hold
            yield return new WaitForSecondsRealtime(1.0f); // Reduced from 1.5s for snappier combat

            // Exit along the same diagonal path
            if (_redBanner != null) _redBanner.DOAnchorPos(slideDir * _bannerSlideDistance, 0.5f).SetEase(Ease.InCubic).SetUpdate(true);
            if (_identityContainer != null)
            {
                _identityContainer.DOFade(0, 0.3f).SetUpdate(true);
                _identityContainer.transform.DOScale(_identityStartScale * 0.8f, 0.3f).SetUpdate(true);
            }
            if (_portraitImage != null)
            {
                _portraitImage.DOFade(0, 0.3f).SetUpdate(true);
                _portraitImage.rectTransform.DOAnchorPos(new Vector2(-500, 0), 0.3f).SetUpdate(true);
            }
            _ultimateText.DOFade(0, 0.3f).SetUpdate(true);
            
            if (_canvasGroup != null)
            {
                yield return _canvasGroup.DOFade(0, 0.5f).SetUpdate(true).WaitForCompletion();
            }

            if (_canvasGroup != null) _canvasGroup.blocksRaycasts = false;
            _activeCoroutine = null;
        }

        [Button("Test Cut-In Animation")]
        private void TestAnimation()
        {
            // Now works because GameObject is active
            StopAllCoroutines();
            StartCoroutine(PlayAnimation("IGNIS", "THE CRIMSON BASTION", "PHOENIX Radiance", Color.red, Color.white, Color.black, Color.black, Color.white, Color.white, null));
        }
    }
}
