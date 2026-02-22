using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Data;
using Zenject;
using DG.Tweening;

namespace MaouSamaTD.UI.MainMenu
{
    public class AscensionPanel : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private GameObject _homeScreenRoot; // To turn back on after Ascension

        [Header("Selections")]
        [SerializeField] private Button _tyrantButton; // Male / Force
        [SerializeField] private Button _sovereignButton; // Female / Guile
        [SerializeField] private GameObject _tyrantSelectedHighlight;
        [SerializeField] private GameObject _sovereignSelectedHighlight;

        [Header("Identity Input")]
        [SerializeField] private CanvasGroup _inputRootCanvasGroup; // For fade in
        [SerializeField] private TMP_InputField _nameInputField;
        [SerializeField] private Button _diceButton; 
        [SerializeField] private Button _ariseButton;
        [SerializeField] private Image _ariseFillImage;
        [SerializeField] private TextMeshProUGUI _ariseText;
        [SerializeField] private Color _ariseTextNormalColor = Color.black;
        [SerializeField] private Color _ariseTextHoverColor = Color.white;

        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;

        private MaouGender _selectedGender = MaouGender.Male;
        private string _selectedTrueName = "Tyrant";
        private bool _hasSelectedClass = false;

        private void Start()
        {
            if (_tyrantButton != null) _tyrantButton.onClick.AddListener(() => OnClassSelected(MaouGender.Male, "Sovereign of Force"));
            if (_sovereignButton != null) _sovereignButton.onClick.AddListener(() => OnClassSelected(MaouGender.Female, "Sovereign of Guile"));
            
            if (_nameInputField != null) _nameInputField.onValueChanged.AddListener(OnNameChanged);
            if (_diceButton != null) _diceButton.onClick.AddListener(OnDiceClicked);
            
            if (_ariseButton != null) 
            {
                _ariseButton.onClick.AddListener(OnAriseClicked);
                
                // Add Hover Listeners Dynamically
                UnityEngine.EventSystems.EventTrigger trigger = _ariseButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (trigger == null) trigger = _ariseButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                
                var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                pointerEnter.callback.AddListener((data) => { OnAriseHover(true); });
                trigger.triggers.Add(pointerEnter);

                var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                pointerExit.callback.AddListener((data) => { OnAriseHover(false); });
                trigger.triggers.Add(pointerExit);
            }

            // Hide input initially
            if (_inputRootCanvasGroup != null) 
            {
                _inputRootCanvasGroup.alpha = 0f;
                _inputRootCanvasGroup.interactable = false;
                _inputRootCanvasGroup.blocksRaycasts = false;
            }

            if (_ariseButton != null) _ariseButton.interactable = false;

            RefreshHighlights();
        }

        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            if (_homeScreenRoot != null) _homeScreenRoot.SetActive(false); // Hide the rest of the game
            
            _hasSelectedClass = false;
            
            if (_nameInputField != null) _nameInputField.text = "";
            
            // Reset Arise Button visuals
            if (_ariseFillImage != null) _ariseFillImage.fillAmount = 0f;
            if (_ariseText != null) _ariseText.color = _ariseTextNormalColor;

            RefreshHighlights();

            // Make sure the input field is re-hidden if they closed and reopened it
            if (_inputRootCanvasGroup != null) 
            {
                _inputRootCanvasGroup.alpha = 0f;
                _inputRootCanvasGroup.interactable = false;
                _inputRootCanvasGroup.blocksRaycasts = false;
            }
        }

        private void OnClassSelected(MaouGender gender, string trueName)
        {
            _selectedGender = gender;
            _selectedTrueName = trueName;

            if (!_hasSelectedClass)
            {
                _hasSelectedClass = true;
                // Animate the input field showing up
                if (_inputRootCanvasGroup != null)
                {
                    _inputRootCanvasGroup.DOFade(1f, 0.5f).SetEase(Ease.OutQuad);
                    _inputRootCanvasGroup.interactable = true;
                    _inputRootCanvasGroup.blocksRaycasts = true;
                }
            }
            
            RefreshHighlights();
            ValidateInput();
        }

        private void RefreshHighlights()
        {
            if (_tyrantSelectedHighlight != null) _tyrantSelectedHighlight.SetActive(_selectedGender == MaouGender.Male && _hasSelectedClass);
            if (_sovereignSelectedHighlight != null) _sovereignSelectedHighlight.SetActive(_selectedGender == MaouGender.Female && _hasSelectedClass);
        }

        private void OnNameChanged(string newName)
        {
            ValidateInput();
        }

        private void ValidateInput()
        {
            bool isValid = _hasSelectedClass && _nameInputField != null && !string.IsNullOrWhiteSpace(_nameInputField.text) && _nameInputField.text.Length >= 3;
            if (_ariseButton != null) _ariseButton.interactable = isValid;
        }

        private void OnAriseHover(bool isHovering)
        {
            if (_ariseButton == null || !_ariseButton.interactable) return;

            if (_ariseFillImage != null)
            {
                _ariseFillImage.DOFillAmount(isHovering ? 1f : 0f, 0.2f).SetEase(Ease.OutQuad);
            }
            if (_ariseText != null)
            {
                _ariseText.DOColor(isHovering ? _ariseTextHoverColor : _ariseTextNormalColor, 0.2f);
            }
        }

        private void OnDiceClicked()
        {
            string[] randomNames = new string[] {
                "Mephisto", "Lucifer", "Astaroth", "Beelzebub", "Lilith", 
                "Asmodeus", "Belial", "Azazel", "Abaddon", "Samael",
                "Morrigan", "Bael", "Valak", "Paimon", "Zagan", "Ereshkigal"
            };
            
            string chosenName = randomNames[Random.Range(0, randomNames.Length)];
            if (_nameInputField != null) 
            {
                _nameInputField.text = chosenName;
                ValidateInput();
            }
        }

        private void OnAriseClicked()
        {
            if (_saveManager.CurrentData == null)
            {
                Debug.LogError("[AscensionPanel] SaveData is null! Cannot ascend. Is SaveManager initialized?");
                return;
            }

            // Apply choices to Save Data
            _saveManager.CurrentData.PlayerName = _nameInputField.text.Trim();
            _saveManager.CurrentData.Gender = _selectedGender;
            _saveManager.CurrentData.TrueName = _selectedTrueName;
            
            // Generate standard starting units like Ignis for new saves
            if (_saveManager.CurrentData.UnlockedUnits == null) _saveManager.CurrentData.UnlockedUnits = new System.Collections.Generic.List<string>();
            if (!_saveManager.CurrentData.UnlockedUnits.Contains("Ignis")) _saveManager.CurrentData.UnlockedUnits.Add("Ignis");

            _saveManager.Save();
            
            Debug.Log($"[AscensionPanel] Arise! Welcome {_saveManager.CurrentData.TrueName} {_saveManager.CurrentData.PlayerName}.");

            // Close Ascension and enter Home
            if (_visualRoot != null) _visualRoot.SetActive(false);
            if (_homeScreenRoot != null) _homeScreenRoot.SetActive(true);
        }
    }
}
