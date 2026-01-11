using UnityEngine;

namespace MaouSamaTD.Units
{
    [CreateAssetMenu(fileName = "NewUnitData", menuName = "MaouSamaTD/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("Identity")]
        public string UnitName;
        public Sprite UnitSprite; // Nullable, if null use Initials

        [Header("Class & Rules")]
        public UnitClass Class;
        public int DeploymentCost = 10;
        public int BlockCount = 1;

        [Header("Stats")]
        public float MaxHp = 100f;
        public float AttackPower = 10f;
        public float AttackInterval = 1f;
        public float Defense = 0f;
        public float Range = 1f; // Tiles
    }
}
