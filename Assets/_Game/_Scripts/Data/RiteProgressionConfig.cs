using UnityEngine;
using System.Collections.Generic;

namespace MaouSamaTD.Data
{
    [CreateAssetMenu(fileName = "RiteProgressionConfig", menuName = "MaouSamaTD/Rite Progression Config")]
    public class RiteProgressionConfig : ScriptableObject
    {
        [Tooltip("Female Rites unlocked by default for a new save game.")]
        public List<MaouSamaTD.Skills.SovereignRiteData> DefaultFemaleRites = new List<MaouSamaTD.Skills.SovereignRiteData>();
        
        [Tooltip("Male Rites unlocked by default for a new save game.")]
        public List<MaouSamaTD.Skills.SovereignRiteData> DefaultMaleRites = new List<MaouSamaTD.Skills.SovereignRiteData>();

        private void OnValidate()
        {
            if (DefaultFemaleRites != null)
            {
                var hash = new System.Collections.Generic.HashSet<MaouSamaTD.Skills.SovereignRiteData>();
                for (int i = DefaultFemaleRites.Count - 1; i >= 0; i--)
                {
                    if (DefaultFemaleRites[i] == null) continue;
                    if (!hash.Add(DefaultFemaleRites[i])) DefaultFemaleRites.RemoveAt(i);
                }
            }

            if (DefaultMaleRites != null)
            {
                var hash = new System.Collections.Generic.HashSet<MaouSamaTD.Skills.SovereignRiteData>();
                for (int i = DefaultMaleRites.Count - 1; i >= 0; i--)
                {
                    if (DefaultMaleRites[i] == null) continue;
                    if (!hash.Add(DefaultMaleRites[i])) DefaultMaleRites.RemoveAt(i);
                }
            }
        }
    }
}
