using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MaouSamaTD.Units;
using MaouSamaTD.UI.Vassals;
using Zenject;

namespace MaouSamaTD.UI
{
    /// <summary>
    /// Handles skin selection, infinite scroll, and visual previews in the unit inspector.
    /// </summary>
    public class UnitInspectorSkinsPanel : MonoBehaviour
    {
        [Header("Skins Page References")]
        [SerializeField] private SkinInfiniteScroll _skinInfiniteScroll;
        [SerializeField] private Image _skinSplashPreview;        // Full-screen art
        [SerializeField] private Animator _skinChibiPreview;       // Idle animator
        [SerializeField] private TextMeshProUGUI _skinNameText;
        [SerializeField] private Button _btnApplySkin;
        [SerializeField] private GameObject _skinItemPrefab;

        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;

        private UnitData _currentUnit;
        private List<UnitData.SkinData> _skinDataList = new List<UnitData.SkinData>();
        private List<SkinCardUI> _spawnedCards = new List<SkinCardUI>();
        private UnitData.SkinData _selectedSkin;
        private UnitData _lastSkinUnit;

        public void Initialize()
        {
            if (_skinInfiniteScroll) _skinInfiniteScroll.OnSelectionChanged += OnSkinScrollSelectionChanged;
            if (_btnApplySkin) _btnApplySkin.onClick.AddListener(OnApplySkinClicked);
        }

        private void OnEnable()
        {
            PlayIdleAnimation();
        }

        public void Refresh(UnitData u)
        {
            _currentUnit = u;
            if (u == null) return;
            
            // Optimization: Only rebuild if it's a different unit
            if (_lastSkinUnit == _currentUnit) 
            {
                int currentEquippedIndex = _skinDataList.FindIndex(s => s != null ? s.SkinID == _currentUnit.EquippedSkinID : string.IsNullOrEmpty(_currentUnit.EquippedSkinID));
                UpdateSkinItemsStatus(currentEquippedIndex);
                
                // FIX: Ensure the side panel (button/text) updates even if we don't rebuild the list
                SelectSkin(_selectedSkin); 
                return;
            }
            _lastSkinUnit = _currentUnit;

            _skinDataList.Clear();
            List<GameObject> items = new List<GameObject>();

            Transform content = (_skinInfiniteScroll != null) ? _skinInfiniteScroll.Content : null;
            if (content == null) return;

            foreach (Transform child in content) Destroy(child.gameObject);
            _spawnedCards.Clear();
                
            _skinDataList.Add(null); // Base Skin
            items.Add(CreateSkinItem(null, content)); 

            foreach (var skin in _currentUnit.Skins)
            {
                if (skin != null)
                {
                    _skinDataList.Add(skin);
                    items.Add(CreateSkinItem(skin, content));
                }
            }

            if (_skinInfiniteScroll) _skinInfiniteScroll.Initialize(items);

            int equippedIndex = _skinDataList.FindIndex(s => s != null ? s.SkinID == _currentUnit.EquippedSkinID : string.IsNullOrEmpty(_currentUnit.EquippedSkinID));
            if (equippedIndex >= 0)
            {
                SelectSkin(_skinDataList[equippedIndex]);
                UpdateSkinItemsStatus(equippedIndex);
            }
        }

        private void OnSkinScrollSelectionChanged(int index)
        {
            if (_skinDataList != null && index >= 0 && index < _skinDataList.Count)
            {
                SelectSkin(_skinDataList[index]);
            }
            UpdateSkinItemsStatus(index);
        }

        private void UpdateSkinItemsStatus(int activeIndex = -1)
        {
            if (_skinInfiniteScroll == null || _currentUnit == null) return;
            
            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                var cardUI = _spawnedCards[i];
                if (cardUI != null)
                {
                    UnitData.SkinData skin = (i < _skinDataList.Count) ? _skinDataList[i] : null;
                    string skinID = (skin != null) ? skin.SkinID : null;

                    // FIX: Check actual equipped status on the unit, not just the scroll index
                    bool isEquipped = (skinID == _currentUnit.EquippedSkinID) || (string.IsNullOrEmpty(skinID) && string.IsNullOrEmpty(_currentUnit.EquippedSkinID));
                    cardUI.SetEquipped(isEquipped);

                    // Highlight the 'selected' card in the scroll view for centering feedback
                    if (activeIndex != -1) cardUI.SetHighlighted(i == activeIndex);
                    
                    // Sync the locked status whenever we refresh.
                    bool isLocked = !_currentUnit.IsSkinUnlocked(skinID);
                    cardUI.SetLocked(isLocked);
                }
            }
        }

        private GameObject CreateSkinItem(UnitData.SkinData skin, Transform parent)
        {
            if (_skinItemPrefab == null) return null;
            var go = Instantiate(_skinItemPrefab, parent);
            var cardUI = go.GetComponent<SkinCardUI>();
            if (cardUI != null)
            {
                _spawnedCards.Add(cardUI);
                string theme = (skin != null) ? skin.SkinThemeName : "Default";
                // PER USER: "use waist up for card not avatar"
                Sprite icon = (skin != null) ? skin.WaistUp : _currentUnit.BaseSkin.WaistUp;
                
                string skinID = (skin != null) ? skin.SkinID : null;
                bool isEquipped = skinID == _currentUnit.EquippedSkinID || (string.IsNullOrEmpty(skinID) && string.IsNullOrEmpty(_currentUnit.EquippedSkinID));
                bool isLocked = !_currentUnit.IsSkinUnlocked(skinID);
                int cost = (skin != null) ? skin.UnlockCost : 0;
                bool isPremium = (skin != null) && skin.IsPremium;

                cardUI.SetState(theme, icon, isEquipped, isLocked, cost, isPremium);

                // Autoscroll to card when clicked
                int cardIndex = _skinDataList.Count - 1; // Current index in creation loop
                cardUI.OnCardClicked += (_) => 
                {
                    if (_skinInfiniteScroll) _skinInfiniteScroll.ScrollTo(cardIndex);
                };
            }
            return go;
        }

        private void SelectSkin(UnitData.SkinData skin)
        {
            _selectedSkin = skin;
            bool isBase = (skin == null);
            
            string skinName = isBase ? _currentUnit.UnitName : (skin != null ? skin.SkinThemeName : "DEFAULT");
            if (_skinSplashPreview) _skinSplashPreview.sprite = isBase ? _currentUnit.BaseSkin.FullSplashArt : skin.FullSplashArt;
            if (_skinNameText) _skinNameText.text = skinName.ToUpper();
            
            if (_skinChibiPreview != null)
            {
                var img = _skinChibiPreview.GetComponent<Image>();
                if (img) img.sprite = isBase ? _currentUnit.BaseSkin.Chibi : skin.Chibi;
                
                // Get animator from data
                _skinChibiPreview.runtimeAnimatorController = _currentUnit.GetAnimatorController(skin);
                PlayIdleAnimation();
            }

            UpdateApplyButton(skin);
        }

        private void PlayIdleAnimation()
        {
            if (_skinChibiPreview != null && _skinChibiPreview.runtimeAnimatorController != null)
            {
                _skinChibiPreview.Play("Idle", 0, 0f);
            }
        }

        private void UpdateApplyButton(UnitData.SkinData skin)
        {
            if (_btnApplySkin == null) return;
            var txt = _btnApplySkin.GetComponentInChildren<TextMeshProUGUI>();
            string skinID = skin != null ? skin.SkinID : null;
            bool alreadyEquipped = (_currentUnit.EquippedSkinID == skinID) || (string.IsNullOrEmpty(skinID) && string.IsNullOrEmpty(_currentUnit.EquippedSkinID));
            bool isUnlocked = _currentUnit.IsSkinUnlocked(skinID);
            
            if (alreadyEquipped) txt.text = "EQUIPPED";
            else if (!isUnlocked) txt.text = "UNLOCK";
            else txt.text = "APPLY";
            
            _btnApplySkin.interactable = !alreadyEquipped;
        }

        private void OnApplySkinClicked()
        {
            if (_currentUnit == null) return;
            
            string skinID = _selectedSkin != null ? _selectedSkin.SkinID : null;
            string skinName = _selectedSkin != null ? _selectedSkin.SkinThemeName : "DEFAULT";
            bool isUnlocked = _currentUnit.IsSkinUnlocked(skinID);

            Debug.Log($"[Skins] Apply Clicked: {skinName} (ID: {skinID}) | Unlocked: {isUnlocked}");

            if (!isUnlocked && _selectedSkin != null)
            {
                // ... (rest of unlock logic)
                bool success = false;
                if (_selectedSkin.IsPremium)
                {
                    success = _saveManager.SpendBloodCrest(_selectedSkin.UnlockCost);
                }
                else
                {
                    success = _saveManager.SpendGold(_selectedSkin.UnlockCost);
                }

                if (success)
                {
                    _currentUnit.UnlockSkin(skinID);
                    Debug.Log($"[Skins] Unlocked skin SUCCESS: {skinID}");
                }
                else
                {
                    Debug.LogWarning("[Skins] Insufficient currency to unlock skin.");
                    return; 
                }
            }

            Debug.Log($"[Skins] Equipping Skin: {skinName}");
            _currentUnit.EquippedSkinID = skinID;
            Refresh(_currentUnit);
        }
    }
}
