using UnityEngine;
using System.Collections.Generic;
using MaouSamaTD.Skills;

namespace MaouSamaTD.Data
{
    [CreateAssetMenu(fileName = "SovereignRiteDatabase", menuName = "MaouSamaTD/SovereignRiteDatabase")]
    public class SovereignRiteDatabase : ScriptableObject
    {
        public List<SovereignRiteData> AllRites = new List<SovereignRiteData>();

        public SovereignRiteData GetRiteByID(string id)
        {
            if (string.IsNullOrEmpty(id) || AllRites == null) return null;
            
            return AllRites.Find(r => r != null && (
                r.name == id || 
                (r.SkillName != null && r.SkillName.Equals(id, System.StringComparison.OrdinalIgnoreCase)) || 
                (r.Tag != null && r.Tag.Equals(id, System.StringComparison.OrdinalIgnoreCase)) ||
                r.name.Replace("_Male", "").Replace("_Female", "").Equals(id, System.StringComparison.OrdinalIgnoreCase)
            ));
        }
    }
}
