using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using MaouSamaTD.Data;
using Zenject;

namespace MaouSamaTD.UI.Gacha
{
    public class GachaAnimationController : MonoBehaviour
    {
        [Header("UI Roots")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private GachaResultPanel _resultPanel;
        
        [Header("Animation References")]
        [SerializeField] private Animator _ritualAnimator;
        [SerializeField] private List<GachaPillar> _pillars;
        
        [Inject] private UnitDatabase _unitDatabase;
        
        private List<UnitInventoryEntry> _pendingResults;
        private bool _isSkipping;

        public void PlayRitual(List<UnitInventoryEntry> results)
        {
            _pendingResults = results;
            _isSkipping = false;
            if (_visualRoot != null) _visualRoot.SetActive(true);
            
            StartCoroutine(RitualSequence());
        }

        private IEnumerator RitualSequence()
        {
            // Play Ritual Circle Animation
            if (_ritualAnimator != null) _ritualAnimator.SetTrigger("StartRitual");
            yield return new WaitForSeconds(1.5f); // Reduced wait for snappier feel
            
            if (_isSkipping) yield break;

            // Show Pillars with color anticipation
            for (int i = 0; i < _pendingResults.Count; i++)
            {
                if (i < _pillars.Count)
                {
                    var unitData = _unitDatabase.GetUnitByID(_pendingResults[i].UnitID);
                    _pillars[i].Show(_pendingResults[i], unitData != null ? unitData.Rarity : MaouSamaTD.Units.UnitRarity.Common);
                }
                yield return new WaitForSeconds(0.4f);
                if (_isSkipping) break;
            }

            if (!_isSkipping) yield return new WaitForSeconds(1.2f);
            
            ShowResults();
        }

        public void Skip()
        {
            _isSkipping = true;
            ShowResults();
        }

        private void ShowResults()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
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
        public Image AuraGlow; // Reference to the glow effect on the pillar
        
        public void Show(UnitInventoryEntry result, MaouSamaTD.Units.UnitRarity rarity)
        {
            if (GameObject != null) GameObject.SetActive(true);
            if (Animator != null) 
            {
                // Set rarity level for different animations/colors in animator
                Animator.SetInteger("Rarity", (int)rarity);
                Animator.SetTrigger("Rise");
            }
            
            // Direct color tinting if using simple UI Image
            if (AuraGlow != null)
            {
                AuraGlow.color = rarity switch
                {
                    MaouSamaTD.Units.UnitRarity.Legendary => new Color(1f, 0.84f, 0f, 0.8f), // Gold
                    MaouSamaTD.Units.UnitRarity.Master => new Color(0.6f, 0.2f, 1f, 0.8f),   // Purple
                    MaouSamaTD.Units.UnitRarity.Elite => new Color(0.2f, 0.6f, 1f, 0.8f),    // Blue
                    _ => new Color(1f, 1f, 1f, 0.4f)                                          // White/Grey
                };
            }
        }
    }
}
