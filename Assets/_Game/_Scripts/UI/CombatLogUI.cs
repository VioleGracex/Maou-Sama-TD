using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Battle;
using MaouSamaTD.Units;
using System.Collections.Generic;
using Zenject;
using DG.Tweening;

namespace MaouSamaTD.UI
{
    public class CombatLogUI : MonoBehaviour
    {
        [Header("Scroll References")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private int _maxDisplayLines = 50;

        [Header("Toggling & Animations")]
        [SerializeField] private GameObject _logPanel; // The main panel to show/hide
        [SerializeField] private Button _toggleButton; // The circular button
        [SerializeField] private TextMeshProUGUI _toggleIconText; // Text child of toggle button, can be "☰" or "✕"
        [SerializeField] private CanvasGroup _logCanvasGroup; // CanvasGroup for fade transition
        [SerializeField] private float _animationDuration = 0.25f;

        private Queue<string> _displayLogs = new Queue<string>();
        private bool _isExpanded = false;
        private bool _wasExpandedBeforeInspector = false;
        private UnitInspectorUI _inspectorUI;

        private void Start()
        {
            // Ensure the parent CanvasGroup is fully visible and interactable so the toggle button is shown and clickable
            CanvasGroup parentGroup = GetComponent<CanvasGroup>();
            if (parentGroup != null)
            {
                parentGroup.alpha = 1f;
                parentGroup.interactable = true;
                parentGroup.blocksRaycasts = true;
            }

            // Dynamically assign or add CanvasGroup specifically on _logPanel so we don't hide the toggle button
            if (_logPanel != null)
            {
                _logCanvasGroup = _logPanel.GetComponent<CanvasGroup>();
                if (_logCanvasGroup == null)
                {
                    _logCanvasGroup = _logPanel.AddComponent<CanvasGroup>();
                }
            }

            // Set up initial toggle listener
            if (_toggleButton != null)
            {
                _toggleButton.onClick.AddListener(ToggleLog);
            }

            // Initialize panel visibility state
            SetExpanded(false, false);

            if (BattleLogManager.Instance != null)
            {
                BattleLogManager.Instance.OnEventLogged += AddLogEntry;
                
                // Load existing logs if any
                foreach (var log in BattleLogManager.Instance.Logs)
                {
                    AddLogEntry(log);
                }
            }
            
            UpdateText();

            // Find and subscribe to UnitInspectorUI events for overlap prevention
            _inspectorUI = FindFirstObjectByType<UnitInspectorUI>();
            if (_inspectorUI != null)
            {
                _inspectorUI.OnPanelShown += OnInspectorShown;
                _inspectorUI.OnPanelHidden += OnInspectorHidden;
            }
        }

        private void OnDestroy()
        {
            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveListener(ToggleLog);
            }

            if (BattleLogManager.Instance != null)
            {
                BattleLogManager.Instance.OnEventLogged -= AddLogEntry;
            }

            if (_inspectorUI != null)
            {
                _inspectorUI.OnPanelShown -= OnInspectorShown;
                _inspectorUI.OnPanelHidden -= OnInspectorHidden;
            }
        }

        public void ToggleLog()
        {
            SetExpanded(!_isExpanded, true);
        }

        public void SetExpanded(bool expanded, bool animate = true)
        {
            _isExpanded = expanded;

            if (_logPanel != null)
            {
                _logPanel.transform.DOKill();
                _logCanvasGroup?.DOKill();

                if (expanded)
                {
                    _logPanel.SetActive(true);
                    if (animate)
                    {
                        _logPanel.transform.localScale = Vector3.zero;
                        _logPanel.transform.DOScale(Vector3.one, _animationDuration).SetEase(Ease.OutBack).SetUpdate(true);
                        if (_logCanvasGroup != null)
                        {
                            _logCanvasGroup.alpha = 0f;
                            _logCanvasGroup.DOFade(1f, _animationDuration).SetUpdate(true);
                        }
                    }
                    else
                    {
                        _logPanel.transform.localScale = Vector3.one;
                        if (_logCanvasGroup != null) _logCanvasGroup.alpha = 1f;
                    }

                    if (_toggleIconText != null) _toggleIconText.text = "✕";
                }
                else
                {
                    if (animate)
                    {
                        _logPanel.transform.DOScale(Vector3.zero, _animationDuration).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
                        {
                            _logPanel.SetActive(false);
                        });
                        _logCanvasGroup?.DOFade(0f, _animationDuration).SetUpdate(true);
                    }
                    else
                    {
                        _logPanel.transform.localScale = Vector3.zero;
                        _logPanel.SetActive(false);
                        if (_logCanvasGroup != null) _logCanvasGroup.alpha = 0f;
                    }

                    if (_toggleIconText != null) _toggleIconText.text = "☰";
                }
            }
        }

        private void OnInspectorShown(PlayerUnit unit)
        {
            // If we are currently expanded, remember that and minimize/hide
            _wasExpandedBeforeInspector = _isExpanded;
            if (_isExpanded)
            {
                SetExpanded(false, true);
            }

            // Hide the toggle button as well so that the Inspector has full screen estate and there is no overlap
            if (_toggleButton != null)
            {
                _toggleButton.gameObject.SetActive(false);
            }
        }

        private void OnInspectorHidden()
        {
            // Restore toggle button
            if (_toggleButton != null)
            {
                _toggleButton.gameObject.SetActive(true);
            }

            // Restore expanded state if we were expanded
            if (_wasExpandedBeforeInspector)
            {
                SetExpanded(true, true);
                _wasExpandedBeforeInspector = false;
            }
        }

        private void AddLogEntry(BattleLogEntry entry)
        {
            string color = GetColorForType(entry.Type);
            string timestamp = $"[{TimeSpanToFormatted(entry.Timestamp)}]";
            string logLine = $"<color={color}>{timestamp} <b>{entry.Source}</b>: {entry.Message}</color>";
            
            _displayLogs.Enqueue(logLine);
            if (_displayLogs.Count > _maxDisplayLines) _displayLogs.Dequeue();
            
            UpdateText();
            Canvas.ForceUpdateCanvases();
            if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 0f;
        }

        private void UpdateText()
        {
            if (_logText != null)
            {
                _logText.text = string.Join("\n", _displayLogs);
            }
        }

        private string GetColorForType(BattleLogType type)
        {
            switch (type)
            {
                case BattleLogType.Damage: return "#ff4d4d"; // Red
                case BattleLogType.Heal: return "#4dff4d"; // Green
                case BattleLogType.BuffApplied: return "#4da6ff"; // Blue
                case BattleLogType.BuffExpired: return "#808080"; // Gray
                case BattleLogType.Death: return "#cc0000"; // Dark Red
                case BattleLogType.WaveStart: return "#ffff4d"; // Yellow
                case BattleLogType.System: return "#ffffff"; // White
                default: return "#ffffff";
            }
        }

        private string TimeSpanToFormatted(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60F);
            int seconds = Mathf.FloorToInt(time % 60F);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}
