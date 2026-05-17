using UnityEngine;

namespace MaouSamaTD.Data
{
    [CreateAssetMenu(fileName = "NewItemConfig", menuName = "MaouSamaTD/Item Config")]
    public class ItemConfigSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("The unique ID used in the SaveManager (e.g., 'xp_core_common')")]
        public string ItemID;
        public string ItemName;
        [TextArea] public string Description;

        [Header("Visuals")]
        public Sprite ItemIcon;
        public Color BackgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        public Color TextColor = Color.white;

        [Header("Progression Data (Optional)")]
        [Tooltip("Amount of XP or currency this item provides.")]
        public int ValueAmount;
    }
}
