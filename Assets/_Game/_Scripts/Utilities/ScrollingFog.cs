using UnityEngine;

namespace MaouSamaTD.Utilities
{
    public class ScrollingFog : MonoBehaviour
    {
        [Header("Scroll Speeds")]
        public Vector2 scrollSpeed = new Vector2(0.015f, 0.008f);

        private Material _material;
        private string _texturePropName = "_BaseMap";
        private Vector2 _offset;

        private void Start()
        {
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                // Instantiate the material so we only scroll this instance (prevents scrolling the project asset)
                _material = rend.material;
                
                if (!_material.HasProperty(_texturePropName))
                {
                    _texturePropName = "_MainTex";
                }
                
                // Randomize initial offset to avoid tiling patterns on duplicated quads
                _offset = new Vector2(Random.value, Random.value);
                _material.SetTextureOffset(_texturePropName, _offset);
            }
        }

        private void Update()
        {
            if (_material == null) return;

            _offset += scrollSpeed * Time.deltaTime;
            // Wrap to keep values within reasonable range (prevents floating point precision loss over long play sessions)
            _offset.x %= 1.0f;
            _offset.y %= 1.0f;
            
            _material.SetTextureOffset(_texturePropName, _offset);
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_material);
                }
                else
                {
                    DestroyImmediate(_material);
                }
            }
        }
    }
}
