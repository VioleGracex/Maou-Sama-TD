using System;
using UnityEngine;

namespace MaouSamaTD.Data
{
    public enum RewardType
    {
        GoldCoins,
        BloodCrests,
        PlayerXP,
        UnitXP,
        Gems
    }

    [Serializable]
    public struct RewardData
    {
        public RewardType Type;
        public int Amount;
    }
}
