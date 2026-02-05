using UnityEngine;

namespace MaouSamaTD.Units
{
    public enum UnitRarity
    {
        Common, // 1 Star
        Uncommon, // 2 Star
        Rare, // 3 Star
        Elite, // 4 Star
        Master, // 5 Star
        Legendary // 6 Star
    }

    [CreateAssetMenu(fileName = "NewUnitData", menuName = "MaouSamaTD/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("Identity")]
        public string UnitName;
        public Sprite UnitSprite; // Nullable, if null use Initials
        public Sprite UnitIcon;   // Specific icon for UI buttons

        [Header("Class & Rules")]
        public UnitRarity Rarity;
        public UnitClass Class;
        public AttackPattern AttackPattern;
        public AttackType AttackType;
        public int DeploymentCost = 10;
        public int BlockCount = 1;

        [Header("Stats")]
        public float MaxHp = 100f;
        public float AttackPower = 10f;
        public float AttackInterval = 1f;
        public float Defense = 0f;
        public float RespawnTime = 5f; // Seconds
        public float Range = 1f; // Tiles
        public float MaxCharge = 100f;
        public float ChargePerSecond = 5f;
        public float ChargePerAttack = 10f;

        [Header("Skill")]
        public MaouSamaTD.Skills.UnitSkillData Skill;

        [Header("Placement Rules")]
        public System.Collections.Generic.List<Grid.TileType> ViableTiles;

        private void OnValidate()
        {
            // Set defaults if empty based on class
            if (ViableTiles == null || ViableTiles.Count == 0)
            {
                ViableTiles = new System.Collections.Generic.List<Grid.TileType>();
                if (Class == UnitClass.Melee) ViableTiles.Add(Grid.TileType.Walkable);
                else if (Class == UnitClass.Ranged) ViableTiles.Add(Grid.TileType.HighGround);
                // Healer might go anywhere or specific? Default to HighGround for now
                else if (Class == UnitClass.Healer) ViableTiles.Add(Grid.TileType.HighGround);
            }
        }
    }
}
