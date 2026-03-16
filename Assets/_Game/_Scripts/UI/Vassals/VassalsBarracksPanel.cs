using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MaouSamaTD.Data;
using MaouSamaTD.UI.MainMenu;
using Zenject;
using DG.Tweening;

namespace MaouSamaTD.UI.Vassals
{
    /// <summary>
    /// Management page for all owned units (Vassals).
    /// Handles inspection, leveling, and upgrading.
    /// </summary>
    public class VassalsBarracksPanel : MonoBehaviour, IUIController
    {
        [Header("Roots")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private UnitCardUI _cardPrefab;
        [SerializeField] private GameObject _sortContainer;
        [SerializeField] private GameObject _filterContainer;
        [SerializeField] private TextMeshProUGUI _unitCountText;

        [Header("Layout Animation")]
        [SerializeField] private RectTransform _scrollViewRect;
        [SerializeField] private float _expandedPaddingLeft = 0f;
        [SerializeField] private float _squeezedPaddingLeft = 400f;
        [SerializeField] private float _paddingTop = 100f;
        [SerializeField] private float _paddingBottom = 0f;

        [Header("Sub Panels")]
        [SerializeField] private UnitInspectorPanel _inspectorPanel;

        [Header("Buttons")]
        [SerializeField] private Button _btnLevelUp;
        [SerializeField] private Button _btnPromote;
        [SerializeField] private Button _btnClose;

        private List<UnitCardUI> _spawnedCards = new List<UnitCardUI>();

        public GameObject VisualRoot => _visualRoot;
        public bool AddsToHistory => true;
        public Button CloseButton => _btnClose;
        public Button LevelUpButton => _btnLevelUp;

        public void Awake()
        {
            if (_btnClose == null)
            {
                var btnTr = transform.Find("Header/Back_MissionReady_Btn");
                if (btnTr != null) _btnClose = btnTr.GetComponent<Button>();
            }
        }

        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            
            // Connect inspector close button if not already
            if (_inspectorPanel != null && _inspectorPanel.CloseButton != null)
            {
                _inspectorPanel.CloseButton.onClick.RemoveAllListeners();
                _inspectorPanel.CloseButton.onClick.AddListener(() => _inspectorPanel.Close());
            }

            RefreshInventory();
        }
    
        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            if (_inspectorPanel != null) _inspectorPanel.Close();
            UpdateScrollRectLayout(false);
        }
    
        public bool RequestClose()
        {
            // If inspector is open, close it first
            if (_inspectorPanel != null && _inspectorPanel.VisualRoot != null && _inspectorPanel.VisualRoot.activeSelf)
            {
                _inspectorPanel.Close();
                UpdateScrollRectLayout(false);
                return false;
            }

            return true;
        }

        public void ResetState()
        {
            if (_inspectorPanel != null) _inspectorPanel.ResetState();
        }

        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;

        public void RefreshInventory()
        {
            if (_cardContainer == null || _cardPrefab == null) return;

            // Get owned units
            List<string> ownedIDs = new List<string>();
            if (_saveManager != null && _saveManager.CurrentData != null)
            {
                ownedIDs = _saveManager.CurrentData.UnlockedUnits;
            }

            // Reuse/Spawn cards
            while (_spawnedCards.Count < ownedIDs.Count)
            {
                var card = Instantiate(_cardPrefab, _cardContainer);
                _spawnedCards.Add(card);
            }

            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                var card = _spawnedCards[i];
                if (i < ownedIDs.Count)
                {
                    var unit = MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase?.GetUnitByID(ownedIDs[i]);
                    if (unit != null)
                    {
                        card.gameObject.SetActive(true);
                        card.Setup(unit, OnCardClicked);
                    }
                    else card.gameObject.SetActive(false);
                }
                else card.gameObject.SetActive(false);
            }

            if (_unitCountText != null)
                _unitCountText.text = $"VASSALS: {ownedIDs.Count}";
        }

        private void OnCardClicked(UnitCardUI card)
        {
            if (_inspectorPanel != null)
            {
                _inspectorPanel.Open(card.Data);
                UpdateScrollRectLayout(true);
            }
        }

        private void UpdateScrollRectLayout(bool isDetailsOpen)
        {
            if (_scrollViewRect == null) return;
            DOTween.Kill(_scrollViewRect);

            float targetLeft = isDetailsOpen ? _squeezedPaddingLeft : _expandedPaddingLeft;
            float targetRight = 0f; 
            
            Vector2 targetMin = new Vector2(targetLeft, _paddingBottom);
            Vector2 targetMax = new Vector2(-targetRight, -_paddingTop);

            DOTween.To(() => _scrollViewRect.offsetMin, x => _scrollViewRect.offsetMin = x, targetMin, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
            DOTween.To(() => _scrollViewRect.offsetMax, x => _scrollViewRect.offsetMax = x, targetMax, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
        }
    }
}
