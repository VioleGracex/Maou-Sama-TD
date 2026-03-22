using UnityEngine;

namespace MaouSamaTD.Units
{
    /// <summary>
    /// Holds multi-crop art and metadata for a specific unit skin.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnitSkinData", menuName = "MaouSamaTD/Unit Skin Data")]
    public class UnitSkinData : MaouSamaTD.Core.GameDataSO
    {
        [Header("Skin Info")]
        public string SkinName;
        public string BrandName;
        public string SeriesName;

        [Header("Art Crops")]
        public Sprite Chibi;        // Battle Render (Idle/Animation)
        public Sprite Icon;         // UI Square Icon
        public Sprite WaistUp;      // Portrait / Inspector View
        public Sprite SplashArt;    // Full Background Art
        public Sprite FullBody;     // Full Body Cutout

        [Header("Unlock Status")]
        public bool IsDefault;
        public int UnlockCost;
        public bool IsPremium;
    }
}
