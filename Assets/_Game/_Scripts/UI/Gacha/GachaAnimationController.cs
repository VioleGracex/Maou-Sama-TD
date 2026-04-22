using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using MaouSamaTD.Data;
using MaouSamaTD.Units;
using Zenject;
using TMPro;

namespace MaouSamaTD.UI.Gacha
{
    public class GachaAnimationController : MonoBehaviour
    {
        [Header("UI Containers")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Image _ritualBackground;
        [SerializeField] private GachaResultPanel _resultPanel;
        
        [Header("Ritual Visuals")]
        [SerializeField] private GameObject _magicCircle;
        [SerializeField] private GameObject _pillarMega; // Large pillar for 1x pull
        [SerializeField] private GameObject _pillarRing; // Ring of pillars for 10x pull
        [SerializeField] private List<GachaPillar> _ringPillars;
        
        [Header("Reveal View")]
        [SerializeField] private GameObject _revealRoot;
        [SerializeField] private Image _characterFullBody;
        [SerializeField] private TextMeshProUGUI _charNameTxt;
        [SerializeField] private TextMeshProUGUI _charTitleTxt;
        [SerializeField] private TextMeshProUGUI _charQuoteTxt;
        [SerializeField] private Image _dialogueBG;
        [SerializeField] private GameObject _skipBtn;
        [SerializeField] private GameObject _skipAllBtn;
        
        [Inject] private UnitDatabase _unitDatabase;
        
        private List<UnitInventoryEntry> _pendingResults;
        private int _currentIndex;
        private bool _isSkippingAll;
        private bool _isSkippingCurrent;

        public void PlayRitual(List<UnitInventoryEntry> results)
        {
            _pendingResults = results;
            _currentIndex = 0;
            _isSkippingAll = false;
            _isSkippingCurrent = false;

            if (_visualRoot != null) _visualRoot.SetActive(true);
            if (_revealRoot != null) _revealRoot.SetActive(false);
            
            // Toggle pillar mode
            bool isMulti = results.Count > 1;
            if (_pillarMega != null) _pillarMega.SetActive(!isMulti);
            if (_pillarRing != null) _pillarRing.SetActive(isMulti);

            StartCoroutine(MainFlowSequence());
        }

        private IEnumerator MainFlowSequence()
        {
            // 1. Ritual Phase
            yield return StartCoroutine(RitualPhase());
            if (_isSkippingAll) { ShowResults(); yield break; }

            // 2. Reveal Phase
            while (_currentIndex < _pendingResults.Count)
            {
                _isSkippingCurrent = false;
                yield return StartCoroutine(RevealSingleUnit(_pendingResults[_currentIndex]));
                
                if (_isSkippingAll) break;
                _currentIndex++;
            }

            // 3. Results Phase
            ShowResults();
        }

        private IEnumerator RitualPhase()
        {
            // Animate magic circle start
            if (_magicCircle != null) _magicCircle.GetComponent<Animator>()?.SetTrigger("Start");
            
            // Anticipation wait
            yield return new WaitForSeconds(2.0f);
        }

        private IEnumerator RevealSingleUnit(UnitInventoryEntry result)
        {
            if (_revealRoot != null) _revealRoot.SetActive(true);
            
            var unitData = _unitDatabase.GetUnitByID(result.UnitID);
            if (unitData != null)
            {
                if (_charNameTxt != null) _charNameTxt.text = unitData.UnitName;
                if (_charTitleTxt != null) _charTitleTxt.text = unitData.UnitTitle ?? "";
                if (_charQuoteTxt != null) _charQuoteTxt.text = unitData.BriefDescription ?? "A new ally joins the cause!";
                
                // Load full body art
                if (_characterFullBody != null) _characterFullBody.sprite = unitData.GetSprite(MaouSamaTD.Units.UnitData.UnitImageType.FullSprite);
            }

            // Animate Reveal (Fade in)
            var canvasGroup = _revealRoot.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                float elapsed = 0;
                while (elapsed < 0.5f && !_isSkippingCurrent && !_isSkippingAll)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = elapsed / 0.5f;
                    yield return null;
                }
                canvasGroup.alpha = 1;
            }

            // Wait for user or timer
            float timer = 0;
            while (timer < 3.0f && !_isSkippingCurrent && !_isSkippingAll)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (_revealRoot != null) _revealRoot.SetActive(false);
        }

        public void SkipOne()
        {
            _isSkippingCurrent = true;
        }

        public void SkipAll()
        {
            _isSkippingAll = true;
        }

        private void ShowResults()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            if (_revealRoot != null) _revealRoot.SetActive(false);
            
            if (_resultPanel != null)
            {
                _resultPanel.DisplayResults(_pendingResults);
            }
        }
    }

    [System.Serializable]
    public class GachaPillar
    {
        public GameObject GameObject;
        public Animator Animator;
        public Image AuraGlow;
        
        public void Show(UnitInventoryEntry result, UnitRarity rarity)
        {
            if (GameObject != null) GameObject.SetActive(true);
            if (Animator != null) 
            {
                Animator.SetInteger("Rarity", (int)rarity);
                Animator.SetTrigger("Rise");
            }
            
            if (AuraGlow != null)
            {
                AuraGlow.color = GetRarityColor(rarity);
            }
        }

        private Color GetRarityColor(UnitRarity rarity)
        {
            return rarity switch
            {
                UnitRarity.Legendary => new Color(1f, 0.84f, 0f, 0.8f),
                UnitRarity.Master => new Color(0.6f, 0.2f, 1f, 0.8f),
                UnitRarity.Elite => new Color(0.2f, 0.6f, 1f, 0.8f),
                _ => new Color(1f, 1f, 1f, 0.4f)
            };
        }
    }
}
