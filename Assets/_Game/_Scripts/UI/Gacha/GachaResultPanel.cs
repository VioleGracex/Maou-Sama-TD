using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MaouSamaTD.Data;

namespace MaouSamaTD.UI.Gacha
{
    public class GachaResultPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Transform _resultContainer;
        [SerializeField] private GameObject _resultItemPrefab;
        [SerializeField] private Button _btnConfirm;

        [SerializeField] private float _revealInterval = 0.2f;

        public void DisplayResults(List<UnitInventoryEntry> results)
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            
            // Clear old icons
            foreach (Transform child in _resultContainer) Destroy(child.gameObject);
            
            StartCoroutine(DisplaySequence(results));
        }

        private System.Collections.IEnumerator DisplaySequence(List<UnitInventoryEntry> results)
        {
            foreach (var result in results)
            {
                var go = Instantiate(_resultItemPrefab, _resultContainer);
                var item = go.GetComponent<GachaResultItem>();
                if (item != null)
                {
                    // Logic: Check if it's new (this should be passed from GachaManager ideally)
                    item.Setup(result, false); 
                }
                
                yield return new WaitForSeconds(_revealInterval);
            }
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }
    }
}
