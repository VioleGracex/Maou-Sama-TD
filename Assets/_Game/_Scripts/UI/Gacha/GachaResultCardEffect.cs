using UnityEngine;
using UnityEngine.UI;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI.Gacha
{
    public class GachaResultCardEffect : MonoBehaviour
    {
        private Material _localMaterial;
        private Image _targetImage;

        public void ApplyGlow(Color color, float intensity = 1.8f, float scaleOffset = 1.15f)
        {
            if (_targetImage == null)
            {
                // Create a separate child for the glow so we can scale it out
                Transform glowChild = transform.Find("GlowFrame");
                if (glowChild == null)
                {
                    GameObject go = new GameObject("GlowFrame");
                    go.transform.SetParent(this.transform);
                    go.transform.SetAsFirstSibling(); // Render behind by default, or SetAsLastSibling for above
                    
                    RectTransform rt = go.AddComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    rt.localScale = Vector3.one * scaleOffset;

                    _targetImage = go.AddComponent<Image>();
                    
                    // Copy sprite from parent if it has one
                    Image parentImg = GetComponent<Image>();
                    if (parentImg != null) _targetImage.sprite = parentImg.sprite;
                }
                else
                {
                    _targetImage = glowChild.GetComponent<Image>();
                }
            }

            if (_targetImage == null) return;

            if (_localMaterial == null)
            {
                var shader = Shader.Find("Custom/CardGlow");
                if (shader != null)
                {
                    _localMaterial = new Material(shader);
                    _targetImage.material = _localMaterial;
                }
                else return;
            }

            if (_localMaterial != null)
            {
                _localMaterial.SetColor("_Color", color);
                _localMaterial.SetFloat("_GlowPower", intensity);
                _localMaterial.SetFloat("_GlowWidth", 0.12f);
                _localMaterial.SetFloat("_Speed", 1.2f);
                _localMaterial.SetVector("_ActiveSides", new Vector4(1, 1, 1, 1));
            }
        }

        private void OnDestroy()
        {
            if (_localMaterial != null)
            {
                Destroy(_localMaterial);
            }
        }
    }
}
