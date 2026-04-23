using UnityEngine;
using UnityEngine.UI;
using MaouSamaTD.Units;

namespace MaouSamaTD.UI.Gacha
{
    public class GachaResultCardEffect : MonoBehaviour
    {
        private static Material _glowMaterialBase;
        private Material _localMaterial;
        private Image _targetImage;

        public void ApplyGlow(Color color, float intensity = 3.0f)
        {
            if (_targetImage == null)
            {
                // Try to find the portrait or frame image to apply the material to
                _targetImage = GetComponent<Image>();
                if (_targetImage == null) _targetImage = GetComponentInChildren<Image>();
            }

            if (_targetImage == null) return;

            if (_localMaterial == null)
            {
                if (_glowMaterialBase == null)
                {
                    _glowMaterialBase = Resources.Load<Material>("Materials/GachaResultGlow"); // We'll create this or use the shader directly
                }

                if (_glowMaterialBase != null)
                {
                    _localMaterial = new Material(_glowMaterialBase);
                }
                else
                {
                    // Fallback: create from shader name
                    var shader = Shader.Find("MaouSamaTD/SpriteGlowOutline");
                    if (shader != null) _localMaterial = new Material(shader);
                }
                
                _targetImage.material = _localMaterial;
            }

            if (_localMaterial != null)
            {
                _localMaterial.SetColor("_GlowColor", color);
                _localMaterial.SetFloat("_GlowIntensity", intensity);
                _localMaterial.SetFloat("_SelectionLevel", 1.0f); // Activate the shader logic
                _localMaterial.SetFloat("_OutlineEnabled", 1.0f);
                _localMaterial.SetFloat("_PulseSpeed", 3.0f); // Animation effect
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
