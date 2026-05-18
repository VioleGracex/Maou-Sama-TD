using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MaouSamaTD.Data;
using MaouSamaTD.Units;
using Zenject;

namespace MaouSamaTD.UI.Vassals
{
    public class ChambersPageUI : MonoBehaviour, IUIController
    {
        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;
        [SerializeField] private GameObject _visualRoot;
        public GameObject VisualRoot => _visualRoot != null ? _visualRoot : gameObject;
        public bool AddsToHistory => true; 
        [SerializeField] private MaouSamaTD.UI.NavigationFeatures _navFeatures = MaouSamaTD.UI.NavigationFeatures.BackButton | MaouSamaTD.UI.NavigationFeatures.CitadelButton;
        public MaouSamaTD.UI.NavigationFeatures ConfiguredNavFeatures => _navFeatures;

        [Header("Left Panel - List/Grid")]
        public Transform characterListContent;
        public GameObject characterListItemPrefab;
        public TMP_InputField searchInputField;
        public Button btnSortLevel;
        public Button btnSortRarity;
        public Button btnToggleViewMode; // List vs Grid

        [Header("Right Panel - Display")]
        public GameObject rightPanel;
        public Image waistUpImage;
        public TextMeshProUGUI subtitleText;
        public TextMeshProUGUI characterNameText;
        public TextMeshProUGUI quoteText;
        public TextMeshProUGUI bgNameText;

        [Header("Bottom Section - Bond & Vigor")]
        public TextMeshProUGUI bondMagnitudeText;
        public TextMeshProUGUI bondTierText;
        public UnityEngine.UI.Image pactStrengtheningSlider;
        public TextMeshProUGUI pactStrengtheningText;
        public TextMeshProUGUI nextRewardText;

        [Header("Action Buttons")]
        public Button restoreVigorBtn;
        public TextMeshProUGUI restoreVigorCostText;
        public Button bestowOfferingBtn;
        public Button privateAudienceBtn;
        
        [Header("Audience Panel")]
        public GameObject audiencePanel;
        public Image audienceSplashImage;
        public TextMeshProUGUI audienceDialogueText;
        public Button btnAudienceClose;

        private UnitData _currentUnit;



        private void Awake()
        {
            if (privateAudienceBtn != null)
            {
                privateAudienceBtn.onClick.AddListener(OpenAudiencePanel);
            }
            if (btnAudienceClose != null)
            {
                btnAudienceClose.onClick.AddListener(CloseAudiencePanel);
            }
        }
        
        private void OpenAudiencePanel()
        {
            if (audiencePanel != null) audiencePanel.SetActive(true);
            if (audienceDialogueText != null) audienceDialogueText.text = "Welcome to my chambers, Overlord. Shall we converse?";
        }
        
        private void CloseAudiencePanel()
        {
            if (audiencePanel != null) audiencePanel.SetActive(false);
        }

        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            if (rightPanel != null) rightPanel.SetActive(_currentUnit != null);
            RefreshList();
        }

        public void Close()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        public void ResetState() { }
        public bool RequestClose() { return true; }

        public void SelectUnit(UnitData unit)
        {
            _currentUnit = unit;
            if (rightPanel != null) rightPanel.SetActive(unit != null);
            if (unit == null) return;
            
            if (characterNameText != null) characterNameText.text = unit.UnitName.ToUpper();
            if (bgNameText != null) bgNameText.text = unit.UnitName.ToUpper();
            if (subtitleText != null) subtitleText.text = "SSR VASSAL"; // Use rarity if available
            if (quoteText != null) quoteText.text = "Your presence is noted, Overlord."; 
            
            // Setup Vigor and Bond based on progression guide
            if (bondMagnitudeText != null) bondMagnitudeText.text = "0";
            if (bondTierText != null) bondTierText.text = "TIER 0\nSYNERGY PULSE ACTIVE";
            if (pactStrengtheningSlider != null) pactStrengtheningSlider.fillAmount = 0;
            if (pactStrengtheningText != null) pactStrengtheningText.text = "PACT STRENGTHENING (0%)";
            if (nextRewardText != null) nextRewardText.text = "NEXT REWARD: LV.20";
            
            if (restoreVigorCostText != null) restoreVigorCostText.text = "100 TRIBUTE";
            
            // Set waist up image
            if (waistUpImage != null && unit.GetCurrentVisualArt() != null)
            {
                waistUpImage.sprite = unit.GetCurrentVisualArt();
            }

            if (privateAudienceBtn != null)
            {
                privateAudienceBtn.gameObject.SetActive(unit.IsRomanceable);
            }
        }
        
        private void RefreshList()
        {
            if (characterListContent != null)
            {
                foreach (UnityEngine.Transform child in characterListContent)
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }

            if (_saveManager == null || _saveManager.CurrentData == null || MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase == null)
            {
                return;
            }

            int count = 0;
            foreach (var id in _saveManager.CurrentData.UnlockedUnits)
            {
                var unit = MaouSamaTD.Core.AppEntryPoint.LoadedUnitDatabase.GetUnitByID(id);
                if (unit != null)
                {
                    if (characterListItemPrefab != null && characterListContent != null)
                    {
                        var itemObj = Instantiate(characterListItemPrefab, characterListContent);
                        itemObj.SetActive(true);
                        
                        // Try to get TextMeshProUGUI for name
                        var texts = itemObj.GetComponentsInChildren<TextMeshProUGUI>();
                        foreach (var t in texts)
                        {
                            if (t.name == "Name" || t.name == "CharName") t.text = unit.UnitName;
                            if (t.name == "BondLevel" || t.name == "Bond") t.text = "Lv." + (Mathf.FloorToInt(unit.Amity / 10f));
                        }

                        // Try to get Image for avatar
                        var images = itemObj.GetComponentsInChildren<Image>();
                        foreach (var img in images)
                        {
                            if (img.name == "Avatar" || img.name == "Icon") img.sprite = unit.GetSprite(UnitData.UnitImageType.Avatar);
                        }

                        // Setup button click
                        var btn = itemObj.GetComponent<Button>();
                        if (btn == null) btn = itemObj.AddComponent<Button>();
                        
                        btn.onClick.AddListener(() => { SelectUnit(unit); });
                        
                        count++;
                    }
                }
            }

            Debug.Log($"[chambers] spawned for count unit lists: {count}");
        }
    }
}
