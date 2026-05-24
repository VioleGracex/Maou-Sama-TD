using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Units;
using DG.Tweening;

namespace MaouSamaTD.UI
{
    public class EnemyInspectorUI : MonoBehaviour
    {
        [Header("UI References (Hierarchy Match)")]
        [SerializeField] private GameObject _panel; // Stats_BG_Panel
        [SerializeField] private TextMeshProUGUI _vassalStatsText; // Vassal_Stats_Txt
        [SerializeField] private TextMeshProUGUI _unitNameText; // Stats_Unit_Name_Txt
        [SerializeField] private TextMeshProUGUI _hpNumberText; // Stats_HP_Number_Txt
        [SerializeField] private Image _hpBarImage; // Stats_HPBar
        
        [SerializeField] private TextMeshProUGUI _dmgText; // Inside Stats_Dmg_BG
        [SerializeField] private TextMeshProUGUI _rangeText; // Inside Stats_Range_BG
        
        [Header("Range Shape")]
        [SerializeField] private RangePatternUI _rangePatternUI;

        [Header("New Stats Panel")]
        [SerializeField] private EnemyInspectorStatsPanel _statsPanel;
        
        [Header("Buttons")]
        [SerializeField] private Button _closeButton;
        
        private EnemyUnit _selectedEnemy;
        public event System.Action OnPanelHidden;
        public event System.Action<EnemyUnit> OnPanelShown;
        public bool IsLocked { get; set; } = false;

        public bool IsPanelActive => _panel != null && _panel.activeInHierarchy;
        public RectTransform PanelRect => _panel != null ? _panel.GetComponent<RectTransform>() : null;

        public void Init()
        {
            if (_panel == null)
            {
                Transform p = transform.Find("Stats_BG_Panel");
                if (p != null) _panel = p.gameObject;
            }

            if (_closeButton == null && _panel != null)
            {
                Transform cb = _panel.transform.Find("Close_Btn");
                if (cb != null) _closeButton = cb.GetComponent<Button>();
            }

            if (_statsPanel == null && _panel != null)
            {
                _statsPanel = _panel.GetComponentInChildren<EnemyInspectorStatsPanel>(true);
            }

            Canvas c = GetComponent<Canvas>();
            if (c != null)
            {
                c.overrideSorting = false;
                Destroy(GetComponent<GraphicRaycaster>());
                Destroy(c);
            }

            if (_panel != null)
            {
                _panel.transform.localScale = Vector3.zero;
                _panel.SetActive(false);
            }

            if (_closeButton) _closeButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            if (_closeButton) _closeButton.onClick.RemoveListener(Hide);
        }

        public void Show(EnemyUnit enemy)
        {
            if (_selectedEnemy != null) _selectedEnemy.SetHighlight(false, Color.white);

            _selectedEnemy = enemy;
            if (_selectedEnemy != null)
            {
                _selectedEnemy.SetHighlight(true, Color.red);
                UpdateVisuals();
                OnPanelShown?.Invoke(_selectedEnemy);
                if (_panel != null)
                {
                    _panel.transform.DOKill();
                    _panel.SetActive(true);
                    _panel.transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutBack).SetUpdate(true);
                }
            }
            else
            {
                Hide();
            }
        }

        public void Hide()
        {
            if (IsLocked) return;
            OnPanelHidden?.Invoke();

            if (_panel != null && _panel.activeSelf)
            {
                _panel.transform.DOKill();
                _panel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => 
                {
                    _panel.SetActive(false);
                    if (_selectedEnemy != null) _selectedEnemy.SetHighlight(false, Color.white);
                    _selectedEnemy = null;
                });
            }
            else
            {
                if (_selectedEnemy != null) _selectedEnemy.SetHighlight(false, Color.white);
                _selectedEnemy = null;
            }
        }

        private void Update()
        {
            if (_panel == null) return;
            if (_selectedEnemy != null && _panel.activeSelf)
            {
                UpdateVisuals();
            }
        }
        
        private void UpdateVisuals()
        {
            if (_selectedEnemy == null || _selectedEnemy.EnemyData == null) return;
            EnemyData data = _selectedEnemy.EnemyData;

            if (_unitNameText != null) _unitNameText.text = data.EnemyName;

            if (_statsPanel != null)
            {
                _statsPanel.Refresh(_selectedEnemy);
            }

            if (_hpNumberText != null) 
                _hpNumberText.text = $"{_selectedEnemy.CurrentHp}/{_selectedEnemy.MaxHp}";
            
            if (_hpBarImage != null && _selectedEnemy.MaxHp > 0)
                _hpBarImage.fillAmount = _selectedEnemy.CurrentHp / _selectedEnemy.MaxHp;

            if (_vassalStatsText != null)
            {
                 _vassalStatsText.text = $"Enemy Stats - {(data.IsBoss ? "Boss" : data.MovementType.ToString())}";
            }
            
            if (_dmgText != null) _dmgText.text = $"ATK: {_selectedEnemy.AttackPower}";
            if (_rangeText != null) _rangeText.text = $"{_selectedEnemy.Range}";

            if (_rangePatternUI != null)
            {
                _rangePatternUI.SetPattern(data.AttackPattern, (int)_selectedEnemy.Range);
            }
        }
    }
}
