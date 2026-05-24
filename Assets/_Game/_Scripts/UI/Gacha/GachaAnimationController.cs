using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using MaouSamaTD.Data;
using MaouSamaTD.Units;
using Zenject;
using TMPro;
using DG.Tweening;
using System.Linq;

namespace MaouSamaTD.UI.Gacha
{
    public class GachaAnimationController : MonoBehaviour
    {
        [Header("UI Containers")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Image _ritualBackground;
        [SerializeField] private GachaResultPanel _resultPanel;
        
        [Header("Ritual Visuals")]
        [SerializeField] private GameObject _pillarRing; 
        [SerializeField] private List<GachaPillar> _ringPillars = new List<GachaPillar>();
        
        [Header("Reveal View")]
        [SerializeField] private GameObject _revealRoot;
        [SerializeField] private Image _characterFullBody;
        [SerializeField] private TextMeshProUGUI _txtName;
        [SerializeField] private TextMeshProUGUI _txtTitle;
        [SerializeField] private TextMeshProUGUI _txtDialogue;
        [SerializeField] private Image _dialogueBG;
        [SerializeField] private Image _bgName;
        [SerializeField] private Image _bgTitle;
        [SerializeField] private GameObject _btnSkip;
        [SerializeField] private GameObject _btnSkipAll;
        
        public enum ManifestationMode
        {
            Batch,      // All pillars then all reveals
            Sequential  // Pillar -> Reveal -> Pillar -> Reveal
        }

        [Header("Timing Settings")]
        [SerializeField] private ManifestationMode _mode = ManifestationMode.Batch;
        [SerializeField] private float _pillarFillDuration = 0.15f;
        [SerializeField] private float _delayBetweenPillars = 0.1f;
        [SerializeField] private float _delayBetweenReveals = 0.5f;
        [SerializeField] private float _revealViewDuration = 3.0f;
        
        [Header("Compensation Animation")]
        [SerializeField] private CanvasGroup _compensationPopGroup; // Central pop-up
        [SerializeField] private RectTransform _compensationPopRect;
        [SerializeField] private TMPro.TextMeshProUGUI _popGoldText;
        [SerializeField] private TMPro.TextMeshProUGUI _popCrestText;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _pillarAppearClip;

        [Header("Compensation UI")]
        [SerializeField] private GameObject _duplicateBadge; // Show if duplicate

        [Inject] private UnitDatabase _unitDatabase;
        
        private List<UnitInventoryEntry> _pendingResults;
        private int _currentIndex;
        private bool _isSkippingAll;
        private bool _isSkippingCurrent;

        private void Awake()
        {
            // Reset central pop
            if (_compensationPopGroup != null) _compensationPopGroup.alpha = 0;

            // We only deactivate the UI roots, NOT the manager object itself
            if (_visualRoot != null) _visualRoot.SetActive(false);
            if (_revealRoot != null) _revealRoot.SetActive(false);
            
            if (_revealRoot != null)
            {
                var cg = _revealRoot.GetComponent<CanvasGroup>();
                if (cg == null) _revealRoot.AddComponent<CanvasGroup>();
            }

            // Wire Skip Buttons
            if (_btnSkip != null)
            {
                var btn = _btnSkip.GetComponent<UnityEngine.UI.Button>();
                if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(SkipOne); }
            }
            if (_btnSkipAll != null)
            {
                var btn = _btnSkipAll.GetComponent<UnityEngine.UI.Button>();
                if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(SkipAll); }
            }
        }

        public void PlayRitual(List<UnitInventoryEntry> results)
        {
            // Sort results by rarity (lowest to highest)
            _pendingResults = results.OrderBy(r => _unitDatabase.GetUnitByID(r.UnitID)?.Rarity ?? UnitRarity.Common).ToList();
            
            _currentIndex = 0;
            _isSkippingAll = false;
            _isSkippingCurrent = false;

            if (_visualRoot != null) _visualRoot.SetActive(true);
            if (_revealRoot != null) _revealRoot.SetActive(false);
            
            // Always use the ring of 10
            if (_pillarRing != null) _pillarRing.SetActive(true);

            StartCoroutine(MainFlowSequence());
        }

        private IEnumerator MainFlowSequence()
        {
            // Debug Log results
            string charNames = string.Join(", ", _pendingResults.Select(r => _unitDatabase.GetUnitByID(r.UnitID)?.UnitName));
            string duplicates = string.Join(", ", _pendingResults.Where(r => r.IsDuplicate).Select(r => _unitDatabase.GetUnitByID(r.UnitID)?.UnitName));
            int totalGold = _pendingResults.Sum(r => r.CompensationGold);
            int totalCrest = _pendingResults.Sum(r => r.CompensationBloodCrest);
            
            Debug.Log($"[GachaAnimationController] gacha pull count: {_pendingResults.Count}, Characters: [{charNames}], Duplicates: [{duplicates}], Compensation: {totalGold} Gold, {totalCrest} Blood Crest");

            if (_mode == ManifestationMode.Batch || _pendingResults.Count == 1)
            {
                // Mode 1: All pillars then all characters
                yield return StartCoroutine(RitualPhase());
                if (_isSkippingAll) { ShowResults(); yield break; }

                while (_currentIndex < _pendingResults.Count)
                {
                    _isSkippingCurrent = false;
                    yield return StartCoroutine(RevealSingleUnit(_pendingResults[_currentIndex]));
                    if (_isSkippingAll) break;
                    _currentIndex++;
                }
            }
            else
            {
                // Mode 2: Sequential (Pillar -> Reveal -> Pillar -> Reveal)
                foreach (var p in _ringPillars) p.ResetForRitual();
                
                for (int i = 0; i < _pendingResults.Count; i++)
                {
                    _currentIndex = i;
                    _isSkippingCurrent = false;

                    // 1. Pillar Fill
                    var unit = _unitDatabase.GetUnitByID(_pendingResults[i].UnitID);
                    _ringPillars[i].Show(unit?.Rarity ?? UnitRarity.Common, _pillarFillDuration);
                    PlayPillarSound();
                    yield return new WaitForSeconds(_pillarFillDuration + _delayBetweenPillars);

                    if (_isSkippingAll) break;

                    // 2. Immediate Reveal
                    yield return StartCoroutine(RevealSingleUnit(_pendingResults[i]));
                    
                    if (_isSkippingAll) break;
                }
            }

            ShowResults();
        }

        private bool _ritualSkipped;

        private IEnumerator RitualPhase()
        {
            _ritualSkipped = false;
            
            // Setup Pillars for vertical fill
            foreach (var p in _ringPillars) p.ResetForRitual();

            bool isSingle = _pendingResults.Count == 1;
            
            if (isSingle)
            {
                // Simultaneous fill for single pull - FASTER
                var unit = _unitDatabase.GetUnitByID(_pendingResults[0].UnitID);
                var rarity = unit?.Rarity ?? UnitRarity.Common;
                
                foreach (var p in _ringPillars) p.Show(rarity, _pillarFillDuration);
                PlayPillarSound();
                
                // Wait for fill
                float timer = 0;
                while (timer < _pillarFillDuration && !_ritualSkipped && !_isSkippingAll)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                // Sequential fill for 10x pull
                for (int i = 0; i < _ringPillars.Count; i++)
                {
                    if (_ritualSkipped || _isSkippingAll) break;

                    if (i < _pendingResults.Count)
                    {
                        var unit = _unitDatabase.GetUnitByID(_pendingResults[i].UnitID);
                        _ringPillars[i].Show(unit?.Rarity ?? UnitRarity.Common, _pillarFillDuration);
                        PlayPillarSound();
                        yield return new WaitForSeconds(_delayBetweenPillars);
                    }
                    else
                    {
                        _ringPillars[i].Hide();
                    }
                }
                
                // Final wait if not skipped to let player see the background/pillars
                if (!_ritualSkipped && !_isSkippingAll) yield return new WaitForSeconds(1.5f);
            }

            // If skipped, ensure all active pillars are full
            if (_ritualSkipped || _isSkippingAll)
            {
                foreach (var p in _ringPillars) p.InstantFill();
            }
        }

        private void PlayPillarSound()
        {
            if (_audioSource != null && _pillarAppearClip != null)
            {
                _audioSource.PlayOneShot(_pillarAppearClip);
            }
        }

        public void SkipOne() 
        {
            if (_visualRoot != null && _visualRoot.activeSelf && !_revealRoot.activeSelf)
                _ritualSkipped = true;
            else
                _isSkippingCurrent = true;
        }

        public void SkipAll() => _isSkippingAll = true;

        private IEnumerator RevealSingleUnit(UnitInventoryEntry result)
        {
            if (_revealRoot != null) _revealRoot.SetActive(true);

            // Compensation UI Reset
            if (_duplicateBadge != null) _duplicateBadge.SetActive(result.IsDuplicate);

            var unit = _unitDatabase.GetUnitByID(result.UnitID);
            if (unit != null)
            {
                if (_txtName != null) _txtName.text = unit.UnitName;
                _txtTitle.text = !string.IsNullOrEmpty(unit.UnitTitle) ? unit.UnitTitle : unit.Rarity.ToString();
                
                // Set dynamic dialogue quote from UnitData
                if (_txtDialogue != null)
                {
                    _txtDialogue.text = !string.IsNullOrEmpty(unit.SummonQuote) 
                        ? $"\"{unit.SummonQuote}\"" 
                        : "\"Greetings, Sovereign. My blade is yours to command.\"";
                }
                
                if (_characterFullBody != null) _characterFullBody.sprite = unit.GetSprite(UnitData.UnitImageType.FullSprite);
            }

            // Reveal Animation
            var cg = _revealRoot.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0;
                cg.DOFade(1f, 0.4f);
            }

            // Duplicate Compensation POP animation
            if (result.IsDuplicate && _compensationPopGroup != null)
            {
                if (_popGoldText != null) _popGoldText.text = "X" + result.CompensationGold.ToString();
                if (_popCrestText != null) _popCrestText.text = "X" + result.CompensationBloodCrest.ToString();
                
                // Setup animation
                _compensationPopGroup.alpha = 0;
                _compensationPopRect.localScale = Vector3.one * 0.5f;
                
                Sequence s = DOTween.Sequence();
                s.AppendInterval(0.5f); // Wait for character reveal
                s.Append(_compensationPopGroup.DOFade(1f, 0.3f));
                s.Join(_compensationPopRect.DOScale(1.1f, 0.3f).SetEase(Ease.OutBack));
                s.Append(_compensationPopRect.DOScale(1.0f, 0.1f));
                s.AppendInterval(1.5f);
                s.Append(_compensationPopGroup.DOFade(0f, 0.3f));
            }

            // Wait for user or timer
            float timer = 0;
            while (timer < _revealViewDuration && !_isSkippingCurrent && !_isSkippingAll)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // Hide pop-up if still active
            if (_compensationPopGroup != null) _compensationPopGroup.DOFade(0f, 0.2f);

            if (cg != null && !_isSkippingAll)
            {
                yield return cg.DOFade(0f, 0.3f).WaitForCompletion();
            }
            
            if (_revealRoot != null) _revealRoot.SetActive(false);
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

        [ContextMenu("Auto-Assign UI")]
        public void AutoAssignUI()
        {
            _visualRoot = gameObject;
            var ritualBGTransform = transform.Find("Ritual_Background");
            if (ritualBGTransform != null)
            {
                _ritualBackground = ritualBGTransform.GetComponent<Image>();
                _pillarRing = ritualBGTransform.gameObject; // Container
                
                _ringPillars.Clear();
                for (int i = 1; i <= 10; i++)
                {
                    var pChild = ritualBGTransform.Find($"Pillar_{i}");
                    if (pChild != null)
                    {
                        var pillar = new GachaPillar();
                        pillar.Root = pChild.gameObject;
                        pillar.GlowImage = pChild.GetComponent<Image>();
                        _ringPillars.Add(pillar);
                    }
                }
            }

            var revealT = transform.Find("Reveal_View");
            if (revealT != null)
            {
                _revealRoot = revealT.gameObject;
                _characterFullBody = revealT.Find("Character_FullBody")?.GetComponent<Image>();
                
                var infoT = revealT.Find("Character_Info");
                if (infoT != null)
                {
                    _txtName = infoT.Find("Txt_Name")?.GetComponent<TextMeshProUGUI>();
                    _txtTitle = infoT.Find("Txt_Title")?.GetComponent<TextMeshProUGUI>();
                    _bgName = infoT.Find("Bg_Name")?.GetComponent<Image>();
                    _bgTitle = infoT.Find("Bg_Title")?.GetComponent<Image>();
                }
                
                var dialogT = revealT.Find("Dialogue_Box");
                if (dialogT != null)
                {
                    _txtDialogue = dialogT.Find("Txt_Dialogue")?.GetComponent<TextMeshProUGUI>();
                    _dialogueBG = dialogT.GetComponent<Image>();
                }

                var skipT = revealT.Find("Skip_Container");
                if (skipT != null)
                {
                    _btnSkip = skipT.Find("Btn_Skip")?.gameObject;
                    _btnSkipAll = skipT.Find("Btn_SkipAll")?.gameObject;
                }
            }
        }
    }

    [System.Serializable]
    public class GachaPillar
    {
        public GameObject Root;
        public Image GlowImage;
        
        public void ResetForRitual()
        {
            if (Root == null) return;
            Root.SetActive(false);
            if (GlowImage != null)
            {
                GlowImage.type = Image.Type.Filled;
                GlowImage.fillMethod = Image.FillMethod.Vertical;
                GlowImage.fillOrigin = (int)Image.OriginVertical.Bottom;
                GlowImage.fillAmount = 0;
            }
        }

        public void Show(UnitRarity rarity, float duration)
        {
            if (Root == null) return;
            Root.SetActive(true);
            
            if (GlowImage != null)
            {
                Color targetColor = GetRarityColor(rarity);
                GlowImage.color = targetColor;
                GlowImage.fillAmount = 0;
                GlowImage.DOFillAmount(1f, duration).SetEase(Ease.Linear);
            }
        }

        public void InstantFill()
        {
            if (Root == null) return;
            Root.SetActive(true);
            if (GlowImage != null) GlowImage.fillAmount = 1f;
        }

        public void Hide()
        {
            if (Root != null) Root.SetActive(false);
        }

        private Color GetRarityColor(UnitRarity rarity)
        {
            return rarity switch
            {
                UnitRarity.Legendary => new Color(1f, 0.2f, 0.2f, 0.9f),  // Vibrant Red
                UnitRarity.Master => new Color(1f, 0.8f, 0f, 0.9f),     // Gold
                UnitRarity.Elite => new Color(0.6f, 0.2f, 1f, 0.8f),     // Purple
                UnitRarity.Rare => new Color(0.2f, 0.6f, 1f, 0.8f),      // Blue
                UnitRarity.Uncommon => new Color(0.2f, 1f, 0.4f, 0.6f),  // Green
                _ => new Color(1f, 1f, 1f, 0.4f)                         // White/Common
            };
        }
    }
}
