using UnityEngine;

namespace MaouSamaTD.Data
{
    [CreateAssetMenu(fileName = "NewBloodCrestOffering", menuName = "MaouSamaTD/Shop/Blood Crest Offering")]
    public class BloodCrestOfferingSO : ScriptableObject
    {
        public string PackageName;
        [TextArea(3, 10)]
        public string Description;
        public int BloodCrestAmount;
        public float USDPrice;
        public Sprite PackageIcon;
        public bool IsOfficialOffering; // To match the "OFFICIAL OFFERING" label in ref
    }
}
