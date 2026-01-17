using UnityEngine;
using TMPro;
using DG.Tweening;

namespace MaouSamaTD.UI
{
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _textComponent;
        
        public void Init(float damage, bool isCrit, Color color)
        {
            if (_textComponent == null) _textComponent = GetComponent<TextMeshPro>();
            if (_textComponent == null) _textComponent = gameObject.AddComponent<TextMeshPro>();

            _textComponent.text = Mathf.RoundToInt(damage).ToString();
            _textComponent.color = color;
            _textComponent.fontSize = isCrit ? 8 : 5; // Bigger if crit
            _textComponent.alignment = TextAlignmentOptions.Center;
            
            // Animation
            transform.localScale = Vector3.one;
            
            // Random Drift
            float randomX = Random.Range(-0.5f, 0.5f);
            Vector3 targetPos = transform.localPosition + new Vector3(randomX, 2f, 0);

            transform.DOLocalMove(targetPos, 1f).SetEase(Ease.OutCirc);
            _textComponent.DOFade(0f, 1f).SetEase(Ease.InQuad).OnComplete(() => Destroy(gameObject));
        }

        private void LateUpdate()
        {
            // Billboard to camera
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
}
