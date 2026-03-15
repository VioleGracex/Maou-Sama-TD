using UnityEngine;
using System;
using NaughtyAttributes;

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
        [ReadOnly]
        public string BannerID;
        public string BannerName;
        public Sprite BannerArt;
        public Sprite TabImage;
        [TextArea(2, 5)]
        public string Description;

        [Header("Cost")]
        public GachaCurrencyType Currency;
        public int SingleCost = 160;
        public int MultiCost = 1600;

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

        [Header("Featured Unit")]
        public string FeaturedUnitName;
        public string FeaturedUnitTitle;

        [Header("Details Panel")]
        [ResizableTextArea]
        public string DetailedDescription;
        
        [Header("Rates Description")]
        [ResizableTextArea]
        public string ProbabilityDetails;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(BannerID))
            {
                BannerID = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }
    }
}
