using UnityEngine;
using UnityEditor;
using MaouSamaTD.Skills;

namespace MaouSamaTD.Editor
{
    public class GenerateSovereignRitesUtility
    {
        [MenuItem("MaouSamaTD/Generate Sovereign Rites")]
        public static void GenerateSovereignRites()
        {
            string basePath = "Assets/_Game/Data/Skills/SovereignRites";
            
            // Create directories if they don't exist
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Data/Skills"))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Skills");
            }
            if (!AssetDatabase.IsValidFolder(basePath))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Data/Skills", "SovereignRites");
            }
            if (!AssetDatabase.IsValidFolder(basePath + "/Male"))
            {
                AssetDatabase.CreateFolder(basePath, "Male");
            }
            if (!AssetDatabase.IsValidFolder(basePath + "/Female"))
            {
                AssetDatabase.CreateFolder(basePath, "Female");
            }

            // Male Rites
            CreateRite("Male/TyrantsAwakening", "Tyrant's Awakening", 
                "The Maou channels pure, concentrated crimson mana into his Odachi, wreathing the blade in dark, magical flames. Significantly increases his own ATK and Attack Speed... drains his own HP.",
                SkillTargetType.None, SkillEffectType.Buff);
            CreateRite("Male/AbyssalGuillotine", "Abyssal Guillotine", 
                "A focused, high-speed execution strike. The Maou vanishes into a burst of red mist and instantly reappears behind the highest-threat target... Deals massive burst Magic Damage.",
                SkillTargetType.Unit, SkillEffectType.Damage);
            CreateRite("Male/CataclysmicGrandCross", "Cataclysmic Grand Cross", 
                "The Maou steps forward with a terrifying roar and drives his Odachi straight deeply into the earth... Gigantic pillars of crimson, magical fire erupt from the ground in a massive radius around him.",
                SkillTargetType.Ground, SkillEffectType.Damage);

            // Female Rites
            CreateRite("Female/SovereignsDomain", "Sovereign's Domain", 
                "The Maou elegantly alters the local gravity and mana density in a massive radius around herself. Creates a persistent zone where all allied Ranged units gain increased Attack Range and Attack Speed.",
                SkillTargetType.Ground, SkillEffectType.Buff);
            CreateRite("Female/EventHorizon", "Event Horizon", 
                "The Maou points a single finger at a high-value target, condensing dark matter into a microscopic singularity... Deals devastating Single Target Magic Damage and collapses into a miniature black hole.",
                SkillTargetType.Unit, SkillEffectType.Damage);
            CreateRite("Female/StarFallRequiem", "Star-Fall Requiem", 
                "The Maou raises her hands lazily toward the sky, tearing open a rift to the void. She summons a localized, apocalyptic meteor shower of jagged dark-crystal projectiles over a designated area.",
                SkillTargetType.Ground, SkillEffectType.Damage);

            // Save all
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Generated all Sovereign Rites at {basePath}!");
        }

        private static void CreateRite(string relativePath, string name, string description, SkillTargetType targetType, SkillEffectType effectType)
        {
            string fullPath = $"Assets/_Game/Data/Skills/SovereignRites/{relativePath}.asset";
            SovereignRiteData asset = AssetDatabase.LoadAssetAtPath<SovereignRiteData>(fullPath);
            
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SovereignRiteData>();
                AssetDatabase.CreateAsset(asset, fullPath);
            }

            asset.SkillName = name;
            asset.Description = description;
            asset.TargetType = targetType;
            asset.EffectType = effectType;
            // Default Values that can be tweaked later in custom inspector
            asset.SealCost = 50; 
            asset.Cooldown = 30f;
            asset.Range = 100f;
            asset.Duration = 5f;
            
            if (targetType == SkillTargetType.Ground) asset.Radius = 5f;
            if (effectType == SkillEffectType.Damage) asset.Value = 150f;
            if (effectType == SkillEffectType.Buff) asset.Value = 1.5f; // E.g., 50% increase

            EditorUtility.SetDirty(asset);
        }
    }
}
