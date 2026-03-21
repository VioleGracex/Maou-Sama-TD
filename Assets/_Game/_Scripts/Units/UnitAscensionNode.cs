using UnityEngine;

namespace MaouSamaTD.Units
{
    /// <summary>
    /// A single node in the unit's Resonance (Duplicate Unlock) tree.
    /// Each node is unlocked by consuming duplicate copies of the unit.
    /// </summary>
    [System.Serializable]
    public class UnitAscensionNode
    {
        [Tooltip("NODE TIER label, e.g. 'NODE TIER 01'")]
        public string TierLabel;

        [Tooltip("User-facing node name, e.g. 'SOVEREIGN HEART'")]
        public string NodeName;

        [Tooltip("Short description of the bonus/effect")]
        [TextArea(2, 4)]
        public string NodeDescription;

        [Tooltip("Number of duplicate copies required to unlock")]
        public int DuplicatesRequired = 1;

        [Tooltip("Optional icon for the node (e.g. awakening crown)")]
        public Sprite NodeIcon;

        [Tooltip("Is this an AWAKENING node (visually special)?")]
        public bool IsAwakening;
    }
}
