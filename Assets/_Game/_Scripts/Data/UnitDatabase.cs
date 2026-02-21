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
            return AllUnits.Find(u => u.UnitID == id);
        }
    }
}