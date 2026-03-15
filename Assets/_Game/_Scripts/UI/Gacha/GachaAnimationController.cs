using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using MaouSamaTD.Data;

namespace MaouSamaTD.UI.Gacha
{
    public class GachaAnimationController : MonoBehaviour
    {
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Animator _ritualAnimator;
        [SerializeField] private List<GachaPillar> _pillars;
        [SerializeField] private Button _btnSkip;
        
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
            yield return new WaitForSeconds(2f);
            
            if (_isSkipping) yield break;

            // Show Pillars
            for (int i = 0; i < _pendingResults.Count; i++)
            {
                if (i < _pillars.Count)
                {
                    _pillars[i].Show(_pendingResults[i]);
                }
                yield return new WaitForSeconds(0.5f);
                if (_isSkipping) break;
            }

            if (!_isSkipping) yield return new WaitForSeconds(1f);
            
            ShowResults();
        }

        public void Skip()
        {
            _isSkipping = true;
            ShowResults();
        }

        private void ShowResults()
        {
            // Close ritual and open results panel
            if (_visualRoot != null) _visualRoot.SetActive(false);
            // Result panel opening logic will go here
        }
    }

    [System.Serializable]
    public class GachaPillar
    {
        public GameObject GameObject;
        public Animator Animator;
        
        public void Show(UnitInventoryEntry result)
        {
            if (GameObject != null) GameObject.SetActive(true);
            if (Animator != null) Animator.SetTrigger("Rise");
        }
    }
}
