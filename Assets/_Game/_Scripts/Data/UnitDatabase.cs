using UnityEngine;
using System.Collections.Generic;

namespace MaouSamaTD.Data
{
    [CreateAssetMenu(fileName = "UnitDatabase", menuName = "MaouSamaTD/UnitDatabase")]
    public class UnitDatabase : ScriptableObject
    {
        public List<MaouSamaTD.Units.UnitData> AllUnits;

        public MaouSamaTD.Units.UnitData GetUnitByID(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            
            return AllUnits.Find(u => 
                (u.UniqueID == id) || 
                (u.name == id) || 
                (u.UnitName == id) ||
                (u.name.Replace("Char_", "").Replace("_UnitData", "") == id)
            );
        }
    }
}