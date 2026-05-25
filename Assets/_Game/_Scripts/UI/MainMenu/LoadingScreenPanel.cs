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
        public static LoadingScreenPanel Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Rehook the scene-specific AppEntryPoint reference to the persistent instance
                Instance._appEntryPoint = this._appEntryPoint;
                Instance.ResetAndBoot();
                Destroy(gameObject); // Destroy the duplicate GameObject completely so it doesn't block the screen
                return;
            }
            Instance = this;
            
            // Reset static state on a fresh boot/awake so play mode starts correctly even with domain reload disabled
            _hasFinishedFirstBoot = false;
            
            // Fix: If this is a child GameObject, unparent it so that DontDestroyOnLoad succeeds!
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            
            // Ensure it has its own Canvas so it renders correctly after being unparented from Overlay_Canvas
            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            canvas.sortingOrder = 30000; // Increased to ensure it's above Dialogue (typically 1000+)
            
            if (gameObject.GetComponent<UnityEngine.UI.CanvasScaler>() == null)
            {
                UnityEngine.UI.CanvasScaler scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }
            
            if (gameObject.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            DontDestroyOnLoad(gameObject);
        }

        public void ResetAndBoot()
        {
            Debug.Log("[LoadingScreenPanel] Resetting and starting Boot Sequence on reload.");
            _isTransitioning = false;
            _isLevelReady = false;
            _hasFinishedFirstBoot = false;

            CanvasGroup cg = gameObject.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
            
            // Show visual root and reset components
            if (_visualRoot != null) _visualRoot.SetActive(true);
            gameObject.SetActive(true);
            
            if (_progressBar != null)
            {
                _progressBar.gameObject.SetActive(true);
                _progressBar.value = 0f;
            }
            if (_startButton != null)
            {
                _startButton.gameObject.SetActive(false);
            }
            if (_confirmWindowRoot != null)
            {
                _confirmWindowRoot.SetActive(false);
            }
            if (_clearCacheButton != null)
            {
                _clearCacheButton.gameObject.SetActive(_appEntryPoint != null);
            }

            // Start Boot Sequence
            if (_appEntryPoint == null)
            {
                _appEntryPoint = Object.FindAnyObjectByType<AppEntryPoint>(FindObjectsInactive.Include);
            }

            if (_appEntryPoint != null)
            {
                _appEntryPoint.StartBootSequence(UpdateProgress, OnLoadComplete);
            }
            else
            {
                Debug.LogError("[LoadingScreenPanel] AppEntryPoint reference is missing in ResetAndBoot!");
            }
        }

        public GameObject VisualRoot => _visualRoot;

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
        [SerializeField] private Button _settingsButton;
        [SerializeField] private SettingsPanel _settingsPanel;

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

        private UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<IList<Sprite>> _splashHandle;
        private IList<Sprite> _splashScreens;
        private int _currentSplashIndex = -1;
        private static bool _hasFinishedFirstBoot = false;
        private bool _isTransitioning = false;
        private bool _isLevelReady = false;

        public static void ResetFirstBootState()
        {
            _hasFinishedFirstBoot = false;
            Debug.Log("[LoadingScreenPanel] Static first boot state reset.");
        }

        public void NotifyLevelReady()
        {
            Debug.Log("[LoadingScreenPanel] Level Ready signal received.");
            _isLevelReady = true;
        }

        private void Start()
        {
            if (Instance != null && Instance != this)
            {
                return;
            }

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
            
            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (_confirmYesButton != null) _confirmYesButton.onClick.AddListener(ExecuteClearCache);
            if (_confirmNoButton != null) _confirmNoButton.onClick.AddListener(() => { if (_confirmWindowRoot != null) _confirmWindowRoot.SetActive(false); });
            if (_confirmWindowRoot != null) _confirmWindowRoot.SetActive(false);

            if (_progressBar != null)
            {
                _progressBar.interactable = false;
                _progressBar.value = 0f;
            }
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
            _splashHandle = Addressables.LoadAssetsAsync<Sprite>((object)"SplashScreen", null);
            _splashHandle.Completed += handle =>
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
                        
                        DOVirtual.DelayedCall(_splashChangeInterval, CycleSplashScreen).SetId("SplashCycle");
                    }
                }
            };
        }

        private void CycleSplashScreen()
        {
            if (gameObject == null || !gameObject.activeSelf || _splashScreens == null || _splashScreens.Count == 0 || _backgroundImage == null) return;

            // Skip nulls if any got destroyed
            int attempts = 0;
            do
            {
                _currentSplashIndex = (_currentSplashIndex + 1) % _splashScreens.Count;
                attempts++;
            }
            while (_splashScreens[_currentSplashIndex] == null && attempts < _splashScreens.Count);

            Sprite nextSprite = _splashScreens[_currentSplashIndex];
            if (nextSprite == null) return;

            // Darken and switch
            _backgroundImage.DOColor(Color.black, _fadeDuration / 2f).OnComplete(() =>
            {
                if (_backgroundImage == null) return;
                _backgroundImage.sprite = nextSprite;
                _backgroundImage.DOColor(Color.white, _fadeDuration / 2f).OnComplete(() =>
                {
                    DOVirtual.DelayedCall(_splashChangeInterval, CycleSplashScreen).SetId("SplashCycle");
                });
            }).SetId("SplashCycle");
        }

        private void OnDestroy()
        {
            if (_splashHandle.IsValid())
            {
                Addressables.Release(_splashHandle);
            }
            DOTween.Kill(this);
            DOTween.Kill("SplashCycle");
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

        private bool _startButtonClicked = false;

        private void OnLoadComplete()
        {
            if (_progressBar != null) _progressBar.gameObject.SetActive(false);

            // If this is NOT the first boot (e.g. returning from a level), auto-proceed
            if (_hasFinishedFirstBoot)
            {
                Debug.Log("[LoadingScreenPanel] Returning to menu - auto-proceeding.");
                OnStartClicked();
                return;
            }

            // Wait for start game button click during initial boot
            if (_startButton != null)
            {
                _startButton.gameObject.SetActive(true);
                _startButton.interactable = true;
            }
        }

        private void OnStartClicked()
        {
            _hasFinishedFirstBoot = true;

            if (_isTransitioning)
            {
                _startButtonClicked = true;
                if (_startButton != null) _startButton.gameObject.SetActive(false);
            }
            else
            {
                // Proceed from initial boot
                if (_visualRoot != null) _visualRoot.SetActive(false);
                else gameObject.SetActive(false);

                if (_appEntryPoint != null)
                {
                    _appEntryPoint.ProceedToGame();
                }
            }
        }

        private void OnSettingsClicked()
        {
            if (_settingsPanel == null)
            {
                _settingsPanel = SettingsPanel.Instance;
            }

            if (_settingsPanel != null)
            {
                _settingsPanel.Initialize();
                _settingsPanel.Open();
            }
            else
            {
                Debug.LogWarning("[LoadingScreenPanel] SettingsPanel instance not found!");
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

        public void ExecuteClearCache()
        {
            Debug.Log("[LoadingScreenPanel] Clearing Cache...");
            
            // 1. Delete Save Data
            if (_saveManager != null)
            {
                _saveManager.DeleteSaveData();
            }
            
            // 2. Clear System Data
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Caching.ClearCache();

            _hasFinishedFirstBoot = false;
            
            // Clear static entrypoint databases so Addressables reload on boot
            MaouSamaTD.Core.AppEntryPoint.ResetStaticData();

            // 3. Restart
            if (_confirmWindowRoot != null) _confirmWindowRoot.SetActive(false);
            
            // Unload all dynamic assets and force garbage collection
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }

        public void LoadSceneTransition(string sceneName)
        {
            _isTransitioning = true;
            _isLevelReady = false;
            
            // Unparent and persist
            transform.SetParent(null);
            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 30000;
                
                UnityEngine.UI.CanvasScaler scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            else
            {
                canvas.sortingOrder = 30000;
            }
            
            CanvasGroup cg = gameObject.GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            DontDestroyOnLoad(gameObject);

            gameObject.SetActive(true);
            if (_visualRoot != null) _visualRoot.SetActive(true);
            if (_confirmWindowRoot != null) _confirmWindowRoot.SetActive(false);
            if (_clearCacheButton != null) _clearCacheButton.gameObject.SetActive(false);
            if (_startButton != null) _startButton.gameObject.SetActive(false);
            if (_progressBar != null)
            {
                _progressBar.interactable = false;
                _progressBar.gameObject.SetActive(true);
                _progressBar.value = 0f;
            }

            if (_splashScreens != null && _splashScreens.Count > 0)
            {
                DOTween.Kill("SplashCycle");
                DOVirtual.DelayedCall(_splashChangeInterval, CycleSplashScreen).SetId("SplashCycle");
            }

            StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
        }

        private System.Collections.IEnumerator LoadSceneAsyncCoroutine(string sceneName)
        {
            float startTime = Time.realtimeSinceStartup;
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

            // Always deactivate the progress bar when loaded
            if (_progressBar != null)
            {
                _progressBar.gameObject.SetActive(false);
            }
            Debug.Log($"[LoadingScreenPanel] Scene {sceneName} ready - auto-proceeding to fade out.");

            float elapsed = Time.realtimeSinceStartup - startTime;
            if (elapsed < 2.0f)
            {
                yield return new WaitForSecondsRealtime(2.0f - elapsed);
            }

            if (sceneName == "Home_New")
            {
                float lobbyTimeout = 5f;
                bool pageIsOn = false;
                while (!pageIsOn && lobbyTimeout > 0)
                {
                    lobbyTimeout -= Time.unscaledDeltaTime;
                    var homeUI = UnityEngine.Object.FindAnyObjectByType<MaouSamaTD.UI.MainMenu.HomeUIManager>(FindObjectsInactive.Include);
                    var gachaUI = UnityEngine.Object.FindAnyObjectByType<MaouSamaTD.UI.Gacha.GachaPanel>(FindObjectsInactive.Include);
                    bool homeOn = homeUI != null && homeUI.VisualRoot != null && homeUI.VisualRoot.activeInHierarchy;
                    bool gachaOn = gachaUI != null && gachaUI.VisualRoot != null && gachaUI.VisualRoot.activeInHierarchy;
                    if (homeOn || gachaOn)
                    {
                        pageIsOn = true;
                        Debug.Log($"[LoadingScreenPanel] Lobby UI Page confirmed on: HomeOn={homeOn}, GachaOn={gachaOn}");
                    }
                    yield return null;
                }
            }

            CanvasGroup cg = gameObject.GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

            cg.DOFade(0f, 0.5f).SetId(this).SetUpdate(true).OnComplete(() =>
            {
                if (_visualRoot != null) 
                {
                    _visualRoot.SetActive(false);
                }
                else 
                {
                    gameObject.SetActive(false);
                }
                _isTransitioning = false;
            });
        }
    }
}
