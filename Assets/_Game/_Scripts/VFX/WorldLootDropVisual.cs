using UnityEngine;
using DG.Tweening;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using MaouSamaTD.Data;

namespace MaouSamaTD.VFX
{
    public class WorldLootDropVisual : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private string _itemID;

        [Header("Animation Settings")]
        [Tooltip("The final scale the loot drop will expand to.")]
        [SerializeField] private Vector3 _targetScale = new Vector3(0.5f, 0.5f, 0.5f);

        private float _zRot;
        private Camera _mainCam;

        public void Initialize(string itemID, Vector3 startPosition)
        {
            _itemID = itemID;
            // Float the loot visual above the ground/tiles to prevent clipping
            transform.position = startPosition + Vector3.up * 0.6f;

            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            _spriteRenderer.sortingLayerName = "UI";
            _spriteRenderer.sortingOrder = 100;
            _spriteRenderer.color = Color.white;

            _mainCam = Camera.main;

            // Give it a random slight angle, store it for billboarding
            _zRot = Random.Range(-15f, 15f);
            
            // Small scale initially
            transform.localScale = Vector3.zero;

            LoadSpriteAndAnimate();
        }

        private void LateUpdate()
        {
            if (_mainCam != null)
            {
                // Billboard to face the camera, but preserve our random Z tilt
                transform.rotation = _mainCam.transform.rotation * Quaternion.Euler(0, 0, _zRot);
            }
        }

        private void LoadSpriteAndAnimate()
        {
            var locOp = Addressables.LoadResourceLocationsAsync(_itemID);
            locOp.Completed += (locHandle) =>
            {
                if (locHandle.Status == AsyncOperationStatus.Succeeded && locHandle.Result.Count > 0)
                {
                    Addressables.LoadAssetAsync<ItemConfigSO>(_itemID).Completed += (op) =>
                    {
                        if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null && op.Result.ItemIcon != null)
                        {
                            if (this == null || _spriteRenderer == null) return;

                            _spriteRenderer.sprite = op.Result.ItemIcon;
                            
                            // Make it pop up and scale
                            float jumpPower = Random.Range(1.5f, 2.5f);
                            float duration = 0.5f;
                            // Jump on the horizontal XZ plane instead of vertical XY plane to prevent clipping underground
                            Vector3 targetPos = transform.position + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                            
                            Sequence seq = DOTween.Sequence();
                            seq.SetUpdate(true); // Run in unscaled time
                            seq.Join(transform.DOScale(_targetScale, duration).SetEase(Ease.OutBack));
                            seq.Join(transform.DOJump(targetPos, jumpPower, 1, duration));
                            
                            // Hover in place
                            seq.AppendInterval(1.0f);
                            
                            // Blink rapidly
                            seq.Append(_spriteRenderer.DOFade(0f, 0.1f).SetLoops(6, LoopType.Yoyo));
                            
                            // Shrink and vanish
                            seq.Append(transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
                            
                            seq.OnComplete(() =>
                            {
                                if (this != null && gameObject != null)
                                    Destroy(gameObject);
                            });
                        }
                        else
                        {
                            // If failed to load, just destroy
                            if (this != null && gameObject != null)
                                Destroy(gameObject);
                        }
                    };
                }
                else
                {
                    // Key doesn't exist, destroy to prevent blocking
                    if (this != null && gameObject != null)
                        Destroy(gameObject);
                }
            };
        }
    }
}
