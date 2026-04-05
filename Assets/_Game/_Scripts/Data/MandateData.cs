using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaouSamaTD.Data
{
    public enum MandateType
    {
        Daily,
        Weekly,
        StoryAndLegacy,
        Event 
    }

    [CreateAssetMenu(fileName = "NewMandate", menuName = "MaouSamaTD/Mandate Data")]
    public class MandateData : MaouSamaTD.Core.GameDataSO
    {
        [Header("Identity")]
        public string Title;
        [TextArea] public string Description;
        public MandateType Type;

        [Header("Requirements")]
        public string RequirementKey; // e.g. "KillEnemies", "WinLevels"
        public int RequiredAmount;
        
        [Header("Rewards")]
        public List<RewardData> Rewards = new List<RewardData>();

        [Header("Visuals")]
        public Sprite Icon;

        // Metadata for Page Flow
        public bool IsLimitedTime => Type == MandateType.Event;
        
        public string CategoryTag => Type == MandateType.StoryAndLegacy ? "STORY & LEGACY" : Type.ToString().ToUpper();
    }
}
