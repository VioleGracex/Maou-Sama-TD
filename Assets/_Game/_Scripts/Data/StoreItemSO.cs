using UnityEngine;

namespace MaouSamaTD.Data
{
    public enum StoreItemType
    {
        Currency,
        Skin,
        Gift
    }

    [CreateAssetMenu(fileName = "NewStoreItem", menuName = "MaouSamaTD/Shop/Store Item")]
    public class StoreItemSO : ScriptableObject
    {
        public string ItemName;
        [TextArea(3, 10)]
        public string Description;
        public StoreItemType Type;
        public float USDPrice;
        public int GemPrice;
        public Sprite Icon;
        public bool IsOfficialOffering;

        [Header("Specific Content")]
        public int CurrencyAmount;
        public string SkinID;
        public string GiftID;
    }
}
