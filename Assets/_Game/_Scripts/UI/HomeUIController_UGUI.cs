using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MaouSamaTD.Managers;
using MaouSamaTD.Units;
using Zenject;
using System.Linq;
using TMPro;
using MaouSamaTD.Data;

/// <summary>
/// UGUI-based controller for the homepage character and UI toggles.
/// Attach this to a Canvas or a root GameObject in the Home scene.
/// </summary>
public class HomeUIController_UGUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Character References")]
    [SerializeField] private Image _characterImage;
    [SerializeField] private RectTransform _characterRect;

    [Header("UI Roots")]
    [SerializeField] private GameObject _mainUIRoot;
    [SerializeField] private GameObject _editModeUIRoot;
    [SerializeField] private GameObject _hideUICatcher; // A full-screen invisible button/panel

    [Header("Buttons")]
    [SerializeField] private Button _btnHideUI;
    [SerializeField] private Button _btnEditMode;
    [SerializeField] private Button _btnResetPos;
    [SerializeField] private Button _btnConfirmEdit;
    [SerializeField] private Button _btnSavePreset;
    [SerializeField] private Button _btnNextPreset;
    [SerializeField] private Button _btnPrevPreset;
    [SerializeField] private Button _btnScaleUp;
    [SerializeField] private Button _btnScaleDown;
    [SerializeField] private TextMeshProUGUI _txtPresetName;

    [Inject] private SaveManager _saveManager;

    private bool _isEditMode = false;
    private Canvas _canvas;

    private void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        
        // Hook up buttons
        if (_btnHideUI != null) _btnHideUI.onClick.AddListener(HideUI);
        if (_btnEditMode != null) _btnEditMode.onClick.AddListener(ToggleEditMode);
        if (_btnConfirmEdit != null) _btnConfirmEdit.onClick.AddListener(ToggleEditMode);
        if (_btnResetPos != null) _btnResetPos.onClick.AddListener(ResetPosition);
        
        if (_btnSavePreset != null) _btnSavePreset.onClick.AddListener(SaveToPreset);
        if (_btnNextPreset != null) _btnNextPreset.onClick.AddListener(() => CyclePreset(1));
        if (_btnPrevPreset != null) _btnPrevPreset.onClick.AddListener(() => CyclePreset(-1));
        if (_btnScaleUp != null) _btnScaleUp.onClick.AddListener(() => AdjustScale(0.05f));
        if (_btnScaleDown != null) _btnScaleDown.onClick.AddListener(() => AdjustScale(-0.05f));
        
        // Catcher for unhiding
        if (_hideUICatcher != null)
        {
            var btn = _hideUICatcher.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(ShowUI);
        }

        LoadHomeSettings();
        UpdateInteractability();
    }

    private void LoadHomeSettings()
    {
        if (_saveManager == null || _saveManager.CurrentData == null) return;

        var data = _saveManager.CurrentData;
        var settings = data.CurrentHomeSettings;
        
        // Load Character Image
        string unitID = settings.SelectedUnitID;
        UnitData unitData = Resources.FindObjectsOfTypeAll<UnitData>().FirstOrDefault(u => u.name.Contains(unitID) || u.UnitName == unitID);
        
        if (unitData != null && _characterImage != null)
        {
            _characterImage.sprite = unitData.GetCurrentVisualArt();
            _characterImage.SetNativeSize();
        }

        // Apply saved position
        if (_characterRect != null)
        {
            _characterRect.anchoredPosition = settings.Position;
            _characterRect.localScale = new Vector3(settings.Scale, settings.Scale, 1);
        }

        if (_txtPresetName != null)
        {
            _txtPresetName.text = $"Preset: {settings.PresetName} ({data.ActivePresetIndex + 1}/{data.HomePresets.Count})";
        }
    }

    public void HideUI()
    {
        if (_mainUIRoot != null) _mainUIRoot.SetActive(false);
        if (_hideUICatcher != null) _hideUICatcher.SetActive(true);
        _btnHideUI.gameObject.SetActive(false);
        _btnEditMode.gameObject.SetActive(false);
    }

    public void ShowUI()
    {
        if (_mainUIRoot != null) _mainUIRoot.SetActive(true);
        if (_hideUICatcher != null) _hideUICatcher.SetActive(false);
        _btnHideUI.gameObject.SetActive(true);
        _btnEditMode.gameObject.SetActive(true);
    }

    public void ToggleEditMode()
    {
        _isEditMode = !_isEditMode;
        
        if (_editModeUIRoot != null) _editModeUIRoot.SetActive(_isEditMode);
        if (_mainUIRoot != null) _mainUIRoot.SetActive(!_isEditMode);

        UpdateInteractability();

        if (!_isEditMode)
        {
            SaveToPreset();
        }
    }

    private void UpdateInteractability()
    {
        // Only allow dragging the character when in edit mode
        if (_characterImage != null)
        {
            _characterImage.raycastTarget = _isEditMode;
        }
    }

    public void ResetPosition()
    {
        if (_characterRect != null)
        {
            _characterRect.anchoredPosition = Vector2.zero;
            _characterRect.localScale = Vector3.one;
        }
        SaveToPreset();
    }

    private void CyclePreset(int delta)
    {
        if (_saveManager == null || _saveManager.CurrentData == null) return;

        var data = _saveManager.CurrentData;
        int count = data.HomePresets.Count;
        if (count == 0) return;

        data.ActivePresetIndex = (data.ActivePresetIndex + delta + count) % count;
        LoadHomeSettings();
    }

    private void AdjustScale(float delta)
    {
        if (_characterRect == null) return;
        float s = Mathf.Clamp(_characterRect.localScale.x + delta, 0.2f, 3.0f);
        _characterRect.localScale = new Vector3(s, s, 1);
    }

    private void SaveToPreset()
    {
        if (_saveManager == null || _saveManager.CurrentData == null || _characterRect == null) return;

        var settings = _saveManager.CurrentData.CurrentHomeSettings;
        settings.Position = _characterRect.anchoredPosition;
        settings.Scale = _characterRect.localScale.x;
        _saveManager.Save();
        
        Debug.Log($"[HomeUI_UGUI] Saved to preset '{settings.PresetName}': {settings.Position}");
        
        if (_txtPresetName != null)
        {
            _txtPresetName.text = $"Preset: {settings.PresetName} ({_saveManager.CurrentData.ActivePresetIndex + 1}/{_saveManager.CurrentData.HomePresets.Count})";
        }
    }

    #region Drag Implementation
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_isEditMode) return;
        Debug.Log("[HomeUI_UGUI] Began dragging character.");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isEditMode || _characterRect == null || _canvas == null) return;

        // Move by delta, taking canvas scale into account
        _characterRect.anchoredPosition += eventData.delta / _canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isEditMode) return;
        Debug.Log("[HomeUI_UGUI] Ended dragging character.");
    }
    #endregion
}
