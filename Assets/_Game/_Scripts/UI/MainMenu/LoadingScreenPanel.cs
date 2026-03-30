using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Core;
using MaouSamaTD.Managers;
using Zenject;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using DG.Tweening;

namespace MaouSamaTD.UI.MainMenu
{
    public class LoadingScreenPanel : MonoBehaviour
    {
        [Header("References")]
        [Header("References")]
        [SerializeField] private AppEntryPoint _appEntryPoint;
        
        [Header("Background Splash")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private float _splashChangeInterval = 4.0f;
        [SerializeField] private float _fadeDuration = 1.0f;
        
        [Header("UI Elements")]
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TextMeshProUGUI _loreText;
        [SerializeField] private TextMeshProUGUI _versionText;
        [SerializeField] private Button _clearCacheButton;
        [SerializeField] private Button _startButton;
        [SerializeField] private GameObject _visualRoot;

        [Header("Cache Confirmation")]
        [SerializeField] private GameObject _confirmWindowRoot;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;

        [Header("Settings")]
        [TextArea] 
        [SerializeField] private string[] _loreLines = new string[] 
        {
            "The Great War scattered the Thirteen across the abyss...",
            "Starmetal can only be forged in the core of a dying world.",
            "Only an Overlord can command the allegiance of a Cohort.",
            "Vassals are loyal, but loyalty alone does not win wars.",
            "Beware the smog-filled eyes of the capital's denizens."
        };
        [SerializeField] private float _loreChangeInterval = 3.0f;

        [Inject] private SaveManager _saveManager;

        private float _loreTimer;
        private int _currentLoreIndex;

        private IList<Sprite> _splashScreens;
        private int _currentSplashIndex = -1;
        private bool _isTransitioning = false;
        private bool _isLevelReady = false;

        public void NotifyLevelReady()
        {
            Debug.Log("[LoadingScreenPanel] Level Ready signal received.");
            _isLevelReady = true;
        }

        private void Start()
        {
            if (_clearCacheButton != null) 
            {
                _clearCacheButton.onClick.AddListener(OnClearCacheClicked);
                _clearCacheButton.gameObject.SetActive(_appEntryPoint != null);
            }
            if (_startButton != null)
            {
                _startButton.onClick.AddListener(OnStartClicked);
                _startButton.gameObject.SetActive(false);
            }

            if (_confirmYesButton != null) _confirmYesButton.onClick.AddListener(ExecuteClearCache);
            if (_confirmNoButton != null) _confirmNoButton.onClick.AddListener(() => { if (_confirmWindowRoot != null) _confirmWindowRoot.SetActive(false); });
            if (_confirmWindowRoot != null) _confirmWindowRoot.SetActive(false);

            if (_progressBar != null) _progressBar.value = 0f;
            if (_versionText != null) _versionText.text = $"Ver: {Application.version}";

            if (_loreLines != null && _loreLines.Length > 0)
            {
                _currentLoreIndex = Random.Range(0, _loreLines.Length);
                if (_loreText != null) _loreText.text = _loreLines[_currentLoreIndex];
            }

            // Start Boot Sequence
            if (!_isTransitioning)
            {
                if (_appEntryPoint != null)
                {
                    _appEntryPoint.StartBootSequence(UpdateProgress, OnLoadComplete);
                }
                else
                {
                    Debug.LogError("[LoadingScreenPanel] AppEntryPoint reference is missing!");
                }
            }

            if (_backgroundImage != null)
            {
                LoadSplashScreens();
            }
        }

        private void LoadSplashScreens()
        {
            Addressables.LoadAssetsAsync<Sprite>("SplashScreen", null).Completed += handle =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    _splashScreens = handle.Result;
                    if (_splashScreens != null && _splashScreens.Count > 0)
                    {
                        // Set the first one immediately without fading
                        _currentSplashIndex = Random.Range(0, _splashScreens.Count);
                        _backgroundImage.sprite = _splashScreens[_currentSplashIndex];
                        _backgroundImage.color = Color.white;
                        
                        DOVirtual.DelayedCall(_splashChangeInterval, CycleSplashScreen).SetId(this);
                    }
                }
            };
        }

        private void CycleSplashScreen()
        {
            if (gameObject == null || !gameObject.activeSelf || _splashScreens == null || _splashScreens.Count == 0 || _backgroundImage == null) return;

            _currentSplashIndex = (_currentSplashIndex + 1) % _splashScreens.Count;
            Sprite nextSprite = _splashScreens[_currentSplashIndex];

            // Darken and switch
            _backgroundImage.DOColor(Color.black, _fadeDuration / 2f).OnComplete(() =>
            {
                if (_backgroundImage == null) return;
                _backgroundImage.sprite = nextSprite;
                _backgroundImage.DOColor(Color.white, _fadeDuration / 2f).OnComplete(() =>
                {
                    DOVirtual.DelayedCall(_splashChangeInterval, CycleSplashScreen).SetId(this);
                });
            }).SetId(this);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        private void Update()
        {
            if (_loreLines == null || _loreLines.Length == 0 || _loreText == null) return;
            
            _loreTimer += Time.deltaTime;
            if (_loreTimer >= _loreChangeInterval)
            {
                _loreTimer = 0f;
                _currentLoreIndex = (_currentLoreIndex + 1) % _loreLines.Length;
                _loreText.text = _loreLines[_currentLoreIndex];
            }
        }

        private void UpdateProgress(float progress)
        {
            if (_progressBar != null)
            {
                _progressBar.value = progress;
            }
        }

        private void OnLoadComplete()
        {
            if (_progressBar != null) _progressBar.gameObject.SetActive(false);
            if (_startButton != null) _startButton.gameObject.SetActive(true);
        }

        private void OnStartClicked()
        {
            // Proceed
            if (_visualRoot != null) _visualRoot.SetActive(false);
            else gameObject.SetActive(false);

            if (_appEntryPoint != null)
            {
                _appEntryPoint.ProceedToGame();
            }
        }

        private void OnClearCacheClicked()
        {
            if (_confirmWindowRoot != null)
            {
                _confirmWindowRoot.SetActive(true);
            }
            else
            {
                ExecuteClearCache();
            }
        }

        private void ExecuteClearCache()
        {
            Debug.Log("[LoadingScreenPanel] Clearing Cache...");
            
            // 1. Delete Save Data
            if (_saveManager != null)
            {
                _saveManager.DeleteSaveData();
            }
            
            // 2. Clear Addressables Cache
            Caching.ClearCache();
            
            // 3. Clear PlayerPrefs
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            Debug.Log("[LoadingScreenPanel] Cache cleared. Restarting Scene...");
            // Reload the active scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadSceneTransition(string sceneName)
        {
            _isTransitioning = true;
            
            // Unparent and persist
            transform.SetParent(null);
            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
                
                UnityEngine.UI.CanvasScaler scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            else
            {
                canvas.sortingOrder = 999;
            }
            DontDestroyOnLoad(gameObject);

            gameObject.SetActive(true);
            if (_visualRoot != null) _visualRoot.SetActive(true);
            if (_confirmWindowRoot != null) _confirmWindowRoot.SetActive(false);
            if (_clearCacheButton != null) _clearCacheButton.gameObject.SetActive(false);
            if (_startButton != null) _startButton.gameObject.SetActive(false);
            if (_progressBar != null)
            {
                _progressBar.gameObject.SetActive(true);
                _progressBar.value = 0f;
            }

            StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
        }

        private System.Collections.IEnumerator LoadSceneAsyncCoroutine(string sceneName)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                UpdateProgress(op.progress);
                yield return null;
            }

            UpdateProgress(0.95f);
            
            yield return new WaitForEndOfFrame();
            // Shader.WarmupAllShaders(); // Removed due to URP "incompatible keyword space" errors
            yield return null;

            UpdateProgress(1.0f);
            
            op.allowSceneActivation = true;

            // Wait until scene is loaded
            while (!op.isDone)
            {
                yield return null;
            }

            Debug.Log("[LoadingScreenPanel] Scene loaded. Waiting for level ready signal...");
            
            // Wait for manual ready signal (with a safety timeout of 5 seconds)
            float timeout = 5f;
            while (!_isLevelReady && timeout > 0)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_isLevelReady) Debug.LogWarning("[LoadingScreenPanel] Level ready signal timed out! Hiding anyway.");

            CanvasGroup cg = gameObject.GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

            cg.DOFade(0f, 0.5f).SetId(this).OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }
    }
}
