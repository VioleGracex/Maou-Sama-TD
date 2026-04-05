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
        [System.Serializable]
        public struct RuntimeStats
        {
            public float MaxHp;
            public float Attack;
            public float Defense;
            public Sprite ClassIcon;
            public string ClassName;
        }

        [Header("Runtime Cache (Source of Truth)")]
        public RuntimeStats CalculatedStats;

        [Header("Identity (Data)")]
        public string UnitName;
        public string UnitTitle;

        [System.Serializable]
        public class SkinData
        {
            public string SkinID;         // Unique ID for save/overrides (e.g. "bikini_01")
            public string SkinThemeName;  // Display Name
            public string SeriesName;

            public Sprite Avatar;         // UI Square Icon (Headshot)
            public Sprite Chibi;          // Battle Render (In-Game Idle)
            public Sprite WaistUp;        // Portrait / Inspector View
            public Sprite FullSplashArt;  // Full Background Art
            public Sprite FullBodyCutout; // Full Body Cutout

            public RuntimeAnimatorController AnimatorController;

            public bool IsDefault;
            public int UnlockCost;
            public bool IsPremium;
        }

        [System.Serializable]
        public struct SkinFields
        {
            public Sprite Avatar;         // UI Square Icon (Headshot)
            public Sprite Chibi;          // Battle Render (In-Game Idle)
            public Sprite WaistUp;        // Portrait / Inspector View
            public Sprite FullSplashArt;  // Full Background Art
            public Sprite FullBodyCutout; // Full Body Cutout
            public RuntimeAnimatorController AnimatorController;
        }

        [Header("Base Skin (Required)")]
        public SkinFields BaseSkin;
        public Sprite Rank2Art; // Elite art style - Only available for Base skin

        [Header("Skins Collection")]
        public System.Collections.Generic.List<SkinData> Skins = new System.Collections.Generic.List<SkinData>();
        
        [SerializeField] private string _equippedSkinID; // Persisted ID. Empty means Base.

        public enum UnitImageType { Avatar, Chibi, WaistUp, SplashArt, FullSprite }

        [Header("UI Selection")]
        public UnitImageType CardSlotImageType = UnitImageType.WaistUp;
        public UnitImageType ButtonImageType = UnitImageType.Avatar;

        public SkinData GetSkinByID(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return Skins.Find(s => s.SkinID == id);
        }

        public Sprite GetSprite(UnitImageType type)
        {
            var equippedSkin = GetSkinByID(_equippedSkinID);

            // If we have an equipped skin, get from it
            if (equippedSkin != null)
            {
                return type switch
                {
                    UnitImageType.Avatar => equippedSkin.Avatar,
                    UnitImageType.Chibi => equippedSkin.Chibi,
                    UnitImageType.WaistUp => equippedSkin.WaistUp,
                    UnitImageType.SplashArt => equippedSkin.FullSplashArt,
                    UnitImageType.FullSprite => equippedSkin.FullBodyCutout,
                    _ => equippedSkin.Avatar
                };
            }

            // Otherwise get from Base (considering Rank 2 art if applicable)
            if (type == UnitImageType.WaistUp && StarRating >= 4 && Rank2Art != null)
                return Rank2Art;

            return type switch
            {
                UnitImageType.Avatar => BaseSkin.Avatar,
                UnitImageType.Chibi => BaseSkin.Chibi,
                UnitImageType.WaistUp => BaseSkin.WaistUp,
                UnitImageType.SplashArt => BaseSkin.FullSplashArt,
                UnitImageType.FullSprite => BaseSkin.FullBodyCutout,
                _ => BaseSkin.Avatar
            };
        }

        public RuntimeAnimatorController GetAnimatorController()
        {
            var equippedSkin = GetSkinByID(_equippedSkinID);
            if (equippedSkin != null && equippedSkin.AnimatorController != null)
                return equippedSkin.AnimatorController;
            
            return BaseSkin.AnimatorController;
        }

        [Header("Progression")]
        public int Level = 1;
        public int Experience = 0;
        public int StarRating = 1; // 1 to 6 Stars
        public float Amity = 0f; // New field for Amity % (Bond)
        public int SkillLevel = 1; // Global skill level for the unit
        public float BaseStatMultiplier = 1.0f; // Permanent boost from advancements
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
        public float UltimateDamageResistance = 0f; // Percentage reduction (0-1)

        [Header("Skills")]
        public MaouSamaTD.Skills.UnitSkillData PassiveSkill;
        public MaouSamaTD.Skills.UnitSkillData ActiveSkill;
        public MaouSamaTD.Skills.UnitSkillData UltimateSkill;

        [Header("Resonance (Duplicate Unlock Nodes)")]
        public System.Collections.Generic.List<UnitAscensionNode> AscensionNodes =
            new System.Collections.Generic.List<UnitAscensionNode>();

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
            return GetSprite(UnitImageType.WaistUp);
        }

        [Header("Unlock Tracking")]
        [SerializeField] private System.Collections.Generic.List<string> _unlockedSkinIDs = new System.Collections.Generic.List<string>();

        public string EquippedSkinID 
        {
            get => _equippedSkinID;
            set => _equippedSkinID = value;
        }

        /// <summary>
        /// Checks if a specific skin is owned/unlocked by the user.
        /// </summary>
        public bool IsSkinUnlocked(string skinID)
        {
            if (string.IsNullOrEmpty(skinID)) return true; // Base is always unlocked
            
            // Check if skin exists and is default
            var skin = GetSkinByID(skinID);
            if (skin != null && skin.IsDefault) return true;

            return _unlockedSkinIDs.Contains(skinID);
        }

        /// <summary>
        /// Unlocks a skin for this unit.
        /// </summary>
        public void UnlockSkin(string skinID)
        {
            if (!string.IsNullOrEmpty(skinID) && !_unlockedSkinIDs.Contains(skinID))
                _unlockedSkinIDs.Add(skinID);
        }

        public void AdvanceStar()
        {
            if (StarRating >= 6) return;
            StarRating++;
            Level = 1;
            Experience = 0;
            BaseStatMultiplier += 0.2f; // Permanent 20% boost per star advancement

            // Trigger stat refresh if we have a global scaling source
            if (MaouSamaTD.Core.AppEntryPoint.LoadedScalingData != null)
                RefreshStats(MaouSamaTD.Core.AppEntryPoint.LoadedScalingData);
        }

        public void RefreshStats(ClassScalingData scaling)
        {
            if (scaling == null) return;

            CalculatedStats.ClassName = Class.ToString();
            
            if (scaling.TryGetMultipliers(Class, out var classMults))
            {
                CalculatedStats.ClassIcon = classMults.ClassIcon;
                if (!string.IsNullOrEmpty(classMults.OverrideClassName))
                    CalculatedStats.ClassName = classMults.OverrideClassName;

                // Base Multiply
                float baseHp = MaxHp * classMults.BaseHpMultiplier;
                float baseAtk = AttackPower * classMults.BaseAtkMultiplier;
                float baseDef = Defense * classMults.BaseDefMultiplier;

                // Growth
                float hpGrowth = 0, atkGrowth = 0, defGrowth = 0;
                scaling.TryGetGrowth(Class, Rarity, out hpGrowth, out atkGrowth, out defGrowth);

                // Level-1 (Growth is per level AFTER 1)
                int levelsGained = Mathf.Max(0, Level - 1);
                
                CalculatedStats.MaxHp = (baseHp + (hpGrowth * levelsGained)) * BaseStatMultiplier;
                CalculatedStats.Attack = (baseAtk + (atkGrowth * levelsGained)) * BaseStatMultiplier;
                CalculatedStats.Defense = (baseDef + (defGrowth * levelsGained)) * BaseStatMultiplier;
            }
            else
            {
                // Fallback to base
                CalculatedStats.MaxHp = MaxHp * BaseStatMultiplier;
                CalculatedStats.Attack = AttackPower * BaseStatMultiplier;
                CalculatedStats.Defense = Defense * BaseStatMultiplier;
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (StarRating < 1) StarRating = 1;
            if (StarRating > 6) StarRating = 6;

            // Recalculate stats in editor if we have a global reference
            if (MaouSamaTD.Core.AppEntryPoint.LoadedScalingData != null)
                RefreshStats(MaouSamaTD.Core.AppEntryPoint.LoadedScalingData);
        }
    }
}
