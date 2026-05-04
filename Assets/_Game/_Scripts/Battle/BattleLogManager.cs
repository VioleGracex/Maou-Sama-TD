using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Units;

namespace MaouSamaTD.Battle
{
    public enum BattleLogType
    {
        Damage,
        Heal,
        BuffApplied,
        BuffExpired,
        Death,
        WaveStart,
        System
    }

    [System.Serializable]
    public class BattleLogEntry
    {
        public float Timestamp;
        public BattleLogType Type;
        public string Source;
        public string Target;
        public string Message;
        public float Value;
    }

    /// <summary>
    /// Centralized logger for all combat and battle events.
    /// Used for debugging, post-game stats, and narrative triggers.
    /// </summary>
    public class BattleLogManager : MonoBehaviour
    {
        private static BattleLogManager _instance;
        public static BattleLogManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("BattleLogManager");
                    _instance = go.AddComponent<BattleLogManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private List<BattleLogEntry> _logs = new List<BattleLogEntry>();
        public IReadOnlyList<BattleLogEntry> Logs => _logs;

        [SerializeField] private int _maxLogs = 500;
        [SerializeField] private bool _logToConsole = true;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LogEvent(BattleLogType type, string source, string target, string message, float value = 0)
        {
            var entry = new BattleLogEntry
            {
                Timestamp = Time.time,
                Type = type,
                Source = source,
                Target = target,
                Message = message,
                Value = value
            };

            _logs.Add(entry);
            if (_logs.Count > _maxLogs) _logs.RemoveAt(0);

            if (_logToConsole)
            {
                string sourceStr = string.IsNullOrEmpty(source) ? "System" : source;
                string targetStr = string.IsNullOrEmpty(target) ? "" : $" -> {target}";
                Debug.Log($"<color=#ff6600>[CombatLog]</color> <b>{type}</b> | {sourceStr}{targetStr} | {message} ({value:F1})");
            }
        }

        public void ClearLogs()
        {
            _logs.Clear();
        }

        /// <summary>
        /// Returns all active buffs on a specific unit.
        /// </summary>
        public List<UnitBase.BuffInstance> GetActiveBuffsOnUnit(UnitBase unit)
        {
            if (unit == null) return new List<UnitBase.BuffInstance>();
            return unit.ActiveBuffs;
        }
    }
}
