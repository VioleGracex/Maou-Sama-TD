using UnityEngine;
using MaouSamaTD.Levels;

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
        public RuntimeAnimatorController AnimatorController;

        [Header("Progression")]
        public int Level = 1;
        public int Experience = 0;
        public int StarRating = 1; // 1 to 6 Stars
        public long AcquisitionDate;

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
        public float Resistance = 0f;
        public float RespawnTime = 5f; // Seconds
        public float Range = 1f; // Tiles
        public float MaxCharge = 100f;
        public float ChargePerSecond = 5f;
        public float ChargePerAttack = 10f;

        [Header("Skill")]
        public MaouSamaTD.Skills.UnitSkillData Skill;

        [Header("Resonance (Duplicate Unlock Nodes)")]
        public System.Collections.Generic.List<UnitAscensionNode> AscensionNodes =
            new System.Collections.Generic.List<UnitAscensionNode>();

        [Header("Skins & Rank Art")]
        public Sprite Rank2Art; // "Elite" art style
        public System.Collections.Generic.List<Sprite> AlternateSkins =
            new System.Collections.Generic.List<Sprite>();
        public Sprite EquippedSkin;

        [Header("Placement Rules")]
        [SerializeField] private System.Collections.Generic.List<Levels.TileType> _viableTiles;
        public System.Collections.Generic.List<Levels.TileType> ViableTiles
        {
            get
            {
                if (_viableTiles == null || _viableTiles.Count == 0)
                {
                    _viableTiles = new System.Collections.Generic.List<Levels.TileType>();
                    if (Class == UnitClass.Ranger || Class == UnitClass.Warlock || Class == UnitClass.Sage || Class == UnitClass.Support)
                        _viableTiles.Add(TileType.HighGround);
                    else
                        _viableTiles.Add(TileType.Walkable);
                }
                return _viableTiles;
            }
            set => _viableTiles = value;
        }

        public int MaxLevel => GetMaxLevelForStars(StarRating);

        public static int GetMaxLevelForStars(int stars)
        {
            return stars switch
            {
                1 => 20,
                2 => 30,
                3 => 45,
                4 => 60,
                5 => 80,
                6 => 90,
                _ => 20
            };
        }

        public Sprite GetCurrentVisualArt()
        {
            if (EquippedSkin != null) return EquippedSkin;
            if (StarRating >= 4 && Rank2Art != null) return Rank2Art; // E.g., Rank 2 art unlocks at 4 stars
            return UnitSprite;
        }

        public void AdvanceStar()
        {
            if (StarRating >= 6) return;
            StarRating++;
            Level = 1;
            Experience = 0;
            // Base stats increase logic can be handled by ProgressionLogic or local modifiers
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (StarRating < 1) StarRating = 1;
            if (StarRating > 6) StarRating = 6;
        }
    }
}
