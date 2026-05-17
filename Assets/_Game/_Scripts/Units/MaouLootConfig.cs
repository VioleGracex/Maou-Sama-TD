using System.Collections.Generic;
using UnityEngine;

namespace MaouSamaTD.Units
{
    [System.Serializable]
    public class CategoryDropSettings
    {
        public EnemyCategory Category;
        public string PrimaryMaterialID;
        
        [Header("Normal Enemy Drop Rates")]
        [Range(0f, 1f)] public float NormalMaterialChance = 0.40f;
        [Range(0f, 1f)] public float NormalXpCoreChance = 0.20f;
        
        [Header("Elite Enemy Drop Rates")]
        [Range(0f, 1f)] public float EliteMaterialChance = 0.40f;
        [Range(0f, 1f)] public float EliteXpCoreChance = 0.20f;
        public int EliteMaterialQuantity = 2;

        [Header("Boss Enemy Drop Rates")]
        public int BossMaterialQuantity = 3;
        public string BossGuaranteedXpCoreID = "xp_core_legendary";
    }

    [System.Serializable]
    public class SpecialEnemyOverride
    {
        public string EnemyUniqueID;
        public string EnemyName; // For convenience and identification
        public bool EnableOverride;
        
        [Header("Override Drops")]
        public string CustomMaterialID;
        public int CustomMaterialQuantity = 1;
        [Range(0f, 1f)] public float CustomMaterialChance = 1.0f;
        
        public string CustomXpCoreID;
        [Range(0f, 1f)] public float CustomXpCoreChance = 0.0f;
    }

    [CreateAssetMenu(fileName = "MaouLootConfig", menuName = "Maou-Sama-TD/Loot Drop Configuration")]
    public class MaouLootConfig : ScriptableObject
    {
        [Header("Default Fallback Config")]
        public EnemyCategory FallbackCategory = EnemyCategory.Bandit;
        
        [Header("XP Core Weight Distributions")]
        [Range(0f, 1f)] public float CommonWeight = 0.75f;
        [Range(0f, 1f)] public float RareWeight = 0.20f;
        [Range(0f, 1f)] public float EpicWeight = 0.05f;

        [Header("Category Loot Rates")]
        public List<CategoryDropSettings> CategorySettings = new List<CategoryDropSettings>();

        [Header("Special Enemy Drop Overrides")]
        public List<SpecialEnemyOverride> SpecialOverrides = new List<SpecialEnemyOverride>();

        /// <summary>
        /// Gets the drop settings for a specific category. Automatically initializes defaults if missing.
        /// </summary>
        public CategoryDropSettings GetSettingsForCategory(EnemyCategory cat)
        {
            if (cat == EnemyCategory.None)
            {
                cat = FallbackCategory;
            }

            var settings = CategorySettings.Find(s => s.Category == cat);
            if (settings == null)
            {
                settings = new CategoryDropSettings
                {
                    Category = cat,
                    PrimaryMaterialID = CategoryToDefaultMaterialID(cat)
                };
                CategorySettings.Add(settings);
            }
            return settings;
        }

        private static string CategoryToDefaultMaterialID(EnemyCategory cat)
        {
            return cat switch
            {
                EnemyCategory.Shadow => "mat_shadow_essence",
                EnemyCategory.Bandit => "mat_bandit_insignia",
                EnemyCategory.Animal => "mat_animal_fang",
                EnemyCategory.Golem  => "mat_golem_core",
                EnemyCategory.Undead => "mat_shadow_essence",
                EnemyCategory.Demon  => "mat_shadow_essence",
                _                    => "mat_bandit_insignia"
            };
        }
    }
}
