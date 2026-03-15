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

        public void DisplayResults(List<UnitInventoryEntry> results)
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            
            // Clear old icons
            foreach (Transform child in _resultContainer) Destroy(child.gameObject);
            
            foreach (var result in results)
            {
                Instantiate(_resultItemPrefab, _resultContainer);
                // Bind data to prefab here
            }
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }
    }
}
