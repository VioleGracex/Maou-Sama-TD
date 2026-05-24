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
            if (bondTierText != null) 
            {
                bondTierText.text = "TIER 0\nSYNERGY PULSE ACTIVE";
                bondTierText.enableWordWrapping = false;
                bondTierText.overflowMode = TextOverflowModes.Overflow;
                var rect = bondTierText.GetComponent<RectTransform>();
                if (rect != null && rect.sizeDelta.x < 200) {
                    rect.sizeDelta = new Vector2(250, rect.sizeDelta.y);
                }
            }
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
                        
                        // Try to get TextMeshProUGUI / Text using robust matching with fallbacks
                        var tmpTexts = itemObj.GetComponentsInChildren<TMPro.TMP_Text>(true);
                        TMPro.TMP_Text nameTmp = null;
                        TMPro.TMP_Text bondTmp = null;

                        foreach (var t in tmpTexts)
                        {
                            string tName = t.gameObject.name.ToLowerInvariant();
                            if (tName.Contains("name") || tName.Contains("title") || tName.Contains("char")) nameTmp = t;
                            else if (tName.Contains("bond") || tName.Contains("lvl") || tName.Contains("level") || tName.Contains("desc")) bondTmp = t;
                        }

                        // Fallback to index if no name matched
                        if (nameTmp == null && tmpTexts.Length > 0) nameTmp = tmpTexts[0];
                        if (bondTmp == null && tmpTexts.Length > 1) bondTmp = tmpTexts[1];

                        string displayName = string.IsNullOrEmpty(unit.UnitTitle) ? unit.UnitName.ToUpper() : $"{unit.UnitTitle.ToUpper()} {unit.UnitName.ToUpper()}";
                        if (nameTmp != null) nameTmp.text = displayName;
                        if (bondTmp != null) bondTmp.text = "BOND LV." + (Mathf.FloorToInt(unit.Amity / 10f));

                        // Same for standard UI Text
                        var legacyTexts = itemObj.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                        UnityEngine.UI.Text nameLeg = null;
                        UnityEngine.UI.Text bondLeg = null;

                        foreach (var t in legacyTexts)
                        {
                            string tName = t.gameObject.name.ToLowerInvariant();
                            if (tName.Contains("name") || tName.Contains("title") || tName.Contains("char")) nameLeg = t;
                            else if (tName.Contains("bond") || tName.Contains("lvl") || tName.Contains("level") || tName.Contains("desc")) bondLeg = t;
                        }

                        if (nameLeg == null && legacyTexts.Length > 0) nameLeg = legacyTexts[0];
                        if (bondLeg == null && legacyTexts.Length > 1) bondLeg = legacyTexts[1];

                        if (nameLeg != null) nameLeg.text = displayName;
                        if (bondLeg != null) bondLeg.text = "BOND LV." + (Mathf.FloorToInt(unit.Amity / 10f));

                        // Try to get Image for avatar using robust matching
                        var images = itemObj.GetComponentsInChildren<Image>(true);
                        foreach (var img in images)
                        {
                            string iName = img.gameObject.name.ToLowerInvariant();
                            if (iName.Contains("avatar") || iName.Contains("icon") || iName.Contains("portrait")) 
                            {
                                var sprite = unit.GetSprite(UnitData.UnitImageType.Avatar);
                                if (sprite != null) 
                                {
                                    img.sprite = sprite;
                                    img.color = Color.white;
                                }
                            }
                            // Populate Amity/Bond slider if it's a Filled Image
                            else if (iName.Contains("fill") && img.type == Image.Type.Filled)
                            {
                                img.fillAmount = (unit.Amity % 10f) / 10f;
                            }
                        }

                        // Setup all sliders inside the item
                        var sliders = itemObj.GetComponentsInChildren<Slider>(true);
                        foreach (var slider in sliders)
                        {
                            if (slider != null)
                            {
                                slider.value = (unit.Amity % 10f) / 10f;
                            }
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
