using UnityEngine;
using System;

namespace MaouSamaTD.Data
{
    [Serializable]
    public enum GachaCurrencyType
    {
        Gold,
        BloodCrest
    }

    [CreateAssetMenu(fileName = "GachaBanner", menuName = "MaouSamaTD/Gacha/Banner")]
    public class GachaBannerSO : ScriptableObject
    {
        [Header("Identity")]
        public string BannerID;
        public string BannerName;
        public Sprite BannerArt;
        public string Description;

        [Header("Cost")]
        public GachaCurrencyType Currency;
        public int SingleCost = 600;
        public int MultiCost = 6000;

        [Header("Rates (%)")]
        public float LegendaryRate = 2f;
        public float MasterRate = 8f;
        public float EliteRate = 50f;
        public float RareRate = 40f; 

        [Header("Pool")]
        public GachaPoolSO Pool;
        
        [Header("Guaranteed Features")]
        public bool HasPity = true;
        public int PityThreshold = 50;
    }
}
