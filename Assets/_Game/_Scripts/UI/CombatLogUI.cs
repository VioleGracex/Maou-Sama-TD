using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaouSamaTD.Battle;
using System.Collections.Generic;
using Zenject;

namespace MaouSamaTD.UI
{
    public class CombatLogUI : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private int _maxDisplayLines = 50;

        private Queue<string> _displayLogs = new Queue<string>();

        private void Start()
        {
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
        }

        private void OnDestroy()
        {
            if (BattleLogManager.Instance != null)
            {
                BattleLogManager.Instance.OnEventLogged -= AddLogEntry;
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
