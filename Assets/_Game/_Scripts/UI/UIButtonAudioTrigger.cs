using UnityEngine;
using UnityEngine.EventSystems;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Replicates the premium HTML audio synthesis by playing custom sound clips 
    /// on hover (pointer enter) and click events.
    /// Attach this to any Button, Sidebar Button, or Icon in your Unity UGUI Canvas!
    /// </summary>
    [RequireComponent(typeof(EventTrigger))]
    public class UIButtonAudioTrigger : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [Header("Audio Settings")]
        [Tooltip("The AudioSource used to play SFX. If unassigned, it will attempt to find a global AudioSource or generate a local one.")]
        [SerializeField] private AudioSource _audioSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _hoverClip;
        [SerializeField] private AudioClip _clickClip;

        [Header("Tuning Settings")]
        [Range(0f, 1f)] [SerializeField] private float _hoverVolume = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _clickVolume = 0.7f;
        [SerializeField] private bool _pitchRandomization = true;
        [Range(0.8f, 1.2f)] [SerializeField] private float _minPitch = 0.95f;
        [Range(0.8f, 1.2f)] [SerializeField] private float _maxPitch = 1.05f;

        private void Awake()
        {
            // Auto-locate or generate AudioSource
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    // Generate a local non-spatial AudioSource so it plays perfectly on UI
                    _audioSource = gameObject.AddComponent<AudioSource>();
                    _audioSource.playOnAwake = false;
                    _audioSource.spatialBlend = 0f; // 2D Sound for UI
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlaySound(_hoverClip, _hoverVolume);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            PlaySound(_clickClip, _clickVolume);
        }

        private void PlaySound(AudioClip clip, float volume)
        {
            if (clip == null || _audioSource == null || !gameObject.activeInHierarchy) return;

            // Optional slight pitch shifting to give sounds a rich, organic feel
            if (_pitchRandomization)
            {
                _audioSource.pitch = Random.Range(_minPitch, _maxPitch);
            }
            else
            {
                _audioSource.pitch = 1f;
            }

            // Play the SFX without cutting off existing ones
            _audioSource.PlayOneShot(clip, volume);
        }
    }
}
