using UnityEngine;
using UnityEngine.UI;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Premium UI component that implements smooth breathing/bobbing idle animations,
    /// a dynamic glowing aura silhouette, and interactive mouse parallax offsets.
    /// Attach this directly to Maou-Sama's Image component in the Home scene Canvas!
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UICharacterEffects : MonoBehaviour
    {
        [Header("Breathing Idle Animation")]
        [SerializeField] private bool _enableBreathing = true;
        [SerializeField] private float _breathingSpeed = 1.2f;
        [SerializeField] private float _bobbingAmplitude = 12f; // Height of bobbing in pixels
        [SerializeField] private float _scalePulseAmplitude = 0.008f; // Subtle scale swell

        [Header("Glowing Aura Silhouette")]
        [SerializeField] private bool _enableGlow = true;
        [SerializeField] private Color _glowColor = new Color(1f, 0.75f, 0.15f, 0.35f); // Beautiful Amber Gold
        [SerializeField] private float _glowScaleMultiplier = 1.025f; // Slightly larger for outline effect
        [SerializeField] private float _glowPulseSpeed = 1.2f;
        [Range(0f, 1f)] [SerializeField] private float _minGlowAlpha = 0.15f;
        [Range(0f, 1f)] [SerializeField] private float _maxGlowAlpha = 0.55f;

        [Header("Parallax Mouse Offset")]
        [SerializeField] private bool _enableParallax = true;
        [SerializeField] private float _parallaxIntensity = 15f; // Max pixel shift on mouse movement
        [SerializeField] private float _parallaxDamping = 8f;     // Smooth lerp speed

        // Cache references
        private Image _characterImage;
        private RectTransform _rectTransform;
        
        private Image _glowImage;
        private RectTransform _glowRect;
        
        // State variables
        private Vector2 _basePosition;
        private Vector3 _baseScale;
        private float _breathingTime = 0f;
        private float _glowTime = 0f;
        private Vector2 _targetParallaxOffset;
        private Vector2 _currentParallaxOffset;

        private void Awake()
        {
            _characterImage = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
            
            _basePosition = _rectTransform.anchoredPosition;
            _baseScale = _rectTransform.localScale;
        }

        private void Start()
        {
            if (_enableGlow)
            {
                CreateGlowAura();
            }
        }

        private void OnEnable()
        {
            // Reset transforms on enable to avoid snapping artifacts
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = _basePosition;
                _rectTransform.localScale = _baseScale;
            }
            _currentParallaxOffset = Vector2.zero;
            _targetParallaxOffset = Vector2.zero;
        }

        private void Update()
        {
            // 1. Smooth Idle Breathing & Bobbing
            if (_enableBreathing && _rectTransform != null)
            {
                _breathingTime += Time.deltaTime * _breathingSpeed;
                
                // Calculate sine waves
                float sinVal = Mathf.Sin(_breathingTime);
                float yOffset = sinVal * _bobbingAmplitude;
                float scaleOffset = (sinVal + 1f) * 0.5f * _scalePulseAmplitude; // Swells from 0 to pulse amplitude
                
                // Apply bobbing & scale
                _rectTransform.localScale = _baseScale + new Vector3(scaleOffset, scaleOffset, 0f);
                _rectTransform.anchoredPosition = _basePosition + new Vector2(0f, yOffset) + _currentParallaxOffset;
            }

            // 2. Parallax Mouse Offset
            if (_enableParallax)
            {
                CalculateMouseParallax();
            }

            // 3. Glowing Aura Pulsing & Silhouette Matching
            if (_enableGlow && _glowImage != null && _glowRect != null)
            {
                // Synchronize Sprite and Position to match parent perfectly
                if (_glowImage.sprite != _characterImage.sprite)
                {
                    _glowImage.sprite = _characterImage.sprite;
                    _glowImage.SetNativeSize();
                }

                _glowRect.anchoredPosition = Vector2.zero;
                _glowRect.localScale = Vector3.one * _glowScaleMultiplier;

                // Pulsate the alpha of the glow silhouette
                _glowTime += Time.deltaTime * _glowPulseSpeed;
                float t = (Mathf.Sin(_glowTime) + 1f) * 0.5f; // Normalizes to 0-1
                float targetAlpha = Mathf.Lerp(_minGlowAlpha, _maxGlowAlpha, t);
                
                Color currentGlow = _glowColor;
                currentGlow.a *= targetAlpha; // Compound transparency
                _glowImage.color = currentGlow;
            }
        }

        private void CreateGlowAura()
        {
            // 1. Spawn Glow GameObject as a child behind the parent image
            GameObject glowGo = new GameObject("CharacterEffects_GlowSilhouette", typeof(RectTransform), typeof(Image));
            glowGo.transform.SetParent(transform, false);
            glowGo.transform.SetAsFirstSibling(); // Renders behind the parent image

            _glowRect = glowGo.GetComponent<RectTransform>();
            _glowImage = glowGo.GetComponent<Image>();

            // 2. Match RectTransform anchors and pivots to stretch with parent
            _glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            _glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            _glowRect.pivot = _rectTransform.pivot;
            _glowRect.sizeDelta = _rectTransform.sizeDelta;

            // 3. Configure matching sprite and solid glow color
            _glowImage.sprite = _characterImage.sprite;
            _glowImage.material = null; // Standard canvas UI rendering
            _glowImage.type = _characterImage.type;
            _glowImage.preserveAspect = _characterImage.preserveAspect;
            _glowImage.raycastTarget = false; // Never block raycasts or button clicks

            // Sync size
            _glowImage.SetNativeSize();
            _glowRect.localScale = Vector3.one * _glowScaleMultiplier;
            
            Color initialColor = _glowColor;
            initialColor.a *= _minGlowAlpha;
            _glowImage.color = initialColor;
        }

        private void CalculateMouseParallax()
        {
            // Get mouse position normalized from -1.0 to 1.0 relative to screen center
            float normX = (Input.mousePosition.x / Screen.width) * 2f - 1f;
            float normY = (Input.mousePosition.y / Screen.height) * 2f - 1f;

            // Clamp normalized inputs just in case mouse goes off window bounds
            normX = Mathf.Clamp(normX, -1f, 1f);
            normY = Mathf.Clamp(normY, -1f, 1f);

            // Set target offset based on mouse position (inverted to create spatial depth)
            _targetParallaxOffset = new Vector2(-normX * _parallaxIntensity, -normY * _parallaxIntensity);

            // Smoothly damp the active offset
            _currentParallaxOffset = Vector2.Lerp(_currentParallaxOffset, _targetParallaxOffset, Time.deltaTime * _parallaxDamping);

            // Apply base position shift if breathing is disabled, otherwise breathing incorporates it
            if (!_enableBreathing && _rectTransform != null)
            {
                _rectTransform.anchoredPosition = _basePosition + _currentParallaxOffset;
            }
        }
    }
}
