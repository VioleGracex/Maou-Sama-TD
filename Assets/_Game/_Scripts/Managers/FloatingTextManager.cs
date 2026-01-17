using UnityEngine;

namespace MaouSamaTD.Managers
{
    public class FloatingTextManager : MonoBehaviour
    {
        public static FloatingTextManager Instance { get; private set; }

        [SerializeField] private GameObject _textPrefab;
        
        // Colors & Tiers
        [Header("Damage Colors")]
        [SerializeField] private Color _smallDmgColor = Color.white;
        [SerializeField] private Color _mediumDmgColor = new Color(1f, 0.5f, 0f); // Orange
        [SerializeField] private Color _bigDmgColor = Color.red;
        [SerializeField] private Color _critDmgColor = new Color(0.5f, 0f, 0f); // Dark Red
        [SerializeField] private Color _healColor = Color.green;

        [Header("Thresholds")]
        [SerializeField] private float _mediumThreshold = 20f;
        [SerializeField] private float _bigThreshold = 50f;
        
        [Header("Spawn Settings")]
        [SerializeField] private float _positionRandomness = 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        public void ShowDamage(Vector3 position, float amount, bool isCrit)
        {
            if (_textPrefab == null) return;

            // Spawn slightly above with random offset
            Vector3 randomOffset = Random.insideUnitSphere * _positionRandomness;
            randomOffset.z = 0; // Keep flat if 2D, but for 3D billboard it's fine. 
                                // Ideally we want separation on X/Y mainly.
            
            Vector3 spawnPos = position + Vector3.up * 1.5f + randomOffset;
            
            GameObject obj = Instantiate(_textPrefab, spawnPos, Quaternion.identity);
            MaouSamaTD.UI.FloatingText textScript = obj.GetComponent<MaouSamaTD.UI.FloatingText>();
            
            if (textScript != null)
            {
                Color c = GetDamageColor(amount, isCrit);
                textScript.Init(amount, isCrit, c);
            }
        }

        public void ShowHeal(Vector3 position, float amount)
        {
            if (_textPrefab == null) return;
            
            Vector3 randomOffset = Random.insideUnitSphere * _positionRandomness;
            randomOffset.z = 0;

            Vector3 spawnPos = position + Vector3.up * 1.5f + randomOffset;
            GameObject obj = Instantiate(_textPrefab, spawnPos, Quaternion.identity);
            var textScript = obj.GetComponent<MaouSamaTD.UI.FloatingText>();
            if (textScript != null)
            {
                textScript.Init(amount, false, _healColor);
            }
        }

        private Color GetDamageColor(float amount, bool isCrit)
        {
            if (isCrit) return _critDmgColor;
            if (amount >= _bigThreshold) return _bigDmgColor;
            if (amount >= _mediumThreshold) return _mediumDmgColor;
            return _smallDmgColor;
        }
    }
}
