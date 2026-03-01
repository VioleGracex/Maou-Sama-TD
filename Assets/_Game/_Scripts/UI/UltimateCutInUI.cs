using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using NaughtyAttributes;

namespace MaouSamaTD.UI
{
    public class UltimateCutInUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _redBanner;
        [SerializeField] private CanvasGroup _identityContainer; // Parent of Name/Title/Skill
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _skillNameText;
        [SerializeField] private TextMeshProUGUI _ultimateText;
        private GameObject _backgroundDim;

        public static UltimateCutInUI Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (_canvasGroup != null) _canvasGroup.alpha = 0;
            
            Transform dimChild = transform.Find("Background_Dim");
            if (dimChild != null) _backgroundDim = dimChild.gameObject;

            gameObject.SetActive(false);
        }

        public IEnumerator PlayAnimation(string unitName, string unitTitle, string skillName, Color bannerColor)
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0;
            
            // 0. Setup Initial State
            _redBanner.anchoredPosition = new Vector2(-Screen.width * 2f, 0); // Far off left
            if (_redBanner.GetComponent<Image>() != null) _redBanner.GetComponent<Image>().color = bannerColor;

            _identityContainer.alpha = 0;
            _ultimateText.alpha = 0;
            // Move ultimate text back to a starting position if it's moving into place
            Vector2 finalUltPos = _ultimateText.rectTransform.anchoredPosition;
            _ultimateText.rectTransform.anchoredPosition = new Vector2(-Screen.width, finalUltPos.y);

            if (_nameText != null) _nameText.text = unitName.ToUpper();
            if (_titleText != null) _titleText.text = unitTitle.ToUpper();
            if (_skillNameText != null) _skillNameText.text = skillName.ToUpper();

            // 1. Show Identity (Name/Title/Skill) in center
            _canvasGroup.DOFade(1, 0.2f).SetUpdate(true);
            yield return _identityContainer.DOFade(1, 0.4f).SetUpdate(true).WaitForCompletion();
            yield return new WaitForSecondsRealtime(0.3f);

            // 2. Ribbon slides from Left + Background Dims
            if (_backgroundDim != null)
            {
                _backgroundDim.SetActive(true);
                CanvasGroup dimCG = _backgroundDim.GetComponent<CanvasGroup>();
                if (dimCG != null) dimCG.DOFade(0.7f, 0.5f).SetUpdate(true);
                else
                {
                    Image dimImage = _backgroundDim.GetComponent<Image>();
                    if (dimImage != null) dimImage.DOFade(0.7f, 0.5f).SetUpdate(true);
                }
            }
            _redBanner.DOAnchorPos(Vector2.zero, 0.6f).SetEase(Ease.OutCubic).SetUpdate(true);
            yield return new WaitForSecondsRealtime(0.4f);

            // 3. Ultimate text moves into position
            _ultimateText.DOFade(1, 0.2f).SetUpdate(true);
            yield return _ultimateText.rectTransform.DOAnchorPos(finalUltPos, 0.5f).SetEase(Ease.OutBack).SetUpdate(true).WaitForCompletion();

            // Hold
            yield return new WaitForSecondsRealtime(1.2f);

            // Exit: Everything fades out or slides away
            _redBanner.DOAnchorPos(new Vector2(Screen.width * 2f, 0), 0.5f).SetEase(Ease.InCubic).SetUpdate(true);
            _identityContainer.DOFade(0, 0.3f).SetUpdate(true);
            _ultimateText.DOFade(0, 0.3f).SetUpdate(true);
            yield return _canvasGroup.DOFade(0, 0.5f).SetUpdate(true).WaitForCompletion();

            gameObject.SetActive(false);
        }

        [Button("Test Cut-In Animation")]
        private void TestAnimation()
        {
            if (Application.isPlaying)
            {
                StopAllCoroutines();
                StartCoroutine(PlayAnimation("IGNIS", "THE CRIMSON BASTION", "PHOENIX ASCEND", Color.red));
            }
            else
            {
                Debug.LogWarning("[UltimateCutInUI] Editor-mode animation preview requires Play Mode to run Coroutines/DOTween correctly.");
            }
        }
    }
}
