using UnityEngine;

namespace MaouSamaTD.Grid
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SelectionGlowController : MonoBehaviour
    {
        [SerializeField] private float fadeSpeed = 5f;
        private Material _material;
        private float _targetLevel = 0f;
        private float _currentLevel = 0f;
        private static readonly int SelectionLevelId = Shader.PropertyToID("_SelectionLevel");

        private void Awake()
        {
            _material = GetComponent<SpriteRenderer>().material;
        }

        public void SetSelected(bool isSelected)
        {
            _targetLevel = isSelected ? 1f : 0f;
        }

        private void Update()
        {
            if (Mathf.Approximately(_currentLevel, _targetLevel)) return;

            _currentLevel = Mathf.MoveTowards(_currentLevel, _targetLevel, fadeSpeed * Time.deltaTime);
            _material.SetFloat(SelectionLevelId, _currentLevel);
        }
    }
}
