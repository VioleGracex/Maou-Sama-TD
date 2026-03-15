using UnityEngine;
using System.Collections.Generic;

namespace MaouSamaTD.Data
{
    [CreateAssetMenu(fileName = "GachaPool", menuName = "MaouSamaTD/Gacha/Pool")]
    public class GachaPoolSO : ScriptableObject
    {
        [Header("Pool Definition")]
        [Header("Addressable Labels")]
        public string LabelLegendary = "Unit_Legendary";
        public string LabelMaster = "Unit_Master";
        public string LabelElite = "Unit_Elite";
        public string LabelRare = "Unit_Rare";
        public string LabelUncommon = "Unit_Uncommon";
        public string LabelCommon = "Unit_Common";

        public string GetLabelByRarity(MaouSamaTD.Units.UnitRarity rarity)
        {
            return rarity switch
            {
                MaouSamaTD.Units.UnitRarity.Legendary => LabelLegendary,
                MaouSamaTD.Units.UnitRarity.Master => LabelMaster,
                MaouSamaTD.Units.UnitRarity.Elite => LabelElite,
                MaouSamaTD.Units.UnitRarity.Rare => LabelRare,
                MaouSamaTD.Units.UnitRarity.Uncommon => LabelUncommon,
                MaouSamaTD.Units.UnitRarity.Common => LabelCommon,
                _ => LabelCommon
            };
        }
    }
}
