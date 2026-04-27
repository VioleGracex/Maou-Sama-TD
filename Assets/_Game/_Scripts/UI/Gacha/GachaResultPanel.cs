using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using MaouSamaTD.Data;
using MaouSamaTD.Units;
using MaouSamaTD.UI.MainMenu;
using Zenject;

namespace MaouSamaTD.UI.Gacha
{
    public class GachaResultPanel : MonoBehaviour
    {
        [Header("UI Containers")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Transform _gridContainer;
        [SerializeField] private Transform _soloSlot;
        [SerializeField] private GameObject _unitCardGachaPrefab; 
        [SerializeField] private Button _btnConfirm;

        [Header("Settings")]
        [SerializeField] private float _revealInterval = 0.15f;

        [Header("Compensation Summary")]
        [SerializeField] private GameObject _compensationOverallRoot;
        [SerializeField] private TMPro.TextMeshProUGUI _txtTotalGold;
        [SerializeField] private TMPro.TextMeshProUGUI _txtTotalBloodCrest;

        [Header("Rarity Appearance Settings")]
        [SerializeField] private List<RarityColorConfig> _rarityConfigs = new List<RarityColorConfig>();
        [SerializeField] private float _defaultGlowIntensity = 2.0f;
        [SerializeField] private float _glowOffsetScale = 1.15f; 

        [System.Serializable]
        public struct RarityColorConfig
        {
            public UnitRarity Rarity;
            public Color Color;
            public float IntensityOverride;
        }
        
        [Inject] private UnitDatabase _unitDatabase;

        private void Awake()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            
            if (_btnConfirm != null)
            {
                _btnConfirm.onClick.RemoveAllListeners();
                _btnConfirm.onClick.AddListener(() => {
                    Close();
                    // Restore navigation in the main GachaPanel
                    var parentPanel = Object.FindAnyObjectByType<GachaPanel>(FindObjectsInactive.Include);
                    if (parentPanel != null) parentPanel.RestoreNavigation();
                });
            }
        }

        public void DisplayResults(List<UnitInventoryEntry> results)
        {  
            if (_visualRoot != null) _visualRoot.SetActive(true);
            
            // Clear old icons
            ClearSlots();

            // Total Compensation Calculation
            int totalGold = 0;
            int totalCrest = 0;
            bool hasAnyDuplicate = false;

            foreach (var r in results)
            {
                if (r.IsDuplicate)
                {
                    totalGold += r.CompensationGold;
                    totalCrest += r.CompensationBloodCrest;
                    hasAnyDuplicate = true;
                }
            }

            bool showCompensation = totalGold > 0 || totalCrest > 0;
            if (_compensationOverallRoot != null) _compensationOverallRoot.SetActive(showCompensation);
            if (_txtTotalGold != null) _txtTotalGold.text = "X" + totalGold.ToString();
            if (_txtTotalBloodCrest != null) _txtTotalBloodCrest.text = "X" + totalCrest.ToString();
            
            if (results.Count == 1)
            {
                if (_soloSlot != null) _soloSlot.gameObject.SetActive(true);
                if (_gridContainer != null) _gridContainer.gameObject.SetActive(false);
                StartCoroutine(DisplaySingle(results[0]));
            }
            else
            {
                if (_soloSlot != null) _soloSlot.gameObject.SetActive(false);
                if (_gridContainer != null) _gridContainer.gameObject.SetActive(true);
                StartCoroutine(DisplaySequence(results));
            }
        }

        private void ClearSlots()
        {
            if (_gridContainer != null)
            {
                foreach (Transform child in _gridContainer)
                {
                    // Clear recursive children if we are placing INSIDE the slots
                    foreach(Transform subChild in child) Destroy(subChild.gameObject);
                }
            }
            if (_soloSlot != null)
            {
                foreach(Transform subChild in _soloSlot) Destroy(subChild.gameObject);
            }
        }

        private IEnumerator DisplaySingle(UnitInventoryEntry result)
        {
            yield return StartCoroutine(SpawnCardInSlot(_soloSlot, result));
        }

        private IEnumerator DisplaySequence(List<UnitInventoryEntry> results)
        {
            int slotIndex = 0;
            foreach (var result in results)
            {
                if (slotIndex >= _gridContainer.childCount) break;
                
                Transform targetSlot = _gridContainer.GetChild(slotIndex);
                yield return StartCoroutine(SpawnCardInSlot(targetSlot, result));
                
                slotIndex++;
                yield return new WaitForSeconds(_revealInterval);
            }
        }

        private IEnumerator SpawnCardInSlot(Transform slot, UnitInventoryEntry result)
        {
            UnitData data = _unitDatabase.GetUnitByID(result.UnitID);
            if (data == null) yield break;

            var go = Instantiate(_unitCardGachaPrefab, slot);
            var cardUI = go.GetComponent<UnitCardUI>();
            
            if (cardUI != null)
            {
                cardUI.Setup(data);
                cardUI.SetInteractable(false);
                
                var effect = go.AddComponent<GachaResultCardEffect>();
                
                var config = _rarityConfigs.Find(x => x.Rarity == data.Rarity);
                Color rarityColor = config.Color != default ? config.Color : GetFallbackColor(data.Rarity);
                float intensity = config.IntensityOverride > 0 ? config.IntensityOverride : _defaultGlowIntensity;

                effect.ApplyGlow(rarityColor, intensity, _glowOffsetScale);
            }
        }

        private Color GetFallbackColor(UnitRarity rarity)
        {
            return rarity switch
            {
                UnitRarity.Legendary => new Color(1f, 0.5f, 0f, 1f),    // Orange/Gold (High End)
                UnitRarity.Master => new Color(0.7f, 0.2f, 1f, 1f),    // Purple (Mythic)
                UnitRarity.Elite => new Color(0.2f, 0.6f, 1f, 1f),     // Blue (Rare/Elite)
                UnitRarity.Rare => new Color(0.2f, 1f, 0.4f, 1f),      // Green (Common/Uncommon)
                _ => new Color(1f, 1f, 1f, 0.5f)                       // White/Gray (Basic)
            };
        }



        public void Close()
        {
            _visualRoot.SetActive(false);
        }

        [ContextMenu("Auto-Assign Result UI")]
        public void AutoAssign()
        {
            _visualRoot = gameObject;
            _gridContainer = transform.Find("Gacha_Results_Grid");
            _soloSlot = transform.Find("Gacha_Result_Slot_Solo");
            _btnConfirm = GetComponentInChildren<Button>(true);
            
            var prefabPath = "Assets/_Game/Prefabs/UI/campaign/UnitCardGacha.prefab";
            _unitCardGachaPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
    }
}
