using System;
using UnityEngine;

namespace MaouSamaTD.Data
{
    public enum RewardType
    {
        GoldCoins,
        BloodCrests // Currency used for gacha/rituals
    }

    [Serializable]
    public struct RewardData
    {
        public RewardType Type;
        public int Amount;
    }
}
