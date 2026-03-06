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

    public enum DamageType
    {
        Melee,
        Ranged,
        Magic
    }

    [CreateAssetMenu(fileName = "NewUnitData", menuName = "MaouSamaTD/Unit Data")]
    public class UnitData : MaouSamaTD.Core.GameDataSO
    {
        [Header("Identity (Data)")]
        public string UnitName;
        public string UnitTitle;
        public Sprite UnitSprite; // Nullable, if null use Initials
        public Sprite UnitIcon;   // Specific icon for UI buttons

        [Header("Progression")]
        public int Level = 1;
        public long AcquisitionDate; // Tick for sorting by age

        [Header("Class & Rules")]
        public UnitRarity Rarity;
        public UnitClass Class;
        public AttackPattern AttackPattern;
        public AttackType AttackType;
        public DamageType DamageType;
        public int DeploymentCost = 10;
        public int BlockCount = 1;
        public System.Collections.Generic.List<Vector2Int> CustomPatternOffsets = new System.Collections.Generic.List<Vector2Int>();

        [Header("Stats Base")]
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

        protected override void OnValidate()
        {
            base.OnValidate();

            // Set defaults if empty based on class logic (expanded for new 11 classes)
            if (ViableTiles == null || ViableTiles.Count == 0)
            {
                ViableTiles = new System.Collections.Generic.List<Grid.TileType>();
                if (Class == UnitClass.Ranger || Class == UnitClass.Warlock || Class == UnitClass.Sage || Class == UnitClass.Support)
                {
                    ViableTiles.Add(Grid.TileType.HighGround);
                }
                else
                {
                    // Bastion, Vanguard, Executioner, etc.
                    ViableTiles.Add(Grid.TileType.Walkable);
                }
            }
        }
    }
}
