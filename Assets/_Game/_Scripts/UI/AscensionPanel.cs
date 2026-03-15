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
        #region References
        [Header("Roots")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private GameObject _homeScreenRoot;

        [Header("Selections")]
        [SerializeField] private Button _tyrantButton;
        [SerializeField] private Button _sovereignButton;
        [SerializeField] private GameObject _tyrantSelectedHighlight;
        [SerializeField] private GameObject _sovereignSelectedHighlight;

        [Header("Character Visuals")]
        [SerializeField] private Image _tyrantVisual;
        [SerializeField] private Image _sovereignVisual;

        [Header("Identity Input")]
        [SerializeField] private CanvasGroup _inputRootCanvasGroup;
        [SerializeField] private TMP_InputField _nameInputField;
        [SerializeField] private Button _diceButton; 
        [SerializeField] private Button _ariseButton;
        [SerializeField] private Image _ariseFillImage;
        [SerializeField] private TextMeshProUGUI _ariseText;
        [SerializeField] private Color _ariseTextNormalColor = Color.black;
        [SerializeField] private Color _ariseTextHoverColor = Color.white;
        #endregion

        #region Dependencies
        [Inject] private MaouSamaTD.Managers.SaveManager _saveManager;
        #endregion

        #region State
        private MaouGender _selectedGender = MaouGender.Male;
        private string _selectedTrueName = "Tyrant";
        private bool _hasSelectedClass = false;
        private bool _tyrantHovering = false;
        private bool _sovereignHovering = false;
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (_tyrantButton != null) 
            {
                _tyrantButton.onClick.AddListener(() => OnClassSelected(MaouGender.Male, "Sovereign of Force"));
                AddHoverEntry(_tyrantButton.gameObject, () => OnCardHover(MaouGender.Male, true));
                AddHoverEntry(_tyrantButton.gameObject, () => OnCardHover(MaouGender.Male, false), false);
            }
            
            if (_sovereignButton != null) 
            {
                _sovereignButton.onClick.AddListener(() => OnClassSelected(MaouGender.Female, "Sovereign of Guile"));
                AddHoverEntry(_sovereignButton.gameObject, () => OnCardHover(MaouGender.Female, true));
                AddHoverEntry(_sovereignButton.gameObject, () => OnCardHover(MaouGender.Female, false), false);
            }
            
            if (_nameInputField != null) _nameInputField.onValueChanged.AddListener(OnNameChanged);
            if (_diceButton != null) _diceButton.onClick.AddListener(OnDiceClicked);
            
            if (_ariseButton != null) 
            {
                _ariseButton.onClick.AddListener(OnAriseClicked);
                
                UnityEngine.EventSystems.EventTrigger trigger = _ariseButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (trigger == null) trigger = _ariseButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                
                var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                pointerEnter.callback.AddListener((data) => { OnAriseHover(true); });
                trigger.triggers.Add(pointerEnter);

                var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                pointerExit.callback.AddListener((data) => { OnAriseHover(false); });
                trigger.triggers.Add(pointerExit);
            }

            if (_inputRootCanvasGroup != null) 
            {
                _inputRootCanvasGroup.alpha = 0f;
                _inputRootCanvasGroup.interactable = false;
                _inputRootCanvasGroup.blocksRaycasts = false;
            }

            if (_ariseButton != null) _ariseButton.interactable = false;

            RefreshHighlights();
        }
        #endregion

        #region Public Methods
        public void Open()
        {
            if (_visualRoot != null) _visualRoot.SetActive(true);
            if (_homeScreenRoot != null) _homeScreenRoot.SetActive(false);
            
            _hasSelectedClass = false;
            
            if (_nameInputField != null) _nameInputField.text = "";
            
            if (_ariseFillImage != null) _ariseFillImage.fillAmount = 0f;
            if (_ariseText != null) _ariseText.color = _ariseTextNormalColor;

            RefreshHighlights();

            if (_inputRootCanvasGroup != null) 
            {
                _inputRootCanvasGroup.alpha = 0f;
                _inputRootCanvasGroup.interactable = false;
                _inputRootCanvasGroup.blocksRaycasts = false;
            }
        }
        #endregion

        #region Private Methods
        private void OnClassSelected(MaouGender gender, string trueName)
        {
            _selectedGender = gender;
            _selectedTrueName = trueName;

            if (!_hasSelectedClass)
            {
                _hasSelectedClass = true;
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
            RefreshCardVisuals();
        }

        private void RefreshCardVisuals()
        {
            bool isTyrantChosen = _hasSelectedClass && _selectedGender == MaouGender.Male;
            bool isSovereignChosen = _hasSelectedClass && _selectedGender == MaouGender.Female;

            if (_tyrantSelectedHighlight != null) _tyrantSelectedHighlight.SetActive(isTyrantChosen);
            if (_sovereignSelectedHighlight != null) _sovereignSelectedHighlight.SetActive(isSovereignChosen);

            UpdateCardVisual(_tyrantButton, _tyrantVisual, isTyrantChosen, _hasSelectedClass, _tyrantHovering);
            UpdateCardVisual(_sovereignButton, _sovereignVisual, isSovereignChosen, _hasSelectedClass, _sovereignHovering);
        }

        private void UpdateCardVisual(Button cardButton, Image targetImage, bool isChosen, bool hasSelection, bool isHovering)
        {
            if (cardButton == null) return;
            
            // If no selection has been made, we want them both greyed out by default
            bool fullyVisible = (hasSelection && isChosen) || isHovering;
            Color targetColor = fullyVisible ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);

            if (targetImage != null)
            {
                targetImage.DOColor(targetColor, 0.2f);
            }
            
            float scale = isChosen ? 1.05f : (isHovering ? 1.02f : 1.0f);
            Vector3 targetScale = new Vector3(scale, scale, scale);
            cardButton.transform.DOScale(targetScale, 0.2f).SetEase(Ease.OutQuad);
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

        private void OnCardHover(MaouGender gender, bool isHovering)
        {
            if (gender == MaouGender.Male) _tyrantHovering = isHovering;
            else _sovereignHovering = isHovering;

            RefreshCardVisuals();
        }

        private void AddHoverEntry(GameObject target, UnityEngine.Events.UnityAction action, bool enter = true)
        {
            UnityEngine.EventSystems.EventTrigger trigger = target.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null) trigger = target.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry 
            { 
                eventID = enter ? UnityEngine.EventSystems.EventTriggerType.PointerEnter : UnityEngine.EventSystems.EventTriggerType.PointerExit 
            };
            entry.callback.AddListener((data) => action.Invoke());
            trigger.triggers.Add(entry);
        }

        private void OnAriseClicked()
        {
            if (_saveManager.CurrentData == null)
            {
                Debug.LogError("[AscensionPanel] SaveData is null! Cannot ascend. Is SaveManager initialized?");
                return;
            }

            _saveManager.CurrentData.PlayerName = _nameInputField.text.Trim();
            _saveManager.CurrentData.Gender = _selectedGender;
            _saveManager.CurrentData.TrueName = _selectedTrueName;
            
            if (_saveManager.CurrentData.UnlockedUnits == null) _saveManager.CurrentData.UnlockedUnits = new System.Collections.Generic.List<string>();
            if (!_saveManager.CurrentData.UnlockedUnits.Contains("Ignis")) _saveManager.CurrentData.UnlockedUnits.Add("Ignis");

            _saveManager.Save();
            
            if (_visualRoot != null) _visualRoot.SetActive(false);
            if (_homeScreenRoot != null) _homeScreenRoot.SetActive(true);
        }
        #endregion
    }
}
